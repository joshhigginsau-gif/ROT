using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Court -> The small council.
	internal static class CouncilMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_council", "{=!}{WAD_COUNCIL}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			Option(s, "wad_cn_summon", "{=WAD_CnSummon}Summon the small council", 0, delegate(MenuCallbackArgs a)
			{
				string why = Council.CanSummon();
				if (why != null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(why);
				}
				else
				{
					a.Tooltip = Styles.Line("Ravens to every seat at the table. They ride in over a day or three and sit for " + Cfg.CouncilSitDays + " in the lord's hall here.");
				}
			}, Council.Summon);

			Option(s, "wad_cn_muster", "{=WAD_CnMuster}Muster a host", 1, delegate(MenuCallbackArgs a)
			{
				if (Guard.Ready().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("A host needs a sworn knight with you to command it.");
				}
				else
				{
					a.Tooltip = Styles.Line("Gold for men: levies at " + Host.Price(Host.Levy) + " a man, men-at-arms at " + Host.Price(Host.Men) + ", veterans at " + Host.Price(Host.Veteran) + "." + ((Cfg.HostMusterDays > 0) ? (" The summons take " + Cfg.HostMusterDays + " days to answer.") : ""));
				}
			}, Host.Muster);

			Option(s, "wad_cn_hosts", "{=WAD_CnHosts}Your hosts", 2, delegate(MenuCallbackArgs a)
			{
				if (Host.Mine().Count == 0 && Muster.Mine().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("You have no host in the field.");
				}
				else
				{
					a.Tooltip = Styles.Line("Give them their orders, or send them home.");
				}
			}, Host.Pick);

			Option(s, "wad_cn_standdown", "{=WAD_CnStandDown}Stand the realm's lords down", 3, delegate(MenuCallbackArgs a)
			{
				if (Council.CouncilArmy() == null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("The realm's lords are not in the field at the council's word.");
				}
				else
				{
					a.Tooltip = Styles.Line("Send the lords the council called back to their own business.");
				}
			}, Council.StandDownBanners);

			s.AddGameMenuOption("wad_council", "wad_cn_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);
		}

		private static void Option(CampaignGameStarter s, string id, string text, int order, Action<MenuCallbackArgs> tip, Action act)
		{
			s.AddGameMenuOption("wad_council", id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					tip(a);
				}
				catch
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Unavailable right now (see wardens_dragons.log).");
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				try
				{
					act();
				}
				catch (Exception ex)
				{
					Log.Write("menu action failed: " + ex);
				}
			}, false, order, false, (object)null);
		}

		private static void SetText()
		{
			try
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("The small council meets at your word, in your hall. Speak to them there, each about their own business.\n\n");
				sb.Append(Council.Summary());
				MBTextManager.SetTextVariable("WAD_COUNCIL", sb.ToString(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_COUNCIL", "The small council.", false);
				Log.Once("cntext", "council text failed: " + e.Message);
			}
		}
	}
}
