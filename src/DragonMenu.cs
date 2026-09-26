using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
internal static class DragonMenu
{
	internal static void Register(CampaignGameStarter s)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Expected O, but got Unknown
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Expected O, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Expected O, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		string[] array = new string[2] { "castle", "town" };
		foreach (string text in array)
		{
			s.AddGameMenuOption(text, "wad_dragonmont_" + text, "{=WAD_Dragonmont}Climb to the Dragonmont", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
			{
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_0046: Unknown result type (might be due to invalid IL or missing references)
				//IL_0050: Expected O, but got Unknown
				Settlement currentSettlement = Settlement.CurrentSettlement;
				Settlement seat = Dragons.Seat;
				if (currentSettlement == null || seat == null || currentSettlement != seat)
				{
					return false;
				}
				a.optionLeaveType = (GameMenuOption.LeaveType)2;
				if (seat.OwnerClan != Clan.PlayerClan)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("Only the lord of Dragonstone may send anyone up the Dragonmont.");
				}
				return true;
			}, (GameMenuOption.OnConsequenceDelegate)delegate
			{
				GameMenu.SwitchToMenu("wad_dragonmont");
			}, false, 2, false, (object)null);
		}
		s.AddGameMenu("wad_dragonmont", "{=!}{WAD_MOUNT}", (OnInitDelegate)delegate
		{
			SetText();
		}, (GameMenu.MenuOverlayType)0, (GameMenu.MenuFlags)0, (object)null);
		s.AddGameMenuOption("wad_dragonmont", "wad_mount_claim", "{=!}Send a claimant up the mountain", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Expected O, but got Unknown
			a.optionLeaveType = (GameMenuOption.LeaveType)2;
			if (!Dragons.All().Any((DragonRec r) => r.Claimable))
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("The Dragonmont is silent. No dragon waits here.");
			}
			else if (!Candidates().Any((KeyValuePair<Hero, string> c) => c.Value == null))
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("No one of your house can climb this year.");
			}
			else
			{
				float odds = Cfg.ClaimBase * Dragons.Crowding() * Dragons.Twilight();
				a.Tooltip = Styles.Line("A claim would take at roughly " + odds.ToString("0") + "% today, before the claimant's own worth. Failing it is usually fatal.");
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			PickClaimant();
		}, false, 0, false, (object)null);
		s.AddGameMenuOption("wad_dragonmont", "wad_mount_leave", "{=!}Come down from the mountain", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (GameMenuOption.LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			Settlement currentSettlement = Settlement.CurrentSettlement;
			GameMenu.SwitchToMenu((currentSettlement == null || !currentSettlement.IsTown) ? "castle" : "town");
		}, true, 9, false, (object)null);
	}

	private static List<KeyValuePair<Hero, string>> Candidates()
	{
		List<KeyValuePair<Hero, string>> list = new List<KeyValuePair<Hero, string>>();
		Clan playerClan = Clan.PlayerClan;
		if (playerClan == null)
		{
			return list;
		}
		foreach (Hero item in (List<Hero>)(object)playerClan.Heroes)
		{
			if (item != null && item.IsAlive)
			{
				list.Add(new KeyValuePair<Hero, string>(item, (!Dragons.CanClaim(item, out var why)) ? why : null));
			}
		}
		return list;
	}

	private static void SetText()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("Smoke rises from the vents of the Dragonmont, and the rock is warm underfoot. Bones lie in the ash - sheep, mostly. Mostly.\n\n");
			List<DragonRec> list = (from r in Dragons.All()
				where r.Claimable
				select r).ToList();
			if (list.Count == 0)
			{
				stringBuilder.Append("No dragon waits here now. The mountain is silent.\n");
			}
			else
			{
				stringBuilder.Append("RIDERLESS DRAGONS\n");
				foreach (DragonRec item in list)
				{
					stringBuilder.Append("  ").Append(item.Name).Append(" - ")
						.Append(Dragons.SizeOf(item))
						.Append(", ")
						.Append(Dragons.TemperOf(item));
					if (item.Status == "wild")
					{
						stringBuilder.Append(", wild");
					}
					if (item.Kills > 0)
					{
						stringBuilder.Append(", has killed ").Append(item.Kills);
					}
					if (item.LastTry > -99999)
					{
						stringBuilder.Append(", last approached ").Append((CourtBehavior.Today() - item.LastTry) / Math.Max(1, Cfg.DaysPerYear)).Append(" year(s) ago");
					}
					stringBuilder.Append("\n");
				}
			}
			stringBuilder.Append("\nOF YOUR HOUSE\n");
			foreach (KeyValuePair<Hero, string> item2 in Candidates())
			{
				stringBuilder.Append("  ").Append(item2.Key.Name).Append((item2.Value != null) ? (" - " + item2.Value) : " - may climb")
					.Append("\n");
			}
			stringBuilder.Append("\n").Append(Dragons.LivingCount()).Append(" dragons live in the world. The more there are, the less each egg and each claim will come to.");
			stringBuilder.Append(" A failed claim kills ").Append(Cfg.ClaimDeathChance).Append("% of the time");
			if (!Cfg.PlayerClaimCanDie)
			{
				// The confirm popup reads DeathChance(hero), which returns 0
				// for the player when this is off. Printing the raw config
				// figure here meant the page said 80% and the popup said 0%,
				// with nothing to say which one applied.
				stringBuilder.Append(" - though never you, by your config");
			}
			stringBuilder.Append(".");
			MBTextManager.SetTextVariable("WAD_MOUNT", stringBuilder.ToString(), false);
		}
		catch (Exception ex)
		{
			MBTextManager.SetTextVariable("WAD_MOUNT", "The Dragonmont.", false);
			Log.Once("mounttext", "dragonmont text failed: " + ex);
		}
	}

	private static void Refresh()
	{
		try
		{
			GameMenu.SwitchToMenu("wad_dragonmont");
		}
		catch
		{
		}
	}

	private static void PickClaimant()
	{
		List<InquiryElement> els = ((IEnumerable<KeyValuePair<Hero, string>>)Candidates()).Select((Func<KeyValuePair<Hero, string>, InquiryElement>)delegate(KeyValuePair<Hero, string> c)
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Expected O, but got Unknown
			return new InquiryElement((object)c.Key, string.Concat(c.Key.Name, (c.Value != null) ? ("  (" + c.Value + ")") : ""), (ImageIdentifier)null, c.Value == null, c.Value ?? "");
		}).ToList();
		Inquiry.Select("The Claimant", "Who will climb the Dragonmont?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((identifier is Hero) ? identifier : null);
			}
			else
			{
				obj = null;
			}
			Hero val = (Hero)obj;
			if (val != null)
			{
				PickDragon(val);
			}
		});
	}

	private static void PickDragon(Hero h)
	{
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (DragonRec item in from r in Dragons.All()
			where r.Claimable
			select r)
		{
			List<string> list2 = new List<string>();
			float num = Dragons.ClaimChance(h, item, list2);
			list.Add(new InquiryElement((object)item, item.Name + " - " + Dragons.SizeOf(item) + ", " + Dragons.TemperOf(item) + " - " + (num * 100f).ToString("0") + "% chance", (ImageIdentifier)null, true, string.Join("\n", list2.ToArray())));
		}
		Inquiry.Select("The Dragon", string.Concat("Which dragon will ", h.Name, " approach?"), list, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			DragonRec dragonRec = ((chosen == null || chosen.Count <= 0) ? null : (chosen[0].Identifier as DragonRec));
			if (dragonRec != null)
			{
				Confirm(h, dragonRec);
			}
		});
	}

	private static void Confirm(Hero h, DragonRec d)
	{
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0150: Expected O, but got Unknown
		List<string> list = new List<string>();
		float num = Dragons.ClaimChance(h, d, list);
		float num2 = Dragons.DeathChance(h);
		string text = ((h != Hero.MainHero) ? ((object)h.Name).ToString() : "You");
		string text2 = text + " will climb toward " + d.Name + ".\n\nChance it bows: " + (num * 100f).ToString("0.0") + "%\nIf it does not: " + (num2 * 100f).ToString("0") + "% chance of death\n\n" + string.Join("\n", list.ToArray()) + ((h != Hero.MainHero || !(num2 > 0f)) ? "" : "\n\nIf you die, your heir takes up your house - or, with no heir, your story ends here.");
		try
		{
			InformationManager.ShowInquiry(new InquiryData("Climb the Dragonmont", text2, true, true, "Climb", "Not today", (Action)delegate
			{
				Dragons.Resolve(h, d);
				Refresh();
			}, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch (Exception ex)
		{
			Log.Write("confirm failed: " + ex.Message);
		}
	}
}
}
