using LeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType;
using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace WardensAndDragons;

internal static class HouseMenu
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
		//IL_0147: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Expected O, but got Unknown
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Expected O, but got Unknown
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Expected O, but got Unknown
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Expected O, but got Unknown
		s.AddGameMenu("wad_house", "{=!}{WAD_HOUSE}", (OnInitDelegate)delegate
		{
			SetText();
		}, (MenuOverlayType)0, (MenuFlags)0, (object)null);
		s.AddGameMenuOption("wad_house", "wad_house_name", "{=WAD_NameHeir}Name your heir", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			try
			{
				if (Succession.Claimants().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Your house has no one of age to follow you.");
				}
				else
				{
					Hero val2 = Succession.Named();
					a.Tooltip = Styles.Line((val2 != null) ? string.Concat(val2.Name, " is named. Changing it now costs ", Cfg.RenameHeirCost.ToString("0"), " standing with the realm.") : "Nobody is named. The realm is guessing, and guessing badly.");
				}
			}
			catch (Exception ex3)
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("This cannot be read just now.");
				Log.Once("heircond", "the heir option failed to draw: " + ex3.Message);
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			PickHeir();
		}, false, 0, false, (object)null);
		s.AddGameMenuOption("wad_house", "wad_house_blade", "{=WAD_NameBlade}Name your house's ancestral blade", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			if (!Cfg.Bastard)
			{
				return false;
			}
			try
			{
				if (Bastard.Risen)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(Lore.Blade() + " is not yours to name any more.");
					return true;
				}
				string text = Lore.Named();
				a.Tooltip = Styles.Line((!string.IsNullOrEmpty(text)) ? ("Your house's sword is " + text + ". You may rename it while it is still in your hand.") : "Valyrian steel outlives the men who carry it, and a house is remembered by what it hands down. Name yours, and whoever ends up holding it will be remembered by it too.");
			}
			catch
			{
				a.Tooltip = Styles.Line("Name the sword your house hands down.");
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			NameBlade();
		}, false, 1, false, (object)null);
		s.AddGameMenuOption("wad_house", "wad_house_legit", "{=WAD_Legit}Acknowledge a child of yours", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			if (!Cfg.Baseborn)
			{
				return false;
			}
			try
			{
				int num = 0;
				foreach (Kid item in Baseborn.Known())
				{
					if (Baseborn.CanLegitimise(item, out var _))
					{
						num++;
					}
				}
				if (num == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("There is nobody at your gate to write into the book.");
				}
				else
				{
					a.Tooltip = Styles.Line(num + " could be given your name. It costs " + Cfg.LegitKinRelation + " relation with each of your trueborn kin and " + Cfg.LegitStanding + " standing with the realm - and it gives them a claim your heir will have to answer for.");
				}
			}
			catch (Exception ex2)
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("This cannot be read just now.");
				Log.Once("legitcond", "the acknowledge option failed to draw: " + ex2.Message);
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			PickChild();
		}, false, 2, false, (object)null);
		s.AddGameMenuOption("wad_house", "wad_house_bequeath", "{=WAD_Bequeath}Put the blade in someone's hand", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			if (!Cfg.Bastard)
			{
				return false;
			}
			try
			{
				if (Bastard.Risen)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(Lore.Blade() + " is not yours to give any more.");
					return true;
				}
				if (Blade.Candidates().Count == 0)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("There is no one of age to carry it.");
					return true;
				}
				Hero val = Blade.HolderOf();
				a.Tooltip = Styles.Line((val != null) ? string.Concat(Lore.Blade(), " is with ", val.Name, ". You may take it back, or put it in another hand.") : (Lore.Blade() + " hangs on your wall. Whoever you give it to is the one the realm will say you chose."));
			}
			catch (Exception ex)
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("This cannot be read just now.");
				Log.Once("bequeathcond", "the bequest option failed to draw: " + ex.Message);
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			Bequeath();
		}, false, 3, false, (object)null);
		s.AddGameMenuOption("wad_house", "wad_house_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
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
			MBTextManager.SetTextVariable("WAD_HOUSE", Body(), false);
		}
		catch (Exception ex)
		{
			MBTextManager.SetTextVariable("WAD_HOUSE", "Your house.", false);
			Log.Once("housetext", "house text failed: " + ex);
		}
	}

	private static string Body()
	{
		StringBuilder stringBuilder = new StringBuilder();
		Hero val = Succession.Named();
		stringBuilder.Append("YOUR HEIR\n  ").Append((val == null) ? "None named. The realm is guessing, and guessing badly." : ((object)val.Name).ToString()).Append("\n");
		if (val != null)
		{
			stringBuilder.Append("  ").Append((int)val.Age).Append(" years old.\n");
		}
		string value = Laws.Reading();
		if (!string.IsNullOrEmpty(value))
		{
			stringBuilder.Append("\nTHE LAW\n  ").Append(value).Append("\n");
		}
		stringBuilder.Append("\nYOUR BLOOD\n");
		List<Hero> list = Succession.Claimants();
		if (list.Count == 0)
		{
			stringBuilder.Append("  There is no one of your house old enough to follow you.\n");
		}
		else
		{
			foreach (Hero item in list)
			{
				stringBuilder.Append("  ").Append(item.Name).Append("  -  ")
					.Append((int)item.Age);
				if (item == val)
				{
					stringBuilder.Append("   [named]");
				}
				stringBuilder.Append("\n");
			}
		}
		if (Cfg.Bastard)
		{
			stringBuilder.Append("\nTHE SWORD OF YOUR HOUSE\n  ");
			if (Bastard.Risen)
			{
				stringBuilder.Append(Lore.Blade()).Append(", and it is not in this house any more.");
			}
			else if (Blade.Given)
			{
				Hero val2 = Blade.HolderOf();
				stringBuilder.Append(Lore.Blade()).Append(", carried by ").Append(val2.Name)
					.Append((!Blade.IsBaseborn(val2)) ? "." : ", who is not in the book.");
			}
			else
			{
				string text = Lore.Named();
				stringBuilder.Append((!string.IsNullOrEmpty(text)) ? text : "Unnamed. A blade nobody has named is a blade nobody misses.");
			}
			stringBuilder.Append("\n");
		}
		if (Cfg.Baseborn)
		{
			string value2 = Baseborn.Summary();
			if (!string.IsNullOrEmpty(value2))
			{
				stringBuilder.Append("\nNOT OF YOUR HOUSE\n").Append(value2);
			}
		}
		string value3 = Bastard.Summary();
		if (!string.IsNullOrEmpty(value3))
		{
			stringBuilder.Append("\nTHE REVERSED BANNER\n").Append(value3).Append("\n");
		}
		else if (Cfg.Bastard)
		{
			stringBuilder.Append("\n  Somewhere there is a child of this house nobody wrote down.\n");
		}
		return stringBuilder.ToString();
	}

	private static void PickHeir()
	{
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Expected O, but got Unknown
		try
		{
			List<InquiryElement> list = new List<InquiryElement>();
			Hero current = Succession.Named();
			foreach (Hero item in Succession.Claimants())
			{
				bool flag = Laws.Legal() == null || Laws.Legal() == item;
				string text = ((!flag) ? "Against your culture's law. Every house in the realm will hold it against your heir." : "The law already says this one, so the naming itself is not held against you.");
				if (current != null && item != current)
				{
					string text2 = text;
					text = string.Concat(text2, "\n\nChanging a name already given costs ", Cfg.RenameHeirCost.ToString("0"), " standing: the realm had made its peace with ", current.Name, ".");
				}
				list.Add(new InquiryElement((object)item, string.Concat(item.Name, "  (", (int)item.Age, ")", (item != current) ? "" : "   - named", (!flag) ? "" : "   - lawful"), (ImageIdentifier)null, item != current, text));
			}
			if (list.Count == 0)
			{
				Flow.Notify("There is no one of your house old enough to follow you.");
				return;
			}
			string text3 = "Who follows you?";
			string text4 = Laws.Name();
			Hero val = Laws.Legal();
			if (!string.IsNullOrEmpty(text4))
			{
				string text2 = text3;
				text3 = text2 + "\n\nYour culture's law is " + text4 + ((val == null) ? "." : string.Concat(", and by it the seat belongs to ", val.Name, ".")) + "\n\nNaming anyone else is allowed. That is what the law is worth.";
			}
			Inquiry.Select("Your Heir", text3, list, 1, 1, "Name them", "Cancel", delegate(List<InquiryElement> chosen)
			{
				Hero val2 = ((chosen == null || chosen.Count <= 0) ? null : (chosen[0].Identifier as Hero));
				if (val2 != null)
				{
					Hero val3 = current;
					Succession.Name(val2);
					if (val3 != null && val3 != val2)
					{
						Standing.Change(-(int)Cfg.RenameHeirCost, 0, "a new heir named over the old");
						Flow.Notify(string.Concat(val2.Name, " is named your heir in place of ", val3.Name, "."));
					}
					else
					{
						Flow.Notify(string.Concat(val2.Name, " is named your heir."));
					}
					Refresh();
				}
			}, delegate
			{
				Refresh();
			});
		}
		catch (Exception ex)
		{
			Log.Write("naming an heir failed: " + ex.Message);
		}
	}

	private static void PickChild()
	{
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Expected O, but got Unknown
		try
		{
			List<InquiryElement> list = new List<InquiryElement>();
			foreach (Kid item in Baseborn.Known())
			{
				Hero val = Baseborn.HeroOf(item);
				string why;
				bool flag = Baseborn.CanLegitimise(item, out why);
				list.Add(new InquiryElement((object)item, string.Concat(val.Name, "  (", (int)val.Age, ")", (!item.Legit) ? "" : "   - acknowledged"), (ImageIdentifier)null, flag, (!flag) ? (char.ToUpper(why[0]) + why.Substring(1) + ".") : ("Writing them into the book gives them your name and a claim. Your trueborn kin will each think less of you by " + Cfg.LegitKinRelation + ", and the realm by " + Cfg.LegitStanding + ".")));
			}
			if (list.Count == 0)
			{
				Flow.Notify("There is nobody at your gate.");
				return;
			}
			Inquiry.Select("Your Other Children", "These are yours. None of them can inherit while they carry the name they were born with.\n\nWriting one into the book changes that, and it cannot be undone.", list, 1, 1, "Give them my name", "Not today", delegate(List<InquiryElement> chosen)
			{
				Kid kid = ((chosen == null || chosen.Count <= 0) ? null : (chosen[0].Identifier as Kid));
				if (kid != null)
				{
					Baseborn.Legitimise(kid);
				}
				Refresh();
			}, delegate
			{
				Refresh();
			});
		}
		catch (Exception ex)
		{
			Log.Write("acknowledging failed: " + ex.Message);
		}
	}

	private static void Bequeath()
	{
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Expected O, but got Unknown
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Expected O, but got Unknown
		try
		{
			string blade = Lore.Blade();
			List<InquiryElement> list = new List<InquiryElement>();
			Hero val = Blade.HolderOf();
			Hero val2 = Succession.Named();
			foreach (Hero item in Blade.Candidates())
			{
				bool flag = Blade.IsBaseborn(item);
				Kid kid = ((!flag) ? null : Blade.RecordFor(item));
				string text = ((!flag) ? ((item != val2) ? "Trueborn, but not your heir. Think about what that looks like from their side." : "Your named heir. The sword goes where the seat goes, and nobody is surprised.") : ((kid == null || !kid.Legit) ? "They are not in the book. A sword in their hand is the only argument they will ever need." : "You have already given them your name. Give them the sword as well and you have named them in every way that matters except the one that counts."));
				list.Add(new InquiryElement((object)item, string.Concat(item.Name, "  (", (int)item.Age, ")", (!flag) ? "" : "   - not of your house", (item != val2) ? "" : "   - your heir", (item != val) ? "" : "   - has it"), (ImageIdentifier)null, item != val, text));
			}
			if (val != null)
			{
				list.Add(new InquiryElement((object)"take", "Take it back and hang it on the wall", (ImageIdentifier)null, true, "It returns to the house. Whatever they thought it meant, they were wrong."));
			}
			if (list.Count == 0)
			{
				Flow.Notify("There is no one of age to carry it.");
				return;
			}
			Inquiry.Select(blade, blade + " has been in this house longer than anyone can account for.\n\nWhoever holds it when you die is the one the realm will say you chose - whatever the book says, and whatever your will says.", list, 1, 1, "Put it in their hand", "Leave it where it is", delegate(List<InquiryElement> chosen)
			{
				object obj = ((chosen == null || chosen.Count <= 0) ? null : chosen[0].Identifier);
				if (obj is string)
				{
					Blade.TakeBack();
					Flow.Notify(blade + " hangs on your wall again.");
				}
				else
				{
					Hero val3 = (Hero)((obj is Hero) ? obj : null);
					if (val3 != null)
					{
						Blade.Give(val3);
						Flow.Notify(string.Concat(val3.Name, " carries ", blade, " from today.", (!Blade.IsBaseborn(val3)) ? "" : " Everyone saw you do it."));
					}
				}
				Refresh();
			}, delegate
			{
				Refresh();
			});
		}
		catch (Exception ex)
		{
			Log.Write("the bequest failed: " + ex.Message);
		}
	}

	private static void NameBlade()
	{
		try
		{
			string text = Lore.Named();
			Inquiry.Text("Your House's Blade", "Valyrian steel outlives the men who carry it. This one has been in your house longer than anyone can account for, and it has never been called anything in particular.\n\nName it.\n\nWhoever holds it when you are gone will be remembered by the name you give it now.", text ?? "", "It is called that", "Leave it unnamed", delegate(string chosen)
			{
				string text2 = (chosen ?? "").Replace("{", "").Replace("}", "").Trim();
				if (text2.Length == 0)
				{
					Flow.Notify("A sword needs a name that can be written down.");
					Refresh();
				}
				else
				{
					if (text2.Length > 24)
					{
						text2 = text2.Substring(0, 24).Trim();
					}
					Store.Set("bs:blade", text2);
					Store.AddDeed(Standing.Date() + "  The sword of your house was named " + text2 + ".");
					Log.Write("the house blade is named " + text2);
					Blade.Sync();
					Flow.Notify("The sword is " + text2 + " from today, and the maesters have written it down.");
					Refresh();
				}
			}, delegate
			{
				Refresh();
			});
		}
		catch (Exception ex)
		{
			Log.Write("naming the blade failed: " + ex.Message);
		}
	}

	private static void Refresh()
	{
		try
		{
			GameMenu.SwitchToMenu("wad_house");
		}
		catch
		{
		}
	}
}
