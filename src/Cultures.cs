using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
internal static class Cultures
{
	private static readonly List<CultureRow> Rows = new List<CultureRow>
	{
		R("Braavosi", new string[1] { "braav" }, 0, 0, 15, -20, -35),
		R("Night's Watch", new string[3] { "nightswatch", "night's watch", "night watch" }, 10, 10, 15, -30, -30),
		R("Free Folk", new string[3] { "freefolk", "free folk", "wildling" }, 5, -10, -10, 10, -35),
		R("Ibbenese", new string[1] { "ibben" }, 0, 0, 15, 0, -5),
		R("Ironborn", new string[3] { "ironborn", "iron isl", "greyjoy" }, -10, -10, -5, 20, 0),
		R("Northmen", new string[2] { "north", "stark" }, 0, 15, -5, -20, 0),
		R("Dornish", new string[2] { "dorn", "martell" }, -10, 5, 0, -20, -40),
		R("Reachmen", new string[2] { "reach", "tyrell" }, 0, 0, 15, -5, 0),
		R("Vale", new string[2] { "vale", "arryn" }, 5, 10, 0, -10, 0),
		R("Rivermen", new string[2] { "river", "tully" }, 15, 0, 0, 0, 0),
		R("Westermen", new string[2] { "westerl", "lannist" }, 0, 0, 10, -5, 0),
		R("Stormlanders", new string[2] { "storm", "baratheon" }, -5, 5, 0, 5, 0),
		R("Valyrian", new string[3] { "valyr", "dragonstone", "targaryen" }, 0, 5, 0, 10, 0),
		R("Ghiscari", new string[4] { "ghis", "meereen", "yunkai", "astapor" }, 0, 0, 10, 10, 0),
		R("Qartheen", new string[1] { "qarth" }, 0, 0, 15, -5, 0),
		R("Dothraki", new string[1] { "dothrak" }, -10, -10, -10, 25, 0),
		R("Sarnori", new string[1] { "sarnor" }, 10, 10, 0, -10, 0),
		R("Yi Tish", new string[3] { "yi ti", "yitish", "yi-ti" }, 5, 10, 0, -10, -10),
		R("Free Cities", new string[9] { "tyrosh", "lys", "myr", "pentos", "volant", "norvos", "qohor", "lorath", "free cit" }, 0, -5, 20, -5, 0)
	};

	private static readonly HashSet<string> _unmatched = new HashSet<string>();

	private static CultureRow R(string l, string[] k, int s, int h, int w, int t, int b)
	{
		CultureRow cultureRow = new CultureRow();
		cultureRow.Label = l;
		cultureRow.Keys = k;
		cultureRow.Shelter = s;
		cultureRow.Honour = h;
		cultureRow.Wealth = w;
		cultureRow.Threat = t;
		cultureRow.Base = b;
		return cultureRow;
	}

	internal static CultureRow For(Kingdom k)
	{
		try
		{
			CultureObject val = ((k == null) ? null : (k.Culture ?? ((k.Leader == null) ? null : k.Leader.Culture)));
			if (val == null)
			{
				return null;
			}
			string text = ((((MBObjectBase)val).StringId ?? "") + " " + ((!(((BasicCultureObject)val).Name != (TextObject)null)) ? "" : ((object)((BasicCultureObject)val).Name).ToString())).ToLowerInvariant();
			foreach (CultureRow row in Rows)
			{
				string[] keys = row.Keys;
				foreach (string value in keys)
				{
					if (text.Contains(value))
					{
						return row;
					}
				}
			}
			if (_unmatched.Add(((MBObjectBase)val).StringId ?? "?"))
			{
				Log.Write(string.Concat("culture not in the negotiation table: id '", ((MBObjectBase)val).StringId, "', name '", ((BasicCultureObject)val).Name, "' - treated as neutral"));
			}
		}
		catch
		{
		}
		return null;
	}
}
}
