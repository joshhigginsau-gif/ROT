using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Court -> Knights of the realm.
	internal static class KnightsMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_knights", "{=!}{WAD_KNIGHTS}", (OnInitDelegate)delegate
			{
				try
				{
					StringBuilder sb = new StringBuilder();
					sb.Append("A knight has a name and a house, and nothing else - no land, no keep. They ride for your realm as free companies, and go when they please.\n\n");
					string sum = Knighting.Summary();
					sb.Append((sum.Length > 0) ? sum : "You have knighted nobody yet.");
					MBTextManager.SetTextVariable("WAD_KNIGHTS", sb.ToString(), false);
				}
				catch (Exception e)
				{
					MBTextManager.SetTextVariable("WAD_KNIGHTS", "Knights of the realm.", false);
					Log.Once("kntext", "knights text failed: " + e.Message);
				}
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			s.AddGameMenuOption("wad_knights", "wad_kn_knight", "{=WAD_KnKnight}Knight someone", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				string why = Knighting.CanKnight();
				if (why != null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(why);
				}
				else
				{
					a.Tooltip = Styles.Line("A soldier of your host, a companion, a wanderer in this town, or a younger child of your house. " + Cfg.KnightCost.ToString("N0") + " gold.");
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Knighting.Pick();
			}, false, 0, false, (object)null);

			s.AddGameMenuOption("wad_knights", "wad_kn_back_in", "{=WAD_KnBack}Ask a free company back", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				a.Tooltip = Styles.Line("A house you knighted that has left your service, and still thinks well of you. " + Cfg.KnightRehireCost.ToString("N0") + " gold.");
				return Knighting.Houses().Count > 0;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Knighting.AskBack();
			}, false, 1, false, (object)null);

			s.AddGameMenuOption("wad_knights", "wad_kn_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);
		}
	}
}
