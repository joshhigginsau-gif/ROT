using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace WardensAndDragons;

internal static class Inquiry
{
	private static MethodInfo _multi;

	private static MethodInfo _text;

	private static bool _looked;

	private static void Find()
	{
		if (_looked)
		{
			return;
		}
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			Assembly[] array = assemblies;
			foreach (Assembly assembly in array)
			{
				if (!assembly.FullName.StartsWith("TaleWorlds"))
				{
					continue;
				}
				Type[] types;
				try
				{
					types = assembly.GetTypes();
				}
				catch
				{
					continue;
				}
				Type[] array2 = types;
				Type[] array3 = array2;
				foreach (Type type in array3)
				{
					if (type == null)
					{
						continue;
					}
					try
					{
						if (_multi == null)
						{
							_multi = Single(type, "ShowMultiSelectionInquiry");
						}
						if (_text == null)
						{
							_text = Single(type, "ShowTextInquiry");
						}
					}
					catch
					{
						continue;
					}
					if (_multi != null && _text != null)
					{
						break;
					}
				}
				if (!(_multi != null) || !(_text != null))
				{
					continue;
				}
				break;
			}
			Log.Write("popup APIs: multiSelect=" + (_multi != null) + " textInput=" + (_text != null));
		}
		catch (Exception ex)
		{
			Log.Write("popup lookup failed: " + ex.Message);
		}
		_looked = true;
	}

	private static MethodInfo Single(Type type, string name)
	{
		MethodInfo methodInfo = null;
		MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo2 in methods)
		{
			if (methodInfo2.Name != name)
			{
				continue;
			}
			ParameterInfo[] parameters = methodInfo2.GetParameters();
			if (parameters.Length != 0)
			{
				string text = ((!(name == "ShowTextInquiry")) ? "MultiSelectionInquiryData" : "TextInquiryData");
				if (parameters[0].ParameterType.Name == text && (methodInfo == null || parameters.Length > methodInfo.GetParameters().Length))
				{
					methodInfo = methodInfo2;
				}
			}
		}
		return methodInfo;
	}

	private static void Call(MethodInfo m, object data)
	{
		ParameterInfo[] parameters = m.GetParameters();
		object[] parameters2 = ((parameters.Length >= 3) ? new object[3] { data, false, false } : ((parameters.Length == 2) ? new object[2] { data, false } : new object[1] { data }));
		m.Invoke(null, parameters2);
	}

	internal static void Select(string title, string desc, List<InquiryElement> els, int min, int max, string yes, string no, Action<List<InquiryElement>> onYes, Action onNo = null)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		Find();
		if (_multi == null)
		{
			Flow.Notify("Cannot open the selection window on this build.");
			if (onNo != null)
			{
				onNo();
			}
			return;
		}
		try
		{
			MultiSelectionInquiryData data = new MultiSelectionInquiryData(title, desc, els, true, min, max, yes, no, onYes, (Action<List<InquiryElement>>)delegate
			{
				if (onNo != null)
				{
					onNo();
				}
			}, "", false);
			Call(_multi, data);
		}
		catch (Exception ex)
		{
			Log.Write("Select failed: " + ex);
			Flow.Notify("Could not open the selection window.");
			if (onNo != null)
			{
				try
				{
					onNo();
					return;
				}
				catch
				{
					return;
				}
			}
		}
	}

	internal static void Confirm(string title, string desc, string yes, string no, Action onYes, Action onNo)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData(title, desc, true, true, yes, no, (onYes == null) ? null : new Action(onYes.Invoke), (onNo == null) ? null : new Action(onNo.Invoke), "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch (Exception ex)
		{
			Log.Write("Confirm failed: " + ex.Message);
			if (onNo != null)
			{
				try
				{
					onNo();
					return;
				}
				catch
				{
					return;
				}
			}
		}
	}

	internal static void Text(string title, string desc, string def, string yes, string no, Action<string> onYes, Action onNo)
	{
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		Find();
		if (_text == null)
		{
			if (onNo != null)
			{
				onNo();
			}
			return;
		}
		try
		{
			TextInquiryData data = new TextInquiryData(title, desc, true, true, yes, no, onYes, (Action)delegate
			{
				if (onNo != null)
				{
					onNo();
				}
			}, false, (Func<string, Tuple<bool, string>>)null, "", def);
			Call(_text, data);
		}
		catch (Exception ex)
		{
			Log.Write("Text prompt failed: " + ex);
			if (onNo != null)
			{
				onNo();
			}
		}
	}
}
