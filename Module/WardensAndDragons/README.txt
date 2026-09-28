Wardens & Dragons  v2.10.2  -  PARLEY AT THE WALLS


v2.10.2 - THE GROUND BETWEEN THE ARMIES

  Single combat at a siege still crashed: it borrowed a town's arena while
  you were camped outside every town. It is now fought on the open field
  where your siege camp stands - the same battlefield any field battle
  there would load - with your weapons, health, target lock and the
  spectator camera, and no arena around it. You and their champion start
  twelve paces apart. If no open ground can be found on that field, the
  combat is decided on your weapon skills instead of crashing.



v2.10.1 - THE GAUNTLET

  Throwing down the gauntlet at a siege crashed the game. The arena's
  crowd dresses its spectators from the town you are standing in, and in
  a siege camp you are standing in no town at all. Outside a settlement
  the crowd is now the camp followers of the castle's own culture. If the
  crowd cannot be found on your version, the combat is decided on your
  weapon skills against theirs instead of crashing.



v2.10.0 - PARLEY AT THE WALLS

  When you lead a siege and the camp is up, the siege menu offers "Ride
  out under a banner of parley". The lord inside comes to the gate (or the
  castellan, if no lord is home), and you see both sides' strength, their
  food, and how long they have held. Then:

  CHALLENGE THEM TO SINGLE COMBAT - the easiest road
    They accept 9 times in 10 if the lord is brave, otherwise 2 in 3. A
    real duel, you against their lord or the castellan's champion. Win and
    the castle yields and the lord is your prisoner. Lose and you lift the
    siege and swear not to come back for 30 days - coming back anyway costs
    20 Honour. Whoever falls has trial_death_chance of never rising. If
    they refuse, their own men saw it, and terms get easier.

  OFFER TERMS - once a day
    The garrison marches out, the lords go free. Hard: in practice they
    have to be starving, or besieged for weeks, or hopelessly outnumbered
    and all three help. Brave lords hold out longer.

  OFFER GOLD - in the millions
    1,500,000 for a castle, 3,000,000 for a town, more the richer it is -
    enough to buy a lord out of his post and his honour. Honourable lords
    mostly refuse; starving or dishonourable ones mostly don't. Paid only
    if they accept; they fall out with their own liege for it.

  TALK THEM ROUND - once a siege
    Three arguments - reason, your word, or fear - and every one must
    land. Charm drives all three, and you learn from trying. One failure
    and the gate closes; a bad one and they are insulted.

  When the gates open under terms or talk, you choose: let them march out
  as you promised (Honour +2), or seize the lords as they come out (Honour
  -15, Dread +10, a charge of oathbreaking, and every house hears of it).

  TESTING

      wad.parley_odds    the odds at the siege you are leading
      wad.starve         the castle you besiege runs out of food



v2.9.0 - HOSTS AGAINST HOSTS

  OTHER RULERS BUY HOSTS TOO

    Every week, a ruler at war may spend part of their gold on a host of
    their own - three times as likely once an enemy of theirs already has
    one in the field, so a host of yours will be answered. Veterans if they
    are rich, men-at-arms if not, levies if that is all they can afford;
    commanded by a lord of the ruling house. You hear of it by raven when
    it is raised against you, and in passing when it is not.

    A ruler sends their host after an enemy host within reach first, and at
    the nearest enemy castle otherwise, and picks a new target when the
    old one falls or is gone. Rulers at war with each other will throw
    their hosts at each other. When a ruler's host has served its season,
    the lord who led it keeps only the men they had before.

  BRING THEM TO BATTLE

    A new order for your own hosts (Court -> The small council -> Your
    hosts): "Bring an enemy host or army to battle". Pick any enemy host
    or army in the field; they hunt it down wherever it goes.

    The Hand's report lists every other ruler's host, and which are at war
    with you.

      ai_hosts_enabled=true       ai_host_weekly_chance=10
      ai_host_spend_percent=40    ai_host_min_men=2000
      ai_host_max_per_realm=1

  TESTING

      wad.ai_host [realm]    an enemy ruler (or the one named) musters a
                             host now



v2.8.0 - THE SMALL COUNCIL

  HOSTS BOUGHT WITH GOLD  (Court -> The small council -> Muster a host)

    Choose one of your sworn knights to command, what sort of men, and how
    much gold to spend:

      levies          40 a man    tier 1-2
      men-at-arms     80 a man    tier 2-4    (2,000,000 buys 25,000)
      veterans       200 a man    tier 4-6

    They are raised from your culture's soldiers, at most 30,000 to a host,
    and serve 84 days. They were paid up front, so they draw no wages,
    carry their own food, and do not desert however large the host is.
    A week before their service ends you are asked whether to keep them
    another 84 days for a quarter of the first price.

    Orders (Court -> The small council -> Your hosts):
      Besiege      march on an enemy castle or town and lay siege to it
      Hold         go to one of your castles or towns and stay there
      March with me   join your army and fight your battles
      Free         the knight campaigns with them as they see fit
      Stand down   the men go home and the knight rides with you again

    Several knights can each command a host. If a host is destroyed or its
    knight is taken, it scatters.

  THE SMALL COUNCIL IN YOUR HALL  (while you rule)

    Summon Bellum's privy council to one of your towns or castles. They
    ride in over a day or three (anyone who hates you may send regrets)
    and sit for three days at the table in the lord's hall. Walk in and
    speak to them - "There is council business":

      Master of Ships       muster a host; call the realm's lords to the
                            banners against a castle (costs influence);
                            orders for your hosts; stand the lords down
      Master of Coin        a levy on your towns - gold, and resentment
      Hand                  how the realm stands: its strength, its wars,
                            and who loves you least
      Master of Laws        what waits on the King's Justice
      Master of Whisperers  what they make of the letter waiting for you,
                            and which houses would most likely lie to you
      Grand Maester         the health of your house

  TESTING

      wad.council_now    a summoned council is in your hall today
      wad.host_end       the next host's service ends tomorrow



v2.7.2 - BAR THE DOORS

  Nobody has written that song yet - Castamere is a hundred years off. At
  your false feast you now simply give the signal, and the doors are
  barred. The chronicle calls it what it is: guest right broken.



v2.7.1 - FEET ON THE FLOOR

  Hall fights put everyone high above the map. The hall was being loaded
  without its "siege" layout, so it carried the spawn points of every
  version of the hall at once, most of them nowhere near the floor.

  Now the hall opens exactly as the game's own keep assault does, and the
  fight uses the game's own rooms: the guests stand on the floor of the
  innermost hall (never the galleries), and whoever comes for them bursts
  in from the room before it. Every point is checked against the walkable
  ground, and anyone still off the floor a second in is brought down to it.

      wad.hall_test          a harmless fight in the hall you are in -
                             you at the door, six soldiers at table
      wad.hall_test guest    the same, with you as the guest

    Nothing in a test counts: nobody dies and nothing is charged. The log
    writes one line per hall saying which rooms it used.



v2.7.0 - "BREAD AND SALT"

  THE WHITE CLOAKS IN THE STREETS

    Your sworn knights now follow you through towns, castles and your hall
    in the white armour instead of their town clothes.
    kingsguard_armour_in_town=false puts them back in doublets.

  RAVENS  (Court -> Ravens)

    Letters come from other houses, about one every month: a match offered,
    a feast, word from your kin who married into their house, a feast to
    end a war. Accept and you are expected at their hall in five days -
    go there and choose "Go in to the feast".

    Most letters mean what they say. Some do not. The odds a letter is
    false run from about 2% to 40%:

      more likely   the sender hates you, is dishonourable, you are feared
      less likely   the sender is honourable, you are honourable
      always        a survivor of a feast of yours, sworn to vengeance

    Your people say what they make of each letter - right more often the
    more Roguery you have and the more white cloaks ride with you, and
    wrong in either direction when they are wrong. You may accept, accept
    and come armoured with your knights (an insult if it was honest), decline
    politely, or burn it.

    If it was a trap, the doors close and their men come in armoured; you
    and whoever rode with you are in your feast clothes unless you came
    armed. Fight your way out and guest right was broken by THEM - you may
    charge them. Fall in their hall and you do not get up.

    From the court you can also offer a marriage (your own blood, or you,
    to anyone suitable - the wedding is at their hall) and hold an honest
    feast of your own.

  A FEAST THEY WILL NOT LEAVE  (Court -> Ravens)

    Choose a house, a pretext and one of your halls:

      a wedding        a real match, really made (+30 to their coming)
      a feast          bread and salt, old quarrels forgotten
      a kin letter     only if their blood lives at your court (+20)

    It costs 30,000 plus 8,000 a guest, paid up front, and takes 7 days to
    prepare. Every day it may leak - more if you are feared, less if you are
    honourable or have a Lord Commander keeping the doors. If it leaks, the
    whole realm knows. If it holds, they may still decline.

    When they come, be at the hall and choose "The feast is laid". You can
    still let them eat and go home. Or give the signal and have the doors
    barred: you and your white cloaks and best men, armoured, against
    the whole house in their feast clothes and a handful of their guards.

    Every guest who falls dies. Anyone who gets out swears vengeance, and
    so does their kin. If nobody of age is left, the house is ended: its
    towns and castles become yours and its children are raised at your
    court. Dread +30, Honour -25, and your Honour can never again rise as
    high (treachery_honour_cap=20). Every house thinks less of you, nobody
    trusts your bread and salt again, and you will be charged with breaking
    guest right.

  TESTING

      wad.raven [marriage|feast|kin|peace] [false]   a letter now
      wad.feast_now        the feast you accepted is today
      wad.scheme_ready     your false feast is prepared and accepted

    Hall scenes differ from castle to castle; the log records the spawn
    points each hall had. Please send the log after a hall fight.



v2.6.0 - "WILL NO KNIGHT STAND FOR ME?"

  A TRIAL OF SEVEN TAKES A DAY TO GATHER

    Choose to stand among the seven yourself and the lists take a day to
    raise (trial_seven_gather_hours=24). In that day you must find six who
    will stand beside you - "Seek six to stand with you", from the King's
    Justice or the town square.

      Your sworn knights and your own blood    always answer
      Your companions                          nearly always
      Other lords                              only at relation 30 or more
                                               (trial_seven_friend), and even
                                               then they may refuse
      Anybody else                             will not die for you

    Each can be asked once, and whatever they say, they meant it. When the
    day is out, whoever you could not find is made up - half from your own
    soldiers, half from the glory hunters and hedge knights who came to
    watch - and the lists are ready at the arena of the town you are in or
    the next one you reach. The other side gathers too: the accused's own
    house, then lords who love them, then hired swords.

    Sending seven without you is still decided on the spot.

  THE WHITE CLOAK

    Swearing a knight is now a ceremony before the throne: the vows spoken,
    the cloak fastened, the White Book opened to a clean page. And they are
    dressed in the white armour there and then - the kit of Realm of
    Thrones' own Kingsguard troop. Their old battle kit is gone; their town
    clothes stay. Knights already sworn in your save are dressed the next
    time you load it. kingsguard_armour=false turns it off.

  "LORD SER ALLISER"

    Realm of Thrones puts its own rank in front of every lord, so a knighted
    soldier read "Lord Ser Alliser". A sworn knight is Ser, and nothing in
    front of it: "Ser Alliser of the Queensguard".

  TESTING

      wad.seven_ready    end a trial of seven's gathering now
      wad.kg_cloak       dress every sworn knight in the white armour



v2.5.1 - A SWORD YOU CAN DRAW

  In a trial by combat or a trial of seven you could not draw a weapon or
  open the weapon wheel. The game builds a mission's screens - the weapon
  controls, the health bars, the crowd - by the mission's name, and the
  trial was opened under a name of its own, so it had none of them. It now
  opens as the game's own arena duel and gets all of them, including the
  camera to watch the rest of a trial of seven after you fall.



v2.5.0 - THE KINGSGUARD

  Court -> The white cloaks.

  Seven knights sworn for life to guard your person. The Kingsguard (or
  Queensguard) when you rule a realm; your house's Sworn Shields when you
  do not. They hold no lands, take no wives and father no heirs, and they
  serve until they die. The White Book on that page records every one of
  them: where they came from, how long they have worn the cloak, and what
  they have done in it.

  WHO CAN WEAR IT

    Grown, unmarried, not the head of a house and not your heir.

    A champion         a sword of your household, or anyone who has won a
                       tourney
    A common soldier   any tier 4+ soldier in your party, knighted in the
                       field. +2 Honour; the lords like it a little less
    A ward             raised at your court. Their house takes it as an
                       honour
    A younger son or   of a house of your realm that likes you. They may
    daughter           refuse. If they accept, they leave their house and
                       give up their inheritance
    Your baseborn      in the white cloak they renounce every claim - the
    child              banner can never rise for them

  WHAT THEY DO

    Guard you     Every sworn knight in your party walks with you through the
                  streets, into the tavern and into your hall - not just the
                  one companion the game allows. In battle they fight at your
                  side, as heroes of your party always have.

    Champion you  The Lord Commander, then the other white cloaks, are first
                  in line when the King's Justice needs a champion.

    Clear the     Pick a knight and 0, 10, 25 or 50 of your best men, and
    outlaw camps  they ride to the nearest outlaw camps. They come back in a
                  few days with plunder, renown and a report - or carried,
                  with fewer men, and 10% of the time not at all.

    Hunt the      A lord with a charge waiting who fled beyond your realm.
    fugitive      Brought back in chains, they become your prisoner and the
                  charge can be heard.

  THE VOWS ARE REAL

    A knight who marries or leaves your house is an oathbreaker, and the
    King's Justice can try them for it. Taking a cloak back yourself costs
    5 Honour - the cloak is for life. When a bastard rises against your heir,
    a knight who likes the claimant better than you may tear off the cloak
    and go over, as the white cloaks did in the Dance. And if you take up
    the bastard's banner yourself, the white cloaks stay with the house you
    left.

  TESTING

      wad.kg                  the White Book, and who is away
      wad.kg_swear <name>     swear anyone, skipping the vows' checks
      wad.kg_knight           knight your best soldier and swear them
      wad.kg_return           every knight away comes home now



v2.4.0 - THE KING'S JUSTICE

  Court -> The King's Justice.

  CRIMES ARE WRITTEN DOWN AS THEY HAPPEN

    Treason        a house walks out of your realm - by defection, rebellion,
                   or simply leaving. The houses that went over to the
                   bastard are traitors now.
    Murder         a lord of your realm, or of your blood, is murdered
    Kinslaying     somebody kills their own blood
    A captive      a lord of yours is put to the sword after being taken
    Tyranny        what they call it when the one accused is the crown

    You can hear a charge against any lord of the realm you rule, and
    against anybody you hold prisoner, wherever they are from. A lord who
    hates you may refuse to come - and then you outlaw their house, or let
    it lie and be seen letting it.

  BRINGING A CHARGE

    Any lord of your realm, or anyone you hold. A crime on record costs
    nothing to press. Anything else has to be bought: witnesses cost
    20,000, and once it is judged there is a 35% chance the truth comes out.

    Not the crown? Your charge goes to your liege, who judges it by the
    evidence and by who they like better - you or the accused.

  JUDGMENT

    Guilty, innocent, or let the gods decide. Find them guilty and they may
    demand trial by combat, as is their right; deny it and the realm calls
    it tyranny (-5 Honour, +3 Dread).

    Sentences:  a pardon, a fine, seize a holding, take the black (to the
                Wall - they are never seen again), exile their house from
                the realm, or death. A grave crime proven and punished is
                justice, and the realm says so; a small one punished with
                death is called something else.

  TRIAL BY COMBAT, AND TRIAL OF SEVEN

    Fight yourself and it is real: the game's own combat, in the arena of
    the town you are in - or of the next town you enter. "Enter the arena
    for your trial" appears on the town menu.

    Trial of seven, for grave charges: you and the six best of your party
    against the accused, their house and their culture's best, in the same
    arena. Or send seven without you and it is decided on their skill.

    Everyone who falls - on either side, you included - has a 50% chance of
    never rising. Whoever loses, the verdict goes against their side.

  WHEN THE CHARGE IS AGAINST YOU

    Execute a lord whose kin kneel to your crown, or let a lord come to hate
    you enough, and you are summoned.

      As a vassal    your liege judges you. Submit (the chance you are found
                     guilty is shown), demand trial by combat, demand a
                     trial of seven, or refuse - and maybe be outlawed.
                     Guilty: a fine, a holding, or for the gravest charges
                     your house put out of the realm.

      As the crown   your own lords want to see whether the law reaches you.
                     Submit to their judgment and pay a weregild, fight, or
                     answer "I am the law" (+5 Dread, -6 Honour, and the
                     accuser's house may rise against you).

  THE PARTY ICON

    Taking up the banner no longer leaves your party without an icon on the
    map. The game made the icon for the new party before it had a leader,
    men or a place on the map; it is now rebuilt once the party is whole.

  TESTING

      wad.law                     every charge, and any trial waiting
      wad.charge <kind> <name>    charge a lord (treason, murder, kinslaying,
                                  execution, tyranny)
      wad.accuse_me [kind]        a lord of your realm accuses you
      wad.trial                   fight the waiting trial in this town

  CONFIG

    A new "The King's Justice" section: accusations on or off and how often,
    the fine, the cost of witnesses and the chance they are found out, the
    chance of rebellion, the death chance in trials (trial_death_chance=50)
    and whether it applies to you (trial_player_can_die=true).



v2.3.2 - READ FROM THE GAME ITSELF

  Everything in this version was checked against the game's own code
  (TaleWorlds.CampaignSystem 1.4.8) rather than guessed at.

  WHY THE CHILDREN HAD NO PAGE AND NO PARENTS

    The game makes a new hero as a copy of an existing character, and the
    copy inherits that character's "hidden in encyclopedia" flag. Realm of
    Thrones keeps hidden lord characters, and the children and their other
    parents were being copied from them. A hidden hero gets no page, and is
    skipped in every family section - so your own page did not show your
    child, and theirs did not show their mother.

    New children and parents are made visible, prefer characters that were
    never hidden in the first place, and everyone already in your save is
    made visible the next time you load it.

  TAKING UP THE BANNER NOW WORKS

    Bannerlord has exactly one player clan for a whole campaign. It is set
    when you make your character and is saved with the game, and the
    game's own character switch only ever moves you to another member of
    that clan - which is all an heir ever is. Moving onto the head of the
    bastard's house broke that, and the game crashed building your party.

    Now the player's clan is handed to the bastard's house first. Your old
    house carries on as an ordinary house under your heir, who keeps their
    own party and goes back to making their own decisions.

  SMALLER

    Unhorsed "early" in your own tourney now means the first round only.



v2.3.1 - FAMILY, CULTURE AND STYLE

  BASTARDS NOW HAVE PARENTS

    (This paragraph guessed wrong. The real cause is in v2.3.2 above: the
    family lines were always there, but the heroes were hidden from the
    encyclopedia. Writing the parents' lists directly is harmless and stays.)

    They also had no page to speak of. Every baseborn child now has a
    written life: where they were got, who raised them, when they came to
    your gate, and - once you acknowledge them - that you did. The other
    parent gets a page too. A child who rises against your heir has the
    rising added to theirs.

    Children already in your save are mended the next time you load it.

  YOUR HOUSE'S CULTURE

    Court -> House and heirs -> Your house's culture. Choose any culture your
    mods define - Valyrian, if one is loaded - for you and your blood, the
    realm if you rule it, and optionally every holding and the notables in
    them (whose culture decides what troops they raise). It changes the
    succession law Bellum Civile holds you to.

    A culture made from nothing needs its own troops, names and clothes in
    XML: that is a module of its own, not something a menu can make.

      wad.culture list                 every culture your mods define
      wad.culture <name> [holdings]    take it up from the console

  YOUR HOUSE'S STYLE

    Court -> House and heirs -> Your house's style. "Warden of the East" is
    the style this mod gives whoever holds the Vale. Change it to whatever
    you like, or wear none.

      wad.style <style> | none

  TAKING UP THE BANNER

    "Take up the banner" failed in play and the log only said an exception
    was thrown. It now logs the real one, so the next attempt says why.



v2.3.0 - THE LISTS

  A tourney in Westeros is politics with lances. Lords come together, debts
  are paid in public, young men die in the lists, and the winner chooses
  whose lap the roses land in - which is how Rhaegar Targaryen started a war.
  Bannerlord already has the arena. This is everything around it.

  CALLING ONE

    Court -> The lists -> "Call a tourney here", in a town your house holds.
    Once a year.

      Modest     5,000 purse     guests +3 each, renown +20, Honour +1
      Great     20,000 purse     guests +6 each, renown +40, Honour +2
      Lavish    50,000 purse     guests +9 each, renown +60, Honour +3

    The feast costs 40% on top of the purse. Then you choose who of your
    blood rides - your baseborn children included, which is the dangerous
    part. Up to eight houses of your realm come, and their young lords are
    brought to the town so they are actually in the lists to be fought.

    It is the game's own tournament. Go to the arena and ride in it, or stay
    away and it is decided without you when its time runs out.

  THE LISTS ARE NOT SAFE

    At a tourney you host or ride in, each lord riding has a 2% chance of
    being killed and 8% of being carried off badly hurt - one of each at most.
    You never die in them, and neither do your companions. When the death is
    at YOUR tourney, the dead man's house holds the host to account.

    Unhorsed in the first round of your own tourney costs you Dread.

  WINNING

    Win any tourney and you gain Honour, and the herald hands you the wreath.

      Your spouse               safe, and kind
      An unmarried lady         her house is flattered, your spouse is not -
                                and there is a feast afterwards
      Another man's wife        in front of him. He will not forgive it

    Go with her after the feast and a child may come of it, in the usual
    three years - and there is a 40% chance her house finds out.

    Win your own tourney and you choose what to do with your own purse:
    keep it (Honour -2) or give it to the smallfolk (Honour +2).

  WHO ELSE WINS MATTERS

    Your heir         every guest house thinks better of them
    A guest           they take the purse, and like you for it
    An enemy          you paid them anyway, in front of everyone (+1 Honour)
    A baseborn child  the crowd is on its feet for a face that is yours and
                      a name that is not

  THE BASTARD IN THE LISTS

    Every tourney a baseborn child of yours wins - anywhere, not only yours -
    takes 5% more of your sworn houses with them if they ever rise (three
    wins at most). Two wins and the crowds would crown them: they found a
    kingdom and declare war even if you never gave them the sword or your
    name. The reckoning at your funeral says so.

  TESTING THE BANNER WITHOUT WAITING TWENTY YEARS

    Open the console (Alt + ~) and type wad.cheats for the list. Save first:
    these run the real thing.

      wad.bastard                what would happen if you died today, and why
      wad.child_new [count]      a baseborn child comes to the gate now
      wad.child_now              every child on the road arrives today
      wad.age <years> <name>     grow a child up so they can be acknowledged
      wad.acknowledge <name>     give them your name
      wad.blade <name> | none    put the blade in their hand
      wad.bastard_rise [force]   the death question, now, without dying
      wad.bastard_reset          forget the last answer
      wad.die yes                die for real, for the end-to-end path

      wad.tourney_status         your tourney, its guests, every champion
      wad.tourney_here [1-3]     call one in this town, free
      wad.tourney_resolve        decide it now
      wad.tourney_win <n> <name> give someone wins

    A fast run: stand in a town, wad.child_new, wad.age 20 <name>,
    wad.blade <name>, wad.bastard to read the verdict, save, wad.bastard_rise.

  CONFIG

    A new Tourneys section is added to your config.txt on load. And keys
    added to an existing section in a newer version are now added too,
    under "Added by a newer version" - until now they only ever reached a
    fresh config file.



v2.2.0 - THE BASTARD IS SOMEONE YOU RAISED

  Until now the claimant was invented at the graveside, which left the mod
  explaining how a stranger came to be holding your ancestral sword. He is
  not a stranger any more. He is a child you had, years ago, and whatever he
  carries at the end you put in his hand yourself.

  THE NIGHT

    In a town or a village: "Take a room for the night." It costs a little
    coin and carries a cooldown. Nothing happens tonight. If you are married
    there is a chance it is talked about, and your spouse will hear.

  THE CHILD

    Three years later someone comes to your gate from that town, and they do
    not come alone. The child is already of an age to matter, and carries the
    bastard surname of where they were got - Snow, Stone, Rivers, Storm,
    Hill, Flowers, Sand, Waters, Pyke. Up to four of them over a reign.

    They are created as heroes outright, never born. That is deliberate. RoT
    Dynasty & Succession hooks the game's birth event and files every newborn
    as bastard-or-trueborn permanently, and that verdict outranks everything
    else it knows - so a child born the ordinary way could be acknowledged by
    you and RoT's encyclopedia would still call them a bastard for the rest
    of the campaign.

  AND RoT AGREES WITH US, FOR FREE

    RoT decides bastardy by reading a hero's SURNAME against exactly those
    nine words. So naming a child Snow is the whole handshake: its hero page
    says "Legitimacy: Bastard" with nothing registered and nothing called.

    Acknowledge them and we rename them into your house, RoT stops reading a
    bastard surname, and its page flips to "Legitimized" by itself. The two
    mods never contradict each other, because they read the same thing.

  ACKNOWLEDGING

    Court -> House and heirs -> "Acknowledge a child of yours". It gives them
    your name and a claim, and puts them in the heir list. It costs 20
    relation with each of your trueborn kin and 10 standing with the realm,
    because it should.

  THE BLADE IS A BEQUEST NOW

    Court -> House and heirs -> "Put the blade in someone's hand". Any child:
    your heir, a younger son, or the one from the gate. Whoever holds it when
    you die is the one the realm will say you chose.

  AND YOUR DEATH IS WHATEVER YOU BUILT

    What you did                 Fief  Kingdom  Vassals  War
    -------------------------------------------------------
    Never had one                the stranger at the gate, or nothing
    Had one, ignored them          -      -       10%      -
    Acknowledged them            yes      -       20%      -
    Gave them the blade          yes     yes      33%     yes
    Both                         yes     yes      50%     yes

    Only a child you wrote into the book takes the sword's name - which is
    the historical order, since Daemon was legitimised before he became
    Daemon Blackfyre. One you armed but never acknowledged rises as what he
    is, under the name he was born with.

  Config: the whole baseborn section, plus share_ignored / share_acknowledged
  / share_armed / share_both, and bastard_stranger to turn off the invented
  claimant entirely.



v2.1.0 - THE SWORD IS THE ARGUMENT

  THE BANNER WAS BLANK, AND IT WAS ALWAYS GOING TO BE

    It rendered as a flat colour with no device at all, every time, and the
    reason is worth writing down.

    "Secondary" on a banner is not the sigil. Entry [0] is the BACKGROUND and
    carries two colour slots; GetSecondaryColorId reads the second of those.
    The device lives at entry [1] and upwards. So swapping primary against
    secondary shuffled the background against itself and never touched the
    charge.

    It could not have worked anyway. The moment a clan belongs to a kingdom
    the game forces both background slots to a single colour - so by the time
    the mod read them they were already identical, the swap was a no-op, and
    painting the device in "primary" painted it in exactly the colour of the
    ground behind it.

    The real reversal is ground against charge, and that is what it does now:
    the field takes the colour the device wore, and every device layer takes
    the colour the field wore. This is the same thing the game itself does
    when it makes a rebel clan.

  THE BASTARD HOUSE IS NAMED FOR A SWORD

    The kingdom used to be named after whichever castle he happened to take -
    "The Reversed Banner of Odivo" - which is a label, not a name.

    It is Blackfyre now, in the proper sense. Blackfyre was House Targaryen's
    ancestral Valyrian sword. Aegon IV gave it to his bastard instead of his
    trueborn heir, and THAT GIFT is why the rebellion had legitimacy at all.
    Daemon took the sword's name for his house because the sword WAS the
    argument.

    So: he carries off your house's ancestral blade, and his house is named
    for it. Twenty-four of them in the pool -

        Blackfyre    Bittersteel   Brightroar    Greyflame
        Nightfall    Widowbane     Palefang      Redtide
        Coldbrand    Ashthorn      Saltclaw      Ironwake
        Duskrain     Winterbane    Stormedge     Dreadtooth
        Sorrowsong   Orphanmaker   Ravenmourn    Hollowfyre
        Gallowsteel  Lastlight     Kinslayer     Truthbane

    He is born a Waters or a Snow, by where he was got - and takes the
    blade's name when he founds the house, exactly as Daemon Waters became
    Daemon Blackfyre.

  AND YOU CAN NAME IT YOURSELF

    Court -> House and heirs -> "Name your house's ancestral blade".

    This is the one thing in the mod you do purely to make your own death
    worse. A sword with a name is a sword that can be carried off, and the
    house that carries it off takes your name for it with them. If you never
    name it, one is rolled from the pool above.

    Once it is gone, it is gone: the option greys out, and the house screen
    tells you what it was called and who has it.



v2.0.1 - THE REALM GETS A NAME OF ITS OWN

  The new kingdom was named after the castle it was declared in - "The
  Reversed Banner of Odivo" - which is a label rather than a name. A realm
  founded on a stolen claim would not call itself after the building.

  There is a pool of twenty-two now, built from who he is instead of where
  he planted the banner: his bastard surname and the house that would not
  write him down. Most of them are an argument rather than a description.

      The Kingdom of House Waters        The Other Targaryens
      The Bend Sinister                  The True Line of Stark
      The Sinister Line                  The Line That Was Not Written
      The Elder Claim                    The Baseborn Crown

  The heraldry ones are the real thing: a bend or baton sinister is the mark
  of bastardy on a coat of arms, the bar running the wrong way across the
  shield. A house already flying its father's arms reversed has the word
  sitting there to be taken.

  Each name has a short form too, so "at war with The True Targaryens" reads
  properly rather than repeating the full title.



WHAT THIS VERSION IS

  Smaller, and about one thing.

  Up to v1.1.0 this mod ran a second council on top of Bellum Civile's, a
  Great Council that argued with a support score every house in the realm
  quietly carried, and a web of plots. Nearly every serious bug found in the
  v1.0.0 sweep lived in that half of the code, and the tell was in the logs:
  a succession would resolve with "the lords who came to your councils keep
  the word they gave you there" for a player who had never held one.

  So that half is gone. About 6,400 lines of it.

  Cut: the privy-council bridge, all thirty duties, the council menu and
  dialogue, the Great Council, the throne room, plots and the knife, the old
  Black Banner, and the per-house support scores that existed only to feed
  the Great Council.

  Kept, untouched: dragons, titles, Harrenhal, wardens and oaths and client
  realms, hostages and wards, Honour and Dread, and the heir choice - naming
  anyone of your blood against your culture's law, and having them actually
  inherit.

  Kept from the council: only the names. The six seats still read as Hand of
  the King, Master of Coin, Ships, Laws and Whisperers, and the Grand
  Maester. That was always a titles feature rather than a council one.


THE BASTARD'S BANNER

  What replaces all of it is one moment, at the one point where a campaign
  was least interesting: your death.

  When the ruler of your house dies you are asked, once, whether a man who
  came to the gate during the funeral was who everyone thinks he was. Say no
  and you are never asked again, by anyone, for the rest of the campaign.

  Say yes and, that afternoon:

    - A child the dead ruler fathered and never acknowledged walks out of
      your own history. He is real blood - your heir's half-sibling, with a
      claim as good as theirs - not a pretender with a story.

    - He is named for where he was born. Snow in the north, Sand in Dorne,
      Waters in the crownlands, Storm, Rivers, Flowers, Hill, Stone, Pyke.

    - He takes one of your towns or castles, at random.

    - He flies YOUR sigil with the colours reversed. Not a new device - the
      same one, worn wrong, which is what a bastard branch does and why it
      reads as an insult from across a field.

    - He founds a real kingdom, by the game's own founding path, with a
      written backstory and a house motto on its encyclopedia page. Both are
      assembled out of the campaign it is actually happening in: the ruler
      who sired him, the castle he took, how he was hidden, what token he
      kept, and what he did with the twenty years nobody was watching.

    - A third of your sworn houses go over to him. At random - there is no
      ledger behind it any more, and nobody in the realm knew who would go
      until the banner went up. A house whose child you hold hostage thinks
      again.

    - He declares war that morning.

  And then you choose which of the two you are. Stay as your heir and defend
  the throne, or take up the banner and fight the house that would not write
  you down.

  Config: bastards_banner, bastard_declares_war, bastard_vassal_share,
  bastard_min_fiefs, bastard_born_years_before, bastard_min_age,
  bastard_max_age, bastard_house_tier.


UPGRADING FROM v1.1.0

  Your save is fine. On the first load, any privy-council seat still assigned
  to one of our old duties is handed back to a Bellum default, so nothing is
  left pointing at a duty that no longer exists. That runs once and then
  never again.

  Your config file is also fine. The new section is appended on load; the
  dead keys are simply ignored from now on.

  One thing does not carry over: if a Black Banner rose in your campaign
  under v1.0.0 or v1.1.0, it stays as it is. The new event is a different
  system with its own record, and it will still offer itself on your next
  ruler's death.


WHAT CHANGED IN v1.1.0

  Three things, all of them from one campaign's worth of evidence: a queen
  who died on the Cannibal, a son who was named and did not inherit, and a
  Black Banner that rose with nowhere for anyone to go.

  1. THE HEIR SCREEN NOW OFFERS YOUR WHOLE HOUSE

    It always did, in fact. The list on that screen is the game's own and it
    contains every adult of your clan - it never held only your eldest son.
    What was happening is that Bellum Civile force-SELECTS its legal heir
    every time the popup is built, and deselects everyone else, so a full
    list looked like a list of one.

    v1.0.0 stood down two of Bellum's checks. There were five, and one of
    them is not a Harmony patch at all - it is a UIExtenderEx binding on the
    OK button, which no amount of Harmony work can reach. Chasing five
    patches was the wrong shape.

    All five ask the same two questions of the same helper, so this build
    answers the questions instead: is there a legally required heir (no),
    and may this one be confirmed (yes). Two postfixes. Nothing of Bellum's
    is disabled - it still works out your culture's law, still shows you its
    reasoning, and simply has no veto. That also puts the mod back inside
    its own postfix-only rule, which this was the one exception to.

  2. THE HEIR YOU CHOOSE NOW ACTUALLY TAKES THE SEAT

    This is the one that mattered. Naming an heir moved a number in this
    mod's ledger and nothing else.

    When a clan leader dies, the game picks the replacement by score, and
    that score gives +10 for being male, +5 for being oldest and +5 for the
    best skills, with nothing whatsoever to stop a spouse who married in.
    A husband scores 20 on gender, age and skill alone. A daughter of your
    own blood scores 10 for being your daughter. He wins.

    And a kingdom's leader is not a separate thing - it is literally the
    ruling clan's leader. So taking the clan is taking the crown, in the
    same instant. That is exactly how Queen Mela's chosen son was proclaimed
    by this mod while King Lucerys got the kingdom.

    Worse, on the player's own death the heir screen's answer IS applied -
    and then overwritten. The mod now writes your answer down as you give it
    and puts it back afterwards, through the game's own action, so the
    treasury, the party and the governorship travel with the title. One
    transfer, not two, and the seat ends up holding the name you picked.

    New key: enforce_named_heir=true.

  3. THE BLACK BANNER IS A KINGDOM NOW, NOT A HOMELESS CLAN

    "No house has gone to them yet" was not a judgement about your realm.
    It was this mod having built a rebellion with no door in it.

    A clan is not something other houses can join. The game's own join
    action takes a kingdom and nothing else, and there is no way for a clan
    to belong to a faction that is not one. So the Black Banner rose, and
    the lords who would not kneel had precisely nowhere to go.

    It now founds a real kingdom by the same path the game uses when you
    declare your own: registered, named, flying the house's banner and
    colours, seated as its own ruling clan, inheriting the wars. Houses
    defect into it properly, which lets the realm they are walking out of
    react to it.

    And the holding you set aside for that child goes with them. Inside your
    own clan that grant could only ever be a record, because settlements are
    owned by clans and they were in yours. Now they are not, so it changes
    hands for real - and a kingdom with a seat behind it is one the game
    will not quietly discontinue.

    New key: pretender_kingdom=true.

  ALSO FIXED, FOUND WHILE VERIFYING THE ABOVE

    The founding path threw an exception every single time it ran. It passes
    arguments by type, and the game's clan-founding call takes an int icon
    id; a null bound to an int always throws. So every Black Banner in every
    campaign so far was built by the fallback, which makes a bare clan with
    no leader and leaves the claimant sitting in your house.

    First draft of the heir fix deposed the LIVING player whenever any
    relative died - a brother, a cousin, a companion - handing a sibling the
    gold, the party and, for a ruler, the crown. Caught before release.

  KNOWN, AND DELIBERATE

    Answering "there is no legally required heir" also quiets Bellum's
    dynastic marriage-claim branch, which asks the same question. No crash,
    but it is a real behaviour change. Set unlock_heir_choice=false to hand
    the whole thing back to your culture's law.


WHAT CHANGED IN v1.0.0

  This build is not new features. It is a full pass over everything built in
  phases 0 to 8, looking for the things that were quietly not working, and
  a pass over the screens so the mod stops hiding what its choices cost.

  Thirty-eight fixes. These are the ones that will change how your campaign
  actually plays:

  THINGS THAT WERE SILENTLY DOING NOTHING

    The twilight of the dragons was dead. Nothing in the mod ever marked a
    dragon dead, so the counter it reads never moved, every egg and every
    claim was multiplied by exactly 1.00 for the whole campaign, and the two
    config knobs behind it did nothing at all. Dragons now die - with a rider
    who falls in battle, at dragon_falls_with_rider percent - and every one
    that falls dims the world for every egg after it, which is the whole idea.

    Mix the Poisons never touched anybody. It called Heal(-25), and Heal
    returns immediately on a target at full health, which a rival king always
    is. It now actually sickens him.

    Teach the Wards ignored the maester's competence entirely. A maester
    rated 5 taught exactly as well as one rated 95.

    The Fief gift transferred no land. It said "the men on it now look to
    them rather than to you" and handed over a free twenty points of claim.
    A holding given to a child is now recorded as THEIR seat, is not
    offered to a second child, and is the seat the Black Banner rises over.

  THINGS THAT WERE WORKING BACKWARDS

    Competence was inverted at the bottom of the range. The code guessed at
    whether Bellum rated competence 0-100 or 0-1 by testing "is it above
    1.5", so a councillor Bellum rated 1 out of 100 skipped the conversion
    and ran his duty at FULL strength. The least able man in your realm was
    the most effective one, and was charged full controversy for it. This was
    in four separate places, including the Spymaster's watch on plots.

    Your council's duty flags were global with no realm in them. A rival king
    on the far side of the map putting HIS maester on the dragonlore handed
    you 1.5x egg hatching, and his Iron Bank loans were written into your
    chronicle.

    Build the Royal Fleet took the gold before building and never gave it
    back. A coastal town with an unbuilt shipyard failed every week forever.
    It now builds first and charges only for a hull that exists.

  THINGS THAT COULD END A CAMPAIGN

    The succession fired for players who never ruled anything. Die as a
    sworn vassal and the mod scattered your LIEGE's houses over a Great
    Council you were never allowed to call.

    Your companions counted as heirs. Clan.Heroes is everyone under your
    roof, so a 35-year-old hired swordsman could be your default heir - which
    is against your culture's law, which cost every house in the realm 20
    support, permanently, for a decision you never made. Claimants are now
    blood: children, grandchildren, siblings, nephews, nieces, cousins,
    uncles and aunts. A sister-wife or cousin-wife counts, as she should.
    Companions and in-laws do not.

    A daughter named heir who then married into another house stayed your
    heir forever in the mod's eyes while the game had moved her into her
    husband's clan.

    One bad reflection lookup could kill every selection window in the mod -
    heir, oath, duty, knife, ward, dragon - until you restarted the game.

    A gift to a child was marked spent before the work was done, so if the
    Cannibal was the last claimable dragon the Dragon gift vanished with no
    message and that child lost a route to a claim permanently.

    If the Black Banner could not be given a clan of its own, the rising
    fired again EVERY WEEK - same popup, same defections, same Honour loss.
    And once you won that war and wiped the rival house out, the mod thought
    a pretender still existed for the rest of the campaign.

  PERFORMANCE

    Without Bellum Civile installed - a supported setup - the council bridge
    re-scanned every type in every loaded assembly on every call, from menu
    option conditions that redraw every frame. Same bug in the bloodline
    bridge without RoT Dynasty, which also wrote a line to the log file each
    time. The court clerk walked the full hero list around 240 times per
    redraw before any Great Council had been held.


WHAT CHANGED ON THE SCREENS

    Every enabled option now has a tooltip saying what it will cost you.
    The knife's 30,000 gold was hidden two popups deep. Naming an heir told
    you it "costs nothing" and then took 6 support from every house in the
    realm - it is now free the first time, priced and stated when you change
    a name already given, and on its own config key.

    "Return to the court" was rendering ABOVE the actions it returns from,
    in every menu. Back is last everywhere now.

    The Dragonmont had no route from the court at all. You had to already
    know to open Dragonstone's town menu.

    The claim page said a failed claim kills 80% of the time while the
    confirm popup said 0%, because player_claim_can_die was off.

    Per-house support now shows the number beside the word, since the page
    quotes the break threshold as a number. Houses that will not kneel are
    marked.

    "There is no council to set" now says what it means: Bellum Civile is
    not running.

    The realm page appended your entire chronicle, pushing Harrenhal and the
    dragons off the bottom of the screen in a long reign. Last ten now.


THE GREAT COUNCIL IS NOW SAT, NOT WALKED

  The first version asked you to go and find your lords on the floor of the
  hall. It relied on a scene, on SandBox, and on every lord being where he
  ought to be, and it did not work.

  This one does what a court actually does. You stay in the seat. They are
  brought before you ONE AT A TIME, in an order that means something - your
  wardens first, because the realm is watching how they are treated, then
  whoever stands worst toward your heir. The herald calls a name, you get
  that lord's question, and you answer it before the next is called.

      "You have my word, and gold behind it."      +18, costs 20,000
      "I will not promise what I cannot hold
       from the grave."                            +8, +2 Honour
      "You will kneel, or your house will not
       outlast me."                                +10, +3 Dread, -2 Honour
      Wave them away without an answer             -3, and they rode a month

  The screen tells you who is before you, where they stand, whether you are
  holding their kin, how many you have heard and how many are still waiting.
  When the last one is answered the council rises by itself.

  Nothing in it depends on a scene loading or a mod being present. It is a
  throne and a queue.

      Court -> House and heirs -> Call a Great Council
      wad.council_call      calls one immediately, wherever you are

  council_in_person is now FALSE by default. Set it true and lords are ALSO
  placed in the hall to walk up to, but the throne is the real path and the
  one that is tested.


FIXED IN v0.9.2

  DUTIES VANISHED ON A SECOND SAVE. Loading a second campaign without
  restarting the game left you with "duties registered: 0 of 30" and an
  empty council. Bellum's assignment registry is static for the whole game
  process and THROWS on a duplicate id - it does not quietly ignore one, as
  was claimed here in v0.7.1. Registration now happens once per launch
  rather than once per campaign, and the registry is asked whether an id is
  already there before anything is offered to it.

  THE REALM WAS PUNISHED ON DAY ONE. A save that predates Phase 7 counted as
  ninety-nine years without a Great Council and lost 16 support across every
  house the moment the mod loaded. The succession clock now starts the first
  time the mod runs on a save.


PHASE 8: THE BLACK BANNER
=======================================================

INSTALLING
  Replace the WardensAndDragons folder. KEEP YOUR OWN config.txt - it lives
  in bin\Win64_Shipping_Client\ and is not in this zip. It upgrades itself on
  load, appending the new Phase 4 section. Back up your save first.


THE COUNCIL IS BELLUM'S

  Bellum Civile already runs a privy council: six offices held by clans,
  appointment by deliberation and vote, controversy per office, and one
  assignment per office that ticks daily. Building a second one would have
  given the realm two of everything and doubled every effect.

  So this adds to it instead. Thirty duties of ours are registered into
  Bellum's own registry at load. They appear in the council screen beside
  Bellum's nine, they tick with them, and rival realms pick them too.


EVERY DUTY LEANS

  Each duty pushes Honour or Dread a little every day it is worked, scaled
  by the councillor's competence. A full-strength duty at full competence
  moves about 12 points over a year. Seasonal drift pulls back about 4.

  The duties that raise Honour cost gold or give up power - endow the Faith,
  ransom the captives, pardon the condemned, tend the sick. The ones that
  raise Dread bring in gold or power and cost you the realm's love - farm
  the customs, debase the coinage, burn the raiders' ports, the King's
  Justice, blackmail the lords, mix the poisons.

  Bellum's own nine lean too, by id: Fabricate Grievances and Sow Rumors
  cost you Honour; Appease the Nobles and Improve Foreign Relations earn it.
  So every seat on your council counts, not only the seats running ours.

  Fractions are banked, so a slow duty still lands eventually instead of
  rounding away to nothing.


THE THIRTY

  HAND OF THE KING (Chancellor)
    Hold the King's Peace    order in your fiefs; a case now and then reaches
                             you to judge, and your verdict moves your standing
    Progress of the Realm    loyalty and prosperity where the Hand has been
    Rule in Your Name        influence without you, and the realm notices
    Silence the Critics      influence, and a vassal who remembers why
    Broker the Marriages     relations rise between your vassal houses

  MASTER OF COIN (Seneschal)
    Treat with the Iron Bank a real loan, a real debt at 125%. Ignore it long
                             enough and the Bank funds a rival realm against you
    Farm the Customs         gold daily, loyalty falling where it is taken
    Debase the Coinage       more gold, prosperity leaking across the realm
    Ransom the Captives      gold out, and houses that remember who paid
    Endow the Faith          gold out, loyalty rising everywhere

  MASTER OF SHIPS (Marshal)
    Muster a Standing Army   paid daily; the realm orderly and afraid
    Clear the Stepstones     brigand parties in your lands actually destroyed
    Burn the Raiders' Ports  twice as many destroyed, and the smallfolk saw how
    Build the Royal Fleet    a hull laid down in one of your ports each week
                             and handed to your house. Needs Warsails and a
                             town of yours with a shipyard
    Raise the Gold Cloaks    crime stops in your cities. So does much else

  MASTER OF LAWS (Second Advisor)
    Hold the Assizes         security, and cases that come to you to decide
    The King's Justice       security hard and fast, loyalty falling
    The Dragonkeepers        see below
    Codify the Realm's Laws  loyalty settling across your holdings
    Pardon the Condemned     cells emptied, relations mended, security lost

  MASTER OF WHISPERERS (Spymaster)
    The Little Birds         names the house turning against you, by name
    Seed the Foreign Courts  what other realms mean to do
    Blackmail the Lords      influence, and lords who know exactly why
    Hold the Hostages        your sworn houses do not break faith
    Guard the Council        controversy settles on every seat

  GRAND MAESTER (First Advisor)
    Tend the Sick            wounded heroes mend; Harrenhal's kin-death
                             chance is HALVED while he is at it
    The Citadel's Ravens     word of wars and the world's dragons
    Teach the Wards          the children of your court
    Study the Dragonlore     cradle eggs hatch 50% more often while studied
    Mix the Poisons          a rival at war with you sickens, now and then


THE DRAGONKEEPERS

  Your claim odds are near-suicide because seventeen dragons are alive and
  the world is crowded. The only lever was editing world_dragon_capacity,
  which is just cheating quietly.

  Set your Master of Laws to the Dragonkeepers and the riderless dragons on
  the Dragonmont grow used to men: one point of temper a week at full
  competence, down to a floor of 25. A gentler dragon is a better claim.
  Vermithor, Silverwing and Seasmoke all cool. The Cannibal never does.

  It needs your house to hold Dragonstone, and it takes years. That is the
  point - it costs a council seat and a long time, instead of a config edit.


TESTING
  wad.duties     all thirty, their office, their lean, their AI appetite
  wad.council    the six seats, holders, competence, controversy, task
  wad.status     Honour and Dread now

  In the log, look for:
      duties registered into Bellum's council: 30 of 30
      council roll call - <your realm>

  Then open the kingdom screen and set one of your idle councillors to a new
  duty. Three of your six had no duties at all, which was costing you a
  relation point a week with each.


SETTINGS
  council_duties=true          add our thirty at all
  council_lean=true            duties move Honour and Dread
  lean_per_year=12             what a full-strength duty moves in a year
  lean_bellum_duties=true      Bellum's own nine lean too
  council_ai_duties=true       rival realms pick ours as well
  iron_bank_loan=120000        what the Bank lends at full competence
  keeper_temper_per_week=1     how fast the Dragonkeepers calm a dragon
  keeper_temper_floor=25       how calm a dragon can ever become
  ship_cost=9000               what a hull costs the treasury


IF A DUTY SEEMS TO DO NOTHING
  Two of them need something of yours first, and both now say so in the log
  and on screen rather than sitting silent:

    The Dragonkeepers      needs your realm to hold Dragonstone
    Build the Royal Fleet  needs Warsails, and a town of yours with a
                           shipyard in it

  Look for lines beginning 'Warsails:' and 'fleet:' in the log.


PHASE 5: PLOTS AND THE KNIFE

  WHAT IS MOVING AGAINST YOU. A house that hates you enough begins to plan.
  Every week a plot creeps toward its end, and your Master of Whisperers
  creeps toward finding it. Whichever arrives first decides how it goes.

  A plot forms where a house's leader holds you at -20 relation or worse.
  Your Dread makes them likelier, not less likely: fear stops a man speaking
  openly and pushes him into the dark instead. Mercy and Honour in the
  plotter hold them back. A cunning plotter moves faster.

  Three kinds:
      A murder        aimed at you, or at one of your house
      A defection     the house leaves your realm, planned long in advance
      A rival claim   they declare for someone else and sour the realm

  UNCOVERED, IT IS YOUR CHOICE. When exposure reaches 100 you are told who,
  what, and who the mark is, and given four ways out:
      Have it out openly      the plot ends, they hate you, +3 Honour
      Deal with it quietly    the head of that house dies. +6 Dread, -5 Honour
      Buy the plotter back    25,000 gold, and they are grateful
      Say nothing and watch   it runs on, but you see it coming

  UNCOVERED IS NOT GUARANTEED. If progress reaches 100 first, it simply
  happens: a knife in the night with a 55% chance of killing its mark, or a
  house walking out of your realm, or a claim raised against you.

  WHO IS WATCHING MATTERS. Exposure scales with your Spymaster's competence
  AND what he is set to. The Little Birds nearly doubles it. Bellum's Uncover
  Dissent and Counter-Espionage are worth 1.4x. An idle Spymaster is worth
  0.4x, which is close to blind. Guard the Council does something different:
  it stops the knife landing when a murder plot does complete.

  THE KNIFE YOU SEND. At the Small Council: "Send a knife into the dark".
  Name the head of any house in your realm or any realm at war with you, see
  the odds and the reasons, and decide. 30,000 gold, once a year.

      Your Honour falls and your Dread rises the moment you say the word,
      whether it works or not.

      If it works, they die and nothing is ever proved.
      If it fails, there is a 40% chance your man is taken alive and talks.
      Then the target knows, their whole realm knows, and it costs you
      another 8 Honour and 40 relation with them.

  TESTING
      wad.plots             what is moving, how far along, what you know
      wad.plot_push <n>     move every plot and its exposure along by n
                            (wad.plot_push 100 resolves everything at once)

  SETTINGS
      plots_enabled=true       plots at all
      plot_speed=4             how fast a plot moves each week
      plot_exposure=9          how fast your Whisperers find it
      plot_max=4               how many can run at once
      plot_relation=-20        how much a house must hate you to start
      plot_buyoff=25000        cost to buy a plotter back
      plot_murder_chance=55    chance a completed murder plot kills its mark
      knife_cost=30000         what your own knife costs
      knife_base=35            base odds before your Whisperers are counted
      knife_cooldown_days=84   one a year
      knife_trace_chance=40    if it fails, chance it is traced to you


FIXED IN THIS BUILD

  THE OFFICES ARE RENAMED, properly this time. The privy council screen,
  the kingdom screen and the log all now read:

      Marshal         ->  Master of Ships
      Chancellor      ->  Hand of the King
      Seneschal       ->  Master of Coin
      Spymaster       ->  Master of Whisperers
      First Advisor   ->  Grand Maester
      Second Advisor  ->  Master of Laws

  Only what is shown changes. The enum, the ids and your save are untouched,
  and a localisation we do not recognise is passed through rather than
  renamed wrongly. council_office_names=false puts it all back.

  THE DUTY LIST CAN BE READ AGAIN. Bellum's council screen cannot scroll its
  list, and thirty of ours pushed the bottom of it off the screen. The court
  now has "Set a councillor to a duty", which shows every duty for a seat -
  Bellum's and ours together, ours marked with a star and showing their lean -
  in a window that scrolls. It hands the choice back to Bellum to apply, so
  the cooldown and every other rule of Bellum's still holds.


PHASE 6: HOSTAGES AND WARDS

  They are the same arrangement. A child of another house lives at your
  court and does not go home. What differs is the word you use, and the word
  is the whole of it.

      A WARD is fostered and taught. +4 Honour, relations warm, and your
      Grand Maester set to Teach the Wards trains them three times as fast.

      A HOSTAGE is surety. +8 Dread, -4 Honour, relations cool, and their
      house keeps faith because it must.

  The court asks you which word, once, and that is the whole of Phase 6.

  WHAT A HOSTAGE IS ACTUALLY FOR. A house whose blood sits at your court
  cannot plot to leave your realm. It will plot something quieter instead.
  And if that house breaks faith anyway - murders one of yours, walks out,
  declares for another claim - the hostage is FORFEIT, and the realm is told
  why.

      Execute a forfeit hostage   +15 Dread, and no Honour lost at all
      Execute one without cause   -25 Honour, +12 Dread, and it is told at
                                  every hearth in the realm

  That is the only place in the mod where killing a child costs you nothing,
  and it costs nothing only because their house spent it first.

  SENDING THEM HOME. +6 Honour, -3 Dread, and 20 relation with their house.
  Releasing a hostage you had every reason to keep is worth more than never
  taking one.

  TESTING
      wad.wards      who is held, how long, and whether their house is
                     forfeit; plus how many houses could be asked

  SETTINGS
      wardship_enabled=true
      hostage_dread=8              hostage_honour_loss=4
      ward_honour=4                ward_xp_per_week=60
      ward_release_honour=6
      execute_forfeit_dread=15     execute_no_cause_honour_loss=25


OUR DUTIES NOW PAY CONTROVERSY, LIKE BELLUM'S

  Bellum charges its own duties controversy - Uncover Dissent costs the
  Spymaster 1 a week, patrol failures cost the Marshal more. Ours charged
  nothing, which quietly made every one of them strictly better than
  Bellum's. That is fixed.

  The charge is worked out from the lean rather than set by hand, because it
  is the same fact twice: the nobility objects to exactly the things that
  make you feared, and forgives the things that make you loved.

      Mix the Poisons         +1.01 a week    Pardon the Condemned  -0.80
      The King's Justice      +0.94           Endow the Faith       -0.64
      Blackmail the Lords     +0.88           Ransom the Captives   -0.51
      Burn the Raiders' Ports +0.86           Tend the Sick         -0.51
      Silence the Critics     +0.83           Progress of the Realm -0.43

  Seventeen generate controversy, thirteen settle it. A seat carrying too
  much controversy is a seat you can lose, so the cruel duties now cost you
  the councillor as well as your Honour.

  That also gives you a use for the honourable ones beyond their lean: a
  councillor sitting on heavy controversy can be put on Endow the Faith or
  Pardon the Condemned to bring it down again.

      council_controversy=true     charge at all
      controversy_scale=1          multiply the whole thing


ON BELLUM CIVILE COMPATIBILITY

  Everything this mod does to Bellum is a POSTFIX or one of Bellum's own
  entry points. There is not a single prefix or transpiler anywhere in it:
  Bellum's code always runs to completion and we only adjust the answer, so
  no logic of Bellum's is ever skipped or replaced.

  The four patches: grant eligibility, clan liberty, hero names, office
  names. Each has its own config switch.

  Everything else goes through Bellum's own sanctioned methods - TryExecute
  Grant, TrySetServiceLevel, TrySetOfficeAssignment, the assignment registry
  - so Bellum validates it exactly as it validates the kingdom screen.

  Two things found and dealt with while checking:
    - Setting a duty from our court was passing ignoreCooldown=true, which
      would have been a way round Bellum's rules rather than a window onto
      them. It now passes false and obeys the cooldown.
    - Bellum's registry de-duplicates by id, so registering our thirty again
      on a second campaign in the same session cannot double them up.

  Known and accepted: our duties write settlement loyalty, security and
  prosperity daily, and so do Bellum's council models. The two stack rather
  than fight; ours are clamped and never move more than 0.35 a day.


PHASE 7: THE SUCCESSION

  Everything else in this mod only matters while you are alive. This is
  where a reign gets added up.

  WHERE EVERY HOUSE STANDS. Each house of your realm has a number, -100 to
  100, for whether it will kneel to your heir. It is not a mood. It is built
  out of the reign they lived through:

      their relation with you, and with your heir
      whether you made them Warden, or took their oath
      whether their child at your court is a ward (+18) or a hostage (-12)
      your Honour, which outlives you
      your Dread, which does NOT - fear holds a realm together while you
        live and is worth nothing the moment you stop being frightening

  That last one is the point of the whole phase. A reign built on Dread
  reads beautifully on the Honour and Dread screen and falls apart the day
  you die.

  Court: House and heirs. Name your heir, see every house and what it will
  do, and call the council.

  THE GREAT COUNCIL, IN PERSON. Call one at a seat of your own house and the
  ravens go out. Every lord of your realm is placed IN YOUR LORD'S HALL as a
  person standing on the floor. Walk up to them and they ask you, to your
  face, the thing that actually worries them - and it is a different
  question depending on what you have done to them:

      a house whose blood you hold asks whether their kin goes home
      a warden asks whether his style survives you
      a realm ruled by fear asks whether your heir frightens anyone
      a house with no love for you asks you to give them a reason

  Three ways to answer:

      Give them your word, with 20,000 gold behind it   +18 support
      Tell them the truth, that they must judge him     +8, +2 Honour
      Tell them they will kneel or not outlast you      +10, +3 Dread,
                                                        -2 Honour, and they
                                                        remember being made to

  A lord who rides a month to be asked and is NOT asked rides home having
  learned something: -6 support each, counted when the council rises.

  NEVER CALLING ONE IS ITS OWN ANSWER. Every year without a Great Council,
  every house in the realm drifts 4 further from your heir, and worse the
  longer you leave it - up to four times that. A realm left to wonder about
  the succession decides for itself, and it does not decide for you.

  THE DAY YOU DIE. If the realm stands at 55 or better, your heir is
  proclaimed and the lords who came to your councils keep the word they gave
  you there. If it does not, every house standing more than 35 against you
  takes its banners out of the realm, one after another, and you are told
  plainly that this is what the councils you did not hold were for.

  IF THE HALL CANNOT BE FILLED - no SandBox, an odd scene, anything - the
  council still sits, on the menu, and nothing is lost but the walking about.
  Look for 'council: hall behaviours bound to' in the log.

  TESTING
      wad.succession        heir, where the realm stands, every house's
                            support and what it would be naturally
      wad.succession_test   resolve the succession now, without dying for it

  SETTINGS
      succession_enabled=true
      council_in_person=true          false = menu only
      council_max_in_hall=12
      great_council_cooldown_days=420 one council every five years
      succession_drift_per_year=4
      council_ignored_cost=6
      council_promise_gold=20000
      succession_break_at=35


FIXED IN v0.9.4: THE HEIR SCREEN

  v0.9.1 unlocked the heir choice by standing Bellum's two law checks down.
  One of those checks returns a bool, and a Harmony prefix that returns bool
  is not answering "was this allowed" - it is telling Harmony whether the
  ORIGINAL method should run at all. Skipping its body left that answer as
  false, so the game skipped the heir screen's own logic and every name on it
  was unclickable.

  The fix is to skip the body AND answer true in its place. If you are on
  v0.9.1 to v0.9.3 and cannot pick an heir, this is why, and either take this
  build or set unlock_heir_choice=false.


THE GREAT COUNCIL IS NOW SAT, NOT WALKED

  The first version asked you to go and find your lords on the floor of the
  hall. It relied on a scene, on SandBox, and on every lord being where he
  ought to be, and it did not work.

  This one does what a court actually does. You stay in the seat. They are
  brought before you ONE AT A TIME, in an order that means something - your
  wardens first, because the realm is watching how they are treated, then
  whoever stands worst toward your heir. The herald calls a name, you get
  that lord's question, and you answer it before the next is called.

      "You have my word, and gold behind it."      +18, costs 20,000
      "I will not promise what I cannot hold
       from the grave."                            +8, +2 Honour
      "You will kneel, or your house will not
       outlast me."                                +10, +3 Dread, -2 Honour
      Wave them away without an answer             -3, and they rode a month

  The screen tells you who is before you, where they stand, whether you are
  holding their kin, how many you have heard and how many are still waiting.
  When the last one is answered the council rises by itself.

  Nothing in it depends on a scene loading or a mod being present. It is a
  throne and a queue.

      Court -> House and heirs -> Call a Great Council
      wad.council_call      calls one immediately, wherever you are

  council_in_person is now FALSE by default. Set it true and lords are ALSO
  placed in the hall to walk up to, but the throne is the real path and the
  one that is tested.


FIXED IN v0.9.2

  DUTIES VANISHED ON A SECOND SAVE. Loading a second campaign without
  restarting the game left you with "duties registered: 0 of 30" and an
  empty council. Bellum's assignment registry is static for the whole game
  process and THROWS on a duplicate id - it does not quietly ignore one, as
  was claimed here in v0.7.1. Registration now happens once per launch
  rather than once per campaign, and the registry is asked whether an id is
  already there before anything is offered to it.

  THE REALM WAS PUNISHED ON DAY ONE. A save that predates Phase 7 counted as
  ninety-nine years without a Great Council and lost 16 support across every
  house the moment the mod loaded. The succession clock now starts the first
  time the mod runs on a save.


PHASE 8: THE BLACK BANNER

  Aegon the Unworthy legitimised his bastards on his deathbed and bought the
  realm a century of war. The trap worked because at the time it looked like
  generosity.

  So this is not built as "would you like a rival house?" Nobody says yes to
  that. It is built as "would you like this child to be worth something?",
  and every yes is useful to you now and costs your heir later.

  RAISING A CHILD UP. Court: House and heirs -> Raise up a child of yours.
  Five things you can do for a child who is not your heir:

    Acknowledge them          +30 claim, +3 Honour. Baseborn only. This is
                              the one Aegon did.
    Give them a holding       +20 claim. A second seat of your house held by
                              blood you trust.
    Give them a command       +12 claim, 15,000 gold. Leadership, tactics,
                              and men who learn to follow them.
    Bond them to a dragon     +25 claim, +4 Dread. Your house has two riders.
    Seat them on your council +15 claim. They learn how the realm is run, in
                              front of every lord in it.

  Every one of those is a real asset while you live. Every one is also a
  reason the disaffected could follow them instead.

  WHO IS BASEBORN. Read from RoT Dynasty & Succession, which already tracks
  trueborn, bastard and legitimised. Without it nobody can be acknowledged
  and the other four still work. The log says which on load.

  THE BANNER RISES THREE WAYS

    IN YOUR LIFETIME. A child at 55 claim or more, a realm standing below 40,
    and an ambitious child - and they raise the banner against you. A war
    with your own blood, fought with everything you gave them.

    ON YOUR DEATH. The realm will not kneel, and now it fractures TOWARD
    someone instead of into nothing. Any child at 25 claim will do once the
    lords are angry enough.

    BECAUSE YOU CHOSE IT. You die, you carry on as a child who is NOT the
    named heir, and you are offered the founding: your own lands, your own
    dragon if one is bonded to you, and every lord who would not kneel.
    Nothing of the crown's comes with you. Only what is actually yours.

  WHO GOES TO THEM. Every house standing worse than -35 on the succession,
  EXCEPT any whose child you hold hostage - they think again, which is the
  first time in this mod a hostage has paid for itself in something other
  than Dread.

  A warden who goes over brings his whole realm with him. That is the single
  most dangerous thing in this mod, and it is a bill for a decision you made
  twenty years earlier.

  NAMING WHO YOU LIKE, AND WHAT IT COSTS

  Bellum gives every culture one of eight succession laws - Agnatic
  Primogeniture, Absolute Primogeniture, Tanistry, Elective Bloodright,
  Gerontocracy, Stratocracy, Agnatic Seniority, Tribal Shura - and enforces
  it on the heir screen. If yours only ever offered your eldest son, that is
  Agnatic Primogeniture working exactly as written. There is no way to change
  a culture's law from inside the game.

  So the law is not deleted here. You are allowed to BREAK it, and then you
  pay for it:

      Every house in the realm drops 20 support for an unlawful heir.

  That is Rhaenyra. She was named against Andal custom, the realm burned for
  it, and the reason it burned was that every lord had been promised
  something else. Now the Great Council is not a nice extra - it is the only
  way to argue an unlawful heir back up again, one lord at a time.

  Court: House and heirs shows your culture's law, who it says the seat
  belongs to, and marks the lawful choice when you name an heir.

      unlock_heir_choice=true      false = Bellum's law stands, as before
      unlawful_heir_penalty=20

  Nothing of Bellum's is reached into: its two law checks are stood down at
  the door and the consequence is taken on our side instead. Turn the setting
  off and Bellum's law is enforced exactly as it was.


  TESTING
      wad.claims               your children, their claims, the banner
                               plus your culture's law and its legal heir
      wad.claim_add <name> <n> push a claim up without waiting

  SETTINGS
      pretender_enabled=true
      pretender_while_alive=true     false = only ever on your death
      pretender_claim_needed=55      to rise in your lifetime
      pretender_claim_on_death=25    what the lords will settle for
      pretender_standing_below=40    they will not move above this
      command_gold=15000


WHAT IS NEXT
  Phase 9 - tourneys, and the broken feast.
