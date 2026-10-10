using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Abdication: set down the crown, and start a house of your own.
	//
	// The ruling house passes to the named heir as an ordinary house. The
	// player plays on with nothing - one horse, one blade, plain clothes and a
	// little coin - as the founder of a new house: either the ruler in person,
	// or an adult younger child sent out to found a cadet line.
	//
	// What the game does when the player's hero changes clan (read from the
	// decompiled CampaignSystem before this was written):
	//  - Hero.Clan's setter moves the hero between the clans' caches and fires
	//    OnHeroChangedClan. It touches no party.
	//  - ClanVariablesCampaignBehavior answers that event: if the hero LED the
	//    clan they left, it picks that clan a new leader itself, at random from
	//    the top-scored heirs. So the heir is made leader BEFORE the move.
	//  - MobileParty.ActualClan is set once, when the party is made, and never
	//    follows its owner. Left alone, the player's party would still count as
	//    one of the old house's war parties, fly its faction and fight its wars.
	//    Its public setter re-registers the party with the new clan, so it is
	//    set by hand.
	//  - ChangeClanLeaderAction hands the old leader's whole purse to the new
	//    one (the crown keeps the gold), and makes the heir leader of whatever
	//    party they ride in - the player's own, if they ride with you. So the
	//    heir is given a party of their own first.
	internal static class Abdication
	{
		private const string DonePrefix = "ab:done:";   // a house that has seen one abdication
		private const string WasPrefix = "ab:was:";     // a house the player left this way
		private const string LastKey = "ab:last";       // old|new|who|day

		internal sealed class Check
		{
			internal string Text;
			internal bool Ok;
			internal bool Blocks;   // a cross here refuses outright
			internal string Cost;   // what a cross costs, if it does not refuse
		}

		internal static bool IsOldHouse(Clan c)
		{
			return c != null && !string.IsNullOrEmpty(Store.Get(WasPrefix + ((MBObjectBase)c).StringId));
		}

		// ------------------------------------------------------------------
		// who

		internal static Hero Heir()
		{
			Hero h = Succession.Named();
			return (h != null && h != Hero.MainHero) ? h : null;
		}

		internal static List<Hero> Children(Hero heir)
		{
			Hero me = Hero.MainHero;
			return Clan.PlayerClan.Heroes.Where((Hero h) => h != null && h.IsAlive && !h.IsChild && !h.IsPrisoner && h != me && h != heir
				&& (h.Father == me || h.Mother == me)).ToList();
		}

		private static bool HeirFit(Hero h)
		{
			return h != null && h.IsAlive && !h.IsChild && !h.IsPrisoner && h.Clan == Clan.PlayerClan && Succession.IsBlood(h, Hero.MainHero);
		}

		private static Kingdom Realm()
		{
			Clan c = Clan.PlayerClan;
			return (c != null) ? c.Kingdom : null;
		}

		private static bool Rules()
		{
			Kingdom k = Realm();
			return k != null && k.RulingClan == Clan.PlayerClan;
		}

		private static bool AtWar(out bool invaded)
		{
			invaded = false;
			Kingdom k = Realm();
			if (k == null)
			{
				return false;
			}
			bool war = Kingdom.All.Any((Kingdom o) => o != k && !o.IsEliminated && FactionManager.IsAtWarAgainstFaction(o, k));
			if (war)
			{
				List<Vec2> ours = k.Settlements.Where((Settlement s) => s.IsFortification).Select((Settlement s) => s.GetPosition2D).ToList();
				invaded = MobileParty.AllLordParties.Any((MobileParty x) => x.IsActive && x.MapFaction != null && FactionManager.IsAtWarAgainstFaction(x.MapFaction, k)
					&& ours.Any((Vec2 v) => v.Distance(x.GetPosition2D) < 30f));
			}
			return war;
		}

		private static bool ExileComing()
		{
			return Store.Get("ex:state") == "exiled";
		}

		// ------------------------------------------------------------------
		// smooth or not

		internal static List<Check> Checks(bool child)
		{
			List<Check> list = new List<Check>();
			Hero heir = Heir();
			string realm = (Realm() != null) ? Realm().Name.ToString() : "the realm";
			list.Add(new Check
			{
				Text = (heir != null) ? ("Your heir is " + heir.Name + (HeirFit(heir) ? ", of age and of your blood" : (heir.IsChild ? ", who is not of age" : (heir.IsPrisoner ? ", who is a prisoner" : ", who cannot take it")))) : "You have not named an heir who can take it",
				Ok = HeirFit(heir),
				Blocks = true
			});
			list.Add(new Check
			{
				Text = (Clan.PlayerClan != null && !string.IsNullOrEmpty(Store.Get(DonePrefix + ((MBObjectBase)Clan.PlayerClan).StringId))) ? "This house has already seen one abdication" : "No one has set this crown down before",
				Ok = Clan.PlayerClan != null && string.IsNullOrEmpty(Store.Get(DonePrefix + ((MBObjectBase)Clan.PlayerClan).StringId)),
				Blocks = true
			});
			bool free = !Hero.MainHero.IsPrisoner && MobileParty.MainParty != null && MobileParty.MainParty.MapEvent == null && MobileParty.MainParty.SiegeEvent == null;
			list.Add(new Check
			{
				Text = free ? "You are free and not in the middle of a fight" : "You are a prisoner, or in a battle or a siege",
				Ok = free,
				Blocks = true
			});
			if (child)
			{
				int n = Children(heir).Count;
				list.Add(new Check
				{
					Text = (n > 0) ? (n + " grown child" + ((n == 1) ? "" : "ren") + " of yours, not the heir, could go") : "No grown child of yours, other than the heir, is free to go",
					Ok = n > 0,
					Blocks = true
				});
			}
			if (Cfg.UnlockHeir && Realm() != null)
			{
				bool lawful = Laws.Lawful();
				Hero legal = Laws.Legal();
				list.Add(new Check
				{
					Text = lawful ? "The heir is the one the law names" : ("The law names " + ((legal != null) ? legal.Name.ToString() : "someone else") + ", not your heir"),
					Ok = lawful,
					Cost = "about " + Cfg.AbdicationUnlawfulShare + "% of the realm's houses leave it"
				});
			}
			bool invaded;
			bool war = AtWar(out invaded);
			list.Add(new Check
			{
				Text = !war ? (realm + " is at peace") : (invaded ? (realm + " is at war, and the enemy is inside its borders") : (realm + " is at war, but no enemy is inside its borders")),
				Ok = !war || !invaded,
				Cost = "every house of the realm thinks less of the heir (-" + Cfg.AbdicationWarRelation + "), and the war goes on under them"
			});
			list.Add(new Check
			{
				Text = Bastard.Risen ? "The Bastard's Banner is raised against the crown" : "No bastard's banner is raised",
				Ok = !Bastard.Risen,
				Cost = "another house goes over to the bastard"
			});
			list.Add(new Check
			{
				Text = ExileComing() ? "An exiled claimant's company waits across the sea" : "No exile waits across the sea",
				Ok = !ExileComing(),
				Cost = "the company sails two years sooner"
			});
			return list;
		}

		internal static string Read(List<Check> checks)
		{
			return string.Join("\n", checks.Select((Check c) => (c.Ok ? "[OK] " : "[X]  ") + c.Text + ((!c.Ok && c.Cost != null) ? ("  - cost: " + c.Cost) : ((!c.Ok && c.Blocks) ? "  - cannot be done" : "")) + "."));
		}

		internal static string Why()
		{
			if (!Cfg.Abdication)
			{
				return "Abdication is turned off in the config.";
			}
			Check bad = Checks(false).FirstOrDefault((Check c) => c.Blocks && !c.Ok);
			return (bad != null) ? (bad.Text + ".") : null;
		}

		internal static string Odds()
		{
			string s = "Set down the crown (I go myself):\n" + Read(Checks(false)) + "\n\nSend a child instead:\n" + Read(Checks(true).Where((Check c) => c.Blocks).ToList());
			Log.Write("abdication odds:\n" + s);
			return s;
		}

		// ------------------------------------------------------------------
		// the menu path

		internal static void Begin()
		{
			string why = Why();
			if (why != null)
			{
				Flow.Notify(why);
				return;
			}
			Hero heir = Heir();
			List<Check> checks = Checks(false);
			bool smooth = checks.All((Check c) => c.Ok);
			Inquiry.Confirm("Set Down the Crown", heir.Name + " takes the crown and everything that goes with it: your gold, your lands, your men, your companions, your white cloaks, your debts and your dragon. You leave with one horse, one blade, plain clothes and " +
				Cfg.AbdicationCoin.ToString("N0") + " in coin, to found a house of your own.\n\n" + Read(checks) + "\n\n" + (smooth ? "The realm is ready. It will pass cleanly." : "The realm is not ready. Every cross above is paid the moment you set the crown down."),
				"Go on", "Not today", delegate
				{
					List<InquiryElement> els = new List<InquiryElement>();
					els.Add(new InquiryElement("self", "I will go myself", null, true, "You play on as yourself, at the head of a new house with nothing."));
					List<Hero> kids = Children(heir);
					els.Add(new InquiryElement("child", "Let one of my children go", null, kids.Count > 0, (kids.Count > 0) ? "You stay on the throne in name only - " + heir.Name + " rules - and you play on as a younger child, founding a cadet house." : "No grown child of yours, other than the heir, is free to go."));
					Inquiry.Select("Who Leaves?", "Who goes out into the world with nothing?", els, 1, 1, "So be it", "Not today", delegate(List<InquiryElement> chosen)
					{
						string o = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
						if (o == "self")
						{
							Name(Hero.MainHero, null);
						}
						else if (o == "child")
						{
							List<InquiryElement> who = kids.Select((Hero k) => new InquiryElement(k, k.Name.ToString(), null, true, k.Age.ToString("0") + " years old" + ((k.Spouse != null) ? (", married to " + k.Spouse.Name + ", who goes too") : ""))).ToList();
							Inquiry.Select("Which Child?", "Who founds the new house?", who, 1, 1, "Them", "Not today", delegate(List<InquiryElement> c2)
							{
								Hero k = (c2 != null && c2.Count > 0) ? (c2[0].Identifier as Hero) : null;
								if (k != null)
								{
									Name(k, k);
								}
							});
						}
					});
				}, null);
		}

		private static void Name(Hero founder, Hero child)
		{
			string suggestion = Knighting.HouseName(founder.Culture);
			Inquiry.Text("The Herald", "The herald suggests " + suggestion + ". What will the new house be called?", suggestion, "So it is written", "Not today", delegate(string text)
			{
				string name = string.IsNullOrWhiteSpace(text) ? suggestion : text.Trim();
				if (!name.StartsWith("House ", StringComparison.OrdinalIgnoreCase))
				{
					name = "House " + name;
				}
				Do(child == null, child, name);
			}, null);
		}

		// Cheat: wad.abdicate self|child
		internal static string Cheat(string mode)
		{
			bool child = mode == "child";
			Hero heir = Heir();
			Check bad = Checks(child).FirstOrDefault((Check c) => c.Blocks && !c.Ok);
			if (bad != null)
			{
				return "Cannot: " + bad.Text + ".";
			}
			Hero kid = child ? Children(heir).OrderBy((Hero h) => h.Age).FirstOrDefault() : null;
			Hero founder = kid ?? Hero.MainHero;
			string name = Knighting.HouseName(founder.Culture);
			if (!name.StartsWith("House ", StringComparison.OrdinalIgnoreCase))
			{
				name = "House " + name;
			}
			Do(!child, kid, name);
			return "Done - see the log. You are " + Hero.MainHero.Name + " of " + Clan.PlayerClan.Name + ".";
		}

		// ------------------------------------------------------------------
		// doing it

		private static void Do(bool self, Hero child, string name)
		{
			Clan old = Clan.PlayerClan;
			Kingdom realm = old.Kingdom;
			Hero me = Hero.MainHero;
			Hero heir = Heir();
			if (!HeirFit(heir) || (!self && (child == null || !child.IsAlive || child.IsPrisoner)))
			{
				Flow.Notify("It cannot be done after all - the log says why.");
				Log.Write("abdication: refused at the last moment (heir " + ((heir != null) ? heir.Name.ToString() : "none") + ", child " + ((child != null) ? child.Name.ToString() : "none") + ")");
				return;
			}
			Hero founder = self ? me : child;
			Log.Write("abdication: " + founder.Name + " leaves " + old.Name + ", " + heir.Name + " takes the crown" + ((realm != null) ? (" of " + realm.Name) : "") + " (" + (self ? "the ruler goes" : "a child goes") + ")");
			try
			{
				// 1. What a realm that is not ready pays.
				Costs(old, realm, heir);

				// 2. The new house.
				Clan house = Found(founder, old, name);
				if (house == null)
				{
					Flow.Notify("The new house could not be made - the log says why.");
					return;
				}

				// 3. The heir must lead a party of their own, or the leader
				// change makes them leader of the one they ride in.
				MobileParty heirParty = OwnParty(heir, old);
				string pdfBefore = (Clan.PlayerClan != null) ? ((MBObjectBase)Clan.PlayerClan).StringId : "?";

				if (self)
				{
					// 4. The player's house first, so the old house is an
					// ordinary house when its leader changes. The new house
					// is given you as its leader by the field alone - the
					// public SetLeader also moves you into it, which would
					// fire the game's leader hook on the house you still lead
					// - so nothing that reads the player's house in between
					// finds it leaderless.
					try
					{
						HarmonyLib.AccessTools.Field(typeof(Clan), "_leader").SetValue(house, me);
					}
					catch (Exception le)
					{
						Log.Write("abdication: the new house's leader field could not be set early: " + le.Message);
					}
					if (!Bastard.SetPlayerFaction(house))
					{
						Flow.Notify("This build cannot change the player's house.");
						return;
					}
					Log.Write("abdication: PlayerDefaultFaction " + pdfBefore + " -> " + ((MBObjectBase)Clan.PlayerClan).StringId);
					// 5. The heir leads the old house (and its purse).
					ChangeClanLeaderAction.ApplyWithSelectedNewLeader(old, heir);
					Log.Write("abdication: " + old.Name + " is led by " + ((old.Leader != null) ? old.Leader.Name.ToString() : "?") + ((realm != null) ? ("; " + realm.Name + " is ruled by " + ((realm.Leader != null) ? realm.Leader.Name.ToString() : "?")) : ""));
					// 6. Then you change house: the game's leader hook is
					// quiet now, because you no longer lead the one you leave.
					me.Clan = house;
					house.SetLeader(me);
					// 7. The party does not follow its owner on its own.
					MobileParty main = MobileParty.MainParty;
					string before = (main.ActualClan != null) ? ((MBObjectBase)main.ActualClan).StringId : "none";
					main.ActualClan = house;
					Log.Write("abdication: MainParty.ActualClan " + before + " -> " + ((main.ActualClan != null) ? ((MBObjectBase)main.ActualClan).StringId : "none") +
						", owner " + ((main.Owner != null) ? main.Owner.Name.ToString() : "none") + ", map faction " + ((main.MapFaction != null) ? main.MapFaction.Name.ToString() : "none"));
					// 8. Everything in it stays with the crown.
					heirParty = heir.PartyBelongedTo ?? heirParty;
					Strip(main, heirParty, me);
					Bastard.RedrawMainParty();
					try
					{
						Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
					}
					catch
					{
					}
				}
				else
				{
					// 4. The child leaves whatever party they are in.
					Loose(child, heirParty ?? MobileParty.MainParty);
					child.Clan = house;
					house.SetLeader(child);
					Hero spouse = child.Spouse;
					if (spouse != null && spouse.IsAlive && spouse != heir && spouse != me && spouse.Clan == old)
					{
						spouse.Clan = house;
						Log.Write("abdication: " + spouse.Name + " goes with " + child.Name);
					}
					// 5. Become the child: the Bastard's switch.
					if (!Bastard.SwitchPlayer(child, house, "abdication"))
					{
						Flow.Notify("You could not become " + child.Name + " - the log says why.");
						return;
					}
					Log.Write("abdication: PlayerDefaultFaction " + pdfBefore + " -> " + ((MBObjectBase)Clan.PlayerClan).StringId + ", you are " + Hero.MainHero.Name);
					// 6. The heir leads the old house, now an ordinary one.
					ChangeClanLeaderAction.ApplyWithSelectedNewLeader(old, heir);
					Log.Write("abdication: " + old.Name + " is led by " + ((old.Leader != null) ? old.Leader.Name.ToString() : "?") + ((realm != null) ? ("; " + realm.Name + " is ruled by " + ((realm.Leader != null) ? realm.Leader.Name.ToString() : "?")) : ""));
					MobileParty main = MobileParty.MainParty;
					if (main != null && main.ActualClan != house)
					{
						string before = (main.ActualClan != null) ? ((MBObjectBase)main.ActualClan).StringId : "none";
						main.ActualClan = house;
						Log.Write("abdication: MainParty.ActualClan " + before + " -> " + ((MBObjectBase)house).StringId);
					}
					if (main != null)
					{
						Log.Write("abdication: MainParty owner " + ((main.Owner != null) ? main.Owner.Name.ToString() : "none") + ", " + main.MemberRoster.TotalManCount + " in it, map faction " + ((main.MapFaction != null) ? main.MapFaction.Name.ToString() : "none"));
					}
				}

				Hero now = Hero.MainHero;
				// The purse: the crown kept the gold; you keep enough to eat.
				int coin = Cfg.AbdicationCoin;
				if (now.Gold != coin)
				{
					if (now.Gold > coin && heir.IsAlive)
					{
						GiveGoldAction.ApplyBetweenCharacters(now, heir, now.Gold - coin, true);
					}
					else
					{
						now.ChangeHeroGold(coin - now.Gold);
					}
				}
				Log.Write("abdication: " + now.Name + " keeps " + now.Gold + " gold; " + heir.Name + " has " + heir.Gold);
				Gear(now, heir.PartyBelongedTo);
				Dragon(now);

				// The house you left: warm, but not yours.
				Relations(now, old, heir);
				Store.Set(DonePrefix + ((MBObjectBase)old).StringId, "1");
				Store.Set(WasPrefix + ((MBObjectBase)old).StringId, "1");
				Store.Set(LastKey, ((MBObjectBase)old).StringId + "|" + ((MBObjectBase)house).StringId + "|" + ((MBObjectBase)now).StringId + "|" + CourtBehavior.Today());
				Guard.Abandon(old);
				Crown(old, realm, heir);
				Store.Set("sc:heir", null);
				Succession.Forget();
				Titles.Invalidate();

				// The chronicle.
				string date = Standing.Date();
				string story = "On " + date + " " + (self ? (now.Name + ", who had worn the crown of " + ((realm != null) ? realm.Name.ToString() : old.Name.ToString()) + ", set it down") : (now.Name + ", a younger child of " + old.Name + ", went out from it")) +
					" with a horse, a blade and " + coin.ToString("N0") + " in coin, and founded " + house.Name + ".";
				Knighting.Append(house, story);
				Knighting.Append(old, "On " + date + " " + (self ? (me.Name + " set down the crown") : (me.Name + " stepped aside")) + ", and " + heir.Name + " took it. " + now.Name + " left to found " + house.Name + ".");
				Store.AddDeed(date + "  " + (self ? "You set down the crown" : (now.Name + " went out from " + old.Name)) + ". " + heir.Name + " took it, and " + house.Name + " was founded.");
				Log.Write("abdication: done - you are " + now.Name + " of " + house.Name + " (" + ((MBObjectBase)house).StringId + "), kingdom " + ((house.Kingdom != null) ? house.Kingdom.Name.ToString() : "none") + "; " + old.Name + " under " + heir.Name);
				Ravens.Popup("The Crown Set Down", heir.Name + " is crowned" + ((realm != null) ? (" in " + realm.Name) : "") + ", and ravens go out to every house of the realm.\n\n" +
					now.Name + " rides out with one horse, one blade and " + coin.ToString("N0") + " in coin, as the first of " + house.Name + ". " + old.Name + " will always think well of you. It will never follow you.");
			}
			catch (Exception e)
			{
				Log.Write("abdication failed: " + ((e.InnerException != null) ? e.InnerException.ToString() : e.ToString()));
				Flow.Notify("The abdication went wrong - the log says why.");
			}
		}

		// ------------------------------------------------------------------
		// the steps

		private static void Costs(Clan old, Kingdom realm, Hero heir)
		{
			if (realm == null || realm.RulingClan != old)
			{
				// Not a crown, only a house: nobody else's realm pays for it.
				return;
			}
			try
			{
				if (Cfg.UnlockHeir && !Laws.Lawful())
				{
					List<Clan> houses = realm.Clans.Where((Clan c) => c != old && !c.IsEliminated && !c.IsUnderMercenaryService && c.Leader != null).ToList();
					int n = (int)Math.Round((double)houses.Count * Cfg.AbdicationUnlawfulShare / 100.0);
					foreach (Clan c in houses.OrderBy((Clan c) => c.Leader.GetRelation(heir)).Take(n).ToList())
					{
						// The price of an unlawful heir, not a rebellion: the King's
						// Justice must not count it as treason.
						Store.Set("lw:exiled:" + ((MBObjectBase)c).StringId, "1");
						ChangeKingdomAction.ApplyByLeaveKingdom(c, true);
						Log.Write("abdication cost: " + c.Name + " will not kneel to an unlawful heir and leaves " + realm.Name);
					}
				}
				bool invaded;
				if (AtWar(out invaded) && invaded)
				{
					foreach (Clan c in realm.Clans.Where((Clan c) => c != old && c.Leader != null && c.Leader.IsAlive).ToList())
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(c.Leader, heir, -Cfg.AbdicationWarRelation, false);
					}
					Log.Write("abdication cost: the realm's houses think less of " + heir.Name + " for a crown passed in wartime (-" + Cfg.AbdicationWarRelation + ")");
				}
				if (Bastard.Risen)
				{
					Clan bh = Bastard.House;
					Kingdom theirs = (bh != null) ? bh.Kingdom : null;
					Clan turn = (theirs != null) ? realm.Clans.Where((Clan c) => c != old && !c.IsEliminated && !c.IsUnderMercenaryService && c.Leader != null).OrderBy((Clan c) => c.Leader.GetRelation(heir)).FirstOrDefault() : null;
					if (turn != null)
					{
						Store.Set("lw:exiled:" + ((MBObjectBase)turn).StringId, "1");
						ChangeKingdomAction.ApplyByJoinToKingdomByDefection(turn, realm, theirs, default(CampaignTime), true);
						Log.Write("abdication cost: " + turn.Name + " goes over to the bastard's " + theirs.Name);
					}
				}
				if (ExileComing())
				{
					int ret = Store.GetI("ex:return", 0);
					if (ret > 0)
					{
						int sooner = Math.Max(CourtBehavior.Today() + 7, ret - 2 * Cfg.DaysPerYear);
						Store.SetI("ex:return", sooner);
						Log.Write("abdication cost: the exiles across the sea hear of it and sail sooner: day " + ret + " -> " + sooner);
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("abdication: a cost could not be paid: " + e.Message);
			}
		}

		private static Clan Found(Hero founder, Clan old, string name)
		{
			try
			{
				Clan house = Clan.CreateClan("wad_abd_" + CourtBehavior.Today() + "_" + MBRandom.RandomInt(1000, 9999));
				if (house == null)
				{
					Log.Write("abdication: Clan.CreateClan returned nothing");
					return null;
				}
				Bastard.Set(house, "Name", new TextObject("{=!}" + name, (Dictionary<string, object>)null));
				Bastard.Set(house, "InformalName", new TextObject("{=!}" + name.Replace("House ", ""), (Dictionary<string, object>)null));
				house.Culture = founder.Culture ?? old.Culture;
				house.Banner = Cadet(old) ?? Banner.CreateRandomClanBanner(-1);
				try
				{
					uint ground = house.Banner.GetPrimaryColor();
					uint charge = house.Banner.GetFirstIconColor();
					house.Color = ground;
					house.Color2 = (ground == charge || Bastard.Bad(charge)) ? ground : charge;
					if (house.Color2 != ground)
					{
						Bastard.Set(house, "BannerBackgroundColorPrimary", ground);
						Bastard.Set(house, "BannerBackgroundColorSecondary", ground);
						Bastard.Set(house, "BannerIconColor", charge);
					}
				}
				catch (Exception be)
				{
					Log.Write("abdication: the new arms' colours would not take: " + be.Message);
				}
				Bastard.Set(house, "Tier", 1);
				try
				{
					Settlement home = old.HomeSettlement ?? founder.HomeSettlement;
					if (home != null)
					{
						house.SetInitialHomeSettlement(home);
					}
				}
				catch
				{
				}
				Bastard.Call(house, "UpdateBannerColorsAccordingToKingdom");
				Log.Write("abdication: founded " + house.Name + " (" + ((MBObjectBase)house).StringId + "), culture " + ((house.Culture != null) ? house.Culture.Name.ToString() : "?"));
				return house;
			}
			catch (Exception e)
			{
				Log.Write("abdication: the new house could not be founded: " + e.Message);
				return null;
			}
		}

		// The old arms with a cadet's difference: the device takes a new
		// colour. Never the reversed field and charge - that means bastardy.
		internal static Banner Cadet(Clan old)
		{
			try
			{
				if (old == null || old.Banner == null || old.Banner.GetBannerDataListCount() < 2)
				{
					return null;
				}
				Banner b = new Banner(old.Banner);
				uint ground = b.GetPrimaryColor();
				uint charge = b.GetFirstIconColor();
				List<uint> palette = BannerManager.Instance.ReadOnlyColorPalette.Values.Select((BannerColor c) => c.Color).Where((uint c) => c != ground && c != charge && !Bastard.Bad(c)).Distinct().ToList();
				if (palette.Count == 0)
				{
					return null;
				}
				// The one that stands out most from the field.
				uint pick = palette.OrderByDescending((uint c) => Contrast(c, ground) * 2f + Contrast(c, charge)).First();
				b.ChangeIconColors(pick);
				Log.Write("abdication: cadet arms - field " + Bastard.Hex(ground) + ", device " + Bastard.Hex(charge) + " -> " + Bastard.Hex(pick));
				return b;
			}
			catch (Exception e)
			{
				Log.Write("abdication: the cadet arms would not draw: " + e.Message);
				return null;
			}
		}

		private static float Contrast(uint a, uint b)
		{
			int dr = (int)((a >> 16) & 0xFF) - (int)((b >> 16) & 0xFF);
			int dg = (int)((a >> 8) & 0xFF) - (int)((b >> 8) & 0xFF);
			int db = (int)(a & 0xFF) - (int)(b & 0xFF);
			return (float)Math.Sqrt(dr * dr + dg * dg + db * db);
		}

		private static MobileParty OwnParty(Hero heir, Clan old)
		{
			try
			{
				MobileParty p = heir.PartyBelongedTo;
				if (p != null && p.LeaderHero == heir && p != MobileParty.MainParty)
				{
					return p;
				}
				if (p != null && p != MobileParty.MainParty && p.LeaderHero != heir)
				{
					// Riding in someone else's party: they will take it over
					// when they become leader, which is the game's own way.
					return p;
				}
				MobileParty made = MobilePartyHelper.CreateNewClanMobileParty(heir, old);
				Log.Write("abdication: " + heir.Name + " given a party of their own (" + ((made != null) ? ((MBObjectBase)made).StringId : "failed") + ")");
				return made;
			}
			catch (Exception e)
			{
				Log.Write("abdication: the heir could not be given a party: " + e.Message);
				return null;
			}
		}

		// Everything in your party goes to the heir: men, prisoners,
		// companions, baggage.
		private static void Strip(MobileParty main, MobileParty to, Hero keep)
		{
			int men = 0;
			int prisoners = 0;
			int companions = 0;
			int items = 0;
			try
			{
				foreach (TroopRosterElement e in main.MemberRoster.GetTroopRoster().ToList())
				{
					if (e.Character == null)
					{
						continue;
					}
					if (e.Character.IsHero)
					{
						Hero h = e.Character.HeroObject;
						if (h != null && h != keep)
						{
							if (to != null)
							{
								AddHeroToPartyAction.Apply(h, to, false);
							}
							else
							{
								main.MemberRoster.AddToCounts(e.Character, -1, false, 0, 0, true, -1);
							}
							companions++;
						}
						continue;
					}
					main.MemberRoster.AddToCounts(e.Character, -e.Number, false, -e.WoundedNumber, 0, true, -1);
					if (to != null)
					{
						to.MemberRoster.AddToCounts(e.Character, e.Number, false, e.WoundedNumber, 0, true, -1);
					}
					men += e.Number;
				}
				foreach (TroopRosterElement e in main.PrisonRoster.GetTroopRoster().ToList())
				{
					if (e.Character == null)
					{
						continue;
					}
					if (e.Character.IsHero)
					{
						if (to != null && e.Character.HeroObject != null)
						{
							TransferPrisonerAction.Apply(e.Character, main.Party, to.Party);
						}
						prisoners++;
						continue;
					}
					main.PrisonRoster.AddToCounts(e.Character, -e.Number, false, 0, 0, true, -1);
					if (to != null)
					{
						to.PrisonRoster.AddToCounts(e.Character, e.Number, false, 0, 0, true, -1);
					}
					prisoners += e.Number;
				}
				for (int i = main.ItemRoster.Count - 1; i >= 0; i--)
				{
					ItemRosterElement el = main.ItemRoster.GetElementCopyAtIndex(i);
					if (el.EquipmentElement.Item == null || el.Amount <= 0)
					{
						continue;
					}
					// A day's bread stays with you.
					if (el.EquipmentElement.Item.IsFood && items == 0)
					{
						int keepFood = Math.Min(el.Amount, 3);
						main.ItemRoster.AddToCounts(el.EquipmentElement, -(el.Amount - keepFood));
						if (to != null)
						{
							to.ItemRoster.AddToCounts(el.EquipmentElement, el.Amount - keepFood);
						}
						items += el.Amount - keepFood;
						continue;
					}
					main.ItemRoster.AddToCounts(el.EquipmentElement, -el.Amount);
					if (to != null)
					{
						to.ItemRoster.AddToCounts(el.EquipmentElement, el.Amount);
					}
					items += el.Amount;
				}
			}
			catch (Exception e)
			{
				Log.Write("abdication: stripping the party failed part-way: " + e.Message);
			}
			Log.Write("abdication: to the crown - " + men + " men, " + prisoners + " prisoners, " + companions + " companions, " + items + " items" + ((to != null) ? (" into " + to.Name) : " (no heir's party: dismissed)"));
		}

		// A child leaving: out of any party, into a hall, so the game builds
		// them a fresh, empty party of their own.
		private static void Loose(Hero child, MobileParty crown)
		{
			try
			{
				MobileParty p = child.PartyBelongedTo;
				if (p != null && p.LeaderHero == child && p != MobileParty.MainParty)
				{
					// They led men of their own: the men stay with the crown,
					// the party (now just them) becomes theirs.
					Strip(p, crown, child);
					return;
				}
				Settlement hall = child.CurrentSettlement;
				if (p != null)
				{
					p.MemberRoster.AddToCounts(child.CharacterObject, -1, false, 0, 0, true, -1);
				}
				if (hall == null)
				{
					hall = SettlementHelper.FindNearestTownToMobileParty(MobileParty.MainParty, MobileParty.NavigationType.Default, (Settlement s) => s.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(s.MapFaction, Clan.PlayerClan.MapFaction))?.Settlement;
				}
				if (hall != null && child.CurrentSettlement != hall)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(child, hall);
				}
				Log.Write("abdication: " + child.Name + " leaves " + ((p != null) ? p.Name.ToString() : "no party") + " and waits at " + ((hall != null) ? hall.Name.ToString() : "?"));
			}
			catch (Exception e)
			{
				Log.Write("abdication: the child could not be freed from their party: " + e.Message);
			}
		}

		// One blade, one horse, plain clothes.
		private static void Gear(Hero h, MobileParty crown)
		{
			try
			{
				Equipment eq = h.BattleEquipment;
				CharacterObject plain = (h.Culture != null) ? h.Culture.BasicTroop : null;
				Equipment basic = (plain != null) ? plain.Equipment : null;
				bool kept = false;
				int gone = 0;
				for (int i = 0; i < 12; i++)
				{
					EquipmentIndex slot = (EquipmentIndex)i;
					EquipmentElement el = eq[slot];
					if (slot == EquipmentIndex.Horse || slot == EquipmentIndex.HorseHarness)
					{
						// The horse stays unless it is a dragon.
						if (slot == EquipmentIndex.Horse && el.Item != null && ((MBObjectBase)el.Item).StringId != null && ((MBObjectBase)el.Item).StringId.StartsWith("dragon_"))
						{
							eq[slot] = (basic != null && basic[slot].Item != null && !((MBObjectBase)basic[slot].Item).StringId.StartsWith("dragon_")) ? basic[slot] : EquipmentElement.Invalid;
							gone++;
						}
						continue;
					}
					if (i <= 3)
					{
						if (el.Item != null && !kept)
						{
							kept = true;
							continue;
						}
						if (el.Item != null)
						{
							if (crown != null)
							{
								crown.ItemRoster.AddToCounts(el, 1);
							}
							eq[slot] = EquipmentElement.Invalid;
							gone++;
						}
						continue;
					}
					// Armour: plain clothes in its place.
					if (el.Item != null && crown != null)
					{
						crown.ItemRoster.AddToCounts(el, 1);
					}
					eq[slot] = (basic != null) ? basic[slot] : EquipmentElement.Invalid;
					if (el.Item != null)
					{
						gone++;
					}
				}
				Log.Write("abdication: " + h.Name + " keeps one blade and a horse; " + gone + " piece(s) of gear stay with the crown");
			}
			catch (Exception e)
			{
				Log.Write("abdication: the gear could not be changed: " + e.Message);
			}
		}

		// The dragon stays with the crown: it goes back to the Dragonmont.
		private static void Dragon(Hero h)
		{
			try
			{
				string id = ((MBObjectBase)h).StringId;
				foreach (DragonRec d in Dragons.All().Where((DragonRec x) => x.Alive && x.Rider == id).ToList())
				{
					d.Status = "riderless";
					d.Rider = "";
					Dragons.Save(d);
					Log.Write("abdication: " + d.Name + " stays with the crown and goes back to the Dragonmont");
				}
			}
			catch (Exception e)
			{
				Log.Write("abdication: the dragon could not be let go: " + e.Message);
			}
		}

		private static void Relations(Hero now, Clan old, Hero heir)
		{
			try
			{
				int target = Cfg.AbdicationOldHouseRelation;
				int have = (int)now.GetRelation(heir);
				if (have != target)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(now, heir, target - have, false);
				}
				foreach (Hero h in old.Heroes.Where((Hero x) => x != null && x.IsAlive && x != heir && x != now).ToList())
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(now, h, 30, false);
				}
				Log.Write("abdication: relation with " + heir.Name + " now " + (int)now.GetRelation(heir) + "; +30 with the rest of " + old.Name);
			}
			catch (Exception e)
			{
				Log.Write("abdication: relations could not be set: " + e.Message);
			}
		}

		// What the crown keeps: hosts, debts.
		private static void Crown(Clan old, Kingdom realm, Hero heir)
		{
			string oldId = ((MBObjectBase)old).StringId;
			try
			{
				int hosts = 0;
				foreach (Host.Rec r in Host.All())
				{
					if (r.Owner == "" || r.Owner == oldId)
					{
						r.Owner = oldId;
						r.Order = "free";
						r.Target = "";
						Host.Save(r);
						hosts++;
					}
				}
				if (hosts > 0)
				{
					Log.Write("abdication: " + hosts + " host(s) stay with the crown, free to campaign under " + heir.Name);
				}
			}
			catch (Exception e)
			{
				Log.Write("abdication: the hosts could not be handed over: " + e.Message);
			}
			IronBank.HandToCrown(realm, heir);
		}

		// Daily: the house you left never comes into yours.
		internal static void Daily()
		{
			try
			{
				if (!Cfg.Abdication || !Store.Initialized || Clan.PlayerClan == null)
				{
					return;
				}
				Kingdom mine = Clan.PlayerClan.Kingdom;
				if (mine == null || mine.RulingClan != Clan.PlayerClan)
				{
					return;
				}
				foreach (Clan c in mine.Clans.Where(IsOldHouse).ToList())
				{
					Store.Set("lw:exiled:" + ((MBObjectBase)c).StringId, "1");
					ChangeKingdomAction.ApplyByLeaveKingdom(c, false);
					Log.Write("abdication: " + c.Name + " will not follow the house that left it, and leaves " + mine.Name);
				}
			}
			catch (Exception e)
			{
				Log.Once("abdicationdaily", "abdication daily failed: " + e.Message);
			}
		}
	}
}
