using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

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

	internal static bool Init()
	{
		if (_tried)
		{
			return Ready;
		}
		_tried = true;
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
				Log.Write("FeudalTitleBehavior not active - inactive");
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

	private static object GetBehavior(Campaign c, Type t)
	{
		try
		{
			MethodInfo[] methods = typeof(Campaign).GetMethods();
			MethodInfo[] array = methods;
			foreach (MethodInfo methodInfo in array)
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
			object obj = _getTitlesHeldByClan.Invoke(_titleBehavior, parameters2) as IEnumerable;
			IEnumerable enumerable = default(IEnumerable);
			if (obj != null)
			{
				enumerable = (IEnumerable)obj;
				obj = enumerable;
			}
			if (obj == null)
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
			Log.Write("TitlesHeldBy failed: " + ex.Message);
		}
		return list;
	}

	internal static string IdOf(object title)
	{
		try
		{
			return (_pTitleId != null) ? (_pTitleId.GetValue(title, null) as string) : null;
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
			return (_pParent != null) ? (_pParent.GetValue(title, null) as string) : null;
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
			return (string.IsNullOrEmpty(id) || !Init() || _getTitle == null) ? null : _getTitle.Invoke(_titleBehavior, new object[1] { id });
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
			return (title != null && _pDeJure != null) ? (_pDeJure.GetValue(title, null) as string) : null;
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
			return (title != null && _pDeFacto != null) ? (_pDeFacto.GetValue(title, null) as string) : null;
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
			string text = ((_pCapital != null) ? (_pCapital.GetValue(title, null) as string) : null);
			return string.IsNullOrEmpty(text) ? null : Settlement.Find(text);
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
			return (_pType != null) ? Convert.ToInt32(_pType.GetValue(title, null)) : (-1);
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
			return (_pType != null) ? _pType.GetValue(title, null).ToString() : "title";
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
			string text = ((_pName != null) ? _pName.GetValue(title, null) : null)?.ToString();
			if (string.IsNullOrEmpty(text))
			{
				text = ((_pTitleId != null) ? (_pTitleId.GetValue(title, null) as string) : "title");
			}
			object obj = ((_pType != null) ? _pType.GetValue(title, null) : null);
			return (obj == null) ? text : string.Concat(text, "  (", obj, ")");
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
			string text = ((_pName != null) ? _pName.GetValue(title, null) : null)?.ToString();
			return string.IsNullOrEmpty(text) ? "the realm" : text;
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
			string text = ((_pDeJure != null) ? (_pDeJure.GetValue(title, null) as string) : null);
			string text2 = ((_pDeFacto != null) ? (_pDeFacto.GetValue(title, null) as string) : null);
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
		return (level == null) ? "?" : level.ToString();
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
			string[] array = names;
			foreach (string value in array)
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
