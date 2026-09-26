using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
internal static class Harrenhal
{
	private const string Curse = "hh:c:";

	private const string Stage = "hh:s:";

	private const string Season = "hh:q:";

	private const string Year = "hh:y:";

	private const string Mad = "hh:m:";

	private static readonly string[] Towers = new string[5] { "the Kingspyre Tower", "the Tower of Dread", "the Widow's Wail", "the Wailing Tower", "the Tower of Ghosts" };

	private static readonly string[] StageNames = new string[5] { "untouched", "sleepless", "suspicious", "ruined", "mad" };

	private static Settlement _seat;

	private static bool _looked;

	private static int _traced;

	internal static int LastTickDay = -1;

	internal static Settlement Seat
	{
		get
		{
			if (_looked)
			{
				return _seat;
			}
			_looked = true;
			try
			{
				if (!string.IsNullOrEmpty(Cfg.HarrenhalId))
				{
					_seat = Settlement.Find(Cfg.HarrenhalId);
				}
				if (_seat == null)
				{
					_seat = ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Settlement s) => s != null && s.Name != (TextObject)null && string.Equals(((object)s.Name).ToString(), "Harrenhal", StringComparison.OrdinalIgnoreCase)) ?? ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Settlement s) => s != null && s.Name != (TextObject)null && ((object)s.Name).ToString().IndexOf("Harrenhal", StringComparison.OrdinalIgnoreCase) >= 0);
				}
				Log.Write((_seat == null) ? "Harrenhal NOT found on this map - the curse is dormant. Set harrenhal_settlement_id in config.txt." : string.Concat("Harrenhal found: ", _seat.Name, " (", ((MBObjectBase)_seat).StringId, ")"));
			}
			catch (Exception ex)
			{
				Log.Write("Harrenhal lookup failed: " + ex.Message);
			}
			return _seat;
		}
	}

	internal static Hero Lord
	{
		get
		{
			Settlement seat = Seat;
			return (seat == null || seat.OwnerClan == null) ? null : seat.OwnerClan.Leader;
		}
	}

	internal static void Reset()
	{
		_seat = null;
		_looked = false;
		_traced = 0;
		LastTickDay = -1;
	}

	internal static void SetCurse(Hero h, float value)
	{
		if (h != null)
		{
			value = Math.Max(0f, Math.Min(100f, value));
			Store.SetF("hh:c:" + ((MBObjectBase)h).StringId, value);
			if (value < 25f)
			{
				Store.Set("hh:s:" + ((MBObjectBase)h).StringId, null);
			}
			else if (h == Lord)
			{
				CheckStages(h, value);
			}
			Log.Write(string.Concat("console: curse of ", h.Name, " set to ", value));
		}
	}

	internal static float CurseOf(Hero h)
	{
		return (h != null) ? Store.GetF("hh:c:" + ((MBObjectBase)h).StringId) : 0f;
	}

	internal static string StageOf(float curse)
	{
		return StageNames[Math.Min(4, (int)(curse / 25f))];
	}

	internal static void Daily(int today)
	{
		try
		{
			if (Seat == null)
			{
				return;
			}
			float num = Cfg.CursePerYear / (float)Cfg.DaysPerYear * Cfg.CurseSpeed;
			float num2 = Cfg.CurseDecayPerYear / (float)Cfg.DaysPerYear * Cfg.CurseSpeed;
			Hero lord = Lord;
			LastTickDay = today;
			if (lord != null && lord.IsAlive)
			{
				float num3 = CurseOf(lord);
				float num4 = Math.Min(100f, num3 + num);
				if (_traced < 5)
				{
					_traced++;
					Log.Write(string.Concat("Harrenhal tick day ", today, ": ", lord.Name, " curse ", num3.ToString("0.00"), " -> ", num4.ToString("0.00"), " (+", num.ToString("0.000"), "/day at speed ", Cfg.CurseSpeed, ")"));
				}
				Store.SetF("hh:c:" + ((MBObjectBase)lord).StringId, num4);
				CheckStages(lord, num4);
				Seasonal(lord, num4, today);
				Yearly(lord, num4, today);
			}
			foreach (string item in Store.Keys("hh:c:"))
			{
				string text = item.Substring("hh:c:".Length);
				if (lord == null || !(text == ((MBObjectBase)lord).StringId))
				{
					float num5 = Store.GetF(item) - num2;
					if (num5 <= 0f)
					{
						Store.Set(item, null);
						Store.Set("hh:s:" + text, null);
					}
					else
					{
						Store.SetF(item, num5);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("hhdaily", "Harrenhal daily failed: " + ex);
		}
	}

	private static void CheckStages(Hero lord, float curse)
	{
		int num = Math.Min(4, (int)(curse / 25f));
		int num2 = Store.GetI("hh:s:" + ((MBObjectBase)lord).StringId);
		while (num2 < num)
		{
			num2++;
			Store.SetI("hh:s:" + ((MBObjectBase)lord).StringId, num2);
			OnStage(lord, num2);
		}
	}

	private static void OnStage(Hero lord, int stage)
	{
		bool flag = lord == Hero.MainHero;
		Log.Write(string.Concat("Harrenhal: ", lord.Name, " reaches stage ", stage, " (", StageNames[stage], ")"));
		switch (stage)
		{
		case 1:
			if (flag)
			{
				Standing.Change(0, Cfg.SleeplessDread, "Sleepless nights at Harrenhal");
			}
			Tell(flag, "Sleepless", "The nights at Harrenhal are long, and you no longer sleep through them. The towers groan in the wind like something in pain, and your household has begun to watch you the way men watch a fire that has not yet caught.", string.Concat(lord.Name, ", lord of Harrenhal, is said to sleep poorly."));
			break;
		case 2:
			Tell(flag, "Suspicion", "You caught a servant at your door tonight, and you did not believe a word he said. There are too many towers here, too many passages, too many people who might be listening. Harren thought the same, before the end.", string.Concat("It is said ", lord.Name, " trusts no one in Harrenhal now, not even his own blood."));
			break;
		case 3:
			Tell(flag, "Ruin", "Harrenhal takes. It took Harren and all his sons in one night of dragonfire, and it has taken from every house that held it since. Now it has begun to take from yours.", string.Concat("Misfortune gathers around Harrenhal and the house of ", lord.Name, "."));
			break;
		case 4:
			if (Store.Get("hh:m:" + ((MBObjectBase)lord).StringId) != "1")
			{
				Store.Set("hh:m:" + ((MBObjectBase)lord).StringId, "1");
				try
				{
					lord.SetTraitLevel(DefaultTraits.Mercy, -2);
				}
				catch (Exception ex)
				{
					Log.Write("mercy trait failed: " + ex.Message);
				}
				if (flag)
				{
					Store.Set("hh:madplayer", "1");
				}
			}
			Tell(flag, "Madness", "Something in you has changed, and everyone can see it but you. Mercy seems a weakness now, and every kindness a trap. Harrenhal has finished what it started.\n\nFeared and faithless will now be your undoing far sooner than it would another's.", string.Concat(lord.Name, " of Harrenhal has gone mad. Those who serve him speak of it only in whispers."));
			break;
		}
	}

	private static void Seasonal(Hero lord, float curse, int today)
	{
		if (curse < 25f)
		{
			return;
		}
		int i = Store.GetI("hh:q:" + ((MBObjectBase)lord).StringId, today);
		if (Store.Get("hh:q:" + ((MBObjectBase)lord).StringId) == null)
		{
			Store.SetI("hh:q:" + ((MBObjectBase)lord).StringId, today);
		}
		else
		{
			if (today - i < Cfg.DaysPerSeason)
			{
				return;
			}
			Store.SetI("hh:q:" + ((MBObjectBase)lord).StringId, today);
			int num = 0;
			foreach (Hero item in Kin(lord, adultsOnly: false))
			{
				try
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(lord, item, -1, false);
					num++;
				}
				catch
				{
				}
			}
			if (num > 0)
			{
				Log.Write(string.Concat("Harrenhal: ", lord.Name, " grows colder to ", num, " of his kin"));
			}
		}
	}

	private static void Yearly(Hero lord, float curse, int today)
	{
		if (curse < 50f)
		{
			return;
		}
		if (Store.Get("hh:y:" + ((MBObjectBase)lord).StringId) == null)
		{
			Store.SetI("hh:y:" + ((MBObjectBase)lord).StringId, today);
		}
		else
		{
			if (today - Store.GetI("hh:y:" + ((MBObjectBase)lord).StringId) < Cfg.DaysPerYear)
			{
				return;
			}
			Store.SetI("hh:y:" + ((MBObjectBase)lord).StringId, today);
			bool flag = lord == Hero.MainHero;
			List<Hero> list = Kin(lord, adultsOnly: true);
			if (list.Count > 0)
			{
				Hero val = list[MBRandom.RandomInt(list.Count)];
				try
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(lord, val, -10, false);
				}
				catch
				{
				}
				Say(flag, string.Concat(lord.Name, " accuses ", val.Name, " of plotting against ", (!flag) ? "him" : "you", "."));
				if (flag)
				{
					Store.AddDeed(string.Concat(Standing.Date(), "  You accused ", val.Name, " of treason."));
				}
			}
			if (curse < 75f)
			{
				return;
			}
			if (MBRandom.RandomInt(100) < Cfg.FireChance && Seat != null && Seat.Town != null)
			{
				try
				{
					Seat.Town.Prosperity = Seat.Town.Prosperity * (1f - (float)Cfg.FireLossPercent / 100f);
					Say(flag, "Fire breaks out in " + Tower() + ". No one can say how it started.");
				}
				catch (Exception ex)
				{
					Log.Write("fire failed: " + ex.Message);
				}
			}
			int kinChance = Cfg.KinDeathChance;
			if (MBRandom.RandomInt(100) >= kinChance)
			{
				return;
			}
			List<Hero> list2 = (from h in Kin(lord, adultsOnly: false)
				where h != Hero.MainHero
				select h).ToList();
			if (flag && !Cfg.CanKillPlayerFamily)
			{
				list2.Clear();
			}
			if (list2.Count <= 0)
			{
				return;
			}
			Hero val2 = list2[MBRandom.RandomInt(list2.Count)];
			string text = Tower();
			try
			{
				KillCharacterAction.ApplyByMurder(val2, (Hero)null, true);
				Log.Write(string.Concat("Harrenhal takes ", val2.Name, " of the house of ", lord.Name));
				if (flag)
				{
					Store.AddDeed(string.Concat(Standing.Date(), "  ", val2.Name, " died in ", text, "."));
					Popup("Harrenhal Takes", string.Concat(val2.Name, " was found at dawn at the foot of ", text, ". The maesters have no explanation they will say aloud.\n\nHarrenhal has always taken from the houses that hold it."));
				}
				else
				{
					Say(mine: false, string.Concat(val2.Name, " has died at Harrenhal. Some call it an accident."));
				}
			}
			catch (Exception ex2)
			{
				Log.Write("kin death failed: " + ex2.Message);
			}
		}
	}

	internal static void OnOwnerChanged(Settlement s, Hero newOwner, Hero oldOwner, string detail)
	{
		try
		{
			if (s != null && Seat != null && s == Seat)
			{
				Clan playerClan = Clan.PlayerClan;
				bool flag = oldOwner != null && oldOwner.Clan == playerClan;
				bool flag2 = newOwner != null && newOwner.Clan == playerClan;
				Log.Write("Harrenhal changes hands: " + ((oldOwner == null) ? "?" : ((object)oldOwner.Name).ToString()) + " -> " + ((newOwner == null) ? "?" : ((object)newOwner.Name).ToString()) + " (" + detail + ")");
				if (flag2 && !flag)
				{
					Store.AddDeed(Standing.Date() + "  Harrenhal is yours.");
					Popup("Harrenhal", "Harrenhal is yours: the greatest castle ever raised in Westeros, and the most unlucky. Harren built it to outlast the ages, and died inside it the year it was finished.\n\nEvery house that has held it since has been ruined. The curse settles on the lord who holds it, and grows with every year they stay.\n\nYou may yet give it away. It has always made a fine gift for a rival.");
				}
				else if (flag && !flag2 && newOwner != null)
				{
					bool flag3 = detail != null && detail.IndexOf("Gift", StringComparison.OrdinalIgnoreCase) >= 0;
					Store.AddDeed(Standing.Date() + ((!flag3) ? string.Concat("  Harrenhal passed to ", newOwner.Name, ".") : string.Concat("  You granted Harrenhal to ", newOwner.Name, ". May it serve them as it served you.")));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("owner change failed: " + ex.Message);
		}
	}

	internal static string CourtSummary()
	{
		Settlement seat = Seat;
		if (seat == null)
		{
			return "Harrenhal could not be found on this map.";
		}
		Hero lord = Lord;
		StringBuilder stringBuilder = new StringBuilder();
		if (lord == null)
		{
			stringBuilder.Append("Harrenhal stands without a lord.");
		}
		else
		{
			float num = CurseOf(lord);
			bool flag = lord == Hero.MainHero;
			stringBuilder.Append("Harrenhal is held by ").Append((!flag) ? ((object)lord.Name).ToString() : "you").Append(". The curse stands at ")
				.Append((!(num < 10f)) ? ((int)num/*cast due to constrained. prefix*/).ToString() : num.ToString("0.0"))
				.Append(" of 100 - ")
				.Append((!flag) ? "they are " : "you are ")
				.Append(StageOf(num))
				.Append(".");
		}
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && lord != mainHero)
		{
			float num2 = CurseOf(mainHero);
			if (num2 > 0.5f)
			{
				stringBuilder.Append(" You still carry ").Append((int)num2).Append(" of its curse, fading with every year away.");
			}
		}
		return stringBuilder.ToString();
	}

	internal static string CourtHeaderLine()
	{
		Hero mainHero = Hero.MainHero;
		float num = CurseOf(mainHero);
		if (num >= 75f)
		{
			return "Harrenhal is killing your house by inches.";
		}
		if (num >= 50f)
		{
			return "You trust no one since Harrenhal.";
		}
		if (num >= 25f)
		{
			return "You have not slept well since Harrenhal.";
		}
		return null;
	}

	private static List<Hero> Kin(Hero lord, bool adultsOnly)
	{
		List<Hero> list = new List<Hero>();
		try
		{
			if (lord == null || lord.Clan == null)
			{
				return list;
			}
			foreach (Hero item in (List<Hero>)(object)lord.Clan.Heroes)
			{
				if (item != null && item != lord && item.IsAlive && (!adultsOnly || !item.IsChild))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static string Tower()
	{
		return Towers[MBRandom.RandomInt(Towers.Length)];
	}

	private static void Tell(bool mine, string title, string playerText, string worldText)
	{
		if (mine)
		{
			Popup(title, playerText);
		}
		else
		{
			Say(mine: false, worldText);
		}
	}

	private static void Say(bool mine, string text)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(text, (!mine) ? Color.FromUint(4289765504u) : Color.FromUint(4292432719u)));
		}
		catch
		{
		}
		Log.Write(text);
	}

	private static void Popup(string title, string text)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", (string)null, (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch (Exception ex)
		{
			Log.Write("popup failed: " + ex.Message);
			Say(mine: true, text);
		}
	}
}
}
