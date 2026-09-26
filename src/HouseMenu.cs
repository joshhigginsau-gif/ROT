using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// House and heirs.
	//
	// Most of what used to be on this page was Great Council machinery: call
	// one, take your seat, let it rise, and a list of every house in the realm
	// with a support score beside it. All of that is gone. What is left is the
	// one decision that was worth making - who follows you - and a plain
	// account of where your blood stands.
	internal static class HouseMenu
	{
		internal static void Register(CampaignGameStarter s)
		{
			s.AddGameMenu("wad_house", "{=!}{WAD_HOUSE}", (OnInitDelegate)delegate
			{
				SetText();
			}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);

			s.AddGameMenuOption("wad_house", "wad_house_name", "{=WAD_NameHeir}Name your heir", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				try
				{
					if (Succession.Claimants().Count == 0)
					{
						a.IsEnabled = false;
						a.Tooltip = Styles.Line("Your house has no one of age to follow you.");
					}
					else
					{
						Hero named = Succession.Named();
						a.Tooltip = Styles.Line((named == null)
							? "Nobody is named. The realm is guessing, and guessing badly."
							: (named.Name + " is named. Changing it now costs " + Cfg.RenameHeirCost.ToString("0") + " standing with the realm."));
					}
				}
				catch (Exception e)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("This cannot be read just now.");
					Log.Once("heircond", "the heir option failed to draw: " + e.Message);
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				PickHeir();
			}, false, 0, false, (object)null);

			s.AddGameMenuOption("wad_house", "wad_house_blade", "{=WAD_NameBlade}Name your house's ancestral blade", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Bastard)
				{
					return false;
				}
				try
				{
					if (Bastard.Risen)
					{
						// It is carried by somebody else now, and the name
						// went with it. Renaming it here would change nothing
						// except this screen.
						a.IsEnabled = false;
						a.Tooltip = Styles.Line(Lore.Blade() + " is not yours to name any more.");
						return true;
					}
					string blade = Lore.Named();
					a.Tooltip = Styles.Line(string.IsNullOrEmpty(blade)
						? "Valyrian steel outlives the men who carry it, and a house is remembered by what it hands down. Name yours, and whoever ends up holding it will be remembered by it too."
						: ("Your house's sword is " + blade + ". You may rename it while it is still in your hand."));
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
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (!Cfg.Baseborn)
				{
					return false;
				}
				try
				{
					int can = 0;
					foreach (Kid k in Baseborn.Known())
					{
						string w;
						if (Baseborn.CanLegitimise(k, out w))
						{
							can++;
						}
					}
					if (can == 0)
					{
						a.IsEnabled = false;
						a.Tooltip = Styles.Line("There is nobody at your gate to write into the book.");
					}
					else
					{
						a.Tooltip = Styles.Line(can + " could be given your name. It costs " + Cfg.LegitKinRelation +
							" relation with each of your trueborn kin and " + Cfg.LegitStanding +
							" standing with the realm - and it gives them a claim your heir will have to answer for.");
					}
				}
				catch (Exception e)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("This cannot be read just now.");
					Log.Once("legitcond", "the acknowledge option failed to draw: " + e.Message);
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				PickChild();
			}, false, 2, false, (object)null);

			s.AddGameMenuOption("wad_house", "wad_house_bequeath", "{=WAD_Bequeath}Put the blade in someone's hand", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
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
					Hero holder = Blade.HolderOf();
					a.Tooltip = Styles.Line((holder == null)
						? (Lore.Blade() + " hangs on your wall. Whoever you give it to is the one the realm will say you chose.")
						: (Lore.Blade() + " is with " + holder.Name + ". You may take it back, or put it in another hand."));
				}
				catch (Exception e)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("This cannot be read just now.");
					Log.Once("bequeathcond", "the bequest option failed to draw: " + e.Message);
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				Bequeath();
			}, false, 3, false, (object)null);

			s.AddGameMenuOption("wad_house", "wad_house_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
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
				MBTextManager.SetTextVariable("WAD_HOUSE", Body(), false);
			}
			catch (Exception e)
			{
				MBTextManager.SetTextVariable("WAD_HOUSE", "Your house.", false);
				Log.Once("housetext", "house text failed: " + e);
			}
		}

		private static string Body()
		{
			StringBuilder sb = new StringBuilder();
			Hero heir = Succession.Named();

			sb.Append("YOUR HEIR\n  ")
			  .Append((heir != null) ? heir.Name.ToString() : "None named. The realm is guessing, and guessing badly.")
			  .Append("\n");
			if (heir != null)
			{
				sb.Append("  ").Append((int)heir.Age).Append(" years old.\n");
			}

			string law = Laws.Reading();
			if (!string.IsNullOrEmpty(law))
			{
				sb.Append("\nTHE LAW\n  ").Append(law).Append("\n");
			}

			sb.Append("\nYOUR BLOOD\n");
			List<Hero> kin = Succession.Claimants();
			if (kin.Count == 0)
			{
				sb.Append("  There is no one of your house old enough to follow you.\n");
			}
			else
			{
				foreach (Hero h in kin)
				{
					sb.Append("  ").Append(h.Name).Append("  -  ").Append((int)h.Age);
					if (h == heir)
					{
						sb.Append("   [named]");
					}
					sb.Append("\n");
				}
			}

			if (Cfg.Bastard)
			{
				// Once the bastard has taken it, the sword has a name whether
				// you gave it one or not - so report the effective name, not
				// just the one you typed. Otherwise this page said the blade
				// was unnamed three lines above announcing House Bittersteel.
				sb.Append("\nTHE SWORD OF YOUR HOUSE\n  ");
				if (Bastard.Risen)
				{
					sb.Append(Lore.Blade()).Append(", and it is not in this house any more.");
				}
				else if (Blade.Given)
				{
					Hero holder = Blade.HolderOf();
					sb.Append(Lore.Blade()).Append(", carried by ").Append(holder.Name)
					  .Append(Blade.IsBaseborn(holder) ? ", who is not in the book." : ".");
				}
				else
				{
					string blade = Lore.Named();
					sb.Append(string.IsNullOrEmpty(blade)
						? "Unnamed. A blade nobody has named is a blade nobody misses."
						: blade);
				}
				sb.Append("\n");
			}

			if (Cfg.Baseborn)
			{
				string kids = Baseborn.Summary();
				if (!string.IsNullOrEmpty(kids))
				{
					sb.Append("\nNOT OF YOUR HOUSE\n").Append(kids);
				}
			}

			string banner = Bastard.Summary();
			if (!string.IsNullOrEmpty(banner))
			{
				sb.Append("\nTHE REVERSED BANNER\n").Append(banner).Append("\n");
			}
			else if (Cfg.Bastard)
			{
				sb.Append("\n  Somewhere there is a child of this house nobody wrote down.\n");
			}

			return sb.ToString();
		}

		private static void PickHeir()
		{
			try
			{
				List<InquiryElement> els = new List<InquiryElement>();
				Hero current = Succession.Named();
				foreach (Hero h in Succession.Claimants())
				{
					bool lawful = Laws.Legal() == null || Laws.Legal() == h;
					string tip = lawful
						? "The law already says this one, so the naming itself is not held against you."
						: ("Against your culture's law. Every house in the realm will hold it against your heir.");
					if (current != null && h != current)
					{
						tip += "\n\nChanging a name already given costs " + Cfg.RenameHeirCost.ToString("0") +
							" standing: the realm had made its peace with " + current.Name + ".";
					}
					els.Add(new InquiryElement(h, h.Name + "  (" + ((int)h.Age) + ")" +
						((h == current) ? "   - named" : "") + (lawful ? "   - lawful" : ""),
						null, h != current, tip));
				}
				if (els.Count == 0)
				{
					Flow.Notify("There is no one of your house old enough to follow you.");
					return;
				}
				string lawline = "Who follows you?";
				string lawName = Laws.Name();
				Hero legal = Laws.Legal();
				if (!string.IsNullOrEmpty(lawName))
				{
					lawline += "\n\nYour culture's law is " + lawName +
						((legal != null) ? (", and by it the seat belongs to " + legal.Name + ".") : ".") +
						"\n\nNaming anyone else is allowed. That is what the law is worth.";
				}
				Inquiry.Select("Your Heir", lawline, els, 1, 1, "Name them", "Cancel",
					delegate(List<InquiryElement> chosen)
					{
						Hero h = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
						if (h == null)
						{
							return;
						}
						Hero was = current;
						Succession.Name(h);
						if (was != null && was != h)
						{
							// Naming an heir for the first time is free. Only
							// changing a name already given is held against you.
							Standing.Change(-(int)Cfg.RenameHeirCost, 0, "a new heir named over the old");
							Flow.Notify(h.Name + " is named your heir in place of " + was.Name + ".");
						}
						else
						{
							Flow.Notify(h.Name + " is named your heir.");
						}
						Refresh();
					},
					delegate
					{
						Refresh();
					});
			}
			catch (Exception e)
			{
				Log.Write("naming an heir failed: " + e.Message);
			}
		}

		// Which child gets your name.
		private static void PickChild()
		{
			try
			{
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Kid k in Baseborn.Known())
				{
					Hero h = Baseborn.HeroOf(k);
					string why;
					bool can = Baseborn.CanLegitimise(k, out why);
					els.Add(new InquiryElement(k, h.Name + "  (" + ((int)h.Age) + ")" + (k.Legit ? "   - acknowledged" : ""),
						null, can,
						can
							? ("Writing them into the book gives them your name and a claim. Your trueborn kin will each think less of you by " +
								Cfg.LegitKinRelation + ", and the realm by " + Cfg.LegitStanding + ".")
							: (char.ToUpper(why[0]) + why.Substring(1) + ".")));
				}
				if (els.Count == 0)
				{
					Flow.Notify("There is nobody at your gate.");
					return;
				}
				Inquiry.Select("Your Other Children",
					"These are yours. None of them can inherit while they carry the name they were born with.\n\n" +
					"Writing one into the book changes that, and it cannot be undone.",
					els, 1, 1, "Give them my name", "Not today",
					delegate(List<InquiryElement> chosen)
					{
						Kid k = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Kid) : null;
						if (k != null)
						{
							Baseborn.Legitimise(k);
						}
						Refresh();
					},
					delegate
					{
						Refresh();
					});
			}
			catch (Exception e)
			{
				Log.Write("acknowledging failed: " + e.Message);
			}
		}

		// And who carries the sword.
		//
		// This is the decision the whole death event now turns on. Giving it
		// to your heir is an heirloom. Giving it to a child who is not in the
		// book is what Aegon IV did, and it is why there were five Blackfyre
		// Rebellions.
		private static void Bequeath()
		{
			try
			{
				string blade = Lore.Blade();
				List<InquiryElement> els = new List<InquiryElement>();
				Hero holder = Blade.HolderOf();
				Hero heir = Succession.Named();
				foreach (Hero h in Blade.Candidates())
				{
					bool baseborn = Blade.IsBaseborn(h);
					Kid k = baseborn ? Blade.RecordFor(h) : null;
					string tip = baseborn
						? ((k != null && k.Legit)
							? "You have already given them your name. Give them the sword as well and you have named them in every way that matters except the one that counts."
							: "They are not in the book. A sword in their hand is the only argument they will ever need.")
						: ((h == heir)
							? "Your named heir. The sword goes where the seat goes, and nobody is surprised."
							: "Trueborn, but not your heir. Think about what that looks like from their side.");
					els.Add(new InquiryElement(h,
						h.Name + "  (" + ((int)h.Age) + ")" + (baseborn ? "   - not of your house" : "") +
						((h == heir) ? "   - your heir" : "") + ((h == holder) ? "   - has it" : ""),
						null, h != holder, tip));
				}
				if (holder != null)
				{
					els.Add(new InquiryElement("take", "Take it back and hang it on the wall", null, true,
						"It returns to the house. Whatever they thought it meant, they were wrong."));
				}
				if (els.Count == 0)
				{
					Flow.Notify("There is no one of age to carry it.");
					return;
				}
				Inquiry.Select(blade,
					blade + " has been in this house longer than anyone can account for.\n\n" +
					"Whoever holds it when you die is the one the realm will say you chose - whatever the book says, and whatever your will says.",
					els, 1, 1, "Put it in their hand", "Leave it where it is",
					delegate(List<InquiryElement> chosen)
					{
						object pick = (chosen != null && chosen.Count > 0) ? chosen[0].Identifier : null;
						if (pick is string)
						{
							Blade.TakeBack();
							Flow.Notify(blade + " hangs on your wall again.");
						}
						else
						{
							Hero h = pick as Hero;
							if (h != null)
							{
								Blade.Give(h);
								Flow.Notify(h.Name + " carries " + blade + " from today." +
									(Blade.IsBaseborn(h) ? " Everyone saw you do it." : ""));
							}
						}
						Refresh();
					},
					delegate
					{
						Refresh();
					});
			}
			catch (Exception e)
			{
				Log.Write("the bequest failed: " + e.Message);
			}
		}

		// Naming the blade.
		//
		// This is the one thing in the mod the player does purely to make
		// their own death worse. A sword with a name is a sword that can be
		// carried off, and the house that carries it off takes the name with
		// it - which is exactly what Blackfyre was.
		private static void NameBlade()
		{
			try
			{
				string current = Lore.Named();
				Inquiry.Text("Your House's Blade",
					"Valyrian steel outlives the men who carry it. This one has been in your house longer than anyone can account for, and it has never been called anything in particular.\n\n" +
					"Name it.\n\n" +
					"Whoever holds it when you are gone will be remembered by the name you give it now.",
					current ?? "",
					"It is called that", "Leave it unnamed",
					delegate(string chosen)
					{
						// Strip braces. A TextObject reads { and } as variable
						// tokens, so a blade typed as "A{B}" would come out as
						// a house called "House A" everywhere it was used.
						string name = (chosen ?? "").Replace("{", "").Replace("}", "").Trim();
						if (name.Length == 0)
						{
							Flow.Notify("A sword needs a name that can be written down.");
							Refresh();
							return;
						}
						if (name.Length > 24)
						{
							name = name.Substring(0, 24).Trim();
						}
						Store.Set(Lore.BladeKey, name);
						Store.AddDeed(Standing.Date() + "  The sword of your house was named " + name + ".");
						Log.Write("the house blade is named " + name);
						// RoT keeps its own ancestral blade name on the clan
						// page. Tell it, so the two mods name one sword.
						Blade.Sync();
						Flow.Notify("The sword is " + name + " from today, and the maesters have written it down.");
						Refresh();
					},
					delegate
					{
						Refresh();
					});
			}
			catch (Exception e)
			{
				Log.Write("naming the blade failed: " + e.Message);
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
}
