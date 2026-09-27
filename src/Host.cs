using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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

		// knight | quality | men | price | end | order | target | warned
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

			internal string Pack()
			{
				return string.Join("|", new string[8] { Knight, Quality, Raised.ToString(), Price.ToString(), End.ToString(), Order, Target, Warned ? "1" : "0" });
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

		private static void Save(Rec r)
		{
			Store.Set(Prefix + r.Party, r.Pack());
			_ids.Add(r.Party);
		}

		private static void Drop(Rec r)
		{
			Store.Set(Prefix + r.Party, null);
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

		internal static bool Is(MobileParty p)
		{
			return p != null && _ids.Count > 0 && _ids.Contains(((MBObjectBase)p).StringId);
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

		private static List<CharacterObject> Troops(string q)
		{
			int lo;
			int hi;
			Band(q, out lo, out hi);
			List<CultureObject> cultures = new List<CultureObject>();
			try
			{
				cultures.Add(Clan.PlayerClan.Culture);
				cultures.Add(Hero.MainHero.Culture);
				if (Clan.PlayerClan.Kingdom != null)
				{
					cultures.Add(Clan.PlayerClan.Kingdom.Culture);
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
				Save(r);
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
			Enforce(r, p, true);
			Flow.Notify("Orders sent: " + Describe(r) + ".");
		}

		private static string Describe(Rec r)
		{
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

		// Daily, and when an order is given: keep them to it.
		private static void Enforce(Rec r, MobileParty p, bool fresh)
		{
			try
			{
				if (p.MapEvent != null)
				{
					return;
				}
				Settlement s = string.IsNullOrEmpty(r.Target) ? null : Settlement.Find(r.Target);
				switch (r.Order)
				{
				case "siege":
					if (s == null || s.MapFaction == Clan.PlayerClan.MapFaction)
					{
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
					if (!FactionManager.IsAtWarAgainstFaction(s.MapFaction, Clan.PlayerClan.MapFaction))
					{
						r.Order = "free";
						r.Target = "";
						Save(r);
						p.Ai.SetDoNotMakeNewDecisions(false);
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
					p.SetMoveBesiegeSettlement(s, MobileParty.NavigationType.Default);
					break;
				case "hold":
					if (s == null || s.MapFaction != Clan.PlayerClan.MapFaction)
					{
						r.Order = "free";
						Save(r);
						p.Ai.SetDoNotMakeNewDecisions(false);
						return;
					}
					p.Ai.SetDoNotMakeNewDecisions(true);
					if (fresh || p.CurrentSettlement != s)
					{
						p.SetMoveDefendSettlement(s, false, MobileParty.NavigationType.Default);
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
					p.SetMoveEscortParty(MobileParty.MainParty, MobileParty.NavigationType.Default, false);
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
				foreach (Rec r in All())
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
			Rec r = All().OrderBy((Rec x) => x.End).FirstOrDefault();
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
			List<Rec> all = All();
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
			foreach (Rec r in All())
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
			Rec r = All().Where((Rec x) => x.End - today <= 7).OrderBy((Rec x) => x.End).FirstOrDefault();
			if (r == null)
			{
				return null;
			}
			Hero k = Law.Find(r.Knight);
			return ((k != null) ? (k.Name + "'s host") : "A host") + " goes home in " + Math.Max(0, r.End - today) + " days.";
		}
	}
}
