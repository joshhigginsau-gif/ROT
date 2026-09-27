using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The small council.
	//
	// Bellum's privy council, called to your hall. Summoned, they ride in
	// over a day or three and sit for a few more; while they sit they are at
	// the table in your lord's hall, and each can be spoken to and set to
	// work: the realm's lords called to the banners, a levy on the towns, a
	// report on the state of things.
	internal static class Council
	{
		private const string SessionKey = "cn:session";
		private const string LastKey = "cn:last";
		private const string ArmyKey = "cn:army";
		private const string LevyKey = "cn:levy";

		internal const string Ships = "Marshal";
		internal const string Hand = "Chancellor";
		internal const string Coin = "Seneschal";
		internal const string Whisperers = "Spymaster";
		internal const string Maester = "FirstAdvisor";
		internal const string Laws = "SecondAdvisor";

		// ------------------------------------------------------------------
		// who sits

		internal static bool Rules
		{
			get
			{
				try
				{
					Kingdom k = Clan.PlayerClan.Kingdom;
					return k != null && k.RulingClan == Clan.PlayerClan;
				}
				catch
				{
					return false;
				}
			}
		}

		internal static string Title(string office)
		{
			switch (office)
			{
			case Ships:
				return "Master of Ships";
			case Hand:
				return Hero.MainHero.IsFemale ? "Hand of the Queen" : "Hand of the King";
			case Coin:
				return "Master of Coin";
			case Whisperers:
				return "Master of Whisperers";
			case Maester:
				return "Grand Maester";
			case Laws:
				return "Master of Laws";
			default:
				return "Councillor";
			}
		}

		internal static List<KeyValuePair<string, Hero>> Members()
		{
			List<KeyValuePair<string, Hero>> list = new List<KeyValuePair<string, Hero>>();
			if (!Rules)
			{
				return list;
			}
			foreach (KeyValuePair<string, Clan> seat in Bellum.CouncilSeats(Clan.PlayerClan.Kingdom))
			{
				Hero h = (seat.Value != null) ? seat.Value.Leader : null;
				if (h != null && h.IsAlive && !h.IsPrisoner && h != Hero.MainHero && !h.IsChild && !list.Any((KeyValuePair<string, Hero> x) => x.Value == h))
				{
					list.Add(new KeyValuePair<string, Hero>(seat.Key, h));
				}
			}
			return list;
		}

		internal static Hero Seat(string office)
		{
			return Members().Where((KeyValuePair<string, Hero> m) => m.Key == office).Select((KeyValuePair<string, Hero> m) => m.Value).FirstOrDefault();
		}

		internal static string OfficeOf(Hero h)
		{
			return (h == null) ? null : Members().Where((KeyValuePair<string, Hero> m) => m.Value == h).Select((KeyValuePair<string, Hero> m) => m.Key).FirstOrDefault();
		}

		// ------------------------------------------------------------------
		// the session: venue | arrive | end | who

		private static string[] Session()
		{
			string[] s = (Store.Get(SessionKey) ?? "").Split('|');
			return (s.Length >= 4) ? s : null;
		}

		internal static bool Called
		{
			get
			{
				return Session() != null;
			}
		}

		internal static bool Sitting(out Settlement venue, out List<Hero> who)
		{
			venue = null;
			who = new List<Hero>();
			string[] s = Session();
			if (s == null)
			{
				return false;
			}
			int arrive;
			int end;
			int.TryParse(s[1], out arrive);
			int.TryParse(s[2], out end);
			int today = CourtBehavior.Today();
			venue = Settlement.Find(s[0]);
			if (venue == null || today < arrive || today > end)
			{
				return false;
			}
			foreach (string id in s[3].Split(','))
			{
				Hero h = (id.Length > 0) ? Law.Find(id) : null;
				if (h != null && h.IsAlive && !h.IsPrisoner)
				{
					who.Add(h);
				}
			}
			return who.Count > 0;
		}

		internal static bool SittingHere(Hero h)
		{
			Settlement venue;
			List<Hero> who;
			return h != null && Sitting(out venue, out who) && venue == Settlement.CurrentSettlement && who.Contains(h);
		}

		internal static string CanSummon()
		{
			if (!Rules)
			{
				return "Only a ruler has a small council.";
			}
			if (Called)
			{
				return "The council is already called.";
			}
			Settlement here = Settlement.CurrentSettlement;
			if (here == null || here.Town == null || here.OwnerClan != Clan.PlayerClan)
			{
				return "Summon them to a town or castle of your own.";
			}
			int wait = Store.GetI(LastKey, -9999) + Cfg.CouncilCooldownDays - CourtBehavior.Today();
			if (wait > 0)
			{
				return "They were only just here. Another " + wait + " day(s).";
			}
			if (Members().Count == 0)
			{
				return "Your council has nobody in it - or Bellum's council could not be found.";
			}
			return null;
		}

		internal static void Summon()
		{
			string why = CanSummon();
			if (why != null)
			{
				Flow.Notify(why);
				return;
			}
			Settlement venue = Settlement.CurrentSettlement;
			List<string> coming = new List<string>();
			StringBuilder sb = new StringBuilder();
			float far = 0f;
			foreach (KeyValuePair<string, Hero> m in Members())
			{
				Hero h = m.Value;
				if (h.GetRelationWithPlayer() <= -30f && MBRandom.RandomInt(100) < 50)
				{
					sb.Append(Title(m.Key)).Append(", ").Append(h.Name).Append(", sends regrets, and no reason.\n");
					continue;
				}
				coming.Add(((MBObjectBase)h).StringId);
				sb.Append(Title(m.Key)).Append(", ").Append(h.Name).Append(", is coming.\n");
				try
				{
					if (h.GetCampaignPosition().IsValid())
					{
						far = Math.Max(far, h.GetCampaignPosition().ToVec2().Distance(venue.GetPosition2D));
					}
				}
				catch
				{
				}
			}
			if (coming.Count == 0)
			{
				Ravens.Popup("The Small Council", "Not one of them is coming.\n\n" + sb);
				return;
			}
			int days = Math.Max(1, Math.Min(3, 1 + (int)(far / 80f)));
			int arrive = CourtBehavior.Today() + days;
			Store.Set(SessionKey, ((MBObjectBase)venue).StringId + "|" + arrive + "|" + (arrive + Cfg.CouncilSitDays - 1) + "|" + string.Join(",", coming.ToArray()));
			Store.SetI(LastKey, CourtBehavior.Today());
			Log.Write("council summoned to " + venue.Name + ": " + coming.Count + " coming, sitting from day " + arrive);
			Ravens.Popup("The Small Council",
				"Ravens go out to every seat at the table.\n\n" + sb + "\nThey will be in the hall at " + venue.Name + " in " + days + " day(s), and will sit for " + Cfg.CouncilSitDays +
				". Be there, and go to the lord's hall to speak with them.");
		}

		// When a scene is about to open: while the council sits here, they
		// are at the table in the lord's hall.
		internal static void SeatThem()
		{
			try
			{
				if (!Cfg.Council || !Store.Initialized)
				{
					return;
				}
				Settlement venue;
				List<Hero> who;
				if (!Sitting(out venue, out who) || Settlement.CurrentSettlement != venue || venue.LocationComplex == null)
				{
					return;
				}
				Location hall = venue.LocationComplex.GetLocationWithId("lordshall");
				if (hall == null)
				{
					return;
				}
				foreach (Hero h in who)
				{
					if (venue.LocationComplex.GetLocationOfCharacter(h) == null)
					{
						hall.AddCharacter(AtTable(h));
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("cnseat", "the council could not be seated: " + e.Message);
			}
		}

		private static LocationCharacter AtTable(Hero h)
		{
			Monster monster = FaceGen.GetMonsterWithSuffix(h.CharacterObject.Race, "_settlement");
			uint colour = (h.MapFaction != null) ? h.MapFaction.Color : 0xFFCCCCCCu;
			AgentData data = new AgentData(new PartyAgentOrigin(null, h.CharacterObject, -1, default(UniqueTroopDescriptor)))
				.Monster(monster).NoHorses(true).ClothingColor1(colour).ClothingColor2(colour);
			string actions = ActionSetCode.GenerateActionSetNameWithSuffix(monster, h.IsFemale, "_lord");
			return new LocationCharacter(data, SandBoxManager.Instance.AgentBehaviorManager.AddFixedCharacterBehaviors, "sp_notable", true,
				LocationCharacter.CharacterRelations.Neutral, actions, true);
		}

		private static void Rise(string[] s)
		{
			try
			{
				Settlement venue = Settlement.Find(s[0]);
				Location hall = (venue != null && venue.LocationComplex != null) ? venue.LocationComplex.GetLocationWithId("lordshall") : null;
				if (hall == null)
				{
					return;
				}
				foreach (string id in s[3].Split(','))
				{
					Hero h = (id.Length > 0) ? Law.Find(id) : null;
					if (h != null && h.CurrentSettlement != venue && hall.ContainsCharacter(h))
					{
						hall.RemoveCharacter(h);
					}
				}
			}
			catch
			{
			}
		}

		// Cheat: a called council is here today.
		internal static bool ArriveNow()
		{
			string[] s = Session();
			if (s == null)
			{
				return false;
			}
			int today = CourtBehavior.Today();
			s[1] = today.ToString();
			s[2] = (today + Cfg.CouncilSitDays - 1).ToString();
			Store.Set(SessionKey, string.Join("|", s));
			return true;
		}

		// ------------------------------------------------------------------
		// the realm's lords to the banners: commander | target

		private static bool Free(MobileParty p)
		{
			return p != null && p.IsActive && p.IsLordParty && p != MobileParty.MainParty && p.Army == null && p.MapEvent == null && p.BesiegedSettlement == null
				&& !Host.Is(p) && p.LeaderHero != null && !p.LeaderHero.IsPrisoner && (p.CurrentSettlement == null || !p.CurrentSettlement.IsUnderSiege);
		}

		private static List<MobileParty> RealmParties()
		{
			Kingdom k = Clan.PlayerClan.Kingdom;
			return k.Clans.Where((Clan c) => c != Clan.PlayerClan && !c.IsEliminated).SelectMany((Clan c) => c.WarPartyComponents).Select((WarPartyComponent w) => w.MobileParty).Where(Free).ToList();
		}

		internal static Army CouncilArmy()
		{
			string[] a = (Store.Get(ArmyKey) ?? "").Split('|');
			if (a.Length < 2)
			{
				return null;
			}
			Hero c = Law.Find(a[0]);
			Army army = (c != null && c.PartyBelongedTo != null) ? c.PartyBelongedTo.Army : null;
			if (army == null || army.LeaderParty != c.PartyBelongedTo)
			{
				Store.Set(ArmyKey, null);
				return null;
			}
			return army;
		}

		internal static void CallBanners()
		{
			if (!Rules)
			{
				return;
			}
			if (CouncilArmy() != null)
			{
				Ravens.Popup("The Banners", "The realm's lords are already in the field at the council's word. Stand them down first.");
				return;
			}
			Kingdom k = Clan.PlayerClan.Kingdom;
			List<MobileParty> free = RealmParties();
			Hero ships = Seat(Ships);
			MobileParty lead = (ships != null) ? free.FirstOrDefault((MobileParty p) => p.LeaderHero == ships) : null;
			if (lead == null)
			{
				lead = free.OrderByDescending((MobileParty p) => p.MemberRoster.TotalManCount).FirstOrDefault();
			}
			if (lead == null)
			{
				Ravens.Popup("The Banners", "Every lord of the realm is busy, captive or already in an army.");
				return;
			}
			Vec2 at = lead.GetPosition2D;
			List<Settlement> can = Settlement.All.Where((Settlement s) => s.IsFortification && s.MapFaction != null && FactionManager.IsAtWarAgainstFaction(s.MapFaction, k))
				.OrderBy((Settlement s) => s.GetPosition2D.Distance(at)).Take(25).ToList();
			if (can.Count == 0)
			{
				Ravens.Popup("The Banners", "You are at war with nobody who holds a castle.");
				return;
			}
			List<MobileParty> called = free.Where((MobileParty p) => p != lead).OrderBy((MobileParty p) => p.GetPosition2D.Distance(at)).Take(6).ToList();
			int cost = (called.Count + 1) * Campaign.Current.Models.ArmyManagementCalculationModel.AverageCallToArmyCost;
			List<InquiryElement> els = can.Select((Settlement s) => new InquiryElement(s, s.Name + ((s.OwnerClan != null) ? ("  (" + s.OwnerClan.Name + ")") : ""), null, Clan.PlayerClan.Influence >= cost, "")).ToList();
			Inquiry.Select("The Banners", lead.LeaderHero.Name + " would lead " + (called.Count + 1) + " parties of the realm, for " + cost + " influence (you have " + (int)Clan.PlayerClan.Influence + "). Against which castle?",
				els, 1, 1, "That one", "Not now",
				delegate(List<InquiryElement> chosen)
				{
					Settlement s = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Settlement) : null;
					if (s == null)
					{
						return;
					}
					try
					{
						ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -cost);
						k.CreateArmy(lead.LeaderHero, s, Army.ArmyTypes.Besieger, new MBList<MobileParty>(called));
						Store.Set(ArmyKey, ((MBObjectBase)lead.LeaderHero).StringId + "|" + ((MBObjectBase)s).StringId);
						Log.Write("council army: " + lead.LeaderHero.Name + " with " + called.Count + " parties against " + s.Name);
						Ravens.Popup("The Banners", lead.LeaderHero.Name + " calls the lords to the banners. When they are gathered, they march on " + s.Name + ".");
					}
					catch (Exception e)
					{
						Log.Write("calling the banners failed: " + e);
					}
				});
		}

		internal static void StandDownBanners()
		{
			Army army = CouncilArmy();
			if (army == null)
			{
				Flow.Notify("The realm's lords are not in the field at the council's word.");
				return;
			}
			try
			{
				if (army.LeaderParty != null)
				{
					army.LeaderParty.Ai.SetDoNotMakeNewDecisions(false);
				}
				DisbandArmyAction.ApplyByObjectiveFinished(army);
			}
			catch (Exception e)
			{
				Log.Write("standing the banners down failed: " + e.Message);
			}
			Store.Set(ArmyKey, null);
			Flow.Notify("The lords go back to their own affairs.");
		}

		// Keep the realm's army to the council's target once it has gathered.
		private static void HoldBanners()
		{
			string[] a = (Store.Get(ArmyKey) ?? "").Split('|');
			Army army = CouncilArmy();
			if (army == null || a.Length < 2)
			{
				return;
			}
			Settlement s = Settlement.Find(a[1]);
			MobileParty lead = army.LeaderParty;
			if (s == null || lead == null || lead.MapEvent != null)
			{
				return;
			}
			if (s.MapFaction == Clan.PlayerClan.MapFaction || !FactionManager.IsAtWarAgainstFaction(s.MapFaction, Clan.PlayerClan.MapFaction))
			{
				lead.Ai.SetDoNotMakeNewDecisions(false);
				Store.Set(ArmyKey, null);
				if (s.MapFaction == Clan.PlayerClan.MapFaction)
				{
					Ravens.Popup("Taken", s.Name + " has fallen to the realm's host.");
				}
				return;
			}
			if (lead.BesiegedSettlement == s)
			{
				lead.Ai.SetDoNotMakeNewDecisions(false);
				return;
			}
			if (!army.IsWaitingForArmyMembers())
			{
				lead.Ai.SetDoNotMakeNewDecisions(true);
				lead.SetMoveBesiegeSettlement(s, MobileParty.NavigationType.Default);
			}
		}

		// ------------------------------------------------------------------
		// the other seats

		internal static void Levy()
		{
			int wait = Store.GetI(LevyKey, -9999) + 21 - CourtBehavior.Today();
			if (wait > 0)
			{
				Ravens.Popup("A Levy", "The towns paid a levy not three weeks ago. Another " + wait + " day(s), or they will pay it in blood instead.");
				return;
			}
			int gold = 0;
			StringBuilder sb = new StringBuilder();
			foreach (Settlement s in Clan.PlayerClan.Settlements.Where((Settlement x) => x.IsTown).ToList())
			{
				int g = (int)(s.Town.Prosperity * 0.5f);
				gold += g;
				s.Town.Loyalty = Math.Max(0f, s.Town.Loyalty - 10f);
				sb.Append(s.Name).Append(": ").Append(g.ToString("N0")).Append("\n");
			}
			if (gold == 0)
			{
				Ravens.Popup("A Levy", "Your house holds no town to levy.");
				return;
			}
			Store.SetI(LevyKey, CourtBehavior.Today());
			Hero.MainHero.ChangeHeroGold(gold);
			Ravens.Popup("A Levy", "The levy is collected, and resented.\n\n" + sb + "\n" + gold.ToString("N0") + " gold in all. Each town's loyalty falls by 10.");
		}

		internal static string Realm()
		{
			StringBuilder sb = new StringBuilder();
			Kingdom k = Clan.PlayerClan.Kingdom;
			if (k == null)
			{
				return "";
			}
			int ours = Strength(k);
			sb.Append("The realm has ").Append(ours.ToString("N0")).Append(" men under its lords' banners.\n\n");
			List<Kingdom> foes = Kingdom.All.Where((Kingdom x) => x != k && !x.IsEliminated && FactionManager.IsAtWarAgainstFaction(x, k)).ToList();
			if (foes.Count == 0)
			{
				sb.Append("We are at war with nobody.\n");
			}
			foreach (Kingdom f in foes)
			{
				sb.Append("At war with ").Append(f.Name).Append(": ").Append(Strength(f).ToString("N0")).Append(" men.\n");
			}
			List<Hero> cold = k.Clans.Where((Clan c) => c != Clan.PlayerClan && !c.IsEliminated && c.Leader != null && c.Leader.IsAlive)
				.Select((Clan c) => c.Leader).OrderBy((Hero h) => h.GetRelationWithPlayer()).Take(3).ToList();
			if (cold.Count > 0)
			{
				sb.Append("\nThose who love you least: ").Append(string.Join(", ", cold.Select((Hero h) => h.Name + " (" + (int)h.GetRelationWithPlayer() + ")").ToArray())).Append(".\n");
			}
			string hosts = Host.Summary();
			if (hosts.Length > 0)
			{
				sb.Append("\nYour hosts:\n").Append(hosts);
			}
			Army army = CouncilArmy();
			if (army != null)
			{
				sb.Append("\nThe realm's lords are in the field under ").Append(army.LeaderParty.LeaderHero.Name).Append(".\n");
			}
			return sb.ToString();
		}

		private static int Strength(Kingdom k)
		{
			try
			{
				return k.Clans.Where((Clan c) => !c.IsEliminated).SelectMany((Clan c) => c.WarPartyComponents).Sum((WarPartyComponent w) => w.MobileParty.MemberRoster.TotalManCount);
			}
			catch
			{
				return 0;
			}
		}

		internal static string Judgement()
		{
			List<Charge> open = Law.All();
			if (open.Count == 0)
			{
				return "Nothing waits on you. The realm is either very law-abiding, or very good at not getting caught.";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(open.Count).Append(" matter(s) wait on the King's Justice:\n\n");
			foreach (Charge c in open.Take(8))
			{
				Hero accused = Law.Find(c.Accused);
				sb.Append(Law.KindName(c.Kind)).Append(" - ").Append((accused != null) ? accused.Name.ToString() : "someone").Append("\n");
			}
			sb.Append("\nHear them from the court: Court -> The King's Justice.");
			return sb.ToString();
		}

		internal static string Ravenry()
		{
			StringBuilder sb = new StringBuilder();
			string letter = Ravens.WhisperOnLetter();
			sb.Append((letter != null) ? ("On the letter waiting for you: " + letter + "\n\n") : "No letter waits for you.\n\n");
			List<Hero> heads = Clan.All.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction && c.Leader != null && c.Leader.IsAlive)
				.Select((Clan c) => c.Leader).OrderByDescending(Ravens.TrapChance).Take(3).ToList();
			if (heads.Count > 0)
			{
				sb.Append("If a raven lies to you, it will likely come from ").Append(string.Join(", ", heads.Select((Hero h) => h.Name + " of " + h.Clan.Name).ToArray())).Append(".");
			}
			return sb.ToString();
		}

		internal static string Health()
		{
			StringBuilder sb = new StringBuilder();
			List<Hero> kin = Clan.PlayerClan.Heroes.Where((Hero h) => h.IsAlive && h != Hero.MainHero && Succession.IsBlood(h, Hero.MainHero)).ToList();
			List<Hero> hurt = kin.Where((Hero h) => h.IsWounded).ToList();
			sb.Append((hurt.Count == 0) ? "Your blood is well." : ("Wounded: " + string.Join(", ", hurt.Select((Hero h) => h.Name.ToString()).ToArray()) + ". They will mend."));
			List<Hero> young = kin.Where((Hero h) => h.IsChild).OrderBy((Hero h) => h.Age).ToList();
			if (young.Count > 0)
			{
				sb.Append("\n\nThe children of your house: ").Append(string.Join(", ", young.Select((Hero h) => h.Name + " (" + (int)h.Age + ")").ToArray())).Append(".");
			}
			return sb.ToString();
		}

		// ------------------------------------------------------------------
		// daily and the court

		internal static void Daily()
		{
			try
			{
				if (!Cfg.Council || !Store.Initialized)
				{
					return;
				}
				string[] s = Session();
				int end;
				if (s != null && int.TryParse(s[2], out end) && CourtBehavior.Today() > end)
				{
					Rise(s);
					Store.Set(SessionKey, null);
					Flow.Notify("The small council has risen, and gone back to their own seats.");
				}
				HoldBanners();
			}
			catch (Exception e)
			{
				Log.Once("cndaily", "the council's tick failed: " + e.Message);
			}
		}

		internal static string Summary()
		{
			StringBuilder sb = new StringBuilder();
			string[] s = Session();
			if (s != null)
			{
				Settlement venue = Settlement.Find(s[0]);
				int arrive;
				int.TryParse(s[1], out arrive);
				int today = CourtBehavior.Today();
				sb.Append((today < arrive) ? ("The council rides for " + ((venue != null) ? venue.Name.ToString() : "your hall") + ", and will sit in " + (arrive - today) + " day(s).\n")
					: ("The council sits in the lord's hall at " + ((venue != null) ? venue.Name.ToString() : "your hall") + ".\n"));
			}
			else if (Rules)
			{
				List<KeyValuePair<string, Hero>> m = Members();
				foreach (KeyValuePair<string, Hero> x in m)
				{
					sb.Append(Title(x.Key)).Append(": ").Append(x.Value.Name).Append("\n");
				}
				if (m.Count == 0)
				{
					sb.Append("Your council's seats are empty.\n");
				}
			}
			else
			{
				sb.Append("Only a ruler has a small council. Your sworn knights can still command a host.\n");
			}
			string hosts = Host.Summary();
			if (hosts.Length > 0)
			{
				sb.Append("\nYour hosts:\n").Append(hosts);
			}
			return sb.ToString();
		}

		internal static string Attention()
		{
			if (!Cfg.Council)
			{
				return null;
			}
			Settlement venue;
			List<Hero> who;
			if (Sitting(out venue, out who))
			{
				return "The small council sits at " + venue.Name + ".";
			}
			return Host.Attention();
		}
	}
}
