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
		private static void AttaintPick()
		{
			List<InquiryElement> els = Attainder.Wrongdoers().Select((KeyValuePair<Clan, string> w) => new InquiryElement(w.Key, w.Key.Name + "  (" + w.Value + ")", null, true,
				(w.Key.Kingdom != null && w.Key.Kingdom != Clan.PlayerClan.Kingdom) ? ("Sworn to " + w.Key.Kingdom.Name + ": you cannot make war on them alone, but the attainder holds whenever you take them.") : "They will be cast out and at war with you.")).ToList();
			Inquiry.Select("Attainder", "Which house is condemned, root and branch?", els, 1, 1, "Attaint them", "Not today", delegate(List<InquiryElement> chosen)
			{
				Clan c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Clan) : null;
				if (c == null)
				{
					return;
				}
				string why = Attainder.Wrongdoers().Where((KeyValuePair<Clan, string> w) => w.Key == c).Select((KeyValuePair<Clan, string> w) => w.Value).FirstOrDefault() ?? "by the King's word";
				Attainder.Declare(c, why);
				GameMenu.SwitchToMenu("wad_law");
			});
		}

		private static void AttaintedPick()
		{
			List<InquiryElement> els = Attainder.All().Where((Attainder.Rec r) => r.Clan != null).Select((Attainder.Rec r) => new InquiryElement(r, r.Clan.Name + " - " + (r.Execute ? "put to death when taken" : "no order") + ", " + r.Heads + " dead", null, true, r.Reason)).ToList();
			Inquiry.Select("The Attainted", "Which house?", els, 1, 1, "That one", "Back", delegate(List<InquiryElement> chosen)
			{
				Attainder.Rec r = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Attainder.Rec) : null;
				if (r == null)
				{
					return;
				}
				List<InquiryElement> opts = new List<InquiryElement>();
				opts.Add(new InquiryElement("exec", r.Execute ? "Rescind the order" : ("Order: every lord of " + r.Clan.Name + " taken is put to death"), null, true,
					r.Execute ? "Your houses will keep them for ransom again." : ("Your houses (and every house of your realm, if you rule it) will execute any lord of " + r.Clan.Name + " they capture - and those they already hold. Children and your own blood are spared. Dread +" + Cfg.AttainderExecuteDread + ", Honour -" + Cfg.AttainderExecuteHonour + " a head.")));
				opts.Add(new InquiryElement("pardon", "Pardon the house", null, true, "The attainder and any order are lifted. Peace is a separate matter."));
				Inquiry.Select(r.Clan.Name.ToString(), r.Reason, opts, 1, 1, "So ordered", "Back", delegate(List<InquiryElement> c2)
				{
					string o = (c2 != null && c2.Count > 0) ? (c2[0].Identifier as string) : null;
					if (o == "exec")
					{
						Attainder.SetOrder(r, !r.Execute);
						Flow.Notify(r.Execute ? ("Every lord of " + r.Clan.Name + " taken by your houses will be put to death.") : "The order is rescinded.");
					}
					else if (o == "pardon")
					{
						Attainder.Pardon(r);
						Flow.Notify(r.Clan.Name + " is pardoned.");
					}
					GameMenu.SwitchToMenu("wad_law");
				});
			});
		}

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

			s.AddGameMenuOption("wad_law", "wad_law_seek", "{=WAD_Seek}Seek six to stand with you", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				return SeekCondition(a);
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Seek();
			}, false, 2, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_arena", "{=WAD_TrialArena}Enter the arena for your trial", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				return ArenaCondition(a);
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Enter();
			}, false, 2, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_attaint", "{=WAD_Attaint}Attaint a house that wronged you", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Attainder)
				{
					return false;
				}
				try
				{
					if (Attainder.Wrongdoers().Count == 0)
					{
						a.IsEnabled = false;
						a.Tooltip = Styles.Line("No house has a wrong against you on the King's record.");
					}
				}
				catch
				{
					a.IsEnabled = false;
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				AttaintPick();
			}, false, 3, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_attainted", "{=WAD_Attainted}The attainted houses", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Attainder)
				{
					return false;
				}
				if (Attainder.All().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("No house is attainted.");
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				AttaintedPick();
			}, false, 4, false, (object)null);

			s.AddGameMenuOption("wad_law", "wad_law_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);

			try
			{
				s.AddGameMenuOption("town", "wad_town_seek", "{=WAD_Seek}Seek six to stand with you in the trial of seven", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
				{
					return SeekCondition(a);
				}, (GameMenuOption.OnConsequenceDelegate)delegate
				{
					Seek();
				}, false, 0, false, (object)null);
			}
			catch (Exception e)
			{
				Log.Write("could not put the gathering on the town menu: " + e.Message);
			}

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

		private static bool SeekCondition(MenuCallbackArgs a)
		{
			try
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Law || !Law.Gathering)
				{
					return false;
				}
				if (Law.Answered() >= 6)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Six stand with you. The lists open in " + Law.HoursLeft() + " hours.");
				}
				else
				{
					a.Tooltip = Styles.Line(Law.Answered() + " of six have answered. The lists open in " + Law.HoursLeft() + " hours. Kin and sworn knights will answer; friends may; strangers will not.");
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static void Seek()
		{
			try
			{
				List<Hero> can = Law.Askable();
				if (can.Count == 0)
				{
					Flow.Notify("There is nobody here left to ask. Whoever is missing tomorrow will be made up from your soldiers and the glory hunters.");
					return;
				}
				int room = Math.Max(1, 6 - Law.Answered());
				List<InquiryElement> els = can.Select((Hero h) => new InquiryElement(h,
					h.Name + "  (relation " + (int)h.GetRelationWithPlayer() + ", rated " + Law.Rating(h.CharacterObject) + ")   - " + Law.Willing(h) + "% to stand",
					null, Law.Willing(h) > 0,
					(Law.Willing(h) >= 100) ? "They will not refuse you." : ((Law.Willing(h) > 0) ? "They may say yes." : "They do not love you enough to die for you."))).ToList();
				Inquiry.Select("Will No Knight Stand For Me?",
					"Ask whoever you would have beside you. Each can be asked once, and whatever they say, they meant it.",
					els, 1, Math.Min(room, els.Count), "Ask them", "Not yet",
					delegate(List<InquiryElement> chosen)
					{
						List<Hero> asked = (chosen ?? new List<InquiryElement>()).Select((InquiryElement e) => e.Identifier as Hero).Where((Hero h) => h != null).ToList();
						if (asked.Count > 0)
						{
							Law.Ask(asked);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("seeking champions failed: " + e.Message);
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
