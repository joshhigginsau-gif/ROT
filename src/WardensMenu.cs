using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
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
				if (Cfg.Sworn && Sworn.IsWarden(Clan.PlayerClan))
				{
					stringBuilder.Append("\n\nYOUR SWORN HOUSES\n").Append(MineText());
				}
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
			if (Cfg.Sworn)
			{
				string sworn = Sworn.Summary(myRealm);
				stringBuilder.Append("\nWARDENS AND THEIR SWORN HOUSES\n").Append((sworn.Length > 0) ? sworn : "  No house holds a county or more yet.\n");
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
		Option(s, "wad_w_bid", "Bid a warden raise a house", () => Cfg.Sworn && Sworn.Wardens(MyRealm).Any((Clan c) => c != Clan.PlayerClan), "No house of your realm holds a county or more.", BidWarden);
		Option(s, "wad_w_mine", "Your sworn houses", () => Cfg.Sworn && Sworn.IsWarden(Clan.PlayerClan), "You hold no county or greater title, and no warden's style.", YourHouses, false);
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

	private static void Option(CampaignGameStarter s, string id, string text, Func<bool> allowed, string whyNot, Action run, bool ruler = true)
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
				if (ruler && MyRealm == null)
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

	private static string MineText()
	{
		List<Sworn.Rec> under = Sworn.Under(Clan.PlayerClan);
		if (under.Count == 0)
		{
			return "  None yet.\n";
		}
		return string.Concat(under.Select((Sworn.Rec r) => "  " + Sworn.Line(r) + "\n"));
	}

	// As ruler: tell one of your wardens to take a house.
	private static void BidWarden()
	{
		List<InquiryElement> els = Sworn.Wardens(MyRealm).Where((Clan c) => c != Clan.PlayerClan).Select((Clan c) =>
		{
			string why = Sworn.CannotGain(c, false);
			return new InquiryElement(c, Styles.Titled(c) + "  (" + Sworn.Under(c).Count + " of " + Sworn.Cap(Sworn.Rank(c)) + ")", null, why == null, why ?? (Sworn.FreeVillages(c).Count + " village(s) free for a manor."));
		}).ToList();
		Inquiry.Select("Bid a Warden", "Which warden takes a new house beneath it? It costs you " + Cfg.SwornRaiseCost.ToString("N0") + ".", els, 1, 1, "That one", "Cancel", delegate(List<InquiryElement> chosen)
		{
			Clan w = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Clan) : null;
			if (w != null)
			{
				PickKind(w, Cfg.SwornRaiseCost);
			}
		});
	}

	// As a warden yourself.
	private static void YourHouses()
	{
		Clan me = Clan.PlayerClan;
		List<Sworn.Rec> under = Sworn.Under(me);
		List<InquiryElement> els = new List<InquiryElement>();
		string why = Sworn.CannotGain(me, false);
		els.Add(new InquiryElement("gain", "Raise or invite a house", null, why == null, why ?? "A cadet of your blood, one of your knights or companions, or a landless house of your realm."));
		List<Settlement> grantable = Sworn.Grantable(me);
		els.Add(new InquiryElement("castle", "Grant a fief to a sworn house", null, under.Count > 0 && grantable.Count > 0, (grantable.Count == 0) ? "You hold nothing you can spare besides your seat - or half your fiefs are already given." : "A town or castle of yours. Bellum records it as their barony, beneath your title."));
		els.Add(new InquiryElement("release", "Release a sworn house", null, under.Count > 0, ""));
		Inquiry.Select("Your Sworn Houses", (under.Count == 0) ? "No house is sworn to you yet." : string.Join("\n", under.Select(Sworn.Line)), els, 1, 1, "Go on", "Cancel", delegate(List<InquiryElement> chosen)
		{
			string o = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
			if (o == "gain")
			{
				PickKind(me, 0);
			}
			else if (o == "castle")
			{
				PickFief(me, 0);
			}
			else if (o == "release")
			{
				List<InquiryElement> hs = under.Select((Sworn.Rec r) => new InquiryElement(r, Sworn.Line(r), null)).ToList();
				Inquiry.Select("Release a House", "Which house goes its own way?", hs, 1, 1, "Release them", "Cancel", delegate(List<InquiryElement> c1)
				{
					Sworn.Rec r = (c1 != null && c1.Count > 0) ? (c1[0].Identifier as Sworn.Rec) : null;
					if (r != null)
					{
						Sworn.Release(r, "released by its warden");
						Flow.Notify("They are released from their oath.");
					}
					Refresh();
				});
			}
		});
	}

	// Choose a sworn house of this warden, then one of the warden's fiefs.
	// influence: what it costs you (as ruler bidding another warden).
	private static void PickFief(Clan w, int influence)
	{
		List<Sworn.Rec> under = Sworn.Under(w).Where((Sworn.Rec r) => r.HouseClan != null && r.HouseClan.Leader != null).ToList();
		List<Settlement> can = Sworn.Grantable(w);
		if (under.Count == 0 || can.Count == 0)
		{
			Flow.Notify((under.Count == 0) ? (w.Name + " has no sworn house to enfeoff.") : (w.Name + " has nothing it can spare besides its seat."));
			return;
		}
		List<InquiryElement> hs = under.Select((Sworn.Rec r) => new InquiryElement(r, Sworn.Line(r), null)).ToList();
		Inquiry.Select("Grant a Fief", "To which house of " + w.Name + "?", hs, 1, 1, "Them", "Cancel", delegate(List<InquiryElement> c1)
		{
			Sworn.Rec r = (c1 != null && c1.Count > 0) ? (c1[0].Identifier as Sworn.Rec) : null;
			if (r == null)
			{
				return;
			}
			bool last = w.Fiefs.Count <= 2;
			List<InquiryElement> cs = can.Select((Settlement x) => new InquiryElement(x, (x.IsTown ? "Town of " : "Castle of ") + x.Name, null, true,
				"Prosperity " + ((x.Town != null) ? ((int)x.Town.Prosperity).ToString() : "?") + ", " + x.BoundVillages.Count + " village(s)." + (last ? " It is the last of your fiefs besides your seat." : ""))).ToList();
			Inquiry.Select("Grant a Fief", "Which?" + ((influence > 0) ? (" It costs you " + influence + " influence to bid it.") : ""), cs, 1, 1, "Grant it", "Cancel", delegate(List<InquiryElement> c2)
			{
				Settlement s2 = (c2 != null && c2.Count > 0) ? (c2[0].Identifier as Settlement) : null;
				if (s2 == null)
				{
					return;
				}
				if (influence > 0 && Clan.PlayerClan.Influence < influence)
				{
					Flow.Notify("You lack the influence to bid it.");
					return;
				}
				if (Sworn.GrantFief(r, s2))
				{
					if (influence > 0)
					{
						TaleWorlds.CampaignSystem.Actions.ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -influence);
					}
					Flow.Notify(r.HouseClan.Name + " holds " + s2.Name + " of " + w.Name + " now.");
				}
				else
				{
					Flow.Notify("It could not be granted - the log says why.");
				}
				Refresh();
			});
		});
	}

	// cost: what the player pays when bidding another warden; for your own
	// following, the raise and invite costs apply.
	private static void PickKind(Clan w, int cost)
	{
		bool mine = w == Clan.PlayerClan;
		int raise = mine ? Cfg.SwornRaiseCost : cost;
		int invite = mine ? Cfg.SwornInviteCost : cost;
		List<Hero> cadets = Sworn.CadetCandidates(w);
		List<Hero> knights = Sworn.KnightCandidates(w);
		List<Clan> invitees = Sworn.InviteCandidates(w);
		List<InquiryElement> els = new List<InquiryElement>();
		els.Add(new InquiryElement("cadet", "A cadet branch of " + w.Name + " (" + raise.ToString("N0") + ")", null, cadets.Count > 0 && Hero.MainHero.Gold >= raise, (cadets.Count > 0) ? (cadets.Count + " of its blood could found it.") : "Nobody of its blood is free."));
		els.Add(new InquiryElement("knight", "Raise a knight to a landed house (" + raise.ToString("N0") + ")", null, Hero.MainHero.Gold >= raise, (knights.Count > 0) ? (knights.Count + " companion(s) or knight(s) to choose from.") : (mine ? "None of your companions or knights is free - a new knight will be found." : "A knight of its following.")));
		els.Add(new InquiryElement("invited", "Invite a landless house (" + invite.ToString("N0") + ")", null, invitees.Count > 0 && Hero.MainHero.Gold >= invite, (invitees.Count > 0) ? (invitees.Count + " landless house(s) of the realm.") : "No landless house to invite."));
		if (!mine)
		{
			bool canFief = Sworn.Under(w).Count > 0 && Sworn.Grantable(w).Count > 0;
			els.Add(new InquiryElement("fief", "Have it grant a fief to one of its sworn houses (" + Cfg.SwornBidFiefInfluence + " influence)", null, canFief && Clan.PlayerClan.Influence >= Cfg.SwornBidFiefInfluence,
				canFief ? "A town or castle of " + w.Name + "'s, given to a house sworn to it." : (w.Name + " has no sworn house, or nothing it can spare.")));
		}
		Inquiry.Select("A New House", "What kind of house swears to " + w.Name + "? It takes a manor of " + w.Name + "'s.", els, 1, 1, "That", "Cancel", delegate(List<InquiryElement> chosen)
		{
			string kind = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
			if (kind == null)
			{
				return;
			}
			if (kind == "fief")
			{
				PickFief(w, Cfg.SwornBidFiefInfluence);
				return;
			}
			int pay = (kind == "invited") ? invite : raise;
			if (kind == "invited")
			{
				List<InquiryElement> cs = invitees.Select((Clan c) => new InquiryElement(c, c.Name + " (" + c.Leader.Name + ")", null)).ToList();
				Inquiry.Select("Invite a House", "Which house?", cs, 1, 1, "Them", "Cancel", delegate(List<InquiryElement> c1)
				{
					Clan c = (c1 != null && c1.Count > 0) ? (c1[0].Identifier as Clan) : null;
					if (c != null)
					{
						Do(w, kind, null, c, null, pay);
					}
				});
				return;
			}
			List<Hero> who = (kind == "cadet") ? cadets : knights;
			if (who.Count == 0)
			{
				Do(w, kind, null, null, null, pay);
				return;
			}
			List<InquiryElement> hs = who.Select((Hero h) => new InquiryElement(h, h.Name.ToString(), null, true, h.Age.ToString("0") + " years old")).ToList();
			Inquiry.Select("Who?", "Who founds the house?", hs, 1, 1, "Them", "Cancel", delegate(List<InquiryElement> c1)
			{
				Hero h = (c1 != null && c1.Count > 0) ? (c1[0].Identifier as Hero) : null;
				if (h == null)
				{
					return;
				}
				string suggestion = Knighting.HouseName(h.Culture ?? w.Culture);
				Inquiry.Text("The Herald", "The herald suggests " + suggestion + ".", suggestion, "So it is written", "Cancel", delegate(string text)
				{
					string name = string.IsNullOrWhiteSpace(text) ? suggestion : text.Trim();
					if (!name.StartsWith("House ", StringComparison.OrdinalIgnoreCase))
					{
						name = "House " + name;
					}
					Do(w, kind, h, null, name, pay);
				}, null);
			});
		});
	}

	private static void Do(Clan w, string kind, Hero who, Clan invitee, string name, int pay)
	{
		if (Hero.MainHero.Gold < pay)
		{
			Flow.Notify("You cannot pay for it.");
			return;
		}
		Clan house = Sworn.Gain(w, kind, who, invitee, name, false);
		if (house == null)
		{
			Flow.Notify("It could not be done - the log says why.");
			return;
		}
		Hero.MainHero.ChangeHeroGold(-pay);
		Refresh();
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
