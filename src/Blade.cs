using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Your house's ancestral sword, and who ends up holding it.
	//
	// This is the Blackfyre mechanism and it is the spine of the whole death
	// event now. Blackfyre was House Targaryen's Valyrian sword; Aegon IV gave
	// it to his bastard rather than his heir, and that gift is the reason the
	// rebellion had legitimacy. The sword was the argument.
	//
	// So the bequest is the decision. Name the blade, then put it in the hand
	// of one of your children - trueborn or not - and live with it. Give it to
	// your heir and it is an heirloom. Give it to the baseborn boy at the gate
	// and you have armed the claim that ends your dynasty.
	internal static class Blade
	{
		private const string HolderKey = "bl:holder";

		// Who carries it. Null while it still hangs on your own wall.
		internal static Hero HolderOf()
		{
			try
			{
				string id = Store.Get(HolderKey);
				if (string.IsNullOrEmpty(id))
				{
					return null;
				}
				Hero h = Hero.AllAliveHeroes.FirstOrDefault((Hero x) => ((MBObjectBase)x).StringId == id);
				if (h == null)
				{
					// Whoever had it is dead. It comes back to the house
					// rather than vanishing with them.
					Store.Set(HolderKey, null);
					Log.Write("the blade has come back to the house");
					return null;
				}
				return h;
			}
			catch
			{
				return null;
			}
		}

		internal static bool Given
		{
			get
			{
				return HolderOf() != null;
			}
		}

		// Everyone who could be handed it: your trueborn claimants and any
		// baseborn child who has come to your gate.
		internal static List<Hero> Candidates()
		{
			List<Hero> list = new List<Hero>();
			try
			{
				foreach (Hero h in Succession.Claimants())
				{
					if (h != null && !list.Contains(h))
					{
						list.Add(h);
					}
				}
				foreach (Kid k in Baseborn.Known())
				{
					Hero h = Baseborn.HeroOf(k);
					// Not yourself. After inheriting as an acknowledged
					// bastard you would otherwise appear in your own bequest
					// list, and the court screen would tell you the sword was
					// "carried by" you.
					if (h != null && !h.IsChild && h != Hero.MainHero && !list.Contains(h))
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

		internal static void Give(Hero to)
		{
			try
			{
				if (to == null)
				{
					return;
				}
				Store.Set(HolderKey, ((MBObjectBase)to).StringId);
				string blade = Lore.Blade();
				Store.AddDeed(Standing.Date() + "  " + blade + " was given to " + to.Name + ".");
				Log.Write("the blade " + blade + " was given to " + to.Name + " (baseborn=" + IsBaseborn(to) + ")");
				Sync();
			}
			catch (Exception e)
			{
				Log.Write("giving the blade failed: " + e.Message);
			}
		}

		internal static void TakeBack()
		{
			try
			{
				Hero was = HolderOf();
				Store.Set(HolderKey, null);
				if (was != null)
				{
					Store.AddDeed(Standing.Date() + "  " + Lore.Blade() + " was taken back from " + was.Name + ".");
					Log.Write("the blade was taken back from " + was.Name);
				}
			}
			catch
			{
			}
		}

		// Is this hero one of your baseborn children, acknowledged or not?
		internal static bool IsBaseborn(Hero h)
		{
			try
			{
				if (h == null)
				{
					return false;
				}
				foreach (Kid k in Baseborn.All())
				{
					if (Baseborn.HeroOf(k) == h)
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

		internal static Kid RecordFor(Hero h)
		{
			try
			{
				foreach (Kid k in Baseborn.All())
				{
					if (Baseborn.HeroOf(k) == h)
					{
						return k;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		// ------------------------------------------------------------------
		// what it adds up to when you die

		// The five states, in order of how badly it goes.
		internal enum Reckoning
		{
			// No baseborn child exists at all.
			None,
			// One exists and you never did anything about them.
			Ignored,
			// You gave them your name but not the sword.
			Acknowledged,
			// You gave them the sword but never your name.
			Armed,
			// Both. This is Aegon IV's mistake exactly.
			Both
		}

		// Nobody can rise against themselves.
		//
		// This is not hypothetical. Acknowledge a child, name them your heir,
		// die - and the game moves you onto them. A day later the reckoning
		// runs, finds a baseborn child of the old ruler holding the sword,
		// and it is you. Without this guard the mod would then tear the
		// player character out of their own clan, gift them one of their own
		// castles, and declare war between two kingdoms they lead.
		private static bool Impossible(Hero h)
		{
			try
			{
				if (h == null || !h.IsAlive || h.IsChild)
				{
					return true;
				}
				if (h == Hero.MainHero)
				{
					return true;
				}
				// A baseborn child in the white cloak renounced every claim.
				if (Guard.IsSworn(h))
				{
					return true;
				}
				Clan mine = Clan.PlayerClan;
				return mine != null && mine.Leader == h;
			}
			catch
			{
				return true;
			}
		}

		// Who rises, and how far. Returns null when nobody does.
		internal static Hero Claimant(out Reckoning how)
		{
			how = Reckoning.None;
			try
			{
				Hero holder = HolderOf();
				// The sword outranks everything: whoever is holding it has the
				// argument, and if that is a baseborn child of yours then this
				// is the Blackfyre case whatever else is true.
				if (holder != null && IsBaseborn(holder) && !Impossible(holder))
				{
					Kid k = RecordFor(holder);
					how = (k != null && k.Legit) ? Reckoning.Both : Reckoning.Armed;
					return holder;
				}
				// Otherwise the strongest claim among your baseborn children:
				// an acknowledged one before an ignored one, and the eldest
				// of those.
				List<Kid> known = Baseborn.Known();
				if (known.Count == 0)
				{
					return null;
				}
				Kid best = null;
				Hero bestHero = null;
				foreach (Kid k in known)
				{
					Hero h = Baseborn.HeroOf(k);
					if (Impossible(h))
					{
						continue;
					}
					if (best == null
						|| (k.Legit && !best.Legit)
						|| (k.Legit == best.Legit && bestHero != null && h.Age > bestHero.Age))
					{
						best = k;
						bestHero = h;
					}
				}
				if (bestHero == null)
				{
					return null;
				}
				how = best.Legit ? Reckoning.Acknowledged : Reckoning.Ignored;
				return bestHero;
			}
			catch (Exception e)
			{
				Log.Once("reckoning", "reading the reckoning failed: " + e.Message);
				return null;
			}
		}

		// How much of your realm goes with them.
		internal static float Share(Reckoning how)
		{
			switch (how)
			{
			case Reckoning.Ignored:
				return Cfg.ShareIgnored;
			case Reckoning.Acknowledged:
				return Cfg.ShareAcknowledged;
			case Reckoning.Armed:
				return Cfg.ShareArmed;
			case Reckoning.Both:
				return Cfg.ShareBoth;
			default:
				return Cfg.BastardShare;
			}
		}

		// A realm of their own needs a claim worth the name. An ignored child
		// with no sword takes a castle and some banners and nothing more.
		internal static bool Crowns(Reckoning how)
		{
			return how == Reckoning.Armed || how == Reckoning.Both;
		}

		internal static bool Declares(Reckoning how)
		{
			return Cfg.BastardWar && Crowns(how);
		}

		internal static string Reading(Reckoning how)
		{
			switch (how)
			{
			case Reckoning.Ignored:
				return "a child you never acknowledged and never armed";
			case Reckoning.Acknowledged:
				return "a child you gave your name to";
			case Reckoning.Armed:
				return "a child you put your ancestral sword into the hands of";
			case Reckoning.Both:
				return "a child you gave both your name and your sword";
			default:
				return "a stranger with your face";
			}
		}

		// ------------------------------------------------------------------
		// telling RoT about it

		// RoT Dynasty & Succession keeps its own per-clan ancestral blade name
		// and shows it on the clan page. It has a public writer, so when you
		// name yours we tell RoT, and the two mods name the same sword rather
		// than each insisting on its own.
		private static bool _synced;

		internal static void Sync()
		{
			try
			{
				if (Clan.PlayerClan == null)
				{
					return;
				}
				string name = Lore.Named();
				if (string.IsNullOrEmpty(name))
				{
					return;
				}
				Type t = AccessTools.TypeByName("RoTDynastyAndSuccession.Houses.HouseLoreBehavior");
				object inst = (t == null) ? null : AccessTools.Property(t, "Instance")?.GetValue(null, null);
				MethodInfo m = (inst == null) ? null : AccessTools.Method(t, "SetAncestralBladeName", (Type[])null, (Type[])null);
				if (m == null)
				{
					if (!_synced)
					{
						_synced = true;
						Log.Write("RoT's house lore was not found; the blade is ours alone");
					}
					return;
				}
				m.Invoke(inst, new object[2] { Clan.PlayerClan, name });
				Log.Write("RoT now names the blade " + name + " too");
			}
			catch (Exception e)
			{
				Log.Once("bladesync", "telling RoT about the blade failed: " + e.Message);
			}
		}

		internal static void Reset()
		{
			_synced = false;
		}
	}
}
