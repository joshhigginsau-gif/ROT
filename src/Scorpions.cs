using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Dragons against hosts.
	//
	// A rider can be sent with a handful of picked men to burn an enemy host
	// where it marches. A host is not helpless: it drags scorpions with it,
	// and a big, seasoned host - a Dornish one above all - may put a bolt
	// through the dragon's eye. The same bolts fly in any battle a rider
	// fights against a host.
	internal static class Scorpions
	{
		private const string StrikePrefix = "hd:";     // rider|dragon|target|from|landHour|escort|mine
		private const string RecentPrefix = "hsx:";    // last day a host was struck
		private const string AiRollKey = "sx:airoll";

		private sealed class Fall
		{
			internal string Dragon;
			internal string Rider;
			internal string Host;
			internal bool Won;
		}

		private static readonly List<Fall> _falls = new List<Fall>();

		// ------------------------------------------------------------------
		// who can fly

		internal static List<KeyValuePair<Hero, DragonRec>> MyRiders()
		{
			List<KeyValuePair<Hero, DragonRec>> list = new List<KeyValuePair<Hero, DragonRec>>();
			foreach (DragonRec d in Dragons.All().Where((DragonRec x) => x.Alive && !string.IsNullOrEmpty(x.Rider)))
			{
				Hero h = Law.Find(d.Rider);
				if (h != null && h.IsAlive && !h.IsPrisoner && !h.IsChild && h.Clan == Clan.PlayerClan && h != Hero.MainHero && !Flying(h))
				{
					list.Add(new KeyValuePair<Hero, DragonRec>(h, d));
				}
			}
			return list;
		}

		private static bool Flying(Hero h)
		{
			string id = ((MBObjectBase)h).StringId;
			return Store.Keys(StrikePrefix).Any((string k) => (Store.Get(k) ?? "").StartsWith(id + "|"));
		}

		// ------------------------------------------------------------------
		// the odds

		internal static bool Dornish(MobileParty host)
		{
			List<string> names = new List<string>();
			try
			{
				if (host.LeaderHero != null && host.LeaderHero.Culture != null)
				{
					names.Add(((MBObjectBase)host.LeaderHero.Culture).StringId + " " + host.LeaderHero.Culture.Name);
				}
				Clan c = host.ActualClan;
				if (c != null && c.Culture != null)
				{
					names.Add(((MBObjectBase)c.Culture).StringId + " " + c.Culture.Name);
				}
				if (c != null && c.Kingdom != null)
				{
					names.Add(c.Kingdom.Name.ToString());
					if (c.Kingdom.Culture != null)
					{
						names.Add(((MBObjectBase)c.Kingdom.Culture).StringId + " " + c.Kingdom.Culture.Name);
					}
				}
			}
			catch
			{
			}
			return names.Any((string n) => n != null && n.ToLowerInvariant().Contains("dorn"));
		}

		private static int SizeOdds(DragonRec d)
		{
			switch (Dragons.SizeOf(d))
			{
			case "a hatchling":
				return 15;
			case "young":
				return 5;
			case "great":
				return -10;
			case "ancient":
				return -15;
			default:
				return 0;
			}
		}

		internal static float Chance(MobileParty host, DragonRec d, Hero rider)
		{
			float c = Cfg.ScorpionBase;
			int men = Generals.Men(host);
			c += Math.Min(20f, (float)men / 1000f);
			Host.Rec r = Host.All().FirstOrDefault((Host.Rec x) => x.Party == ((MBObjectBase)host).StringId);
			if (r != null && r.Quality == Host.Veteran)
			{
				c += 10f;
			}
			if (Dornish(host))
			{
				c += Cfg.ScorpionDorneBonus;
			}
			if (d != null)
			{
				c += SizeOdds(d);
			}
			c -= (float)Generals.Skill(rider, DefaultSkills.Riding) / 30f;
			int last = Store.GetI(RecentPrefix + ((MBObjectBase)host).StringId, -999);
			if (CourtBehavior.Today() - last <= 30)
			{
				c += 10f;
			}
			return Math.Max(1f, Math.Min((float)Cfg.ScorpionMax, c));
		}

		// ------------------------------------------------------------------
		// sending a dragon

		// escortFrom: the party the picked men are taken from (may be null).
		internal static void Launch(Hero rider, DragonRec d, MobileParty target, MobileParty escortFrom, bool mine)
		{
			try
			{
				List<string> escort = new List<string>();
				int want = 50 + MBRandom.RandomInt(151);
				if (escortFrom != null && escortFrom.IsActive)
				{
					foreach (TroopRosterElement e in escortFrom.MemberRoster.GetTroopRoster().Where((TroopRosterElement t) => t.Character != null && !t.Character.IsHero && t.Number > t.WoundedNumber)
						.OrderByDescending((TroopRosterElement t) => t.Character.Tier).ToList())
					{
						if (want <= 0)
						{
							break;
						}
						int n = Math.Min(want, e.Number - e.WoundedNumber);
						escortFrom.MemberRoster.AddToCounts(e.Character, -n, false, 0, 0, true, -1);
						escort.Add(((MBObjectBase)e.Character).StringId + ":" + n);
						want -= n;
					}
				}
				Vec2 from = (rider.PartyBelongedTo != null) ? rider.PartyBelongedTo.GetPosition2D : ((escortFrom != null) ? escortFrom.GetPosition2D : target.GetPosition2D);
				float dist = from.Distance(target.GetPosition2D);
				int hours = Math.Max(2, Math.Min(48, (int)(dist / 12f)));
				long land = (long)CampaignTime.Now.ToHours + hours;
				string id = StrikePrefix + ((MBObjectBase)rider).StringId;
				Store.Set(id, string.Join("|", new string[7]
				{
					((MBObjectBase)rider).StringId,
					d.Id,
					((MBObjectBase)target).StringId,
					(escortFrom != null) ? ((MBObjectBase)escortFrom).StringId : "",
					land.ToString(),
					string.Join(",", escort),
					mine ? "1" : "0"
				}));
				Log.Write("dragon strike: " + d.Name + " under " + rider.Name + " flies for " + target.Name + " (" + hours + "h, escort " + escort.Count + " troop type(s))");
				if (mine)
				{
					Ravens.Popup("Dragonfire", rider.Name + " mounts " + d.Name + " and rises from the camp, with picked men riding hard beneath. They will reach " + ((target.LeaderHero != null) ? (target.LeaderHero.Name + "'s host") : "the enemy host") +
						" in about " + hours + " hours.\n\nA host that size carries scorpions.");
				}
				else if (target.LeaderHero != null && target.LeaderHero.Clan == Clan.PlayerClan)
				{
					Ravens.Popup("A Dragon in the Sky", "Outriders have seen " + d.Name + " in the sky, " + rider.Name + " on its back, flying for " + target.LeaderHero.Name + "'s host. Let the scorpion crews be awake.");
				}
			}
			catch (Exception e)
			{
				Log.Write("the dragon could not be sent: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// hourly: strikes that have arrived, and dragons that fell in battle

		internal static void Hourly()
		{
			if (!Cfg.Generals)
			{
				return;
			}
			long now = (long)CampaignTime.Now.ToHours;
			foreach (string key in Store.Keys(StrikePrefix).ToList())
			{
				string[] s = (Store.Get(key) ?? "").Split('|');
				long land;
				if (s.Length < 7 || !long.TryParse(s[4], out land))
				{
					Store.Set(key, null);
					continue;
				}
				if (now < land)
				{
					continue;
				}
				Store.Set(key, null);
				try
				{
					Resolve(s);
				}
				catch (Exception e)
				{
					Log.Write("a dragon strike could not be settled: " + e.Message);
				}
			}
			if (_falls.Count > 0)
			{
				List<Fall> falls = _falls.ToList();
				_falls.Clear();
				foreach (Fall f in falls)
				{
					try
					{
						Bring(f);
					}
					catch (Exception e)
					{
						Log.Write("a fallen dragon could not be settled: " + e.Message);
					}
				}
			}
		}

		private static void Resolve(string[] s)
		{
			Hero rider = Law.Find(s[0]);
			DragonRec d = Dragons.All().FirstOrDefault((DragonRec x) => x.Id == s[1]);
			MobileParty target = MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == s[2]);
			MobileParty from = MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == s[3]);
			bool mine = s[6] == "1";
			List<KeyValuePair<CharacterObject, int>> escort = new List<KeyValuePair<CharacterObject, int>>();
			foreach (string part in s[5].Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries))
			{
				string[] kv = part.Split(':');
				int n;
				CharacterObject c = (kv.Length == 2) ? MBObjectManager.Instance.GetObject<CharacterObject>(kv[0]) : null;
				if (c != null && int.TryParse(kv[1], out n))
				{
					escort.Add(new KeyValuePair<CharacterObject, int>(c, n));
				}
			}
			int escortMen = escort.Sum((KeyValuePair<CharacterObject, int> x) => x.Value);
			Action home = delegate
			{
				MobileParty back = (from != null && from.IsActive) ? from : ((rider != null && rider.PartyBelongedTo != null) ? rider.PartyBelongedTo : null);
				if (back == null)
				{
					return;
				}
				foreach (KeyValuePair<CharacterObject, int> e in escort)
				{
					back.MemberRoster.AddToCounts(e.Key, e.Value, false, 0, 0, true, -1);
				}
			};
			if (rider == null || !rider.IsAlive || rider.IsPrisoner || d == null || !d.Alive || d.Rider != ((MBObjectBase)rider).StringId)
			{
				home();
				Log.Write("dragon strike called off: the rider or the dragon is gone");
				return;
			}
			if (target == null || !target.IsActive || target.MemberRoster.TotalManCount <= 0)
			{
				home();
				if (mine)
				{
					Flow.Notify(d.Name + " found no host where it was sent, and has flown back.");
				}
				return;
			}
			string hostName = ((target.LeaderHero != null) ? (target.LeaderHero.Name + "'s host") : target.Name.ToString());
			bool targetMine = target.LeaderHero != null && target.LeaderHero.Clan == Clan.PlayerClan;
			float chance = Chance(target, d, rider);
			Store.SetI(RecentPrefix + ((MBObjectBase)target).StringId, CourtBehavior.Today());
			bool hit = MBRandom.RandomFloat * 100f < chance;
			if (hit)
			{
				Log.Write("dragon strike: " + d.Name + " vs " + target.Name + " - shot down (chance " + (int)chance + "%" + (Dornish(target) ? ", Dornish" : "") + ")");
				Dragons.Kill(d, "brought down by the scorpions of " + hostName);
				int took = Generals.Casualties(target, Math.Min(0.05f, (float)escortMen * 0.5f / Math.Max(1f, (float)Generals.Men(target))));
				string fate;
				if (rider != Hero.MainHero && MBRandom.RandomFloat * 100f < (float)Cfg.DragonRiderFall)
				{
					fate = rider.Name + " fell with it.";
					KillCharacterAction.ApplyByBattle(rider, target.LeaderHero, true);
				}
				else
				{
					fate = rider.Name + " survived the fall and was dragged from the wreck in chains.";
					try
					{
						TakePrisonerAction.Apply(target.Party, rider);
					}
					catch
					{
						fate = rider.Name + " survived the fall and got away on foot.";
					}
				}
				string text = "A scorpion bolt took " + d.Name + " as it dived on " + hostName + ". " + fate + " The men who rode beneath fought to the last and took " + took.ToString("N0") + " with them.";
				Store.AddDeed(Standing.Date() + "  " + d.Name + " was brought down by the scorpions of " + hostName + ".");
				if (mine)
				{
					Ravens.Popup("The Dragon Falls", text);
				}
				else if (targetMine)
				{
					Ravens.Popup("A Dragon Brought Down", text + "\n\nYour scorpion crews will be drinking on that for a year.");
					Standing.Change(0, 3, "Your host brought down " + d.Name);
				}
				return;
			}
			// The fire.
			float burn = (float)Cfg.DragonBurnMin + MBRandom.RandomFloat * (float)(Cfg.DragonBurnMax - Cfg.DragonBurnMin) - (float)SizeOdds(d) / 2f + (float)(d.Temper - 50) / 10f;
			Host.Rec r = Host.All().FirstOrDefault((Host.Rec x) => x.Party == ((MBObjectBase)target).StringId);
			if (r != null && r.Quality == Host.Levy)
			{
				burn += 10f;
			}
			burn = Math.Max(5f, Math.Min(90f, burn));
			int dead = Generals.Casualties(target, burn / 100f);
			home();
			Log.Write("dragon strike: " + d.Name + " vs " + target.Name + " - burned " + dead + " (" + (int)burn + "%, scorpion chance " + (int)chance + "%)");
			string where = (target.LastVisitedSettlement != null) ? (" near " + target.LastVisitedSettlement.Name) : "";
			Store.AddDeed(Standing.Date() + "  " + d.Name + " burned " + hostName + where + ": " + dead.ToString("N0") + " dead.");
			bool broke = false;
			if (r != null && target.IsActive && target.MemberRoster.TotalManCount - r.Base < r.Raised * 40 / 100)
			{
				broke = true;
				Log.Write("dragon strike: " + target.Name + " breaks");
				Host.StandDown(r, "Dragonfire broke them: the survivors would not stand again.");
			}
			string report = d.Name + " came down on " + hostName + where + ". " + dead.ToString("N0") + " men burned" + (broke ? ", and the rest broke and ran. The host is no more." : ". The rest stand, shaken.");
			if (mine)
			{
				Standing.Change(0, 5, d.Name + " burned " + hostName);
				Ravens.Popup("Dragonfire", report + " The scorpions missed.");
			}
			else if (targetMine)
			{
				Ravens.Popup("Dragonfire", report + " The scorpions missed.");
			}
		}

		// ------------------------------------------------------------------
		// other rulers' riders

		internal static void AiWeekly(int today)
		{
			if (!Cfg.Generals || !Cfg.AiDragonStrikes || today - Store.GetI(AiRollKey, -999) < 7)
			{
				return;
			}
			Store.SetI(AiRollKey, today);
			List<MobileParty> mine = Host.Mine().Select(Host.PartyOf).Where((MobileParty x) => x != null && x.IsActive).ToList();
			if (mine.Count == 0)
			{
				return;
			}
			foreach (DragonRec d in Dragons.All().Where((DragonRec x) => x.Alive && !string.IsNullOrEmpty(x.Rider)).ToList())
			{
				Hero h = Law.Find(d.Rider);
				if (h == null || !h.IsAlive || h.IsPrisoner || h.IsChild || h.Clan == null || h.Clan == Clan.PlayerClan || h.MapFaction == null || Flying(h))
				{
					continue;
				}
				if (!FactionManager.IsAtWarAgainstFaction(h.MapFaction, Clan.PlayerClan.MapFaction))
				{
					continue;
				}
				Vec2 at = (h.PartyBelongedTo != null) ? h.PartyBelongedTo.GetPosition2D : ((h.CurrentSettlement != null) ? h.CurrentSettlement.GetPosition2D : Vec2.Invalid);
				if (!at.IsValid)
				{
					continue;
				}
				MobileParty prey = mine.Where((MobileParty x) => x.MapEvent == null && x.GetPosition2D.Distance(at) < 300f).OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).FirstOrDefault();
				if (prey == null || MBRandom.RandomFloat >= 0.25f)
				{
					continue;
				}
				MobileParty own = (h.PartyBelongedTo != null && h.PartyBelongedTo.LeaderHero == h) ? h.PartyBelongedTo : null;
				Launch(h, d, prey, own, false);
				return;
			}
		}

		// ------------------------------------------------------------------
		// riders who fought a host in an ordinary battle

		internal static void OnMapEventEnded(MapEvent me)
		{
			try
			{
				if (!Cfg.Generals || me == null)
				{
					return;
				}
				foreach (BattleSideEnum side in new BattleSideEnum[2] { BattleSideEnum.Attacker, BattleSideEnum.Defender })
				{
					MapEventSide ours = me.GetMapEventSide(side);
					MapEventSide theirs = me.GetMapEventSide((side == BattleSideEnum.Attacker) ? BattleSideEnum.Defender : BattleSideEnum.Attacker);
					if (ours == null || theirs == null)
					{
						continue;
					}
					MobileParty host = theirs.Parties.Select((MapEventParty x) => (x.Party != null) ? x.Party.MobileParty : null).FirstOrDefault((MobileParty x) => x != null && Host.Is(x));
					if (host == null)
					{
						continue;
					}
					bool won = me.WinningSide == side;
					foreach (MapEventParty mp in ours.Parties)
					{
						MobileParty q = (mp.Party != null) ? mp.Party.MobileParty : null;
						if (q == null)
						{
							continue;
						}
						foreach (TroopRosterElement e in q.MemberRoster.GetTroopRoster().Where((TroopRosterElement t) => t.Character != null && t.Character.IsHero && t.Character.HeroObject != null))
						{
							Hero h = e.Character.HeroObject;
							if (!Dragons.Rides(h))
							{
								continue;
							}
							string id = ((MBObjectBase)h).StringId;
							DragonRec d = Dragons.All().FirstOrDefault((DragonRec x) => x.Alive && x.Rider == id);
							if (d != null)
							{
								_falls.Add(new Fall
								{
									Dragon = d.Id,
									Rider = id,
									Host = ((MBObjectBase)host).StringId,
									Won = won
								});
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("scorpionbattle", "the scorpions' reckoning failed: " + e.Message);
			}
		}

		private static void Bring(Fall f)
		{
			DragonRec d = Dragons.All().FirstOrDefault((DragonRec x) => x.Id == f.Dragon);
			Hero rider = Law.Find(f.Rider);
			MobileParty host = MobileParty.All.FirstOrDefault((MobileParty x) => ((MBObjectBase)x).StringId == f.Host);
			if (d == null || !d.Alive || host == null)
			{
				return;
			}
			float chance = Chance(host, d, rider) * (f.Won ? 0.5f : 1f);
			bool hit = MBRandom.RandomFloat * 100f < chance;
			Log.Write("scorpions: " + d.Name + " in battle with " + host.Name + " - " + (hit ? "brought down" : "survived") + " (chance " + (int)chance + "%)");
			if (!hit)
			{
				return;
			}
			string hostName = (host.LeaderHero != null) ? (host.LeaderHero.Name + "'s host") : host.Name.ToString();
			Dragons.Kill(d, "brought down by the scorpions of " + hostName);
			if (rider != null && rider.IsAlive && rider != Hero.MainHero && MBRandom.RandomFloat * 100f < (float)Cfg.DragonRiderFall / 2f)
			{
				KillCharacterAction.ApplyByBattle(rider, host.LeaderHero, true);
			}
			if (rider != null && (rider.Clan == Clan.PlayerClan || (host.LeaderHero != null && host.LeaderHero.Clan == Clan.PlayerClan)))
			{
				Ravens.Popup("Scorpions", "In the battle with " + hostName + ", a scorpion bolt found " + d.Name + ". The dragon is dead" + ((rider != null && !rider.IsAlive) ? (", and " + rider.Name + " with it.") : "."));
			}
		}

		// Cheat: your first rider strikes the nearest enemy host now.
		internal static string ForceStrike()
		{
			KeyValuePair<Hero, DragonRec> rider = MyRiders().FirstOrDefault();
			if (rider.Key == null)
			{
				return "No rider of your house (other than you) has a dragon.";
			}
			Vec2 at = (rider.Key.PartyBelongedTo != null) ? rider.Key.PartyBelongedTo.GetPosition2D : MobileParty.MainParty.GetPosition2D;
			MobileParty target = Host.All().Where((Host.Rec r) => !r.Mine).Select(Host.PartyOf).Where((MobileParty x) => x != null && x.IsActive && x.MapFaction != null && FactionManager.IsAtWarAgainstFaction(x.MapFaction, Clan.PlayerClan.MapFaction))
				.OrderBy((MobileParty x) => x.GetPosition2D.Distance(at)).FirstOrDefault();
			if (target == null)
			{
				return "No enemy host is in the field.";
			}
			MobileParty from = Host.Mine().Select(Host.PartyOf).FirstOrDefault((MobileParty x) => x != null && x.IsActive);
			Launch(rider.Key, rider.Value, target, from, true);
			// Land it now.
			string[] s = (Store.Get(StrikePrefix + ((MBObjectBase)rider.Key).StringId) ?? "").Split('|');
			if (s.Length >= 7)
			{
				s[4] = "0";
				Store.Set(StrikePrefix + ((MBObjectBase)rider.Key).StringId, string.Join("|", s));
			}
			Hourly();
			return rider.Value.Name + " struck " + target.Name + ". See the log.";
		}
	}
}
