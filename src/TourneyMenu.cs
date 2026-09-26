using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Court -> The lists.
	internal static class TourneyMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_lists", "{=!}{WAD_LISTS}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			s.AddGameMenuOption("wad_lists", "wad_lists_call", "{=WAD_CallTourney}Call a tourney here", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					string why;
					if (!Tourney.CanHost(out why))
					{
						a.IsEnabled = false;
						a.Tooltip = Styles.Line(char.ToUpper(why[0]) + why.Substring(1) + ".");
					}
					else
					{
						a.Tooltip = Styles.Line("From " + Tourney.Cost(1).ToString("N0") + " for a modest tourney to " + Tourney.Cost(3).ToString("N0") +
							" for a lavish one. Guest houses come, their young lords ride, and whatever happens in the lists is remembered.");
					}
				}
				catch
				{
					return false;
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Tourney.Call();
			}, false, 0, false, (object)null);

			s.AddGameMenuOption("wad_lists", "wad_lists_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);
		}

		private static void SetText()
		{
			try
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("A tourney is where the realm comes to look at itself. Lords ride against lords in front of everyone, debts are paid in public, and the winner chooses whose lap the roses land in.\n\n");
				sb.Append("The lists are not safe. Anyone who rides can be carried off them, and when it happens at your tourney, their house looks at the host.\n\n");
				string body = Tourney.Summary();
				sb.Append(string.IsNullOrEmpty(body) ? "You have never held one." : body);
				MBTextManager.SetTextVariable("WAD_LISTS", sb.ToString(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_LISTS", "The lists.", false);
				Log.Once("liststext", "lists text failed: " + e.Message);
			}
		}
	}
}
