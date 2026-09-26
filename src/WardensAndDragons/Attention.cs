using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;

namespace WardensAndDragons;

internal static class Attention
{
	internal static List<string> Items()
	{
		List<string> list = new List<string>();
		try
		{
			try
			{
				List<Held> list2 = (from h in Wardship.All()
					where h.Forfeit
					select h).ToList();
				if (list2.Count > 0)
				{
					list.Add(list2.Count + " house" + ((list2.Count != 1) ? "s have" : " has") + " broken faith while you hold their blood. Nobody would call it murder now.");
				}
			}
			catch
			{
			}
			if (Cfg.Succession && Succession.Rules())
			{
				if (Succession.Named() == null)
				{
					list.Add("You have named no heir. The realm is guessing, and guessing badly.");
				}
				else if (!Laws.Lawful() && Cfg.UnlockHeir)
				{
					list.Add("Your heir is named against your culture's law, and every house in the realm knows it.");
				}
			}
			try
			{
				if (Dragons.Seat != null && Dragons.Seat.OwnerClan == Clan.PlayerClan)
				{
					int num = Dragons.All().Count((DragonRec d) => d.Alive && d.Claimable);
					if (num > 0)
					{
						float num2 = (float)Cfg.ClaimBase * Dragons.Crowding() * Dragons.Twilight();
						if (num2 < 15f)
						{
							list.Add(num + " dragon" + ((num != 1) ? "s wait" : " waits") + " riderless, but a claim is only " + num2.ToString("0") + "% today.");
						}
					}
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Log.Once("attention", "the court clerk failed: " + ex.Message);
		}
		return list;
	}

	internal static string Block()
	{
		if (!Cfg.CourtAttention)
		{
			return null;
		}
		List<string> list = Items();
		if (list.Count == 0)
		{
			return null;
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("\nWAITING ON YOU\n");
		int num = 0;
		foreach (string item in list)
		{
			stringBuilder.Append("  - ").Append(item).Append("\n");
			if (++num >= Cfg.CourtAttentionMax)
			{
				int num2 = list.Count - num;
				if (num2 > 0)
				{
					stringBuilder.Append("  - and ").Append(num2).Append(" other thing")
						.Append((num2 != 1) ? "s" : "")
						.Append(".\n");
				}
				break;
			}
		}
		return stringBuilder.ToString();
	}
}
