using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// A host is not bought, it is summoned: the gold is paid when the call
	// goes out, and the men stand a year later (host_muster_days). The same
	// for other rulers and for the Iron Bank's hosts - so a threat can be seen
	// coming a year off.
	internal static class Muster
	{
		private const string Prefix = "hm:";
		private const int Grace = 30;

		// owner|commander|quality|men|cost|ready|hunt|funded|threatened
		internal sealed class Rec
		{
			internal string Id = "";
			internal string Owner = "";
			internal string Commander = "";
			internal string Quality = Host.Men;
			internal int Men;
			internal int Cost;
			internal int Ready;
			internal string Hunt = "";
			internal bool Funded;
			internal bool Threatened;

			internal string Pack()
			{
				return string.Join("|", new string[9] { Owner, Commander, Quality, Men.ToString(), Cost.ToString(), Ready.ToString(), Hunt, Funded ? "1" : "0", Threatened ? "1" : "0" });
			}

			internal static Rec Unpack(string id, string s)
			{
				string[] p = (s ?? "").Split('|');
				if (p.Length < 9)
				{
					return null;
				}
				Rec r = new Rec();
				r.Id = id;
				r.Owner = p[0];
				r.Commander = p[1];
				r.Quality = p[2];
				int.TryParse(p[3], out r.Men);
				int.TryParse(p[4], out r.Cost);
				int.TryParse(p[5], out r.Ready);
				r.Hunt = p[6];
				r.Funded = p[7] == "1";
				r.Threatened = p[8] == "1";
				return r;
			}

			internal bool Mine
			{
				get
				{
					return Clan.PlayerClan != null && Owner == ((MBObjectBase)Clan.PlayerClan).StringId;
				}
			}

			internal Clan OwnerClan
			{
				get
				{
					return Sworn.Find(Owner);
				}
			}
		}

		internal static List<Rec> All()
		{
			List<Rec> list = new List<Rec>();
			foreach (string key in Store.Keys(Prefix))
			{
				Rec r = Rec.Unpack(key.Substring(Prefix.Length), Store.Get(key));
				if (r != null)
				{
					list.Add(r);
				}
			}
			return list;
		}

		private static void Save(Rec r)
		{
			Store.Set(Prefix + r.Id, r.Pack());
		}

		private static void Drop(Rec r)
		{
			Store.Set(Prefix + r.Id, null);
		}

		private static string NewId()
		{
			int n = Store.GetI("hmx:next", 1);
			Store.SetI("hmx:next", n + 1);
			return n.ToString();
		}

		internal static IEnumerable<string> Commanders()
		{
			return All().Where((Rec r) => r.Mine && r.Commander != "").Select((Rec r) => r.Commander);
		}

		internal static int PendingFor(string owner)
		{
			return All().Count((Rec r) => r.Owner == owner);
		}

		// ------------------------------------------------------------------
		// the summons

		internal static void BeginMine(Hero knight, string q, int men, int cost)
		{
			if (Hero.MainHero.Gold < cost)
			{
				Flow.Notify("You cannot pay for them.");
				return;
			}
			Hero.MainHero.ChangeHeroGold(-cost);
			Rec r = new Rec();
			r.Id = NewId();
			r.Owner = ((MBObjectBase)Clan.PlayerClan).StringId;
			r.Commander = ((MBObjectBase)knight).StringId;
			r.Quality = q;
			r.Men = men;
			r.Cost = cost;
			r.Ready = CourtBehavior.Today() + Cfg.HostMusterDays;
			Save(r);
			if (Guard.IsSworn(knight))
			{
				Guard.SetState(knight, "mustering");
			}
			Log.Write("host muster begun: " + men + " " + q + " under " + knight.Name + " for " + cost + ", ready day " + r.Ready);
			Store.AddDeed(Standing.Date() + "  The summons went out for " + men.ToString("N0") + " " + Host.QualityName(q) + " under " + knight.Name + ".");
			Ravens.Popup("The Summons Go Out", knight.Name + " sends riders to every holdfast and village: " + men.ToString("N0") + " " + Host.QualityName(q) + " are to gather. The gold is paid.\n\nA host is not raised in a day. They will stand ready in about " + Cfg.HostMusterDays + " days (day " + r.Ready + ").");
		}

		internal static void BeginTheirs(Clan clan, string q, int men, int cost, bool funded, bool threatened, MobileParty hunt)
		{
			Rec r = new Rec();
			r.Id = NewId();
			r.Owner = ((MBObjectBase)clan).StringId;
			r.Quality = q;
			r.Men = men;
			r.Cost = cost;
			r.Ready = CourtBehavior.Today() + Cfg.HostMusterDays;
			r.Hunt = (hunt != null) ? ((MBObjectBase)hunt).StringId : "";
			r.Funded = funded;
			r.Threatened = threatened;
			Save(r);
			Kingdom k = clan.Kingdom;
			string realm = (k != null) ? k.Name.ToString() : clan.Name.ToString();
			Log.Write("host muster begun by " + realm + ": " + men + " " + q + ", ready day " + r.Ready + (funded ? " (Iron Bank gold)" : "") + (threatened ? " - answering an enemy host" : ""));
			IFaction mine = Clan.PlayerClan.MapFaction;
			if (k != null && mine != null && FactionManager.IsAtWarAgainstFaction(k, mine))
			{
				Ravens.Popup("A Host Is Summoned", "Word comes that " + realm + " has sent out the summons" + (funded ? ", with the Iron Bank's gold," : "") + " for " + men.ToString("N0") + " " + Host.QualityName(q) +
					". They will stand ready in about " + Cfg.HostMusterDays + " days.");
			}
		}

		// ------------------------------------------------------------------
		// the ready day

		internal static void Daily(int today)
		{
			foreach (Rec r in All().Where((Rec x) => today >= x.Ready).ToList())
			{
				try
				{
					if (r.Mine)
					{
						Mine(r, today);
					}
					else
					{
						Theirs(r, today);
					}
				}
				catch (Exception e)
				{
					Log.Write("host muster: settling " + r.Id + " failed: " + e.Message);
				}
			}
		}

		private static bool Free(Hero h)
		{
			return h != null && h.IsAlive && !h.IsPrisoner && !h.IsChild;
		}

		private static void Mine(Rec r, int today)
		{
			Hero knight = Law.Find(r.Commander);
			if (!Free(knight) && today - r.Ready >= Grace)
			{
				Hero other = Guard.Ready().FirstOrDefault() ?? Host.Family().FirstOrDefault();
				if (other != null)
				{
					Log.Write("host muster: " + ((knight != null) ? knight.Name.ToString() : "the commander") + " cannot take command; " + other.Name + " does");
					if (knight != null && Guard.IsSworn(knight) && knight.IsAlive)
					{
						Guard.SetState(knight, "guard");
					}
					knight = other;
				}
				else
				{
					Drop(r);
					int back = r.Cost / 2;
					Hero.MainHero.ChangeHeroGold(back);
					Log.Write("host muster refunded: nobody to command " + r.Men + " " + r.Quality + "; " + back + " back");
					Ravens.Popup("The Muster Breaks Up", "The men gathered, and waited, and nobody came to lead them. They have gone home. Half the gold - " + back.ToString("N0") + " - came back.");
					return;
				}
			}
			if (!Free(knight))
			{
				return;
			}
			if (Host.Raise(knight, r.Quality, r.Men, r.Cost, true))
			{
				Drop(r);
				Log.Write("host muster complete: " + r.Men + " " + r.Quality + " under " + knight.Name);
			}
			else if (today - r.Ready >= Grace)
			{
				Drop(r);
				int back = r.Cost / 2;
				Hero.MainHero.ChangeHeroGold(back);
				Log.Write("host muster lapsed: it could not be raised in " + Grace + " days; " + back + " back");
				Ravens.Popup("The Muster Breaks Up", "The host could not be brought together in time, and the men have gone home. Half the gold - " + back.ToString("N0") + " - came back.");
			}
		}

		private static void Theirs(Rec r, int today)
		{
			Clan clan = r.OwnerClan;
			Kingdom k = (clan != null) ? clan.Kingdom : null;
			if (k == null || k.IsEliminated || k.RulingClan != clan)
			{
				Drop(r);
				Log.Write("host muster lapsed (" + r.Owner + "): the realm that summoned it is no more, or its house no longer rules");
				return;
			}
			MobileParty hunt = string.IsNullOrEmpty(r.Hunt) ? null : MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == r.Hunt);
			if (Host.AiRaiseNow(k, r.Quality, r.Men, r.Cost, r.Funded, r.Threatened, hunt, false))
			{
				Drop(r);
				Log.Write("host muster complete for " + k.Name + ": " + r.Men + " " + r.Quality);
			}
			else if (today - r.Ready >= Grace)
			{
				Drop(r);
				Log.Write("host muster lapsed for " + k.Name + ": no lord to lead it");
			}
		}

		// ------------------------------------------------------------------
		// reading, cancelling, testing

		internal static string MineText()
		{
			int today = CourtBehavior.Today();
			StringBuilder sb = new StringBuilder();
			foreach (Rec r in All().Where((Rec x) => x.Mine))
			{
				Hero k = Law.Find(r.Commander);
				sb.Append("Mustering: ").Append(r.Men.ToString("N0")).Append(" ").Append(Host.QualityName(r.Quality)).Append(" under ").Append((k != null) ? k.Name.ToString() : "?")
				  .Append(", ready in ").Append(Math.Max(0, r.Ready - today)).Append(" days.\n");
			}
			return sb.ToString();
		}

		internal static string TheirsText()
		{
			int today = CourtBehavior.Today();
			StringBuilder sb = new StringBuilder();
			foreach (Rec r in All().Where((Rec x) => !x.Mine))
			{
				Clan c = r.OwnerClan;
				string realm = (c != null && c.Kingdom != null) ? c.Kingdom.Name.ToString() : ((c != null) ? c.Name.ToString() : "?");
				bool war = c != null && c.MapFaction != null && Clan.PlayerClan.MapFaction != null && FactionManager.IsAtWarAgainstFaction(c.MapFaction, Clan.PlayerClan.MapFaction);
				sb.Append(realm).Append(": mustering ").Append(r.Men.ToString("N0")).Append(" ").Append(Host.QualityName(r.Quality)).Append(", ready in ")
				  .Append(Math.Max(0, r.Ready - today)).Append(" days").Append(war ? "  - AT WAR WITH YOU" : "").Append(".\n");
			}
			return sb.ToString();
		}

		internal static List<Rec> Mine()
		{
			return All().Where((Rec x) => x.Mine).ToList();
		}

		internal static void Cancel(Rec r)
		{
			if (Store.Get(Prefix + r.Id) == null)
			{
				return;
			}
			Drop(r);
			int back = r.Cost / 2;
			Hero.MainHero.ChangeHeroGold(back);
			Hero k = Law.Find(r.Commander);
			if (k != null && k.IsAlive && Guard.IsSworn(k))
			{
				Guard.SetState(k, "guard");
			}
			Log.Write("host muster called off: " + r.Men + " " + r.Quality + "; " + back + " refunded");
			Flow.Notify("The muster is called off. Half the gold, " + back.ToString("N0") + ", comes back.");
		}

		internal static string ReadyNow()
		{
			int today = CourtBehavior.Today();
			int n = 0;
			foreach (Rec r in All())
			{
				r.Ready = today;
				Save(r);
				n++;
			}
			return n + " muster(s) will stand ready on the next day's tick.";
		}
	}
}
