using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Titles in front of a name.
	//
	// A warden wears the style his house was granted - "Warden of the North
	// Eddard Stark" - and only the head of the house wears it. Anyone bonded
	// to a dragon is a Dragon Rider. Both are read from state we already
	// keep, so nothing new is stored and nothing is written to the save.
	//
	// This hangs off Hero.Name, which is the one place every surface reads
	// from: conversation, the encyclopedia, party lists, notifications. The
	// name in the save file is untouched - only what is displayed changes.
	internal static class Titles
	{
		internal const string RiderStyle = "Dragon Rider";

		// ROT names its heroes with a rank already - "Lord Daemon Targaryen",
		// "King Lucerys Velaryon". Putting a style in front of that reads
		// badly, so a lesser rank is replaced by the style and a royal one
		// keeps the hero as he is: a king is not styled Dragon Rider.
		private static readonly string[] Lesser = { "Lord ", "Lady ", "Ser ", "Maester ", "Septon ", "Septa " };

		private static readonly string[] Royal = { "King ", "Queen ", "Prince ", "Princess ", "Khal ", "Khaleesi ", "High King " };

		[ThreadStatic]
		private static bool _busy;

		// heroId -> the plain name we last saw, and the titled one to hand back.
		private static readonly Dictionary<string, string> _plain = new Dictionary<string, string>();

		private static readonly Dictionary<string, TextObject> _titled = new Dictionary<string, TextObject>();

		private static bool _patched;

		// Called whenever a style is granted or revoked, or a dragon changes hands.
		internal static void Invalidate()
		{
			_plain.Clear();
			_titled.Clear();
		}

		internal static void Reset()
		{
			Invalidate();
		}

		// The style this hero wears, or null. Wardens outrank riders: a warden
		// who also rides is styled by his realm, not his dragon.
		internal static string StyleOf(Hero h)
		{
			if (h == null || !h.IsAlive)
			{
				return null;
			}
			if (Cfg.TitleWardens)
			{
				Clan clan = h.Clan;
				if (clan != null && clan.Leader == h)
				{
					string style = Styles.Of(clan);
					if (!string.IsNullOrEmpty(style))
					{
						return style;
					}
				}
			}
			if (Cfg.TitleDragonriders && Dragons.IsRider(h))
			{
				return RiderStyle;
			}
			return null;
		}

		// What the court would call this hero, titled if they have one.
		internal static string Titled(Hero h)
		{
			if (h == null)
			{
				return "";
			}
			return h.Name.ToString();
		}

		// The styled name, or null if this hero should be left alone.
		internal static string Compose(string style, string plain)
		{
			if (string.IsNullOrEmpty(style) || plain == null)
			{
				return null;
			}
			// ROT prefixes some names with a zero-width space.
			string name = plain.TrimStart(' ', '\t', '​', '‎', '‏', '﻿');
			if (name.Length == 0)
			{
				return null;
			}
			if (name.IndexOf(style, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return null;
			}
			if (Cfg.TitleSuffix)
			{
				return name + ", " + style;
			}
			foreach (string r in Royal)
			{
				if (name.StartsWith(r, StringComparison.OrdinalIgnoreCase))
				{
					// A king outranks anything we would put in front of him.
					return null;
				}
			}
			// ROT doubles the rank on some heroes - "Lord Lord Forrest Frey" -
			// so strip until none is left rather than once.
			bool stripped = true;
			int guard = 0;
			while (stripped && guard++ < 4)
			{
				stripped = false;
				foreach (string r in Lesser)
				{
					if (name.StartsWith(r, StringComparison.OrdinalIgnoreCase))
					{
						name = name.Substring(r.Length).TrimStart();
						stripped = true;
						break;
					}
				}
			}
			// ROT also writes some the other way round: "Baela Targaryen, Lady".
			foreach (string r in Lesser)
			{
				string tail = ", " + r.TrimEnd();
				if (name.EndsWith(tail, StringComparison.OrdinalIgnoreCase))
				{
					name = name.Substring(0, name.Length - tail.Length).TrimEnd();
					break;
				}
			}
			return (name.Length == 0) ? null : (style + " " + name);
		}

		internal static void Patch(Harmony h)
		{
			try
			{
				if (!Cfg.Titles)
				{
					Log.Write("name titles are off in config");
					return;
				}
				if (_patched || h == null)
				{
					return;
				}
				MethodInfo target = AccessTools.Method(typeof(Hero), "get_Name", (Type[])null, (Type[])null);
				if (target == null)
				{
					Log.Write("titles: Hero.get_Name not found - names left alone");
					return;
				}
				HarmonyMethod post = new HarmonyMethod(AccessTools.Method(typeof(Titles), "NamePostfix", (Type[])null, (Type[])null));
				h.Patch((MethodBase)target, (HarmonyMethod)null, post, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched = true;
				Log.Write("name titles patched onto Hero.Name (wardens=" + Cfg.TitleWardens + ", riders=" + Cfg.TitleDragonriders + ")");
			}
			catch (Exception e)
			{
				Log.Write("titles patch failed: " + e.Message);
			}
		}

		// Runs on every read of Hero.Name, so it does as little as it can:
		// a dictionary hit in the common case, and the real work only when a
		// hero is seen for the first time since the last invalidation.
		internal static void NamePostfix(Hero __instance, ref TextObject __result)
		{
			if (_busy || __instance == null || __result == null || !Store.Initialized)
			{
				return;
			}
			_busy = true;
			try
			{
				string id = ((MBObjectBase)__instance).StringId;
				if (string.IsNullOrEmpty(id))
				{
					return;
				}
				string plain = __result.ToString();
				string seen;
				if (_plain.TryGetValue(id, out seen) && seen == plain)
				{
					TextObject cached;
					if (_titled.TryGetValue(id, out cached))
					{
						if (cached != null)
						{
							__result = cached;
						}
						return;
					}
				}
				_plain[id] = plain;
				string style = StyleOf(__instance);
				string text = Compose(style, plain);
				// The white cloak reads after the name, the way the books
				// write it: "Ser Criston Cole of the Kingsguard".
				string cloak = Guard.StyleOf(__instance);
				if (cloak != null && plain.IndexOf(cloak.TrimStart(',', ' '), StringComparison.OrdinalIgnoreCase) < 0)
				{
					text = plain.TrimStart(' ', '\t', '\u200B') + cloak;
				}
				if (text == null)
				{
					_titled[id] = null;
					return;
				}
				TextObject titled = new TextObject(text, (Dictionary<string, object>)null);
				_titled[id] = titled;
				__result = titled;
			}
			catch
			{
				// A name is never worth a crash.
			}
			finally
			{
				_busy = false;
			}
		}
	}
}
