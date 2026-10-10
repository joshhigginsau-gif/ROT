using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Some do not stay dead. Rarely, a lord struck down by another crawls out
	// of the ditch or off the block, vanishes for a season, and comes back
	// scarred, renamed by what was done to them, and sworn to kill the one who
	// did it. Anyone can become anyone's nemesis - yours, your kin's, or two
	// strangers'.
	internal static class Nemesis
	{
		private const string Prefix = "nm:";      // hero -> enemy|rank|epithet|baseName
		private const string FallPrefix = "nmf:"; // hero -> killer|detail
		private const string AwayPrefix = "nma:"; // hero -> returnDay|killer|detail

		internal sealed class Rec
		{
			internal string Hero = "";
			internal string Enemy = "";
			internal int Rank;
			internal string Epithet = "";
			internal string BaseName = "";
		}

		internal static Rec Of(Hero h)
		{
			if (h == null)
			{
				return null;
			}
			string[] p = (Store.Get(Prefix + ((MBObjectBase)h).StringId) ?? "").Split('|');
			if (p.Length < 4)
			{
				return null;
			}
			Rec r = new Rec { Hero = ((MBObjectBase)h).StringId, Enemy = p[0], Epithet = p[2], BaseName = p[3] };
			int.TryParse(p[1], out r.Rank);
			return r;
		}

		private static void Save(Rec r)
		{
			Store.Set(Prefix + r.Hero, r.Enemy + "|" + r.Rank + "|" + r.Epithet.Replace("|", "") + "|" + r.BaseName.Replace("|", ""));
		}

		internal static string Describe(Hero h)
		{
			Rec r = Of(h);
			if (r == null || !Cfg.Nemeses)
			{
				return null;
			}
			Hero e = Law.Find(r.Enemy);
			return "Nemesis" + (r.Rank > 1 ? (" (" + Roman(r.Rank) + ")") : "") + " - has returned from death " + r.Rank + " time" + (r.Rank == 1 ? "" : "s") + ", sworn against " + ((e != null) ? e.Name.ToString() : "a dead enemy") + ".";
		}

		private static string Roman(int n)
		{
			return (n <= 1) ? "I" : ((n == 2) ? "II" : "III");
		}

		// ------------------------------------------------------------------
		// cheating death

		internal static void Patch(Harmony h)
		{
			try
			{
				System.Reflection.MethodInfo m = AccessTools.Method(typeof(KillCharacterAction), "ApplyInternal");
				if (m == null)
				{
					Log.Write("nemesis: KillCharacterAction.ApplyInternal not found - nemeses off");
					return;
				}
				h.Patch(m, new HarmonyMethod(typeof(Nemesis).GetMethod("Pre", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)));
				Log.Write("nemesis: watching for those who will not stay dead");
			}
			catch (Exception e)
			{
				Log.Write("nemesis: patch failed: " + e.Message);
			}
		}

		internal static bool ForceNext;

		private static bool Pre(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail actionDetail)
		{
			try
			{
				if (!Cfg.Nemeses || !Store.Initialized || victim == null || !victim.IsAlive || victim.IsChild || victim == Hero.MainHero || !victim.IsLord)
				{
					return true;
				}
				if (actionDetail != KillCharacterAction.KillCharacterActionDetail.DiedInBattle && actionDetail != KillCharacterAction.KillCharacterActionDetail.Executed
					&& actionDetail != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent && actionDetail != KillCharacterAction.KillCharacterActionDetail.Murdered)
				{
					return true;
				}
				// The game first only marks a death in battle; the real death comes after.
				if ((victim.PartyBelongedTo?.MapEvent != null || victim.PartyBelongedTo?.SiegeEvent != null || actionDetail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)
					&& victim.DeathMark == KillCharacterAction.KillCharacterActionDetail.None)
				{
					return true;
				}
				Hero by = killer ?? victim.DeathMarkKillerHero;
				if (by == null || by == victim || !by.IsAlive)
				{
					return true;
				}
				Rec r = Of(victim);
				int returns = (r != null) ? r.Rank : 0;
				int chance = (returns == 0) ? Cfg.NemesisChance : ((returns == 1) ? Cfg.NemesisChance2 : ((returns == 2) ? Cfg.NemesisChance3 : 0));
				bool involvesYou = Touches(victim) || Touches(by);
				if (!involvesYou)
				{
					chance = chance * Cfg.NemesisAiRate / 100;
				}
				bool forced = ForceNext;
				ForceNext = false;
				if (!forced && MBRandom.RandomFloat * 100f >= chance)
				{
					if (r != null)
					{
						History.Hero(victim, "Was slain for good by " + by.Name + ".", 3);
						History.Hero(by, "Slew " + victim.Name + " for good - they will not come back again.", 3);
						Store.Set(Prefix + r.Hero, null);
					}
					return true;
				}
				// Not today.
				victim.MakeWounded(null, KillCharacterAction.KillCharacterActionDetail.None);
				Store.Set(FallPrefix + ((MBObjectBase)victim).StringId, ((MBObjectBase)by).StringId + "|" + (int)actionDetail);
				Log.Write("nemesis: " + victim.Name + " was left for dead by " + by.Name + " (" + actionDetail + ") - but is not dead");
				return false;
			}
			catch (Exception e)
			{
				Log.Once("nemesispre", "nemesis: " + e.Message);
				return true;
			}
		}

		internal static bool Touches(Hero h)
		{
			if (h == null || Clan.PlayerClan == null)
			{
				return false;
			}
			return h == Hero.MainHero || h.Clan == Clan.PlayerClan || (Clan.PlayerClan.Kingdom != null && h.MapFaction == Clan.PlayerClan.Kingdom && h.Clan != null && h.Clan.Leader == h);
		}

		// ------------------------------------------------------------------
		// the vanishing and the return

		internal static void Hourly()
		{
			if (!Cfg.Nemeses || !Store.Initialized)
			{
				return;
			}
			foreach (string key in Store.Keys(FallPrefix).ToList())
			{
				try
				{
					Hero h = Law.Find(key.Substring(FallPrefix.Length));
					if (h == null || !h.IsAlive)
					{
						Store.Set(key, null);
						continue;
					}
					if (h.PartyBelongedTo != null && h.PartyBelongedTo.MapEvent != null)
					{
						continue;
					}
					string[] v = (Store.Get(key) ?? "").Split('|');
					Store.Set(key, null);
					DisableHeroAction.Apply(h);
					int back = CourtBehavior.Today() + MBRandom.RandomInt(20, 41);
					Store.Set(AwayPrefix + key.Substring(FallPrefix.Length), back + "|" + v[0] + "|" + ((v.Length > 1) ? v[1] : "0"));
					Hero killer = Law.Find(v[0]);
					Log.Write("nemesis: " + h.Name + " has vanished - the body was never found; returns day " + back);
					if (Touches(h) || Touches(killer))
					{
						Ravens.Popup("No Body", "When they went to bury " + h.Name + ", there was no body to bury. Only blood, and a trail that went nowhere.");
					}
				}
				catch (Exception e)
				{
					Log.Once("nmfall", "nemesis: vanishing failed: " + e.Message);
				}
			}
		}

		internal static void Daily(int today)
		{
			if (!Cfg.Nemeses || !Store.Initialized)
			{
				return;
			}
			foreach (string key in Store.Keys(AwayPrefix).ToList())
			{
				try
				{
					string[] v = (Store.Get(key) ?? "").Split('|');
					int day;
					if (v.Length < 3 || !int.TryParse(v[0], out day))
					{
						Store.Set(key, null);
						continue;
					}
					if (today < day)
					{
						continue;
					}
					Store.Set(key, null);
					Hero h = Law.Find(key.Substring(AwayPrefix.Length));
					int d;
					int.TryParse(v[2], out d);
					if (h != null && h.IsAlive)
					{
						Return(h, Law.Find(v[1]), (KillCharacterAction.KillCharacterActionDetail)d);
					}
				}
				catch (Exception e)
				{
					Log.Once("nmreturn", "nemesis: a return failed: " + e.Message);
				}
			}
			if (today - Store.GetI("nmt:taunt", -9999) >= 21)
			{
				Store.SetI("nmt:taunt", today);
				Taunt();
			}
			Hunt();
		}

		private static readonly string[] Battle = new string[6] { "the Scarred", "One-Eye", "Scarface", "the Unbroken", "Ironjaw", "the Ditch-Born" };
		private static readonly string[] Block = new string[4] { "the Unhanged", "Halfneck", "the Headsman's Debt", "Ropeburn" };
		private static readonly string[] Knife = new string[3] { "the Poisoned", "Twice-Buried", "the Unkillable" };

		private static void Return(Hero h, Hero enemy, KillCharacterAction.KillCharacterActionDetail how)
		{
			Rec r = Of(h) ?? new Rec { Hero = ((MBObjectBase)h).StringId, BaseName = h.Name.ToString() };
			r.Rank = Math.Min(3, r.Rank + 1);
			r.Enemy = (enemy != null) ? ((MBObjectBase)enemy).StringId : r.Enemy;
			string[] pool = (how == KillCharacterAction.KillCharacterActionDetail.DiedInBattle) ? Battle : ((how == KillCharacterAction.KillCharacterActionDetail.Murdered) ? Knife : Block);
			r.Epithet = (r.Rank >= 3) ? "Deathless" : ((r.Rank == 2) ? "the Twice-Dead" : pool[MBRandom.RandomInt(pool.Length)]);
			Save(r);
			try
			{
				h.SetName(new TextObject("{=!}" + r.BaseName + " " + r.Epithet, null), h.FirstName ?? new TextObject("{=!}" + r.BaseName, null));
			}
			catch
			{
			}
			// What did not kill them made them harder.
			int gain = 15 + 10 * r.Rank;
			foreach (SkillObject s in new SkillObject[5] { DefaultSkills.OneHanded, DefaultSkills.Leadership, DefaultSkills.Tactics, DefaultSkills.Riding, DefaultSkills.Athletics })
			{
				h.SetSkillValue(s, Math.Min(330, h.GetSkillValue(s) + gain));
			}
			// Back among the living, at home.
			Settlement home = (h.Clan != null && h.Clan.HomeSettlement != null) ? h.Clan.HomeSettlement : Settlement.All.FirstOrDefault((Settlement s) => s.IsTown && s.MapFaction == h.MapFaction);
			try
			{
				h.ChangeState(Hero.CharacterStates.Active);
				h.HitPoints = h.MaxHitPoints;
				if (home != null)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(h, home);
				}
			}
			catch (Exception e)
			{
				Log.Write("nemesis: bringing " + h.Name + " back failed: " + e.Message);
			}
			if (enemy != null && enemy.IsAlive)
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(h, enemy, -100, false);
			}
			string who = (enemy != null) ? enemy.Name.ToString() : "the one who did it";
			History.Hero(h, "Came back from the dead as " + h.Name + ", sworn to kill " + who + ".", 3);
			if (enemy != null)
			{
				History.Hero(enemy, h.Name + " - whom they thought dead - returned, sworn against them.", 3);
			}
			if (h.Clan != null)
			{
				History.House(h.Clan, r.BaseName + " returned from the dead, and is called " + r.Epithet + " now.", 3);
			}
			Log.Write("nemesis: " + h.Name + " returns (rank " + r.Rank + "), sworn against " + who);
			if (enemy == Hero.MainHero)
			{
				Ravens.Popup("Back From the Dead", Line(h, r, true));
			}
			else if (Touches(h) || Touches(enemy))
			{
				Ravens.Popup("Back From the Dead", h.Name + " is alive. They were " + ((how == KillCharacterAction.KillCharacterActionDetail.DiedInBattle) ? "left for dead on the field" : "put to death") + " by " + who + ", and they have come back with a new name and a single purpose.");
			}
		}

		private static readonly string[] Taunts = new string[8]
		{
			"\"You left me in the mud. The mud gave me back. I am coming for you.\"",
			"\"Count your guards, {ME}. Then count them again. It will not be enough.\"",
			"\"I died once for you. You will only get to do it once.\"",
			"\"They call me {EPITHET} now. Ask yourself who gave me the name.\"",
			"\"Sleep well. I don't.\"",
			"\"Every scar on me has your name on it.\"",
			"\"You should have finished it.\"",
			"\"I will see you in the field. Bring your best men. Bring all of them.\""
		};

		private static string Line(Hero h, Rec r, bool first)
		{
			string t = Taunts[MBRandom.RandomInt(Taunts.Length)].Replace("{ME}", Hero.MainHero.FirstName != null ? Hero.MainHero.FirstName.ToString() : Hero.MainHero.Name.ToString()).Replace("{EPITHET}", r.Epithet);
			return (first ? (r.BaseName + " did not die. They have come back, scarred, and the realm calls them " + r.Epithet + ".\n\nA raven comes with their seal:\n\n") : ("A raven from " + h.Name + ":\n\n")) + t;
		}

		private static void Taunt()
		{
			string me = ((MBObjectBase)Hero.MainHero).StringId;
			foreach (string key in Store.Keys(Prefix).Where((string k) => !k.StartsWith(FallPrefix) && !k.StartsWith(AwayPrefix)).ToList())
			{
				Hero h = Law.Find(key.Substring(Prefix.Length));
				Rec r = Of(h);
				if (h == null || r == null || !h.IsAlive || h.IsPrisoner || r.Enemy != me || MBRandom.RandomInt(100) >= 30)
				{
					continue;
				}
				Ravens.Popup("A Raven Sealed in Black", Line(h, r, false));
				return;
			}
		}

		// A nemesis leading men goes after the one they are sworn against.
		private static void Hunt()
		{
			foreach (string key in Store.Keys(Prefix).Where((string k) => !k.StartsWith(FallPrefix) && !k.StartsWith(AwayPrefix)).ToList())
			{
				try
				{
					Hero h = Law.Find(key.Substring(Prefix.Length));
					Rec r = Of(h);
					if (h == null || r == null || !h.IsAlive || h.IsPrisoner)
					{
						continue;
					}
					MobileParty p = h.PartyBelongedTo;
					Hero e = Law.Find(r.Enemy);
					MobileParty ep = (e != null) ? e.PartyBelongedTo : null;
					if (p == null || p.LeaderHero != h || p == MobileParty.MainParty || ep == null || p.MapEvent != null || p.Army != null || p.BesiegedSettlement != null)
					{
						continue;
					}
					if (h.MapFaction == null || ep.MapFaction == null || !FactionManager.IsAtWarAgainstFaction(h.MapFaction, ep.MapFaction))
					{
						continue;
					}
					if (p.GetPosition2D.Distance(ep.GetPosition2D) < 40f && p.MemberRoster.TotalManCount >= ep.MemberRoster.TotalManCount * 0.7f)
					{
						p.SetMoveEngageParty(ep, MobileParty.NavigationType.Default);
						Log.Write("nemesis: " + h.Name + " rides against " + e.Name);
					}
				}
				catch (Exception ex)
				{
					Log.Once("nmhunt", "nemesis: hunt failed: " + ex.Message);
				}
			}
		}

		internal static string Report()
		{
			List<string> lines = new List<string>();
			foreach (string key in Store.Keys(Prefix).Where((string k) => !k.StartsWith(FallPrefix) && !k.StartsWith(AwayPrefix)))
			{
				Hero h = Law.Find(key.Substring(Prefix.Length));
				Rec r = Of(h);
				if (h != null && r != null)
				{
					Hero e = Law.Find(r.Enemy);
					lines.Add(h.Name + " (rank " + r.Rank + ") vs " + ((e != null) ? e.Name.ToString() : "?") + (h.IsAlive ? "" : " [dead]"));
				}
			}
			foreach (string key in Store.Keys(AwayPrefix))
			{
				lines.Add("away: " + key.Substring(AwayPrefix.Length) + " returns day " + (Store.Get(key) ?? "").Split('|')[0]);
			}
			return (lines.Count == 0) ? "No nemeses." : string.Join("\n", lines);
		}
	}
}
