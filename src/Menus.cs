using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
internal static class Menus
{
	internal static void Register(CampaignGameStarter s)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Expected O, but got Unknown
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected O, but got Unknown
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Expected O, but got Unknown
		//IL_0219: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Expected O, but got Unknown
		//IL_0236: Unknown result type (might be due to invalid IL or missing references)
		//IL_0240: Expected O, but got Unknown
		// Each injection into a VANILLA menu gets its own guard.
		//
		// AddGameMenuOption throws when the menu id does not resolve, and this
		// runs near the top of Register - so one missing or replaced vanilla
		// menu would take the court, the house, the wardens, the wards, the
		// realm and the Dragonmont down with it, silently.
		string[] array = new string[2] { "town", "castle" };
		foreach (string text in array)
		{
			try
			{
				s.AddGameMenuOption(text, "wad_hold_court_" + text, "{=WAD_HoldCourt}Hold court", new GameMenuOption.OnConditionDelegate(CanHoldCourt), (GameMenuOption.OnConsequenceDelegate)delegate
				{
					GameMenu.SwitchToMenu("wad_court");
				}, false, 1, false, (object)null);
			}
			catch (Exception ce)
			{
				Log.Write("could not put the court on the " + text + " menu: " + ce.Message);
			}
		}
		// The night. In a town or a village, where there is a room to take.
		string[] beds = new string[2] { "town", "village" };
		foreach (string text2 in beds)
		{
		  try
		  {
			s.AddGameMenuOption(text2, "wad_night_" + text2, "{=WAD_Night}Take a room for the night", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					if (!Cfg.Baseborn)
					{
						return false;
					}
					string why;
					if (!Baseborn.CanSpendNight(out why))
					{
						a.IsEnabled = false;
						a.Tooltip = Styles.Line(char.ToUpper(why[0]) + why.Substring(1) + ".");
					}
					else
					{
						a.Tooltip = Styles.Line("Costs " + Cfg.BaseNightCost + " for the room and the discretion. Nothing happens tonight." +
							((Hero.MainHero != null && Hero.MainHero.Spouse != null)
								? (" You are married, and there is a " + Cfg.BaseWhisperChance + "% chance it is talked about.")
								: ""));
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
		  catch (Exception ne)
		  {
			Log.Write("could not put the night on the " + text2 + " menu: " + ne.Message);
		  }
		}
		s.AddGameMenu("wad_court", "{=!}{WAD_COURT}", (OnInitDelegate)delegate
		{
			SetCourtText();
		}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);
		AddBranch(s, "wad_court", "wad_opt_house", "{=WAD_House}House and heirs", "wad_house", 0,
			() => HouseTip());
		AddBranch(s, "wad_court", "wad_opt_wardens", "{=WAD_Wardens}Wardens and clients", "wad_wardens", 2,
			() => "Grant a realm, take an oath, and collect what your clients owe.");
		AddBranch(s, "wad_court", "wad_opt_wards", "{=WAD_Wards}Hostages and wards", "wad_wards", 3,
			() => "The children of other houses, held at your court.");
		AddBranch(s, "wad_court", "wad_opt_realm", "{=WAD_Realm}Realm affairs", "wad_realm", 4,
			() => "Your standing, your chronicle, Harrenhal and the dragons.");
		if (Cfg.Law)
		{
			AddBranch(s, "wad_court", "wad_opt_law", "{=WAD_Law}The King's Justice", "wad_law", 7,
				() => "Hear charges, bring them, and answer them.");
		}
		if (Cfg.Tourneys)
		{
			AddBranch(s, "wad_court", "wad_opt_lists", "{=WAD_Lists}The lists", "wad_lists", 6,
				() => "Call a tourney, and see what the last ones cost.");
		}
		s.AddGameMenuOption("wad_court", "wad_leave", "{=WAD_Leave}Leave the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (GameMenuOption.LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			ReturnToSettlement();
		}, true, 9, false, (object)null);
		// The Dragonmont had no route from the court at all: the only way in
		// was Dragonstone's own town menu, so a player who never opened it
		// would not find out the feature existed.
		s.AddGameMenuOption("wad_court", "wad_opt_mount", "{=WAD_Mount}Climb to the Dragonmont", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			a.optionLeaveType = (GameMenuOption.LeaveType)2;
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
					a.Tooltip = Styles.Line("The dragons are at " + seat.Name + ". You would have to be there.");
				}
				else if (seat.OwnerClan != Clan.PlayerClan)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Only the lord of " + seat.Name + " may send anyone up the mountain.");
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
		}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);
		WardensMenu.AddOptions(s);
		WardMenu.Register(s);
		HouseMenu.Register(s);
		TourneyMenu.Register(s);
		LawMenu.Register(s);
		s.AddGameMenu("wad_realm", "{=!}{WAD_REALM}", (OnInitDelegate)delegate
		{
			SetRealmText();
		}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);
		s.AddGameMenuOption("wad_realm", "wad_realm_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (GameMenuOption.LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("wad_court");
		}, true, 9, false, (object)null);
		Log.Write("court menus registered");
	}

	private static void AddBranch(CampaignGameStarter s, string menu, string id, string text, string target, int order = 0, Func<string> tip = null)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		s.AddGameMenuOption(menu, id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (GameMenuOption.LeaveType)2;
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

	// A one-line read on the succession, for the court's own menu.
	private static string HouseTip()
	{
		try
		{
			if (!Cfg.Succession)
			{
				return "Your children, and who follows you.";
			}
			Hero heir = Succession.Named();
			if (heir == null)
			{
				return "No heir is named. The realm is guessing, and guessing badly.";
			}
			return heir.Name + " is named" + (Laws.Lawful() ? "." : ", against your culture's law.");
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
		}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);
		s.AddGameMenuOption(id, id + "_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (GameMenuOption.LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("wad_court");
		}, true, 9, false, (object)null);
	}

	private static bool CanHoldCourt(MenuCallbackArgs a)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Expected O, but got Unknown
		try
		{
			a.optionLeaveType = (GameMenuOption.LeaveType)2;
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
					a.Tooltip = Styles.Line("You hold court at " + homeSettlement.Name + ".");
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
			stringBuilder.Append("The court of ").Append((playerClan == null) ? "your house" : ((object)playerClan.Name).ToString());
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
			string waiting = Attention.Block();
			if (!string.IsNullOrEmpty(waiting))
			{
				stringBuilder.Append(waiting);
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
			// Show the most recent first and cap the page. This appended the
			// entire unbounded ledger, so in a long reign the two summaries
			// below it were pushed off the bottom of the screen.
			int shown = 0;
			for (int li = Store.Ledger.Count - 1; li >= 0 && shown < 10; li--)
			{
				stringBuilder.Append("  ").Append(Store.Ledger[li]).Append("\n");
				shown++;
			}
			if (Store.Ledger.Count > shown)
			{
				stringBuilder.Append("  ...and ").Append(Store.Ledger.Count - shown).Append(" older.\n");
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
}
