using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Ravens.
	//
	// Letters between houses: a marriage offered, a feast, word from kin who
	// married away, a feast to end a war. Most of them mean what they say.
	// Some do not - and whether a house would stoop to it depends on how much
	// they hate you, what sort of lord they are, and what sort you have
	// shown yourself to be. A feared lord gets fewer honest invitations; an
	// honourable one is harder to lure, because nobody believes they would
	// do the same.
	//
	// A letter accepted is an appointment: be at their hall on the day, and
	// go in to the feast. If it was false, the doors close behind you.
	internal static class Ravens
	{
		internal const string Marriage = "marriage";
		internal const string Feast = "feast";
		internal const string Kin = "kin";
		internal const string Peace = "peace";

		private const string LetterKey = "rv:letter";
		private const string ApptKey = "rv:appt";
		private const string FightKey = "rv:fight";
		private const string ResultKey = "rv:result";
		internal const string SaltKey = "rv:salt";
		internal const string VengeancePrefix = "rv:veng:";

		// ------------------------------------------------------------------
		// who might lie to you

		// The chance a letter from this lord is false, 2 to 40.
		internal static int TrapChance(Hero from)
		{
			if (from == null)
			{
				return 0;
			}
			if (Store.Get(VengeancePrefix + ((MBObjectBase)from).StringId) == "1")
			{
				return 100;
			}
			int chance = 5;
			float rel = from.GetRelationWithPlayer();
			if (rel <= -20f)
			{
				chance += Math.Min(15, (int)(-rel - 20f) / 3 + 5);
			}
			chance += Store.Dread / 10;
			chance -= (Store.Honour - 50) / 5;
			try
			{
				int honor = from.GetTraitLevel(DefaultTraits.Honor);
				chance += (honor < 0) ? 10 : ((honor > 0) ? -10 : 0);
			}
			catch
			{
			}
			return Math.Max(2, Math.Min(40, chance));
		}

		// What your people make of it. Right more often the more cunning you
		// are and the more white cloaks stand with you - and when they are
		// wrong, they are wrong in either direction.
		internal static string Hint(bool isFalse)
		{
			int sharp = 0;
			try
			{
				sharp = Hero.MainHero.GetSkillValue(DefaultSkills.Roguery) / 3 + 10 * Guard.Ready().Count;
			}
			catch
			{
			}
			bool right = MBRandom.RandomInt(100) < Math.Min(60, sharp);
			bool warn = right ? isFalse : !isFalse;
			return warn
				? "Your people do not like it. The seal is right; something else is not."
				: "Your people read it twice and see nothing amiss.";
		}

		// ------------------------------------------------------------------
		// marriages

		internal static bool Suitable(Hero h)
		{
			try
			{
				return h != null && h.IsAlive && !h.IsChild && Campaign.Current.Models.MarriageModel.IsSuitableForMarriage(h) && !Guard.IsSworn(h);
			}
			catch
			{
				return false;
			}
		}

		// Your unmarried blood, and you.
		internal static List<Hero> OurUnwed()
		{
			List<Hero> list = new List<Hero>();
			try
			{
				if (Suitable(Hero.MainHero))
				{
					list.Add(Hero.MainHero);
				}
				foreach (Hero h in Clan.PlayerClan.Heroes)
				{
					if (h != Hero.MainHero && Suitable(h) && Succession.IsBlood(h, Hero.MainHero))
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

		internal static List<Hero> TheirUnwed(Clan c, Hero forWhom)
		{
			List<Hero> list = new List<Hero>();
			try
			{
				foreach (Hero h in c.Heroes)
				{
					if (Suitable(h) && (forWhom == null || (h.IsFemale != forWhom.IsFemale && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(forWhom, h))))
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

		// Will their house say yes.
		internal static int MarriageChance(Hero ours, Hero theirs)
		{
			Hero head = (theirs.Clan != null && theirs.Clan.Leader != null) ? theirs.Clan.Leader : theirs;
			int chance = 30 + (int)head.GetRelationWithPlayer() / 2 + (Store.Honour - 50) / 3;
			try
			{
				chance += (Clan.PlayerClan.Tier - theirs.Clan.Tier) * 5;
			}
			catch
			{
			}
			if (Store.Get(SaltKey) == "1")
			{
				chance -= 30;
			}
			return Math.Max(3, Math.Min(95, chance));
		}

		// ------------------------------------------------------------------
		// letters to you

		internal static bool Waiting
		{
			get
			{
				return !string.IsNullOrEmpty(Store.Get(LetterKey));
			}
		}

		internal static bool Appointment
		{
			get
			{
				return !string.IsNullOrEmpty(Store.Get(ApptKey));
			}
		}

		// Checked daily; a letter at most once a week.
		internal static void Daily()
		{
			try
			{
				if (!Cfg.Ravens || !Store.Initialized || Hero.MainHero == null || Clan.PlayerClan == null)
				{
					return;
				}
				int today = CourtBehavior.Today();
				Lapse(today);
				if (today - Store.GetI("rv:roll", -9999) < 7)
				{
					return;
				}
				Store.SetI("rv:roll", today);
				if (Waiting || Appointment || Clan.PlayerClan.Tier < 2)
				{
					return;
				}
				if (MBRandom.RandomInt(100) >= Cfg.RavensLetterChance)
				{
					return;
				}
				Arrive(null, false);
			}
			catch (Exception e)
			{
				Log.Once("rvdaily", "the ravens' tick failed: " + e.Message);
			}
		}

		// A letter arrives. kind null = whatever suits the sender; force =
		// the console making it false.
		internal static bool Arrive(string kind, bool forceFalse)
		{
			Hero from = Sender();
			if (from == null)
			{
				Log.Write("no house had anything to write to you");
				return false;
			}
			Settlement venue = Seat(from.Clan);
			if (venue == null)
			{
				return false;
			}
			string ours = "";
			string theirs = "";
			if (kind == null)
			{
				bool war = from.MapFaction != null && FactionManager.IsAtWarAgainstFaction(from.MapFaction, Clan.PlayerClan.MapFaction);
				Hero a;
				Hero b;
				if (Pair(from.Clan, out a, out b) && MBRandom.RandomInt(100) < 35)
				{
					kind = Marriage;
				}
				else if (KinThere(from.Clan) != null && MBRandom.RandomInt(100) < 25)
				{
					kind = Kin;
				}
				else if ((war || from.GetRelationWithPlayer() < 0f) && MBRandom.RandomInt(100) < 40)
				{
					kind = Peace;
				}
				else
				{
					kind = Feast;
				}
			}
			if (kind == Marriage)
			{
				Hero a;
				Hero b;
				if (!Pair(from.Clan, out a, out b))
				{
					kind = Feast;
				}
				else
				{
					ours = ((MBObjectBase)a).StringId;
					theirs = ((MBObjectBase)b).StringId;
				}
			}
			if (kind == Kin && KinThere(from.Clan) == null)
			{
				kind = Feast;
			}
			bool isFalse = forceFalse || MBRandom.RandomInt(100) < TrapChance(from);
			Store.Set(LetterKey, kind + "|" + ((MBObjectBase)from).StringId + "|" + ours + "|" + theirs + "|" + ((MBObjectBase)venue).StringId + "|" + (isFalse ? "1" : "0") + "|" + CourtBehavior.Today());
			Log.Write("a raven from " + from.Name + ": " + kind + (isFalse ? " (false)" : ""));
			Read();
			return true;
		}

		// Who writes: someone sworn to vengeance first, then any house head
		// with a hall to host in.
		private static Hero Sender()
		{
			List<Hero> vengeful = Store.Keys(VengeancePrefix).Select((string k) => Law.Find(k.Substring(VengeancePrefix.Length)))
				.Where((Hero h) => h != null && !h.IsPrisoner && Seat(h.Clan) != null).ToList();
			if (vengeful.Count > 0 && MBRandom.RandomInt(100) < 50)
			{
				return vengeful[MBRandom.RandomInt(vengeful.Count)];
			}
			List<Hero> heads = Clan.All.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction
				&& c.Leader != null && c.Leader.IsAlive && !c.Leader.IsPrisoner && !c.Leader.IsChild && Seat(c) != null)
				.Select((Clan c) => c.Leader).ToList();
			return (heads.Count == 0) ? null : heads[MBRandom.RandomInt(heads.Count)];
		}

		internal static Settlement Seat(Clan c)
		{
			if (c == null)
			{
				return null;
			}
			if (c.HomeSettlement != null && (c.HomeSettlement.IsTown || c.HomeSettlement.IsCastle) && c.HomeSettlement.OwnerClan == c)
			{
				return c.HomeSettlement;
			}
			return c.Settlements.FirstOrDefault((Settlement s) => s.IsTown || s.IsCastle);
		}

		internal static bool Pair(Clan c, out Hero ours, out Hero theirs)
		{
			ours = null;
			theirs = null;
			foreach (Hero a in OurUnwed())
			{
				List<Hero> b = TheirUnwed(c, a);
				if (b.Count > 0)
				{
					ours = a;
					theirs = b[MBRandom.RandomInt(b.Count)];
					return true;
				}
			}
			return false;
		}

		// Blood of yours living in that house.
		internal static Hero KinThere(Clan c)
		{
			try
			{
				return c.Heroes.FirstOrDefault((Hero h) => h.IsAlive && h != Hero.MainHero && Succession.IsBlood(h, Hero.MainHero));
			}
			catch
			{
				return null;
			}
		}

		// Put the letter in front of the player.
		internal static void Read()
		{
			try
			{
				string[] p = (Store.Get(LetterKey) ?? "").Split('|');
				if (p.Length < 7)
				{
					return;
				}
				Hero from = Law.Find(p[1]);
				Settlement venue = Settlement.Find(p[4]);
				if (from == null || venue == null)
				{
					Store.Set(LetterKey, null);
					return;
				}
				bool isFalse = p[5] == "1";
				Hero ours = Law.Find(p[2]);
				Hero theirs = Law.Find(p[3]);
				string body;
				switch (p[0])
				{
				case Marriage:
					body = from.Name + " of " + from.Clan.Name + " proposes a match: " + ((theirs != null) ? theirs.Name.ToString() : "one of theirs") +
						" for " + ((ours == Hero.MainHero) ? "you" : ((ours != null) ? ours.Name.ToString() : "one of yours")) +
						". The wedding would be held at " + venue.Name + ", and all of your house is welcome.";
					break;
				case Kin:
				{
					Hero k = KinThere(from.Clan);
					body = "A raven from " + venue.Name + ", in the hand of " + from.Name + ": " + ((k != null) ? k.Name.ToString() : "your kin") +
						" is unwell, and asks for you. Come, and bring whoever you like.";
					break;
				}
				case Peace:
					body = from.Name + " writes that there has been enough blood between your houses, and invites you to " + venue.Name +
						" to break bread and put an end to it.";
					break;
				default:
					body = from.Name + " invites you to a feast at " + venue.Name + ", in honour of nothing in particular, which is usually the best kind.";
					break;
				}
				body += "\n\nThe feast would be in " + Cfg.RavensFeastDays + " days.\n\n" + Hint(isFalse);
				List<InquiryElement> els = new List<InquiryElement>();
				els.Add(new InquiryElement("go", "Accept, and ride to " + venue.Name, null, true, "Be there on the day and go in to the feast."));
				els.Add(new InquiryElement("armed", "Accept - and come armoured, with your white cloaks", null, true,
					"If it is a trap, you and your sworn knights walk in dressed for it. If it is not, they will see the armour and take it as the insult it is."));
				els.Add(new InquiryElement("decline", "Decline politely", null, true, "They will not like it much."));
				els.Add(new InquiryElement("burn", "Burn it", null, true, "They will hear that you did."));
				Inquiry.Select("A Raven", body, els, 1, 1, "So be it", null,
					delegate(List<InquiryElement> chosen)
					{
						Answer((chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : "decline");
					},
					delegate
					{
						Answer("decline");
					});
			}
			catch (Exception e)
			{
				Log.Write("reading the letter failed: " + e.Message);
			}
		}

		private static void Answer(string pick)
		{
			string[] p = (Store.Get(LetterKey) ?? "").Split('|');
			Store.Set(LetterKey, null);
			if (p.Length < 7)
			{
				return;
			}
			Hero from = Law.Find(p[1]);
			if (from == null)
			{
				return;
			}
			switch (pick)
			{
			case "go":
			case "armed":
			{
				int due = CourtBehavior.Today() + Cfg.RavensFeastDays;
				// kind | host | venue | due | false | armed | ours | theirs
				Store.Set(ApptKey, p[0] + "|" + p[1] + "|" + p[4] + "|" + due + "|" + p[5] + "|" + ((pick == "armed") ? "1" : "0") + "|" + p[2] + "|" + p[3]);
				Settlement venue = Settlement.Find(p[4]);
				Flow.Notify("You will be at " + ((venue != null) ? venue.Name.ToString() : "their hall") + " in " + Cfg.RavensFeastDays + " days.");
				break;
			}
			case "burn":
				ChangeRelationAction.ApplyPlayerRelation(from, -15, false, false);
				Standing.Change(0, 1, "Burned a letter from " + from.Name);
				break;
			default:
				ChangeRelationAction.ApplyPlayerRelation(from, -5, false, false);
				break;
			}
		}

		// A feast you did not come to.
		private static void Lapse(int today)
		{
			string[] a = Appt();
			int due;
			if (a == null || !int.TryParse(a[3], out due) || today <= due + 3)
			{
				return;
			}
			Store.Set(ApptKey, null);
			Hero host = Law.Find(a[1]);
			if (host != null && a[4] != "1")
			{
				ChangeRelationAction.ApplyPlayerRelation(host, -10, false, false);
			}
			Flow.Notify("You did not come to the feast" + ((host != null) ? (" at " + host.Name + "'s table") : "") + ".");
		}

		internal static string[] Appt()
		{
			string[] a = (Store.Get(ApptKey) ?? "").Split('|');
			return (a.Length >= 8) ? a : null;
		}

		internal static bool FeastHere()
		{
			string[] a = Appt();
			int due;
			return a != null && Settlement.CurrentSettlement != null && ((MBObjectBase)Settlement.CurrentSettlement).StringId == a[2]
				&& int.TryParse(a[3], out due) && CourtBehavior.Today() >= due;
		}

		internal static void FeastNow()
		{
			string[] a = Appt();
			if (a != null)
			{
				a[3] = CourtBehavior.Today().ToString();
				Store.Set(ApptKey, string.Join("|", a));
			}
		}

		// ------------------------------------------------------------------
		// going in

		internal static void GoIn()
		{
			try
			{
				string[] a = Appt();
				if (a == null)
				{
					return;
				}
				Hero host = Law.Find(a[1]);
				Settlement venue = Settlement.Find(a[2]);
				bool isFalse = a[4] == "1";
				bool armed = a[5] == "1";
				if (host == null || venue == null)
				{
					Store.Set(ApptKey, null);
					Flow.Notify("There is nobody here to feast with.");
					return;
				}
				if (!isFalse)
				{
					Store.Set(ApptKey, null);
					Honest(a, host, venue, armed);
					return;
				}
				Trap(a, host, venue, armed);
			}
			catch (Exception e)
			{
				Log.Write("going in to the feast failed: " + e);
			}
		}

		private static void Honest(string[] a, Hero host, Settlement venue, bool armed)
		{
			string kind = a[0];
			string text;
			if (armed)
			{
				ChangeRelationAction.ApplyPlayerRelation(host, -10, true, false);
				text = "You came to " + host.Name + "'s table in armour, with your white cloaks behind you. There was no knife in the hall but the ones for the meat. Everybody noticed which of you had come expecting otherwise.";
			}
			else
			{
				ChangeRelationAction.ApplyPlayerRelation(host, 10, true, false);
				Standing.Change(1, 0, "Feasted at " + venue.Name);
				text = "The feast at " + venue.Name + " was a feast. Nobody died, and a few things were said that could not have been written down.";
			}
			if (kind == Marriage)
			{
				Hero ours = Law.Find(a[6]);
				Hero theirs = Law.Find(a[7]);
				if (ours != null && theirs != null && Suitable(ours) && Suitable(theirs))
				{
					MarriageAction.Apply(ours, theirs, true);
					Store.AddDeed(Standing.Date() + "  " + ours.Name + " married " + theirs.Name + " at " + venue.Name + ".");
					text += "\n\n" + ours.Name + " and " + theirs.Name + " are married.";
				}
			}
			else if (kind == Peace && host.MapFaction != null && Clan.PlayerClan.MapFaction != null
				&& FactionManager.IsAtWarAgainstFaction(host.MapFaction, Clan.PlayerClan.MapFaction) && Succession.Rules())
			{
				try
				{
					MakePeaceAction.Apply(host.MapFaction, Clan.PlayerClan.MapFaction);
					text += "\n\nAnd there is peace between your realms.";
				}
				catch (Exception e)
				{
					Log.Write("the peace would not take: " + e.Message);
				}
			}
			Popup("The Feast", text);
		}

		// The doors close.
		private static void Trap(string[] a, Hero host, Settlement venue, bool armed)
		{
			// You, and whoever rode in with you.
			List<HallSeat> ours = new List<HallSeat>();
			foreach (TroopRosterElement e in MobileParty.MainParty.MemberRoster.GetTroopRoster())
			{
				if (ours.Count >= 8)
				{
					break;
				}
				if (e.Character != null && e.Character.IsHero && e.Character != CharacterObject.PlayerCharacter && e.Character.HeroObject != null && !e.Character.HeroObject.IsWounded)
				{
					bool knight = Guard.IsSworn(e.Character.HeroObject);
					ours.Add(new HallSeat(e.Character, !(armed && knight)));
				}
			}
			// Their men, in armour.
			List<HallSeat> theirs = new List<HallSeat>();
			foreach (Hero h in host.Clan.Heroes.Where((Hero x) => x.IsAlive && !x.IsChild && !x.IsPrisoner && x != Hero.MainHero).Take(4))
			{
				theirs.Add(new HallSeat(h.CharacterObject, false));
			}
			List<CharacterObject> elite = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Culture == host.Culture
				&& x.Occupation == Occupation.Soldier && x.Tier >= 3).ToList();
			if (elite.Count == 0)
			{
				elite = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Occupation == Occupation.Soldier && x.Tier >= 3).ToList();
			}
			int want = Math.Min(16, 8 + ours.Count * 2);
			while (theirs.Count < want && elite.Count > 0)
			{
				theirs.Add(new HallSeat(elite[MBRandom.RandomInt(elite.Count)], false));
			}
			Store.Set(FightKey, "trap|" + ((MBObjectBase)venue).StringId + "|" + ((MBObjectBase)host).StringId + "|" +
				Ids(ours.Select((HallSeat x) => x.Who)) + "|" + Ids(theirs.Select((HallSeat x) => x.Who)));
			Store.Set(ApptKey, null);
			Store.AddDeed(Standing.Date() + "  " + host.Name + " closed the doors of " + venue.Name + " on you.");
			string why;
			bool opened = HallFight.Open(venue, ours, !armed, theirs, true, delegate(bool won, List<CharacterObject> fallen)
			{
				Store.Set(ResultKey, (won ? "1" : "0") + "|" + Ids(fallen));
			}, out why);
			if (!opened)
			{
				Log.Write("the trap could not be staged: " + why);
				// Decided without the hall, on strength.
				bool won = MBRandom.RandomInt(100) < 35 + (armed ? 25 : 0) + 5 * ours.Count;
				Store.Set(ResultKey, (won ? "1" : "0") + "|" + (won ? "" : ((MBObjectBase)CharacterObject.PlayerCharacter).StringId));
			}
		}

		// ------------------------------------------------------------------
		// after the hall

		// Off the arena, never in it: both kinds of hall fight end here.
		internal static void Settle()
		{
			try
			{
				if (!Cfg.Ravens || !Store.Initialized || InMission())
				{
					return;
				}
				string result = Store.Get(ResultKey);
				string fight = Store.Get(FightKey);
				if (string.IsNullOrEmpty(result) || string.IsNullOrEmpty(fight))
				{
					return;
				}
				Store.Set(ResultKey, null);
				Store.Set(FightKey, null);
				string[] r = result.Split('|');
				bool won = r[0] == "1";
				List<CharacterObject> fallen = Chars((r.Length > 1) ? r[1] : "");
				string[] f = fight.Split('|');
				if (f[0] == "trap")
				{
					AfterTrap(f, won, fallen);
				}
				else if (f[0] == "test")
				{
					Log.Write("hall test finished at " + ((f.Length > 1) ? f[1] : "?") + (won ? ": your side won" : ": your side lost"));
				}
				else if (f[0] == "scheme")
				{
					Treachery.After(f, won, fallen);
				}
			}
			catch (Exception e)
			{
				Log.Write("settling the hall failed: " + e);
			}
		}

		private static void AfterTrap(string[] f, bool won, List<CharacterObject> fallen)
		{
			Settlement venue = Settlement.Find(f[1]);
			Hero host = Law.Find(f[2]);
			List<CharacterObject> ours = Chars(f[3]);
			List<CharacterObject> theirs = Chars(f[4]);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			List<Hero> dead = new List<Hero>();
			foreach (CharacterObject c in fallen)
			{
				Hero h = (c != null && c.IsHero) ? c.HeroObject : null;
				if (h != null && h.IsAlive && h != Hero.MainHero && MBRandom.RandomInt(100) < Cfg.TrialDeathChance)
				{
					dead.Add(h);
				}
			}
			Law.Quiet = true;
			try
			{
				foreach (Hero h in dead)
				{
					try
					{
						KillCharacterAction.ApplyByMurder(h, ours.Contains(h.CharacterObject) ? host : Hero.MainHero, true);
						sb.Append(h.Name).Append(" died in the hall.\n");
					}
					catch
					{
					}
				}
			}
			finally
			{
				Law.Quiet = false;
			}
			if (won)
			{
				Standing.Change(0, 10, "Cut your way out of " + ((venue != null) ? venue.Name.ToString() : "a hall"));
				if (host != null && host.IsAlive)
				{
					Law.Record(Law.GuestRight, host, Hero.MainHero, null, false);
				}
				Popup("The Doors Were Barred",
					"The musicians stopped, and the doors were barred, and " + ((host != null) ? host.Name.ToString() : "your host") + "'s men came in armoured.\n\n" +
					"You came out anyway.\n\n" + sb + "\nGuest right was broken under that roof, and the King's Justice can hear it.");
				return;
			}
			// You fell. In their hall, that is the end of you.
			Popup("The Rains of Castamere",
				"The musicians stopped, and the doors were barred, and " + ((host != null) ? host.Name.ToString() : "your host") + "'s men came in armoured.\n\n" +
				sb + "\nYou did not come out.");
			Store.AddDeed(Standing.Date() + "  Murdered at the table of " + ((host != null) ? host.Name.ToString() : "a host") + ".");
			try
			{
				KillCharacterAction.ApplyByMurder(Hero.MainHero, host, true);
			}
			catch (Exception e)
			{
				Log.Write("the player's death in the hall failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// letters you send

		internal static void Propose()
		{
			List<Hero> ours = OurUnwed();
			if (ours.Count == 0)
			{
				Flow.Notify("Nobody of your blood is free to marry.");
				return;
			}
			List<InquiryElement> els = ours.Select((Hero h) => new InquiryElement(h, h.Name + "  (" + (int)h.Age + ")" + ((h == Hero.MainHero) ? "   - yourself" : ""), null, true, "")).ToList();
			Inquiry.Select("A Match", "Whose hand do you offer?", els, 1, 1, "Them", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Hero a = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
					if (a != null)
					{
						ProposeTo(a);
					}
				});
		}

		private static void ProposeTo(Hero a)
		{
			List<Hero> can = Clan.All.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction)
				.SelectMany((Clan c) => TheirUnwed(c, a)).Take(60).ToList();
			if (can.Count == 0)
			{
				Flow.Notify("No house has anyone suitable for " + a.Name + ".");
				return;
			}
			List<InquiryElement> els = can.OrderByDescending((Hero h) => MarriageChance(a, h)).Select((Hero h) => new InquiryElement(h,
				h.Name + "  (" + (int)h.Age + ", " + h.Clan.Name + ")   - " + MarriageChance(a, h) + "% they accept", null, true, "")).ToList();
			Inquiry.Select("A Match for " + a.Name, "To whom?", els, 1, 1, "Send the raven", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Hero b = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
					if (b == null)
					{
						return;
					}
					Hero head = b.Clan.Leader ?? b;
					if (MBRandom.RandomInt(100) >= MarriageChance(a, b))
					{
						ChangeRelationAction.ApplyPlayerRelation(head, -2, false, false);
						Popup("The Answer", head.Name + " thanks you for the honour, and declines it.");
						return;
					}
					Settlement venue = Seat(b.Clan);
					if (venue == null)
					{
						MarriageAction.Apply(a, b, true);
						Popup("The Answer", head.Name + " accepts. " + a.Name + " and " + b.Name + " are married.");
						return;
					}
					// Accepted - and the wedding is at their hall. Which is
					// where a false yes would want you.
					bool isFalse = MBRandom.RandomInt(100) < TrapChance(head);
					int due = CourtBehavior.Today() + Cfg.RavensFeastDays;
					Store.Set(ApptKey, Marriage + "|" + ((MBObjectBase)head).StringId + "|" + ((MBObjectBase)venue).StringId + "|" + due + "|" + (isFalse ? "1" : "0") + "|0|" +
						((MBObjectBase)a).StringId + "|" + ((MBObjectBase)b).StringId);
					Log.Write("match accepted: " + a.Name + " and " + b.Name + " at " + venue.Name + (isFalse ? " (false)" : ""));
					Popup("The Answer", head.Name + " accepts, gladly. The wedding will be at " + venue.Name + " in " + Cfg.RavensFeastDays + " days, and all your house is welcome.\n\n" + Hint(isFalse));
				});
		}

		// An honest feast at your own hall.
		internal static void Invite()
		{
			List<Clan> can = Clan.All.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && !c.IsBanditFaction && c.Leader != null && c.Leader.IsAlive)
				.OrderByDescending((Clan c) => c.Leader.GetRelationWithPlayer()).Take(40).ToList();
			int cost = Cfg.RavensFeastCost;
			List<InquiryElement> els = can.Select((Clan c) => new InquiryElement(c, c.Name + "  (" + c.Leader.Name + ", relation " + (int)c.Leader.GetRelationWithPlayer() + ")", null,
				Hero.MainHero.Gold >= cost, "")).ToList();
			Inquiry.Select("A Feast", "Whom do you feast? It costs " + cost.ToString("N0") + "." + ((Store.Get(SaltKey) == "1") ? "\n\nNobody has trusted your bread and salt since the last time." : ""),
				els, 1, 1, "Send the raven", "Not today",
				delegate(List<InquiryElement> chosen)
				{
					Clan c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Clan) : null;
					if (c == null)
					{
						return;
					}
					Hero.MainHero.ChangeHeroGold(-cost);
					int chance = 50 + (int)c.Leader.GetRelationWithPlayer() / 2 + (Store.Honour - 50) / 2 - Store.Dread / 4 - ((Store.Get(SaltKey) == "1") ? 30 : 0);
					if (MBRandom.RandomInt(100) < Math.Max(5, Math.Min(95, chance)))
					{
						ChangeRelationAction.ApplyPlayerRelation(c.Leader, 10, true, false);
						Standing.Change(1, 0, "Feasted " + c.Name);
						Popup("The Feast", c.Name + " came, ate, drank, and went home. It was a good night, and they will remember it.");
					}
					else
					{
						Popup("The Feast", c.Name + " sent their regrets. The food was eaten anyway.");
					}
				});
		}

		// ------------------------------------------------------------------
		// the court

		internal static string Summary()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			string[] a = Appt();
			if (a != null)
			{
				Hero host = Law.Find(a[1]);
				Settlement venue = Settlement.Find(a[2]);
				int due;
				int.TryParse(a[3], out due);
				sb.Append("You are expected at ").Append((venue != null) ? venue.Name.ToString() : "a hall")
				  .Append((host != null) ? (", at " + host.Name + "'s table") : "")
				  .Append((due > CourtBehavior.Today()) ? (", in " + (due - CourtBehavior.Today()) + " days.\n") : ", now.\n");
			}
			if (Waiting)
			{
				sb.Append("A letter is waiting to be read.\n");
			}
			if (Store.Get(SaltKey) == "1")
			{
				sb.Append("Since the last feast you held, nobody quite trusts your bread and salt.\n");
			}
			int vengeful = Store.Keys(VengeancePrefix).Count((string k) => Law.Find(k.Substring(VengeancePrefix.Length)) != null);
			if (vengeful > 0)
			{
				sb.Append(vengeful).Append((vengeful == 1) ? " survivor has" : " survivors have").Append(" sworn to pay you back in kind.\n");
			}
			return sb.ToString();
		}

		internal static string Attention()
		{
			if (!Cfg.Ravens)
			{
				return null;
			}
			if (Waiting)
			{
				return "A raven waits to be read.";
			}
			string[] a = Appt();
			if (a != null)
			{
				Settlement venue = Settlement.Find(a[2]);
				return "You are expected at a feast at " + ((venue != null) ? venue.Name.ToString() : "a hall") + ".";
			}
			return null;
		}

		// ------------------------------------------------------------------
		// bits

		internal static string Ids(IEnumerable<CharacterObject> cs)
		{
			return string.Join(",", cs.Where((CharacterObject x) => x != null).Select((CharacterObject x) => ((MBObjectBase)x).StringId).ToArray());
		}

		internal static List<CharacterObject> Chars(string ids)
		{
			List<CharacterObject> list = new List<CharacterObject>();
			foreach (string id in (ids ?? "").Split(','))
			{
				if (id.Length == 0)
				{
					continue;
				}
				CharacterObject c = MBObjectManager.Instance.GetObject<CharacterObject>(id);
				if (c != null)
				{
					list.Add(c);
				}
			}
			return list;
		}

		internal static bool InMission()
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

		internal static void SetFight(string rec)
		{
			Store.Set(FightKey, rec);
		}

		internal static void SetResult(string rec)
		{
			Store.Set(ResultKey, rec);
		}

		private static readonly Queue<Action<Action>> _queue = new Queue<Action<Action>>();

		private static bool _showing;

		internal static void Reset()
		{
			_queue.Clear();
			_showing = false;
		}

		internal static void Popup(string title, string text)
		{
			_queue.Enqueue(delegate(Action done)
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
			catch
			{
				done();
			}
		}
	}
}
