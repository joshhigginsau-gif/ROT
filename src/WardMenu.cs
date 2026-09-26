using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// The children of other houses, living at your court.
	internal static class WardMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_wards", "{=!}{WAD_WARDS}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			s.AddGameMenuOption("wad_wards", "wad_wards_take", "{=WAD_TakeWard}Ask a house for one of their own", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Wardship)
				{
					return false;
				}
				if (Wardship.Candidates().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("No house owes you enough to give up their blood.");
				}
				else
				{
					a.Tooltip = Styles.Line("A ward is raised at your court and earns you " + Cfg.WardHonour.ToString("0") +
						" Honour. A hostage is the same child held as surety, worth " + Cfg.HostageDread.ToString("0") +
						" Dread and costing " + Cfg.HostageHonour.ToString("0") + " Honour.");
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				PickHouse();
			}, false, 0, false, (object)null);

			s.AddGameMenuOption("wad_wards", "wad_wards_manage", "{=WAD_ManageWard}Decide what becomes of one", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Wardship)
				{
					return false;
				}
				int held = Wardship.All().Count;
				if (held == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("You hold no one.");
				}
				else
				{
					int forfeit = Wardship.All().Count((Held x) => x.Forfeit);
					a.Tooltip = Styles.Line(held + " at your court" + ((forfeit > 0)
						? (", and " + forfeit + " whose house has broken faith.")
						: ". Every house of them has kept faith so far."));
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				PickHeld();
			}, false, 1, false, (object)null);

			s.AddGameMenuOption("wad_wards", "wad_wards_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)16;
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_court");
			}, true, 9, false, (object)null);
		}

		internal static void SetText()
		{
			try
			{
				StringBuilder sb = new StringBuilder();
				sb.Append("Other men's children eat at your table.\n\n");
				sb.Append(Wardship.Summary());
				sb.Append("\nA ward is fostered and taught, and his house thanks you for it. A hostage is surety, and his house does not. It is the same arrangement. The word is the whole of the difference.\n");
				MBTextManager.SetTextVariable("WAD_WARDS", sb.ToString(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_WARDS", "Your court.", false);
				Log.Once("wardtext", "ward text failed: " + e);
			}
		}

		private static void PickHouse()
		{
			try
			{
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Clan c in Wardship.Candidates())
				{
					string how = (Oaths.Of(c) != OathKind.None) ? "sworn to you" : "of your realm";
					els.Add(new InquiryElement(c, Styles.Titled(c) + "  (" + how + ")", null, Wardship.Offerable(c).Count > 0,
						(Wardship.Offerable(c).Count > 0) ? "Relation " + ((c.Leader != null) ? c.Leader.GetRelation(Hero.MainHero) : 0) : "This house has no one to give but its head."));
				}
				if (els.Count == 0)
				{
					Flow.Notify("No house owes you enough.");
					return;
				}
				Inquiry.Select("Whose Blood", "Which house will give up one of their own?", els, 1, 1, "Choose", "Cancel",
					delegate(List<InquiryElement> chosen)
					{
						Clan c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Clan) : null;
						if (c != null)
						{
							PickChild(c);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("choosing a house failed: " + e.Message);
			}
		}

		private static void PickChild(Clan house)
		{
			try
			{
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Hero h in Wardship.Offerable(house))
				{
					els.Add(new InquiryElement(h, h.Name + "  (" + ((int)h.Age) + ")", null, true,
						h.IsChild ? "Young enough to be taught, and to grow up yours." : "Grown. They will remember this as it is."));
				}
				if (els.Count == 0)
				{
					Flow.Notify("There is no one that house can give up.");
					return;
				}
				Inquiry.Select("Which One", "Name the one who comes to your court.", els, 1, 1, "Choose", "Cancel",
					delegate(List<InquiryElement> chosen)
					{
						Hero child = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
						if (child != null)
						{
							PickWord(child, house);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("choosing a child failed: " + e.Message);
			}
		}

		// The whole of Phase 6 is this one question.
		private static void PickWord(Hero child, Clan house)
		{
			try
			{
				InformationManager.ShowInquiry(new InquiryData("The Word for It",
					child.Name + " will come to your court either way.\n\n" +
					"Call it a WARDSHIP and they are fostered and taught, their house is honoured, and relations warm. " +
					"Call it a HOSTAGE and they are surety, their house keeps faith because it must, and hates you for it.\n\n" +
					"Honour and Dread will follow whichever word you use.",
					true, true, "A ward", "A hostage",
					delegate
					{
						Wardship.Take(child, house, false);
					},
					delegate
					{
						Wardship.Take(child, house, true);
					}), true, false);
			}
			catch (Exception e)
			{
				Log.Write("the word failed: " + e.Message);
			}
		}

		private static void PickHeld()
		{
			try
			{
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Held h in Wardship.All())
				{
					Hero child = Wardship.HeroOf(h);
					Clan house = Wardship.HouseOf(h);
					if (child == null)
					{
						continue;
					}
					els.Add(new InquiryElement(h,
						child.Name + " of " + ((house != null) ? house.Name.ToString() : "?") + (h.Hostage ? "  - hostage" : "  - ward") + (h.Forfeit ? "  - FORFEIT" : ""),
						null, true,
						h.Forfeit ? "Their house broke faith. The realm knows what is owed." : "Their house has kept faith so far."));
				}
				if (els.Count == 0)
				{
					// Say so. A silent return left the option enabled and
					// apparently doing nothing when every remaining record had
					// a dead child behind it.
					Flow.Notify("There is nobody at your court to speak about.");
					return;
				}
				Inquiry.Select("Your Court", "Who are we speaking about?", els, 1, 1, "Choose", "Cancel",
					delegate(List<InquiryElement> chosen)
					{
						Held h = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Held) : null;
						if (h != null)
						{
							Decide(h);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("choosing who failed: " + e.Message);
			}
		}

		private static void Decide(Held h)
		{
			try
			{
				Hero child = Wardship.HeroOf(h);
				Clan house = Wardship.HouseOf(h);
				if (child == null)
				{
					return;
				}
				string axe = h.Forfeit
					? ("Their house broke faith while you held them. +" + Cfg.ExecuteForfeitDread + " Dread, and no Honour lost - the realm was told why.")
					: ("Their house has done nothing. -" + Cfg.WardExecuteHonour + " Honour, +" + Cfg.WardExecuteDread + " Dread, and it will be told at every hearth in the realm.");
				List<InquiryElement> els = new List<InquiryElement>
				{
					new InquiryElement("home", "Send them home", null, true,
						"+" + Cfg.WardReleaseHonour + " Honour, relations mended with " + ((house != null) ? house.Name.ToString() : "their house") + "."),
					new InquiryElement("keep", "Keep them where they are", null, true, "Nothing changes."),
					new InquiryElement("axe", "Put them to death", null, true, axe)
				};
				Inquiry.Select(child.Name.ToString(),
					child.Name + " has been at your court " + ((CourtBehavior.Today() - h.Taken) / Math.Max(1, Cfg.DaysPerYear)) + " year(s).",
					els, 1, 1, "Decide", "Later",
					delegate(List<InquiryElement> chosen)
					{
						string pick = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : "keep";
						if (pick == "home")
						{
							Wardship.Release(h);
						}
						else if (pick == "axe")
						{
							Confirm(h, child);
						}
						try
						{
							GameMenu.SwitchToMenu("wad_wards");
						}
						catch
						{
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("deciding failed: " + e.Message);
			}
		}

		private static void Confirm(Held h, Hero child)
		{
			try
			{
				InformationManager.ShowInquiry(new InquiryData("Be Certain",
					child.Name + " will be taken out and killed.\n\n" +
					(h.Forfeit ? "Their house broke faith first, and every lord in the realm knows it." : "Their house has done nothing to you."),
					true, true, "Do it", "No",
					delegate
					{
						Wardship.Execute(h);
					}, null), true, false);
			}
			catch
			{
			}
		}
	}
}
