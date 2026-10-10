using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The younger sons and daughters, and the kin who are not the head of
	// their house, want things: a fortune, a match, a house of their own, the
	// seat itself, a vow, glory. Now and then they act on it.
	internal static class Ambition
	{
		private const string ForbidPrefix = "amb:no:";

		internal static readonly string[] Kinds = new string[6] { "fortune", "match", "house", "seat", "vow", "squire" };

		internal static string KindName(string k)
		{
			switch (k)
			{
			case "fortune": return "to seek their fortune";
			case "match": return "to marry for love";
			case "house": return "to found a house of their own";
			case "seat": return "to sit in the head of the house's chair";
			case "vow": return "to take a vow and leave the world";
			case "squire": return "to squire for a great lord";
			}
			return "nothing in particular";
		}

		// Each hero's ambition, decided by who they are; re-drawn every few years.
		internal static string Of(Hero h)
		{
			if (h == null)
			{
				return null;
			}
			int seed = Math.Abs((((MBObjectBase)h).StringId + (CourtBehavior.Today() / Math.Max(1, Cfg.DaysPerYear * 3))).GetHashCode());
			if (h.IsChild)
			{
				return (h.Age >= 14) ? "squire" : null;
			}
			bool heir = h.Clan != null && h.Clan.Leader != null && (h.Father == h.Clan.Leader || h.Mother == h.Clan.Leader);
			List<string> want = new List<string>();
			if (h.Spouse == null && h.Age < 35) { want.Add("match"); want.Add("match"); }
			if (h.PartyBelongedTo == null) { want.Add("fortune"); }
			if (h.Age >= 25 && h.Clan != null && h.Clan.Tier >= 3) { want.Add("house"); }
			if (heir && h.Clan.Leader.Age > 55) { want.Add("seat"); }
			if (h.Age > 30 && h.Spouse == null) { want.Add("vow"); }
			if (want.Count == 0) { want.Add("fortune"); }
			return want[seed % want.Count];
		}

		private static bool Eligible(Hero h)
		{
			return h != null && h.IsAlive && h.IsLord && h != Hero.MainHero && !h.IsPrisoner && h.Clan != null && h.Clan.Leader != h
				&& !h.Clan.IsBanditFaction && !h.Clan.IsMinorFaction && !Guard.IsSworn(h) && h.Age >= 14
				&& (h.PartyBelongedTo == null || h.PartyBelongedTo.MapEvent == null);
		}

		internal static bool Forbidden(Hero h)
		{
			return Store.Get(ForbidPrefix + ((MBObjectBase)h).StringId) == "1";
		}

		internal static void SetForbidden(Hero h, bool no)
		{
			Store.Set(ForbidPrefix + ((MBObjectBase)h).StringId, no ? "1" : null);
			if (no)
			{
				ChangeRelationAction.ApplyPlayerRelation(h, -5, false, true);
			}
		}

		internal static void Daily(int today)
		{
			if (!Cfg.Ambitions || !Store.Initialized || today - Store.GetI("amb:last", -9999) < Cfg.AmbitionStepDays)
			{
				return;
			}
			Store.SetI("amb:last", today);
			int acted = 0;
			foreach (Hero h in Hero.AllAliveHeroes.Where(Eligible).ToList())
			{
				bool mine = h.Clan == Clan.PlayerClan;
				int chance = mine ? Cfg.AmbitionFamilyChance : Cfg.AmbitionChance;
				if (MBRandom.RandomFloat * 1000f >= chance * 10f)
				{
					continue;
				}
				if (mine && Forbidden(h))
				{
					continue;
				}
				try
				{
					if (Act(h, Of(h)))
					{
						acted++;
					}
				}
				catch (Exception e)
				{
					Log.Once("amb:" + ((MBObjectBase)h).StringId, "ambition: " + h.Name + " failed: " + e.Message);
				}
				if (acted >= 6)
				{
					break;
				}
			}
		}

		private static string N(Hero h) { return h.Name.ToString(); }

		internal static bool Act(Hero h, string what)
		{
			bool mine = h.Clan == Clan.PlayerClan;
			switch (what)
			{
			case "fortune":
				return Fortune(h, mine);
			case "match":
				return Match(h, mine);
			case "house":
				return House(h, mine);
			case "seat":
				return Seat(h, mine);
			case "vow":
				return Vow(h, mine);
			case "squire":
				return Squire(h, mine);
			}
			return false;
		}

		private static void Tell(Hero h, string title, string text)
		{
			if (h.Clan == Clan.PlayerClan || Nemesis.Touches(h))
			{
				Ravens.Popup(title, text);
			}
		}

		private static bool Fortune(Hero h, bool mine)
		{
			if (h.PartyBelongedTo != null || h.CurrentSettlement == null || h.IsChild)
			{
				return false;
			}
			Clan c = h.Clan;
			int limit;
			try
			{
				limit = Campaign.Current.Models.ClanTierModel.GetPartyLimitForTier(c, c.Tier);
			}
			catch
			{
				limit = 2;
			}
			if (c.WarPartyComponents.Count >= limit)
			{
				// No banner to spare: they ride as a household knight and win a name.
				c.AddRenown(5f, false);
				h.SetSkillValue(DefaultSkills.OneHanded, Math.Min(300, h.GetSkillValue(DefaultSkills.OneHanded) + 10));
				History.Hero(h, "Rode in the tourneys and the wars as a household knight, and made a name.", 2);
				Tell(h, "Seeking a Fortune", N(h) + " has ridden out to win a name in the lists and the wars, and is coming back with one.");
				return true;
			}
			MobileParty p = MobilePartyHelper.CreateNewClanMobileParty(h, c);
			if (p == null)
			{
				return false;
			}
			History.Hero(h, "Rode out with a company of their own to seek their fortune.", 2);
			Log.Write("ambition: " + N(h) + " rides out to seek their fortune");
			Tell(h, "Seeking a Fortune", N(h) + " has gathered a company and ridden out to seek their fortune under your banner.");
			return true;
		}

		private static bool Match(Hero h, bool mine)
		{
			if (h.Spouse != null || h.IsChild)
			{
				return false;
			}
			Hero b = Hero.AllAliveHeroes.Where((Hero x) => x != h && x.IsLord && !x.IsChild && x.Spouse == null && x.IsFemale != h.IsFemale && x.Clan != null && x.Clan != h.Clan && !x.IsPrisoner
				&& x.GetRelation(h) >= 20 && Campaign.Current.Models.MarriageModel.IsCoupleSuitableForMarriage(h, x)).OrderByDescending((Hero x) => x.GetRelation(h)).FirstOrDefault();
			if (b == null)
			{
				return false;
			}
			Hero head = h.Clan.Leader;
			bool approved = head == null || b.Clan.Leader == null || head.GetRelation(b.Clan.Leader) >= 0 || mine;
			if (!approved)
			{
				// Against the family's will: that is an elopement, and a scandal.
				return Scandal.Elope(false).StartsWith("Elopement");
			}
			MarriageAction.Apply(h, b, false);
			History.Hero(h, "Married " + N(b) + " for love.", 2);
			History.Hero(b, "Married " + N(h) + " for love.", 2);
			Log.Write("ambition: " + N(h) + " married " + N(b) + " by their own choice");
			Tell(h, "A Match", N(h) + " has married " + N(b) + " of " + b.Clan.Name + " - their own choice, and a happy one.");
			return true;
		}

		private static bool House(Hero h, bool mine)
		{
			if (!Cfg.Sworn || h.IsChild || h.Clan.Tier < 3 || h.Clan.Heroes.Count((Hero x) => x.IsAlive && !x.IsChild) < 4)
			{
				return false;
			}
			if (Sworn.CannotGain(h.Clan, !mine) != null)
			{
				return false;
			}
			Clan house = Sworn.Gain(h.Clan, "cadet", h, null, null, !mine);
			if (house == null)
			{
				return false;
			}
			History.Hero(h, "Founded " + house.Name + ", a cadet branch of " + h.Clan.Name + ".", 3);
			Tell(h, "A House of Their Own", N(h) + " has founded " + house.Name + ", a cadet branch sworn to your house.");
			return true;
		}

		private static bool Seat(Hero h, bool mine)
		{
			Hero head = h.Clan.Leader;
			if (head == null || head == h || h.IsChild)
			{
				return false;
			}
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(h, head, -15, false);
			History.Hero(h, "Grew tired of waiting for " + N(head) + "'s chair, and said so.", 2);
			Log.Write("ambition: " + N(h) + " covets " + N(head) + "'s seat");
			if (mine)
			{
				Tell(h, "An Impatient Heir", N(h) + " has been heard asking, a little too loudly, how long you mean to live.");
				return true;
			}
			// In other houses it can go further.
			int roll = MBRandom.RandomInt(100);
			if (roll < 4 && head.Age > 60 && h.GetRelation(head) < -40)
			{
				History.Hero(h, "Was suspected of poisoning " + N(head) + ".", 3);
				History.House(h.Clan, N(head) + " died suddenly, and people looked at " + N(h) + ".", 3);
				Log.Write("ambition: " + N(h) + " poisons " + N(head));
				KillCharacterAction.ApplyByMurder(head, h, true);
				return true;
			}
			if (roll < 12 && h.GetRelation(head) < -30)
			{
				Clan rival = Clan.All.Where((Clan c) => c != h.Clan && !c.IsEliminated && !c.IsBanditFaction && !c.IsMinorFaction && c.Leader != null && c.Kingdom != null && h.MapFaction != null
					&& FactionManager.IsAtWarAgainstFaction(c.Kingdom, h.MapFaction)).OrderByDescending((Clan c) => c.Tier).FirstOrDefault();
				if (rival != null && h.PartyBelongedTo == null)
				{
					h.Clan = rival;
					History.Hero(h, "Left " + head.Clan.Name + " in anger and took service with " + rival.Name + ", their enemies.", 3);
					History.House(head.Clan, N(h) + " went over to " + rival.Name + ".", 3);
					Log.Write("ambition: " + N(h) + " defects to " + rival.Name);
					Tell(h, "A Defection", N(h) + " has left their house and gone over to " + rival.Name + ".");
				}
			}
			return true;
		}

		private static bool Vow(Hero h, bool mine)
		{
			if (h.Spouse != null || h.PartyBelongedTo != null || h.IsChild)
			{
				return false;
			}
			Clan watch = Clan.All.FirstOrDefault((Clan c) => !c.IsEliminated && c.Name != null && c.Name.ToString().IndexOf("Night's Watch", StringComparison.OrdinalIgnoreCase) >= 0);
			if (mine)
			{
				// Your own kin only go with your leave: you are told, and nothing more.
				History.Hero(h, "Spoke of taking a vow and leaving the world.", 1);
				Tell(h, "A Vow", N(h) + " has been speaking of taking vows. Forbid it at court if you would keep them.");
				return true;
			}
			if (watch != null)
			{
				h.Clan = watch;
				History.Hero(h, "Took the black, and went to the Wall.", 3);
				Log.Write("ambition: " + N(h) + " takes the black");
			}
			else
			{
				History.Hero(h, "Took holy vows and left the world.", 3);
				Log.Write("ambition: " + N(h) + " takes vows");
				DisableHeroAction.Apply(h);
			}
			return true;
		}

		private static bool Squire(Hero h, bool mine)
		{
			if (!h.IsChild || h.Age < 14)
			{
				return false;
			}
			Hero lord = Hero.AllAliveHeroes.Where((Hero x) => x.IsLord && !x.IsChild && x.Clan != null && x.Clan != h.Clan && x.MapFaction == h.MapFaction && x.Clan.Tier >= 3 && x.PartyBelongedTo != null)
				.OrderByDescending((Hero x) => x.Clan.Renown).FirstOrDefault();
			if (lord == null)
			{
				return false;
			}
			foreach (SkillObject s in new SkillObject[3] { DefaultSkills.OneHanded, DefaultSkills.Riding, DefaultSkills.Athletics })
			{
				h.SetSkillValue(s, Math.Min(200, h.GetSkillValue(s) + 15));
			}
			ChangeRelationAction.ApplyRelationChangeBetweenHeroes(h, lord, 15, false);
			History.Hero(h, "Squired for " + N(lord) + ".", 2);
			History.Hero(lord, "Took " + N(h) + " as a squire.", 1);
			Tell(h, "A Squire", N(h) + " has gone to squire for " + N(lord) + ", and is learning the sword and the saddle.");
			return true;
		}
	}
}
