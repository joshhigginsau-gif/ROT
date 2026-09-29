# The Art of War: host tactics design (approved direction, 2026-09-29)

Tier 1 only: everything happens on the campaign map; battles are untouched.
Player's brief: clever generalship first (traps, ambushes, deception), dragons as
game-changers, grim logistics as the backdrop. Hands-on control. Brutal costs.
Rivals use every trick against the player. Bought hosts CAN desert.
In-game names and flavour must be in-world; nothing after the Dance is named.

## Core systems (per host, checked on the daily tick in Host.Enforce)

| System | States | Effect | Hook |
|---|---|---|---|
| Readiness | Camped / Marching / Forced march | Speed; marching or forced hosts can be caught unready by traps, camped cannot | Postfix on RoT's concrete party speed model |
| Supply | Supplied / Depot / Foraging / Starving | Hosts carry N days of food; foraging strips villages; starving kills men daily | Existing food patch + days-of-supply counter in Store |
| Season | Spring..Winter | Winter halves forage, bleeds hosts camped in the open | Bannerlord season of year |
| Sight | What each side knows | Traps and lies work on what the enemy can see; scouts, dragons, Whisperers widen it | Distance checks daily; ravens report sightings |

Commander skills: Scouting (sight), Steward (food), Tactics (forced-march losses, odds of being surprised), Leadership (morale).
Morale: forced marches, hunger, winter, being trapped lower it. Bought hosts LOSE their never-desert rule.

## Tactics (12)

Traps
- Lie in wait (Teutoburg, 9 AD): hide a camped host at a forest/ford/pass; seen only close up; first enemy host in reach is hit unready (loses men + morale before battle).
- Feigned retreat (Kalka 1223, Hastings 1066): bait host draws a pursuer onto a host lying in wait; pursuer arrives forced-marched and unready.
- Catch them at the crossing (Stirling Bridge 1297): lie in wait at a bridge/ford; a host caught crossing fights with only part of its men.
- Night attack: strike a host that did not camp; it loses men first; commander may be taken. Failure scatters your own men.

Deception
- Feint march (Hydaspes 326 BC): march openly on A; enemy hosts that see it move to defend A, leaving B thin.
- False raven (Themistocles before Salamis, 480 BC): Whisperers send a rival a false march/weakness; their hosts move on it. Gold; if traced, Honour + relations.
- Turncoat guide (Ephialtes, 480 BC): bribe a local; enemy host loses days or walks into an ambush. Gold.
- False campfires (Hannibal's torch-oxen, 217 BC): host looks stronger to enemy scouts so weaker rivals won't attack. FEASIBILITY UNKNOWN: check whether the AI's strength estimates can be fooled.

Dragons at war
- Burn their stores (Riverlands burnings, 130 AC): rider over an enemy host zeroes its supply.
- Harren's end (Harrenhal, 2 BC): dragon at a siege; garrison yields or burns within days.
- Eyes in the sky: dragon with a host sees ~10x as far; traps and feints against it rarely work.
- Scorpions (Meraxes in Dorne, 10 AC): hosts can haul scorpions; attacking dragons may be wounded or fall. Rivals use them too.

## Orders (Court > The small council > Your hosts)
Readiness, Supply, Order (existing five + lie in wait, bait, feint march, night attack; with a rider: burn stores, dragon to the siege). Schemes (false raven, turncoat guide, false campfires) via Master of Whisperers. The Hand's report becomes a war map in words.

## Rivals
Lie in wait on the player's roads and bait into them; send false ravens (reuse Ravens' false-letter system); riders burn player stores; haul scorpions vs player dragons; feint; a rival fooled before is harder to fool again.

## Build order
- v2.14 The march: readiness, supply, season, sight, desertion, war map.
- v2.15 Traps: lie in wait, feigned retreat, crossings, night attacks.
- v2.16 Lies: feint, false ravens, turncoat guides, false campfires if possible.
- v2.17 Dragons at war + rivals using everything.

## Check in the game's code before building
- RoT's concrete party speed and morale model types.
- Can a host fall on a passing party without the AI cancelling the order?
- Detecting a party mid-crossing at a bridge/ford.
- How RoT models winter/snow.
- Do two parties engaging one enemy the same day join one battle?
- Can the AI's view of a party's strength be faked (false campfires)?

## Open decisions for the player
- Does the player's own party obey supply/season/traps too?
- Harren's end: near-certain or a gamble?
- Does springing an ambush cost Honour?
- Cut (can return): shadowing, ravaging, circumvallation, converging columns, terror.
