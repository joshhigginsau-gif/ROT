# Wardens & Dragons — handover

A Game of Thrones political-layer mod for Mount & Blade II: Bannerlord, built
to sit alongside Realm of Thrones, Bellum Civile and RoT Dynasty & Succession.
Currently v2.2.0. About 13,700 lines across 43 C# files.

## Building

`build.sh` compiles `src/*.cs` with Mono's `mcs` against every DLL in a `refs/`
folder, and writes the result to both `bin/Win64_Shipping_Client` and
`bin/Gaming.Desktop.x64_Shipping_Client`. It prints BUILD OK or BUILD FAILED.

`refs/` is NOT in this zip — copy these 14 out of your game and mod folders:

  TaleWorlds.CampaignSystem.dll, TaleWorlds.CampaignSystem.ViewModelCollection.dll,
  TaleWorlds.Core.dll, TaleWorlds.Library.dll, TaleWorlds.Localization.dll,
  TaleWorlds.MountAndBlade.dll, TaleWorlds.ObjectSystem.dll, 0Harmony.dll,
  BellumCivile.dll, RoTDynastyAndSuccession.dll, NavalDLC.dll,
  NavalDLC.CustomBattle.dll, NavalDLC.ViewModelCollection.dll, HoldCourt.dll

Edit the three paths at the top of build.sh to match where you put things.

## What it does

Dragons (bonding, eggs, claims, a twilight that dims as dragons die),
titles shown in dialogue and the encyclopedia, Harrenhal's curse, wardens and
oaths and client realms, hostages and wards, Honour and Dread, an heir you can
name against your culture's law and who actually inherits — and the Bastard's
Banner, which is the endgame.

## The two rules that matter

**Postfix-only Harmony.** Every patch is a postfix, with exactly one deliberate
prefix: `Laws.Listen` records the player's answer on the heir screen, and it has
to be a prefix because the body of that method kills the player. It returns void
and only reads.

**Everything optional stays optional.** Bellum Civile, RoT Dynasty and Warsails
are all reached by reflection, never referenced directly, so the mod runs with
any combination of them absent.

## Things that are true and non-obvious

Verify against IL before trusting a method name. Most of the serious bugs in
this project came from reasoning about what an API sounded like. `ikdasm x.dll > x.il`
then grep.

- **The game picks the new clan leader before any mod hears about the death.**
  Scoring gives +10 for male, +5 oldest, +5 best skills, and nothing stops a
  spouse who married in — so a husband beats a daughter. And `Kingdom.Leader`
  IS `RulingClan.Leader`, so the clan is the crown. `Succession.Install` corrects
  it afterwards via `ChangeClanLeaderAction.ApplyWithSelectedNewLeader`.

- **Banner "secondary" is not the sigil.** Entry [0] is the background with two
  colour slots; the device is at [1]+. A clan in a kingdom has both background
  slots forced to one colour. Reverse ground against charge, not primary
  against secondary.

- **RoT decides bastardy by SURNAME** — Snow, Stone, Rivers, Storm, Hill,
  Flowers, Sand, Waters, Pyke (`Surnames.cs`). No register API exists. Naming a
  hero Snow makes RoT call them a bastard; renaming them off it makes RoT call
  them legitimised, but only if RoT was asked about them at least once while the
  bastard name was still on them — hence `Baseborn.Observe`.

- **Never create these children through a real birth.** RoT hooks the birth event
  and files newborns permanently; that verdict outranks everything and would make
  acknowledging them impossible to display. They are made with
  `HeroCreator.CreateSpecialHero`.

- **`CreateSpecialHero` returns a NotSpawned hero.** Always follow with
  `ChangeState(Active)` and `SetNewOccupation(Occupation.Lord)`. A wanderer-
  occupation hero gets culled by vanilla's companion behaviour within a campaign.

- **Bellum does not hide other heirs; it force-selects its own.** Five separate
  patches converge on that screen and one is UIExtenderEx, which Harmony cannot
  stand down. `Laws.Unlock` answers the two questions they all ask
  (`TryResolveLegalPlayerHeir` → false, `CanConfirmSelectedPlayerHeir` → true)
  instead of chasing the patches.

## How this was worked on

Every substantial change went to a fresh subagent to verify against IL before
shipping. That caught, among others: a save-corrupting path where the player
rose in rebellion against themselves, a feature that was inert because heroes
were never activated, a migration that was dead code, and a fix I reported as
done whose edits had never been written to disk. It is worth continuing.

## Known open items

- The README inside the v2.2.0 zip still has a v2.1.0 header. Cosmetic.
- Config keys added after a section already exists won't appear in an existing
  `config.txt` — `AppendSection` works per section, not per key. Defaults match,
  so behaviour is identical, but the keys are undiscoverable without deleting
  the file.
- The Bastard's Banner has been played through once. Its failure paths (no lord
  template, kingdom founder refusing, clan creation failing) are verified by
  reading IL, not by having happened.
- Harrenhal's curse ticks ~0.095/day and in testing has never reached a stage
  where anything visible happens. Worth shortening or giving it beats.

## v2.3.0 — the lists, and test commands (added in the Claude Code session)

- `Tourney.cs` / `TourneyMenu.cs`: hosted tourneys and their consequences. The game's
  `TournamentFinished` can fire while the arena mission is still running, so it only
  writes a pending record; `Tourney.Settle()` does the killing, paying and popups from
  `GameMenuOpened` / the hourly tick, and never while `Mission.Current` is set.
- `Cheats.cs`: `wad.cheats` lists every test command.
- `Cfg.AppendMissingKeys` closes the open item above: keys added to an existing section
  are now appended to an existing `config.txt`.
- Built with `dotnet build` against BUTR's 1.4.8 reference assemblies, which carry
  signatures but no method bodies. Two vanilla behaviours were assumed rather than
  read from IL, and are worth confirming in play:
  - whether `TournamentManager.ResolveTournament` removes the tournament itself
    (`Tourney.ResolveNow` removes it afterwards if it is still registered);
  - whether `PlayerEliminatedFromTournament`'s round number is 0- or 1-based
    (`OnEliminated` treats round <= 1 as "early").

## v2.3.2 — verified against the player's own TaleWorlds.CampaignSystem.dll

- `Clan.PlayerClan` is `Campaign.PlayerDefaultFaction`: set once at character creation,
  saved (`SaveableProperty(17)`), never changed by `ChangePlayerCharacterAction`. Switching
  the player onto another clan's leader requires setting it first (`Bastard.Become`).
- `CharacterObject.CreateFrom` copies `HiddenInEncyclopedia` from the template; ROT has
  hidden lord characters. `Baseborn.Visible` clears it on everything the mod creates.
- `Hero.Father`/`Mother` setters DO append to the parent's `_children` (the v2.3.1 note
  saying otherwise was wrong; the extra write is guarded and harmless).
- `TournamentManager.ResolveTournament` removes the tournament itself and fires
  `TournamentCancelled` when the town is under siege. `PlayerEliminatedFromTournament`
  rounds are 0-based.

## v2.4.0 — the King's Justice

- `Law.cs` (charges in `lw:c:*`, judging, sentences, summons, trials), `LawMenu.cs`,
  `TrialFight.cs` (N-v-N arena `MissionLogic`). Builds against BUTR's
  `Bannerlord.ReferenceAssemblies.SandBox` too now.
- `TrialFight` is a widened copy of SandBox's `ArenaDuelMissionController` (the spy quest's
  duel), opened with `MissionState.OpenNew` + `SandBoxMissions.CreateSandBoxMissionInitializerRecord`
  and the arena `Location` from `LocationComplex.Current`. Its end callback only writes
  `lw:result`; `Law.Settle()` applies it after the mission (never while `Mission.Current` is set).
- `Law._sentencing` stops an execution ordered by the court being recorded as a crime.
- Party icon after `Bastard.Become`: `MobilePartyVisualManager` builds a visual on
  `MobilePartyCreated`, and `Campaign.OnPlayerCharacterChanged` creates the new main party
  empty (`CreateParty(id, null)`) before giving it a leader, men and a position.
  `Bastard.RedrawMainParty` removes and re-adds the visual by reflection afterwards.
- Untested in game: the 7-v-7 spacing in real arenas (fighters are placed 1.4 m apart
  along the spawn frame's side axis), and whether every ROT town arena has two or more
  `sp_arena` frames (the trial logs and aborts the staging if not).

## v2.5.0 — the Kingsguard

- `Guard.cs` (records in `kg:*`), `GuardMenu.cs`.
- Bodyguards use the game's own mechanism (SandBox `ClanMemberRolesCampaignBehavior`):
  `LocationCharacter.CreateBodyguardHero(hero, MainParty, SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior)`
  + `PlayerEncounter.LocationEncounter.AddAccompanyingCharacter(lc, true)` on
  `BeforeMissionOpenedEvent`. Vanilla allows one follower; this adds every sworn knight in the
  party. The arena is deliberately excluded (trials are staged there).
- Nobles, wards and baseborn children leave their house (`hero.Clan = PlayerClan`);
  companions stay companions (`AddCompanionAction` only sets `CompanionOf`, which would leave
  a noble counted in their birth house). Knighted soldiers are new Lord-occupation heroes.
- `Guard.Abandon` is called from `Bastard.Become` so the daily vow check does not read the
  whole order as oathbreakers when the player clan changes.
- Errands are abstract (knight + men removed, teleported to the nearest town, resolved on
  return from `Law.Rating` against bandit man count / the quarry's party).

## v2.7.0 — ravens, and the barred doors

- **Kingsguard armour in town.** `Guard.Armoured(h)` builds the same `LocationCharacter` as
  `CreateBodyguardHero` (PartyAgentOrigin, `_settlement` monster, AddFirstCompanionBehavior)
  but with `useCivilianEquipment: false`. `kingsguard_armour_in_town`.
- **`HallFight.cs`**: TrialFight taken indoors. The scene is the venue's `lordshall`
  (`GetSceneName(wallLevel)`), opened as `"ArenaDuelMission"` for the duel's views, with
  `MissionAgentHandler` + `MissionLocationLogic(hall)`; the hall's cast is cleared first
  (`RemoveAllCharacters`). Guests stand on `defender_infantry`/`defender_archer`; the other
  side spawns at the frame furthest from the guests' centroid (the door). The spawn-point
  counts are logged per hall. Fewer than two frames → decided without a mission.
  The callback only writes `rv:result`; `Ravens.Settle` applies it off-mission.
- **`Ravens.cs`** (`rv:*`): a weekly roll for a letter (marriage / feast / kin / peace),
  false with `TrapChance` (sender hatred, Honor trait, your Dread and Honour; 2–40, 100 for
  anyone with a `rv:veng:<id>` flag). An accepted letter is an appointment `rv:appt`; at
  the venue on the day, "Go in to the feast" → honest (relation, marriage, peace) or a trap
  (you as guest, civilian unless you came armed). Falling in a trap = `ApplyByMurder(MainHero)`.
  Proposing a marriage from the court makes a wedding appointment at their seat, itself
  possibly false.
- **`Treachery.cs`** (`tr:scheme`): your false feast. Target → pretext (wedding / feast /
  kin letter) → one of your halls; gold up front, a daily leak roll during preparation,
  then an acceptance roll. At the venue "The feast is laid": let them eat (Honour +3) or the
  Rains. `After`: fallen guests die (`Law.Quiet` suppresses the per-death murder charges),
  survivors and kin of the dead get vengeance flags, Dread +30 / Honour −25 /
  `HonourCap −= treachery_honour_cap`, world relations −15, kin −60, `rv:salt`, and one
  `Law.GuestRight` charge against you. If no adult of the house is left, its towns and
  castles and its children are taken **before** the killing (so the game's own
  leader-death destruction can't hand them elsewhere), then `DestroyClanAction.Apply`.
- Cheats: `wad.raven [kind] [false]`, `wad.feast_now`, `wad.scheme_ready`.

## v2.7.1 — hall fights on the floor

- Cause of "spawned way above the map": `HallFight` opened the lordshall scene with scene
  levels `""`; vanilla's keep assault uses `"siege"`. With no level, entities of every level
  variant are present (Pentos logged 96 defender points).
- Now mirrors `LordsHallFightMissionController`: opens with `"siege"`, places agents on the
  first `OnMissionTick`, reads `Mission.ActiveMissionObjects.FindAllWithType<FightAreaMarker>()`
  (namespace `TaleWorlds.MountAndBlade.Objects`), guests on the `defender_infantry` points of the
  highest `AreaIndex` (innermost), the door side on the next-lower room. Every point must pass
  `Scene.GetNavMeshFaceIndex` and is snapped with `WorldPosition.GetGroundVec3()`. No markers →
  navmesh-valid tag points at the median floor height. One second in, anyone off the navmesh or
  >2 m above ground is `TeleportToPosition`'d to their side's points.
- `wad.hall_test [guest]` opens the current hall with record `"test|…"`, which `Ravens.Settle`
  only logs.

## v2.7.2 — bar the doors

- Text only: the Rains of Castamere (a century after the Dance) replaced by "Give the signal: bar the doors". Choice id `rains` kept, so nothing in saves changes.

## v2.8.0 — the small council, and hosts bought with gold

- `Host.cs` (`hs:<partyId>` = knight|quality|men|price|end|order|target|warned). Raised with
  `MobilePartyHelper.CreateNewClanMobileParty(knight, PlayerClan)`, roster filled from the
  clan culture's soldiers in the quality's tier band. Knight state `"host"` in `Guard`.
- Four Harmony postfixes on the *concrete* model types in `Campaign.Current.Models` (applied once
  at session launch): wage → 0, `DoesPartyConsumeFood` → false, size limit ≥ roster + 10,
  desertion → empty roster. Each only for parties in `Host._ids`.
- Orders are enforced daily: siege locks `DoNotMakeNewDecisions` and `SetMoveBesiegeSettlement`
  until the siege camp is up, then unlocks so vanilla siege AI assaults; hold = locked defend;
  follow = joins the player's army (created as Patrolling if needed, cohesion kept at 100).
- `Council.cs`: seats from `Bellum.CouncilSeats` (reflection on `PrivyCouncilBehavior`), session
  `cn:session`, councillors added to `lordshall` as LocationCharacters on
  `BeforeMissionOpenedEvent`, removed when the session ends. Realm army via
  `Kingdom.CreateArmy(... Besieger, parties)` held to its target like a host.
- `CouncilDialogue.cs`: `hero_main_options` → `wad_cn_*`; actions run on
  `ConversationEndOneShot`. `CouncilMenu.cs`: Court → The small council.
- The assassination feature that was planned here was dropped.

## v2.9.0 — hosts against hosts

- `Host.Rec` gains `Owner` (clan id; blank = player, for v2.8.0 saves) and `Base` (party size
  before the host was added). `Host.Mine()` is the player's; the model patches cover all hosts.
- `AiMuster` (weekly, `hx:airoll`): each AI-ruled kingdom at war, below `ai_host_max_per_realm`,
  rolls `ai_host_weekly_chance` (×3 if an enemy of theirs has a host), spends
  `ai_host_spend_percent` of the ruler's gold. Commander = a ruling-clan lord leading a free
  party (troops added to it, `Base` = its size) or a partyless lord (new clan party).
- `AiChoose`: engage an enemy host within 250, else besiege the nearest enemy fortification.
  `TheirDaily` re-chooses when free, when a target falls, or when an enemy host comes within 100.
  `Disperse` trims the party back to `max(Base, 60)` rather than destroying it.
- New order `engage` (`SetMoveEngageParty`, locked) for any host, target from `Foes()`: enemy
  hosts, then enemy army leader parties.

## v2.10.0 — parley at the walls

- `Parley.cs`, `ParleyMenu.cs`. Option on vanilla `menu_siege_strategies` when the player leads
  the besieger camp (`PlayerSiege.PlayerSide == Attacker`, `MainParty.BesiegerCamp.LeaderParty
  == MainParty`) → menu `wad_parley`.
- Surrender mirrors `KingdomManager.SiegeCompleted`: garrison roster cleared, lord parties inside
  `LeaveSettlementAction` (or `TakePrisonerAction` when seized), influence award,
  `BesiegerCamp.RemoveAllSiegeParties()`, `settlement.Party.MemberRoster.Clear()`,
  `ChangeOwnerOfSettlementAction.ApplyBySiege(kingdom leader, MainHero, s)`, then
  `PlayerSiege.FinalizePlayerSiege()` + `PlayerEncounter.Finish(true)`.
- Single combat: `TrialFight.OpenScene` (no location logic) on the nearest town's arena scene;
  result in `pa:result`/`pa:duel`, settled on GameMenuOpened/hourly with a one-button inquiry
  whose callback surrenders or lifts the siege (`BesiegerCamp = null` + `PlayerEncounter.Finish`).
  Truce `pa:truce:<id>` checked on `OnSiegeEventStartedEvent`.
- Odds are deliberately harsh (player's request): single combat easy, terms need starvation,
  gold in millions, Charm needs 3 of 3.
