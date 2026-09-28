using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace WardensAndDragons
{
public class SubModule : MBSubModuleBase
{
	internal static bool OldModPresent;

	protected override void OnSubModuleLoad()
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Expected O, but got Unknown
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Expected O, but got Unknown
		base.OnSubModuleLoad();
		Log.Init();
		Cfg.Load();
		Log.Write("=== Wardens & Dragons v2.10.3 - parley at the walls ===");
		Log.Write("config read from: " + Cfg.LoadedFrom);
		Log.Write("config in effect: " + Cfg.Describe());
		try
		{
			Harmony val = new Harmony("community.wardens.and.dragons");
			if (Cfg.RelaxEligibility)
			{
				int num = 0;
				string[] array = new string[3] { "BellumCivile.FeudalTitlePlayerActionService", "BellumCivile.Behaviors.FeudalTitleBehavior", "BellumCivile.FeudalTitleBehavior" };
				foreach (string text in array)
				{
					Type type = AccessTools.TypeByName(text);
					MethodInfo methodInfo = ((!(type != null)) ? null : AccessTools.Method(type, "RecipientHoldsImmediateChildTitle", (Type[])null, (Type[])null));
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
		base.OnGameStart(game, gameStarterObject);
		try
		{
			CampaignGameStarter val = (CampaignGameStarter)(object)((gameStarterObject is CampaignGameStarter) ? gameStarterObject : null);
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
				val.AddBehavior((CampaignBehaviorBase)(object)new CourtBehavior());
				Log.Write("campaign starting: state cleared, waiting for save data");
			}
		}
		catch (Exception ex)
		{
			Log.Write("OnGameStart failed: " + ex);
		}
	}
}
}
