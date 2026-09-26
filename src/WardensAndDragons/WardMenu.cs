using LeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WardensAndDragons;

internal static class WardMenu
{
	internal static void Register(CampaignGameStarter s)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Expected O, but got Unknown
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		s.AddGameMenu("wad_wards", "{=!}{WAD_WARDS}", (OnInitDelegate)delegate
		{
			SetText();
		}, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		s.AddGameMenuOption("wad_wards", "wad_wards_take", "{=WAD_TakeWard}Ask a house for one of their own", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
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
				a.Tooltip = Styles.Line("A ward is raised at your court and earns you " + Cfg.WardHonour.ToString("0") + " Honour. A hostage is the same child held as surety, worth " + Cfg.HostageDread.ToString("0") + " Dread and costing " + Cfg.HostageHonour.ToString("0") + " Honour.");
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			PickHouse();
		}, false, 0, false, (object)null);
		s.AddGameMenuOption("wad_wards", "wad_wards_manage", "{=WAD_ManageWard}Decide what becomes of one", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			if (!Cfg.Wardship)
			{
				return false;
			}
			int count = Wardship.All().Count;
			if (count == 0)
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("You hold no one.");
			}
			else
			{
				int num = Wardship.All().Count((Held x) => x.Forfeit);
				a.Tooltip = Styles.Line(count + " at your court" + ((num <= 0) ? ". Every house of them has kept faith so far." : (", and " + num + " whose house has broken faith.")));
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			PickHeld();
		}, false, 1, false, (object)null);
		s.AddGameMenuOption("wad_wards", "wad_wards_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)16;
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
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Other men's children eat at your table.\n\n");
			stringBuilder.Append(Wardship.Summary());
			stringBuilder.Append("\nA ward is fostered and taught, and his house thanks you for it. A hostage is surety, and his house does not. It is the same arrangement. The word is the whole of the difference.\n");
			MBTextManager.SetTextVariable("WAD_WARDS", stringBuilder.ToString(), false);
		}
		catch (Exception ex)
		{
			MBTextManager.SetTextVariable("WAD_WARDS", "Your court.", false);
			Log.Once("wardtext", "ward text failed: " + ex);
		}
	}

	private static void PickHouse()
	{
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Expected O, but got Unknown
		try
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (Clan item in Wardship.Candidates())
			{
				string text = ((Oaths.Of(item) == OathKind.None) ? "of your realm" : "sworn to you");
				list.Add(new InquiryElement((object)item, Styles.Titled(item) + "  (" + text + ")", (ImageIdentifier)null, Wardship.Offerable(item).Count > 0, (Wardship.Offerable(item).Count <= 0) ? "This house has no one to give but its head." : ("Relation " + ((item.Leader != null) ? item.Leader.GetRelation(Hero.MainHero) : 0))));
			}
			if (list.Count == 0)
			{
				Flow.Notify("No house owes you enough.");
				return;
			}
			Inquiry.Select("Whose Blood", "Which house will give up one of their own?", list, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
			{
				Clan val = ((chosen == null || chosen.Count <= 0) ? null : (chosen[0].Identifier as Clan));
				if (val != null)
				{
					PickChild(val);
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("choosing a house failed: " + ex.Message);
		}
	}

	private static void PickChild(Clan house)
	{
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		try
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (Hero item in Wardship.Offerable(house))
			{
				list.Add(new InquiryElement((object)item, string.Concat(item.Name, "  (", (int)item.Age, ")"), (ImageIdentifier)null, true, (!item.IsChild) ? "Grown. They will remember this as it is." : "Young enough to be taught, and to grow up yours."));
			}
			if (list.Count == 0)
			{
				Flow.Notify("There is no one that house can give up.");
				return;
			}
			Inquiry.Select("Which One", "Name the one who comes to your court.", list, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
			{
				Hero val = ((chosen == null || chosen.Count <= 0) ? null : (chosen[0].Identifier as Hero));
				if (val != null)
				{
					PickWord(val, house);
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("choosing a child failed: " + ex.Message);
		}
	}

	private static void PickWord(Hero child, Clan house)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData("The Word for It", string.Concat(child.Name, " will come to your court either way.\n\nCall it a WARDSHIP and they are fostered and taught, their house is honoured, and relations warm. Call it a HOSTAGE and they are surety, their house keeps faith because it must, and hates you for it.\n\nHonour and Dread will follow whichever word you use."), true, true, "A ward", "A hostage", (Action)delegate
			{
				Wardship.Take(child, house, hostage: false);
			}, (Action)delegate
			{
				Wardship.Take(child, house, hostage: true);
			}, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch (Exception ex)
		{
			Log.Write("the word failed: " + ex.Message);
		}
	}

	private static void PickHeld()
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Expected O, but got Unknown
		try
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (Held item in Wardship.All())
			{
				Hero val = Wardship.HeroOf(item);
				Clan val2 = Wardship.HouseOf(item);
				if (val != null)
				{
					list.Add(new InquiryElement((object)item, string.Concat(val.Name, " of ", (val2 == null) ? "?" : ((object)val2.Name).ToString(), (!item.Hostage) ? "  - ward" : "  - hostage", (!item.Forfeit) ? "" : "  - FORFEIT"), (ImageIdentifier)null, true, (!item.Forfeit) ? "Their house has kept faith so far." : "Their house broke faith. The realm knows what is owed."));
				}
			}
			if (list.Count == 0)
			{
				Flow.Notify("There is nobody at your court to speak about.");
				return;
			}
			Inquiry.Select("Your Court", "Who are we speaking about?", list, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
			{
				Held held = ((chosen == null || chosen.Count <= 0) ? null : (chosen[0].Identifier as Held));
				if (held != null)
				{
					Decide(held);
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("choosing who failed: " + ex.Message);
		}
	}

	private static void Decide(Held h)
	{
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_0120: Expected O, but got Unknown
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Expected O, but got Unknown
		try
		{
			Hero child = Wardship.HeroOf(h);
			Clan val = Wardship.HouseOf(h);
			if (child == null)
			{
				return;
			}
			string text = ((!h.Forfeit) ? ("Their house has done nothing. -" + Cfg.WardExecuteHonour + " Honour, +" + Cfg.WardExecuteDread + " Dread, and it will be told at every hearth in the realm.") : ("Their house broke faith while you held them. +" + Cfg.ExecuteForfeitDread + " Dread, and no Honour lost - the realm was told why."));
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement((object)"home", "Send them home", (ImageIdentifier)null, true, "+" + Cfg.WardReleaseHonour + " Honour, relations mended with " + ((val == null) ? "their house" : ((object)val.Name).ToString()) + "."));
			list.Add(new InquiryElement((object)"keep", "Keep them where they are", (ImageIdentifier)null, true, "Nothing changes."));
			list.Add(new InquiryElement((object)"axe", "Put them to death", (ImageIdentifier)null, true, text));
			List<InquiryElement> els = list;
			Inquiry.Select(((object)child.Name).ToString(), string.Concat(child.Name, " has been at your court ", (CourtBehavior.Today() - h.Taken) / Math.Max(1, Cfg.DaysPerYear), " year(s)."), els, 1, 1, "Decide", "Later", delegate(List<InquiryElement> chosen)
			{
				string text2 = ((chosen == null || chosen.Count <= 0) ? "keep" : (chosen[0].Identifier as string));
				if (text2 == "home")
				{
					Wardship.Release(h);
				}
				else if (text2 == "axe")
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
		catch (Exception ex)
		{
			Log.Write("deciding failed: " + ex.Message);
		}
	}

	private static void Confirm(Held h, Hero child)
	{
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData("Be Certain", string.Concat(child.Name, " will be taken out and killed.\n\n", (!h.Forfeit) ? "Their house has done nothing to you." : "Their house broke faith first, and every lord in the realm knows it."), true, true, "Do it", "No", (Action)delegate
			{
				Wardship.Execute(h);
			}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
		}
	}
}
