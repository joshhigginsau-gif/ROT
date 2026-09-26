using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace WardensAndDragons;

internal static class Log
{
	private static string _path;

	private static readonly HashSet<string> _seen = new HashSet<string>();

	internal static void Init()
	{
		string[] array = Paths();
		string[] array2 = array;
		foreach (string text in array2)
		{
			try
			{
				if (text == null)
				{
					continue;
				}
				Directory.CreateDirectory(Path.GetDirectoryName(text));
				File.WriteAllText(text, "");
				_path = text;
				return;
			}
			catch
			{
			}
		}
		_path = null;
	}

	private static string[] Paths()
	{
		string text = null;
		string text2 = null;
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (directoryName != null)
			{
				text = Path.Combine(directoryName, "wardens_dragons.log");
			}
		}
		catch
		{
		}
		try
		{
			text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mount and Blade II Bannerlord", "WardensAndDragons", "wardens_dragons.log");
		}
		catch
		{
		}
		return new string[2] { text, text2 };
	}

	internal static void Write(string msg)
	{
		if (_path == null)
		{
			return;
		}
		try
		{
			File.AppendAllText(_path, DateTime.Now.ToString("HH:mm:ss") + "  " + msg + Environment.NewLine);
		}
		catch
		{
		}
	}

	internal static void Once(string key, string msg)
	{
		if (!_seen.Contains(key))
		{
			_seen.Add(key);
			Write(msg);
		}
	}
}
