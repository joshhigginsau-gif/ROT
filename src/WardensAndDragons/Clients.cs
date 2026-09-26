using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace WardensAndDragons;

internal static class Clients
{
	private static bool _tried;

	private static object _behavior;

	private static MethodInfo _can;

	private static MethodInfo _establish;

	private static MethodInfo _isClient;

	private static MethodInfo _getSuzerain;

	private static MethodInfo _end;

	internal static void Reset()
	{
		_tried = false;
		_behavior = null;
	}

	internal static bool Init()
	{
		if (_tried)
		{
			return _behavior != null;
		}
		_tried = true;
		try
		{
			Type type = AccessTools.TypeByName("BellumCivile.Behaviors.ClientKingdomBehavior") ?? AccessTools.TypeByName("BellumCivile.ClientKingdomBehavior");
			if (type == null)
			{
				Log.Write("ClientKingdomBehavior not found - suzerainty disabled");
				return false;
			}
			PropertyInfo propertyInfo = AccessTools.Property(type, "Instance");
			if (propertyInfo != null)
			{
				_behavior = propertyInfo.GetValue(null, null);
			}
			if (_behavior == null)
			{
				Campaign current = Campaign.Current;
				if (current != null)
				{
					MethodInfo[] methods = typeof(Campaign).GetMethods();
					MethodInfo[] array = methods;
					foreach (MethodInfo methodInfo in array)
					{
						if (!(methodInfo.Name != "GetCampaignBehavior") && methodInfo.IsGenericMethodDefinition && methodInfo.GetParameters().Length == 0)
						{
							_behavior = methodInfo.MakeGenericMethod(type).Invoke(current, null);
							break;
						}
					}
				}
			}
			if (_behavior == null)
			{
				_tried = false;
				return false;
			}
			_can = AccessTools.Method(type, "CanEstablishClientKingdom", (Type[])null, (Type[])null);
			_establish = AccessTools.Method(type, "TryEstablishClientKingdom", (Type[])null, (Type[])null);
			_isClient = AccessTools.Method(type, "IsClientKingdom", (Type[])null, (Type[])null);
			_getSuzerain = AccessTools.Method(type, "GetSuzerain", (Type[])null, (Type[])null);
			_end = AccessTools.Method(type, "EndClientStatus", (Type[])null, (Type[])null);
			Log.Write("client kingdom API: can=" + (_can != null) + " establish=" + (_establish != null) + " isClient=" + (_isClient != null) + " end=" + (_end != null));
			return _establish != null;
		}
		catch (Exception ex)
		{
			Log.Write("client init failed: " + ex.Message);
			return false;
		}
	}

	internal static bool IsClient(Kingdom k)
	{
		try
		{
			return Init() && _isClient != null && (bool)_isClient.Invoke(_behavior, new object[1] { k });
		}
		catch
		{
			return false;
		}
	}

	internal static Kingdom SuzerainOf(Kingdom k)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		try
		{
			object obj2;
			if (Init() && _getSuzerain != null)
			{
				object obj = _getSuzerain.Invoke(_behavior, new object[1] { k });
				obj2 = ((!(obj is Kingdom)) ? null : obj);
			}
			else
			{
				obj2 = null;
			}
			return (Kingdom)obj2;
		}
		catch
		{
			return null;
		}
	}

	internal static bool CanEstablish(Kingdom client, Kingdom suzerain, out string report)
	{
		report = null;
		try
		{
			if (!Init() || _can == null)
			{
				report = "client kingdom API unavailable";
				return false;
			}
			object[] array = new object[3] { client, suzerain, null };
			bool result = (bool)_can.Invoke(_behavior, array);
			report = array[2] as string;
			return result;
		}
		catch (Exception ex)
		{
			report = ex.Message;
			return false;
		}
	}

	internal static bool Establish(Kingdom client, Kingdom suzerain, out string report)
	{
		report = null;
		try
		{
			if (!Init() || _establish == null)
			{
				report = "client kingdom API unavailable";
				return false;
			}
			object[] array = new object[4] { client, suzerain, true, null };
			bool result = (bool)_establish.Invoke(_behavior, array);
			report = array[3] as string;
			return result;
		}
		catch (Exception ex)
		{
			report = ex.Message;
			return false;
		}
	}

	internal static bool Release(Kingdom client, out string report)
	{
		report = null;
		try
		{
			if (!Init() || _end == null)
			{
				report = "client kingdom API unavailable";
				return false;
			}
			return (bool)_end.Invoke(_behavior, new object[2] { client, "released by suzerain" });
		}
		catch (Exception ex)
		{
			report = ex.Message;
			return false;
		}
	}

	internal static int Odds(Kingdom theirs, Kingdom mine, List<string> reasons)
	{
		//IL_020e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0218: Expected O, but got Unknown
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Expected O, but got Unknown
		int num = 5;
		try
		{
			float num2 = Math.Max(1f, mine.CurrentTotalStrength);
			float num3 = Math.Max(1f, theirs.CurrentTotalStrength);
			float num4 = num2 / num3;
			int num5 = (int)Math.Min(55f, Math.Max(-25f, (num4 - 1f) * 30f));
			num += num5;
			reasons.Add(((num5 >= 0) ? "+" : "") + num5 + "  your strength is " + num4.ToString("0.0") + "x theirs");
			Hero leader = mine.Leader;
			Hero leader2 = theirs.Leader;
			if (leader != null && leader2 != null)
			{
				int relation = leader2.GetRelation(leader);
				int num6 = Math.Max(-25, Math.Min(25, relation / 4));
				num += num6;
				reasons.Add(string.Concat((num6 >= 0) ? "+" : "", num6, "  ", leader2.Name, " regards you at ", relation));
			}
			int num7 = 0;
			foreach (Kingdom item in (List<Kingdom>)(object)Kingdom.All)
			{
				if (item != null && item != theirs && !item.IsEliminated && theirs.IsAtWarWith((IFaction)item))
				{
					num7++;
				}
			}
			if (num7 > 0)
			{
				int num8 = Math.Min(30, num7 * 12);
				num += num8;
				reasons.Add("+" + num8 + "  they are fighting " + num7 + " war(s) and want shelter");
			}
			if (theirs.IsAtWarWith((IFaction)mine))
			{
				num -= 20;
				reasons.Add("-20  you are at war with them - submission galls");
			}
			int num9 = ((theirs.Fiefs != null) ? ((List<Town>)(object)theirs.Fiefs).Count : 0);
			if (num9 >= 8)
			{
				int num10 = Math.Min(25, (num9 - 7) * 4);
				num -= num10;
				reasons.Add("-" + num10 + "  a realm of " + num9 + " fiefs does not kneel easily");
			}
		}
		catch (Exception ex)
		{
			Log.Write("odds failed: " + ex.Message);
		}
		return Math.Max(3, Math.Min(95, num));
	}
}
