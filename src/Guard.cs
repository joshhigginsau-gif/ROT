using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// One sworn brother of the white cloak.
	internal sealed class Knight
	{
		internal string Id = "";
		internal int Sworn;
		// companion, champion, commoner, baseborn, ward:<clan>, noble:<clan>
		internal string Origin = "";
		// 1 = Lord Commander.
		internal int Rank;
		internal int Camps;
		internal int Outlaws;
		// guard, or away on an errand.
		internal string State = "guard";
		internal int Back;
		// camps:<hideout id> or hunt:<hero id>
		internal string Errand = "";
		// the men who went with them: troop:count;troop:count
		internal string Men = "";
		// Dressed in the white armour yet.
		internal bool Cloaked;

		internal string Pack()
		{
			return string.Join("|", new string[10] { Sworn.ToString(), Origin, Rank.ToString(), Camps.ToString(), Outlaws.ToString(), State, Back.ToString(), Errand, Men, Cloaked ? "c" : "" });
		}

		internal static Knight Unpack(string id, string s)
		{
			string[] p = (s ?? "").Split('|');
			if (p.Length < 9)
			{
				return null;
			}
			int n;
			Knight k = new Knight();
			k.Id = id;
			k.Sworn = int.TryParse(p[0], out n) ? n : 0;
			k.Origin = p[1];
			k.Rank = int.TryParse(p[2], out n) ? n : 0;
			k.Camps = int.TryParse(p[3], out n) ? n : 0;
			k.Outlaws = int.TryParse(p[4], out n) ? n : 0;
			k.State = p[5];
			k.Back = int.TryParse(p[6], out n) ? n : 0;
			k.Errand = p[7];
			k.Men = p[8];
			// Tolerated missing: records sworn before the ceremony existed.
			k.Cloaked = p.Length > 9 && p[9] == "c";
			return k;
		}
	}

	// The Kingsguard.
	//
	// Seven knights sworn for life to guard the crown's person: no lands, no
	// wives, no inheritance, and no leaving. They follow you through the
	// streets and into your hall, stand beside you in the field, and ride out
	// on the crown's errands - clearing the kingsroad of outlaws, bringing
	// back the lords who fled your justice.
	//
	// They come from anywhere: a champion of the lists, a common soldier who
	// earned it, a ward raised at your court, a younger son of a great house,
	// your own baseborn child - who, in the white cloak, can never press a
	// claim against your heir.
	//
	// And the vows are real. A knight who marries or walks out of your house
	// is an oathbreaker, and the King's Justice can try them for it. When a
	// bastard rises against your heir, the white cloaks can split, as they
	// did in the Dance.
	internal static class Guard
	{
		private const string Prefix = "kg:";

		// ------------------------------------------------------------------
		// the White Book

		internal static string Title()
		{
			if (!Succession.Rules())
			{
				return "Sworn Shields";
			}
			return (Hero.MainHero != null && Hero.MainHero.IsFemale) ? "Queensguard" : "Kingsguard";
		}

		internal static List<Knight> All()
		{
			List<Knight> list = new List<Knight>();
			foreach (string key in Store.Keys(Prefix))
			{
				Knight k = Knight.Unpack(key.Substring(Prefix.Length), Store.Get(key));
				if (k != null)
				{
					list.Add(k);
				}
			}
			return list.OrderByDescending((Knight k) => k.Rank).ThenBy((Knight k) => k.Sworn).ToList();
		}

		private static void Save(Knight k)
		{
			Store.Set(Prefix + k.Id, k.Pack());
			Titles.Invalidate();
		}

		private static void Drop(Knight k)
		{
			Store.Set(Prefix + k.Id, null);
			Titles.Invalidate();
		}

		internal static Knight Of(Hero h)
		{
			return (h == null) ? null : Knight.Unpack(((MBObjectBase)h).StringId, Store.Get(Prefix + ((MBObjectBase)h).StringId));
		}

		// "guard", "away" on an errand, or "host" in command of one.
		internal static void SetState(Hero h, string state)
		{
			Knight k = Of(h);
			if (k != null)
			{
				k.State = state;
				Save(k);
			}
		}

		internal static bool IsSworn(Hero h)
		{
			return Cfg.Kingsguard && h != null && !string.IsNullOrEmpty(Store.Get(Prefix + ((MBObjectBase)h).StringId));
		}

		internal static Hero HeroOf(Knight k)
		{
			return Law.Find(k.Id);
		}

		internal static int Places()
		{
			return Math.Max(0, Cfg.KgSize - All().Count);
		}

		// What the name says after it. Suffix, whatever title_position says:
		// "Ser Criston Cole of the Kingsguard" is how the books write it.
		internal static string StyleOf(Hero h)
		{
			Knight k = Of(h);
			if (k == null || !Cfg.Kingsguard)
			{
				return null;
			}
			string order = Title();
			return (k.Rank >= 1) ? (", Lord Commander of the " + order) : (" of the " + order);
		}

		// ------------------------------------------------------------------
		// who could be sworn

		internal static bool Eligible(Hero h, out string why)
		{
			why = null;
			if (h == null || !h.IsAlive)
			{
				why = "they are dead";
				return false;
			}
			if (h == Hero.MainHero)
			{
				why = "you cannot guard yourself";
				return false;
			}
			if (IsSworn(h))
			{
				why = "already sworn";
				return false;
			}
			if (h.IsChild)
			{
				why = "too young to take the vows";
				return false;
			}
			if (h.Spouse != null)
			{
				why = "married - the vows forbid it";
				return false;
			}
			if (h.Clan != null && h.Clan.Leader == h)
			{
				why = "the head of a house cannot give up their lands";
				return false;
			}
			if (h == Succession.Named())
			{
				why = "your named heir";
				return false;
			}
			if (h.IsPrisoner)
			{
				why = "a prisoner";
				return false;
			}
			if (h.PartyBelongedTo != null && h.PartyBelongedTo.LeaderHero == h && h.PartyBelongedTo != MobileParty.MainParty)
			{
				why = "leading a party of their own";
				return false;
			}
			return true;
		}

		// Everyone who could be asked, by where they come from.
		internal static List<Hero> Candidates(string origin)
		{
			IEnumerable<Hero> pool = Enumerable.Empty<Hero>();
			try
			{
				switch (origin)
				{
				case "champion":
				{
					List<Hero> list = new List<Hero>();
					if (Clan.PlayerClan != null)
					{
						list.AddRange(Clan.PlayerClan.Heroes.Where((Hero h) => h.CompanionOf == Clan.PlayerClan));
						list.AddRange(Clan.PlayerClan.Companions);
					}
					list.AddRange(Hero.AllAliveHeroes.Where((Hero h) => h.IsLord && Tourney.Wins(h) > 0));
					pool = list.Distinct();
					break;
				}
				case "ward":
					pool = Wardship.All().Where((Held w) => !w.Hostage).Select((Held w) => Wardship.HeroOf(w)).Where((Hero h) => h != null);
					break;
				case "noble":
				{
					Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
					if (realm != null)
					{
						pool = realm.Clans.Where((Clan c) => c != null && c != Clan.PlayerClan && !c.IsEliminated)
							.SelectMany((Clan c) => c.Heroes).Where((Hero h) => h.IsLord && h.GetRelationWithPlayer() >= 0f);
					}
					break;
				}
				case "baseborn":
					pool = Baseborn.Known().Select((Kid k) => Baseborn.HeroOf(k)).Where((Hero h) => h != null);
					break;
				}
			}
			catch (Exception e)
			{
				Log.Once("kgcand", "listing candidates failed: " + e.Message);
			}
			string why;
			return pool.Where((Hero h) => Eligible(h, out why)).Take(40).ToList();
		}

		// Soldiers of your party good enough to be knighted.
		internal static List<CharacterObject> Soldiers()
		{
			List<CharacterObject> list = new List<CharacterObject>();
			try
			{
				foreach (TroopRosterElement e in MobileParty.MainParty.MemberRoster.GetTroopRoster())
				{
					if (e.Character != null && !e.Character.IsHero && e.Character.Tier >= Cfg.KgCommonerTier && e.Number - e.WoundedNumber > 0)
					{
						list.Add(e.Character);
					}
				}
			}
			catch
			{
			}
			return list.OrderByDescending((CharacterObject c) => c.Tier).ToList();
		}

		// ------------------------------------------------------------------
		// the vows

		// origin: companion, champion, commoner, baseborn, ward, noble.
		internal static bool Swear(Hero h, string origin, bool force, out string why)
		{
			why = null;
			try
			{
				if (!Cfg.Kingsguard)
				{
					why = "the order is off in the config";
					return false;
				}
				if (!force && Places() <= 0)
				{
					why = "every place in the " + Title() + " is filled";
					return false;
				}
				if (!force && !Eligible(h, out why))
				{
					return false;
				}
				Clan birth = h.Clan;
				bool companion = h.CompanionOf == Clan.PlayerClan;
				// A lord of another house is asked, not told.
				if (!force && birth != null && birth != Clan.PlayerClan && (origin == "noble" || origin == "champion"))
				{
					int yes = 40 + (int)h.GetRelationWithPlayer() / 2 + (Store.Honour - 50) / 2;
					if (MBRandom.RandomInt(100) >= Math.Max(5, Math.Min(95, yes)))
					{
						why = h.Name + " thanks you, and declines. " + (h.IsFemale ? "She" : "He") + " has other plans for " + (h.IsFemale ? "her" : "his") + " life";
						return false;
					}
				}
				string tag = origin;
				if ((origin == "noble" || origin == "ward" || origin == "champion") && birth != null && birth != Clan.PlayerClan)
				{
					tag = ((origin == "champion") ? "noble" : origin) + ":" + ((MBObjectBase)birth).StringId;
				}
				if (companion && origin == "champion")
				{
					tag = "companion";
				}

				// Into your household. A companion already is; anyone else
				// leaves their house - that is what the vows are.
				if (!companion && h.Clan != Clan.PlayerClan)
				{
					h.Clan = Clan.PlayerClan;
				}
				if (h.PartyBelongedTo != MobileParty.MainParty && MobileParty.MainParty != null)
				{
					try
					{
						AddHeroToPartyAction.Apply(h, MobileParty.MainParty, false);
					}
					catch (Exception pe)
					{
						Log.Once("kgparty", "a knight could not join your party: " + pe.Message);
					}
				}

				// What their house makes of it.
				if (birth != null && birth != Clan.PlayerClan && birth.Leader != null && birth.Leader.IsAlive)
				{
					ChangeRelationAction.ApplyPlayerRelation(birth.Leader, 10, false, false);
				}
				if (tag.StartsWith("ward:"))
				{
					Held w = Wardship.All().FirstOrDefault((Held x) => x.Hero == ((MBObjectBase)h).StringId);
					if (w != null)
					{
						Wardship.Drop(w);
					}
				}

				Knight k = new Knight();
				k.Id = ((MBObjectBase)h).StringId;
				k.Sworn = CourtBehavior.Today();
				k.Origin = tag;
				k.Rank = All().Any((Knight x) => x.Rank >= 1) ? 0 : 1;
				Save(k);
				Page(h, k, null);
				Store.AddDeed(Standing.Date() + "  " + h.Name + " took the white cloak.");
				Ceremony(h, k, tag);
				Log.Write("sworn to the " + Title() + ": " + h.Name + " (" + tag + ")" + ((k.Rank >= 1) ? ", Lord Commander" : ""));
				return true;
			}
			catch (Exception e)
			{
				why = "the vows could not be taken: " + e.Message;
				Log.Write("swearing a knight failed: " + e);
				return false;
			}
		}

		// A common soldier, raised up. They become a hero of your house.
		internal static Hero KnightSoldier(CharacterObject troop)
		{
			try
			{
				if (troop == null || MobileParty.MainParty == null)
				{
					return null;
				}
				CharacterObject template = Template(troop.Culture, troop.IsFemale);
				if (template == null)
				{
					Log.Write("no lord template to knight a soldier from");
					return null;
				}
				Settlement born = MobileParty.MainParty.CurrentSettlement ?? ((Clan.PlayerClan != null) ? Clan.PlayerClan.HomeSettlement : null);
				Hero h = HeroCreator.CreateSpecialHero(template, born, null, null, MBRandom.RandomInt(22, 36));
				if (h == null)
				{
					return null;
				}
				h.ChangeState(Hero.CharacterStates.Active);
				h.SetNewOccupation(Occupation.Lord);
				Baseborn.Visible(h);
				string given = (h.FirstName != null) ? h.FirstName.ToString() : "Arlan";
				h.SetName(new TextObject("{=!}Ser " + given, (Dictionary<string, object>)null), new TextObject("{=!}" + given, (Dictionary<string, object>)null));
				try
				{
					h.BattleEquipment.FillFrom(troop.FirstBattleEquipment, false);
				}
				catch (Exception ee)
				{
					Log.Once("kgequip", "the soldier's kit would not transfer: " + ee.Message);
				}
				foreach (SkillObject s in new SkillObject[7] { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm, DefaultSkills.Bow, DefaultSkills.Crossbow, DefaultSkills.Riding, DefaultSkills.Athletics })
				{
					int want = troop.GetSkillValue(s) + 10;
					if (h.GetSkillValue(s) < want)
					{
						h.SetSkillValue(s, want);
					}
				}
				h.Clan = Clan.PlayerClan;
				MobileParty.MainParty.MemberRoster.AddToCounts(troop, -1);
				h.EncyclopediaText = new TextObject("{=!}" + given + " was born common and served as " + troop.Name +
					" in the host of " + Hero.MainHero.Name + ", until the day a crown decided that was not all " + (h.IsFemale ? "she" : "he") + " would ever be.", (Dictionary<string, object>)null);
				Log.Write("a soldier was knighted: " + h.Name + " (was " + troop.Name + ")");
				return h;
			}
			catch (Exception e)
			{
				Log.Write("knighting the soldier failed: " + e);
				return null;
			}
		}

		private static CharacterObject Template(CultureObject culture, bool female)
		{
			List<CharacterObject> all = CharacterObject.All.Where((CharacterObject c) => c != null && c.IsHero && c.Occupation == Occupation.Lord
				&& c.Culture == culture && c.IsFemale == female && !c.HiddenInEncyclopedia).ToList();
			if (all.Count == 0)
			{
				all = CharacterObject.All.Where((CharacterObject c) => c != null && c.IsHero && c.Occupation == Occupation.Lord && c.IsFemale == female).ToList();
			}
			return (all.Count == 0) ? null : all[MBRandom.RandomInt(all.Count)];
		}

		internal static void Commander(Knight k)
		{
			foreach (Knight x in All())
			{
				if (x.Rank != 0)
				{
					x.Rank = 0;
					Save(x);
				}
			}
			k.Rank = 1;
			Save(k);
			Hero h = HeroOf(k);
			Store.AddDeed(Standing.Date() + "  " + ((h != null) ? h.Name.ToString() : "A knight") + " was named Lord Commander of the " + Title() + ".");
		}

		// Released from the vows. The white cloak is for life, and everybody
		// knows it.
		internal static void Dismiss(Knight k)
		{
			try
			{
				Hero h = HeroOf(k);
				Drop(k);
				Standing.Change(-Cfg.KgDismissHonour, 0, "Took back a white cloak");
				if (h == null)
				{
					return;
				}
				string home = k.Origin.Contains(":") ? k.Origin.Substring(k.Origin.IndexOf(':') + 1) : null;
				Clan back = string.IsNullOrEmpty(home) ? null : Clan.All.FirstOrDefault((Clan c) => ((MBObjectBase)c).StringId == home && !c.IsEliminated);
				if (back != null)
				{
					if (h.PartyBelongedTo == MobileParty.MainParty)
					{
						MobileParty.MainParty.MemberRoster.AddToCounts(h.CharacterObject, -1);
					}
					h.Clan = back;
				}
				else if (h.CompanionOf == Clan.PlayerClan)
				{
					RemoveCompanionAction.ApplyByFire(Clan.PlayerClan, h);
				}
				Store.AddDeed(Standing.Date() + "  " + h.Name + " was released from the vows" + ((back != null) ? (" and went home to " + back.Name) : "") + ".");
			}
			catch (Exception e)
			{
				Log.Write("releasing the knight failed: " + e.Message);
			}
		}

		// Checked daily: the dead, the returning, and the forsworn.
		internal static void Daily()
		{
			try
			{
				if (!Cfg.Kingsguard || !Store.Initialized)
				{
					return;
				}
				int today = CourtBehavior.Today();
				foreach (Knight k in All())
				{
					Hero h = HeroOf(k);
					if (h == null)
					{
						Hero dead = Hero.DeadOrDisabledHeroes.FirstOrDefault((Hero x) => ((MBObjectBase)x).StringId == k.Id);
						Drop(k);
						Store.AddDeed(Standing.Date() + "  " + ((dead != null) ? dead.Name.ToString() : "A sworn brother") + " died in the white cloak.");
						continue;
					}
					if (k.State == "away")
					{
						if (today >= k.Back)
						{
							Return(k);
						}
						continue;
					}
					if (h.Spouse != null)
					{
						Forsworn(k, h, "took a " + (h.Spouse.IsFemale ? "wife" : "husband"));
					}
					else if (h.Clan != Clan.PlayerClan)
					{
						Forsworn(k, h, "left your service for " + ((h.Clan != null) ? h.Clan.Name.ToString() : "nobody"));
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("kgdaily", "the white book's daily tick failed: " + e.Message);
			}
		}

		private static void Forsworn(Knight k, Hero h, string what)
		{
			Drop(k);
			Law.Record(Law.Oathbreaking, h, Hero.MainHero, null, false);
			Store.AddDeed(Standing.Date() + "  " + h.Name + " broke the vows of the " + Title() + " and " + what + ".");
			Popup("Forsworn", h.Name + " " + what + ".\n\nThe vows were for life. The King's Justice can hear the charge of oathbreaking.");
		}

		// The Dance splits the white cloaks. Called when a bastard rises: a
		// knight who likes the claimant better may go over.
		internal static List<string> Split(Clan house, Hero claimant)
		{
			List<string> gone = new List<string>();
			try
			{
				if (!Cfg.Kingsguard || house == null || claimant == null)
				{
					return gone;
				}
				Hero you = Hero.MainHero;
				foreach (Knight k in All())
				{
					Hero h = HeroOf(k);
					if (h == null || k.State == "away" || h == claimant)
					{
						continue;
					}
					if (h.GetRelation(claimant) > h.GetRelation(you) && MBRandom.RandomInt(100) < Cfg.KgDefectChance)
					{
						Drop(k);
						if (h.PartyBelongedTo == MobileParty.MainParty)
						{
							MobileParty.MainParty.MemberRoster.AddToCounts(h.CharacterObject, -1);
						}
						if (h.CompanionOf != null)
						{
							h.CompanionOf = null;
						}
						h.Clan = house;
						Law.Record(Law.Oathbreaking, h, you, null, false);
						Store.AddDeed(Standing.Date() + "  " + h.Name + " tore off the white cloak and went over to " + claimant.Name + ".");
						gone.Add(h.Name.ToString());
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("the white cloaks' split failed: " + e.Message);
			}
			return gone;
		}

		// You took up the bastard's banner. The white cloaks were sworn to the
		// house you left, and they stay with it - that is not oathbreaking,
		// and must not be read as it by tomorrow's vow check.
		internal static void Abandon(Clan old)
		{
			try
			{
				List<Knight> all = All();
				foreach (Knight k in all)
				{
					Drop(k);
				}
				if (all.Count > 0)
				{
					Store.AddDeed(Standing.Date() + "  The white cloaks stayed with " + ((old != null) ? old.Name.ToString() : "the house you left") + ".");
					Log.Write("the white cloaks stay with " + ((old != null) ? old.Name.ToString() : "the old house") + ": " + all.Count);
				}
			}
			catch (Exception e)
			{
				Log.Write("leaving the white cloaks behind failed: " + e.Message);
			}
		}

		// ------------------------------------------------------------------
		// the crown's errands

		internal static List<Settlement> Camps()
		{
			try
			{
				Vec2 at = MobileParty.MainParty.GetPosition2D;
				return Settlement.All.Where((Settlement s) => s.IsHideout && s.Hideout != null && s.Hideout.IsInfested)
					.OrderBy((Settlement s) => s.GetPosition2D.Distance(at)).Take(6).ToList();
			}
			catch
			{
				return new List<Settlement>();
			}
		}

		internal static List<Hero> Outlaws()
		{
			List<Hero> list = new List<Hero>();
			try
			{
				Kingdom realm = (Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null;
				foreach (Charge c in Law.All())
				{
					Hero h = Law.Find(c.Accused);
					if (h != null && h != Hero.MainHero && !h.IsPrisoner && (realm == null || h.MapFaction != realm) && !list.Contains(h)
						&& (h.PartyBelongedTo == null || h.PartyBelongedTo.Army == null))
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

		// Knights free to ride out: sworn, with you, and whole.
		internal static List<Hero> Ready()
		{
			return All().Where((Knight k) => k.State == "guard").Select(HeroOf)
				.Where((Hero h) => h != null && h.PartyBelongedTo == MobileParty.MainParty && !h.IsWounded && !h.IsPrisoner).ToList();
		}

		// target: a hideout Settlement, or a Hero to hunt.
		internal static void Send(Hero h, object target, int men)
		{
			try
			{
				Knight k = Of(h);
				if (k == null || target == null)
				{
					return;
				}
				Settlement camp = target as Settlement;
				Hero quarry = target as Hero;
				Vec2 there = (camp != null) ? camp.GetPosition2D : ((quarry != null && quarry.GetCampaignPosition().IsValid()) ? quarry.GetCampaignPosition().ToVec2() : MobileParty.MainParty.GetPosition2D);
				float distance = MobileParty.MainParty.GetPosition2D.Distance(there);

				// The men go with them.
				TroopRoster roster = MobileParty.MainParty.MemberRoster;
				List<string> taken = new List<string>();
				int left = men;
				foreach (TroopRosterElement e in roster.GetTroopRoster().Where((TroopRosterElement x) => x.Character != null && !x.Character.IsHero)
					.OrderByDescending((TroopRosterElement x) => x.Character.Tier).ToList())
				{
					if (left <= 0)
					{
						break;
					}
					int n = Math.Min(left, e.Number - e.WoundedNumber);
					if (n <= 0)
					{
						continue;
					}
					roster.AddToCounts(e.Character, -n);
					taken.Add(((MBObjectBase)e.Character).StringId + ":" + n);
					left -= n;
				}

				// And the knight leaves, to the town nearest the work.
				Settlement town = Settlement.All.Where((Settlement s) => s.IsTown).OrderBy((Settlement s) => s.GetPosition2D.Distance(there)).FirstOrDefault();
				roster.AddToCounts(h.CharacterObject, -1);
				if (town != null)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(h, town);
				}
				k.State = "away";
				k.Back = CourtBehavior.Today() + Math.Max(3, Math.Min(20, Cfg.KgErrandDays + (int)(distance / 30f)));
				k.Errand = (camp != null) ? ("camps:" + ((MBObjectBase)camp).StringId) : ("hunt:" + ((MBObjectBase)quarry).StringId);
				k.Men = string.Join(";", taken.ToArray());
				Save(k);
				string what = (camp != null) ? ("to clear the outlaws out of " + camp.Name) : ("to bring back " + quarry.Name);
				Store.AddDeed(Standing.Date() + "  " + h.Name + " rode out " + what + ".");
				Flow.Notify(h.Name + " rides out " + what + " with " + (men - left) + " men. Back in about " + (k.Back - CourtBehavior.Today()) + " days.");
			}
			catch (Exception e)
			{
				Log.Write("sending the knight failed: " + e);
			}
		}

		// Home again, with a tale.
		internal static void Return(Knight k)
		{
			try
			{
				Hero h = HeroOf(k);
				Dictionary<CharacterObject, int> men = new Dictionary<CharacterObject, int>();
				int total = 0;
				int tiers = 0;
				foreach (string part in (k.Men ?? "").Split(';'))
				{
					int colon = part.LastIndexOf(':');
					int n;
					if (colon <= 0 || !int.TryParse(part.Substring(colon + 1), out n))
					{
						continue;
					}
					CharacterObject c = MBObjectManager.Instance.GetObject<CharacterObject>(part.Substring(0, colon));
					if (c != null)
					{
						men[c] = n;
						total += n;
						tiers += c.Tier * n;
					}
				}
				k.State = "guard";
				k.Errand = k.Errand ?? "";
				string errand = k.Errand;
				k.Errand = "";
				k.Men = "";
				Save(k);
				if (h == null)
				{
					return;
				}

				float ours = Law.Rating(h.CharacterObject) * 2f + tiers * 10f;
				bool won;
				string tale;
				float keep;
				if (errand.StartsWith("camps:"))
				{
					Settlement camp = Settlement.Find(errand.Substring(6));
					List<MobileParty> bandits = (camp != null) ? camp.Parties.Where((MobileParty p) => p.IsBandit).ToList() : new List<MobileParty>();
					int banditMen = bandits.Sum((MobileParty p) => p.MemberRoster.TotalManCount);
					if (camp == null || bandits.Count == 0)
					{
						won = true;
						keep = 1f;
						tale = h.Name + " found " + ((camp != null) ? camp.Name.ToString() : "the camp") + " already empty, and came home.";
					}
					else
					{
						float theirs = banditMen * 12f;
						won = MBRandom.RandomFloat < ours / (ours + theirs);
						if (won)
						{
							foreach (MobileParty p in bandits)
							{
								DestroyPartyAction.Apply(null, p);
							}
							int loot = Cfg.KgLoot * bandits.Count + banditMen * 20;
							Hero.MainHero.ChangeHeroGold(loot);
							GainRenownAction.Apply(Hero.MainHero, 5f, true);
							Standing.Change(1, 0, "The " + Title() + " cleared " + camp.Name);
							Settlement near = Settlement.All.Where((Settlement s) => s.IsTown).OrderBy((Settlement s) => s.GetPosition2D.Distance(camp.GetPosition2D)).FirstOrDefault();
							if (near != null && near.OwnerClan != null && near.OwnerClan.Leader != null && near.OwnerClan != Clan.PlayerClan)
							{
								ChangeRelationAction.ApplyPlayerRelation(near.OwnerClan.Leader, 5, false, false);
							}
							k.Camps++;
							keep = 0.9f;
							tale = h.Name + " put " + banditMen + " outlaws to the sword at " + camp.Name + " and brought back " + loot.ToString("N0") + " in plunder. The roads around " +
								((near != null) ? near.Name.ToString() : "it") + " are quiet.";
						}
						else
						{
							keep = 0.5f;
							tale = h.Name + " went into " + camp.Name + " and came out carried. The outlaws are still there.";
						}
					}
				}
				else
				{
					Hero quarry = Law.Find(errand.StartsWith("hunt:") ? errand.Substring(5) : "");
					if (quarry == null || quarry.IsPrisoner)
					{
						won = false;
						keep = 1f;
						tale = h.Name + " came back empty-handed: " + ((quarry == null) ? "the one they hunted is dead." : "somebody else had them first.");
					}
					else
					{
						float theirs = Law.Rating(quarry.CharacterObject) * 2f + ((quarry.PartyBelongedTo != null) ? quarry.PartyBelongedTo.MemberRoster.TotalManCount * 10f : 50f);
						won = MBRandom.RandomFloat < ours / (ours + theirs);
						if (won)
						{
							TakePrisonerAction.Apply(PartyBase.MainParty, quarry);
							k.Outlaws++;
							keep = 0.85f;
							tale = h.Name + " brought " + quarry.Name + " back in chains. The King's Justice can hear the charge.";
						}
						else
						{
							keep = 0.5f;
							tale = h.Name + " caught up with " + quarry.Name + ", and lost.";
						}
					}
				}
				Save(k);

				// The men who came back.
				int back = 0;
				foreach (KeyValuePair<CharacterObject, int> m in men)
				{
					int n = (int)Math.Round(m.Value * keep);
					if (n > 0)
					{
						MobileParty.MainParty.MemberRoster.AddToCounts(m.Key, n);
						back += n;
					}
				}
				if (total > 0)
				{
					tale += "\n\n" + back + " of the " + total + " who rode out came back.";
				}

				// And the knight.
				if (!won && errand != "" && MBRandom.RandomInt(100) < Cfg.KgErrandDeath)
				{
					tale += "\n\n" + h.Name + " died of the wounds on the road home.";
					KillCharacterAction.ApplyByBattle(h, null, true);
				}
				else
				{
					if (!won)
					{
						h.HitPoints = 1;
					}
					try
					{
						AddHeroToPartyAction.Apply(h, MobileParty.MainParty, false);
					}
					catch (Exception pe)
					{
						Log.Write("the knight could not rejoin your party: " + pe.Message);
					}
				}
				Page(h, k, null);
				Popup(won ? "The " + Title() + " Returns" : "A Knight Comes Home", tale);
			}
			catch (Exception e)
			{
				Log.Write("the knight's return failed: " + e);
			}
		}

		// ------------------------------------------------------------------
		// bodyguards

		// In the streets and the hall, your sworn knights walk with you. The
		// game already does this for one companion (ClanMemberRolesCampaign
		// Behavior); this is the same call, for every knight you have with
		// you. Runs as a mission is about to open, and builds nothing but the
		// characters who will be in it.
		internal static void Bodyguards()
		{
			try
			{
				if (!Cfg.Kingsguard || !Store.Initialized || PlayerEncounter.LocationEncounter == null || Settlement.CurrentSettlement == null)
				{
					return;
				}
				LocationEncounter enc = PlayerEncounter.LocationEncounter;
				// Not the arena: a trial is staged there, and a knight walking into
				// somebody else's duel is not what the vows mean.
				string[] rooms = new string[4] { "center", "lordshall", "tavern", "village_center" };
				foreach (Knight k in All())
				{
					Hero h = HeroOf(k);
					if (h == null || k.State != "guard" || h.PartyBelongedTo != MobileParty.MainParty || h.IsWounded || h.IsPrisoner)
					{
						continue;
					}
					if (enc.GetAccompanyingCharacter(h.CharacterObject) != null)
					{
						continue;
					}
					// The game's own CreateBodyguardHero puts a bodyguard in town
					// clothes in every town and castle. This is the same
					// character, built the same way, in the white armour.
					LocationCharacter lc = Cfg.KgArmourInTown ? Armoured(h) : LocationCharacter.CreateBodyguardHero(h, MobileParty.MainParty, SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior);
					enc.AddAccompanyingCharacter(lc, true);
					AccompanyingCharacter ac = enc.GetAccompanyingCharacter(h.CharacterObject);
					if (ac != null)
					{
						ac.DisallowEntranceToAllLocations();
						ac.AllowEntranceToLocations((Location l) => rooms.Contains(l.StringId));
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("kgbody", "the bodyguards could not come in with you: " + e.Message);
			}
		}

		private static LocationCharacter Armoured(Hero h)
		{
			UniqueTroopDescriptor no = new UniqueTroopDescriptor(FlattenedTroopRoster.GenerateUniqueNoFromParty(MobileParty.MainParty, 0));
			Monster monster = FaceGen.GetMonsterWithSuffix(h.CharacterObject.Race, "_settlement");
			AgentData data = new AgentData(new TaleWorlds.CampaignSystem.AgentOrigins.PartyAgentOrigin(PartyBase.MainParty, h.CharacterObject, -1, no)).Monster(monster).NoHorses(true);
			return new LocationCharacter(data, SandBoxManager.Instance.AgentBehaviorManager.AddFirstCompanionBehavior, null, false, LocationCharacter.CharacterRelations.Friendly, null, false);
		}

		// ------------------------------------------------------------------
		// the court

		private static string OriginName(string origin)
		{
			string o = (origin ?? "").Split(':')[0];
			switch (o)
			{
			case "companion":
				return "sworn sword";
			case "champion":
				return "champion of the lists";
			case "commoner":
				return "common-born";
			case "baseborn":
				return "baseborn";
			case "ward":
				return "a ward of your court";
			case "noble":
			{
				string id = origin.Contains(":") ? origin.Substring(origin.IndexOf(':') + 1) : "";
				Clan c = Clan.All.FirstOrDefault((Clan x) => ((MBObjectBase)x).StringId == id);
				return "of " + ((c != null) ? c.Name.ToString() : "a noble house");
			}
			default:
				return o;
			}
		}

		// The White Book, as the court menu shows it.
		internal static string Book()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			List<Knight> all = All();
			sb.Append("THE WHITE BOOK OF THE ").Append(Title().ToUpperInvariant()).Append("\n");
			if (all.Count == 0)
			{
				sb.Append("  The pages are empty.\n");
			}
			int today = CourtBehavior.Today();
			foreach (Knight k in all)
			{
				Hero h = HeroOf(k);
				if (h == null)
				{
					continue;
				}
				int years = Math.Max(0, (today - k.Sworn) / Math.Max(1, Cfg.DaysPerYear));
				sb.Append("  ").Append(h.FirstName).Append((k.Rank >= 1) ? ", Lord Commander" : "").Append(" - ").Append(OriginName(k.Origin))
				  .Append(", ").Append(years).Append((years == 1) ? " year" : " years").Append(" in the cloak");
				if (k.Camps > 0)
				{
					sb.Append(", ").Append(k.Camps).Append(" camp").Append((k.Camps == 1) ? "" : "s").Append(" cleared");
				}
				if (k.Outlaws > 0)
				{
					sb.Append(", ").Append(k.Outlaws).Append(" outlaw").Append((k.Outlaws == 1) ? "" : "s").Append(" taken");
				}
				if (k.State == "away")
				{
					sb.Append("   [away, back in ").Append(Math.Max(0, k.Back - today)).Append(" days]");
				}
				else if (k.State == "host")
				{
					sb.Append("   [commanding a host]");
				}
				else if (h.PartyBelongedTo != MobileParty.MainParty)
				{
					sb.Append("   [not with you]");
				}
				sb.Append("\n");
			}
			int open = Places();
			if (open > 0)
			{
				sb.Append("\n").Append(open).Append((open == 1) ? " place stands" : " places stand").Append(" empty.\n");
			}
			return sb.ToString();
		}

		internal static string Attention()
		{
			if (!Cfg.Kingsguard)
			{
				return null;
			}
			int away = All().Count((Knight k) => k.State == "away");
			if (away > 0)
			{
				return away + " of your " + Title() + " " + ((away == 1) ? "is" : "are") + " away on the crown's business.";
			}
			return null;
		}

		// The encyclopedia page gets a line for the cloak.
		private static void Page(Hero h, Knight k, string extra)
		{
			try
			{
				string before = (h.EncyclopediaText != null) ? h.EncyclopediaText.ToString() : "";
				string marker = "Sworn to the ";
				int at = before.IndexOf(marker, StringComparison.Ordinal);
				if (at >= 0)
				{
					before = before.Substring(0, at).TrimEnd();
				}
				string line = marker + Title() + " by " + Hero.MainHero.Name + ", " + OriginName(k.Origin) + "." +
					((k.Camps > 0) ? (" Cleared " + k.Camps + " outlaw camp" + ((k.Camps == 1) ? "" : "s") + ".") : "") +
					((k.Outlaws > 0) ? (" Brought " + k.Outlaws + " outlaw" + ((k.Outlaws == 1) ? "" : "s") + " to justice.") : "") +
					(extra ?? "");
				h.EncyclopediaText = new TextObject("{=!}" + before + ((before.Length > 0) ? "\n\n" : "") + line, (Dictionary<string, object>)null);
			}
			catch
			{
			}
		}

		// Popups one after another.
		private static readonly Queue<Action<Action>> _queue = new Queue<Action<Action>>();

		private static bool _showing;

		internal static void Reset()
		{
			_queue.Clear();
			_showing = false;
			_armour = null;
			_looked = false;
		}

		// ------------------------------------------------------------------
		// the white armour

		private static CharacterObject _armour;

		private static bool _looked;

		// Realm of Thrones ships a Kingsguard troop, white enamel and all. Its
		// kit is what a new brother is dressed in. Looked up once a session.
		internal static CharacterObject Armour()
		{
			if (_looked)
			{
				return _armour;
			}
			_looked = true;
			try
			{
				List<CharacterObject> all = CharacterObject.All.Where((CharacterObject c) => c != null && !c.IsHero && c.FirstBattleEquipment != null).ToList();
				_armour = all.Where((CharacterObject c) => ((MBObjectBase)c).StringId.ToLowerInvariant().Contains("kingsguard") || c.Name.ToString().ToLowerInvariant().Contains("kingsguard"))
					.OrderByDescending((CharacterObject c) => c.Tier).FirstOrDefault()
					?? all.Where((CharacterObject c) => c.Name.ToString().ToLowerInvariant().Contains("white cloak"))
					.OrderByDescending((CharacterObject c) => c.Tier).FirstOrDefault();
				Log.Write((_armour != null)
					? ("the white armour is " + _armour.Name + " (" + ((MBObjectBase)_armour).StringId + ")")
					: "no Kingsguard troop found to take the white armour from; knights keep their own");
			}
			catch (Exception e)
			{
				Log.Write("looking for the white armour failed: " + e.Message);
			}
			return _armour;
		}

		// Dress them. Their old battle kit is gone - the cloak is all they
		// need now. Town clothes are left as they are.
		internal static bool Dress(Hero h)
		{
			try
			{
				CharacterObject white = Armour();
				if (!Cfg.KgArmour || h == null || white == null)
				{
					return false;
				}
				h.BattleEquipment.FillFrom(white.FirstBattleEquipment, false);
				Knight k = Of(h);
				if (k != null && !k.Cloaked)
				{
					k.Cloaked = true;
					Save(k);
				}
				return true;
			}
			catch (Exception e)
			{
				Log.Write("dressing " + ((h != null) ? h.Name.ToString() : "a knight") + " failed: " + e.Message);
				return false;
			}
		}

		// The swearing itself, in the hall.
		private static void Ceremony(Hero h, Knight k, string origin)
		{
			bool dressed = Dress(h);
			string order = Title();
			string who = (h.FirstName != null) ? h.FirstName.ToString() : h.Name.ToString();
			string from;
			switch ((origin ?? "").Split(':')[0])
			{
			case "commoner":
				from = who + " came into the hall a common soldier, in the kit " + (h.IsFemale ? "she" : "he") + " had marched in, and everybody knew it.";
				break;
			case "baseborn":
				from = who + " came into the hall with your face and without your name, and everybody knew that too.";
				break;
			case "ward":
				from = who + " came into the hall as the ward " + (h.IsFemale ? "she" : "he") + " had been since childhood, at the table of the house " + (h.IsFemale ? "she" : "he") + " now swears to die for.";
				break;
			case "noble":
				from = who + " came into the hall a child of a great house, with a name and an inheritance, and knelt to give up both.";
				break;
			default:
				from = who + " came into the hall a sword of your household, as " + (h.IsFemale ? "she" : "he") + " has been for years.";
				break;
			}
			string text = from + "\n\n" +
				"The words were spoken before the throne, with the court standing: to guard the crown's person with " + (h.IsFemale ? "her" : "his") + " life, to take no wife, hold no lands, father no children, and keep " + (h.IsFemale ? "her" : "his") + " silence and the crown's counsel until death.\n\n" +
				(dressed
					? ("The white cloak was fastened at " + (h.IsFemale ? "her" : "his") + " throat, and the white armour brought out and buckled on. When " + who + " stood, nobody in the hall would have known what " + (h.IsFemale ? "she" : "he") + " had been an hour before.\n\n")
					: ("The white cloak was fastened at " + (h.IsFemale ? "her" : "his") + " throat.\n\n")) +
				"The Lord Commander opened the White Book to a clean page." +
				((k.Rank >= 1) ? (" It was " + who + "'s own - the first page of the " + order + ", and " + (h.IsFemale ? "hers" : "his") + " to keep.") : "");
			Popup("The White Cloak", text);
		}

		// Knights sworn before the ceremony existed get dressed once, on load.
		internal static void Repair()
		{
			try
			{
				if (!Cfg.Kingsguard || !Cfg.KgArmour)
				{
					return;
				}
				int n = 0;
				foreach (Knight k in All())
				{
					if (!k.Cloaked && Dress(HeroOf(k)))
					{
						n++;
					}
				}
				if (n > 0)
				{
					Log.Write("sworn knights dressed in the white armour on load: " + n);
				}
			}
			catch (Exception e)
			{
				Log.Write("dressing the sworn knights failed: " + e.Message);
			}
		}

		private static void Popup(string title, string text)
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
