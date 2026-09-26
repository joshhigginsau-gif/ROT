using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Localization;

namespace WardensAndDragons;

internal static class Offices
{
	private static readonly string[,] Map = new string[8, 2]
	{
		{ "Marshal", "Master of Ships" },
		{ "Chancellor", "Hand of the King" },
		{ "Seneschal", "Master of Coin" },
		{ "Spymaster", "Master of Whisperers" },
		{ "First Advisor", "Grand Maester" },
		{ "FirstAdvisor", "Grand Maester" },
		{ "Second Advisor", "Master of Laws" },
		{ "SecondAdvisor", "Master of Laws" }
	};

	private static bool _patched;

	internal static string Rename(string original)
	{
		if (!Cfg.OfficeNames || string.IsNullOrEmpty(original))
		{
			return original;
		}
		string a = original.Trim();
		for (int i = 0; i < Map.GetLength(0); i++)
		{
			if (string.Equals(a, Map[i, 0], StringComparison.OrdinalIgnoreCase))
			{
				return Map[i, 1];
			}
		}
		return original;
	}

	internal static string Pretty(string original)
	{
		string text = Rename(original);
		return (!(text == original)) ? text : original;
	}

	internal static void Patch(Harmony h)
	{
		try
		{
			if (!Cfg.OfficeNames)
			{
				Log.Write("office names left as Bellum writes them");
			}
			else if (!_patched && h != null)
			{
				Type t = AccessTools.TypeByName("BellumCivile.Behaviors.PrivyCouncilBehavior");
				Type t2 = AccessTools.TypeByName("BellumCivile.UI.VanillaTabs.Kingdoms.Factions.PrivyCouncilOfficeVM");
				int num = 0;
				num += PatchText(h, t, "GetLocalizedOfficeName");
				num += PatchText(h, t, "GetOfficeName");
				num += PatchString(h, t2, "GetOfficeName");
				_patched = num > 0;
				Log.Write("office names renamed to the Westerosi titles on " + num + " method(s)");
			}
		}
		catch (Exception ex)
		{
			Log.Write("renaming the offices failed: " + ex.Message);
		}
	}

	private static int PatchText(Harmony h, Type t, string name)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		try
		{
			if (t == null)
			{
				return 0;
			}
			MethodInfo methodInfo = AccessTools.Method(t, name, (Type[])null, (Type[])null);
			if (methodInfo == null || methodInfo.ReturnType != typeof(TextObject))
			{
				return 0;
			}
			h.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(AccessTools.Method(typeof(Offices), "TextPostfix", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
			return 1;
		}
		catch (Exception ex)
		{
			Log.Write("could not rename via " + name + ": " + ex.Message);
			return 0;
		}
	}

	private static int PatchString(Harmony h, Type t, string name)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		try
		{
			if (t == null)
			{
				return 0;
			}
			MethodInfo methodInfo = AccessTools.Method(t, name, (Type[])null, (Type[])null);
			if (methodInfo == null || methodInfo.ReturnType != typeof(string))
			{
				return 0;
			}
			h.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(AccessTools.Method(typeof(Offices), "StringPostfix", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
			return 1;
		}
		catch (Exception ex)
		{
			Log.Write("could not rename via " + name + ": " + ex.Message);
			return 0;
		}
	}

	internal static void TextPostfix(ref TextObject __result)
	{
		try
		{
			if (!(__result == (TextObject)null))
			{
				string text = ((object)__result).ToString();
				string text2 = Rename(text);
				if (text2 != text)
				{
					__result = Styles.Line(text2);
				}
			}
		}
		catch
		{
		}
	}

	internal static void StringPostfix(ref string __result)
	{
		try
		{
			__result = Rename(__result);
		}
		catch
		{
		}
	}
}
