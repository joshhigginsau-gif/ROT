using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace WardensAndDragons
{
	// Where the bastard's story comes from.
	//
	// Everything here is assembled at the moment the banner goes up, out of
	// the campaign it is actually happening in: the ruler who sired him, the
	// castle he took, the house he is breaking away from. Nothing is a fixed
	// string with a name dropped into it, because that reads as a form letter
	// the second time you see it.
	internal static class Lore
	{
		// The surname table lives in Surnames.cs now. It has to match RoT's
		// nine exactly, and having two copies of it was a standing invitation
		// for them to drift apart.
		internal const string DefaultSurname = Surnames.Default;

		// ------------------------------------------------------------------
		// mottos

		// Deliberately all of a kind: short, grim, and about being owed
		// something. A house founded this way does not get a motto about
		// honour or growing things.
		private static readonly string[] Mottos = new string[20]
		{
			"Ours By Right Denied",
			"The Same Blood",
			"Unacknowledged, Unforgotten",
			"We Were Never Given",
			"Reversed, Not Lesser",
			"Count Us Now",
			"Born In The Dark",
			"No Man's Leavings",
			"We Do Not Ask Twice",
			"The Debt Is Older Than You",
			"What Was Withheld",
			"Half A Name Is Still A Name",
			"Sown And Reaped",
			"We Remember The Gate",
			"Take What Is Owed",
			"Blood Does Not Bastardise",
			"By Right Of Birth Refused",
			"The Colours Turned",
			"We Kept The Token",
			"Answer For It"
		};

		// How he was hidden, and by whom.
		private static readonly string[] Hidings = new string[8]
		{
			"fostered out to a hedge knight who was paid once a year and never asked why",
			"raised in a holdfast two valleys over by a woman who had been a maid at court",
			"put on a ship as a boy and written down as cargo",
			"given to a septry as an orphan of the wars, with coin enough to keep the question shut",
			"kept in the household as a stablehand, in plain sight, for nineteen years",
			"sent to the free companies young, under a name that was not his",
			"quartered with a merchant family who thought they were being generous",
			"left with a miller's wife who had just lost her own and asked nothing"
		};

		// The token. Always a thing a ruler could plausibly have given away,
		// and always the reason anybody believes him now.
		private static readonly string[] Tokens = new string[8]
		{
			"a signet ring with the arms filed half away",
			"a torn banner, folded small enough to carry in a boot",
			"a dagger with the house's device on the pommel",
			"a letter in a hand the maesters say they recognise",
			"a cloak clasp nobody outside the family has worn in sixty years",
			"a lock of hair in a lead case, and a name written under it",
			"half a silver coin, cut clean, the other half never found",
			"a child's toy sword made at the castle forge, stamped underneath"
		};

		// What he did before this, which is why men follow him at all.
		private static readonly string[] Lives = new string[8]
		{
			"He has spent his adult life under arms and is said to be better at it than anyone would like.",
			"He worked as a factor for a trading house and knows exactly what every holding in the region is worth.",
			"He fought a long way from here for people who paid badly, and came back with men who owe him.",
			"He has been a steward, which means he has read the accounts of half the houses in the realm.",
			"He was a sellsword captain and never lost enough of his company to be forgotten.",
			"He has spent ten years as a guard on the roads and knows every one of them in the dark.",
			"He kept a ferry, and every lord in the region has been carried across by him at least once.",
			"He served in a garrison and was well liked there, which is a harder thing to arrange than it sounds."
		};

		private static string Any(string[] pool)
		{
			return pool[MBRandom.RandomInt(pool.Length)];
		}

		// A motto, held steady for the campaign once it has been rolled, so
		// the encyclopedia and the court screen never disagree about it.
		internal static string Motto()
		{
			string kept = Store.Get("bs:motto");
			if (!string.IsNullOrEmpty(kept))
			{
				return kept;
			}
			string m = Any(Mottos);
			Store.Set("bs:motto", m);
			return m;
		}

		// ------------------------------------------------------------------
		// the pages

		// The hero's own encyclopedia entry.
		internal static string Life(Hero him, Hero dead, Settlement seat, string given, string surname)
		{
			try
			{
				string house = (Clan.PlayerClan != null) ? Clan.PlayerClan.Name.ToString() : "the royal house";
				string where = (seat != null) ? seat.Name.ToString() : "the realm";
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append(given).Append(" ").Append(surname).Append(" was born to ")
				  .Append((dead != null) ? dead.Name.ToString() : "the late ruler")
				  .Append(" and was never acknowledged.\n\n");
				sb.Append("He was ").Append(Any(Hidings)).Append(". ");
				sb.Append("What he kept was ").Append(Any(Tokens)).Append(", and it was enough to be listened to.\n\n");
				// The blade is the argument. Everything above only got him a
				// hearing; this is why men knelt.
				string blade = Blade();
				sb.Append("And he carried ").Append(blade).Append(", the ancestral sword of ").Append(house).Append(". ");
				sb.Append(BladeIsYours()
					? "It was not his to carry. Every man who saw it knew whose hand it belonged in, and that is exactly why they followed the hand that held it.\n\n"
					: "How it left the house nobody will say aloud. But a blade is an argument no maester can write around, and he had it.\n\n");
				sb.Append(Any(Lives)).Append("\n\n");
				sb.Append("On the death of ").Append((dead != null) ? dead.Name.ToString() : "the ruler")
				  .Append(" he came to ").Append(where)
				  .Append(" and took it, and raised the arms of ").Append(house)
				  .Append(" with the colours reversed - the old sign of a branch that was never written into the book.\n\n");
				sb.Append("He took the sword's name for his own, and House ").Append(blade)
				  .Append(" has carried it since.\n\n");
				sb.Append("Its words are \"").Append(Motto()).Append("\".");
				return sb.ToString();
			}
			catch
			{
				return given + " " + surname + " was born to a ruler who never acknowledged him, and took up arms on their death.";
			}
		}

		// The blade.
		//
		// This is the Blackfyre logic, and it is worth being exact about it.
		// Blackfyre was House Targaryen's ancestral Valyrian sword. Aegon IV
		// gave it to his bastard instead of to his trueborn heir, and THAT
		// GIFT is why the rebellion had legitimacy at all - Daemon took the
		// sword's name for his house because the sword was the argument.
		//
		// So a bastard house here is named after the thing he took that should
		// have gone to your heir. The token in his backstory stops being
		// decoration and becomes the reason anybody kneels to him.
		//
		// If the player has named their own house's blade, that name is used
		// instead of anything in this list - see Blade().
		private static readonly string[] Swords = new string[24]
		{
			"Blackfyre", "Bittersteel", "Brightroar", "Greyflame",
			"Nightfall", "Widowbane", "Palefang", "Redtide",
			"Coldbrand", "Ashthorn", "Saltclaw", "Ironwake",
			"Duskrain", "Winterbane", "Stormedge", "Dreadtooth",
			"Sorrowsong", "Orphanmaker", "Ravenmourn", "Hollowfyre",
			"Gallowsteel", "Lastlight", "Kinslayer", "Truthbane"
		};

		internal const string BladeKey = "bs:blade";

		// What the player called their own ancestral blade, if they ever did.
		internal static string Named()
		{
			try
			{
				string n = Store.Get(BladeKey);
				return string.IsNullOrEmpty(n) ? null : n;
			}
			catch
			{
				return null;
			}
		}

		// The name the bastard's house will carry. Rolled once and kept, so
		// the house, the kingdom and the encyclopedia never disagree.
		internal static string Blade()
		{
			string yours = Named();
			if (!string.IsNullOrEmpty(yours))
			{
				return yours;
			}
			string kept = Store.Get("bs:sword");
			if (!string.IsNullOrEmpty(kept))
			{
				return kept;
			}
			string rolled = Any(Swords);
			Store.Set("bs:sword", rolled);
			return rolled;
		}

		// Was the blade one the player named themselves? The backstory reads
		// differently when it was - it is their own loss being described.
		internal static bool BladeIsYours()
		{
			return !string.IsNullOrEmpty(Named());
		}

		// ------------------------------------------------------------------
		// what the realm is called

		private static readonly string[] RealmShapes = new string[6]
		{
			"The Kingdom of {B}",
			"The {B} Throne",
			"The Realm of {B}",
			"The Crown of {B}",
			"The Kingdom of House {B}",
			"{B}"
		};

		private static int Shape()
		{
			int kept = Store.GetI("bs:name", -1);
			if (kept >= 0 && kept < RealmShapes.Length)
			{
				return kept;
			}
			int i = MBRandom.RandomInt(RealmShapes.Length);
			Store.SetI("bs:name", i);
			return i;
		}

		internal static string RealmName()
		{
			try
			{
				return RealmShapes[Shape()].Replace("{B}", Blade());
			}
			catch
			{
				return "The Reversed Banner";
			}
		}

		// The short form, for "at war with ___".
		internal static string RealmInformal()
		{
			try
			{
				return Blade();
			}
			catch
			{
				return "The Reversed Banner";
			}
		}

		internal static string HouseName()
		{
			return "House " + Blade();
		}

		// The kingdom's encyclopedia page.
		internal static string RealmStory(Clan house, Hero him, Settlement seat)
		{
			try
			{
				string old = (Clan.PlayerClan != null) ? Clan.PlayerClan.Name.ToString() : "the royal house";
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append("Declared from ").Append((seat != null) ? seat.Name.ToString() : "a castle taken in the night")
				  .Append(" by ").Append((him != null) ? him.Name.ToString() : "a baseborn claimant")
				  .Append(", who is of the blood of ").Append(old).Append(" and was never written into its book.\n\n");
				sb.Append("It flies the arms of ").Append(old)
				  .Append(" with the colours reversed, and it does not consider itself a rebellion. ");
				sb.Append("By its own account it is the elder claim, kept out of the light by people who found that convenient - ");
				sb.Append("and it is named for the sword its founder carried out of ").Append(old)
				  .Append("'s own hall, which is the only part of the argument nobody disputes.\n\n");
				sb.Append("Its words are \"").Append(Motto()).Append("\".");
				return sb.ToString();
			}
			catch
			{
				return "A breakaway realm founded by a baseborn son of the royal house.";
			}
		}
	}
}
