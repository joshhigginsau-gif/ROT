using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Exile, and the company that comes back.
	//
	// Beat the claimant and he does not die - he takes ship. Across the
	// Narrow Sea he, or the champion who carried his son out, founds a
	// sellsword company, and it grows. Ten or twenty years on it lands with
	// his son at its head and somebody's gold behind it, takes a castle and
	// is crowned. Beat the son, and the next one takes ship.
	internal static class Exile
	{
		private const string StateKey = "ex:state";   // exiled / landed / done
		private const string RivalKey = "ex:rival";
		private const string HouseKey = "ex:house";
		private const string CompanyKey = "ex:company";
		private const string CaptainKey = "ex:captain";
		private const string HeirKey = "ex:heir";
		private const string ReturnKey = "ex:return";
		private const string GenKey = "ex:gen";
		private const string MenKey = "ex:men";
		private const string GrowKey = "ex:grow";
		private const string HireKey = "ex:hire";
		private const string PartyKey = "ex:party";
		private const string TownKey = "ex:town";

		private static readonly string[] Colours = { "Ashen", "Crimson", "Silver", "Iron", "Bright", "Grey", "Sable", "Amber", "Pale", "Scarlet", "Azure", "Bronze", "Black", "Gilded", "Burning", "Broken" };

		private static readonly string[] CompanyWords = { "Beneath the Gold, the Bitter Steel", "We Remember", "The Sea Gives Back", "Paid in Blood", "Every Oath Kept", "Across the Water", "Not Forgotten", "Home, One Day" };

		private static string State
		{
			get
			{
				return Store.Get(StateKey) ?? "";
			}
		}

		private static Hero HeroOf(string key)
		{
			string id = Store.Get(key);
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			return Hero.AllAliveHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id)
				?? Hero.DeadOrDisabledHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
		}

		private static Clan ClanOf(string id)
		{
			return string.IsNullOrEmpty(id) ? null : Clan.All.FirstOrDefault((Clan c) => ((MBObjectBase)c).StringId == id);
		}

		internal static Clan Company
		{
			get
			{
				return ClanOf(Store.Get(CompanyKey));
			}
		}

		// ------------------------------------------------------------------
		// who is the rival, and has he lost

		// The claimant you fought, and the house he fought for.
		private static bool Rival(out Hero rival, out Clan house)
		{
			rival = null;
			house = null;
			if (State == "landed")
			{
				rival = HeroOf(RivalKey);
				house = ClanOf(Store.Get(HouseKey));
				return house != null;
			}
			if (State != "" || !Bastard.Risen)
			{
				return false;
			}
			Clan bastards = ClanOf(Store.Get("bs:house"));
			if (bastards != null && bastards == Clan.PlayerClan)
			{
				// You took up his banner: the loser is the house you left.
				house = ClanOf(Store.Get("bs:old"));
				if (house == null)
				{
					house = Clan.All.Where((Clan c) => c != Clan.PlayerClan && !c.IsEliminated && c.Leader != null && Succession.IsBlood(c.Leader, Hero.MainHero))
						.OrderByDescending((Clan c) => c.Tier).FirstOrDefault();
				}
				rival = (house != null) ? house.Leader : null;
				return house != null;
			}
			house = bastards;
			rival = HeroOf("bs:head");
			return house != null;
		}

		// Nothing left to hold: no castle or town in his house, or in his
		// realm if his house rules one.
		private static bool Lost(Clan house)
		{
			if (house == null || house.IsEliminated)
			{
				return true;
			}
			try
			{
				Kingdom k = house.Kingdom;
				if (k != null && k.RulingClan == house)
				{
					return !k.Settlements.Any((Settlement s) => s.IsFortification);
				}
				return !house.Settlements.Any((Settlement s) => s.IsFortification);
			}
			catch
			{
				return false;
			}
		}

		// ------------------------------------------------------------------
		// the day

		internal static void Daily()
		{
			try
			{
				if (!Cfg.Exile || !Store.Initialized)
				{
					return;
				}
				string state = State;
				if (state == "exiled")
				{
					Waiting();
					return;
				}
				Hero rival;
				Clan house;
				if (!Rival(out rival, out house))
				{
					return;
				}
				if (Store.GetI(GenKey, 1) > Cfg.ExileMaxGenerations)
				{
					return;
				}
				if (Lost(house))
				{
					Beaten(rival, house);
				}
			}
			catch (Exception e)
			{
				Log.Once("exdaily", "the exile's tick failed: " + e.Message);
			}
		}

		private static void Beaten(Hero rival, Clan house)
		{
			IFaction mine = Clan.PlayerClan.MapFaction;
			if (rival != null && rival.IsAlive && rival.IsPrisoner && rival.PartyBelongedToAsPrisoner != null && rival.PartyBelongedToAsPrisoner.MapFaction == mine)
			{
				// Yours to decide.
				Store.Set(StateKey, "deciding");
				Inquiry.Confirm("The Claimant in Chains", rival.Name + " is your prisoner, and there is nothing left for him to be king of.\n\nThe axe would end it. Or you could let him take ship - and hope the sea keeps him.",
					"Let him take ship", "The axe",
					delegate
					{
						Standing.Change(3, 0, "Let " + rival.Name + " take ship");
						Store.Set(StateKey, null);
						Ship(rival, rival, house);
					},
					delegate
					{
						Standing.Change(0, 10, "Executed " + rival.Name);
						Store.Set(StateKey, null);
						Hero son = SonOf(rival);
						try
						{
							KillCharacterAction.ApplyByExecution(rival, Hero.MainHero, true, true);
						}
						catch (Exception e)
						{
							Log.Write("the execution failed: " + e.Message);
						}
						Ship(Champion(house, rival), son, house);
					});
				return;
			}
			if (State == "deciding")
			{
				return;
			}
			if (rival != null && rival.IsAlive)
			{
				Ship(rival, SonOf(rival), house);
			}
			else
			{
				Ship(Champion(house, rival), SonOf(rival), house);
			}
		}

		private static Hero SonOf(Hero h)
		{
			if (h == null)
			{
				return null;
			}
			try
			{
				return h.Children.Where((Hero c) => c.IsAlive && !c.IsFemale && c != Hero.MainHero && c.Clan != Clan.PlayerClan).OrderByDescending((Hero c) => c.Age).FirstOrDefault()
					?? h.Children.Where((Hero c) => c.IsAlive && c != Hero.MainHero && c.Clan != Clan.PlayerClan).OrderByDescending((Hero c) => c.Age).FirstOrDefault();
			}
			catch
			{
				return null;
			}
		}

		// The sword who carries the cause over the sea when its claimant cannot.
		private static Hero Champion(Clan house, Hero rival)
		{
			try
			{
				if (house != null)
				{
					Hero best = house.Heroes.Where((Hero h) => h.IsAlive && !h.IsChild && !h.IsPrisoner && h != Hero.MainHero)
						.OrderByDescending((Hero h) => h.GetSkillValue(DefaultSkills.OneHanded)).FirstOrDefault();
					if (best != null)
					{
						return best;
					}
				}
				CultureObject culture = (rival != null) ? rival.Culture : ((house != null) ? house.Culture : Clan.PlayerClan.Culture);
				return Make(culture, MBRandom.RandomInt(35, 50), "Ser ");
			}
			catch
			{
				return null;
			}
		}

		internal static Hero Make(CultureObject culture, int age, string prefix)
		{
			CharacterObject template = Bastard.Template(culture);
			if (template == null)
			{
				return null;
			}
			Settlement born = Settlement.All.FirstOrDefault((Settlement s) => s.IsTown && s.Culture == culture) ?? Settlement.All.First((Settlement s) => s.IsTown);
			Hero h = HeroCreator.CreateSpecialHero(template, born, null, null, age);
			if (h == null)
			{
				return null;
			}
			h.ChangeState(Hero.CharacterStates.Active);
			try
			{
				h.SetNewOccupation(Occupation.Lord);
			}
			catch
			{
			}
			Baseborn.Visible(h);
			string first = (h.FirstName != null) ? h.FirstName.ToString() : "Aegor";
			if (prefix.Length > 0)
			{
				h.SetName(new TextObject("{=!}" + prefix + first, (Dictionary<string, object>)null), new TextObject("{=!}" + first, (Dictionary<string, object>)null));
			}
			return h;
		}

		// ------------------------------------------------------------------
		// taking ship

		private static Settlement Harbour()
		{
			List<Settlement> essos = Settlement.All.Where((Settlement s) => s.IsTown && Knighting.Essos(s.Culture) && s.MapFaction != Clan.PlayerClan.MapFaction).ToList();
			if (essos.Count > 0)
			{
				return essos[MBRandom.RandomInt(essos.Count)];
			}
			Vec2 home = (Clan.PlayerClan.HomeSettlement != null) ? Clan.PlayerClan.HomeSettlement.GetPosition2D : MobileParty.MainParty.GetPosition2D;
			return Settlement.All.Where((Settlement s) => s.IsTown && s.MapFaction != Clan.PlayerClan.MapFaction).OrderByDescending((Settlement s) => s.GetPosition2D.Distance(home)).FirstOrDefault();
		}

		private static string CompanyName()
		{
			for (int i = 0; i < 30; i++)
			{
				string n = "The " + Colours[MBRandom.RandomInt(Colours.Length)] + " Company";
				if (!Clan.All.Any((Clan c) => c.Name != null && c.Name.ToString() == n))
				{
					return n;
				}
			}
			return "The Exiles' Company";
		}

		private static void Ship(Hero captain, Hero son, Clan house)
		{
			try
			{
				if (captain == null)
				{
					Log.Write("exile: nobody to take ship");
					Store.Set(StateKey, "done");
					return;
				}
				int gen = Store.GetI(GenKey, 1);
				Settlement harbour = Harbour();
				if (harbour == null)
				{
					Log.Write("exile: no harbour across the sea");
					return;
				}
				// Free, and out of his old war.
				if (captain.IsPrisoner)
				{
					try
					{
						EndCaptivityAction.ApplyByReleasedByChoice(captain, Hero.MainHero);
					}
					catch
					{
					}
				}
				MobileParty old = captain.PartyBelongedTo;
				if (old != null && old.LeaderHero == captain && old != MobileParty.MainParty)
				{
					try
					{
						DestroyPartyAction.Apply(null, old);
					}
					catch
					{
					}
				}
				Clan company = Clan.CreateClan("wad_company_" + gen + "_" + CourtBehavior.Today());
				string name = CompanyName();
				Bastard.Set(company, "Name", new TextObject("{=!}" + name, (Dictionary<string, object>)null));
				Bastard.Set(company, "InformalName", new TextObject("{=!}" + name.Replace("The ", ""), (Dictionary<string, object>)null));
				company.Culture = captain.Culture ?? ((house != null) ? house.Culture : Clan.PlayerClan.Culture);
				company.Banner = (house != null && house.Banner != null) ? house.Banner : Banner.CreateRandomClanBanner(-1);
				company.Color = (house != null) ? house.Color : company.Banner.GetPrimaryColor();
				company.Color2 = (house != null) ? house.Color2 : company.Banner.GetFirstIconColor();
				Bastard.Set(company, "Tier", 4);
				captain.Clan = company;
				company.SetLeader(captain);
				if (son != null && son.IsAlive && son != captain)
				{
					son.Clan = company;
				}
				try
				{
					company.SetInitialHomeSettlement(harbour);
				}
				catch
				{
				}
				Bastard.Call(company, "CalculateMidSettlement");
				Bastard.Announce(company);
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(captain, harbour);
				if (son != null && son.IsAlive && son != captain)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(son, harbour);
				}
				MobileParty party = MobilePartyHelper.CreateNewClanMobileParty(captain, company);
				int men = Cfg.ExileStartMen;
				if (party != null)
				{
					Host.Fill(party, Host.Troops(Host.Veteran, company), Host.Veteran, men);
					Host.Protect(((MBObjectBase)party).StringId);
					Store.Set(PartyKey, ((MBObjectBase)party).StringId);
				}
				int years = MBRandom.RandomInt(Cfg.ExileYearsMin, Cfg.ExileYearsMax + 1);
				int back = CourtBehavior.Today() + years * Cfg.DaysPerYear;
				string words = CompanyWords[MBRandom.RandomInt(CompanyWords.Length)];
				Store.Set(StateKey, "exiled");
				Store.Set(CompanyKey, ((MBObjectBase)company).StringId);
				Store.Set(CaptainKey, ((MBObjectBase)captain).StringId);
				Store.Set(HeirKey, (son != null) ? ((MBObjectBase)son).StringId : "");
				Store.SetI(ReturnKey, back);
				Store.SetI(MenKey, men);
				Store.SetI(GrowKey, CourtBehavior.Today());
				Store.Set(TownKey, ((MBObjectBase)harbour).StringId);
				Bastard.Set(company, "EncyclopediaText", new TextObject("{=!}" + Page(company, captain, son, house, harbour, words), (Dictionary<string, object>)null));
				try
				{
					string before = (captain.EncyclopediaText != null) ? captain.EncyclopediaText.ToString() : "";
					captain.EncyclopediaText = new TextObject("{=!}" + before + ((before.Length > 0) ? "\n\n" : "") + "On " + Standing.Date() + ", with nothing left to hold, " + captain.Name +
						" took ship for " + harbour.Name + " and founded " + name + ".", (Dictionary<string, object>)null);
				}
				catch
				{
				}
				Store.AddDeed(Standing.Date() + "  " + captain.Name + " took ship for " + harbour.Name + ", and founded " + name + ".");
				Log.Write("exile (gen " + gen + "): " + captain.Name + " took ship for " + harbour.Name + ", founded " + name + (son != null ? (" with " + son.Name) : "") + "; returns in " + years + " years (day " + back + ")");
				Ravens.Popup("He Takes Ship", ((captain == son || son == null) ? captain.Name.ToString() : (captain.Name + ", with " + son.Name + " beside him,")) +
					" has taken ship across the Narrow Sea, to " + harbour.Name + ". The men who would not kneel went with " + ((captain.IsFemale) ? "her" : "him") + ".\n\n" +
					"They call themselves " + name + " now, and sell their swords to anyone but you. The war is won. Whether it is over is another matter.");
			}
			catch (Exception e)
			{
				Log.Write("taking ship failed: " + e);
			}
		}

		private static string Page(Clan company, Hero captain, Hero son, Clan house, Settlement harbour, string words)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(company.Name).Append(" was founded in ").Append(harbour.Name).Append(" on ").Append(Standing.Date()).Append(" by ").Append(captain.Name)
			  .Append(", who took ship with the last of ").Append((house != null) ? house.Name.ToString() : "a lost cause").Append(" when the war for the crown was lost. ");
			if (son != null && son != captain)
			{
				sb.Append("With ").Append(captain.IsFemale ? "her" : "him").Append(" went ").Append(son.Name).Append(", the claimant's heir, for whom the company keeps the claim warm. ");
			}
			sb.Append("It sells its swords in the wars of the Free Cities, and to any lord in Westeros but one. Its words are \"").Append(words).Append("\".\n\n");
			sb.Append("Every sellsword in it is waiting for the same thing.");
			return sb.ToString();
		}

		// ------------------------------------------------------------------
		// the waiting years

		private static void Waiting()
		{
			int today = CourtBehavior.Today();
			Clan company = Company;
			if (company == null || company.IsEliminated)
			{
				Log.Write("exile: the company is gone");
				Store.Set(StateKey, "done");
				return;
			}
			// It grows.
			if (today - Store.GetI(GrowKey, today) >= 21)
			{
				Store.SetI(GrowKey, today);
				int men = Math.Min(Cfg.ExileMaxMen, Store.GetI(MenKey, 0) + Cfg.ExileGrowthMen);
				int added = men - Store.GetI(MenKey, 0);
				Store.SetI(MenKey, men);
				MobileParty p = Party();
				if (p != null && added > 0)
				{
					Host.Fill(p, Host.Troops(Host.Veteran, company), Host.Veteran, added);
				}
			}
			int back = Store.GetI(ReturnKey, today);
			// Hired out, by anyone but you, until a year before the landing.
			if (Cfg.ExileHireable && back - today > Cfg.DaysPerYear && company.Kingdom == null && today - Store.GetI(HireKey, -9999) >= 7)
			{
				Store.SetI(HireKey, today);
				if (MBRandom.RandomInt(100) < 10)
				{
					Kingdom k = Kingdom.All.Where((Kingdom x) => !x.IsEliminated && x.Leader != null && x.Leader.IsAlive && x != Clan.PlayerClan.Kingdom && x.RulingClan != Clan.PlayerClan
						&& Kingdom.All.Any((Kingdom o) => o != x && FactionManager.IsAtWarAgainstFaction(o, x))).OrderByDescending((Kingdom x) => x.Leader.Gold).FirstOrDefault();
					if (k != null)
					{
						try
						{
							ChangeKingdomAction.ApplyByJoinFactionAsMercenary(company, k, CampaignTime.DaysFromNow((float)Cfg.DaysPerYear), 50, false);
							Log.Write("exile: " + company.Name + " hired by " + k.Name);
						}
						catch
						{
						}
					}
				}
			}
			if (today >= back)
			{
				Land();
			}
		}

		private static MobileParty Party()
		{
			string id = Store.Get(PartyKey);
			return string.IsNullOrEmpty(id) ? null : MobileParty.All.FirstOrDefault((MobileParty p) => ((MBObjectBase)p).StringId == id && p.IsActive);
		}

		// ------------------------------------------------------------------
		// the landing

		private static void Land()
		{
			try
			{
				Clan company = Company;
				Hero captain = HeroOf(CaptainKey);
				Hero son = HeroOf(HeirKey);
				if (company == null)
				{
					Store.Set(StateKey, "done");
					return;
				}
				if (company.IsUnderMercenaryService)
				{
					try
					{
						ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(company, false);
					}
					catch
					{
					}
				}
				// The son, grown - or one the years have made.
				if (son == null || !son.IsAlive || son.IsChild)
				{
					Hero made = Make(company.Culture, MBRandom.RandomInt(20, 29), "");
					if (made != null)
					{
						if (captain != null)
						{
							try
							{
								made.Father = captain;
							}
							catch
							{
							}
						}
						string surname = (captain != null && captain.Name != null && captain.FirstName != null) ? captain.Name.ToString().Replace(captain.FirstName.ToString(), "").Replace("Ser ", "").Trim() : "";
						string first = (made.FirstName != null) ? made.FirstName.ToString() : "Daemon";
						string full = (surname.Length > 0) ? (first + " " + surname) : (first + " of " + company.Name.ToString().Replace("The ", "the "));
						made.SetName(new TextObject("{=!}" + full, (Dictionary<string, object>)null), new TextObject("{=!}" + first, (Dictionary<string, object>)null));
						made.Clan = company;
						son = made;
					}
				}
				if (son == null)
				{
					Log.Write("exile: nobody to lead the landing");
					Store.Set(StateKey, "done");
					return;
				}
				// Who pays.
				Kingdom mine = Clan.PlayerClan.Kingdom;
				IFaction yours = Clan.PlayerClan.MapFaction;
				Kingdom funder = Kingdom.All.Where((Kingdom k) => !k.IsEliminated && k != mine && k.Leader != null && k.Leader.IsAlive && k.RulingClan != Clan.PlayerClan)
					.OrderByDescending((Kingdom k) => (yours != null && FactionManager.IsAtWarAgainstFaction(k, yours)) ? 1000 : 0)
					.ThenBy((Kingdom k) => k.Leader.GetRelationWithPlayer()).FirstOrDefault();
				int funds = 0;
				if (funder != null)
				{
					funds = Math.Min(Cfg.ExileFundCap, (int)((long)funder.Leader.Gold * 30 / 100));
					funder.Leader.ChangeHeroGold(-funds);
				}
				// Where.
				List<Settlement> ours = Clan.PlayerClan.Settlements.Where((Settlement s) => s.IsCastle).ToList();
				if (ours.Count == 0 && mine != null)
				{
					ours = mine.Settlements.Where((Settlement s) => s.IsCastle).ToList();
				}
				if (ours.Count == 0)
				{
					ours = Clan.PlayerClan.Settlements.Where((Settlement s) => s.IsTown).ToList();
				}
				if (ours.Count == 0)
				{
					Log.Write("exile: no castle to land at; the landing waits a season");
					Store.SetI(ReturnKey, CourtBehavior.Today() + 21);
					return;
				}
				Vec2 home = (Clan.PlayerClan.HomeSettlement != null) ? Clan.PlayerClan.HomeSettlement.GetPosition2D : MobileParty.MainParty.GetPosition2D;
				Settlement seat = ours.OrderByDescending((Settlement s) => s.GetPosition2D.Distance(home)).First();
				ChangeOwnerOfSettlementAction.ApplyByGift(seat, son);
				company.SetLeader(son);
				// The old company party is replaced by the landing force.
				MobileParty old = Party();
				if (old != null)
				{
					Host.Unprotect(((MBObjectBase)old).StringId);
					try
					{
						DestroyPartyAction.Apply(null, old);
					}
					catch
					{
					}
				}
				TeleportHeroAction.ApplyImmediateTeleportToSettlement(son, seat);
				MobileParty host = MobilePartyHelper.CreateNewClanMobileParty(son, company);
				int men = Store.GetI(MenKey, Cfg.ExileStartMen) + funds / Math.Max(1, Cfg.HostPriceVeteran);
				men = Math.Min(Cfg.HostMaxMen, men);
				if (host != null)
				{
					Host.Fill(host, Host.Troops(Host.Veteran, company), Host.Veteran, men);
					Host.Protect(((MBObjectBase)host).StringId);
					Store.Set(PartyKey, ((MBObjectBase)host).StringId);
				}
				if (captain != null && captain.IsAlive && captain != son)
				{
					TeleportHeroAction.ApplyImmediateTeleportToSettlement(captain, seat);
				}
				Kingdom realm = Bastard.Crown(company, son, seat);
				if (realm != null)
				{
					try
					{
						string colour = company.Name.ToString().Replace("The ", "").Replace(" Company", "");
						realm.ChangeKingdomName(new TextObject("{=!}The " + colour + " Crown", (Dictionary<string, object>)null), new TextObject("{=!}" + colour, (Dictionary<string, object>)null));
					}
					catch
					{
					}
					if (yours != null)
					{
						try
						{
							DeclareWarAction.ApplyByDefault(realm, yours);
						}
						catch (Exception we)
						{
							Log.Write("exile: the war would not be declared: " + we.Message);
						}
					}
				}
				int gen = Store.GetI(GenKey, 1);
				Store.Set(StateKey, "landed");
				Store.Set(RivalKey, ((MBObjectBase)son).StringId);
				Store.Set(HouseKey, ((MBObjectBase)company).StringId);
				Store.SetI(GenKey, gen + 1);
				Store.AddDeed(Standing.Date() + "  " + company.Name + " landed at " + seat.Name + " under " + son.Name + ".");
				try
				{
					string before = (company.EncyclopediaText != null) ? company.EncyclopediaText.ToString() : "";
					Bastard.Set(company, "EncyclopediaText", new TextObject("{=!}" + before + "\n\nOn " + Standing.Date() + " it landed at " + seat.Name + " under " + son.Name +
						((funder != null) ? (", with the gold of " + funder.Name + " behind it") : "") + ", and proclaimed him king.", (Dictionary<string, object>)null));
					son.EncyclopediaText = new TextObject("{=!}" + son.Name + " was raised in exile across the Narrow Sea, among the sellswords of " + company.Name + ", on stories of a crown " +
						(son.IsFemale ? "her" : "his") + " blood was owed. On " + Standing.Date() + " " + (son.IsFemale ? "she" : "he") + " came to take it.", (Dictionary<string, object>)null);
				}
				catch
				{
				}
				Log.Write("exile: " + company.Name + " landed at " + seat.Name + " under " + son.Name + " with " + men + " men" + ((funder != null) ? (", paid by " + funder.Name + " (" + funds + ")") : ""));
				Ravens.Popup("The Company Comes Back", company.Name + " has landed at " + seat.Name + ".\n\n" + son.Name + " is at its head - " +
					((captain != null) ? (captain.Name + "'s ") : "the claimant's ") + "son, grown up across the sea on stories about you. " + men.ToString("N0") + " swords came off the ships" +
					((funder != null) ? (", paid for with " + funder.Name + "'s gold") : "") + ".\n\n" + seat.Name + " is his. He has been proclaimed king. The war you won is not over after all.");
			}
			catch (Exception e)
			{
				Log.Write("the landing failed: " + e);
			}
		}

		// ------------------------------------------------------------------
		// load, the court, and the console

		internal static void Load()
		{
			Host.ClearProtected();
			string id = Store.Get(PartyKey);
			if (!string.IsNullOrEmpty(id) && (State == "exiled" || State == "landed"))
			{
				Host.Protect(id);
			}
		}

		internal static string Summary()
		{
			if (!Cfg.Exile)
			{
				return null;
			}
			string state = State;
			Clan company = Company;
			if (state == "exiled" && company != null)
			{
				Hero captain = HeroOf(CaptainKey);
				Hero son = HeroOf(HeirKey);
				int left = Store.GetI(ReturnKey, 0) - CourtBehavior.Today();
				string when = (left < Cfg.DaysPerYear * 2) ? "Word from Braavos is that they are buying ships." : ((left < Cfg.DaysPerYear * 6) ? "They will come back. Not yet." : "It will be many years before they come back - if they ever do.");
				return "  " + company.Name + " waits across the Narrow Sea" + ((captain != null && captain.IsAlive) ? (" under " + captain.Name) : "") + ", " + Store.GetI(MenKey, 0).ToString("N0") + " swords" +
					((son != null && son.IsAlive) ? (", with " + son.Name + " among them") : "") + ". " + when;
			}
			if (state == "landed" && company != null)
			{
				Hero son = HeroOf(RivalKey);
				return "  " + company.Name + " has landed" + ((son != null) ? (" under " + son.Name) : "") + ".";
			}
			return null;
		}

		internal static string Now()
		{
			Hero rival;
			Clan house;
			if (State == "exiled")
			{
				return "The company is already across the sea. Use wad.exile_return to bring it back.";
			}
			if (!Rival(out rival, out house))
			{
				return "There is no claimant to send into exile - raise the bastard first (wad.bastard_rise).";
			}
			Beaten(rival, house);
			return "The claimant is beaten.";
		}

		internal static string ReturnNow()
		{
			if (State != "exiled")
			{
				return "No company waits across the sea.";
			}
			Store.SetI(ReturnKey, CourtBehavior.Today() + 1);
			return "The company lands tomorrow.";
		}
	}
}
