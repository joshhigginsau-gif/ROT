using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Titles
{
	internal const string RiderStyle = "Dragon Rider";

	private static readonly string[] Lesser = new string[6] { "Lord ", "Lady ", "Ser ", "Maester ", "Septon ", "Septa " };

	private static readonly string[] Royal = new string[7] { "King ", "Queen ", "Prince ", "Princess ", "Khal ", "Khaleesi ", "High King " };

	[ThreadStatic]
	private static bool _busy;

	private static readonly Dictionary<string, string> _plain = new Dictionary<string, string>();

	private static readonly Dictionary<string, TextObject> _titled = new Dictionary<string, TextObject>();

	private static bool _patched;

	internal static void Invalidate()
	{
		_plain.Clear();
		_titled.Clear();
	}

	internal static void Reset()
	{
		Invalidate();
	}

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
				string text = Styles.Of(clan);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
		}
		if (Cfg.TitleDragonriders && Dragons.IsRider(h))
		{
			return "Dragon Rider";
		}
		return null;
	}

	internal static string Titled(Hero h)
	{
		if (h == null)
		{
			return "";
		}
		return ((object)h.Name).ToString();
	}

	internal static string Compose(string style, string plain)
	{
		if (string.IsNullOrEmpty(style) || plain == null)
		{
			return null;
		}
		string text = plain.TrimStart(' ', '\t', '\u200b', '\u200e', '\u200f', '\ufeff');
		if (text.Length == 0)
		{
			return null;
		}
		if (text.IndexOf(style, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return null;
		}
		if (Cfg.TitleSuffix)
		{
			return text + ", " + style;
		}
		string[] royal = Royal;
		foreach (string value in royal)
		{
			if (text.StartsWith(value, StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
		}
		bool flag = true;
		int num = 0;
		while (flag && num++ < 4)
		{
			flag = false;
			string[] lesser = Lesser;
			foreach (string text2 in lesser)
			{
				if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
				{
					text = text.Substring(text2.Length).TrimStart(Array.Empty<char>());
					flag = true;
					break;
				}
			}
		}
		string[] lesser2 = Lesser;
		foreach (string text3 in lesser2)
		{
			string text4 = ", " + text3.TrimEnd(Array.Empty<char>());
			if (text.EndsWith(text4, StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(0, text.Length - text4.Length).TrimEnd(Array.Empty<char>());
				break;
			}
		}
		return (text.Length != 0) ? (style + " " + text) : null;
	}

	internal static void Patch(Harmony h)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Expected O, but got Unknown
		try
		{
			if (!Cfg.Titles)
			{
				Log.Write("name titles are off in config");
			}
			else if (!_patched && h != null)
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(Hero), "get_Name", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Log.Write("titles: Hero.get_Name not found - names left alone");
					return;
				}
				HarmonyMethod val = new HarmonyMethod(AccessTools.Method(typeof(Titles), "NamePostfix", (Type[])null, (Type[])null));
				h.Patch((MethodBase)methodInfo, (HarmonyMethod)null, val, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched = true;
				Log.Write("name titles patched onto Hero.Name (wardens=" + Cfg.TitleWardens + ", riders=" + Cfg.TitleDragonriders + ")");
			}
		}
		catch (Exception ex)
		{
			Log.Write("titles patch failed: " + ex.Message);
		}
	}

	internal static void NamePostfix(Hero __instance, ref TextObject __result)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d1: Expected O, but got Unknown
		if (_busy || __instance == null || __result == (TextObject)null || !Store.Initialized)
		{
			return;
		}
		_busy = true;
		try
		{
			string stringId = ((MBObjectBase)__instance).StringId;
			if (string.IsNullOrEmpty(stringId))
			{
				return;
			}
			string text = ((object)__result).ToString();
			if (_plain.TryGetValue(stringId, out var value) && value == text && _titled.TryGetValue(stringId, out var value2))
			{
				if (value2 != (TextObject)null)
				{
					__result = value2;
				}
				return;
			}
			_plain[stringId] = text;
			string style = StyleOf(__instance);
			string text2 = Compose(style, text);
			if (text2 == null)
			{
				_titled[stringId] = null;
				return;
			}
			TextObject val = new TextObject(text2, (Dictionary<string, object>)null);
			_titled[stringId] = val;
			__result = val;
		}
		catch
		{
		}
		finally
		{
			_busy = false;
		}
	}
}
