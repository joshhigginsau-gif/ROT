using System;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Court -> Ravens, and the two feasts on the town and castle menus.
	internal static class RavensMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_ravens", "{=!}{WAD_RAVENS}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			Option(s, "wad_rv_read", "{=WAD_RvRead}Read the letter that came", 0, delegate(MenuCallbackArgs a)
			{
				if (!Ravens.Waiting)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("No raven is waiting.");
				}
			}, Ravens.Read);

			Option(s, "wad_rv_propose", "{=WAD_RvPropose}Offer a marriage", 1, delegate(MenuCallbackArgs a)
			{
				if (Ravens.Appointment)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("You are already expected at someone's table.");
				}
				else
				{
					a.Tooltip = Styles.Line("Offer the hand of one of your blood - or your own - to another house. If they accept, the wedding is at their hall. Most weddings are weddings.");
				}
			}, Ravens.Propose);

			Option(s, "wad_rv_invite", "{=WAD_RvInvite}Hold a feast for another house", 2, delegate(MenuCallbackArgs a)
			{
				a.Tooltip = Styles.Line("An honest feast, costing " + Cfg.RavensFeastCost.ToString("N0") + ". If they come, they will think better of you.");
			}, Ravens.Invite);

			Option(s, "wad_rv_scheme", "{=WAD_RvScheme}Plan a feast they will not leave", 3, delegate(MenuCallbackArgs a)
			{
				if (Treachery.Active)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("A feast is already being prepared.");
				}
				else
				{
					a.Tooltip = Styles.Line("A false wedding, a false feast, a false letter from their own kin. At least " + Cfg.TreacheryCostBase.ToString("N0") + " gold and " + Cfg.TreacheryPrepDays + " days - and if it works, guest right is broken, and nobody ever forgets it.");
				}
			}, Treachery.Plan);

			s.AddGameMenuOption("wad_ravens", "wad_rv_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);

			foreach (string menu in new string[2] { "town", "castle" })
			{
				try
				{
					s.AddGameMenuOption(menu, "wad_" + menu + "_feast", "{=WAD_GoInFeast}Go in to the feast", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
					{
						a.optionLeaveType = (GameMenuOption.LeaveType)2;
						try
						{
							return Cfg.Ravens && Ravens.FeastHere();
						}
						catch
						{
							return false;
						}
					}, (GameMenuOption.OnConsequenceDelegate)delegate
					{
						Ravens.GoIn();
					}, false, 0, false, (object)null);
					s.AddGameMenuOption(menu, "wad_" + menu + "_laid", "{=WAD_FeastLaid}The feast is laid", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
					{
						a.optionLeaveType = (GameMenuOption.LeaveType)2;
						try
						{
							return Cfg.Ravens && Treachery.FeastHere();
						}
						catch
						{
							return false;
						}
					}, (GameMenuOption.OnConsequenceDelegate)delegate
					{
						Treachery.Feast();
					}, false, 0, false, (object)null);
				}
				catch (Exception e)
				{
					Log.Write("could not put the feast on the " + menu + " menu: " + e.Message);
				}
			}
		}

		private static void Option(CampaignGameStarter s, string id, string text, int order, Action<MenuCallbackArgs> tip, Action act)
		{
			s.AddGameMenuOption("wad_ravens", id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					tip(a);
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

		private static void SetText()
		{
			try
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("The maester's tower, and the birds in it. Most letters mean what they say.\n\n");
				string r = Ravens.Summary() + Treachery.Summary();
				sb.Append(string.IsNullOrEmpty(r) ? "Nothing is waiting on you." : r);
				MBTextManager.SetTextVariable("WAD_RAVENS", sb.ToString(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_RAVENS", "The ravens.", false);
				Log.Once("rvtext", "ravens text failed: " + e.Message);
			}
		}
	}
}
