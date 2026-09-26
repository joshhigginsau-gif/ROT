using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace WardensAndDragons;

internal static class Handback
{
	private const string DoneKey = "wad:handback";

	private static readonly Dictionary<string, string> Fallback = new Dictionary<string, string>
	{
		{ "Marshal", "marshal_organize_patrols" },
		{ "Chancellor", "chancellor_appease_nobles" },
		{ "Seneschal", "seneschal_audit_vassals" },
		{ "Spymaster", "spymaster_counter_espionage" },
		{ "FirstAdvisor", "first_advisor_counsel_crown" },
		{ "SecondAdvisor", "second_advisor_counsel_crown" }
	};

	internal static void Run()
	{
		try
		{
			if (Store.Get("wad:handback") == "1")
			{
				return;
			}
			Kingdom val = ((Clan.PlayerClan == null) ? null : Clan.PlayerClan.Kingdom);
			if (val == null || Campaign.Current == null)
			{
				return;
			}
			Type type = AccessTools.TypeByName("BellumCivile.PrivyCouncilOffice");
			Type type2 = AccessTools.TypeByName("BellumCivile.Behaviors.PrivyCouncilBehavior") ?? AccessTools.TypeByName("BellumCivile.PrivyCouncilBehavior");
			if (type == null || type2 == null)
			{
				Store.Set("wad:handback", "1");
				return;
			}
			object obj = Behavior(type2);
			MethodInfo methodInfo = AccessTools.Method(type2, "GetOfficeAssignment", new Type[2]
			{
				typeof(Kingdom),
				type
			}, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(type2, "TrySetOfficeAssignment", (Type[])null, (Type[])null);
			if (obj == null || methodInfo == null || methodInfo2 == null)
			{
				Store.Set("wad:handback", "1");
				return;
			}
			int num = 0;
			foreach (object value2 in Enum.GetValues(type))
			{
				try
				{
					object assignment = methodInfo.Invoke(obj, new object[2] { val, value2 });
					string text = Id(assignment);
					if (!string.IsNullOrEmpty(text) && text.StartsWith("wad_"))
					{
						string text2 = ((value2 == null) ? "" : value2.ToString());
						if (Fallback.TryGetValue(text2, out var value) && Assign(methodInfo2, obj, val, value2, value))
						{
							num++;
							Log.Write("  " + text2 + ": " + text + " -> " + value);
						}
					}
				}
				catch (Exception ex)
				{
					Log.Once("handbackone", "a seat could not be handed back: " + ex.Message);
				}
			}
			Store.Set("wad:handback", "1");
			Log.Write((num <= 0) ? "council handed back to Bellum: nothing of ours was still assigned" : ("council handed back to Bellum: " + num + " seat(s) moved off our duties"));
		}
		catch (Exception ex2)
		{
			Log.Write("handing the council back failed: " + ex2.Message);
			Store.Set("wad:handback", "1");
		}
	}

	private static object Behavior(Type t)
	{
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Campaign), "GetCampaignBehavior", (Type[])null, (Type[])null);
			if (methodInfo != null && methodInfo.IsGenericMethodDefinition)
			{
				return methodInfo.MakeGenericMethod(t).Invoke(Campaign.Current, null);
			}
		}
		catch
		{
		}
		try
		{
			PropertyInfo propertyInfo = AccessTools.Property(typeof(Campaign), "CampaignBehaviorManager");
			object obj2 = ((!(propertyInfo != null)) ? null : propertyInfo.GetValue(Campaign.Current, null));
			IEnumerable enumerable = ((obj2 == null) ? null : (AccessTools.Property(obj2.GetType(), "Behaviors")?.GetValue(obj2, null) as IEnumerable));
			if (enumerable != null)
			{
				foreach (object item in enumerable)
				{
					if (item != null && t.IsInstanceOfType(item))
					{
						return item;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static string Id(object assignment)
	{
		try
		{
			if (assignment == null)
			{
				return null;
			}
			if (assignment is string)
			{
				return (string)assignment;
			}
			PropertyInfo propertyInfo = AccessTools.Property(assignment.GetType(), "Id");
			object obj = ((!(propertyInfo != null)) ? null : propertyInfo.GetValue(assignment, null));
			return (obj == null) ? assignment.ToString() : obj.ToString();
		}
		catch
		{
			return null;
		}
	}

	private static bool Assign(MethodInfo set, object behavior, Kingdom k, object office, string id)
	{
		try
		{
			ParameterInfo[] parameters = set.GetParameters();
			object[] array = new object[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
			{
				Type parameterType = parameters[i].ParameterType;
				if (parameterType.IsByRef)
				{
					array[i] = null;
				}
				else if (parameterType == typeof(Kingdom))
				{
					array[i] = k;
				}
				else if (parameterType == office.GetType())
				{
					array[i] = office;
				}
				else if (parameterType == typeof(string))
				{
					array[i] = id;
				}
				else if (parameterType == typeof(Clan))
				{
					array[i] = Clan.PlayerClan;
				}
				else if (parameterType == typeof(bool))
				{
					string text = (parameters[i].Name ?? "").ToLowerInvariant();
					array[i] = !text.Contains("notif") && !text.Contains("show");
				}
				else
				{
					array[i] = ((!parameterType.IsValueType) ? null : Activator.CreateInstance(parameterType));
				}
			}
			object obj = set.Invoke(behavior, array);
			return !(obj is bool) || (bool)obj;
		}
		catch (Exception ex)
		{
			Log.Once("handbackset", "a seat would not take a Bellum duty: " + ex.Message);
			return false;
		}
	}
}
