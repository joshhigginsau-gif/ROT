using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace WardensAndDragons;

internal static class Laws
{
	private static bool _looked;

	private static Type _helper;

	private static MethodInfo _typeForClan;

	private static MethodInfo _lawName;

	private static MethodInfo _line;

	private static bool _unlocked;

	private static bool _listening;

	private static Hero _legal;

	private static int _legalDay = -9999;

	internal static bool Ready
	{
		get
		{
			Init();
			return _helper != null;
		}
	}

	internal static void Reset()
	{
		_looked = false;
		_helper = null;
		_legal = null;
		_legalDay = -9999;
	}

	private static void Init()
	{
		if (_looked)
		{
			return;
		}
		_looked = true;
		try
		{
			_helper = AccessTools.TypeByName("BellumCivile.SuccessionLawHelper");
			if (_helper == null)
			{
				return;
			}
			MethodInfo[] methods = _helper.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (methodInfo.Name == "GetSuccessionTypeForClan" && parameters.Length == 1 && _typeForClan == null)
				{
					_typeForClan = methodInfo;
				}
				else if (methodInfo.Name == "GetSuccessionLawName" && parameters.Length == 1 && _lawName == null)
				{
					_lawName = methodInfo;
				}
				else if (methodInfo.Name == "GetOrderedSuccessionLine" && parameters.Length == 1 && _line == null)
				{
					_line = methodInfo;
				}
			}
			Log.Write("succession law: helper=" + (_helper != null) + " type=" + (_typeForClan != null) + " name=" + (_lawName != null) + " line=" + (_line != null));
		}
		catch (Exception ex)
		{
			Log.Write("reading the succession law failed: " + ex.Message);
		}
	}

	internal static string Name()
	{
		try
		{
			Init();
			if (_typeForClan == null || _lawName == null || Clan.PlayerClan == null)
			{
				return null;
			}
			object obj = _typeForClan.Invoke(null, new object[1] { Clan.PlayerClan });
			return _lawName.Invoke(null, new object[1] { obj })?.ToString();
		}
		catch
		{
			return null;
		}
	}

	internal static Hero Legal()
	{
		try
		{
			int num = CourtBehavior.Today();
			if (_legalDay == num && (_legal == null || _legal.IsAlive))
			{
				return _legal;
			}
			_legalDay = num;
			_legal = LegalUncached();
			return _legal;
		}
		catch
		{
			return null;
		}
	}

	private static Hero LegalUncached()
	{
		try
		{
			Init();
			if (_line == null || Clan.PlayerClan == null)
			{
				return null;
			}
			if (!(_line.Invoke(null, new object[1] { Clan.PlayerClan }) is IEnumerable enumerable))
			{
				return null;
			}
			foreach (object item in enumerable)
			{
				Hero val = (Hero)((item is Hero) ? item : null);
				if (val != null && val.IsAlive)
				{
					return val;
				}
				if (item != null)
				{
					PropertyInfo propertyInfo = item.GetType().GetProperty("Hero") ?? item.GetType().GetProperty("Candidate");
					Hero val2 = ((propertyInfo == null) ? null : (propertyInfo.GetValue(item) as Hero));
					if (val2 != null && val2.IsAlive)
					{
						return val2;
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("legalheir", "reading the legal heir failed: " + ex.Message);
		}
		return null;
	}

	internal static bool Lawful()
	{
		Hero val = Succession.Named();
		Hero val2 = Legal();
		return val2 == null || val == null || val == val2;
	}

	internal static void Unlock(Harmony h)
	{
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		try
		{
			if (!Cfg.UnlockHeir)
			{
				Log.Write("heir choice left to your culture's law");
			}
			else
			{
				if (_unlocked || h == null)
				{
					return;
				}
				Type type = AccessTools.TypeByName("BellumCivile.SuccessionLawHelper");
				if (type == null)
				{
					Log.Write("heir choice: Bellum's succession helper was not found, so nothing was changed");
					return;
				}
				int num = 0;
				MethodInfo methodInfo = AccessTools.Method(type, "TryResolveLegalPlayerHeir", (Type[])null, (Type[])null);
				if (methodInfo != null)
				{
					h.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(AccessTools.Method(typeof(Laws), "NoLegalHeir", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
					num++;
					Log.Write("  answered TryResolveLegalPlayerHeir: the law names nobody");
				}
				MethodInfo methodInfo2 = AccessTools.Method(type, "CanConfirmSelectedPlayerHeir", (Type[])null, (Type[])null);
				if (methodInfo2 != null)
				{
					h.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(AccessTools.Method(typeof(Laws), "AnyHeirConfirms", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null);
					num++;
					Log.Write("  answered CanConfirmSelectedPlayerHeir: any of your blood may be named");
				}
				_unlocked = num > 0;
				Log.Write((!_unlocked) ? "heir choice: Bellum's law helper had neither method, so nothing was changed" : ("heir choice unlocked: " + num + " answer(s) given to Bellum's law. Every adult of your house is selectable; naming against the law costs you instead."));
			}
		}
		catch (Exception ex)
		{
			Log.Write("unlocking the heir choice failed: " + ex.Message);
		}
	}

	internal static void NoLegalHeir(ref bool __result)
	{
		if (Cfg.UnlockHeir)
		{
			__result = false;
		}
	}

	internal static void AnyHeirConfirms(ref bool __result, Hero selectedHeir, ref Hero legalHeir)
	{
		if (Cfg.UnlockHeir)
		{
			__result = true;
			if (selectedHeir != null)
			{
				legalHeir = selectedHeir;
			}
		}
	}

	internal static void Listen(Harmony h)
	{
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Expected O, but got Unknown
		try
		{
			if (!_listening && h != null)
			{
				MethodInfo methodInfo = AccessTools.Method("SandBox.CampaignBehaviors.HeirSelectionCampaignBehavior:OnHeirSelectionOver", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Log.Write("heir screen: its handler was not found, so the name on file will be used instead");
					return;
				}
				h.Patch((MethodBase)methodInfo, new HarmonyMethod(AccessTools.Method(typeof(Laws), "HeirChosen", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_listening = true;
				Log.Write("heir screen: listening for your answer, so it survives the death that follows it");
			}
		}
		catch (Exception ex)
		{
			Log.Write("listening to the heir screen failed: " + ex.Message);
		}
	}

	internal static void HeirChosen(Hero selectedHeir)
	{
		if (Cfg.UnlockHeir)
		{
			Succession.Remember(selectedHeir);
		}
	}

	internal static float Penalty()
	{
		try
		{
			if (!Cfg.UnlockHeir || Lawful())
			{
				return 0f;
			}
			return Cfg.UnlawfulHeirPenalty;
		}
		catch
		{
			return 0f;
		}
	}

	internal static string Reading()
	{
		string text = Name();
		if (string.IsNullOrEmpty(text))
		{
			return null;
		}
		Hero val = Legal();
		string text2 = "Your culture's law is " + text + ".";
		if (val != null)
		{
			string text3 = text2;
			text2 = string.Concat(text3, " By it the seat belongs to ", val.Name, ".");
		}
		if (!Lawful())
		{
			text2 += "\n  You have named someone else, and every house in the realm knows it.";
		}
		return text2;
	}
}
