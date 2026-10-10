using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The world remembers. Every lord, house and realm keeps a short history
	// of the things worth remembering - great battles, deaths, sieges,
	// marriages, crowns - and nothing that is not.
	internal static class History
	{
		private const char Sep = '\u001e';
		private const int Cap = 30;

		internal static string KeyOf(Hero h) { return (h != null) ? ("hh:" + ((MBObjectBase)h).StringId) : null; }
		internal static string KeyOf(Clan c) { return (c != null) ? ("hc:" + ((MBObjectBase)c).StringId) : null; }
		internal static string KeyOf(Kingdom k) { return (k != null) ? ("hk:" + ((MBObjectBase)k).StringId) : null; }

		// ------------------------------------------------------------------
		// writing

		private static void Put(string key, string text, int weight)
		{
			if (key == null || string.IsNullOrEmpty(text) || !Cfg.Histories || !Store.Initialized)
			{
				return;
			}
			text = text.Replace("|", "/").Replace(Sep, ' ').Trim();
			List<string> list = Raw(key);
			string line = CourtBehavior.Today() + "|" + weight + "|" + text;
			if (list.Count > 0 && list[list.Count - 1].EndsWith("|" + text))
			{
				return;
			}
			list.Add(line);
			while (list.Count > Cap)
			{
				// Drop the oldest of the least weighty; the great things stay.
				int min = list.Min((string l) => WeightOf(l));
				int i = list.FindIndex((string l) => WeightOf(l) == min);
				list.RemoveAt(i);
			}
			Store.Set(key, string.Join(Sep.ToString(), list));
		}

		private static int WeightOf(string line)
		{
			string[] p = line.Split(new char[1] { '|' }, 3);
			int w;
			return (p.Length > 1 && int.TryParse(p[1], out w)) ? w : 1;
		}

		private static List<string> Raw(string key)
		{
			string s = Store.Get(key);
			return string.IsNullOrEmpty(s) ? new List<string>() : s.Split(Sep).Where((string x) => x.Length > 0).ToList();
		}

		internal static void Hero(Hero h, string text, int weight = 2)
		{
			if (h == null || (weight < 2 && !Mine(h)))
			{
				return;
			}
			Put(KeyOf(h), text, weight);
		}

		internal static void House(Clan c, string text, int weight = 2)
		{
			if (c == null || c.IsBanditFaction)
			{
				return;
			}
			Put(KeyOf(c), text, weight);
		}

		internal static void Realm(Kingdom k, string text, int weight = 2)
		{
			Put(KeyOf(k), text, weight);
		}

		internal static void Realm(IFaction f, string text, int weight = 2)
		{
			Realm(f as Kingdom, text, weight);
		}

		private static bool Mine(Hero h)
		{
			return h != null && Clan.PlayerClan != null && h.Clan == Clan.PlayerClan;
		}

		// ------------------------------------------------------------------
		// reading

		internal static string DateOf(int day)
		{
			int dpy = Math.Max(1, Cfg.DaysPerYear);
			string[] seasons = new string[4] { "Spring", "Summer", "Autumn", "Winter" };
			return seasons[Math.Min(3, day % dpy / Math.Max(1, dpy / 4))] + ", year " + (day / dpy);
		}

		internal static List<KeyValuePair<string, string>> Lines(string key)
		{
			List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
			if (key == null)
			{
				return list;
			}
			foreach (string l in Raw(key).AsEnumerable().Reverse())
			{
				string[] p = l.Split(new char[1] { '|' }, 3);
				int day;
				if (p.Length == 3 && int.TryParse(p[0], out day))
				{
					list.Add(new KeyValuePair<string, string>(DateOf(day), p[2]));
				}
			}
			return list;
		}

		internal static string Text(string key)
		{
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, string> kv in Lines(key))
			{
				sb.Append(kv.Key).Append(" - ").Append(kv.Value).Append("\n");
			}
			return sb.ToString().TrimEnd();
		}

		// ------------------------------------------------------------------
		// what the world does

		private static string N(Hero h) { return (h != null) ? h.Name.ToString() : "someone"; }

		internal static void OnBattle(MapEvent me)
		{
			try
			{
				if (!Cfg.Histories || me == null || !me.HasWinner)
				{
					return;
				}
				MapEventSide won = me.GetMapEventSide(me.WinningSide);
				MapEventSide lost = me.GetMapEventSide(me.DefeatedSide);
				int total = won.TroopCount + won.TroopCasualties + lost.TroopCount + lost.TroopCasualties;
				bool lordFell = lost.Parties.Concat(won.Parties).Any((MapEventParty p) => p.Party != null && p.Party.LeaderHero != null && p.Party.LeaderHero.DeathMark != KillCharacterAction.KillCharacterActionDetail.None);
				if (total < Cfg.HistoryBattleMin && !lordFell)
				{
					return;
				}
				Hero wl = (won.LeaderParty != null) ? won.LeaderParty.LeaderHero : null;
				Hero ll = (lost.LeaderParty != null) ? lost.LeaderParty.LeaderHero : null;
				string where = (me.MapEventSettlement != null) ? me.MapEventSettlement.Name.ToString() : Near(me);
				string what = me.IsSiegeAssault ? "the storming of " + where : (me.IsNavalMapEvent ? "the sea off " + where : where);
				int theirs = lost.TroopCount + lost.TroopCasualties;
				int ours = won.TroopCount + won.TroopCasualties;
				foreach (MapEventParty p in won.Parties)
				{
					Hero h = (p.Party != null) ? p.Party.LeaderHero : null;
					Hero(h, (h == wl ? "Led the victory at " : "Fought in the victory at ") + what + ": " + ours.ToString("N0") + " against " + theirs.ToString("N0") + (ll != null ? (" under " + N(ll)) : "") + ".", 3);
				}
				foreach (MapEventParty p in lost.Parties)
				{
					Hero h = (p.Party != null) ? p.Party.LeaderHero : null;
					Hero(h, (h == ll ? "Was broken at " : "Shared the defeat at ") + what + (wl != null ? (" by " + N(wl)) : "") + ", " + lost.TroopCasualties.ToString("N0") + " of " + theirs.ToString("N0") + " lost.", 3);
				}
				if (wl != null)
				{
					House(wl.Clan, N(wl) + " won the battle of " + what + (ll != null ? (" against " + N(ll)) : "") + ".", 3);
					Realm(won.MapFaction, "Victory at " + what + ": " + N(wl) + " broke " + (ll != null ? N(ll) : "the enemy") + " (" + ours.ToString("N0") + " against " + theirs.ToString("N0") + ").", 3);
				}
				if (ll != null)
				{
					House(ll.Clan, N(ll) + " was beaten at " + what + ".", 3);
					Realm(lost.MapFaction, "Defeat at " + what + ".", 3);
				}
			}
			catch (Exception e)
			{
				Log.Once("histbattle", "history: a battle could not be written: " + e.Message);
			}
		}

		private static string Near(MapEvent me)
		{
			try
			{
				Settlement s = Settlement.All.Where((Settlement x) => x.IsTown || x.IsCastle || x.IsVillage).OrderBy((Settlement x) => x.GatePosition.DistanceSquared(me.Position)).FirstOrDefault();
				return (s != null) ? s.Name.ToString() : "the field";
			}
			catch
			{
				return "the field";
			}
		}

		internal static void OnKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail)
		{
			try
			{
				if (victim == null || !victim.IsLord)
				{
					return;
				}
				bool great = victim.Clan != null && (victim.Clan.Leader == victim || (victim.Clan.Kingdom != null && victim.Clan.Kingdom.Leader == victim));
				string how;
				switch (detail)
				{
				case KillCharacterAction.KillCharacterActionDetail.DiedInBattle:
					how = "Fell in battle" + (killer != null ? (" to " + N(killer)) : "") + ".";
					break;
				case KillCharacterAction.KillCharacterActionDetail.Executed:
				case KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent:
					how = "Was executed" + (killer != null ? (" by " + N(killer)) : "") + ".";
					break;
				case KillCharacterAction.KillCharacterActionDetail.Murdered:
					how = "Was murdered" + (killer != null ? (" by " + N(killer)) : "") + ".";
					break;
				case KillCharacterAction.KillCharacterActionDetail.DiedInLabor:
					how = "Died in childbed.";
					break;
				case KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge:
					if (!great && !Mine(victim))
					{
						return;
					}
					how = "Died of old age, at " + (int)victim.Age + ".";
					break;
				default:
					if (!great && !Mine(victim))
					{
						return;
					}
					how = "Died.";
					break;
				}
				Hero(victim, how, 3);
				if (killer != null && killer.IsLord && detail != KillCharacterAction.KillCharacterActionDetail.DiedOfOldAge)
				{
					Hero(killer, (detail == KillCharacterAction.KillCharacterActionDetail.DiedInBattle ? "Slew " : "Put to death ") + N(victim) + (victim.Clan != null ? (" of " + victim.Clan.Name) : "") + ".", 3);
				}
				if (victim.Clan != null)
				{
					House(victim.Clan, N(victim) + " " + char.ToLowerInvariant(how[0]) + how.Substring(1), great ? 3 : 2);
				}
				if (great && victim.Clan != null && victim.Clan.Kingdom != null && victim.Clan.Kingdom.Leader == victim)
				{
					Realm(victim.Clan.Kingdom, "The ruler " + N(victim) + " " + char.ToLowerInvariant(how[0]) + how.Substring(1), 3);
				}
			}
			catch (Exception e)
			{
				Log.Once("histkill", "history: a death could not be written: " + e.Message);
			}
		}

		internal static void OnOwnerChanged(Settlement s, Hero newOwner, Hero oldOwner, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (s == null || !(s.IsTown || s.IsCastle) || detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
			{
				return;
			}
			string place = (s.IsTown ? "the city of " : "") + s.Name;
			Hero(newOwner, "Took " + place + " by siege" + (oldOwner != null ? (" from " + N(oldOwner)) : "") + ".", 3);
			Hero(oldOwner, "Lost " + place + " to " + N(newOwner) + ".", 3);
			if (newOwner != null)
			{
				House(newOwner.Clan, N(newOwner) + " took " + place + ".", 3);
				Realm(newOwner.MapFaction, place.Substring(0, 1).ToUpper() + place.Substring(1) + " was won.", 2);
			}
			if (oldOwner != null)
			{
				House(oldOwner.Clan, place.Substring(0, 1).ToUpper() + place.Substring(1) + " was lost to " + N(newOwner) + ".", 3);
				Realm(oldOwner.MapFaction, place.Substring(0, 1).ToUpper() + place.Substring(1) + " fell to " + ((newOwner != null && newOwner.MapFaction != null) ? newOwner.MapFaction.Name.ToString() : "the enemy") + ".", 2);
			}
		}

		internal static void OnCaptured(PartyBase captor, Hero prisoner)
		{
			if (prisoner == null || !prisoner.IsLord)
			{
				return;
			}
			Hero by = (captor != null) ? captor.LeaderHero : null;
			bool great = prisoner.Clan != null && (prisoner.Clan.Leader == prisoner || (prisoner.Clan.Kingdom != null && prisoner.Clan.Kingdom.Leader == prisoner));
			if (!great && !Mine(prisoner) && !Mine(by))
			{
				return;
			}
			Hero(prisoner, "Was taken prisoner" + (by != null ? (" by " + N(by)) : "") + ".", 2);
			Hero(by, "Took " + N(prisoner) + " prisoner.", 2);
		}

		internal static void OnBirth(Hero mother, List<Hero> kids)
		{
			if (kids == null)
			{
				return;
			}
			foreach (Hero k in kids.Where((Hero x) => x != null))
			{
				Hero father = k.Father;
				Hero(mother, "Bore " + N(k) + ".", 2);
				Hero(father, "Fathered " + N(k) + ".", 2);
				Hero(k, "Was born to " + N(father) + " and " + N(mother) + ".", 2);
			}
		}

		internal static void OnMarried(Hero a, Hero b)
		{
			if (a == null || b == null || !(a.IsLord || b.IsLord))
			{
				return;
			}
			Hero(a, "Married " + N(b) + (b.Clan != null ? (" of " + b.Clan.Name) : "") + ".", 2);
			Hero(b, "Married " + N(a) + (a.Clan != null ? (" of " + a.Clan.Name) : "") + ".", 2);
			if (a.Clan != null && a.Clan == b.Clan)
			{
				return;
			}
			House(a.Clan, N(a) + " married " + N(b) + ".", 1);
			House(b.Clan, N(b) + " married " + N(a) + ".", 1);
		}

		internal static void OnLeaderChanged(Hero now, Hero before)
		{
			if (now == null || now.Clan == null)
			{
				return;
			}
			Hero(now, "Became head of " + now.Clan.Name + (before != null ? (" after " + N(before)) : "") + ".", 3);
			House(now.Clan, N(now) + " became head of the house.", 3);
		}

		internal static void OnRulingClanChanged(Kingdom k, Clan c)
		{
			if (k == null || c == null || c.Leader == null)
			{
				return;
			}
			Hero(c.Leader, "Took the crown of " + k.Name + ".", 3);
			House(c, N(c.Leader) + " took the crown of " + k.Name + ".", 3);
			Realm(k, N(c.Leader) + " of " + c.Name + " came to the throne.", 3);
		}

		internal static void OnClanChangedKingdom(Clan c, Kingdom from, Kingdom to, ChangeKingdomAction.ChangeKingdomActionDetail detail)
		{
			if (c == null || c.IsBanditFaction || c.IsMinorFaction)
			{
				return;
			}
			string d = detail.ToString();
			if (d.Contains("Rebellion") || (from != null && to != null && from != to && !d.Contains("Mercenary")))
			{
				string line = (to != null) ? ((from != null ? ("Left " + from.Name + " and swore to ") : "Swore to ") + to.Name + ".") : ("Left " + (from != null ? from.Name.ToString() : "its realm") + ".");
				House(c, line, 2);
				if (from != null)
				{
					Realm(from, c.Name + " left the realm" + (to != null ? (" for " + to.Name) : "") + ".", 2);
				}
				if (to != null)
				{
					Realm(to, c.Name + " joined the realm.", 1);
				}
			}
		}

		internal static void OnClanDestroyed(Clan c)
		{
			if (c == null || c.IsBanditFaction || c.IsMinorFaction)
			{
				return;
			}
			House(c, "The house came to an end.", 3);
			if (c.Kingdom != null)
			{
				Realm(c.Kingdom, c.Name + " is no more.", 2);
			}
		}

		internal static void OnWar(IFaction a, IFaction b)
		{
			if (a is Kingdom && b is Kingdom)
			{
				Realm(a, "Went to war with " + b.Name + ".", 2);
				Realm(b, a.Name + " declared war.", 2);
			}
		}

		internal static void OnPeace(IFaction a, IFaction b)
		{
			if (a is Kingdom && b is Kingdom)
			{
				Realm(a, "Made peace with " + b.Name + ".", 2);
				Realm(b, "Made peace with " + a.Name + ".", 2);
			}
		}

		internal static void OnKingdomCreated(Kingdom k)
		{
			if (k == null)
			{
				return;
			}
			Realm(k, "The realm was founded" + (k.Leader != null ? (" by " + N(k.Leader)) : "") + ".", 3);
			Hero(k.Leader, "Founded the realm of " + k.Name + ".", 3);
		}

		internal static void OnKingdomDestroyed(Kingdom k)
		{
			Realm(k, "The realm fell.", 3);
		}

		// ------------------------------------------------------------------
		// the encyclopedia: a History section on hero, house and realm pages

		internal static void Patch(Harmony h)
		{
			Post(h, "TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaHeroPageVM", "UpdateInformationText", "HeroPost");
			Post(h, "TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaClanPageVM", "Refresh", "ClanPost");
			Post(h, "TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaFactionPageVM", "Refresh", "RealmPost");
		}

		private static void Post(Harmony h, string type, string method, string post)
		{
			try
			{
				Type t = AccessTools.TypeByName(type);
				System.Reflection.MethodInfo m = (t != null) ? AccessTools.Method(t, method) : null;
				if (m == null)
				{
					Log.Write("history: " + type + "." + method + " not found");
					return;
				}
				h.Patch(m, null, new HarmonyMethod(typeof(History).GetMethod(post, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)));
			}
			catch (Exception e)
			{
				Log.Write("history: patching " + method + " failed: " + e.Message);
			}
		}

		private static void Append(object vm, string key, string title)
		{
			if (!Cfg.Histories || !Store.Initialized || key == null)
			{
				return;
			}
			string body = Text(key);
			if (string.IsNullOrEmpty(body))
			{
				return;
			}
			Traverse prop = Traverse.Create(vm).Property("InformationText");
			string current = prop.GetValue<string>() ?? "";
			if (current.Contains("\n\n" + title + "\n"))
			{
				return;
			}
			prop.SetValue(current + "\n\n" + title + "\n" + body);
		}

		private static void HeroPost(object __instance)
		{
			try
			{
				Hero h = Traverse.Create(__instance).Field("_hero").GetValue<Hero>();
				string extra = Nemesis.Describe(h);
				Append(__instance, KeyOf(h), "HISTORY" + (string.IsNullOrEmpty(extra) ? "" : ("\n" + extra)));
			}
			catch (Exception e)
			{
				Log.Once("histhero", "history: hero page failed: " + e.Message);
			}
		}

		private static void ClanPost(object __instance)
		{
			try
			{
				Clan c = Traverse.Create(__instance).Field("_clan").GetValue<Clan>();
				Append(__instance, KeyOf(c), "HISTORY OF THE HOUSE");
			}
			catch (Exception e)
			{
				Log.Once("histclan", "history: house page failed: " + e.Message);
			}
		}

		private static void RealmPost(object __instance)
		{
			try
			{
				Kingdom k = Traverse.Create(__instance).Field("_faction").GetValue<Kingdom>();
				Append(__instance, KeyOf(k), "HISTORY OF THE REALM");
			}
			catch (Exception e)
			{
				Log.Once("histrealm", "history: realm page failed: " + e.Message);
			}
		}
	}
}
