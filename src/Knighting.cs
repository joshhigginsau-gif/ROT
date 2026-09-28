using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Knights of the realm.
	//
	// A ruler may knight anyone - a soldier out of the ranks, a wanderer met
	// in a tavern, a companion, a younger son. A knight has a name and a house
	// of their own, and nothing else: no land, no keep. The house rides for
	// your realm as a free company, paid as sellswords are, and like
	// sellswords it may go whenever it pleases.
	internal static class Knighting
	{
		private const string Prefix = "kn:";
		private const string RollKey = "kx:roll";

		// ------------------------------------------------------------------
		// who

		internal sealed class Candidate
		{
			internal string Kind;
			internal Hero Hero;
			internal CharacterObject Troop;
			internal string Label;
		}

		internal static string CanKnight()
		{
			if (!Cfg.Knights)
			{
				return "Knighthoods are turned off.";
			}
			if (!Council.Rules)
			{
				return "Only a ruler can knight.";
			}
			if (Hero.MainHero.Gold < Cfg.KnightCost)
			{
				return "A knighting costs " + Cfg.KnightCost.ToString("N0") + " gold - the purse, the arms and the men.";
			}
			return null;
		}

		internal static List<Candidate> Candidates()
		{
			List<Candidate> list = new List<Candidate>();
			try
			{
				Hero heir = null;
				try
				{
					heir = Succession.Named();
				}
				catch
				{
				}
				foreach (Hero h in Clan.PlayerClan.Heroes.Where((Hero x) => x.IsAlive && !x.IsChild && !x.IsPrisoner && x != Hero.MainHero && x != heir && !Guard.IsSworn(x)))
				{
					bool companion = h.IsWanderer || h.CompanionOf == Clan.PlayerClan;
					list.Add(new Candidate { Kind = companion ? "companion" : "kin", Hero = h, Label = h.Name + (companion ? "  - your companion" : "  - of your blood, and your house") });
				}
				foreach (Hero h in Hero.MainHero.CompanionsInParty.Where((Hero x) => x.IsAlive && !Guard.IsSworn(x) && !list.Any((Candidate c) => c.Hero == x)))
				{
					list.Add(new Candidate { Kind = "companion", Hero = h, Label = h.Name + "  - your companion" });
				}
				Settlement here = Settlement.CurrentSettlement;
				if (here != null)
				{
					foreach (Hero h in here.HeroesWithoutParty.Where((Hero x) => x.IsAlive && x.IsWanderer && x.CompanionOf == null && x.Clan == null && !x.IsChild))
					{
						list.Add(new Candidate { Kind = "wanderer", Hero = h, Label = h.Name + "  - a wanderer at " + here.Name });
					}
				}
				foreach (TroopRosterElement e in MobileParty.MainParty.MemberRoster.GetTroopRoster().Where((TroopRosterElement x) => x.Character != null && !x.Character.IsHero && x.Character.Tier >= 3 && x.Number > x.WoundedNumber)
					.OrderByDescending((TroopRosterElement x) => x.Character.Tier).Take(12))
				{
					list.Add(new Candidate { Kind = "soldier", Troop = e.Character, Label = e.Character.Name + "  - a soldier of your host (tier " + e.Character.Tier + ")" });
				}
			}
			catch (Exception e)
			{
				Log.Write("finding knights failed: " + e.Message);
			}
			return list;
		}

		internal static void Pick()
		{
			string why = CanKnight();
			if (why != null)
			{
				Flow.Notify(why);
				return;
			}
			List<Candidate> can = Candidates();
			if (can.Count == 0)
			{
				Flow.Notify("There is nobody here worth the spurs.");
				return;
			}
			List<InquiryElement> els = can.Select((Candidate c) => new InquiryElement(c, c.Label, null, true, "")).ToList();
			Inquiry.Select("Knight Someone", "Who kneels? A knight is given a house of their own - no land, no keep - and rides for your realm as a free company, for as long as it suits them. It costs " +
				Cfg.KnightCost.ToString("N0") + " gold.", els, 1, 1, "Arise", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Candidate c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Candidate) : null;
					if (c != null)
					{
						Knight(c);
					}
				});
		}

		// ------------------------------------------------------------------
		// the ceremony

		private static void Knight(Candidate c)
		{
			try
			{
				if (CanKnight() != null)
				{
					return;
				}
				Hero h;
				string origin;
				if (c.Kind == "soldier")
				{
					h = Guard.KnightSoldier(c.Troop);
					origin = "soldier:" + ((MBObjectBase)c.Troop).StringId;
				}
				else
				{
					h = c.Hero;
					origin = c.Kind;
					if (c.Kind == "wanderer" || c.Kind == "companion")
					{
						try
						{
							h.SetNewOccupation(Occupation.Lord);
						}
						catch
						{
						}
					}
					// A companion's house is read from CompanionOf before anything
					// else; the game's own path for a companion made a lord
					// clears it. Leaving your party is done when the company
					// is raised.
					if (h.CompanionOf != null)
					{
						try
						{
							RemoveCompanionAction.ApplyByByTurningToLord(h.CompanionOf, h);
						}
						catch (Exception e)
						{
							Log.Write("releasing the companion failed: " + e.Message);
						}
					}
					string first = (h.FirstName != null) ? h.FirstName.ToString() : h.Name.ToString();
					if (!h.Name.ToString().StartsWith("Ser ") && !h.Name.ToString().StartsWith("Lord ") && !h.Name.ToString().StartsWith("Lady "))
					{
						h.SetName(new TextObject("{=!}Ser " + first, (Dictionary<string, object>)null), new TextObject("{=!}" + first, (Dictionary<string, object>)null));
					}
				}
				if (h == null)
				{
					Flow.Notify("The knighting could not be done - see the log.");
					return;
				}
				Clan house = Found(h);
				if (house == null)
				{
					Flow.Notify("The house could not be founded - see the log.");
					return;
				}
				Hero.MainHero.ChangeHeroGold(-Cfg.KnightCost);
				Standing.Change(Cfg.KnightHonour, 0, "Knighted " + h.Name);
				Company(h, house, c.Troop);
				Serve(house);
				string story = Story(h, c.Kind, c.Troop, house);
				string before = (h.EncyclopediaText != null) ? h.EncyclopediaText.ToString() : "";
				h.EncyclopediaText = new TextObject("{=!}" + ((c.Kind == "soldier" || string.IsNullOrEmpty(before)) ? "" : (before + "\n\n")) + story, (Dictionary<string, object>)null);
				Store.Set(Prefix + ((MBObjectBase)house).StringId, ((MBObjectBase)h).StringId + "|" + CourtBehavior.Today() + "|" + origin);
				Store.AddDeed(Standing.Date() + "  Knighted " + h.Name + ", who founded " + house.Name + ".");
				Log.Write("knighted " + h.Name + " (" + origin + "), " + house.Name + " (" + ((MBObjectBase)house).StringId + ")");
				Ravens.Popup("Arise, " + h.Name, h.Name + " kneels a " + ((c.Kind == "soldier") ? "soldier" : ((c.Kind == "kin") ? "younger child of your house" : "wanderer")) +
					" and rises a knight, and the heralds write a new name in their rolls: " + house.Name + ".\n\nIt holds no land and no keep. It rides for your realm as a free company, paid as sellswords are - and like sellswords, it may go when it pleases.");
			}
			catch (Exception e)
			{
				Log.Write("the knighting failed: " + e);
			}
		}

		// A house with a name and arms, and nothing else.
		private static Clan Found(Hero h)
		{
			try
			{
				int n = Store.GetI("kx:next", 1);
				Store.SetI("kx:next", n + 1);
				Clan house = Clan.CreateClan("wad_knight_" + n + "_" + CourtBehavior.Today());
				if (house == null)
				{
					return null;
				}
				string name = Lore.HouseName();
				Bastard.Set(house, "Name", new TextObject("{=!}" + name, (Dictionary<string, object>)null));
				Bastard.Set(house, "InformalName", new TextObject("{=!}" + name.Replace("House ", ""), (Dictionary<string, object>)null));
				house.Culture = h.Culture ?? Clan.PlayerClan.Culture;
				house.Banner = Banner.CreateRandomClanBanner(-1);
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
				Bastard.Set(house, "Tier", Cfg.KnightHouseTier);
				h.Clan = house;
				house.SetLeader(h);
				Settlement home = Clan.PlayerClan.HomeSettlement ?? Settlement.CurrentSettlement;
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
				Log.Write("founding the knight's house failed: " + e);
				return null;
			}
		}

		// His company: a lance of men to start.
		private static void Company(Hero h, Clan house, CharacterObject oldTroop)
		{
			try
			{
				MobileParty p = MobilePartyHelper.CreateNewClanMobileParty(h, house);
				if (p == null)
				{
					return;
				}
				List<CharacterObject> troops = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Culture == house.Culture && x.Occupation == Occupation.Soldier && x.Tier >= 2 && x.Tier <= 4).ToList();
				int men = Cfg.KnightStartingMen;
				if (oldTroop != null && men > 0)
				{
					int mine = Math.Max(1, men / 3);
					p.MemberRoster.AddToCounts(oldTroop, mine, false, 0, 0, true, -1);
					men -= mine;
				}
				for (int i = 0; i < men && troops.Count > 0; i++)
				{
					p.MemberRoster.AddToCounts(troops[MBRandom.RandomInt(troops.Count)], 1, false, 0, 0, true, -1);
				}
			}
			catch (Exception e)
			{
				Log.Write("raising the knight's company failed: " + e.Message);
			}
		}

		private static void Serve(Clan house)
		{
			try
			{
				Kingdom k = Clan.PlayerClan.Kingdom;
				if (k != null)
				{
					ChangeKingdomAction.ApplyByJoinFactionAsMercenary(house, k, CampaignTime.DaysFromNow((float)Cfg.KnightContractDays), 50, false);
				}
			}
			catch (Exception e)
			{
				Log.Write("the knight's house could not take service: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the page in the book

		private static string Story(Hero h, string kind, CharacterObject troop, Clan house)
		{
			string he = h.IsFemale ? "she" : "he";
			string his = h.IsFemale ? "her" : "his";
			string place = (h.BornSettlement != null) ? h.BornSettlement.Name.ToString() : "a place nobody remembers";
			string culture = (h.Culture != null) ? h.Culture.Name.ToString() : "the east";
			string ruler = Hero.MainHero.Name.ToString();
			StringBuilder sb = new StringBuilder();
			switch (kind)
			{
			case "soldier":
				sb.Append(Name(h)).Append(" was born common in ").Append(place).Append(", and served in the ranks of ").Append(ruler).Append("'s host as ")
				  .Append((troop != null) ? troop.Name.ToString() : "a soldier").Append(" through more battles than ").Append(he).Append(" will speak of. ");
				break;
			case "wanderer":
				sb.Append(Name(h)).Append(" came out of ").Append(culture).Append(" lands with a sword and no master, and sold both for years in the taverns and the free companies of the realm. ");
				break;
			case "kin":
				sb.Append(Name(h)).Append(" was born of the blood of ").Append(Clan.PlayerClan.Name).Append(", too far down the line ever to hold its seat. ");
				break;
			default:
				sb.Append(Name(h)).Append(" rode at ").Append(ruler).Append("'s side as a companion, for no better reason than that it paid and the company was good. ");
				break;
			}
			sb.Append(Trait(h)).Append(" ");
			sb.Append("On ").Append(Standing.Date()).Append(", ").Append(ruler).Append(" knighted ").Append(him(h)).Append(", and ").Append(he).Append(" founded ").Append(house.Name)
			  .Append(" - a house with a name and arms and no land at all, which rides for the crown as a free company, and owes it exactly as much as it is paid.");
			return sb.ToString();
		}

		private static string him(Hero h)
		{
			return h.IsFemale ? "her" : "him";
		}

		private static string Name(Hero h)
		{
			return (h.FirstName != null) ? h.FirstName.ToString() : h.Name.ToString();
		}

		private static string Trait(Hero h)
		{
			try
			{
				string he = h.IsFemale ? "She" : "He";
				if (h.GetTraitLevel(DefaultTraits.Valor) > 0)
				{
					return he + " is known for going first through a breach and last out of a rout.";
				}
				if (h.GetTraitLevel(DefaultTraits.Honor) > 0)
				{
					return he + " has never been known to break " + (h.IsFemale ? "her" : "his") + " word, which is rarer than courage.";
				}
				if (h.GetTraitLevel(DefaultTraits.Calculating) > 0)
				{
					return he + " thinks before " + (h.IsFemale ? "she" : "he") + " fights, and is still alive because of it.";
				}
				if (h.GetTraitLevel(DefaultTraits.Mercy) > 0)
				{
					return he + " has spared more men than " + (h.IsFemale ? "she" : "he") + " has killed, and some of them were grateful.";
				}
				if (h.GetTraitLevel(DefaultTraits.Honor) < 0)
				{
					return he + " has been paid by both sides of more than one war, and says it was only the once.";
				}
			}
			catch
			{
			}
			return (h.IsFemale ? "She" : "He") + " has a scar for every year of service, and a story for most of them.";
		}

		// ------------------------------------------------------------------
		// free companies come and go

		internal static List<Clan> Houses()
		{
			List<Clan> list = new List<Clan>();
			foreach (string key in Store.Keys(Prefix))
			{
				string id = key.Substring(Prefix.Length);
				Clan c = Clan.FindFirst((Clan x) => ((MBObjectBase)x).StringId == id);
				if (c != null)
				{
					list.Add(c);
				}
			}
			return list;
		}

		internal static void Weekly()
		{
			try
			{
				if (!Cfg.Knights || !Store.Initialized)
				{
					return;
				}
				int today = CourtBehavior.Today();
				if (today - Store.GetI(RollKey, -9999) < 7)
				{
					return;
				}
				Store.SetI(RollKey, today);
				Kingdom mine = Clan.PlayerClan.Kingdom;
				foreach (Clan c in Houses())
				{
					if (c.IsEliminated || c.Leader == null || !c.Leader.IsAlive)
					{
						continue;
					}
					if (mine == null || c.Kingdom != mine || !c.IsUnderMercenaryService)
					{
						continue;
					}
					float rel = c.Leader.GetRelationWithPlayer();
					bool unhappy = rel < (float)Cfg.KnightLeaveRelation;
					if ((unhappy && MBRandom.RandomInt(100) < Cfg.KnightLeaveChance * 2) || MBRandom.RandomInt(100) < ((rel < 0f) ? Cfg.KnightLeaveChance : Cfg.KnightLeaveChance / 5))
					{
						Leave(c, unhappy ? "they have no love left for you" : "a better purse was offered elsewhere");
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("knweekly", "the knights' tick failed: " + e.Message);
			}
		}

		internal static void Leave(Clan c, string why)
		{
			try
			{
				ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(c, false);
			}
			catch (Exception e)
			{
				Log.Write("a knight's house leaving failed: " + e.Message);
				return;
			}
			Log.Write(c.Name + " left your service: " + why);
			Store.AddDeed(Standing.Date() + "  " + c.Name + " rode out of your service.");
			Ravens.Popup("A Free Company Rides Out", c.Name + " has left your service - " + why + ". They were knighted by your hand, and owe you nothing but the name.");
		}

		// Cheat: the newest serving house leaves now.
		internal static string LeaveNow()
		{
			Clan c = Houses().LastOrDefault((Clan x) => !x.IsEliminated && x.Kingdom == Clan.PlayerClan.Kingdom);
			if (c == null)
			{
				return "No knight's house is in your service.";
			}
			Leave(c, "you told them to go");
			return c.Name + " has left.";
		}

		internal static void AskBack()
		{
			Kingdom mine = Clan.PlayerClan.Kingdom;
			List<Clan> gone = Houses().Where((Clan c) => !c.IsEliminated && c.Leader != null && c.Leader.IsAlive && c.Kingdom == null && c.Leader.GetRelationWithPlayer() >= 0f).ToList();
			if (mine == null || gone.Count == 0)
			{
				Flow.Notify("No knight's house is free and willing to come back.");
				return;
			}
			List<InquiryElement> els = gone.Select((Clan c) => new InquiryElement(c, c.Name + " (" + c.Leader.Name + ")", null, Hero.MainHero.Gold >= Cfg.KnightRehireCost, "")).ToList();
			Inquiry.Select("Ask Them Back", "A new contract costs " + Cfg.KnightRehireCost.ToString("N0") + " gold.", els, 1, 1, "Send the offer", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Clan c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Clan) : null;
					if (c == null || Hero.MainHero.Gold < Cfg.KnightRehireCost)
					{
						return;
					}
					Hero.MainHero.ChangeHeroGold(-Cfg.KnightRehireCost);
					Serve(c);
					Ravens.Popup("Back in Service", c.Name + " rides for your realm again.");
				});
		}

		internal static string Summary()
		{
			StringBuilder sb = new StringBuilder();
			Kingdom mine = Clan.PlayerClan.Kingdom;
			foreach (Clan c in Houses())
			{
				Hero l = c.Leader;
				string status = c.IsEliminated ? "is no more" : ((l == null || !l.IsAlive) ? "has lost its knight" : ((c.Kingdom == mine && mine != null) ? "serves you" : ((c.Kingdom != null) ? ("serves " + c.Kingdom.Name) : "is unsworn")));
				int men = 0;
				try
				{
					men = c.WarPartyComponents.Sum((TaleWorlds.CampaignSystem.Party.PartyComponents.WarPartyComponent w) => w.MobileParty.MemberRoster.TotalManCount);
				}
				catch
				{
				}
				sb.Append(c.Name).Append(" - ").Append((l != null) ? l.Name.ToString() : "?").Append(", ").Append(men).Append(" men, ").Append(status);
				if (l != null && l.IsAlive)
				{
					sb.Append(", relation ").Append((int)l.GetRelationWithPlayer());
				}
				sb.Append(".\n");
			}
			return sb.ToString();
		}
	}
}
