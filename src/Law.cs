using System;
using System.Collections.Generic;
using System.Linq;
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
	// A crime somebody could be made to answer for.
	internal sealed class Charge
	{
		internal string Id = "";
		internal string Kind = "";
		internal string Accused = "";
		internal string Accuser = "";
		internal string Victim = "";
		internal int Day;
		// Invented by whoever brought it. A fabricated charge can still be
		// won - that is what makes it worth fabricating - but it can also be
		// found out.
		internal bool False;
		// open: waiting to be heard. summons: brought against the player and
		// not yet put to them. trial: a trial is arranged. closed.
		internal string State = "open";

		internal string Pack()
		{
			return string.Join("|", new string[8] { Kind, Accused, Accuser, Victim, Day.ToString(), False ? "1" : "0", State, "" });
		}

		internal static Charge Unpack(string id, string s)
		{
			string[] p = (s ?? "").Split('|');
			if (p.Length < 7)
			{
				return null;
			}
			int d;
			Charge c = new Charge();
			c.Id = id;
			c.Kind = p[0];
			c.Accused = p[1];
			c.Accuser = p[2];
			c.Victim = p[3];
			c.Day = int.TryParse(p[4], out d) ? d : 0;
			c.False = p[5] == "1";
			c.State = p[6];
			return c;
		}
	}

	// The King's Justice.
	//
	// Crimes are written down as they happen - a house walks out of your
	// realm, a lord of yours is murdered, a captive is put to the sword - and
	// whoever holds the court can hear them. A charge can also simply be
	// brought, true or not.
	//
	// The accused has the rights Westeros gives a noble: to be judged, to
	// demand trial by combat, and for the gravest charges to demand a trial of
	// seven. When the player fights, it is fought for real in a town's arena
	// (TrialFight). When they do not, it is decided on the fighters' skill.
	//
	// And none of it stops at the throne. Execute the wrong man, or make
	// enemies of the wrong houses, and the charge comes to you - from your
	// liege if you have one, and from your own lords if you do not.
	internal static class Law
	{
		internal const string Treason = "treason";
		internal const string Murder = "murder";
		internal const string Kinslaying = "kinslaying";
		internal const string Execution = "execution";
		internal const string Tyranny = "tyranny";
		internal const string Oathbreaking = "oathbreaking";
		internal const string GuestRight = "guestright";

		private const string Prefix = "lw:c:";
		private const string TrialKey = "lw:trial";
		private const string ResultKey = "lw:result";

		// Set while the crown's own sentence is being carried out, so an
		// execution ordered by the court is not recorded as a crime of the
		// court.
		private static bool _sentencing;

		// Set while a massacre is being counted, so each death in the hall is
		// not also charged as its own murder - the crime is guest right, and
		// it is charged once.
		internal static bool Quiet;

		// ------------------------------------------------------------------
		// the record

		internal static List<Charge> All()
		{
			List<Charge> list = new List<Charge>();
			foreach (string key in Store.Keys(Prefix))
			{
				Charge c = Charge.Unpack(key.Substring(Prefix.Length), Store.Get(key));
				if (c != null)
				{
					list.Add(c);
				}
			}
			return list.OrderBy((Charge c) => c.Day).ToList();
		}

		internal static Charge Get(string id)
		{
			return Charge.Unpack(id, Store.Get(Prefix + id));
		}

		internal static void Save(Charge c)
		{
			Store.Set(Prefix + c.Id, c.Pack());
		}

		private static void Close(Charge c, string how)
		{
			// Closed charges are dropped rather than kept: the chronicle is the
			// record of what came of them, and the key store travels in every
			// save.
			Store.Set(Prefix + c.Id, null);
			if (!string.IsNullOrEmpty(how))
			{
				Store.AddDeed(Standing.Date() + "  " + how);
				Log.Write("law: " + how);
			}
		}

		internal static Charge Record(string kind, Hero accused, Hero accuser, Hero victim, bool fabricated)
		{
			try
			{
				if (!Cfg.Law || accused == null || !accused.IsAlive)
				{
					return null;
				}
				// One charge of a kind per accused at a time.
				string aid = ((MBObjectBase)accused).StringId;
				if (All().Any((Charge x) => x.Accused == aid && x.Kind == kind))
				{
					return null;
				}
				int n = Store.GetI("lw:next", 1);
				Store.SetI("lw:next", n + 1);
				Charge c = new Charge();
				c.Id = n.ToString();
				c.Kind = kind;
				c.Accused = aid;
				c.Accuser = (accuser != null) ? ((MBObjectBase)accuser).StringId : "";
				c.Victim = (victim != null) ? ((MBObjectBase)victim).StringId : "";
				c.Day = CourtBehavior.Today();
				c.False = fabricated;
				c.State = (accused == Hero.MainHero) ? "summons" : "open";
				Save(c);
				Log.Write("law: " + Describe(c) + (fabricated ? " (fabricated)" : ""));
				return c;
			}
			catch (Exception e)
			{
				Log.Write("recording a charge failed: " + e.Message);
				return null;
			}
		}

		internal static int Severity(string kind)
		{
			switch (kind)
			{
			case Treason:
			case Kinslaying:
			case GuestRight:
				return 3;
			case Murder:
			case Execution:
			case Oathbreaking:
				return 2;
			default:
				return 1;
			}
		}

		internal static string KindName(string kind)
		{
			switch (kind)
			{
			case Treason:
				return "Treason";
			case Murder:
				return "Murder";
			case Kinslaying:
				return "Kinslaying";
			case Execution:
				return "The killing of a captive";
			case Tyranny:
				return "Tyranny";
			case Oathbreaking:
				return "Oathbreaking";
			case GuestRight:
				return "Breaking guest right";
			default:
				return kind;
			}
		}

		internal static string Describe(Charge c)
		{
			Hero accused = Find(c.Accused);
			Hero victim = Find(c.Victim) ?? FindDead(c.Victim);
			string who = (accused != null) ? accused.Name.ToString() : "someone now dead";
			string what = KindName(c.Kind);
			if (victim != null)
			{
				what += " of " + victim.Name;
			}
			return what + " - " + who;
		}

		// ------------------------------------------------------------------
		// crimes, as they happen

		internal static void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
		{
			try
			{
				if (!Cfg.Law || victim == null || killer == null || killer == victim || _sentencing || Quiet || !Store.Initialized)
				{
					return;
				}
				bool murder = detail == KillCharacterAction.KillCharacterActionDetail.Murdered;
				bool executed = detail == KillCharacterAction.KillCharacterActionDetail.Executed || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent;
				if (!murder && !executed)
				{
					return;
				}
				bool kin = Succession.IsBlood(killer, victim);
				string kind = kin ? Kinslaying : (murder ? Murder : Execution);

				// The player's own crime - but only if somebody with a court
				// over you can bring it: kin of the dead who kneel to the same
				// crown you do.
				if (killer == Hero.MainHero)
				{
					Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
					Hero accuser = (victim.Clan != null && victim.Clan.Leader != null && victim.Clan.Leader.IsAlive && victim.Clan.Leader != victim)
						? victim.Clan.Leader : null;
					if (realm != null && accuser != null && accuser.Clan.Kingdom == realm && victim.IsLord)
					{
						Record(kind, Hero.MainHero, accuser, victim, false);
					}
					return;
				}

				// Somebody else's: worth writing down if the dead were yours -
				// of your realm or of your blood.
				Kingdom yours = Succession.Realm();
				bool ours = (yours != null && victim.MapFaction == yours) || Succession.IsBlood(victim, Hero.MainHero);
				if (ours && killer.IsAlive && killer.IsLord)
				{
					Record(kind, killer, Hero.MainHero, victim, false);
				}
			}
			catch (Exception e)
			{
				Log.Once("lawkill", "recording a killing failed: " + e.Message);
			}
		}

		internal static void OnClanChangedKingdom(Clan clan, Kingdom old, Kingdom now, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool notify)
		{
			try
			{
				if (!Cfg.Law || clan == null || old == null || clan == Clan.PlayerClan || !Store.Initialized)
				{
					return;
				}
				if (old != Succession.Realm())
				{
					return;
				}
				if (detail != ChangeKingdomAction.ChangeKingdomActionDetail.JoinKingdomByDefection
					&& detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveWithRebellion
					&& detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveKingdom)
				{
					return;
				}
				// A house the crown itself put out is not a traitor for going.
				if (Store.Get("lw:exiled:" + ((MBObjectBase)clan).StringId) == "1")
				{
					Store.Set("lw:exiled:" + ((MBObjectBase)clan).StringId, null);
					return;
				}
				if (clan.Leader != null && clan.Leader.IsAlive)
				{
					Record(Treason, clan.Leader, Hero.MainHero, null, false);
				}
			}
			catch (Exception e)
			{
				Log.Once("lawtreason", "recording treason failed: " + e.Message);
			}
		}

		// Once a day: the dead are beyond justice, and a lord who hates you
		// may decide to do something about it.
		internal static void Daily()
		{
			try
			{
				if (!Cfg.Law || !Store.Initialized)
				{
					return;
				}
				foreach (Charge c in All())
				{
					if (Find(c.Accused) == null && c.State != "trial")
					{
						Close(c, null);
					}
				}
				int today = CourtBehavior.Today();
				if (today - Store.GetI("lw:roll", -9999) < 28)
				{
					return;
				}
				Store.SetI("lw:roll", today);
				Accusers();
			}
			catch (Exception e)
			{
				Log.Once("lawdaily", "the law's daily tick failed: " + e.Message);
			}
		}

		// Politics. A lord of your realm who hates you enough goes to whoever
		// can judge you, with a story. It does not have to be true.
		private static void Accusers()
		{
			if (!Cfg.LawAiAccusations || Clan.PlayerClan == null || Clan.PlayerClan.Kingdom == null)
			{
				return;
			}
			if (All().Any((Charge c) => c.Accused == ((MBObjectBase)Hero.MainHero).StringId))
			{
				return;
			}
			List<Hero> hateful = Clan.PlayerClan.Kingdom.Clans
				.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated && c.Leader != null && c.Leader.IsAlive)
				.Select((Clan c) => c.Leader)
				.Where((Hero h) => h.GetRelationWithPlayer() <= Cfg.LawHatred)
				.ToList();
			if (hateful.Count == 0 || MBRandom.RandomInt(100) >= Cfg.LawAccusationChance)
			{
				return;
			}
			Hero accuser = hateful[MBRandom.RandomInt(hateful.Count)];
			bool rules = Succession.Rules();
			Record(rules ? Tyranny : Treason, Hero.MainHero, accuser, null, true);
		}

		// ------------------------------------------------------------------
		// can it be heard

		internal static bool CanJudge(Charge c, out string why)
		{
			why = null;
			Hero accused = Find(c.Accused);
			if (accused == null)
			{
				why = "the accused is dead";
				return false;
			}
			if (accused == Hero.MainHero)
			{
				why = "you cannot sit in judgment of yourself";
				return false;
			}
			if (c.State == "trial")
			{
				why = "a trial is already arranged";
				return false;
			}
			if (Held(accused))
			{
				return true;
			}
			if (!Succession.Rules())
			{
				why = "only the crown can summon a lord who is not your prisoner";
				return false;
			}
			if (accused.MapFaction != Succession.Realm())
			{
				why = accused.Name + " is beyond your reach. Take them prisoner first";
				return false;
			}
			return true;
		}

		// Your prisoner: in your party, or in a cell of your house.
		internal static bool Held(Hero h)
		{
			try
			{
				if (h == null || !h.IsPrisoner || h.PartyBelongedToAsPrisoner == null)
				{
					return false;
				}
				PartyBase p = h.PartyBelongedToAsPrisoner;
				if (p == PartyBase.MainParty)
				{
					return true;
				}
				if (p.IsSettlement && p.Settlement.OwnerClan == Clan.PlayerClan)
				{
					return true;
				}
				return p.IsMobile && p.MobileParty.ActualClan == Clan.PlayerClan;
			}
			catch
			{
				return false;
			}
		}

		// ------------------------------------------------------------------
		// hearing a charge

		internal static void Hear(Charge c)
		{
			try
			{
				string why;
				if (!CanJudge(c, out why))
				{
					Flow.Notify("It cannot be heard: " + why + ".");
					return;
				}
				Hero accused = Find(c.Accused);
				// A lord who is not your prisoner may refuse to come.
				if (!Held(accused) && accused.GetRelationWithPlayer() <= -30 && MBRandom.RandomInt(100) < 40)
				{
					Refused(c, accused);
					return;
				}
				int sev = Severity(c.Kind);
				string text = accused.Name + " stands before you, accused of " + KindName(c.Kind).ToLowerInvariant() +
					(Victim(c) != null ? (" - the death of " + Victim(c).Name) : "") + ".\n\n" +
					(c.False
						? "You know where this charge came from, and so does anybody who looks at it closely."
						: "It is written down, and the witnesses are not hard to find.") +
					"\n\n" + accused.Name + " may ask the gods instead of you" + ((sev >= 2) ? ", and for a charge this grave may ask for seven." : ".");
				List<InquiryElement> els = new List<InquiryElement>();
				els.Add(new InquiryElement("guilty", "Guilty", null, true, "Pass sentence. The accused may still demand trial by combat - it is their right."));
				els.Add(new InquiryElement("innocent", "Innocent", null, true, "The charge is dismissed. " + ((Accuser(c) != null && Accuser(c) != Hero.MainHero) ? (Accuser(c).Name + " will not thank you.") : "")));
				els.Add(new InquiryElement("combat", "Let the gods decide: trial by combat", null, true, "One champion a side. Fight yourself - in the arena, for real - or name a champion."));
				if (sev >= 2)
				{
					els.Add(new InquiryElement("seven", "A trial of seven", null, true, "Seven a side, in the arena. You may stand among them."));
				}
				Inquiry.Select("The King's Justice", text, els, 1, 1, "So judged", "Not today",
					delegate(List<InquiryElement> chosen)
					{
						string pick = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : null;
						switch (pick)
						{
						case "guilty":
							Demand(c);
							break;
						case "innocent":
							Acquit(c);
							break;
						case "combat":
							Combat(c, true);
							break;
						case "seven":
							Seven(c, true);
							break;
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("hearing the charge failed: " + e.Message);
			}
		}

		// Summoned, and would not come.
		private static void Refused(Charge c, Hero accused)
		{
			Inquiry.Confirm("The Summons Refused",
				accused.Name + " will not come to be judged. The raven came back with a line about which of you the realm would rather follow.\n\n" +
				"You can declare " + accused.Name + "'s house outlaw, and they will leave your realm - as rebels. Or you can let it lie, and everybody will know you did.",
				"Declare them outlaw", "Let it lie",
				delegate
				{
					try
					{
						Clan clan = accused.Clan;
						if (clan != null && clan.Kingdom == Succession.Realm() && clan != Clan.PlayerClan)
						{
							Store.Set("lw:exiled:" + ((MBObjectBase)clan).StringId, "1");
							ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(clan, true);
						}
						Standing.Change(0, 3, "Declared " + accused.Name + " outlaw");
						Close(c, accused.Name + " refused the summons and was declared outlaw.");
					}
					catch (Exception e)
					{
						Log.Write("declaring them outlaw failed: " + e.Message);
					}
				},
				delegate
				{
					Standing.Change(0, -3, "Let " + accused.Name + " defy the summons");
				});
		}

		// Guilty - unless they ask the gods.
		private static void Demand(Charge c)
		{
			Hero accused = Find(c.Accused);
			if (accused == null)
			{
				return;
			}
			// A fighter who fancies their chances asks for it. So does a guilty
			// one with nothing left to lose.
			int chance = Math.Min(85, 20 + Rating(accused.CharacterObject) / 6 + (c.False ? 20 : 0));
			if (MBRandom.RandomInt(100) >= chance)
			{
				Sentence(c);
				return;
			}
			Inquiry.Confirm("Trial by Combat",
				accused.Name + " does not accept your judgment, and demands trial by combat. It is every noble's right, and every lord in the hall heard it asked.",
				"Grant it", "Deny it",
				delegate
				{
					Combat(c, true);
				},
				delegate
				{
					Standing.Change(-5, 3, "Denied " + accused.Name + " trial by combat");
					Sentence(c);
				});
		}

		private static void Acquit(Charge c)
		{
			Hero accused = Find(c.Accused);
			Hero accuser = Accuser(c);
			if (accused != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(accused, 10, false, false);
			}
			if (accuser != null && accuser != Hero.MainHero && accuser.IsAlive)
			{
				ChangeRelationAction.ApplyPlayerRelation(accuser, -10, false, false);
			}
			Close(c, ((accused != null) ? accused.Name.ToString() : "The accused") + " was found innocent of " + KindName(c.Kind).ToLowerInvariant() + ".");
		}

		// ------------------------------------------------------------------
		// sentence

		internal static void Sentence(Charge c)
		{
			try
			{
				Hero accused = Find(c.Accused);
				if (accused == null)
				{
					return;
				}
				int sev = Severity(c.Kind);
				int fine = FineFor(c, accused);
				List<InquiryElement> els = new List<InquiryElement>();
				els.Add(new InquiryElement("pardon", "A pardon", null, true, "Mercy. +3 Honour, -2 Dread, and " + accused.Name + " owes you their life."));
				els.Add(new InquiryElement("fine", "A fine of " + fine.ToString("N0"), null, true, "Paid into your treasury, as much as they have."));
				Settlement seat = Holding(accused);
				els.Add(new InquiryElement("seize", "Seize " + ((seat != null) ? seat.Name.ToString() : "a holding"), null, seat != null,
					(seat != null) ? ("It is yours. Their house will never forgive it. +3 Dread.") : "They hold nothing to take."));
				bool canBlack = accused.Clan == null || accused.Clan.Leader != accused || Heirs(accused.Clan, accused) > 0;
				els.Add(new InquiryElement("black", "Take the black", null, canBlack,
					canBlack ? "They go to the Wall and are never seen in the realm again. Merciful, and final." : "They are the last of their house. The Wall would end it."));
				bool canExile = accused.Clan != null && accused.Clan != Clan.PlayerClan && accused.Clan.Kingdom != null
					&& accused.Clan.Kingdom == Succession.Realm() && accused.Clan != accused.Clan.Kingdom.RulingClan;
				els.Add(new InquiryElement("exile", "Exile their house", null, canExile,
					canExile ? "Their house is put out of your realm. Everything they hold goes with them, and they will come back as enemies." : "Their house is not yours to put out."));
				els.Add(new InquiryElement("execute", "Death", null, true,
					"The sword." + (sev >= 2 ? " For a crime this grave the realm will call it justice." : " For a crime this small, the realm will call it something else.") +
					(c.False ? " And if the charge is ever shown for what it is, they will call it murder." : "")));
				Inquiry.Select("The Sentence",
					accused.Name + " is guilty of " + KindName(c.Kind).ToLowerInvariant() + ". What is the sentence?",
					els, 1, 1, "So be it", null,
					delegate(List<InquiryElement> chosen)
					{
						string pick = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : "pardon";
						Carry(c, accused, pick, fine, seat);
					},
					delegate
					{
						Carry(c, accused, "pardon", fine, seat);
					});
			}
			catch (Exception e)
			{
				Log.Write("sentencing failed: " + e.Message);
			}
		}

		private static void Carry(Charge c, Hero accused, string pick, int fine, Settlement seat)
		{
			try
			{
				int sev = Severity(c.Kind);
				Clan house = accused.Clan;
				string name = accused.Name.ToString();
				string outcome;
				switch (pick)
				{
				case "fine":
				{
					int paid = Math.Min(Math.Max(0, accused.Gold), fine);
					if (paid > 0)
					{
						GiveGoldAction.ApplyBetweenCharacters(accused, Hero.MainHero, paid, false);
					}
					ChangeRelationAction.ApplyPlayerRelation(accused, -10, false, false);
					outcome = name + " was fined " + paid.ToString("N0") + " for " + KindName(c.Kind).ToLowerInvariant() + ".";
					Just(c, 1);
					break;
				}
				case "seize":
					if (seat != null)
					{
						ChangeOwnerOfSettlementAction.ApplyByKingDecision(Hero.MainHero, seat);
					}
					ChangeRelationAction.ApplyPlayerRelation(accused, -30, true, false);
					Standing.Change(0, 3, "Seized " + ((seat != null) ? seat.Name.ToString() : "a holding") + " from " + name);
					outcome = name + " lost " + ((seat != null) ? seat.Name.ToString() : "their holding") + " for " + KindName(c.Kind).ToLowerInvariant() + ".";
					Just(c, 1);
					break;
				case "black":
					Black(accused);
					outcome = name + " took the black for " + KindName(c.Kind).ToLowerInvariant() + ".";
					Just(c, 2);
					break;
				case "exile":
					if (house != null && house.Kingdom != null)
					{
						Store.Set("lw:exiled:" + ((MBObjectBase)house).StringId, "1");
						ChangeKingdomAction.ApplyByLeaveKingdom(house, true);
					}
					ChangeRelationAction.ApplyPlayerRelation(accused, -40, true, false);
					Standing.Change(0, 2, "Exiled " + ((house != null) ? house.Name.ToString() : name));
					outcome = ((house != null) ? house.Name.ToString() : name) + " was exiled from the realm for the crime of " + name + ".";
					Just(c, 2);
					break;
				case "execute":
					_sentencing = true;
					try
					{
						KillCharacterAction.ApplyByExecution(accused, Hero.MainHero, true, true);
					}
					finally
					{
						_sentencing = false;
					}
					// The execution itself was charged at the usual rate by the
					// kill hook. For a grave crime proved, most of that comes
					// back: the realm calls it justice.
					if (!c.False && sev >= 2)
					{
						Standing.Change(Math.Max(0, Cfg.ExecuteHonour - 2), 0, "Justice done on " + name);
					}
					outcome = name + " was put to death for " + KindName(c.Kind).ToLowerInvariant() + ".";
					break;
				default:
					ChangeRelationAction.ApplyPlayerRelation(accused, 20, false, false);
					Standing.Change(3, -2, "Pardoned " + name);
					outcome = name + " was pardoned of " + KindName(c.Kind).ToLowerInvariant() + ".";
					break;
				}
				Close(c, outcome);
				Exposed(c, name, house);
				Flow.Notify(outcome);
			}
			catch (Exception e)
			{
				Log.Write("carrying out the sentence failed: " + e);
			}
		}

		// A real crime, fittingly punished, is justice.
		private static void Just(Charge c, int honour)
		{
			if (!c.False)
			{
				Standing.Change(honour, 0, "Justice done");
			}
		}

		// A charge you invented can come out.
		private static void Exposed(Charge c, string name, Clan house)
		{
			if (!c.False || Accuser(c) != Hero.MainHero || MBRandom.RandomInt(100) >= Cfg.LawExposed)
			{
				return;
			}
			Standing.Change(-8, 0, "The charge against " + name + " was shown to be false");
			if (house != null && house.Leader != null && house.Leader.IsAlive)
			{
				ChangeRelationAction.ApplyPlayerRelation(house.Leader, -30, true, false);
			}
			Kingdom realm = Succession.Realm();
			if (realm != null)
			{
				foreach (Clan cl in realm.Clans)
				{
					if (cl != null && cl != Clan.PlayerClan && cl.Leader != null && cl.Leader.IsAlive)
					{
						ChangeRelationAction.ApplyPlayerRelation(cl.Leader, -5, false, false);
					}
				}
			}
			Popup("The Truth Comes Out", "It is all over the realm that the charge against " + name + " was yours, and was false.\n\nNobody is saying it to your face.");
		}

		// To the Wall.
		private static void Black(Hero h)
		{
			try
			{
				Clan clan = h.Clan;
				if (clan != null && clan.Leader == h)
				{
					ChangeClanLeaderAction.ApplyWithoutSelectedNewLeader(clan);
				}
				string before = (h.EncyclopediaText != null) ? h.EncyclopediaText.ToString() : "";
				h.EncyclopediaText = new TextObject("{=!}" + before + ((before.Length > 0) ? "\n\n" : "") +
					"Convicted of a crime before the crown, " + h.FirstName + " took the black and went to the Wall. Brothers of the Night's Watch have no families, and hold no lands.", (Dictionary<string, object>)null);
				DisableHeroAction.Apply(h);
			}
			catch (Exception e)
			{
				Log.Write("sending them to the Wall failed: " + e.Message);
			}
		}

		private static int Heirs(Clan clan, Hero except)
		{
			return clan.Heroes.Count((Hero x) => x != null && x != except && x.IsAlive && !x.IsChild && x.IsLord);
		}

		private static Settlement Holding(Hero h)
		{
			try
			{
				if (h.Clan == null || h.Clan.Leader != h)
				{
					return null;
				}
				return h.Clan.Settlements.Where((Settlement s) => s.IsTown || s.IsCastle)
					.OrderBy((Settlement s) => (s == h.Clan.HomeSettlement) ? 1 : 0).FirstOrDefault();
			}
			catch
			{
				return null;
			}
		}

		private static int FineFor(Charge c, Hero accused)
		{
			return Cfg.LawFine * Severity(c.Kind) + Math.Max(0, accused.Gold) / 10;
		}

		// ------------------------------------------------------------------
		// the gods decide

		// One champion a side. prosecution = the player is bringing the charge.
		internal static void Combat(Charge c, bool prosecution)
		{
			try
			{
				Hero accused = Find(c.Accused);
				Hero other = prosecution ? accused : Champion(Accuser(c), c);
				if (other == null)
				{
					Flow.Notify("There is nobody to stand against you.");
					return;
				}
				Hero theirs = prosecution ? Champion(accused, c) : other;
				List<InquiryElement> els = new List<InquiryElement>();
				els.Add(new InquiryElement("you", "Fight yourself", null, true,
					"In the arena of a town, for real. Whoever falls has a " + Cfg.TrialDeathChance + "% chance of never getting up" + (Cfg.TrialPlayerCanDie ? " - you included." : ", though you will be carried out alive.")));
				foreach (Hero h in Champions())
				{
					els.Add(new InquiryElement(h, h.Name + "  (" + Rating(h.CharacterObject) + ")", null, true,
						"They fight for you. If they fall, there is a " + Cfg.TrialDeathChance + "% chance they die."));
				}
				Inquiry.Select("Trial by Combat",
					(prosecution
						? (theirs.Name + " will fight for " + accused.Name + ".")
						: (theirs.Name + " will fight for the accusation.")) +
					" Rated " + Rating(theirs.CharacterObject) + ".\n\nWho stands for you?",
					els, 1, 1, "Into the lists", null,
					delegate(List<InquiryElement> chosen)
					{
						object pick = (chosen != null && chosen.Count > 0) ? chosen[0].Identifier : "you";
						Hero ours = pick as Hero;
						Arrange(c, "combat", prosecution, (ours != null) ? new List<CharacterObject> { ours.CharacterObject } : new List<CharacterObject>(),
							new List<CharacterObject> { theirs.CharacterObject }, ours == null);
					},
					delegate
					{
						Arrange(c, "combat", prosecution, new List<CharacterObject>(), new List<CharacterObject> { theirs.CharacterObject }, true);
					});
			}
			catch (Exception e)
			{
				Log.Write("arranging the trial failed: " + e.Message);
			}
		}

		// Seven a side.
		internal static void Seven(Charge c, bool prosecution)
		{
			try
			{
				Hero accused = Find(c.Accused);
				Hero head = prosecution ? accused : (Accuser(c) ?? Champion(null, c));
				if (head == null)
				{
					Flow.Notify("There is nobody to stand against you.");
					return;
				}
				List<CharacterObject> theirs = SevenFor(head);
				List<CharacterObject> ours = OursSeven();
				string roll = string.Join(", ", theirs.Select((CharacterObject x) => x.Name.ToString()).ToArray());
				Inquiry.Confirm("A Trial of Seven",
					"Seven for " + (prosecution ? accused.Name.ToString() : "the accusation") + ": " + roll + ".\n\n" +
					"Stand among them yourself, and you have a day to find six who will stand with you - and only those who love you will. " +
					"Or send seven of your party without you: " +
					string.Join(", ", ours.Select((CharacterObject x) => x.Name.ToString()).ToArray()) + ".",
					"Stand among them", "Send seven without me",
					delegate
					{
						Gather(c, prosecution, theirs);
					},
					delegate
					{
						List<CharacterObject> sent = ours.ToList();
						sent.Insert(0, CharacterObject.PlayerCharacter);
						// Seven without you: the best six and one more.
						Arrange(c, "seven", prosecution, sent.Skip(1).Take(7).ToList(), theirs, false);
					});
			}
			catch (Exception e)
			{
				Log.Write("arranging the trial of seven failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the gathering
		//
		// "Will no knight stand for me?" A trial of seven is not called and
		// fought in the same breath. The lists take a day to raise, and in
		// that day you must find six who will stand beside you - which is a
		// question of who loves you, not who you pay. Whoever you cannot find
		// is made up at the end by your own soldiers and whatever glory
		// hunters turn up to watch.

		private const string GatherKey = "lw:gather";

		internal static bool Gathering
		{
			get
			{
				return !string.IsNullOrEmpty(Store.Get(GatherKey));
			}
		}

		// chargeId | p/d | ready hour | ours | theirs | asked
		private static string[] GatherRec()
		{
			string[] p = (Store.Get(GatherKey) ?? "").Split('|');
			return (p.Length >= 6) ? p : null;
		}

		private static void Gather(Charge c, bool prosecution, List<CharacterObject> theirs)
		{
			c.State = "trial";
			Save(c);
			int ready = (int)CampaignTime.Now.ToHours + Math.Max(1, Cfg.TrialSevenGatherHours);
			Store.Set(GatherKey, c.Id + "|" + (prosecution ? "p" : "d") + "|" + ready + "||" + Ids(theirs) + "|");
			Store.AddDeed(Standing.Date() + "  A trial of seven was called: " + Describe(c) + ".");
			Popup("A Trial of Seven",
				"The lists will be raised by this time tomorrow. Seven will stand against you: " +
				string.Join(", ", theirs.Select((CharacterObject x) => x.Name.ToString()).ToArray()) + ".\n\n" +
				"Six must stand with you. Go and find them - Court -> The King's Justice, or the town square. " +
				"Kin and sworn knights will answer. Friends may. Strangers will not.\n\n" +
				"Whoever you cannot find by tomorrow will be made up from your own soldiers, and from whatever glory hunters come to watch.");
		}

		internal static int Answered()
		{
			string[] p = GatherRec();
			return (p == null) ? 0 : Chars(p[3]).Count;
		}

		internal static int HoursLeft()
		{
			string[] p = GatherRec();
			int ready;
			return (p == null || !int.TryParse(p[2], out ready)) ? 0 : Math.Max(0, ready - (int)CampaignTime.Now.ToHours);
		}

		// The chance they say yes, 0 to 100.
		internal static int Willing(Hero h)
		{
			if (Guard.IsSworn(h))
			{
				return 100;
			}
			if (h.Clan == Clan.PlayerClan && Succession.IsBlood(h, Hero.MainHero))
			{
				return 100;
			}
			if (h.CompanionOf == Clan.PlayerClan)
			{
				return 90;
			}
			int rel = (int)h.GetRelationWithPlayer();
			if (rel < Cfg.TrialSevenFriend)
			{
				return 0;
			}
			return Math.Min(90, 40 + (rel - Cfg.TrialSevenFriend));
		}

		// Everyone who could be asked: your house, your companions, the lords
		// in this town, and the lords of your realm who like you enough.
		internal static List<Hero> Askable()
		{
			List<Hero> list = new List<Hero>();
			string[] p = GatherRec();
			if (p == null)
			{
				return list;
			}
			Charge c = Get(p[0]);
			HashSet<string> taken = new HashSet<string>(p[3].Split(',').Concat(p[4].Split(',')).Concat(p[5].Split(',')).Where((string x) => x.Length > 0));
			Action<Hero> add = delegate(Hero h)
			{
				if (h != null && h.IsAlive && !h.IsChild && !h.IsPrisoner && !h.IsWounded && h != Hero.MainHero && !list.Contains(h)
					&& !taken.Contains(((MBObjectBase)h).StringId)
					&& (c == null || (((MBObjectBase)h).StringId != c.Accused && ((MBObjectBase)h).StringId != c.Accuser)))
				{
					list.Add(h);
				}
			};
			try
			{
				foreach (Hero h in Clan.PlayerClan.Heroes)
				{
					add(h);
				}
				foreach (Hero h in Clan.PlayerClan.Companions)
				{
					add(h);
				}
				Settlement here = Settlement.CurrentSettlement;
				if (here != null)
				{
					foreach (Hero h in here.HeroesWithoutParty)
					{
						if (h.IsLord)
						{
							add(h);
						}
					}
					foreach (MobileParty mp in here.Parties)
					{
						if (mp != null && mp.LeaderHero != null && mp.LeaderHero.IsLord)
						{
							add(mp.LeaderHero);
						}
					}
				}
				Kingdom realm = Clan.PlayerClan.Kingdom;
				if (realm != null)
				{
					foreach (Clan cl in realm.Clans)
					{
						foreach (Hero h in cl.Heroes)
						{
							if (h.IsLord && h.GetRelationWithPlayer() >= Cfg.TrialSevenFriend)
							{
								add(h);
							}
						}
					}
				}
			}
			catch
			{
			}
			return list.OrderByDescending(Willing).ThenByDescending((Hero h) => Rating(h.CharacterObject)).Take(40).ToList();
		}

		// Ask them. Each is asked once, and the answer stands.
		internal static void Ask(List<Hero> asked)
		{
			string[] p = GatherRec();
			if (p == null)
			{
				return;
			}
			List<string> ours = p[3].Split(',').Where((string x) => x.Length > 0).ToList();
			List<string> done = p[5].Split(',').Where((string x) => x.Length > 0).ToList();
			List<string> yes = new List<string>();
			List<string> no = new List<string>();
			foreach (Hero h in asked)
			{
				string id = ((MBObjectBase)h).StringId;
				if (done.Contains(id) || ours.Contains(id))
				{
					continue;
				}
				done.Add(id);
				if (ours.Count < 6 && MBRandom.RandomInt(100) < Willing(h))
				{
					ours.Add(id);
					yes.Add(h.Name.ToString());
				}
				else
				{
					no.Add(h.Name.ToString());
				}
			}
			p[3] = string.Join(",", ours.ToArray());
			p[5] = string.Join(",", done.ToArray());
			Store.Set(GatherKey, string.Join("|", p));
			string text = "";
			if (yes.Count > 0)
			{
				text += string.Join(", ", yes.ToArray()) + " will stand with you.\n\n";
			}
			if (no.Count > 0)
			{
				text += string.Join(", ", no.ToArray()) + " will not.\n\n";
			}
			text += ours.Count + " of six have answered. The lists open in " + HoursLeft() + " hours.";
			Popup("Who Will Stand With You", text);
		}

		// The lists are ready. Whoever did not answer is made up.
		private static void Open()
		{
			string[] p = GatherRec();
			Store.Set(GatherKey, null);
			if (p == null)
			{
				return;
			}
			Charge c = Get(p[0]);
			if (c == null)
			{
				return;
			}
			List<CharacterObject> ours = Chars(p[3]).Take(6).ToList();
			List<CharacterObject> theirs = Chars(p[4]);
			List<string> named = ours.Select((CharacterObject x) => x.Name.ToString()).ToList();
			int missing = 6 - ours.Count;
			List<string> made = new List<string>();
			if (missing > 0)
			{
				// Half your own soldiers, half glory hunters.
				List<CharacterObject> soldiers = OursSeven().Where((CharacterObject x) => !x.IsHero).ToList();
				int own = Math.Min(soldiers.Count, (missing + 1) / 2);
				for (int i = 0; i < own; i++)
				{
					ours.Add(soldiers[i]);
					made.Add("a " + soldiers[i].Name + " of your own host");
				}
				List<CharacterObject> hunters = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero
					&& x.Occupation == Occupation.Soldier && x.Tier >= 3 && x.Tier <= 5).ToList();
				while (ours.Count < 6 && hunters.Count > 0)
				{
					CharacterObject g = hunters[MBRandom.RandomInt(hunters.Count)];
					ours.Add(g);
					made.Add((MBRandom.RandomInt(2) == 0) ? ("a glory hunter who fights as a " + g.Name) : ("a hedge knight with no house, armed as a " + g.Name));
				}
			}
			string text = "The lists are raised.\n\n" +
				((named.Count > 0) ? ("Standing with you: " + string.Join(", ", named.ToArray()) + ".\n\n") : "Nobody you asked would stand with you.\n\n") +
				((made.Count > 0) ? ("And the rest: " + string.Join("; ", made.ToArray()) + ".\n\n") : "") +
				"Against you: " + string.Join(", ", theirs.Select((CharacterObject x) => x.Name.ToString()).ToArray()) + ".\n\n" +
				"Go to the arena of the town you are in, or the next one you reach.";
			Popup("The Lists Are Ready", text);
			// Not straight into the arena: the lists open on the clock, and the
			// player walks in when they choose.
			Arrange(c, "seven", p[1] == "p", ours, theirs, true, false);
		}

		// Ready now, for the console.
		internal static bool OpenNow()
		{
			if (!Gathering)
			{
				return false;
			}
			Open();
			return true;
		}

		// With you in it, it is fought in an arena: now if you are in a town,
		// otherwise at the next town you enter. Without you, it is decided on
		// the spot.
		private static void Arrange(Charge c, string form, bool prosecution, List<CharacterObject> ours, List<CharacterObject> theirs, bool youFight, bool start = true)
		{
			c.State = "trial";
			Save(c);
			string rec = c.Id + "|" + form + "|" + (prosecution ? "p" : "d") + "|" + Ids(ours) + "|" + Ids(theirs);
			if (!youFight)
			{
				List<CharacterObject> fallen;
				bool won = Simulate(ours, theirs, out fallen);
				Store.Set(TrialKey, rec);
				Store.Set(ResultKey, (won ? "1" : "0") + "|" + Ids(fallen) + "|sim");
				Settle();
				return;
			}
			Store.Set(TrialKey, rec);
			Store.AddDeed(Standing.Date() + "  A " + ((form == "seven") ? "trial of seven" : "trial by combat") + " was called: " + Describe(c) + ".");
			if (!start)
			{
				return;
			}
			string why;
			if (!Fight(out why))
			{
				Flow.Notify("The trial will be fought in the arena of the next town you enter (" + why + ").");
			}
		}

		// Into the arena, if there is one here.
		internal static bool Fight(out string why)
		{
			why = null;
			string rec = Store.Get(TrialKey);
			if (string.IsNullOrEmpty(rec) || !string.IsNullOrEmpty(Store.Get(ResultKey)))
			{
				why = "no trial is waiting";
				return false;
			}
			string[] p = rec.Split('|');
			List<CharacterObject> ours = Chars(p[3]).Where((CharacterObject x) => x != CharacterObject.PlayerCharacter).ToList();
			List<CharacterObject> theirs = Chars(p[4]);
			if (theirs.Count == 0)
			{
				// Nobody left to face you: the trial is forfeit, in your favour.
				Store.Set(ResultKey, "1|");
				Log.Write("law: trial forfeit - nobody is left to face you");
				why = "nobody is left to face you - the trial is yours by forfeit";
				return false;
			}
			float health = (p[1] == "seven") ? Cfg.TrialHealth * 0.8f : Cfg.TrialHealth;
			return TrialFight.Open(ours, theirs, health, delegate(bool won, List<CharacterObject> fallen)
			{
				// Inside the mission: write it down and nothing else.
				Store.Set(ResultKey, (won ? "1" : "0") + "|" + Ids(fallen) + "|arena");
			}, out why);
		}

		internal static bool TrialWaiting
		{
			get
			{
				return !string.IsNullOrEmpty(Store.Get(TrialKey)) && string.IsNullOrEmpty(Store.Get(ResultKey));
			}
		}

		// On skill, when the player is not in it.
		private static bool Simulate(List<CharacterObject> ours, List<CharacterObject> theirs, out List<CharacterObject> fallen)
		{
			fallen = new List<CharacterObject>();
			float a = ours.Sum((CharacterObject x) => (float)Rating(x)) + 1f;
			float b = theirs.Sum((CharacterObject x) => (float)Rating(x)) + 1f;
			bool won = MBRandom.RandomFloat < a / (a + b);
			List<CharacterObject> losers = won ? theirs : ours;
			List<CharacterObject> winners = won ? ours : theirs;
			fallen.AddRange(losers);
			foreach (CharacterObject w in winners)
			{
				if (MBRandom.RandomInt(100) < 25)
				{
					fallen.Add(w);
				}
			}
			return won;
		}

		// ------------------------------------------------------------------
		// what the gods said

		// Out of the arena, never inside it.
		internal static void Settle()
		{
			try
			{
				if (!Cfg.Law || !Store.Initialized || InMission())
				{
					return;
				}
				string result = Store.Get(ResultKey);
				if (!string.IsNullOrEmpty(result))
				{
					string rec = Store.Get(TrialKey);
					Store.Set(ResultKey, null);
					Store.Set(TrialKey, null);
					Verdict(rec, result);
					return;
				}
				if (Gathering && HoursLeft() <= 0)
				{
					Open();
					return;
				}
				// One summons at a time.
				if (_asking)
				{
					return;
				}
				Charge s = All().FirstOrDefault((Charge c) => c.State == "summons");
				if (s != null)
				{
					Summons(s);
				}
			}
			catch (Exception e)
			{
				Log.Write("settling the law failed: " + e);
			}
		}

		private static void Verdict(string rec, string result)
		{
			string[] p = (rec ?? "").Split('|');
			string[] r = result.Split('|');
			if (p.Length < 5 || r.Length < 2)
			{
				return;
			}
			Charge c = Get(p[0]);
			bool seven = p[1] == "seven";
			bool prosecution = p[2] == "p";
			bool won = r[0] == "1";
			List<CharacterObject> fallen = Chars(r[1]);
			List<CharacterObject> ours = Chars(p[3]);
			List<CharacterObject> theirs = Chars(p[4]);

			System.Text.StringBuilder tale = new System.Text.StringBuilder();
			tale.Append(won ? "The gods have spoken for you." : "The gods have spoken, and not for you.").Append("\n\n");

			// Who died. Everyone who fell - on either side, in either kind of
			// trial - takes the same chance. The verdict does not care: it
			// goes to whichever side was left standing.
			List<Hero> dead = new List<Hero>();
			bool youFell = fallen.Contains(CharacterObject.PlayerCharacter);
			foreach (CharacterObject f in fallen)
			{
				Hero h = (f != null && f.IsHero) ? f.HeroObject : null;
				if (h == null || !h.IsAlive || h == Hero.MainHero)
				{
					continue;
				}
				if (MBRandom.RandomInt(100) < Cfg.TrialDeathChance)
				{
					dead.Add(h);
				}
			}
			foreach (Hero h in dead)
			{
				try
				{
					KillCharacterAction.ApplyByBattle(h, null, true);
					tale.Append(h.Name).Append(" did not rise.\n");
				}
				catch (Exception e)
				{
					Log.Write("a death in the trial would not take: " + e.Message);
				}
			}
			// You, last of all, and after the verdict is written: a dead
			// player hands the game to their heir, and the sentence must land
			// on the house before that happens.
			bool youDie = youFell && Cfg.TrialPlayerCanDie && MBRandom.RandomInt(100) < Cfg.TrialDeathChance;
			if (youFell)
			{
				tale.Append(youDie ? "\nYou did not rise either.\n" : "\nYou were carried out of the arena, and you will live.\n");
				if (!youDie)
				{
					Hero.MainHero.HitPoints = Math.Min(Hero.MainHero.HitPoints, 5);
				}
			}
			else if (dead.Count > 0)
			{
				tale.Append("\n");
			}

			if (c == null)
			{
				Popup(seven ? "The Trial of Seven" : "Trial by Combat", tale.ToString().TrimEnd());
				if (youDie)
				{
					Die();
				}
				return;
			}
			Hero accused = Find(c.Accused);
			bool guilty = prosecution ? won : !won;
			Popup(seven ? "The Trial of Seven" : "Trial by Combat", tale.ToString().TrimEnd() + "\n\n" +
				((accused != null) ? accused.Name.ToString() : "The accused") + " is found " + (guilty ? "guilty." : "innocent."));

			if (accused == null || !accused.IsAlive)
			{
				Close(c, "The gods judged " + Describe(c) + ", and the accused did not survive it.");
				if (youDie)
				{
					Die();
				}
				return;
			}
			if (prosecution)
			{
				if (guilty)
				{
					c.State = "open";
					Save(c);
					bool court = Succession.Rules() || Held(accused);
					Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
					QueueAction(delegate(Action done)
					{
						if (court || realm == null || realm.Leader == null || realm.Leader == Hero.MainHero)
						{
							Sentence(c);
						}
						else
						{
							LiegeSentence(c, accused, realm.Leader);
						}
						done();
					});
				}
				else
				{
					Standing.Change(0, -2, "The gods found against the crown");
					Acquit(c);
				}
			}
			else if (guilty)
			{
				Punish(c);
			}
			else
			{
				Hero accuser = Accuser(c);
				if (accuser != null && accuser.IsAlive)
				{
					ChangeRelationAction.ApplyPlayerRelation(accuser, -10, false, false);
				}
				Standing.Change(2, 0, "Vindicated by trial");
				Close(c, "You were found innocent of " + KindName(c.Kind).ToLowerInvariant() + " by trial.");
			}
			Next();
			if (youDie)
			{
				Die();
			}
		}

		private static void Die()
		{
			try
			{
				Log.Write("the player died of a trial");
				KillCharacterAction.ApplyByBattle(Hero.MainHero, null, true);
			}
			catch (Exception e)
			{
				Log.Write("the player's death in the trial failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the charge against you

		private static bool _asking;

		private static void Summons(Charge c)
		{
			Hero accuser = Accuser(c);
			Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
			if (realm == null || accuser == null || !accuser.IsAlive)
			{
				Close(c, null);
				return;
			}
			bool ruler = Succession.Rules();
			Hero judge = ruler ? null : realm.Leader;
			if (!ruler && judge == null)
			{
				// No one sits in judgment just now; the summons waits.
				return;
			}
			int sev = Severity(c.Kind);
			_asking = true;
			c.State = "open";
			Save(c);
			string body = accuser.Name + " of " + ((accuser.Clan != null) ? accuser.Clan.Name.ToString() : "no house") +
				" accuses you of " + KindName(c.Kind).ToLowerInvariant() + (Victim(c) != null ? (" - the death of " + Victim(c).Name) : "") + ".\n\n" +
				(ruler
					? "You are the crown. But the lords of the realm are listening, and they want to see whether the law reaches as high as you."
					: (judge.Name + " has summoned you to answer for it.")) +
				"\n\n" + (c.False ? "It is not true. That has never been enough." : "It is true, and there are witnesses.");
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement("submit", ruler ? "Submit to the judgment of your lords" : ("Submit to " + judge.Name + "'s judgment"), null, true,
				"About " + GuiltChance(c, judge) + "% that you are found guilty. " + PenaltyLine(c, ruler)));
			els.Add(new InquiryElement("combat", "Demand trial by combat", null, true, "You, or a champion of yours, against theirs."));
			if (sev >= 2)
			{
				els.Add(new InquiryElement("seven", "Demand a trial of seven", null, true, "Seven a side. The gods will want to see it."));
			}
			els.Add(new InquiryElement("refuse", ruler ? "\"I am the law.\"" : "Refuse the summons", null, true,
				ruler
					? ("+5 Dread, -6 Honour, and " + accuser.Name + " may take their house out of your realm.")
					: ("-5 Honour, and " + judge.Name + " may put your house out of the realm.")));
			Inquiry.Select(ruler ? "The Crown Accused" : "A Summons", body, els, 1, 1, "So be it", null,
				delegate(List<InquiryElement> chosen)
				{
					_asking = false;
					string pick = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : "submit";
					switch (pick)
					{
					case "combat":
						Combat(c, false);
						break;
					case "seven":
						Seven(c, false);
						break;
					case "refuse":
						Refuse(c, ruler, judge, accuser);
						break;
					default:
						Submit(c, judge);
						break;
					}
				},
				delegate
				{
					_asking = false;
					Submit(c, judge);
				});
		}

		private static int GuiltChance(Charge c, Hero judge)
		{
			int chance = c.False ? 25 : 60;
			if (judge != null)
			{
				chance -= (int)judge.GetRelationWithPlayer() / 2;
			}
			else
			{
				Kingdom realm = Succession.Realm();
				if (realm != null)
				{
					List<float> rel = realm.Clans.Where((Clan x) => x != Clan.PlayerClan && x.Leader != null && x.Leader.IsAlive)
						.Select((Clan x) => x.Leader.GetRelationWithPlayer()).ToList();
					if (rel.Count > 0)
					{
						chance -= (int)rel.Average() / 2;
					}
				}
			}
			chance -= (Store.Honour - 50) / 3;
			return Math.Max(5, Math.Min(95, chance));
		}

		private static string PenaltyLine(Charge c, bool ruler)
		{
			int sev = Severity(c.Kind);
			if (ruler)
			{
				return "If guilty: a weregild of " + (Cfg.LawFine * 2 * sev).ToString("N0") + " to the accuser, and your Honour.";
			}
			return (sev >= 3)
				? "If guilty: a holding taken, and your house put out of the realm."
				: ("If guilty: a fine of " + (Cfg.LawFine * 2 * sev).ToString("N0") + (sev >= 2 ? ", or a holding if you cannot pay." : "."));
		}

		private static void Submit(Charge c, Hero judge)
		{
			if (MBRandom.RandomInt(100) < GuiltChance(c, judge))
			{
				Punish(c);
			}
			else
			{
				Hero accuser = Accuser(c);
				if (accuser != null && accuser.IsAlive)
				{
					ChangeRelationAction.ApplyPlayerRelation(accuser, -10, false, false);
				}
				Standing.Change(1, 0, "Found innocent");
				Close(c, "You were found innocent of " + KindName(c.Kind).ToLowerInvariant() + ".");
				Popup("Innocent", "You are found innocent. " + ((accuser != null) ? (accuser.Name + " will not look at you.") : ""));
			}
			Next();
		}

		// Guilty: what the realm does to you.
		private static void Punish(Charge c)
		{
			try
			{
				int sev = Severity(c.Kind);
				bool ruler = Succession.Rules();
				Hero accuser = Accuser(c);
				Hero you = Hero.MainHero;
				int owed = Cfg.LawFine * 2 * sev;
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append("You are found guilty of ").Append(KindName(c.Kind).ToLowerInvariant()).Append(".\n\n");
				if (ruler)
				{
					int paid = Math.Min(Math.Max(0, you.Gold), owed);
					if (accuser != null && accuser.IsAlive && paid > 0)
					{
						GiveGoldAction.ApplyBetweenCharacters(you, accuser, paid, false);
					}
					Standing.Change(-3 * sev, 0, "Found guilty by your own lords");
					sb.Append("A king cannot be put out of his own realm. You pay a weregild of ").Append(paid.ToString("N0"))
					  .Append(", and the realm has seen the crown bow its head.");
				}
				else if (sev >= 3)
				{
					Settlement lost = Bastard.Fiefs().OrderBy((Settlement s) => (s == Clan.PlayerClan.HomeSettlement) ? 1 : 0).FirstOrDefault();
					Hero king = (Clan.PlayerClan.Kingdom != null) ? Clan.PlayerClan.Kingdom.Leader : null;
					if (lost != null && king != null)
					{
						ChangeOwnerOfSettlementAction.ApplyByKingDecision(king, lost);
						sb.Append(lost.Name).Append(" is taken from you. ");
					}
					if (Clan.PlayerClan.Kingdom != null)
					{
						ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, true);
						sb.Append("And your house is put out of the realm.");
					}
					Standing.Change(-8, 0, "Convicted of " + KindName(c.Kind).ToLowerInvariant());
				}
				else
				{
					if (you.Gold >= owed)
					{
						if (accuser != null && accuser.IsAlive)
						{
							GiveGoldAction.ApplyBetweenCharacters(you, accuser, owed, false);
						}
						else
						{
							you.ChangeHeroGold(-owed);
						}
						sb.Append("You pay ").Append(owed.ToString("N0")).Append(".");
					}
					else
					{
						Settlement lost = (sev >= 2) ? Bastard.Fiefs().OrderBy((Settlement s) => (s == Clan.PlayerClan.HomeSettlement) ? 1 : 0).FirstOrDefault() : null;
						Hero to = (accuser != null && accuser.IsAlive) ? accuser : ((Clan.PlayerClan.Kingdom != null) ? Clan.PlayerClan.Kingdom.Leader : null);
						if (lost != null && to != null)
						{
							ChangeOwnerOfSettlementAction.ApplyByKingDecision(to, lost);
							sb.Append("You cannot pay, and ").Append(lost.Name).Append(" is taken instead.");
						}
						else
						{
							int paid = Math.Max(0, you.Gold);
							you.ChangeHeroGold(-paid);
							sb.Append("You pay everything you have, which is ").Append(paid.ToString("N0")).Append(".");
						}
					}
					Standing.Change(-2 * sev, 0, "Convicted of " + KindName(c.Kind).ToLowerInvariant());
				}
				Close(c, "You were convicted of " + KindName(c.Kind).ToLowerInvariant() + ".");
				Popup("Guilty", sb.ToString());
			}
			catch (Exception e)
			{
				Log.Write("the sentence against you failed: " + e);
			}
		}

		private static void Refuse(Charge c, bool ruler, Hero judge, Hero accuser)
		{
			if (ruler)
			{
				Standing.Change(-6, 5, "\"I am the law\"");
				ChangeRelationAction.ApplyPlayerRelation(accuser, -40, true, false);
				bool rebels = MBRandom.RandomInt(100) < Cfg.LawRebelChance && accuser.Clan != null && accuser.Clan.Kingdom == Succession.Realm()
					&& accuser.Clan != Clan.PlayerClan;
				if (rebels)
				{
					Store.Set("lw:exiled:" + ((MBObjectBase)accuser.Clan).StringId, "1");
					ChangeKingdomAction.ApplyByLeaveWithRebellionAgainstKingdom(accuser.Clan, true);
				}
				Close(c, "You answered a charge of " + KindName(c.Kind).ToLowerInvariant() + " with \"I am the law\"." +
					(rebels ? (" " + accuser.Clan.Name + " rose against you for it.") : ""));
				Popup("The Crown Above the Law", "The lords heard you. " +
					(rebels ? (accuser.Clan.Name + " did not stay to hear the rest - they have taken their banners out of your realm.") : "Nobody said anything. That is not the same as agreeing."));
			}
			else
			{
				Standing.Change(-5, 0, "Refused the king's summons");
				if (judge != null)
				{
					ChangeRelationAction.ApplyPlayerRelation(judge, -30, true, false);
				}
				bool outlawed = MBRandom.RandomInt(100) < 50 && Clan.PlayerClan.Kingdom != null;
				if (outlawed)
				{
					ChangeKingdomAction.ApplyByLeaveKingdom(Clan.PlayerClan, true);
				}
				Close(c, "You refused the summons over a charge of " + KindName(c.Kind).ToLowerInvariant() + "." + (outlawed ? " Your house was declared outlaw." : ""));
				Popup("Summons Refused", outlawed
					? ((judge != null ? judge.Name.ToString() : "The crown") + " has declared your house outlaw. You are no longer of the realm.")
					: "The raven came back without an answer. For now.");
			}
			Next();
		}

		// ------------------------------------------------------------------
		// bringing a charge

		internal static void Bring()
		{
			try
			{
				List<Hero> can = new List<Hero>();
				// The lords of whatever realm you kneel in. If you rule it you
				// hear the charge yourself; if not, your liege does.
				Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
				if (realm != null)
				{
					can.AddRange(realm.Clans.Where((Clan x) => x != null && x != Clan.PlayerClan && !x.IsEliminated)
						.SelectMany((Clan x) => x.Heroes).Where((Hero h) => h.IsAlive && h.IsLord && !h.IsChild));
				}
				foreach (Hero h in Hero.AllAliveHeroes)
				{
					if (h.IsLord && Held(h) && !can.Contains(h))
					{
						can.Add(h);
					}
				}
				if (can.Count == 0)
				{
					Flow.Notify("There is nobody you have the power to accuse.");
					return;
				}
				List<InquiryElement> els = can.OrderBy((Hero h) => h.GetRelationWithPlayer()).Take(60)
					.Where((Hero h) => realm == null || h != realm.Leader || Succession.Rules() || Held(h))
					.Select((Hero h) => new InquiryElement(h, h.Name + "  (" + ((h.Clan != null) ? h.Clan.Name.ToString() : "-") + ", relation " + (int)h.GetRelationWithPlayer() + ")" +
						(Held(h) ? "   - your prisoner" : "") + (All().Any((Charge x) => x.Accused == ((MBObjectBase)h).StringId && !x.False) ? "   - a crime is on record" : ""),
						null, true, "")).ToList();
				Inquiry.Select("Bring a Charge", "Whom do you accuse?", els, 1, 1, "Accuse them", "Not today",
					delegate(List<InquiryElement> chosen)
					{
						Hero h = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as Hero) : null;
						if (h != null)
						{
							Frame(h);
						}
					});
			}
			catch (Exception e)
			{
				Log.Write("bringing a charge failed: " + e.Message);
			}
		}

		// Of what. A crime already on record costs nothing to press. Anything
		// else has to be bought.
		private static void Frame(Hero h)
		{
			string hid = ((MBObjectBase)h).StringId;
			List<InquiryElement> els = new List<InquiryElement>();
			foreach (Charge c in All().Where((Charge x) => x.Accused == hid && !x.False && x.State == "open"))
			{
				els.Add(new InquiryElement(c, "Press the charge: " + Describe(c), null, true, "It is on record, and the witnesses are real."));
			}
			foreach (string k in new string[3] { Treason, Murder, Tyranny })
			{
				string name = (k == Tyranny) ? "Abuse of the smallfolk" : KindName(k);
				els.Add(new InquiryElement(k, "Invent a charge: " + name, null, Hero.MainHero.Gold >= Cfg.LawFabricate,
					"Witnesses cost " + Cfg.LawFabricate.ToString("N0") + ". There is a " + Cfg.LawExposed + "% chance the truth comes out once it is judged."));
			}
			bool judge = Succession.Rules() || Held(h);
			Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
			Inquiry.Select("The Charge",
				"What is " + h.Name + " to answer for?" + (judge ? "" : ("\n\nYou do not hold the court. " + ((realm != null && realm.Leader != null) ? realm.Leader.Name.ToString() : "Your liege") + " will judge it.")),
				els, 1, 1, "Bring it", "Leave it",
				delegate(List<InquiryElement> chosen)
				{
					object pick = (chosen != null && chosen.Count > 0) ? chosen[0].Identifier : null;
					Charge c = pick as Charge;
					if (c == null && pick is string)
					{
						Hero.MainHero.ChangeHeroGold(-Cfg.LawFabricate);
						c = Record((string)pick, h, Hero.MainHero, null, true);
					}
					if (c == null)
					{
						return;
					}
					if (judge)
					{
						Hear(c);
					}
					else
					{
						Liege(c);
					}
				});
		}

		// A vassal's charge, judged by the crown they kneel to.
		internal static void Liege(Charge c)
		{
			try
			{
				Hero accused = Find(c.Accused);
				Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
				Hero king = (realm != null) ? realm.Leader : null;
				if (accused == null || king == null || king == Hero.MainHero)
				{
					return;
				}
				// The accused may ask the gods instead - against you.
				int dare = Math.Min(85, 20 + Rating(accused.CharacterObject) / 6 + (c.False ? 20 : 0));
				if (MBRandom.RandomInt(100) < dare)
				{
					Inquiry.Confirm("Trial by Combat",
						accused.Name + " will not wait for " + king.Name + "'s judgment, and demands trial by combat - against you, or whoever you name.",
						"Accept", "Withdraw the charge",
						delegate
						{
							Combat(c, true);
						},
						delegate
						{
							Standing.Change(-2, 0, "Withdrew a charge against " + accused.Name);
							Close(c, "You withdrew your charge against " + accused.Name + " rather than fight for it.");
						});
					return;
				}
				int chance = (c.False ? 30 : 60) + (king.GetRelation(Hero.MainHero) - king.GetRelation(accused)) / 3;
				chance = Math.Max(5, Math.Min(95, chance));
				if (MBRandom.RandomInt(100) < chance)
				{
					LiegeSentence(c, accused, king);
				}
				else
				{
					ChangeRelationAction.ApplyPlayerRelation(accused, -20, false, false);
					Close(c, king.Name + " found " + accused.Name + " innocent of the charge you brought.");
					Popup("Innocent", king.Name + " heard your charge against " + accused.Name + " and dismissed it.\n\n" + accused.Name + " will remember who brought it.");
					Exposed(c, accused.Name.ToString(), accused.Clan);
				}
			}
			catch (Exception e)
			{
				Log.Write("the liege's judgment failed: " + e.Message);
			}
		}

		// The crown's sentence on a lord you accused.
		private static void LiegeSentence(Charge c, Hero accused, Hero king)
		{
			int sev = Severity(c.Kind);
			string name = accused.Name.ToString();
			Clan house = accused.Clan;
			string outcome;
			Settlement seat = Holding(accused);
			if (sev >= 3 && house != null && house.Kingdom != null && house != house.Kingdom.RulingClan && house != Clan.PlayerClan)
			{
				Store.Set("lw:exiled:" + ((MBObjectBase)house).StringId, "1");
				ChangeKingdomAction.ApplyByLeaveKingdom(house, true);
				outcome = king.Name + " found " + name + " guilty of " + KindName(c.Kind).ToLowerInvariant() + " and put " + house.Name + " out of the realm.";
			}
			else if (sev >= 2 && seat != null)
			{
				ChangeOwnerOfSettlementAction.ApplyByKingDecision(king, seat);
				outcome = king.Name + " found " + name + " guilty of " + KindName(c.Kind).ToLowerInvariant() + " and took " + seat.Name + " from them.";
			}
			else
			{
				int paid = Math.Min(Math.Max(0, accused.Gold), FineFor(c, accused));
				if (paid > 0)
				{
					GiveGoldAction.ApplyBetweenCharacters(accused, Hero.MainHero, paid, false);
				}
				outcome = king.Name + " found " + name + " guilty of " + KindName(c.Kind).ToLowerInvariant() + " and ordered " + paid.ToString("N0") + " paid to you.";
			}
			ChangeRelationAction.ApplyPlayerRelation(accused, -30, true, false);
			Just(c, 1);
			Close(c, outcome);
			Popup("Guilty", outcome);
			Exposed(c, name, house);
		}

		// ------------------------------------------------------------------
		// the court

		internal static string Summary()
		{
			try
			{
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				sb.Append(Succession.Rules()
					? "You are the crown, and the crown is the law. Charges against lords of your realm and against your prisoners are yours to hear.\n\n"
					: "You rule no realm. You can judge only the prisoners you hold - and you can be summoned by whoever you kneel to.\n\n");
				if (TrialWaiting)
				{
					sb.Append("A TRIAL IS WAITING for you in the arena of the next town you enter.\n\n");
				}
				List<Charge> all = All();
				List<Charge> mine = all.Where((Charge c) => c.Accused == ((MBObjectBase)Hero.MainHero).StringId).ToList();
				List<Charge> theirs = all.Except(mine).ToList();
				sb.Append("CHARGES WAITING TO BE HEARD\n");
				if (theirs.Count == 0)
				{
					sb.Append("  None.\n");
				}
				foreach (Charge c in theirs.Take(12))
				{
					string why;
					bool can = CanJudge(c, out why);
					sb.Append("  ").Append(Describe(c)).Append(c.False ? "  (your own invention)" : "")
					  .Append(can ? "" : ("  - " + why)).Append("\n");
				}
				if (mine.Count > 0)
				{
					sb.Append("\nAGAINST YOU\n");
					foreach (Charge c in mine)
					{
						Hero a = Accuser(c);
						sb.Append("  ").Append(KindName(c.Kind)).Append(", brought by ").Append((a != null) ? a.Name.ToString() : "someone").Append("\n");
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
			if (!Cfg.Law)
			{
				return null;
			}
			if (Gathering)
			{
				return "Your trial of seven gathers: " + Answered() + " of six have answered, and the lists open in " + HoursLeft() + " hours.";
			}
			if (TrialWaiting)
			{
				return "A trial waits for you in the arena of the next town you enter.";
			}
			int open = All().Count((Charge c) => c.State == "open" && c.Accused != ((MBObjectBase)Hero.MainHero).StringId && CanJudgeQuiet(c));
			return (open > 0) ? (open + " charge" + ((open == 1) ? " waits" : "s wait") + " to be heard.") : null;
		}

		private static bool CanJudgeQuiet(Charge c)
		{
			string why;
			return CanJudge(c, out why);
		}

		// ------------------------------------------------------------------
		// who fights

		// How good somebody is with a sword, roughly - enough to pick a
		// champion and to decide a fight nobody watched.
		internal static int Rating(CharacterObject c)
		{
			if (c == null)
			{
				return 0;
			}
			try
			{
				int weapon = Math.Max(c.GetSkillValue(DefaultSkills.OneHanded), Math.Max(c.GetSkillValue(DefaultSkills.TwoHanded), c.GetSkillValue(DefaultSkills.Polearm)));
				return weapon + c.GetSkillValue(DefaultSkills.Athletics) / 3 + c.Level * 2;
			}
			catch
			{
				return 50;
			}
		}

		// The best fighter a hero can put forward: themselves, or the best of
		// their house.
		private static Hero Champion(Hero of, Charge c)
		{
			Hero best = of;
			try
			{
				IEnumerable<Hero> pool = (of != null && of.Clan != null) ? of.Clan.Heroes : Enumerable.Empty<Hero>();
				// The crown's champion comes from the crown's own house.
				if (of == null)
				{
					Kingdom k = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
					pool = (k != null && k.RulingClan != null) ? k.RulingClan.Heroes : Enumerable.Empty<Hero>();
				}
				foreach (Hero h in pool)
				{
					if (h != null && h.IsAlive && !h.IsChild && h != Hero.MainHero && !h.IsPrisoner
						&& (best == null || Rating(h.CharacterObject) > Rating(best.CharacterObject)))
					{
						best = h;
					}
				}
			}
			catch
			{
			}
			return best;
		}

		// Who could fight for you: your blood and your companions, free and
		// grown.
		private static List<Hero> Champions()
		{
			List<Hero> list = new List<Hero>();
			try
			{
				foreach (Hero h in Clan.PlayerClan.Heroes)
				{
					if (h != null && h != Hero.MainHero && h.IsAlive && !h.IsChild && !h.IsPrisoner)
					{
						list.Add(h);
					}
				}
			}
			catch
			{
			}
			// The white cloaks first: the Lord Commander, then the sworn
			// brothers. They are the crown's champions.
			return list.OrderByDescending((Hero h) => (Guard.Of(h) != null) ? (Guard.Of(h).Rank + 1) : 0)
				.ThenByDescending((Hero h) => Rating(h.CharacterObject)).Take(8).ToList();
		}

		// Your six: the best heroes in your party, then its best men.
		private static List<CharacterObject> OursSeven()
		{
			List<CharacterObject> list = new List<CharacterObject>();
			try
			{
				TroopRoster roster = MobileParty.MainParty.MemberRoster;
				List<TroopRosterElement> els = roster.GetTroopRoster();
				foreach (TroopRosterElement e in els.Where((TroopRosterElement x) => x.Character != null && x.Character.IsHero && x.Character != CharacterObject.PlayerCharacter)
					.OrderByDescending((TroopRosterElement x) => Rating(x.Character)))
				{
					if (list.Count >= 6)
					{
						break;
					}
					if (e.Character.HeroObject != null && !e.Character.HeroObject.IsWounded)
					{
						list.Add(e.Character);
					}
				}
				foreach (TroopRosterElement e in els.Where((TroopRosterElement x) => x.Character != null && !x.Character.IsHero)
					.OrderByDescending((TroopRosterElement x) => x.Character.Tier))
				{
					for (int i = 0; i < e.Number - e.WoundedNumber && list.Count < 6; i++)
					{
						list.Add(e.Character);
					}
				}
			}
			catch
			{
			}
			return list;
		}

		// Their seven: the head of it, their house, and their culture's best.
		private static List<CharacterObject> SevenFor(Hero head)
		{
			List<CharacterObject> list = new List<CharacterObject> { head.CharacterObject };
			try
			{
				if (head.Clan != null)
				{
					foreach (Hero h in head.Clan.Heroes.Where((Hero x) => x != null && x != head && x.IsAlive && !x.IsChild && x != Hero.MainHero && !x.IsPrisoner)
						.OrderByDescending((Hero x) => Rating(x.CharacterObject)))
					{
						if (list.Count >= 7)
						{
							break;
						}
						list.Add(h.CharacterObject);
					}
				}
				// Then the head's friends - lords who love them enough to stand.
				foreach (Hero h in Hero.AllAliveHeroes.Where((Hero x) => x.IsLord && x != head && x != Hero.MainHero && !x.IsChild && !x.IsPrisoner
					&& x.Clan != head.Clan && head.GetRelation(x) >= Cfg.TrialSevenFriend).OrderByDescending((Hero x) => head.GetRelation(x)).Take(6))
				{
					if (list.Count >= 7)
					{
						break;
					}
					list.Add(h.CharacterObject);
				}
				CultureObject culture = head.Culture;
				List<CharacterObject> elite = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Culture == culture
					&& x.Occupation == Occupation.Soldier && x.Tier >= 4).OrderByDescending((CharacterObject x) => x.Tier).ToList();
				if (elite.Count == 0)
				{
					elite = CharacterObject.All.Where((CharacterObject x) => x != null && !x.IsHero && x.Occupation == Occupation.Soldier && x.Tier >= 4).ToList();
				}
				while (list.Count < 7 && elite.Count > 0)
				{
					list.Add(elite[MBRandom.RandomInt(Math.Min(elite.Count, 4))]);
				}
			}
			catch
			{
			}
			return list;
		}

		// ------------------------------------------------------------------
		// bits

		internal static Hero Find(string id)
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

		private static Hero FindDead(string id)
		{
			try
			{
				return string.IsNullOrEmpty(id) ? null : Hero.DeadOrDisabledHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
			}
			catch
			{
				return null;
			}
		}

		private static Hero Accuser(Charge c)
		{
			return Find(c.Accuser);
		}

		private static Hero Victim(Charge c)
		{
			return Find(c.Victim) ?? FindDead(c.Victim);
		}

		private static string Ids(IEnumerable<CharacterObject> cs)
		{
			return string.Join(",", cs.Where((CharacterObject x) => x != null).Select((CharacterObject x) => ((MBObjectBase)x).StringId).ToArray());
		}

		private static List<CharacterObject> Chars(string ids)
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

		// Popups one after another, as the lists do.
		private static readonly Queue<Action<Action>> _queue = new Queue<Action<Action>>();

		private static bool _showing;

		internal static void Reset()
		{
			_queue.Clear();
			_showing = false;
			_asking = false;
		}

		private static void Popup(string title, string text)
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
			Next();
		}

		private static void QueueAction(Action<Action> a)
		{
			_queue.Enqueue(a);
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
				Log.Write("a law popup failed: " + e.Message);
				done();
			}
		}
	}
}
