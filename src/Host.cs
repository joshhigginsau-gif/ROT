using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Hosts bought with gold.
	//
	// Name a sworn knight to command, choose what sort of men, and say how
	// much gold: the host is raised in a day and serves for a season. They
	// were paid up front, so they draw no wages and carry their own bread,
	// and a host of twenty thousand does not melt away the way an
	// oversized party normally would.
	internal static class Host
	{
		private const string Prefix = "hs:";

		internal const string Levy = "levy";
		internal const string Men = "men";
		internal const string Veteran = "veteran";

		// knight | quality | men | price | end | order | target | warned | owner | base
		// owner: the clan that paid (blank = yours, from before other lords
		// could muster). base: how many men the party had before the host was
		// added to it (0 = the party was raised for the host).
		internal sealed class Rec
		{
			internal string Party = "";
			internal string Knight = "";
			internal string Quality = Men;
			internal int Raised;
			internal int Price;
			internal int End;
			internal string Order = "free";
			internal string Target = "";
			internal bool Warned;
			internal string Owner = "";
			internal int Base;

			internal bool Mine
			{
				get
				{
					return Owner == "" || (Clan.PlayerClan != null && Owner == ((MBObjectBase)Clan.PlayerClan).StringId);
				}
			}

			internal Clan OwnerClan
			{
				get
				{
					if (Mine)
					{
						return Clan.PlayerClan;
					}
					string o = Owner;
					return Clan.FindFirst((Clan c) => ((MBObjectBase)c).StringId == o);
				}
			}

			internal string Pack()
			{
				return string.Join("|", new string[10] { Knight, Quality, Raised.ToString(), Price.ToString(), End.ToString(), Order, Target, Warned ? "1" : "0", Owner, Base.ToString() });
			}

			internal static Rec Unpack(string party, string s)
			{
				string[] p = (s ?? "").Split('|');
				if (p.Length < 8)
				{
					return null;
				}
				Rec r = new Rec();
				r.Party = party;
				r.Knight = p[0];
				r.Quality = p[1];
				int.TryParse(p[2], out r.Raised);
				int.TryParse(p[3], out r.Price);
				int.TryParse(p[4], out r.End);
				r.Order = p[5];
				r.Target = p[6];
				r.Warned = p[7] == "1";
				if (p.Length >= 10)
				{
					r.Owner = p[8];
					int.TryParse(p[9], out r.Base);
				}
				return r;
			}
		}

		private static readonly HashSet<string> _ids = new HashSet<string>();

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

		internal static List<Rec> Mine()
		{
			return All().Where((Rec r) => r.Mine).ToList();
		}

		private static void Save(Rec r)
		{
			Store.Set(Prefix + r.Party, r.Pack());
			_ids.Add(r.Party);
		}

		private static void Drop(Rec r)
		{
			Store.Set(Prefix + r.Party, null);
			Store.Set(VoyagePrefix + r.Party, null);
			Store.Set(StuckPrefix + r.Party, null);
			_ids.Remove(r.Party);
		}

		internal static void Reset()
		{
			_ids.Clear();
		}

		// After a load: which parties are hosts.
		internal static void Load()
		{
			_ids.Clear();
			foreach (Rec r in All())
			{
				_ids.Add(r.Party);
			}
		}

		// Parties that are not hosts but need the same keeping: the exiles'
		// company waiting across the sea.
		private static readonly HashSet<string> _protected = new HashSet<string>();

		internal static void Protect(string partyId)
		{
			if (!string.IsNullOrEmpty(partyId))
			{
				_protected.Add(partyId);
			}
		}

		internal static void Unprotect(string partyId)
		{
			_protected.Remove(partyId ?? "");
		}

		internal static void ClearProtected()
		{
			_protected.Clear();
		}

		internal static bool Is(MobileParty p)
		{
			if (p == null || (_ids.Count == 0 && _protected.Count == 0))
			{
				return false;
			}
			string id = ((MBObjectBase)p).StringId;
			return _ids.Contains(id) || _protected.Contains(id);
		}

		internal static MobileParty PartyOf(Rec r)
		{
			return MobileParty.All.FirstOrDefault((MobileParty p) => ((MBObjectBase)p).StringId == r.Party);
		}

		internal static string QualityName(string q)
		{
			switch (q)
			{
			case Levy:
				return "levies";
			case Veteran:
				return "veterans";
			default:
				return "men-at-arms";
			}
		}

		private static int Price(string q)
		{
			switch (q)
			{
			case Levy:
				return Cfg.HostPriceLevy;
			case Veteran:
				return Cfg.HostPriceVeteran;
			default:
				return Cfg.HostPriceMen;
			}
		}

		private static void Band(string q, out int lo, out int hi)
		{
			switch (q)
			{
			case Levy:
				lo = 1;
				hi = 2;
				break;
			case Veteran:
				lo = 4;
				hi = 6;
				break;
			default:
				lo = 2;
				hi = 4;
				break;
			}
		}

		// ------------------------------------------------------------------
		// the model patches: wages, food, size, desertion

		private static bool _patched;

		internal static void Patch()
		{
			if (_patched || Campaign.Current == null)
			{
				return;
			}
			_patched = true;
			Harmony h = new Harmony("community.wardens.and.dragons.hosts");
			GameModels m = Campaign.Current.Models;
			Apply(h, m.PartyWageModel, "GetTotalWage", "WagePost");
			Apply(h, m.MobilePartyFoodConsumptionModel, "DoesPartyConsumeFood", "FoodPost");
			Apply(h, m.PartySizeLimitModel, "GetPartyMemberSizeLimit", "SizePost");
			Apply(h, m.PartyDesertionModel, "GetTroopsToDesert", "DesertPost");
		}

		private static void Apply(Harmony h, object model, string method, string post)
		{
			try
			{
				if (model == null)
				{
					Log.Write("hosts: no model for " + method);
					return;
				}
				MethodInfo target = model.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public);
				if (target == null || target.IsAbstract)
				{
					Log.Write("hosts: " + method + " not found on " + model.GetType().Name);
					return;
				}
				h.Patch(target, null, new HarmonyMethod(typeof(Host).GetMethod(post, BindingFlags.Static | BindingFlags.NonPublic)));
				Log.Write("hosts: patched " + target.DeclaringType.Name + "." + method);
			}
			catch (Exception e)
			{
				Log.Write("hosts: patching " + method + " failed: " + e.Message);
			}
		}

		private static void WagePost(MobileParty __0, ref ExplainedNumber __result)
		{
			if (Is(__0))
			{
				__result = new ExplainedNumber(0f, false, null);
			}
		}

		private static void FoodPost(MobileParty __0, ref bool __result)
		{
			if (Is(__0))
			{
				__result = false;
			}
		}

		private static void SizePost(PartyBase __0, ref ExplainedNumber __result)
		{
			try
			{
				MobileParty p = (__0 != null) ? __0.MobileParty : null;
				if (Is(p))
				{
					int need = p.MemberRoster.TotalManCount + 10 - (int)__result.ResultNumber;
					if (need > 0)
					{
						__result.Add((float)need, null, null);
					}
				}
			}
			catch
			{
			}
		}

		private static void DesertPost(MobileParty __0, ref TroopRoster __result)
		{
			if (Is(__0))
			{
				__result = TroopRoster.CreateDummyTroopRoster();
			}
		}

		// ------------------------------------------------------------------
		// mustering

		internal static void Muster()
		{
			List<Hero> knights = Guard.Ready();
			if (knights.Count == 0)
			{
				Flow.Notify("No sworn knight is with you to take command.");
				return;
			}
			List<InquiryElement> els = knights.Select((Hero k) => new InquiryElement(k, k.Name.ToString(), null, true, "")).ToList();
			Inquiry.Select("Muster a Host", "Which of your sworn knights will command it?", els, 1, 1, "Them", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Hero k = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
					if (k != null)
					{
						ChooseQuality(k);
					}
				});
		}

		private static void ChooseQuality(Hero knight)
		{
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement(Levy, "Levies - " + Cfg.HostPriceLevy + " a man", null, true, "Farmers with spears. Many of them, cheaply."));
			els.Add(new InquiryElement(Men, "Men-at-arms - " + Cfg.HostPriceMen + " a man", null, true, "Soldiers who have seen a battle or two."));
			els.Add(new InquiryElement(Veteran, "Veterans - " + Cfg.HostPriceVeteran + " a man", null, true, "The best of your culture's soldiery. Dear, and worth it."));
			Inquiry.Select("Muster a Host", "What sort of men?", els, 1, 1, "Those", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					string q = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
					if (q != null)
					{
						ChooseGold(knight, q);
					}
				});
		}

		private static void ChooseGold(Hero knight, string q)
		{
			int price = Price(q);
			int most = Math.Min(Hero.MainHero.Gold / price, Cfg.HostMaxMen);
			if (most < 100)
			{
				Flow.Notify("You cannot pay for even a hundred " + QualityName(q) + ".");
				return;
			}
			Inquiry.Text("Muster a Host", "How much gold? At " + price + " a man you can afford " + most.ToString("N0") + " " + QualityName(q) +
				" (at most " + Cfg.HostMaxMen.ToString("N0") + "). They serve " + Cfg.HostDays + " days.",
				(most * price).ToString(), "Count it out", "Not today",
				delegate(string text)
				{
					long gold;
					if (!long.TryParse((text ?? "").Replace(",", "").Replace(".", "").Trim(), out gold) || gold <= 0)
					{
						Flow.Notify("That is not a sum of gold.");
						return;
					}
					int men = (int)Math.Min((long)most, gold / price);
					if (men < 100)
					{
						Flow.Notify("That will not buy a hundred men.");
						return;
					}
					int cost = men * price;
					Inquiry.Confirm("Muster a Host", men.ToString("N0") + " " + QualityName(q) + " under " + knight.Name + ", for " + Cfg.HostDays + " days, for " + cost.ToString("N0") + " gold.",
						"Raise them", "Not today", delegate
						{
							Raise(knight, q, men, cost);
						}, null);
				}, null);
		}

		internal static List<CharacterObject> Troops(string q, Clan owner = null)
		{
			int lo;
			int hi;
			Band(q, out lo, out hi);
			List<CultureObject> cultures = new List<CultureObject>();
			try
			{
				Clan c = owner ?? Clan.PlayerClan;
				cultures.Add(c.Culture);
				if (c == Clan.PlayerClan)
				{
					cultures.Add(Hero.MainHero.Culture);
				}
				else if (c.Leader != null)
				{
					cultures.Add(c.Leader.Culture);
				}
				if (c.Kingdom != null)
				{
					cultures.Add(c.Kingdom.Culture);
				}
			}
			catch
			{
			}
			foreach (CultureObject c in cultures.Where((CultureObject x) => x != null).Distinct())
			{
				List<CharacterObject> list = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Culture == c && x.Occupation == Occupation.Soldier && x.Tier >= lo && x.Tier <= hi).ToList();
				if (list.Count >= 2)
				{
					return list;
				}
			}
			return CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Occupation == Occupation.Soldier && x.Tier >= lo && x.Tier <= hi && x.Culture != null && x.Culture.IsMainCulture).ToList();
		}

		private static void Raise(Hero knight, string q, int men, int cost)
		{
			try
			{
				if (Hero.MainHero.Gold < cost)
				{
					Flow.Notify("You cannot pay for them.");
					return;
				}
				List<CharacterObject> troops = Troops(q);
				if (troops.Count == 0)
				{
					Flow.Notify("There are no such soldiers in your lands to raise.");
					return;
				}
				MobileParty party = MobilePartyHelper.CreateNewClanMobileParty(knight, Clan.PlayerClan);
				if (party == null)
				{
					Flow.Notify("The host could not be raised.");
					return;
				}
				Hero.MainHero.ChangeHeroGold(-cost);
				int lo;
				int hi;
				Band(q, out lo, out hi);
				// Weighted towards the top of the band.
				List<int> weights = troops.Select((CharacterObject t) => t.Tier - lo + 1).ToList();
				int total = weights.Sum();
				int given = 0;
				for (int i = 0; i < troops.Count; i++)
				{
					int n = (i == troops.Count - 1) ? (men - given) : (men * weights[i] / total);
					if (n > 0)
					{
						party.MemberRoster.AddToCounts(troops[i], n, false, 0, 0, true, -1);
						given += n;
					}
				}
				Rec r = new Rec();
				r.Party = ((MBObjectBase)party).StringId;
				r.Knight = ((MBObjectBase)knight).StringId;
				r.Quality = q;
				r.Raised = men;
				r.Price = cost;
				r.End = CourtBehavior.Today() + Cfg.HostDays;
				r.Owner = ((MBObjectBase)Clan.PlayerClan).StringId;
				Save(r);
				Fleet(party);
				Guard.SetState(knight, "host");
				Store.AddDeed(Standing.Date() + "  " + knight.Name + " took command of " + men.ToString("N0") + " " + QualityName(q) + ".");
				Log.Write("host raised: " + men + " " + q + " under " + knight.Name + " (" + r.Party + ") for " + cost);
				Ravens.Popup("The Host Is Mustered",
					men.ToString("N0") + " " + QualityName(q) + " stand under " + knight.Name + "'s command, paid through the next " + Cfg.HostDays + " days.\n\nGive them their orders from the small council: Court -> The small council -> Your hosts.");
			}
			catch (Exception e)
			{
				Log.Write("raising the host failed: " + e);
			}
		}

		// ------------------------------------------------------------------
		// orders

		internal static void Orders(Rec r)
		{
			MobileParty p = PartyOf(r);
			Hero knight = Law.Find(r.Knight);
			if (p == null || knight == null)
			{
				return;
			}
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement("siege", "Besiege a castle or town", null, true, "They march on it, and do not stop for anything else until the siege is laid."));
			els.Add(new InquiryElement("engage", "Bring an enemy host or army to battle", null, true, "They hunt it down and fight it, wherever it goes."));
			els.Add(new InquiryElement("hold", "Hold one of your castles or towns", null, true, "They go there and stay."));
			els.Add(new InquiryElement("follow", "March with me", null, true, "They join your army and fight your battles. You must be in a realm."));
			els.Add(new InquiryElement("free", "Free to campaign", null, true, knight.Name + " uses them as they see fit."));
			els.Add(new InquiryElement("down", "Stand them down", null, true, "The men go home. What they were paid is spent."));
			Inquiry.Select(knight.Name + "'s Host", p.MemberRoster.TotalManCount.ToString("N0") + " men, serving " + Math.Max(0, r.End - CourtBehavior.Today()) + " more days. Now: " + Describe(r) + ".",
				els, 1, 1, "So ordered", "Not now",
				delegate(List<InquiryElement> chosen)
				{
					string o = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
					switch (o)
					{
					case "siege":
						PickTarget(r, true);
						break;
					case "hold":
						PickTarget(r, false);
						break;
					case "engage":
						PickFoe(r);
						break;
					case "follow":
						SetOrder(r, "follow", "");
						break;
					case "free":
						SetOrder(r, "free", "");
						break;
					case "down":
						StandDown(r, "You stood them down.");
						break;
					}
				});
		}

		private static void PickTarget(Rec r, bool siege)
		{
			MobileParty p = PartyOf(r);
			if (p == null)
			{
				return;
			}
			Vec2 at = p.GetPosition2D;
			IFaction me = Clan.PlayerClan.MapFaction;
			List<Settlement> can = siege
				? Settlement.All.Where((Settlement s) => s.IsFortification && s.MapFaction != null && FactionManager.IsAtWarAgainstFaction(s.MapFaction, me)).OrderBy((Settlement s) => s.GetPosition2D.Distance(at)).Take(25).ToList()
				: Settlement.All.Where((Settlement s) => s.IsFortification && s.MapFaction == me).OrderBy((Settlement s) => s.GetPosition2D.Distance(at)).Take(25).ToList();
			if (can.Count == 0)
			{
				Flow.Notify(siege ? "You are at war with nobody who holds a castle." : "Your realm holds no castle.");
				return;
			}
			List<InquiryElement> els = can.Select((Settlement s) => new InquiryElement(s, s.Name + ((s.OwnerClan != null) ? ("  (" + s.OwnerClan.Name + ")") : ""), null, true, "")).ToList();
			Inquiry.Select(siege ? "Besiege" : "Hold", siege ? "Which?" : "Where?", els, 1, 1, "There", "Not now",
				delegate(List<InquiryElement> chosen)
				{
					Settlement s = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Settlement) : null;
					if (s != null)
					{
						SetOrder(r, siege ? "siege" : "hold", ((MBObjectBase)s).StringId);
					}
				});
		}

		// Enemy hosts first, then enemy armies, nearest first.
		internal static List<MobileParty> Foes(IFaction mine, Vec2 at)
		{
			List<MobileParty> hosts = All().Select(PartyOf).Where((MobileParty x) => x != null && x.IsActive && x.MapFaction != null && FactionManager.IsAtWarAgainstFaction(x.MapFaction, mine))
				.OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).ToList();
			List<MobileParty> armies = MobileParty.All.Where((MobileParty x) => x.IsActive && x.Army != null && x.Army.LeaderParty == x && !hosts.Contains(x) && x.MapFaction != null
				&& FactionManager.IsAtWarAgainstFaction(x.MapFaction, mine)).OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).Take(15).ToList();
			return hosts.Concat(armies).ToList();
		}

		private static string FoeName(MobileParty x)
		{
			Rec h = All().FirstOrDefault((Rec r) => r.Party == ((MBObjectBase)x).StringId);
			int men = (x.Army != null && x.Army.LeaderParty == x) ? x.Army.TotalManCount : x.MemberRoster.TotalManCount;
			string who = (x.LeaderHero != null) ? x.LeaderHero.Name.ToString() : x.Name.ToString();
			return ((h != null) ? "Host of " : "Army of ") + who + " (" + x.MapFaction.Name + ", " + men.ToString("N0") + " men)";
		}

		private static void PickFoe(Rec r)
		{
			MobileParty p = PartyOf(r);
			if (p == null)
			{
				return;
			}
			List<MobileParty> foes = Foes(Clan.PlayerClan.MapFaction, p.GetPosition2D);
			if (foes.Count == 0)
			{
				Flow.Notify("No enemy host or army is in the field.");
				return;
			}
			List<InquiryElement> els = foes.Select((MobileParty x) => new InquiryElement(x, FoeName(x), null, true, "")).ToList();
			Inquiry.Select("Bring Them to Battle", "Which?", els, 1, 1, "Them", "Not now",
				delegate(List<InquiryElement> chosen)
				{
					MobileParty x = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as MobileParty) : null;
					if (x != null)
					{
						SetOrder(r, "engage", ((MBObjectBase)x).StringId);
					}
				});
		}

		private static void SetOrder(Rec r, string order, string target)
		{
			MobileParty p = PartyOf(r);
			if (p == null)
			{
				return;
			}
			if (r.Order == "follow" && order != "follow" && p.Army != null)
			{
				p.Army = null;
			}
			r.Order = order;
			r.Target = target;
			Save(r);
			// New orders: any voyage under way is for the old target.
			Store.Set(VoyagePrefix + r.Party, null);
			Store.Set(StuckPrefix + r.Party, null);
			Enforce(r, p, true);
			Flow.Notify("Orders sent: " + Describe(r) + ".");
		}

		private static string Describe(Rec r)
		{
			if (r.Order == "engage")
			{
				MobileParty f = MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == r.Target);
				return "hunting " + ((f != null && f.LeaderHero != null) ? (f.LeaderHero.Name + "'s men") : "the enemy");
			}
			Settlement s = string.IsNullOrEmpty(r.Target) ? null : Settlement.Find(r.Target);
			switch (r.Order)
			{
			case "siege":
				return "besieging " + ((s != null) ? s.Name.ToString() : "?");
			case "hold":
				return "holding " + ((s != null) ? s.Name.ToString() : "?");
			case "follow":
				return "marching with you";
			default:
				return "free to campaign";
			}
		}

		// ------------------------------------------------------------------
		// the sea
		//
		// A party raised from nothing owns no ships, and without ships the
		// game will only march it by land: a host in Essos told to besiege
		// Sunspear stands on the shore for ever. So every host is given a
		// fleet, is ordered with whatever navigation it has, and when the sea
		// still stands in the way it is carried over: a voyage of some days,
		// then the host is put ashore before its target.

		private const string VoyagePrefix = "hv:";   // target | landing day
		private const string StuckPrefix = "hk:";    // x | y | days without moving

		internal static void Fleet(MobileParty p)
		{
			try
			{
				if (p == null || !p.IsActive || p.Party == null || ((IEnumerable<Ship>)p.Ships).Any())
				{
					return;
				}
				List<ShipHull> hulls = new List<ShipHull>();
				Clan clan = p.ActualClan;
				if (clan != null && clan.DefaultPartyTemplate != null)
				{
					hulls.AddRange(clan.DefaultPartyTemplate.ShipHulls.Select((ShipTemplateStack x) => x.ShipHull).Where((ShipHull x) => x != null));
				}
				CultureObject culture = (clan != null) ? clan.Culture : null;
				if (hulls.Count == 0 && culture != null && culture.DefaultPartyTemplate != null)
				{
					hulls.AddRange(culture.DefaultPartyTemplate.ShipHulls.Select((ShipTemplateStack x) => x.ShipHull).Where((ShipHull x) => x != null));
				}
				if (hulls.Count == 0)
				{
					MBReadOnlyList<ShipHull> any = MBObjectManager.Instance.GetObjectTypeList<ShipHull>();
					if (any != null)
					{
						hulls.AddRange(any.Where((ShipHull x) => x != null));
					}
				}
				if (hulls.Count == 0)
				{
					Log.Once("hostfleetnone", "host fleet: the game knows no ships (War Sails not loaded?) - hosts will be ferried instead");
					return;
				}
				int n = Math.Max(1, Math.Min(20, p.MemberRoster.TotalManCount / 500));
				for (int i = 0; i < n; i++)
				{
					ChangeShipOwnerAction.ApplyByMobilePartyCreation(p.Party, new Ship(hulls[MBRandom.RandomInt(hulls.Count)]));
				}
				bool naval = false;
				try
				{
					naval = p.HasNavalNavigationCapability;
				}
				catch
				{
				}
				Log.Write("host fleet: " + n + " ship(s) for " + p.Name + " (naval=" + naval + ")");
			}
			catch (Exception e)
			{
				Log.Once("hostfleet" + ((MBObjectBase)p).StringId, "host fleet failed: " + e.Message);
			}
		}

		private static bool CanSail(MobileParty p)
		{
			try
			{
				return p.HasNavalNavigationCapability;
			}
			catch
			{
				return false;
			}
		}

		// The best way to a settlement for this party, or None when the sea is
		// in the way and it cannot sail.
		internal static MobileParty.NavigationType Nav(MobileParty p, Settlement s)
		{
			try
			{
				MobileParty.NavigationType best;
				float dist;
				bool fromPort;
				AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(p, s, false, out best, out dist, out fromPort);
				if (best != MobileParty.NavigationType.None)
				{
					return best;
				}
				if (CanSail(p))
				{
					return p.NavigationCapability;
				}
				return MobileParty.NavigationType.None;
			}
			catch (Exception e)
			{
				Log.Once("hostnav" + ((MBObjectBase)p).StringId, "host navigation failed: " + e.Message);
				return MobileParty.NavigationType.Default;
			}
		}

		// For chasing a party: everything it has.
		private static MobileParty.NavigationType NavAny(MobileParty p)
		{
			return CanSail(p) ? p.NavigationCapability : MobileParty.NavigationType.Default;
		}

		private static bool AtSea(Rec r)
		{
			return !string.IsNullOrEmpty(Store.Get(VoyagePrefix + r.Party));
		}

		internal static void Embark(Rec r, MobileParty p, Settlement s)
		{
			Vec2 a = p.GetPosition2D;
			Vec2 b = s.GetPosition2D;
			int days = Math.Max(5, Math.Min(12, 5 + (int)(a.Distance(b) / 60f)));
			int land = CourtBehavior.Today() + days;
			Store.Set(VoyagePrefix + r.Party, ((MBObjectBase)s).StringId + "|" + land);
			Store.Set(StuckPrefix + r.Party, null);
			p.Ai.SetDoNotMakeNewDecisions(true);
			p.SetMoveGoToPoint(p.Position, MobileParty.NavigationType.Default);
			Log.Write("host voyage: " + p.Name + " -> " + s.Name + ", lands day " + land);
			if (r.Mine)
			{
				Hero kn = Law.Find(r.Knight);
				Ravens.Popup("The Host Takes Ship", ((kn != null) ? kn.Name.ToString() : "Your knight") + " cannot march to " + s.Name + ": the sea is in the way. The host has hired every hull in the harbour. Expect them ashore before " + s.Name + " in about " + days + " days.");
			}
		}

		// True while the voyage lasts (the host is left alone); lands it on
		// the day.
		private static bool Voyage(Rec r, MobileParty p)
		{
			string v = Store.Get(VoyagePrefix + r.Party);
			if (string.IsNullOrEmpty(v))
			{
				return false;
			}
			string[] parts = v.Split('|');
			int land = 0;
			if (parts.Length > 1)
			{
				int.TryParse(parts[1], out land);
			}
			Settlement s = Settlement.Find(parts[0]);
			if (s == null)
			{
				Store.Set(VoyagePrefix + r.Party, null);
				return false;
			}
			if (CourtBehavior.Today() < land)
			{
				p.Ai.SetDoNotMakeNewDecisions(true);
				p.SetMoveGoToPoint(p.Position, MobileParty.NavigationType.Default);
				return true;
			}
			Store.Set(VoyagePrefix + r.Party, null);
			try
			{
				if (p.Army != null && p.Army.LeaderParty != p)
				{
					p.Army = null;
				}
				p.SetPositionAfterMapChange(s.GatePosition);
				Log.Write("host landed: " + p.Name + " before " + s.Name);
				if (r.Mine)
				{
					Hero kn = Law.Find(r.Knight);
					Ravens.Popup("Ashore", ((kn != null) ? kn.Name.ToString() : "Your host") + " has landed before " + s.Name + ".");
				}
			}
			catch (Exception e)
			{
				Log.Write("landing the host failed: " + e.Message);
			}
			return false;
		}

		// A host that has not moved for three days while marching on a place
		// is taken to be stuck at the water's edge.
		private static bool Stuck(Rec r, MobileParty p)
		{
			Vec2 at = p.GetPosition2D;
			string[] parts = (Store.Get(StuckPrefix + r.Party) ?? "").Split('|');
			float x;
			float y;
			int days = 0;
			if (parts.Length == 3 && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
				&& float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)
				&& int.TryParse(parts[2], out days) && new Vec2(x, y).Distance(at) < 1f)
			{
				days++;
			}
			else
			{
				days = 0;
			}
			Store.Set(StuckPrefix + r.Party, at.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + at.y.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + days);
			return days >= 3;
		}

		// Test: put your first host on a ship for its target now.
		internal static string ForceVoyage()
		{
			Rec r = Mine().FirstOrDefault((Rec x) => (x.Order == "siege" || x.Order == "hold") && !string.IsNullOrEmpty(x.Target));
			if (r == null)
			{
				return "None of your hosts is ordered to a siege or a hold.";
			}
			MobileParty p = PartyOf(r);
			Settlement s = Settlement.Find(r.Target);
			if (p == null || s == null)
			{
				return "That host or its target is gone.";
			}
			Embark(r, p, s);
			return p.Name + " takes ship for " + s.Name + ".";
		}

		// Daily, and when an order is given: keep them to it.
		private static void Enforce(Rec r, MobileParty p, bool fresh)
		{
			try
			{
				if (p.MapEvent != null)
				{
					return;
				}
				Fleet(p);
				if (Voyage(r, p))
				{
					return;
				}
				IFaction mine = p.MapFaction;
				Settlement s = (string.IsNullOrEmpty(r.Target) || r.Order == "engage") ? null : Settlement.Find(r.Target);
				switch (r.Order)
				{
				case "engage":
				{
					string tid = r.Target;
					MobileParty f = MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == tid);
					if (f == null || !f.IsActive || f.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(f.MapFaction, mine))
					{
						r.Order = "free";
						r.Target = "";
						Save(r);
						p.Ai.SetDoNotMakeNewDecisions(false);
						if (r.Mine)
						{
							Ravens.Popup("The Hunt Is Over", "The host your men were hunting is no longer in the field.");
						}
						return;
					}
					p.Ai.SetDoNotMakeNewDecisions(true);
					p.SetMoveEngageParty(f, NavAny(p));
					break;
				}
				case "siege":
					if (s == null || s.MapFaction == mine)
					{
						if (!r.Mine)
						{
							r.Order = "free";
							r.Target = "";
							Save(r);
							p.Ai.SetDoNotMakeNewDecisions(false);
							return;
						}
						if (s != null)
						{
							Hero kn = Law.Find(r.Knight);
							Ravens.Popup("Taken", ((kn != null) ? kn.Name.ToString() : "Your host") + " has taken " + s.Name + ". The host holds it until you say otherwise.");
							Store.AddDeed(Standing.Date() + "  " + s.Name + " fell to your host.");
						}
						r.Order = "hold";
						Save(r);
						Enforce(r, p, true);
						return;
					}
					if (!FactionManager.IsAtWarAgainstFaction(s.MapFaction, mine))
					{
						r.Order = "free";
						r.Target = "";
						Save(r);
						p.Ai.SetDoNotMakeNewDecisions(false);
						if (r.Mine)
						Ravens.Popup("Peace", "There is peace with " + s.MapFaction.Name + ", and your host has turned back from " + s.Name + ".");
						return;
					}
					if (p.BesiegedSettlement == s)
					{
						// The siege is laid; the game's own siege craft takes it
						// from here.
						p.Ai.SetDoNotMakeNewDecisions(false);
						return;
					}
					p.Ai.SetDoNotMakeNewDecisions(true);
					{
						MobileParty.NavigationType nav = Nav(p, s);
						if (nav == MobileParty.NavigationType.None || (!fresh && p.CurrentSettlement == null && p.BesiegedSettlement == null && Stuck(r, p)))
						{
							Embark(r, p, s);
							return;
						}
						p.SetMoveBesiegeSettlement(s, nav);
					}
					break;
				case "hold":
					if (s == null || s.MapFaction != mine)
					{
						r.Order = "free";
						Save(r);
						p.Ai.SetDoNotMakeNewDecisions(false);
						return;
					}
					p.Ai.SetDoNotMakeNewDecisions(true);
					if (fresh || p.CurrentSettlement != s)
					{
						MobileParty.NavigationType nav = Nav(p, s);
						if (nav == MobileParty.NavigationType.None || (!fresh && p.CurrentSettlement == null && Stuck(r, p)))
						{
							Embark(r, p, s);
							return;
						}
						p.SetMoveDefendSettlement(s, false, nav);
					}
					break;
				case "follow":
				{
					Kingdom k = Clan.PlayerClan.Kingdom;
					if (k != null)
					{
						if (MobileParty.MainParty.Army == null)
						{
							Settlement near = SettlementHelper.FindNearestFortificationToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default);
							k.CreateArmy(Hero.MainHero, near, Army.ArmyTypes.Patrolling);
						}
						Army army = MobileParty.MainParty.Army;
						if (army != null)
						{
							p.Ai.SetDoNotMakeNewDecisions(false);
							if (p.Army != army)
							{
								p.Army = army;
							}
							army.Cohesion = 100f;
							break;
						}
					}
					p.Ai.SetDoNotMakeNewDecisions(true);
					p.SetMoveEscortParty(MobileParty.MainParty, NavAny(p), false);
					break;
				}
				default:
					if (fresh)
					{
						p.Ai.SetDoNotMakeNewDecisions(false);
					}
					break;
				}
			}
			catch (Exception e)
			{
				Log.Once("hostorder" + r.Party, "keeping a host to its orders failed: " + e.Message);
			}
		}

		internal static void StandDown(Rec r, string why)
		{
			if (!r.Mine)
			{
				Disperse(r, why);
				return;
			}
			MobileParty p = PartyOf(r);
			Hero knight = Law.Find(r.Knight);
			Drop(r);
			try
			{
				if (p != null && p.IsActive)
				{
					if (p.Army != null)
					{
						p.Army = null;
					}
					p.Ai.SetDoNotMakeNewDecisions(false);
					foreach (TroopRosterElement e in p.MemberRoster.GetTroopRoster().ToList())
					{
						if (e.Character != null && !e.Character.IsHero)
						{
							p.MemberRoster.AddToCounts(e.Character, -e.Number, false, -e.WoundedNumber, 0, true, -1);
						}
					}
					if (knight != null && knight.IsAlive && !knight.IsPrisoner)
					{
						AddHeroToPartyAction.Apply(knight, MobileParty.MainParty, false);
					}
					if (p.IsActive)
					{
						DestroyPartyAction.Apply(null, p);
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("standing the host down failed: " + e.Message);
			}
			if (knight != null && knight.IsAlive)
			{
				Guard.SetState(knight, "guard");
			}
			Log.Write("host stood down (" + r.Party + "): " + why);
			Ravens.Popup("The Host Goes Home", ((knight != null) ? (knight.Name + "'s host") : "The host") + " is disbanded. " + why +
				((knight != null && knight.PartyBelongedTo == MobileParty.MainParty) ? ("\n\n" + knight.Name + " rides with you again.") : ""));
		}

		// ------------------------------------------------------------------
		// daily

		internal static void Daily()
		{
			try
			{
				if (!Cfg.Council || !Store.Initialized)
				{
					return;
				}
				int today = CourtBehavior.Today();
				foreach (Rec r in All().Where((Rec x) => !x.Mine).ToList())
				{
					TheirDaily(r, today);
				}
				AiMuster(today);
				foreach (Rec r in Mine())
				{
					MobileParty p = PartyOf(r);
					Hero knight = Law.Find(r.Knight);
					if (p == null || !p.IsActive || knight == null || !knight.IsAlive || knight.IsPrisoner || p.LeaderHero != knight)
					{
						Drop(r);
						if (knight != null && knight.IsAlive)
						{
							Guard.SetState(knight, "guard");
						}
						string how = (knight == null || !knight.IsAlive) ? " fell, and the host scattered." : (knight.IsPrisoner ? " was taken, and the host scattered." : "'s host is gone.");
						Store.AddDeed(Standing.Date() + "  " + ((knight != null) ? knight.Name.ToString() : "A knight") + how);
						Ravens.Popup("A Host Lost", ((knight != null) ? knight.Name.ToString() : "Your knight") + how);
						continue;
					}
					if (today >= r.End)
					{
						StandDown(r, "Their season's service is done.");
						continue;
					}
					if (!r.Warned && r.End - today <= 7)
					{
						r.Warned = true;
						Save(r);
						Renewal(r, knight);
					}
					Enforce(r, p, false);
				}
			}
			catch (Exception e)
			{
				Log.Once("hostdaily", "the hosts' tick failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// other rulers' hosts

		private const string AiRollKey = "hx:airoll";

		private static bool AtWarWithMe(IFaction f)
		{
			try
			{
				return f != null && Clan.PlayerClan.MapFaction != null && FactionManager.IsAtWarAgainstFaction(f, Clan.PlayerClan.MapFaction);
			}
			catch
			{
				return false;
			}
		}

		private static bool FreeLordParty(MobileParty p)
		{
			return p != null && p.IsActive && p.IsLordParty && p != MobileParty.MainParty && p.Army == null && p.MapEvent == null && p.BesiegedSettlement == null && !Is(p);
		}

		// Once a week: a ruler at war with gold to spare may buy a host -
		// likelier if an enemy of theirs already has one in the field.
		private static void AiMuster(int today)
		{
			if (!Cfg.AiHosts || today - Store.GetI(AiRollKey, -9999) < 7)
			{
				return;
			}
			Store.SetI(AiRollKey, today);
			foreach (Kingdom k in Kingdom.All.ToList())
			{
				try
				{
					if (k == null || k.IsEliminated || k.RulingClan == null || k.RulingClan == Clan.PlayerClan)
					{
						continue;
					}
					Hero ruler = k.Leader;
					if (ruler == null || !ruler.IsAlive || ruler.IsPrisoner)
					{
						continue;
					}
					List<Kingdom> foes = Kingdom.All.Where((Kingdom o) => o != k && !o.IsEliminated && FactionManager.IsAtWarAgainstFaction(o, k)).ToList();
					if (foes.Count == 0)
					{
						continue;
					}
					string owner = ((MBObjectBase)k.RulingClan).StringId;
					if (All().Count((Rec r) => r.Owner == owner) >= Cfg.AiHostMaxPerRealm)
					{
						continue;
					}
					bool threatened = All().Any((Rec r) =>
					{
						MobileParty hp = PartyOf(r);
						return hp != null && hp.MapFaction != null && FactionManager.IsAtWarAgainstFaction(hp.MapFaction, k);
					});
					int chance = Cfg.AiHostWeeklyChance * (threatened ? 3 : 1);
					if (MBRandom.RandomInt(100) >= chance)
					{
						continue;
					}
					AiRaise(k, ruler, threatened);
				}
				catch (Exception e)
				{
					Log.Once("aimuster" + ((MBObjectBase)k).StringId, "a ruler's muster failed: " + e.Message);
				}
			}
		}

		// funded: gold the Iron Bank puts up (the ruler pays nothing); hunt: a
		// party the host is sent after.
		internal static bool AiRaise(Kingdom k, Hero ruler, bool threatened, int funded = 0, MobileParty hunt = null)
		{
			int budget = (funded > 0) ? funded : (int)((long)ruler.Gold * Cfg.AiHostSpendPercent / 100);
			if (funded <= 0 && budget / Cfg.HostPriceMen < Cfg.AiHostMinMen)
			{
				// Short of gold: the Bank may lend it.
				int loan = IronBank.AiBorrow(k, ruler, Cfg.AiHostMinMen * Cfg.HostPriceMen * 2 - budget);
				if (loan > 0)
				{
					budget += loan;
				}
			}
			string q = (budget >= Cfg.AiHostMinMen * Cfg.HostPriceVeteran * 3) ? Veteran : Men;
			int men = Math.Min(Cfg.HostMaxMen, budget / Price(q));
			if (men < Cfg.AiHostMinMen)
			{
				q = Levy;
				men = Math.Min(Cfg.HostMaxMen, budget / Price(q));
			}
			if (men < Cfg.AiHostMinMen)
			{
				return false;
			}
			Clan clan = k.RulingClan;
			List<CharacterObject> troops = Troops(q, clan);
			if (troops.Count == 0)
			{
				return false;
			}
			// A lord of the ruling house to command: one already at the head
			// of a free party, else one with no party at all.
			List<Hero> lords = clan.Heroes.Where((Hero h) => h.IsAlive && !h.IsChild && !h.IsPrisoner && h.IsLord).ToList();
			Hero commander = lords.Where((Hero h) => h.PartyBelongedTo != null && h.PartyBelongedTo.LeaderHero == h && FreeLordParty(h.PartyBelongedTo))
				.OrderBy((Hero h) => (h == ruler) ? 1 : 0).FirstOrDefault();
			MobileParty party = (commander != null) ? commander.PartyBelongedTo : null;
			int basis = (party != null) ? party.MemberRoster.TotalManCount : 0;
			if (party == null)
			{
				commander = lords.FirstOrDefault((Hero h) => h.PartyBelongedTo == null && h.CurrentSettlement != null);
				if (commander == null)
				{
					return false;
				}
				party = MobilePartyHelper.CreateNewClanMobileParty(commander, clan);
				if (party == null)
				{
					return false;
				}
			}
			int cost = men * Price(q);
			if (funded <= 0)
			{
				ruler.ChangeHeroGold(-Math.Min(cost, ruler.Gold));
			}
			Fill(party, troops, q, men);
			Rec r = new Rec();
			r.Party = ((MBObjectBase)party).StringId;
			r.Knight = ((MBObjectBase)commander).StringId;
			r.Quality = q;
			r.Raised = men;
			r.Price = cost;
			r.End = CourtBehavior.Today() + Cfg.HostDays;
			r.Owner = ((MBObjectBase)clan).StringId;
			r.Base = basis;
			Save(r);
			AiChoose(r, party);
			if (hunt != null && hunt.IsActive && hunt.MapFaction != null && FactionManager.IsAtWarAgainstFaction(hunt.MapFaction, party.MapFaction))
			{
				r.Order = "engage";
				r.Target = ((MBObjectBase)hunt).StringId;
				Save(r);
				Enforce(r, party, true);
			}
			Log.Write("host raised by " + k.Name + ": " + men + " " + q + " under " + commander.Name + " (" + r.Party + "), " + Describe(r) + (threatened ? " - answering an enemy host" : ""));
			string news = k.Name + ((funded > 0) ? " has been given a host by the Iron Bank: " : " has bought a host: ") + men.ToString("N0") + " " + QualityName(q) + " under " + commander.Name + ", " + Describe(r) + ".";
			if (AtWarWithMe(k))
			{
				Ravens.Popup("A Host Gathers", news + "\n\nYour own hosts can be sent to bring them to battle: Court -> The small council -> Your hosts.");
			}
			else
			{
				Flow.Notify(news);
			}
			return true;
		}

		// Where a ruler sends a host: at an enemy host within reach, else at
		// the nearest enemy castle.
		private static void AiChoose(Rec r, MobileParty p)
		{
			IFaction mine = p.MapFaction;
			Vec2 at = p.GetPosition2D;
			MobileParty foe = Foes(mine, at).FirstOrDefault((MobileParty x) => Is(x) && x.GetPosition2D.Distance(at) < 250f);
			if (foe != null)
			{
				r.Order = "engage";
				r.Target = ((MBObjectBase)foe).StringId;
			}
			else
			{
				Settlement s = Settlement.All.Where((Settlement x) => x.IsFortification && x.MapFaction != null && FactionManager.IsAtWarAgainstFaction(x.MapFaction, mine))
					.OrderBy((Settlement x) => x.GetPosition2D.Distance(at)).FirstOrDefault();
				r.Order = (s != null) ? "siege" : "free";
				r.Target = (s != null) ? ((MBObjectBase)s).StringId : "";
			}
			Save(r);
			Enforce(r, p, true);
		}

		private static void TheirDaily(Rec r, int today)
		{
			MobileParty p = PartyOf(r);
			Hero lord = Law.Find(r.Knight);
			if (p == null || !p.IsActive || lord == null || !lord.IsAlive || lord.IsPrisoner || p.LeaderHero != lord)
			{
				Drop(r);
				Clan owner = r.OwnerClan;
				string what = ((owner != null && owner.Kingdom != null) ? owner.Kingdom.Name.ToString() : "A realm") + "'s host under " + ((lord != null) ? lord.Name.ToString() : "its lord") + " is broken.";
				Log.Write(what);
				if (owner != null && AtWarWithMe(owner.MapFaction))
				{
					Ravens.Popup("A Host Broken", what);
					Store.AddDeed(Standing.Date() + "  " + what);
				}
				return;
			}
			if (today >= r.End)
			{
				Disperse(r, "their service is done");
				return;
			}
			if (p.MapEvent != null)
			{
				return;
			}
			// An enemy host close by is worth more than a castle.
			if (r.Order == "siege" && p.BesiegedSettlement == null)
			{
				Vec2 at = p.GetPosition2D;
				if (Foes(p.MapFaction, at).Any((MobileParty x) => Is(x) && x.GetPosition2D.Distance(at) < 100f))
				{
					AiChoose(r, p);
					return;
				}
			}
			if (r.Order == "free")
			{
				AiChoose(r, p);
				return;
			}
			Enforce(r, p, false);
		}

		// A ruler's host going home: the lord keeps the party they had.
		private static void Disperse(Rec r, string why)
		{
			MobileParty p = PartyOf(r);
			Drop(r);
			try
			{
				if (p != null && p.IsActive)
				{
					p.Ai.SetDoNotMakeNewDecisions(false);
					int keep = Math.Max(r.Base, 60);
					int excess = p.MemberRoster.TotalManCount - keep;
					foreach (TroopRosterElement e in p.MemberRoster.GetTroopRoster().Where((TroopRosterElement x) => x.Character != null && !x.Character.IsHero).OrderByDescending((TroopRosterElement x) => x.Number).ToList())
					{
						if (excess <= 0)
						{
							break;
						}
						int n = Math.Min(excess, e.Number);
						int wounded = Math.Min(n, e.WoundedNumber);
						p.MemberRoster.AddToCounts(e.Character, -n, false, -wounded, 0, true, -1);
						excess -= n;
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("dispersing a host failed: " + e.Message);
			}
			Clan owner = r.OwnerClan;
			Log.Write("host dispersed (" + r.Party + ", " + ((owner != null) ? owner.Name.ToString() : "?") + "): " + why);
			if (owner != null && AtWarWithMe(owner.MapFaction))
			{
				Flow.Notify(((owner.Kingdom != null) ? owner.Kingdom.Name.ToString() : owner.Name.ToString()) + "'s host has gone home: " + why + ".");
			}
		}

		// Cheat: a ruler at war with you (or the one named) buys a host now.
		internal static string ForceAi(string name)
		{
			Kingdom k = Kingdom.All.Where((Kingdom x) => !x.IsEliminated && x.RulingClan != Clan.PlayerClan && x.Leader != null && x.Leader.IsAlive)
				.OrderByDescending((Kingdom x) => (!string.IsNullOrEmpty(name) && x.Name.ToString().ToLowerInvariant().Contains(name.ToLowerInvariant())) ? 2 : (AtWarWithMe(x) ? 1 : 0)).FirstOrDefault();
			if (k == null)
			{
				return "No other ruler to muster a host.";
			}
			int need = Cfg.AiHostMinMen * Cfg.HostPriceMen * 100 / Math.Max(1, Cfg.AiHostSpendPercent) * 5;
			if (k.Leader.Gold < need)
			{
				k.Leader.ChangeHeroGold(need - k.Leader.Gold);
			}
			int before = All().Count;
			AiRaise(k, k.Leader, false);
			return (All().Count > before) ? (k.Name + " has mustered a host. See the log, or the Hand's report.") : (k.Name + " could not muster: no free lord of their house to command.");
		}

		// Everyone else's hosts, for the Hand's report.
		internal static string Theirs()
		{
			StringBuilder sb = new StringBuilder();
			foreach (Rec r in All().Where((Rec x) => !x.Mine))
			{
				MobileParty p = PartyOf(r);
				Hero lord = Law.Find(r.Knight);
				if (p == null)
				{
					continue;
				}
				sb.Append((p.MapFaction != null) ? p.MapFaction.Name.ToString() : "?").Append(": ").Append(p.MemberRoster.TotalManCount.ToString("N0")).Append(" under ")
				  .Append((lord != null) ? lord.Name.ToString() : "?").Append(", ").Append(Describe(r)).Append(AtWarWithMe(p.MapFaction) ? "  - AT WAR WITH YOU" : "").Append(".\n");
			}
			return sb.ToString();
		}

		internal static void Fill(MobileParty party, List<CharacterObject> troops, string q, int men)
		{
			int lo;
			int hi;
			Band(q, out lo, out hi);
			List<int> weights = troops.Select((CharacterObject t) => t.Tier - lo + 1).ToList();
			int total = Math.Max(1, weights.Sum());
			int given = 0;
			for (int i = 0; i < troops.Count; i++)
			{
				int n = (i == troops.Count - 1) ? (men - given) : (men * weights[i] / total);
				if (n > 0)
				{
					party.MemberRoster.AddToCounts(troops[i], n, false, 0, 0, true, -1);
					given += n;
				}
			}
			Fleet(party);
		}

		private static int RenewCost(Rec r)
		{
			return r.Price * Cfg.HostRenewPercent / 100;
		}

		private static void Renewal(Rec r, Hero knight)
		{
			int cost = RenewCost(r);
			Inquiry.Confirm("A Season's Service", knight.Name + "'s host serves " + Math.Max(0, r.End - CourtBehavior.Today()) + " more days. Keep them another " + Cfg.HostDays + " for " + cost.ToString("N0") + "?",
				"Pay them", "Let them go when it is done", delegate
				{
					Renew(r);
				}, null);
		}

		internal static void Renew(Rec r)
		{
			int cost = RenewCost(r);
			if (Hero.MainHero.Gold < cost)
			{
				Flow.Notify("You cannot pay them.");
				return;
			}
			Hero.MainHero.ChangeHeroGold(-cost);
			r.End += Cfg.HostDays;
			r.Warned = false;
			Save(r);
			Flow.Notify("The host is paid for another " + Cfg.HostDays + " days.");
		}

		// Cheat: the nearest host's term ends tomorrow.
		internal static bool EndSoon()
		{
			Rec r = Mine().OrderBy((Rec x) => x.End).FirstOrDefault();
			if (r == null)
			{
				return false;
			}
			r.End = CourtBehavior.Today() + 1;
			r.Warned = false;
			Save(r);
			return true;
		}

		// ------------------------------------------------------------------
		// the court

		internal static void Pick()
		{
			List<Rec> all = Mine();
			if (all.Count == 0)
			{
				Flow.Notify("You have no host in the field.");
				return;
			}
			List<InquiryElement> els = all.Select((Rec r) =>
			{
				Hero k = Law.Find(r.Knight);
				MobileParty p = PartyOf(r);
				return new InquiryElement(r, ((k != null) ? k.Name.ToString() : "?") + " - " + ((p != null) ? p.MemberRoster.TotalManCount.ToString("N0") : "?") + " men, " + Describe(r), null, true, "");
			}).ToList();
			Inquiry.Select("Your Hosts", "Which?", els, 1, 1, "That one", "Not now",
				delegate(List<InquiryElement> chosen)
				{
					Rec r = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Rec) : null;
					if (r != null)
					{
						Orders(r);
					}
				});
		}

		internal static string Summary()
		{
			StringBuilder sb = new StringBuilder();
			int today = CourtBehavior.Today();
			foreach (Rec r in Mine())
			{
				Hero k = Law.Find(r.Knight);
				MobileParty p = PartyOf(r);
				sb.Append((k != null) ? k.Name.ToString() : "?").Append(": ").Append((p != null) ? p.MemberRoster.TotalManCount.ToString("N0") : "?").Append(" ").Append(QualityName(r.Quality))
				  .Append(", ").Append(Describe(r)).Append(", ").Append(Math.Max(0, r.End - today)).Append(" days left.\n");
			}
			return sb.ToString();
		}

		internal static string Attention()
		{
			int today = CourtBehavior.Today();
			Rec r = Mine().Where((Rec x) => x.End - today <= 7).OrderBy((Rec x) => x.End).FirstOrDefault();
			if (r == null)
			{
				return null;
			}
			Hero k = Law.Find(r.Knight);
			return ((k != null) ? (k.Name + "'s host") : "A host") + " goes home in " + Math.Max(0, r.End - today) + " days.";
		}
	}
}
