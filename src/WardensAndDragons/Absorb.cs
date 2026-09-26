using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;

namespace WardensAndDragons;

internal static class Absorb
{
	private static MethodInfo _liberty;

	private static MethodInfo _reconcile;

	private static PropertyInfo _pRealmLiberty;

	private static bool _mapped;

	private static void Map()
	{
		if (_mapped)
		{
			return;
		}
		_mapped = true;
		try
		{
			Type type = AccessTools.TypeByName("BellumCivile.Behaviors.ClientKingdomBehavior") ?? AccessTools.TypeByName("BellumCivile.ClientKingdomBehavior");
			if (type != null)
			{
				_liberty = AccessTools.Method(type, "BuildLibertyAssessment", (Type[])null, (Type[])null);
			}
			Type type2 = AccessTools.TypeByName("BellumCivile.ClientLibertyAssessment");
			if (type2 != null)
			{
				_pRealmLiberty = AccessTools.Property(type2, "RealmLibertyDesire");
			}
			Type type3 = AccessTools.TypeByName("BellumCivile.Behaviors.FeudalTitleBehavior") ?? AccessTools.TypeByName("BellumCivile.FeudalTitleBehavior");
			if (type3 != null)
			{
				_reconcile = AccessTools.Method(type3, "ReconcilePoliticalHierarchy", (Type[])null, (Type[])null);
			}
			Log.Write("absorb API: liberty=" + (_liberty != null) + " reconcile=" + (_reconcile != null));
		}
		catch (Exception ex)
		{
			Log.Write("absorb map failed: " + ex.Message);
		}
	}

	internal static float LibertyOf(Kingdom client)
	{
		Map();
		try
		{
			if (_liberty == null || _pRealmLiberty == null)
			{
				return -1f;
			}
			object obj = ClientsBehavior();
			if (obj == null)
			{
				return -1f;
			}
			object obj2 = _liberty.Invoke(obj, new object[1] { client });
			if (obj2 == null)
			{
				return -1f;
			}
			return Convert.ToSingle(_pRealmLiberty.GetValue(obj2, null));
		}
		catch
		{
			return -1f;
		}
	}

	private static object ClientsBehavior()
	{
		try
		{
			Type type = AccessTools.TypeByName("BellumCivile.Behaviors.ClientKingdomBehavior") ?? AccessTools.TypeByName("BellumCivile.ClientKingdomBehavior");
			if (type == null)
			{
				return null;
			}
			PropertyInfo propertyInfo = AccessTools.Property(type, "Instance");
			if (propertyInfo != null)
			{
				object value = propertyInfo.GetValue(null, null);
				if (value != null)
				{
					return value;
				}
			}
			Campaign current = Campaign.Current;
			if (current == null)
			{
				return null;
			}
			MethodInfo[] methods = typeof(Campaign).GetMethods();
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
			{
				if (!(methodInfo.Name != "GetCampaignBehavior") && methodInfo.IsGenericMethodDefinition && methodInfo.GetParameters().Length == 0)
				{
					return methodInfo.MakeGenericMethod(type).Invoke(current, null);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	internal static bool Eligible(Kingdom client, out string why, out float liberty)
	{
		why = null;
		liberty = LibertyOf(client);
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || playerClan.Kingdom == null)
			{
				why = "you rule no kingdom";
				return false;
			}
			if (liberty < 0f)
			{
				if (!Cfg.AbsorbAllowUnknownLiberty)
				{
					why = "their contentment cannot be read";
					return false;
				}
			}
			else if (liberty > Cfg.AbsorbMaxLiberty)
			{
				why = "they are too restless - liberty desire " + liberty.ToString("0") + ", and it must be " + Cfg.AbsorbMaxLiberty + " or less";
				return false;
			}
			int num = ((Hero.MainHero != null) ? Hero.MainHero.Gold : 0);
			if (num < Cfg.AbsorbGold)
			{
				why = "you need " + Cfg.AbsorbGold.ToString("N0") + " denars and hold " + num.ToString("N0");
				return false;
			}
			float influence = playerClan.Influence;
			if (influence < (float)Cfg.AbsorbInfluence)
			{
				why = "you need " + Cfg.AbsorbInfluence + " influence and hold " + influence.ToString("0");
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			why = ex.Message;
			return false;
		}
	}

	internal static void Offer(Hero theirLeader)
	{
		//IL_0224: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Expected O, but got Unknown
		//IL_023b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Expected O, but got Unknown
		try
		{
			if (theirLeader == null || theirLeader.Clan == null)
			{
				return;
			}
			Kingdom theirs = theirLeader.Clan.Kingdom;
			Clan playerClan = Clan.PlayerClan;
			Kingdom mine = ((playerClan != null) ? playerClan.Kingdom : null);
			if (theirs == null || mine == null)
			{
				return;
			}
			if (!Eligible(theirs, out var why, out var liberty))
			{
				Flow.Notify("Not yet: " + why + ".");
				Log.Write("absorb blocked: " + why);
				return;
			}
			int num = 0;
			int num2 = 0;
			foreach (Clan item in (List<Clan>)(object)theirs.Clans)
			{
				if (item != null && !item.IsEliminated)
				{
					num++;
				}
			}
			if (theirs.Fiefs != null)
			{
				num2 = ((List<Town>)(object)theirs.Fiefs).Count;
			}
			string desc = string.Concat(theirs.Name, " would cease to be a realm. Their ", num, " house(s) and ", num2, " fief(s) become yours, and ", theirLeader.Name, " holds their old realm of you as Warden.\n\nContentment (liberty desire): ", (liberty < 0f) ? "unknown" : liberty.ToString("0"), "\nCost: ", Cfg.AbsorbGold.ToString("N0"), " denars and ", Cfg.AbsorbInfluence, " influence\n\nThis cannot be undone by any means I know of.");
			List<InquiryElement> list = new List<InquiryElement>();
			list.Add(new InquiryElement((object)"do", "Bring them into the Empire", (ImageIdentifier)null));
			list.Add(new InquiryElement((object)"no", "Leave them as they are", (ImageIdentifier)null));
			List<InquiryElement> els = list;
			Inquiry.Select("Into the Empire", desc, els, 1, 1, "Continue", "Withdraw", delegate(List<InquiryElement> chosen)
			{
				if (chosen != null && chosen.Count != 0 && !(chosen[0].Identifier as string != "do"))
				{
					Execute(theirs, mine, theirLeader);
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("absorb offer failed: " + ex.Message);
		}
	}

	private static void Execute(Kingdom theirs, Kingdom mine, Hero theirLeader)
	{
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (!Eligible(theirs, out var why, out var _))
			{
				Flow.Notify("Not yet: " + why + ".");
				return;
			}
			Clan playerClan = Clan.PlayerClan;
			Clan rulingClan = theirs.RulingClan;
			List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(rulingClan);
			Log.Write(string.Concat("absorbing ", theirs.Name, "; their ruling house holds ", list.Count, " title(s) beforehand"));
			try
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, (Hero)null, Cfg.AbsorbGold, true);
				ChangeClanInfluenceAction.Apply(playerClan, (float)(-Cfg.AbsorbInfluence));
				Log.Write("charged " + Cfg.AbsorbGold + " gold and " + Cfg.AbsorbInfluence + " influence");
			}
			catch (Exception ex)
			{
				Log.Write("charge failed: " + ex.Message);
			}
			if (!Clients.Release(theirs, out var report))
			{
				Log.Write("note: EndClientStatus returned false (" + report + ") - continuing");
			}
			List<Clan> list2 = new List<Clan>();
			foreach (Clan item in (List<Clan>)(object)theirs.Clans)
			{
				if (item != null && !item.IsEliminated)
				{
					list2.Add(item);
				}
			}
			int num = 0;
			foreach (Clan item2 in list2)
			{
				try
				{
					ChangeKingdomAction.ApplyByJoinToKingdom(item2, mine, CampaignTime.Zero, false);
					num++;
				}
				catch (Exception ex2)
				{
					Log.Write(string.Concat("could not move ", item2.Name, ": ", ex2.Message));
				}
			}
			Log.Write("moved " + num + "/" + list2.Count + " house(s) into " + mine.Name);
			try
			{
				Map();
				if (_reconcile != null)
				{
					Type type = AccessTools.TypeByName("BellumCivile.Behaviors.FeudalTitleBehavior") ?? AccessTools.TypeByName("BellumCivile.FeudalTitleBehavior");
					object obj = null;
					Campaign current3 = Campaign.Current;
					MethodInfo[] methods = typeof(Campaign).GetMethods();
					MethodInfo[] array = methods;
					foreach (MethodInfo methodInfo in array)
					{
						if (!(methodInfo.Name != "GetCampaignBehavior") && methodInfo.IsGenericMethodDefinition && methodInfo.GetParameters().Length == 0)
						{
							obj = methodInfo.MakeGenericMethod(type).Invoke(current3, null);
							break;
						}
					}
					if (obj != null)
					{
						ParameterInfo[] parameters = _reconcile.GetParameters();
						_reconcile.Invoke(obj, (parameters.Length != 1) ? new object[0] : new object[1] { "absorbed client realm" });
						Log.Write("asked Bellum to reconcile the hierarchy");
					}
				}
			}
			catch (Exception ex3)
			{
				Log.Write("reconcile failed: " + ex3.Message);
			}
			try
			{
				OathKind oathKind = Oaths.Of(theirs);
				Oaths.ClearKingdom(theirs);
				if (rulingClan != null)
				{
					Oaths.SetForClan(rulingClan, (oathKind == OathKind.None) ? OathKind.Fealty : oathKind, quiet: true);
					string style = Styles.CanonicalFor(((object)theirs.Name).ToString()) ?? ("Warden of " + theirs.Name);
					Styles.Set(rulingClan, style);
				}
			}
			catch (Exception ex4)
			{
				Log.Write("absorb style/oath failed: " + ex4.Message);
			}
			List<KeyValuePair<string, object>> list3 = Bellum.TitlesHeldBy(rulingClan);
			Log.Write("their ruling house now holds " + list3.Count + " title(s)");
			Flow.Notify(string.Concat(theirs.Name, " is part of your empire. ", theirLeader.Name, " serves as Warden of their old realm."));
			try
			{
				ChangeRelationAction.ApplyPlayerRelation(theirLeader, Cfg.AbsorbRelation, true, true);
			}
			catch
			{
			}
		}
		catch (Exception ex5)
		{
			Log.Write("absorb execute failed: " + ex5);
			Flow.Notify("Something went wrong - see wardens.log.");
		}
	}
}
