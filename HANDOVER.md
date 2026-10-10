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
- v2.10.1: `MissionAudienceHandler.GetRandomAudienceCharacterToSpawn` reads `Settlement.CurrentSettlement.Culture` (null in a siege camp → crash). Harmony prefix (by type name) substitutes the besieged settlement's townsfolk when outside a settlement; if unpatched, `Parley.Decide` resolves the duel on weapon skills.
- v2.10.2: siege single combat moved to `FieldDuel`: the map-patch battle scene (`SceneModel.GetBattleSceneForMapPatch`) opened as "Camp" (vanilla camp views: weapons, HUD, lock, spectator; no crowd, no settlement). Fighters placed on the first tick, 12 m apart on navmesh near the boundary centroid. No ground → `FieldDuel.Failed` → `Parley.Decide` on skill.
- v2.10.3: siege single combat uses RoT's own duel via reflection (`RotDuel.cs`): private static `ROTDuelsBehavior.OpenDuelMission(scene, hero, false, friendly=true, inside=false)`, result read from private `_duelFightResult` (None/PlayerWon/PlayerLost) and cleared with `ResetDuelResult()`; RoT's own post-duel menu only opens on `_duelStarted`, which we never set. The foe must be a hero (RoT reads its clan banner): the lord inside, else an adult of the owning clan.
- v2.10.4: challenge acceptance by the fighter's Valor (75/45/12/3, ± skill gap, starvation, player Honour), once a day (`pa:siege` field 4). After a won duel the garrison may renege (`parley_renege_chance_*` by the house's Honor, +15 if the sworn lord died): castle kept, fighter still captured, Oathbreaking charge, `pa:siege` field 5 = no more duels and terms +25.

## v2.11.0 — knights of the realm

- `Knighting.cs`, `KnightsMenu.cs` (records `kn:<clanId>` = hero|day|origin). Ruler only
  (`Council.Rules`). Candidates: party soldiers tier ≥ 3 (via `Guard.KnightSoldier`), clan heroes
  and companions (not you, not the named heir, not sworn Kingsguard), wanderers in the settlement.
- `Found`: `Clan.CreateClan("wad_knight_<n>_<day>")`, name `Lore.HouseName()`, random banner
  with the Bastard's colour fix (Bastard.Set/Bad/Call/Announce made internal), tier
  `knight_house_tier`, `SetLeader`, home = player's home settlement. Companions are released
  with `RemoveCompanionAction.ApplyByByTurningToLord` first (Clan getter reads CompanionOf).
- Company: `MobilePartyHelper.CreateNewClanMobileParty` + `knight_starting_men` troops;
  service: `ChangeKingdomAction.ApplyByJoinFactionAsMercenary(..., DaysFromNow(contract))`.
- Weekly: serving houses may `ApplyByLeaveKingdomAsMercenary` (more likely below
  `knight_leave_relation`). Vanilla mercenary AI may hire them elsewhere afterwards.
- v2.11.1: knight houses named by `Knighting.HouseName(culture)` (Westerosi/Essosi syllable banks, unique vs Clan.All) with an `Inquiry.Text` prompt; `Story` randomised (openings × deed × quirk × trait, shuffled, words); kin no longer candidates; `Titles.Unser` strips 'Lord/Lady Ser' for every hero.
- v2.11.2: knight houses get `Clan.EncyclopediaText` (saveable, private setter, via `Bastard.Set`) from `Knighting.HouseStory`; words shared with the knight's page and stored as the 4th field of `kn:`; `Append` adds dated leave/return lines; `Knighting.Repair` backfills empty pages at launch.

## v2.12.0 — the Iron Bank

- `IronBank.cs`, `BankMenu.cs`. Player loan `ib:loan` = principal|owed|instalment|due|left|missed|lastFunded;
  standing `ib:standing` (-100..100) moves the limit and rate. Daily: pay if gold ≥ instalment,
  else missed++, +`bank_penalty_percent`, standing −15; missed ≥ 2 → every `bank_fund_every_days`
  `Host.AiRaise(strongest enemy kingdom, leader, true, funded=min(owed, cap), hunt=MainParty)`.
- `Host.AiRaise` is now internal, returns bool, and takes `funded` (Bank money; ruler pays nothing)
  and `hunt` (engage order). When a ruler is short it calls `IronBank.AiBorrow` (50%, capped,
  `ib:ai:<kingdom>` = owed|due). AI default → a cheap loan offered to the player if at war with
  them, else a funded host for one of their enemies.

## v2.13.0 — exile, and the company that comes back

- `Exile.cs` (keys `ex:*`: state exiled/landed/done/deciding, rival, house, company, captain, heir,
  return day, gen, men, party). Rival: `bs:head` (or the old house `bs:old`, now written by
  `Bastard.Become`, when the player took up the banner); after a landing, the son.
- Beaten when the rival's house (or realm, if ruling) holds no fortifications. Prisoner of yours →
  ship/axe choice; alive → ships; dead → champion + son. `Ship`: new clan "The <Colour> Company",
  captain teleported to an Essos town (`Knighting.Essos`), party via `CreateNewClanMobileParty`,
  filled with `Host.Fill` and kept with `Host.Protect` (non-host ids in the patch set).
- Waiting: +men every 21 days, hired as mercenaries by rich AI rulers at war (never the player),
  until a year before the landing. Landing: son (existing adult, else created with Father =
  captain), funder = worst-relation ruler (prefers at war), castle farthest from your home,
  `ApplyByGift`, `Bastard.Crown` (now internal) + `ChangeKingdomName("The <Colour> Crown")`,
  `DeclareWarAction.ApplyByDefault`. State → landed; the son is the next rival (gen+1, cap 5).

## v2.13.1 — hosts take ship

- Hosts had no ships and were ordered with `NavigationType.Default` (land only), so targets over
  water were never reached. `Host.Fleet(p)` gives one `new Ship(hull)` per 500 men (max 20) via
  `ChangeShipOwnerAction.ApplyByMobilePartyCreation`; hulls from clan template → culture template →
  any `ShipHull`. Called from `Raise`, `Fill` (AI/Bank/Exile hosts) and the top of `Enforce` (so
  old hosts get ships). Whether they can actually sail is the nav model's call (War Sails).
- `Host.Nav(p, s)` = `AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty`;
  None → the host can't get there. Engage/follow use `NavAny` (capability). Council's
  `HoldBanners` uses `Host.Nav` too (falls back to Default).
- Voyage fallback (siege/hold): Nav None, or 3 days without moving (`hk:<party>` = x|y|days) →
  `Embark`: `hv:<party>` = target|landing day (5–12 days by distance), parked in place; on the day
  `SetPositionAfterMapChange(s.GatePosition)` and the order resumes. Cleared by `SetOrder`/`Drop`.
  Cheat `wad.host_voyage`.

## v2.14.0 — the generals' war

- `Generals.cs`: tactical orders `raid`/`ambush`/`shadow`/`screen`/`feint`/`avoid` (`Generals.Handles`),
  dispatched from the top of `Host.Enforce`; hourly checks (`Host.Hourly` → `Generals.Hourly`) spring
  ambushes (Tactics vs Scouting, `Casualties` kills a share of non-heroes across an army), strike
  shadowed besiegers (confirm for the player), and screen. State `ht:<party>`; cleared by `SetOrder`/`Drop`.
- Upkeep replaces the season when `generals_enabled`: `r.End` = next upkeep day (+`DaysPerYear`),
  `hu:<party>` = asked. `MyUpkeep`/`AskUpkeep`/`PayUpkeep`; unpaid → `Desert` (looter parties via
  `BanditPartyComponent.CreateLooterParty`, then `StandDown`/`Disperse`). AI: `TheirUpkeep` (ruler gold,
  `IronBank.AiBorrow`, else desert).
- AI brain `Generals.Think` (daily from `TheirDaily`; `hg:<party>` last think): threat to own
  fortification → hold; foe host near → engage/ambush/avoid/raid by `Strength` ratio and traits
  (Valor/Calculating); stale defensive orders → free → `AiChoose`.
- Commanders: `Host.Family()` adds free adult clan members; one leading own party keeps it (`r.Base`),
  `StandDown` trims to base. "follow" retired (old records become free).
- `Scorpions.cs`: `Chance` (Dorne via culture/kingdom name containing "dorn"), `Launch`/`Resolve`
  dragon strikes (`hd:<rider>` = rider|dragon|target|from|landHour|escort|mine; escort troops taken out
  and returned), AI riders weekly (`sx:airoll`), `MapEventEnded` → queued falls settled hourly.
- Config section "Generals"; cheats `wad.host_upkeep`, `wad.ambush_now`, `wad.dragon_strike`, `wad.host_think`.

## v2.15.0 — abdication (docs/ABDICATION.md)

- Verified in the decompiled game before writing the self path: `Hero.Clan`'s setter touches no
  party; `ClanVariablesCampaignBehavior` re-picks a random leader if the hero LED the clan it left;
  `MobileParty.ActualClan` is set only at party creation and must be set by hand (its setter moves
  the war-party registration); `ChangeClanLeaderAction` gives the old leader's gold to the heir and
  makes the heir leader of whatever party they ride in.
- `Bastard.SwitchPlayer(him, house, why)` = Become's switch (PlayerDefaultFaction, ChangePlayerCharacterAction,
  old party handback, redraw), used by Become and the child path. `Bastard.SetPlayerFaction(clan)`.
- `Abdication.cs`: `Checks`/`Odds`, `Begin` (menu), `Cheat`. Self path order: heir own party →
  new house `_leader` field → PlayerDefaultFaction → `ApplyWithSelectedNewLeader(old, heir)` →
  `MainHero.Clan` → `MainParty.ActualClan` → strip to heir. Child path: child out of parties →
  child/spouse to new house → `SwitchPlayer` → leader change on the now-AI old house.
- Keys `ab:done:<clan>`, `ab:was:<clan>`, `ab:last`. Hosts' owner set to the old clan, order free;
  `IronBank.HandToCrown`; `Guard.Abandon`; `sc:heir` cleared. `Abdication.Daily` removes an old
  house from the player's realm. Config "Abdication"; cheats `wad.abdicate_odds`, `wad.abdicate self|child`.

## v2.16.0 — sworn houses

- `Sworn.cs`: records `sw:<house>` = warden|kind|manor village|castle|day|lostDay; `swg:<warden>` last gain;
  `swx:roll` season roll; `swx:income` manor payday. Warden = Bellum tier >= 1 (county) or a style.
  `Gain(warden, kind, who, invitee, name, ai)` founds (cadet arms via `Abdication.Cadet`, knights via
  `Exile.Make` + `Knighting.Company`) or invites, joins the realm, assigns the richest free village.
  A founder's own party gets `ActualClan` set by hand (never follows its owner).
- Castles: `ChangeOwnerOfSettlementAction.ApplyByGift` (Bellum re-syncs the barony in its own
  OnSettlementOwnerChanged) → `Bellum.PlaceBeneath(barony, warden's top title)` → `SetService(.., CustomaryTenure)`.
- Bellum hierarchy tab is titles-only; manors shown via a postfix on private static
  `HierarchyTitleNodeVM.BuildTooltip` (adds TooltipProperty rows by reflection, no extra references).
- AI: one roll per `DaysPerSeason`, 50% nothing, one random ready warden (cap by rank 2/3/4, cooldown
  168 days, free village, 30k gold); world cap = village count (or `sworn_world_cap`). 10% chance of an
  AI castle grant when nobody gains. Daily: manor checks/reassignment, seasonal pay, call to arms,
  `OnClanChangedKingdom` follow-or-break.
- Menus in `WardensMenu.cs` (Option gained a `ruler` flag): Bid a warden; Your sworn houses.
  Cheats `wad.sworn`, `wad.sworn_roll`, `wad.sworn_raise`.

## v2.16.1 — fixes from the v2.15.0 log

- Abdication costs and `Abdication.Daily` set `lw:exiled:<clan>` before forcing a house out, so
  `Law.OnClanChangedKingdom` skips the treason record.
- `Generals.Think`: the defend branch stamps `hg:<party>`; a hold is only freed when
  `StillThreatened` (enemy host or army within 60 of the castle) is false.
- `Host.PickPlace`: "You are at war with no one." when at peace.

## v2.16.3 — court anywhere, and attainder

- Court: `CanHoldCourt` drops the ownership check when `court_anywhere` (default true); injected into
  `town`, `castle`, `village`. `ReturnToSettlement` handles village and the field (`GameMenu.ExitToLast`).
  Field hotkey: `SubModule.OnApplicationTick` -> `Menus.FieldCourtTick` opens `wad_court` with
  `GameMenu.ActivateGameMenu` only on the map (`MapState`, `!AtMenu`), no encounter, battle, siege,
  settlement or captivity; edge-triggered on `court_field_key` (InputKey name, default J).
  Hall-only actions (council, tourneys, trials) already grey themselves when not in your own hall.
- `Attainder.cs`: `at:<clan>` = day|reason|execute|heads. `Declare` casts out of your realm
  (`lw:exiled:` set first so Law skips treason) and `DeclareWarAction.ApplyByDefault(clan, your faction)`;
  houses of another crown are attainted without war. Ravens' massacre-survived branch calls `Declare`.
  `HeroPrisonerTaken` queues captives of attainted houses taken by your houses (your clan, or your
  realm's clans if you rule); `Attainder.Hourly` executes with `Law.Quiet` set (no crime recorded).
  Children and your blood spared. Cheats `wad.attaint`, `wad.attainted`.

## v2.16.4 — wardens grant fiefs

- `Sworn.Rec.Castle` is now a comma list of granted fiefs (old single ids still read). `Fiefs(r)` = still
  owned; daily tick drops lost ones. `Grantable(warden)` = towns/castles but the seat, never past half its
  fiefs. `GrantFief` replaces `GrantCastle` (gift -> Bellum barony re-sync -> PlaceBeneath -> service).
  `AiFief(only)` on the season roll (`sworn_ai_fief_chance`, independent of gaining houses); towns only from
  rank >= 2 with 5+ fiefs. Menus: `WardensMenu.PickFief(warden, influence)` for your own houses and as a
  ruler's bid (`sworn_bid_fief_influence`). Cheat `wad.sworn_fief`.

## v2.16.5 — a year to muster, dearer hosts, host battles, naval routs

- `Muster.cs`: `hm:<id>` = owner|commander|quality|men|cost|ready|hunt|funded|threatened (`hmx:next`).
  Yours: `Host` ChooseGold -> `Muster.BeginMine` (pays now) when `host_muster_days` > 0; AI `AiRaise` decides,
  charges, then `Muster.BeginTheirs`; `Muster.Daily` calls `Host.Raise(..., paid:true)` / `Host.AiRaiseNow(..., charge:false)`.
  30-day grace, then substitute commander / half refund / lapse. `ai_host_max_per_realm` counts pending.
  Note `Host` has its own `Muster()` method, so Host.cs refers to the class as `WardensAndDragons.Muster`.
- `Host.Price` multiplies by `host_cost_multiplier`.
- `Host.WavesPost`: postfix on SandBox `SandBoxMissionSpawnHandler.CreateSandBoxBattleWaveSpawnSettings` sets
  `MaximumReinforcementWaveCount = 0` when `MapEvent.PlayerMapEvent` involves a host (the wave cap is what
  `DefaultBattleMissionAgentSpawnLogic.Init` truncates reserves to).
- `NavalRout.cs`: on `MapEventEnded` for `IsNavalMapEvent` with a winner, queues defeated parties (not main, not
  garrisons); an hour later culls common troops (raft state / no ships -> `naval_rout_losses_percent`, else
  `naval_retreat_losses_percent`; a third to the winner leader's prison roster). Lost `BlockadeBattle` relief
  (winner = defender = besiegers) culls the besieged garrison by `naval_relief_garrison_percent`. Cheat `wad.host_ready`.

## v2.16.6 — save load fix

- Crash on load: `BanditSpawnCampaignBehavior.CacheBanditCounts` keys a dictionary on each bandit party's
  `HomeSettlement`; a null home throws. `Generals.Desert` passed `SettlementHelper.FindNearestSettlementToMobileParty`
  (null at sea) to `BanditPartyComponent.CreateLooterParty`. The party survived the swallowed exception and was saved.
- `BanditHome.cs`: Harmony prefix on `CacheBanditCounts` (patched in `OnSubModuleLoad`) sets
  `BanditPartyComponent._relatedSettlement` to the nearest town/village for homeless, hideout-less bands.
  `Desert` falls back to `BanditHome.Nearest(p)` and makes no bands without a home.

## v2.17.0 — dragon duels

- `DragonDuel.cs`. RoT's `ROTDuelsMissionController` spawns both sides mounted when `spawnBothSidesWithHorse`; an AI
  rider on a `MonsterUsage == "dragon"` mount is swapped to the `<id>2` `dragonfly` item and spawned airborne (the
  player keeps their own mount). `RotDuel.Open(foe, out why, mounted)` passes that flag (Parley still false).
- `dd:duel` = foe|you/them while the mission runs; `DragonDuel.Settle` (GameMenuOpened + hourly) reads
  `RotDuel.TakeResult`. Fallback when RoT can't open: decided on `Odds` (Riding + One-Handed + Polearm/2 + dragon age).
- `Aftermath(winner, loser)`: dragon dies at `dragon_duel_dragon_death_percent` via `Dragons.Kill`; if it lives and the
  rider dies it is set riderless first so `Dragons.OnRiderDeath` doesn't roll again; rider dies at
  `dragon_duel_death_percent` (`KillCharacterAction.ApplyByBattle`, `Law.Quiet`), else HP to 10%.
- AI: `Daily` -> every 28 days `TheyCallYou` (riders at relation <= `dragon_duel_hatred`, each
  `dragon_duel_ai_challenge_chance`%), every 21 days `World` (one mutually hating pair, `dragon_duel_ai_vs_ai_chance`%).
- Menu: `wad_realm` -> "Call out a dragon rider". Cheats `wad.dragon_duel`, `wad.dragon_challenge`, `wad.dragon_duel_ai`.

## v2.18.0 — battle speeches and the chronicle

- `BattleSpeech.cs` (MissionLogic) added on `CampaignEvents.OnMissionStartedEvent` when `MapEvent.PlayerMapEvent` is a
  field/siege battle led by `PartyBase.MainParty` with >= `speech_min_troops`. Once `Mission.Mode == Battle`, it slows
  time (`Mission.AddTimeSpeedRequest`, id 724601), offers themes via `Inquiry.Select`, releases time, and plays lines with
  `MBInformationManager.AddQuickInformation` (speaker portrait) at real-time intervals. Then `Rouse`: `ChangeMorale`
  on the team, `act_cheer_*`/`act_cheering_high_*` on channel 1, `MakeVoice(Victory)`. The enemy leader answers.
  Text pools are in `Speeches.cs` (opener + heart + colour + closer, with tokens; `sp:used` keeps the last 200 hashes).
- `Chronicle.cs`: `hx:<n>` = day|kind|hero|flags|text (`hxn` counter; flags w/l/f/a/s/x). `Store.AddDeed(line, kind)`
  mirrors every ledger line; specific sites pass bastard/house/conquest/marriage/duel. Hooks: OnGivenBirth (child),
  OwnerChanged BySiege (conquest), MapEventEnded (battle + the speech via `BattleSpeech.TakeForChronicle`), Sworn
  player raise (house). Scribe `sc:hired`, wage every 21 days; amend keeps the original in `hxo:<n>`; exposure is
  scheduled in `hxe:<n>` and handled on the daily tick. Encyclopedia: postfix on
  `EncyclopediaHeroPageVM.UpdateInformationText` appends to `InformationText` for player-clan heroes.
- Menu `wad_chronicle` under the court. Cheat `wad.chronicle`.

## v2.18.1 — fixes

- Ravens: `Wed(a, place, out why)` does the marriage and reports why not; `Lapse` performs honest weddings the player
  missed. Generals: `_askedThisSession` re-asks lost upkeep questions; `TheirUpkeep` gives AI one season (`r.Warned`,
  20% cull) before `Desert`. Host `Enforce` siege: `SetDoNotMakeNewDecisions(true)` while besieging; skip a settlement
  besieged by another faction (AI -> free, player -> patrol near). Council `Watch()` logs seats that empty.

## v2.17.1 — v2.18.x rolled back

- Speeches, chronicle and scribe (BattleSpeech/Chronicle/Speeches.cs) removed to be redone; the v2.18.1 fixes kept on the v2.17.0 code. Old `hx:`/`sc:` keys in saves are ignored.

## v2.17.2 — full-scan fixes

- `CourtBehavior.Safe(name, action)` around every session-launch and daily step; `Log.ClearOnce()` and static resets
  (`Generals/NavalRout/Scorpions.Reset`, `Host.Reset/ClearProtected`) in `OnGameStart`.
- `Host.Busy/Defer` + `hpend:<party>` = kind|why: Desert/StandDown/Disperse wait out battles; `Host.Pending()` on Daily.
- RotDuel `owner` (`rd:owner`); stale fallbacks: `dd:duel` 3rd field = day, `pa:duelday`, `rv:fightday` (2 days).
- Law: forfeit when the opposing side is empty; no summons without a judge. Muster: lapse after Grace on failed raise;
  `Raise` refuses busy/army parties. `Host.Orphan` strips raised men when the commander is lost.
- `SetOrder(..., before)` for Feint state. `Host.HostsAndMustersOf` caps Iron Bank funding. `Ravens.Vengeful` expiry.
- `Dragons.Kill` unmounts; Sworn follows on join only and marks `lw:exiled`; `DragonMenu.FromCourt`.

## v2.17.3 — household

- `Baseborn.CanTakeIn/TakeIn`: `child.Clan = PlayerClan` (Active, Lord) without legitimising; HouseMenu `wad_house_takein`. `Repair` restores acknowledged kids with no clan.

## v2.18.0 — histories, nemeses, scandal, ambitions

- `History.cs`: `hh:/hc:/hk:<id>` = entries `day|weight|text` joined by \u001e, cap 30 (lowest weight evicted). Fed by
  MapEventEnded (>= `history_battle_min` or a lord death-marked), HeroKilled, OwnerChanged BySiege, HeroPrisonerTaken,
  OnGivenBirth, BeforeHeroesMarried, OnClanLeaderChanged, RulingClanChanged, ClanChangedKingdom, ClanDestroyed,
  WarDeclared, MakePeace, KingdomCreated/Destroyed; `Store.AddDeed` also writes the player's house. Shown by postfix on
  EncyclopediaHero/Clan/FactionPageVM (InformationText). TODO: UIExtenderEx collapsible divider (needs prefab XMLs).
- `Nemesis.cs`: prefix on private `KillCharacterAction.ApplyInternal` (skips the mark-only call); on success
  `MakeWounded(None)`, `nmf:` queue -> hourly `DisableHeroAction` -> `nma:` return day -> `Return` (Active, teleport home,
  epithet via `SetName`, skills +, relation -100). `nm:<hero>` = enemy|rank|epithet|baseName. Hunt/taunt daily.
- `Scandal.cs` (affair/elopement, `HeroCreator.DeliverOffSpring` bastard), `Ambition.cs` (fortune/match/house/seat/vow/
  squire; `amb:no:<hero>` forbids for player kin).

## v2.18.1 — encyclopedia section

- `src/UI/HistoryUI.cs`: `HeroHistoryMixin` (EncyclopediaHeroPageVM: HasWadHistory, WadHistoryTitle, WadHistory list) + `HeroHistoryPrefab` Prepend at `descendant::EncyclopediaDivider[@Id='AlliesDivider']` (Bellum Replaces the same node; Prepend survives either order). Package Bannerlord.UIExtenderEx 2.13.3 (compile only); `SubModule.EnableUI` is NoInlining so a missing UIExtenderEx can't break load; `History.SectionShown` turns off the text fallback for heroes. Clan/faction: text fallback until their divider Ids are known.

## v2.18.2 — summaries and prose

- `Chronicler.cs`: deeds of note `ac:<clan>` / `ak:<kingdom>` (code|day|text); counters `acn:`/`akn:`; Weekly land check groups towns+castles by Settlement.Culture. `HouseSummary`/`RealmSummary`, `Prose(key, hero)` = one paragraph per year. UI: `HistoryMixinBase<T>`; clan/faction prefabs Prepend at `LeaderDivider` (summary + collapsible divider); hero at `AlliesDivider`.

## v2.18.3

- `Chronicler.Merge/JoinLands/Land/LandNameOf`: unite deeds merged; region-vs-people naming. `Host.AiChoose` skips forts besieged by another faction; crowding log via `Log.Once` per day.
