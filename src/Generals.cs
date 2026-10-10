using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The generals' war.
	//
	// Orders beyond "go there and fight": raid a town's lands, lie in wait on
	// an enemy's road, shadow them, screen a castle, feint at one place and
	// strike another, or refuse battle. Every one of them turns on the
	// commander - his Tactics against the enemy's Scouting, his Valor and his
	// cunning - and the other rulers' commanders think the same way, so a
	// feint draws them and an ambush catches the rash ones.
	internal static class Generals
	{
		private const string StatePrefix = "ht:";   // per-order state
		private const string ThinkPrefix = "hg:";   // an AI general's last think day
		private const string UpkeepPrefix = "hu:";  // the year's upkeep has been asked for

		internal static bool Handles(string order)
		{
			switch (order)
			{
			case "raid":
			case "ambush":
			case "shadow":
			case "screen":
			case "feint":
			case "avoid":
				return true;
			default:
				return false;
			}
		}

		// ------------------------------------------------------------------
		// helpers

		internal static string[] State(Host.Rec r)
		{
			return (Store.Get(StatePrefix + r.Party) ?? "").Split('|');
		}

		internal static void SetState(Host.Rec r, params string[] parts)
		{
			Store.Set(StatePrefix + r.Party, string.Join("|", parts));
		}

		internal static void Clear(string party)
		{
			Store.Set(StatePrefix + party, null);
			Store.Set(ThinkPrefix + party, null);
			Store.Set(UpkeepPrefix + party, null);
		}

		private static int Int(string[] s, int i, int dflt = 0)
		{
			int v;
			return (s.Length > i && int.TryParse(s[i], out v)) ? v : dflt;
		}

		internal static int Skill(Hero h, SkillObject s)
		{
			try
			{
				return (h != null) ? h.GetSkillValue(s) : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static bool Reckless(Hero h)
		{
			try
			{
				return h != null && h.GetTraitLevel(DefaultTraits.Valor) > 0 && h.GetTraitLevel(DefaultTraits.Calculating) <= 0;
			}
			catch
			{
				return false;
			}
		}

		internal static bool Cunning(Hero h)
		{
			try
			{
				return h != null && h.GetTraitLevel(DefaultTraits.Calculating) > 0;
			}
			catch
			{
				return false;
			}
		}

		internal static int Men(MobileParty x)
		{
			if (x == null)
			{
				return 0;
			}
			return (x.Army != null && x.Army.LeaderParty == x) ? (int)x.Army.TotalManCount : x.MemberRoster.TotalManCount;
		}

		// Men weighed by the skill of whoever leads them.
		internal static float Strength(MobileParty x)
		{
			return (float)Men(x) * (1f + (float)Skill((x != null) ? x.LeaderHero : null, DefaultSkills.Tactics) / 300f);
		}

		private static MobileParty Find(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			return MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == id);
		}

		private static bool Enemy(MobileParty x, IFaction mine)
		{
			return x != null && x.IsActive && x.MapFaction != null && mine != null && FactionManager.IsAtWarAgainstFaction(x.MapFaction, mine);
		}

		// Kill a share of a party's (or its whole army's) common soldiers. Heroes
		// are spared: this is arrows and fire, not the battle itself.
		internal static int Casualties(MobileParty x, float share)
		{
			int dead = 0;
			if (x == null || share <= 0f)
			{
				return 0;
			}
			List<MobileParty> parties = (x.Army != null && x.Army.LeaderParty == x) ? x.Army.Parties.ToList() : new List<MobileParty> { x };
			foreach (MobileParty q in parties)
			{
				try
				{
					foreach (TroopRosterElement e in q.MemberRoster.GetTroopRoster().Where((TroopRosterElement t) => t.Character != null && !t.Character.IsHero).ToList())
					{
						int n = MBRandom.RoundRandomized((float)e.Number * share);
						n = Math.Min(n, e.Number);
						if (n <= 0)
						{
							continue;
						}
						int wounded = Math.Min(n, e.WoundedNumber);
						q.MemberRoster.AddToCounts(e.Character, -n, false, -wounded, 0, true, -1);
						dead += n;
					}
				}
				catch (Exception ex)
				{
					Log.Once("gencas" + ((MBObjectBase)q).StringId, "casualties failed: " + ex.Message);
				}
			}
			return dead;
		}

		// enter: go inside (your own walls); otherwise wait outside the gate.
		private static void Go(Host.Rec r, MobileParty p, Settlement s, bool enter = false)
		{
			MobileParty.NavigationType nav = Host.Nav(p, s);
			if (nav == MobileParty.NavigationType.None)
			{
				Host.Embark(r, p, s);
				return;
			}
			if (enter)
			{
				p.SetMoveGoToSettlement(s, nav, false);
			}
			else
			{
				p.SetMoveGoToPoint(s.GatePosition, nav);
			}
		}

		private static void Chase(MobileParty p, MobileParty f)
		{
			p.SetMoveGoToPoint(f.Position, Host.NavAny(p));
		}

		private static void Tell(Host.Rec r, string title, string text)
		{
			if (r.Mine)
			{
				Ravens.Popup(title, text);
			}
		}

		private static string Who(Host.Rec r)
		{
			Hero k = Law.Find(r.Knight);
			return (k != null) ? k.Name.ToString() : "Your commander";
		}

		private static string Leader(MobileParty f)
		{
			return (f != null && f.LeaderHero != null) ? f.LeaderHero.Name.ToString() : "the enemy";
		}

		private static void Free(Host.Rec r, MobileParty p)
		{
			r.Order = "free";
			r.Target = "";
			Host.Save(r);
			Store.Set(StatePrefix + r.Party, null);
			p.Ai.SetDoNotMakeNewDecisions(false);
		}

		private static Settlement Walls(MobileParty p)
		{
			try
			{
				IFaction mine = p.MapFaction;
				return SettlementHelper.FindNearestFortificationToMobileParty(p, MobileParty.NavigationType.Default, (Settlement s) => s.MapFaction == mine);
			}
			catch
			{
				return null;
			}
		}

		private static MobileParty StrongerNear(MobileParty p, float radius)
		{
			IFaction mine = p.MapFaction;
			Vec2 at = p.GetPosition2D;
			float me = Strength(p);
			return MobileParty.AllLordParties.Where((MobileParty x) => Enemy(x, mine) && (x.Army == null || x.Army.LeaderParty == x) && x.GetPosition2D.Distance(at) < radius)
				.Where((MobileParty x) => Strength(x) > me * 1.1f).OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).FirstOrDefault();
		}

		// ------------------------------------------------------------------
		// setting an order that needs more than a target

		internal static void Feint(Host.Rec r, Settlement first, Settlement then)
		{
			MobileParty p = Host.PartyOf(r);
			if (p == null)
			{
				return;
			}
			int days = Math.Max(3, Math.Min(8, (int)(p.GetPosition2D.Distance(first.GetPosition2D) / 40f) + 2));
			Host.SetOrder(r, "feint", ((MBObjectBase)first).StringId);
			SetState(r, ((MBObjectBase)then).StringId, (CourtBehavior.Today() + days).ToString());
		}

		// ------------------------------------------------------------------
		// daily: keep them to the order

		internal static void Enforce(Host.Rec r, MobileParty p, bool fresh)
		{
			IFaction mine = p.MapFaction;
			switch (r.Order)
			{
			case "raid":
				Raid(r, p);
				break;
			case "ambush":
			{
				MobileParty f = Find(r.Target);
				if (!Enemy(f, mine))
				{
					Tell(r, "The Ambush Is Off", "The men " + Who(r) + " lay in wait for are gone from the field.");
					Free(r, p);
					return;
				}
				string[] st = State(r);
				int start = Int(st, 0, -1);
				if (fresh || start < 0)
				{
					start = CourtBehavior.Today();
				}
				if (CourtBehavior.Today() - start > 5)
				{
					Tell(r, "The Ambush Is Off", Leader(f) + " never came down that road. " + Who(r) + " waits for new orders.");
					Log.Write("ambush: " + p.Name + " gave up on " + f.Name);
					Free(r, p);
					return;
				}
				// Wait where they are going: the place they march on, or failing
				// that the castle nearest them.
				Settlement lie = f.TargetSettlement;
				if (lie == null)
				{
					lie = SettlementHelper.FindNearestFortificationToMobileParty(f, MobileParty.NavigationType.Default, null);
				}
				SetState(r, start.ToString(), (lie != null) ? ((MBObjectBase)lie).StringId : "");
				p.Ai.SetDoNotMakeNewDecisions(true);
				if (lie != null && p.GetPosition2D.Distance(lie.GetPosition2D) > 6f)
				{
					Go(r, p, lie);
				}
				else
				{
					p.SetMoveGoToPoint(p.Position, MobileParty.NavigationType.Default);
				}
				break;
			}
			case "shadow":
			case "avoid":
			{
				MobileParty f = Find(r.Target);
				if (!Enemy(f, mine))
				{
					Tell(r, "Lost Them", "The enemy " + Who(r) + " was following is no longer in the field.");
					Free(r, p);
					return;
				}
				p.Ai.SetDoNotMakeNewDecisions(true);
				Shadow(r, p, f);
				break;
			}
			case "screen":
			{
				Settlement s = Settlement.Find(r.Target);
				if (s == null || s.MapFaction != mine)
				{
					Free(r, p);
					return;
				}
				p.Ai.SetDoNotMakeNewDecisions(true);
				if (fresh || p.DefaultBehavior != AiBehavior.PatrolAroundPoint)
				{
					MobileParty.NavigationType nav = Host.Nav(p, s);
					if (nav == MobileParty.NavigationType.None)
					{
						Host.Embark(r, p, s);
						return;
					}
					p.SetMovePatrolAroundSettlement(s, nav, false);
				}
				break;
			}
			case "feint":
			{
				Settlement first = Settlement.Find(r.Target);
				string[] st = State(r);
				Settlement then = (st.Length > 0) ? Settlement.Find(st[0]) : null;
				int turn = Int(st, 1, 0);
				if (then == null || then.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(then.MapFaction, mine))
				{
					Free(r, p);
					return;
				}
				if (first == null || CourtBehavior.Today() >= turn)
				{
					r.Order = "siege";
					r.Target = ((MBObjectBase)then).StringId;
					Host.Save(r);
					Store.Set(StatePrefix + r.Party, null);
					Log.Write("feint: " + p.Name + " turns from " + ((first != null) ? first.Name.ToString() : "?") + " to " + then.Name);
					Tell(r, "The Feint", Who(r) + " has turned the host about in the night. They march on " + then.Name + " now" + ((first != null) ? (", and whoever rode to save " + first.Name + " is on the wrong road.") : "."));
					Host.Enforce(r, p, true);
					return;
				}
				p.Ai.SetDoNotMakeNewDecisions(true);
				// March openly on the first place - not a siege, just the look of one.
				Go(r, p, first);
				break;
			}
			}
		}

		// ------------------------------------------------------------------
		// raiding

		private static void Raid(Host.Rec r, MobileParty p)
		{
			Settlement town = Settlement.Find(r.Target);
			IFaction mine = p.MapFaction;
			if (town == null || town.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(town.MapFaction, mine))
			{
				Free(r, p);
				return;
			}
			string[] st = State(r);
			Village current = (st.Length > 0 && st[0] != "") ? town.BoundVillages.FirstOrDefault((Village v) => ((MBObjectBase)v.Settlement).StringId == st[0]) : null;
			int burned = Int(st, 1, 0);
			if (current != null && current.VillageState == Village.VillageStates.Looted)
			{
				burned++;
				try
				{
					if (town.Town != null)
					{
						town.Town.FoodStocks = Math.Max(0f, town.Town.FoodStocks - 30f);
					}
				}
				catch
				{
				}
				if (r.Mine)
				{
					Standing.Change(-1, 1, "Your host burned " + current.Name);
				}
				Log.Write("raid: " + p.Name + " burned " + current.Name + " (" + burned + " of " + town.Name + "'s villages)");
				current = null;
			}
			if (current == null || current.VillageState != Village.VillageStates.Normal && current.VillageState != Village.VillageStates.BeingRaided)
			{
				current = town.BoundVillages.Where((Village v) => v.VillageState == Village.VillageStates.Normal)
					.OrderBy((Village v) => v.Settlement.GetPosition2D.Distance(p.GetPosition2D)).FirstOrDefault();
			}
			if (current == null)
			{
				Log.Write("raid: " + p.Name + " has burned the lands of " + town.Name);
				Tell(r, "The Lands Are Ash", "Every village of " + town.Name + " is burned. Its granaries will feel it. " + Who(r) + " waits for new orders.");
				Free(r, p);
				return;
			}
			SetState(r, ((MBObjectBase)current.Settlement).StringId, burned.ToString());
			p.Ai.SetDoNotMakeNewDecisions(true);
			if (p.MapEvent == null)
			{
				MobileParty.NavigationType nav = Host.Nav(p, current.Settlement);
				if (nav == MobileParty.NavigationType.None)
				{
					Host.Embark(r, p, current.Settlement);
					return;
				}
				p.SetMoveRaidSettlement(current.Settlement, nav, false);
			}
		}

		// ------------------------------------------------------------------
		// shadowing, and refusing battle

		private static void Shadow(Host.Rec r, MobileParty p, MobileParty f)
		{
			if (r.Order == "avoid")
			{
				MobileParty danger = StrongerNear(p, 25f);
				if (danger != null)
				{
					Settlement walls = Walls(p);
					if (walls != null && p.CurrentSettlement != walls)
					{
						Go(r, p, walls, true);
					}
					return;
				}
			}
			float d = p.GetPosition2D.Distance(f.GetPosition2D);
			if (d > 14f)
			{
				Chase(p, f);
			}
			else if (d < 6f)
			{
				p.SetMoveGoToPoint(p.Position, MobileParty.NavigationType.Default);
			}
		}

		// ------------------------------------------------------------------
		// hourly: the things that cannot wait for morning

		internal static void Hourly(Host.Rec r, MobileParty p)
		{
			if (p.MapEvent != null || p.CurrentSettlement != null && r.Order != "screen" && r.Order != "avoid")
			{
				return;
			}
			IFaction mine = p.MapFaction;
			switch (r.Order)
			{
			case "ambush":
			{
				MobileParty f = Find(r.Target);
				if (!Enemy(f, mine) || f.MapEvent != null)
				{
					return;
				}
				if (p.GetPosition2D.Distance(f.GetPosition2D) < 10f)
				{
					Spring(r, p, f);
				}
				break;
			}
			case "shadow":
			case "avoid":
			{
				MobileParty f = Find(r.Target);
				if (!Enemy(f, mine))
				{
					return;
				}
				Shadow(r, p, f);
				if (r.Order == "shadow" && f.BesiegedSettlement != null)
				{
					string[] st = State(r);
					string siege = ((MBObjectBase)f.BesiegedSettlement).StringId;
					if (st.Length > 0 && st[0] == siege)
					{
						return;
					}
					SetState(r, siege);
					AtTheWalls(r, p, f);
				}
				break;
			}
			case "screen":
			{
				Settlement s = Settlement.Find(r.Target);
				if (s == null)
				{
					return;
				}
				Vec2 at = s.GetPosition2D;
				MobileParty intruder = MobileParty.AllLordParties.Where((MobileParty x) => Enemy(x, mine) && (x.Army == null || x.Army.LeaderParty == x) && x.MapEvent == null && x.GetPosition2D.Distance(at) < 25f)
					.OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).FirstOrDefault();
				if (intruder == null)
				{
					if (p.CurrentSettlement == s || p.DefaultBehavior != AiBehavior.PatrolAroundPoint)
					{
						p.SetMovePatrolAroundSettlement(s, MobileParty.NavigationType.Default, false);
					}
					return;
				}
				if (Strength(p) > Strength(intruder) * 1.1f)
				{
					if (p.TargetParty != intruder)
					{
						Log.Write("screen: " + p.Name + " falls on " + intruder.Name + " near " + s.Name);
						p.SetMoveEngageParty(intruder, MobileParty.NavigationType.Default);
					}
				}
				else if (p.CurrentSettlement != s)
				{
					if (p.TargetSettlement != s)
					{
						Log.Write("screen: " + p.Name + " falls back into " + s.Name + " before " + intruder.Name);
						Tell(r, "Falling Back", Who(r) + " has seen " + Leader(intruder) + "'s " + Men(intruder).ToString("N0") + " men coming, too many to meet in the open, and has fallen back into " + s.Name + ".");
					}
					p.SetMoveGoToSettlement(s, MobileParty.NavigationType.Default, false);
				}
				break;
			}
			}
		}

		// The ambush: his Tactics against their Scouting.
		private static void Spring(Host.Rec r, MobileParty p, MobileParty f)
		{
			Hero ours = p.LeaderHero;
			Hero theirs = f.LeaderHero;
			int tac = Skill(ours, DefaultSkills.Tactics);
			int scout = Skill(theirs, DefaultSkills.Scouting);
			float chance = 50f + (float)(tac - scout) / 3f;
			if (Reckless(theirs))
			{
				chance += 15f;
			}
			if (Cunning(theirs))
			{
				chance -= 10f;
			}
			chance = Math.Max(15f, Math.Min(90f, chance));
			bool sprung = MBRandom.RandomFloat * 100f < chance;
			r.Order = "engage";
			r.Target = ((MBObjectBase)f).StringId;
			Host.Save(r);
			Store.Set(StatePrefix + r.Party, null);
			if (sprung)
			{
				float share = ((float)Cfg.AmbushMinPercent + (float)(Cfg.AmbushMaxPercent - Cfg.AmbushMinPercent) * Math.Min(1f, (float)tac / 250f)) / 100f;
				int dead = Casualties(f, share);
				Log.Write("ambush: sprung on " + f.Name + ", -" + dead + " men (tac " + tac + " vs scout " + scout + ", chance " + (int)chance + "%)");
				string text = Who(r) + " sprang the trap on " + Leader(f) + ". " + dead.ToString("N0") + " of them fell before they could form a line, and the rest are fighting for their lives.";
				Tell(r, "The Ambush Is Sprung", text);
				if (!r.Mine && f.MapFaction == Clan.PlayerClan.MapFaction && f.LeaderHero != null && f.LeaderHero.Clan == Clan.PlayerClan)
				{
					Ravens.Popup("Ambushed", "Your men under " + Leader(f) + " were caught on the road by " + ((ours != null) ? ours.Name.ToString() : "the enemy") + ". " + dead.ToString("N0") + " fell before the fighting began.");
				}
			}
			else
			{
				Log.Write("ambush: seen through by " + f.Name + " (tac " + tac + " vs scout " + scout + ", chance " + (int)chance + "%)");
				Tell(r, "The Ambush Is Seen", Leader(f) + "'s outriders found the trap before it closed. There is no surprise now - " + Who(r) + " goes at them in the open.");
			}
			p.Ai.SetDoNotMakeNewDecisions(true);
			p.SetMoveEngageParty(f, Host.NavAny(p));
		}

		// The shadowed enemy has sat down before a castle: now is the moment.
		private static void AtTheWalls(Host.Rec r, MobileParty p, MobileParty f)
		{
			Settlement s = f.BesiegedSettlement;
			Action strike = delegate
			{
				MobileParty q = Host.PartyOf(r);
				if (q == null || !q.IsActive || f == null || !f.IsActive)
				{
					return;
				}
				int dead = Casualties(f, (10f + MBRandom.RandomFloat * 5f) / 100f);
				Log.Write("shadow: " + q.Name + " fell on the siege camp of " + f.Name + " at " + ((s != null) ? s.Name.ToString() : "?") + ", -" + dead);
				r.Order = "engage";
				r.Target = ((MBObjectBase)f).StringId;
				Host.Save(r);
				Store.Set(StatePrefix + r.Party, null);
				q.SetMoveEngageParty(f, Host.NavAny(q));
				if (r.Mine)
				{
					Flow.Notify(dead.ToString("N0") + " of " + Leader(f) + "'s men died in their siege lines before they could turn around.");
				}
			};
			if (!r.Mine)
			{
				strike();
				return;
			}
			Inquiry.Confirm("At the Walls", Leader(f) + " has sat down before " + ((s != null) ? s.Name.ToString() : "a castle") + " with " + Men(f).ToString("N0") + " men, and " + Who(r) + " is close behind with " + p.MemberRoster.TotalManCount.ToString("N0") +
				". Their backs are to us and their camp is full of ladders, not spears.\n\nStrike now?", "Strike", "Keep watching", strike, null);
		}

		// ------------------------------------------------------------------
		// upkeep: a year's bread and pay

		private static int Cull(MobileParty p, float share)
		{
			int gone = 0;
			foreach (TroopRosterElement e in p.MemberRoster.GetTroopRoster().Where((TroopRosterElement t) => t.Character != null && !t.Character.IsHero).ToList())
			{
				int n = (int)(e.Number * share);
				if (n > 0)
				{
					p.MemberRoster.AddToCounts(e.Character, -n, false, -Math.Min(n, e.WoundedNumber), 0, true, -1);
					gone += n;
				}
			}
			return gone;
		}

		internal static int UpkeepCost(Host.Rec r, MobileParty p)
		{
			int alive = Math.Max(0, ((p != null) ? p.MemberRoster.TotalManCount : 0) - r.Base);
			long cost = (long)r.Price * Cfg.HostUpkeepPercent / 100 * alive / Math.Max(1, r.Raised);
			return (int)Math.Min(int.MaxValue, Math.Max(0L, cost));
		}

		private static readonly HashSet<string> _askedThisSession = new HashSet<string>();

		// Returns true if the host is gone.
		internal static bool MyUpkeep(Host.Rec r, MobileParty p, Hero knight, int today)
		{
			bool asked = !string.IsNullOrEmpty(Store.Get(UpkeepPrefix + r.Party));
			if (!asked && r.End - today <= 7)
			{
				if (r.End - today < 3)
				{
					// A host from before the reckoning: give them a few days.
					r.End = today + 3;
					Host.Save(r);
				}
				Store.Set(UpkeepPrefix + r.Party, "1");
				_askedThisSession.Add(r.Party);
				AskUpkeep(r, p, knight);
				return false;
			}
			if (asked && !_askedThisSession.Contains(r.Party))
			{
				// The question may have been lost to a load; ask it again before they go.
				_askedThisSession.Add(r.Party);
				if (today >= r.End)
				{
					r.End = today + 3;
					Host.Save(r);
				}
				AskUpkeep(r, p, knight);
				return false;
			}
			if (asked && today >= r.End)
			{
				Desert(r, p, "The year's upkeep went unpaid.");
				return true;
			}
			return false;
		}

		internal static void AskUpkeep(Host.Rec r, MobileParty p, Hero knight)
		{
			int cost = UpkeepCost(r, p);
			Inquiry.Confirm("A Year's Upkeep", ((knight != null) ? knight.Name.ToString() : "Your commander") + "'s host of " + p.MemberRoster.TotalManCount.ToString("N0") + " needs feeding and paying for another year: " + cost.ToString("N0") +
				" gold, due in " + Math.Max(0, r.End - CourtBehavior.Today()) + " days.\n\nUnpaid, they will not go home quietly - they will scatter across the countryside as deserters.",
				"Pay them", "Let them go", delegate
				{
					PayUpkeep(r);
				}, delegate
				{
					MobileParty q = Host.PartyOf(r);
					if (q != null)
					{
						Desert(r, q, "You would not pay their upkeep.");
					}
				});
		}

		internal static void PayUpkeep(Host.Rec r)
		{
			MobileParty p = Host.PartyOf(r);
			if (p == null)
			{
				return;
			}
			int cost = UpkeepCost(r, p);
			if (Hero.MainHero.Gold < cost)
			{
				Flow.Notify("You cannot pay them. They will wait until the day it is due, and not an hour longer.");
				return;
			}
			Hero.MainHero.ChangeHeroGold(-cost);
			r.End = Math.Max(r.End, CourtBehavior.Today()) + Cfg.DaysPerYear;
			r.Warned = false;
			Host.Save(r);
			Store.Set(UpkeepPrefix + r.Party, null);
			Log.Write("upkeep: paid " + cost + " for " + p.Name + ", next due day " + r.End);
			Flow.Notify("The host is fed and paid for another year.");
		}

		// Another ruler's host: the ruler pays, or borrows, or loses it.
		internal static bool TheirUpkeep(Host.Rec r, MobileParty p, int today)
		{
			if (today < r.End)
			{
				return false;
			}
			int cost = UpkeepCost(r, p);
			Clan owner = r.OwnerClan;
			Kingdom k = (owner != null) ? owner.Kingdom : null;
			Hero ruler = (k != null) ? k.Leader : ((owner != null) ? owner.Leader : null);
			if (ruler != null && ruler.Gold < cost && k != null)
			{
				try
				{
					IronBank.AiBorrow(k, ruler, cost - ruler.Gold);
				}
				catch
				{
				}
			}
			if (ruler != null && ruler.Gold >= cost)
			{
				ruler.ChangeHeroGold(-cost);
				r.End = today + Cfg.DaysPerYear;
				r.Warned = false;
				Host.Save(r);
				Log.Write("upkeep: " + ((k != null) ? k.Name.ToString() : "?") + " paid " + cost + " for " + p.Name);
				return false;
			}
			if (!r.Warned)
			{
				// One more season of grace, on short rations: some go home.
				r.Warned = true;
				r.End = today + Cfg.DaysPerYear / 4;
				Host.Save(r);
				int gone = Cull(p, 0.2f);
				Log.Write("upkeep: " + ((k != null) ? k.Name.ToString() : "?") + " could not pay " + cost + " for " + p.Name + " - " + gone + " men went home; a season's grace");
				return false;
			}
			Desert(r, p, "their ruler could not pay them");
			return true;
		}

		// Unpaid men do not go home: they take to the roads.
		internal static void Desert(Host.Rec r, MobileParty p, string why)
		{
			int loose = 0;
			int bands = 0;
			try
			{
				int excess = p.MemberRoster.TotalManCount - Math.Max(r.Base, 0);
				List<TroopRosterElement> troops = p.MemberRoster.GetTroopRoster().Where((TroopRosterElement t) => t.Character != null && !t.Character.IsHero).OrderByDescending((TroopRosterElement t) => t.Number).ToList();
				bands = Math.Max(1, Math.Min(8, excess / 1000));
				Clan bandits = Clan.All.FirstOrDefault((Clan c) => ((MBObjectBase)c).StringId == "looters") ?? Clan.All.FirstOrDefault((Clan c) => c.IsBanditFaction);
				Settlement near = SettlementHelper.FindNearestSettlementToMobileParty(p, MobileParty.NavigationType.Default, (Settlement s) => s.IsVillage || s.IsTown);
				if (near == null)
				{
					near = BanditHome.Nearest(p);
				}
				List<MobileParty> made = new List<MobileParty>();
				if (bandits != null && bandits.DefaultPartyTemplate != null && near != null && excess > 0)
				{
					for (int i = 0; i < bands; i++)
					{
						try
						{
							MobileParty band = TaleWorlds.CampaignSystem.Party.PartyComponents.BanditPartyComponent.CreateLooterParty("wad_deserters_" + MBRandom.RandomInt(1, int.MaxValue), bandits, near, false, bandits.DefaultPartyTemplate, p.Position);
							if (band != null)
							{
								band.MemberRoster.Clear();
								made.Add(band);
							}
						}
						catch (Exception e)
						{
							Log.Once("deserterband", "a deserter band could not be made: " + e.Message);
						}
					}
				}
				int left = excess;
				int i2 = 0;
				foreach (TroopRosterElement e in troops)
				{
					if (left <= 0)
					{
						break;
					}
					int n = Math.Min(left, e.Number);
					int wounded = Math.Min(n, e.WoundedNumber);
					p.MemberRoster.AddToCounts(e.Character, -n, false, -wounded, 0, true, -1);
					left -= n;
					if (made.Count > 0)
					{
						// Share them out among the bands.
						int each = Math.Max(1, n / made.Count);
						int given = 0;
						for (int j = 0; j < made.Count && given < n; j++)
						{
							int m = (j == made.Count - 1) ? (n - given) : Math.Min(each, n - given);
							if (m > 0)
							{
								made[(j + i2) % made.Count].MemberRoster.AddToCounts(e.Character, m, false, 0, 0, true, -1);
								given += m;
								loose += m;
							}
						}
						i2++;
					}
				}
				foreach (MobileParty band in made.Where((MobileParty x) => x.MemberRoster.TotalManCount == 0).ToList())
				{
					try
					{
						TaleWorlds.CampaignSystem.Actions.DestroyPartyAction.Apply(null, band);
					}
					catch
					{
					}
				}
				bands = made.Count((MobileParty x) => x.IsActive && x.MemberRoster.TotalManCount > 0);
			}
			catch (Exception e)
			{
				Log.Write("the host could not desert cleanly: " + e.Message);
			}
			string where = (p.LastVisitedSettlement != null) ? (" near " + p.LastVisitedSettlement.Name) : "";
			Log.Write("host deserted (" + r.Party + "): " + why + " - " + loose + " men in " + bands + " band(s)" + where);
			if (r.Mine)
			{
				Standing.Change(-2, 0, "Your host went unpaid and turned to banditry");
				Store.AddDeed(Standing.Date() + "  " + Who(r) + "'s host went unpaid and turned to banditry.");
				Host.StandDown(r, why + " " + loose.ToString("N0") + " of them have scattered" + where + " in " + bands + " bands, and live off whatever they can take.");
			}
			else
			{
				Host.Disperse(r, why + "; the men turned deserter");
			}
		}

		// ------------------------------------------------------------------
		// the other rulers' generals

		// Returns true if it gave the host something to do.
		internal static bool Think(Host.Rec r, MobileParty p, int today)
		{
			if (!Cfg.Generals || !Cfg.AiGenerals || p.MapEvent != null || p.BesiegedSettlement != null)
			{
				return false;
			}
			Hero lord = p.LeaderHero;
			IFaction mine = p.MapFaction;
			Vec2 at = p.GetPosition2D;
			bool reckless = Reckless(lord);
			bool cunning = Cunning(lord);

			// 1. A threat to one of our own castles comes first.
			MobileParty threat = MobileParty.AllLordParties.Where((MobileParty x) => Enemy(x, mine) && (x.Army == null || x.Army.LeaderParty == x) && x.TargetSettlement != null && x.TargetSettlement.IsFortification
				&& x.TargetSettlement.MapFaction == mine && (Host.Is(x) || x.Army != null) && x.GetPosition2D.Distance(at) < 250f).OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).FirstOrDefault();
			if (threat != null && !(r.Order == "hold" && r.Target == ((MBObjectBase)threat.TargetSettlement).StringId))
			{
				if (cunning && MBRandom.RandomFloat < 0.5f)
				{
					Log.Write("general: " + Name(lord) + " is not fooled by " + threat.Name + "'s march on " + threat.TargetSettlement.Name);
				}
				else
				{
					Log.Write("general: " + Name(lord) + " sees " + threat.Name + " marching on " + threat.TargetSettlement.Name + " - moves to defend");
					Order(r, p, "hold", ((MBObjectBase)threat.TargetSettlement).StringId);
					// A defence lasts at least one thinking spell.
					Store.SetI(ThinkPrefix + r.Party, today);
					return true;
				}
			}

			// Otherwise think only when idle, or every five days.
			int last = Store.GetI(ThinkPrefix + r.Party, -999);
			if (r.Order != "free" && today - last < 5)
			{
				return false;
			}
			Store.SetI(ThinkPrefix + r.Party, today);

			// 2. An enemy host near: fight, dodge, trap it, or draw it out.
			MobileParty foe = Host.Foes(mine, at).FirstOrDefault((MobileParty x) => Host.Is(x) && x.GetPosition2D.Distance(at) < 200f);
			if (foe != null)
			{
				float ratio = Strength(p) / Math.Max(1f, Strength(foe));
				float fight = reckless ? 0.9f : (cunning ? 1.4f : 1.2f);
				float flee = reckless ? 0.5f : (cunning ? 0.9f : 0.7f);
				bool playerHost = foe.LeaderHero != null && foe.LeaderHero.Clan == Clan.PlayerClan;
				if (ratio >= fight)
				{
					// A cunning general lays a trap rather than charging.
					if ((cunning || MBRandom.RandomFloat < 0.25f) && foe.TargetSettlement != null)
					{
						Log.Write("general: " + Name(lord) + " lays an ambush for " + foe.Name + " (x" + ratio.ToString("0.00") + ")");
						Order(r, p, "ambush", ((MBObjectBase)foe).StringId);
						if (playerHost)
						{
							Warn(foe, Name(lord) + "'s host is lying in wait on the road ahead.");
						}
					}
					else
					{
						Log.Write("general: " + Name(lord) + " attacks " + foe.Name + " (x" + ratio.ToString("0.00") + ")");
						Order(r, p, "engage", ((MBObjectBase)foe).StringId);
					}
					return true;
				}
				if (ratio < flee)
				{
					Log.Write("general: " + Name(lord) + " refuses battle with " + foe.Name + " (x" + ratio.ToString("0.00") + ")");
					Order(r, p, "avoid", ((MBObjectBase)foe).StringId);
					return true;
				}
				// Evenly matched: burn their lands and make them come.
				Settlement bait = RaidTarget(p, foe.MapFaction);
				if (bait != null)
				{
					Log.Write("general: " + Name(lord) + " raids " + bait.Name + " to draw out " + foe.Name);
					Order(r, p, "raid", ((MBObjectBase)bait).StringId);
					if (playerHost)
					{
						Warn(foe, Name(lord) + "'s host is turning towards the lands of " + bait.Name + " to burn them.");
					}
					return true;
				}
			}

			// 3. Nothing near: a castle, or now and then a raid.
			if (r.Order == "free" || today - last >= 5)
			{
				if (MBRandom.RandomFloat < 0.2f)
				{
					Settlement bait = RaidTarget(p, null);
					if (bait != null)
					{
						Log.Write("general: " + Name(lord) + " raids the lands of " + bait.Name);
						Order(r, p, "raid", ((MBObjectBase)bait).StringId);
						return true;
					}
				}
			}
			// Nothing to guard against and nobody to dodge: back to the war.
			if (r.Order == "hold" && StillThreatened(r, mine))
			{
				return false;
			}
			if (r.Order == "hold" || r.Order == "avoid" || r.Order == "shadow" || r.Order == "screen")
			{
				Log.Write("general: " + Name(lord) + " sees no more need to " + r.Order + " - free to campaign");
				Free(r, p);
			}
			return false;
		}

		// An enemy army or host still near the castle a hold guards.
		private static bool StillThreatened(Host.Rec r, IFaction mine)
		{
			Settlement s = Settlement.Find(r.Target);
			if (s == null)
			{
				return false;
			}
			Vec2 at = s.GetPosition2D;
			return MobileParty.AllLordParties.Any((MobileParty x) => Enemy(x, mine) && (Host.Is(x) || (x.Army != null && x.Army.LeaderParty == x)) && x.GetPosition2D.Distance(at) < 60f);
		}

		private static string Name(Hero h)
		{
			return (h != null) ? h.Name.ToString() : "a lord";
		}

		private static void Order(Host.Rec r, MobileParty p, string order, string target)
		{
			if (r.Order == order && r.Target == target)
			{
				return;
			}
			r.Order = order;
			r.Target = target;
			Host.Save(r);
			Store.Set(StatePrefix + r.Party, null);
			Host.Enforce(r, p, true);
		}

		private static Settlement RaidTarget(MobileParty p, IFaction of)
		{
			IFaction mine = p.MapFaction;
			Vec2 at = p.GetPosition2D;
			return Settlement.All.Where((Settlement s) => s.IsFortification && s.MapFaction != null && FactionManager.IsAtWarAgainstFaction(s.MapFaction, mine) && (of == null || s.MapFaction == of)
				&& s.BoundVillages.Any((Village v) => v.VillageState == Village.VillageStates.Normal)).OrderBy((Settlement s) => s.GetPosition2D.Distance(at)).FirstOrDefault();
		}

		// A good scout in your host sees it coming.
		private static void Warn(MobileParty mineParty, string what)
		{
			Hero k = mineParty.LeaderHero;
			if (k != null && Skill(k, DefaultSkills.Scouting) > 80)
			{
				Ravens.Popup("Scouts' Report", k.Name + "'s outriders report: " + what);
			}
		}
	}
}
