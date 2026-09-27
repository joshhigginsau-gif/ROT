using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using TaleWorlds.MountAndBlade.Objects;
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
		private static string _scene = "";
		private bool _placed;
		private float _checkAt = -1f;
		private List<Vec3> _guestPoints;
		private List<Vec3> _doorPoints;

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
				_scene = scene;
				MissionState.OpenNew("ArenaDuelMission", SandBoxMissions.CreateSandBoxMissionInitializerRecord(scene, "siege", false, (DecalAtlasGroup)3),
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
			_placed = false;
			_checkAt = -1f;
			_endTimer = new BasicMissionTimer();
			Mission.Teams.Add(BattleSideEnum.Defender, Hero.MainHero.MapFaction.Color, Hero.MainHero.MapFaction.Color2, null, true, false, true);
			CultureObject them = (_theirs.Count > 0 && _theirs[0].Who != null) ? _theirs[0].Who.Culture : null;
			Mission.Teams.Add(BattleSideEnum.Attacker, (them != null) ? them.Color : 0xFF8B0000u, (them != null) ? them.Color2 : 0xFF000000u, null, true, false, true);
			Mission.PlayerTeam = Mission.Teams.Defender;
		}

		// Everyone is placed on the first tick, as the game's own keep fight
		// does: by then the scene's rooms are live.
		private void Place()
		{
			List<Vec3> guests;
			List<Vec3> door;
			string how = Rooms(out guests, out door);
			if (guests.Count == 0 || door.Count == 0)
			{
				how = Loose(out guests, out door);
			}
			Log.Write("hall " + _scene + ": " + how);
			if (guests.Count == 0 || door.Count == 0)
			{
				Log.Write("this hall has no floor the feast can use; the fight is decided outside");
				Finish(true);
				return;
			}
			_guestPoints = guests;
			_doorPoints = door;

			List<HallSeat> guestSide = _weAreGuests ? _ours : _theirs;
			List<HallSeat> doorSide = _weAreGuests ? _theirs : _ours;
			Team guestTeam = _weAreGuests ? Mission.PlayerTeam : Mission.PlayerEnemyTeam;
			Team doorTeam = _weAreGuests ? Mission.PlayerEnemyTeam : Mission.PlayerTeam;
			List<Agent> guestAgents = _weAreGuests ? _ourAgents : _theirAgents;
			List<Agent> doorAgents = _weAreGuests ? _theirAgents : _ourAgents;

			int gi = 0;
			int di = 0;
			// The player first, wherever their side stands.
			if (_weAreGuests)
			{
				_ourAgents.Add(Spawn(CharacterObject.PlayerCharacter, _youCivilian, Mission.PlayerTeam, guests, gi++, door));
			}
			else
			{
				_ourAgents.Add(Spawn(CharacterObject.PlayerCharacter, _youCivilian, Mission.PlayerTeam, door, di++, guests));
			}
			foreach (HallSeat h in guestSide)
			{
				guestAgents.Add(Spawn(h.Who, h.Civilian, guestTeam, guests, gi++, door));
			}
			foreach (HallSeat h in doorSide)
			{
				doorAgents.Add(Spawn(h.Who, h.Civilian, doorTeam, door, di++, guests));
			}
			_checkAt = Mission.CurrentTime + 1f;
		}

		// The game's own rooms: the innermost hall for the guests, the room
		// before it for whoever comes through the door. Floor points only -
		// the archer points are the galleries.
		private string Rooms(out List<Vec3> guests, out List<Vec3> door)
		{
			guests = new List<Vec3>();
			door = new List<Vec3>();
			try
			{
				List<FightAreaMarker> markers = Mission.ActiveMissionObjects.FindAllWithType<FightAreaMarker>().ToList();
				if (markers.Count == 0)
				{
					return "no rooms marked";
				}
				SortedDictionary<int, List<Vec3>> rooms = new SortedDictionary<int, List<Vec3>>();
				int rejected = 0;
				StringBuilder sb = new StringBuilder();
				foreach (FightAreaMarker m in markers)
				{
					List<Vec3> list;
					if (!rooms.TryGetValue(m.AreaIndex, out list))
					{
						list = new List<Vec3>();
						rooms[m.AreaIndex] = list;
					}
					foreach (GameEntity e in m.GetGameEntitiesWithTagInRange("defender_infantry"))
					{
						Vec3 at;
						if (Floor(e.GetGlobalFrame().origin, out at))
						{
							if (!list.Any((Vec3 x) => x.Distance(at) < 0.5f))
							{
								list.Add(at);
							}
						}
						else
						{
							rejected++;
						}
					}
				}
				foreach (KeyValuePair<int, List<Vec3>> r in rooms)
				{
					sb.Append(" room ").Append(r.Key).Append("=").Append(r.Value.Count);
				}
				List<int> usable = rooms.Where((KeyValuePair<int, List<Vec3>> r) => r.Value.Count > 0).Select((KeyValuePair<int, List<Vec3>> r) => r.Key).ToList();
				if (usable.Count == 0)
				{
					return "rooms with no floor:" + sb + ", " + rejected + " rejected";
				}
				int hall = usable[usable.Count - 1];
				guests.AddRange(rooms[hall]);
				if (usable.Count >= 2)
				{
					int before = usable[usable.Count - 2];
					door.AddRange(rooms[before]);
					if (guests.Count < 3 && usable.Count >= 3)
					{
						guests.AddRange(rooms[before]);
						door.Clear();
						door.AddRange(rooms[usable[usable.Count - 3]]);
					}
				}
				else
				{
					// One room: the door is its far end.
					Vec3 mid = Middle(guests);
					Vec3 far = guests.OrderByDescending((Vec3 x) => x.Distance(mid)).First();
					door.Add(far);
					if (guests.Count > 1)
					{
						guests.Remove(far);
					}
				}
				// The door side starts at the edge of its room nearest the
				// hall, so they come straight in.
				Vec3 hallMid = Middle(guests);
				door = door.OrderBy((Vec3 x) => x.Distance(hallMid)).ToList();
				return "rooms" + sb + ", " + rejected + " rejected; guests in room " + hall + " (" + guests.Count + "), door " + door.Count;
			}
			catch (Exception e)
			{
				return "reading the rooms failed: " + e.Message;
			}
		}

		// A hall without rooms: any spawn point on the floor that most of
		// them stand on.
		private string Loose(out List<Vec3> guests, out List<Vec3> door)
		{
			guests = new List<Vec3>();
			door = new List<Vec3>();
			List<Vec3> all = new List<Vec3>();
			int rejected = 0;
			foreach (string tag in GuestTags.Concat(OtherTags))
			{
				try
				{
					foreach (GameEntity e in Mission.Scene.FindEntitiesWithTag(tag))
					{
						Vec3 at;
						if (Floor(e.GetGlobalFrame().origin, out at))
						{
							if (!all.Any((Vec3 x) => x.Distance(at) < 0.5f))
							{
								all.Add(at);
							}
						}
						else
						{
							rejected++;
						}
					}
				}
				catch
				{
				}
			}
			if (all.Count < 2)
			{
				return "no rooms, and only " + all.Count + " point(s) on the floor (" + rejected + " rejected)";
			}
			float floorZ = all.Select((Vec3 x) => x.z).OrderBy((float z) => z).ElementAt(all.Count / 2);
			List<Vec3> level = all.Where((Vec3 x) => Math.Abs(x.z - floorZ) < 3f).ToList();
			if (level.Count < 2)
			{
				level = all;
			}
			Vec3 mid = Middle(level);
			Vec3 far = level.OrderByDescending((Vec3 x) => x.Distance(mid)).First();
			List<Vec3> near = level.Where((Vec3 x) => x.Distance(far) < 4f).ToList();
			door.AddRange(near);
			guests.AddRange(level.Where((Vec3 x) => !near.Contains(x)));
			if (guests.Count == 0)
			{
				guests.Add(door[door.Count - 1]);
				door.RemoveAt(door.Count - 1);
			}
			return "no rooms; " + level.Count + " floor point(s) at height " + floorZ.ToString("F1") + ", " + rejected + " rejected";
		}

		// On the navigation mesh, and put down on the ground under it.
		private bool Floor(Vec3 p, out Vec3 at)
		{
			at = p;
			try
			{
				PathFaceRecord face = PathFaceRecord.NullFaceRecord;
				Mission.Scene.GetNavMeshFaceIndex(ref face, p, true);
				if (face.FaceIndex == -1)
				{
					return false;
				}
				at = new WorldPosition(Mission.Scene, p).GetGroundVec3();
				if (!at.IsValid || Math.Abs(at.z - p.z) > 3f)
				{
					at = p;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static Vec3 Middle(List<Vec3> points)
		{
			Vec3 m = Vec3.Zero;
			foreach (Vec3 v in points)
			{
				m += v;
			}
			return (points.Count == 0) ? m : (m * (1f / points.Count));
		}

		// The i-th agent of a side: a point of its own while there are
		// points, then beside one, if the floor is there.
		private Agent Spawn(CharacterObject c, bool civilian, Team team, List<Vec3> points, int i, List<Vec3> facing)
		{
			Vec3 at = points[i % points.Count];
			int round = i / points.Count;
			Vec3 look = Middle(facing) - at;
			Vec2 dir = look.AsVec2;
			dir = (dir.Length > 0.01f) ? dir.Normalized() : new Vec2(0f, 1f);
			if (round > 0)
			{
				Vec2 side = dir.LeftVec();
				float d = 0.9f * ((round + 1) / 2) * ((round % 2 == 1) ? 1f : -1f);
				Vec3 beside = new Vec3(at.x + side.x * d, at.y + side.y * d, at.z, -1f);
				Vec3 ground;
				if (Floor(beside, out ground))
				{
					at = ground;
				}
			}
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

		// Anyone the scene still put in the air comes down to their side's
		// floor.
		private void Rescue()
		{
			int n = 0;
			foreach (Agent a in _ourAgents.Concat(_theirAgents))
			{
				try
				{
					if (a == null || !a.IsActive())
					{
						continue;
					}
					Vec3 p = a.Position;
					Vec3 ground;
					bool ok = Floor(p, out ground) && p.z - ground.z < 2f;
					if (ok)
					{
						continue;
					}
					bool guest = _weAreGuests ? _ourAgents.Contains(a) : _theirAgents.Contains(a);
					List<Vec3> pts = guest ? _guestPoints : _doorPoints;
					if (pts == null || pts.Count == 0)
					{
						continue;
					}
					a.TeleportToPosition(pts[MBRandom.RandomInt(pts.Count)]);
					n++;
				}
				catch
				{
				}
			}
			if (n > 0)
			{
				Log.Write("hall " + _scene + ": " + n + " fighter(s) were off the floor and were brought down to it");
			}
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
			if (!_placed)
			{
				_placed = true;
				try
				{
					Place();
				}
				catch (Exception e)
				{
					Log.Write("placing the hall failed: " + e);
					Finish(true);
				}
				return;
			}
			if (_checkAt > 0f && Mission.CurrentTime >= _checkAt)
			{
				_checkAt = -1f;
				Rescue();
			}
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
