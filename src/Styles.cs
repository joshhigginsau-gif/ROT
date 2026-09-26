using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
internal static class Styles
{
	private static readonly string[,] Map = new string[36, 2]
	{
		{ "night's watch", "Lord Commander of the Night's Watch" },
		{ "free folk", "King-Beyond-the-Wall" },
		{ "skagos", "Lord of Skagos" },
		{ "iron islands", "Lord Reaper of Pyke" },
		{ "pyke", "Lord Reaper of Pyke" },
		{ "north", "Warden of the North" },
		{ "vale", "Warden of the East" },
		{ "arryn", "Warden of the East" },
		{ "westerlands", "Warden of the West" },
		{ "casterly", "Warden of the West" },
		{ "reach", "Warden of the South" },
		{ "riverlands", "Lord Paramount of the Trident" },
		{ "trident", "Lord Paramount of the Trident" },
		{ "stormlands", "Lord Paramount of the Stormlands" },
		{ "dorne", "Prince of Dorne" },
		{ "dragonstone", "Lord of Dragonstone" },
		{ "valyria", "Dragonlord of Valyria" },
		{ "volantis", "Triarch of Volantis" },
		{ "braavos", "Sealord of Braavos" },
		{ "pentos", "Prince of Pentos" },
		{ "tyrosh", "Archon of Tyrosh" },
		{ "lys", "Magister of Lys" },
		{ "myr", "Magister of Myr" },
		{ "qohor", "Magister of Qohor" },
		{ "lorath", "Magister of Lorath" },
		{ "norvos", "High Priest of Norvos" },
		{ "meereen", "Great Master of Meereen" },
		{ "yunkai", "Wise Master of Yunkai" },
		{ "astapor", "Good Master of Astapor" },
		{ "slaver", "Great Master of Slaver's Bay" },
		{ "qarth", "Pureborn of Qarth" },
		{ "sarnor", "High King of Sarnor" },
		{ "khalasar", "Khal of the Dothraki Sea" },
		{ "dothraki", "Khal of the Dothraki Sea" },
		{ "yi ti", "Prince of the Golden Empire" },
		{ "ibben", "Lord of Ibben" }
	};

	internal static string CanonicalFor(string realmName)
	{
		if (string.IsNullOrEmpty(realmName))
		{
			return null;
		}
		string text = realmName.ToLowerInvariant();
		for (int i = 0; i < Map.GetLength(0); i++)
		{
			if (text.Contains(Map[i, 0]))
			{
				return Map[i, 1];
			}
		}
		return null;
	}

	internal static string Of(Clan c)
	{
		return (c != null) ? Store.Get("style:" + ((MBObjectBase)c).StringId) : null;
	}

	internal static void Set(Clan c, string style)
	{
		if (c != null)
		{
			Store.Set("style:" + ((MBObjectBase)c).StringId, (!string.IsNullOrEmpty(style)) ? style : null);
			Titles.Invalidate();
			if (!string.IsNullOrEmpty(style))
			{
				Store.AddDeed(string.Concat(Standing.Date(), "  ", c.Name, " styled ", style, "."));
			}
		}
	}

	internal static string Titled(Clan c)
	{
		if (c == null)
		{
			return "";
		}
		string text = Of(c);
		return (text != null) ? string.Concat(c.Name, ", ", text) : ((object)c.Name).ToString();
	}

	// A tooltip or label built from runtime text.
	//
	// Runtime text belongs in a VARIABLE, never concatenated into the format
	// string. new TextObject("{=!}" + settlement.Name) makes any brace inside
	// that name a live token of the outer TextObject, and the player is shown
	// a raw {SETTLEMENT} where a name should be - which is exactly what
	// happens when an inner name failed to resolve, so the one case that most
	// needed to read clearly was the case that broke.
	internal static TextObject Line(string text)
	{
		TextObject t = new TextObject("{=!}{WAD_LINE}", (Dictionary<string, object>)null);
		t.SetTextVariable("WAD_LINE", text ?? "");
		return t;
	}
}
}
