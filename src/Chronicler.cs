using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Turns the bare record into history as the old chroniclers wrote it - by
	// the year, in prose - and keeps the great deeds a house or realm will be
	// remembered for, long after the lands themselves are lost.
	internal static class Chronicler
	{
		// A stand-in for a culture, by name only, for renaming stored lands.
		private sealed class CultureObjectName
		{
			internal readonly string Name;
			internal CultureObjectName(string n) { Name = n; }
		}

		private static string LandName(CultureObjectName c)
		{
			return LandNameOf(c.Name);
		}

		// ------------------------------------------------------------------
		// deeds of note: "ac:<clan>" and "ak:<kingdom>" = code|day|text joined by \u001e

		private static List<string[]> Deeds(string key)
		{
			string s = Store.Get(key);
			return string.IsNullOrEmpty(s) ? new List<string[]>() : s.Split('\u001e').Select((string x) => x.Split(new char[1] { '|' }, 3)).Where((string[] p) => p.Length == 3).ToList();
		}

		private static void Note(string key, string code, string text)
		{
			if (key == null || !Store.Initialized)
			{
				return;
			}
			List<string[]> list = Deeds(key);
			if (list.Any((string[] p) => p[0] == code))
			{
				return;
			}
			list.Add(new string[3] { code, CourtBehavior.Today().ToString(), text.Replace("|", "/").Replace('\u001e', ' ') });
			Store.Set(key, string.Join("\u001e", list.Select((string[] p) => string.Join("|", p))));
			Log.Write("deed of note: " + key + " - " + text);
		}

		internal static void HouseDeed(Clan c, string code, string text)
		{
			if (c != null && !c.IsBanditFaction)
			{
				Note("ac:" + ((MBObjectBase)c).StringId, code, text);
			}
		}

		internal static void RealmDeed(Kingdom k, string code, string text)
		{
			if (k != null)
			{
				Note("ak:" + ((MBObjectBase)k).StringId, code, text);
			}
		}

		private static void Count(string key, int needed, Action reached)
		{
			int n = Store.GetI(key, 0) + 1;
			Store.SetI(key, n);
			if (n == needed)
			{
				reached();
			}
		}

		internal static void OnBattleWon(Clan c, Kingdom k)
		{
			if (c != null)
			{
				Count("acn:" + ((MBObjectBase)c).StringId + ":fields", 3, () => HouseDeed(c, "fields3", "won three great battles"));
				Count("acn:" + ((MBObjectBase)c).StringId + ":fields10", 10, () => HouseDeed(c, "fields10", "won ten great battles and was never brought to heel"));
			}
			if (k != null)
			{
				Count("akn:" + ((MBObjectBase)k).StringId + ":fields", 10, () => RealmDeed(k, "fields10", "won ten great battles in the field"));
			}
		}

		internal static void OnCityTaken(Clan c, Settlement s)
		{
			if (c == null || s == null || !s.IsTown)
			{
				return;
			}
			Count("acn:" + ((MBObjectBase)c).StringId + ":cities", 3, () => HouseDeed(c, "cities3", "stormed three cities"));
		}

		internal static void OnRulerSlain(Hero victim, Hero killer)
		{
			if (killer == null || killer.Clan == null || victim == null)
			{
				return;
			}
			HouseDeed(killer.Clan, "kingslayer", "slew " + victim.Name + ", a crowned ruler");
		}

		internal static void OnRealmFounded(Kingdom k)
		{
			if (k == null || k.RulingClan == null)
			{
				return;
			}
			HouseDeed(k.RulingClan, "founded:" + ((MBObjectBase)k).StringId, "founded the realm of " + k.Name);
		}

		internal static void OnCrowned(Kingdom k, Clan c)
		{
			if (k != null && c != null)
			{
				HouseDeed(c, "crown:" + ((MBObjectBase)k).StringId, "wore the crown of " + k.Name);
			}
		}

		// Weekly: who holds whole lands? A land is every town and castle of one
		// people (the culture of the place).
		internal static void Weekly(int today)
		{
			if (!Cfg.Histories || !Store.Initialized || today - Store.GetI("acw:last", -9999) < 7)
			{
				return;
			}
			Store.SetI("acw:last", today);
			try
			{
				foreach (IGrouping<string, Settlement> land in Settlement.All.Where((Settlement s) => (s.IsTown || s.IsCastle) && s.Culture != null).GroupBy((Settlement s) => ((MBObjectBase)s.Culture).StringId))
				{
					List<Settlement> all = land.ToList();
					if (all.Count < 3)
					{
						continue;
					}
					string name = LandName(all[0].Culture);
					Clan clan = all[0].OwnerClan;
					if (clan != null && all.All((Settlement s) => s.OwnerClan == clan))
					{
						HouseDeed(clan, "unite:" + land.Key, "held every castle and city of " + name + " at once");
					}
					Kingdom k = all[0].OwnerClan?.Kingdom;
					if (k != null && all.All((Settlement s) => s.OwnerClan != null && s.OwnerClan.Kingdom == k))
					{
						RealmDeed(k, "unite:" + land.Key, "brought all of " + name + " under one crown");
						if (k.RulingClan != null)
						{
							HouseDeed(k.RulingClan, "uniteK:" + land.Key, "united all of " + name + " under its rule");
						}
					}
				}
				foreach (Clan c in Clan.All.Where((Clan x) => !x.IsEliminated && !x.IsBanditFaction && !x.IsMinorFaction))
				{
					if (c.Fiefs.Count >= 10)
					{
						HouseDeed(c, "fiefs10", "held ten fiefs and more");
					}
					try
					{
						if (Dragons.LivingRidersIn(c) >= 3)
						{
							HouseDeed(c, "dragons3", "flew three dragons at once");
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("deedsweekly", "deeds: " + e.Message);
			}
		}

		// "the North", "the Crownlands", or "the lands of the Lyseni".
		internal static string LandName(CultureObject c)
		{
			return LandNameOf((c != null && c.Name != null) ? c.Name.ToString() : "a people");
		}

		private static string LandNameOf(string n)
		{
			n = n.Trim();
			if (n.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
			{
				return "t" + n.Substring(1);
			}
			string[] regions = new string[12] { "North", "Vale", "Reach", "Westerlands", "Riverlands", "Stormlands", "Crownlands", "Iron Islands", "Dorne", "Neck", "Wall", "Beyond the Wall" };
			if (regions.Any((string r) => n.Equals(r, StringComparison.OrdinalIgnoreCase)) || n.EndsWith("lands", StringComparison.OrdinalIgnoreCase) || n.EndsWith("Islands", StringComparison.OrdinalIgnoreCase))
			{
				return (n.Equals("Dorne", StringComparison.OrdinalIgnoreCase) ? "" : "the ") + n;
			}
			return "the lands of the " + n;
		}

		// ------------------------------------------------------------------
		// summaries at the head of the page

		// Many lands united read as one deed, not nine.
		private static List<string> Merge(List<string[]> deeds, string single, string pluralPrefix)
		{
			List<string> lands = deeds.Where((string[] p) => p[0].StartsWith(single)).Select((string[] p) => Land(p[2])).Where((string l) => l != null).ToList();
			List<string> rest = deeds.Where((string[] p) => !p[0].StartsWith(single)).Select((string[] p) => p[2]).ToList();
			if (lands.Count > 0)
			{
				rest.Insert(0, pluralPrefix + JoinLands(lands));
			}
			return rest;
		}

		// "the lands of the Lyseni, the Myrish and the Volantene, and the Crownlands"
		private static string JoinLands(List<string> lands)
		{
			const string p = "the lands of the ";
			List<string> peoples = lands.Where((string l) => l.StartsWith(p)).Select((string l) => "the " + l.Substring(p.Length)).Distinct().ToList();
			List<string> regions = lands.Where((string l) => !l.StartsWith(p)).Distinct().ToList();
			List<string> parts = new List<string>(regions);
			if (peoples.Count > 0)
			{
				peoples[0] = "the lands of " + peoples[0];
				parts.Add(JoinDeeds(peoples));
			}
			return (parts.Count == 1) ? parts[0] : (string.Join(", ", parts.Take(parts.Count - 1)) + ", and " + parts.Last());
		}

		// The land named inside a stored deed ("... of the lands of the Lyseni at once").
		private static string Land(string text)
		{
			foreach (string marker in new string[3] { " of ", "all of ", "unite " })
			{
				int i = text.IndexOf(marker, StringComparison.Ordinal);
				if (i >= 0)
				{
					string t = text.Substring(i + marker.Length);
					foreach (string end in new string[3] { " at once", " under its rule", " under one crown" })
					{
						int j = t.IndexOf(end, StringComparison.Ordinal);
						if (j > 0)
						{
							t = t.Substring(0, j);
						}
					}
					t = t.Replace("the lands of the lands of", "the lands of");
					return Rename(t.Trim());
				}
			}
			return null;
		}

		private static string Rename(string land)
		{
			const string p = "the lands of the ";
			if (land.StartsWith(p))
			{
				string n = land.Substring(p.Length);
				return LandName(new CultureObjectName(n));
			}
			return land;
		}

		private static string JoinDeeds(List<string> d)
		{
			if (d.Count == 1)
			{
				return d[0];
			}
			return string.Join(", ", d.Take(d.Count - 1)) + " and " + d.Last();
		}

		internal static string HouseSummary(Clan c)
		{
			if (c == null)
			{
				return null;
			}
			StringBuilder sb = new StringBuilder();
			string culture = (c.Culture != null) ? c.Culture.Name.ToString() : null;
			sb.Append(c.Name).Append(c.IsEliminated ? " was" : " is").Append(" a house").Append(culture != null ? (" of the " + culture) : "");
			if (!c.IsEliminated)
			{
				if (c.Kingdom != null)
				{
					sb.Append(c.Kingdom.RulingClan == c ? (", and the ruling house of " + c.Kingdom.Name) : (", sworn to " + c.Kingdom.Name));
				}
				if (c.Leader != null)
				{
					sb.Append(". It is led by ").Append(c.Leader.Name);
				}
				int towns = c.Fiefs.Count((Town t) => t.IsTown);
				int castles = c.Fiefs.Count - towns;
				if (c.Fiefs.Count > 0)
				{
					sb.Append(", and holds ").Append(Count(towns, "city", "cities")).Append(towns > 0 && castles > 0 ? " and " : "").Append(castles > 0 ? Count(castles, "castle", "castles") : "");
				}
				else
				{
					sb.Append(", and holds no land of its own");
				}
			}
			sb.Append(".");
			List<string[]> all = Deeds("ac:" + ((MBObjectBase)c).StringId);
			List<string> deeds = Merge(all.Where((string[] p) => !p[0].StartsWith("unite:")).ToList(), "uniteK:", "united ");
			HashSet<string> united = new HashSet<string>(all.Where((string[] p) => p[0].StartsWith("uniteK:")).Select((string[] p) => p[0].Substring(7)));
			List<string> held = all.Where((string[] p) => p[0].StartsWith("unite:") && !united.Contains(p[0].Substring(6))).Select((string[] p) => Land(p[2])).Where((string l) => l != null).ToList();
			if (held.Count > 0)
			{
				deeds.Add("held every castle and city of " + JoinLands(held) + " at once");
			}
			if (deeds.Count > 0)
			{
				sb.Append(" It is remembered as the house that ").Append(JoinDeeds(deeds)).Append(".");
			}
			return sb.ToString();
		}

		internal static string RealmSummary(Kingdom k)
		{
			if (k == null)
			{
				return null;
			}
			StringBuilder sb = new StringBuilder();
			sb.Append(k.Name).Append(k.IsEliminated ? " was" : " is").Append(" a realm");
			if (k.Culture != null)
			{
				sb.Append(" of the ").Append(k.Culture.Name);
			}
			if (!k.IsEliminated)
			{
				if (k.Leader != null)
				{
					sb.Append(", ruled by ").Append(k.Leader.Name).Append(k.RulingClan != null ? (" of " + k.RulingClan.Name) : "");
				}
				sb.Append(", with ").Append(Count(k.Clans.Count((Clan x) => !x.IsEliminated), "house", "houses")).Append(" sworn to it and ")
					.Append(Count(k.Fiefs.Count, "fief", "fiefs"));
			}
			sb.Append(".");
			List<string> deeds = Merge(Deeds("ak:" + ((MBObjectBase)k).StringId), "unite:", "brought under one crown ");
			if (deeds.Count > 0)
			{
				sb.Append(" It is remembered as the realm that ").Append(JoinDeeds(deeds)).Append(".");
			}
			return sb.ToString();
		}

		private static string Count(int n, string one, string many)
		{
			string[] words = new string[11] { "no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
			return ((n <= 10) ? words[n] : n.ToString()) + " " + (n == 1 ? one : many);
		}

		// ------------------------------------------------------------------
		// prose: a paragraph to a year, as the chroniclers wrote

		private static readonly string[] SameYear = new string[5] { "That same year, ", "In the same year ", "Later that year ", "Before the year was out, ", "Afterwards " };
		private static readonly string[] Seasons = new string[4] { "spring", "summer", "autumn", "winter" };

		internal static List<string> Prose(string key, Hero subject)
		{
			List<string> paras = new List<string>();
			List<string[]> raw = Raw(key);
			if (raw.Count == 0)
			{
				return paras;
			}
			int dpy = Math.Max(1, Cfg.DaysPerYear);
			string pronoun = (subject != null) ? (subject.IsFemale ? "she" : "he") : null;
			foreach (IGrouping<int, string[]> year in raw.GroupBy((string[] p) => int.Parse(p[0]) / dpy).OrderByDescending((IGrouping<int, string[]> g) => g.Key))
			{
				StringBuilder sb = new StringBuilder();
				int i = 0;
				foreach (string[] e in year.OrderBy((string[] p) => int.Parse(p[0])))
				{
					int day = int.Parse(e[0]);
					string season = Seasons[Math.Min(3, day % dpy / Math.Max(1, dpy / 4))];
					string text = e[2].TrimEnd('.');
					string lead;
					if (i == 0)
					{
						lead = "In the " + season + " of the year " + year.Key + ", ";
					}
					else if ((day + i) % 3 == 0)
					{
						lead = "In the " + season + ", ";
					}
					else
					{
						lead = SameYear[(day + i) % SameYear.Length];
					}
					sb.Append(lead).Append(Sentence(text, pronoun, i == 0 && subject != null ? subject.FirstName?.ToString() : null)).Append(". ");
					i++;
				}
				paras.Add(sb.ToString().Trim());
			}
			return paras;
		}

		// "Led the victory at X" for a lord becomes "Aegon led the victory at X";
		// "Bennard Stark won the battle..." stays as written.
		private static string Sentence(string text, string pronoun, string firstName)
		{
			if (string.IsNullOrEmpty(text))
			{
				return text;
			}
			string first = text.Split(' ')[0];
			if (pronoun != null && IsVerb(first))
			{
				return (firstName ?? pronoun) + " " + char.ToLowerInvariant(text[0]) + text.Substring(1);
			}
			return IsName(text) ? text : (char.ToLowerInvariant(text[0]) + text.Substring(1));
		}

		private static readonly HashSet<string> Verbs = new HashSet<string>
		{
			"Led", "Fought", "Was", "Shared", "Slew", "Put", "Took", "Lost", "Fell", "Died", "Married", "Bore", "Fathered", "Became", "Founded", "Rode", "Eloped",
			"Came", "Learned", "Grew", "Left", "Spoke", "Squired", "Born", "Won"
		};

		private static bool IsVerb(string w)
		{
			return Verbs.Contains(w);
		}

		private static bool IsName(string text)
		{
			// Lines that open with a name or "The"/"House" keep their capital.
			string first = text.Split(' ')[0];
			return !IsVerb(first) && !new HashSet<string> { "Victory", "Defeat", "Scandal:", "The", "A", "An", "It", "Nothing", "No" }.Contains(first);
		}

		private static List<string[]> Raw(string key)
		{
			string s = (key != null) ? Store.Get(key) : null;
			return string.IsNullOrEmpty(s) ? new List<string[]>() : s.Split('\u001e').Select((string x) => x.Split(new char[1] { '|' }, 3)).Where((string[] p) => p.Length == 3 && int.TryParse(p[0], out int d)).ToList();
		}
	}
}
