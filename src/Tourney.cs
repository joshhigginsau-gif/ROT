using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The lists.
	//
	// Bannerlord already has tournaments: an arena, a prize, some renown, and
	// nothing that happens afterwards. In Westeros a tourney is politics with
	// lances. Lords are brought together, debts are paid in public, young men
	// die in the lists, and the man who wins chooses whose lap the wreath lands
	// in - which is how Rhaegar Targaryen started a war.
	//
	// So this does not replace the game's tournament. It calls one, fills it,
	// and then reads what happened in it:
	//
	//   - You host from your court, in a town you hold. You choose the purse
	//     and who of your blood rides. Guest houses send their young lords,
	//     and they are actually there in the town to be fought.
	//   - Ride in it yourself and it is the game's own arena. Stay away and it
	//     is decided without you when its time runs out.
	//   - Win, and you crown a Queen of Love and Beauty. Choose badly and the
	//     wrong house remembers - or there is a night after the feast.
	//   - At a tourney you host or ride in, somebody can be killed or maimed.
	//     At yours, their house blames the host.
	//   - A baseborn child of yours who wins is cheered by people who will
	//     follow them later. The Bastard's Banner reads that.
	internal static class Tourney
	{
		private const string TownKey = "tn:town";
		private const string TierKey = "tn:tier";
		private const string DayKey = "tn:day";
		private const string GuestsKey = "tn:guests";
		private const string LastKey = "tn:last";
		private const string RodeKey = "tn:rode";
		private const string HistoryKey = "tn:history";
		private const string PendingPrefix = "tn:pend:";
		private const string WinsPrefix = "tn:wins:";

		// ------------------------------------------------------------------
		// the record

		internal static Town Hosted
		{
			get
			{
				string id = Store.Get(TownKey);
				Settlement s = FindSettlement(id);
				return (s != null) ? s.Town : null;
			}
		}

		internal static int Tier
		{
			get
			{
				return Math.Max(1, Math.Min(3, Store.GetI(TierKey, 1)));
			}
		}

		internal static int Wins(Hero h)
		{
			return (h == null) ? 0 : Store.GetI(WinsPrefix + ((MBObjectBase)h).StringId, 0);
		}

		internal static void AddWin(Hero h, int n = 1)
		{
			if (h != null)
			{
				Store.SetI(WinsPrefix + ((MBObjectBase)h).StringId, Math.Max(0, Wins(h) + n));
			}
		}

		// Enough wins that the crowds would make them a king.
		internal static bool Crowned(Hero h)
		{
			return Cfg.Tourneys && Cfg.TourneyBastardCrowning > 0 && Wins(h) >= Cfg.TourneyBastardCrowning;
		}

		// How much more of your realm follows a champion when the banner rises.
		internal static float ShareBonus(Hero h)
		{
			if (!Cfg.Tourneys)
			{
				return 0f;
			}
			return Math.Min(3, Wins(h)) * Cfg.TourneyBastardShare / 100f;
		}

		internal static int Purse(int tier)
		{
			switch (tier)
			{
			case 3:
				return Cfg.TourneyPurseLavish;
			case 2:
				return Cfg.TourneyPurseGreat;
			default:
				return Cfg.TourneyPurseModest;
			}
		}

		internal static int Cost(int tier)
		{
			int purse = Purse(tier);
			return purse + purse * Cfg.TourneyFeastPercent / 100;
		}

		private static string TierName(int tier)
		{
			return (tier == 3) ? "lavish" : ((tier == 2) ? "great" : "modest");
		}

		// ------------------------------------------------------------------
		// calling one

		internal static bool CanHost(out string why)
		{
			why = null;
			try
			{
				if (!Cfg.Tourneys)
				{
					why = "turned off in the config";
					return false;
				}
				Settlement here = Settlement.CurrentSettlement;
				if (here == null || !here.IsTown || here.Town == null)
				{
					why = "a tourney needs a town, with lists and stands and somewhere to feast";
					return false;
				}
				if (here.OwnerClan != Clan.PlayerClan)
				{
					why = "you can only call one in a town your house holds";
					return false;
				}
				if (Hosted != null)
				{
					why = "your tourney at " + Hosted.Name + " has not been ridden yet";
					return false;
				}
				int last = Store.GetI(LastKey, -9999);
				int wait = Cfg.TourneyCooldown - (CourtBehavior.Today() - last);
				if (last > -9000 && wait > 0)
				{
					why = "the realm has barely recovered from the last one. " + wait + " more days";
					return false;
				}
				if (Hero.MainHero == null || Hero.MainHero.Gold < Cost(1))
				{
					why = "even a modest tourney costs " + Cost(1).ToString("N0");
					return false;
				}
				return true;
			}
			catch (Exception e)
			{
				why = "this cannot be read just now";
				Log.Once("tncan", "the tourney check failed: " + e.Message);
				return false;
			}
		}

		// The court's path: choose a purse, choose who rides, and it begins.
		internal static void Call()
		{
			try
			{
				string why;
				if (!CanHost(out why))
				{
					Flow.Notify("No tourney: " + why + ".");
					return;
				}
				Town town = Settlement.CurrentSettlement.Town;
				List<InquiryElement> els = new List<InquiryElement>();
				for (int tier = 1; tier <= 3; tier++)
				{
					int cost = Cost(tier);
					bool can = Hero.MainHero.Gold >= cost;
					els.Add(new InquiryElement(tier,
						char.ToUpper(TierName(tier)[0]) + TierName(tier).Substring(1) + " - " + Purse(tier).ToString("N0") + " purse, " + cost.ToString("N0") + " in all",
						null, can,
						can
							? ("Each guest house thinks " + (Cfg.TourneyHostRelation * tier) + " better of you, your renown rises by " +
								(Cfg.TourneyHostRenown * tier) + ", and a generous host gains " + tier + " Honour. The purse goes to whoever wins it.")
							: "Your treasury will not stretch that far."));
				}
				Inquiry.Select("Call a Tourney",
					"Heralds to every hall in the realm, lists raised outside " + town.Name + ", a feast for everyone who comes.\n\n" +
					"The tourney is the game's own: ride in it yourself, or leave it to be decided without you. Either way, what happens in the lists will be remembered - and the lists are not safe.",
					els, 1, 1, "Proclaim it", "Not this year",
					delegate(List<InquiryElement> chosen)
					{
						int tier = (chosen != null && chosen.Count > 0 && chosen[0].Identifier is int) ? (int)chosen[0].Identifier : 0;
						if (tier > 0)
						{
							ChooseRiders(town, tier);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("calling a tourney failed: " + e.Message);
			}
		}

		// Who rides for your house.
		private static void ChooseRiders(Town town, int tier)
		{
			try
			{
				List<Hero> can = HouseRiders();
				if (can.Count == 0)
				{
					Begin(town, tier, new List<Hero>(), false);
					Refresh();
					return;
				}
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Hero h in can)
				{
					bool baseborn = Blade.IsBaseborn(h);
					els.Add(new InquiryElement(h, h.Name + "  (" + (int)h.Age + ")" + (baseborn ? "   - not of your house" : ""), null, true,
						baseborn
							? ("They ride under no banner of yours. If they win, the smallfolk will cheer a face that looks like yours - and remember it when you are gone. " +
								"Every win takes " + Cfg.TourneyBastardShare + "% more of your realm with them if they ever rise.")
							: ("A win is renown for your house" + ((h == Succession.Named()) ? ", and the realm sees what your heir can do." : ".") +
								(Cfg.TourneyKinCanDie ? " The lists are not safe." : ""))));
				}
				Inquiry.Select("Who Rides for Your House",
					"They will be brought to " + town.Name + " to ride. You can ride yourself as well, by going there.\n\n" +
					"Choose none and the house is represented by you alone, or not at all.",
					els, 0, els.Count, "They ride", "None of them",
					delegate(List<InquiryElement> chosen)
					{
						List<Hero> riders = (chosen ?? new List<InquiryElement>())
							.Select((InquiryElement e) => e.Identifier as Hero).Where((Hero h) => h != null).ToList();
						Begin(town, tier, riders, false);
						Refresh();
					},
					delegate
					{
						Begin(town, tier, new List<Hero>(), false);
						Refresh();
					});
			}
			catch (Exception e)
			{
				Log.Write("choosing the riders failed: " + e.Message);
			}
		}

		// Your blood who are free to go: nobody leading a party, governing a
		// town or sitting in a cell. Baseborn children who have come to the
		// gate count, which is the point.
		internal static List<Hero> HouseRiders()
		{
			List<Hero> list = new List<Hero>();
			try
			{
				foreach (Hero h in Succession.Claimants())
				{
					if (Free(h) && !list.Contains(h))
					{
						list.Add(h);
					}
				}
				foreach (Kid k in Baseborn.Known())
				{
					Hero h = Baseborn.HeroOf(k);
					if (Free(h) && h != Hero.MainHero && !list.Contains(h))
					{
						list.Add(h);
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static bool Free(Hero h)
		{
			try
			{
				return h != null && h.IsAlive && h.IsActive && !h.IsChild && !h.IsPrisoner
					&& h.PartyBelongedTo == null && h.GovernorOf == null && h != Hero.MainHero;
			}
			catch
			{
				return false;
			}
		}

		// The tourney itself. free = the cheat: no cost, no cooldown.
		internal static bool Begin(Town town, int tier, List<Hero> riders, bool free)
		{
			try
			{
				if (town == null || Hero.MainHero == null)
				{
					return false;
				}
				tier = Math.Max(1, Math.Min(3, tier));
				int cost = Cost(tier);
				if (!free)
				{
					if (Hero.MainHero.Gold < cost)
					{
						Flow.Notify("The treasury cannot pay for it.");
						return false;
					}
					Hero.MainHero.ChangeHeroGold(-cost);
				}

				// A fresh tourney, not whatever the town already had. An old
				// one would be resolved on its own schedule, possibly
				// tomorrow, before a single guest had arrived.
				ITournamentManager manager = Campaign.Current.TournamentManager;
				TournamentGame old = manager.GetTournamentGame(town);
				if (old != null)
				{
					Remove(old);
				}
				manager.AddTournament(new FightTournamentGame(town));
				if (manager.GetTournamentGame(town) == null)
				{
					Log.Write("the tourney would not be registered at " + town.Name);
					if (!free)
					{
						Hero.MainHero.ChangeHeroGold(cost);
					}
					Flow.Notify("The heralds could not be sent. Nothing was spent.");
					return false;
				}

				// Who comes.
				List<Clan> guests = Guests(town);
				int brought = 0;
				foreach (Hero h in GuestRiders(guests))
				{
					if (Bring(h, town.Settlement))
					{
						brought++;
					}
				}
				int ours = 0;
				foreach (Hero h in riders ?? new List<Hero>())
				{
					if (Bring(h, town.Settlement))
					{
						ours++;
					}
				}

				Store.Set(TownKey, ((MBObjectBase)town.Settlement).StringId);
				Store.SetI(TierKey, tier);
				Store.SetI(DayKey, CourtBehavior.Today());
				Store.Set(GuestsKey, string.Join(",", guests.Select((Clan c) => ((MBObjectBase)c).StringId).ToArray()));
				if (!free)
				{
					Store.SetI(LastKey, CourtBehavior.Today());
				}
				Store.AddDeed(Standing.Date() + "  A " + TierName(tier) + " tourney was called at " + town.Name + ".");
				Log.Write("tourney called at " + town.Name + ": tier " + tier + ", " + guests.Count + " house(s), " +
					brought + " guest rider(s), " + ours + " of ours" + (free ? " (console, free)" : ""));

				Popup("The Heralds Ride",
					"The lists are going up outside " + town.Name + ".\n\n" +
					((guests.Count > 0)
						? (guests.Count + " house" + ((guests.Count == 1) ? " has" : "s have") + " sent word they are coming" +
							((brought > 0) ? (", and " + brought + " of their young lords are already in the town, looking at the lists.") : "."))
						: "Nobody of note has answered yet, which is its own kind of answer.") +
					((ours > 0) ? ("\n\n" + ours + " of your own will ride.") : "") +
					"\n\nGo to the arena to ride yourself. If you stay away it will be decided without you.");
				return true;
			}
			catch (Exception e)
			{
				Log.Write("the tourney could not begin: " + e);
				return false;
			}
		}

		// Your realm's houses, and failing that the nearest of anyone's who is
		// not at war with you.
		private static List<Clan> Guests(Town town)
		{
			List<Clan> list = new List<Clan>();
			try
			{
				Clan mine = Clan.PlayerClan;
				if (mine != null && mine.Kingdom != null)
				{
					list = mine.Kingdom.Clans.Where((Clan c) => Guest(c)).ToList();
				}
				Shuffle(list);
				if (list.Count < 3)
				{
					Vec2 at = town.Settlement.GetPosition2D;
					foreach (Clan c in Clan.All.Where((Clan c) => Guest(c) && !list.Contains(c) && c.HomeSettlement != null
						&& (mine == null || !FactionManager.IsAtWarAgainstFaction(c.MapFaction, mine.MapFaction)))
						.OrderBy((Clan c) => c.HomeSettlement.GetPosition2D.Distance(at)).Take(Math.Max(0, Cfg.TourneyGuests - list.Count)))
					{
						list.Add(c);
					}
				}
				if (list.Count > Cfg.TourneyGuests)
				{
					list = list.Take(Cfg.TourneyGuests).ToList();
				}
			}
			catch (Exception e)
			{
				Log.Once("tnguests", "the guest list failed: " + e.Message);
			}
			return list;
		}

		private static bool Guest(Clan c)
		{
			return c != null && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction
				&& c.Leader != null && c.Leader.IsAlive && c.Heroes.Count > 0;
		}

		// The young lords the guest houses send: of age, under forty, and not
		// already leading a party somewhere or governing a town.
		private static List<Hero> GuestRiders(List<Clan> guests)
		{
			List<Hero> all = new List<Hero>();
			try
			{
				foreach (Clan c in guests)
				{
					foreach (Hero h in c.Heroes)
					{
						if (Free(h) && h.IsLord && h.Age <= 40f && h != c.Leader)
						{
							all.Add(h);
						}
					}
				}
				Shuffle(all);
			}
			catch
			{
			}
			return all.Take(Cfg.TourneyGuestRiders).ToList();
		}

		// Put somebody in the town. The game only lets a hero into a
		// tournament if they are actually there.
		private static bool Bring(Hero h, Settlement where)
		{
			try
			{
				if (h == null || where == null || !Free(h))
				{
					return false;
				}
				if (h.CurrentSettlement == where)
				{
					return true;
				}
				if (h.CurrentSettlement != null)
				{
					LeaveSettlementAction.ApplyForCharacterOnly(h);
				}
				EnterSettlementAction.ApplyForCharacterOnly(h, where);
				return h.CurrentSettlement == where;
			}
			catch (Exception e)
			{
				Log.Once("tnbring", "a rider could not be brought: " + e.Message);
				return false;
			}
		}

		// ------------------------------------------------------------------
		// the game's events

		// The game says the tourney is over. This can arrive while the arena
		// mission is still running, so nothing is done here but writing it
		// down: killing a lord who still has a body in the lists, or opening
		// a popup over the arena, is asking for trouble. Settle() does the
		// rest once the map is back.
		internal static void OnFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
		{
			try
			{
				if (!Cfg.Tourneys || town == null || town.Settlement == null || !Store.Initialized)
				{
					return;
				}
				string tid = ((MBObjectBase)town.Settlement).StringId;
				Hero w = (winner != null && winner.IsHero) ? winner.HeroObject : null;
				bool mine = Store.Get(TownKey) == tid;
				bool rode = Store.Get(RodeKey) == tid;
				bool ours = w != null && (w == Hero.MainHero || w.Clan == Clan.PlayerClan || Blade.IsBaseborn(w));
				if (!mine && !rode && !ours)
				{
					return;
				}
				List<string> ids = new List<string>();
				if (participants != null)
				{
					foreach (CharacterObject c in participants)
					{
						if (c != null && c.IsHero && c.HeroObject != null)
						{
							ids.Add(((MBObjectBase)c.HeroObject).StringId);
						}
					}
				}
				string winnerId = (w != null) ? ((MBObjectBase)w).StringId : ("~" + ((winner != null) ? winner.Name.ToString() : "nobody"));
				int n = Store.GetI("tn:pendnext", 1);
				Store.SetI("tn:pendnext", n + 1);
				Store.Set(PendingPrefix + n, tid + "|" + winnerId + "|" + (mine ? "1" : "0") + "|" + (rode ? "1" : "0") + "|" + string.Join(",", ids.ToArray()));
				if (rode)
				{
					Store.Set(RodeKey, null);
				}
				Log.Write("tourney finished at " + town.Name + ": " + ((winner != null) ? winner.Name.ToString() : "nobody") +
					" won" + (mine ? " (yours)" : "") + (rode ? " (you rode)" : ""));
			}
			catch (Exception e)
			{
				Log.Write("reading the tourney's end failed: " + e.Message);
			}
		}

		internal static void OnJoined(Town town, bool participant)
		{
			try
			{
				if (participant && town != null && town.Settlement != null)
				{
					Store.Set(RodeKey, ((MBObjectBase)town.Settlement).StringId);
				}
			}
			catch
			{
			}
		}

		// Unhorsed in the first round of your own lists. Everyone saw.
		internal static void OnEliminated(int round, Town town)
		{
			try
			{
				if (!Cfg.Tourneys || town == null || town.Settlement == null)
				{
					return;
				}
				// Rounds count from 0: the game's own quests compare round + 1
				// against a round goal (LadysKnightOutIssueBehavior).
				if (Store.Get(TownKey) == ((MBObjectBase)town.Settlement).StringId && round == 0)
				{
					Standing.Change(0, -2, "Unhorsed early in your own tourney");
				}
			}
			catch
			{
			}
		}

		internal static void OnCancelled(Town town)
		{
			try
			{
				if (town == null || town.Settlement == null || Store.Get(TownKey) != ((MBObjectBase)town.Settlement).StringId)
				{
					return;
				}
				int refund = Purse(Tier);
				Hero.MainHero.ChangeHeroGold(refund);
				Store.AddDeed(Standing.Date() + "  The tourney at " + town.Name + " was called off.");
				Flow.Notify("The tourney at " + town.Name + " was called off. The purse, " + refund.ToString("N0") + ", comes back to you; the feast does not.");
				Clear();
			}
			catch (Exception e)
			{
				Log.Write("cancelling the tourney failed: " + e.Message);
			}
		}

		// Checked daily: a tourney of ours that vanished without finishing.
		internal static void Daily()
		{
			try
			{
				Town t = Hosted;
				if (t == null)
				{
					if (!string.IsNullOrEmpty(Store.Get(TownKey)))
					{
						Clear();
					}
					return;
				}
				if (Store.Keys(PendingPrefix).Count > 0)
				{
					return;
				}
				if (Campaign.Current.TournamentManager.GetTournamentGame(t) == null && CourtBehavior.Today() > Store.GetI(DayKey, 0) + 1)
				{
					Log.Write("the tourney at " + t.Name + " is gone without a winner");
					OnCancelled(t);
				}
			}
			catch (Exception e)
			{
				Log.Once("tndaily", "the tourney tick failed: " + e.Message);
			}
		}

		// Whatever has finished and not been settled. Called from the map,
		// the menus and the console - never from inside the arena.
		internal static void Settle()
		{
			try
			{
				if (!Store.Initialized || InMission())
				{
					return;
				}
				foreach (string key in Store.Keys(PendingPrefix).OrderBy((string k) => k.Length).ThenBy((string k) => k))
				{
					string rec = Store.Get(key);
					Store.Set(key, null);
					Resolve(rec);
				}
			}
			catch (Exception e)
			{
				Log.Write("settling the tourney failed: " + e);
			}
		}

		private static bool InMission()
		{
			try
			{
				return TaleWorlds.MountAndBlade.Mission.Current != null;
			}
			catch
			{
				return false;
			}
		}

		// ------------------------------------------------------------------
		// what it meant

		private static void Resolve(string rec)
		{
			string[] p = (rec ?? "").Split('|');
			if (p.Length < 5)
			{
				return;
			}
			Settlement where = FindSettlement(p[0]);
			Town town = (where != null) ? where.Town : null;
			if (town == null)
			{
				return;
			}
			bool mine = p[2] == "1";
			bool rode = p[3] == "1";
			Hero winner = p[1].StartsWith("~") ? null : FindHero(p[1]);
			string winnerName = p[1].StartsWith("~") ? p[1].Substring(1) : ((winner != null) ? winner.Name.ToString() : "somebody");
			List<Hero> riders = p[4].Split(',').Select(FindHero).Where((Hero h) => h != null).ToList();
			int tier = mine ? Tier : 0;

			System.Text.StringBuilder tale = new System.Text.StringBuilder();
			tale.Append(mine ? ("Your tourney at " + town.Name + " is over.") : ("The tourney at " + town.Name + " is over.")).Append("\n\n");

			// 1. The lists are not safe.
			Hero dead = null;
			Hero maimed = null;
			if (mine || rode)
			{
				Blood(riders, winner, out dead, out maimed);
				if (dead != null)
				{
					tale.Append(dead.Name).Append(" was carried from the lists and did not wake. ");
					if (mine && dead.Clan != null && dead.Clan != Clan.PlayerClan && dead.Clan.Leader != null && dead.Clan.Leader != dead)
					{
						ChangeRelationAction.ApplyPlayerRelation(dead.Clan.Leader, -Cfg.TourneyDeathRelation, false, false);
						tale.Append(dead.Clan.Name).Append(" came to your lists and went home with a body, and they hold the host to account.");
						Standing.Change(0, 2, "A death in your lists");
					}
					tale.Append("\n\n");
					Store.AddDeed(Standing.Date() + "  " + dead.Name + " died in the lists at " + town.Name + ".");
				}
				if (maimed != null)
				{
					tale.Append(maimed.Name).Append(" was badly hurt, and will not ride again for a long while.\n\n");
				}
			}

			// 2. The winner.
			tale.Append(winnerName).Append(" won");
			if (winner == null)
			{
				tale.Append(" - a hedge knight nobody could put a house to.");
			}
			tale.Append(mine ? (", and the " + Purse(tier).ToString("N0") + " purse with it.\n\n") : ".\n\n");

			bool youWon = winner != null && winner == Hero.MainHero;
			Hero heir = Succession.Named();
			List<Clan> guests = mine ? GuestClans() : new List<Clan>();

			if (youWon)
			{
				Standing.Change(Cfg.TourneyWinHonour, 0, "Won the tourney at " + town.Name);
			}
			else if (winner != null && Blade.IsBaseborn(winner))
			{
				AddWin(winner);
				int wins = Wins(winner);
				tale.Append("The crowd was on its feet for ").Append(winner.Name).Append(", who has your face and not your name. ");
				tale.Append(Crowned(winner)
					? "That is " + wins + " tourneys now. The smallfolk have started to say what the lords are only thinking.\n\n"
					: "They will remember it.\n\n");
				Store.AddDeed(Standing.Date() + "  " + winner.Name + ", baseborn, won the tourney at " + town.Name + ".");
			}
			else if (winner != null && winner == heir)
			{
				if (mine)
				{
					foreach (Clan c in guests)
					{
						if (c.Leader != null && c.Leader != heir)
						{
							ChangeRelationAction.ApplyRelationChangeBetweenHeroes(heir, c.Leader, Cfg.TourneyHeirRelation, false);
						}
					}
				}
				tale.Append("The realm saw what your heir can do in the saddle. That is worth more than the purse.\n\n");
				Store.AddDeed(Standing.Date() + "  " + heir.Name + ", your heir, won the tourney at " + town.Name + ".");
			}
			else if (winner != null && winner.Clan == Clan.PlayerClan)
			{
				Store.AddDeed(Standing.Date() + "  " + winner.Name + " won the tourney at " + town.Name + " for your house.");
			}
			else if (mine && winner != null)
			{
				bool enemy = winner.MapFaction != null && Clan.PlayerClan != null && FactionManager.IsAtWarAgainstFaction(winner.MapFaction, Clan.PlayerClan.MapFaction);
				ChangeRelationAction.ApplyPlayerRelation(winner, 10, false, false);
				if (enemy)
				{
					tale.Append("They are of a house at war with you, and you paid them anyway, in front of everyone. It was well done.\n\n");
					Standing.Change(1, 0, "Paid an enemy's champion");
				}
			}

			// 3. The purse, and what hosting earned.
			if (mine)
			{
				int purse = Purse(tier);
				if (winner != null && winner != Hero.MainHero)
				{
					winner.ChangeHeroGold(purse);
				}
				int rel = Cfg.TourneyHostRelation * tier;
				int shown = 0;
				foreach (Clan c in guests)
				{
					if (c.Leader != null && c.Leader.IsAlive && (dead == null || c != dead.Clan))
					{
						ChangeRelationAction.ApplyPlayerRelation(c.Leader, rel, false, false);
						shown++;
					}
				}
				GainRenownAction.Apply(Hero.MainHero, Cfg.TourneyHostRenown * tier, true);
				Standing.Change(tier, 0, "A " + TierName(tier) + " tourney at " + town.Name);
				if (shown > 0)
				{
					tale.Append(shown).Append(" guest house").Append((shown == 1) ? "" : "s").Append(" went home thinking better of you.");
				}
				History(Standing.Date() + "  " + town.Name + ": " + winnerName + ((dead != null) ? (", and " + dead.Name + " died") : ""));
				Clear();
			}

			Queue(mine ? "Your Tourney" : "The Lists", tale.ToString().TrimEnd());

			if (youWon)
			{
				if (mine)
				{
					int won = Purse(tier);
					QueueAction(delegate(Action done)
					{
						KeepOrGive(won, done);
					});
				}
				if (Cfg.TourneyQueen)
				{
					Settlement at = town.Settlement;
					QueueAction(delegate(Action done)
					{
						Wreath(at, guests, riders, done);
					});
				}
			}
			Next();
		}

		// One death and one maiming, at most. Never you.
		private static void Blood(List<Hero> riders, Hero winner, out Hero dead, out Hero maimed)
		{
			dead = null;
			maimed = null;
			try
			{
				// Your household is spared outright: companions and anyone who
				// married in ride under your roof, and losing them to a lance
				// every time you entered a tourney would make the lists a tax.
				// Your blood only if the config allows it.
				Hero you = Hero.MainHero;
				List<Hero> pool = riders.Where((Hero h) => h != null && h.IsAlive && h != you && h != winner
					&& (h.Clan != Clan.PlayerClan || (Cfg.TourneyKinCanDie && Succession.IsBlood(h, you)))).ToList();
				Shuffle(pool);
				foreach (Hero h in pool)
				{
					if (dead == null && MBRandom.RandomInt(100) < Cfg.TourneyDeathChance)
					{
						dead = h;
						continue;
					}
					if (maimed == null && MBRandom.RandomInt(100) < Cfg.TourneyMaimChance)
					{
						maimed = h;
					}
				}
				if (dead != null)
				{
					// "Died of his wounds" is the honest line for a lance
					// through the visor, and the encyclopedia will say so.
					KillCharacterAction.ApplyByWounds(dead, true);
					Log.Write("died in the lists: " + dead.Name);
				}
				if (maimed != null)
				{
					maimed.HitPoints = 1;
					Log.Write("maimed in the lists: " + maimed.Name);
				}
			}
			catch (Exception e)
			{
				Log.Write("the lists' toll failed: " + e.Message);
			}
		}

		// You won your own purse. Keep it, and everyone knows the host rode
		// for his own gold. Give it away, and everyone knows that too.
		private static void KeepOrGive(int purse, Action done)
		{
			Inquiry.Confirm("Your Own Purse",
				"You won the lists you paid for. The " + purse.ToString("N0") + " is yours by right, and every guest at the feast is watching to see what you do with it.",
				"Keep it", "Give it to the smallfolk",
				delegate
				{
					Hero.MainHero.ChangeHeroGold(purse);
					Standing.Change(-2, 0, "Kept your own purse");
					done();
				},
				delegate
				{
					Standing.Change(2, 0, "Gave your purse to the smallfolk");
					done();
				});
		}

		// ------------------------------------------------------------------
		// the Queen of Love and Beauty

		private static void Wreath(Settlement at, List<Clan> guests, List<Hero> riders, Action done)
		{
			try
			{
				Hero you = Hero.MainHero;
				List<Hero> can = Crownable(at, guests, riders);
				if (can.Count == 0)
				{
					done();
					return;
				}
				string title = you.IsFemale ? "King of Love and Beauty" : "Queen of Love and Beauty";
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (Hero h in can)
				{
					string tip;
					string tag;
					if (h == you.Spouse)
					{
						tag = "   - your " + (h.IsFemale ? "wife" : "husband");
						tip = "The safe choice, and the kind one. Nobody will talk about it tomorrow.";
					}
					else if (h.Spouse != null)
					{
						tag = "   - married to " + h.Spouse.Name;
						tip = "Another man's " + (h.IsFemale ? "wife" : "husband") + ", in front of him. " + h.Spouse.Name +
							" will not forgive it, and you lose Honour for it.";
					}
					else
					{
						tag = "   - unmarried, of " + ((h.Clan != null) ? h.Clan.Name.ToString() : "no house");
						tip = "Her house is flattered." + ((you.Spouse != null)
							? (" " + you.Spouse.Name + " is not, and neither is the court.")
							: "") + " And there is a feast afterwards.";
					}
					els.Add(new InquiryElement(h, h.Name + tag, null, true, tip));
				}
				Inquiry.Select("The " + title,
					"The herald hands you the wreath of winter roses. The whole of the stands goes quiet to see where you ride with it.",
					els, 1, 1, "Lay it in their lap", "Hand it back to the herald",
					delegate(List<InquiryElement> chosen)
					{
						Hero h = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
						if (h == null)
						{
							done();
							return;
						}
						Crown(h, at, title, done);
					},
					done);
			}
			catch (Exception e)
			{
				Log.Write("the wreath failed: " + e.Message);
				done();
			}
		}

		// Of the opposite sex, grown, noble, and not your own blood. Your
		// spouse first; then whoever is actually in the town; then the houses
		// who came.
		private static List<Hero> Crownable(Settlement at, List<Clan> guests, List<Hero> riders)
		{
			Hero you = Hero.MainHero;
			List<Hero> list = new List<Hero>();
			Action<Hero> add = delegate(Hero h)
			{
				if (h != null && h.IsAlive && h.IsLord && !h.IsChild && h.IsFemale != you.IsFemale && h != you
					&& !list.Contains(h) && (h == you.Spouse || !Succession.IsBlood(h, you)) && list.Count < 10)
				{
					list.Add(h);
				}
			};
			try
			{
				add(you.Spouse);
				if (at != null)
				{
					foreach (Hero h in at.HeroesWithoutParty)
					{
						add(h);
					}
					foreach (MobileParty mp in at.Parties)
					{
						if (mp != null && mp.LeaderHero != null)
						{
							add(mp.LeaderHero);
						}
					}
				}
				foreach (Clan c in guests)
				{
					foreach (Hero h in c.Heroes)
					{
						add(h);
					}
				}
				foreach (Hero r in riders)
				{
					if (r != null && r.Clan != null)
					{
						foreach (Hero h in r.Clan.Heroes)
						{
							add(h);
						}
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static void Crown(Hero h, Settlement at, string title, Action done)
		{
			try
			{
				Hero you = Hero.MainHero;
				Store.AddDeed(Standing.Date() + "  " + h.Name + " was crowned " + title + ".");
				if (h == you.Spouse)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, h, 10, false);
					Standing.Change(1, 0, "Crowned your own spouse");
					done();
					return;
				}
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, h, 15, false);
				if (h.Spouse != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, h.Spouse, -30, false);
					Standing.Change(-4, 0, "Crowned " + h.Spouse.Name + "'s spouse over everyone");
					if (you.Spouse != null)
					{
						ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, you.Spouse, -15, false);
					}
					done();
					return;
				}
				if (h.Clan != null && h.Clan.Leader != null && h.Clan.Leader != h)
				{
					ChangeRelationAction.ApplyPlayerRelation(h.Clan.Leader, 8, false, false);
				}
				if (you.Spouse != null)
				{
					ChangeRelationAction.ApplyRelationChangeBetweenHeroes(you, you.Spouse, -15, false);
					Standing.Change(-2, 0, "Passed over your own spouse for the wreath");
				}
				// Rhaegar at Harrenhal.
				if (Cfg.Baseborn && Baseborn.HasRoom() && you.Age >= Cfg.BaseNightMinAge && at != null)
				{
					Inquiry.Confirm("After the Feast",
						h.Name + " finds you when the feast is breaking up, still wearing the roses.\n\n" +
						"Nobody is watching. That is not the same as nobody seeing.",
						"Go with " + (h.IsFemale ? "her" : "him"), "Bid " + (h.IsFemale ? "her" : "him") + " goodnight",
						delegate
						{
							Night(h, at);
							done();
						},
						done);
					return;
				}
				done();
			}
			catch (Exception e)
			{
				Log.Write("the crowning failed: " + e.Message);
				done();
			}
		}

		// The night. A child may come of it, in the usual three years - and
		// this one has a mother whose house knows exactly who you are.
		private static void Night(Hero h, Settlement at)
		{
			try
			{
				Kid k = Baseborn.Conceive(h, at);
				if (k == null)
				{
					return;
				}
				Store.AddDeed(Standing.Date() + "  A night after the feast at " + at.Name + ".");
				if (MBRandom.RandomInt(100) < Cfg.TourneyScandalChance && h.Clan != null && h.Clan.Leader != null && h.Clan.Leader != h)
				{
					ChangeRelationAction.ApplyPlayerRelation(h.Clan.Leader, -25, false, false);
					Standing.Change(-3, 0, "Dishonoured a daughter of " + h.Clan.Name);
					Popup("Found Out",
						h.Clan.Leader.Name + " knows where " + h.Name + " spent the night after your tourney.\n\n" +
						"The roses were one thing. This is another, and " + h.Clan.Name + " will not forget it.");
				}
				else
				{
					Flow.Notify("Nobody saw. Or nobody is saying.");
				}
			}
			catch (Exception e)
			{
				Log.Write("the night after the feast failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the console

		// Resolve a tourney now, as the game would when its time ran out.
		internal static string ResolveNow(Town town)
		{
			try
			{
				if (town == null)
				{
					return "No town.";
				}
				ITournamentManager manager = Campaign.Current.TournamentManager;
				TournamentGame game = manager.GetTournamentGame(town);
				if (game == null)
				{
					return town.Name + " has no tourney to resolve.";
				}
				manager.ResolveTournament(game, town);
				if (manager.GetTournamentGame(town) == game)
				{
					Remove(game);
				}
				Settle();
				return "The tourney at " + town.Name + " was decided without you.";
			}
			catch (Exception e)
			{
				Log.Write("resolving the tourney failed: " + e);
				return "It could not be resolved: " + e.Message;
			}
		}

		internal static string Status()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.AppendLine("Tourneys " + (Cfg.Tourneys ? "on" : "OFF"));
			Town t = Hosted;
			if (t != null)
			{
				sb.AppendLine("  yours: " + t.Name + ", " + TierName(Tier) + ", called day " + Store.GetI(DayKey, 0) +
					", still registered: " + (Campaign.Current.TournamentManager.GetTournamentGame(t) != null));
				sb.AppendLine("  guest houses: " + string.Join(", ", GuestClans().Select((Clan c) => c.Name.ToString()).ToArray()));
			}
			else
			{
				sb.AppendLine("  none of yours running");
			}
			int last = Store.GetI(LastKey, -9999);
			sb.AppendLine("  last called: " + ((last > -9000) ? ("day " + last) : "never") + ", today is day " + CourtBehavior.Today());
			sb.AppendLine("  waiting to be settled: " + Store.Keys(PendingPrefix).Count);
			foreach (string k in Store.Keys(WinsPrefix))
			{
				Hero h = FindHero(k.Substring(WinsPrefix.Length));
				if (h != null)
				{
					sb.AppendLine("  " + h.Name + ": " + Store.GetI(k, 0) + " win(s)" + (Blade.IsBaseborn(h) ? ", baseborn" : "") + (Crowned(h) ? ", CROWD'S KING" : ""));
				}
			}
			string why;
			sb.AppendLine(CanHost(out why) ? "  you can call one here" : ("  cannot call one here: " + why));
			return sb.ToString();
		}

		// ------------------------------------------------------------------
		// the court

		internal static string Summary()
		{
			try
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				Town t = Hosted;
				if (t != null)
				{
					sb.Append("Your ").Append(TierName(Tier)).Append(" tourney at ").Append(t.Name)
					  .Append(" is under way. Ride there to take part; stay away and it is decided without you.\n");
				}
				string hist = Store.Get(HistoryKey);
				if (!string.IsNullOrEmpty(hist))
				{
					sb.Append("\nTOURNEYS YOU HAVE HELD\n");
					foreach (string line in hist.Split('\n'))
					{
						sb.Append("  ").Append(line).Append("\n");
					}
				}
				List<string> champs = new List<string>();
				foreach (Kid k in Baseborn.Known())
				{
					Hero h = Baseborn.HeroOf(k);
					if (Wins(h) > 0)
					{
						champs.Add(h.Name + " - " + Wins(h) + " win" + ((Wins(h) == 1) ? "" : "s") + (Crowned(h) ? ", and the crowds would crown them" : ""));
					}
				}
				if (champs.Count > 0)
				{
					sb.Append("\nYOUR BLOOD IN THE LISTS\n");
					foreach (string c in champs)
					{
						sb.Append("  ").Append(c).Append("\n");
					}
				}
				return sb.ToString();
			}
			catch
			{
				return "";
			}
		}

		internal static string Attention()
		{
			Town t = Hosted;
			return (t == null) ? null : ("Your tourney at " + t.Name + " is under way. Ride there to take part.");
		}

		// ------------------------------------------------------------------
		// bits

		private static List<Clan> GuestClans()
		{
			List<Clan> list = new List<Clan>();
			try
			{
				foreach (string id in (Store.Get(GuestsKey) ?? "").Split(','))
				{
					if (id.Length == 0)
					{
						continue;
					}
					Clan c = Clan.All.FirstOrDefault((Clan x) => ((MBObjectBase)x).StringId == id && !x.IsEliminated);
					if (c != null)
					{
						list.Add(c);
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static void Clear()
		{
			Store.Set(TownKey, null);
			Store.Set(TierKey, null);
			Store.Set(DayKey, null);
			Store.Set(GuestsKey, null);
		}

		private static void History(string line)
		{
			List<string> lines = (Store.Get(HistoryKey) ?? "").Split('\n').Where((string l) => l.Length > 0).ToList();
			lines.Insert(0, line);
			Store.Set(HistoryKey, string.Join("\n", lines.Take(6).ToArray()));
		}

		private static void Refresh()
		{
			try
			{
				GameMenu.SwitchToMenu("wad_lists");
			}
			catch
			{
			}
		}

		// RemoveTournament is on the manager class, not its interface.
		private static void Remove(TournamentGame game)
		{
			try
			{
				object m = Campaign.Current.TournamentManager;
				MethodInfo rm = AccessTools.Method(m.GetType(), "RemoveTournament", (Type[])null, (Type[])null);
				if (rm != null)
				{
					rm.Invoke(m, new object[1] { game });
				}
			}
			catch (Exception e)
			{
				Log.Once("tnremove", "an old tourney could not be taken down: " + e.Message);
			}
		}

		private static Settlement FindSettlement(string id)
		{
			try
			{
				return string.IsNullOrEmpty(id) ? null : Settlement.Find(id);
			}
			catch
			{
				return null;
			}
		}

		private static Hero FindHero(string id)
		{
			try
			{
				return string.IsNullOrEmpty(id) ? null : Hero.AllAliveHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
			}
			catch
			{
				return null;
			}
		}

		private static void Shuffle<T>(List<T> list)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = MBRandom.RandomInt(i + 1);
				T tmp = list[i];
				list[i] = list[j];
				list[j] = tmp;
			}
		}

		// Popups one after another. A tourney can end with a death, a purse,
		// a wreath and a night, and four inquiries opened at once would stack
		// on top of each other with the last one on top.
		private static readonly Queue<Action<Action>> _queue = new Queue<Action<Action>>();

		private static bool _showing;

		// A popup left open when a save was loaded would otherwise block every
		// tourney popup for the rest of the session.
		internal static void Reset()
		{
			_queue.Clear();
			_showing = false;
		}

		private static void Queue(string title, string text)
		{
			QueueAction(delegate(Action done)
			{
				try
				{
					InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", null, done, null, "", 0f, null, null, null), true, false);
				}
				catch
				{
					done();
				}
			});
		}

		private static void QueueAction(Action<Action> a)
		{
			_queue.Enqueue(a);
		}

		private static void Popup(string title, string text)
		{
			Queue(title, text);
			Next();
		}

		private static void Next()
		{
			if (_showing || _queue.Count == 0)
			{
				return;
			}
			_showing = true;
			Action<Action> a = _queue.Dequeue();
			bool finished = false;
			Action done = delegate
			{
				if (finished)
				{
					return;
				}
				finished = true;
				_showing = false;
				Next();
			};
			try
			{
				a(done);
			}
			catch (Exception e)
			{
				Log.Write("a tourney popup failed: " + e.Message);
				done();
			}
		}
	}
}
