using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Bastard
{
	private const string RisenKey = "bs:risen";

	private const string HeadKey = "bs:head";

	private const string HouseKey = "bs:house";

	private const string RealmKey = "bs:realm";

	internal static bool Risen => Store.Get("bs:risen") == "1";

	internal static Hero Head => Find(Store.Get("bs:head"));

	internal static Clan House
	{
		get
		{
			try
			{
				string id = Store.Get("bs:house");
				return (!string.IsNullOrEmpty(id)) ? ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Func<Clan, bool>)((Clan c) => ((MBObjectBase)c).StringId == id && !c.IsEliminated)) : null;
			}
			catch
			{
				return null;
			}
		}
	}

	private static Hero Find(string id)
	{
		try
		{
			return (!string.IsNullOrEmpty(id)) ? ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero h) => ((MBObjectBase)h).StringId == id)) : null;
		}
		catch
		{
			return null;
		}
	}

	internal static void Offer(Hero dead)
	{
		try
		{
			if (!Cfg.Bastard || dead == null || !string.IsNullOrEmpty(Store.Get("bs:risen")))
			{
				return;
			}
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || playerClan.Kingdom == null)
			{
				return;
			}
			if (!Succession.Rules())
			{
				Log.Write("the bastard was not offered: your house rules nothing to divide");
				return;
			}
			if (Fiefs().Count < Cfg.BastardMinFiefs)
			{
				Log.Write("the bastard was not offered: your house holds too little to divide");
				return;
			}
			Hero heir = Hero.MainHero;
			Blade.Reckoning how;
			Hero val = Blade.Claimant(out how);
			if (val != null)
			{
				Reckon(dead, heir, val, how);
				return;
			}
			if (!Cfg.BastardStranger)
			{
				Log.Write("no child of yours is out there, and the stranger is turned off");
				return;
			}
			string desc = "A man came to the gate during the funeral and would not give his name to the guards.\n\nHe carried a token " + ((!dead.IsFemale) ? "your father" : "your mother") + " is said to have given away a long time ago, in a part of " + ((dead.BornSettlement == null) ? "the realm" : ((object)dead.BornSettlement.Name).ToString()) + " nobody in this house talks about. He has the look. Everyone who saw him says so, and then says they did not.\n\nHe is not asking to be acknowledged. He has already gone, and men have gone with him.\n\nSomewhere out there a banner is being sewn in your colours, the wrong way round.";
			Inquiry.Confirm("A Face You Know", desc, "Let it come", "There was no man at the gate", delegate
			{
				Rise(dead, heir);
			}, delegate
			{
				Store.Set("bs:risen", "declined");
				Log.Write("the bastard was turned away at the gate");
			});
		}
		catch (Exception ex)
		{
			Log.Write("offering the bastard failed: " + ex.Message);
		}
	}

	private static void Reckon(Hero dead, Hero heir, Hero him, Blade.Reckoning how)
	{
		try
		{
			string value = Lore.Blade();
			bool flag = how == Blade.Reckoning.Armed || how == Blade.Reckoning.Both;
			bool flag2 = how == Blade.Reckoning.Acknowledged || how == Blade.Reckoning.Both;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(him.Name).Append(" has not come to the funeral.\n\n");
			stringBuilder.Append((!flag2) ? "They were never written into the book. That has not stopped anyone from counting on their fingers.\n\n" : "You wrote them into the book yourself. They have your name, and every lord who knelt at that ceremony remembers doing it.\n\n");
			if (flag)
			{
				stringBuilder.Append("And they are carrying ").Append(value).Append(". You put it in their hand. Whatever anybody says about their birth, nobody can say you did not choose them.\n\n");
			}
			stringBuilder.Append("Men have been riding to them since the day you died.\n\n");
			stringBuilder.Append((!flag) ? "How far it goes is anyone's guess." : "This will be a war.");
			Inquiry.Confirm("A Child of Yours", stringBuilder.ToString(), "Let it come", "They would not dare", delegate
			{
				Rise(dead, heir, him, how);
			}, delegate
			{
				Store.Set("bs:risen", "declined");
				Log.Write("the reckoning was waved off");
			});
		}
		catch (Exception ex)
		{
			Log.Write("the reckoning failed: " + ex.Message);
		}
	}

	private static void Rise(Hero dead, Hero heir)
	{
		Rise(dead, heir, null, Blade.Reckoning.None);
	}

	private static void Rise(Hero dead, Hero heir, Hero already, Blade.Reckoning how)
	{
		try
		{
			if (already != null && (already == heir || already == Hero.MainHero || (Clan.PlayerClan != null && Clan.PlayerClan.Leader == already)))
			{
				Log.Write("the rising was stopped: the claimant is the one on the seat");
				Store.Set("bs:risen", null);
				Flow.Notify("There is nobody to raise a banner against you. The claim is yours, and you are already holding it.");
				return;
			}
			Clan playerClan = Clan.PlayerClan;
			Kingdom val = ((playerClan == null) ? null : playerClan.Kingdom);
			if (playerClan == null || val == null)
			{
				return;
			}
			Settlement val2 = Pick(Fiefs());
			if (val2 == null)
			{
				Log.Write("the bastard rose and there was nothing to give them");
				return;
			}
			Store.Set("bs:risen", "failed");
			Hero val3 = already ?? Sire(dead, val2);
			if (val3 == null)
			{
				Log.Write("the bastard could not be born");
				return;
			}
			Clan val4 = Raise(val3, val2, playerClan, Lore.Blade());
			if (val4 == null)
			{
				Log.Write("the bastard has no house to his name");
				if (already == null)
				{
					Unmake(val3);
				}
				return;
			}
			Store.Set("bs:risen", "1");
			if (already == null || how == Blade.Reckoning.Both)
			{
				Rename(val3);
			}
			if (already == null)
			{
				Claim(val3, dead);
			}
			try
			{
				ChangeOwnerOfSettlementAction.ApplyByGift(val2, val3);
			}
			catch (Exception ex)
			{
				Log.Write("the seat would not go with him: " + ex.Message);
			}
			Kingdom val5 = ((already == null || Blade.Crowns(how)) ? Crown(val4, val3, val2) : null);
			if (val5 == null)
			{
				Log.Write(string.Concat("no kingdom was founded; the bastard keeps ", val2.Name, " and nothing else"));
				Store.Set("bs:head", ((MBObjectBase)val3).StringId);
				Store.Set("bs:house", ((MBObjectBase)val4).StringId);
				Store.AddDeed(string.Concat(Standing.Date(), "  ", val3.Name, " took ", val2.Name, " and held it."));
				return;
			}
			int num = Defect(val, val5, val4, Blade.Share(how));
			if ((already != null) ? Blade.Declares(how) : Cfg.BastardWar)
			{
				try
				{
					DeclareWarAction.ApplyByDefault((IFaction)(object)val5, (IFaction)(object)val);
				}
				catch (Exception ex2)
				{
					Log.Write("the war would not be declared: " + ex2.Message);
				}
			}
			Store.Set("bs:head", ((MBObjectBase)val3).StringId);
			Store.Set("bs:house", ((MBObjectBase)val4).StringId);
			Store.Set("bs:realm", ((MBObjectBase)val5).StringId);
			Store.AddDeed(string.Concat(Standing.Date(), "  ", val3.Name, " raised a banner at ", val2.Name, "."));
			Log.Write(string.Concat("the bastard rises: ", val3.Name, " of ", val4.Name, " holds ", val2.Name, ", ", num, " house(s) went over, war=", Cfg.BastardWar));
			Tell(val3, val4, val5, val2, num, heir);
		}
		catch (Exception ex3)
		{
			Log.Write("the rising failed: " + ex3);
		}
	}

	private static Hero Sire(Hero dead, Settlement seat)
	{
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Expected O, but got Unknown
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Expected O, but got Unknown
		//IL_014b: Expected O, but got Unknown
		try
		{
			CultureObject culture = ((dead.Culture != null) ? dead.Culture : ((Clan.PlayerClan == null) ? null : Clan.PlayerClan.Culture));
			CharacterObject val = Template(culture);
			if (val == null)
			{
				Log.Write("no character template to build a bastard from");
				return null;
			}
			int num = (int)Math.Max(Cfg.BastardMinAge, Math.Min(Cfg.BastardMaxAge, dead.Age - (float)Cfg.BastardBornWhen));
			Hero val2 = HeroCreator.CreateSpecialHero(val, seat, (Clan)null, (Clan)null, num);
			if (val2 == null)
			{
				return null;
			}
			try
			{
				val2.ChangeState((CharacterStates)1);
			}
			catch (Exception ex)
			{
				Log.Write("the bastard would not wake: " + ex.Message);
			}
			try
			{
				EnterSettlementAction.ApplyForCharacterOnly(val2, seat);
			}
			catch (Exception ex2)
			{
				Log.Once("bastardplace", "the bastard could not be placed: " + ex2.Message);
			}
			string text = ((!(val2.FirstName != (TextObject)null)) ? "The Bastard" : ((object)val2.FirstName).ToString());
			string text2 = Surname(seat);
			try
			{
				val2.SetName(new TextObject("{=!}" + text + " " + text2, (Dictionary<string, object>)null), new TextObject("{=!}" + text, (Dictionary<string, object>)null));
			}
			catch (Exception ex3)
			{
				Log.Write("the name would not take: " + ex3.Message);
			}
			try
			{
				val2.EncyclopediaText = new TextObject("{=!}" + Story(val2, dead, seat, text, text2), (Dictionary<string, object>)null);
				val2.IsKnownToPlayer = true;
			}
			catch
			{
			}
			return val2;
		}
		catch (Exception ex4)
		{
			Log.Write("siring the bastard failed: " + ex4.Message);
			return null;
		}
	}

	private static void Rename(Hero him)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_0066: Expected O, but got Unknown
		try
		{
			if (him != null)
			{
				string text = ((!(him.FirstName != (TextObject)null)) ? "The Bastard" : ((object)him.FirstName).ToString());
				string text2 = Lore.Blade();
				him.SetName(new TextObject("{=!}" + text + " " + text2, (Dictionary<string, object>)null), new TextObject("{=!}" + text, (Dictionary<string, object>)null));
				Log.Write(text + " takes the name " + text2);
			}
		}
		catch (Exception ex)
		{
			Log.Once("bastardrename", "he kept his old name: " + ex.Message);
		}
	}

	private static void Claim(Hero him, Hero dead)
	{
		try
		{
			if (him != null && dead != null)
			{
				if (dead.IsFemale)
				{
					him.Mother = dead;
				}
				else
				{
					him.Father = dead;
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("the parentage would not take: " + ex.Message);
		}
	}

	private static CharacterObject Template(CultureObject culture)
	{
		try
		{
			List<CharacterObject> list = ((IEnumerable<CharacterObject>)CharacterObject.All).Where((CharacterObject c) => c != null && ((BasicCharacterObject)c).IsHero && (int)c.Occupation == 3 && c.Culture == culture).ToList();
			if (list.Count == 0)
			{
				list = ((IEnumerable<CharacterObject>)CharacterObject.All).Where((CharacterObject c) => c != null && ((BasicCharacterObject)c).IsHero && (int)c.Occupation == 3).ToList();
			}
			if (list.Count == 0)
			{
				return null;
			}
			List<CharacterObject> list2 = list.Where((CharacterObject c) => !((BasicCharacterObject)c).IsFemale).ToList();
			return Pick((list2.Count <= 0) ? list : list2);
		}
		catch
		{
			return null;
		}
	}

	private static Clan Raise(Hero him, Settlement seat, Clan yours, string name)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		try
		{
			Clan val = Clan.CreateClan("wad_bastard_" + CourtBehavior.Today());
			if (val == null)
			{
				return null;
			}
			Set(val, "Name", (object)new TextObject("{=!}" + Lore.HouseName(), (Dictionary<string, object>)null));
			Set(val, "InformalName", (object)new TextObject("{=!}" + name, (Dictionary<string, object>)null));
			val.Culture = ((yours == null) ? him.Culture : yours.Culture);
			val.Banner = Reversed(yours) ?? Banner.CreateRandomClanBanner(-1);
			if (val.Banner != null)
			{
				try
				{
					uint primaryColor = val.Banner.GetPrimaryColor();
					uint num = val.Banner.GetFirstIconColor();
					if (primaryColor == num || Bad(num))
					{
						num = val.Banner.GetSecondaryColor();
					}
					if (primaryColor == num || Bad(num))
					{
						val.Color = primaryColor;
						val.Color2 = primaryColor;
						Log.Write("the bastard's arms were left as drawn: no second colour to give them");
					}
					else
					{
						val.Color = primaryColor;
						val.Color2 = num;
						Set(val, "BannerBackgroundColorPrimary", primaryColor);
						Set(val, "BannerBackgroundColorSecondary", primaryColor);
						Set(val, "BannerIconColor", num);
						Log.Write("the house flies " + Hex(primaryColor) + " with its device in " + Hex(num));
					}
				}
				catch (Exception ex)
				{
					Log.Write("the bastard's colours would not take: " + ex.Message);
				}
			}
			Set(val, "Tier", Cfg.BastardTier);
			val.SetLeader(him);
			try
			{
				val.SetInitialHomeSettlement(seat);
			}
			catch (Exception ex2)
			{
				Log.Once("bastardhome", "the house has no seat recorded: " + ex2.Message);
			}
			Call(val, "CalculateMidSettlement");
			Announce(val);
			return val;
		}
		catch (Exception ex3)
		{
			Log.Write("raising the house failed: " + ex3.Message);
			return null;
		}
	}

	private static Banner Reversed(Clan yours)
	{
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected O, but got Unknown
		try
		{
			if (yours == null || yours.Banner == null)
			{
				return null;
			}
			Banner banner = yours.Banner;
			if (banner.GetBannerDataListCount() < 2)
			{
				Log.Write("your arms carry no device, so the bastard takes arms of his own");
				return null;
			}
			uint primaryColor = banner.GetPrimaryColor();
			uint firstIconColor = banner.GetFirstIconColor();
			if (primaryColor == firstIconColor || Bad(primaryColor) || Bad(firstIconColor))
			{
				Log.Write("your arms would not reverse legibly, so the bastard takes arms of his own");
				return null;
			}
			Banner val = new Banner(banner);
			Log.Write("arms reversed: ground " + Hex(firstIconColor) + " over device " + Hex(primaryColor) + " across " + banner.GetBannerDataListCount() + " layer(s)");
			val.ChangeBackgroundColor(firstIconColor, firstIconColor);
			val.ChangeIconColors(primaryColor);
			return val;
		}
		catch (Exception ex)
		{
			Log.Write("the banner would not reverse: " + ex.Message);
			return null;
		}
	}

	private static string Hex(uint colour)
	{
		return "0x" + colour.ToString("X8");
	}

	private static bool Bad(uint colour)
	{
		return colour == 3735928559u || colour == uint.MaxValue;
	}

	private static void Call(Clan c, string method)
	{
		try
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Clan), method, (Type[])null, (Type[])null);
			if (methodInfo != null && methodInfo.GetParameters().Length == 0)
			{
				methodInfo.Invoke(c, null);
			}
		}
		catch (Exception ex)
		{
			Log.Once("clancall", method + " failed on the bastard's house: " + ex.Message);
		}
	}

	private static void Announce(Clan house)
	{
		try
		{
			object obj = AccessTools.Property(typeof(CampaignEventDispatcher), "Instance")?.GetValue(null, null);
			MethodInfo methodInfo = ((obj != null) ? AccessTools.Method(obj.GetType(), "OnClanCreated", (Type[])null, (Type[])null) : null);
			if (!(methodInfo == null))
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				object[] array = new object[parameters.Length];
				for (int i = 0; i < parameters.Length; i++)
				{
					array[i] = ((!(parameters[i].ParameterType == typeof(Clan))) ? ((!parameters[i].ParameterType.IsValueType) ? null : Activator.CreateInstance(parameters[i].ParameterType)) : house);
				}
				methodInfo.Invoke(obj, array);
			}
		}
		catch (Exception ex)
		{
			Log.Once("clanannounce", "the new house was not announced: " + ex.Message);
		}
	}

	private static void Unmake(Hero him)
	{
		try
		{
			if (him != null && him.IsAlive)
			{
				KillCharacterAction.ApplyByRemove(him, false, true);
			}
		}
		catch (Exception ex)
		{
			Log.Once("unmake", "the stillborn bastard could not be removed: " + ex.Message);
		}
	}

	private static void Set(Clan c, string prop, object value)
	{
		try
		{
			PropertyInfo propertyInfo = AccessTools.Property(typeof(Clan), prop);
			MethodInfo methodInfo = ((!(propertyInfo != null)) ? null : propertyInfo.GetSetMethod(nonPublic: true));
			if (methodInfo != null)
			{
				methodInfo.Invoke(c, new object[1] { value });
			}
		}
		catch (Exception ex)
		{
			Log.Once("clanset", "could not set " + prop + " on the bastard's house: " + ex.Message);
		}
	}

	private static Kingdom Crown(Clan house, Hero him, Settlement seat)
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Expected O, but got Unknown
		try
		{
			if (house == null || house.Leader == null || house.Culture == null)
			{
				return null;
			}
			object obj = typeof(Campaign).GetField("KingdomManager")?.GetValue(Campaign.Current);
			MethodInfo methodInfo = ((obj != null) ? AccessTools.Method(obj.GetType(), "CreateKingdom", (Type[])null, (Type[])null) : null);
			if (methodInfo == null)
			{
				Log.Write("no kingdom founder on this build; the bastard stays a house");
				return null;
			}
			string text = Lore.RealmName();
			TextObject val = new TextObject("{=!}" + text, (Dictionary<string, object>)null);
			TextObject val2 = new TextObject("{=!}" + Lore.RealmInformal(), (Dictionary<string, object>)null);
			TextObject val3 = new TextObject("{=!}" + Lore.RealmStory(house, him, seat), (Dictionary<string, object>)null);
			ParameterInfo[] parameters = methodInfo.GetParameters();
			object[] array = new object[parameters.Length];
			int num = 0;
			for (int i = 0; i < parameters.Length; i++)
			{
				Type parameterType = parameters[i].ParameterType;
				if (parameterType == typeof(Clan))
				{
					array[i] = house;
				}
				else if (parameterType == typeof(CultureObject))
				{
					array[i] = house.Culture;
				}
				else if (parameterType == typeof(TextObject))
				{
					num++;
					array[i] = num switch
					{
						1 => val, 
						2 => val2, 
						3 => val3, 
						_ => null, 
					};
				}
				else
				{
					array[i] = ((!parameters[i].HasDefaultValue) ? null : parameters[i].DefaultValue);
				}
			}
			methodInfo.Invoke(obj, array);
			Kingdom kingdom = house.Kingdom;
			if (kingdom != null)
			{
				Log.Write("the bastard is crowned: " + kingdom.Name);
				Store.AddDeed(string.Concat(Standing.Date(), "  ", kingdom.Name, " was proclaimed."));
			}
			return kingdom;
		}
		catch (Exception ex)
		{
			Log.Write("crowning the bastard failed: " + ex.Message);
			return null;
		}
	}

	private static int Defect(Kingdom yours, Kingdom realm, Clan house, float share)
	{
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		try
		{
			List<Clan> list = ((IEnumerable<Clan>)yours.Clans).Where((Clan c) => c != null && c != Clan.PlayerClan && c != house && c != yours.RulingClan && !c.IsEliminated && c.Leader != null && c.Leader.IsAlive).ToList();
			if (list.Count == 0)
			{
				return 0;
			}
			Shuffle(list);
			int num2 = Math.Max(1, (int)Math.Round((float)list.Count * share));
			foreach (Clan item in list)
			{
				if (num >= num2)
				{
					break;
				}
				Held held = Wardship.From(item);
				if (held != null && held.Hostage && !held.Forfeit)
				{
					Log.Write(string.Concat("  ", item.Name, " stays - you hold their blood"));
					continue;
				}
				try
				{
					ChangeKingdomAction.ApplyByJoinToKingdomByDefection(item, yours, realm, CampaignTime.Zero, true);
					num++;
					Log.Write(string.Concat("  ", item.Name, " went over"));
				}
				catch (Exception ex)
				{
					Log.Once("defect", "a house could not go over: " + ex.Message);
				}
			}
		}
		catch (Exception ex2)
		{
			Log.Write("the defections failed: " + ex2.Message);
		}
		return num;
	}

	private static void Tell(Hero him, Clan house, Kingdom realm, Settlement seat, int gone, Hero heir)
	{
		try
		{
			string desc = string.Concat(him.Name, " has taken ", seat.Name, " and will not give it back.\n\n", realm.Name, " is proclaimed, and ", (gone <= 0) ? "no house has gone to him yet, which is not the same as none ever will." : (gone + " house" + ((gone != 1) ? "s have" : " has") + " gone over to him."), (!Cfg.BastardWar) ? "" : "\n\nThere is a war on as of this morning.", "\n\nHe flies your arms with the colours reversed, and he has as much of ", (heir == null) ? "your heir" : ((object)heir.Name).ToString(), "'s blood in him as they do.\n\nWhose side of this are you?");
			string yes = ((heir == null) ? "Stay with your house" : ("Stay as " + heir.Name));
			string no = "Take up the banner as " + him.Name;
			Inquiry.Confirm("Two Houses", desc, yes, no, delegate
			{
				Log.Write("you stayed with your own house");
			}, delegate
			{
				Become(him);
			});
		}
		catch (Exception ex)
		{
			Log.Write("the choice could not be offered: " + ex.Message);
		}
	}

	private static void Become(Hero him)
	{
		try
		{
			if (him != null && him.IsAlive)
			{
				MethodInfo methodInfo = AccessTools.Method("TaleWorlds.CampaignSystem.Actions.ChangePlayerCharacterAction:Apply", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Log.Write("this build will not let the player change character");
					Flow.Notify("You cannot take up the banner on this build.");
					return;
				}
				methodInfo.Invoke(null, new object[1] { him });
				Log.Write("you took up the bastard's banner as " + him.Name);
				Store.AddDeed(Standing.Date() + "  You took up the banner yourself.");
			}
		}
		catch (Exception ex)
		{
			Log.Write("taking up the banner failed: " + ex.Message);
		}
	}

	internal static List<Settlement> Fiefs()
	{
		List<Settlement> list = new List<Settlement>();
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null)
			{
				return list;
			}
			foreach (Settlement item in (List<Settlement>)(object)playerClan.Settlements)
			{
				if (item != null && (item.IsTown || item.IsCastle))
				{
					list.Add(item);
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static string Surname(Settlement seat)
	{
		return Surnames.Of(seat);
	}

	private static string Story(Hero him, Hero dead, Settlement seat, string given, string surname)
	{
		return Lore.Life(him, dead, seat, given, surname);
	}

	private static T Pick<T>(List<T> list)
	{
		if (list == null || list.Count == 0)
		{
			return default(T);
		}
		return list[MBRandom.RandomInt(list.Count)];
	}

	private static void Shuffle<T>(List<T> list)
	{
		for (int num = list.Count - 1; num > 0; num--)
		{
			int index = MBRandom.RandomInt(num + 1);
			T value = list[num];
			list[num] = list[index];
			list[index] = value;
		}
	}

	internal static string Summary()
	{
		try
		{
			if (!Cfg.Bastard)
			{
				return null;
			}
			string text = Store.Get("bs:risen");
			if (text == "declined")
			{
				return "  There was no man at the gate. There never was.";
			}
			if (text == "failed")
			{
				return null;
			}
			if (!Risen)
			{
				return null;
			}
			Clan house = House;
			if (house == null)
			{
				return "  The banner was raised against you once, and it was put down.";
			}
			Hero head = Head;
			return string.Concat("  ", house.Name, (head == null) ? "" : (" under " + head.Name), (house.Kingdom == null) ? "" : (", holding " + house.Kingdom.Name), ".");
		}
		catch
		{
			return null;
		}
	}
}
