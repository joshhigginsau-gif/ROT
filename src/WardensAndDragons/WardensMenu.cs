using LeaveType = TaleWorlds.CampaignSystem.GameMenus.GameMenuOption.LeaveType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class WardensMenu
{
	private static Kingdom MyRealm
	{
		get
		{
			Clan playerClan = Clan.PlayerClan;
			return (playerClan != null && playerClan.Kingdom != null && playerClan.Kingdom.RulingClan == playerClan) ? playerClan.Kingdom : null;
		}
	}

	private static List<Clan> Vassals()
	{
		Kingdom myRealm = MyRealm;
		if (myRealm == null)
		{
			return new List<Clan>();
		}
		return ((IEnumerable<Clan>)myRealm.Clans).Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && c.Leader != null).ToList();
	}

	private static List<Clan> Wardens()
	{
		return (from c in Vassals()
			where Bellum.TitlesHeldBy(c).Count > 0
			select c).ToList();
	}

	private static List<Kingdom> ClientRealms()
	{
		Kingdom i = MyRealm;
		if (i == null)
		{
			return new List<Kingdom>();
		}
		return ((IEnumerable<Kingdom>)Kingdom.All).Where((Kingdom x) => x != null && !x.IsEliminated && object.ReferenceEquals(Clients.SuzerainOf(x), i)).ToList();
	}

	internal static void SetText()
	{
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			Kingdom myRealm = MyRealm;
			if (myRealm == null)
			{
				stringBuilder.Append("You rule no realm. Wardens and clients answer only to a sovereign.");
				MBTextManager.SetTextVariable("WAD_WARDENS", stringBuilder.ToString(), false);
				return;
			}
			List<Clan> list = Vassals();
			List<Clan> list2 = list.Where((Clan c) => Oaths.Of(c) != 0 || Styles.Of(c) != null).ToList();
			stringBuilder.Append("WARDENS AND SWORN HOUSES\n");
			if (list2.Count == 0)
			{
				stringBuilder.Append("  None yet. Name a warden in conversation, or set a house's oath below.\n");
			}
			foreach (Clan item in list2)
			{
				OathKind oathKind = Oaths.Of(item);
				stringBuilder.Append("  ").Append(Styles.Titled(item)).Append(" - ");
				if (oathKind == OathKind.None)
				{
					stringBuilder.Append("no oath sworn");
				}
				else
				{
					stringBuilder.Append(Oaths.Def(oathKind).Name).Append(", ").Append(Oaths.YearsUnder(item))
						.Append(" yr, ")
						.Append(Oaths.TributeOf(item).ToString("N0"))
						.Append(" a year");
				}
				string liege = Store.Get("sworn:" + ((MBObjectBase)item).StringId);
				if (liege != null)
				{
					Clan val = ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Func<Clan, bool>)((Clan x) => x != null && ((MBObjectBase)x).StringId == liege));
					if (val != null)
					{
						stringBuilder.Append(", holds of ").Append(val.Name);
					}
				}
				stringBuilder.Append("\n");
			}
			int num = list.Count - list2.Count;
			if (num > 0)
			{
				stringBuilder.Append("  ").Append(num).Append(" other house(s) of your realm have sworn no named oath.\n");
			}
			List<Kingdom> list3 = ClientRealms();
			stringBuilder.Append("\nCLIENT REALMS\n");
			if (list3.Count == 0)
			{
				stringBuilder.Append("  None. Offer suzerainty to a foreign ruler in conversation.\n");
			}
			foreach (Kingdom item2 in list3)
			{
				OathKind oathKind2 = Oaths.Of(item2);
				float num2 = Absorb.LibertyOf(item2);
				stringBuilder.Append("  ").Append(item2.Name).Append(" - ");
				stringBuilder.Append((oathKind2 == OathKind.None) ? "no terms set" : (Oaths.Def(oathKind2).Name + ", " + Oaths.TributeOf(item2).ToString("N0") + " a year"));
				stringBuilder.Append(", liberty ").Append((num2 < 0f) ? "unknown" : ((int)num2).ToString());
				if (num2 >= 0f && num2 < Cfg.AbsorbMaxLiberty)
				{
					stringBuilder.Append(" (content enough to absorb)");
				}
				stringBuilder.Append("\n");
			}
			int i = Store.GetI("tribute:last", CourtBehavior.Today());
			int value = Math.Max(0, Cfg.DaysPerYear - (CourtBehavior.Today() - i));
			stringBuilder.Append("\nTribute is gathered once a year - next in ").Append(value).Append(" days.");
			MBTextManager.SetTextVariable("WAD_WARDENS", stringBuilder.ToString(), false);
		}
		catch (Exception ex)
		{
			MBTextManager.SetTextVariable("WAD_WARDENS", "Your wardens and clients.", false);
			Log.Once("wardenstext", "wardens text failed: " + ex);
		}
	}

	internal static void AddOptions(CampaignGameStarter s)
	{
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b3: Expected O, but got Unknown
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Expected O, but got Unknown
		Option(s, "wad_w_oath", "Set a house's oath", () => Vassals().Count > 0, "No other house serves you.", PickHouseForOath);
		Option(s, "wad_w_style", "Grant or change a style", () => Vassals().Count > 0, "No other house serves you.", PickHouseForStyle);
		Option(s, "wad_w_swear", "Swear houses to a warden", () => Wardens().Count > 0, "No house holds land to be a warden over others.", PickWardenToSwear);
		Option(s, "wad_w_client", "Set a client realm's terms", () => ClientRealms().Count > 0, "You have no client realms.", PickClientForTerms);
		Option(s, "wad_w_release", "Release a client realm", () => ClientRealms().Count > 0, "You have no client realms.", PickClientToRelease);
		s.AddGameMenuOption("wad_wardens", "wad_wardens_back", "{=WAD_Back}Return to the court", (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)16;
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			GameMenu.SwitchToMenu("wad_court");
		}, true, 9, false, (object)null);
	}

	private static void Option(CampaignGameStarter s, string id, string text, Func<bool> allowed, string whyNot, Action run)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected O, but got Unknown
		//IL_0050: Expected O, but got Unknown
		s.AddGameMenuOption("wad_wardens", id, "{=!}" + text, (GameMenuOption.OnConditionDelegate)delegate(MenuCallbackArgs a)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			a.optionLeaveType = (LeaveType)2;
			try
			{
				if (MyRealm == null)
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line("You rule no realm.");
				}
				else if (!allowed())
				{
					a.IsEnabled = false;
					a.Tooltip = Styles.Line(whyNot);
				}
			}
			catch (Exception ex2)
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("This cannot be read just now.");
				Log.Once("wardencond", "a warden option failed to draw: " + ex2.Message);
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			try
			{
				run();
			}
			catch (Exception ex)
			{
				Log.Once("wardenrun", "a warden option failed: " + ex.Message);
				Flow.Notify("That could not be carried out.");
			}
		}, false, -1, false, (object)null);
	}

	private static void Refresh()
	{
		try
		{
			GameMenu.SwitchToMenu("wad_wardens");
		}
		catch
		{
		}
	}

	private static void PickHouseForOath()
	{
		List<InquiryElement> els = ((IEnumerable<Clan>)Vassals()).Select((Func<Clan, InquiryElement>)((Clan c) => new InquiryElement((object)c, Styles.Titled(c) + "  (" + ((Oaths.Of(c) == OathKind.None) ? "no oath" : Oaths.Def(Oaths.Of(c)).Name) + ")", (ImageIdentifier)null))).ToList();
		Inquiry.Select("Set an Oath", "Which house's terms will you set?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((!(identifier is Clan)) ? null : identifier);
			}
			else
			{
				obj = null;
			}
			Clan c2 = (Clan)obj;
			if (c2 != null)
			{
				Oaths.Choose("Terms of the Oath", string.Concat("On what terms does ", c2.Name, " hold of you?"), c2, null, delegate(OathKind kind)
				{
					if (kind != 0)
					{
						Flow.Notify(Oaths.SetForClan(c2, kind));
					}
					Refresh();
				});
			}
		});
	}

	private static void PickHouseForStyle()
	{
		List<InquiryElement> els = ((IEnumerable<Clan>)Vassals()).Select((Func<Clan, InquiryElement>)((Clan c) => new InquiryElement((object)c, Styles.Titled(c), (ImageIdentifier)null))).ToList();
		Inquiry.Select("Grant a Style", "Which house will you style?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Expected O, but got Unknown
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Expected O, but got Unknown
			//IL_0102: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Expected O, but got Unknown
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((!(identifier is Clan)) ? null : identifier);
			}
			else
			{
				obj = null;
			}
			Clan c2 = (Clan)obj;
			if (c2 != null)
			{
				List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(c2);
				string canon = ((list.Count > 0) ? Styles.CanonicalFor(Bellum.PlainName(list[0].Value)) : null);
				List<InquiryElement> list2 = new List<InquiryElement>();
				if (canon != null)
				{
					list2.Add(new InquiryElement((object)"canon", canon, (ImageIdentifier)null));
				}
				list2.Add(new InquiryElement((object)"custom", "A style of your own choosing...", (ImageIdentifier)null));
				if (Styles.Of(c2) != null)
				{
					list2.Add(new InquiryElement((object)"clear", "Strip their style", (ImageIdentifier)null));
				}
				Inquiry.Select("The Style", string.Concat("How shall ", c2.Name, " be known?"), list2, 1, 1, "Proclaim", "Cancel", delegate(List<InquiryElement> pick)
				{
					switch ((pick != null && pick.Count > 0) ? (pick[0].Identifier as string) : null)
					{
					case "canon":
						Styles.Set(c2, canon);
						Flow.Notify(string.Concat(c2.Name, " is proclaimed ", canon, "."));
						Refresh();
						break;
					case "clear":
						Styles.Set(c2, null);
						Flow.Notify(string.Concat(c2.Name, " no longer bears a style."));
						Refresh();
						break;
					case "custom":
						Inquiry.Text("The Style", string.Concat("Name the style ", c2.Name, " shall bear."), canon ?? "Warden of ", "Proclaim", "Cancel", delegate(string text)
						{
							if (!string.IsNullOrEmpty(text))
							{
								Styles.Set(c2, text.Trim());
								Flow.Notify(string.Concat(c2.Name, " is proclaimed ", text.Trim(), "."));
							}
							Refresh();
						}, Refresh);
						break;
					}
				});
			}
		});
	}

	private static void PickWardenToSwear()
	{
		List<InquiryElement> els = ((IEnumerable<Clan>)Wardens()).Select((Func<Clan, InquiryElement>)((Clan c) => new InquiryElement((object)c, Styles.Titled(c), (ImageIdentifier)null))).ToList();
		Inquiry.Select("Swear Houses", "Which warden shall other houses answer to?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((!(identifier is Clan)) ? null : identifier);
			}
			else
			{
				obj = null;
			}
			Clan w = (Clan)obj;
			if (w != null)
			{
				List<KeyValuePair<string, object>> list = (from t in Bellum.TitlesHeldBy(w)
					orderby Bellum.TierOf(t.Value) descending
					select t).ToList();
				if (list.Count == 1)
				{
					Flow.SwearHouses(w, list[0].Value, Refresh);
				}
				else
				{
					List<InquiryElement> els2 = ((IEnumerable<KeyValuePair<string, object>>)list).Select((Func<KeyValuePair<string, object>, InquiryElement>)((KeyValuePair<string, object> t) => new InquiryElement(t.Value, t.Key, (ImageIdentifier)null))).ToList();
					Inquiry.Select("Which Seat", string.Concat("Beneath which of ", w.Name, "'s titles shall they hold?"), els2, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> pick)
					{
						if (pick != null && pick.Count > 0)
						{
							Flow.SwearHouses(w, pick[0].Identifier, Refresh);
						}
					});
				}
			}
		});
	}

	private static void PickClientForTerms()
	{
		List<InquiryElement> els = ((IEnumerable<Kingdom>)ClientRealms()).Select((Func<Kingdom, InquiryElement>)((Kingdom k) => new InquiryElement((object)k, string.Concat(k.Name, "  (", (Oaths.Of(k) == OathKind.None) ? "no terms" : Oaths.Def(Oaths.Of(k)).Name, ")"), (ImageIdentifier)null))).ToList();
		Inquiry.Select("Set Terms", "Which realm's terms will you set?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected O, but got Unknown
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((!(identifier is Kingdom)) ? null : identifier);
			}
			else
			{
				obj = null;
			}
			Kingdom i = (Kingdom)obj;
			if (i != null)
			{
				Oaths.Choose("Terms of the Oath", string.Concat("On what terms does ", i.Name, " hold of you? Tightening them angers the realm for a year."), null, i, delegate(OathKind kind)
				{
					if (kind != 0)
					{
						Flow.Notify(Oaths.SetForKingdom(i, kind));
					}
					Refresh();
				});
			}
		});
	}

	private static void PickClientToRelease()
	{
		List<InquiryElement> els = ((IEnumerable<Kingdom>)ClientRealms()).Select((Func<Kingdom, InquiryElement>)((Kingdom k) => new InquiryElement((object)k, ((object)k.Name).ToString(), (ImageIdentifier)null))).ToList();
		Inquiry.Select("Release a Realm", "Which realm will you free from its oath? This cannot be taken back.", els, 1, 1, "Release", "Cancel", delegate(List<InquiryElement> chosen)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected O, but got Unknown
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((!(identifier is Kingdom)) ? null : identifier);
			}
			else
			{
				obj = null;
			}
			Kingdom val = (Kingdom)obj;
			if (val != null)
			{
				if (Clients.Release(val, out var report))
				{
					Oaths.ClearKingdom(val);
					Standing.Change(Cfg.ReleaseHonour, 0, string.Concat("Released ", val.Name, " from its oath"));
					Flow.Notify(string.Concat(val.Name, " is free of you."));
				}
				else
				{
					Flow.Notify("It could not be done: " + (report ?? "no reason given"));
				}
				Refresh();
			}
		});
	}
}
