using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Who follows you.
	//
	// This used to be a great deal more than that. Every house in the realm
	// carried a support score that drifted year by year, a Great Council could
	// be called to argue with it, and the realm fractured on your death
	// according to the sum. All of it was arithmetic the player could not see,
	// resolved in a popup at the moment they stopped being able to do anything
	// about it - and it produced sentences like "the lords who came to your
	// councils keep the word they gave you there" for players who had never
	// held one.
	//
	// What is left is the part that was actually worth having: you name an
	// heir, you may name them against your culture's law, and the name you
	// chose is the one that ends up on the seat. The drama of a succession now
	// lives in Bastard.cs, where it is a thing that happens rather than a
	// number that was being kept.
	internal static class Succession
	{
		// ------------------------------------------------------------------
		// the realm you rule

		// Gated on the CLAN rather than on Hero.MainHero, because by the time
		// a death is settled the game has already moved the player onto the
		// heir. A sworn vassal rules nothing and has no succession of his own.
		internal static Kingdom Realm()
		{
			try
			{
				Clan mine = Clan.PlayerClan;
				if (mine == null || mine.Kingdom == null)
				{
					return null;
				}
				Kingdom k = mine.Kingdom;
				return (k.Leader != null && k.Leader.Clan == mine) ? k : null;
			}
			catch
			{
				return null;
			}
		}

		internal static bool Rules()
		{
			return Realm() != null;
		}

		// ------------------------------------------------------------------
		// who could follow you

		private static Hero _named;

		private static int _namedDay = -9999;

		internal static void Forget()
		{
			_named = null;
			_namedDay = -9999;
		}

		// Cached for the day: this walks Hero.AllAliveHeroes and then builds
		// and sorts the claimants, and the court screens ask for it often.
		internal static Hero Named()
		{
			try
			{
				int today = CourtBehavior.Today();
				if (_namedDay == today && _named != null && _named.IsAlive && _named.Clan == Clan.PlayerClan)
				{
					return _named;
				}
				_named = NamedUncached();
				_namedDay = today;
				return _named;
			}
			catch
			{
				return null;
			}
		}

		private static Hero NamedUncached()
		{
			try
			{
				string id = Store.Get("sc:heir");
				if (!string.IsNullOrEmpty(id))
				{
					Hero h = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => ((MBObjectBase)x).StringId == id);
					// Alive is not enough. A daughter you named heir who then
					// marries into another house is moved into HIS clan by the
					// game, and cannot inherit your seat from there.
					if (h != null && h.IsAlive && h.Clan == Clan.PlayerClan)
					{
						return h;
					}
					Store.Set("sc:heir", null);
				}
				List<Hero> c = Claimants();
				return (c.Count > 0) ? c[0] : null;
			}
			catch
			{
				return null;
			}
		}

		internal static void Name(Hero h)
		{
			if (h == null)
			{
				return;
			}
			Store.Set("sc:heir", ((MBObjectBase)h).StringId);
			Forget();
			Store.AddDeed(Standing.Date() + "  " + h.Name + " was named heir.");
		}

		// Your own blood, oldest first.
		internal static List<Hero> Claimants()
		{
			List<Hero> list = new List<Hero>();
			try
			{
				Hero you = Hero.MainHero;
				if (you == null || Clan.PlayerClan == null)
				{
					return list;
				}
				foreach (Hero h in Clan.PlayerClan.Heroes)
				{
					if (h == null || !h.IsAlive || h == you || h.IsChild)
					{
						continue;
					}
					// Clan.Heroes is everyone under your roof: blood, marriage
					// and hired service alike. Only the blood has a claim.
					if (!IsBlood(h, you))
					{
						continue;
					}
					// The white cloak gives up every claim.
					if (Guard.IsSworn(h))
					{
						continue;
					}
					list.Add(h);
				}
				list = list.OrderByDescending((Hero h) => h.Age).ToList();
			}
			catch
			{
			}
			return list;
		}

		// Of the blood, rather than of the household.
		//
		// Share an ancestor, or descend from one another. That admits
		// children, grandchildren, siblings, nephews, nieces, uncles, aunts and
		// cousins, and it admits nobody who merely married in or was hired.
		// There is deliberately no spouse clause: for an unrelated spouse the
		// ancestry test already says no, so a clause would only ever change the
		// answer for a spouse who IS blood - and in this setting that is a
		// sister-wife or a cousin, who has as good a claim as anyone alive.
		private static void Climb(Hero h, int depth, HashSet<Hero> into)
		{
			if (h == null || depth < 0 || !into.Add(h))
			{
				return;
			}
			Climb(h.Father, depth - 1, into);
			Climb(h.Mother, depth - 1, into);
		}

		internal static bool IsBlood(Hero h, Hero you)
		{
			try
			{
				if (h == null || you == null || h.IsPlayerCompanion)
				{
					return false;
				}
				HashSet<Hero> mine = new HashSet<Hero>();
				Climb(you, 2, mine);
				HashSet<Hero> theirs = new HashSet<Hero>();
				Climb(h, 3, theirs);
				if (theirs.Contains(you) || mine.Contains(h))
				{
					return true;
				}
				foreach (Hero a in theirs)
				{
					if (mine.Contains(a))
					{
						return true;
					}
				}
			}
			catch
			{
			}
			return false;
		}

		// ------------------------------------------------------------------
		// the answer you gave on the screen

		// The order inside the game is cruel here: the heir screen is shown,
		// the chosen hero IS seated, and then the death itself runs and hands
		// the seat to whoever scores highest on the game's own tally, which
		// undoes the answer just given. So the answer is written down as it is
		// given (from a prefix in Laws.cs) and put back afterwards.
		internal static Hero Chosen()
		{
			try
			{
				string id = Store.Get("sc:chosen");
				if (string.IsNullOrEmpty(id))
				{
					return null;
				}
				Hero h = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => ((MBObjectBase)x).StringId == id);
				// Must still be of this house. The same screen is shown on
				// retirement, with no death, so a pick can otherwise sit here
				// for years and be applied to a succession it was never about.
				if (h == null || !h.IsAlive || h.Clan != Clan.PlayerClan)
				{
					Store.Set("sc:chosen", null);
					return null;
				}
				return h;
			}
			catch
			{
				return null;
			}
		}

		internal static void Remember(Hero selectedHeir)
		{
			try
			{
				if (selectedHeir == null)
				{
					return;
				}
				Store.Set("sc:chosen", ((MBObjectBase)selectedHeir).StringId);
				Store.Set("sc:heir", ((MBObjectBase)selectedHeir).StringId);
				Forget();
				Log.Write("you chose " + selectedHeir.Name + " on the heir screen");
			}
			catch
			{
			}
		}

		// ------------------------------------------------------------------
		// putting them on the seat

		// The game assigns the new clan leader inside KillCharacterAction, some
		// distance BEFORE HeroKilledEvent fires, by scoring every adult of the
		// clan: +10 for being male, +5 for being oldest, +5 for best skills,
		// +10 for being the dead ruler's child or sibling. Nothing excludes a
		// spouse. So a husband who married in scores 20 on gender, age and
		// skill alone, and a daughter named heir scores 10 for being a
		// daughter - and he takes the clan. Because Kingdom.Leader is literally
		// RulingClan.Leader, taking the clan is taking the crown.
		//
		// We cannot get in front of it, so we correct it immediately
		// afterwards, through the game's own public action, which carries the
		// treasury, the party and the governorship with the title.
		internal static bool Install(Clan clan, Hero heir, string why)
		{
			try
			{
				if (!Cfg.EnforceHeir || clan == null || heir == null)
				{
					return false;
				}
				if (!heir.IsAlive || heir.Clan != clan || heir.IsChild)
				{
					return false;
				}
				if (clan.Leader == heir || clan.Leader == null)
				{
					// ApplyWithSelectedNewLeader moves the outgoing leader's
					// gold to the incoming one and reads clan.Leader to do it,
					// so it must not be called with the seat empty.
					return false;
				}
				Hero was = clan.Leader;
				ChangeClanLeaderAction.ApplyWithSelectedNewLeader(clan, heir);
				Store.Set("sc:chosen", null);
				Log.Write("the seat was taken back: " + heir.Name + " now leads " + clan.Name +
					" in place of " + was.Name + " (" + why + ")");
				Store.AddDeed(Standing.Date() + "  " + heir.Name + " took their father's seat.");
				return true;
			}
			catch (Exception e)
			{
				Log.Write("installing the heir on the seat failed: " + e.Message);
				return false;
			}
		}

		// Who should be holding the seat: whoever the player is now, if they
		// are of this house, and otherwise the name they gave. After the heir
		// popup the game moves the player onto their choice, so Hero.MainHero
		// is the best answer once the dust is down.
		internal static Hero ShouldLead(Clan clan)
		{
			try
			{
				if (clan == null)
				{
					return null;
				}
				Hero you = Hero.MainHero;
				if (you != null && you.IsAlive && you.Clan == clan && !you.IsChild)
				{
					return you;
				}
				Hero named = Named();
				return (named != null && named.Clan == clan) ? named : null;
			}
			catch
			{
				return null;
			}
		}

		// ------------------------------------------------------------------
		// what the court screen says about it

		internal static string Reading()
		{
			try
			{
				Hero heir = Named();
				if (heir == null)
				{
					return "no heir named. The realm is guessing, and guessing badly.";
				}
				return Laws.Lawful()
					? ("settled on " + heir.Name + ".")
					: ("on " + heir.Name + ", named against your culture's law, and every house knows it.");
			}
			catch
			{
				return "uncertain.";
			}
		}
	}
}
