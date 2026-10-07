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
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The history of your house, kept for as long as the house lasts. Every
	// deed the court records lands here too, along with the battles you won,
	// the places you took, the houses you founded and the children - trueborn
	// and otherwise. A scribe writes it handsomely, and for a price will write
	// it as you would like it remembered.
	internal static class Chronicle
	{
		private const string Prefix = "hx:";
		private const string NextKey = "hxn";
		private const string OriginalPrefix = "hxo:";
		private const string ScribeKey = "sc:hired";
		private const string ScribePaidKey = "sc:paid";
		private const string ExposePrefix = "hxe:";

		internal static readonly string[] Kinds = new string[9] { "battle", "conquest", "house", "child", "bastard", "speech", "marriage", "duel", "deed" };

		internal sealed class Entry
		{
			internal int Id;
			internal int Day;
			internal string Kind = "deed";
			internal string Hero = "";
			internal string Flags = "";
			internal string Text = "";

			internal bool Struck => Flags.Contains("s");
			internal bool Forged => Flags.Contains("f");
			internal bool Amended => Flags.Contains("a");
			internal bool Exposed => Flags.Contains("x");

			internal string Pack()
			{
				return Day + "|" + Kind + "|" + Hero + "|" + Flags + "|" + Text;
			}

			internal static Entry Unpack(int id, string s)
			{
				string[] p = (s ?? "").Split(new char[1] { '|' }, 5);
				if (p.Length < 5)
				{
					return null;
				}
				Entry e = new Entry();
				e.Id = id;
				int.TryParse(p[0], out e.Day);
				e.Kind = p[1];
				e.Hero = p[2];
				e.Flags = p[3];
				e.Text = p[4];
				return e;
			}
		}

		// ------------------------------------------------------------------
		// the record

		internal static List<Entry> All()
		{
			List<Entry> list = new List<Entry>();
			foreach (string key in Store.Keys(Prefix))
			{
				int id;
				if (int.TryParse(key.Substring(Prefix.Length), out id))
				{
					Entry e = Entry.Unpack(id, Store.Get(key));
					if (e != null)
					{
						list.Add(e);
					}
				}
			}
			return list.OrderBy((Entry e) => e.Id).ToList();
		}

		private static void Save(Entry e)
		{
			Store.Set(Prefix + e.Id, e.Pack());
		}

		internal static Entry Add(string kind, string text, Hero hero = null, string flags = "")
		{
			try
			{
				if (!Cfg.Chronicle || !Store.Initialized || string.IsNullOrEmpty(text))
				{
					return null;
				}
				Entry e = new Entry();
				e.Id = Store.GetI(NextKey, 1);
				Store.SetI(NextKey, e.Id + 1);
				e.Day = CourtBehavior.Today();
				e.Kind = kind ?? "deed";
				e.Hero = (hero != null) ? ((MBObjectBase)hero).StringId : "";
				e.Flags = flags ?? "";
				e.Text = Clean(text);
				if (HasScribe && kind != "deed")
				{
					e.Text = Flourish(e.Kind, e.Text);
				}
				Save(e);
				return e;
			}
			catch (Exception ex)
			{
				Log.Once("chronicleadd", "chronicle: writing failed: " + ex.Message);
				return null;
			}
		}

		// Ledger lines start with the date; the chronicle keeps its own.
		private static string Clean(string text)
		{
			text = text.Replace("|", "/").Replace("\u001e", " ").Trim();
			int i = text.IndexOf("  ", StringComparison.Ordinal);
			if (i > 0 && i < 40 && text.Substring(0, i).Any(char.IsDigit))
			{
				text = text.Substring(i + 2).Trim();
			}
			return text;
		}

		private static readonly string[] Flourishes = new string[8]
		{
			"Let it be written that ", "The maesters will tell it thus: ", "In the year's long telling, ", "So it was recorded: ",
			"Mark it well - ", "As the singers have it, ", "Here is set down that ", "The chronicle remembers: "
		};

		private static string Flourish(string kind, string text)
		{
			if (text.Length == 0)
			{
				return text;
			}
			string f = Flourishes[MBRandom.RandomInt(Flourishes.Length)];
			return f + char.ToLowerInvariant(text[0]) + text.Substring(1);
		}

		internal static string DateOf(int day)
		{
			int dpy = Math.Max(1, Cfg.DaysPerYear);
			int year = day / dpy;
			int season = day % dpy / Math.Max(1, dpy / 4);
			string[] seasons = new string[4] { "spring", "summer", "autumn", "winter" };
			return seasons[Math.Min(3, season)] + " of year " + year;
		}

		// ------------------------------------------------------------------
		// what the world does

		internal static void OnBirth(Hero mother, List<Hero> kids)
		{
			if (kids == null)
			{
				return;
			}
			foreach (Hero k in kids)
			{
				if (k != null && (k.Father == Hero.MainHero || k.Mother == Hero.MainHero))
				{
					Hero other = (k.Father == Hero.MainHero) ? k.Mother : k.Father;
					Add("child", k.Name + " was born" + ((other != null) ? (" to you and " + other.Name) : " to you") + ".", k);
				}
			}
		}

		internal static void OnOwnerChanged(Settlement s, Hero newOwner, Hero oldOwner, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
		{
			if (s == null || newOwner == null || (newOwner.Clan != Clan.PlayerClan) || !(s.IsTown || s.IsCastle))
			{
				return;
			}
			if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
			{
				Add("conquest", "Took " + s.Name + " by storm" + ((oldOwner != null && oldOwner.Clan != null) ? (" from " + oldOwner.Clan.Name) : "") + ".", newOwner);
			}
		}

		internal static void OnMapEventEnded(MapEvent me)
		{
			try
			{
				if (!Cfg.Chronicle || me == null || !me.HasWinner || !me.InvolvedParties.Contains(PartyBase.MainParty))
				{
					BattleSpeech.Forget();
					return;
				}
				BattleSideEnum mine = PartyBase.MainParty.Side;
				MapEventSide ours = me.GetMapEventSide(mine);
				MapEventSide theirs = me.GetMapEventSide((mine == BattleSideEnum.Attacker) ? BattleSideEnum.Defender : BattleSideEnum.Attacker);
				int us = ours.TroopCount + ours.TroopCasualties;
				int them = theirs.TroopCount + theirs.TroopCasualties;
				bool won = me.WinningSide == mine;
				string foe = (theirs.LeaderParty != null) ? theirs.LeaderParty.Name.ToString() : "the enemy";
				string where = (me.MapEventSettlement != null) ? me.MapEventSettlement.Name.ToString() : Near();
				string speech = BattleSpeech.TakeForChronicle(won);
				if (us + them < Cfg.ChronicleMinBattle)
				{
					return;
				}
				string what = (me.IsSiegeAssault ? "the storming of " : (me.IsNavalMapEvent ? "the sea fight off " : "the battle near ")) + where;
				string line = (won ? "Victory at " : "Defeat at ") + what + ": " + us.ToString("N0") + " against " + them.ToString("N0") + " under " + foe + ". " +
					(won ? (theirs.TroopCasualties.ToString("N0") + " of theirs fell.") : (ours.TroopCasualties.ToString("N0") + " of ours fell."));
				if (!string.IsNullOrEmpty(speech))
				{
					line += " Before it you told your men: \"" + speech + "\"";
				}
				Add("battle", line, Hero.MainHero, won ? "w" : "l");
			}
			catch (Exception e)
			{
				Log.Once("chroniclebattle", "chronicle: a battle could not be written: " + e.Message);
			}
		}

		private static string Near()
		{
			try
			{
				Settlement s = Settlement.All.Where((Settlement x) => x.IsTown || x.IsCastle || x.IsVillage).OrderBy((Settlement x) => x.GatePosition.DistanceSquared(MobileParty.MainParty.Position)).FirstOrDefault();
				return (s != null) ? s.Name.ToString() : "the field";
			}
			catch
			{
				return "the field";
			}
		}

		// ------------------------------------------------------------------
		// the scribe

		internal static bool HasScribe => Store.Get(ScribeKey) == "1";

		internal static void Daily(int today)
		{
			if (!Cfg.Chronicle || !Store.Initialized)
			{
				return;
			}
			try
			{
				if (HasScribe && today - Store.GetI(ScribePaidKey, today) >= 21)
				{
					if (Hero.MainHero.Gold >= Cfg.ScribeWage)
					{
						Hero.MainHero.ChangeHeroGold(-Cfg.ScribeWage);
						Store.SetI(ScribePaidKey, today);
					}
					else
					{
						Store.Set(ScribeKey, null);
						Log.Write("chronicle: the scribe left unpaid");
						Ravens.Popup("The Scribe Leaves", "Your scribe was not paid this season, and has packed his inks and gone. What he wrote stays written.");
					}
				}
				foreach (string key in Store.Keys(ExposePrefix).ToList())
				{
					int due;
					if (!int.TryParse(Store.Get(key), out due) || today < due)
					{
						continue;
					}
					Store.Set(key, null);
					int id;
					if (int.TryParse(key.Substring(ExposePrefix.Length), out id))
					{
						Expose(id);
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("chronicledaily", "chronicle: the daily round failed: " + e.Message);
			}
		}

		internal static void Hire()
		{
			if (HasScribe)
			{
				Store.Set(ScribeKey, null);
				Log.Write("chronicle: scribe dismissed");
				Flow.Notify("The scribe is dismissed.");
				return;
			}
			if (Hero.MainHero.Gold < Cfg.ScribeHireCost)
			{
				Flow.Notify("You cannot afford a scribe (" + Cfg.ScribeHireCost.ToString("N0") + ").");
				return;
			}
			Hero.MainHero.ChangeHeroGold(-Cfg.ScribeHireCost);
			Store.Set(ScribeKey, "1");
			Store.SetI(ScribePaidKey, CourtBehavior.Today());
			Log.Write("chronicle: scribe hired");
			Ravens.Popup("A Scribe Is Engaged", "A grey-sleeved scribe joins your household, with his inks and his long memory. From now on your deeds are written handsomely - and if you ask him, written differently.\n\nHe costs " + Cfg.ScribeWage.ToString("N0") + " a season.");
		}

		private static List<InquiryElement> Pickable(Func<Entry, bool> ok)
		{
			return All().Where((Entry e) => !e.Struck && ok(e)).OrderByDescending((Entry e) => e.Id).Take(60)
				.Select((Entry e) => new InquiryElement(e, DateOf(e.Day) + " - " + Short(e.Text), null, true, e.Text)).ToList();
		}

		private static string Short(string s)
		{
			return (s.Length > 70) ? (s.Substring(0, 67) + "...") : s;
		}

		private static bool Pay(int cost)
		{
			if (!HasScribe)
			{
				Flow.Notify("You have no scribe.");
				return false;
			}
			if (Hero.MainHero.Gold < cost)
			{
				Flow.Notify("The scribe wants " + cost.ToString("N0") + " for that.");
				return false;
			}
			Hero.MainHero.ChangeHeroGold(-cost);
			return true;
		}

		private static void MaybeExpose(Entry e, int chance)
		{
			if (MBRandom.RandomInt(100) < chance)
			{
				int when = CourtBehavior.Today() + MBRandom.RandomInt(21, 336);
				Store.Set(ExposePrefix + e.Id, when.ToString());
				Log.Write("chronicle: entry " + e.Id + " will come out on day " + when);
			}
		}

		internal static void Amend()
		{
			List<InquiryElement> els = Pickable((Entry e) => true);
			if (els.Count == 0)
			{
				Flow.Notify("There is nothing written yet.");
				return;
			}
			Inquiry.Select("Amend the Chronicle", "Choose what the scribe should write differently. It costs " + Cfg.ScribeAmendCost.ToString("N0") + ", and lies have a way of coming out.", els, 1, 1, "Amend it", "Leave it", (List<InquiryElement> sel) =>
			{
				Entry e = (sel != null && sel.Count > 0) ? (sel[0].Identifier as Entry) : null;
				if (e == null)
				{
					return;
				}
				Inquiry.Text("Amend the Chronicle", "As it stands:\n\n" + e.Text + "\n\nWrite it as it should be remembered.", e.Text, "Write it", "Leave it", (string text) =>
				{
					if (string.IsNullOrEmpty(text) || text.Trim() == e.Text || !Pay(Cfg.ScribeAmendCost))
					{
						return;
					}
					if (Store.Get(OriginalPrefix + e.Id) == null)
					{
						Store.Set(OriginalPrefix + e.Id, e.Text);
					}
					e.Text = Clean(text);
					if (!e.Flags.Contains("a"))
					{
						e.Flags += "a";
					}
					Save(e);
					MaybeExpose(e, Cfg.ScribeExposeChance);
					Log.Write("chronicle: entry " + e.Id + " amended");
					Flow.Notify("The scribe scrapes the vellum and writes it again.");
				}, null);
			});
		}

		internal static void Strike()
		{
			List<InquiryElement> els = Pickable((Entry e) => true);
			if (els.Count == 0)
			{
				Flow.Notify("There is nothing written yet.");
				return;
			}
			Inquiry.Select("Strike from the Chronicle", "Choose what should never have happened. It costs " + Cfg.ScribeStrikeCost.ToString("N0") + ".", els, 1, 1, "Strike it", "Leave it", (List<InquiryElement> sel) =>
			{
				Entry e = (sel != null && sel.Count > 0) ? (sel[0].Identifier as Entry) : null;
				if (e == null || !Pay(Cfg.ScribeStrikeCost))
				{
					return;
				}
				e.Flags += "s";
				Save(e);
				MaybeExpose(e, Cfg.ScribeExposeChance);
				Log.Write("chronicle: entry " + e.Id + " struck");
				Flow.Notify("It is gone from the page, if not from men's memories.");
			});
		}

		internal static void Invent()
		{
			if (!HasScribe)
			{
				Flow.Notify("You have no scribe.");
				return;
			}
			List<InquiryElement> els = new List<InquiryElement>
			{
				new InquiryElement("battle", "A victory", null, true, "A battle you won."),
				new InquiryElement("conquest", "A conquest", null, true, "A place you took."),
				new InquiryElement("house", "A house founded", null, true, "A house that owes its being to you."),
				new InquiryElement("deed", "A deed", null, true, "Anything at all.")
			};
			Inquiry.Select("Invent a Deed", "The scribe can write what never happened, for " + Cfg.ScribeInventCost.ToString("N0") + ". The bolder the lie, the likelier it comes out.", els, 1, 1, "Go on", "No", (List<InquiryElement> sel) =>
			{
				string kind = (sel != null && sel.Count > 0) ? (sel[0].Identifier as string) : null;
				if (kind == null)
				{
					return;
				}
				Inquiry.Text("Invent a Deed", "What should the chronicle say you did?", "", "Write it", "No", (string text) =>
				{
					if (string.IsNullOrEmpty(text) || !Pay(Cfg.ScribeInventCost))
					{
						return;
					}
					Entry e = Add(kind, text, Hero.MainHero, (kind == "battle") ? "fw" : "f");
					if (e != null)
					{
						MaybeExpose(e, Cfg.ScribeInventExposeChance);
						Log.Write("chronicle: entry " + e.Id + " invented (" + kind + ")");
						Flow.Notify("It is written. Now it is history.");
					}
				}, null);
			});
		}

		private static void Expose(int id)
		{
			Entry e = Entry.Unpack(id, Store.Get(Prefix + id));
			if (e == null || e.Exposed)
			{
				return;
			}
			string original = Store.Get(OriginalPrefix + id);
			string what = e.Forged ? ("that you " + e.Text.TrimEnd('.') + " - which never happened") : (e.Struck ? "what you had struck out" : "what was really written before your scribe went at it");
			e.Flags += "x";
			if (e.Struck)
			{
				e.Flags = e.Flags.Replace("s", "");
			}
			if (original != null)
			{
				e.Text = original;
			}
			e.Text += " (The chronicle was tampered with here, and everyone knows it.)";
			Save(e);
			Standing.Change(-Cfg.ScribeExposeHonour, 0, "a falsified chronicle came out");
			Log.Write("chronicle: entry " + id + " exposed");
			Ravens.Popup("The Truth Comes Out", "Someone has read your chronicle against the memories of men who were there. The realm now knows " + what + ".");
		}

		// ------------------------------------------------------------------
		// reading it

		internal static string Counts(Func<Entry, bool> mine)
		{
			List<Entry> all = All().Where((Entry e) => !e.Struck && !e.Exposed && mine(e)).ToList();
			int won = all.Count((Entry e) => e.Kind == "battle" && e.Flags.Contains("w"));
			int lost = all.Count((Entry e) => e.Kind == "battle" && e.Flags.Contains("l"));
			int took = all.Count((Entry e) => e.Kind == "conquest");
			int houses = all.Count((Entry e) => e.Kind == "house");
			int kids = all.Count((Entry e) => e.Kind == "child");
			int bastards = all.Count((Entry e) => e.Kind == "bastard");
			int speeches = all.Count((Entry e) => e.Kind == "speech");
			return "Battles won " + won + ", lost " + lost + ". Places taken " + took + ". Houses founded " + houses + ". Children " + kids + ", baseborn " + bastards + ". Speeches remembered " + speeches + ".";
		}

		internal static string Text(Func<Entry, bool> filter, int max)
		{
			StringBuilder sb = new StringBuilder();
			foreach (Entry e in All().Where((Entry x) => !x.Struck && filter(x)).OrderByDescending((Entry x) => x.Id).Take(max))
			{
				sb.Append(DateOf(e.Day)).Append(": ").Append(e.Text).Append("\n\n");
			}
			return sb.ToString().TrimEnd();
		}

		internal static void Read()
		{
			string body = Text((Entry e) => true, Cfg.ChronicleShown);
			Ravens.Popup("The Chronicle of " + Clan.PlayerClan.Name, Counts((Entry e) => true) + "\n\n" + (body.Length > 0 ? body : "Nothing is written yet.") + (HasScribe ? "" : "\n\n(Without a scribe, only the bare facts are kept.)"));
		}

		// The encyclopedia: your page carries your house's chronicle, a kinsman's
		// page whatever names them.
		internal static void Patch(Harmony h)
		{
			try
			{
				Type t = AccessTools.TypeByName("TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages.EncyclopediaHeroPageVM");
				System.Reflection.MethodInfo m = (t != null) ? AccessTools.Method(t, "UpdateInformationText") : null;
				if (m == null)
				{
					Log.Write("chronicle: the encyclopedia page was not found");
					return;
				}
				h.Patch(m, null, new HarmonyMethod(typeof(Chronicle).GetMethod("PagePost", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)));
				Log.Write("chronicle: written into the encyclopedia");
			}
			catch (Exception e)
			{
				Log.Write("chronicle: encyclopedia patch failed: " + e.Message);
			}
		}

		private static void PagePost(object __instance)
		{
			try
			{
				if (!Cfg.Chronicle || !Store.Initialized || Clan.PlayerClan == null)
				{
					return;
				}
				Hero h = Traverse.Create(__instance).Field("_hero").GetValue<Hero>();
				if (h == null || h.Clan != Clan.PlayerClan)
				{
					return;
				}
				string body;
				string head;
				if (h == Hero.MainHero)
				{
					head = "THE CHRONICLE OF " + Clan.PlayerClan.Name.ToString().ToUpperInvariant() + "\n" + Counts((Entry e) => true);
					body = Text((Entry e) => true, Cfg.ChronicleShown);
				}
				else
				{
					string id = ((MBObjectBase)h).StringId;
					string name = h.FirstName != null ? h.FirstName.ToString() : h.Name.ToString();
					head = "IN THE CHRONICLE OF " + Clan.PlayerClan.Name.ToString().ToUpperInvariant();
					body = Text((Entry e) => e.Hero == id || e.Text.Contains(name), 15);
				}
				if (string.IsNullOrEmpty(body) && h != Hero.MainHero)
				{
					return;
				}
				Traverse prop = Traverse.Create(__instance).Property("InformationText");
				string current = prop.GetValue<string>() ?? "";
				prop.SetValue(current + "\n\n" + head + "\n\n" + (string.IsNullOrEmpty(body) ? "Nothing is written yet." : body));
			}
			catch (Exception e)
			{
				Log.Once("chroniclepage", "chronicle: the encyclopedia page failed: " + e.Message);
			}
		}
	}
}
