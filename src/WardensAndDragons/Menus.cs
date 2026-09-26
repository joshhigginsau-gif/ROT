using LeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType;
using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace WardensAndDragons;

internal static class Menus
{
	internal static void Register(CampaignGameStarter s)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Expected O, but got Unknown
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Expected O, but got Unknown
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Expected O, but got Unknown
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Expected O, but got Unknown
		//IL_0287: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Expected O, but got Unknown
		//IL_02a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ae: Expected O, but got Unknown
		//IL_02db: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e5: Expected O, but got Unknown
		//IL_02f8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0302: Expected O, but got Unknown
		//IL_032f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Expected O, but got Unknown
		//IL_0371: Unknown result type (might be due to invalid IL or missing references)
		//IL_037b: Expected O, but got Unknown
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b0: Expected O, but got Unknown
		//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cd: Expected O, but got Unknown
		string[] array = new string[2] { "town", "castle" };
		string[] array2 = array;
		foreach (string text in array2)
		{
			try
			{
				s.AddGameMenuOption(text, "wad_hold_court_" + text, "{=WAD_HoldCourt}Hold court", new GameMenuOption.OnConditionDelegate(CanHoldCourt), (GameMenuOption.OnConsequenceDelegate)delegate
				{
					GameMenu.SwitchToMenu("wad_court");
				}, false, 1, false, (object)null);
			}
			catch (Exception ex)
			{
				Log.Write("could not put the court on the " + text + " menu: " + ex.Message);
			}
		}
		string[] array3 = new string[2] { "town", "village" };
		string[] array4 = array3;
		foreach (string text2 in array4)
		{
			try
			{
				s.AddGameMenuOption(text2, "wad_night_" + text2, "{=WAD_Night}Take a room for the night", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
				{
					//IL_0002: Unknown result type (might be due to invalid IL or missing references)
					a.optionLeaveType = (LeaveType)2;
					try
					{
						if (!Cfg.Baseborn)
						{
							return false;
						}
						if (!Baseborn.CanSpendNight(out var why))
						{
							a.IsEnabled = false;
							a.Tooltip = Styles.Line(char.ToUpper(why[0]) + why.Substring(1) + ".");
						}
						else
						{
							a.Tooltip = Styles.Line("Costs " + Cfg.BaseNightCost + " for the room and the discretion. Nothing happens tonight." + ((Hero.MainHero == null || Hero.MainHero.Spouse == null) ? "" : (" You are married, and there is a " + Cfg.BaseWhisperChance + "% chance it is talked about.")));
						}
					}
					catch
					{
						return false;
					}
					return true;
				}, (GameMenuOption.OnConsequenceDelegate)delegate
				{
					Baseborn.SpendNight();
				}, false, 2, false, (object)null);
			}
			catch (Exception ex2)
			{
				Log.Write("could not put the night on the " + text2 + " menu: " + ex2.Message);
			}
		}
		s.AddGameMenu("wad_court", "{=!}{WAD_COURT}", (OnInitDelegate)delegate
		{
			SetCourtText();
		}, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		AddBranch(s, "wad_court", "wad_opt_house", "{=WAD_House}House and heirs", "wad_house", 0, () => HouseTip());
		AddBranch(s, "wad_court", "wad_opt_wardens", "{=WAD_Wardens}Wardens and clients", "wad_wardens", 2, () => "Grant a realm, take an oath, and collect what your clients owe.");
		AddBranch(s, "wad_court", "wad_opt_wards", "{=WAD_Wards}Hostages and wards", "wad_wards", 3, () => "The children of other houses, held at your court.");
		AddBranch(s, "wad_court", "wad_opt_realm", "{=WAD_Realm}Realm affairs", "wad_realm", 4, () => "Your standing, your chronicle, Harrenhal and the dragons.");
		s.AddGameMenuOption("wad_court", "wad_leave", "{=WAD_Leave}Leave the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			ReturnToSettlement();
		}, true, 9, false, (object)null);
		s.AddGameMenuOption("wad_court", "wad_opt_mount", "{=WAD_Mount}Climb to the Dragonmont", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			try
			{
				Settlement seat = Dragons.Seat;
				if (seat == null)
				{
					return false;
				}
				if (Settlement.CurrentSettlement != seat)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(string.Concat("The dragons are at ", seat.Name, ". You would have to be there."));
				}
				else if (seat.OwnerClan != Clan.PlayerClan)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(string.Concat("Only the lord of ", seat.Name, " may send anyone up the mountain."));
				}
				else
				{
					a.Tooltip = Styles.Line(Dragons.LivingCount() + " dragons live in the world.");
				}
			}
			catch
			{
				return false;
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("wad_dragonmont");
		}, false, 5, false, (object)null);
		DragonMenu.Register(s);
		s.AddGameMenu("wad_wardens", "{=!}{WAD_WARDENS}", (OnInitDelegate)delegate
		{
			WardensMenu.SetText();
		}, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		WardensMenu.AddOptions(s);
		WardMenu.Register(s);
		HouseMenu.Register(s);
		s.AddGameMenu("wad_realm", "{=!}{WAD_REALM}", (OnInitDelegate)delegate
		{
			SetRealmText();
		}, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		s.AddGameMenuOption("wad_realm", "wad_realm_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("wad_court");
		}, true, 9, false, (object)null);
		Log.Write("court menus registered");
	}

	private static void AddBranch(CampaignGameStarter s, string menu, string id, string text, string target, int order = 0, Func<string> tip = null)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_003c: Expected O, but got Unknown
		s.AddGameMenuOption(menu, id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			if (tip != null)
			{
				try
				{
					a.Tooltip = Styles.Line(tip());
				}
				catch
				{
				}
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu(target);
		}, false, order, false, (object)null);
	}

	private static string HouseTip()
	{
		try
		{
			if (!Cfg.Succession)
			{
				return "Your children, and who follows you.";
			}
			Hero val = Succession.Named();
			if (val == null)
			{
				return "No heir is named. The realm is guessing, and guessing badly.";
			}
			return string.Concat(val.Name, " is named", (!Laws.Lawful()) ? ", against your culture's law." : ".");
		}
		catch
		{
			return "Your children, and who follows you.";
		}
	}

	private static void Placeholder(CampaignGameStarter s, string id, string body)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		s.AddGameMenu(id, "{=!}" + body, (OnInitDelegate)delegate
		{
		}, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		s.AddGameMenuOption(id, id + "_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("wad_court");
		}, true, 9, false, (object)null);
	}

	private static bool CanHoldCourt(MenuCallbackArgs a)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			a.optionLeaveType = (LeaveType)2;
			Settlement currentSettlement = Settlement.CurrentSettlement;
			Clan playerClan = Clan.PlayerClan;
			if (currentSettlement == null || playerClan == null || currentSettlement.OwnerClan != playerClan)
			{
				return false;
			}
			if (Cfg.CapitalOnly)
			{
				Settlement homeSettlement = playerClan.HomeSettlement;
				if (homeSettlement != null && homeSettlement != currentSettlement)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(string.Concat("You hold court at ", homeSettlement.Name, "."));
				}
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static void ReturnToSettlement()
	{
		Settlement currentSettlement = Settlement.CurrentSettlement;
		if (currentSettlement != null && currentSettlement.IsCastle)
		{
			GameMenu.SwitchToMenu("castle");
		}
		else
		{
			GameMenu.SwitchToMenu("town");
		}
	}

	private static void SetCourtText()
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			Settlement currentSettlement = Settlement.CurrentSettlement;
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("The court of ").Append((playerClan != null) ? ((object)playerClan.Name).ToString() : "your house");
			if (currentSettlement != null)
			{
				stringBuilder.Append(", sitting at ").Append(currentSettlement.Name);
			}
			stringBuilder.Append(".\n\n");
			stringBuilder.Append("Honour ").Append(Store.Honour).Append(" - ")
				.Append(Standing.HonourBand())
				.Append(".\n");
			stringBuilder.Append("Dread ").Append(Store.Dread).Append(" - ")
				.Append(Standing.DreadBand())
				.Append(".\n");
			if (Hero.MainHero != null)
			{
				stringBuilder.Append("Treasury ").Append(Hero.MainHero.Gold.ToString("N0")).Append(" denars.\n");
			}
			string text = Harrenhal.CourtHeaderLine();
			if (text != null)
			{
				stringBuilder.Append("\n").Append(text).Append("\n");
			}
			if (Store.HonourCap < 100)
			{
				stringBuilder.Append("\nYour Honour can never again rise above ").Append(Store.HonourCap).Append(".\n");
			}
			if (Standing.OnMadPath())
			{
				stringBuilder.Append("\nThe court has grown quiet around you. Men choose their words with care, and some have stopped choosing them at all.");
			}
			else if (Standing.NearMadness())
			{
				stringBuilder.Append("\nThere is talk in the corridors. Feared and faithless is a dangerous thing to be, and you are close to it.");
			}
			string value = Attention.Block();
			if (!string.IsNullOrEmpty(value))
			{
				stringBuilder.Append(value);
			}
			MBTextManager.SetTextVariable("WAD_COURT", stringBuilder.ToString(), false);
		}
		catch (Exception ex)
		{
			MBTextManager.SetTextVariable("WAD_COURT", "The court is assembled.", false);
			Log.Once("courterr", "court text failed: " + ex.Message);
		}
	}

	private static void SetRealmText()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Honour is whether your word is worth anything. Dread is whether you are feared. They are not opposites: a ruler can be both, or neither.\n\n");
			stringBuilder.Append("Honour ").Append(Store.Honour).Append(", Dread ")
				.Append(Store.Dread)
				.Append(". Both drift back toward ")
				.Append(Cfg.StartHonour)
				.Append(" and ")
				.Append(Cfg.StartDread)
				.Append(" by ")
				.Append(Cfg.DriftPerSeason)
				.Append(" each season.\n\n");
			stringBuilder.Append("Deeds remembered:\n");
			if (Store.Ledger.Count == 0)
			{
				stringBuilder.Append("  Nothing yet.\n");
			}
			int num = 0;
			int num2 = Store.Ledger.Count - 1;
			while (num2 >= 0 && num < 10)
			{
				stringBuilder.Append("  ").Append(Store.Ledger[num2]).Append("\n");
				num++;
				num2--;
			}
			if (Store.Ledger.Count > num)
			{
				stringBuilder.Append("  ...and ").Append(Store.Ledger.Count - num).Append(" older.\n");
			}
			stringBuilder.Append("\nHARRENHAL\n").Append(Harrenhal.CourtSummary()).Append("\n");
			stringBuilder.Append("\nDRAGONS\n").Append(Dragons.CourtSummary()).Append("\n");
			MBTextManager.SetTextVariable("WAD_REALM", stringBuilder.ToString(), false);
		}
		catch (Exception ex)
		{
			MBTextManager.SetTextVariable("WAD_REALM", "The realm's affairs.", false);
			Log.Once("realmerr", "realm text failed: " + ex.Message);
		}
	}
}
