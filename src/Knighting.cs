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
					if (!companion)
					{
						// Blood of your house is not knighted out of it.
						continue;
					}
					list.Add(new Candidate { Kind = "companion", Hero = h, Label = h.Name + "  - your companion" });
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
						NameIt(c);
					}
				});
		}

		// ------------------------------------------------------------------
		// the ceremony

		private static void NameIt(Candidate c)
		{
			CultureObject culture = (c.Troop != null) ? c.Troop.Culture : ((c.Hero != null) ? c.Hero.Culture : Clan.PlayerClan.Culture);
			string suggestion = HouseName(culture);
			Inquiry.Text("A House of Their Own", "What will the house be called? (Leave it as it is to take the herald's suggestion.)", suggestion, "So be it", "Let the herald choose",
				delegate(string text)
				{
					string name = (text ?? "").Trim();
					if (name.Length == 0)
					{
						name = suggestion;
					}
					if (!name.StartsWith("House ", StringComparison.OrdinalIgnoreCase))
					{
						name = "House " + name;
					}
					Knight(c, name);
				},
				delegate
				{
					Knight(c, suggestion);
				});
		}

		private static void Knight(Candidate c, string houseName)
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
				Clan house = Found(h, houseName);
				if (house == null)
				{
					Flow.Notify("The house could not be founded - see the log.");
					return;
				}
				Hero.MainHero.ChangeHeroGold(-Cfg.KnightCost);
				Standing.Change(Cfg.KnightHonour, 0, "Knighted " + h.Name);
				Company(h, house, c.Troop);
				Serve(house);
				string words = Pick(Words);
				string story = Story(h, c.Kind, c.Troop, house, words);
				Bastard.Set(house, "EncyclopediaText", new TextObject("{=!}" + HouseStory(house, h, c.Kind, c.Troop, words), (Dictionary<string, object>)null));
				string before = (h.EncyclopediaText != null) ? h.EncyclopediaText.ToString() : "";
				h.EncyclopediaText = new TextObject("{=!}" + ((c.Kind == "soldier" || string.IsNullOrEmpty(before)) ? "" : (before + "\n\n")) + story, (Dictionary<string, object>)null);
				Store.Set(Prefix + ((MBObjectBase)house).StringId, ((MBObjectBase)h).StringId + "|" + CourtBehavior.Today() + "|" + origin + "|" + words);
				Store.AddDeed(Standing.Date() + "  Knighted " + h.Name + ", who founded " + house.Name + ".");
				Log.Write("knighted " + h.Name + " (" + origin + "), " + house.Name + " (" + ((MBObjectBase)house).StringId + ")");
				Ravens.Popup("Arise, " + h.Name, h.Name + " kneels a " + ((c.Kind == "soldier") ? "soldier" : ((c.Kind == "companion") ? "companion" : "wanderer")) +
					" and rises a knight, and the heralds write a new name in their rolls: " + house.Name + ".\n\nIt holds no land and no keep. It rides for your realm as a free company, paid as sellswords are - and like sellswords, it may go when it pleases.");
			}
			catch (Exception e)
			{
				Log.Write("the knighting failed: " + e);
			}
		}

		// A house with a name and arms, and nothing else.
		private static Clan Found(Hero h, string name)
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
		internal static void Company(Hero h, Clan house, CharacterObject oldTroop)
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
		// what the heralds call them

		private static readonly string[] WestA = { "Ash", "Black", "Bright", "Crane", "Dun", "Fair", "Grey", "Hard", "High", "Iron", "Long", "Oak", "Red", "Rook", "Stone", "Storm", "Thorn", "Cold", "Deep", "Hollow", "Brack", "Tall", "Rush", "Wyn", "Mar", "Hay", "Crow", "Salt", "Elm", "Heron", "Cask", "Wick" };

		private static readonly string[] WestB = { "wood", "ford", "wick", "ton", "well", "mere", "hill", "stone", "brook", "vale", "more", "ley", "hart", "shaw", "by", "field", "holt", "burn", "den", "gate", "moor", "cliff" };

		private static readonly string[] EastA = { "Var", "Mael", "Tor", "Qar", "Vel", "Dar", "Sael", "Rhae", "Ves", "Nor", "Zhar", "Mys", "Oth", "Lor", "Bel", "Tyr" };

		private static readonly string[] EastB = { "yon", "ys", "aris", "enos", "aro", "ion", "ahar", "eris", "oros", "ane", "aqo", "ello", "ivar", "aros" };

		internal static bool Essos(CultureObject c)
		{
			if (c == null)
			{
				return false;
			}
			string k = (((MBObjectBase)c).StringId + " " + c.Name).ToLowerInvariant();
			foreach (string w in new string[15] { "volant", "pentos", "braav", "lys", "myr", "tyrosh", "qohor", "norvos", "lorath", "ghis", "yunkai", "meereen", "astapor", "valyr", "essos" })
			{
				if (k.Contains(w))
				{
					return true;
				}
			}
			return false;
		}

		internal static string HouseName(CultureObject culture)
		{
			bool east = Essos(culture);
			for (int i = 0; i < 40; i++)
			{
				string n = east ? (EastA[MBRandom.RandomInt(EastA.Length)] + EastB[MBRandom.RandomInt(EastB.Length)]) : (WestA[MBRandom.RandomInt(WestA.Length)] + WestB[MBRandom.RandomInt(WestB.Length)]);
				string full = "House " + n;
				if (!Clan.All.Any((Clan c) => c != null && c.Name != null && (c.Name.ToString() == full || c.Name.ToString() == n)))
				{
					return full;
				}
			}
			return "House " + WestA[MBRandom.RandomInt(WestA.Length)] + WestB[MBRandom.RandomInt(WestB.Length)] + "e";
		}

		// ------------------------------------------------------------------
		// the page in the book

		private static readonly string[] Quirks =
		{
			"sings badly and often, and will not be talked out of it",
			"will not ride a grey horse, and has never said why",
			"keeps a list of every man {he} has killed, and reads it on feast days",
			"prays to every god anyone has ever named to {him}, just in case",
			"cannot read, and has memorised the Seven-Pointed Star by ear to hide it",
			"carries a wooden sword from childhood in {his} saddlebag",
			"has never lost at dice, which is its own kind of reputation",
			"eats standing up, the way {he} learned to in the ranks",
			"names every horse {he} owns after a lord {he} has outlived",
			"is famous in three taverns for a song about {himself} that {he} did not write",
			"sleeps with {his} boots on and a knife under the pillow, even in a castle",
			"will fight any man who calls {him} common, and has",
			"sends half of every purse home to a village nobody else has heard of",
			"has a laugh you can hear across a battlefield",
			"never drinks before a fight, and never stops after one",
			"remembers the name of every man who ever served under {him}"
		};

		private static readonly string[] Words =
		{
			"We Hold", "Steel Remembers", "No Debt Unpaid", "From Nothing", "First Through the Breach", "Earned, Not Given",
			"The Road Is Long", "Sworn and Paid", "We Answer", "Blood and Coin", "Never Knelt Twice", "Last Off the Field",
			"Ask Our Enemies", "By Our Own Hand", "Rise", "Hold the Line"
		};

		private static string Pick(string[] a)
		{
			return a[MBRandom.RandomInt(a.Length)];
		}

		private static string Sub(string text, Hero h)
		{
			return text.Replace("{he}", h.IsFemale ? "she" : "he").Replace("{him}", h.IsFemale ? "her" : "him").Replace("{his}", h.IsFemale ? "her" : "his")
				.Replace("{himself}", h.IsFemale ? "herself" : "himself");
		}

		private static string Place(Hero h)
		{
			try
			{
				List<Settlement> near = Settlement.All.Where((Settlement s) => (s.IsTown || s.IsCastle) && s.Culture == h.Culture).ToList();
				if (near.Count == 0)
				{
					near = Settlement.All.Where((Settlement s) => s.IsTown || s.IsCastle).ToList();
				}
				return near[MBRandom.RandomInt(near.Count)].Name.ToString();
			}
			catch
			{
				return "a place nobody remembers";
			}
		}

		private static string Deed(Hero h)
		{
			string at = Place(h);
			string[] deeds =
			{
				"{he} held a ford alone for the better part of an hour at " + at + ", and has the scars to show for every minute of it",
				"{he} was the last off the walls when " + at + " fell, and carried two men down with {him}",
				"{he} won a purse at the tourney at " + at + ", and spent it all before the next dawn",
				"{he} once carried a wounded captain three leagues out of an ambush near " + at,
				"{he} took a banner at " + at + " that {he} still will not give back",
				"{he} broke a lance on a lord's son at " + at + " and has been paying for it ever since",
				"{he} talked a garrison at " + at + " into opening its gates, and never tells the same story about how",
				"{he} lost two fingers at " + at + " and learned to fight left-handed within the year",
				"{he} was left for dead after a skirmish near " + at + " and walked home anyway",
				"{he} kept a village near " + at + " from being burned, which nobody paid {him} for",
				"{he} swam a flooded river at " + at + " with {his} sword in {his} teeth",
				"{he} held the rear of a rout at " + at + " until there was nobody left to hold it for",
				"{he} unhorsed a hedge knight thrice {his} size at " + at + " with a borrowed lance",
				"{he} guarded a merchant's gold from " + at + " to the sea and did not steal a coin of it",
				"{he} escaped from the cells at " + at + " with a spoon and a great deal of patience",
				"{he} was knighted in all but name on the field at " + at + ", years before anyone made it true"
			};
			return Sub(Pick(deeds), h);
		}

		private static string Story(Hero h, string kind, CharacterObject troop, Clan house, string words)
		{
			string name = (h.FirstName != null) ? h.FirstName.ToString() : h.Name.ToString();
			string place = (h.BornSettlement != null) ? h.BornSettlement.Name.ToString() : Place(h);
			string culture = (h.Culture != null) ? h.Culture.Name.ToString() : "far";
			string ruler = Hero.MainHero.Name.ToString();
			string trooped = (troop != null) ? troop.Name.ToString() : "a soldier";
			string[] openings;
			switch (kind)
			{
			case "soldier":
				openings = new string[4]
				{
					name + " was born common in " + place + ", and served in the ranks of " + ruler + "'s host as " + trooped + " through more battles than {he} will speak of.",
					"The son of nobody in particular from " + place + ", " + name + " took the coin of " + ruler + "'s host and fought as " + trooped + " until there was nobody left in the company older than {him}.",
					name + " came out of " + place + " with nothing but {his} own two hands, and put them to work as " + trooped + " in " + ruler + "'s service.",
					"Nobody in " + place + " expected much of " + name + ", who went for a soldier young and served as " + trooped + " under " + ruler + "'s banner for years."
				};
				break;
			case "wanderer":
				openings = new string[4]
				{
					name + " came out of " + culture + " lands with a sword and no master, and sold both for years in the taverns and free companies of the realm.",
					"Born in " + place + ", " + name + " has been a hedge knight in all but the name, and a sellsword when the name did not pay.",
					name + " has walked more roads than most lords have heard of, and fought on more sides of more wars than {he} admits to.",
					"The taverns of " + place + " knew " + name + " long before any lord did - as a sword for hire, and a good one."
				};
				break;
			default:
				openings = new string[4]
				{
					name + " rode at " + ruler + "'s side as a companion, for no better reason at first than that it paid and the company was good.",
					"Born in " + place + ", " + name + " fell in with " + ruler + " on the road and never quite found a reason to leave.",
					name + " followed " + ruler + " out of a tavern one night on a promise of work, and stayed for the wars.",
					"A wanderer out of " + culture + " lands, " + name + " has been at " + ruler + "'s shoulder longer than most of the lords at court."
				};
				break;
			}
			List<string> middle = new List<string>
			{
				"It is told that " + Deed(h) + ".",
				Sub(name + " " + Pick(Quirks) + ".", h),
				Trait(h)
			};
			// Shuffle the middle so no two pages run the same way.
			for (int i = middle.Count - 1; i > 0; i--)
			{
				int j = MBRandom.RandomInt(i + 1);
				string t = middle[i];
				middle[i] = middle[j];
				middle[j] = t;
			}
			string[] endings =
			{
				"On " + Standing.Date() + " " + ruler + " knighted {him}, and {he} founded " + house.Name + ", whose words are \"" + words + "\". It holds no land at all, and rides for the crown as a free company, owing it exactly as much as it is paid.",
				ruler + " gave {him} the accolade on " + Standing.Date() + ". " + house.Name + " took the words \"" + words + "\" and not an acre of ground, and serves the crown as sellswords serve - well, and for as long as the purse lasts.",
				"Knighted by " + ruler + " on " + Standing.Date() + ", {he} founded " + house.Name + " (\"" + words + "\"): a house with a name and arms, no keep, and a free company that goes where the coin is."
			};
			StringBuilder sb = new StringBuilder();
			sb.Append(Sub(Pick(openings), h)).Append(" ");
			foreach (string m in middle.Take(2 + MBRandom.RandomInt(2)))
			{
				sb.Append(m).Append(" ");
			}
			sb.Append("\n\n").Append(Sub(Pick(endings), h));
			return sb.ToString();
		}

		// The house's own page.
		internal static string HouseStory(Clan house, Hero knight, string kind, CharacterObject troop, string words)
		{
			string ruler = Hero.MainHero.Name.ToString();
			string realm = (Clan.PlayerClan.Kingdom != null) ? Clan.PlayerClan.Kingdom.Name.ToString() : Clan.PlayerClan.Name.ToString();
			string who = (knight != null) ? knight.Name.ToString() : "a knight whose name the heralds have mislaid";
			string place = (knight != null && knight.BornSettlement != null) ? knight.BornSettlement.Name.ToString() : ((knight != null) ? Place(knight) : "nowhere in particular");
			string field = (knight != null) ? Place(knight) : "a field long since ploughed";
			string bare = house.Name.ToString().Replace("House ", "");
			string when = Standing.Date();
			string founding;
			switch (kind)
			{
			case "wanderer":
				founding = house.Name + " was founded on " + when + ", when " + ruler + " knighted " + who + " - a sword for hire who had never before had anything to call " + Own(knight) + " but the sword.";
				break;
			case "companion":
				founding = house.Name + " was founded on " + when + ", when " + ruler + " knighted " + who + ", who had ridden at the crown's side for years with no more title than a friend has.";
				break;
			default:
				founding = house.Name + " was founded on " + when + ", when " + ruler + " took " + who + " out of the ranks - where " + Pronoun(knight) + " had served as " + ((troop != null) ? troop.Name.ToString() : "a common soldier") + " - and made " + Object(knight) + " a knight.";
				break;
			}
			string[] names =
			{
				"The name is taken from " + place + ", where the founder was born and which has never before produced anybody the heralds needed to write down.",
				"The name is said to come from a ford near " + field + " where the founder held the crossing, though the ford has another name on every map.",
				"It takes its name from the founder's mother's people, who would be astonished to hear it.",
				"\"" + bare + "\" was what the founder's company called " + Object(knight) + " in the ranks; the heralds made it respectable.",
				"The name belongs to a ruined hold near " + place + " that the founder has never owned, and says one day " + Pronoun(knight) + " will.",
				"Nobody is quite sure where the name comes from, and the founder tells it differently every time " + Pronoun(knight) + " is asked.",
				"The founder chose the name on the morning of the knighting, and has pretended ever since that it is very old.",
				"The name was bought, with the arms, from a herald who needed the money more than the house needed the history."
			};
			string[] known =
			{
				"Its company is known for fighting hard and asking for its pay the same evening.",
				"It is known for paying its men on the day, which makes it the envy of better houses.",
				"It is known for never looting a village it did not have to, which costs it men every year.",
				"It is known for looting everything that is not nailed down, and some things that are.",
				"Its men are known for their songs, which are loud, many, and mostly about the founder.",
				"Its riders are known for their horses, which are better than the house can afford.",
				"Its crossbowmen are known up and down the realm, and hired twice over when they can be.",
				"It is known for keeping its word, which in a free company is rarer than steel.",
				"It is known to have sold its word more than once, but never, yet, in the middle of a battle.",
				"It is known for holding a line after better men have left it."
			};
			List<string> middle = new List<string> { Pick(names), Pick(known) };
			if (MBRandom.RandomInt(2) == 0)
			{
				middle.Reverse();
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(founding).Append(" ");
			sb.Append(middle[0]).Append(" ").Append(middle[1]).Append(" ");
			sb.Append("Its words are \"").Append(words).Append("\".");
			sb.Append("\n\n").Append("It holds no land, no keep and no village; its seat is wherever its company makes camp. It rides for ").Append(realm)
			  .Append(" as a free company, paid as sellswords are, and may take its swords elsewhere whenever it chooses.");
			return sb.ToString();
		}

		private static string Pronoun(Hero h)
		{
			return (h != null && h.IsFemale) ? "she" : "he";
		}

		private static string Object(Hero h)
		{
			return (h != null && h.IsFemale) ? "her" : "him";
		}

		private static string Own(Hero h)
		{
			return (h != null && h.IsFemale) ? "her own" : "his own";
		}

		// A new line of the house's history on its page.
		internal static void Append(Clan house, string line)
		{
			try
			{
				string before = (house.EncyclopediaText != null) ? house.EncyclopediaText.ToString() : "";
				Bastard.Set(house, "EncyclopediaText", new TextObject("{=!}" + before + ((before.Length > 0) ? "\n\n" : "") + line, (Dictionary<string, object>)null));
			}
			catch
			{
			}
		}

		// Houses knighted before they had pages get one now.
		internal static void Repair()
		{
			try
			{
				if (!Cfg.Knights || !Store.Initialized)
				{
					return;
				}
				int n = 0;
				foreach (Clan c in Houses())
				{
					if (c.EncyclopediaText != null && c.EncyclopediaText.ToString().Length > 0)
					{
						continue;
					}
					string[] rec = (Store.Get(Prefix + ((MBObjectBase)c).StringId) ?? "").Split('|');
					Hero h = (rec.Length > 0 && rec[0].Length > 0) ? Law.Find(rec[0]) : c.Leader;
					string origin = (rec.Length > 2) ? rec[2] : "companion";
					string kind = origin.StartsWith("soldier") ? "soldier" : ((origin == "wanderer") ? "wanderer" : "companion");
					CharacterObject troop = origin.StartsWith("soldier:") ? MBObjectManager.Instance.GetObject<CharacterObject>(origin.Substring(8)) : null;
					string words = (rec.Length > 3 && rec[3].Length > 0) ? rec[3] : Pick(Words);
					Bastard.Set(c, "EncyclopediaText", new TextObject("{=!}" + HouseStory(c, h ?? c.Leader, kind, troop, words), (Dictionary<string, object>)null));
					n++;
				}
				if (n > 0)
				{
					Log.Write("knights' houses given their pages on load: " + n);
				}
			}
			catch (Exception e)
			{
				Log.Write("repairing the knights' houses failed: " + e.Message);
			}
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
			Append(c, "On " + Standing.Date() + " it rode out of " + ((Clan.PlayerClan.Kingdom != null) ? Clan.PlayerClan.Kingdom.Name.ToString() : "the crown") + "'s service - " + why + ".");
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
					Append(c, "On " + Standing.Date() + " it took service with " + ((Clan.PlayerClan.Kingdom != null) ? Clan.PlayerClan.Kingdom.Name.ToString() : "the crown") + " again.");
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
