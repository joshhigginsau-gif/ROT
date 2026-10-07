using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Single combat on dragonback. RoT's own duel, mounted: a rider whose mount
	// is a dragon is put on its flying form. Whoever loses will very likely not
	// rise again, and neither will the dragon.
	internal static class DragonDuel
	{
		private const string DuelKey = "dd:duel";
		private const string LastKey = "dd:last";
		private const string AiRollKey = "dd:airoll";
		private const string WorldRollKey = "dd:worldroll";

		// ------------------------------------------------------------------
		// who may fight

		internal static DragonRec DragonOf(Hero h)
		{
			if (h == null)
			{
				return null;
			}
			string id = ((MBObjectBase)h).StringId;
			return Dragons.All().FirstOrDefault((DragonRec r) => r.Alive && r.Rider == id);
		}

		private static bool Free(Hero h)
		{
			return h != null && h.IsAlive && !h.IsPrisoner && !h.IsChild;
		}

		internal static bool CanFight(Hero h)
		{
			try
			{
				return Free(h) && Dragons.Rides(h) && DragonOf(h) != null;
			}
			catch
			{
				return false;
			}
		}

		internal static List<Hero> Riders()
		{
			HashSet<string> ids = new HashSet<string>(Dragons.All().Where((DragonRec r) => r.Alive && !string.IsNullOrEmpty(r.Rider)).Select((DragonRec r) => r.Rider));
			return ids.Select((string id) => Law.Find(id)).Where((Hero h) => h != null && h != Hero.MainHero && CanFight(h)).ToList();
		}

		// Riding and the blade, and the dragon's age.
		private static float Strength(Hero h)
		{
			float s = h.GetSkillValue(DefaultSkills.Riding) + h.GetSkillValue(DefaultSkills.OneHanded) + h.GetSkillValue(DefaultSkills.Polearm) / 2f;
			DragonRec d = DragonOf(h);
			if (d != null)
			{
				float years = (float)(CourtBehavior.Today() - d.Born) / (float)Math.Max(1, Cfg.DaysPerYear);
				s += Math.Min(150f, years * 2f);
			}
			return Math.Max(1f, s);
		}

		private static string Who(Hero h)
		{
			DragonRec d = DragonOf(h);
			return h.Name + ((d != null) ? (" on " + d.Name) : "");
		}

		internal static string Cooldown()
		{
			int left = Store.GetI(LastKey, -99999) + Cfg.DragonDuelCooldownDays - CourtBehavior.Today();
			return (left > 0) ? ("You called a rider out not long ago. Another challenge in " + left + " days.") : null;
		}

		internal static string WhyNot()
		{
			if (!Cfg.DragonDuels)
			{
				return "Dragon duels are turned off.";
			}
			if (!CanFight(Hero.MainHero))
			{
				return "You do not ride a dragon.";
			}
			if (!string.IsNullOrEmpty(Store.Get(DuelKey)))
			{
				return "A duel is already under way.";
			}
			return Cooldown();
		}

		// ------------------------------------------------------------------
		// you call them out

		internal static void Pick()
		{
			string why = WhyNot();
			if (why != null)
			{
				Flow.Notify(why);
				return;
			}
			List<Hero> riders = Riders();
			if (riders.Count == 0)
			{
				Flow.Notify("No other rider lives who could answer you.");
				return;
			}
			List<InquiryElement> els = riders.OrderBy((Hero h) => h.GetRelation(Hero.MainHero)).Select((Hero h) =>
			{
				DragonRec d = DragonOf(h);
				string realm = (h.MapFaction != null) ? h.MapFaction.Name.ToString() : "no realm";
				return new InquiryElement(h, h.Name.ToString(), null, true, realm + ". Rides " + d.Name + ", " + Dragons.SizeOf(d) + ". Relation with you " + h.GetRelation(Hero.MainHero) + ". Your odds about " + (int)(Odds(Hero.MainHero, h) * 100f) + "%.");
			}).ToList();
			Inquiry.Select("Call Out a Rider", "Name the rider you would meet in the sky. Whoever falls will almost certainly die, and their dragon with them.", els, 1, 1, "Send the challenge", "Not today", (List<InquiryElement> sel) =>
			{
				Hero foe = (sel != null && sel.Count > 0) ? (sel[0].Identifier as Hero) : null;
				if (foe != null)
				{
					Challenge(foe, false);
				}
			});
		}

		internal static float Odds(Hero a, Hero b)
		{
			float sa = Strength(a);
			float sb = Strength(b);
			return sa / (sa + sb);
		}

		private static void Challenge(Hero foe, bool force)
		{
			Store.SetI(LastKey, CourtBehavior.Today());
			int rel = foe.GetRelation(Hero.MainHero);
			int accept = (rel <= Cfg.DragonDuelHatred) ? 90 : (35 + (int)((0.5f - Odds(Hero.MainHero, foe)) * 50f));
			accept = Math.Max(5, Math.Min(95, accept));
			bool yes = force || MBRandom.RandomInt(100) < accept;
			Log.Write("dragon duel: you call out " + Who(foe) + " (relation " + rel + ", accept " + accept + "%) - " + (yes ? "accepted" : "refused"));
			if (!yes)
			{
				if (foe.Clan != null)
				{
					foe.Clan.AddRenown(-50f, false);
				}
				Standing.Change(2, 0, "offered single combat on dragonback");
				Store.AddDeed(Standing.Date() + "  " + foe.Name + " would not meet you in the sky.");
				Ravens.Popup("The Challenge Refused", foe.Name + " will not fly against you. The realm hears of it, and the word it uses is craven.");
				return;
			}
			Inquiry.Confirm("The Challenge Accepted", Who(foe) + " accepts. They will meet you in the sky.\n\nWhoever falls will almost certainly die - " + Cfg.DragonDuelDeathPercent + "% - and so will their dragon.", "Take wing", "Not yet", () => Fly(foe, true), () =>
			{
				Standing.Change(-Cfg.DragonDuelRefuseHonour, 0, "called a rider out and did not come");
				Log.Write("dragon duel: you did not come to your own challenge");
			});
		}

		// ------------------------------------------------------------------
		// they call you out

		internal static void Daily(int today)
		{
			if (!Cfg.DragonDuels || !Store.Initialized)
			{
				return;
			}
			try
			{
				if (today - Store.GetI(AiRollKey, -9999) >= 28)
				{
					Store.SetI(AiRollKey, today);
					TheyCallYou(false);
				}
				if (today - Store.GetI(WorldRollKey, -9999) >= 21)
				{
					Store.SetI(WorldRollKey, today);
					World(false);
				}
			}
			catch (Exception e)
			{
				Log.Once("dragonduel", "dragon duel: the daily roll failed: " + e.Message);
			}
		}

		internal static string TheyCallYou(bool force)
		{
			if (!CanFight(Hero.MainHero) || !string.IsNullOrEmpty(Store.Get(DuelKey)) || Ravens.InMission() || Hero.MainHero.IsPrisoner)
			{
				return "You cannot be called out now (no dragon, a duel pending, or not free).";
			}
			List<Hero> haters = Riders().Where((Hero h) => force || h.GetRelation(Hero.MainHero) <= Cfg.DragonDuelHatred).OrderBy((Hero h) => h.GetRelation(Hero.MainHero)).ToList();
			Hero foe = null;
			foreach (Hero h in haters)
			{
				if (force || MBRandom.RandomInt(100) < Cfg.DragonDuelAiChallengeChance)
				{
					foe = h;
					break;
				}
			}
			if (foe == null)
			{
				return "No rider hates you enough to call you out.";
			}
			Log.Write("dragon duel: " + Who(foe) + " calls you out (relation " + foe.GetRelation(Hero.MainHero) + ")");
			Inquiry.Confirm("A Rider Calls You Out", Who(foe) + " sends a raven: they will meet you in the sky, and one of you will not come down.\n\nWhoever falls will almost certainly die - " + Cfg.DragonDuelDeathPercent + "% - and so will their dragon. To refuse costs " + Cfg.DragonDuelRefuseHonour + " Honour.", "Take wing", "Refuse", () => Fly(foe, false), () =>
			{
				Standing.Change(-Cfg.DragonDuelRefuseHonour, 0, "refused " + foe.Name + "'s challenge");
				ChangeRelationAction.ApplyPlayerRelation(foe, -10, false, true);
				Store.AddDeed(Standing.Date() + "  You would not meet " + foe.Name + " in the sky.");
				Log.Write("dragon duel: you refused " + foe.Name);
			});
			return foe.Name + " has called you out.";
		}

		// ------------------------------------------------------------------
		// the fight

		private static void Fly(Hero foe, bool youCalled)
		{
			if (!CanFight(Hero.MainHero) || !CanFight(foe))
			{
				Flow.Notify("One of you no longer has a dragon to fly.");
				return;
			}
			Store.Set(DuelKey, ((MBObjectBase)foe).StringId + "|" + (youCalled ? "you" : "them"));
			string why;
			if (RotDuel.Open(foe, out why, true))
			{
				Log.Write("dragon duel: in the sky with " + Who(foe));
				return;
			}
			Log.Write("dragon duel: " + why + " - decided on skill");
			bool won = MBRandom.RandomFloat < Odds(Hero.MainHero, foe);
			Store.Set(DuelKey, null);
			Aftermath(won ? Hero.MainHero : foe, won ? foe : Hero.MainHero);
		}

		// Off the field, after RoT's duel ends.
		internal static void Settle()
		{
			try
			{
				if (!Store.Initialized || Ravens.InMission())
				{
					return;
				}
				string duel = Store.Get(DuelKey);
				if (string.IsNullOrEmpty(duel))
				{
					return;
				}
				bool won;
				if (!RotDuel.TakeResult(out won))
				{
					return;
				}
				Store.Set(DuelKey, null);
				Hero foe = Law.Find(duel.Split('|')[0]);
				if (foe == null)
				{
					return;
				}
				Log.Write("dragon duel: " + (won ? "won" : "lost") + " against " + foe.Name);
				Aftermath(won ? Hero.MainHero : foe, won ? foe : Hero.MainHero);
			}
			catch (Exception e)
			{
				Log.Write("dragon duel: settling failed: " + e.Message);
			}
		}

		private static void Aftermath(Hero winner, Hero loser)
		{
			DragonRec d = DragonOf(loser);
			string dragon = (d != null) ? d.Name : "the dragon";
			bool dragonDies = d != null && MBRandom.RandomInt(100) < Cfg.DragonDuelDragonDeathPercent;
			bool riderDies = MBRandom.RandomInt(100) < Cfg.DragonDuelDeathPercent;
			if (d != null)
			{
				if (dragonDies)
				{
					Dragons.Kill(d, "in the sky against " + winner.Name);
				}
				else if (riderDies)
				{
					// Riderless before the rider's death, or the death would roll the dragon's fall again.
					d.Status = "riderless";
					d.Rider = "";
					Dragons.Save(d);
				}
			}
			if (riderDies && loser.IsAlive)
			{
				Law.Quiet = true;
				try
				{
					KillCharacterAction.ApplyByBattle(loser, winner, true);
				}
				finally
				{
					Law.Quiet = false;
				}
			}
			else if (loser.IsAlive)
			{
				loser.HitPoints = Math.Max(1, loser.MaxHitPoints / 10);
			}
			string line = winner.Name + " brought down " + loser.Name + " in the sky. " + (riderDies ? (loser.Name + " is dead") : (loser.Name + " lived")) + "; " + (dragonDies ? (dragon + " is dead too.") : (dragon + " lived."));
			Log.Write("dragon duel: " + line);
			Store.AddDeed(Standing.Date() + "  " + line, (winner == Hero.MainHero || loser == Hero.MainHero) ? "duel" : "deed");
			bool mine = winner == Hero.MainHero || loser == Hero.MainHero;
			if (winner == Hero.MainHero)
			{
				Standing.Change(2, 5, "won a duel on dragonback");
			}
			bool near = mine || IsNear(winner) || IsNear(loser);
			if (near && (loser != Hero.MainHero || !riderDies))
			{
				Ravens.Popup(winner == Hero.MainHero ? "Victory in the Sky" : "Death in the Sky", line);
			}
		}

		private static bool IsNear(Hero h)
		{
			return h != null && (h.Clan == Clan.PlayerClan || (Clan.PlayerClan.Kingdom != null && h.MapFaction == Clan.PlayerClan.Kingdom));
		}

		// ------------------------------------------------------------------
		// the world: two riders who hate each other, very rarely

		internal static string World(bool force)
		{
			List<Hero> riders = Riders();
			List<Tuple<Hero, Hero>> pairs = new List<Tuple<Hero, Hero>>();
			for (int i = 0; i < riders.Count; i++)
			{
				for (int j = i + 1; j < riders.Count; j++)
				{
					Hero a = riders[i];
					Hero b = riders[j];
					if (force || (a.GetRelation(b) <= Cfg.DragonDuelHatred && b.GetRelation(a) <= Cfg.DragonDuelHatred))
					{
						pairs.Add(Tuple.Create(a, b));
					}
				}
			}
			if (pairs.Count == 0)
			{
				return "No two riders hate each other enough.";
			}
			if (!force && MBRandom.RandomInt(100) >= Cfg.DragonDuelAiVsAiChance)
			{
				return "No duel this season.";
			}
			Tuple<Hero, Hero> p = pairs.OrderBy((Tuple<Hero, Hero> x) => x.Item1.GetRelation(x.Item2)).First();
			bool firstWins = MBRandom.RandomFloat < Odds(p.Item1, p.Item2);
			Hero w = firstWins ? p.Item1 : p.Item2;
			Hero l = firstWins ? p.Item2 : p.Item1;
			Log.Write("dragon duel (world): " + Who(p.Item1) + " against " + Who(p.Item2));
			Aftermath(w, l);
			return w.Name + " beat " + l.Name + ".";
		}

		internal static string Force(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return "Usage: wad.dragon_duel <rider name>";
			}
			if (!CanFight(Hero.MainHero))
			{
				return "You do not ride a dragon.";
			}
			Hero foe = Riders().FirstOrDefault((Hero h) => h.Name.ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
			if (foe == null)
			{
				return "No rider by that name. Riders: " + string.Join(", ", Riders().Select((Hero h) => h.Name.ToString()));
			}
			Challenge(foe, true);
			return "You call out " + foe.Name + ".";
		}
	}
}
