using System;
using System.Collections.Generic;
using System.Linq;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Missions.MissionLogics;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers.Logic;
using SandBox.Missions;

namespace WardensAndDragons
{
	// A trial fought in the arena of a town, in the game's own combat.
	//
	// Modelled on SandBox's ArenaDuelMissionController (the duel the spy
	// quest uses), widened to any number a side: one against one for a trial
	// by combat, seven against seven for a trial of seven. The player is
	// always on the first side; everybody else is spawned from their own
	// CharacterObject in their own battle equipment.
	//
	// The arena knocks people down rather than killing them. Who actually
	// dies is decided afterwards by Law, from who fell - so a death here is
	// the campaign's decision, not an accident of the combat model.
	internal sealed class TrialFight : MissionLogic
	{
		private readonly List<CharacterObject> _ours;

		private readonly List<CharacterObject> _theirs;

		private readonly float _health;

		private readonly List<Agent> _ourAgents = new List<Agent>();

		private readonly List<Agent> _theirAgents = new List<Agent>();

		private readonly List<CharacterObject> _fallen = new List<CharacterObject>();

		private bool _ended;

		private BasicMissionTimer _endTimer;

		// Static like the game's own duel: the mission outlives nothing else
		// that could hold it. true = the player's side won.
		private static Action<bool, List<CharacterObject>> _onEnd;

		private TrialFight(List<CharacterObject> ours, List<CharacterObject> theirs, float health, Action<bool, List<CharacterObject>> onEnd)
		{
			_ours = ours;
			_theirs = theirs;
			_health = health;
			_onEnd = onEnd;
		}

		// Open it in the arena of the town the player is standing in.
		internal static bool Open(List<CharacterObject> ours, List<CharacterObject> theirs, float health, Action<bool, List<CharacterObject>> onEnd, out string why)
		{
			why = null;
			try
			{
				Settlement here = Settlement.CurrentSettlement;
				if (here == null || !here.IsTown || here.Town == null)
				{
					why = "a trial is fought in a town's arena";
					return false;
				}
				LocationComplex complex = LocationComplex.Current;
				Location arena = (complex != null) ? complex.GetLocationWithId("arena") : null;
				if (arena == null)
				{
					why = here.Name + " has no arena";
					return false;
				}
				string scene = arena.GetSceneName(here.Town.GetWallLevel());
				MissionState.OpenNew("WadTrial", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, (DecalAtlasGroup)3),
					(InitializeMissionBehaviorsDelegate)((Mission mission) => new MissionBehavior[]
					{
						new MissionOptionsComponent(),
						new TrialFight(ours, theirs, health, onEnd),
						new MissionFacialAnimationHandler(),
						new MissionAgentPanicHandler(),
						new AgentHumanAILogic(),
						new ArenaAgentStateDeciderLogic(),
						new VisualTrackerMissionBehavior(),
						new CampaignMissionComponent(),
						new EquipmentControllerLeaveLogic(),
						new MissionAgentHandler(),
						new MissionLocationLogic(arena)
					}), true, true);
				return true;
			}
			catch (Exception e)
			{
				why = "the arena could not be opened: " + e.Message;
				Log.Write("opening the trial failed: " + e);
				return false;
			}
		}

		public override void AfterStart()
		{
			_ended = false;
			_endTimer = new BasicMissionTimer();
			try
			{
				TournamentBehavior.DeleteTournamentSetsExcept(Mission.Scene.FindEntityWithTag("tournament_fight"));
			}
			catch
			{
			}
			Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			CultureObject them = (_theirs.Count > 0) ? _theirs[0].Culture : null;
			Mission.Teams.Add(BattleSideEnum.Attacker, (them != null) ? them.Color : 0xFF8B0000u, (them != null) ? them.Color2 : 0xFF000000u, null, true, false, true);
			Mission.PlayerTeam = Mission.Teams.Defender;

			List<MatrixFrame> frames = Mission.Scene.FindEntitiesWithTag("sp_arena").Select((GameEntity e) => e.GetGlobalFrame()).ToList();
			for (int i = 0; i < frames.Count; i++)
			{
				MatrixFrame f = frames[i];
				f.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				frames[i] = f;
			}
			if (frames.Count < 2)
			{
				Log.Write("the arena has " + frames.Count + " spawn point(s); the trial cannot be staged");
				return;
			}
			// The two spawn points furthest apart, one a side.
			MatrixFrame a = frames[0];
			MatrixFrame b = frames[1];
			float best = -1f;
			for (int i = 0; i < frames.Count; i++)
			{
				for (int j = i + 1; j < frames.Count; j++)
				{
					float d = frames[i].origin.Distance(frames[j].origin);
					if (d > best)
					{
						best = d;
						a = frames[i];
						b = frames[j];
					}
				}
			}
			_ourAgents.Add(Spawn(CharacterObject.PlayerCharacter, Mission.PlayerTeam, a, 0));
			for (int i = 0; i < _ours.Count; i++)
			{
				_ourAgents.Add(Spawn(_ours[i], Mission.PlayerTeam, a, i + 1));
			}
			for (int i = 0; i < _theirs.Count; i++)
			{
				_theirAgents.Add(Spawn(_theirs[i], Mission.PlayerEnemyTeam, b, i));
			}
		}

		// Shoulder to shoulder along the spawn point's side axis, so seven do
		// not spawn inside one another.
		private Agent Spawn(CharacterObject c, Team team, MatrixFrame frame, int slot)
		{
			Vec3 at = frame.origin;
			if (slot > 0)
			{
				int side = (slot % 2 == 1) ? 1 : -1;
				float step = 1.4f * ((slot + 1) / 2);
				at = at + frame.rotation.s * (side * step);
			}
			Vec2 dir = frame.rotation.f.AsVec2.Normalized();
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
			bool oursStanding = _ourAgents.Any((Agent x) => x != null && x.IsActive());
			bool theirsStanding = _theirAgents.Any((Agent x) => x != null && x.IsActive());
			if (!oursStanding || !theirsStanding)
			{
				Finish(oursStanding);
			}
		}

		private void Finish(bool weWon)
		{
			if (_ended)
			{
				return;
			}
			_ended = true;
			_endTimer.Reset();
			Action<bool, List<CharacterObject>> cb = _onEnd;
			_onEnd = null;
			if (cb != null)
			{
				cb(weWon, _fallen.ToList());
			}
		}

		public override void OnMissionTick(float dt)
		{
			if (_ended && _endTimer.ElapsedTime > 4f)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_duel_has_ended", null), 0, null, null, "");
				_endTimer.Reset();
			}
		}

		// You may not walk out of a trial while you are still standing in it.
		// Once you are down, you may - and whoever has more still standing at
		// that moment is judged the winner.
		public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
		{
			canPlayerLeave = true;
			if (_ended)
			{
				return null;
			}
			Agent me = Mission.MainAgent;
			if (me != null && me.IsActive())
			{
				canPlayerLeave = false;
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_can_not_retreat_duel_ongoing", null), 0, null, null, "");
				return null;
			}
			int ours = _ourAgents.Count((Agent x) => x != null && x.IsActive());
			int theirs = _theirAgents.Count((Agent x) => x != null && x.IsActive());
			Finish(ours > theirs);
			return null;
		}
	}
}
