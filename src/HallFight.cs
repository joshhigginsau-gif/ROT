using System;
using System.Collections.Generic;
using System.Linq;
using SandBox;
using SandBox.Missions;
using SandBox.Missions.MissionLogics;
using SandBox.Missions.MissionLogics.Arena;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
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
	// One fighter in the hall, and whether they came dressed for a feast.
	internal sealed class HallSeat
	{
		internal CharacterObject Who;
		internal bool Civilian;

		internal HallSeat(CharacterObject who, bool civilian)
		{
			Who = who;
			Civilian = civilian;
		}
	}

	// A massacre in a lord's hall - or an escape from one.
	//
	// TrialFight taken indoors. The scene is the venue's own "lordshall"
	// (the one the game opens when you storm a keep), and the mission is
	// opened under the name of the game's arena duel so it gets the duel's
	// screens: weapons, health, the camera to watch the rest when you fall.
	//
	// The guests - the side the feast was laid for - stand where the hall's
	// own defenders would ("defender_infantry" / "defender_archer"), spread
	// through the room in whatever they wore to dinner. The others come in
	// from the spawn point furthest from them, which is the door.
	internal sealed class HallFight : MissionLogic
	{
		private readonly List<HallSeat> _ours;
		private readonly bool _youCivilian;
		private readonly List<HallSeat> _theirs;
		// true: the player's side are the guests (their trap); false: the
		// other side are (your feast).
		private readonly bool _weAreGuests;
		private readonly List<Agent> _ourAgents = new List<Agent>();
		private readonly List<Agent> _theirAgents = new List<Agent>();
		private readonly List<CharacterObject> _fallen = new List<CharacterObject>();
		private bool _ended;
		private BasicMissionTimer _endTimer;
		private static Action<bool, List<CharacterObject>> _onEnd;

		private static readonly string[] GuestTags = new string[2] { "defender_infantry", "defender_archer" };

		private static readonly string[] OtherTags = new string[8] { "sp_notable", "sp_player", "spawnpoint_player", "sp_npc", "npc_common", "sp_guard", "sp_throne", "sp_lord" };

		private HallFight(List<HallSeat> ours, bool youCivilian, List<HallSeat> theirs, bool weAreGuests, Action<bool, List<CharacterObject>> onEnd)
		{
			_ours = ours;
			_youCivilian = youCivilian;
			_theirs = theirs;
			_weAreGuests = weAreGuests;
			_onEnd = onEnd;
		}

		internal static bool Open(Settlement venue, List<HallSeat> ours, bool youCivilian, List<HallSeat> theirs, bool weAreGuests, Action<bool, List<CharacterObject>> onEnd, out string why)
		{
			why = null;
			try
			{
				if (venue == null || venue.Town == null || venue.LocationComplex == null)
				{
					why = "there is no hall here";
					return false;
				}
				if (Settlement.CurrentSettlement != venue)
				{
					why = "you must be at " + venue.Name;
					return false;
				}
				Location hall = venue.LocationComplex.GetLocationWithId("lordshall");
				if (hall == null)
				{
					why = venue.Name + " has no lord's hall";
					return false;
				}
				string scene = hall.GetSceneName(venue.Town.GetWallLevel());
				// Clear the hall's own cast - the lord at table, the servants.
				// The game puts them back the next time anyone comes in.
				try
				{
					hall.RemoveAllCharacters();
				}
				catch
				{
				}
				MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "", false, (DecalAtlasGroup)3),
					(InitializeMissionBehaviorsDelegate)((Mission mission) => new MissionBehavior[]
					{
						new MissionOptionsComponent(),
						new HallFight(ours, youCivilian, theirs, weAreGuests, onEnd),
						new MissionFacialAnimationHandler(),
						new MissionAgentPanicHandler(),
						new AgentHumanAILogic(),
						new ArenaAgentStateDeciderLogic(),
						new VisualTrackerMissionBehavior(),
						new CampaignMissionComponent(),
						new EquipmentControllerLeaveLogic(),
						new MissionAgentHandler(),
						new MissionLocationLogic(hall)
					}), true, true);
				return true;
			}
			catch (Exception e)
			{
				why = "the hall could not be opened: " + e.Message;
				Log.Write("opening the hall failed: " + e);
				return false;
			}
		}

		public override void AfterStart()
		{
			_ended = false;
			_endTimer = new BasicMissionTimer();
			Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			CultureObject them = (_theirs.Count > 0 && _theirs[0].Who != null) ? _theirs[0].Who.Culture : null;
			Mission.Teams.Add(BattleSideEnum.Attacker, (them != null) ? them.Color : 0xFF8B0000u, (them != null) ? them.Color2 : 0xFF000000u, null, true, false, true);
			Mission.PlayerTeam = Mission.Teams.Defender;

			List<MatrixFrame> guests = Frames(GuestTags);
			List<MatrixFrame> other = Frames(OtherTags);
			Log.Write("hall spawn points: " + guests.Count + " guest, " + other.Count + " other");
			List<MatrixFrame> all = guests.Concat(other).ToList();
			if (all.Count < 2)
			{
				Log.Write("this hall has no spawn points the feast can use; the fight is decided outside");
				Finish(true);
				return;
			}
			if (guests.Count == 0)
			{
				guests = all;
			}
			Vec3 middle = Vec3.Zero;
			foreach (MatrixFrame f in guests)
			{
				middle += f.origin;
			}
			middle *= 1f / guests.Count;
			MatrixFrame door = all.OrderByDescending((MatrixFrame f) => f.origin.Distance(middle)).First();

			List<HallSeat> guestSide = _weAreGuests ? _ours : _theirs;
			List<HallSeat> doorSide = _weAreGuests ? _theirs : _ours;
			Team guestTeam = _weAreGuests ? Mission.PlayerTeam : Mission.PlayerEnemyTeam;
			Team doorTeam = _weAreGuests ? Mission.PlayerEnemyTeam : Mission.PlayerTeam;
			List<Agent> guestAgents = _weAreGuests ? _ourAgents : _theirAgents;
			List<Agent> doorAgents = _weAreGuests ? _theirAgents : _ourAgents;

			// The player first, wherever their side stands.
			MatrixFrame playerAt = _weAreGuests ? guests[0] : door;
			_ourAgents.Add(Spawn(CharacterObject.PlayerCharacter, _youCivilian, Mission.PlayerTeam, playerAt, 0));
			for (int i = 0; i < guestSide.Count; i++)
			{
				MatrixFrame f = guests[(i + (_weAreGuests ? 1 : 0)) % guests.Count];
				int slot = (i + (_weAreGuests ? 1 : 0)) / guests.Count;
				guestAgents.Add(Spawn(guestSide[i].Who, guestSide[i].Civilian, guestTeam, f, slot));
			}
			for (int i = 0; i < doorSide.Count; i++)
			{
				doorAgents.Add(Spawn(doorSide[i].Who, doorSide[i].Civilian, doorTeam, door, i + (_weAreGuests ? 0 : 1)));
			}
		}

		private List<MatrixFrame> Frames(string[] tags)
		{
			List<MatrixFrame> list = new List<MatrixFrame>();
			foreach (string tag in tags)
			{
				try
				{
					foreach (GameEntity e in Mission.Scene.FindEntitiesWithTag(tag))
					{
						MatrixFrame f = e.GetGlobalFrame();
						f.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
						list.Add(f);
					}
				}
				catch
				{
				}
			}
			return list;
		}

		private Agent Spawn(CharacterObject c, bool civilian, Team team, MatrixFrame frame, int slot)
		{
			Vec3 at = frame.origin;
			if (slot > 0)
			{
				int side = (slot % 2 == 1) ? 1 : -1;
				at = at + frame.rotation.s * (side * 1.2f * ((slot + 1) / 2));
			}
			Vec2 dir = frame.rotation.f.AsVec2.Normalized();
			Equipment kit = civilian ? (c.FirstCivilianEquipment ?? c.FirstBattleEquipment) : c.FirstBattleEquipment;
			AgentBuildData data = new AgentBuildData(c)
				.BodyProperties(c.GetBodyPropertiesMax(false))
				.Team(team)
				.InitialPosition(in at)
				.InitialDirection(in dir)
				.NoHorses(true)
				.CivilianEquipment(civilian)
				.Equipment(kit)
				.TroopOrigin(new SimpleAgentOrigin(c))
				.Controller((c == CharacterObject.PlayerCharacter) ? AgentControllerType.Player : AgentControllerType.AI);
			Agent agent = Mission.SpawnAgent(data, false);
			agent.FadeIn();
			if (agent.IsAIControlled)
			{
				agent.SetWatchState(Agent.WatchState.Alarmed);
			}
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

		public override void OnMissionTick(float dt)
		{
			if (_ended && _endTimer != null && _endTimer.ElapsedTime > 4f)
			{
				MBInformationManager.AddQuickInformation(GameTexts.FindText("str_duel_has_ended", null), 0, null, null, "");
				_endTimer.Reset();
			}
		}

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

		// Everyone still standing on the losing side got out.
		internal static List<CharacterObject> Standing(List<CharacterObject> side, List<CharacterObject> fallen)
		{
			return side.Where((CharacterObject c) => !fallen.Contains(c)).ToList();
		}
	}
}
