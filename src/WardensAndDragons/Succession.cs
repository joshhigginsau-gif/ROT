using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Succession
{
	private static Hero _named;

	private static int _namedDay = -9999;

	internal static Kingdom Realm()
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || playerClan.Kingdom == null)
			{
				return null;
			}
			Kingdom kingdom = playerClan.Kingdom;
			return (kingdom.Leader == null || kingdom.Leader.Clan != playerClan) ? null : kingdom;
		}
		catch
		{
			return null;
		}
	}

	internal static bool Rules()
	{
		return Realm() != null;
	}

	internal static void Forget()
	{
		_named = null;
		_namedDay = -9999;
	}

	internal static Hero Named()
	{
		try
		{
			int num = CourtBehavior.Today();
			if (_namedDay == num && _named != null && _named.IsAlive && _named.Clan == Clan.PlayerClan)
			{
				return _named;
			}
			_named = NamedUncached();
			_namedDay = num;
			return _named;
		}
		catch
		{
			return null;
		}
	}

	private static Hero NamedUncached()
	{
		try
		{
			string id = Store.Get("sc:heir");
			if (!string.IsNullOrEmpty(id))
			{
				Hero val = ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero x) => ((MBObjectBase)x).StringId == id));
				if (val != null && val.IsAlive && val.Clan == Clan.PlayerClan)
				{
					return val;
				}
				Store.Set("sc:heir", null);
			}
			List<Hero> list = Claimants();
			return (list.Count <= 0) ? null : list[0];
		}
		catch
		{
			return null;
		}
	}

	internal static void Name(Hero h)
	{
		if (h != null)
		{
			Store.Set("sc:heir", ((MBObjectBase)h).StringId);
			Forget();
			Store.AddDeed(string.Concat(Standing.Date(), "  ", h.Name, " was named heir."));
		}
	}

	internal static List<Hero> Claimants()
	{
		List<Hero> list = new List<Hero>();
		try
		{
			Hero mainHero = Hero.MainHero;
			if (mainHero == null || Clan.PlayerClan == null)
			{
				return list;
			}
			foreach (Hero item in (List<Hero>)(object)Clan.PlayerClan.Heroes)
			{
				if (item != null && item.IsAlive && item != mainHero && !item.IsChild && IsBlood(item, mainHero))
				{
					list.Add(item);
				}
			}
			list = list.OrderByDescending((Hero h) => h.Age).ToList();
		}
		catch
		{
		}
		return list;
	}

	private static void Climb(Hero h, int depth, HashSet<Hero> into)
	{
		if (h != null && depth >= 0 && into.Add(h))
		{
			Climb(h.Father, depth - 1, into);
			Climb(h.Mother, depth - 1, into);
		}
	}

	internal static bool IsBlood(Hero h, Hero you)
	{
		try
		{
			if (h == null || you == null || h.IsPlayerCompanion)
			{
				return false;
			}
			HashSet<Hero> hashSet = new HashSet<Hero>();
			Climb(you, 2, hashSet);
			HashSet<Hero> hashSet2 = new HashSet<Hero>();
			Climb(h, 3, hashSet2);
			if (hashSet2.Contains(you) || hashSet.Contains(h))
			{
				return true;
			}
			foreach (Hero item in hashSet2)
			{
				if (hashSet.Contains(item))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static Hero Chosen()
	{
		try
		{
			string id = Store.Get("sc:chosen");
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			Hero val = ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero x) => ((MBObjectBase)x).StringId == id));
			if (val == null || !val.IsAlive || val.Clan != Clan.PlayerClan)
			{
				Store.Set("sc:chosen", null);
				return null;
			}
			return val;
		}
		catch
		{
			return null;
		}
	}

	internal static void Remember(Hero selectedHeir)
	{
		try
		{
			if (selectedHeir != null)
			{
				Store.Set("sc:chosen", ((MBObjectBase)selectedHeir).StringId);
				Store.Set("sc:heir", ((MBObjectBase)selectedHeir).StringId);
				Forget();
				Log.Write(string.Concat("you chose ", selectedHeir.Name, " on the heir screen"));
			}
		}
		catch
		{
		}
	}

	internal static bool Install(Clan clan, Hero heir, string why)
	{
		try
		{
			if (!Cfg.EnforceHeir || clan == null || heir == null)
			{
				return false;
			}
			if (!heir.IsAlive || heir.Clan != clan || heir.IsChild)
			{
				return false;
			}
			if (clan.Leader == heir || clan.Leader == null)
			{
				return false;
			}
			Hero leader = clan.Leader;
			ChangeClanLeaderAction.ApplyWithSelectedNewLeader(clan, heir);
			Store.Set("sc:chosen", null);
			Log.Write(string.Concat("the seat was taken back: ", heir.Name, " now leads ", clan.Name, " in place of ", leader.Name, " (", why, ")"));
			Store.AddDeed(string.Concat(Standing.Date(), "  ", heir.Name, " took their father's seat."));
			return true;
		}
		catch (Exception ex)
		{
			Log.Write("installing the heir on the seat failed: " + ex.Message);
			return false;
		}
	}

	internal static Hero ShouldLead(Clan clan)
	{
		try
		{
			if (clan == null)
			{
				return null;
			}
			Hero mainHero = Hero.MainHero;
			if (mainHero != null && mainHero.IsAlive && mainHero.Clan == clan && !mainHero.IsChild)
			{
				return mainHero;
			}
			Hero val = Named();
			return (val == null || val.Clan != clan) ? null : val;
		}
		catch
		{
			return null;
		}
	}

	internal static string Reading()
	{
		try
		{
			Hero val = Named();
			if (val == null)
			{
				return "no heir named. The realm is guessing, and guessing badly.";
			}
			return (!Laws.Lawful()) ? string.Concat("on ", val.Name, ", named against your culture's law, and every house knows it.") : string.Concat("settled on ", val.Name, ".");
		}
		catch
		{
			return "uncertain.";
		}
	}
}
