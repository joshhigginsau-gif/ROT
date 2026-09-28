using System;
using System.Collections.Generic;
using System.Linq;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;

namespace WardensAndDragons
{
	// Single combat on the ground between the armies.
	//
	// The scene is the field-battle scene for the spot where your siege camp
	// stands - the one any field battle there would load - and the mission is
	// opened under the name of vanilla's camp scene, whose screens give you
	// your weapons, your health, target lock and the spectator camera, with
	// no arena crowd and nothing that needs a town around you.
	internal sealed class FieldDuel : MissionLogic
	{
		private readonly CharacterObject _foe;
		private readonly float _health;
		private Agent _me;
		private Agent _them;
		private readonly List<CharacterObject> _fallen = new List<CharacterObject>();
		private bool _placed;
		private bool _ended;
		private BasicMissionTimer _endTimer;
		private static Action<bool, List<CharacterObject>> _onEnd;
		private static string _scene = "";

		// Set when no ground could be found: the duel is then decided on
		// skill once you are back on the campaign map.
		internal static bool Failed;

		private FieldDuel(CharacterObject foe, float health, Action<bool, List<CharacterObject>> onEnd)
		{
			_foe = foe;
			_health = health;
			_onEnd = onEnd;
		}

		internal static bool Open(CharacterObject foe, float health, Action<bool, List<CharacterObject>> onEnd, out string why)
		{
			why = null;
			Failed = false;
			try
			{
				CampaignVec2 at = MobileParty.MainParty.Position;
				MapPatchData patch = Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(in at);
				string scene = Campaign.Current.Models.SceneModel.GetBattleSceneForMapPatch(patch, false);
				if (string.IsNullOrEmpty(scene))
				{
					why = "there is no battlefield here";
					return false;
				}
				_scene = scene;
				Log.Write("field duel: scene " + scene);
				MissionState.OpenNew("Camp", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, (DecalAtlasGroup)2),
					(InitializeMissionBehaviorsDelegate)((Mission mission) => new MissionBehavior[]
					{
						new MissionOptionsComponent(),
						new FieldDuel(foe, health, onEnd),
						new MissionFacialAnimationHandler(),
						new MissionAgentPanicHandler(),
						new AgentHumanAILogic(),
						new ArenaAgentStateDeciderLogic(),
						new VisualTrackerMissionBehavior(),
						new CampaignMissionComponent(),
						new EquipmentControllerLeaveLogic()
					}), true, true);
				return true;
			}
			catch (Exception e)
			{
				why = "the field could not be opened: " + e.Message;
				Log.Write("opening the field duel failed: " + e);
				return false;
			}
		}

		public override void AfterStart()
		{
			_ended = false;
			_placed = false;
			_endTimer = new BasicMissionTimer();
			Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			CultureObject them = (_foe != null) ? _foe.Culture : null;
			Mission.Teams.Add(BattleSideEnum.Attacker, (them != null) ? them.Color : 0xFF8B0000u, (them != null) ? them.Color2 : 0xFF000000u, null, true, false, true);
			Mission.PlayerTeam = Mission.Teams.Defender;
		}

		public override void OnMissionTick(float dt)
		{
			if (!_placed)
			{
				_placed = true;
				try
				{
					Place();
				}
				catch (Exception e)
				{
					Log.Write("placing the field duel failed: " + e);
					GiveUp("placement failed");
				}
				return;
			}
			if (_ended && _endTimer != null && _endTimer.ElapsedTime > 4f)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_duel_has_ended", null), 0, null, null, "");
				_endTimer.Reset();
			}
		}

		private void GiveUp(string why)
		{
			Log.Write("field duel: " + why + "; it will be decided on skill instead");
			Failed = true;
			_ended = true;
			_onEnd = null;
			try
			{
				Mission.EndMission();
			}
			catch
			{
			}
		}

		private void Place()
		{
			Vec2 centre;
			string how;
			if (!Centre(out centre, out how))
			{
				GiveUp("no ground found in " + _scene);
				return;
			}
			Vec3 a;
			Vec3 b;
			if (!Pair(centre, out a, out b))
			{
				GiveUp("no open ground near the middle of " + _scene + " (" + how + ")");
				return;
			}
			Log.Write("field duel: " + how + ", fighters at " + a.x.ToString("F0") + "," + a.y.ToString("F0") + " and " + b.x.ToString("F0") + "," + b.y.ToString("F0"));
			_me = Spawn(CharacterObject.PlayerCharacter, Mission.PlayerTeam, a, b);
			_them = Spawn(_foe, Mission.PlayerEnemyTeam, b, a);
		}

		// The middle of the playable ground.
		private bool Centre(out Vec2 centre, out string how)
		{
			centre = Vec2.Zero;
			how = "";
			try
			{
				foreach (KeyValuePair<string, ICollection<Vec2>> kv in Mission.Boundaries)
				{
					if (kv.Value != null && kv.Value.Count >= 3)
					{
						Vec2 sum = Vec2.Zero;
						foreach (Vec2 p in kv.Value)
						{
							sum += p;
						}
						centre = sum * (1f / kv.Value.Count);
						how = "boundary '" + kv.Key + "'";
						return true;
					}
				}
			}
			catch
			{
			}
			try
			{
				Vec3 min;
				Vec3 max;
				Mission.Scene.GetBoundingBox(out min, out max);
				centre = new Vec2((min.x + max.x) / 2f, (min.y + max.y) / 2f);
				how = "scene box";
				return true;
			}
			catch
			{
				return false;
			}
		}

		// Two points twelve paces apart, both on open ground, searching out
		// from the middle.
		private bool Pair(Vec2 centre, out Vec3 a, out Vec3 b)
		{
			a = Vec3.Zero;
			b = Vec3.Zero;
			for (float r = 0f; r <= 150f; r += 6f)
			{
				int steps = (r <= 0f) ? 1 : Math.Max(8, (int)(r / 3f));
				for (int i = 0; i < steps; i++)
				{
					float ang = (float)(2.0 * Math.PI * i / steps);
					Vec2 p = centre + new Vec2((float)Math.Cos(ang), (float)Math.Sin(ang)) * r;
					for (int d = 0; d < 4; d++)
					{
						float dang = (float)(Math.PI / 2.0 * d);
						Vec2 q = p + new Vec2((float)Math.Cos(dang), (float)Math.Sin(dang)) * 12f;
						if (Ground(p, out a) && Ground(q, out b))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private bool Ground(Vec2 p, out Vec3 at)
		{
			at = Vec3.Zero;
			try
			{
				float h = Mission.Scene.GetTerrainHeight(p, true);
				Vec3 probe = new Vec3(p.x, p.y, h, -1f);
				PathFaceRecord face = PathFaceRecord.NullFaceRecord;
				Mission.Scene.GetNavMeshFaceIndex(ref face, probe, true);
				if (face.FaceIndex == -1)
				{
					return false;
				}
				at = new WorldPosition(Mission.Scene, probe).GetGroundVec3();
				if (!at.IsValid || Math.Abs(at.z - h) > 3f)
				{
					at = probe;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private Agent Spawn(CharacterObject c, Team team, Vec3 at, Vec3 facing)
		{
			Vec2 dir = (facing - at).AsVec2;
			dir = (dir.Length > 0.01f) ? dir.Normalized() : new Vec2(0f, 1f);
			AgentBuildData data = new AgentBuildData(c)
				.BodyProperties(c.GetBodyPropertiesMax(false))
				.Team(team)
				.InitialPosition(in at)
				.InitialDirection(in dir)
				.NoHorses(true)
				.Equipment(c.FirstBattleEquipment)
				.TroopOrigin(new SimpleAgentOrigin(c))
				.Controller((c == CharacterObject.PlayerCharacter) ? AgentControllerType.Player : AgentControllerType.AI);
			Agent agent = Mission.SpawnAgent(data, false);
			agent.FadeIn();
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
			agent.Health = _health;
			agent.BaseHealthLimit = _health;
			agent.HealthLimit = _health;
			return agent;
		}

		public override void OnAgentRemoved(Agent affected, Agent affector, AgentState state, KillingBlow blow)
		{
			if (affected == null || !affected.IsHuman)
			{
				return;
			}
			CharacterObject c = affected.Character as CharacterObject;
			if (c != null && !_fallen.Contains(c))
			{
				_fallen.Add(c);
			}
			if (_ended)
			{
				return;
			}
			if (affected == _me)
			{
				Finish(false);
			}
			else if (affected == _them)
			{
				Finish(true);
			}
		}

		private void Finish(bool weWon)
		{
			if (_ended)
			{
				return;
			}
			_ended = true;
			if (_endTimer != null)
			{
				_endTimer.Reset();
			}
			Action<bool, List<CharacterObject>> cb = _onEnd;
			_onEnd = null;
			if (cb != null)
			{
				cb(weWon, _fallen.ToList());
			}
		}

		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			if (_ended)
			{
				return null;
			}
			if (_me != null && _me.IsActive())
			{
				canPlayerLeave = false;
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat_duel_ongoing", null), 0, null, null, "");
				return null;
			}
			Finish(false);
			return null;
		}
	}
}
