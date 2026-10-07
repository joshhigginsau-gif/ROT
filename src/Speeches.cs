namespace WardensAndDragons
{
	// The words. Each speech is an opening, a heart, a line of colour and an
	// ending, drawn fresh each time and filled with the names of the day.
	internal static class Speeches
	{
		internal static string[][] Of(string theme)
		{
			switch (theme)
			{
			case "honour": return Honour;
			case "fear": return Fear;
			case "gold": return Gold;
			case "house": return House;
			case "dragon": return Dragon;
			case "vengeance": return Vengeance;
			case "laststand": return LastStand;
			case "walls": return Walls;
			}
			return Honour;
		}

		internal static readonly string[][] Honour = new string[3][]
		{
			new string[6]
			{
				"Men of {HOUSE}! Every one of you swore an oath. Today we keep it.",
				"Look at the man beside you. He has your back. Have his.",
				"They will sing of {PLACE} long after we are dust - let them sing it true.",
				"I asked nothing of you I would not ask of myself. Today I ask it of us all.",
				"There is no shame in fear. There is only shame in running from it.",
				"An oath is a small thing to say and a hard thing to keep. Keep it with me."
			},
			new string[7]
			{
				"{FOE} thinks we are farmers with spears. Show them what sworn men are.",
				"When this is over, I want to know every name of every man who stood. Stand.",
				"No man of mine strikes a man who yields. No man of mine yields.",
				"The gods are watching, and so am I. Neither of us will look away.",
				"We did not come to {PLACE} for gold or land. We came because it is right.",
				"Your fathers kept faith with mine. Today we keep faith with our sons.",
				"Hold the line, mind your brothers, and leave the rest to me."
			},
			new string[6]
			{
				"For {HOUSE} - and for every oath that was ever kept!",
				"Forward, and let no man say we broke!",
				"With me! Honour and steel!",
				"Today we are the men our sons will want to be. Forward!",
				"Stand fast, strike true, and come home with your names clean!",
				"For the realm, for {HOUSE}, for each other - advance!"
			}
		};

		internal static readonly string[][] Fear = new string[3][]
		{
			new string[6]
			{
				"You know what I am. So does {FOE}. Let them remember it.",
				"Look across at them. Every one of them has heard my name. Most of them are shaking.",
				"I have burned men for less than what {FOEHOUSE} has done.",
				"I will tell you what happens today, so none of you are surprised.",
				"Some of you are afraid. Good. Be more afraid of me than of them.",
				"There are two ways off this field: through them, or past me."
			},
			new string[7]
			{
				"Any man who runs will hang from the nearest tree, and I will choose the branch myself.",
				"When we are done, {PLACE} will be a word mothers use to frighten children.",
				"Leave none standing who raised a blade. Spare the ones who kneel - they will tell the others.",
				"Take their banners. Take their heads. Leave their crows fat.",
				"{FOE} will kneel in the mud before dusk, or not have knees to kneel with.",
				"They think there are rules to this. Teach them.",
				"I want them to remember this day with their eyes shut for the rest of their lives."
			},
			new string[6]
			{
				"No mercy for the armed. Go!",
				"Break them!",
				"Let them learn what {HOUSE} is!",
				"Now - make them afraid!",
				"Kill them all, and let the gods sort the rest!",
				"Forward, and do not stop until there is nothing left to stop for!"
			}
		};

		internal static readonly string[][] Gold = new string[3][]
		{
			new string[6]
			{
				"Lads, look at them! Every one of them wearing a month's wages.",
				"{FOE} brought a baggage train. I'd hate to see it go to waste.",
				"I am not going to tell you about honour. I am going to tell you about silver.",
				"There is a purse at the end of this day for every man still standing.",
				"Some of you are here for glory. The rest of you, listen closely.",
				"I promised you plunder when you signed. Here it is, standing in a line."
			},
			new string[7]
			{
				"What you take off a dead man today is yours. I won't ask for a penny of it.",
				"Double pay for every man who stands where I put him.",
				"Their lords are worth a ransom. Take them alive and you'll drink for a year.",
				"There's good steel over there - better than what you're holding. Go and get it.",
				"{PLACE} has a market, a treasury and a lord's cellar. All of it ours by supper.",
				"Every banner you bring me back is a handful of gold.",
				"I never yet left my men poorer than I found them."
			},
			new string[6]
			{
				"Go and get rich!",
				"For gold, lads - and plenty of it!",
				"The spoils to the bold!",
				"Forward - their silver is waiting!",
				"Last one to their wagons buys the wine!",
				"Take everything!"
			}
		};

		internal static readonly string[][] House = new string[3][]
		{
			new string[6]
			{
				"Look up. That is the banner of {HOUSE}. It has never been taken.",
				"Men of {REALM}! This is our land, and they are standing on it.",
				"My house has stood a long time. It will be standing tonight.",
				"I am {ME} of {HOUSE}, and you are my people. That means something today.",
				"{REALM} did not raise you to watch it burn.",
				"Every man here is {HOUSE} today, whatever his father's name."
			},
			new string[7]
			{
				"{FOEREALM} wants what we have. Let them come and take it.",
				"Your homes are behind us. Your wives, your children. Nothing passes.",
				"{FOE} has no claim here that a sword cannot answer.",
				"When the realm is old, it will remember who stood at {PLACE}.",
				"We are the shield of {REALM}. A shield does not step aside.",
				"Our grandsons will tell this to their grandsons. Give them something to tell.",
				"The crown is watching. So is every house that ever doubted us."
			},
			new string[6]
			{
				"For {REALM}!",
				"{HOUSE}! {HOUSE}!",
				"For hearth and banner - forward!",
				"For the realm, and for {HOUSE}!",
				"Raise the banner and follow it!",
				"For our homes - charge!"
			}
		};

		internal static readonly string[][] Dragon = new string[3][]
		{
			new string[6]
			{
				"You hear that? That is {DRAGON}. That is the sound of their end.",
				"They brought spears. We brought fire.",
				"{FOE} has never seen a dragon at war. Today they will see one.",
				"Look up when you are tired. {DRAGON} will be there.",
				"There is an old word for what we are about to do to them. It is fire.",
				"When the sky darkens, do not be afraid. That shadow is ours."
			},
			new string[7]
			{
				"Hold them where they stand. I will do the rest from above.",
				"When {DRAGON} comes down on their lines, go in behind the fire.",
				"Fire and blood - that is the old promise of my blood. Today I keep it.",
				"No wall, no shield wall, no army of men has ever stood against a dragon. They will not be the first.",
				"Keep your heads low and your spears high, and leave the sky to me.",
				"They will break when they see her. Be there when they do.",
				"There will be smoke. Walk through it."
			},
			new string[6]
			{
				"Fire and blood!",
				"Dracarys!",
				"For {HOUSE} and for the fire!",
				"Forward - and follow the flames!",
				"The dragon flies - so do we!",
				"Burn them!"
			}
		};

		internal static readonly string[][] Vengeance = new string[3][]
		{
			new string[6]
			{
				"You all know what {FOEHOUSE} did. I have not forgotten one day of it.",
				"{FOE} is over there. I have waited a long time to say that.",
				"There is a debt owed to us on this field, and today we collect.",
				"Some of you lost brothers to {FOEHOUSE}. Today you can tell them they are answered.",
				"I swore a thing, a long time ago, about {FOEHOUSE}. Help me keep it.",
				"The North remembers. The South remembers. {HOUSE} remembers."
			},
			new string[7]
			{
				"Bring me {FOE}. Alive if you can, but bring them.",
				"Not one of us will sleep easy until {FOEHOUSE} is a name in an old book.",
				"Every man who fell to them is standing behind you now. Feel them push.",
				"They thought there would be no reckoning. This is it.",
				"No ransom, no terms, no talking. We are past talking.",
				"I have dreamed of this field. In the dream, they ran.",
				"Remember the dead. Then go and make more of theirs."
			},
			new string[6]
			{
				"For the fallen!",
				"Vengeance!",
				"Remember - and repay!",
				"For every one of ours - one of theirs! Forward!",
				"Let {FOEHOUSE} pay in full!",
				"Now! For the debt!"
			}
		};

		internal static readonly string[][] LastStand = new string[3][]
		{
			new string[6]
			{
				"Yes, they are many. {THEM} of them, {US} of us. I like those odds.",
				"Count them if you like. I have. It doesn't matter.",
				"There are more of them than there are of us. There always are, in the songs worth singing.",
				"If this is our last day, let it be the one they talk about.",
				"Every man here is worth three of theirs. Today you will prove it.",
				"They have the numbers. We have the ground, and we have each other."
			},
			new string[7]
			{
				"A narrow front, a strong line, and nobody steps back. That is all it takes.",
				"Every one of them that falls is one less. Do the arithmetic with your sword.",
				"They are tired of marching. We have been waiting. That counts for something.",
				"Help is coming. Until it does, we are the wall.",
				"Make them pay for every step. When they have paid enough, they'll stop.",
				"Do not look at how many there are. Look at the one in front of you.",
				"A thousand years from now, someone will say the name {PLACE} and mean us."
			},
			new string[6]
			{
				"Stand and die, or stand and win - but stand!",
				"Hold! Hold! Hold!",
				"Make them remember the few!",
				"Not one step back!",
				"Together, now - and forever after!",
				"Let them come!"
			}
		};

		internal static readonly string[][] Walls = new string[3][]
		{
			new string[6]
			{
				"Those walls are stone. Stone does not fight. The men on it do, and they are frightened.",
				"Behind these walls are our people. Nothing comes over them.",
				"{PLACE} has stood a long time. It has never seen the like of us.",
				"I won't pretend this is easy. It is not. It is only necessary.",
				"Ladders, rams and stubbornness. We have all three.",
				"Every wall ever built was built to be tested. Today we test it."
			},
			new string[7]
			{
				"The first man on the wall will never pay for a drink again in my hall.",
				"Keep your shields up on the climb and your swords out at the top.",
				"Push the ladders off. Pour the oil. Drop the stones. Again and again.",
				"Once the gate goes, they are ours. Make it go.",
				"They are hungry, they are tired, and they know they are beaten. Show them.",
				"Hold the breach and the day is ours.",
				"Up there is everything they have. Take it from them."
			},
			new string[6]
			{
				"To the walls!",
				"Over the top!",
				"Break the gate!",
				"Hold the walls - for {HOUSE}!",
				"Up, and in!",
				"For {PLACE}!"
			}
		};

		internal static readonly string[] Colour = new string[14]
		{
			"I see old faces here, and new ones. By tonight you will all be veterans.",
			"The ground is good, the wind is with us, and the gods are not fools.",
			"Drink tonight. Fight now.",
			"When you are tired, think of home. When you are afraid, think of the man beside you.",
			"I fought at worse places than {PLACE}, against worse men than {FOE}.",
			"Keep your formation and they will break on you like water on rock.",
			"A cold morning, a hot fight and a long evening - that is a soldier's whole life.",
			"Some of you won't see the sun go down. I'll make sure you're remembered.",
			"They are men like you. They bleed like you. They are not you.",
			"Watch the banner. Where it goes, you go.",
			"Pray if you pray. Spit if you don't. Then get ready.",
			"I will be in the front. Look for me.",
			"The maesters will write tonight. Give them something worth the ink.",
			"Steady breath. Steady hands. Steady hearts."
		};

		internal static readonly string[] Botch = new string[8]
		{
			"...and so, in conclusion - er - for the realm. Yes.",
			"Something about the gods. Or honour. The ranks aren't sure which.",
			"{ME} loses the thread halfway, and starts again, and loses it again.",
			"The wind takes most of it. The rest is hard to hear over the horses.",
			"A joke that does not land. Somebody coughs.",
			"{ME} mixes up the names of the enemy and the place. Someone in the second rank corrects them.",
			"It goes on a little too long. The men shift their feet.",
			"The horse picks that moment to wander off. It is a while before anyone can hear again."
		};

		internal static readonly string[] EnemyOpen = new string[10]
		{
			"Across the field, {FOE} raises a hand: \"There they are - {HOUSE}, come to die at {PLACE}.\"",
			"{FOE} rides the length of their line: \"Look at them! {US} of them, and half of those boys.\"",
			"From the other side, {FOE}'s voice carries: \"{ME} of {HOUSE} thinks this land is theirs. Show them whose it is.\"",
			"{FOE} stands in the stirrups: \"For {FOEREALM}! They came here - they do not leave!\"",
			"{FOE} points their sword across the field: \"I have beaten better than {HOUSE}.\"",
			"An answer comes from the enemy lines - {FOE}: \"Do you hear that? That is a lot of noise from very few men.\"",
			"{FOE} lifts their banner: \"{FOEHOUSE} has never broken. We will not start today.\"",
			"{FOE}: \"They talk well. Let us see if they bleed as well as they talk.\"",
			"{FOE}'s horn sounds, and then the voice: \"Remember who you are. Remember who they are.\"",
			"{FOE}: \"Today {ME} of {HOUSE} learns what {FOEREALM} is made of.\""
		};

		internal static readonly string[] EnemyClose = new string[8]
		{
			"\"Forward, for {FOEREALM}!\" - and their ranks roar back.",
			"\"Kill them all!\" - and the enemy lines bang their shields.",
			"\"With me!\" - and a great shout comes from the other side.",
			"\"No quarter!\" - and their men take it up, over and over.",
			"\"For {FOEHOUSE}!\" - and the enemy horns answer.",
			"\"Break them, and we go home!\" - the enemy cheers.",
			"\"Stand, and they cannot win!\" - their spears go up as one.",
			"\"Now!\" - and their whole line roars."
		};

		internal static readonly string[] EnemyFlat = new string[6]
		{
			"\"...Forward!\" A thin cheer from their side.",
			"The rest is lost on the wind. Their ranks barely stir.",
			"Their men cheer, but it sounds like duty.",
			"\"For - for {FOEREALM}!\" A few voices answer.",
			"A horse shies under {FOE}, and the moment is lost.",
			"Somebody on their side laughs at the wrong moment."
		};
	}
}
