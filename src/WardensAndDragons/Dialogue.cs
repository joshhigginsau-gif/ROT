using static TaleWorlds.CampaignSystem.Conversation.ConversationSentence;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;

namespace WardensAndDragons;

internal static class Dialogue
{
	internal static void Add(CampaignGameStarter s)
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected O, but got Unknown
		//IL_00af: Expected O, but got Unknown
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_0117: Expected O, but got Unknown
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_0109: Expected O, but got Unknown
		//IL_017f: Expected O, but got Unknown
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Expected O, but got Unknown
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ee: Expected O, but got Unknown
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_022b: Expected O, but got Unknown
		//IL_025e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0268: Expected O, but got Unknown
		//IL_029b: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Expected O, but got Unknown
		//IL_0318: Unknown result type (might be due to invalid IL or missing references)
		//IL_0322: Expected O, but got Unknown
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Expected O, but got Unknown
		//IL_0392: Unknown result type (might be due to invalid IL or missing references)
		//IL_039c: Expected O, but got Unknown
		//IL_03cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d9: Expected O, but got Unknown
		//IL_046b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0475: Expected O, but got Unknown
		//IL_04a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b2: Expected O, but got Unknown
		//IL_04e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_04ef: Expected O, but got Unknown
		//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0522: Unknown result type (might be due to invalid IL or missing references)
		//IL_052c: Expected O, but got Unknown
		//IL_05e2: Expected O, but got Unknown
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d4: Expected O, but got Unknown
		s.AddPlayerLine("wardens_offer", "lord_talk_speak_diplomacy_2", "wardens_reply", "{=Wardens_Offer}Kneel. I would name you Warden of a realm, to hold in my name.", new OnConditionDelegate(CanName), (OnConsequenceDelegate)delegate
		{
			Flow.Begin(Hero.OneToOneConversationHero);
		}, 110, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_reply", "wardens_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_Reply}You honour my house beyond deserving. Name the realm and I shall hold it.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 110, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_revoke", "lord_talk_speak_diplomacy_2", "wardens_revoke_reply", "{=Wardens_Revoke}You have held your wardenship poorly. I mean to take it back.", new OnConditionDelegate(CanRevoke), (OnConsequenceDelegate)delegate
		{
			Flow.BeginRevoke(Hero.OneToOneConversationHero);
		}, 109, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_revoke_reply", "wardens_revoke_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_RevokeReply}You would strip my house of what was given? Choose, then.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 109, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_client_offer", "lord_talk_speak_diplomacy_2", "wardens_client_reply", "{=Wardens_ClientOffer}Rule your realm as my Warden. Keep your crown, your lands and your laws - but my banner flies above them.", new OnConditionDelegate(CanAskSuzerainty), (OnConsequenceDelegate)delegate
		{
			Offer.Open(Hero.OneToOneConversationHero);
		}, 108, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_client_reply", "wardens_client_reply", "wardens_persuade", "{=Wardens_ClientReply}You ask me to bend the knee and call it honour. Say your piece.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 108, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_client_release", "lord_talk_speak_diplomacy_2", "wardens_client_release_reply", "{=Wardens_ClientRelease}I release your realm from its oath. Rule free of me.", new OnConditionDelegate(CanRelease), (OnConsequenceDelegate)delegate
		{
			Suzerainty.Release(Hero.OneToOneConversationHero);
		}, 107, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_client_release_reply", "wardens_client_release_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_ClientReleaseReply}Freely given? My house will remember it.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 107, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_client_reply", "wardens_client_reply", "wardens_offer_how", "{=Wardens_ClientReply}You ask me to bend the knee and call it honour. On what grounds?", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 108, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_how_shelter", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Shelter}Your enemies gather. Under my banner they must face me first.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Shelter);
		}, 100, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_honour", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Honour}I do not ask for your crown. Your house keeps its name, its halls and its laws.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Honour);
		}, 99, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_wealth", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Wealth}My roads and my markets would be yours. Your people would grow fat on it.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Wealth);
		}, 98, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_threat", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Threat}Kneel now and keep your realm whole. Refuse, and I will take it in pieces.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Threat);
		}, 97, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_never_mind", "wardens_offer_how", "wardens_offer_dropped", "{=Wardens_How_Drop}Forget I spoke. The moment is wrong.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 90, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_offer_dropped", "wardens_offer_dropped", "lord_talk_speak_diplomacy_2", "{=Wardens_Dropped}It is already forgotten.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_offer_yes", "wardens_offer_result", "wardens_after_yes", "{=Wardens_Yes}...Then let it be so. My realm holds of yours. My banners answer your call, and my enemies are yours to name. See that you are worth the kneeling.", (OnConditionDelegate)(() => Offer.Accepted), (OnConsequenceDelegate)null, 120, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_yes_gracious", "wardens_after_yes", "wardens_yes_gracious_reply", "{=Wardens_Yes_Gracious}You lose nothing today but a title on a map. You have my word on it.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Bond(8);
		}, 100, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_yes_cold", "wardens_after_yes", "wardens_yes_cold_reply", "{=Wardens_Yes_Cold}You kneel because you must. Do not mistake it for friendship.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Bond(-6);
		}, 99, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_yes_why", "wardens_after_yes", "wardens_yes_why_reply", "{=Wardens_Yes_Why}What decided you, in the end?", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Offer.ShowBreakdown();
		}, 98, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_yes_gracious_reply", "wardens_yes_gracious_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_Yes_GraciousR}Words are cheap until they are kept. I shall watch which sort yours are.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_yes_cold_reply", "wardens_yes_cold_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_Yes_ColdR}No. I shall not mistake it. Nor forget it.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_yes_why_reply", "wardens_yes_why_reply", "wardens_after_yes", "{=Wardens_Yes_WhyR}A ruler weighs what is before them and calls it wisdom afterwards. Ask me again in ten years.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_offer_no", "wardens_offer_result", "wardens_after_no", "{=Wardens_No}No. My forebears did not bleed for this realm so that I might hand it across a table. My crown is my own, and so it stays.", (OnConditionDelegate)(() => !Offer.Accepted), (OnConsequenceDelegate)null, 119, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_no_grace", "wardens_after_no", "wardens_no_grace_reply", "{=Wardens_No_Grace}Then keep it, and keep my respect with it. The offer stands when you need it.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Bond(5);
		}, 100, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_no_threat", "wardens_after_no", "wardens_no_threat_reply", "{=Wardens_No_Threat}Remember this hour when my banners are on your border.", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Bond(-10);
		}, 99, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_no_why", "wardens_after_no", "wardens_no_why_reply", "{=Wardens_No_Why}What would it have taken?", (OnConditionDelegate)null, (OnConsequenceDelegate)delegate
		{
			Offer.ShowBreakdown();
		}, 98, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_no_grace_reply", "wardens_no_grace_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_No_GraceR}That is more graciously said than I expected. It will be remembered - which is not the same as accepted.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_no_threat_reply", "wardens_no_threat_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_No_ThreatR}I shall. And my walls will remember it with me.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_no_why_reply", "wardens_no_why_reply", "wardens_after_no", "{=Wardens_No_WhyR}Strength, mostly. And the sense that you would keep your word. Bring me more of both.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 100, (OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_absorb", "lord_talk_speak_diplomacy_2", "wardens_absorb_reply", "{=Wardens_Absorb}The time has come. Your realm should be part of mine in name as well as fact.", new OnConditionDelegate(CanAbsorb), (OnConsequenceDelegate)delegate
		{
			Absorb.Offer(Hero.OneToOneConversationHero);
		}, 106, (OnClickableConditionDelegate)null, (OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_absorb_reply", "wardens_absorb_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_AbsorbReply}You would end my realm and call it honour. Speak your terms.", (OnConditionDelegate)null, (OnConsequenceDelegate)null, 106, (OnClickableConditionDelegate)null);
	}

	private static bool Sovereign(out Clan player, out Kingdom kingdom, out Clan other)
	{
		player = null;
		kingdom = null;
		other = null;
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero == null || Campaign.Current == null)
			{
				return false;
			}
			player = Clan.PlayerClan;
			if (player == null)
			{
				return false;
			}
			kingdom = player.Kingdom;
			if (kingdom == null || kingdom.RulingClan != player)
			{
				return false;
			}
			other = oneToOneConversationHero.Clan;
			if (other == null || other == player)
			{
				return false;
			}
			if (other.Leader != oneToOneConversationHero || other.IsEliminated)
			{
				return false;
			}
			if (other.Kingdom != kingdom)
			{
				return false;
			}
			return Bellum.Init();
		}
		catch
		{
			return false;
		}
	}

	private static bool CanName()
	{
		try
		{
			if (!Sovereign(out var player, out var _, out var _))
			{
				return false;
			}
			return Bellum.TitlesHeldBy(player).Count > 0;
		}
		catch
		{
			return false;
		}
	}

	private static bool ForeignRuler(out Kingdom mine, out Kingdom theirs)
	{
		mine = null;
		theirs = null;
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero == null || Campaign.Current == null)
			{
				return false;
			}
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null)
			{
				return false;
			}
			mine = playerClan.Kingdom;
			if (mine == null || mine.RulingClan != playerClan)
			{
				return false;
			}
			Clan clan = oneToOneConversationHero.Clan;
			if (clan == null)
			{
				return false;
			}
			theirs = clan.Kingdom;
			if (theirs == null || theirs == mine || theirs.IsEliminated)
			{
				return false;
			}
			if (theirs.RulingClan != clan)
			{
				return false;
			}
			if (clan.Leader != oneToOneConversationHero)
			{
				return false;
			}
			return Clients.Init();
		}
		catch
		{
			return false;
		}
	}

	private static bool CanAskSuzerainty()
	{
		try
		{
			if (!Cfg.AllowSuzerainty)
			{
				return false;
			}
			if (!ForeignRuler(out var _, out var theirs))
			{
				return false;
			}
			if (Clients.IsClient(theirs))
			{
				return false;
			}
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool CanRelease()
	{
		try
		{
			if (!Cfg.AllowSuzerainty)
			{
				return false;
			}
			if (!ForeignRuler(out var mine, out var theirs))
			{
				return false;
			}
			return object.ReferenceEquals(Clients.SuzerainOf(theirs), mine);
		}
		catch
		{
			return false;
		}
	}

	private static void Bond(int amount)
	{
		try
		{
			Hero oneToOneConversationHero = Hero.OneToOneConversationHero;
			if (oneToOneConversationHero != null)
			{
				ChangeRelationAction.ApplyPlayerRelation(oneToOneConversationHero, amount, true, true);
			}
		}
		catch
		{
		}
	}

	private static bool CanAbsorb()
	{
		try
		{
			if (!Cfg.AllowAbsorb)
			{
				return false;
			}
			if (!ForeignRuler(out var mine, out var theirs))
			{
				return false;
			}
			return object.ReferenceEquals(Clients.SuzerainOf(theirs), mine);
		}
		catch
		{
			return false;
		}
	}

	private static bool CanRevoke()
	{
		try
		{
			if (!Cfg.AllowRevoke)
			{
				return false;
			}
			if (!Sovereign(out var _, out var _, out var other))
			{
				return false;
			}
			return Bellum.TitlesHeldBy(other).Count > 0;
		}
		catch
		{
			return false;
		}
	}
}
