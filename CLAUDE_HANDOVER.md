# Wardens & Dragons: handover for a new Claude chat

Paste or upload this file at the start of a new chat. It explains everything
needed to carry on. The detailed history of changes is in `HANDOVER.md` (in
the repo), and the player-facing notes are in
`Module/WardensAndDragons/README.txt`.

## What this is

Wardens & Dragons is a Mount & Blade II: Bannerlord **1.4.8** mod, written in
C#. It adds a Game of Thrones political layer (House of the Dragon era) to a
**Realm of Thrones (RoT)** campaign. It runs alongside Bellum Civile, RoT
Dynasty & Succession and War Sails (NavalDLC).

- **Current version:** v2.18.0 (battle speeches, the chronicle and the scribe; v2.17.0 dragon duels; v2.16.6 load fix. v2.16.3 onward is untested in game at time of writing).
  and confirmed they work.
- **Repo:** https://github.com/joshhigginsau-gif/ROT, branch
  `claude/bannerlord-rot-mod-jqh7py`.
- **Source:** `src/*.cs`, about 67 files. The mod itself is in
  `Module/WardensAndDragons` (`SubModule.xml`, `README.txt`, and `bin/` with
  the DLL and `config.txt`).
- **Build:** run `dotnet build -c Release` in `src/`. The DLL is written into
  `Module/WardensAndDragons/bin/Win64_Shipping_Client`. The older `build.sh`
  (Mono `mcs` against a `refs/` folder) is described in `HANDOVER.md`.
- **Log:** the mod writes `wardens_dragons.log` next to the DLL. The player
  sends this log after every test, and it is the main way to debug.

## How each change is done (keep this workflow)

1. Make the change and build it with no errors.
2. Bump the version in **both** places:
   - the `Log.Write("=== Wardens & Dragons vX - ... ===")` header in
     `src/SubModule.cs`;
   - `<Version value="vX"/>` in `Module/WardensAndDragons/SubModule.xml`.
3. Add a new section at the **top** of `README.txt` for the player, including
   test steps and cheats. Add a short technical section at the end of
   `HANDOVER.md`.
4. Commit and push, then zip `Module/WardensAndDragons` for the player.
5. Tell the player how to test it and ask for the log.

**In a normal chat (no repo access):** the player pastes the `.cs` files that
matter. Claude replies with **complete replacement files** or exact
find-and-replace blocks. The player builds locally, copies the DLL into the
game's `Modules/WardensAndDragons/bin/Win64_Shipping_Client`, plays, and sends
the log back.

## Architecture: the patterns to reuse

- **`Store`** is a string key-value store saved in the campaign save via
  `SyncData`.
  - Calls: `Store.Get/Set/GetI/SetI/Keys(prefix)`. Setting a value to `null`
    deletes the key.
  - Every system owns a key prefix (see the feature map).
  - Records are `|`-joined strings. To stay compatible with old saves, add new
    fields at the end or use a new prefix.
- **`CourtBehavior`** is the campaign behaviour. It registers the menus and
  dialogue, calls each system's `Load()` after a load, and runs the daily and
  hourly ticks (for example `Host.Daily()`). `CourtBehavior.Today()` is the
  day counter.
- **Mission results:** when a mission (a duel, a hall fight) ends, its
  callback writes the result to Store. A `Settle` step then applies it off the
  mission, on `GameMenuOpened` or the hourly tick. Never change campaign state
  inside a mission callback.
- **`Cfg`** reads `config.txt`. The pattern is:
  - a field;
  - a parse case (`if (flag) { X = ...; }`, with `flag2` for bools);
  - a line in the `Default()` text;
  - an `AppendSection` chain. The sections run Kingsguard → Ravens → The small
    council → Parley → Knights → The Iron Bank → Exile, with Exile last.

  New keys only reach an existing `config.txt` if they come in a new section.
- **Output:**
  - `Log.Write` / `Log.Once(key, msg)` for the log file;
  - `Ravens.Popup(title, text)` for a raven, which is a popup;
  - `Flow.Notify` for a message in the corner;
  - `Store.AddDeed` for the house chronicle.
- **Harmony:** patches are postfixes only (the one prefix is `Laws.Listen`).
  Patch the **concrete** RoT model types, not the abstract ones. For example,
  hosts patch `ROTPartyWageModel.GetTotalWage`,
  `ROTMobilePartyFoodConsumptionModel`, `ROTPartySizeLimitModel` and
  `DefaultPartyDesertionModel`.
- **Optional mods are reached by reflection only** (Bellum, RoT, War Sails),
  so the mod must still work when any of them is missing:
  - Bellum council seats: `Bellum.CouncilSeats`.
  - The RoT duel: `RotDuel` calls the private static
    `ROTDuelsBehavior.OpenDuelMission` and reads `_duelFightResult`. The foe
    must be a hero.
  - Private setters on clans: `Bastard.Set(clan, prop, value)`.
  - Kingdom creation: `Bastard.Crown`. Rename with
    `Kingdom.ChangeKingdomName`.
- **Heroes:** `HeroCreator.CreateSpecialHero` must be followed by
  `ChangeState(Active)` and `SetNewOccupation(Lord)`. `Titles.Unser()` stops
  "Lord Ser" from appearing.

## Feature map (file: what it does, and its Store prefix)

- **Dragons** (`Dragons.cs`, `DragonRec.cs`, `DragonMenu.cs`): bonding, eggs,
  riders, the registry. Prefix `dr:`.
- **Standing** (`Standing.cs`): Honour and Dread, and seasonal drift.
- **Wardens, oaths and client realms** (`Oaths.cs`, `OathDef.cs`,
  `OathKind.cs`, `Clients.cs`, `Suzerainty.cs`, `Absorb.cs`,
  `WardensMenu.cs`). Prefix `wd:`.
- **Wards and hostages** (`Wardship.cs`, `WardMenu.cs`).
- **Heir and succession** (`Succession.cs`, `Laws.cs`, `Law.cs`,
  `LawMenu.cs`). Prefixes `sc:` and `lw:`.
- **The Bastard's Banner** (`Bastard.cs`, `Baseborn.cs`, `Surnames.cs`): the
  endgame rebellion. Prefix `bs:`. `bs:old` is the old house when the player
  took up the banner.
- **Harrenhal's curse** (`Harrenhal.cs`). Prefix `hh:`.
- **The house blade** (`Blade.cs`).
- **Titles and cultures** (`Titles.cs`, `Cultures.cs`, `CultureRow.cs`,
  `Heritage.cs`).
- **Kingsguard/Queensguard** (`Guard.cs`, `GuardMenu.cs`): white cloaks wear
  their armour in towns. Prefix `kg:`.
- **Tourneys** (`Tourney.cs`, `TourneyMenu.cs`). Prefix `tn:`.
- **Trials and the King's Justice** (`TrialFight.cs`, `Offices.cs`).
- **Ravens** (`Ravens.cs`, `RavensMenu.cs`): letters, marriage proposals,
  vengeance. Prefix `rv:`.
- **Treachery feasts** (`Treachery.cs`, `HallFight.cs`): "Give the signal:
  bar the doors" (not the Rains of Castamere, which isn't in the HOTD
  timeline). Prefix `tr:`.
  - Hall fights use the siege scene level with FightAreaMarker rooms and
    navmesh snapping.
- **The small council** (`Council.cs`, `CouncilMenu.cs`,
  `CouncilDialogue.cs`): the ruler's council in the lord's hall, and calling
  the banners. Prefix `sc:`.
- **Hosts** (`Host.cs`): armies bought with gold, up to about 25,000, under a
  Kingsguard knight, serving for a season.
  - Record `hs:<party>` = knight|quality|men|price|end|order|target|warned|
    owner|base.
  - Orders are siege, hold, engage, follow or free, enforced daily in
    `Enforce`.
  - AI rulers buy hosts too (`AiMuster`, `AiRaise`).
  - **v2.13.1:** hosts get ships (`Fleet`) and pick land or sea routes
    (`Nav`). If the sea still blocks them, they take ship: a 5–12 day voyage,
    then they are put ashore at the target (`hv:` for the voyage, `hk:` to
    detect a stuck host). Cheat `wad.host_voyage`.
- **The generals' war** (`Generals.cs`, `Scorpions.cs`, v2.14.0): orders raid, ambush, shadow,
  screen, feint, avoid, dragon strike; yearly upkeep (20%) or the host deserts into bandits; knights
  or family command (no leading in person - the player's choice); AI generals react to threats,
  strength and temperament; dragons can be shot down by host scorpions, Dornish hosts best at it.
  Prefixes `ht:`, `hg:`, `hu:`, `hd:`, `hsx:`.
- **Attainder** (`Attainder.cs`, v2.16.3): condemn a house that wronged you (Law record); cast out and
  at war; optional order to execute its lords when your houses capture them. Surviving a barred-doors
  feast attaints the host's house automatically. Prefix `at:`.
- **Court anywhere** (v2.16.3): any settlement, or J on the map (`Menus.FieldCourtTick`).
- **Sworn houses** (`Sworn.cs`, v2.16.0): wardens (Bellum county+) gather cadet, knight and invited
  houses; each holds a manor (a warden's village, paid a share of its taxes); castles can be granted
  (Bellum barony placed beneath the warden). Manors show in Bellum's hierarchy tooltips. AI grows very
  slowly (one per season worldwide, world cap = villages). Prefixes `sw:`, `swg:`, `swx:`.
- **Abdication** (`Abdication.cs`, v2.15.0, design in `docs/ABDICATION.md`): set down the crown;
  the heir takes everything, you or a grown younger child found a new independent house with nothing.
  Reuses `Bastard.SwitchPlayer` (extracted from Become). Key finding: `MainParty.ActualClan` never
  follows `MainHero.Clan` - set it by hand. Prefix `ab:`.
- **Parley at sieges** (`Parley.cs`, `ParleyMenu.cs`, `RotDuel.cs`).
  Prefix `pa:`.
  - Terms need starvation; gold costs millions; Charm needs 3 of 3.
  - Single combat is the easiest road: acceptance depends on Valor
    (75/45/12/3%), one try a day, and the garrison may go back on its word.
  - Uses RoT's own duel. `FieldDuel.cs` is no longer used for sieges.
- **Knights of the realm** (`Knighting.cs`, `KnightsMenu.cs`): the ruler
  knights anyone into a landless house.
  - The house serves as a mercenary company and can leave whenever it likes.
  - Name generator (`Lore.cs`), naming prompt, randomised encyclopedia pages
    for the knight and the house.
  - Kin can't be knighted. Prefixes `kn:` and `kx:`.
- **The Iron Bank** (`IronBank.cs`, `BankMenu.cs`). Prefix `ib:`.
  - Player loans with instalments. Missed payments make the Bank fund enemy
    hosts that hunt you.
  - AI rulers borrow. An AI default brings a loan offer to the player.
- **Exile** (`Exile.cs`). Prefix `ex:`.
  - The beaten claimant sails to Essos and founds "The <Colour> Company".
  - It returns after 10–20 years (canon) under his son, funded by your worst
    enemy, takes a castle, is crowned and declares war. This repeats for up
    to 5 generations.
  - The company never takes the player's gold.
  - It works both ways: if the player took up the bastard's banner, the old
    house's heir goes into exile.
- **Plumbing:** `Cheats.cs` (console `wad.*` test commands), `Menus.cs`,
  `HouseMenu.cs`, `Attention.cs`, `Commands.cs`, `Inquiry.cs`, `Offer.cs`,
  `Dialogue.cs`, `Relax.cs`, `Handback.cs`, `Styles.cs`.

## Lessons learned the hard way

- **Check real method names in the decompiled DLLs.** Guessing APIs caused
  most of the bugs. Use ILSpy/dnSpy on `TaleWorlds.CampaignSystem.dll`,
  `ROT.dll` and `BellumCivile.dll`.
- **Custom mission scenes for duels crashed.** Both the arena and a field
  scene did. The fix was calling RoT's own duel through reflection.
- **Hall-fight spawns** placed people above the map until the scene level was
  set to "siege" and spawns went through FightAreaMarker rooms with a navmesh
  check.
- **Parties made with `CreateNewClanMobileParty` have no ships**, so they
  can't cross the sea. Give them ships with
  `ChangeShipOwnerAction.ApplyByMobilePartyCreation(p.Party, new Ship(hull))`
  and move them with `AiHelper.GetBestNavigationType...`, not
  `NavigationType.Default`.
- **Namespaces:** `FightAreaMarker` is in `TaleWorlds.MountAndBlade.Objects`;
  `MapPatchData` needs `TaleWorlds.CampaignSystem.Map`; `Ship` is in
  `TaleWorlds.CampaignSystem.Naval`; `ShipHull` is in `TaleWorlds.Core`.
- **C#:** you can't use an `out` parameter inside a lambda. Collect into a
  local list instead.
- **No assassination feature.** An attempt was stopped, and the player agreed
  to go without it.

## The player's preferences

- House of the Dragon era, and canon-feeling. Nothing from after that timeline.
- Big consequences should be **hard and expensive**: millions of gold, rare
  odds, starvation. Honour and Dread should matter.
- Choices come through in-game menus and ravens, with encyclopedia flavour
  text. Variety matters: no identical names or pages.
- The player tests in game and sends the log. Always log the key decisions so
  a log alone can confirm a feature works.

## Roadmap and open items

- **Ideas the player liked but hasn't asked for yet:**
  - coronations and funerals for your lords (not for a master);
  - grudges in the style of Shadow of Mordor's nemesis system (shelved).
- **Known gaps:**
  - The voyage fallback only covers siege and hold orders; a host chasing an
    enemy party across the sea is not ferried.
  - Council armies choose sea routes but are not ferried.
  - Harrenhal's curse never visibly escalates.
  - New config keys don't appear in an existing `config.txt`.
  - Compiler warning: `Parley._crowdFailed` is unused.
