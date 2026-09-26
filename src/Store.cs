using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WardensAndDragons
{
internal static class Store
{
	internal const int Schema = 1;

	internal static int Honour;

	internal static int Dread;

	internal static int HonourCap = 100;

	internal static bool Initialized;

	internal static int LastDriftDay;

	internal static int SavedSchema;

	private static readonly List<string> _ledger = new List<string>();

	private static readonly Dictionary<string, string> _kv = new Dictionary<string, string>();

	internal static IList<string> Ledger => _ledger.AsReadOnly();

	internal static void AddDeed(string line)
	{
		_ledger.Insert(0, line);
		while (_ledger.Count > Cfg.LedgerLength)
		{
			_ledger.RemoveAt(_ledger.Count - 1);
		}
	}

	internal static string Get(string key, string fallback = null)
	{
		string value;
		return (!_kv.TryGetValue(key, out value)) ? fallback : value;
	}

	internal static void Set(string key, string value)
	{
		if (value == null)
		{
			_kv.Remove(key);
		}
		else
		{
			_kv[key] = value;
		}
	}

	internal static List<string> Keys(string prefix)
	{
		List<string> list = new List<string>();
		foreach (string key in _kv.Keys)
		{
			if (key.StartsWith(prefix))
			{
				list.Add(key);
			}
		}
		return list;
	}

	internal static float GetF(string key, float fallback = 0f)
	{
		string text = Get(key);
		float result;
		return (text == null || !float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result)) ? fallback : result;
	}

	internal static void SetF(string key, float value)
	{
		Set(key, value.ToString("0.####", CultureInfo.InvariantCulture));
	}

	internal static int GetI(string key, int fallback = 0)
	{
		string text = Get(key);
		int result;
		return (text == null || !int.TryParse(text, out result)) ? fallback : result;
	}

	internal static void SetI(string key, int value)
	{
		Set(key, value.ToString());
	}

	internal static string PackLedger()
	{
		return string.Join("\u001e", _ledger.ToArray());
	}

	internal static void UnpackLedger(string s)
	{
		_ledger.Clear();
		if (string.IsNullOrEmpty(s))
		{
			return;
		}
		string[] array = s.Split('\u001e');
		foreach (string text in array)
		{
			if (text.Length > 0)
			{
				_ledger.Add(text);
			}
		}
	}

	internal static string PackKv()
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> item in _kv)
		{
			stringBuilder.Append(Esc(item.Key)).Append('\u001f').Append(Esc(item.Value))
				.Append('\u001e');
		}
		return stringBuilder.ToString();
	}

	internal static void UnpackKv(string s)
	{
		_kv.Clear();
		if (string.IsNullOrEmpty(s))
		{
			return;
		}
		string[] array = s.Split('\u001e');
		foreach (string text in array)
		{
			if (text.Length != 0)
			{
				int num = text.IndexOf('\u001f');
				if (num >= 0)
				{
					_kv[Unesc(text.Substring(0, num))] = Unesc(text.Substring(num + 1));
				}
			}
		}
	}

	private static string Esc(string s)
	{
		return (s ?? "").Replace("\u001e", " ").Replace("\u001f", " ");
	}

	private static string Unesc(string s)
	{
		return s;
	}

	internal static void ResetForNewCampaign()
	{
		Honour = 0;
		Dread = 0;
		HonourCap = 100;
		Initialized = false;
		LastDriftDay = 0;
		SavedSchema = 0;
		_ledger.Clear();
		_kv.Clear();
	}
}
}
