using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

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

	internal static readonly string[] All = new string[9] { "Snow", "Stone", "Rivers", "Storm", "Hill", "Flowers", "Sand", "Waters", "Pyke" };

	internal static string Of(Settlement where)
	{
		try
		{
			string text = ((where == null || where.Culture == null) ? "" : ((MBObjectBase)where.Culture).StringId.ToLowerInvariant());
			foreach (KeyValuePair<string, string> item in ByCulture)
			{
				if (text.Contains(item.Key))
				{
					return item.Value;
				}
			}
		}
		catch
		{
		}
		return "Rivers";
	}

	internal static bool IsBaseborn(string surname)
	{
		if (string.IsNullOrEmpty(surname))
		{
			return false;
		}
		string[] all = All;
		foreach (string a in all)
		{
			if (string.Equals(a, surname.Trim(), StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
