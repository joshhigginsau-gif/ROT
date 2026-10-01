using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// War Sails only lets a ship's crew fight. A fleet that is sunk floats home
	// on rafts with every man who never boarded, and a won blockade battle never
	// touches the town. Here a lost sea battle costs the loser what it should:
	// the men who were on the water drown or are taken, and a garrison whose
	// relief was beaten loses heart. Done an hour after the battle, once War
	// Sails has finished putting the losers on their rafts.
	internal static class NavalRout
	{
		private sealed class Pending
		{
			internal string Party = "";
			internal string Winner = "";
			internal string Relief = "";
			internal int Due;
		}

		private static readonly List<Pending> _queue = new List<Pending>();

		internal static void OnMapEventEnded(MapEvent me)
		{
			try
			{
				if (!Cfg.NavalRout || me == null || !me.IsNavalMapEvent || !me.HasWinner)
				{
					return;
				}
				MapEventSide lost = me.GetMapEventSide(me.DefeatedSide);
				MapEventSide won = me.GetMapEventSide(me.WinningSide);
				if (lost == null || won == null)
				{
					return;
				}
				MobileParty winner = (won.LeaderParty != null) ? won.LeaderParty.MobileParty : null;
				string relief = "";
				// a blockade relief beaten: the relievers attack, the besiegers defend
				if (me.EventType == MapEvent.BattleTypes.BlockadeBattle && me.WinningSide == BattleSideEnum.Defender)
				{
					Settlement s = me.MapEventSettlement;
					if (s == null && winner != null)
					{
						s = winner.BesiegedSettlement;
					}
					if (s != null)
					{
						relief = ((MBObjectBase)s).StringId;
					}
				}
				int due = (int)CampaignTime.Now.ToHours + 1;
				foreach (MapEventParty mp in lost.Parties)
				{
					MobileParty p = (mp.Party != null) ? mp.Party.MobileParty : null;
					if (p == null || p == MobileParty.MainParty || p.IsGarrison)
					{
						continue;
					}
					_queue.Add(new Pending { Party = ((MBObjectBase)p).StringId, Winner = (winner != null) ? ((MBObjectBase)winner).StringId : "", Relief = relief, Due = due });
					relief = "";
				}
				if (relief != "")
				{
					_queue.Add(new Pending { Relief = relief, Due = due });
				}
			}
			catch (Exception e)
			{
				Log.Write("naval rout: reading the battle failed: " + e.Message);
			}
		}

		internal static void Hourly()
		{
			if (_queue.Count == 0)
			{
				return;
			}
			int now = (int)CampaignTime.Now.ToHours;
			foreach (Pending r in _queue.Where((Pending x) => now >= x.Due).ToList())
			{
				_queue.Remove(r);
				try
				{
					Settle(r);
				}
				catch (Exception e)
				{
					Log.Write("naval rout: settling failed: " + e.Message);
				}
			}
		}

		private static MobileParty Find(string id)
		{
			return string.IsNullOrEmpty(id) ? null : MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == id);
		}

		private static void Settle(Pending r)
		{
			MobileParty p = Find(r.Party);
			if (p != null && p.IsActive && p.MapEvent == null)
			{
				bool sunk = p.IsInRaftState || p.Ships == null || p.Ships.Count == 0;
				int pct = sunk ? Cfg.NavalRoutLosses : Cfg.NavalRetreatLosses;
				MobileParty winner = Find(r.Winner);
				int before = p.MemberRoster.TotalManCount - p.MemberRoster.TotalHeroes;
				int taken;
				int gone = Cull(p.MemberRoster, pct, (winner != null && winner.IsActive) ? winner.PrisonRoster : null, out taken);
				if (gone > 0)
				{
					Log.Write("naval rout: " + p.Name + (sunk ? " lost all its ships" : " fled with what ships it had") + " - " + gone + " of " + before + " men drowned or taken (" + taken + " prisoners to " + ((winner != null) ? winner.Name.ToString() : "nobody") + ")");
				}
			}
			if (r.Relief != "")
			{
				Settlement s = Settlement.Find(r.Relief);
				MobileParty g = (s != null && s.Town != null) ? s.Town.GarrisonParty : null;
				if (g != null && s.IsUnderSiege)
				{
					int taken;
					int gone = Cull(g.MemberRoster, Cfg.NavalReliefGarrison, null, out taken);
					Log.Write("naval relief failed at " + s.Name + ": garrison -" + gone);
					if (gone > 0 && s.SiegeEvent != null && s.SiegeEvent.BesiegerCamp != null && s.SiegeEvent.BesiegerCamp.LeaderParty == MobileParty.MainParty)
					{
						Flow.Notify("The relief fleet is beaten. " + gone + " of " + s.Name + "'s garrison go over the walls in the night.");
					}
				}
			}
		}

		// Takes pct% of every common troop line; a share of them (a third) go to
		// the prison roster given, the rest are lost.
		private static int Cull(TroopRoster roster, int pct, TroopRoster prison, out int taken)
		{
			taken = 0;
			if (roster == null || pct <= 0)
			{
				return 0;
			}
			int gone = 0;
			foreach (TroopRosterElement e in roster.GetTroopRoster().ToList())
			{
				if (e.Character == null || e.Character.IsHero || e.Number <= 0)
				{
					continue;
				}
				int n = e.Number * pct / 100;
				if (n <= 0)
				{
					continue;
				}
				int w = Math.Max(0, e.WoundedNumber - (e.Number - n));
				roster.AddToCounts(e.Character, -n, false, -w);
				gone += n;
				if (prison != null)
				{
					int t = n / 3;
					if (t > 0)
					{
						prison.AddToCounts(e.Character, t);
						taken += t;
					}
				}
			}
			return gone;
		}
	}
}
