using TaleWorlds.CampaignSystem;

namespace WardensAndDragons;

internal static class Relax
{
	internal static void RecipientPostfix(Clan recipientClan, ref bool __result)
	{
		if (__result)
		{
			return;
		}
		try
		{
			if (recipientClan == null || Campaign.Current == null)
			{
				return;
			}
			Clan playerClan = Clan.PlayerClan;
			if (playerClan != null && !object.ReferenceEquals(recipientClan, playerClan))
			{
				Kingdom kingdom = playerClan.Kingdom;
				if (kingdom != null && kingdom.RulingClan == playerClan && recipientClan.Kingdom == kingdom)
				{
					__result = true;
				}
			}
		}
		catch
		{
		}
	}
}
