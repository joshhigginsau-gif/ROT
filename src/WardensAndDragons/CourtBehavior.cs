using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

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
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener((object)this, (Action<Hero, Hero, KillCharacterActionDetail, bool>)OnHeroKilled);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener((object)this, (Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementDetail>)OnOwnerChanged);
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
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Expected O, but got Unknown
		try
		{
			if (!Store.Initialized)
			{
				Store.Honour = Cfg.StartHonour;
				Store.Dread = Cfg.StartDread;
				Store.HonourCap = 100;
				Store.LastDriftDay = Today();
				Store.Initialized = true;
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
			Menus.Register(starter);
			Dialogue.Add(starter);
			Log.Write("warden dialogue registered");
			Dragons.EnsureSeeded();
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

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterActionDetail detail, bool notify)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected I4, but got Unknown
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Invalid comparison between Unknown and I4
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Invalid comparison between Unknown and I4
		try
		{
			if (victim != null && killer != null && killer == Hero.MainHero && ((int)detail == 6 || (int)detail == 7))
			{
				Standing.Change(-Cfg.ExecuteHonour, Cfg.ExecuteDread, "Executed " + victim.Name);
			}
			int num = (int)detail;
			bool violent = num == 1 || num == 4 || num == 5 || num == 6 || num == 7;
			Dragons.OnRiderDeath(victim, violent);
			try
			{
				if (victim != null && victim == Hero.MainHero && Cfg.Succession && Cfg.EnforceHeir && Clan.PlayerClan != null)
				{
					Hero val = ((!Cfg.UnlockHeir) ? (Laws.Legal() ?? Succession.Named()) : (Succession.Chosen() ?? Succession.Named()));
					if (val != null && val != victim)
					{
						Succession.Install(Clan.PlayerClan, val, "the heir you chose");
					}
				}
			}
			catch (Exception ex)
			{
				Log.Once("installheir", "taking the seat back failed: " + ex.Message);
			}
			if (victim != null && (victim == Hero.MainHero || (victim.Clan != null && victim.Clan == Clan.PlayerClan && victim.Clan.Leader == victim)) && string.IsNullOrEmpty(Store.Get("sc:dead")))
			{
				Store.Set("sc:dead", ((MBObjectBase)victim).StringId);
				Store.SetI("sc:deadday", Today());
				Log.Write(string.Concat("the ruler ", victim.Name, " is dead; the succession will be settled on the next tick"));
			}
		}
		catch (Exception ex2)
		{
			Log.Once("killerr", "execution hook failed: " + ex2.Message);
		}
	}

	private void OnOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturer, ChangeOwnerOfSettlementDetail detail)
	{
		Harrenhal.OnOwnerChanged(settlement, newOwner, oldOwner, detail.ToString());
	}

	private static void SettleTheDead()
	{
		try
		{
			string id = Store.Get("sc:dead");
			if (string.IsNullOrEmpty(id) || Today() <= Store.GetI("sc:deadday", -9999))
			{
				return;
			}
			Hero val = ((IEnumerable<Hero>)Hero.DeadOrDisabledHeroes).FirstOrDefault((Func<Hero, bool>)((Hero h) => ((MBObjectBase)h).StringId == id)) ?? ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero h) => ((MBObjectBase)h).StringId == id));
			if (val == null)
			{
				Store.Set("sc:dead", null);
				Store.Set("sc:deadday", null);
			}
			else
			{
				if (!Succession.Rules() && Today() - Store.GetI("sc:deadday", Today()) < 7)
				{
					return;
				}
				Store.Set("sc:dead", null);
				Store.Set("sc:deadday", null);
				try
				{
					if (Cfg.Succession && Cfg.EnforceHeir && Clan.PlayerClan != null)
					{
						Hero val2 = Succession.ShouldLead(Clan.PlayerClan);
						if (val2 != null && Clan.PlayerClan.Leader != val2)
						{
							Succession.Install(Clan.PlayerClan, val2, "the heir you chose when it came");
						}
					}
				}
				catch (Exception ex)
				{
					Log.Once("settleheir", "seating the heir failed: " + ex.Message);
				}
				Bastard.Offer(val);
			}
		}
		catch (Exception ex2)
		{
			Log.Once("settledead", "settling the succession failed: " + ex2.Message);
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
