using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace WardensAndDragons
{
	// What the realm needs from you, at the top of the court.
	//
	// This used to speak for the council, the plots and the Great Council as
	// well, and most of what it said was about systems the player could not
	// see. With those gone it has less to say and says it better: only things
	// you could actually do something about today, and nothing at all when
	// there is nothing.
	internal static class Attention
	{
		internal static List<string> Items()
		{
			List<string> list = new List<string>();
			try
			{
				// --- the children of other houses ---
				try
				{
					List<Held> forfeit = Wardship.All().Where((Held h) => h.Forfeit).ToList();
					if (forfeit.Count > 0)
					{
						list.Add(forfeit.Count + " house" + ((forfeit.Count == 1) ? " has" : "s have") +
							" broken faith while you hold their blood. Nobody would call it murder now.");
					}
				}
				catch
				{
				}

				// --- the succession ---
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

				// --- the law ---
				try
				{
					string law = Law.Attention();
					if (law != null)
					{
						list.Add(law);
					}
				}
				catch
				{
				}

				// --- the white cloaks ---
				try
				{
					string kg = Guard.Attention();
					if (kg != null)
					{
						list.Add(kg);
					}
				}
				catch
				{
				}

				// --- the ravens ---
				try
				{
					string rv = Treachery.Attention() ?? Ravens.Attention();
					if (rv != null)
					{
						list.Add(rv);
					}
				}
				catch
				{
				}

				// --- the lists ---
				try
				{
					string lists = Tourney.Attention();
					if (lists != null)
					{
						list.Add(lists);
					}
				}
				catch
				{
				}

				// --- dragons ---
				try
				{
					if (Dragons.Seat != null && Dragons.Seat.OwnerClan == Clan.PlayerClan)
					{
						int waiting = Dragons.All().Count((DragonRec d) => d.Alive && d.Claimable);
						if (waiting > 0)
						{
							float odds = Cfg.ClaimBase * Dragons.Crowding() * Dragons.Twilight();
							if (odds < 15f)
							{
								list.Add(waiting + " dragon" + ((waiting == 1) ? " waits" : "s wait") +
									" riderless, but a claim is only " + odds.ToString("0") + "% today.");
							}
						}
					}
				}
				catch
				{
				}
			}
			catch (Exception e)
			{
				Log.Once("attention", "the court clerk failed: " + e.Message);
			}
			return list;
		}

		// The block that goes at the top of the court, or nothing at all when
		// there is nothing to say. Silence is the point: a list that is always
		// there stops being read.
		internal static string Block()
		{
			if (!Cfg.CourtAttention)
			{
				return null;
			}
			List<string> items = Items();
			if (items.Count == 0)
			{
				return null;
			}
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("\nWAITING ON YOU\n");
			int n = 0;
			foreach (string s in items)
			{
				sb.Append("  - ").Append(s).Append("\n");
				if (++n >= Cfg.CourtAttentionMax)
				{
					int more = items.Count - n;
					if (more > 0)
					{
						sb.Append("  - and ").Append(more).Append(" other thing").Append((more == 1) ? "" : "s").Append(".\n");
					}
					break;
				}
			}
			return sb.ToString();
		}
	}
}
