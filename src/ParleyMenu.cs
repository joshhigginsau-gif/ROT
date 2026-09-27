using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Siege -> "Ride out under a banner of parley".
	internal static class ParleyMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			try
			{
				s.AddGameMenuOption("menu_siege_strategies", "wad_siege_parley", "{=WAD_Parley}Ride out under a banner of parley", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
				{
					a.optionLeaveType = (GameMenuOption.LeaveType)2;
					try
					{
						return Parley.Besieged() != null;
					}
					catch
					{
						return false;
					}
				}, (GameMenuOption.OnConsequenceDelegate)delegate
				{
					GameMenu.SwitchToMenu("wad_parley");
				}, false, 2, false, (object)null);
			}
			catch (Exception e)
			{
				Log.Write("could not put parley on the siege menu: " + e.Message);
			}

			s.AddGameMenu("wad_parley", "{=!}{WAD_PARLEY}", (OnInitDelegate)delegate
			{
				try
				{
					Settlement st = Parley.Besieged();
					MBTextManager.SetTextVariable("WAD_PARLEY", (st != null) ? Parley.Describe(st) : "There is nobody to parley with.", false);
				}
				catch (Exception e)
				{
					MBTextManager.SetTextVariable("WAD_PARLEY", "A banner of parley.", false);
					Log.Once("patext", "parley text failed: " + e.Message);
				}
			}, GameMenu.MenuOverlayType.Encounter, (GameMenu.MenuFlags)0, (object)null);

			Option(s, "wad_pa_duel", "{=WAD_PaDuel}Challenge them to single combat", 0, delegate(MenuCallbackArgs a, Settlement st)
			{
				a.Tooltip = Styles.Line("Win, and the castle yields. Lose, and you lift the siege and swear not to return for " + Cfg.ParleyTruceDays + " days. They accept " + Parley.ChallengeChance(st) + "% of the time.");
			}, Parley.Challenge);

			Option(s, "wad_pa_terms", "{=WAD_PaTerms}Offer terms", 1, delegate(MenuCallbackArgs a, Settlement st)
			{
				string why = Parley.CanTerms(st);
				if (why != null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(why);
				}
				else
				{
					a.Tooltip = Styles.Line("The garrison marches out under arms and the lords go free. " + Parley.TermsChance(st) + "% - hunger and time are what move them.");
				}
			}, Parley.Terms);

			Option(s, "wad_pa_buy", "{=WAD_PaBuy}Offer gold for the castle", 2, delegate(MenuCallbackArgs a, Settlement st)
			{
				string why = Parley.CanBuy(st) ?? Parley.CanTerms(st);
				if (why != null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(why);
				}
				else
				{
					a.Tooltip = Styles.Line(Parley.Price(st).ToString("N0") + " gold, paid only if they take it. " + Parley.BuyChance(st) + "%.");
				}
			}, Parley.Buy);

			Option(s, "wad_pa_charm", "{=WAD_PaCharm}Talk them round", 3, delegate(MenuCallbackArgs a, Settlement st)
			{
				string why = Parley.CanCharm(st);
				if (why != null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(why);
				}
				else
				{
					a.Tooltip = Styles.Line("Three arguments, and every one must land. Your Charm matters, and you learn from trying. Once a siege.");
				}
			}, Parley.Charm);

			s.AddGameMenuOption("wad_parley", "wad_pa_back", "{=WAD_PaBack}Ride back to your lines", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("menu_siege_strategies");
			}, true, 9, false, (object)null);
		}

		private static void Option(CampaignGameStarter s, string id, string text, int order, Action<MenuCallbackArgs, Settlement> tip, Action act)
		{
			s.AddGameMenuOption("wad_parley", id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					Settlement st = Parley.Besieged();
					if (st == null)
					{
						return false;
					}
					tip(a, st);
				}
				catch
				{
					return false;
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				act();
			}, false, order, false, (object)null);
		}
	}
}
