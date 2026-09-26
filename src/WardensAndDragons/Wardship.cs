using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Wardship
{
	internal static List<Held> All()
	{
		List<Held> list = new List<Held>();
		foreach (string item in Store.Keys("wd:"))
		{
			Held held = Held.Unpack(item.Substring(3), Store.Get(item));
			if (held != null)
			{
				list.Add(held);
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
			return ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero x) => ((MBObjectBase)x).StringId == h.Hero));
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
			return ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Func<Clan, bool>)((Clan c) => ((MBObjectBase)c).StringId == h.House));
		}
		catch
		{
			return null;
		}
	}

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
		return From(c)?.Hostage ?? false;
	}

	internal static List<Clan> Candidates()
	{
		List<Clan> list = new List<Clan>();
		try
		{
			Kingdom val = ((Clan.PlayerClan == null) ? null : Clan.PlayerClan.Kingdom);
			foreach (Clan item in (List<Clan>)(object)Clan.All)
			{
				if (item != null && item != Clan.PlayerClan && !item.IsEliminated && item.Leader != null && item.Leader.IsAlive && From(item) == null)
				{
					bool flag = Oaths.Of(item) != OathKind.None;
					bool flag2 = val != null && item.Kingdom == val;
					if (flag || flag2)
					{
						list.Add(item);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	internal static List<Hero> Offerable(Clan c)
	{
		List<Hero> list = new List<Hero>();
		try
		{
			foreach (Hero item in (List<Hero>)(object)c.Heroes)
			{
				if (item != null && item.IsAlive && item != c.Leader && !item.IsPrisoner)
				{
					list.Add(item);
				}
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
				return;
			}
			Held held = new Held();
			held.Id = "w" + Store.GetI("wd:next", 1).ToString("000");
			Store.SetI("wd:next", Store.GetI("wd:next", 1) + 1);
			held.Hero = ((MBObjectBase)child).StringId;
			held.House = ((MBObjectBase)house).StringId;
			held.Hostage = hostage;
			held.Taken = CourtBehavior.Today();
			Save(held);
			if (hostage)
			{
				Standing.Change(-Cfg.HostageHonour, Cfg.HostageDread, "a hostage taken");
				if (house.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, -Cfg.HostageRelation, false);
				}
				Store.AddDeed(string.Concat(Standing.Date(), "  ", child.Name, " was taken from ", house.Name, " as surety."));
				Popup("A Hostage Taken", string.Concat(child.Name, " has been brought to your court, and will not be going home.\n\n", house.Name, " understands the arrangement perfectly. They will keep faith while you hold their blood, and they will not forgive you for it."));
			}
			else
			{
				Standing.Change(Cfg.WardHonour, 0, "a ward taken into your household");
				if (house.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, house.Leader, Cfg.WardRelation, false);
				}
				Store.AddDeed(string.Concat(Standing.Date(), "  ", child.Name, " of ", house.Name, " came to your court as a ward."));
				Popup("A Ward Taken", string.Concat(child.Name, " of ", house.Name, " has come to be raised at your court.\n\nThey will be taught, and fed, and watched. Their house calls it an honour. It is also surety, and everyone knows that too."));
			}
		}
		catch (Exception ex)
		{
			Log.Write("taking a ward failed: " + ex);
		}
	}

	internal static void Weekly()
	{
		try
		{
			if (!Cfg.Wardship || !Store.Initialized)
			{
				return;
			}
			foreach (Held item in All())
			{
				Hero val = HeroOf(item);
				Clan val2 = HouseOf(item);
				if (val == null || !val.IsAlive || val2 == null || val2.IsEliminated)
				{
					Drop(item);
					continue;
				}
				if (item.Hostage)
				{
					if (val2.Leader != null && MBRandom.RandomFloat < 0.2f)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, val2.Leader, -1, false);
					}
					continue;
				}
				if (val2.Leader != null && MBRandom.RandomFloat < 0.35f)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, val2.Leader, 1, false);
				}
				Teach(val);
			}
		}
		catch (Exception ex)
		{
			Log.Once("wardweek", "wards failed: " + ex.Message);
		}
	}

	private static void Teach(Hero child)
	{
		try
		{
			if (child.HeroDeveloper != null)
			{
				int wardXp = Cfg.WardXp;
				SkillObject[] array = (SkillObject[])(object)new SkillObject[4]
				{
					DefaultSkills.OneHanded,
					DefaultSkills.Riding,
					DefaultSkills.Leadership,
					DefaultSkills.Charm
				};
				SkillObject val = array[MBRandom.RandomInt(array.Length)];
				child.HeroDeveloper.AddSkillXp(val, (float)wardXp, true, true);
			}
		}
		catch
		{
		}
	}

	internal static void BreaksFaith(Clan house, string what)
	{
		try
		{
			Held held = From(house);
			if (held != null && !held.Forfeit)
			{
				held.Forfeit = true;
				Save(held);
				Hero val = HeroOf(held);
				if (val != null)
				{
					Store.AddDeed(string.Concat(Standing.Date(), "  ", house.Name, " broke faith while you held ", val.Name, "."));
					Popup("Faith Broken", string.Concat(house.Name, " has ", what, ", and you are holding ", val.Name, ".\n\nEvery lord in the realm knows what is owed now. What you do about it is still yours to decide, but no one will call it murder."));
				}
			}
		}
		catch
		{
		}
	}

	internal static void Release(Held h)
	{
		try
		{
			Hero val = HeroOf(h);
			Clan val2 = HouseOf(h);
			Drop(h);
			if (val != null && val2 != null)
			{
				Standing.Change(Cfg.WardReleaseHonour, -Cfg.WardReleaseDread, "kin sent home");
				if (val2.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, val2.Leader, Cfg.WardReleaseRelation, false);
				}
				Store.AddDeed(string.Concat(Standing.Date(), "  ", val.Name, " was sent home to ", val2.Name, "."));
				Popup("Sent Home", string.Concat(val.Name, " has gone back to ", val2.Name, ".\n\n", (!h.Hostage) ? "They were taught well, and they leave thinking well of you." : "You had every reason to keep them and you did not. That will be remembered longer than the taking was."));
			}
		}
		catch
		{
		}
	}

	internal static void Execute(Held h)
	{
		try
		{
			Hero val = HeroOf(h);
			Clan val2 = HouseOf(h);
			bool forfeit = h.Forfeit;
			Drop(h);
			if (val != null)
			{
				if (forfeit)
				{
					Standing.Change(0, Cfg.ExecuteForfeitDread, "a forfeit paid");
					Store.AddDeed(string.Concat(Standing.Date(), "  ", val.Name, " was put to death after ", (val2 == null) ? "their house" : ((object)val2.Name).ToString(), " broke faith."));
					Popup("The Forfeit", string.Concat(val.Name, " was taken out in the morning and it was done quickly.\n\nThe realm had been told why. Nobody spoke against it, and nobody will forget it either."));
				}
				else
				{
					Standing.Change(-Cfg.WardExecuteHonour, Cfg.WardExecuteDread, "a killing without cause");
					Store.AddDeed(string.Concat(Standing.Date(), "  ", val.Name, " was put to death. There was no cause."));
					Popup("Without Cause", string.Concat(val.Name, " is dead, and ", (val2 == null) ? "their house" : ((object)val2.Name).ToString(), " had done nothing.\n\nWord of this will be at every hearth in the realm by the end of the month, and it will not be told kindly."));
				}
				if (val2 != null && val2.Leader != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, val2.Leader, (!forfeit) ? (-80) : (-40), false);
				}
				KillCharacterAction.ApplyByExecution(val, Hero.MainHero, true, false);
			}
		}
		catch (Exception ex)
		{
			Log.Write("the execution failed: " + ex.Message);
		}
	}

	internal static string Summary()
	{
		List<Held> list = All();
		if (list.Count == 0)
		{
			return "No one else's children live at your court.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (Held item in list)
		{
			Hero val = HeroOf(item);
			Clan val2 = HouseOf(item);
			stringBuilder.Append("  ").Append((val == null) ? "?" : ((object)val.Name).ToString()).Append(" of ")
				.Append((val2 == null) ? "?" : ((object)val2.Name).ToString())
				.Append((!item.Hostage) ? " - ward" : " - hostage")
				.Append(", ")
				.Append((CourtBehavior.Today() - item.Taken) / Math.Max(1, Cfg.DaysPerYear))
				.Append(" year(s)");
			if (item.Forfeit)
			{
				stringBuilder.Append("  [their house broke faith]");
			}
			stringBuilder.Append("\n");
		}
		return stringBuilder.ToString();
	}

	private static void Popup(string title, string text)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", (string)null, (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
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
