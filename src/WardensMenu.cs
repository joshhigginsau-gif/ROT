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

namespace WardensAndDragons
{
internal static class WardensMenu
{
	private static Kingdom MyRealm
	{
		get
		{
			Clan playerClan = Clan.PlayerClan;
			return (playerClan == null || playerClan.Kingdom == null || playerClan.Kingdom.RulingClan != playerClan) ? null : playerClan.Kingdom;
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
		Kingdom k = MyRealm;
		if (k == null)
		{
			return new List<Kingdom>();
		}
		return ((IEnumerable<Kingdom>)Kingdom.All).Where((Kingdom x) => x != null && !x.IsEliminated && object.ReferenceEquals(Clients.SuzerainOf(x), k)).ToList();
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
			List<Clan> list2 = list.Where((Clan c) => Oaths.Of(c) != OathKind.None || Styles.Of(c) != null).ToList();
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
					Clan val = ((IEnumerable<Clan>)Clan.All).FirstOrDefault((Clan x) => x != null && ((MBObjectBase)x).StringId == liege);
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
				stringBuilder.Append((oathKind2 != OathKind.None) ? (Oaths.Def(oathKind2).Name + ", " + Oaths.TributeOf(item2).ToString("N0") + " a year") : "no terms set");
				stringBuilder.Append(", liberty ").Append((!(num2 < 0f)) ? ((int)num2/*cast due to constrained. prefix*/).ToString() : "unknown");
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
			a.optionLeaveType = (GameMenuOption.LeaveType)16;
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
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Expected O, but got Unknown
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Expected O, but got Unknown
			a.optionLeaveType = (GameMenuOption.LeaveType)2;
			// All six options on this menu come through here, so one throw out
			// of allowed() took the whole menu's option list with it - the way
			// out included. Everything below is guarded for that reason.
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
			catch (Exception e)
			{
				a.IsEnabled = false;
				a.Tooltip = Styles.Line("This cannot be read just now.");
				Log.Once("wardencond", "a warden option failed to draw: " + e.Message);
			}
			return true;
		}, (GameMenuOption.OnConsequenceDelegate)delegate
		{
			try
			{
				run();
			}
			catch (Exception e)
			{
				Log.Once("wardenrun", "a warden option failed: " + e.Message);
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
		List<InquiryElement> els = ((IEnumerable<Clan>)Vassals()).Select((Func<Clan, InquiryElement>)delegate(Clan c)
		{
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected O, but got Unknown
			return new InquiryElement((object)c, Styles.Titled(c) + "  (" + ((Oaths.Of(c) != OathKind.None) ? Oaths.Def(Oaths.Of(c)).Name : "no oath") + ")", (ImageIdentifier)null);
		}).ToList();
		Inquiry.Select("Set an Oath", "Which house's terms will you set?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((identifier is Clan) ? identifier : null);
			}
			else
			{
				obj = null;
			}
			Clan c = (Clan)obj;
			if (c != null)
			{
				Oaths.Choose("Terms of the Oath", string.Concat("On what terms does ", c.Name, " hold of you?"), c, null, delegate(OathKind kind)
				{
					if (kind != OathKind.None)
					{
						Flow.Notify(Oaths.SetForClan(c, kind));
					}
					Refresh();
				});
			}
		});
	}

	private static void PickHouseForStyle()
	{
		List<InquiryElement> els = ((IEnumerable<Clan>)Vassals()).Select((Func<Clan, InquiryElement>)delegate(Clan c)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Expected O, but got Unknown
			return new InquiryElement((object)c, Styles.Titled(c), (ImageIdentifier)null);
		}).ToList();
		Inquiry.Select("Grant a Style", "Which house will you style?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Expected O, but got Unknown
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Expected O, but got Unknown
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Expected O, but got Unknown
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((identifier is Clan) ? identifier : null);
			}
			else
			{
				obj = null;
			}
			Clan c = (Clan)obj;
			if (c != null)
			{
				List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(c);
				string canon = ((list.Count <= 0) ? null : Styles.CanonicalFor(Bellum.PlainName(list[0].Value)));
				List<InquiryElement> list2 = new List<InquiryElement>();
				if (canon != null)
				{
					list2.Add(new InquiryElement((object)"canon", canon, (ImageIdentifier)null));
				}
				list2.Add(new InquiryElement((object)"custom", "A style of your own choosing...", (ImageIdentifier)null));
				if (Styles.Of(c) != null)
				{
					list2.Add(new InquiryElement((object)"clear", "Strip their style", (ImageIdentifier)null));
				}
				Inquiry.Select("The Style", string.Concat("How shall ", c.Name, " be known?"), list2, 1, 1, "Proclaim", "Cancel", delegate(List<InquiryElement> pick)
				{
					switch ((pick == null || pick.Count <= 0) ? null : (pick[0].Identifier as string))
					{
					case "canon":
						Styles.Set(c, canon);
						Flow.Notify(string.Concat(c.Name, " is proclaimed ", canon, "."));
						Refresh();
						break;
					case "clear":
						Styles.Set(c, null);
						Flow.Notify(string.Concat(c.Name, " no longer bears a style."));
						Refresh();
						break;
					case "custom":
						Inquiry.Text("The Style", string.Concat("Name the style ", c.Name, " shall bear."), canon ?? "Warden of ", "Proclaim", "Cancel", delegate(string text)
						{
							if (!string.IsNullOrEmpty(text))
							{
								Styles.Set(c, text.Trim());
								Flow.Notify(string.Concat(c.Name, " is proclaimed ", text.Trim(), "."));
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
		List<InquiryElement> els = ((IEnumerable<Clan>)Wardens()).Select((Func<Clan, InquiryElement>)delegate(Clan c)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Expected O, but got Unknown
			return new InquiryElement((object)c, Styles.Titled(c), (ImageIdentifier)null);
		}).ToList();
		Inquiry.Select("Swear Houses", "Which warden shall other houses answer to?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((identifier is Clan) ? identifier : null);
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
					List<InquiryElement> els2 = ((IEnumerable<KeyValuePair<string, object>>)list).Select((Func<KeyValuePair<string, object>, InquiryElement>)delegate(KeyValuePair<string, object> t)
					{
						//IL_000f: Unknown result type (might be due to invalid IL or missing references)
						//IL_0015: Expected O, but got Unknown
						return new InquiryElement(t.Value, t.Key, (ImageIdentifier)null);
					}).ToList();
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
		List<InquiryElement> els = ((IEnumerable<Kingdom>)ClientRealms()).Select((Func<Kingdom, InquiryElement>)delegate(Kingdom k)
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Expected O, but got Unknown
			return new InquiryElement((object)k, string.Concat(k.Name, "  (", (Oaths.Of(k) != OathKind.None) ? Oaths.Def(Oaths.Of(k)).Name : "no terms", ")"), (ImageIdentifier)null);
		}).ToList();
		Inquiry.Select("Set Terms", "Which realm's terms will you set?", els, 1, 1, "Choose", "Cancel", delegate(List<InquiryElement> chosen)
		{
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((identifier is Kingdom) ? identifier : null);
			}
			else
			{
				obj = null;
			}
			Kingdom k = (Kingdom)obj;
			if (k != null)
			{
				Oaths.Choose("Terms of the Oath", string.Concat("On what terms does ", k.Name, " hold of you? Tightening them angers the realm for a year."), null, k, delegate(OathKind kind)
				{
					if (kind != OathKind.None)
					{
						Flow.Notify(Oaths.SetForKingdom(k, kind));
					}
					Refresh();
				});
			}
		});
	}

	private static void PickClientToRelease()
	{
		List<InquiryElement> els = ((IEnumerable<Kingdom>)ClientRealms()).Select((Func<Kingdom, InquiryElement>)delegate(Kingdom k)
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected O, but got Unknown
			return new InquiryElement((object)k, ((object)k.Name).ToString(), (ImageIdentifier)null);
		}).ToList();
		Inquiry.Select("Release a Realm", "Which realm will you free from its oath? This cannot be taken back.", els, 1, 1, "Release", "Cancel", delegate(List<InquiryElement> chosen)
		{
			object obj;
			if (chosen != null && chosen.Count > 0)
			{
				object identifier = chosen[0].Identifier;
				obj = ((identifier is Kingdom) ? identifier : null);
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
}
