using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Court -> The Iron Bank.
	internal static class BankMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_bank", "{=!}{WAD_BANK}", (OnInitDelegate)delegate
			{
				try
				{
					MBTextManager.SetTextVariable("WAD_BANK", "The Iron Bank of Braavos lends to kings, and to anyone else it expects to be paid by. It is always paid.\n\n" + IronBank.Summary(), false);
				}
				catch (Exception e)
				{
					MBTextManager.SetTextVariable("WAD_BANK", "The Iron Bank.", false);
					Log.Once("ibtext", "bank text failed: " + e.Message);
				}
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			Option(s, "wad_ib_borrow", "{=WAD_IbBorrow}Borrow", 0, delegate(MenuCallbackArgs a)
			{
				string why = IronBank.CanBorrow();
				if (why != null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(why);
				}
				else
				{
					a.Tooltip = Styles.Line("Up to " + IronBank.Limit().ToString("N0") + " at " + IronBank.Rate() + "%, repaid every " + Cfg.BankPaymentDays + " days. Miss two payments and the Bank funds your enemies.");
				}
				return true;
			}, IronBank.Borrow);

			Option(s, "wad_ib_next", "{=WAD_IbNext}Pay the next instalment now", 1, delegate(MenuCallbackArgs a)
			{
				return IronBank.InDebt;
			}, delegate
			{
				IronBank.PayNext();
				GameMenu.SwitchToMenu("wad_bank");
			});

			Option(s, "wad_ib_all", "{=WAD_IbAll}Pay the whole debt", 2, delegate(MenuCallbackArgs a)
			{
				if (IronBank.InDebt)
				{
					a.Tooltip = Styles.Line(IronBank.Owed.ToString("N0") + " gold. The Bank will think better of you.");
				}
				return IronBank.InDebt;
			}, delegate
			{
				IronBank.PayAll();
				GameMenu.SwitchToMenu("wad_bank");
			});

			s.AddGameMenuOption("wad_bank", "wad_ib_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);
		}

		private static void Option(CampaignGameStarter s, string id, string text, int order, Func<MenuCallbackArgs, bool> show, Action act)
		{
			s.AddGameMenuOption("wad_bank", id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					return show(a);
				}
				catch
				{
					return false;
				}
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				act();
			}, false, order, false, (object)null);
		}
	}
}
