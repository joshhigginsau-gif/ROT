using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace WardensAndDragons;

internal static class Lore
{
	internal const string DefaultSurname = "Rivers";

	private static readonly string[] Mottos = new string[20]
	{
		"Ours By Right Denied", "The Same Blood", "Unacknowledged, Unforgotten", "We Were Never Given", "Reversed, Not Lesser", "Count Us Now", "Born In The Dark", "No Man's Leavings", "We Do Not Ask Twice", "The Debt Is Older Than You",
		"What Was Withheld", "Half A Name Is Still A Name", "Sown And Reaped", "We Remember The Gate", "Take What Is Owed", "Blood Does Not Bastardise", "By Right Of Birth Refused", "The Colours Turned", "We Kept The Token", "Answer For It"
	};

	private static readonly string[] Hidings = new string[8] { "fostered out to a hedge knight who was paid once a year and never asked why", "raised in a holdfast two valleys over by a woman who had been a maid at court", "put on a ship as a boy and written down as cargo", "given to a septry as an orphan of the wars, with coin enough to keep the question shut", "kept in the household as a stablehand, in plain sight, for nineteen years", "sent to the free companies young, under a name that was not his", "quartered with a merchant family who thought they were being generous", "left with a miller's wife who had just lost her own and asked nothing" };

	private static readonly string[] Tokens = new string[8] { "a signet ring with the arms filed half away", "a torn banner, folded small enough to carry in a boot", "a dagger with the house's device on the pommel", "a letter in a hand the maesters say they recognise", "a cloak clasp nobody outside the family has worn in sixty years", "a lock of hair in a lead case, and a name written under it", "half a silver coin, cut clean, the other half never found", "a child's toy sword made at the castle forge, stamped underneath" };

	private static readonly string[] Lives = new string[8] { "He has spent his adult life under arms and is said to be better at it than anyone would like.", "He worked as a factor for a trading house and knows exactly what every holding in the region is worth.", "He fought a long way from here for people who paid badly, and came back with men who owe him.", "He has been a steward, which means he has read the accounts of half the houses in the realm.", "He was a sellsword captain and never lost enough of his company to be forgotten.", "He has spent ten years as a guard on the roads and knows every one of them in the dark.", "He kept a ferry, and every lord in the region has been carried across by him at least once.", "He served in a garrison and was well liked there, which is a harder thing to arrange than it sounds." };

	private static readonly string[] Swords = new string[24]
	{
		"Blackfyre", "Bittersteel", "Brightroar", "Greyflame", "Nightfall", "Widowbane", "Palefang", "Redtide", "Coldbrand", "Ashthorn",
		"Saltclaw", "Ironwake", "Duskrain", "Winterbane", "Stormedge", "Dreadtooth", "Sorrowsong", "Orphanmaker", "Ravenmourn", "Hollowfyre",
		"Gallowsteel", "Lastlight", "Kinslayer", "Truthbane"
	};

	internal const string BladeKey = "bs:blade";

	private static readonly string[] RealmShapes = new string[6] { "The Kingdom of {B}", "The {B} Throne", "The Realm of {B}", "The Crown of {B}", "The Kingdom of House {B}", "{B}" };

	private static string Any(string[] pool)
	{
		return pool[MBRandom.RandomInt(pool.Length)];
	}

	internal static string Motto()
	{
		string text = Store.Get("bs:motto");
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2 = Any(Mottos);
		Store.Set("bs:motto", text2);
		return text2;
	}

	internal static string Life(Hero him, Hero dead, Settlement seat, string given, string surname)
	{
		try
		{
			string value = ((Clan.PlayerClan == null) ? "the royal house" : ((object)Clan.PlayerClan.Name).ToString());
			string value2 = ((seat == null) ? "the realm" : ((object)seat.Name).ToString());
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(given).Append(" ").Append(surname)
				.Append(" was born to ")
				.Append((dead == null) ? "the late ruler" : ((object)dead.Name).ToString())
				.Append(" and was never acknowledged.\n\n");
			stringBuilder.Append("He was ").Append(Any(Hidings)).Append(". ");
			stringBuilder.Append("What he kept was ").Append(Any(Tokens)).Append(", and it was enough to be listened to.\n\n");
			string value3 = Blade();
			stringBuilder.Append("And he carried ").Append(value3).Append(", the ancestral sword of ")
				.Append(value)
				.Append(". ");
			stringBuilder.Append((!BladeIsYours()) ? "How it left the house nobody will say aloud. But a blade is an argument no maester can write around, and he had it.\n\n" : "It was not his to carry. Every man who saw it knew whose hand it belonged in, and that is exactly why they followed the hand that held it.\n\n");
			stringBuilder.Append(Any(Lives)).Append("\n\n");
			stringBuilder.Append("On the death of ").Append((dead == null) ? "the ruler" : ((object)dead.Name).ToString()).Append(" he came to ")
				.Append(value2)
				.Append(" and took it, and raised the arms of ")
				.Append(value)
				.Append(" with the colours reversed - the old sign of a branch that was never written into the book.\n\n");
			stringBuilder.Append("He took the sword's name for his own, and House ").Append(value3).Append(" has carried it since.\n\n");
			stringBuilder.Append("Its words are \"").Append(Motto()).Append("\".");
			return stringBuilder.ToString();
		}
		catch
		{
			return given + " " + surname + " was born to a ruler who never acknowledged him, and took up arms on their death.";
		}
	}

	internal static string Named()
	{
		try
		{
			string text = Store.Get("bs:blade");
			return (!string.IsNullOrEmpty(text)) ? text : null;
		}
		catch
		{
			return null;
		}
	}

	internal static string Blade()
	{
		string text = Named();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		string text2 = Store.Get("bs:sword");
		if (!string.IsNullOrEmpty(text2))
		{
			return text2;
		}
		string text3 = Any(Swords);
		Store.Set("bs:sword", text3);
		return text3;
	}

	internal static bool BladeIsYours()
	{
		return !string.IsNullOrEmpty(Named());
	}

	private static int Shape()
	{
		int i = Store.GetI("bs:name", -1);
		if (i >= 0 && i < RealmShapes.Length)
		{
			return i;
		}
		int num = MBRandom.RandomInt(RealmShapes.Length);
		Store.SetI("bs:name", num);
		return num;
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

	internal static string RealmStory(Clan house, Hero him, Settlement seat)
	{
		try
		{
			string value = ((Clan.PlayerClan == null) ? "the royal house" : ((object)Clan.PlayerClan.Name).ToString());
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Declared from ").Append((seat == null) ? "a castle taken in the night" : ((object)seat.Name).ToString()).Append(" by ")
				.Append((him == null) ? "a baseborn claimant" : ((object)him.Name).ToString())
				.Append(", who is of the blood of ")
				.Append(value)
				.Append(" and was never written into its book.\n\n");
			stringBuilder.Append("It flies the arms of ").Append(value).Append(" with the colours reversed, and it does not consider itself a rebellion. ");
			stringBuilder.Append("By its own account it is the elder claim, kept out of the light by people who found that convenient - ");
			stringBuilder.Append("and it is named for the sword its founder carried out of ").Append(value).Append("'s own hall, which is the only part of the argument nobody disputes.\n\n");
			stringBuilder.Append("Its words are \"").Append(Motto()).Append("\".");
			return stringBuilder.ToString();
		}
		catch
		{
			return "A breakaway realm founded by a baseborn son of the royal house.";
		}
	}
}
