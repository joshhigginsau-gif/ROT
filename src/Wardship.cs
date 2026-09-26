using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Phase 6. Hostages and wards.
	//
	// They are the same arrangement. A child of another house lives at your
	// court and does not go home. What differs is the word you use for it,
	// and the word is the whole of it: a ward is fostered and taught and his
	// house thanks you, a hostage is surety and his house does not.
	//
	// The child does not know the difference. Everyone else does.
	internal sealed class Held
	{
		internal string Id = "";

		internal string Hero = "";

		internal string House = "";

		internal bool Hostage;

		internal int Taken;

		// Set when the house they came from breaks faith with you. It is the
		// difference between an execution and a murder.
		internal bool Forfeit;

		internal string Pack()
		{
			return string.Join("|", new string[5] { Hero, House, Hostage ? "1" : "0", Taken.ToString(), Forfeit ? "1" : "0" });
		}

		internal static Held Unpack(string id, string s)
		{
			string[] p = (s ?? "").Split('|');
			if (p.Length < 5)
			{
				return null;
			}
			int n;
			Held h = new Held();
			h.Id = id;
			h.Hero = p[0];
			h.House = p[1];
			h.Hostage = p[2] == "1";
			h.Taken = int.TryParse(p[3], out n) ? n : 0;
			h.Forfeit = p[4] == "1";
			return h;
		}
	}

	internal static class Wardship
	{
		internal static List<Held> All()
		{
			List<Held> list = new List<Held>();
			foreach (string key in Store.Keys("wd:"))
			{
				Held h = Held.Unpack(key.Substring(3), Store.Get(key));
				if (h != null)
				{
					list.Add(h);
				}
			}
			return list.OrderBy((Held h) => h.Id).ToList();
		}

		internal static void Save(Held h)
		{
			Store.Set("wd:" + h.Id, h.Pack());
		}

		internal static void Drop(Held h)
		{
			Store.Set("wd:" + h.Id, null);
		}

		internal static Hero HeroOf(Held h)
		{
			try
			{
				return Hero.AllAliveHeroes.FirstOrDefault((Hero x) => ((MBObjectBase)x).StringId == h.Hero);
			}
			catch
			{
				return null;
			}
		}

		internal static Clan HouseOf(Held h)
		{
			try
			{
				return Clan.All.FirstOrDefault((Clan c) => ((MBObjectBase)c).StringId == h.House);
			}
			catch
			{
				return null;
			}
		}

		// Is this house's kin at your court?
		internal static Held From(Clan c)
		{
			if (c == null)
			{
				return null;
			}
			string id = ((MBObjectBase)c).StringId;
			return All().FirstOrDefault((Held h) => h.House == id);
		}

		internal static bool HoldsHostage(Clan c)
		{
			Held h = From(c);
			return h != null && h.Hostage;
		}

		// ------------------------------------------------------------------
		// taking one

		// Houses you could take from: those sworn to you, and those you have
		// beaten. Not your own, and not one you already hold kin from.
		internal static List<Clan> Candidates()
		{
			List<Clan> list = new List<Clan>();
			try
			{
				Kingdom k = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
				foreach (Clan c in Clan.All)
				{
					if (c == null || c == Clan.PlayerClan || c.IsEliminated || c.Leader == null || !c.Leader.IsAlive)
					{
						continue;
					}
					if (From(c) != null)
					{
						continue;
					}
					bool sworn = Oaths.Of(c) != OathKind.None;
					bool mine = k != null && c.Kingdom == k;
					if (sworn || mine)
					{
						list.Add(c);
					}
				}
			}
			catch
			{
			}
			return list;
		}

		// Who could be given up: the young of that house, and failing that any
		// member who is not its head.
		internal static List<Hero> Offerable(Clan c)
		{
			List<Hero> list = new List<Hero>();
			try
			{
				foreach (Hero h in c.Heroes)
				{
					if (h == null || !h.IsAlive || h == c.Leader || h.IsPrisoner)
					{
						continue;
					}
					list.Add(h);
				}
				list = list.OrderBy((Hero h) => h.Age).ToList();
			}
			catch
			{
			}
			return list;
		}

		internal static void Take(Hero child, Clan house, bool hostage)
		{
			try
			{
				if (child == null || house == null || !Cfg.Wardship)
				{
					// Cfg.Wardship used to gate only the weekly tick, so with
					// the feature turned off the player could still take
					// hostages and pay the Honour and relation costs - while
					// nothing was ever taught, no house ever drifted, and dead
					// children were never swept from the list, because the
					// weekly tick was the only cleanup path there is.
					return;
				}
				Held h = new Held();
				h.Id = "w" + Store.GetI("wd:next", 1).ToString("000");
				Store.SetI("wd:next", Store.GetI("wd:next", 1) + 1);
				h.Hero = ((MBObjectBase)child).StringId;
				h.House = ((MBObjectBase)house).StringId;
				h.Hostage = hostage;
				h.Taken = CourtBehavior.Today();
				Save(h);
				if (hostage)
				{
					Standing.Change(-Cfg.HostageHonour, Cfg.HostageDread, "a hostage taken");
					if (house.Leader != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, -Cfg.HostageRelation, false);
					}
					Store.AddDeed(Standing.Date() + "  " + child.Name + " was taken from " + house.Name + " as surety.");
					Popup("A Hostage Taken",
						child.Name + " has been brought to your court, and will not be going home.\n\n" + house.Name +
						" understands the arrangement perfectly. They will keep faith while you hold their blood, and they will not forgive you for it.");
				}
				else
				{
					Standing.Change(Cfg.WardHonour, 0, "a ward taken into your household");
					if (house.Leader != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, Cfg.WardRelation, false);
					}
					Store.AddDeed(Standing.Date() + "  " + child.Name + " of " + house.Name + " came to your court as a ward.");
					Popup("A Ward Taken",
						child.Name + " of " + house.Name + " has come to be raised at your court.\n\nThey will be taught, and fed, and watched. Their house calls it an honour. It is also surety, and everyone knows that too.");
				}
			}
			catch (Exception e)
			{
				Log.Write("taking a ward failed: " + e);
			}
		}

		// ------------------------------------------------------------------
		// keeping them

		internal static void Weekly()
		{
			try
			{
				if (!Cfg.Wardship || !Store.Initialized)
				{
					return;
				}
				foreach (Held h in All())
				{
					Hero child = HeroOf(h);
					Clan house = HouseOf(h);
					if (child == null || !child.IsAlive || house == null || house.IsEliminated)
					{
						Drop(h);
						continue;
					}
					if (h.Hostage)
					{
						// Surety weighs on a house, and the weight tells.
						if (house.Leader != null && MBRandom.RandomFloat < 0.2f)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, -1, false);
						}
					}
					else
					{
						if (house.Leader != null && MBRandom.RandomFloat < 0.35f)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, 1, false);
						}
						Teach(child);
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("wardweek", "wards failed: " + e.Message);
			}
		}

		private static void Teach(Hero child)
		{
			try
			{
				if (child.HeroDeveloper == null)
				{
					return;
				}
				// The Grand Maester's teaching duty used to triple this. It
				// went with the council; a ward is now taught at one rate.
				int xp = Cfg.WardXp;
				SkillObject[] taughtSkills = new SkillObject[4]
				{
					DefaultSkills.OneHanded, DefaultSkills.Riding, DefaultSkills.Leadership, DefaultSkills.Charm
				};
				SkillObject s = taughtSkills[MBRandom.RandomInt(taughtSkills.Length)];
				child.HeroDeveloper.AddSkillXp(s, xp);
			}
			catch
			{
			}
		}

		// Called when a house does something you cannot forgive. After this,
		// the axe costs you nothing that the realm will hold against you.
		internal static void BreaksFaith(Clan house, string what)
		{
			try
			{
				Held h = From(house);
				if (h == null || h.Forfeit)
				{
					return;
				}
				h.Forfeit = true;
				Save(h);
				Hero child = HeroOf(h);
				if (child == null)
				{
					return;
				}
				Store.AddDeed(Standing.Date() + "  " + house.Name + " broke faith while you held " + child.Name + ".");
				Popup("Faith Broken",
					house.Name + " has " + what + ", and you are holding " + child.Name + ".\n\n" +
					"Every lord in the realm knows what is owed now. What you do about it is still yours to decide, but no one will call it murder.");
			}
			catch
			{
			}
		}

		// ------------------------------------------------------------------
		// letting them go, one way or the other

		internal static void Release(Held h)
		{
			try
			{
				Hero child = HeroOf(h);
				Clan house = HouseOf(h);
				Drop(h);
				if (child == null || house == null)
				{
					return;
				}
				Standing.Change(Cfg.WardReleaseHonour, -Cfg.WardReleaseDread, "kin sent home");
				if (house.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, Cfg.WardReleaseRelation, false);
				}
				Store.AddDeed(Standing.Date() + "  " + child.Name + " was sent home to " + house.Name + ".");
				Popup("Sent Home",
					child.Name + " has gone back to " + house.Name + ".\n\n" +
					(h.Hostage
						? "You had every reason to keep them and you did not. That will be remembered longer than the taking was."
						: "They were taught well, and they leave thinking well of you."));
			}
			catch
			{
			}
		}

		internal static void Execute(Held h)
		{
			try
			{
				Hero child = HeroOf(h);
				Clan house = HouseOf(h);
				bool owed = h.Forfeit;
				Drop(h);
				if (child == null)
				{
					return;
				}
				if (owed)
				{
					Standing.Change(0, Cfg.ExecuteForfeitDread, "a forfeit paid");
					Store.AddDeed(Standing.Date() + "  " + child.Name + " was put to death after " + ((house != null) ? house.Name.ToString() : "their house") + " broke faith.");
					Popup("The Forfeit",
						child.Name + " was taken out in the morning and it was done quickly.\n\n" +
						"The realm had been told why. Nobody spoke against it, and nobody will forget it either.");
				}
				else
				{
					Standing.Change(-Cfg.WardExecuteHonour, Cfg.WardExecuteDread, "a killing without cause");
					Store.AddDeed(Standing.Date() + "  " + child.Name + " was put to death. There was no cause.");
					Popup("Without Cause",
						child.Name + " is dead, and " + ((house != null) ? house.Name.ToString() : "their house") + " had done nothing.\n\n" +
						"Word of this will be at every hearth in the realm by the end of the month, and it will not be told kindly.");
				}
				if (house != null && house.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, owed ? -40 : -80, false);
				}
				KillCharacterAction.ApplyByExecution(child, Hero.MainHero, true);
			}
			catch (Exception e)
			{
				Log.Write("the execution failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------

		internal static string Summary()
		{
			List<Held> all = All();
			if (all.Count == 0)
			{
				return "No one else's children live at your court.";
			}
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			foreach (Held h in all)
			{
				Hero child = HeroOf(h);
				Clan house = HouseOf(h);
				sb.Append("  ").Append((child != null) ? child.Name.ToString() : "?")
				  .Append(" of ").Append((house != null) ? house.Name.ToString() : "?")
				  .Append(h.Hostage ? " - hostage" : " - ward")
				  .Append(", ").Append((CourtBehavior.Today() - h.Taken) / Math.Max(1, Cfg.DaysPerYear)).Append(" year(s)");
				if (h.Forfeit)
				{
					sb.Append("  [their house broke faith]");
				}
				sb.Append("\n");
			}
			return sb.ToString();
		}

		private static void Popup(string title, string text)
		{
			try
			{
				InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", null, null, null), true, false);
			}
			catch
			{
				try
				{
					InformationManager.DisplayMessage(new InformationMessage(text));
				}
				catch
				{
				}
			}
			Log.Write(title + ": " + text.Replace("\n", " "));
		}
	}
}
