using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// A feast they will not leave.
	//
	// The other side of the ravens: a false letter of your own. Choose a
	// house, a pretext and one of your halls; pay the servants, the
	// crossbowmen in the galleries and the men who know when to bar the doors; wait
	// while it is prepared, and hope nobody talks. If the house comes, you
	// may still let them eat and go home. Or you may not.
	internal static class Treachery
	{
		private const string SchemeKey = "tr:scheme";

		internal const string Wedding = "wedding";
		internal const string Reconcile = "feast";
		internal const string KinLetter = "kin";

		// clan | pretext | venue | ready day | state | ours | theirs
		// state: prep (being prepared), laid (they are coming)
		private static string[] Scheme()
		{
			string[] s = (Store.Get(SchemeKey) ?? "").Split('|');
			return (s.Length >= 7) ? s : null;
		}

		private static void Put(string[] s)
		{
			Store.Set(SchemeKey, (s == null) ? null : string.Join("|", s));
		}

		internal static bool Active
		{
			get
			{
				return Scheme() != null;
			}
		}

		// ------------------------------------------------------------------
		// who would come

		// Every adult of the house who could sit at a table: alive, free,
		// not in a battle and not marching in an army. Those riding at the
		// head of their own party count - the feast draws them in.
		internal static List<Hero> Guests(Clan c)
		{
			List<Hero> list = new List<Hero>();
			if (c == null)
			{
				return list;
			}
			try
			{
				foreach (Hero h in c.Heroes)
				{
					if (h == null || !h.IsAlive || h.IsChild || h.IsPrisoner || h == Hero.MainHero)
					{
						continue;
					}
					MobileParty p = h.PartyBelongedTo;
					if (p != null && (p.MapEvent != null || p.Army != null || p == MobileParty.MainParty))
					{
						continue;
					}
					list.Add(h);
				}
			}
			catch
			{
			}
			return list;
		}

		internal static int Cost(Clan c)
		{
			return Cfg.TreacheryCostBase + Cfg.TreacheryCostPerGuest * Guests(c).Count;
		}

		private static bool KinPretext(Clan c)
		{
			try
			{
				if (Wardship.From(c) != null)
				{
					return true;
				}
				return Clan.PlayerClan.Heroes.Any((Hero h) => h.IsAlive && ((h.Father != null && h.Father.Clan == c) || (h.Mother != null && h.Mother.Clan == c)));
			}
			catch
			{
				return false;
			}
		}

		private static List<Settlement> Halls()
		{
			return Clan.PlayerClan.Settlements.Where((Settlement s) => s.IsTown || s.IsCastle).ToList();
		}

		// ------------------------------------------------------------------
		// the plan

		internal static void Plan()
		{
			if (Active)
			{
				Flow.Notify("One feast at a time.");
				return;
			}
			if (Halls().Count == 0)
			{
				Flow.Notify("You hold no hall to lay a feast in.");
				return;
			}
			List<Clan> can = Clan.All.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction && c.Leader != null && c.Leader.IsAlive && Guests(c).Count > 0)
				.OrderBy((Clan c) => c.Leader.GetRelationWithPlayer()).Take(60).ToList();
			if (can.Count == 0)
			{
				Flow.Notify("There is no house to invite.");
				return;
			}
			List<InquiryElement> els = can.Select((Clan c) => new InquiryElement(c,
				c.Name + "  (" + Guests(c).Count + " would come, " + Cost(c).ToString("N0") + " gold, relation " + (int)c.Leader.GetRelationWithPlayer() + ")", null,
				Hero.MainHero.Gold >= Cost(c), "")).ToList();
			Inquiry.Select("A Feast They Will Not Leave", "Which house?\n\nThe gold is paid up front - servants, crossbowmen for the galleries, servants who will bar the doors when you say - and it is not coming back whatever happens.",
				els, 1, 1, "That one", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Clan c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Clan) : null;
					if (c != null)
					{
						Pretext(c);
					}
				});
		}

		private static void Pretext(Clan c)
		{
			Hero a;
			Hero b;
			bool wed = Ravens.Pair(c, out a, out b);
			bool kin = KinPretext(c);
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement(Wedding, wed ? ("A wedding: " + a.Name + " to " + b.Name) : "A wedding", null, wed,
				wed ? "A real match, really made. They will all come to see it. (+30 to their coming)" : "Nobody of your blood is free to marry anyone of theirs."));
			els.Add(new InquiryElement(Reconcile, "A feast of friendship", null, true, "Bread and salt, and old quarrels forgotten."));
			els.Add(new InquiryElement(KinLetter, "A letter from their own kin at your court", null, kin,
				kin ? "Their blood is under your roof, and asks for them. (+20 to their coming)" : "None of their blood lives with you."));
			Inquiry.Select("The Pretext", "What brings " + c.Name + " to your table?", els, 1, 1, "That", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					string p = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
					if (p != null)
					{
						Venue(c, p, (p == Wedding) ? a : null, (p == Wedding) ? b : null);
					}
				});
		}

		private static void Venue(Clan c, string pretext, Hero ours, Hero theirs)
		{
			List<InquiryElement> els = Halls().Select((Settlement s) => new InquiryElement(s, s.Name.ToString(), null, true, "")).ToList();
			int cost = Cost(c);
			Inquiry.Select("The Hall", "Where is it held? You must be there on the day.\n\nIt costs " + cost.ToString("N0") + ", and takes " + Cfg.TreacheryPrepDays + " days to prepare, any of which it may leak.",
				els, 1, 1, "Here", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Settlement s = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Settlement) : null;
					if (s == null)
					{
						return;
					}
					if (Hero.MainHero.Gold < cost)
					{
						Flow.Notify("You cannot pay for it.");
						return;
					}
					Hero.MainHero.ChangeHeroGold(-cost);
					Put(new string[7]
					{
						((MBObjectBase)c).StringId,
						pretext,
						((MBObjectBase)s).StringId,
						(CourtBehavior.Today() + Cfg.TreacheryPrepDays).ToString(),
						"prep",
						(ours != null) ? ((MBObjectBase)ours).StringId : "",
						(theirs != null) ? ((MBObjectBase)theirs).StringId : ""
					});
					Log.Write("scheme laid against " + c.Name + " at " + s.Name + " (" + pretext + ", " + cost + " gold)");
					Ravens.Popup("The Feast Is Planned",
						"The servants are paid. The galleries over the hall at " + s.Name + " will have men in them, and the servants know which word means bar the doors.\n\n" +
						"In " + Cfg.TreacheryPrepDays + " days the raven goes to " + c.Name + ". Until then, the fewer who know, the better.");
				});
		}

		// ------------------------------------------------------------------
		// days of preparation

		internal static int LeakChance()
		{
			int chance = 2 + Store.Dread / 20 - Store.Honour / 25;
			try
			{
				if (Guard.All().Any((Knight k) => k.Rank == 1))
				{
					chance--;
				}
			}
			catch
			{
			}
			return Math.Max(1, chance);
		}

		internal static int ComeChance(Clan c, string pretext)
		{
			int chance = 40 + (int)c.Leader.GetRelationWithPlayer() / 2 + (Store.Honour - 50) / 2 - Store.Dread / 4;
			chance += (pretext == Wedding) ? 30 : ((pretext == KinLetter) ? 20 : 0);
			if (Store.Get(Ravens.SaltKey) == "1")
			{
				chance -= 30;
			}
			return Math.Max(5, Math.Min(95, chance));
		}

		internal static void Daily()
		{
			try
			{
				string[] s = Scheme();
				if (s == null || !Cfg.Ravens || !Store.Initialized)
				{
					return;
				}
				Clan c = Clan.FindFirst((Clan x) => ((MBObjectBase)x).StringId == s[0]);
				Settlement venue = Settlement.Find(s[2]);
				if (c == null || c.IsEliminated || c.Leader == null || venue == null || venue.OwnerClan != Clan.PlayerClan)
				{
					Put(null);
					Ravens.Popup("The Feast", "The feast you were preparing will not happen now. The gold is spent.");
					return;
				}
				int today = CourtBehavior.Today();
				int ready;
				int.TryParse(s[3], out ready);
				if (s[4] == "prep")
				{
					if (MBRandom.RandomInt(100) < LeakChance())
					{
						Put(null);
						ChangeRelationAction.ApplyPlayerRelation(c.Leader, -40, true, false);
						Standing.Change(-5, 0, "A plot against " + c.Name + " came out");
						Store.AddDeed(Standing.Date() + "  The whole realm knows what you meant to do to " + c.Name + " at " + venue.Name + ".");
						Log.Write("the scheme against " + c.Name + " leaked");
						Ravens.Popup("It Came Out",
							"Somebody talked - a servant, a cupbearer, a crossbowman with a sweetheart. " + c.Name + " know what was waiting for them at " + venue.Name + ", and soon everyone else will.\n\nThe gold is gone. So is anything they ever thought of you.");
						return;
					}
					if (today < ready)
					{
						return;
					}
					if (MBRandom.RandomInt(100) >= ComeChance(c, s[1]))
					{
						Put(null);
						Log.Write(c.Name + " declined the feast");
						Ravens.Popup("Their Regrets", c.Name + " thank you for the invitation, and will not be coming. The servants were paid all the same.");
						return;
					}
					s[4] = "laid";
					s[3] = today.ToString();
					Put(s);
					Log.Write(c.Name + " accepted the feast at " + venue.Name);
					Ravens.Popup("They Are Coming",
						c.Name + " accept. " + Guests(c).Count + " of them will sit at your table in " + venue.Name + ".\n\nBe there. They will wait five days, and then they will go home and wonder why you asked.");
					return;
				}
				if (s[4] == "laid" && today > ready + 5)
				{
					Put(null);
					ChangeRelationAction.ApplyPlayerRelation(c.Leader, -10, true, false);
					Ravens.Popup("An Empty Chair", c.Name + " came to " + venue.Name + " and found no host. They have gone home, insulted and alive.");
				}
			}
			catch (Exception e)
			{
				Log.Once("trdaily", "the scheme's tick failed: " + e.Message);
			}
		}

		// Cheat: skip the preparation, and they have said yes.
		internal static bool Ready()
		{
			string[] s = Scheme();
			if (s == null)
			{
				return false;
			}
			s[4] = "laid";
			s[3] = CourtBehavior.Today().ToString();
			Put(s);
			return true;
		}

		internal static bool FeastHere()
		{
			string[] s = Scheme();
			return s != null && s[4] == "laid" && Settlement.CurrentSettlement != null && ((MBObjectBase)Settlement.CurrentSettlement).StringId == s[2];
		}

		// ------------------------------------------------------------------
		// the feast

		internal static void Feast()
		{
			string[] s = Scheme();
			if (s == null)
			{
				return;
			}
			Clan c = Clan.FindFirst((Clan x) => ((MBObjectBase)x).StringId == s[0]);
			if (c == null)
			{
				Put(null);
				return;
			}
			List<Hero> guests = Guests(c);
			string names = string.Join(", ", guests.Select((Hero h) => h.Name.ToString()).ToArray());
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement("eat", "Let them eat, and go home", null, true, "Guest right is kept. The gold was the price of a feast after all."));
			els.Add(new InquiryElement("rains", "Give the signal: bar the doors", null, true,
				"The doors are barred. You and your men are armoured; they are dressed for dinner. Guest right will be broken, and no one will ever eat your bread again without wondering."));
			Inquiry.Select("The Feast Is Laid", c.Name + " are at your table: " + names + ".\n\nThe wine is poured. The servants are watching you for the sign.", els, 1, 1, "So be it", null,
				delegate(List<InquiryElement> chosen)
				{
					string p = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : "eat";
					if (p == "rains")
					{
						Rains(s, c, guests);
					}
					else
					{
						LetThemEat(s, c);
					}
				},
				delegate
				{
				});
		}

		private static void Wed(string[] s, Settlement venue)
		{
			if (s[1] != Wedding)
			{
				return;
			}
			Hero a = Law.Find(s[5]);
			Hero b = Law.Find(s[6]);
			if (a != null && b != null && Ravens.Suitable(a) && Ravens.Suitable(b))
			{
				MarriageAction.Apply(a, b, true);
				Store.AddDeed(Standing.Date() + "  " + a.Name + " married " + b.Name + " at " + venue.Name + ".");
			}
		}

		private static void LetThemEat(string[] s, Clan c)
		{
			Put(null);
			Settlement venue = Settlement.Find(s[2]);
			Wed(s, venue);
			ChangeRelationAction.ApplyPlayerRelation(c.Leader, 20, true, false);
			Standing.Change(3, 0, "Kept guest right with " + c.Name);
			Ravens.Popup("Bread and Salt",
				"You never gave the sign. " + c.Name + " ate, and drank, and went home, and will never know how close it was.\n\nThe crossbowmen in the galleries were paid to watch a feast. It is the most any of them has ever been paid to do nothing.");
		}

		private static void Rains(string[] s, Clan c, List<Hero> guests)
		{
			Settlement venue = Settlement.Find(s[2]);
			Wed(s, venue);
			Put(null);

			// Your side: you, your white cloaks, then the best men you have.
			List<HallSeat> ours = new List<HallSeat>();
			foreach (Hero h in Guard.Ready())
			{
				if (ours.Count >= 12)
				{
					break;
				}
				ours.Add(new HallSeat(h.CharacterObject, false));
			}
			foreach (TroopRosterElement e in MobileParty.MainParty.MemberRoster.GetTroopRoster().Where((TroopRosterElement x) => x.Character != null && !x.Character.IsHero)
				.OrderByDescending((TroopRosterElement x) => x.Character.Tier))
			{
				for (int i = 0; i < e.Number - e.WoundedNumber && ours.Count < 12; i++)
				{
					ours.Add(new HallSeat(e.Character, false));
				}
			}
			// Theirs: the family in their feast clothes, and a few of their
			// own guards who were allowed their swords.
			List<HallSeat> theirs = guests.Select((Hero h) => new HallSeat(h.CharacterObject, true)).ToList();
			List<CharacterObject> guards = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Culture == c.Culture
				&& x.Occupation == Occupation.Soldier && x.Tier >= 3).ToList();
			int n = 2 + MBRandom.RandomInt(3);
			for (int i = 0; i < n && guards.Count > 0; i++)
			{
				theirs.Add(new HallSeat(guards[MBRandom.RandomInt(guards.Count)], false));
			}
			Ravens.SetFight("scheme|" + ((MBObjectBase)venue).StringId + "|" + ((MBObjectBase)c).StringId + "|" +
				Ravens.Ids(ours.Select((HallSeat x) => x.Who)) + "|" + Ravens.Ids(theirs.Select((HallSeat x) => x.Who)) + "|" +
				Ravens.Ids(guests.Select((Hero h) => h.CharacterObject)));
			Log.Write("the doors barred at " + venue.Name + ": " + ours.Count + " of yours against " + guests.Count + " guests and " + (theirs.Count - guests.Count) + " guards");
			string why;
			bool opened = HallFight.Open(venue, ours, false, theirs, false, delegate(bool won, List<CharacterObject> fallen)
			{
				Ravens.SetResult((won ? "1" : "0") + "|" + Ravens.Ids(fallen));
			}, out why);
			if (!opened)
			{
				// Decided without the hall: a massacre usually goes the way
				// it was planned.
				Log.Write("the hall could not be staged: " + why);
				bool won = MBRandom.RandomInt(100) < 90;
				List<CharacterObject> fallen = guests.Where((Hero h) => won ? (MBRandom.RandomInt(100) < 85) : (MBRandom.RandomInt(100) < 30)).Select((Hero h) => h.CharacterObject).ToList();
				if (!won)
				{
					fallen.Add(CharacterObject.PlayerCharacter);
				}
				Ravens.SetResult((won ? "1" : "0") + "|" + Ravens.Ids(fallen));
			}
		}

		// ------------------------------------------------------------------
		// after

		// f = scheme | venue | clan | ours | theirs | guests
		internal static void After(string[] f, bool won, List<CharacterObject> fallen)
		{
			Settlement venue = Settlement.Find(f[1]);
			Clan c = Clan.FindFirst((Clan x) => ((MBObjectBase)x).StringId == f[2]);
			List<CharacterObject> ours = Ravens.Chars(f[3]);
			List<Hero> guests = Ravens.Chars((f.Length > 5) ? f[5] : "").Where((CharacterObject x) => x.IsHero && x.HeroObject != null).Select((CharacterObject x) => x.HeroObject).ToList();
			string where = (venue != null) ? venue.Name.ToString() : "your hall";
			string house = (c != null) ? c.Name.ToString() : "their house";
			StringBuilder tale = new StringBuilder();

			// The guests who fell do not get up: it was not a trial.
			List<Hero> dead = new List<Hero>();
			List<Hero> fled = new List<Hero>();
			foreach (Hero h in guests)
			{
				if (!h.IsAlive)
				{
					continue;
				}
				if (fallen.Contains(h.CharacterObject))
				{
					dead.Add(h);
				}
				else
				{
					fled.Add(h);
				}
			}
			// Kin of the dead who were not at the table: they will hear.
			List<Hero> kin = new List<Hero>();
			foreach (Hero h in dead)
			{
				try
				{
					IEnumerable<Hero> rel = new Hero[3] { h.Father, h.Mother, h.Spouse }.Concat(h.Children).Concat(h.Siblings);
					foreach (Hero k in rel)
					{
						if (k != null && k.IsAlive && k != Hero.MainHero && k.Clan != Clan.PlayerClan && !guests.Contains(k) && !kin.Contains(k))
						{
							kin.Add(k);
						}
					}
				}
				catch
				{
				}
			}
			// If nobody of age will be left, the house's lands and children
			// are taken first - before the game, seeing its lord dead, hands
			// them to someone else.
			bool wipe = c != null && !c.IsEliminated && c != Clan.PlayerClan && dead.Count > 0
				&& c.Heroes.Where((Hero h) => h.IsAlive && !h.IsChild).All((Hero h) => dead.Contains(h));
			List<Settlement> fiefs = new List<Settlement>();
			List<Hero> children = new List<Hero>();
			if (wipe)
			{
				Seize(c, fiefs, children);
			}
			Law.Quiet = true;
			try
			{
				foreach (Hero h in dead)
				{
					try
					{
						KillCharacterAction.ApplyByMurder(h, Hero.MainHero, true);
						tale.Append(h.Name).Append(" died at your table.\n");
					}
					catch (Exception e)
					{
						Log.Write("a death at the feast would not take: " + e.Message);
					}
				}
				// Your own who fell take their chance, as in any fight.
				foreach (CharacterObject o in fallen)
				{
					Hero h = (o != null && o.IsHero) ? o.HeroObject : null;
					if (h != null && h != Hero.MainHero && h.IsAlive && ours.Contains(o) && MBRandom.RandomInt(100) < Cfg.TrialDeathChance)
					{
						try
						{
							KillCharacterAction.ApplyByBattle(h, null, true);
							tale.Append(h.Name).Append(", yours, died in the hall as well.\n");
						}
						catch
						{
						}
					}
				}
			}
			finally
			{
				Law.Quiet = false;
			}
			foreach (Hero h in fled)
			{
				Store.Set(Ravens.VengeancePrefix + ((MBObjectBase)h).StringId, CourtBehavior.Today().ToString());
				tale.Append(h.Name).Append(" got out of the hall alive, and has sworn to repay you in kind.\n");
			}

			// What it costs you, whether or not it went to plan.
			Standing.Change(-25, 30, "Broke guest right at " + where);
			Store.HonourCap = Math.Max(10, Store.HonourCap - Cfg.TreacheryHonourCap);
			if (Store.Honour > Store.HonourCap)
			{
				Store.Honour = Store.HonourCap;
			}
			Store.Set(Ravens.SaltKey, "1");
			try
			{
				foreach (Clan other in Clan.All.Where((Clan x) => x != null && x != Clan.PlayerClan && x != c && !x.IsEliminated && !x.IsBanditFaction && x.Leader != null && x.Leader.IsAlive).ToList())
				{
					ChangeRelationAction.ApplyPlayerRelation(other.Leader, -15, false, false);
				}
				foreach (Hero k in kin)
				{
					ChangeRelationAction.ApplyPlayerRelation(k, -60, false, false);
					Store.Set(Ravens.VengeancePrefix + ((MBObjectBase)k).StringId, CourtBehavior.Today().ToString());
				}
			}
			catch (Exception e)
			{
				Log.Write("the realm's answer to the feast failed: " + e.Message);
			}
			Store.AddDeed(Standing.Date() + "  Guest right broken at " + where + ": " + dead.Count + " of " + house + " died at your table.");
			Log.Write("massacre at " + where + ": " + dead.Count + " dead, " + fled.Count + " escaped, won=" + won);

			// The charge: from your liege, or from your own lords.
			try
			{
				Hero accuser = null;
				Kingdom realm = Clan.PlayerClan.Kingdom;
				if (realm != null && realm.Leader != null && realm.Leader != Hero.MainHero && realm.Leader.IsAlive)
				{
					accuser = realm.Leader;
				}
				else if (realm != null)
				{
					accuser = realm.Clans.Where((Clan x) => x != Clan.PlayerClan && x.Leader != null && x.Leader.IsAlive && !x.IsEliminated)
						.Select((Clan x) => x.Leader).OrderBy((Hero x) => x.GetRelationWithPlayer()).FirstOrDefault();
				}
				Hero victim = dead.FirstOrDefault((Hero x) => c != null && x == c.Leader) ?? dead.FirstOrDefault();
				Law.Record(Law.GuestRight, Hero.MainHero, accuser ?? kin.FirstOrDefault(), victim, false);
			}
			catch (Exception e)
			{
				Log.Write("the guest right charge failed: " + e.Message);
			}

			// The house, if nobody of age is left of it.
			string end = wipe ? End(c, fiefs, children) : null;
			if (end != null)
			{
				tale.Append("\n").Append(end).Append("\n");
			}

			bool youFell = fallen.Contains(CharacterObject.PlayerCharacter);
			bool youDie = youFell && Cfg.TrialPlayerCanDie && MBRandom.RandomInt(100) < Cfg.TrialDeathChance;
			string head = won
				? "You gave the sign. The doors were barred, and the crossbows came out of the galleries.\n\n"
				: "You gave the sign, and it did not go the way it was meant to.\n\n";
			if (youFell)
			{
				tale.Append(youDie ? "\nYou did not get up either.\n" : "\nYou were carried out of your own hall, and you will live.\n");
				if (!youDie)
				{
					Hero.MainHero.HitPoints = Math.Min(Hero.MainHero.HitPoints, 5);
				}
			}
			tale.Append("\nGuest right was broken under your roof. Nobody will eat your bread and salt again without looking at the doors.");
			Ravens.Popup("The Doors Are Barred", head + tale);
			if (youDie)
			{
				try
				{
					Log.Write("the player died at their own feast");
					KillCharacterAction.ApplyByMurder(Hero.MainHero, fled.FirstOrDefault(), true);
				}
				catch (Exception e)
				{
					Log.Write("the player's death at the feast failed: " + e.Message);
				}
			}
		}

		// Nobody of age left: their castles and towns are yours, their
		// children are raised at your court, and the house is ended.
		private static void Seize(Clan c, List<Settlement> fiefs, List<Hero> children)
		{
			try
			{
				fiefs.AddRange(c.Settlements.Where((Settlement s) => s.IsTown || s.IsCastle));
				foreach (Settlement s in fiefs)
				{
					try
					{
						ChangeOwnerOfSettlementAction.ApplyByKingDecision(Hero.MainHero, s);
					}
					catch (Exception e)
					{
						Log.Write("taking " + s.Name + " failed: " + e.Message);
					}
				}
				children.AddRange(c.Heroes.Where((Hero h) => h.IsAlive && h.IsChild));
				foreach (Hero ch in children)
				{
					try
					{
						ch.Clan = Clan.PlayerClan;
					}
					catch
					{
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("seizing the house failed: " + e);
			}
		}

		private static string End(Clan c, List<Settlement> fiefs, List<Hero> children)
		{
			string name = c.Name.ToString();
			try
			{
				if (!c.IsEliminated && !c.Heroes.Any((Hero h) => h.IsAlive && !h.IsChild))
				{
					DestroyClanAction.Apply(c);
				}
			}
			catch (Exception e)
			{
				Log.Write("ending the house failed: " + e.Message);
			}
			Log.Write(name + " was wiped out; " + fiefs.Count + " fief(s) to you, " + children.Count + " child(ren) to your court");
			Store.AddDeed(Standing.Date() + "  " + name + " is no more.");
			StringBuilder sb = new StringBuilder();
			sb.Append(name).Append(" is no more.");
			List<Settlement> kept = fiefs.Where((Settlement s) => s.OwnerClan == Clan.PlayerClan).ToList();
			if (kept.Count > 0)
			{
				sb.Append(" ").Append(string.Join(", ", kept.Select((Settlement s) => s.Name.ToString()).ToArray())).Append((kept.Count == 1) ? " is" : " are").Append(" yours now.");
			}
			if (children.Count > 0)
			{
				sb.Append(" Their children will be raised at your court, by the people who killed their parents.");
			}
			return sb.ToString();
		}

		// ------------------------------------------------------------------
		// the court

		internal static string Summary()
		{
			string[] s = Scheme();
			if (s == null)
			{
				return "";
			}
			Clan c = Clan.FindFirst((Clan x) => ((MBObjectBase)x).StringId == s[0]);
			Settlement venue = Settlement.Find(s[2]);
			string house = (c != null) ? c.Name.ToString() : "a house";
			string where = (venue != null) ? venue.Name.ToString() : "your hall";
			if (s[4] == "laid")
			{
				return house + " are waiting at your table in " + where + ".\n";
			}
			int ready;
			int.TryParse(s[3], out ready);
			return "A feast for " + house + " is being prepared at " + where + " (" + Math.Max(0, ready - CourtBehavior.Today()) + " days; " + LeakChance() + "% a day it leaks).\n";
		}

		internal static string Attention()
		{
			if (!Cfg.Ravens)
			{
				return null;
			}
			string[] s = Scheme();
			if (s == null || s[4] != "laid")
			{
				return null;
			}
			Settlement venue = Settlement.Find(s[2]);
			return "Your guests are waiting at " + ((venue != null) ? venue.Name.ToString() : "your hall") + ".";
		}
	}
}
