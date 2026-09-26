using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace WardensAndDragons;

public class SubModule : MBSubModuleBase
{
	internal static bool OldModPresent;

	protected override void OnSubModuleLoad()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Expected O, but got Unknown
		base.OnSubModuleLoad();
		Log.Init();
		Cfg.Load();
		Log.Write("=== Wardens & Dragons v2.2.0 - children of your own ===");
		Log.Write("config read from: " + Cfg.LoadedFrom);
		Log.Write("config in effect: " + Cfg.Describe());
		try
		{
			Harmony val = new Harmony("community.wardens.and.dragons");
			if (Cfg.RelaxEligibility)
			{
				int num = 0;
				string[] array = new string[3] { "BellumCivile.FeudalTitlePlayerActionService", "BellumCivile.Behaviors.FeudalTitleBehavior", "BellumCivile.FeudalTitleBehavior" };
				string[] array2 = array;
				foreach (string text in array2)
				{
					Type type = AccessTools.TypeByName(text);
					MethodInfo methodInfo = ((type != null) ? AccessTools.Method(type, "RecipientHoldsImmediateChildTitle", (Type[])null, (Type[])null) : null);
					if (!(methodInfo == null))
					{
						MethodInfo methodInfo2 = methodInfo;
						HarmonyMethod val2 = new HarmonyMethod(AccessTools.Method(typeof(Relax), "RecipientPostfix", (Type[])null, (Type[])null));
						val.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, val2, (HarmonyMethod)null, (HarmonyMethod)null);
						num++;
					}
				}
				Log.Write("grant eligibility relaxed on " + num + " method(s)");
			}
			Oaths.PatchLiberty(val);
			Titles.Patch(val);
			Offices.Patch(val);
			Laws.Unlock(val);
			Laws.Listen(val);
			OldModPresent = AccessTools.TypeByName("WardensOfTheRealm.SubModule") != null;
			if (OldModPresent)
			{
				Log.Write("WARNING: Wardens of the Realm is still installed - remove it");
			}
		}
		catch (Exception ex)
		{
			Log.Write("patching failed: " + ex);
		}
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		base.OnGameStart(game, gameStarterObject);
		try
		{
			CampaignGameStarter val = (CampaignGameStarter)((!(gameStarterObject is CampaignGameStarter)) ? null : gameStarterObject);
			if (game.GameType is Campaign && val != null)
			{
				Store.ResetForNewCampaign();
				Harrenhal.Reset();
				Bellum.Reset();
				Dragons.Reset();
				Titles.Reset();
				Laws.Reset();
				Succession.Forget();
				Blade.Reset();
				Clients.Reset();
				val.AddBehavior((CampaignBehaviorBase)new CourtBehavior());
				Log.Write("campaign starting: state cleared, waiting for save data");
			}
		}
		catch (Exception ex)
		{
			Log.Write("OnGameStart failed: " + ex);
		}
	}
}
