using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// The six seats, under the names Westeros uses.
	//
	// Bellum's offices are Marshal, Chancellor, Seneschal, Spymaster and two
	// Advisors. The canonical small council maps onto those six exactly, one
	// for one, with none left over, so this renames what is shown without
	// touching anything underneath: the enum, the save, and every id stay
	// exactly as Bellum wrote them.
	//
	// The mapping is done on the text Bellum hands back rather than on the
	// enum, so a localisation we do not recognise is passed through untouched
	// instead of being renamed wrongly.
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
			string trimmed = original.Trim();
			for (int i = 0; i < Map.GetLength(0); i++)
			{
				if (string.Equals(trimmed, Map[i, 0], StringComparison.OrdinalIgnoreCase))
				{
					return Map[i, 1];
				}
			}
			return original;
		}

		// Whatever your game calls the seat, under our name for it.
		internal static string Pretty(string original)
		{
			string renamed = Rename(original);
			return (renamed == original) ? original : renamed;
		}

		internal static void Patch(Harmony h)
		{
			try
			{
				if (!Cfg.OfficeNames)
				{
					Log.Write("office names left as Bellum writes them");
					return;
				}
				if (_patched || h == null)
				{
					return;
				}
				Type behavior = AccessTools.TypeByName("BellumCivile.Behaviors.PrivyCouncilBehavior");
				Type vm = AccessTools.TypeByName("BellumCivile.UI.VanillaTabs.Kingdoms.Factions.PrivyCouncilOfficeVM");
				int n = 0;
				n += PatchText(h, behavior, "GetLocalizedOfficeName");
				n += PatchText(h, behavior, "GetOfficeName");
				n += PatchString(h, vm, "GetOfficeName");
				_patched = n > 0;
				Log.Write("office names renamed to the Westerosi titles on " + n + " method(s)");
			}
			catch (Exception e)
			{
				Log.Write("renaming the offices failed: " + e.Message);
			}
		}

		private static int PatchText(Harmony h, Type t, string name)
		{
			try
			{
				if (t == null)
				{
					return 0;
				}
				MethodInfo m = AccessTools.Method(t, name, (Type[])null, (Type[])null);
				if (m == null || m.ReturnType != typeof(TextObject))
				{
					return 0;
				}
				h.Patch((MethodBase)m, (HarmonyMethod)null,
					new HarmonyMethod(AccessTools.Method(typeof(Offices), "TextPostfix", (Type[])null, (Type[])null)),
					(HarmonyMethod)null, (HarmonyMethod)null);
				return 1;
			}
			catch (Exception e)
			{
				Log.Write("could not rename via " + name + ": " + e.Message);
				return 0;
			}
		}

		private static int PatchString(Harmony h, Type t, string name)
		{
			try
			{
				if (t == null)
				{
					return 0;
				}
				MethodInfo m = AccessTools.Method(t, name, (Type[])null, (Type[])null);
				if (m == null || m.ReturnType != typeof(string))
				{
					return 0;
				}
				h.Patch((MethodBase)m, (HarmonyMethod)null,
					new HarmonyMethod(AccessTools.Method(typeof(Offices), "StringPostfix", (Type[])null, (Type[])null)),
					(HarmonyMethod)null, (HarmonyMethod)null);
				return 1;
			}
			catch (Exception e)
			{
				Log.Write("could not rename via " + name + ": " + e.Message);
				return 0;
			}
		}

		internal static void TextPostfix(ref TextObject __result)
		{
			try
			{
				if (__result == null)
				{
					return;
				}
				string was = __result.ToString();
				string now = Rename(was);
				if (now != was)
				{
					__result = Styles.Line(now);
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
}
