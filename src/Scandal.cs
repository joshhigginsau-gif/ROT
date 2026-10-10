using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Love, and what comes of it: affairs found out, elopements against a
	// father's will, children who should not exist. You hear of it when it
	// touches your house or realm; otherwise the histories remember.
	internal static class Scandal
	{
		private static bool Lord(Hero h)
		{
			return h != null && h.IsAlive && h.IsLord && !h.IsChild && !h.IsPrisoner && h != Hero.MainHero && h.Clan != null && !h.Clan.IsBanditFaction && !h.Clan.IsMinorFaction && !Guard.IsSworn(h);
		}

		private static string N(Hero h) { return h.Name.ToString(); }

		internal static void Daily(int today)
		{
			if (!Cfg.Scandals || !Store.Initialized)
			{
				return;
			}
			try
			{
				if (MBRandom.RandomFloat * 100f < Cfg.ScandalDailyChance)
				{
					if (MBRandom.RandomInt(100) < 55)
					{
						Affair(false);
					}
					else
					{
						Elope(false);
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("scandal", "scandal: " + e.Message);
			}
		}

		private static List<Hero> Pool()
		{
			return Hero.AllAliveHeroes.Where(Lord).ToList();
		}

		// Weighted toward your own house and realm: a scandal there is three
		// times as likely to be the one that happens.
		private static Hero Pick(List<Hero> pool, Func<Hero, bool> ok)
		{
			List<Hero> can = pool.Where(ok).ToList();
			if (can.Count == 0)
			{
				return null;
			}
			List<Hero> near = can.Where((Hero h) => Nemesis.Touches(h) || (Clan.PlayerClan.Kingdom != null && h.MapFaction == Clan.PlayerClan.Kingdom)).ToList();
			if (near.Count > 0 && MBRandom.RandomInt(100) < Math.Min(75, 100 * near.Count * 3 / Math.Max(1, can.Count + near.Count * 2)))
			{
				return near[MBRandom.RandomInt(near.Count)];
			}
			return can[MBRandom.RandomInt(can.Count)];
		}

		internal static string Affair(bool force)
		{
			List<Hero> pool = Pool();
			Hero a = Pick(pool, (Hero h) => h.Spouse != null && h.Spouse.IsAlive);
			if (a == null)
			{
				return "Nobody married to stray.";
			}
			Hero b = pool.Where((Hero h) => h != a && h != a.Spouse && h.IsFemale != a.IsFemale && h.Clan != a.Clan && h.Age > 18 && (force || h.GetRelation(a) >= 20))
				.OrderByDescending((Hero h) => h.GetRelation(a)).FirstOrDefault();
			if (b == null)
			{
				return "No lover to be found for " + N(a) + ".";
			}
			Hero wronged = a.Spouse;
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(wronged, a, -MBRandom.RandomInt(40, 61), false);
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(wronged, b, -MBRandom.RandomInt(40, 61), false);
			if (a.Clan.Leader != null && b.Clan.Leader != null && a.Clan.Leader != b.Clan.Leader)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(a.Clan.Leader, b.Clan.Leader, -20, false);
			}
			string child = null;
			Hero mother = a.IsFemale ? a : b;
			Hero father = a.IsFemale ? b : a;
			if (MBRandom.RandomInt(100) < 25 && mother.Age < 42)
			{
				child = Bastard(mother, father);
			}
			History.Hero(a, "Was found out in an affair with " + N(b) + ", betraying " + N(wronged) + ".", 3);
			History.Hero(b, "Was found out as the lover of " + N(a) + ".", 3);
			History.Hero(wronged, "Learned that " + N(a) + " had been unfaithful with " + N(b) + ".", 2);
			History.House(a.Clan, "Scandal: " + N(a) + " and " + N(b) + (child != null ? (" - and a child, " + child + ".") : "."), 2);
			History.House(b.Clan, "Scandal: " + N(b) + " and " + N(a) + ".", 2);
			Log.Write("scandal: affair " + N(a) + " / " + N(b) + (child != null ? (", child " + child) : ""));
			if (Nemesis.Touches(a) || Nemesis.Touches(b) || Nemesis.Touches(wronged))
			{
				Ravens.Popup("Scandal", "The whole court is talking: " + N(a) + " has been found out with " + N(b) + ", and " + N(wronged) + " knows." + (child != null ? ("\n\nThere is a child, too: " + child + ".") : "") +
					((wronged == Hero.MainHero) ? "\n\nIt is you they are whispering about." : ""));
				if (a.Clan == Clan.PlayerClan || b.Clan == Clan.PlayerClan)
				{
					Standing.Change(-2, 0, "a scandal in your house");
				}
			}
			return "Affair: " + N(a) + " and " + N(b) + ".";
		}

		private static string Bastard(Hero mother, Hero father)
		{
			try
			{
				Hero kid = HeroCreator.DeliverOffSpring(mother, father, MBRandom.RandomInt(2) == 0);
				if (kid == null)
				{
					return null;
				}
				string surname = Surnames.Of(mother.HomeSettlement ?? (mother.Clan != null ? mother.Clan.HomeSettlement : null));
				string given = (kid.FirstName != null) ? kid.FirstName.ToString() : kid.Name.ToString();
				kid.SetName(new TextObject("{=!}" + given + " " + surname, null), new TextObject("{=!}" + given, null));
				History.Hero(kid, "Born out of wedlock to " + N(mother) + " and " + N(father) + ".", 3);
				return kid.Name.ToString();
			}
			catch (Exception e)
			{
				Log.Once("scandalkid", "scandal: the child could not be made: " + e.Message);
				return null;
			}
		}

		internal static string Elope(bool force)
		{
			List<Hero> pool = Pool();
			Hero a = Pick(pool, (Hero h) => h.Spouse == null && h.Age >= 18 && h.Age <= 32 && h.Clan.Leader != h);
			if (a == null)
			{
				return "Nobody young and unwed.";
			}
			Hero b = pool.Where((Hero h) => h != a && h.Spouse == null && h.IsFemale != a.IsFemale && h.Clan != a.Clan && h.Age >= 18 && h.Age <= 40
				&& Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(a, h)
				&& (force || (h.GetRelation(a) >= 10 && a.Clan.Leader != null && h.Clan.Leader != null && a.Clan.Leader.GetRelation(h.Clan.Leader) < 0)))
				.OrderByDescending((Hero h) => h.GetRelation(a)).FirstOrDefault();
			if (b == null)
			{
				return "No forbidden match for " + N(a) + ".";
			}
			Hero head = a.Clan.Leader;
			if (a.Clan == Clan.PlayerClan && !Cfg.AmbitionFamilyFreedom)
			{
				return "Your family is not free to elope.";
			}
			MarriageAction.Apply(a, b, false);
			if (head != null && b.Clan.Leader != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(head, b.Clan.Leader, -30, false);
			}
			if (head != null)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(head, a, -30, false);
			}
			History.Hero(a, "Eloped with " + N(b) + (head != null ? (", against the will of " + N(head)) : "") + ".", 3);
			History.Hero(b, "Eloped with " + N(a) + ".", 3);
			History.House(a.Clan, N(a) + " eloped with " + N(b) + " of " + b.Clan.Name + ".", 2);
			History.House(b.Clan, N(b) + " eloped with " + N(a) + " of " + a.Clan.Name + ".", 2);
			Log.Write("scandal: elopement " + N(a) + " / " + N(b));
			if (Nemesis.Touches(a) || Nemesis.Touches(b))
			{
				Ravens.Popup("An Elopement", N(a) + " and " + N(b) + " have run off together and been married by a hedge septon, whatever " + ((head == Hero.MainHero) ? "you" : (head != null ? N(head) : "their family")) + " may think of it.");
			}
			return "Elopement: " + N(a) + " and " + N(b) + ".";
		}
	}
}
