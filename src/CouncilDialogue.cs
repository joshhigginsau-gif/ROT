using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
	// Speaking to a councillor at the table in your hall.
	internal static class CouncilDialogue
	{
		private static Action _pending;

		private static string Office()
		{
			Hero h = Hero.OneToOneConversationHero;
			return (h != null && Council.SittingHere(h)) ? Council.OfficeOf(h) : null;
		}

		private static bool Is(string office)
		{
			return Office() == office;
		}

		private static void Later(Action a)
		{
			_pending = a;
		}

		internal static void Add(CampaignGameStarter s)
		{
			s.AddPlayerLine("wad_cn_start", "hero_main_options", "wad_cn_open", "{=WAD_CnStart}There is council business.",
				(ConversationSentence.OnConditionDelegate)(() => Cfg.Council && Office() != null), null, 120, null, null);

			s.AddDialogLine("wad_cn_open", "wad_cn_open", "wad_cn_opts", "{=!}{WAD_CN_GREET}", (ConversationSentence.OnConditionDelegate)delegate
			{
				Hero h = Hero.OneToOneConversationHero;
				bool warm = h != null && h.GetRelationWithPlayer() > 0f;
				string text;
				switch (Office())
				{
				case Council.Ships:
					text = warm ? "The ships and the swords are yours to command, Your Grace. Say where." : "If you must have the host, say where, and quickly.";
					break;
				case Council.Coin:
					text = warm ? "The treasury is... a subject, Your Grace. What would you have of it?" : "More gold, I suppose.";
					break;
				case Council.Hand:
					text = warm ? "I have kept the realm while you rode. It has missed you." : "The realm stands. Barely, and no thanks to anyone at this table.";
					break;
				case Council.Laws:
					text = warm ? "The law is patient, Your Grace. It waits on you." : "The law waits. It has been waiting some time.";
					break;
				case Council.Whisperers:
					text = warm ? "Walls have ears, Your Grace. Mine, mostly." : "What is it you want to know?";
					break;
				default:
					text = warm ? "The ravens come and go, Your Grace. Most bring nothing worth a candle." : "The Citadel has nothing to say to you today.";
					break;
				}
				MBTextManager.SetTextVariable("WAD_CN_GREET", text, false);
				return true;
			}, null, 120, null);

			Line(s, "wad_cn_muster", "Muster a host of our own - gold for men, under one of my knights.", Council.Ships, delegate { Later(Host.Muster); });
			Line(s, "wad_cn_banners", "Call the realm's lords to the banners. I want a castle taken.", Council.Ships, delegate { Later(Council.CallBanners); });
			Line(s, "wad_cn_down", "Stand the realm's lords down.", Council.Ships, delegate { Later(Council.StandDownBanners); }, () => Council.CouncilArmy() != null);
			Line(s, "wad_cn_hosts", "My hosts in the field - I have orders for them.", Council.Ships, delegate { Later(Host.Pick); }, () => Host.Mine().Count > 0 || Muster.Mine().Count > 0);
			Line(s, "wad_cn_bank", "What do we owe the Iron Bank?", Council.Coin, delegate { Later(delegate { Ravens.Popup("The Master of Coin", IronBank.Summary()); }); });
			Line(s, "wad_cn_levy", "Raise a levy on the towns.", Council.Coin, delegate { Later(Council.Levy); });
			Line(s, "wad_cn_realm", "How stands the realm?", Council.Hand, delegate { Later(delegate { Ravens.Popup("The Hand's Report", Council.Realm()); }); });
			Line(s, "wad_cn_laws", "What waits for judgement?", Council.Laws, delegate { Later(delegate { Ravens.Popup("The Master of Laws", Council.Judgement()); }); });
			Line(s, "wad_cn_ravens", "What do the ravens say?", Council.Whisperers, delegate { Later(delegate { Ravens.Popup("The Master of Whisperers", Council.Ravenry()); }); });
			Line(s, "wad_cn_word", "Any word?", Council.Maester, delegate { Later(delegate { Ravens.Popup("The Grand Maester", Council.Health()); }); });

			s.AddPlayerLine("wad_cn_done", "wad_cn_opts", "close_window", "{=WAD_CnDone}That is all.", null, null, 1, null, null);

			s.AddDialogLine("wad_cn_ack", "wad_cn_ack", "close_window", "{=WAD_CnAck}As you command, Your Grace.", null, (ConversationSentence.OnConsequenceDelegate)delegate
			{
				Action a = _pending;
				_pending = null;
				if (a != null)
				{
					Campaign.Current.ConversationManager.ConversationEndOneShot += a;
				}
			}, 120, null);
		}

		private static void Line(CampaignGameStarter s, string id, string text, string office, Action act, Func<bool> also = null)
		{
			s.AddPlayerLine(id, "wad_cn_opts", "wad_cn_ack", "{=!}" + text, (ConversationSentence.OnConditionDelegate)delegate
			{
				try
				{
					return Is(office) && (also == null || also());
				}
				catch
				{
					return false;
				}
			}, (ConversationSentence.OnConsequenceDelegate)delegate
			{
				act();
			}, 110, null, null);
		}
	}
}
