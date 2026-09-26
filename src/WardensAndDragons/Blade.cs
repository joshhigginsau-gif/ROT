using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Blade
{
	internal enum Reckoning
	{
		None,
		Ignored,
		Acknowledged,
		Armed,
		Both
	}

	private const string HolderKey = "bl:holder";

	private static bool _synced;

	internal static bool Given => HolderOf() != null;

	internal static Hero HolderOf()
	{
		try
		{
			string id = Store.Get("bl:holder");
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			Hero val = ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero x) => ((MBObjectBase)x).StringId == id));
			if (val == null)
			{
				Store.Set("bl:holder", null);
				Log.Write("the blade has come back to the house");
				return null;
			}
			return val;
		}
		catch
		{
			return null;
		}
	}

	internal static List<Hero> Candidates()
	{
		List<Hero> list = new List<Hero>();
		try
		{
			foreach (Hero item in Succession.Claimants())
			{
				if (item != null && !list.Contains(item))
				{
					list.Add(item);
				}
			}
			foreach (Kid item2 in Baseborn.Known())
			{
				Hero val = Baseborn.HeroOf(item2);
				if (val != null && !val.IsChild && val != Hero.MainHero && !list.Contains(val))
				{
					list.Add(val);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	internal static void Give(Hero to)
	{
		try
		{
			if (to != null)
			{
				Store.Set("bl:holder", ((MBObjectBase)to).StringId);
				string text = Lore.Blade();
				Store.AddDeed(string.Concat(Standing.Date(), "  ", text, " was given to ", to.Name, "."));
				Log.Write(string.Concat("the blade ", text, " was given to ", to.Name, " (baseborn=", IsBaseborn(to), ")"));
				Sync();
			}
		}
		catch (Exception ex)
		{
			Log.Write("giving the blade failed: " + ex.Message);
		}
	}

	internal static void TakeBack()
	{
		try
		{
			Hero val = HolderOf();
			Store.Set("bl:holder", null);
			if (val != null)
			{
				Store.AddDeed(string.Concat(Standing.Date(), "  ", Lore.Blade(), " was taken back from ", val.Name, "."));
				Log.Write("the blade was taken back from " + val.Name);
			}
		}
		catch
		{
		}
	}

	internal static bool IsBaseborn(Hero h)
	{
		try
		{
			if (h == null)
			{
				return false;
			}
			foreach (Kid item in Baseborn.All())
			{
				if (Baseborn.HeroOf(item) == h)
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

	internal static Kid RecordFor(Hero h)
	{
		try
		{
			foreach (Kid item in Baseborn.All())
			{
				if (Baseborn.HeroOf(item) == h)
				{
					return item;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool Impossible(Hero h)
	{
		try
		{
			if (h == null || !h.IsAlive || h.IsChild)
			{
				return true;
			}
			if (h == Hero.MainHero)
			{
				return true;
			}
			Clan playerClan = Clan.PlayerClan;
			return playerClan != null && playerClan.Leader == h;
		}
		catch
		{
			return true;
		}
	}

	internal static Hero Claimant(out Reckoning how)
	{
		how = Reckoning.None;
		try
		{
			Hero val = HolderOf();
			if (val != null && IsBaseborn(val) && !Impossible(val))
			{
				Kid kid = RecordFor(val);
				how = ((kid == null || !kid.Legit) ? Reckoning.Armed : Reckoning.Both);
				return val;
			}
			List<Kid> list = Baseborn.Known();
			if (list.Count == 0)
			{
				return null;
			}
			Kid kid2 = null;
			Hero val2 = null;
			foreach (Kid item in list)
			{
				Hero val3 = Baseborn.HeroOf(item);
				if (!Impossible(val3) && (kid2 == null || (item.Legit && !kid2.Legit) || (item.Legit == kid2.Legit && val2 != null && val3.Age > val2.Age)))
				{
					kid2 = item;
					val2 = val3;
				}
			}
			if (val2 == null)
			{
				return null;
			}
			how = ((!kid2.Legit) ? Reckoning.Ignored : Reckoning.Acknowledged);
			return val2;
		}
		catch (Exception ex)
		{
			Log.Once("reckoning", "reading the reckoning failed: " + ex.Message);
			return null;
		}
	}

	internal static float Share(Reckoning how)
	{
		return how switch
		{
			Reckoning.Ignored => Cfg.ShareIgnored, 
			Reckoning.Acknowledged => Cfg.ShareAcknowledged, 
			Reckoning.Armed => Cfg.ShareArmed, 
			Reckoning.Both => Cfg.ShareBoth, 
			_ => Cfg.BastardShare, 
		};
	}

	internal static bool Crowns(Reckoning how)
	{
		return how == Reckoning.Armed || how == Reckoning.Both;
	}

	internal static bool Declares(Reckoning how)
	{
		return Cfg.BastardWar && Crowns(how);
	}

	internal static string Reading(Reckoning how)
	{
		return how switch
		{
			Reckoning.Ignored => "a child you never acknowledged and never armed", 
			Reckoning.Acknowledged => "a child you gave your name to", 
			Reckoning.Armed => "a child you put your ancestral sword into the hands of", 
			Reckoning.Both => "a child you gave both your name and your sword", 
			_ => "a stranger with your face", 
		};
	}

	internal static void Sync()
	{
		try
		{
			if (Clan.PlayerClan == null)
			{
				return;
			}
			string text = Lore.Named();
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			Type type = AccessTools.TypeByName("RoTDynastyAndSuccession.Houses.HouseLoreBehavior");
			object obj = ((type == null) ? null : AccessTools.Property(type, "Instance")?.GetValue(null, null));
			MethodInfo methodInfo = ((obj != null) ? AccessTools.Method(type, "SetAncestralBladeName", (Type[])null, (Type[])null) : null);
			if (methodInfo == null)
			{
				if (!_synced)
				{
					_synced = true;
					Log.Write("RoT's house lore was not found; the blade is ours alone");
				}
			}
			else
			{
				methodInfo.Invoke(obj, new object[2]
				{
					Clan.PlayerClan,
					text
				});
				Log.Write("RoT now names the blade " + text + " too");
			}
		}
		catch (Exception ex)
		{
			Log.Once("bladesync", "telling RoT about the blade failed: " + ex.Message);
		}
	}

	internal static void Reset()
	{
		_synced = false;
	}
}
