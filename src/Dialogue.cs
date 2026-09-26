using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;

namespace WardensAndDragons
{
internal static class Dialogue
{
	internal static void Add(CampaignGameStarter s)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_0115: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Expected O, but got Unknown
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected O, but got Unknown
		//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Expected O, but got Unknown
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_0232: Expected O, but got Unknown
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Expected O, but got Unknown
		//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Expected O, but got Unknown
		//IL_02df: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e9: Expected O, but got Unknown
		//IL_035c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Expected O, but got Unknown
		//IL_0399: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a3: Expected O, but got Unknown
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e0: Expected O, but got Unknown
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_041d: Expected O, but got Unknown
		//IL_04af: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b9: Expected O, but got Unknown
		//IL_04ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f6: Expected O, but got Unknown
		//IL_0529: Unknown result type (might be due to invalid IL or missing references)
		//IL_0533: Expected O, but got Unknown
		//IL_0566: Unknown result type (might be due to invalid IL or missing references)
		//IL_0570: Expected O, but got Unknown
		//IL_0602: Unknown result type (might be due to invalid IL or missing references)
		//IL_060c: Expected O, but got Unknown
		//IL_061f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0629: Expected O, but got Unknown
		s.AddPlayerLine("wardens_offer", "lord_talk_speak_diplomacy_2", "wardens_reply", "{=Wardens_Offer}Kneel. I would name you Warden of a realm, to hold in my name.", new ConversationSentence.OnConditionDelegate(CanName), (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Flow.Begin(Hero.OneToOneConversationHero);
		}, 110, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_reply", "wardens_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_Reply}You honour my house beyond deserving. Name the realm and I shall hold it.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 110, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_revoke", "lord_talk_speak_diplomacy_2", "wardens_revoke_reply", "{=Wardens_Revoke}You have held your wardenship poorly. I mean to take it back.", new ConversationSentence.OnConditionDelegate(CanRevoke), (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Flow.BeginRevoke(Hero.OneToOneConversationHero);
		}, 109, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_revoke_reply", "wardens_revoke_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_RevokeReply}You would strip my house of what was given? Choose, then.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 109, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_client_offer", "lord_talk_speak_diplomacy_2", "wardens_client_reply", "{=Wardens_ClientOffer}Rule your realm as my Warden. Keep your crown, your lands and your laws - but my banner flies above them.", new ConversationSentence.OnConditionDelegate(CanAskSuzerainty), (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.Open(Hero.OneToOneConversationHero);
		}, 108, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_client_reply", "wardens_client_reply", "wardens_persuade", "{=Wardens_ClientReply}You ask me to bend the knee and call it honour. Say your piece.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 108, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_client_release", "lord_talk_speak_diplomacy_2", "wardens_client_release_reply", "{=Wardens_ClientRelease}I release your realm from its oath. Rule free of me.", new ConversationSentence.OnConditionDelegate(CanRelease), (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Suzerainty.Release(Hero.OneToOneConversationHero);
		}, 107, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_client_release_reply", "wardens_client_release_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_ClientReleaseReply}Freely given? My house will remember it.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 107, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_client_reply", "wardens_client_reply", "wardens_offer_how", "{=Wardens_ClientReply}You ask me to bend the knee and call it honour. On what grounds?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 108, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_how_shelter", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Shelter}Your enemies gather. Under my banner they must face me first.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Shelter);
		}, 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_honour", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Honour}I do not ask for your crown. Your house keeps its name, its halls and its laws.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Honour);
		}, 99, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_wealth", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Wealth}My roads and my markets would be yours. Your people would grow fat on it.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Wealth);
		}, 98, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_threat", "wardens_offer_how", "wardens_offer_result", "{=Wardens_How_Threat}Kneel now and keep your realm whole. Refuse, and I will take it in pieces.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.Choose(Offer.Approach.Threat);
		}, 97, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_how_never_mind", "wardens_offer_how", "wardens_offer_dropped", "{=Wardens_How_Drop}Forget I spoke. The moment is wrong.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 90, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_offer_dropped", "wardens_offer_dropped", "lord_talk_speak_diplomacy_2", "{=Wardens_Dropped}It is already forgotten.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_offer_yes", "wardens_offer_result", "wardens_after_yes", "{=Wardens_Yes}...Then let it be so. My realm holds of yours. My banners answer your call, and my enemies are yours to name. See that you are worth the kneeling.", (ConversationSentence.OnConditionDelegate)(() => Offer.Accepted), (ConversationSentence.OnConsequenceDelegate)null, 120, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_yes_gracious", "wardens_after_yes", "wardens_yes_gracious_reply", "{=Wardens_Yes_Gracious}You lose nothing today but a title on a map. You have my word on it.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Bond(8);
		}, 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_yes_cold", "wardens_after_yes", "wardens_yes_cold_reply", "{=Wardens_Yes_Cold}You kneel because you must. Do not mistake it for friendship.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Bond(-6);
		}, 99, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_yes_why", "wardens_after_yes", "wardens_yes_why_reply", "{=Wardens_Yes_Why}What decided you, in the end?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.ShowBreakdown();
		}, 98, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_yes_gracious_reply", "wardens_yes_gracious_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_Yes_GraciousR}Words are cheap until they are kept. I shall watch which sort yours are.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_yes_cold_reply", "wardens_yes_cold_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_Yes_ColdR}No. I shall not mistake it. Nor forget it.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_yes_why_reply", "wardens_yes_why_reply", "wardens_after_yes", "{=Wardens_Yes_WhyR}A ruler weighs what is before them and calls it wisdom afterwards. Ask me again in ten years.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_offer_no", "wardens_offer_result", "wardens_after_no", "{=Wardens_No}No. My forebears did not bleed for this realm so that I might hand it across a table. My crown is my own, and so it stays.", (ConversationSentence.OnConditionDelegate)(() => !Offer.Accepted), (ConversationSentence.OnConsequenceDelegate)null, 119, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_no_grace", "wardens_after_no", "wardens_no_grace_reply", "{=Wardens_No_Grace}Then keep it, and keep my respect with it. The offer stands when you need it.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Bond(5);
		}, 100, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_no_threat", "wardens_after_no", "wardens_no_threat_reply", "{=Wardens_No_Threat}Remember this hour when my banners are on your border.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Bond(-10);
		}, 99, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddPlayerLine("wardens_no_why", "wardens_after_no", "wardens_no_why_reply", "{=Wardens_No_Why}What would it have taken?", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Offer.ShowBreakdown();
		}, 98, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_no_grace_reply", "wardens_no_grace_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_No_GraceR}That is more graciously said than I expected. It will be remembered - which is not the same as accepted.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_no_threat_reply", "wardens_no_threat_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_No_ThreatR}I shall. And my walls will remember it with me.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddDialogLine("wardens_no_why_reply", "wardens_no_why_reply", "wardens_after_no", "{=Wardens_No_WhyR}Strength, mostly. And the sense that you would keep your word. Bring me more of both.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 100, (ConversationSentence.OnClickableConditionDelegate)null);
		s.AddPlayerLine("wardens_absorb", "lord_talk_speak_diplomacy_2", "wardens_absorb_reply", "{=Wardens_Absorb}The time has come. Your realm should be part of mine in name as well as fact.", new ConversationSentence.OnConditionDelegate(CanAbsorb), (ConversationSentence.OnConsequenceDelegate)delegate
		{
			Absorb.Offer(Hero.OneToOneConversationHero);
		}, 106, (ConversationSentence.OnClickableConditionDelegate)null, (ConversationSentence.OnPersuasionOptionDelegate)null);
		s.AddDialogLine("wardens_absorb_reply", "wardens_absorb_reply", "lord_talk_speak_diplomacy_2", "{=Wardens_AbsorbReply}You would end my realm and call it honour. Speak your terms.", (ConversationSentence.OnConditionDelegate)null, (ConversationSentence.OnConsequenceDelegate)null, 106, (ConversationSentence.OnClickableConditionDelegate)null);
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
			if (!Sovereign(out var player, out var _d1, out var _d2))
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
			if (!Sovereign(out var _d3, out var _d4, out var other))
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
}
