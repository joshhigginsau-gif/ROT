# Abdication: set down the crown, start a house of your own (design, 2026-10-01)

Court -> House and heirs -> "Set down the crown". The ruling house passes to the
named heir as an ordinary AI house, and the player plays on as the founder of a
brand-new house with nothing.

## The player's choices (agreed)

- Who leaves: EITHER, chosen at the moment of abdication.
  - "I will go myself": the player's own hero leaves the throne and founds the new house.
  - "Let one of my children go": the heir takes the crown, and the player switches to a
    younger child of the house (not the heir, adult, unmarried or married) who founds a
    cadet house.
- What you take: NOTHING. One horse, one weapon, plain clothes, a little coin to eat
  (config, default 1,000). No troops, no companions, no white cloaks, no holdings, no
  dragon. Gold, fiefs, party, companions and Kingsguard all stay with the crown.
- The old house: relation set to 100 both ways with its new head (and warm with its
  members), but it never joins, follows or protects the new house. It stays in its
  own kingdom; the new house starts independent (no kingdom).
- Price: smooth if prepared. Cheap and clean when the realm is ready, painful when not.

## When it is smooth and when it is not

Checked on the confirmation screen, each line shown with a tick or a cross:
- The named heir is an adult of your blood (Succession/Laws already know who that is).
- The heir is lawful under the culture's law, or a Great-Council-style approval is not
  needed (skip if unlock_heir_choice=false).
- You are not at war, or no enemy host is inside your realm.
- No Bastard's Banner is risen and no exile company is on its way.

All ticks: the crown passes cleanly. Each cross is a real cost, listed before you confirm:
- Heir not of age: refused (a regency is out of scope).
- Unlawful heir: some houses (config share, default 15%) leave the realm.
- At war: every house in the realm loses relation with the heir, and the war continues
  under them.
- Banner risen / exile coming: the claimant is offered the throne's weakness: their
  odds/shares rise (reuse Bastard/Exile hooks).

## What happens (order matters - reuse Bastard.Become, it already works)

Bannerlord has ONE player clan per campaign (Campaign.PlayerDefaultFaction). The mod
already moves it safely in Bastard.Become: set PlayerDefaultFaction to the new clan,
then ChangePlayerCharacterAction, give the old main party back to the hero who keeps it,
redraw the party icon, stop time. Abdication is the same move with a different new house.

1. Create the new clan by the same path Bastard uses for the bastard's house (HeroCreator
   rules from the handover apply; names from Lore/Surnames; banner: the old arms with a
   cadet difference (a label/bordure colour change), never the reversed colours, which
   mean bastardy). Tier 0 or 1, no kingdom, renown 0.
2. Name it: prompt with a herald's suggestion, as knighted houses already do.
3. "Child goes": child.Clan = new house, SetLeader(child), then the Become path.
   "I go myself": the hard case - the player stays the same hero but changes clan. The
   old clan's leader must become the heir FIRST (ChangeClanLeaderAction / SetLeader, and
   the kingdom's ruler follows the ruling clan's leader), then MainHero.Clan = new house,
   then PlayerDefaultFaction = new house. MainParty stays the player's but is stripped:
   troops, prisoners, companions and most gold go to the heir's party or the old clan.
   VERIFY in the decompiled game what changing MainHero.Clan does to MainParty and the
   party's ActualClan before writing it; if it misbehaves, fall back to: the heir takes
   the old main party, and the player gets a fresh party via the Become path's rebuild.
4. Relations: ChangeRelationAction to 100 between the player and the old house's head;
   +30 with the other members. Store a flag so no system (Law, Ravens, Treachery,
   succession) treats the old house as the player's any more, and so the old house never
   joins, hires or is asked to join the new one (block in Knighting rehire, Clients,
   Oaths, Ravens marriage offers to vassalise, AI army calls).
5. Clean up mod state tied to the old house, the same way Become does: Guard.Abandon,
   Titles.Invalidate, Succession heir records, named blade (stays with the crown),
   Iron Bank loan (stays with the crown - the crown owes it), hosts (stay with the crown,
   order Free), council session (cancelled).
6. Chronicle: a deed on both houses' encyclopedia pages - when, why, who took the crown.
   A raven to every house of the old realm.

## Edge cases to handle

- Player is not a ruler (just a clan head): allow it - the clan passes to the heir; the
  old clan stays sworn where it was.
- No eligible heir / no eligible younger child: option greyed, tooltip says why.
- Player's spouse: stays with the crown by default; Hero.Spouse stays married (no divorce
  action needed). A child-founder takes their own spouse with them.
- Save/load straight after: everything must survive a reload (Store + clan ids only).
- Abdicating twice: allowed once per house; the new house can abdicate again later.

## Testing (log every step, as always)

- wad.abdicate_odds - shows the smooth-or-not checks for the current realm.
- wad.abdicate self|child - runs it now, skipping the menus.
- Log lines: "abdication: <who> leaves <old house>, <heir> takes the crown", each cost
  applied, the new house id, PlayerDefaultFaction before/after, MainParty owner after.

## Config (new section "Abdication", appended after Exile)

abdication_enabled=true, abdication_coin=1000, abdication_unlawful_share=15,
abdication_war_relation=10, abdication_old_house_relation=100.
