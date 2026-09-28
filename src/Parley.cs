using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Parley at the walls.
	//
	// A banner of parley from your siege lines, and four ways a castle
	// changes hands without an assault. None of them is easy: a lord does not
	// give up his post for a promise. Terms are for the starving; gold must
	// be enough to buy a man's honour outright; talking them round takes a
	// silver tongue three times over. Single combat is the one road a proud
	// lord will walk - if you can win it.
	internal static class Parley
	{
		// settlement | terms day | refused challenge | charm used
		private const string SiegeKey = "pa:siege";
		private const string DuelKey = "pa:duel";
		private const string ResultKey = "pa:result";
		private const string TrucePrefix = "pa:truce:";

		// ------------------------------------------------------------------
		// the siege in front of you

		internal static Settlement Besieged()
		{
			try
			{
				if (!Cfg.Parley || PlayerSiege.PlayerSiegeEvent == null || PlayerSiege.PlayerSide != BattleSideEnum.Attacker)
				{
					return null;
				}
				BesiegerCamp camp = MobileParty.MainParty.BesiegerCamp;
				if (camp == null || camp.LeaderParty != MobileParty.MainParty)
				{
					return null;
				}
				return PlayerSiege.BesiegedSettlement;
			}
			catch
			{
				return null;
			}
		}

		private static string[] State(Settlement s)
		{
			string[] a = (Store.Get(SiegeKey) ?? "").Split('|');
			if (a.Length < 4 || a[0] != ((MBObjectBase)s).StringId)
			{
				a = new string[4] { ((MBObjectBase)s).StringId, "-1", "0", "0" };
				Store.Set(SiegeKey, string.Join("|", a));
			}
			return a;
		}

		private static void Put(string[] a)
		{
			Store.Set(SiegeKey, string.Join("|", a));
		}

		// Who speaks for the walls: the lord of the strongest party inside,
		// else the owner if at home. Null lord = a castellan, whose champion
		// is the best soldier in the garrison.
		internal static Hero Lord(Settlement s)
		{
			try
			{
				Hero best = Defenders(s).Where((PartyBase p) => p.LeaderHero != null && !p.LeaderHero.IsPrisoner)
					.OrderByDescending((PartyBase p) => p.NumberOfHealthyMembers).Select((PartyBase p) => p.LeaderHero).FirstOrDefault();
				if (best != null)
				{
					return best;
				}
				Hero owner = (s.OwnerClan != null) ? s.OwnerClan.Leader : null;
				return (owner != null && owner.CurrentSettlement == s && !owner.IsPrisoner) ? owner : null;
			}
			catch
			{
				return null;
			}
		}

		internal static CharacterObject Champion(Settlement s)
		{
			Hero lord = Lord(s);
			if (lord != null)
			{
				return lord.CharacterObject;
			}
			try
			{
				Hero knight = (s.OwnerClan != null) ? s.OwnerClan.Heroes.Where((Hero h) => h.IsAlive && !h.IsChild && !h.IsPrisoner && h != Hero.MainHero && h.PartyBelongedTo != MobileParty.MainParty)
					.OrderByDescending((Hero h) => h.GetSkillValue(DefaultSkills.OneHanded)).FirstOrDefault() : null;
				if (knight != null)
				{
					return knight.CharacterObject;
				}
			}
			catch
			{
			}
			try
			{
				MobileParty g = s.Town.GarrisonParty;
				if (g != null)
				{
					CharacterObject c = g.MemberRoster.GetTroopRoster().Where((TroopRosterElement e) => e.Character != null && !e.Character.IsHero && e.Number > e.WoundedNumber)
						.OrderByDescending((TroopRosterElement e) => e.Character.Tier).Select((TroopRosterElement e) => e.Character).FirstOrDefault();
					if (c != null)
					{
						return c;
					}
				}
			}
			catch
			{
			}
			return s.Culture.EliteBasicTroop ?? s.Culture.BasicTroop;
		}

		private static IEnumerable<PartyBase> Defenders(Settlement s)
		{
			return s.SiegeEvent.GetSiegeEventSide(BattleSideEnum.Defender).GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege);
		}

		internal static int Ours(Settlement s)
		{
			try
			{
				return s.SiegeEvent.BesiegerCamp.GetInvolvedPartiesForEventType(MapEvent.BattleTypes.Siege).Sum((PartyBase p) => p.NumberOfHealthyMembers);
			}
			catch
			{
				return MobileParty.MainParty.MemberRoster.TotalHealthyCount;
			}
		}

		internal static int Theirs(Settlement s)
		{
			try
			{
				return Math.Max(1, Defenders(s).Sum((PartyBase p) => p.NumberOfHealthyMembers));
			}
			catch
			{
				return 1;
			}
		}

		private static float Ratio(Settlement s)
		{
			return Math.Max(0.1f, (float)Ours(s) / Theirs(s));
		}

		private static int Days(Settlement s)
		{
			try
			{
				return Math.Max(0, (int)(CampaignTime.Now - s.SiegeEvent.SiegeStartTime).ToDays);
			}
			catch
			{
				return 0;
			}
		}

		// 0 fed, 1 short (under five days), 2 starving.
		private static int Hunger(Settlement s)
		{
			try
			{
				Town t = s.Town;
				if (t.FoodStocks <= 0f)
				{
					return 2;
				}
				float change = t.FoodChange;
				return (change < 0f && t.FoodStocks / -change < 5f) ? 1 : 0;
			}
			catch
			{
				return 0;
			}
		}

		private static string FoodText(Settlement s)
		{
			switch (Hunger(s))
			{
			case 2:
				return "starving";
			case 1:
				return "down to their last days of food";
			default:
				return "fed";
			}
		}

		private static int Trait(Hero h, TraitObject t)
		{
			try
			{
				return (h != null) ? h.GetTraitLevel(t) : 0;
			}
			catch
			{
				return 0;
			}
		}

		private static float Log2(float x)
		{
			return (float)(Math.Log(x) / Math.Log(2.0));
		}

		private static int Clamp(int v, int lo, int hi)
		{
			return Math.Max(lo, Math.Min(hi, v));
		}

		// ------------------------------------------------------------------
		// the odds

		internal static int TermsChance(Settlement s)
		{
			Hero lord = Lord(s);
			string[] st = State(s);
			int c = -25;
			c += Clamp((int)(8f * Log2(Ratio(s))), -15, 15);
			int hunger = Hunger(s);
			c += (hunger == 2) ? 45 : ((hunger == 1) ? 20 : 0);
			c += Math.Min(20, 2 * Days(s));
			c += (lord != null) ? ((int)lord.GetRelationWithPlayer() / 5) : 0;
			c += (Store.Honour - 50) / 5;
			c += Store.Dread / 10;
			c -= (Trait(lord, DefaultTraits.Valor) > 0) ? 15 : 0;
			c += (st[2] == "1") ? 10 : 0;
			return Clamp(c, 1, 75);
		}

		internal static int Price(Settlement s)
		{
			float basis = s.IsTown ? Cfg.ParleyBuyTown : Cfg.ParleyBuyCastle;
			float prosperity = 0f;
			try
			{
				prosperity = s.Town.Prosperity;
			}
			catch
			{
			}
			return (int)(basis * (1f + prosperity / 10000f));
		}

		internal static int BuyChance(Settlement s)
		{
			Hero lord = Lord(s);
			int hunger = Hunger(s);
			int c = 45 + ((hunger == 2) ? 30 : ((hunger == 1) ? 15 : 0));
			int honor = Trait(lord, DefaultTraits.Honor);
			c += (honor > 0) ? -30 : ((honor < 0) ? 20 : 0);
			return Clamp(c, 5, 90);
		}

		internal static int ChallengeChance(Settlement s)
		{
			Hero lord = Lord(s);
			if (lord == null)
			{
				// A castellan's champion is glad of the chance.
				return 90;
			}
			return (Trait(lord, DefaultTraits.Valor) > 0) ? 90 : 65;
		}

		private static int CharmChance(Settlement s, string arg)
		{
			int charm = Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 8;
			int hunger = Hunger(s);
			int starve = (hunger == 2) ? 45 : ((hunger == 1) ? 20 : 0);
			int c;
			switch (arg)
			{
			case "reason":
				c = charm + (int)(6f * Log2(Ratio(s))) + starve / 2;
				break;
			case "honour":
				c = charm + (Store.Honour - 50) / 3;
				break;
			default:
				c = charm + Store.Dread / 4;
				break;
			}
			return Clamp(c, 5, 60);
		}

		internal static string Describe(Settlement s)
		{
			Hero lord = Lord(s);
			StringBuilder sb = new StringBuilder();
			sb.Append("You ride out under a banner of parley. ");
			sb.Append((lord != null) ? (lord.Name + " comes to the gate to hear you") : "The castellan comes to the gate - no lord of theirs is inside");
			sb.Append(".\n\n");
			sb.Append("Your men: ").Append(Ours(s).ToString("N0")).Append("    Theirs: ").Append(Theirs(s).ToString("N0")).Append("\n");
			sb.Append("They are ").Append(FoodText(s)).Append(", and have been besieged ").Append(Days(s)).Append(" day(s).\n\n");
			sb.Append("Terms: ").Append(TermsChance(s)).Append("%    Gold: ").Append(Price(s).ToString("N0")).Append(" for ").Append(BuyChance(s)).Append("%\n");
			sb.Append("Single combat: they accept ").Append(ChallengeChance(s)).Append("% of the time");
			return sb.ToString();
		}

		// ------------------------------------------------------------------
		// 1. terms

		internal static string CanTerms(Settlement s)
		{
			int day;
			int.TryParse(State(s)[1], out day);
			return (day == CourtBehavior.Today()) ? "They gave you their answer today. Ask again tomorrow." : null;
		}

		internal static void Terms()
		{
			Settlement s = Besieged();
			if (s == null || CanTerms(s) != null)
			{
				return;
			}
			string[] st = State(s);
			st[1] = CourtBehavior.Today().ToString();
			Put(st);
			Hero lord = Lord(s);
			int chance = TermsChance(s);
			string who = (lord != null) ? lord.Name.ToString() : "The castellan";
			if (MBRandom.RandomInt(100) >= chance)
			{
				Log.Write("parley at " + s.Name + ": terms refused (" + chance + "%)");
				Ravens.Popup("Terms Refused", who + " hears you out, and says the walls still stand and so does their oath. The gate closes.\n\nHunger changes minds. So does time.");
				return;
			}
			Log.Write("parley at " + s.Name + ": terms accepted (" + chance + "%)");
			KeepOrBreak(s, who + " accepts. The garrison will march out under arms, and the lords of " + s.Name + " with them, on your word that they may go.");
		}

		// The gates open: let them go, or take them anyway.
		private static void KeepOrBreak(Settlement s, string text)
		{
			List<InquiryElement> els = new List<InquiryElement>();
			els.Add(new InquiryElement("keep", "Let them march out", null, true, "You gave your word. Honour +2."));
			els.Add(new InquiryElement("break", "Seize the lords as they come out", null, true,
				"The gate is open and they are unarmed in your camp. Honour -" + Cfg.ParleyBreakHonour + ", Dread +" + Cfg.ParleyBreakDread + ", and every house will hear how your word is kept."));
			Inquiry.Select("The Gates Open", text, els, 1, 1, "So be it", null,
				delegate(List<InquiryElement> chosen)
				{
					bool keep = chosen == null || chosen.Count == 0 || (chosen[0].Identifier as string) != "break";
					Surrender(s, keep ? "keep" : "break", null);
				}, delegate
				{
					Surrender(s, "keep", null);
				});
		}

		// ------------------------------------------------------------------
		// 2. gold

		internal static string CanBuy(Settlement s)
		{
			return (Hero.MainHero.Gold < Price(s)) ? ("It would take " + Price(s).ToString("N0") + " gold.") : null;
		}

		internal static void Buy()
		{
			Settlement s = Besieged();
			if (s == null || CanBuy(s) != null)
			{
				return;
			}
			Hero lord = Lord(s);
			int price = Price(s);
			int chance = BuyChance(s);
			string who = (lord != null) ? lord.Name.ToString() : "The castellan";
			Inquiry.Confirm("Buy the Castle", "You offer " + price.ToString("N0") + " gold for " + s.Name + ": enough for " + ((lord != null) ? lord.Name.ToString() : "the castellan") +
				" to walk away from the post and the oath, and never want for anything again. (" + chance + "%)", "Make the offer", "Not today",
				delegate
				{
					string[] st = State(s);
					st[1] = CourtBehavior.Today().ToString();
					Put(st);
					if (MBRandom.RandomInt(100) >= chance)
					{
						Log.Write("parley at " + s.Name + ": gold refused (" + chance + "%)");
						Ravens.Popup("The Gold Refused", who + " looks at the chests a long time, and then has them carried back to you. Some things are not for sale. Yet.");
						return;
					}
					Hero.MainHero.ChangeHeroGold(-price);
					if (lord != null)
					{
						try
						{
							Hero liege = (lord.MapFaction != null) ? lord.MapFaction.Leader : null;
							if (liege != null && liege != lord)
							{
								ChangeRelationAction.ApplyRelationChangeBetweenHeroes(lord, liege, -20, false);
							}
						}
						catch
						{
						}
					}
					Store.AddDeed(Standing.Date() + "  " + who + " sold " + s.Name + " to you for " + price.ToString("N0") + " gold.");
					Log.Write("parley at " + s.Name + ": bought for " + price);
					Surrender(s, "keep", who + " took the gold and the garrison marched out. Whatever they tell their liege, everyone will know.");
				}, null);
		}

		// ------------------------------------------------------------------
		// 3. talk them round: three arguments, all three must land

		internal static string CanCharm(Settlement s)
		{
			return (State(s)[3] == "1") ? "You have already tried to talk them round this siege." : null;
		}

		internal static void Charm()
		{
			Settlement s = Besieged();
			if (s == null || CanCharm(s) != null)
			{
				return;
			}
			string[] st = State(s);
			st[3] = "1";
			Put(st);
			Round(s, new List<string> { "reason", "honour", "fear" }, 1);
		}

		private static void Round(Settlement s, List<string> left, int round)
		{
			Hero lord = Lord(s);
			string who = (lord != null) ? lord.Name.ToString() : "The castellan";
			List<InquiryElement> els = new List<InquiryElement>();
			foreach (string a in left)
			{
				string label = (a == "reason") ? "\"You cannot hold, and you know it.\"" : ((a == "honour") ? "\"I have never broken my word. Ask anyone.\"" : "\"I will not offer this twice.\"");
				els.Add(new InquiryElement(a, label + "  (" + CharmChance(s, a) + "%)", null, true, ""));
			}
			Inquiry.Select("Talk Them Round - " + round + " of 3", who + " is listening. Every argument must land; one that does not, and the gate closes.", els, 1, 1, "Say it", null,
				delegate(List<InquiryElement> chosen)
				{
					string a = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as string) : left[0];
					int chance = CharmChance(s, a);
					bool ok = MBRandom.RandomInt(100) < chance;
					try
					{
						Hero.MainHero.AddSkillXp(DefaultSkills.Charm, ok ? 60f : 25f);
					}
					catch
					{
					}
					if (!ok)
					{
						bool crit = MBRandom.RandomInt(100) < 15;
						if (crit && lord != null)
						{
							ChangeRelationAction.ApplyPlayerRelation(lord, -10, true, false);
						}
						Log.Write("parley at " + s.Name + ": talking failed on " + a + " (" + chance + "%)");
						Ravens.Popup("The Gate Closes", crit ? (who + " takes it as an insult, and says so. The gate closes, and they will remember what you said.") : (who + " is not persuaded. The gate closes."));
						return;
					}
					if (a == "fear")
					{
						Standing.Change(0, 2, "Frightened the walls of " + s.Name);
					}
					List<string> rest = left.Where((string x) => x != a).ToList();
					if (rest.Count == 0)
					{
						Log.Write("parley at " + s.Name + ": talked round");
						KeepOrBreak(s, who + " is quiet a long time. Then they give the order, and the gate opens.");
						return;
					}
					Round(s, rest, round + 1);
				}, delegate
				{
				});
		}

		// ------------------------------------------------------------------
		// 4. single combat

		internal static void Challenge()
		{
			Settlement s = Besieged();
			if (s == null)
			{
				return;
			}
			Hero lord = Lord(s);
			CharacterObject champ = Champion(s);
			if (champ == null)
			{
				Flow.Notify("There is nobody on the walls to fight.");
				return;
			}
			string who = (lord != null) ? lord.Name.ToString() : ((champ.IsHero && champ.HeroObject.Clan != null) ? (champ.Name + ", who comes out for " + champ.HeroObject.Clan.Name) : ("the castellan's champion, " + champ.Name));
			Inquiry.Confirm("Single Combat", "You challenge " + who + " to meet you in single combat, before both armies. If you win, " + s.Name + " yields. If you lose, you lift the siege and swear not to return for " +
				Cfg.ParleyTruceDays + " days.\n\nWhoever falls has a " + Cfg.TrialDeathChance + "% chance of never rising" + (Cfg.TrialPlayerCanDie ? " - you included." : "."),
				"Throw down the gauntlet", "Not today",
				delegate
				{
					if (MBRandom.RandomInt(100) >= ChallengeChance(s))
					{
						string[] st = State(s);
						st[2] = "1";
						Put(st);
						Log.Write("parley at " + s.Name + ": challenge refused");
						Ravens.Popup("The Challenge Refused", who + " will not come down. Their own men watched them refuse - and men who have watched that are easier to talk to.");
						return;
					}
					Fight(s, lord, champ);
				}, null);
		}

		// The arena crowd dresses its spectators from the town you are standing
		// in - and before the walls you are standing in no town at all, which
		// crashed the game. Outside a settlement, the crowd is the camp
		// followers of the castle's own culture instead.
		private static bool _crowdPatched;
		private static CultureObject _crowd;

		internal static void PatchCrowd()
		{
			if (_crowdPatched)
			{
				return;
			}
			_crowdPatched = true;
			try
			{
				Type t = AccessTools.TypeByName("SandBox.View.Missions.MissionAudienceHandler");
				MethodInfo m = (t != null) ? AccessTools.Method(t, "GetRandomAudienceCharacterToSpawn") : null;
				if (m == null)
				{
					Log.Write("parley: the arena crowd was not found; single combat at a siege will be decided without a duel");
					_crowdPatched = false;
					_crowdFailed = true;
					return;
				}
				new Harmony("community.wardens.and.dragons.parley").Patch(m, new HarmonyMethod(typeof(Parley).GetMethod("CrowdPrefix", BindingFlags.Static | BindingFlags.NonPublic)));
				Log.Write("parley: crowd patched on MissionAudienceHandler.GetRandomAudienceCharacterToSpawn");
			}
			catch (Exception e)
			{
				_crowdFailed = true;
				Log.Write("parley: patching the arena crowd failed: " + e.Message);
			}
		}

		private static bool _crowdFailed;

		private static bool CrowdPrefix(ref CharacterObject __result)
		{
			try
			{
				if (Settlement.CurrentSettlement != null)
				{
					return true;
				}
				CultureObject c = _crowd ?? Hero.MainHero.Culture;
				CharacterObject who = (MBRandom.RandomFloat < 0.65f) ? c.Townsman : c.Townswoman;
				who = who ?? c.Townsman ?? c.Townswoman;
				if (who == null)
				{
					return true;
				}
				__result = who;
				return false;
			}
			catch
			{
				return true;
			}
		}

		// No duel to be had: decided on skill instead.
		private static void Decide(Settlement s, Hero lord, CharacterObject champ)
		{
			Func<CharacterObject, int> arms = (CharacterObject c) => Math.Max(c.GetSkillValue(DefaultSkills.OneHanded), Math.Max(c.GetSkillValue(DefaultSkills.TwoHanded), c.GetSkillValue(DefaultSkills.Polearm)));
			int mine = arms(CharacterObject.PlayerCharacter);
			int theirs = arms(champ) + ((Trait(lord, DefaultTraits.Valor) > 0) ? 10 : 0);
			int chance = Clamp(50 + (mine - theirs) / 3, 10, 90);
			bool won = MBRandom.RandomInt(100) < chance;
			Log.Write("parley at " + s.Name + ": single combat decided without a duel (" + chance + "%)");
			Store.Set(ResultKey, (won ? "1" : "0") + "|" + ((MBObjectBase)(won ? champ : CharacterObject.PlayerCharacter)).StringId);
			Settle();
		}

		private const string RotKey = "pa:rot";

		private static void Fight(Settlement s, Hero lord, CharacterObject champ)
		{
			_crowd = s.Culture;
			// Whoever fights for the walls: the lord, or a knight of the house.
			Hero foe = lord ?? (champ.IsHero ? champ.HeroObject : null);
			Store.Set(DuelKey, ((MBObjectBase)s).StringId + "|" + ((foe != null) ? ((MBObjectBase)foe).StringId : "") + "|" + ((MBObjectBase)champ).StringId);
			Log.Write("parley at " + s.Name + ": single combat against " + champ.Name);
			if (foe == null)
			{
				Decide(s, lord, champ);
				return;
			}
			string why;
			if (RotDuel.Open(foe, out why))
			{
				Store.Set(RotKey, "1");
				return;
			}
			Log.Write("parley at " + s.Name + ": " + why);
			Decide(s, foe, champ);
		}

		// Off the field, never on it.
		internal static void Settle()
		{
			try
			{
				if (!Cfg.Parley || !Store.Initialized || Ravens.InMission())
				{
					return;
				}
				string duel = Store.Get(DuelKey);
				bool rotWon;
				if (Store.Get(RotKey) == "1" && !string.IsNullOrEmpty(duel) && RotDuel.TakeResult(out rotWon))
				{
					Store.Set(RotKey, null);
					string[] rd = duel.Split('|');
					Hero rf = (rd.Length > 1 && rd[1].Length > 0) ? Law.Find(rd[1]) : null;
					string loser = rotWon ? ((rf != null) ? rf.CharacterObject.StringId : ((rd.Length > 2) ? rd[2] : "")) : CharacterObject.PlayerCharacter.StringId;
					Store.Set(ResultKey, (rotWon ? "1" : "0") + "|" + loser);
					Log.Write("rot duel: " + (rotWon ? "won" : "lost"));
				}
				string result = Store.Get(ResultKey);
				if (FieldDuel.Failed && string.IsNullOrEmpty(result) && !string.IsNullOrEmpty(duel))
				{
					FieldDuel.Failed = false;
					string[] dd = duel.Split('|');
					Settlement ds = Settlement.Find(dd[0]);
					Hero dl = (dd.Length > 1 && dd[1].Length > 0) ? Law.Find(dd[1]) : null;
					CharacterObject dc = (dd.Length > 2) ? MBObjectManager.Instance.GetObject<CharacterObject>(dd[2]) : null;
					if (ds != null && dc != null)
					{
						Decide(ds, dl, dc);
					}
					else
					{
						Store.Set(DuelKey, null);
					}
					return;
				}
				if (string.IsNullOrEmpty(result) || string.IsNullOrEmpty(duel))
				{
					return;
				}
				Store.Set(ResultKey, null);
				Store.Set(DuelKey, null);
				string[] r = result.Split('|');
				string[] d = duel.Split('|');
				bool won = r[0] == "1";
				List<CharacterObject> fallen = Ravens.Chars((r.Length > 1) ? r[1] : "");
				Settlement s = Settlement.Find(d[0]);
				Hero lord = (d[1].Length > 0) ? Law.Find(d[1]) : null;
				bool youFell = fallen.Contains(CharacterObject.PlayerCharacter);
				bool youDie = youFell && Cfg.TrialPlayerCanDie && MBRandom.RandomInt(100) < Cfg.TrialDeathChance;
				bool lordDies = lord != null && lord.IsAlive && fallen.Contains(lord.CharacterObject) && MBRandom.RandomInt(100) < Cfg.TrialDeathChance;
				string who = (lord != null) ? lord.Name.ToString() : "The champion";
				if (won)
				{
					string text;
					if (lordDies)
					{
						Law.Quiet = true;
						try
						{
							KillCharacterAction.ApplyByBattle(lord, Hero.MainHero, true);
						}
						finally
						{
							Law.Quiet = false;
						}
						text = who + " did not rise. " + s.Name + " yields, as was sworn.";
					}
					else
					{
						text = who + " yields, and " + s.Name + " with them.";
					}
					try
					{
						GainRenownAction.Apply(Hero.MainHero, 10f, false);
					}
					catch
					{
					}
					Store.AddDeed(Standing.Date() + "  Won " + s.Name + " in single combat against " + who + ".");
					Log.Write("parley at " + s.Name + ": single combat won" + (lordDies ? " (the lord died)" : ""));
					Hero captive = (lord != null && lord.IsAlive && !lordDies) ? lord : null;
					InformationManager.ShowInquiry(new InquiryData("Single Combat", text, true, false, "Take the castle", null, delegate
					{
						Surrender(s, "duel", null, captive);
					}, null, "", 0f, null, null, null), true, false);
					return;
				}
				Log.Write("parley at " + s.Name + ": single combat lost");
				Store.SetI(TrucePrefix + ((MBObjectBase)s).StringId, CourtBehavior.Today() + Cfg.ParleyTruceDays);
				Store.AddDeed(Standing.Date() + "  Lost " + s.Name + " in single combat, and lifted the siege.");
				if (youDie)
				{
					Log.Write("the player died in single combat");
					KillCharacterAction.ApplyByBattle(Hero.MainHero, lord, true);
					return;
				}
				if (youFell)
				{
					Hero.MainHero.HitPoints = Math.Min(Hero.MainHero.HitPoints, 5);
				}
				InformationManager.ShowInquiry(new InquiryData("Single Combat", who + " stood over you before both armies. You swore to lift the siege, and not to return for " + Cfg.ParleyTruceDays + " days.",
					true, false, "Strike the camp", null, delegate
					{
						Lift();
					}, null, "", 0f, null, null, null), true, false);
			}
			catch (Exception e)
			{
				Log.Write("settling the single combat failed: " + e);
			}
		}

		// ------------------------------------------------------------------
		// the castle changes hands

		// how: keep (let them go), break (seize them), duel (the loser is
		// yours, the rest go).
		private static void Surrender(Settlement s, string how, string text, Hero captive = null)
		{
			try
			{
				if (s == null || s.SiegeEvent == null)
				{
					Flow.Notify("The siege is over already.");
					return;
				}
				List<MobileParty> inside = s.Parties.Where((MobileParty p) => p != MobileParty.MainParty && p.LeaderHero != null && p.MapFaction != null && FactionManager.IsAtWarAgainstFaction(p.MapFaction, MobileParty.MainParty.MapFaction)).ToList();
				List<Hero> lords = inside.Select((MobileParty p) => p.LeaderHero).ToList();
				// The garrison marches out.
				try
				{
					if (s.Town.GarrisonParty != null)
					{
						s.Town.GarrisonParty.MemberRoster.Clear();
					}
				}
				catch
				{
				}
				List<Hero> taken = new List<Hero>();
				foreach (MobileParty p in inside)
				{
					Hero h = p.LeaderHero;
					bool seize = how == "break" || h == captive;
					try
					{
						LeaveSettlementAction.ApplyForParty(p);
						if (seize)
						{
							TakePrisonerAction.Apply(MobileParty.MainParty.Party, h);
							taken.Add(h);
						}
						else
						{
							p.Ai.DisableForHours(6);
						}
					}
					catch (Exception e)
					{
						Log.Write("a lord leaving " + s.Name + ": " + e.Message);
					}
				}
				if (captive != null && !taken.Contains(captive) && captive.IsAlive && !captive.IsPrisoner)
				{
					try
					{
						TakePrisonerAction.Apply(MobileParty.MainParty.Party, captive);
						taken.Add(captive);
					}
					catch
					{
					}
				}
				// Vanilla's own capture, as after an assault.
				Hero newOwner = (MobileParty.MainParty.MapFaction is Kingdom k) ? k.Leader : Hero.MainHero;
				try
				{
					if (MobileParty.MainParty.MapFaction is Kingdom)
					{
						GainKingdomInfluenceAction.ApplyForCapturingEnemySettlement(MobileParty.MainParty, Campaign.Current.Models.DiplomacyModel.GetInfluenceAwardForSettlementCapturer(s));
					}
				}
				catch
				{
				}
				s.SiegeEvent.BesiegerCamp.RemoveAllSiegeParties();
				s.Party.MemberRoster.Clear();
				ChangeOwnerOfSettlementAction.ApplyBySiege(newOwner, Hero.MainHero, s);
				try
				{
					if (PlayerSiege.PlayerSiegeEvent != null)
					{
						PlayerSiege.FinalizePlayerSiege();
					}
				}
				catch
				{
				}
				try
				{
					if (PlayerEncounter.Current != null)
					{
						PlayerEncounter.Finish(true);
					}
				}
				catch
				{
				}
				Store.Set(SiegeKey, null);
				StringBuilder sb = new StringBuilder();
				if (text != null)
				{
					sb.Append(text).Append("\n\n");
				}
				if (how == "break")
				{
					Standing.Change(-Cfg.ParleyBreakHonour, Cfg.ParleyBreakDread, "Seized the lords of " + s.Name + " under parley");
					foreach (Hero h in taken)
					{
						ChangeRelationAction.ApplyPlayerRelation(h, -30, true, false);
						Hero head = (h.Clan != null && h.Clan.Leader != null) ? h.Clan.Leader : h;
						Law.Record(Law.Oathbreaking, Hero.MainHero, head, h, false);
					}
					Store.AddDeed(Standing.Date() + "  Took " + s.Name + " under a banner of parley, and seized its lords as they came out.");
					sb.Append("The gates opened, and your men took the lords of ").Append(s.Name).Append(" as they walked out under your word: ")
					  .Append(string.Join(", ", taken.Select((Hero h) => h.Name.ToString()).ToArray())).Append(".\n\nEvery house will hear how your word is kept.");
				}
				else
				{
					if (how == "keep")
					{
						Standing.Change(2, 0, "Kept faith at " + s.Name);
					}
					Store.AddDeed(Standing.Date() + "  " + s.Name + " yielded to you" + ((how == "duel") ? " after single combat." : " under a banner of parley."));
					sb.Append(s.Name).Append(" is yours. ");
					if (lords.Count > taken.Count)
					{
						sb.Append("Its lords marched out under your word, and are gone. ");
					}
					if (taken.Count > 0)
					{
						sb.Append(string.Join(", ", taken.Select((Hero h) => h.Name.ToString()).ToArray())).Append(" is your prisoner. ");
					}
				}
				Log.Write("parley: " + s.Name + " yielded (" + how + "), " + taken.Count + " taken");
				Ravens.Popup(s.Name.ToString(), sb.ToString().Trim());
			}
			catch (Exception e)
			{
				Log.Write("the surrender of " + ((s != null) ? s.Name.ToString() : "?") + " failed: " + e);
				Flow.Notify("Something went wrong with the surrender - see the log.");
			}
		}

		private static void Lift()
		{
			try
			{
				MobileParty.MainParty.BesiegerCamp = null;
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounter.Finish(true);
				}
				else
				{
					TaleWorlds.CampaignSystem.GameMenus.GameMenu.ExitToLast();
				}
			}
			catch (Exception e)
			{
				Log.Write("lifting the siege failed: " + e.Message);
			}
			Store.Set(SiegeKey, null);
		}

		// Swore to stay away, and came back.
		internal static void OnSiegeStarted(SiegeEvent se)
		{
			try
			{
				if (!Cfg.Parley || se == null || se.BesiegerCamp == null || se.BesiegerCamp.LeaderParty != MobileParty.MainParty || se.BesiegedSettlement == null)
				{
					return;
				}
				string key = TrucePrefix + ((MBObjectBase)se.BesiegedSettlement).StringId;
				int until = Store.GetI(key, 0);
				if (until <= 0)
				{
					return;
				}
				Store.Set(key, null);
				if (CourtBehavior.Today() < until)
				{
					Standing.Change(-Cfg.ParleyTruceHonour, 0, "Broke the truce sworn at " + se.BesiegedSettlement.Name);
					Store.AddDeed(Standing.Date() + "  Came back to " + se.BesiegedSettlement.Name + " before the truce was out.");
					Ravens.Popup("A Truce Broken", "You swore after single combat not to come back to " + se.BesiegedSettlement.Name + " for " + Cfg.ParleyTruceDays + " days. Everyone remembers that you did.");
				}
			}
			catch
			{
			}
		}

		internal static string Odds()
		{
			Settlement s = Besieged();
			return (s == null) ? "You are not leading a siege." : Describe(s).Replace("\n\n", "\n");
		}
	}
}
