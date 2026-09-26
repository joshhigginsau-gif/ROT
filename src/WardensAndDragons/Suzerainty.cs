using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;

namespace WardensAndDragons;

internal static class Suzerainty
{
	internal static void Ask(Hero hero)
	{
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		try
		{
			if (hero == null || hero.Clan == null)
			{
				return;
			}
			Kingdom theirs = hero.Clan.Kingdom;
			Clan playerClan = Clan.PlayerClan;
			Kingdom mine = ((playerClan != null) ? playerClan.Kingdom : null);
			if (theirs == null || mine == null)
			{
				return;
			}
			if (!Clients.CanEstablish(theirs, mine, out var report))
			{
				Flow.Notify("It cannot be done: " + (string.IsNullOrEmpty(report) ? "Bellum refused" : report));
				Log.Write("CanEstablishClientKingdom refused: " + report);
				return;
			}
			List<string> list = new List<string>();
			int odds = Clients.Odds(theirs, mine, list);
			string desc = string.Concat("If ", theirs.Name, " accepts, they keep their crown, their lands and their laws - but their wars and their friends become yours to choose.\n\nChance they accept: ", odds, "%\n\n", string.Join("\n", list.ToArray()));
			List<InquiryElement> list2 = new List<InquiryElement>();
			list2.Add(new InquiryElement((object)"ask", "Press the offer  (" + odds + "%)", (ImageIdentifier)null));
			list2.Add(new InquiryElement((object)"wait", "Say nothing for now", (ImageIdentifier)null));
			List<InquiryElement> els = list2;
			Inquiry.Select("Offer of Suzerainty", desc, els, 1, 1, "Continue", "Withdraw", delegate(List<InquiryElement> chosen)
			{
				if (chosen != null && chosen.Count != 0 && !(chosen[0].Identifier as string != "ask"))
				{
					Resolve(theirs, mine, odds, hero);
				}
			});
		}
		catch (Exception ex)
		{
			Log.Write("suzerainty ask failed: " + ex.Message);
		}
	}

	private static void Resolve(Kingdom theirs, Kingdom mine, int odds, Hero theirLeader)
	{
		try
		{
			if (!Cfg.SuzeraintyAlwaysAccepted && MBRandom.RandomInt(100) >= odds)
			{
				Flow.Notify(string.Concat(theirs.Name, " refuses. They will not kneel today."));
				Log.Write(string.Concat("suzerainty refused by ", theirs.Name, " (odds ", odds, "%)"));
				try
				{
					if (theirLeader != null && Hero.MainHero != null)
					{
						ChangeRelationAction.ApplyPlayerRelation(theirLeader, -5, true, true);
					}
					return;
				}
				catch
				{
					return;
				}
			}
			if (Clients.Establish(theirs, mine, out var report))
			{
				Flow.Notify(string.Concat(theirs.Name, " now holds their realm of you. Their banner answers to yours."));
				Log.Write(string.Concat("client kingdom established: ", theirs.Name, " under ", mine.Name));
			}
			else
			{
				Flow.Notify("They agreed, but it could not be set: " + (report ?? "no reason given"));
				Log.Write("TryEstablishClientKingdom failed: " + report);
			}
		}
		catch (Exception ex)
		{
			Log.Write("suzerainty resolve failed: " + ex.Message);
		}
	}

	internal static void Release(Hero hero)
	{
		try
		{
			if (hero == null || hero.Clan == null)
			{
				return;
			}
			Kingdom kingdom = hero.Clan.Kingdom;
			if (kingdom == null)
			{
				return;
			}
			if (Clients.Release(kingdom, out var report))
			{
				Oaths.ClearKingdom(kingdom);
				Standing.Change(Cfg.ReleaseHonour, 0, string.Concat("Released ", kingdom.Name, " from its oath"));
				Flow.Notify(string.Concat("You have released ", kingdom.Name, " from their oath."));
				Log.Write("released client: " + kingdom.Name);
				try
				{
					if (Hero.MainHero != null)
					{
						ChangeRelationAction.ApplyPlayerRelation(hero, 10, true, true);
					}
					return;
				}
				catch
				{
					return;
				}
			}
			Flow.Notify("It could not be undone: " + (report ?? "no reason given"));
			Log.Write("release failed: " + report);
		}
		catch (Exception ex)
		{
			Log.Write("release failed: " + ex.Message);
		}
	}
}
