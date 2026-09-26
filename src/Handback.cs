using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace WardensAndDragons
{
	// Giving the council back.
	//
	// Up to v1.1.0 this mod registered thirty duties of its own into Bellum
	// Civile's privy council. They are gone now, and that leaves a problem for
	// anybody upgrading: a saved game can have a seat assigned to
	// "wad_whisper_courts", and on load that id resolves to nothing.
	//
	// So this runs once, finds any seat in the player's realm still holding
	// one of ours, and hands it back to the nearest Bellum duty for that
	// office. Then it writes a flag and never runs again.
	//
	// It is deliberately self-contained. The whole council bridge was deleted;
	// this is the last hundred lines of reflection in the mod and it exists
	// only to leave Bellum's save data tidy on the way out.
	internal static class Handback
	{
		private const string DoneKey = "wad:handback";

		// One safe Bellum duty per office, by the office's own enum name.
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
				if (Store.Get(DoneKey) == "1")
				{
					return;
				}
				Kingdom k = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
				if (k == null || Campaign.Current == null)
				{
					// No realm to tidy yet. Leave the flag unwritten so this
					// gets another go once there is one.
					return;
				}

				Type tOffice = AccessTools.TypeByName("BellumCivile.PrivyCouncilOffice");
				Type tBehavior = AccessTools.TypeByName("BellumCivile.Behaviors.PrivyCouncilBehavior")
					?? AccessTools.TypeByName("BellumCivile.PrivyCouncilBehavior");
				if (tOffice == null || tBehavior == null)
				{
					// Bellum is not installed, so there is nothing of ours in
					// anybody's council. Done for good.
					Store.Set(DoneKey, "1");
					return;
				}

				object behavior = Behavior(tBehavior);
				MethodInfo get = AccessTools.Method(tBehavior, "GetOfficeAssignment", new Type[2] { typeof(Kingdom), tOffice }, (Type[])null);
				MethodInfo set = AccessTools.Method(tBehavior, "TrySetOfficeAssignment", (Type[])null, (Type[])null);
				if (behavior == null || get == null || set == null)
				{
					Store.Set(DoneKey, "1");
					return;
				}

				int moved = 0;
				foreach (object office in Enum.GetValues(tOffice))
				{
					try
					{
						object now = get.Invoke(behavior, new object[2] { k, office });
						string id = Id(now);
						if (string.IsNullOrEmpty(id) || !id.StartsWith("wad_"))
						{
							continue;
						}
						string name = (office != null) ? office.ToString() : "";
						string to;
						if (!Fallback.TryGetValue(name, out to))
						{
							continue;
						}
						if (Assign(set, behavior, k, office, to))
						{
							moved++;
							Log.Write("  " + name + ": " + id + " -> " + to);
						}
					}
					catch (Exception oe)
					{
						Log.Once("handbackone", "a seat could not be handed back: " + oe.Message);
					}
				}

				Store.Set(DoneKey, "1");
				Log.Write((moved > 0)
					? ("council handed back to Bellum: " + moved + " seat(s) moved off our duties")
					: "council handed back to Bellum: nothing of ours was still assigned");
			}
			catch (Exception e)
			{
				Log.Write("handing the council back failed: " + e.Message);
				// Written anyway. A migration that throws every load is worse
				// than one that quietly does not finish.
				Store.Set(DoneKey, "1");
			}
		}

		private static object Behavior(Type t)
		{
			try
			{
				MethodInfo m = AccessTools.Method(typeof(Campaign), "GetCampaignBehavior", (Type[])null, (Type[])null);
				if (m != null && m.IsGenericMethodDefinition)
				{
					return m.MakeGenericMethod(t).Invoke(Campaign.Current, null);
				}
			}
			catch
			{
			}
			try
			{
				PropertyInfo p = AccessTools.Property(typeof(Campaign), "CampaignBehaviorManager");
				object mgr = (p != null) ? p.GetValue(Campaign.Current, null) : null;
				IEnumerable all = (mgr != null) ? (AccessTools.Property(mgr.GetType(), "Behaviors")?.GetValue(mgr, null) as IEnumerable) : null;
				if (all != null)
				{
					foreach (object b in all)
					{
						if (b != null && t.IsInstanceOfType(b))
						{
							return b;
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
				PropertyInfo p = AccessTools.Property(assignment.GetType(), "Id");
				object v = (p != null) ? p.GetValue(assignment, null) : null;
				return (v != null) ? v.ToString() : assignment.ToString();
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
				ParameterInfo[] ps = set.GetParameters();
				object[] args = new object[ps.Length];
				for (int i = 0; i < ps.Length; i++)
				{
					Type pt = ps[i].ParameterType;
					if (pt.IsByRef)
					{
						args[i] = null;
					}
					else if (pt == typeof(Kingdom))
					{
						args[i] = k;
					}
					else if (pt == office.GetType())
					{
						args[i] = office;
					}
					else if (pt == typeof(string))
					{
						args[i] = id;
					}
					else if (pt == typeof(Clan))
					{
						args[i] = Clan.PlayerClan;
					}
					else if (pt == typeof(bool))
					{
						// ignoreCooldown true: this is a repair, not a move the
						// player made, and it must not be refused.
						string pn = (ps[i].Name ?? "").ToLowerInvariant();
						args[i] = !(pn.Contains("notif") || pn.Contains("show"));
					}
					else
					{
						args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
					}
				}
				object ok = set.Invoke(behavior, args);
				return !(ok is bool) || (bool)ok;
			}
			catch (Exception e)
			{
				Log.Once("handbackset", "a seat would not take a Bellum duty: " + e.Message);
				return false;
			}
		}
	}
}
