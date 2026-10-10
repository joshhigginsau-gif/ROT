using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Sworn houses: lesser houses beneath a warden.
	//
	// A warden - a house holding a Bellum county or higher, or styled warden -
	// gathers houses of its own: cadet branches of its blood, knights raised to
	// a name, landless houses that come asking. Each holds a MANOR: one of the
	// warden's villages. Bannerlord cannot give a village an owner of its own,
	// so the manor lord is named and paid (a share of the village's taxes, each
	// season, from the warden's purse) while the village stays, in law, under
	// the warden's castle. A warden may also grant one of its castles outright:
	// that is a vanilla gift, Bellum re-syncs the castle's barony itself, and
	// the barony is placed beneath the warden's title so it shows as a branch
	// of the warden in Bellum's hierarchy tab. Manors cannot be nodes there
	// (Bellum has no village titles), so they are written into that tab's
	// tooltips instead.
	//
	// The AI grows slowly on purpose: one house, for one warden, in the whole
	// world, per season at most - and never more houses than there are
	// villages to give them.
	internal static class Sworn
	{
		private const string Prefix = "sw:";          // house -> warden|kind|manor|castle|day|lostDay
		private const string GainPrefix = "swg:";     // warden -> last day it gained a house
		private const string RollKey = "swx:roll";    // last season roll
		private const string IncomeKey = "swx:income"; // last manor payday

		internal sealed class Rec
		{
			internal string House = "";
			internal string Warden = "";
			internal string Kind = "invited";
			internal string Manor = "";
			internal string Castle = "";
			internal int Day;
			internal int Lost = -1;

			internal string Pack()
			{
				return string.Join("|", new string[6] { Warden, Kind, Manor, Castle, Day.ToString(), Lost.ToString() });
			}

			internal static Rec Unpack(string house, string s)
			{
				string[] p = (s ?? "").Split('|');
				if (p.Length < 5)
				{
					return null;
				}
				Rec r = new Rec();
				r.House = house;
				r.Warden = p[0];
				r.Kind = p[1];
				r.Manor = p[2];
				r.Castle = p[3];
				int.TryParse(p[4], out r.Day);
				if (p.Length >= 6)
				{
					int.TryParse(p[5], out r.Lost);
				}
				return r;
			}

			internal Clan HouseClan
			{
				get
				{
					return Find(House);
				}
			}

			internal Clan WardenClan
			{
				get
				{
					return Find(Warden);
				}
			}
		}

		internal static Clan Find(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			return Clan.FindFirst((Clan c) => ((MBObjectBase)c).StringId == id);
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

		internal static Rec Of(Clan house)
		{
			if (house == null)
			{
				return null;
			}
			return Rec.Unpack(((MBObjectBase)house).StringId, Store.Get(Prefix + ((MBObjectBase)house).StringId));
		}

		internal static List<Rec> Under(Clan warden)
		{
			string id = (warden != null) ? ((MBObjectBase)warden).StringId : "";
			return All().Where((Rec r) => r.Warden == id).ToList();
		}

		private static void Save(Rec r)
		{
			Store.Set(Prefix + r.House, r.Pack());
		}

		private static void Drop(Rec r)
		{
			Store.Set(Prefix + r.House, null);
		}

		// ------------------------------------------------------------------
		// wardens

		// A warden's highest title tier: 1 county, 2 duchy, 3 kingdom, 4 empire;
		// 1 for a house styled warden without such a title; -1 for none.
		internal static int Rank(Clan c)
		{
			if (c == null || c.IsEliminated || c.Leader == null || c.Kingdom == null || c.IsBanditFaction || c.IsMinorFaction)
			{
				return -1;
			}
			int best = -1;
			try
			{
				foreach (KeyValuePair<string, object> t in Bellum.TitlesHeldBy(c))
				{
					best = Math.Max(best, Bellum.TierOf(t.Value));
				}
			}
			catch
			{
			}
			if (best >= 1)
			{
				return best;
			}
			return (Styles.Of(c) != null) ? 1 : -1;
		}

		internal static bool IsWarden(Clan c)
		{
			return Rank(c) >= 1;
		}

		internal static int Cap(int rank)
		{
			return (rank <= 1) ? 2 : ((rank == 2) ? 3 : 4);
		}

		// The warden's highest Bellum title, for the hierarchy.
		internal static object TopTitle(Clan warden)
		{
			try
			{
				return Bellum.TitlesHeldBy(warden).Select((KeyValuePair<string, object> t) => t.Value).OrderByDescending(Bellum.TierOf).FirstOrDefault((object t) => Bellum.TierOf(t) >= 1);
			}
			catch
			{
				return null;
			}
		}

		internal static List<Clan> Wardens(Kingdom k = null)
		{
			return Clan.All.Where((Clan c) => c != null && !c.IsEliminated && c.Kingdom != null && (k == null || c.Kingdom == k) && IsWarden(c)).ToList();
		}

		private static HashSet<string> Manors()
		{
			return new HashSet<string>(All().Where((Rec r) => !string.IsNullOrEmpty(r.Manor)).Select((Rec r) => r.Manor));
		}

		internal static List<Village> FreeVillages(Clan warden)
		{
			HashSet<string> taken = Manors();
			List<Village> list = new List<Village>();
			try
			{
				foreach (Town t in warden.Fiefs)
				{
					foreach (Village v in t.Settlement.BoundVillages)
					{
						if (v != null && !taken.Contains(((MBObjectBase)v.Settlement).StringId))
						{
							list.Add(v);
						}
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static int WorldCap()
		{
			int villages = Settlement.All.Count((Settlement s) => s.IsVillage);
			return (Cfg.SwornWorldCap > 0) ? Math.Min(Cfg.SwornWorldCap, villages) : villages;
		}

		// Why this warden cannot take another house now (null = it can).
		internal static string CannotGain(Clan warden, bool ai)
		{
			int rank = Rank(warden);
			if (rank < 1)
			{
				return warden.Name + " holds no wardenship";
			}
			int n = Under(warden).Count;
			if (n >= Cap(rank))
			{
				return warden.Name + " already has as many sworn houses as its rank allows (" + Cap(rank) + ")";
			}
			if (FreeVillages(warden).Count == 0)
			{
				return warden.Name + " has no village left to give as a manor";
			}
			if (All().Count >= WorldCap())
			{
				return "every village in the world already has a lord";
			}
			if (ai)
			{
				int last = Store.GetI(GainPrefix + ((MBObjectBase)warden).StringId, -9999);
				if (CourtBehavior.Today() - last < Cfg.SwornWardenCooldown)
				{
					return warden.Name + " gained a house too recently";
				}
				if (warden.Leader == null || warden.Leader.Gold < Cfg.SwornAiRaiseCost)
				{
					return warden.Name + " cannot afford a new house";
				}
			}
			return null;
		}

		// ------------------------------------------------------------------
		// candidates

		internal static List<Hero> CadetCandidates(Clan warden)
		{
			Hero heir = (warden == Clan.PlayerClan) ? Succession.Named() : null;
			return warden.Heroes.Where((Hero h) => h != null && h.IsAlive && !h.IsChild && !h.IsPrisoner && h != warden.Leader && h != Hero.MainHero && h != heir
				&& h.Spouse != warden.Leader && h.CompanionOf == null && !Guard.IsSworn(h) && (h.PartyBelongedTo == null || h.PartyBelongedTo.LeaderHero != h || h.PartyBelongedTo.Army == null)).ToList();
		}

		internal static List<Hero> KnightCandidates(Clan warden)
		{
			List<Hero> list = new List<Hero>();
			try
			{
				list.AddRange(warden.Companions.Where((Hero h) => h != null && h.IsAlive && !h.IsChild && !h.IsPrisoner));
			}
			catch
			{
			}
			if (warden == Clan.PlayerClan)
			{
				list.AddRange(Guard.Ready());
			}
			return list.Distinct().ToList();
		}

		// Landless houses of the warden's realm, not already sworn to anyone.
		internal static List<Clan> InviteCandidates(Clan warden)
		{
			Kingdom k = warden.Kingdom;
			if (k == null)
			{
				return new List<Clan>();
			}
			HashSet<string> sworn = new HashSet<string>(All().Select((Rec r) => r.House));
			return Clan.All.Where((Clan c) => c != null && c != warden && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction && c.Leader != null && c.Leader.IsAlive
				&& c.Fiefs.Count == 0 && !sworn.Contains(((MBObjectBase)c).StringId) && !Abdication.IsOldHouse(c) && !IsWarden(c)
				&& (c.Kingdom == k || (c.Kingdom == null && !c.IsMinorFaction) || (c.IsUnderMercenaryService && c.Kingdom == k))).ToList();
		}

		// ------------------------------------------------------------------
		// making a house

		private static Clan Found(Hero leader, Clan warden, string name, bool cadet)
		{
			try
			{
				int n = Store.GetI("swx:next", 1);
				Store.SetI("swx:next", n + 1);
				Clan house = Clan.CreateClan("wad_sworn_" + n + "_" + CourtBehavior.Today());
				if (house == null)
				{
					return null;
				}
				Bastard.Set(house, "Name", new TextObject("{=!}" + name, (Dictionary<string, object>)null));
				Bastard.Set(house, "InformalName", new TextObject("{=!}" + name.Replace("House ", ""), (Dictionary<string, object>)null));
				house.Culture = leader.Culture ?? warden.Culture;
				house.Banner = (cadet ? Abdication.Cadet(warden) : null) ?? Banner.CreateRandomClanBanner(-1);
				if (house.Banner != null)
				{
					uint ground = house.Banner.GetPrimaryColor();
					uint charge = house.Banner.GetFirstIconColor();
					if (ground == charge || Bastard.Bad(charge))
					{
						charge = house.Banner.GetSecondaryColor();
					}
					house.Color = ground;
					house.Color2 = (ground == charge || Bastard.Bad(charge)) ? ground : charge;
					if (house.Color2 != ground)
					{
						Bastard.Set(house, "BannerBackgroundColorPrimary", ground);
						Bastard.Set(house, "BannerBackgroundColorSecondary", ground);
						Bastard.Set(house, "BannerIconColor", charge);
					}
				}
				Bastard.Set(house, "Tier", 1);
				leader.Clan = house;
				house.SetLeader(leader);
				Settlement home = warden.HomeSettlement;
				if (home != null)
				{
					try
					{
						house.SetInitialHomeSettlement(home);
					}
					catch
					{
					}
				}
				Bastard.Call(house, "CalculateMidSettlement");
				Bastard.Announce(house);
				return house;
			}
			catch (Exception e)
			{
				Log.Write("sworn: the house could not be founded: " + e.Message);
				return null;
			}
		}

		private static string HouseName(CultureObject c)
		{
			string n = Knighting.HouseName(c);
			return n.StartsWith("House ", StringComparison.OrdinalIgnoreCase) ? n : ("House " + n);
		}

		// kind: cadet | knight | invited. who: the hero to raise (cadet,
		// knight) or null to pick/make one; invitee: the house invited.
		// name: null for the herald's choice. Returns the house or null.
		internal static Clan Gain(Clan warden, string kind, Hero who, Clan invitee, string name, bool ai)
		{
			string why = CannotGain(warden, ai);
			if (why != null)
			{
				Log.Write("sworn: " + warden.Name + " cannot take a house - " + why);
				return null;
			}
			Kingdom realm = warden.Kingdom;
			Clan house = null;
			try
			{
				if (kind == "invited")
				{
					house = invitee ?? InviteCandidates(warden).OrderByDescending((Clan c) => c.Leader.GetRelation(warden.Leader)).FirstOrDefault();
					if (house == null)
					{
						Log.Write("sworn: no landless house for " + warden.Name + " to invite");
						return null;
					}
					if (house.IsUnderMercenaryService)
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(house, false);
					}
					if (house.Kingdom != realm)
					{
						if (house.Kingdom != null)
						{
							// Following its warden is not treason.
							Store.Set("lw:exiled:" + ((MBObjectBase)house).StringId, "1");
							ChangeKingdomAction.ApplyByLeaveKingdom(house, false);
						}
						ChangeKingdomAction.ApplyByJoinToKingdom(house, realm, default(CampaignTime), false);
					}
				}
				else
				{
					if (kind == "cadet")
					{
						who = who ?? CadetCandidates(warden).OrderBy((Hero h) => h.Age).FirstOrDefault();
						if (who == null)
						{
							Log.Write("sworn: " + warden.Name + " has nobody of its blood free to found a cadet house");
							return null;
						}
					}
					else
					{
						if (who == null)
						{
							who = KnightCandidates(warden).FirstOrDefault() ?? Exile.Make(warden.Culture, MBRandom.RandomInt(24, 40), "Ser ");
						}
						if (who == null)
						{
							Log.Write("sworn: no knight could be found or made for " + warden.Name);
							return null;
						}
						if (who.CompanionOf != null)
						{
							try
							{
								RemoveCompanionAction.ApplyByByTurningToLord(who.CompanionOf, who);
							}
							catch (Exception e)
							{
								Log.Write("sworn: releasing the companion failed: " + e.Message);
							}
						}
						try
						{
							who.SetNewOccupation(Occupation.Lord);
						}
						catch
						{
						}
						if (Guard.IsSworn(who))
						{
							Log.Write("sworn: " + who.Name + " gives up the white cloak for land");
						}
						string first = (who.FirstName != null) ? who.FirstName.ToString() : who.Name.ToString();
						if (!who.Name.ToString().StartsWith("Ser "))
						{
							who.SetName(new TextObject("{=!}Ser " + first, (Dictionary<string, object>)null), new TextObject("{=!}" + first, (Dictionary<string, object>)null));
						}
					}
					MobileParty carrier = who.PartyBelongedTo;
					house = Found(who, warden, name ?? HouseName(who.Culture ?? warden.Culture), kind == "cadet");
					if (house == null)
					{
						return null;
					}
					ChangeKingdomAction.ApplyByJoinToKingdom(house, realm, default(CampaignTime), false);
					if (carrier != null && carrier.LeaderHero == who && carrier != MobileParty.MainParty)
					{
						// Their own men come with them - but a party never follows
						// its owner into a new house by itself.
						carrier.ActualClan = house;
						Log.Write("sworn: " + who.Name + "'s party goes with them into " + house.Name);
					}
					else if (kind == "knight")
					{
						// The game's helper takes them out of whatever party they
						// ride in and gives them a lance of their own.
						Knighting.Company(who, house, null);
					}
					else
					{
						try
						{
							MobilePartyHelper.CreateNewClanMobileParty(who, house);
						}
						catch (Exception e)
						{
							Log.Write("sworn: the cadet's party could not be raised: " + e.Message);
						}
					}
				}
				Rec r = new Rec();
				r.House = ((MBObjectBase)house).StringId;
				r.Warden = ((MBObjectBase)warden).StringId;
				r.Kind = kind;
				r.Day = CourtBehavior.Today();
				Village manor = FreeVillages(warden).OrderByDescending((Village v) => v.Hearth).FirstOrDefault();
				r.Manor = (manor != null) ? ((MBObjectBase)manor.Settlement).StringId : "";
				Save(r);
				Store.SetI(GainPrefix + ((MBObjectBase)warden).StringId, CourtBehavior.Today());
				if (ai && warden.Leader != null)
				{
					warden.Leader.ChangeHeroGold(-Math.Min(Cfg.SwornAiRaiseCost, warden.Leader.Gold));
				}
				if (house.Leader != null && warden.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(house.Leader, warden.Leader, 20, false);
				}
				string what = (kind == "cadet") ? ("a cadet branch of " + warden.Name) : ((kind == "knight") ? ("raised from knighthood by " + warden.Name) : ("sworn to " + warden.Name));
				string date = Standing.Date();
				Knighting.Append(house, "On " + date + " it was " + what + ((manor != null) ? (", and given the manor of " + manor.Name) : "") + ".");
				Knighting.Append(warden, "On " + date + " " + house.Name + " swore to it" + ((manor != null) ? (", holding the manor of " + manor.Name) : "") + ".");
				Log.Write("sworn: " + warden.Name + " " + ((kind == "invited") ? "invited" : "raised") + " " + house.Name + " (" + kind + ", " + ((MBObjectBase)house).StringId + ") with manor " + ((manor != null) ? manor.Name.ToString() : "none") +
					(ai ? " [AI]" : ""));
				if (warden == Clan.PlayerClan || (realm != null && realm == Clan.PlayerClan.Kingdom && Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan))
				{
					Ravens.Popup("A House Sworn", house.Name + " is " + what + ((manor != null) ? (", and holds the manor of " + manor.Name + " of " + warden.Name + ".") : "."));
				}
				return house;
			}
			catch (Exception e)
			{
				Log.Write("sworn: " + warden.Name + " could not take a house: " + e);
				return null;
			}
		}

		// The fiefs a sworn house was given and still holds.
		internal static List<Settlement> Fiefs(Rec r)
		{
			Clan house = r.HouseClan;
			return (r.Castle ?? "").Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select((string id) => Settlement.Find(id))
				.Where((Settlement x) => x != null && house != null && x.OwnerClan == house).ToList();
		}

		// What a warden could give: any town or castle but its seat, and never
		// more than half of what it holds.
		internal static List<Settlement> Grantable(Clan warden)
		{
			if (warden == null)
			{
				return new List<Settlement>();
			}
			int given = Under(warden).Sum((Rec r) => Fiefs(r).Count);
			int held = warden.Fiefs.Count;
			if (held <= 1 || given >= held)
			{
				return new List<Settlement>();
			}
			return warden.Fiefs.Select((Town t) => t.Settlement).Where((Settlement x) => x != null && x != warden.HomeSettlement && (x.IsTown || x.IsCastle)).ToList();
		}

		// A warden gives a sworn house one of its towns or castles.
		internal static bool GrantFief(Rec r, Settlement fief)
		{
			Clan house = r.HouseClan;
			Clan warden = r.WardenClan;
			if (house == null || warden == null || fief == null || fief.OwnerClan != warden || house.Leader == null || fief == warden.HomeSettlement || !(fief.IsTown || fief.IsCastle))
			{
				return false;
			}
			try
			{
				ChangeOwnerOfSettlementAction.ApplyByGift(fief, house.Leader);
				List<string> ids = (r.Castle ?? "").Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
				ids.Add(((MBObjectBase)fief).StringId);
				r.Castle = string.Join(",", ids.Distinct());
				Save(r);
				string how = "Bellum hierarchy: not placed";
				object top = TopTitle(warden);
				object barony = Bellum.TitlesHeldBy(house).Select((KeyValuePair<string, object> t) => t.Value).FirstOrDefault((object t) => Bellum.TierOf(t) == 0 && Bellum.CapitalOf(t) == fief);
				if (top != null && barony != null)
				{
					string why;
					how = Bellum.PlaceBeneath(barony, top, out why) ? ("barony placed beneath " + Bellum.PlainName(top)) : ("barony not placed: " + why);
					Bellum.RebuildIndexes();
					try
					{
						object level = Bellum.ServiceLevels().FirstOrDefault((object l) => l.ToString() == "CustomaryTenure");
						string sr;
						if (level != null)
						{
							how += Bellum.SetService(warden, barony, level, out sr) ? ", customary service" : (", service unchanged (" + sr + ")");
						}
					}
					catch
					{
					}
				}
				string what = (fief.IsTown ? "the town of " : "the castle of ") + fief.Name;
				Knighting.Append(house, "On " + Standing.Date() + " " + warden.Name + " gave it " + what + ".");
				Knighting.Append(warden, "On " + Standing.Date() + " it gave " + what + " to " + house.Name + ".");
				Log.Write("sworn: " + warden.Name + " granted " + fief.Name + " to " + house.Name + " - " + how);
				return true;
			}
			catch (Exception e)
			{
				Log.Write("sworn: the fief could not be granted: " + e.Message);
				return false;
			}
		}

		internal static void Release(Rec r, string why)
		{
			Drop(r);
			Clan house = r.HouseClan;
			Log.Write("sworn: " + ((house != null) ? house.Name.ToString() : r.House) + " released from " + ((r.WardenClan != null) ? r.WardenClan.Name.ToString() : "?") + ": " + why);
			if (house != null)
			{
				Knighting.Append(house, "On " + Standing.Date() + " its oath to " + ((r.WardenClan != null) ? r.WardenClan.Name.ToString() : "its warden") + " ended: " + why + ".");
			}
		}

		// ------------------------------------------------------------------
		// the season: AI wardens, slowly

		internal static string Roll(bool force)
		{
			if (!Cfg.Sworn || !Cfg.SwornAi)
			{
				return "Sworn houses or their AI are turned off.";
			}
			int today = CourtBehavior.Today();
			if (!force && today - Store.GetI(RollKey, -9999) < Cfg.DaysPerSeason)
			{
				return null;
			}
			Store.SetI(RollKey, today);
			// A great warden may enfeoff one of its houses this season.
			string fief = (force || MBRandom.RandomInt(100) < Cfg.SwornAiFiefChance) ? AiFief(null) : null;
			if (fief != null)
			{
				Log.Write("sworn: season fief - " + fief);
			}
			// Half the seasons no house is gained at all.
			if (!force && MBRandom.RandomFloat >= 0.5f)
			{
				Log.Write("sworn: season roll - no warden gains a house this season");
				return "No warden gains a house this season.";
			}
			List<Clan> ready = Wardens().Where((Clan c) => c != Clan.PlayerClan && CannotGain(c, true) == null).ToList();
			if (ready.Count == 0)
			{
				Log.Write("sworn: season roll - no warden is ready (cap, cooldown, villages or gold)");
				return "No warden is ready.";
			}
			Clan w = ready[MBRandom.RandomInt(ready.Count)];
			bool canInvite = InviteCandidates(w).Count > 0;
			string kind = (canInvite && MBRandom.RandomFloat < 0.6f) ? "invited" : ((CadetCandidates(w).Count > 0 && MBRandom.RandomFloat < 0.5f) ? "cadet" : "knight");
			Clan house = Gain(w, kind, null, null, null, true);
			return (house != null) ? (w.Name + " gained " + house.Name + " (" + kind + ").") : (w.Name + " tried and failed - see the log.");
		}

		// One warden gives one fief to one sworn house. only: a warden to force.
		internal static string AiFief(Clan only)
		{
			try
			{
				foreach (Clan w in Wardens().Where((Clan c) => (only != null) ? (c == only) : (c != Clan.PlayerClan && c.Fiefs.Count >= 3)).OrderBy((Clan c) => MBRandom.RandomFloat).ToList())
				{
					List<Settlement> can = Grantable(w);
					if (Rank(w) < 2 || w.Fiefs.Count < 5)
					{
						can = can.Where((Settlement x) => x.IsCastle).ToList();
					}
					Settlement fief = can.OrderBy((Settlement x) => (x.Town != null) ? x.Town.Prosperity : 0f).FirstOrDefault();
					Rec r = Under(w).Where((Rec x) => x.HouseClan != null && x.HouseClan.Leader != null && Fiefs(x).Count < 2)
						.OrderBy((Rec x) => (x.Kind == "cadet") ? 0 : ((x.Kind == "knight") ? 1 : 2)).ThenBy((Rec x) => x.Day).FirstOrDefault();
					if (fief == null || r == null)
					{
						if (only != null)
						{
							return w.Name + " has " + ((fief == null) ? "no fief it can spare" : "no sworn house waiting for land");
						}
						continue;
					}
					return GrantFief(r, fief) ? (w.Name + " granted " + fief.Name + " to " + r.HouseClan.Name) : (w.Name + " could not grant " + fief.Name);
				}
			}
			catch (Exception e)
			{
				Log.Write("sworn: the AI fief grant failed: " + e.Message);
			}
			return (only != null) ? "No such warden." : null;
		}

		// ------------------------------------------------------------------
		// daily: manors, oaths, armies

		internal static void Daily()
		{
			try
			{
				if (!Cfg.Sworn || !Store.Initialized)
				{
					return;
				}
				int today = CourtBehavior.Today();
				Roll(false);
				bool payday = today - Store.GetI(IncomeKey, today) >= Cfg.DaysPerSeason;
				if (payday || Store.GetI(IncomeKey, -1) < 0)
				{
					Store.SetI(IncomeKey, today);
				}
				foreach (Rec r in All())
				{
					Clan house = r.HouseClan;
					if (house == null || house.IsEliminated)
					{
						Drop(r);
						Log.Write("sworn: " + r.House + " is gone; its manor is free");
						continue;
					}
					Clan warden = r.WardenClan;
					if (warden == null || warden.IsEliminated)
					{
						if (r.Warden != "")
						{
							r.Warden = "";
							r.Manor = "";
							Save(r);
							Log.Write("sworn: the warden of " + house.Name + " is gone; it holds of the crown now");
						}
						continue;
					}
					// Fiefs lost in war, or given on.
					if (!string.IsNullOrEmpty(r.Castle))
					{
						List<string> keep = new List<string>();
						foreach (string id in r.Castle.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						{
							Settlement f = Settlement.Find(id);
							if (f != null && f.OwnerClan == house)
							{
								keep.Add(id);
							}
							else
							{
								Log.Write("sworn: " + house.Name + " lost " + ((f != null) ? f.Name.ToString() : id));
							}
						}
						string now = string.Join(",", keep);
						if (now != r.Castle)
						{
							r.Castle = now;
							Save(r);
						}
					}
					// The manor is still the warden's?
					if (!string.IsNullOrEmpty(r.Manor))
					{
						Settlement v = Settlement.Find(r.Manor);
						Clan owner = (v != null && v.Village != null && v.Village.Bound != null) ? v.Village.Bound.OwnerClan : null;
						if (owner != warden && owner != house)
						{
							Log.Write("sworn: " + house.Name + " lost the manor of " + ((v != null) ? v.Name.ToString() : r.Manor) + " - it is no longer " + warden.Name + "'s");
							r.Manor = "";
							Village next = FreeVillages(warden).FirstOrDefault();
							if (next != null)
							{
								r.Manor = ((MBObjectBase)next.Settlement).StringId;
								Log.Write("sworn: " + warden.Name + " gives " + house.Name + " the manor of " + next.Name + " instead");
							}
							else
							{
								r.Lost = today;
							}
							Save(r);
						}
						else if (payday)
						{
							Pay(r, house, warden, v);
						}
					}
					else if (r.Lost >= 0 && today - r.Lost >= Cfg.DaysPerYear)
					{
						Release(r, "a year without land");
						continue;
					}
					else
					{
						Village next = FreeVillages(warden).FirstOrDefault();
						if (next != null)
						{
							r.Manor = ((MBObjectBase)next.Settlement).StringId;
							r.Lost = -1;
							Save(r);
							Log.Write("sworn: " + house.Name + " is given the manor of " + next.Name);
						}
					}
					Muster(house, warden);
				}
			}
			catch (Exception e)
			{
				Log.Once("sworndaily", "sworn houses' daily tick failed: " + e.Message);
			}
		}

		private static void Pay(Rec r, Clan house, Clan warden, Settlement v)
		{
			try
			{
				if (v == null || v.Village == null || house.Leader == null || warden.Leader == null)
				{
					return;
				}
				int daily = 0;
				try
				{
					daily = Campaign.Current.Models.ClanFinanceModel.CalculateVillageIncome(warden, v.Village, false);
				}
				catch
				{
					daily = (int)(v.Village.Hearth / 20f);
				}
				int due = Math.Max(0, daily * Cfg.DaysPerSeason * Cfg.SwornManorIncomePercent / 100);
				int paid = Math.Min(due, warden.Leader.Gold);
				if (paid > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(warden.Leader, house.Leader, paid, true);
				}
				Log.Write("sworn: " + house.Name + " takes " + paid + " from the manor of " + v.Name + " (" + daily + "/day, " + Cfg.SwornManorIncomePercent + "% for a season)");
				if (warden == Clan.PlayerClan && paid > 0)
				{
					Flow.Notify(house.Name + " takes its share of " + v.Name + ": " + paid.ToString("N0") + ".");
				}
			}
			catch (Exception e)
			{
				Log.Once("swornpay" + r.House, "sworn: the manor could not be paid: " + e.Message);
			}
		}

		// The warden calls: sworn houses close by join its army.
		private static void Muster(Clan house, Clan warden)
		{
			try
			{
				if (house == Clan.PlayerClan || warden.Leader == null)
				{
					return;
				}
				MobileParty wp = warden.Leader.PartyBelongedTo;
				Army army = (wp != null) ? wp.Army : null;
				if (army == null || army.LeaderParty != wp)
				{
					return;
				}
				foreach (WarPartyComponent c in house.WarPartyComponents.ToList())
				{
					MobileParty p = c.MobileParty;
					if (p != null && p.IsActive && p.Army == null && p.MapEvent == null && p.BesiegedSettlement == null && p.CurrentSettlement == null && p.LeaderHero != null && !p.IsDisbanding
						&& !Host.Is(p) && p.GetPosition2D.Distance(wp.GetPosition2D) < 150f)
					{
						p.Army = army;
						Log.Write("sworn: " + p.Name + " answers " + warden.Name + "'s call to arms");
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("swornmuster", "sworn: the call to arms failed: " + e.Message);
			}
		}

		// A warden changing realm: its houses follow, or break their oath.
		internal static void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool notify)
		{
			try
			{
				if (!Cfg.Sworn || !Store.Initialized || clan == null || newKingdom == null)
				{
					// A move between realms is a leave and then a join: decide once,
					// on the join.
					return;
				}
				foreach (Rec r in Under(clan))
				{
					Clan house = r.HouseClan;
					if (house == null || house.Kingdom == newKingdom)
					{
						continue;
					}
					if (MBRandom.RandomInt(100) < Cfg.SwornFollowChance)
					{
						if (house.Kingdom != null)
						{
							ChangeKingdomAction.ApplyByLeaveKingdom(house, false);
						}
						if (newKingdom != null)
						{
							ChangeKingdomAction.ApplyByJoinToKingdom(house, newKingdom, default(CampaignTime), false);
						}
						Log.Write("sworn: " + house.Name + " follows " + clan.Name + " " + ((newKingdom != null) ? ("into " + newKingdom.Name) : "out of its realm"));
					}
					else
					{
						Release(r, "it would not follow " + clan.Name + " out of " + ((oldKingdom != null) ? oldKingdom.Name.ToString() : "the realm"));
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("sworn: following the warden failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// Bellum's hierarchy tab: manors in the tooltips

		private static bool _patched;

		internal static void Patch()
		{
			if (_patched)
			{
				return;
			}
			_patched = true;
			try
			{
				Type t = AccessTools.TypeByName("BellumCivile.UI.VanillaTabs.Kingdoms.Hierarchy.HierarchyTitleNodeVM");
				MethodInfo m = (t != null) ? AccessTools.Method(t, "BuildTooltip", (Type[])null, (Type[])null) : null;
				if (m == null)
				{
					Log.Write("sworn: Bellum's hierarchy tooltip not found - manors will not show there");
					return;
				}
				new Harmony("community.wardens.and.dragons.sworn").Patch(m, null, new HarmonyMethod(typeof(Sworn).GetMethod("TooltipPost", BindingFlags.Static | BindingFlags.NonPublic)));
				Log.Write("sworn: manors written into Bellum's hierarchy tooltips");
			}
			catch (Exception e)
			{
				Log.Write("sworn: patching Bellum's hierarchy tooltip failed: " + e.Message);
			}
		}

		private static void TooltipPost(object __1, object __result)
		{
			try
			{
				IList list = __result as IList;
				if (list == null || __1 == null || !Cfg.Sworn)
				{
					return;
				}
				Type tp = __result.GetType().GetGenericArguments().FirstOrDefault();
				if (tp == null)
				{
					return;
				}
				List<string> lines = new List<string>();
				int tier = Bellum.TierOf(__1);
				if (tier >= 1)
				{
					Clan holder = Find(Bellum.DeJureHolderOf(__1));
					if (holder != null)
					{
						object top = TopTitle(holder);
						if (top != null && Bellum.IdOf(top) == Bellum.IdOf(__1))
						{
							foreach (Rec r in Under(holder))
							{
								Clan h = r.HouseClan;
								if (h == null)
								{
									continue;
								}
								lines.Add(h.Name + " - " + Holdings(r));
							}
						}
					}
					if (lines.Count > 0)
					{
						Add(list, tp, "Sworn houses", "");
					}
				}
				else if (tier == 0)
				{
					Settlement s = Bellum.CapitalOf(__1);
					if (s != null)
					{
						foreach (Rec r in All())
						{
							Settlement v = Settlement.Find(r.Manor);
							if (v != null && v.Village != null && v.Village.Bound == s && r.HouseClan != null)
							{
								lines.Add(v.Name + ": " + r.HouseClan.Name);
							}
						}
					}
					if (lines.Count > 0)
					{
						Add(list, tp, "Manors", "");
					}
				}
				foreach (string line in lines)
				{
					Add(list, tp, "  " + line, "");
				}
			}
			catch (Exception e)
			{
				Log.Once("sworntooltip", "sworn: the hierarchy tooltip failed: " + e.Message);
			}
		}

		private static void Add(IList list, Type tp, string a, string b)
		{
			ConstructorInfo ctor = tp.GetConstructors().FirstOrDefault((ConstructorInfo c) => c.GetParameters().Length == 5 && c.GetParameters()[0].ParameterType == typeof(string) && c.GetParameters()[1].ParameterType == typeof(string));
			if (ctor == null)
			{
				return;
			}
			ParameterInfo[] ps = ctor.GetParameters();
			object flags = Enum.ToObject(ps[4].ParameterType, 0);
			list.Add(ctor.Invoke(new object[5] { a, b, 0, false, flags }));
		}

		// ------------------------------------------------------------------
		// reading

		internal static string Line(Rec r)
		{
			Clan h = r.HouseClan;
			return ((h != null) ? h.Name.ToString() : r.House) + " (" + r.Kind + "), " + Holdings(r);
		}

		// "town of A, castle of B, manor of C", or "landless".
		internal static string Holdings(Rec r)
		{
			List<string> parts = Fiefs(r).Select((Settlement x) => (x.IsTown ? "town of " : "castle of ") + x.Name).ToList();
			Settlement v = Settlement.Find(r.Manor);
			if (v != null)
			{
				parts.Add("manor of " + v.Name);
			}
			return (parts.Count > 0) ? string.Join(", ", parts) : "landless";
		}

		internal static string ForceFief(string name)
		{
			Clan w = Wardens().FirstOrDefault((Clan c) => string.IsNullOrEmpty(name) || c.Name.ToString().ToLowerInvariant().Contains(name.ToLowerInvariant()));
			return (w == null) ? "No warden by that name." : (AiFief(w) ?? "Nothing happened - see the log.");
		}

		internal static string Summary(Kingdom k)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (Clan w in Wardens(k))
			{
				List<Rec> under = Under(w);
				sb.Append("  ").Append(Styles.Titled(w)).Append(" - ").Append(under.Count).Append(" of ").Append(Cap(Rank(w))).Append(" sworn houses\n");
				foreach (Rec r in under)
				{
					sb.Append("      ").Append(Line(r)).Append("\n");
				}
			}
			return sb.ToString();
		}

		internal static string Report()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int n = All().Count;
			sb.Append("Sworn houses: ").Append(n).Append(" of a world cap of ").Append(WorldCap()).Append("\n");
			foreach (Kingdom k in Kingdom.All.Where((Kingdom x) => !x.IsEliminated))
			{
				string s = Summary(k);
				if (s.Length > 0)
				{
					sb.Append(k.Name).Append(":\n").Append(s);
				}
			}
			Log.Write("sworn report:\n" + sb);
			return sb.ToString();
		}

		internal static string ForceRaise(string name)
		{
			Clan w = Wardens().Where((Clan c) => string.IsNullOrEmpty(name) || c.Name.ToString().ToLowerInvariant().Contains(name.ToLowerInvariant())).FirstOrDefault();
			if (w == null)
			{
				return "No warden by that name.";
			}
			string why = CannotGain(w, false);
			if (why != null)
			{
				return "Cannot: " + why + ".";
			}
			string kind = (InviteCandidates(w).Count > 0) ? "invited" : ((CadetCandidates(w).Count > 0) ? "cadet" : "knight");
			Clan house = Gain(w, kind, null, null, null, false);
			return (house != null) ? (w.Name + " gained " + house.Name + " (" + kind + ").") : "It failed - see the log.";
		}
	}
}
