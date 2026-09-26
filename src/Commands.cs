using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
public static class Commands
{
	[CommandLineFunctionality.CommandLineArgumentFunction("status", "wad")]
	public static string Status(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Wardens & Dragons");
		stringBuilder.AppendLine("  Honour " + Store.Honour + " (cap " + Store.HonourCap + "), Dread " + Store.Dread + ((!(Store.Get("hh:madplayer") == "1")) ? "" : ", MADDENED by Harrenhal"));
		stringBuilder.AppendLine("  Mad King thresholds: Dread " + Standing.MadDread + ", Honour " + Standing.MadHonour + ((!Standing.OnMadPath()) ? "" : "  <-- ON THE PATH"));
		stringBuilder.AppendLine("  config: " + Cfg.Describe());
		stringBuilder.AppendLine("  config file: " + Cfg.LoadedFrom);
		Settlement seat = Harrenhal.Seat;
		stringBuilder.AppendLine("  Harrenhal: " + ((seat != null) ? string.Concat(seat.Name, " (", ((MBObjectBase)seat).StringId, ")") : "NOT FOUND"));
		Hero lord = Harrenhal.Lord;
		if (lord != null)
		{
			stringBuilder.AppendLine(string.Concat("  lord: ", lord.Name, (lord != Hero.MainHero) ? "" : " (you)", ", curse ", Harrenhal.CurseOf(lord).ToString("0.00"), " - ", Harrenhal.StageOf(Harrenhal.CurseOf(lord))));
		}
		stringBuilder.AppendLine("  last curse tick: day " + Harrenhal.LastTickDay + ", today is day " + CourtBehavior.Today());
		Hero mainHero = Hero.MainHero;
		if (mainHero != null && mainHero != lord)
		{
			stringBuilder.AppendLine("  your own residue: " + Harrenhal.CurseOf(mainHero).ToString("0.00"));
		}
		return stringBuilder.ToString();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("harrenhal_curse", "wad")]
	public static string SetCurse(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		if (args == null || args.Count < 1 || !float.TryParse(args[0], out var result))
		{
			return "Usage: wad.harrenhal_curse <0-100>   (sets the curse of Harrenhal's current lord)";
		}
		Hero lord = Harrenhal.Lord;
		if (lord == null)
		{
			return "Harrenhal has no lord" + ((Harrenhal.Seat != null) ? "." : " - it was not found on this map.");
		}
		Harrenhal.SetCurse(lord, result);
		return string.Concat(lord.Name, "'s curse is now ", Harrenhal.CurseOf(lord).ToString("0.0"), " - ", Harrenhal.StageOf(Harrenhal.CurseOf(lord)), ". Stage scenes play as you cross them.");
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("repair_sworn", "wad")]
	public static string RepairSworn(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		if (!Bellum.Init())
		{
			return "Bellum is not available.";
		}
		int num = 0;
		int num2 = 0;
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string key in Store.Keys("sworn:"))
		{
			string clanId = key.Substring("sworn:".Length);
			Clan val = ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Clan x) => x != null && ((MBObjectBase)x).StringId == clanId);
			Clan warden = ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Clan x) => x != null && ((MBObjectBase)x).StringId == Store.Get(key));
			if (val == null || warden == null)
			{
				continue;
			}
			string seat = Store.Get("swornt:" + clanId);
			Func<string, bool> func = delegate(string pid)
			{
				if (pid == seat)
				{
					return true;
				}
				object obj = Bellum.TitleById(pid);
				return obj != null && (Bellum.DeJureHolderOf(obj) == ((MBObjectBase)warden).StringId || Bellum.DeFactoHolderOf(obj) == ((MBObjectBase)warden).StringId);
			};
			List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(val);
			int num3 = 0;
			foreach (KeyValuePair<string, object> t in list)
			{
				string text = Bellum.ParentIdOf(t.Value);
				if (text == null || !func(text))
				{
					continue;
				}
				int tier = Bellum.TierOf(t.Value);
				List<KeyValuePair<string, object>> list2 = list.Where((KeyValuePair<string, object> c) => Bellum.TierOf(c.Value) > tier && Bellum.IdOf(c.Value) != Bellum.IdOf(t.Value)).ToList();
				if (list2.Count != 0)
				{
					int nextTier = list2.Min((KeyValuePair<string, object> c) => Bellum.TierOf(c.Value));
					Settlement here = Bellum.CapitalOf(t.Value);
					KeyValuePair<string, object> keyValuePair = (from c in list2
						where Bellum.TierOf(c.Value) == nextTier
						orderby Distance(here, Bellum.CapitalOf(c.Value))
						select c).First();
					if (Bellum.PlaceBeneath(t.Value, keyValuePair.Value, out var why))
					{
						num3++;
						Log.Write(string.Concat("repair: ", t.Key, " of ", val.Name, " back beneath ", keyValuePair.Key));
					}
					else
					{
						Log.Write("repair: could not move " + t.Key + ": " + why);
					}
				}
			}
			if (num3 > 0)
			{
				num2++;
				num += num3;
				stringBuilder.AppendLine(string.Concat("  ", val.Name, ": ", num3, " title(s) back beneath their own lands"));
			}
		}
		if (num > 0)
		{
			Bellum.RebuildIndexes();
		}
		return ((num != 0) ? ("Repaired " + num + " title(s) across " + num2 + " house(s):\n" + stringBuilder) : "Nothing needed repairing.") + "\nLand that sat inside ANOTHER house's county cannot be restored this way - only a save from before the swear can.";
	}

	private static float Distance(Settlement a, Settlement b)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			float result;
			if (a == null || b == null)
			{
				result = 1E+09f;
			}
			else
			{
				Vec2 getPosition2D = a.GetPosition2D;
				result = getPosition2D.Distance(b.GetPosition2D);
			}
			return result;
		}
		catch
		{
			return 1E+09f;
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cultures", "wad")]
	public static string CultureList(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Every realm, its culture, and how it negotiates:");
		int num = 0;
		foreach (Kingdom item in (List<Kingdom>)(object)Kingdom.All)
		{
			if (item != null && !item.IsEliminated)
			{
				CultureObject val = item.Culture ?? ((item.Leader == null) ? null : item.Leader.Culture);
				CultureRow cultureRow = Cultures.For(item);
				if (cultureRow == null)
				{
					num++;
				}
				stringBuilder.AppendLine(string.Concat("  ", item.Name, "  -  culture '", (val == null) ? "?" : ((MBObjectBase)val).StringId, "' (", (val == null) ? "?" : ((object)((BasicCultureObject)val).Name).ToString(), ")  ->  ", (cultureRow != null) ? (cultureRow.Label + "  [shelter " + cultureRow.Shelter + ", honour " + cultureRow.Honour + ", wealth " + cultureRow.Wealth + ", threat " + cultureRow.Threat + ", base " + cultureRow.Base + "]") : "NEUTRAL, not in the table"));
			}
		}
		stringBuilder.AppendLine((num != 0) ? (num + " realm(s) fall back to neutral - copy this list back to be added.") : "Every realm is in the table.");
		Log.Write(stringBuilder.ToString());
		return stringBuilder.ToString();
	}

	// wad.succession_test - resolve the succession now, without dying for it.
	[CommandLineFunctionality.CommandLineArgumentFunction("succession_test", "wad")]
	public static string SuccessionTest(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		Bastard.Offer(Hero.MainHero);
		return "You were asked the question a death asks. You are, in fact, fine.";
	}

	// wad.wards - the children of other houses living at your court.
	[CommandLineFunctionality.CommandLineArgumentFunction("wards", "wad")]
	public static string WardList(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("Held at your court:");
		sb.Append(Wardship.Summary());
		sb.AppendLine("Houses who could be asked: " + Wardship.Candidates().Count);
		Log.Write(sb.ToString());
		return sb.ToString();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("dragons", "wad")]
	public static string DragonList(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Dragon registry (" + Dragons.LivingCount() + " living, " + Dragons.Deaths() + " dead since records began):");
		foreach (DragonRec item in Dragons.All())
		{
			Hero val = Dragons.Find(item.Rider);
			stringBuilder.AppendLine("  " + item.Id + "  " + item.Name + "  [" + item.Item + "]  " + item.Status + ((val == null) ? "" : (" - " + val.Name)) + ", " + Dragons.SizeOf(item) + ", " + Dragons.TemperOf(item) + ((item.Kills <= 0) ? "" : (", killed " + item.Kills)));
		}
		stringBuilder.AppendLine("Dragonstone: " + ((Dragons.Seat == null) ? "NOT FOUND" : string.Concat(Dragons.Seat.Name, " (", ((MBObjectBase)Dragons.Seat).StringId, "), held by ", (Dragons.Seat.OwnerClan == null) ? "?" : ((object)Dragons.Seat.OwnerClan.Name).ToString())));
		stringBuilder.AppendLine("Cradle egg chance today: " + (Dragons.HatchChance() * 100f).ToString("0.0") + "%");
		return stringBuilder.ToString();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("hatch", "wad")]
	public static string Hatch(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		Clan playerClan = Clan.PlayerClan;
		if (args == null || args.Count == 0)
		{
			return "Usage: wad.hatch <part of a name>. Your house: " + string.Join(", ", (from h in (IEnumerable<Hero>)playerClan.Heroes
				where h.IsAlive
				select ((object)h.Name).ToString()).ToArray());
		}
		string part = string.Join(" ", args.ToArray()).ToLowerInvariant();
		Hero val = ((IEnumerable<Hero>)playerClan.Heroes).FirstOrDefault((Hero h) => h.IsAlive && ((object)h.Name).ToString().ToLowerInvariant().Contains(part));
		if (val == null)
		{
			return "No one of your house matches '" + part + "'.";
		}
		DragonRec dragonRec = Dragons.CradleEgg(val, mine: true, forceHatch: true);
		return (dragonRec != null) ? string.Concat(val.Name, " now has ", dragonRec.Name, ", a hatchling. Rideable at ", Cfg.RideableAge, " - or use wad.mature.") : "It did not hatch.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("mature", "wad")]
	public static string Mature(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		int num = 0;
		foreach (DragonRec item in from r in Dragons.All()
			where r.Status == "hatchling"
			select r)
		{
			Hero val = Dragons.Find(item.Rider);
			if (val != null && val.Clan == Clan.PlayerClan)
			{
				item.Status = "bonded";
				Dragons.Save(item);
				num++;
			}
		}
		Dragons.Weekly();
		return num + " hatchling(s) grown and mounted.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("dragon_check", "wad")]
	public static string DragonCheck(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		Dragons.Weekly();
		return "Bonds checked - see the log for anything returned or taken.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("tribute_now", "wad")]
	public static string TributeNow(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		int num = CourtBehavior.Today();
		int num2 = ((Hero.MainHero != null) ? Hero.MainHero.Gold : 0);
		Store.SetI("tribute:last", num - Cfg.DaysPerYear);
		Oaths.Yearly(num);
		int num3 = ((Hero.MainHero != null) ? Hero.MainHero.Gold : 0);
		return "Tribute gathered: " + (num3 - num2).ToString("N0") + " denars. See the court's ledger for who paid.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("honour", "wad")]
	public static string SetHonour(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		if (args == null || args.Count < 1 || !int.TryParse(args[0], out var result))
		{
			return "Usage: wad.honour <0-100>";
		}
		Standing.Change(result - Store.Honour, 0, "Console");
		return "Honour is now " + Store.Honour + ".";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("dread", "wad")]
	public static string SetDread(List<string> args)
	{
		if (Campaign.Current == null)
		{
			return "Load a campaign first.";
		}
		if (args == null || args.Count < 1 || !int.TryParse(args[0], out var result))
		{
			return "Usage: wad.dread <0-100>";
		}
		Standing.Change(0, result - Store.Dread, "Console");
		return "Dread is now " + Store.Dread + ".";
	}
}
}
