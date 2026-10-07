using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Before the lines meet, the commander rides out and speaks. You choose
	// what kind of speech; the words are put together fresh each time from
	// who you are, who you face and where you stand. The men cheer, or they
	// don't. Then the other side answers.
	internal sealed class BattleSpeech : MissionLogic
	{
		private const int TimeRequestId = 724601;

		private sealed class Line
		{
			internal string Text;
			internal BasicCharacterObject Speaker;
		}

		// what is said this battle, for the chronicle
		private static string _spoken;
		private static bool _great;
		private static string _theme;

		private int _state;
		private DateTime _since;
		private readonly Queue<Line> _lines = new Queue<Line>();
		private DateTime _nextLine;
		private Action _afterLines;
		private bool _slowed;

		private readonly Dictionary<string, string> _tokens = new Dictionary<string, string>();
		private Hero _foe;
		private int _us;
		private int _them;
		private bool _siege;
		private bool _defending;

		// ------------------------------------------------------------------
		// joining a battle

		internal static void OnMissionStarted(IMission im)
		{
			try
			{
				Mission m = im as Mission;
				if (!Cfg.Speeches || m == null || !Store.Initialized)
				{
					return;
				}
				MapEvent me = MapEvent.PlayerMapEvent;
				if (me == null || !(me.IsFieldBattle || me.IsSiegeAssault || me.IsSallyOut || me.IsSiegeOutside || me.IsSiegeAmbush))
				{
					return;
				}
				MapEventSide ours = me.GetMapEventSide(PartyBase.MainParty.Side);
				if (ours == null || ours.LeaderParty != PartyBase.MainParty)
				{
					return;
				}
				int total = me.AttackerSide.TroopCount + me.DefenderSide.TroopCount;
				if (total < Cfg.SpeechMinTroops)
				{
					return;
				}
				_spoken = null;
				_great = false;
				m.AddMissionBehavior(new BattleSpeech());
				Log.Write("speech: battle of " + total + " - the commander will speak");
			}
			catch (Exception e)
			{
				Log.Once("speechstart", "speech: joining the battle failed: " + e.Message);
			}
		}

		internal static void Forget()
		{
			_spoken = null;
			_great = false;
		}

		// After the battle: the words for the chronicle, and fame for a great one.
		internal static string TakeForChronicle(bool won)
		{
			string s = _spoken;
			bool great = _great;
			string theme = _theme;
			Forget();
			if (s == null)
			{
				return null;
			}
			Chronicle.Add("speech", "Before battle you spoke of " + ThemeName(theme) + ": \"" + s + "\"" + (won ? (great ? " The men remembered it, and won." : " The day was won.") : " The day was lost all the same."), Hero.MainHero);
			if (won && great)
			{
				Clan.PlayerClan.AddRenown(Cfg.SpeechFameRenown, true);
				if (theme == "fear")
				{
					Standing.Change(0, 1, "a speech of terror before a victory");
				}
				else if (theme == "honour" || theme == "house")
				{
					Standing.Change(1, 0, "a speech remembered after a victory");
				}
				Log.Write("speech: remembered - +" + Cfg.SpeechFameRenown + " renown");
			}
			return s;
		}

		// ------------------------------------------------------------------
		// the mission

		public override void OnMissionTick(float dt)
		{
			try
			{
				Tick();
			}
			catch (Exception e)
			{
				Log.Once("speechtick", "speech: " + e.Message);
				_state = 9;
				Release();
			}
		}

		private void Tick()
		{
			Mission m = Mission;
			if (m == null || _state >= 9)
			{
				return;
			}
			if (_state == 0)
			{
				if (m.Mode != MissionMode.Battle || Agent.Main == null || !Agent.Main.IsActive())
				{
					return;
				}
				_state = 1;
				_since = DateTime.UtcNow;
				return;
			}
			if (_state == 1)
			{
				if ((DateTime.UtcNow - _since).TotalSeconds < 1.5)
				{
					return;
				}
				_state = 2;
				Ask();
				return;
			}
			if (_state == 3 && DateTime.UtcNow >= _nextLine)
			{
				if (_lines.Count > 0)
				{
					Line l = _lines.Dequeue();
					MBInformationManager.AddQuickInformation(new TextObject(l.Text, null), 1500, l.Speaker, null, "");
					InformationManager.DisplayMessage(new InformationMessage(l.Text, Colors.White));
					_nextLine = DateTime.UtcNow.AddSeconds(Math.Max(3.0, l.Text.Length / 18.0));
				}
				else
				{
					_state = 4;
					Action a = _afterLines;
					_afterLines = null;
					if (a != null)
					{
						a();
					}
				}
			}
		}

		protected override void OnEndMission()
		{
			Release();
		}

		private void Slow()
		{
			if (!_slowed && Mission != null)
			{
				Mission.AddTimeSpeedRequest(new Mission.TimeSpeedRequest(0.05f, TimeRequestId));
				_slowed = true;
			}
		}

		private void Release()
		{
			try
			{
				if (_slowed && Mission != null)
				{
					Mission.RemoveTimeSpeedRequest(TimeRequestId);
				}
			}
			catch
			{
			}
			_slowed = false;
		}

		// ------------------------------------------------------------------
		// choosing

		private void Gather()
		{
			MapEvent me = MapEvent.PlayerMapEvent;
			BattleSideEnum side = PartyBase.MainParty.Side;
			MapEventSide ours = me.GetMapEventSide(side);
			MapEventSide theirs = me.GetMapEventSide((side == BattleSideEnum.Attacker) ? BattleSideEnum.Defender : BattleSideEnum.Attacker);
			_us = ours.TroopCount;
			_them = theirs.TroopCount;
			_siege = me.IsSiegeAssault || me.IsSallyOut || me.IsSiegeOutside;
			_defending = side == BattleSideEnum.Defender;
			_foe = (theirs.LeaderParty != null) ? theirs.LeaderParty.LeaderHero : null;
			Hero h = Hero.MainHero;
			_tokens["ME"] = h.FirstName != null ? h.FirstName.ToString() : h.Name.ToString();
			_tokens["HOUSE"] = Clan.PlayerClan.Name.ToString();
			_tokens["REALM"] = (Clan.PlayerClan.Kingdom != null) ? Clan.PlayerClan.Kingdom.Name.ToString() : Clan.PlayerClan.Name.ToString();
			_tokens["FOE"] = (_foe != null) ? _foe.Name.ToString() : ((theirs.LeaderParty != null) ? theirs.LeaderParty.Name.ToString() : "the enemy");
			_tokens["FOEHOUSE"] = (_foe != null && _foe.Clan != null) ? _foe.Clan.Name.ToString() : "their kind";
			_tokens["FOEREALM"] = (theirs.MapFaction != null) ? theirs.MapFaction.Name.ToString() : "the enemy";
			_tokens["PLACE"] = (me.MapEventSettlement != null) ? me.MapEventSettlement.Name.ToString() : NearName();
			DragonRec d = DragonDuel.DragonOf(h);
			_tokens["DRAGON"] = (d != null) ? d.Name : "the dragon";
			_tokens["US"] = _us.ToString("N0");
			_tokens["THEM"] = _them.ToString("N0");
		}

		private static string NearName()
		{
			try
			{
				TaleWorlds.CampaignSystem.Settlements.Settlement s = TaleWorlds.CampaignSystem.Settlements.Settlement.All.Where((TaleWorlds.CampaignSystem.Settlements.Settlement x) => x.IsTown || x.IsCastle || x.IsVillage)
					.OrderBy((TaleWorlds.CampaignSystem.Settlements.Settlement x) => x.GatePosition.DistanceSquared(MobileParty.MainParty.Position)).FirstOrDefault();
				return (s != null) ? s.Name.ToString() : "this field";
			}
			catch
			{
				return "this field";
			}
		}

		private bool Hated()
		{
			if (_foe == null)
			{
				return false;
			}
			if (_foe.GetRelation(Hero.MainHero) <= -30)
			{
				return true;
			}
			try
			{
				return _foe.Clan != null && Attainder.Of(_foe.Clan) != null;
			}
			catch
			{
				return false;
			}
		}

		private List<string> Themes()
		{
			List<string> t = new List<string> { "honour", "fear", "gold", "house" };
			if (Dragons.Rides(Hero.MainHero))
			{
				t.Insert(0, "dragon");
			}
			if (Hated())
			{
				t.Insert(0, "vengeance");
			}
			if (_us * 3 < _them * 2)
			{
				t.Insert(0, "laststand");
			}
			if (_siege)
			{
				t.Insert(0, "walls");
			}
			return t.Take(6).ToList();
		}

		private int Fit(string theme)
		{
			switch (theme)
			{
			case "honour": return (Store.Honour >= 60) ? 15 : ((Store.Honour < 30) ? -15 : 0);
			case "fear": return (Store.Dread >= 40) ? 15 : ((Store.Dread < 15) ? -10 : 0);
			case "gold": return (Hero.MainHero.Gold > 500000) ? 10 : 0;
			case "house": return (Clan.PlayerClan.Tier >= 4 || (Clan.PlayerClan.Kingdom != null && Clan.PlayerClan.Kingdom.RulingClan == Clan.PlayerClan)) ? 10 : 0;
			case "dragon": return 20;
			case "vengeance": return 20;
			case "laststand": return 15;
			case "walls": return 10;
			}
			return 0;
		}

		internal static string ThemeName(string theme)
		{
			switch (theme)
			{
			case "honour": return "honour";
			case "fear": return "what is done to those who stand against you";
			case "gold": return "gold and plunder";
			case "house": return "house and realm";
			case "dragon": return "fire and blood";
			case "vengeance": return "vengeance";
			case "laststand": return "standing against the odds";
			case "walls": return "the walls";
			}
			return "the day ahead";
		}

		private static string Hint(string theme)
		{
			switch (theme)
			{
			case "honour": return "Speak of oaths kept and a name worth dying for. Lands best when you are known to be honourable.";
			case "fear": return "Remind them what you do to your enemies - and to cowards. Lands best when you are dreaded.";
			case "gold": return "Promise them the spoils. Every soldier understands it.";
			case "house": return "Speak of your banner and your realm. Lands best from a great house or a crown.";
			case "dragon": return "You ride a dragon. Let them remember it.";
			case "vengeance": return "This enemy has wronged you. Say so.";
			case "laststand": return "You are outnumbered. Make it a song.";
			case "walls": return "Stone and blood: the walls must fall, or hold.";
			}
			return "";
		}

		private void Ask()
		{
			Gather();
			Slow();
			Hero h = Hero.MainHero;
			int lead = h.GetSkillValue(DefaultSkills.Leadership);
			int charm = h.GetSkillValue(DefaultSkills.Charm);
			List<InquiryElement> els = Themes().Select((string t) => new InquiryElement(t, char.ToUpperInvariant(ThemeName(t)[0]) + ThemeName(t).Substring(1), null, true, Hint(t) + "\n\nYour chance: about " + Chance(t, lead, charm) + "%.")).ToList();
			Inquiry.Select("Before the Battle", _tokens["US"] + " of yours face " + _tokens["THEM"] + " under " + _tokens["FOE"] + ". The men are looking at you. What do you tell them?", els, 1, 1, "Speak", "Say nothing", (List<InquiryElement> sel) =>
			{
				string theme = (sel != null && sel.Count > 0) ? (sel[0].Identifier as string) : null;
				if (theme == null)
				{
					Silent();
					return;
				}
				Speak(theme, lead, charm);
			}, Silent);
		}

		private int Chance(string theme, int lead, int charm)
		{
			return Math.Max(10, Math.Min(95, 35 + lead / 5 + charm / 8 + Fit(theme)));
		}

		private void Silent()
		{
			Release();
			_state = 9;
			Log.Write("speech: you said nothing");
		}

		private void Speak(string theme, int lead, int charm)
		{
			Release();
			int chance = Chance(theme, lead, charm);
			int roll = MBRandom.RandomInt(100);
			bool ok = roll < chance;
			bool great = roll < chance / 3;
			List<string> words = Compose(theme, ok);
			string full = string.Join(" ", words);
			_spoken = full;
			_great = great;
			_theme = theme;
			foreach (string w in words)
			{
				_lines.Enqueue(new Line { Text = w, Speaker = CharacterObject.PlayerCharacter });
			}
			_state = 3;
			_nextLine = DateTime.UtcNow;
			Log.Write("speech: " + theme + " (" + chance + "%, rolled " + roll + ") - " + (great ? "great" : (ok ? "well received" : "botched")) + ": " + full);
			_afterLines = () =>
			{
				float delta = great ? Cfg.SpeechGreatMorale : (ok ? Cfg.SpeechMorale : -Cfg.SpeechBotchMorale);
				Rouse(Mission.PlayerTeam, delta, ok);
				InformationManager.DisplayMessage(new InformationMessage(great ? "The ranks roar your name." : (ok ? "The men cheer." : "A few cheer. Most look at their boots."), ok ? Colors.Green : Colors.Red));
				Answer();
			};
		}

		// The other side's commander answers.
		private void Answer()
		{
			if (!Cfg.SpeechEnemyAnswers || _foe == null)
			{
				_state = 9;
				return;
			}
			int lead = _foe.GetSkillValue(DefaultSkills.Leadership);
			bool ok = MBRandom.RandomInt(100) < Math.Max(15, Math.Min(90, 35 + lead / 5));
			List<string> words = Enemy(ok);
			foreach (string w in words)
			{
				_lines.Enqueue(new Line { Text = w, Speaker = _foe.CharacterObject });
			}
			_state = 3;
			_nextLine = DateTime.UtcNow.AddSeconds(1.5);
			Log.Write("speech: " + _foe.Name + " answers - " + (ok ? "well received" : "flat") + ": " + string.Join(" ", words));
			_afterLines = () =>
			{
				Rouse(Mission.PlayerEnemyTeam, ok ? Cfg.SpeechMorale : 0f, ok);
				_state = 9;
			};
		}

		private void Rouse(Team team, float delta, bool cheer)
		{
			if (team == null)
			{
				return;
			}
			ActionIndexCache[] acts = new ActionIndexCache[8]
			{
				ActionIndexCache.act_cheer_1, ActionIndexCache.act_cheer_2, ActionIndexCache.act_cheer_3, ActionIndexCache.act_cheer_4,
				ActionIndexCache.act_cheering_high_01, ActionIndexCache.act_cheering_high_02, ActionIndexCache.act_cheering_high_03, ActionIndexCache.act_cheering_high_04
			};
			int n = 0;
			foreach (Agent a in team.ActiveAgents.ToList())
			{
				if (a == null || !a.IsHuman || !a.IsActive())
				{
					continue;
				}
				if (Math.Abs(delta) > 0.01f)
				{
					a.ChangeMorale(delta);
				}
				if (!cheer || a == Agent.Main || a.HasMount)
				{
					continue;
				}
				if (n < Cfg.SpeechCheerers)
				{
					a.SetActionChannel(1, in acts[MBRandom.RandomInt(acts.Length)], false, (AnimFlags)0uL);
					if (n % 3 == 0)
					{
						a.MakeVoice(SkinVoiceManager.VoiceType.Victory, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
					}
					n++;
				}
			}
		}

		// ------------------------------------------------------------------
		// the words

		private string Fill(string s)
		{
			foreach (KeyValuePair<string, string> kv in _tokens)
			{
				s = s.Replace("{" + kv.Key + "}", kv.Value);
			}
			return s;
		}

		private static string Pick(string[] pool)
		{
			return pool[MBRandom.RandomInt(pool.Length)];
		}

		private List<string> Compose(string theme, bool ok)
		{
			string[][] parts = Speeches.Of(theme);
			List<string> words = new List<string>();
			// Never quite the same speech twice.
			for (int tries = 0; tries < 12; tries++)
			{
				words.Clear();
				words.Add(Fill(Pick(parts[0])));
				words.Add(Fill(Pick(parts[1])));
				words.Add(Fill(Pick(Speeches.Colour)));
				words.Add(Fill(Pick(parts[2])));
				string sig = string.Join("", words).GetHashCode().ToString();
				string used = Store.Get("sp:used") ?? "";
				if (!used.Contains(sig))
				{
					List<string> list = used.Split(',').Where((string x) => x.Length > 0).ToList();
					list.Add(sig);
					Store.Set("sp:used", string.Join(",", list.Skip(Math.Max(0, list.Count - 200))));
					break;
				}
			}
			if (!ok)
			{
				words[MBRandom.RandomInt(1, words.Count)] = Fill(Pick(Speeches.Botch));
			}
			return words;
		}

		private List<string> Enemy(bool ok)
		{
			List<string> words = new List<string>();
			words.Add(Fill(Pick(Speeches.EnemyOpen)));
			words.Add(Fill(Pick(ok ? Speeches.EnemyClose : Speeches.EnemyFlat)));
			return words;
		}
	}
}
