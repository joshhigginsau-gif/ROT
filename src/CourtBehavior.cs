using System;
using System.Linq;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace WardensAndDragons
{
public class CourtBehavior : CampaignBehaviorBase
{
	public override void RegisterEvents()
	{
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)delegate
		{
			Store.ResetForNewCampaign();
		});
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener((object)this, (Action<CampaignGameStarter>)OnSessionLaunched);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener((object)this, (Action)OnDailyTick);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener((object)this, (Action<Hero, Hero, KillCharacterAction.KillCharacterActionDetail, bool>)OnHeroKilled);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener((object)this, (Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>)OnOwnerChanged);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener((object)this, (Action)delegate
		{
			if (Store.Initialized)
			{
				Dragons.Weekly();
				Wardship.Weekly();
			}
		});
		CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener((object)this, (Action<Hero, List<Hero>, int>)delegate(Hero mother, List<Hero> kids, int stillborn)
		{
			Dragons.OnBirth(kids);
		});
		CampaignEvents.HeroComesOfAgeEvent.AddNonSerializedListener((object)this, (Action<Hero>)Dragons.OnComesOfAge);
		// The lists. The game runs the tournament; we read what happened in it.
		CampaignEvents.TournamentFinished.AddNonSerializedListener((object)this, (Action<CharacterObject, MBReadOnlyList<CharacterObject>, Town, ItemObject>)Tourney.OnFinished);
		CampaignEvents.OnPlayerJoinedTournamentEvent.AddNonSerializedListener((object)this, (Action<Town, bool>)Tourney.OnJoined);
		CampaignEvents.PlayerEliminatedFromTournament.AddNonSerializedListener((object)this, (Action<int, Town>)Tourney.OnEliminated);
		CampaignEvents.TournamentCancelled.AddNonSerializedListener((object)this, (Action<Town>)Tourney.OnCancelled);
		// And settle it once the arena is behind us: the town menu opening
		// after the mission is the first safe moment, and the hourly tick
		// catches the ones decided on the map without the player.
		CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, (Action<MenuCallbackArgs>)delegate
		{
			Tourney.Settle();
		});
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, (Action)Tourney.Settle);
		// The King's Justice: houses that walk out are traitors, and a trial
		// fought in the arena is judged once the arena is behind us.
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener((object)this, (Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>)Law.OnClanChangedKingdom);
		CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, (Action<MenuCallbackArgs>)delegate
		{
			Law.Settle();
		});
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, (Action)Law.Settle);
		// The white cloaks walk in with you.
		CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener((object)this, (Action)Guard.Bodyguards);
		// The small council at the table in your hall.
		CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener((object)this, (Action)Council.SeatThem);
		// The ravens: a feast that became a fight is counted off the hall.
		CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, (Action<MenuCallbackArgs>)delegate
		{
			Ravens.Settle();
		});
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, (Action)Ravens.Settle);
		// Parley at the walls: single combat is settled off the field, and a
		// truce sworn after losing one is remembered.
		CampaignEvents.GameMenuOpened.AddNonSerializedListener((object)this, (Action<MenuCallbackArgs>)delegate
		{
			Parley.Settle();
		});
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, (Action)Parley.Settle);
		CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener((object)this, (Action<TaleWorlds.CampaignSystem.Siege.SiegeEvent>)Parley.OnSiegeStarted);
		// The generals' war: ambushes and screens act by the hour, and riders
		// who fought a host may have met its scorpions.
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, (Action)Host.Hourly);
		// Attainder: the order is carried out off the capture, on the hour.
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener((object)this, (Action<TaleWorlds.CampaignSystem.Party.PartyBase, Hero>)Attainder.OnPrisonerTaken);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener((object)this, (Action)Attainder.Hourly);
		// Sworn houses follow their warden from realm to realm.
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener((object)this, (Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>)Sworn.OnClanChangedKingdom);
		CampaignEvents.MapEventEnded.AddNonSerializedListener((object)this, (Action<TaleWorlds.CampaignSystem.MapEvents.MapEvent>)Scorpions.OnMapEventEnded);
	}

	public override void SyncData(IDataStore ds)
	{
		try
		{
			int num = 1;
			ds.SyncData<int>("wad_schema", ref num);
			Store.SavedSchema = num;
			ds.SyncData<bool>("wad_initialized", ref Store.Initialized);
			ds.SyncData<int>("wad_honour", ref Store.Honour);
			ds.SyncData<int>("wad_dread", ref Store.Dread);
			ds.SyncData<int>("wad_honour_cap", ref Store.HonourCap);
			ds.SyncData<int>("wad_last_drift", ref Store.LastDriftDay);
			string s = Store.PackLedger();
			ds.SyncData<string>("wad_ledger", ref s);
			string s2 = Store.PackKv();
			ds.SyncData<string>("wad_kv", ref s2);
			if (ds.IsLoading)
			{
				Store.UnpackLedger(s);
				Store.UnpackKv(s2);
				if (Store.HonourCap <= 0)
				{
					Store.HonourCap = 100;
				}
				Log.Write("loaded: schema " + num + ", Honour " + Store.Honour + ", Dread " + Store.Dread + ", " + Store.Ledger.Count + " deed(s) in the ledger");
			}
		}
		catch (Exception ex)
		{
			Log.Write("SyncData failed: " + ex);
		}
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Expected O, but got Unknown
		try
		{
			if (!Store.Initialized)
			{
				Store.Honour = Cfg.StartHonour;
				Store.Dread = Cfg.StartDread;
				Store.HonourCap = 100;
				Store.LastDriftDay = Today();
				Store.Initialized = true;
				// The succession clock starts now. Without this, a save that
				// predates the mod is treated as ninety-nine years without a
				// Great Council and the whole realm is hammered on day one.
				if (Store.GetI("sc:council", -9999) < -9000)
				{
					Store.SetI("sc:council", Today());
					Store.SetI("sc:drift", Today());
				}
				Store.AddDeed(Standing.Date() + "  The court is convened.");
				try
				{
					InformationManager.DisplayMessage(new InformationMessage("Wardens & Dragons: the court is convened. Honour " + Store.Honour + ", Dread " + Store.Dread + "."));
				}
				catch
				{
				}
				Log.Write("first session: Honour " + Store.Honour + ", Dread " + Store.Dread);
			}
			Tourney.Reset();
			Law.Reset();
			Guard.Reset();
			Ravens.Reset();
			Host.Load();
			Exile.Load();
			Host.Patch();
			Sworn.Patch();
			Parley.PatchCrowd();
			Log.Write("rot duel available: " + RotDuel.Available);
			Menus.Register(starter);
			Dialogue.Add(starter);
			CouncilDialogue.Add(starter);
			Log.Write("warden dialogue registered");
			Dragons.EnsureSeeded();
			// Children who came to the gate before their family lines and
			// pages were written get them now.
			Baseborn.Repair();
			// Sworn knights from before the ceremony existed get the white
			// armour now.
			Guard.Repair();
			// Knights' houses from before they had pages get them now.
			Knighting.Repair();
		// One-time, for anyone upgrading: hand any privy-council seat still
		// holding one of our old duties back to a Bellum default, now that
		// ours no longer exist.
		Handback.Run();
			if (SubModule.OldModPresent)
			{
				Flow.Notify("Wardens of the Realm is still installed. Remove it - Wardens & Dragons now does its job, and running both doubles every conversation option.");
			}
		}
		catch (Exception ex)
		{
			Log.Write("session launch failed: " + ex);
		}
	}

	private void OnDailyTick()
	{
		try
		{
			if (Store.Initialized)
			{
				int num = Today();
				Harrenhal.Daily(num);
				Oaths.Yearly(num);
				Baseborn.Daily();
				Tourney.Daily();
				Law.Daily();
				Guard.Daily();
				Ravens.Daily();
				Treachery.Daily();
				Council.Daily();
				Host.Daily();
				Abdication.Daily();
				Sworn.Daily();
				Attainder.Daily();
				Knighting.Weekly();
				IronBank.Daily();
				Exile.Daily();
				SettleTheDead();
				Titles.Invalidate();
				if (num - Store.LastDriftDay >= Cfg.DaysPerSeason)
				{
					Store.LastDriftDay = num;
					Standing.Drift();
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("tickerr", "daily tick failed: " + ex.Message);
		}
	}

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool notify)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		try
		{
			if (victim != null && killer != null && killer == Hero.MainHero && ((int)detail == 6 || (int)detail == 7))
			{
				Standing.Change(-Cfg.ExecuteHonour, Cfg.ExecuteDread, "Executed " + victim.Name);
			}
			// A rider who falls in battle usually takes his dragon down with
			// him; one who dies of old age leaves it riderless. This is the
			// only place the game tells us which kind of death it was, and
			// without it no dragon in the world ever died.
			// KillCharacterActionDetail: 1 Murdered, 2 DiedInLabor,
			// 3 DiedOfOldAge, 4 DiedInBattle, 5 WoundedInBattle, 6 Executed,
			// 7 ExecutionAfterMapEvent, 8 Lost.
			//
			// DiedInBattle is the whole point of this hook and it was left out,
			// while DiedInLabor was counted as violent - so a rider killed in
			// battle by an ordinary trooper (killer null, which is the usual
			// case) never took his dragon down, and a woman who died in
			// childbirth had a good chance of killing hers.
			int dd = (int)detail;
			bool violent = dd == 1 || dd == 4 || dd == 5 || dd == 6 || dd == 7;
			Dragons.OnRiderDeath(victim, violent);

			// The law writes it down, if it was a crime somebody could answer for.
			Law.OnHeroKilled(victim, killer, detail);

			// Take the seat back for the heir who was named for it.
			//
			// By the time this event fires the game has already handed the
			// clan - and with it the crown, since a kingdom's leader is just
			// its ruling clan's leader - to whoever scored highest on its own
			// tally, which rewards being male and older far more than being of
			// the blood. Correct it now, in the same breath, before any screen
			// has drawn the wrong name.
			//
			// ONLY when the player themself has died. The guard used to be
			// "victim.Clan == Clan.PlayerClan", which is true of every brother,
			// cousin, spouse and companion in the house - so a relative dying
			// of old age deposed the LIVING player, handed their gold and
			// their party to a sibling, and, for a ruler, handed over the
			// crown with them.
			try
			{
				if (victim != null && victim == Hero.MainHero && Cfg.Succession && Cfg.EnforceHeir && Clan.PlayerClan != null)
				{
					// What the player just answered on the heir screen, if
					// they were asked. That screen runs BEFORE the kill, and
					// the kill then re-runs the game's own leader scoring and
					// overwrites their answer - which is the whole of why a
					// queen's chosen son was proclaimed and her husband got
					// the kingdom. Their choice is the last word, so put it
					// back.
					// With the unlock off, nothing is recorded and the law is
					// the answer; with it on, the player's own choice is.
					Hero chosen = Cfg.UnlockHeir
						? (Succession.Chosen() ?? Succession.Named())
						: (Laws.Legal() ?? Succession.Named());
					if (chosen != null && chosen != victim)
					{
						Succession.Install(Clan.PlayerClan, chosen, "the heir you chose");
					}
				}
			}
			catch (Exception se)
			{
				Log.Once("installheir", "taking the seat back failed: " + se.Message);
			}
			// The ruler is dead. Do NOT resolve it here: the game has not yet
			// settled who you are playing, the heir screen has not been
			// answered, and reading Hero.MainHero in this moment gives the
			// wrong answer. Write the name down and deal with it on the next
			// tick, when the dust is down.
			bool wasTheRuler = victim != null && (victim == Hero.MainHero ||
				(victim.Clan != null && victim.Clan == Clan.PlayerClan && victim.Clan.Leader == victim));
			if (wasTheRuler && string.IsNullOrEmpty(Store.Get("sc:dead")))
			{
				Store.Set("sc:dead", ((MBObjectBase)victim).StringId);
				Store.SetI("sc:deadday", Today());
				Log.Write("the ruler " + victim.Name + " is dead; the succession will be settled on the next tick");
			}
		}
		catch (Exception ex)
		{
			Log.Once("killerr", "execution hook failed: " + ex.Message);
		}
	}

	private void OnOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturer, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		Harrenhal.OnOwnerChanged(settlement, newOwner, oldOwner, detail.ToString());
	}

	// Settle a death we wrote down earlier, once the game has decided who we
	// are now. The succession resolves first, and only then is the other road
	// offered - so a realm that fractures has already fractured before you are
	// asked whether you want to be the one it fractured toward.
	private static void SettleTheDead()
	{
		try
		{
			string id = Store.Get("sc:dead");
			if (string.IsNullOrEmpty(id))
			{
				return;
			}
			if (Today() <= Store.GetI("sc:deadday", -9999))
			{
				return;
			}
			Hero was = Hero.DeadOrDisabledHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id)
				?? Hero.AllAliveHeroes.FirstOrDefault((Hero h) => ((MBObjectBase)h).StringId == id);
			if (was == null)
			{
				Store.Set("sc:dead", null);
				Store.Set("sc:deadday", null);
				return;
			}
			// Do not throw the record away until it has actually been settled.
			//
			// Succession.OnDeath now declines to act when the player's house
			// does not rule, and Kingdom.Leader can read null for a tick while
			// a crown changes hands. Clearing the keys first gave that check
			// exactly one chance to be right, and one transient false reading
			// discarded the whole succession - popup, defections, chronicle -
			// permanently and silently. Give it a few days to settle instead.
			if (!Succession.Rules() && Today() - Store.GetI("sc:deadday", Today()) < 7)
			{
				return;
			}
			Store.Set("sc:dead", null);
			Store.Set("sc:deadday", null);
			// The heir popup has been answered by now, and the game has moved
			// the player onto their choice. That choice is the most recent
			// word on who should hold this house, so it outranks anything
			// written down earlier - and the game will happily have left
			// somebody else on the seat.
			try
			{
				if (Cfg.Succession && Cfg.EnforceHeir && Clan.PlayerClan != null)
				{
					Hero lead = Succession.ShouldLead(Clan.PlayerClan);
					if (lead != null && Clan.PlayerClan.Leader != lead)
					{
						Succession.Install(Clan.PlayerClan, lead, "the heir you chose when it came");
					}
				}
			}
			catch (Exception ie)
			{
				Log.Once("settleheir", "seating the heir failed: " + ie.Message);
			}
			// And then the one question this mod asks about a death. It
			// replaces the whole succession ledger that used to resolve here:
			// nothing has been accumulating, nothing is owed, and if the
			// player says no the campaign simply carries on.
			Bastard.Offer(was);
		}
		catch (Exception e)
		{
			Log.Once("settledead", "settling the succession failed: " + e.Message);
		}
	}

	internal static int Today()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CampaignTime now = CampaignTime.Now;
			return (int)now.ToDays;
		}
		catch
		{
			return 0;
		}
	}
}
}
