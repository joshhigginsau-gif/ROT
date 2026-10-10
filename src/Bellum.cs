using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
internal static class Bellum
{
	private static bool _tried;

	private static object _titleBehavior;

	private static MethodInfo _getTitlesHeldByClan;

	private static MethodInfo _grant;

	private static MethodInfo _rename;

	private static MethodInfo _setService;

	private static MethodInfo _revoke;

	private static MethodInfo _serviceLevelName;

	private static MethodInfo _setParent;

	private static MethodInfo _setDeFactoParent;

	private static MethodInfo _rebuildIndexes;

	private static MethodInfo _rebuildCaches;

	private static MethodInfo _getTitle;

	private static PropertyInfo _pParent;

	private static PropertyInfo _pCapital;

	private static PropertyInfo _pName;

	private static PropertyInfo _pTitleId;

	private static PropertyInfo _pType;

	private static PropertyInfo _pDeJure;

	private static PropertyInfo _pDeFacto;

	internal static Type ServiceLevelType;

	internal static bool Ready { get; private set; }

	internal static void Reset()
	{
		_tried = false;
		Ready = false;
		_titleBehavior = null;
	}

	private static int _triedDay = -1;

	internal static bool Init()
	{
		if (_tried && (Ready || CourtBehavior.Today() == _triedDay))
		{
			return Ready;
		}
		_tried = true;
		_triedDay = CourtBehavior.Today();
		try
		{
			Campaign current = Campaign.Current;
			if (current == null)
			{
				_tried = false;
				return false;
			}
			Type type = Find("BellumCivile.Behaviors.FeudalTitleBehavior", "BellumCivile.FeudalTitleBehavior");
			Type type2 = Find("BellumCivile.FeudalTitlePlayerActionService");
			Type type3 = Find("BellumCivile.FeudalTitleRecord");
			ServiceLevelType = Find("BellumCivile.FeudalServiceLevel");
			if (type == null || type2 == null || type3 == null)
			{
				Log.Write("Bellum types not found - Wardens inactive");
				return false;
			}
			_titleBehavior = GetBehavior(current, type);
			if (_titleBehavior == null)
			{
				Log.Once("bellumbeh", "FeudalTitleBehavior not active yet - will try again tomorrow");
				return false;
			}
			_getTitlesHeldByClan = AccessTools.Method(type, "GetTitlesHeldByClan", (Type[])null, (Type[])null);
			_grant = AccessTools.Method(type2, "TryExecuteGrant", (Type[])null, (Type[])null);
			_rename = AccessTools.Method(type2, "TryExecuteRename", (Type[])null, (Type[])null);
			_setService = AccessTools.Method(type2, "TrySetServiceLevel", (Type[])null, (Type[])null);
			_revoke = AccessTools.Method(type2, "TryExecuteRevocation", (Type[])null, (Type[])null);
			_serviceLevelName = AccessTools.Method(type2, "GetServiceLevelName", (Type[])null, (Type[])null);
			_pName = AccessTools.Property(type3, "Name");
			_pTitleId = AccessTools.Property(type3, "TitleId");
			_pType = AccessTools.Property(type3, "TitleType");
			_pDeJure = AccessTools.Property(type3, "DeJureHolderClanId");
			_pDeFacto = AccessTools.Property(type3, "DeFactoHolderClanId");
			_setParent = AccessTools.Method(type3, "SetParentTitle", (Type[])null, (Type[])null);
			_setDeFactoParent = AccessTools.Method(type3, "SetDeFactoParentTitle", (Type[])null, (Type[])null);
			_rebuildIndexes = AccessTools.Method(type, "RebuildRuntimeIndexes", (Type[])null, (Type[])null);
			_rebuildCaches = AccessTools.Method(type, "RebuildReferenceLookupCaches", (Type[])null, (Type[])null);
			_getTitle = AccessTools.Method(type, "GetTitle", new Type[1] { typeof(string) }, (Type[])null);
			_pParent = AccessTools.Property(type3, "ParentTitleId");
			_pCapital = AccessTools.Property(type3, "CapitalSettlementId");
			Ready = _getTitlesHeldByClan != null && _grant != null;
			Log.Write("Bellum API: grant=" + (_grant != null) + " rename=" + (_rename != null) + " service=" + (_setService != null) + " revoke=" + (_revoke != null));
			return Ready;
		}
		catch (Exception ex)
		{
			Log.Write("Bellum init failed: " + ex.Message);
			return false;
		}
	}

	private static Type Find(params string[] names)
	{
		foreach (string text in names)
		{
			Type type = AccessTools.TypeByName(text);
			if (type != null)
			{
				return type;
			}
		}
		return null;
	}

	private static Campaign _councilCampaign;
	private static object _council;
	private static MethodInfo _councilRecords;

	// Bellum's privy council for a realm: each seat's office name and the
	// house that holds it (null when empty). Empty list without Bellum.
	internal static List<KeyValuePair<string, Clan>> CouncilSeats(Kingdom k)
	{
		List<KeyValuePair<string, Clan>> list = new List<KeyValuePair<string, Clan>>();
		try
		{
			Campaign c = Campaign.Current;
			if (k == null || c == null)
			{
				return list;
			}
			if (_councilCampaign != c)
			{
				_councilCampaign = c;
				_council = null;
				_councilRecords = null;
				Type t = Find("BellumCivile.Behaviors.PrivyCouncilBehavior", "BellumCivile.PrivyCouncilBehavior");
				if (t != null)
				{
					_council = GetBehavior(c, t);
					_councilRecords = t.GetMethod("GetOfficeRecords", new Type[1] { typeof(Kingdom) });
				}
				Log.Write("Bellum council: behaviour=" + (_council != null) + " records=" + (_councilRecords != null));
			}
			if (_council == null || _councilRecords == null)
			{
				return list;
			}
			System.Collections.IEnumerable recs = _councilRecords.Invoke(_council, new object[1] { k }) as System.Collections.IEnumerable;
			if (recs == null)
			{
				return list;
			}
			foreach (object r in recs)
			{
				if (r == null)
				{
					continue;
				}
				Type rt = r.GetType();
				object office = rt.GetProperty("Office").GetValue(r, null);
				string cid = rt.GetProperty("HolderClanId").GetValue(r, null) as string;
				Clan holder = string.IsNullOrEmpty(cid) ? null : Clan.FindFirst((Clan x) => ((MBObjectBase)x).StringId == cid);
				list.Add(new KeyValuePair<string, Clan>((office != null) ? office.ToString() : "", holder));
			}
		}
		catch (Exception e)
		{
			Log.Once("bellumcouncil", "reading Bellum's council failed: " + e.Message);
		}
		return list;
	}

	private static object GetBehavior(Campaign c, Type t)
	{
		try
		{
			MethodInfo[] methods = typeof(Campaign).GetMethods();
			foreach (MethodInfo methodInfo in methods)
			{
				if (!(methodInfo.Name != "GetCampaignBehavior") && methodInfo.IsGenericMethodDefinition && methodInfo.GetParameters().Length == 0)
				{
					return methodInfo.MakeGenericMethod(t).Invoke(c, null);
				}
			}
		}
		catch
		{
		}
		return null;
	}

	internal static List<KeyValuePair<string, object>> TitlesHeldBy(Clan clan)
	{
		List<KeyValuePair<string, object>> list = new List<KeyValuePair<string, object>>();
		try
		{
			if (!Init() || clan == null)
			{
				return list;
			}
			ParameterInfo[] parameters = _getTitlesHeldByClan.GetParameters();
			object[] parameters2 = ((parameters.Length == 3) ? new object[3] { clan, true, null } : ((parameters.Length == 2) ? new object[2] { clan, true } : new object[1] { clan }));
			if (!(_getTitlesHeldByClan.Invoke(_titleBehavior, parameters2) is IEnumerable enumerable))
			{
				return list;
			}
			foreach (object item in enumerable)
			{
				if (item != null)
				{
					list.Add(new KeyValuePair<string, object>(Describe(item), item));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("titlesheld", "TitlesHeldBy failed: " + ex.Message);
		}
		return list;
	}

	internal static string IdOf(object title)
	{
		try
		{
			return (!(_pTitleId != null)) ? null : (_pTitleId.GetValue(title, null) as string);
		}
		catch
		{
			return null;
		}
	}

	internal static string ParentIdOf(object title)
	{
		try
		{
			return (!(_pParent != null)) ? null : (_pParent.GetValue(title, null) as string);
		}
		catch
		{
			return null;
		}
	}

	internal static object TitleById(string id)
	{
		try
		{
			return (!string.IsNullOrEmpty(id) && Init() && !(_getTitle == null)) ? _getTitle.Invoke(_titleBehavior, new object[1] { id }) : null;
		}
		catch
		{
			return null;
		}
	}

	internal static string DeJureHolderOf(object title)
	{
		try
		{
			return (title == null || !(_pDeJure != null)) ? null : (_pDeJure.GetValue(title, null) as string);
		}
		catch
		{
			return null;
		}
	}

	internal static string DeFactoHolderOf(object title)
	{
		try
		{
			return (title == null || !(_pDeFacto != null)) ? null : (_pDeFacto.GetValue(title, null) as string);
		}
		catch
		{
			return null;
		}
	}

	internal static Settlement CapitalOf(object title)
	{
		try
		{
			string text = ((!(_pCapital != null)) ? null : (_pCapital.GetValue(title, null) as string));
			return (!string.IsNullOrEmpty(text)) ? Settlement.Find(text) : null;
		}
		catch
		{
			return null;
		}
	}

	internal static bool HoldsDirectlyOfPlayer(object title)
	{
		string text = ParentIdOf(title);
		if (string.IsNullOrEmpty(text))
		{
			return true;
		}
		object obj = TitleById(text);
		if (obj == null)
		{
			return true;
		}
		Clan playerClan = Clan.PlayerClan;
		return playerClan != null && DeJureHolderOf(obj) == ((MBObjectBase)playerClan).StringId;
	}

	internal static int TierOf(object title)
	{
		try
		{
			return (!(_pType != null)) ? (-1) : Convert.ToInt32(_pType.GetValue(title, null));
		}
		catch
		{
			return -1;
		}
	}

	internal static string TierName(object title)
	{
		try
		{
			return (!(_pType != null)) ? "title" : _pType.GetValue(title, null).ToString();
		}
		catch
		{
			return "title";
		}
	}

	internal static bool PlaceBeneath(object child, object parent, out string why)
	{
		why = null;
		try
		{
			if (!Init() || _setParent == null)
			{
				why = "Bellum's hierarchy cannot be changed on this version";
				return false;
			}
			string text = IdOf(parent);
			if (string.IsNullOrEmpty(text))
			{
				why = "the warden's title has no id";
				return false;
			}
			if (TierOf(child) >= TierOf(parent))
			{
				why = "it ranks as high as the warden's own title";
				return false;
			}
			_setParent.Invoke(child, new object[1] { text });
			if (_setDeFactoParent != null)
			{
				_setDeFactoParent.Invoke(child, new object[1] { text });
			}
			return true;
		}
		catch (Exception ex)
		{
			why = ex.Message;
			return false;
		}
	}

	internal static void RebuildIndexes()
	{
		try
		{
			if (_rebuildIndexes != null)
			{
				_rebuildIndexes.Invoke(_titleBehavior, new object[1] { true });
			}
			if (_rebuildCaches != null)
			{
				_rebuildCaches.Invoke(_titleBehavior, new object[0]);
			}
		}
		catch (Exception ex)
		{
			Log.Write("index rebuild failed: " + ex.Message);
		}
	}

	internal static string Describe(object title)
	{
		try
		{
			string text = ((!(_pName != null)) ? null : _pName.GetValue(title, null))?.ToString();
			if (string.IsNullOrEmpty(text))
			{
				text = ((!(_pTitleId != null)) ? "title" : (_pTitleId.GetValue(title, null) as string));
			}
			object obj = ((!(_pType != null)) ? null : _pType.GetValue(title, null));
			return (obj != null) ? string.Concat(text, "  (", obj, ")") : text;
		}
		catch
		{
			return "title";
		}
	}

	internal static string PlainName(object title)
	{
		try
		{
			string text = ((!(_pName != null)) ? null : _pName.GetValue(title, null))?.ToString();
			return (!string.IsNullOrEmpty(text)) ? text : "the realm";
		}
		catch
		{
			return "the realm";
		}
	}

	internal static bool HeldBy(object title, Clan clan)
	{
		try
		{
			if (title == null || clan == null)
			{
				return false;
			}
			string text = ((!(_pDeJure != null)) ? null : (_pDeJure.GetValue(title, null) as string));
			string text2 = ((!(_pDeFacto != null)) ? null : (_pDeFacto.GetValue(title, null) as string));
			return text == ((MBObjectBase)clan).StringId || text2 == ((MBObjectBase)clan).StringId;
		}
		catch
		{
			return false;
		}
	}

	internal static bool Grant(object title, Clan recipient, out string reason)
	{
		reason = null;
		try
		{
			if (!Init() || _grant == null)
			{
				reason = "grant API unavailable";
				return false;
			}
			object[] array = new object[5]
			{
				Clan.PlayerClan,
				title,
				recipient,
				null,
				null
			};
			bool result = (bool)_grant.Invoke(null, array);
			reason = array[4] as string;
			return result;
		}
		catch (Exception ex)
		{
			reason = ex.Message;
			return false;
		}
	}

	internal static bool Rename(object title, string newName, out string reason)
	{
		reason = null;
		try
		{
			if (!Init() || _rename == null)
			{
				reason = "rename API unavailable";
				return false;
			}
			object[] array = new object[4]
			{
				Clan.PlayerClan,
				title,
				newName,
				null
			};
			bool result = (bool)_rename.Invoke(null, array);
			reason = array[3] as string;
			return result;
		}
		catch (Exception ex)
		{
			reason = ex.Message;
			return false;
		}
	}

	internal static bool SetService(Clan clan, object title, object level, out string reason)
	{
		reason = null;
		try
		{
			if (!Init() || _setService == null)
			{
				reason = "service API unavailable";
				return false;
			}
			object[] array = new object[4] { clan, title, level, null };
			bool result = (bool)_setService.Invoke(null, array);
			reason = array[3] as string;
			return result;
		}
		catch (Exception ex)
		{
			reason = ex.Message;
			return false;
		}
	}

	internal static bool Revoke(object title, out string reason)
	{
		reason = null;
		try
		{
			if (!Init() || _revoke == null)
			{
				reason = "revoke API unavailable";
				return false;
			}
			object[] array = new object[4]
			{
				Clan.PlayerClan,
				title,
				false,
				null
			};
			bool result = (bool)_revoke.Invoke(null, array);
			reason = array[3] as string;
			return result;
		}
		catch (Exception ex)
		{
			reason = ex.Message;
			return false;
		}
	}

	internal static string ServiceLevelName(object level)
	{
		try
		{
			if (_serviceLevelName != null)
			{
				object obj = _serviceLevelName.Invoke(null, new object[1] { level });
				if (obj != null)
				{
					return obj.ToString();
				}
			}
		}
		catch
		{
		}
		return (level != null) ? level.ToString() : "?";
	}

	internal static List<object> ServiceLevels()
	{
		List<object> list = new List<object>();
		try
		{
			if (ServiceLevelType == null)
			{
				return list;
			}
			string[] names = Enum.GetNames(ServiceLevelType);
			foreach (string value in names)
			{
				list.Add(Enum.Parse(ServiceLevelType, value));
			}
		}
		catch
		{
		}
		return list;
	}
}
}
