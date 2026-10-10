using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Court -> The white cloaks.
	internal static class GuardMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_kg", "{=!}{WAD_KG}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			Option(s, "wad_kg_swear", "{=WAD_KgSwear}Swear a knight to the white cloak", 0, delegate(MenuCallbackArgs a)
			{
				if (Guard.Places() <= 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Every place is filled. A place only opens when a knight dies, breaks the vows, or you take the cloak back.");
				}
				else
				{
					a.Tooltip = Styles.Line(Guard.Places() + " of " + Cfg.KgSize + " places stand empty. The vows are for life: no lands, no marriage, no inheritance.");
				}
			}, Swear);

			Option(s, "wad_kg_lc", "{=WAD_KgLc}Name a Lord Commander", 1, delegate(MenuCallbackArgs a)
			{
				if (Guard.All().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("There is nobody to command.");
				}
				else
				{
					a.Tooltip = Styles.Line("The Lord Commander is named first when a champion is wanted, and keeps the White Book.");
				}
			}, Commander);

			Option(s, "wad_kg_send", "{=WAD_KgSend}Send a knight on the crown's business", 2, delegate(MenuCallbackArgs a)
			{
				if (Guard.Ready().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("No sworn knight is with you and fit to ride.");
				}
				else
				{
					a.Tooltip = Styles.Line("Clear the outlaw camps nearest you, or hunt down a lord who fled your justice. They take men from your party, and they may not all come back.");
				}
			}, Send);

			Option(s, "wad_kg_release", "{=WAD_KgRelease}Take back a white cloak", 3, delegate(MenuCallbackArgs a)
			{
				if (Guard.All().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Nobody wears it.");
				}
				else
				{
					a.Tooltip = Styles.Line("The vows are for life. Releasing a knight from them costs " + Cfg.KgDismissHonour + " Honour, and the realm will remember why.");
				}
			}, Release);

			s.AddGameMenuOption("wad_kg", "wad_kg_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
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
			s.AddGameMenuOption("wad_kg", id, text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
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
				sb.Append("They guard your person in the street, in the hall and in the field, and ride out when the crown has work for a sword. They hold no lands, take no wives and father no heirs, and they serve until they die.\n\n");
				sb.Append(Guard.Book());
				MBTextManager.SetTextVariable("WAD_KG", sb.ToString(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_KG", "The white cloaks.", false);
				Log.Once("kgtext", "white book text failed: " + e.Message);
			}
		}

		private static void Refresh()
		{
			try
			{
				GameMenu.SwitchToMenu("wad_kg");
			}
			catch
			{
			}
		}

		// ------------------------------------------------------------------

		private static void Swear()
		{
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement("champion", "A champion", null, true, "A sword of your household, or anyone who has won a tourney."));
			els.Add(new InquiryElement("commoner", "A common soldier", null, Guard.Soldiers().Count > 0,
				(Guard.Soldiers().Count > 0) ? ("Raise one of your tier " + Cfg.KgCommonerTier + "+ soldiers to knighthood. The smallfolk will love you for it; the lords, less.") : ("No soldier in your party is tier " + Cfg.KgCommonerTier + " or better.")));
			els.Add(new InquiryElement("ward", "A ward of your court", null, true, "Raised at your table. Their house takes it as an honour."));
			els.Add(new InquiryElement("noble", "A younger son or daughter of a great house", null, true, "They may refuse. If they accept, they give up their inheritance, and their house is honoured."));
			els.Add(new InquiryElement("baseborn", "A baseborn child of yours", null, true, "In the white cloak they can never press a claim against your heir."));
			Inquiry.Select("The White Cloak", "Where does the new knight come from?", els, 1, 1, "Choose", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					string origin = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
					if (origin == "commoner")
					{
						Soldier();
					}
					else if (origin != null)
					{
						Person(origin);
					}
				});
		}

		private static void Person(string origin)
		{
			List<Hero> can = Guard.Candidates(origin);
			if (can.Count == 0)
			{
				Flow.Notify("Nobody of that kind is free to take the vows: they must be grown, unmarried, not the head of a house and not your heir.");
				return;
			}
			List<InquiryElement> els = can.Select((Hero h) => new InquiryElement(h,
				h.Name + "  (" + (int)h.Age + ", rated " + Law.Rating(h.CharacterObject) + ")" + ((h.Clan != null && h.Clan != Clan.PlayerClan) ? ("   - of " + h.Clan.Name) : "") +
				((Tourney.Wins(h) > 0) ? ("   - " + Tourney.Wins(h) + " tourney win(s)") : ""), null, true, "")).ToList();
			Inquiry.Select("Who Takes the Vows", "They kneel, and they are asked to give up everything but the sword.", els, 1, 1, "Swear them", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Hero h = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
					if (h == null)
					{
						return;
					}
					string why;
					if (Guard.Swear(h, origin, false, out why))
					{
						if (origin == "baseborn")
						{
							Standing.Change(1, 0, "A baseborn child took the white cloak");
						}
						Flow.Notify(h.Name + " takes the white cloak.");
					}
					else
					{
						Flow.Notify(char.ToUpper(why[0]) + why.Substring(1) + ".");
					}
					Refresh();
				});
		}

		private static void Soldier()
		{
			List<InquiryElement> els = Guard.Soldiers().Select((CharacterObject c) => new InquiryElement(c, c.Name + "  (tier " + c.Tier + ")", null, true, "")).ToList();
			Inquiry.Select("Knighted in the Field", "Which of them has earned it?", els, 1, 1, "Knight them", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					CharacterObject c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as CharacterObject) : null;
					if (c == null)
					{
						return;
					}
					Hero h = Guard.KnightSoldier(c);
					string why;
					if (h != null && Guard.Swear(h, "commoner", false, out why))
					{
						Standing.Change(2, 0, "Knighted a common soldier into the white cloak");
						Kingdom realm = Succession.Realm();
						if (realm != null)
						{
							foreach (Clan cl in realm.Clans)
							{
								if (cl != null && cl != Clan.PlayerClan && cl.Leader != null && cl.Leader.IsAlive)
								{
									ChangeRelation(cl.Leader, -2);
								}
							}
						}
						Flow.Notify(h.Name + ", once " + c.Name + ", takes the white cloak.");
					}
					Refresh();
				});
		}

		private static void ChangeRelation(Hero h, int n)
		{
			try
			{
				TaleWorlds.CampaignSystem.Actions.ChangeRelationAction.ApplyPlayerRelation(h, n, false, false);
			}
			catch
			{
			}
		}

		private static void Commander()
		{
			List<InquiryElement> els = Guard.All().Select((Knight k) => new { k, h = Guard.HeroOf(k) }).Where(x => x.h != null)
				.Select(x => new InquiryElement(x.k, x.h.Name + ((x.k.Rank >= 1) ? "   - Lord Commander" : "") + "  (rated " + Law.Rating(x.h.CharacterObject) + ")", null, x.k.Rank < 1, "")).ToList();
			Inquiry.Select("Lord Commander", "Who leads the white cloaks?", els, 1, 1, "Name them", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Knight k = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Knight) : null;
					if (k != null)
					{
						Guard.Commander(k);
					}
					Refresh();
				});
		}

		private static void Send()
		{
			List<InquiryElement> els = new List<InquiryElement>();
			foreach (Settlement camp in Guard.Camps())
			{
				int men = camp.Parties.Where((TaleWorlds.CampaignSystem.Party.MobileParty p) => p.IsBandit).Sum((TaleWorlds.CampaignSystem.Party.MobileParty p) => p.MemberRoster.TotalManCount);
				els.Add(new InquiryElement(camp, "Clear the outlaws out of " + camp.Name + "  (" + men + " men)", null, true, ""));
			}
			foreach (Hero h in Guard.Outlaws())
			{
				els.Add(new InquiryElement(h, "Bring back " + h.Name + ", wanted by your court", null, true,
					"They are beyond your realm and have a charge waiting. Brought back, they can be judged."));
			}
			if (els.Count == 0)
			{
				Flow.Notify("There is no outlaw camp known near you and nobody wanted by your court within reach.");
				return;
			}
			Inquiry.Select("The Crown's Business", "What is to be done?", els, 1, 1, "Choose", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					object target = (chosen != null && chosen.Count > 0) ? chosen[0].Identifier : null;
					if (target != null)
					{
						Who(target);
					}
				});
		}

		private static void Who(object target)
		{
			List<InquiryElement> els = Guard.Ready().Select((Hero h) => new InquiryElement(h, h.Name + "  (rated " + Law.Rating(h.CharacterObject) + ")", null, true, "")).ToList();
			Inquiry.Select("Which Knight", "Who rides?", els, 1, 1, "Choose", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Hero h = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
					if (h != null)
					{
						Men(h, target);
					}
				});
		}

		private static void Men(Hero h, object target)
		{
			List<InquiryElement> els = new List<InquiryElement>();
			foreach (int n in new int[4] { 0, 10, 25, 50 })
			{
				els.Add(new InquiryElement(n, (n == 0) ? "Alone" : (n + " of your best men"), null, true,
					(n == 0) ? "A knight of the white cloak is worth a company. Almost." : "Taken from your party. Not all of them may come back."));
			}
			Inquiry.Select("With Whom", h.Name + " rides with...", els, 1, 1, "Ride", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					int n = (chosen != null && chosen.Count > 0 && chosen[0].Identifier is int) ? (int)chosen[0].Identifier : 0;
					Guard.Send(h, target, n);
					Refresh();
				});
		}

		private static void Release()
		{
			List<InquiryElement> els = Guard.All().Select((Knight k) => new { k, h = Guard.HeroOf(k) }).Where(x => x.h != null)
				.Select(x => new InquiryElement(x.k, x.h.Name.ToString(), null, x.k.State != "away", (x.k.State == "away") ? "Away on the crown's business." : "")).ToList();
			Inquiry.Select("Take Back the Cloak", "Whom do you release from the vows?", els, 1, 1, "Release them", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Knight k = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Knight) : null;
					if (k != null)
					{
						Guard.Dismiss(k);
					}
					Refresh();
				});
		}
	}
}
