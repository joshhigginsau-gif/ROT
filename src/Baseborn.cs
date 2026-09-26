using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// One child, got outside your marriage and raised outside your house.
	internal sealed class Kid
	{
		internal string Id = "";

		// The child. Empty until they surface, which is three years after.
		internal string Hero = "";

		// The other parent. A real person, in a real town, who remembers you.
		internal string Other = "";

		// Where it happened, which decides the surname they carry.
		internal string Where = "";

		// The day of the night itself.
		internal int Night;

		// Whose night it was. Three years is long enough for a ruler to die in
		// this mod, and without this the child would be fathered on whoever
		// happened to be the player on the day word arrived - so a successor
		// would be written in as the parent of their predecessor's child.
		internal string Parent = "";

		// Have they come to your gate yet?
		internal bool Known;

		// Have you given them your name?
		internal bool Legit;

		internal string Pack()
		{
			return string.Join("|", new string[7]
			{
				Hero, Other, Where, Night.ToString(), Known ? "1" : "0", Legit ? "1" : "0", Parent
			});
		}

		internal static Kid Unpack(string id, string s)
		{
			string[] p = (s ?? "").Split('|');
			if (p.Length < 6)
			{
				return null;
			}
			int n;
			Kid k = new Kid();
			k.Id = id;
			k.Hero = p[0];
			k.Other = p[1];
			k.Where = p[2];
			k.Night = int.TryParse(p[3], out n) ? n : 0;
			k.Known = p[4] == "1";
			k.Legit = p[5] == "1";
			// Tolerated missing, so a record written before this field existed
			// still loads instead of being thrown away.
			k.Parent = (p.Length > 6) ? p[6] : "";
			return k;
		}
	}

	// Children of yours who are not of your house.
	//
	// The Bastard's Banner used to invent its claimant at the graveside, which
	// left the mod explaining how a stranger came to be holding your ancestral
	// sword. This is the answer: the children exist first, you know about
	// them, and whatever they have at the end you gave them yourself.
	//
	// A note on how they are made, because it is not an implementation detail.
	// They are created as heroes outright, never through a real pregnancy.
	// RoT Dynasty & Succession hooks the game's birth event and files every
	// newborn as bastard-or-trueborn on the spot, from whether the father is
	// the mother's husband - and that verdict is permanent and outranks
	// everything else it knows. A child born that way could be legitimised by
	// us and RoT's encyclopedia would still call them a bastard for the rest
	// of the campaign. Creating them outside that event leaves RoT reading
	// their SURNAME instead, which is a thing we control: name them Snow and
	// RoT agrees they are baseborn, rename them later and RoT agrees they were
	// legitimised. The two mods never contradict each other on screen.
	internal static class Baseborn
	{
		private const string Prefix = "bb:";

		internal static List<Kid> All()
		{
			List<Kid> list = new List<Kid>();
			try
			{
				foreach (string key in Store.Keys(Prefix))
				{
					Kid k = Kid.Unpack(key.Substring(Prefix.Length), Store.Get(key));
					if (k != null)
					{
						list.Add(k);
					}
				}
			}
			catch
			{
			}
			return list.OrderBy((Kid k) => k.Night).ToList();
		}

		internal static void Save(Kid k)
		{
			Store.Set(Prefix + k.Id, k.Pack());
		}

		internal static void Drop(Kid k)
		{
			Store.Set(Prefix + k.Id, null);
		}

		internal static Hero HeroOf(Kid k)
		{
			return Find(k != null ? k.Hero : null);
		}

		internal static Hero OtherOf(Kid k)
		{
			return Find(k != null ? k.Other : null);
		}

		// The parent who spent the night, living or dead.
		internal static Hero Parent(Kid k)
		{
			try
			{
				string id = (k != null) ? k.Parent : null;
				if (string.IsNullOrEmpty(id))
				{
					return null;
				}
				return Hero.AllAliveHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id)
					?? Hero.DeadOrDisabledHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
			}
			catch
			{
				return null;
			}
		}

		private static Hero Find(string id)
		{
			try
			{
				return string.IsNullOrEmpty(id)
					? null
					: Hero.AllAliveHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
			}
			catch
			{
				return null;
			}
		}

		private static Settlement Place(string id)
		{
			try
			{
				return string.IsNullOrEmpty(id)
					? null
					: Settlement.All.FirstOrDefault((Settlement s) => ((MBObjectBase)s).StringId == id);
			}
			catch
			{
				return null;
			}
		}

		// The ones who have come to your gate and are still alive.
		internal static List<Kid> Known()
		{
			return All().Where((Kid k) => k.Known && HeroOf(k) != null).ToList();
		}

		// ------------------------------------------------------------------
		// the night

		internal static bool CanSpendNight(out string why)
		{
			why = null;
			try
			{
				if (!Cfg.Baseborn)
				{
					why = "turned off in the config";
					return false;
				}
				Hero you = Hero.MainHero;
				Settlement here = Settlement.CurrentSettlement;
				if (you == null || here == null || (!here.IsTown && !here.IsVillage))
				{
					why = "there is nowhere here to take a room";
					return false;
				}
				if (you.IsChild || you.Age < Cfg.BaseNightMinAge)
				{
					why = "you are too young";
					return false;
				}
				if (!Cfg.BaseWhileMarried && you.Spouse != null)
				{
					why = "you are married, and your config does not allow it";
					return false;
				}
				if (All().Count >= Cfg.BaseMax)
				{
					why = "you have as many children out there as you can account for";
					return false;
				}
				int last = Store.GetI("bb:last", -9999);
				int wait = Cfg.BaseCooldown - (CourtBehavior.Today() - last);
				if (last > -9000 && wait > 0)
				{
					why = "not so soon. " + wait + " more days";
					return false;
				}
				if (you.Gold < Cfg.BaseNightCost)
				{
					why = "you cannot pay for the room and the silence";
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				why = "this cannot be read just now";
				Log.Once("nightcheck", "the night check failed: " + e.Message);
				return false;
			}
		}

		// Nothing happens tonight. Something may happen in three years.
		internal static void SpendNight()
		{
			try
			{
				string why;
				if (!CanSpendNight(out why))
				{
					Flow.Notify("Not tonight: " + why + ".");
					return;
				}
				Hero you = Hero.MainHero;
				Settlement here = Settlement.CurrentSettlement;

				Kid k = Conceive(null, here);
				Hero other = OtherOf(k);
				if (k == null || other == null)
				{
					Flow.Notify("The night passed quietly and alone.");
					return;
				}

				you.ChangeHeroGold(-Cfg.BaseNightCost);
				Store.SetI("bb:last", CourtBehavior.Today());

				// And the whisper, if you are married and unlucky.
				Whisper(you, here);

				Popup("A Room Above the Common Hall",
					"You took a room, and you were not in it alone.\n\n" +
					"In the morning " + other.Name + " was gone about " + (other.IsFemale ? "her" : "his") +
					" business and so were you, and neither of you said anything worth writing down.\n\n" +
					"Nothing has happened. Nothing is going to happen for a long while.");
			}
			catch (Exception e)
			{
				Log.Write("the night failed: " + e.Message);
			}
		}

		// Write the night down. Nothing else: no cost, no cooldown, no whisper -
		// the callers own those, because a room above the common hall and a
		// night after a tourney feast are paid for in different coin.
		//
		// With no other parent given, one is invented at the place. With one
		// given - the lady you crowned in the lists - it is her, and she is the
		// one who will come to your gate.
		internal static Kid Conceive(Hero other, Settlement here)
		{
			try
			{
				Hero you = Hero.MainHero;
				if (you == null || here == null)
				{
					return null;
				}
				if (other == null)
				{
					other = Beget(here, you);
				}
				if (other == null)
				{
					return null;
				}
				Kid k = new Kid();
				k.Id = Next();
				k.Other = ((MBObjectBase)other).StringId;
				k.Where = ((MBObjectBase)here).StringId;
				k.Night = CourtBehavior.Today();
				k.Parent = ((MBObjectBase)you).StringId;
				Save(k);
				Log.Write("a night at " + here.Name + " with " + other.Name + " (record " + k.Id + ")");
				return k;
			}
			catch (Exception e)
			{
				Log.Write("the night could not be written down: " + e.Message);
				return null;
			}
		}

		// Is there room for one more? Only the cap - the cheats and the tourney
		// feast skip the cost and the cooldown, but not the number of children
		// the mod will keep track of.
		internal static bool HasRoom()
		{
			return Cfg.Baseborn && All().Count < Cfg.BaseMax;
		}

		// Every child still on the road arrives today. For testing: the three
		// years are the point in play, and the whole problem when testing.
		internal static int HurryAll()
		{
			int n = 0;
			try
			{
				foreach (Kid k in All())
				{
					if (k.Known)
					{
						continue;
					}
					Arrive(k);
					Kid now = All().FirstOrDefault((Kid x) => x.Id == k.Id);
					if (now != null && now.Known)
					{
						n++;
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("hurrying the children failed: " + e.Message);
			}
			return n;
		}

		// The other parent. Invented, but real from here on: she lives in the
		// town you met her in and she is the one who will bring the child.
		private static Hero Beget(Settlement here, Hero you)
		{
			try
			{
				CharacterObject template = Template(here, !you.IsFemale);
				if (template == null)
				{
					Log.Write("no one at " + here.Name + " to have met");
					return null;
				}
				int age = MBRandom.RandomInt(Cfg.BaseOtherMinAge, Cfg.BaseOtherMaxAge + 1);
				Hero other = HeroCreator.CreateSpecialHero(template, here, null, null, age);
				if (other == null)
				{
					return null;
				}
				try
				{
					other.ChangeState(Hero.CharacterStates.Active);
					// The occupation is copied from the template, so set it
					// outright rather than trusting what we drew.
					other.SetNewOccupation(Occupation.Lord);
					EnterSettlementAction.ApplyForCharacterOnly(other, here);
					other.IsKnownToPlayer = true;
				}
				catch (Exception se)
				{
					Log.Once("begetplace", "they could not be placed: " + se.Message);
				}
				return other;
			}
			catch (Exception e)
			{
				Log.Write("inventing the other parent failed: " + e.Message);
				return null;
			}
		}

		// Somebody ordinary of this place, and of the opposite sex to you.
		private static CharacterObject Template(Settlement here, bool wantFemale)
		{
			try
			{
				// Lord templates only.
				//
				// A wanderer template makes a hero the game considers a tavern
				// companion, and vanilla quietly culls those: its companion
				// behaviour rolls daily, picks a wanderer with a registered
				// template and no employer, and removes them outright. Over a
				// long campaign that would have deleted almost every child and
				// every mother this mod created, and the daily sweep here
				// would have read the disappearance as a death and destroyed
				// the record with them. Wanderers also show up as hireable in
				// taverns - you could recruit your own child as a companion -
				// and are explicitly skipped by the game's own heir screen, so
				// an acknowledged child could never have been offered as heir.
				//
				// They do still wander. The game moves any party-less adult
				// around the map daily and a town holding a tournament pulls
				// hard, so the other parent will not necessarily stay where
				// you met them. That is cosmetic: the record stores ids, and
				// the child's surname comes from where the night happened
				// rather than from wherever anyone has drifted to since.
				CultureObject culture = (here != null) ? here.Culture : null;
				List<CharacterObject> all = CharacterObject.All.Where((CharacterObject c) =>
					c != null && c.IsHero && c.Culture == culture && c.IsFemale == wantFemale
					&& c.Occupation == Occupation.Lord).ToList();
				if (all.Count == 0)
				{
					all = CharacterObject.All.Where((CharacterObject c) =>
						c != null && c.IsHero && c.IsFemale == wantFemale && c.Occupation == Occupation.Lord).ToList();
				}
				return (all.Count == 0) ? null : all[MBRandom.RandomInt(all.Count)];
			}
			catch
			{
				return null;
			}
		}

		// Word gets around, sometimes.
		private static void Whisper(Hero you, Settlement here)
		{
			try
			{
				Hero spouse = (you != null) ? you.Spouse : null;
				if (spouse == null || MBRandom.RandomInt(100) >= Cfg.BaseWhisperChance)
				{
					return;
				}
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, spouse, -Cfg.BaseWhisperRelation, false);
				Standing.Change(-Cfg.BaseWhisperHonour, 0, "a night talked about at court");
				Store.AddDeed(Standing.Date() + "  There was talk about a night at " + ((here != null) ? here.Name.ToString() : "a town") + ".");
				Flow.Notify(spouse.Name + " has heard where you spent the night.");
			}
			catch
			{
			}
		}

		// ------------------------------------------------------------------
		// three years later

		// Checked once a day. Nothing is born here - the child is simply made,
		// already grown enough to matter, on the day word reaches you.
		internal static void Daily()
		{
			try
			{
				if (!Cfg.Baseborn || !Store.Initialized)
				{
					return;
				}
				int today = CourtBehavior.Today();
				foreach (Kid k in All())
				{
					if (k.Known)
					{
						// Sweep the dead: a child who dies out there is simply
						// gone, and the record should not outlive them.
						if (!string.IsNullOrEmpty(k.Hero) && HeroOf(k) == null)
						{
							Log.Write("the child of record " + k.Id + " is dead");
							Drop(k);
						}
						continue;
					}
					if (today - k.Night < Cfg.BaseYearsUntil * Math.Max(1, Cfg.DaysPerYear))
					{
						continue;
					}
					Arrive(k);
				}
			}
			catch (Exception e)
			{
				Log.Once("bbdaily", "the baseborn tick failed: " + e.Message);
			}
		}

		private static void Arrive(Kid k)
		{
			try
			{
				Settlement where = Place(k.Where);
				Hero other = OtherOf(k);
				// The parent named on the record, alive or dead. A child got
				// by a ruler who has since died is still that ruler's child,
				// and the reckoning at the next funeral has to say so.
				Hero you = Parent(k) ?? Hero.MainHero;
				if (you == null || where == null)
				{
					Drop(k);
					return;
				}

				// A daughter as readily as a son. This used to pass null for
				// the player, which made the gender test always false and so
				// every child in every campaign was a boy.
				bool girl = MBRandom.RandomInt(100) < 50;
				CharacterObject template = Template(where, girl);
				if (template == null)
				{
					Drop(k);
					return;
				}
				int age = MBRandom.RandomInt(Cfg.BaseChildMinAge, Cfg.BaseChildMaxAge + 1);
				Hero child = HeroCreator.CreateSpecialHero(template, where, null, null, age);
				if (child == null)
				{
					Drop(k);
					return;
				}

				// Awake, and somewhere. Without this they are NotSpawned and
				// the game will never give them a party or let them lead.
				try
				{
					child.ChangeState(Hero.CharacterStates.Active);
					child.SetNewOccupation(Occupation.Lord);
					EnterSettlementAction.ApplyForCharacterOnly(child, where);
					child.IsKnownToPlayer = true;
				}
				catch (Exception se)
				{
					Log.Once("kidplace", "the child could not be placed: " + se.Message);
				}

				// Your blood, and hers.
				try
				{
					if (you.IsFemale)
					{
						child.Mother = you;
						if (other != null)
						{
							child.Father = other;
						}
					}
					else
					{
						child.Father = you;
						if (other != null)
						{
							child.Mother = other;
						}
					}
				}
				catch (Exception pe)
				{
					Log.Once("kidparent", "the parentage would not take: " + pe.Message);
				}

				// And the surname of the place they were got. This is what
				// RoT reads to know they are baseborn - no registration, no
				// API, just the name everyone in Westeros would have given
				// them anyway.
				string given = (child.FirstName != null) ? child.FirstName.ToString() : "The Child";
				string surname = Surnames.Of(where);
				try
				{
					child.SetName(new TextObject("{=!}" + given + " " + surname, (Dictionary<string, object>)null),
						new TextObject("{=!}" + given, (Dictionary<string, object>)null));
				}
				catch
				{
				}

				k.Hero = ((MBObjectBase)child).StringId;
				k.Known = true;
				Save(k);

				Store.AddDeed(Standing.Date() + "  " + child.Name + " was brought to your gate.");
				Log.Write("a child surfaces: " + child.Name + ", " + (int)child.Age + ", of " + where.Name);

				Popup("A Child at the Gate",
					((other != null) ? other.Name.ToString() : "Someone") + " came to the gate from " + where.Name +
					", and " + ((other != null && !other.IsFemale) ? "he" : "she") + " did not come alone.\n\n" +
					"The " + ((child.IsFemale) ? "girl" : "boy") + " is " + (int)child.Age + ", and carries the name " + surname +
					", as everyone born the way " + ((child.IsFemale) ? "she" : "he") + " was carries it.\n\n" +
					"You are under no obligation. " + child.Name + " is not of your house and cannot inherit, and nobody expects you to pretend otherwise.\n\n" +
					"They are yours, though. Everyone who looks at them can see it.");
			}
			catch (Exception e)
			{
				Log.Write("the child could not be brought: " + e.Message);
				Drop(k);
			}
		}

		// ------------------------------------------------------------------
		// giving them your name

		internal static bool CanLegitimise(Kid k, out string why)
		{
			why = null;
			try
			{
				Hero child = HeroOf(k);
				if (k == null || child == null)
				{
					why = "there is nobody to acknowledge";
					return false;
				}
				if (k.Legit)
				{
					why = "already acknowledged";
					return false;
				}
				if (!Succession.Rules())
				{
					why = "only a ruling house can write a name into its book";
					return false;
				}
				if (child.IsChild)
				{
					why = "they are too young for it to mean anything yet";
					return false;
				}
				if (Bastard.Risen)
				{
					why = "that is long past";
					return false;
				}
				// And never somebody who belongs to another house, least of
				// all one they lead. Setting Hero.Clan would pull them out of
				// it while that clan still called them its head - and if it
				// were a kingdom's ruling clan, the crown would come with them.
				if (child.Clan != null && child.Clan != Clan.PlayerClan)
				{
					why = (child.Clan.Leader == child)
						? ("they lead " + child.Clan.Name + " now")
						: ("they belong to " + child.Clan.Name + " now");
					return false;
				}
				return true;
			}
			catch
			{
				why = "this cannot be read just now";
				return false;
			}
		}

		// Into the house, and out of the surname.
		//
		// Renaming them off the bastard surname is not cosmetic: it is what
		// makes RoT report them as Legitimized rather than Bastard, because
		// RoT remembers they once read as baseborn and derives the rest.
		internal static void Legitimise(Kid k)
		{
			try
			{
				string why;
				if (!CanLegitimise(k, out why))
				{
					Flow.Notify("Not possible: " + why + ".");
					return;
				}
				Hero child = HeroOf(k);
				Clan mine = Clan.PlayerClan;
				string house = (mine != null && mine.Name != null) ? mine.Name.ToString() : "your house";
				if (house.StartsWith("House ", StringComparison.OrdinalIgnoreCase))
				{
					house = house.Substring(6).Trim();
				}

				// RoT reads legitimacy off the surname, so the new one must
				// not itself be one of the nine. A house literally called
				// "House Rivers" would otherwise rename Jon Rivers to Jon
				// Rivers, and RoT would go on calling him baseborn while our
				// own screen said acknowledged.
				if (Surnames.IsBaseborn(house))
				{
					string seat = (mine != null && mine.HomeSettlement != null) ? mine.HomeSettlement.Name.ToString() : null;
					house = string.IsNullOrEmpty(seat) ? (house + " of the Book") : (house + " of " + seat);
					Log.Write("your house name reads as baseborn, so the acknowledged name is '" + house + "'");
				}

				// Make RoT look at them BEFORE the rename.
				//
				// It only records "this one was once baseborn" as a side
				// effect of being asked, and it only reports Legitimized for
				// somebody it remembers asking about. Rename them without
				// ever having asked and RoT simply calls them trueborn, which
				// loses the whole story.
				Observe(child);

				string given = (child.FirstName != null) ? child.FirstName.ToString() : child.Name.ToString();
				try
				{
					child.SetName(new TextObject("{=!}" + given + " " + house, (Dictionary<string, object>)null),
						new TextObject("{=!}" + given, (Dictionary<string, object>)null));
				}
				catch (Exception ne)
				{
					Log.Write("the new name would not take: " + ne.Message);
				}
				try
				{
					child.Clan = mine;
				}
				catch (Exception ce)
				{
					Log.Write("they could not be brought into the house: " + ce.Message);
				}

				// What it costs. Your trueborn children did not ask for a new
				// sibling with a claim, and neither did the person you married.
				int hurt = 0;
				try
				{
					Hero you = Hero.MainHero;
					List<Hero> kin = new List<Hero>();
					if (you != null)
					{
						if (you.Spouse != null)
						{
							kin.Add(you.Spouse);
						}
						foreach (Hero h in Succession.Claimants())
						{
							if (h != child && !kin.Contains(h))
							{
								kin.Add(h);
							}
						}
						foreach (Hero h in kin)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, h, -Cfg.LegitKinRelation, false);
							hurt++;
						}
					}
				}
				catch
				{
				}
				Standing.Change(-Cfg.LegitStanding, 0, "a baseborn child written into the book");

				k.Legit = true;
				Save(k);
				Store.AddDeed(Standing.Date() + "  " + child.Name + " was acknowledged and given your name.");
				Log.Write("legitimised: " + child.Name + " (" + hurt + " kin took it badly)");

				Popup("Your Name",
					child.Name + " kneels baseborn and stands with your name on them.\n\n" +
					"It is a generous thing and the court will say so out loud. " +
					((hurt > 0) ? (hurt + " of your own took it rather differently, and said so in private.\n\n") : "\n") +
					"From today there is one more person in the realm who can be argued to have a right to your seat. The lords have already worked out which.");
			}
			catch (Exception e)
			{
				Log.Write("acknowledging the child failed: " + e.Message);
			}
		}

		// Ask RoT for this hero's legitimacy and throw the answer away.
		//
		// The asking is the point: RoT stamps "was once baseborn" onto anyone
		// it is asked about while they still read as baseborn, and that stamp
		// is what later lets it report them as Legitimized rather than simply
		// trueborn. If the player never happened to open the encyclopedia page
		// before acknowledging them, nobody would ever have asked.
		private static void Observe(Hero child)
		{
			try
			{
				Type t = HarmonyLib.AccessTools.TypeByName("RoTDynastyAndSuccession.Legitimacy.LegitimacyTracker");
				object inst = (t == null) ? null : HarmonyLib.AccessTools.Property(t, "Instance")?.GetValue(null, null);
				System.Reflection.MethodInfo m = (inst == null)
					? null
					: HarmonyLib.AccessTools.Method(t, "GetStatus", (Type[])null, (Type[])null);
				if (m == null)
				{
					return;
				}
				object status = m.Invoke(inst, new object[1] { child });
				Log.Write("RoT has taken note of " + child.Name + " (" + status + ")");
			}
			catch (Exception e)
			{
				Log.Once("observe", "RoT would not look at the child: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// bits

		private static string Next()
		{
			int n = Store.GetI("bb:next", 1);
			Store.SetI("bb:next", n + 1);
			return n.ToString();
		}

		private static void Popup(string title, string text)
		{
			try
			{
				InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", null, null, null), true, false);
			}
			catch
			{
			}
		}

		// What the court screen says about them.
		internal static string Summary()
		{
			try
			{
				List<Kid> known = Known();
				if (known.Count == 0)
				{
					List<Kid> waiting = All();
					return (waiting.Count > 0)
						? "  Nothing has come of it yet.\n"
						: null;
				}
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				foreach (Kid k in known)
				{
					Hero h = HeroOf(k);
					sb.Append("  ").Append(h.Name).Append("  -  ").Append((int)h.Age);
					sb.Append(k.Legit ? "   [acknowledged]" : "   [not of your house]");
					if (Blade.HolderOf() == h)
					{
						sb.Append("   [carries ").Append(Lore.Blade()).Append("]");
					}
					sb.Append("\n");
				}
				return sb.ToString();
			}
			catch
			{
				return null;
			}
		}
	}
}
