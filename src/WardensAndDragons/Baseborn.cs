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
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Baseborn
{
	private const string Prefix = "bb:";

	internal static List<Kid> All()
	{
		List<Kid> list = new List<Kid>();
		try
		{
			foreach (string item in Store.Keys("bb:"))
			{
				Kid kid = Kid.Unpack(item.Substring("bb:".Length), Store.Get(item));
				if (kid != null)
				{
					list.Add(kid);
				}
			}
		}
		catch
		{
		}
		return list.OrderBy((Kid k) => k.Night).ToList();
	}

	internal static void Save(Kid k)
	{
		Store.Set("bb:" + k.Id, k.Pack());
	}

	internal static void Drop(Kid k)
	{
		Store.Set("bb:" + k.Id, null);
	}

	internal static Hero HeroOf(Kid k)
	{
		return Find(k?.Hero);
	}

	internal static Hero OtherOf(Kid k)
	{
		return Find(k?.Other);
	}

	internal static Hero Parent(Kid k)
	{
		try
		{
			string id = k?.Parent;
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			return ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero h) => ((MBObjectBase)h).StringId == id)) ?? ((IEnumerable<Hero>)Hero.DeadOrDisabledHeroes).FirstOrDefault((Func<Hero, bool>)((Hero h) => ((MBObjectBase)h).StringId == id));
		}
		catch
		{
			return null;
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

	private static Settlement Place(string id)
	{
		try
		{
			return (!string.IsNullOrEmpty(id)) ? ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Func<Settlement, bool>)((Settlement s) => ((MBObjectBase)s).StringId == id)) : null;
		}
		catch
		{
			return null;
		}
	}

	internal static List<Kid> Known()
	{
		return (from k in All()
			where k.Known && HeroOf(k) != null
			select k).ToList();
	}

	internal static bool CanSpendNight(out string why)
	{
		why = null;
		try
		{
			if (!Cfg.Baseborn)
			{
				why = "turned off in the config";
				return false;
			}
			Hero mainHero = Hero.MainHero;
			Settlement currentSettlement = Settlement.CurrentSettlement;
			if (mainHero == null || currentSettlement == null || (!currentSettlement.IsTown && !currentSettlement.IsVillage))
			{
				why = "there is nowhere here to take a room";
				return false;
			}
			if (mainHero.IsChild || mainHero.Age < (float)Cfg.BaseNightMinAge)
			{
				why = "you are too young";
				return false;
			}
			if (!Cfg.BaseWhileMarried && mainHero.Spouse != null)
			{
				why = "you are married, and your config does not allow it";
				return false;
			}
			if (All().Count >= Cfg.BaseMax)
			{
				why = "you have as many children out there as you can account for";
				return false;
			}
			int i = Store.GetI("bb:last", -9999);
			int num = Cfg.BaseCooldown - (CourtBehavior.Today() - i);
			if (i > -9000 && num > 0)
			{
				why = "not so soon. " + num + " more days";
				return false;
			}
			if (mainHero.Gold < Cfg.BaseNightCost)
			{
				why = "you cannot pay for the room and the silence";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			why = "this cannot be read just now";
			Log.Once("nightcheck", "the night check failed: " + ex.Message);
			return false;
		}
	}

	internal static void SpendNight()
	{
		try
		{
			if (!CanSpendNight(out var why))
			{
				Flow.Notify("Not tonight: " + why + ".");
				return;
			}
			Hero mainHero = Hero.MainHero;
			Settlement currentSettlement = Settlement.CurrentSettlement;
			Hero val = Beget(currentSettlement, mainHero);
			if (val == null)
			{
				Flow.Notify("The night passed quietly and alone.");
				return;
			}
			mainHero.ChangeHeroGold(-Cfg.BaseNightCost);
			Store.SetI("bb:last", CourtBehavior.Today());
			Kid kid = new Kid();
			kid.Id = Next();
			kid.Other = ((MBObjectBase)val).StringId;
			kid.Where = ((MBObjectBase)currentSettlement).StringId;
			kid.Night = CourtBehavior.Today();
			kid.Parent = ((MBObjectBase)mainHero).StringId;
			Save(kid);
			Log.Write(string.Concat("a night at ", currentSettlement.Name, " with ", val.Name, " (record ", kid.Id, ")"));
			Whisper(mainHero, currentSettlement);
			Popup("A Room Above the Common Hall", string.Concat("You took a room, and you were not in it alone.\n\nIn the morning ", val.Name, " was gone about ", (!val.IsFemale) ? "his" : "her", " business and so were you, and neither of you said anything worth writing down.\n\nNothing has happened. Nothing is going to happen for a long while."));
		}
		catch (Exception ex)
		{
			Log.Write("the night failed: " + ex.Message);
		}
	}

	private static Hero Beget(Settlement here, Hero you)
	{
		try
		{
			CharacterObject val = Template(here, !you.IsFemale);
			if (val == null)
			{
				Log.Write(string.Concat("no one at ", here.Name, " to have met"));
				return null;
			}
			int num = MBRandom.RandomInt(Cfg.BaseOtherMinAge, Cfg.BaseOtherMaxAge + 1);
			Hero val2 = HeroCreator.CreateSpecialHero(val, here, (Clan)null, (Clan)null, num);
			if (val2 == null)
			{
				return null;
			}
			try
			{
				val2.ChangeState((CharacterStates)1);
				val2.SetNewOccupation((Occupation)3);
				EnterSettlementAction.ApplyForCharacterOnly(val2, here);
				val2.IsKnownToPlayer = true;
			}
			catch (Exception ex)
			{
				Log.Once("begetplace", "they could not be placed: " + ex.Message);
			}
			return val2;
		}
		catch (Exception ex2)
		{
			Log.Write("inventing the other parent failed: " + ex2.Message);
			return null;
		}
	}

	private static CharacterObject Template(Settlement here, bool wantFemale)
	{
		try
		{
			CultureObject culture = here?.Culture;
			List<CharacterObject> list = ((IEnumerable<CharacterObject>)CharacterObject.All).Where((CharacterObject c) => c != null && ((BasicCharacterObject)c).IsHero && c.Culture == culture && ((BasicCharacterObject)c).IsFemale == wantFemale && (int)c.Occupation == 3).ToList();
			if (list.Count == 0)
			{
				list = ((IEnumerable<CharacterObject>)CharacterObject.All).Where((CharacterObject c) => c != null && ((BasicCharacterObject)c).IsHero && ((BasicCharacterObject)c).IsFemale == wantFemale && (int)c.Occupation == 3).ToList();
			}
			return (list.Count != 0) ? list[MBRandom.RandomInt(list.Count)] : null;
		}
		catch
		{
			return null;
		}
	}

	private static void Whisper(Hero you, Settlement here)
	{
		try
		{
			Hero val = ((you == null) ? null : you.Spouse);
			if (val != null && MBRandom.RandomInt(100) < Cfg.BaseWhisperChance)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, val, -Cfg.BaseWhisperRelation, false);
				Standing.Change(-Cfg.BaseWhisperHonour, 0, "a night talked about at court");
				Store.AddDeed(Standing.Date() + "  There was talk about a night at " + ((here == null) ? "a town" : ((object)here.Name).ToString()) + ".");
				Flow.Notify(string.Concat(val.Name, " has heard where you spent the night."));
			}
		}
		catch
		{
		}
	}

	internal static void Daily()
	{
		try
		{
			if (!Cfg.Baseborn || !Store.Initialized)
			{
				return;
			}
			int num = CourtBehavior.Today();
			foreach (Kid item in All())
			{
				if (item.Known)
				{
					if (!string.IsNullOrEmpty(item.Hero) && HeroOf(item) == null)
					{
						Log.Write("the child of record " + item.Id + " is dead");
						Drop(item);
					}
				}
				else if (num - item.Night >= Cfg.BaseYearsUntil * Math.Max(1, Cfg.DaysPerYear))
				{
					Arrive(item);
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("bbdaily", "the baseborn tick failed: " + ex.Message);
		}
	}

	private static void Arrive(Kid k)
	{
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_019b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected O, but got Unknown
		//IL_01a5: Expected O, but got Unknown
		try
		{
			Settlement val = Place(k.Where);
			Hero val2 = OtherOf(k);
			Hero val3 = Parent(k) ?? Hero.MainHero;
			if (val3 == null || val == null)
			{
				Drop(k);
				return;
			}
			bool wantFemale = MBRandom.RandomInt(100) < 50;
			CharacterObject val4 = Template(val, wantFemale);
			if (val4 == null)
			{
				Drop(k);
				return;
			}
			int num = MBRandom.RandomInt(Cfg.BaseChildMinAge, Cfg.BaseChildMaxAge + 1);
			Hero val5 = HeroCreator.CreateSpecialHero(val4, val, (Clan)null, (Clan)null, num);
			if (val5 == null)
			{
				Drop(k);
				return;
			}
			try
			{
				val5.ChangeState((CharacterStates)1);
				val5.SetNewOccupation((Occupation)3);
				EnterSettlementAction.ApplyForCharacterOnly(val5, val);
				val5.IsKnownToPlayer = true;
			}
			catch (Exception ex)
			{
				Log.Once("kidplace", "the child could not be placed: " + ex.Message);
			}
			try
			{
				if (val3.IsFemale)
				{
					val5.Mother = val3;
					if (val2 != null)
					{
						val5.Father = val2;
					}
				}
				else
				{
					val5.Father = val3;
					if (val2 != null)
					{
						val5.Mother = val2;
					}
				}
			}
			catch (Exception ex2)
			{
				Log.Once("kidparent", "the parentage would not take: " + ex2.Message);
			}
			string text = ((!(val5.FirstName != (TextObject)null)) ? "The Child" : ((object)val5.FirstName).ToString());
			string text2 = Surnames.Of(val);
			try
			{
				val5.SetName(new TextObject("{=!}" + text + " " + text2, (Dictionary<string, object>)null), new TextObject("{=!}" + text, (Dictionary<string, object>)null));
			}
			catch
			{
			}
			k.Hero = ((MBObjectBase)val5).StringId;
			k.Known = true;
			Save(k);
			Store.AddDeed(string.Concat(Standing.Date(), "  ", val5.Name, " was brought to your gate."));
			Log.Write(string.Concat("a child surfaces: ", val5.Name, ", ", (int)val5.Age, ", of ", val.Name));
			Popup("A Child at the Gate", string.Concat((val2 == null) ? "Someone" : ((object)val2.Name).ToString(), " came to the gate from ", val.Name, ", and ", (val2 == null || val2.IsFemale) ? "she" : "he", " did not come alone.\n\nThe ", (!val5.IsFemale) ? "boy" : "girl", " is ", (int)val5.Age, ", and carries the name ", text2, ", as everyone born the way ", (!val5.IsFemale) ? "he" : "she", " was carries it.\n\nYou are under no obligation. ", val5.Name, " is not of your house and cannot inherit, and nobody expects you to pretend otherwise.\n\nThey are yours, though. Everyone who looks at them can see it."));
		}
		catch (Exception ex3)
		{
			Log.Write("the child could not be brought: " + ex3.Message);
			Drop(k);
		}
	}

	internal static bool CanLegitimise(Kid k, out string why)
	{
		why = null;
		try
		{
			Hero val = HeroOf(k);
			if (k == null || val == null)
			{
				why = "there is nobody to acknowledge";
				return false;
			}
			if (k.Legit)
			{
				why = "already acknowledged";
				return false;
			}
			if (!Succession.Rules())
			{
				why = "only a ruling house can write a name into its book";
				return false;
			}
			if (val.IsChild)
			{
				why = "they are too young for it to mean anything yet";
				return false;
			}
			if (Bastard.Risen)
			{
				why = "that is long past";
				return false;
			}
			if (val.Clan != null && val.Clan != Clan.PlayerClan)
			{
				why = ((val.Clan.Leader != val) ? string.Concat("they belong to ", val.Clan.Name, " now") : string.Concat("they lead ", val.Clan.Name, " now"));
				return false;
			}
			return true;
		}
		catch
		{
			why = "this cannot be read just now";
			return false;
		}
	}

	internal static void Legitimise(Kid k)
	{
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		//IL_0156: Expected O, but got Unknown
		try
		{
			if (!CanLegitimise(k, out var why))
			{
				Flow.Notify("Not possible: " + why + ".");
				return;
			}
			Hero val = HeroOf(k);
			Clan playerClan = Clan.PlayerClan;
			string text = ((playerClan == null || !(playerClan.Name != (TextObject)null)) ? "your house" : ((object)playerClan.Name).ToString());
			if (text.StartsWith("House ", StringComparison.OrdinalIgnoreCase))
			{
				text = text.Substring(6).Trim();
			}
			if (Surnames.IsBaseborn(text))
			{
				string text2 = ((playerClan == null || playerClan.HomeSettlement == null) ? null : ((object)playerClan.HomeSettlement.Name).ToString());
				text = ((!string.IsNullOrEmpty(text2)) ? (text + " of " + text2) : (text + " of the Book"));
				Log.Write("your house name reads as baseborn, so the acknowledged name is '" + text + "'");
			}
			Observe(val);
			string text3 = ((!(val.FirstName != (TextObject)null)) ? ((object)val.Name).ToString() : ((object)val.FirstName).ToString());
			try
			{
				val.SetName(new TextObject("{=!}" + text3 + " " + text, (Dictionary<string, object>)null), new TextObject("{=!}" + text3, (Dictionary<string, object>)null));
			}
			catch (Exception ex)
			{
				Log.Write("the new name would not take: " + ex.Message);
			}
			try
			{
				val.Clan = playerClan;
			}
			catch (Exception ex2)
			{
				Log.Write("they could not be brought into the house: " + ex2.Message);
			}
			int num = 0;
			try
			{
				Hero mainHero = Hero.MainHero;
				List<Hero> list = new List<Hero>();
				if (mainHero != null)
				{
					if (mainHero.Spouse != null)
					{
						list.Add(mainHero.Spouse);
					}
					foreach (Hero item in Succession.Claimants())
					{
						if (item != val && !list.Contains(item))
						{
							list.Add(item);
						}
					}
					foreach (Hero item2 in list)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(mainHero, item2, -Cfg.LegitKinRelation, false);
						num++;
					}
				}
			}
			catch
			{
			}
			Standing.Change(-Cfg.LegitStanding, 0, "a baseborn child written into the book");
			k.Legit = true;
			Save(k);
			Store.AddDeed(string.Concat(Standing.Date(), "  ", val.Name, " was acknowledged and given your name."));
			Log.Write(string.Concat("legitimised: ", val.Name, " (", num, " kin took it badly)"));
			Popup("Your Name", string.Concat(val.Name, " kneels baseborn and stands with your name on them.\n\nIt is a generous thing and the court will say so out loud. ", (num <= 0) ? "\n" : (num + " of your own took it rather differently, and said so in private.\n\n"), "From today there is one more person in the realm who can be argued to have a right to your seat. The lords have already worked out which."));
		}
		catch (Exception ex3)
		{
			Log.Write("acknowledging the child failed: " + ex3.Message);
		}
	}

	private static void Observe(Hero child)
	{
		try
		{
			Type type = AccessTools.TypeByName("RoTDynastyAndSuccession.Legitimacy.LegitimacyTracker");
			object obj = ((type == null) ? null : AccessTools.Property(type, "Instance")?.GetValue(null, null));
			MethodInfo methodInfo = ((obj != null) ? AccessTools.Method(type, "GetStatus", (Type[])null, (Type[])null) : null);
			if (!(methodInfo == null))
			{
				object obj2 = methodInfo.Invoke(obj, new object[1] { child });
				Log.Write(string.Concat("RoT has taken note of ", child.Name, " (", obj2, ")"));
			}
		}
		catch (Exception ex)
		{
			Log.Once("observe", "RoT would not look at the child: " + ex.Message);
		}
	}

	private static string Next()
	{
		int i = Store.GetI("bb:next", 1);
		Store.SetI("bb:next", i + 1);
		return i.ToString();
	}

	private static void Popup(string title, string text)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", (string)null, (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
		}
	}

	internal static string Summary()
	{
		try
		{
			List<Kid> list = Known();
			if (list.Count == 0)
			{
				List<Kid> list2 = All();
				return (list2.Count <= 0) ? null : "  Nothing has come of it yet.\n";
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Kid item in list)
			{
				Hero val = HeroOf(item);
				stringBuilder.Append("  ").Append(val.Name).Append("  -  ")
					.Append((int)val.Age);
				stringBuilder.Append((!item.Legit) ? "   [not of your house]" : "   [acknowledged]");
				if (Blade.HolderOf() == val)
				{
					stringBuilder.Append("   [carries ").Append(Lore.Blade()).Append("]");
				}
				stringBuilder.Append("\n");
			}
			return stringBuilder.ToString();
		}
		catch
		{
			return null;
		}
	}
}
