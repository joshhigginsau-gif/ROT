using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace WardensAndDragons
{
internal static class Offer
{
	internal enum Approach
	{
		Shelter,
		Honour,
		Wealth,
		Threat
	}

	internal static Kingdom Theirs;

	internal static Kingdom Mine;

	internal static Hero TheirLeader;

	internal static int Odds;

	internal static bool Accepted;

	internal static Approach Chosen;

	private static readonly List<string> _reasons = new List<string>();

	internal static void Open(Hero hero)
	{
		try
		{
			TheirLeader = hero;
			Theirs = ((hero == null || hero.Clan == null) ? null : hero.Clan.Kingdom);
			Clan playerClan = Clan.PlayerClan;
			Mine = ((playerClan == null) ? null : playerClan.Kingdom);
			_reasons.Clear();
			Odds = 0;
			Accepted = false;
		}
		catch
		{
		}
	}

	private static int TraitFit(Approach a)
	{
		try
		{
			if (TheirLeader == null)
			{
				return 0;
			}
			switch (a)
			{
			case Approach.Shelter:
				return TheirLeader.GetTraitLevel(DefaultTraits.Valor) * -8;
			case Approach.Honour:
				return TheirLeader.GetTraitLevel(DefaultTraits.Honor) * 10;
			case Approach.Wealth:
				return TheirLeader.GetTraitLevel(DefaultTraits.Generosity) * 8;
			case Approach.Threat:
				return TheirLeader.GetTraitLevel(DefaultTraits.Valor) * -12 + TheirLeader.GetTraitLevel(DefaultTraits.Mercy) * -6;
			}
		}
		catch
		{
		}
		return 0;
	}

	internal static void Choose(Approach a)
	{
		try
		{
			Chosen = a;
			_reasons.Clear();
			if (Theirs == null || Mine == null)
			{
				Odds = 0;
				Accepted = false;
				return;
			}
			int num = Clients.Odds(Theirs, Mine, _reasons);
			int num2 = 0;
			try
			{
				if (Hero.MainHero != null)
				{
					num2 = Math.Min(25, Hero.MainHero.GetSkillValue(DefaultSkills.Charm) / 10);
					if (num2 != 0)
					{
						_reasons.Add("+" + num2 + "  your silver tongue");
					}
				}
			}
			catch
			{
			}
			num += num2;
			CultureRow cultureRow = Cultures.For(Theirs);
			if (cultureRow != null)
			{
				int num3;
				switch (a)
				{
					case Approach.Shelter: num3 = cultureRow.Shelter; break;
					case Approach.Honour: num3 = cultureRow.Honour; break;
					case Approach.Wealth: num3 = cultureRow.Wealth; break;
					default: num3 = cultureRow.Threat; break;
				}
				if (num3 != 0)
				{
					num += num3;
					_reasons.Add(((num3 <= 0) ? "" : "+") + num3 + "  " + cultureRow.Label + " answer " + Name(a) + " this way");
				}
				int num4 = cultureRow.Base;
				if (num4 < 0 && Theirs.RulingClan != null && Oaths.MarriageTie(Theirs.RulingClan))
				{
					num4 /= 2;
					_reasons.Add("   a marriage between your houses softens their pride");
				}
				if (num4 != 0)
				{
					num += num4;
					_reasons.Add(((num4 <= 0) ? "" : "+") + num4 + "  " + cultureRow.Label + " pride");
				}
			}
			if (a == Approach.Threat)
			{
				int num5 = Dragons.LivingRidersIn(Clan.PlayerClan);
				int num6 = Math.Min(Cfg.DragonThreatCap, num5 * Cfg.DragonThreatPerRider);
				if (num6 > 0)
				{
					num += num6;
					_reasons.Add("+" + num6 + "  your house has " + num5 + " dragon" + ((num5 != 1) ? "s" : ""));
				}
			}
			int num7 = TraitFit(a);
			if (num7 != 0)
			{
				_reasons.Add(((num7 < 0) ? "" : "+") + num7 + "  " + Name(a) + " suits their character");
			}
			num += num7;
			if (a == Approach.Threat)
			{
				num += 15;
				_reasons.Add("+15  naked force concentrates the mind");
			}
			Odds = Math.Max(3, Math.Min(95, num));
			Accepted = Cfg.SuzeraintyAlwaysAccepted || MBRandom.RandomInt(100) < Odds;
			Log.Write(string.Concat("suzerainty offer to ", Theirs.Name, " via ", Name(a), ": odds=", Odds, " accepted=", Accepted));
			foreach (string reason in _reasons)
			{
				Log.Write("    " + reason);
			}
			if (Accepted)
			{
				if (!Clients.Establish(Theirs, Mine, out var report))
				{
					Log.Write("establish failed after acceptance: " + report);
					Flow.Notify("They agreed, but it could not be set: " + (report ?? "no reason given"));
					Accepted = false;
				}
				else
				{
					Log.Write("client kingdom established: " + Theirs.Name);
					OathKind kind = ((a != Approach.Threat) ? OathKind.Fealty : OathKind.Duress);
					Log.Write(Oaths.SetForKingdom(Theirs, kind, quiet: true));
				}
				return;
			}
			try
			{
				if (TheirLeader != null)
				{
					ChangeRelationAction.ApplyPlayerRelation(TheirLeader, -5, true, true);
				}
			}
			catch
			{
			}
		}
		catch (Exception ex)
		{
			Log.Write("offer resolve failed: " + ex.Message);
			Odds = 0;
			Accepted = false;
		}
	}

	private static string Name(Approach a)
	{
		switch (a)
		{
			case Approach.Shelter: return "an offer of shelter";
			case Approach.Honour: return "an appeal to honour";
			case Approach.Wealth: return "a promise of wealth";
			default: return "a threat";
		}
	}

	internal static TextObject Breakdown()
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		string text = "";
		foreach (string reason in _reasons)
		{
			text = text + reason + "\n";
		}
		return new TextObject(text.Trim(), (Dictionary<string, object>)null);
	}

	internal static void ShowBreakdown()
	{
		Flow.Notify("Chance was " + Odds + "%. " + string.Join("  ", _reasons.ToArray()));
	}
}
}
