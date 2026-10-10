using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// The Iron Bank of Braavos.
	//
	// It will lend to anyone it expects to be paid by - enough for a host a
	// house could never otherwise afford - and it is always paid. Miss an
	// instalment and the debt grows; miss two and the Bank stops asking and
	// starts investing, in whoever is most likely to take the money out of
	// your hide. Rulers borrow from it too, and when they cannot pay, the
	// Bank comes looking for someone to finance their fall.
	internal static class IronBank
	{
		// principal | owed | instalment | next due | payments left | missed | last funding
		private const string LoanKey = "ib:loan";
		private const string StandingKey = "ib:standing";
		private const string AiPrefix = "ib:ai:";

		// ------------------------------------------------------------------
		// the Bank's opinion of you, -100 to 100

		internal static int Standing
		{
			get
			{
				return Store.GetI(StandingKey, 0);
			}
			set
			{
				Store.SetI(StandingKey, Math.Max(-100, Math.Min(100, value)));
			}
		}

		internal static string Opinion()
		{
			int s = Standing;
			return (s >= 50) ? "a valued client" : ((s >= 15) ? "in good standing" : ((s > -15) ? "known to the Bank" : ((s > -50) ? "a poor risk" : "in default")));
		}

		private static string[] Loan()
		{
			string[] a = (Store.Get(LoanKey) ?? "").Split('|');
			return (a.Length >= 7) ? a : null;
		}

		private static void Put(string[] a)
		{
			Store.Set(LoanKey, (a == null) ? null : string.Join("|", a));
		}

		internal static bool InDebt
		{
			get
			{
				return Loan() != null;
			}
		}

		internal static int Owed
		{
			get
			{
				string[] a = Loan();
				int o = 0;
				if (a != null)
				{
					int.TryParse(a[1], out o);
				}
				return o;
			}
		}

		internal static int Limit()
		{
			if (Standing <= -50)
			{
				return 0;
			}
			long limit = Cfg.BankBaseCredit;
			try
			{
				Clan c = Clan.PlayerClan;
				limit += (long)c.Tier * Cfg.BankCreditPerTier;
				foreach (Settlement s in c.Settlements)
				{
					if (s.IsTown)
					{
						limit += (long)(s.Town.Prosperity * 30f);
					}
					else if (s.IsCastle)
					{
						limit += 75000;
					}
				}
				limit += (long)(c.Renown * 20f);
			}
			catch
			{
			}
			limit = limit * (100 + Standing) / 100;
			return (int)Math.Max(0L, Math.Min(int.MaxValue / 2, limit));
		}

		internal static int Rate()
		{
			return Math.Max(5, Math.Min(40, Cfg.BankRate - Standing / 10));
		}

		// ------------------------------------------------------------------
		// borrowing

		internal static string CanBorrow()
		{
			if (!Cfg.Bank)
			{
				return "The Iron Bank is turned off.";
			}
			if (InDebt)
			{
				return "You already owe the Bank " + Owed.ToString("N0") + ". It lends once.";
			}
			if (Limit() < 10000)
			{
				return "The Bank will not lend to you. It remembers.";
			}
			return null;
		}

		internal static void Borrow()
		{
			string why = CanBorrow();
			if (why != null)
			{
				Flow.Notify(why);
				return;
			}
			int limit = Limit();
			int rate = Rate();
			Inquiry.Text("The Iron Bank", "The Bank's man looks at your ledgers, and at you.\n\nIt will lend up to " + limit.ToString("N0") + " gold at " + rate + "%, as you are " + Opinion() + ". How much?",
				Math.Min(limit, 1000000).ToString(), "That much", "Not today",
				delegate(string text)
				{
					long want;
					if (!long.TryParse((text ?? "").Replace(",", "").Replace(".", "").Trim(), out want) || want <= 0)
					{
						Flow.Notify("That is not a sum of gold.");
						return;
					}
					int sum = (int)Math.Min((long)limit, want);
					List<InquiryElement> els = new List<InquiryElement>();
					foreach (int n in new int[3] { 4, 8, 12 })
					{
						int owed = (int)Math.Min(int.MaxValue / 2, (long)sum * (100 + rate) / 100);
						els.Add(new InquiryElement(n, n + " payments of " + (owed / n).ToString("N0") + ", one every " + Cfg.BankPaymentDays + " days", null, true, ""));
					}
					Inquiry.Select("The Iron Bank", sum.ToString("N0") + " gold. " + ((long)sum * (100 + rate) / 100).ToString("N0") + " to repay. Over how long?", els, 1, 1, "Sign", "Not today",
						delegate(List<InquiryElement> chosen)
						{
							int n = (chosen != null && chosen.Count > 0) ? (int)chosen[0].Identifier : 8;
							Take(sum, rate, n);
						});
				}, null);
		}

		private static void Take(int sum, int rate, int payments)
		{
			if (InDebt)
			{
				return;
			}
			int owed = (int)Math.Min(int.MaxValue / 2, (long)sum * (100 + rate) / 100);
			int each = (owed + payments - 1) / payments;
			Put(new string[7] { sum.ToString(), owed.ToString(), each.ToString(), (CourtBehavior.Today() + Cfg.BankPaymentDays).ToString(), payments.ToString(), "0", "-9999" });
			Hero.MainHero.ChangeHeroGold(sum);
			Store.AddDeed(Standing2() + "  Borrowed " + sum.ToString("N0") + " from the Iron Bank of Braavos.");
			Log.Write("iron bank: borrowed " + sum + " at " + rate + "%, " + payments + " x " + each);
			Ravens.Popup("The Iron Bank", "The gold is counted out: " + sum.ToString("N0") + ". The first of " + payments + " payments of " + each.ToString("N0") + " is due in " + Cfg.BankPaymentDays + " days.\n\nThe Iron Bank will have its due.");
		}

		private static string Standing2()
		{
			return WardensAndDragons.Standing.Date();
		}

		internal static void PayNext()
		{
			string[] a = Loan();
			if (a == null)
			{
				return;
			}
			int owed;
			int each;
			int.TryParse(a[1], out owed);
			int.TryParse(a[2], out each);
			int pay = Math.Min(owed, each);
			if (Hero.MainHero.Gold < pay)
			{
				Flow.Notify("You cannot pay " + pay.ToString("N0") + ".");
				return;
			}
			Paid(a, pay, true);
		}

		internal static void PayAll()
		{
			string[] a = Loan();
			if (a == null)
			{
				return;
			}
			int owed;
			int.TryParse(a[1], out owed);
			if (Hero.MainHero.Gold < owed)
			{
				Flow.Notify("You cannot pay " + owed.ToString("N0") + ".");
				return;
			}
			Paid(a, owed, true);
		}

		private static void Paid(string[] a, int pay, bool early)
		{
			int owed;
			int left;
			int.TryParse(a[1], out owed);
			int.TryParse(a[4], out left);
			Hero.MainHero.ChangeHeroGold(-pay);
			owed -= pay;
			left = Math.Max(0, left - 1);
			if (owed <= 0)
			{
				Put(null);
				Standing += 10;
				Store.AddDeed(Standing2() + "  Paid the Iron Bank in full.");
				Log.Write("iron bank: paid in full");
				Ravens.Popup("The Iron Bank", "The debt is paid. The Bank's man bows, exactly as deeply as before. You are " + Opinion() + ".");
				return;
			}
			a[1] = owed.ToString();
			a[4] = Math.Max(1, left).ToString();
			a[3] = (CourtBehavior.Today() + Cfg.BankPaymentDays).ToString();
			Put(a);
			Standing += early ? 3 : 2;
			Flow.Notify("Paid the Iron Bank " + pay.ToString("N0") + ". " + owed.ToString("N0") + " still owed.");
		}

		// ------------------------------------------------------------------
		// the clock

		internal static void Daily()
		{
			try
			{
				if (!Cfg.Bank || !Store.Initialized)
				{
					return;
				}
				AiDaily();
				string[] a = Loan();
				if (a == null)
				{
					return;
				}
				int today = CourtBehavior.Today();
				int due;
				int.TryParse(a[3], out due);
				int missed;
				int.TryParse(a[5], out missed);
				int funded;
				int.TryParse(a[6], out funded);
				if (today >= due)
				{
					int owed;
					int each;
					int.TryParse(a[1], out owed);
					int.TryParse(a[2], out each);
					int pay = Math.Min(owed, each);
					if (Hero.MainHero.Gold >= pay)
					{
						Paid(a, pay, false);
						return;
					}
					missed++;
					int penalty = (int)((long)owed * Cfg.BankPenaltyPercent / 100);
					owed += penalty;
					a[1] = owed.ToString();
					a[3] = (today + Cfg.BankPaymentDays).ToString();
					a[5] = missed.ToString();
					Put(a);
					Standing -= 15;
					Log.Write("iron bank: payment missed (" + missed + "), owed " + owed);
					Store.AddDeed(Standing2() + "  Missed a payment to the Iron Bank.");
					if (missed == 1)
					{
						Ravens.Popup("A Letter from Braavos", "The Bank notes, with regret, that the payment of " + pay.ToString("N0") + " did not arrive. The debt stands at " + owed.ToString("N0") +
							", which includes a small consideration for the Bank's patience.\n\nThe Bank's patience is not large.");
					}
					else if (missed >= 3 && Standing > -50)
					{
						Standing = -50;
					}
				}
				// Two missed: the Bank invests in your enemies, every six weeks
				// until it is paid.
				if (missed >= 2 && today - funded >= Cfg.BankFundEveryDays)
				{
					a[6] = today.ToString();
					Put(a);
					Punish();
				}
			}
			catch (Exception e)
			{
				Log.Once("ibdaily", "the Iron Bank's tick failed: " + e.Message);
			}
		}

		// The Bank funds whoever is best placed to hurt you.
		private static void Punish()
		{
			IFaction mine = Clan.PlayerClan.MapFaction;
			List<Kingdom> foes = Kingdom.All.Where((Kingdom k) => !k.IsEliminated && k.Leader != null && k.Leader.IsAlive && k.RulingClan != Clan.PlayerClan && mine != null && FactionManager.IsAtWarAgainstFaction(k, mine))
				.OrderByDescending((Kingdom k) => k.Clans.Sum((Clan c) => c.Tier)).ToList();
			int budget = Math.Min(Owed, Cfg.BankFundCap);
			if (foes.Count == 0)
			{
				string[] a = Loan();
				if (a != null)
				{
					int owed;
					int.TryParse(a[1], out owed);
					owed += (int)((long)owed * Cfg.BankPenaltyPercent / 100);
					a[1] = owed.ToString();
					Put(a);
				}
				Ravens.Popup("A Letter from Braavos", "The Bank finds, to its regret, that you have no enemies worth investing in. It is patient. Its interest is not: the debt now stands at " + Owed.ToString("N0") + ".");
				return;
			}
			Kingdom k2 = foes.FirstOrDefault((Kingdom k) => Host.HostsAndMustersOf(k) < Math.Max(1, Cfg.AiHostMaxPerRealm) + 1) ?? foes[0];
			if (Host.HostsAndMustersOf(k2) >= Math.Max(1, Cfg.AiHostMaxPerRealm) + 1)
			{
				Log.Write("iron bank: every foe already fields its hosts - the Bank waits");
				return;
			}
			bool raised = Host.AiRaise(k2, k2.Leader, true, budget, MobileParty.MainParty);
			Log.Write("iron bank: funding " + k2.Name + " against you with " + budget + (raised ? "" : " (no host could be raised)"));
			Ravens.Popup("The Iron Bank Will Have Its Due", "A letter from Braavos, very polite: the Bank has lent " + budget.ToString("N0") + " to " + k2.Leader.Name + " of " + k2.Name + ", on the understanding that it will be spent on you.\n\n" +
				(raised ? ((Cfg.HostMusterDays > 0) ? ("The summons have gone out; their host will stand in about " + Cfg.HostMusterDays + " days.") : "Their host is already marching.") : "They have not yet found a lord to lead it. They will.") + "\n\nIt will go on doing this until you pay. You owe " + Owed.ToString("N0") + ".");
		}

		// ------------------------------------------------------------------
		// rulers borrow too

		// Abdication: the crown owes it, not the house that left. The player's
		// loan becomes the realm's (or the heir's, with no realm).
		internal static void HandToCrown(Kingdom realm, Hero heir)
		{
			try
			{
				if (!InDebt)
				{
					return;
				}
				int owed = Owed;
				Store.Set(LoanKey, null);
				if (realm != null)
				{
					Store.Set(AiPrefix + ((MBObjectBase)realm).StringId, owed + "|" + (CourtBehavior.Today() + Cfg.BankAiDays));
					Log.Write("abdication: the Iron Bank loan (" + owed + ") is now owed by " + realm.Name + ", due in " + Cfg.BankAiDays + " days");
				}
				else
				{
					Log.Write("abdication: the Iron Bank loan (" + owed + ") goes with the house to " + ((heir != null) ? heir.Name.ToString() : "the heir") + "; the Bank no longer looks to you for it");
				}
			}
			catch (Exception e)
			{
				Log.Write("abdication: the loan could not be handed to the crown: " + e.Message);
			}
		}

		// A ruler short of gold for a host; returns what the Bank lent.
		internal static int AiBorrow(Kingdom k, Hero ruler, int want)
		{
			try
			{
				if (!Cfg.Bank || !Cfg.BankAi || k == null || ruler == null || want <= 0)
				{
					return 0;
				}
				string key = AiPrefix + ((MBObjectBase)k).StringId;
				if (!string.IsNullOrEmpty(Store.Get(key)) || MBRandom.RandomInt(100) >= 50)
				{
					return 0;
				}
				int sum = Math.Min(want, Cfg.BankAiLoanCap);
				int owed = (int)((long)sum * (100 + Cfg.BankRate) / 100);
				ruler.ChangeHeroGold(sum);
				Store.Set(key, owed + "|" + (CourtBehavior.Today() + Cfg.BankAiDays));
				Log.Write("iron bank: lent " + sum + " to " + k.Name + ", " + owed + " due in " + Cfg.BankAiDays + " days");
				return sum;
			}
			catch
			{
				return 0;
			}
		}

		private static void AiDaily()
		{
			if (!Cfg.BankAi)
			{
				return;
			}
			int today = CourtBehavior.Today();
			foreach (string key in Store.Keys(AiPrefix))
			{
				string[] a = (Store.Get(key) ?? "").Split('|');
				int owed;
				int due;
				if (a.Length < 2 || !int.TryParse(a[0], out owed) || !int.TryParse(a[1], out due) || today < due)
				{
					continue;
				}
				string id = key.Substring(AiPrefix.Length);
				Kingdom k = Kingdom.All.FirstOrDefault((Kingdom x) => ((MBObjectBase)x).StringId == id);
				Store.Set(key, null);
				if (k == null || k.IsEliminated || k.Leader == null)
				{
					continue;
				}
				if (k.Leader.Gold >= owed)
				{
					k.Leader.ChangeHeroGold(-owed);
					Log.Write("iron bank: " + k.Name + " repaid " + owed);
					continue;
				}
				Log.Write("iron bank: " + k.Name + " defaulted on " + owed);
				Defaulted(k, owed);
			}
		}

		// A ruler who cannot pay: the Bank looks for someone to finance
		// their fall - you, if you are at war with them.
		private static void Defaulted(Kingdom k, int owed)
		{
			IFaction mine = Clan.PlayerClan.MapFaction;
			int sum = Math.Min(owed, Cfg.BankFundCap);
			if (mine != null && FactionManager.IsAtWarAgainstFaction(k, mine) && !InDebt)
			{
				int rate = Math.Max(3, Cfg.BankRate / 4);
				Inquiry.Confirm("A Letter from Braavos", k.Leader.Name + " of " + k.Name + " has not paid the Iron Bank, and the Bank would like very much to see " + k.Name +
					" in the hands of someone who will.\n\nIt offers you " + sum.ToString("N0") + " gold at " + rate + "%, over eight payments, for the war.", "Take the gold", "Decline",
					delegate
					{
						if (!InDebt)
						{
							Take(sum, rate, 8);
							if (InDebt)
							{
								Standing += 5;
							}
						}
					}, null);
				return;
			}
			Kingdom foe = Kingdom.All.Where((Kingdom x) => x != k && !x.IsEliminated && x.RulingClan != Clan.PlayerClan && x.Leader != null && x.Leader.IsAlive && FactionManager.IsAtWarAgainstFaction(x, k))
				.OrderByDescending((Kingdom x) => x.Clans.Sum((Clan c) => c.Tier)).FirstOrDefault();
			if (foe != null)
			{
				Host.AiRaise(foe, foe.Leader, true, sum, null);
				Flow.Notify(k.Name + " has not paid the Iron Bank. The Bank has paid " + foe.Name + " to see to it.");
			}
		}

		// ------------------------------------------------------------------
		// the court

		internal static string Summary()
		{
			StringBuilder sb = new StringBuilder();
			string[] a = Loan();
			if (a == null)
			{
				sb.Append("You owe the Bank nothing. It would lend you up to ").Append(Limit().ToString("N0")).Append(" gold at ").Append(Rate()).Append("%.\n");
			}
			else
			{
				int owed;
				int each;
				int due;
				int left;
				int missed;
				int.TryParse(a[1], out owed);
				int.TryParse(a[2], out each);
				int.TryParse(a[3], out due);
				int.TryParse(a[4], out left);
				int.TryParse(a[5], out missed);
				sb.Append("You owe the Iron Bank ").Append(owed.ToString("N0")).Append(". The next payment of ").Append(Math.Min(owed, each).ToString("N0"))
				  .Append(" is due in ").Append(Math.Max(0, due - CourtBehavior.Today())).Append(" days (").Append(left).Append(" left).\n");
				if (missed > 0)
				{
					sb.Append("Missed payments: ").Append(missed).Append((missed >= 2) ? " - the Bank is funding your enemies.\n" : " - one more and the Bank stops asking.\n");
				}
			}
			sb.Append("The Bank considers you ").Append(Opinion()).Append(".");
			return sb.ToString();
		}

		internal static string Attention()
		{
			string[] a = Loan();
			if (!Cfg.Bank || a == null)
			{
				return null;
			}
			int due;
			int.TryParse(a[3], out due);
			int days = due - CourtBehavior.Today();
			return (days <= 5) ? ("A payment to the Iron Bank is due in " + Math.Max(0, days) + " days.") : null;
		}

		// Cheat: the next payment is due today.
		internal static string DueNow()
		{
			string[] a = Loan();
			if (a == null)
			{
				return "You owe the Bank nothing.";
			}
			a[3] = CourtBehavior.Today().ToString();
			Put(a);
			return "The next payment is due today.";
		}
	}
}
