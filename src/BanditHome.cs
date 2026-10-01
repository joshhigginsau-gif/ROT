using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// A bandit or looter party with no home settlement crashes every load:
	// BanditSpawnCampaignBehavior.CacheBanditCounts keys a dictionary on it.
	// Deserter bands made at sea could be born homeless. Before the count is
	// taken, every homeless band is given the nearest town or village.
	internal static class BanditHome
	{
		private static FieldInfo _related;

		internal static void Patch(Harmony h)
		{
			try
			{
				_related = AccessTools.Field(typeof(BanditPartyComponent), "_relatedSettlement");
				Type t = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.BanditSpawnCampaignBehavior");
				MethodInfo m = (t != null) ? AccessTools.Method(t, "CacheBanditCounts", (Type[])null, (Type[])null) : null;
				if (m == null || _related == null)
				{
					Log.Write("bandit home guard: not installed (method=" + (m != null) + " field=" + (_related != null) + ")");
					return;
				}
				h.Patch(m, new HarmonyMethod(typeof(BanditHome).GetMethod("Pre", BindingFlags.Static | BindingFlags.NonPublic)));
				Log.Write("bandit home guard: installed on BanditSpawnCampaignBehavior.CacheBanditCounts");
			}
			catch (Exception e)
			{
				Log.Write("bandit home guard: failed: " + e.Message);
			}
		}

		private static void Pre()
		{
			try
			{
				foreach (MobileParty p in MobileParty.AllBanditParties.ToList())
				{
					try
					{
						BanditPartyComponent c = (p != null) ? (p.PartyComponent as BanditPartyComponent) : null;
						if (c == null || c.Hideout != null || c.HomeSettlement != null)
						{
							continue;
						}
						Settlement s = Nearest(p);
						if (s == null)
						{
							continue;
						}
						_related.SetValue(c, s);
						Log.Write("bandit home repaired: " + ((MBObjectBase)p).StringId + " -> " + s.Name);
					}
					catch (Exception e)
					{
						Log.Once("bandithome1", "bandit home: repairing a party failed: " + e.Message);
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("bandithome", "bandit home: the check failed: " + e.Message);
			}
		}

		internal static Settlement Nearest(MobileParty p)
		{
			Settlement best = null;
			float d = float.MaxValue;
			foreach (Settlement s in Settlement.All)
			{
				if (s == null || !(s.IsTown || s.IsVillage))
				{
					continue;
				}
				float x = s.GatePosition.DistanceSquared(p.Position);
				if (x < d)
				{
					d = x;
					best = s;
				}
			}
			return best;
		}
	}
}
