using System;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace WardensAndDragons;

internal static class Standing
{
	internal static int MadDread
	{
		[CompilerGenerated]
		get
		{
			return (Store.Get("hh:madplayer") == "1") ? 50 : 70;
		}
	}

	internal static int MadHonour
	{
		[CompilerGenerated]
		get
		{
			return (Store.Get("hh:madplayer") == "1") ? 45 : 25;
		}
	}

	internal static void Change(int honour, int dread, string reason)
	{
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Expected O, but got Unknown
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		if (honour == 0 && dread == 0)
		{
			return;
		}
		int honour2 = Store.Honour;
		int dread2 = Store.Dread;
		Store.Honour = Clamp(Store.Honour + honour, 0, Math.Min(100, Store.HonourCap));
		Store.Dread = Clamp(Store.Dread + dread, 0, 100);
		int num = Store.Honour - honour2;
		int num2 = Store.Dread - dread2;
		if (num == 0 && num2 == 0)
		{
			return;
		}
		string text = Fmt("Honour", num) + ((num != 0 && num2 != 0) ? ", " : "") + Fmt("Dread", num2);
		Store.AddDeed(Date() + "  " + reason + "  (" + text + ")");
		Log.Write("standing: " + reason + " -> " + text + "  now Honour " + Store.Honour + ", Dread " + Store.Dread);
		if (Cfg.Notify)
		{
			try
			{
				Color val = ((num2 > 0 || num < 0) ? Color.FromUint(4292432719u) : Color.FromUint(4286562175u));
				InformationManager.DisplayMessage(new InformationMessage(reason + ": " + text, val));
			}
			catch
			{
			}
		}
	}

	internal static void Drift()
	{
		int num = Math.Min(Cfg.StartHonour, Store.HonourCap);
		int num2 = Store.Honour;
		int num3 = Store.Dread;
		if (num2 < num)
		{
			num2 = Math.Min(num, num2 + Cfg.DriftPerSeason);
		}
		else if (num2 > num)
		{
			num2 = Math.Max(num, num2 - Cfg.DriftPerSeason);
		}
		if (num3 < Cfg.StartDread)
		{
			num3 = Math.Min(Cfg.StartDread, num3 + Cfg.DriftPerSeason);
		}
		else if (num3 > Cfg.StartDread)
		{
			num3 = Math.Max(Cfg.StartDread, num3 - Cfg.DriftPerSeason);
		}
		if (num2 != Store.Honour || num3 != Store.Dread)
		{
			Log.Write("seasonal drift: Honour " + Store.Honour + "->" + num2 + ", Dread " + Store.Dread + "->" + num3);
			Store.Honour = num2;
			Store.Dread = num3;
		}
	}

	internal static string HonourBand()
	{
		int honour = Store.Honour;
		if (honour >= 70)
		{
			return "your word is gold";
		}
		if (honour >= 50)
		{
			return "your word is trusted";
		}
		if (honour > 25)
		{
			return "your word is doubted";
		}
		return "your word is worthless";
	}

	internal static string DreadBand()
	{
		int dread = Store.Dread;
		if (dread >= 70)
		{
			return "men whisper your name and look away";
		}
		if (dread >= 60)
		{
			return "you are feared";
		}
		if (dread >= 30)
		{
			return "you are respected";
		}
		return "few have reason to fear you";
	}

	internal static bool NearMadness()
	{
		return Store.Dread >= MadDread - 10 && Store.Honour <= MadHonour + 10;
	}

	internal static bool OnMadPath()
	{
		return Store.Dread >= MadDread && Store.Honour <= MadHonour;
	}

	private static int Clamp(int v, int lo, int hi)
	{
		return (v < lo) ? lo : ((v > hi) ? hi : v);
	}

	private static string Fmt(string n, int v)
	{
		return (v == 0) ? "" : (n + " " + ((v > 0) ? "+" : "") + v);
	}

	internal static string Date()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return ((object)CampaignTime.Now).ToString();
		}
		catch
		{
			return "";
		}
	}
}
