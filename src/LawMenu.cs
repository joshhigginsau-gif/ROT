using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Court -> The King's Justice, and the way into the arena for a trial.
	internal static class LawMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_law", "{=!}{WAD_LAW}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_hear", "{=WAD_Hear}Hear a charge", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					int can = Law.All().Count((Charge c) => c.Accused != MainId() && Judgeable(c));
					if (can == 0)
					{
						a.IsEnabled = false;
						a.Tooltip = Styles.Line("No charge waits that you have the power to hear.");
					}
					else
					{
						a.Tooltip = Styles.Line(can + " charge" + ((can == 1) ? "" : "s") + " you can hear.");
					}
				}
				catch
				{
					return false;
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Pick();
			}, false, 0, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_bring", "{=WAD_Bring}Bring a charge against a lord", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				a.Tooltip = Styles.Line(Succession.Rules()
					? "Any lord of your realm, or anyone you hold. A crime on record costs nothing to press; anything else has to be bought."
					: "A lord of the realm you kneel in, judged by your liege - or anyone you hold, judged by you.");
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Law.Bring();
			}, false, 1, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_arena", "{=WAD_TrialArena}Enter the arena for your trial", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				return ArenaCondition(a);
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Enter();
			}, false, 2, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);

			// And from any town: a trial waits for you in whichever arena you
			// reach first, whether or not the town is yours.
			try
			{
				s.AddGameMenuOption("town", "wad_town_trial", "{=WAD_TrialArena}Enter the arena for your trial", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
				{
					return ArenaCondition(a);
				}, (GameMenuOption.OnConsequenceDelegate)delegate
				{
					Enter();
				}, false, 0, false, (object)null);
			}
			catch (Exception e)
			{
				Log.Write("could not put the trial on the town menu: " + e.Message);
			}
		}

		private static bool ArenaCondition(MenuCallbackArgs a)
		{
			try
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Law || !Law.TrialWaiting)
				{
					return false;
				}
				Settlement here = Settlement.CurrentSettlement;
				if (here == null || !here.IsTown)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("A trial is fought in a town's arena.");
				}
				else
				{
					a.Tooltip = Styles.Line("The lists are cleared, and the realm is watching. Whoever falls has a " + Cfg.TrialDeathChance + "% chance of never rising.");
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static void Enter()
		{
			string why;
			if (!Law.Fight(out why))
			{
				Flow.Notify("The trial cannot be fought here: " + why + ".");
			}
		}

		private static void Pick()
		{
			try
			{
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Charge c in Law.All().Where((Charge x) => x.Accused != MainId()))
				{
					string why;
					bool can = Law.CanJudge(c, out why);
					els.Add(new InquiryElement(c, Law.Describe(c) + (c.False ? "   - your own invention" : ""), null, can,
						can ? ("Brought on day " + c.Day + ".") : (char.ToUpper(why[0]) + why.Substring(1) + ".")));
				}
				if (els.Count == 0)
				{
					Flow.Notify("No charge waits to be heard.");
					return;
				}
				Inquiry.Select("Charges Waiting", "Which do you hear?", els, 1, 1, "Hear it", "Not today",
					delegate(List<InquiryElement> chosen)
					{
						Charge c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Charge) : null;
						if (c != null)
						{
							Law.Hear(c);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("choosing a charge failed: " + e.Message);
			}
		}

		private static bool Judgeable(Charge c)
		{
			string why;
			return Law.CanJudge(c, out why);
		}

		private static string MainId()
		{
			return (Hero.MainHero != null) ? Hero.MainHero.StringId : "";
		}

		private static void SetText()
		{
			try
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("The law is whatever the crown can make stick. A charge can be true or bought; the accused can take your judgment, or ask the gods instead - one champion a side, or seven.\n\n");
				sb.Append(Law.Summary());
				MBTextManager.SetTextVariable("WAD_LAW", sb.ToString(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_LAW", "The King's Justice.", false);
				Log.Once("lawtext", "law text failed: " + e.Message);
			}
		}
	}
}
