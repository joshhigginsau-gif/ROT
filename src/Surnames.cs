using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;

namespace WardensAndDragons
{
	// The name a baseborn child is given, by where they were got.
	//
	// This is not decoration and it is not ours alone. RoT Dynasty &
	// Succession decides whether a hero is baseborn by reading their SURNAME
	// against exactly this list - it has no register-a-bastard API, and the
	// name is the whole handshake. So these nine spellings have to match RoT's
	// table character for character, or its encyclopedia will call one of our
	// children trueborn while our own court screen calls them baseborn.
	//
	// The upside is that it works in both directions for free: rename a child
	// off this list when you acknowledge them, and RoT reports them as
	// Legitimized on its own, because it remembers they once read as baseborn.
	internal static class Surnames
	{
		private static readonly List<KeyValuePair<string, string>> ByCulture = new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string>("north", "Snow"),
			new KeyValuePair<string, string>("stark", "Snow"),
			new KeyValuePair<string, string>("wildling", "Snow"),
			new KeyValuePair<string, string>("vale", "Stone"),
			new KeyValuePair<string, string>("arryn", "Stone"),
			new KeyValuePair<string, string>("riverland", "Rivers"),
			new KeyValuePair<string, string>("tully", "Rivers"),
			new KeyValuePair<string, string>("storm", "Storm"),
			new KeyValuePair<string, string>("baratheon", "Storm"),
			new KeyValuePair<string, string>("westerland", "Hill"),
			new KeyValuePair<string, string>("lannister", "Hill"),
			new KeyValuePair<string, string>("reach", "Flowers"),
			new KeyValuePair<string, string>("tyrell", "Flowers"),
			new KeyValuePair<string, string>("dorne", "Sand"),
			new KeyValuePair<string, string>("martell", "Sand"),
			new KeyValuePair<string, string>("crownland", "Waters"),
			new KeyValuePair<string, string>("targaryen", "Waters"),
			new KeyValuePair<string, string>("valyria", "Waters"),
			new KeyValuePair<string, string>("dragonstone", "Waters"),
			new KeyValuePair<string, string>("iron", "Pyke"),
			new KeyValuePair<string, string>("greyjoy", "Pyke")
		};

		internal const string Default = "Rivers";

		// The nine RoT knows. Used to tell whether a name still reads as
		// baseborn after we have renamed somebody.
		internal static readonly string[] All = new string[9]
		{
			"Snow", "Stone", "Rivers", "Storm", "Hill", "Flowers", "Sand", "Waters", "Pyke"
		};

		internal static string Of(Settlement where)
		{
			try
			{
				string culture = (where != null && where.Culture != null)
					? where.Culture.StringId.ToLowerInvariant()
					: "";
				foreach (KeyValuePair<string, string> row in ByCulture)
				{
					if (culture.Contains(row.Key))
					{
						return row.Value;
					}
				}
			}
			catch
			{
			}
			return Default;
		}

		internal static bool IsBaseborn(string surname)
		{
			if (string.IsNullOrEmpty(surname))
			{
				return false;
			}
			foreach (string s in All)
			{
				if (string.Equals(s, surname.Trim(), StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}
}
