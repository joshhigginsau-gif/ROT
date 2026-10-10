using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;

namespace WardensAndDragons
{
	// Realm of Thrones' own duel on the field, borrowed.
	//
	// RoT stages duels on the battlefield outside a settlement (the "Duels"
	// menu in an encounter), and that mission is known to work on this
	// install. Its opener is private, so it is reached by reflection, and its
	// result is read back from the behaviour afterwards. RoT's own post-duel
	// menu only opens when its _duelStarted flag is set, which is never
	// touched here.
	internal static class RotDuel
	{
		private static bool _looked;
		private static object _instance;
		private static MethodInfo _open;
		private static FieldInfo _result;
		private static MethodInfo _reset;

		private static void Look()
		{
			if (_looked && _instance != null)
			{
				return;
			}
			_looked = true;
			try
			{
				Type t = AccessTools.TypeByName("ROT.CampaignBehaviors.ROTDuelsBehavior");
				if (t == null)
				{
					Log.Write("rot duel: RoT's duels were not found");
					return;
				}
				FieldInfo inst = AccessTools.Field(t, "Instance");
				_instance = (inst != null) ? inst.GetValue(null) : null;
				_open = AccessTools.Method(t, "OpenDuelMission", new Type[5] { typeof(string), typeof(CharacterObject), typeof(bool), typeof(bool), typeof(bool) });
				_result = AccessTools.Field(t, "_duelFightResult");
				_reset = AccessTools.Method(t, "ResetDuelResult");
				Log.Write("rot duel: behaviour=" + (_instance != null) + " open=" + (_open != null) + " result=" + (_result != null) + " reset=" + (_reset != null));
			}
			catch (Exception e)
			{
				Log.Write("rot duel: looking for RoT's duels failed: " + e.Message);
			}
		}

		internal static bool Available
		{
			get
			{
				Look();
				return _instance != null && _open != null && _result != null && _reset != null;
			}
		}

		// The battlefield where you stand, with RoT's own substitute for the
		// few scenes it knows are broken.
		internal static string Scene()
		{
			string scene = null;
			try
			{
				CampaignVec2 at = MobileParty.MainParty.Position;
				MapPatchData patch = Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(in at);
				scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, false);
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(scene) || scene == "battle_terrain_biome_030" || scene == "battle_terrain_biome_053" || scene == "battle_terrain_biome_088")
			{
				scene = "battle_terrain_biome_065";
			}
			return scene;
		}

		internal static bool Open(Hero foe, out string why, bool mounted = false, string owner = "parley")
		{
			why = null;
			if (!Available)
			{
				why = "RoT's duels are not available";
				return false;
			}
			try
			{
				_reset.Invoke(_instance, null);
				Store.Set("rd:owner", owner);
				string scene = Scene();
				Log.Write("rot duel: " + foe.Name + " on " + scene + (mounted ? " (mounted)" : ""));
				_open.Invoke(null, new object[5] { scene, foe.CharacterObject, mounted, true, false });
				return true;
			}
			catch (Exception e)
			{
				why = "RoT's duel would not open: " + ((e.InnerException != null) ? e.InnerException.Message : e.Message);
				Log.Write("rot duel: opening failed: " + e);
				return false;
			}
		}

		// After the mission: true if RoT recorded a result (and clears it).
		internal static bool TakeResult(out bool won, string owner = "parley")
		{
			won = false;
			if (!Available || (Store.Get("rd:owner") ?? "parley") != owner)
			{
				return false;
			}
			try
			{
				int v = Convert.ToInt32(_result.GetValue(_instance));
				if (v == 0)
				{
					return false;
				}
				won = v == 1;
				_reset.Invoke(_instance, null);
				Store.Set("rd:owner", null);
				return true;
			}
			catch (Exception e)
			{
				Log.Write("rot duel: reading the result failed: " + e.Message);
				return false;
			}
		}
	}
}
