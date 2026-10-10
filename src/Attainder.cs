using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Attainder: a whole house condemned.
	//
	// A house that tried to wrong you or kill you - a barred-doors feast you
	// cut your way out of, a treason, a murder of your blood - can be attainted
	// from the King's Justice. It is thrown out of your realm and is at war
	// with you. And you may give the order every king in the songs gives
	// sooner or later: every lord of that house your houses take is put to
	// death. Survive a massacre and the host's house is attainted at once.
	internal static class Attainder
	{
		private const string Prefix = "at:";   // clan -> day|reason|execute|heads

		internal sealed class Rec
		{
			internal string House = "";
			internal int Day;
			internal string Reason = "";
			internal bool Execute;
			internal int Heads;

			internal string Pack()
			{
				return string.Join("|", new string[4] { Day.ToString(), (Reason ?? "").Replace("|", "/"), Execute ? "1" : "0", Heads.ToString() });
			}

			internal static Rec Unpack(string house, string s)
			{
				string[] p = (s ?? "").Split('|');
				if (p.Length < 4)
				{
					return null;
				}
				Rec r = new Rec();
				r.House = house;
				int.TryParse(p[0], out r.Day);
				r.Reason = p[1];
				r.Execute = p[2] == "1";
				int.TryParse(p[3], out r.Heads);
				return r;
			}

			internal Clan Clan
			{
				get
				{
					return Sworn.Find(House);
				}
			}
		}

		// Captives waiting for the hourly tick: hero id -> captor hero id.
		private static readonly List<KeyValuePair<string, string>> _queue = new List<KeyValuePair<string, string>>();

		internal static List<Rec> All()
		{
			List<Rec> list = new List<Rec>();
			foreach (string key in Store.Keys(Prefix))
			{
				Rec r = Rec.Unpack(key.Substring(Prefix.Length), Store.Get(key));
				if (r != null)
				{
					list.Add(r);
				}
			}
			return list;
		}

		internal static Rec Of(Clan c)
		{
			return (c == null) ? null : Rec.Unpack(((MBObjectBase)c).StringId, Store.Get(Prefix + ((MBObjectBase)c).StringId));
		}

		private static void Save(Rec r)
		{
			Store.Set(Prefix + r.House, r.Pack());
		}

		// ------------------------------------------------------------------
		// who has wronged you

		// Houses with a wrong against you or your blood on the King's record,
		// with what they did.
		internal static List<KeyValuePair<Clan, string>> Wrongdoers()
		{
			List<KeyValuePair<Clan, string>> list = new List<KeyValuePair<Clan, string>>();
			HashSet<string> seen = new HashSet<string>();
			Hero me = Hero.MainHero;
			try
			{
				foreach (Charge c in Law.All())
				{
					if (c.Kind != Law.GuestRight && c.Kind != Law.Treason && c.Kind != Law.Murder && c.Kind != Law.Kinslaying)
					{
						continue;
					}
					Hero accused = Law.Find(c.Accused);
					if (accused == null || accused == me || accused.Clan == null || accused.Clan == Clan.PlayerClan)
					{
						continue;
					}
					Hero accuser = Law.Find(c.Accuser);
					Hero victim = Law.Find(c.Victim);
					bool mine = accuser == me || victim == me || (victim != null && Succession.IsBlood(victim, me)) || (c.Kind == Law.Treason);
					if (!mine)
					{
						continue;
					}
					string id = ((MBObjectBase)accused.Clan).StringId;
					if (seen.Add(id) && !accused.Clan.IsEliminated && Of(accused.Clan) == null)
					{
						list.Add(new KeyValuePair<Clan, string>(accused.Clan, Law.KindName(c.Kind) + " - " + accused.Name));
					}
				}
			}
			catch (Exception e)
			{
				Log.Write("attainder: the record could not be read: " + e.Message);
			}
			return list;
		}

		// ------------------------------------------------------------------
		// attainting

		internal static void Declare(Clan house, string reason, bool quiet = false)
		{
			if (!Cfg.Attainder || house == null || house.IsEliminated || house == Clan.PlayerClan)
			{
				return;
			}
			if (Of(house) != null)
			{
				Log.Write("attainder: " + house.Name + " is already attainted");
				return;
			}
			Rec r = new Rec();
			r.House = ((MBObjectBase)house).StringId;
			r.Day = CourtBehavior.Today();
			r.Reason = reason ?? "";
			Save(r);
			string what;
			try
			{
				what = Enemy(house);
			}
			catch (Exception e)
			{
				what = "the war could not be declared (" + e.Message + ")";
				Log.Write("attainder: " + e);
			}
			Standing.Change(0, Cfg.AttainderDread, "Attainted " + house.Name);
			Store.AddDeed(Standing.Date() + "  " + house.Name + " was attainted: " + reason + ".");
			Knighting.Append(house, "On " + Standing.Date() + " it was attainted by " + Hero.MainHero.Name + ": " + reason + ".");
			Log.Write("attainder: " + house.Name + " attainted - " + reason + "; " + what);
			if (!quiet)
			{
				Ravens.Popup("Attainted", house.Name + " is attainted: " + reason + ".\n\n" + Capitalise(what) + ".\n\nIn the King's Justice you may order every lord of " + house.Name + " put to death when your houses take them.");
			}
		}

		private static string Capitalise(string s)
		{
			return string.IsNullOrEmpty(s) ? s : (char.ToUpper(s[0]) + s.Substring(1));
		}

		// Make the house your enemy, as far as the game allows.
		private static string Enemy(Clan house)
		{
			Clan me = Clan.PlayerClan;
			Kingdom mine = me.Kingdom;
			bool rule = mine != null && mine.RulingClan == me;
			IFaction ours = me.MapFaction;
			if (house.Kingdom != null && house.Kingdom == mine)
			{
				if (!rule)
				{
					return "it is sworn to your own liege, so you cannot cast it out - but your house holds the attainder";
				}
				Store.Set("lw:exiled:" + ((MBObjectBase)house).StringId, "1");
				if (house.IsUnderMercenaryService)
				{
					ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(house, false);
				}
				else
				{
					ChangeKingdomAction.ApplyByLeaveKingdom(house, false);
				}
				if (!FactionManager.IsAtWarAgainstFaction(house, ours))
				{
					DeclareWarAction.ApplyByDefault(house, ours);
				}
				return "it is cast out of " + mine.Name + " and at war with it";
			}
			if (house.Kingdom == null)
			{
				if (!FactionManager.IsAtWarAgainstFaction(house, ours))
				{
					DeclareWarAction.ApplyByDefault(house, ours);
				}
				return "it is at war with " + ours.Name;
			}
			return "it is sworn to " + house.Kingdom.Name + ", and you cannot make war on one house of another crown - but whenever your houses take its lords, the attainder holds";
		}

		internal static void SetOrder(Rec r, bool execute)
		{
			r.Execute = execute;
			Save(r);
			Clan c = r.Clan;
			Log.Write("attainder: order for " + ((c != null) ? c.Name.ToString() : r.House) + " - " + (execute ? "every lord taken is put to death" : "rescinded"));
			if (execute)
			{
				Sweep(r);
			}
		}

		internal static void Pardon(Rec r)
		{
			Store.Set(Prefix + r.House, null);
			Store.Set("lw:exiled:" + r.House, null);
			Clan c = r.Clan;
			Log.Write("attainder: " + ((c != null) ? c.Name.ToString() : r.House) + " pardoned");
			if (c != null)
			{
				Store.AddDeed(Standing.Date() + "  " + c.Name + " was pardoned.");
				Knighting.Append(c, "On " + Standing.Date() + " its attainder was lifted.");
			}
		}

		// ------------------------------------------------------------------
		// the order

		// Is this captor one of your houses?
		private static bool Ours(IFaction f, Clan clan)
		{
			Clan me = Clan.PlayerClan;
			if (clan == me)
			{
				return true;
			}
			Kingdom mine = me.Kingdom;
			return mine != null && mine.RulingClan == me && clan != null && clan.Kingdom == mine;
		}

		private static bool Spared(Hero h)
		{
			return h == null || !h.IsAlive || h.IsChild || h == Hero.MainHero || Succession.IsBlood(h, Hero.MainHero);
		}

		// Everyone of the house already in your houses' hands.
		private static void Sweep(Rec r)
		{
			Clan house = r.Clan;
			if (house == null)
			{
				return;
			}
			int n = 0;
			foreach (Hero h in house.Heroes.Where((Hero x) => x != null && x.IsPrisoner && !Spared(x)).ToList())
			{
				PartyBase held = h.PartyBelongedToAsPrisoner;
				Clan captor = null;
				Hero by = null;
				if (held != null)
				{
					if (held.IsMobile && held.MobileParty != null)
					{
						captor = held.MobileParty.ActualClan;
						by = held.MobileParty.LeaderHero;
					}
					else if (held.IsSettlement && held.Settlement != null)
					{
						captor = held.Settlement.OwnerClan;
						by = (captor != null) ? captor.Leader : null;
					}
				}
				if (captor != null && Ours(null, captor))
				{
					_queue.Add(new KeyValuePair<string, string>(((MBObjectBase)h).StringId, (by != null) ? ((MBObjectBase)by).StringId : ""));
					n++;
				}
			}
			Log.Write("attainder: " + n + " of " + house.Name + " already held by your houses are sent to the block");
		}

		internal static void OnPrisonerTaken(PartyBase captor, Hero prisoner)
		{
			try
			{
				if (!Cfg.Attainder || !Store.Initialized || prisoner == null || prisoner.Clan == null || captor == null)
				{
					return;
				}
				Rec r = Of(prisoner.Clan);
				if (r == null || !r.Execute || Spared(prisoner))
				{
					return;
				}
				Clan by = (captor.IsMobile && captor.MobileParty != null) ? captor.MobileParty.ActualClan : (captor.IsSettlement && captor.Settlement != null ? captor.Settlement.OwnerClan : null);
				if (!Ours(null, by))
				{
					return;
				}
				Hero who = (captor.IsMobile && captor.MobileParty != null) ? captor.MobileParty.LeaderHero : ((by != null) ? by.Leader : null);
				_queue.Add(new KeyValuePair<string, string>(((MBObjectBase)prisoner).StringId, (who != null) ? ((MBObjectBase)who).StringId : ""));
			}
			catch (Exception e)
			{
				Log.Once("attaintaken", "attainder: a capture could not be read: " + e.Message);
			}
		}

		// Hourly: the block.
		internal static void Hourly()
		{
			if (_queue.Count == 0)
			{
				return;
			}
			List<KeyValuePair<string, string>> due = _queue.ToList();
			_queue.Clear();
			foreach (KeyValuePair<string, string> q in due)
			{
				try
				{
					Hero h = Law.Find(q.Key);
					if (h == null || !h.IsPrisoner || Spared(h))
					{
						continue;
					}
					Rec r = Of(h.Clan);
					if (r == null || !r.Execute)
					{
						continue;
					}
					Hero by = Law.Find(q.Value);
					if (by == null || !by.IsAlive)
					{
						by = Hero.MainHero;
					}
					Law.Quiet = true;
					try
					{
						KillCharacterAction.ApplyByExecution(h, by, true, true);
					}
					finally
					{
						Law.Quiet = false;
					}
					r.Heads++;
					Save(r);
					Standing.Change(-Cfg.AttainderExecuteHonour, Cfg.AttainderExecuteDread, "Put " + h.Name + " of the attainted " + ((h.Clan != null) ? h.Clan.Name.ToString() : "house") + " to death");
					Log.Write("attainder: " + h.Name + " of " + ((h.Clan != null) ? h.Clan.Name.ToString() : "?") + " put to death by " + by.Name + " (" + r.Heads + " so far)");
					Flow.Notify(h.Name + " of the attainted " + ((h.Clan != null) ? h.Clan.Name.ToString() : "house") + " has been put to death by " + by.Name + ".");
				}
				catch (Exception e)
				{
					Log.Write("attainder: an execution could not be carried out: " + e.Message);
				}
			}
		}

		internal static void Daily()
		{
			try
			{
				if (!Cfg.Attainder || !Store.Initialized)
				{
					return;
				}
				foreach (Rec r in All())
				{
					Clan c = r.Clan;
					if (c == null || c.IsEliminated || !c.Heroes.Any((Hero h) => h.IsAlive))
					{
						Store.Set(Prefix + r.House, null);
						string name = (c != null) ? c.Name.ToString() : "An attainted house";
						Store.AddDeed(Standing.Date() + "  " + name + " is no more.");
						Log.Write("attainder: " + name + " is no more (" + r.Heads + " put to death)");
						Ravens.Popup("A House Ended", name + " is no more." + ((r.Heads > 0) ? (" " + r.Heads + " of its lords went to the block.") : ""));
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("attaintdaily", "attainder daily failed: " + e.Message);
			}
		}

		internal static string Report()
		{
			List<Rec> all = All();
			if (all.Count == 0)
			{
				return "No house is attainted.";
			}
			return string.Join("\n", all.Select((Rec r) => ((r.Clan != null) ? r.Clan.Name.ToString() : r.House) + " - " + r.Reason + "; " + (r.Execute ? "every lord taken is put to death" : "no order given") + "; " + r.Heads + " put to death"));
		}

		internal static string Force(string name)
		{
			Clan c = Clan.All.FirstOrDefault((Clan x) => x != null && !x.IsEliminated && x != Clan.PlayerClan && !string.IsNullOrEmpty(name) && x.Name.ToString().ToLowerInvariant().Contains(name.ToLowerInvariant()));
			if (c == null)
			{
				return "No house by that name.";
			}
			Declare(c, "by your word, for testing");
			return c.Name + " is attainted. See the log.";
		}
	}
}
