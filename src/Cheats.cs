using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Console commands for testing the Bastard's Banner and the lists.
	//
	// Open the console with Alt + ~ (Ctrl + ~ on some layouts) and type them
	// there. The banner is built out of years - a night, three years on the
	// road, a child who has to come of age, a ruler who has to die - and
	// every one of those can be skipped from here.
	//
	// Save first. These run the real thing: wad.bastard_rise founds a real
	// kingdom and declares a real war, and wad.die kills you.
	public static class Cheats
	{
		[CommandLineFunctionality.CommandLineArgumentFunction("cheats", "wad")]
		public static string Help(List<string> args)
		{
			return string.Join("\n", new string[]
			{
				"Wardens & Dragons test commands. Save first - these do the real thing.",
				"",
				"THE BASTARD'S BANNER",
				"  wad.bastard                 what would happen if you died today, and why",
				"  wad.child_new [count]       a baseborn child of yours comes to the gate now",
				"  wad.child_now               every child still on the road arrives today",
				"  wad.age <years> <name>      set the age of someone of your blood",
				"  wad.acknowledge <name>      give a baseborn child your name",
				"  wad.blade <name> | none     put the house blade in their hand, or take it back",
				"  wad.bastard_rise [force]    the death question, now, without dying",
				"                              force = forget the last answer first",
				"  wad.bastard_reset           forget the last answer, so the next death asks again",
				"  wad.die yes                 die now, for the real end-to-end path",
				"",
				"THE LISTS",
				"  wad.tourney_status          your tourney, its guests, and every champion",
				"  wad.tourney_here [1-3]      call a tourney in this town, free, no cooldown",
				"  wad.tourney_resolve         decide the tourney in this town (or yours) now",
				"  wad.tourney_win <n> <name>  give someone tourney wins (baseborn champions)",
				"",
				"THE KING'S JUSTICE",
				"  wad.law                     every charge, and any trial waiting",
				"  wad.charge <kind> <name>    a real charge against a lord (treason, murder,",
				"                              kinslaying, execution, tyranny)",
				"  wad.accuse_me [kind]        a lord of your realm accuses you",
				"  wad.trial                   fight the waiting trial in this town's arena",
				"",
				"THE WHITE CLOAKS",
				"  wad.kg                      the White Book, and who is away",
				"  wad.kg_swear <name>         swear anyone, skipping the vows' checks",
				"  wad.kg_knight               knight your best soldier and swear them",
				"  wad.kg_return               every knight away comes home now",
				"  wad.kg_cloak                dress every sworn knight in the white armour",
				"  wad.seven_ready             end a trial of seven's gathering now",
				"",
				"YOUR HOUSE",
				"  wad.culture list            every culture your mods define",
				"  wad.culture <name> [holdings]  your house (and holdings) take that culture",
				"  wad.style <style> | none    the style the head of your house wears",
				"",
				"Also: wad.status, wad.honour <n>, wad.dread <n>, wad.wards, wad.dragons, wad.hatch <name>"
			});
		}

		// ------------------------------------------------------------------
		// the banner

		[CommandLineFunctionality.CommandLineArgumentFunction("bastard", "wad")]
		public static string BastardStatus(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("The Bastard's Banner");
			string why = Bastard.WhyNot();
			sb.AppendLine((why == null) ? "  IF YOU DIED TODAY: you would be asked." : ("  IF YOU DIED TODAY: nothing - " + why + "."));
			sb.AppendLine("  answer on record: " + (Store.Get("bs:risen") ?? "none"));
			sb.AppendLine("  your house rules a realm: " + Succession.Rules() + ";  towns and castles: " + Bastard.Fiefs().Count + " (need " + Cfg.BastardMinFiefs + ")");

			Blade.Reckoning how;
			Hero him = Blade.Claimant(out how);
			if (him != null)
			{
				bool champion = Tourney.Crowned(him);
				float share = Math.Min(0.9f, Blade.Share(how) + Tourney.ShareBonus(him));
				sb.AppendLine("  CLAIMANT: " + him.Name + ", " + (int)him.Age + " - " + Blade.Reading(how));
				sb.AppendLine("    takes " + (share * 100f).ToString("0") + "% of your sworn houses" +
					((Tourney.ShareBonus(him) > 0f) ? (" (incl. +" + (Tourney.ShareBonus(him) * 100f).ToString("0") + "% for " + Tourney.Wins(him) + " tourney win(s))") : ""));
				bool crowns = Blade.Crowns(how) || champion;
				sb.AppendLine("    founds a kingdom: " + crowns + ((champion && !Blade.Crowns(how)) ? " (the crowds' champion)" : "") +
					";  declares war: " + (crowns && Cfg.BastardWar));
			}
			else
			{
				sb.AppendLine("  CLAIMANT: none of your children - " + (Cfg.BastardStranger ? "a stranger with your face would come instead" : "and the stranger is off"));
			}

			Hero holder = Blade.HolderOf();
			sb.AppendLine("  the blade: " + Lore.Blade() + ((holder != null) ? (", carried by " + holder.Name) : ", on your wall"));

			List<Kid> kids = Baseborn.All();
			sb.AppendLine("  baseborn children: " + kids.Count + " of " + Cfg.BaseMax);
			int today = CourtBehavior.Today();
			foreach (Kid k in kids)
			{
				Hero h = Baseborn.HeroOf(k);
				if (k.Known && h != null)
				{
					string legit;
					Baseborn.CanLegitimise(k, out legit);
					sb.AppendLine("    " + h.Name + ", " + (int)h.Age + (k.Legit ? ", acknowledged" : ", not acknowledged") +
						(h.IsChild ? ", STILL A CHILD (wad.age)" : "") + ((Tourney.Wins(h) > 0) ? (", " + Tourney.Wins(h) + " tourney win(s)") : "") +
						((!k.Legit && legit != null) ? (" - cannot be acknowledged: " + legit) : ""));
				}
				else if (!k.Known)
				{
					int due = k.Night + Cfg.BaseYearsUntil * Math.Max(1, Cfg.DaysPerYear) - today;
					sb.AppendLine("    record " + k.Id + ": on the road, arrives in " + Math.Max(0, due) + " days (wad.child_now)");
				}
			}
			string s = sb.ToString();
			Log.Write(s);
			return s;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("child_new", "wad")]
		public static string ChildNew(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (!Cfg.Baseborn)
			{
				return "baseborn_children is off in the config.";
			}
			int count = 1;
			if (args != null && args.Count > 0)
			{
				int.TryParse(args[0], out count);
			}
			count = Math.Max(1, Math.Min(4, count));
			Settlement where = Place();
			if (where == null)
			{
				return "There is no town to have had a child in.";
			}
			int made = 0;
			for (int i = 0; i < count; i++)
			{
				if (!Baseborn.HasRoom())
				{
					break;
				}
				if (Baseborn.Conceive(null, where) != null)
				{
					made++;
				}
			}
			if (made == 0)
			{
				return "No room: you already have " + Baseborn.All().Count + " of baseborn_max=" + Cfg.BaseMax + ".";
			}
			int came = Baseborn.HurryAll();
			return made + " night(s) at " + where.Name + "; " + came + " child(ren) came to the gate. They may be under 18 - wad.age 20 <name> to grow them up.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("child_now", "wad")]
		public static string ChildNow(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			int came = Baseborn.HurryAll();
			return (came == 0) ? "Nobody is on the road. wad.child_new makes one." : (came + " child(ren) came to the gate.");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("age", "wad")]
		public static string Age(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			int years;
			if (args == null || args.Count < 2 || !int.TryParse(args[0], out years))
			{
				return "Usage: wad.age <years> <part of a name>   - anyone of your blood, or a baseborn child";
			}
			Hero h = Find(args.Skip(1));
			if (h == null)
			{
				return "Nobody of your blood matches '" + string.Join(" ", args.Skip(1).ToArray()) + "'.";
			}
			years = Math.Max(1, Math.Min(90, years));
			h.SetBirthDay(CampaignTime.YearsFromNow(-years));
			return h.Name + " is now " + (int)h.Age + (h.IsChild ? " - still a child in the game's eyes." : ".");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("acknowledge", "wad")]
		public static string Acknowledge(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			Hero h = Find(args);
			Kid k = (h != null) ? Blade.RecordFor(h) : null;
			if (k == null)
			{
				return "No baseborn child of yours matches that. wad.bastard lists them.";
			}
			string why;
			if (!Baseborn.CanLegitimise(k, out why))
			{
				return "Not possible: " + why + ".";
			}
			Baseborn.Legitimise(k);
			return h.Name + " is written into the book.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("blade", "wad")]
		public static string GiveBlade(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (args != null && args.Count == 1 && args[0].ToLowerInvariant() == "none")
			{
				Blade.TakeBack();
				return Lore.Blade() + " hangs on your wall again.";
			}
			Hero h = Find(args);
			if (h == null || !Blade.Candidates().Contains(h))
			{
				return "Usage: wad.blade <name> | none.  Can carry it: " +
					string.Join(", ", Blade.Candidates().Select((Hero x) => x.Name.ToString()).ToArray());
			}
			Blade.Give(h);
			return h.Name + " carries " + Lore.Blade() + ".";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("bastard_rise", "wad")]
		public static string Rise(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (args != null && args.Count > 0 && args[0].ToLowerInvariant() == "force")
			{
				Bastard.ForgetAnswer();
			}
			string why = Bastard.WhyNot();
			if (why != null)
			{
				return "It would not be asked: " + why + "." + (why.StartsWith("it has already") ? " Use wad.bastard_rise force." : "");
			}
			Bastard.OfferNow();
			return "The question is on screen. Close the console to answer it. You are, in fact, alive.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("bastard_reset", "wad")]
		public static string Reset(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			Bastard.ForgetAnswer();
			return "Forgotten. The next death - or wad.bastard_rise - asks again. Anything already raised stays raised.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("die", "wad")]
		public static string Die(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (args == null || args.Count == 0 || args[0].ToLowerInvariant() != "yes")
			{
				return "This kills your character for real: the heir screen, the succession, then the banner the next day. Save first, then type: wad.die yes";
			}
			Hero you = Hero.MainHero;
			Log.Write("console: " + you.Name + " dies on purpose");
			KillCharacterAction.ApplyByMurder(you, null, true);
			return "Done. Close the console. The banner is asked the day after the heir is settled.";
		}

		// ------------------------------------------------------------------
		// the lists

		[CommandLineFunctionality.CommandLineArgumentFunction("tourney_status", "wad")]
		public static string TourneyStatus(List<string> args)
		{
			return (Campaign.Current == null) ? "Load a campaign first." : Tourney.Status();
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("tourney_here", "wad")]
		public static string TourneyHere(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			Settlement here = Settlement.CurrentSettlement;
			if (here == null || !here.IsTown || here.Town == null)
			{
				return "Stand in a town first.";
			}
			if (Tourney.Hosted != null)
			{
				return "Your tourney at " + Tourney.Hosted.Name + " is still running. wad.tourney_resolve, then try again.";
			}
			int tier = 2;
			if (args != null && args.Count > 0)
			{
				int.TryParse(args[0], out tier);
			}
			// Everyone of your blood who is free rides, so the lists have your
			// own in them to win, lose or die.
			List<Hero> riders = Tourney.HouseRiders();
			return Tourney.Begin(here.Town, tier, riders, true)
				? ("A tourney is called at " + here.Name + ", free. " + riders.Count + " of your blood will ride. Visit the arena to ride, or wad.tourney_resolve to decide it now.")
				: "It could not be called - see the log.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("tourney_resolve", "wad")]
		public static string TourneyResolve(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			Settlement here = Settlement.CurrentSettlement;
			Town town = (here != null && here.IsTown && Campaign.Current.TournamentManager.GetTournamentGame(here.Town) != null)
				? here.Town
				: Tourney.Hosted;
			return (town == null) ? "No tourney here, and none of yours running." : (Tourney.ResolveNow(town) + " Close the console to see how it went.");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("tourney_win", "wad")]
		public static string TourneyWin(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			int n;
			if (args == null || args.Count < 2 || !int.TryParse(args[0], out n))
			{
				return "Usage: wad.tourney_win <count> <part of a name>";
			}
			Hero h = Find(args.Skip(1));
			if (h == null)
			{
				return "Nobody of your blood matches that.";
			}
			Tourney.AddWin(h, n);
			return h.Name + " now has " + Tourney.Wins(h) + " tourney win(s)" + (Tourney.Crowned(h) ? " - enough that the crowds would crown them." : ".");
		}

		// ------------------------------------------------------------------
		// the law

		[CommandLineFunctionality.CommandLineArgumentFunction("law", "wad")]
		public static string LawStatus(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			return Law.Summary() + (Law.TrialWaiting ? "\nA trial is waiting - wad.trial in a town to fight it." : "");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("charge", "wad")]
		public static string ChargeLord(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (args == null || args.Count < 2)
			{
				return "Usage: wad.charge <treason|murder|kinslaying|execution|tyranny> <part of a lord's name>   - a real charge, for testing";
			}
			string kind = args[0].ToLowerInvariant();
			string part = string.Join(" ", args.Skip(1).ToArray()).ToLowerInvariant();
			Hero h = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x.IsLord && x != Hero.MainHero && x.Name.ToString().ToLowerInvariant() == part)
				?? Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x.IsLord && x != Hero.MainHero && x.Name.ToString().ToLowerInvariant().Contains(part));
			if (h == null)
			{
				return "No living lord matches '" + part + "'.";
			}
			Charge c = Law.Record(kind, h, Hero.MainHero, null, false);
			if (c == null)
			{
				return "Not recorded - law_enabled is off, or " + h.Name + " already faces that charge.";
			}
			string why;
			return "Charged: " + Law.Describe(c) + ". " + (Law.CanJudge(c, out why) ? "Court -> The King's Justice -> Hear a charge." : ("You cannot hear it: " + why + "."));
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("accuse_me", "wad")]
		public static string AccuseMe(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (Clan.PlayerClan == null || Clan.PlayerClan.Kingdom == null)
			{
				return "You kneel to no crown and rule none - nobody has a court over you.";
			}
			string kind = (args != null && args.Count > 0) ? args[0].ToLowerInvariant() : Law.Treason;
			Hero accuser = Clan.PlayerClan.Kingdom.Clans.Where((Clan c) => c != Clan.PlayerClan && c.Leader != null && c.Leader.IsAlive)
				.Select((Clan c) => c.Leader).OrderBy((Hero x) => x.GetRelationWithPlayer()).FirstOrDefault();
			if (accuser == null)
			{
				return "There is nobody in your realm to accuse you.";
			}
			Charge ch = Law.Record(kind, Hero.MainHero, accuser, null, false);
			return (ch == null) ? "Not recorded - you may already face that charge." : (accuser.Name + " accuses you of " + Law.KindName(kind).ToLowerInvariant() + ". Close the console to answer.");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("trial", "wad")]
		public static string TrialNow(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			string why;
			return Law.Fight(out why) ? "Into the arena." : ("Not now: " + why + ".");
		}

		// ------------------------------------------------------------------
		// the white cloaks

		[CommandLineFunctionality.CommandLineArgumentFunction("kg", "wad")]
		public static string KgBook(List<string> args)
		{
			return (Campaign.Current == null) ? "Load a campaign first." : Guard.Book();
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("kg_swear", "wad")]
		public static string KgSwear(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			string part = string.Join(" ", (args ?? new List<string>()).ToArray()).Trim().ToLowerInvariant();
			if (part.Length == 0)
			{
				return "Usage: wad.kg_swear <part of a name>   - swears any living hero, skipping the vows' checks";
			}
			Hero h = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != Hero.MainHero && x.Name.ToString().ToLowerInvariant() == part)
				?? Hero.AllAliveHeroes.FirstOrDefault((Hero x) => x != Hero.MainHero && x.Name.ToString().ToLowerInvariant().Contains(part));
			if (h == null)
			{
				return "Nobody matches '" + part + "'.";
			}
			string why;
			return Guard.Swear(h, (h.CompanionOf == Clan.PlayerClan) ? "champion" : "noble", true, out why) ? (h.Name + " is sworn.") : ("Not sworn: " + why + ".");
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("kg_knight", "wad")]
		public static string KgKnight(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			CharacterObject best = Guard.Soldiers().FirstOrDefault();
			if (best == null)
			{
				return "No soldier in your party is tier " + Cfg.KgCommonerTier + " or better.";
			}
			Hero h = Guard.KnightSoldier(best);
			string why;
			return (h != null && Guard.Swear(h, "commoner", true, out why)) ? (h.Name + ", once " + best.Name + ", is knighted and sworn.") : "It did not take - see the log.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("kg_cloak", "wad")]
		public static string KgCloak(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			CharacterObject white = Guard.Armour();
			if (white == null)
			{
				return "No Kingsguard troop was found to take the white armour from.";
			}
			int n = 0;
			foreach (Knight k in Guard.All())
			{
				if (Guard.Dress(Guard.HeroOf(k)))
				{
					n++;
				}
			}
			return n + " knight(s) dressed in the kit of " + white.Name + ".";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("seven_ready", "wad")]
		public static string SevenReady(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			return Law.OpenNow() ? "The lists are raised. Close the console to see who stands with you." : "No trial of seven is gathering.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("kg_return", "wad")]
		public static string KgReturn(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			int n = 0;
			foreach (Knight k in Guard.All().Where((Knight x) => x.State == "away").ToList())
			{
				Guard.Return(k);
				n++;
			}
			return (n == 0) ? "Nobody is away." : (n + " knight(s) came home. Close the console to hear how it went.");
		}

		// ------------------------------------------------------------------
		// your house

		[CommandLineFunctionality.CommandLineArgumentFunction("culture", "wad")]
		public static string SetCulture(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			if (args == null || args.Count == 0 || args[0].ToLowerInvariant() == "list")
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine("Usage: wad.culture <id or name> [holdings]   - e.g. wad.culture valyrian holdings");
				sb.AppendLine("Your house is " + ((Clan.PlayerClan != null && Clan.PlayerClan.Culture != null) ? Clan.PlayerClan.Culture.Name.ToString() : "?") + ". Cultures your mods define:");
				foreach (CultureObject c in Heritage.Choices())
				{
					sb.AppendLine("  " + ((MBObjectBase)c).StringId + "  -  " + c.Name + (c.IsMainCulture ? "" : "  (minor)") + (Heritage.Valyrian(c) ? "  <- Valyrian" : ""));
				}
				return sb.ToString();
			}
			bool holdings = args.Count > 1 && args[args.Count - 1].ToLowerInvariant() == "holdings";
			string what = string.Join(" ", (holdings ? args.Take(args.Count - 1) : args).ToArray());
			CultureObject found = Heritage.Find(what);
			return (found == null) ? ("No culture matches '" + what + "'. wad.culture list shows them.") : Heritage.Apply(found, holdings);
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("style", "wad")]
		public static string SetStyle(List<string> args)
		{
			if (Campaign.Current == null)
			{
				return "Load a campaign first.";
			}
			string style = string.Join(" ", (args ?? new List<string>()).ToArray()).Replace("{", "").Replace("}", "").Trim();
			if (style.Length == 0)
			{
				return "Usage: wad.style <style> | none.  Now: " + (Styles.Of(Clan.PlayerClan) ?? "none");
			}
			Styles.Set(Clan.PlayerClan, (style.ToLowerInvariant() == "none") ? null : style);
			return "Your house's style: " + (Styles.Of(Clan.PlayerClan) ?? "none") + ".";
		}

		// ------------------------------------------------------------------
		// bits

		// Your blood and your baseborn children, by part of a name. An exact
		// match wins over a partial one, so "Jon" does not pick "Jonos".
		private static Hero Find(IEnumerable<string> words)
		{
			try
			{
				string part = string.Join(" ", (words ?? new string[0]).ToArray()).Trim().ToLowerInvariant();
				if (part.Length == 0)
				{
					return null;
				}
				List<Hero> pool = new List<Hero>();
				if (Clan.PlayerClan != null)
				{
					pool.AddRange(Clan.PlayerClan.Heroes.Where((Hero h) => h != null && h.IsAlive));
				}
				foreach (Kid k in Baseborn.Known())
				{
					Hero h = Baseborn.HeroOf(k);
					if (h != null && !pool.Contains(h))
					{
						pool.Add(h);
					}
				}
				return pool.FirstOrDefault((Hero h) => h.Name.ToString().ToLowerInvariant() == part || (h.FirstName != null && h.FirstName.ToString().ToLowerInvariant() == part))
					?? pool.FirstOrDefault((Hero h) => h.Name.ToString().ToLowerInvariant().Contains(part));
			}
			catch
			{
				return null;
			}
		}

		// Where a console child was got: here if this is a town or a village,
		// otherwise your house's seat, otherwise any town at all.
		private static Settlement Place()
		{
			Settlement here = Settlement.CurrentSettlement;
			if (here != null && (here.IsTown || here.IsVillage))
			{
				return here;
			}
			Settlement home = (Clan.PlayerClan != null) ? Clan.PlayerClan.HomeSettlement : null;
			if (home != null && (home.IsTown || home.IsVillage))
			{
				return home;
			}
			return Settlement.All.FirstOrDefault((Settlement s) => s.IsTown);
		}
	}
}
