using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Oaths
{
	internal static readonly List<OathDef> All = new List<OathDef>
	{
		new OathDef
		{
			Kind = OathKind.Exemption,
			Name = "Oath of Exemption",
			Level = "Exemption",
			Burden = 0,
			LibertyPerYear = -10,
			TributePerFief = () => 0,
			Blurb = "Independence in all but name. Content, and yields nothing."
		},
		new OathDef
		{
			Kind = OathKind.Sword,
			Name = "Oath of the Sword",
			Level = "Lessened",
			Burden = 1,
			LibertyPerYear = 2,
			TributePerFief = () => 0,
			Blurb = "Swords instead of gold. They answer every call to war."
		},
		new OathDef
		{
			Kind = OathKind.Marriage,
			Name = "Marriage Oath",
			Level = "Lessened",
			Burden = 2,
			FlatLiberty = -20,
			TributePerFief = () => Cfg.TributeMarriage,
			Blurb = "Bound by blood. The steadiest bond there is. Needs a marriage between the houses."
		},
		new OathDef
		{
			Kind = OathKind.Fealty,
			Name = "Oath of Fealty",
			Level = "CustomaryTenure",
			Burden = 3,
			LibertyPerYear = 0,
			TributePerFief = () => Cfg.TributeFealty,
			Blurb = "The ordinary bond. Modest tribute, their host when called."
		},
		new OathDef
		{
			Kind = OathKind.Tribute,
			Name = "Oath of Tribute",
			Level = "Elevated",
			Burden = 4,
			LibertyPerYear = 5,
			TributePerFief = () => Cfg.TributeTribute,
			Blurb = "Gold instead of swords. Heavy dues, slow resentment."
		},
		new OathDef
		{
			Kind = OathKind.Duress,
			Name = "Oath under Duress",
			Level = "Extortion",
			Burden = 5,
			LibertyPerYear = 15,
			TributePerFief = () => Cfg.TributeDuress,
			Blurb = "Sworn at swordpoint. They pay a great deal and hate you for it."
		}
	};

	private static FieldInfo _libField;

	private static FieldInfo _reasonsField;

	private static ConstructorInfo _reasonCtor;

	private static bool _mapped;

	internal static OathDef Def(OathKind k)
	{
		return All.FirstOrDefault((OathDef o) => o.Kind == k);
	}

	internal static void Choose(string title, string desc, Clan clan, Kingdom realm, Action<OathKind> done)
	{
		//IL_021d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Expected O, but got Unknown
		int num = ((clan != null) ? ((clan.Fiefs != null) ? ((List<Town>)(object)clan.Fiefs).Count : 0) : ((realm != null && realm.Fiefs != null) ? ((List<Town>)(object)realm.Fiefs).Count : 0));
		OathKind oathKind = ((clan != null) ? Of(clan) : Of(realm));
		Clan other = clan ?? ((realm != null) ? realm.RulingClan : null);
		List<InquiryElement> list = new List<InquiryElement>();
		foreach (OathDef item in All)
		{
			bool flag = item.Kind != OathKind.Marriage || MarriageTie(other);
			int num2 = item.TributePerFief() * num;
			string text = ((realm == null) ? "" : ((item.FlatLiberty != 0) ? (", liberty " + item.FlatLiberty) : ((item.LibertyPerYear != 0) ? (", liberty " + ((item.LibertyPerYear > 0) ? "+" : "") + item.LibertyPerYear + " a year") : "")));
			string text2 = item.Name + ((item.Kind == oathKind) ? "  (current)" : "") + " - " + ((num2 > 0) ? (num2.ToString("N0") + " a year") : "no tribute") + text;
			string text3 = item.Blurb + (flag ? "" : " No marriage binds your houses.");
			list.Add(new InquiryElement((object)item.Kind, text2, (ImageIdentifier)null, flag, text3));
		}
		Inquiry.Select(title, desc, list, 1, 1, "Swear", "Skip", delegate(List<InquiryElement> chosen)
		{
			done((chosen != null && chosen.Count > 0) ? ((OathKind)chosen[0].Identifier) : OathKind.None);
		}, delegate
		{
			done(OathKind.None);
		});
	}

	private static string CK(Clan c)
	{
		return "oath:c:" + ((MBObjectBase)c).StringId;
	}

	private static string KK(Kingdom k)
	{
		return "oath:k:" + ((MBObjectBase)k).StringId;
	}

	internal static OathKind Of(Clan c)
	{
		if (c == null)
		{
			return OathKind.None;
		}
		OathKind result;
		return Enum.TryParse<OathKind>(Store.Get(CK(c), "None"), out result) ? result : OathKind.None;
	}

	internal static OathKind Of(Kingdom k)
	{
		if (k == null)
		{
			return OathKind.None;
		}
		OathKind result;
		return Enum.TryParse<OathKind>(Store.Get(KK(k), "None"), out result) ? result : OathKind.None;
	}

	internal static int YearsUnder(Clan c)
	{
		return Years(Store.GetI("oath:cs:" + ((MBObjectBase)c).StringId, -1));
	}

	internal static int YearsUnder(Kingdom k)
	{
		return Years(Store.GetI("oath:ks:" + ((MBObjectBase)k).StringId, -1));
	}

	private static int Years(int since)
	{
		if (since < 0)
		{
			return 0;
		}
		return Math.Max(0, (CourtBehavior.Today() - since) / Math.Max(1, Cfg.DaysPerYear));
	}

	internal static int DaysUntilChangeAllowed(string key)
	{
		int i = Store.GetI(key, -99999);
		return Math.Max(0, Cfg.DaysPerYear - (CourtBehavior.Today() - i));
	}

	internal static string SetForClan(Clan clan, OathKind kind, bool quiet = false)
	{
		if (clan == null)
		{
			return "No house.";
		}
		OathKind oathKind = Of(clan);
		if (oathKind == kind)
		{
			return string.Concat(clan.Name, " already holds under the ", Def(kind).Name, ".");
		}
		if (!quiet && oathKind != 0)
		{
			int num = DaysUntilChangeAllowed("oath:cc:" + ((MBObjectBase)clan).StringId);
			if (num > 0)
			{
				return string.Concat("Terms with ", clan.Name, " can change again in ", num, " days.");
			}
		}
		if (kind == OathKind.Marriage && !MarriageTie(clan))
		{
			return string.Concat("No marriage binds your house to ", clan.Name, ".");
		}
		Store.Set(CK(clan), kind.ToString());
		Store.SetI("oath:cs:" + ((MBObjectBase)clan).StringId, CourtBehavior.Today());
		Store.SetI("oath:cc:" + ((MBObjectBase)clan).StringId, CourtBehavior.Today());
		ApplyBellumLevel(clan, kind);
		Lightened(oathKind, kind, ((object)clan.Name).ToString());
		Store.AddDeed(string.Concat(Standing.Date(), "  ", clan.Name, " swore the ", Def(kind).Name, "."));
		Log.Write(string.Concat("oath: ", clan.Name, " ", oathKind, " -> ", kind));
		return string.Concat(clan.Name, " now holds under the ", Def(kind).Name, ".");
	}

	internal static string SetForKingdom(Kingdom k, OathKind kind, bool quiet = false)
	{
		if (k == null)
		{
			return "No realm.";
		}
		OathKind oathKind = Of(k);
		if (oathKind == kind)
		{
			return string.Concat(k.Name, " already holds under the ", Def(kind).Name, ".");
		}
		if (!quiet && oathKind != 0)
		{
			int num = DaysUntilChangeAllowed("oath:kc:" + ((MBObjectBase)k).StringId);
			if (num > 0)
			{
				return string.Concat("Terms with ", k.Name, " can change again in ", num, " days.");
			}
		}
		if (kind == OathKind.Marriage && (k.RulingClan == null || !MarriageTie(k.RulingClan)))
		{
			return string.Concat("No marriage binds your house to the rulers of ", k.Name, ".");
		}
		Store.Set(KK(k), kind.ToString());
		Store.SetI("oath:ks:" + ((MBObjectBase)k).StringId, CourtBehavior.Today());
		Store.SetI("oath:kc:" + ((MBObjectBase)k).StringId, CourtBehavior.Today());
		if (oathKind != 0 && Def(kind).Burden > Def(oathKind).Burden)
		{
			Store.SetI("oath:tight:" + ((MBObjectBase)k).StringId, CourtBehavior.Today());
		}
		Lightened(oathKind, kind, ((object)k.Name).ToString());
		Store.AddDeed(string.Concat(Standing.Date(), "  ", k.Name, " swore the ", Def(kind).Name, "."));
		Log.Write(string.Concat("oath: realm ", k.Name, " ", oathKind, " -> ", kind));
		return string.Concat(k.Name, " now holds under the ", Def(kind).Name, ".");
	}

	internal static void ClearKingdom(Kingdom k)
	{
		if (k != null)
		{
			string[] array = new string[4] { "oath:k:", "oath:ks:", "oath:kc:", "oath:tight:" };
			string[] array2 = array;
			foreach (string text in array2)
			{
				Store.Set(text + ((MBObjectBase)k).StringId, null);
			}
		}
	}

	private static void Lightened(OathKind old, OathKind now, string who)
	{
		if (old != 0 && Def(now).Burden < Def(old).Burden)
		{
			Standing.Change(Cfg.LightenHonour, 0, "Lightened the oath of " + who);
		}
	}

	private static void ApplyBellumLevel(Clan clan, OathKind kind)
	{
		try
		{
			List<KeyValuePair<string, object>> list = Bellum.TitlesHeldBy(clan);
			Type serviceLevelType = Bellum.ServiceLevelType;
			if (list.Count == 0 || serviceLevelType == null)
			{
				Log.Write(string.Concat("oath: ", clan.Name, " holds no land - tribute only"));
				return;
			}
			object level = Enum.Parse(serviceLevelType, Def(kind).Level);
			string text = null;
			foreach (KeyValuePair<string, object> item in list)
			{
				if (Bellum.SetService(clan, item.Value, level, out var reason))
				{
					Log.Write(string.Concat("service level ", Def(kind).Level, " set for ", clan.Name, " on ", item.Key));
					return;
				}
				text = reason;
			}
			Log.Write(string.Concat("oath: ", clan.Name, " holds nothing directly beneath you, so Bellum keeps its own service terms (", text, ")"));
		}
		catch (Exception ex)
		{
			Log.Write("service level failed: " + ex.Message);
		}
	}

	internal static bool MarriageTie(Clan other)
	{
		try
		{
			Clan playerClan = Clan.PlayerClan;
			if (playerClan == null || other == null)
			{
				return false;
			}
			foreach (Hero item in (List<Hero>)(object)playerClan.Heroes)
			{
				if (item != null && item.Spouse != null && item.Spouse.Clan == other)
				{
					return true;
				}
			}
			foreach (Hero item2 in (List<Hero>)(object)other.Heroes)
			{
				if (item2 != null && item2.Spouse != null && item2.Spouse.Clan == playerClan)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	internal static void Yearly(int today)
	{
		try
		{
			if (Store.Get("tribute:last") == null)
			{
				Store.SetI("tribute:last", today);
			}
			else
			{
				if (today - Store.GetI("tribute:last") < Cfg.DaysPerYear)
				{
					return;
				}
				Store.SetI("tribute:last", today);
				Hero mainHero = Hero.MainHero;
				Clan playerClan = Clan.PlayerClan;
				if (mainHero == null || playerClan == null)
				{
					return;
				}
				int num = 0;
				int num2 = 0;
				List<string> list = new List<string>();
				if (playerClan.Kingdom != null)
				{
					foreach (Clan item in ((IEnumerable<Clan>)playerClan.Kingdom.Clans).ToList())
					{
						if (item == null || item == playerClan || item.IsEliminated || item.Leader == null)
						{
							continue;
						}
						OathKind oathKind = Of(item);
						if (oathKind != 0)
						{
							int num3 = Def(oathKind).TributePerFief() * Count((IEnumerable<Town>)item.Fiefs);
							int num4 = Collect(item.Leader, mainHero, num3);
							if (num4 > 0)
							{
								num += num4;
								num2++;
							}
							if (num4 < num3)
							{
								list.Add(((object)item.Name).ToString());
							}
						}
					}
				}
				foreach (Kingdom item2 in ((IEnumerable<Kingdom>)Kingdom.All).ToList())
				{
					if (item2 == null || item2.IsEliminated || item2.Leader == null || !object.ReferenceEquals(Clients.SuzerainOf(item2), playerClan.Kingdom))
					{
						continue;
					}
					OathKind oathKind2 = Of(item2);
					if (oathKind2 != 0)
					{
						int num5 = Def(oathKind2).TributePerFief() * Count((IEnumerable<Town>)item2.Fiefs);
						int num6 = Collect(item2.Leader, mainHero, num5);
						if (num6 > 0)
						{
							num += num6;
							num2++;
						}
						if (num6 < num5)
						{
							list.Add(((object)item2.Name).ToString());
						}
					}
				}
				if (num > 0 || list.Count > 0)
				{
					string text = "Tribute: " + num.ToString("N0") + " denars from " + num2 + " sworn house(s) and realm(s)";
					if (list.Count > 0)
					{
						text = text + ". Fell short: " + string.Join(", ", list.ToArray());
					}
					Store.AddDeed(Standing.Date() + "  " + text + ".");
					Flow.Notify(text + ".");
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("tributeerr", "tribute failed: " + ex);
		}
	}

	private static int Count<T>(IEnumerable<T> list)
	{
		int? num = list?.Count();
		return num.HasValue ? num.Value : 0;
	}

	private static int Collect(Hero from, Hero to, int owed)
	{
		if (owed <= 0 || from == null || to == null)
		{
			return 0;
		}
		int num = Math.Min(owed, Math.Max(0, from.Gold));
		if (num <= 0)
		{
			return 0;
		}
		try
		{
			GiveGoldAction.ApplyBetweenCharacters(from, to, num, true);
			return num;
		}
		catch (Exception ex)
		{
			Log.Write("tribute transfer failed: " + ex.Message);
			return 0;
		}
	}

	internal static int TributeOf(Clan c)
	{
		OathKind oathKind = Of(c);
		return (oathKind != 0) ? (Def(oathKind).TributePerFief() * Count((IEnumerable<Town>)c.Fiefs)) : 0;
	}

	internal static int TributeOf(Kingdom k)
	{
		OathKind oathKind = Of(k);
		return (oathKind != 0) ? (Def(oathKind).TributePerFief() * Count((IEnumerable<Town>)k.Fiefs)) : 0;
	}

	internal static void PatchLiberty(Harmony h)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		try
		{
			Type type = AccessTools.TypeByName("BellumCivile.Behaviors.ClientKingdomBehavior") ?? AccessTools.TypeByName("BellumCivile.ClientKingdomBehavior");
			MethodInfo methodInfo = ((type != null) ? AccessTools.Method(type, "CalculateClanLiberty", (Type[])null, (Type[])null) : null);
			if (methodInfo == null)
			{
				Log.Write("liberty hook: CalculateClanLiberty not found - oaths will not move liberty");
				return;
			}
			MethodInfo methodInfo2 = methodInfo;
			HarmonyMethod val = new HarmonyMethod(AccessTools.Method(typeof(Oaths), "LibertyPostfix", (Type[])null, (Type[])null));
			h.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, val, (HarmonyMethod)null, (HarmonyMethod)null);
			Log.Write("liberty hook installed on ClientKingdomBehavior.CalculateClanLiberty");
		}
		catch (Exception ex)
		{
			Log.Write("liberty hook failed: " + ex.Message);
		}
	}

	private static void Map(object result)
	{
		if (!_mapped)
		{
			_mapped = true;
			Type type = result.GetType();
			_libField = AccessTools.Field(type, "<LibertyDesire>k__BackingField");
			_reasonsField = AccessTools.Field(type, "<Reasons>k__BackingField");
			Type type2 = AccessTools.TypeByName("BellumCivile.ClientLibertyReason");
			if (type2 != null)
			{
				_reasonCtor = type2.GetConstructor(new Type[2]
				{
					typeof(string),
					typeof(float)
				});
			}
			Log.Write("liberty hook mapped: field=" + (_libField != null) + " reasons=" + (_reasonsField != null) + " ctor=" + (_reasonCtor != null));
		}
	}

	internal static void LibertyPostfix(Clan clan, Kingdom suzerain, object __result)
	{
		try
		{
			if (__result == null || clan == null || clan.Kingdom == null)
			{
				return;
			}
			Kingdom val = ((Clan.PlayerClan != null) ? Clan.PlayerClan.Kingdom : null);
			if (val == null || !object.ReferenceEquals(suzerain, val))
			{
				return;
			}
			Kingdom kingdom = clan.Kingdom;
			OathKind oathKind = Of(kingdom);
			if (oathKind == OathKind.None)
			{
				return;
			}
			Map(__result);
			if (!(_libField == null))
			{
				OathDef oathDef = Def(oathKind);
				int num = YearsUnder(kingdom) + 1;
				float amount = ((oathDef.FlatLiberty != 0) ? oathDef.FlatLiberty : Math.Max(-Cfg.LibertyCap, Math.Min(Cfg.LibertyCap, oathDef.LibertyPerYear * num)));
				string label = ((oathDef.FlatLiberty != 0) ? oathDef.Name : (oathDef.Name + " (" + num + ((num == 1) ? " year)" : " years)")));
				Add(__result, amount, label);
				int i = Store.GetI("oath:tight:" + ((MBObjectBase)kingdom).StringId, -99999);
				if (CourtBehavior.Today() - i < Cfg.DaysPerYear)
				{
					Add(__result, 10f, "Terms tightened this year");
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("liberr", "liberty postfix failed: " + ex.Message);
		}
	}

	private static void Add(object assessment, float amount, string label)
	{
		if (amount == 0f)
		{
			return;
		}
		float num = Convert.ToSingle(_libField.GetValue(assessment));
		_libField.SetValue(assessment, num + amount);
		try
		{
			IList list = ((_reasonsField != null) ? (_reasonsField.GetValue(assessment) as IList) : null);
			if (list != null && _reasonCtor != null)
			{
				list.Add(_reasonCtor.Invoke(new object[2] { label, amount }));
			}
		}
		catch
		{
		}
	}
}
