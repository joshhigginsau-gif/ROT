using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace WardensAndDragons
{
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
		// _looked is set at the END, and each type is probed inside its own
		// guard. It used to be set first with one try around the whole scan,
		// so a single AmbiguousMatchException from any TaleWorlds type that
		// happens to declare two public static overloads of either name
		// aborted the search with both handles null and _looked already true -
		// and every selection window in the mod (heir, oath, style, duty,
		// knife, ward, dragon) was dead until the game was restarted.
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
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
				Type[] array = types;
				foreach (Type type in array)
				{
					if (!(type == null))
					{
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

	// Matching on name alone could bind an overload whose first parameter is
	// not the data object at all, and then throw ArgumentException at every
	// call site instead of once here. Take the one whose first parameter is
	// the type its name implies.
	private static MethodInfo Single(Type type, string name)
	{
		MethodInfo best = null;
		foreach (MethodInfo m in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
		{
			if (m.Name != name)
			{
				continue;
			}
			ParameterInfo[] ps = m.GetParameters();
			if (ps.Length == 0)
			{
				continue;
			}
			string want = (name == "ShowTextInquiry") ? "TextInquiryData" : "MultiSelectionInquiryData";
			if (ps[0].ParameterType.Name == want && (best == null || ps.Length > best.GetParameters().Length))
			{
				best = m;
			}
		}
		return best;
	}

	private static void Call(MethodInfo m, object data)
	{
		ParameterInfo[] parameters = m.GetParameters();
		object[] parameters2 = ((parameters.Length >= 3) ? new object[3] { data, false, false } : ((parameters.Length == 2) ? new object[2] { data, false } : new object[1] { data }));
		m.Invoke(null, parameters2);
	}

	internal static void Select(string title, string desc, List<InquiryElement> els, int min, int max, string yes, string no, Action<List<InquiryElement>> onYes, Action onNo = null)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Expected O, but got Unknown
		Find();
		if (_multi == null)
		{
			// Call onNo. It is not decoration: in Flow.StepStyle it is the
			// ONLY route on to the oath and vassal steps, so dropping it here
			// granted a wardenship and then silently skipped the oath, and
			// that warden paid no tribute for the rest of the campaign.
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
				}
				catch
				{
				}
			}
		}
	}

	// A plain two-button question. InquiryData is stock and always present,
	// so this needs none of the reflection the selection window does.
	internal static void Confirm(string title, string desc, string yes, string no, Action onYes, Action onNo)
	{
		try
		{
			InformationManager.ShowInquiry(new InquiryData(title, desc, true, true, yes, no,
				(onYes != null) ? new Action(onYes) : null,
				(onNo != null) ? new Action(onNo) : null, "", 0f, null, null, null), true, false);
		}
		catch (Exception ex)
		{
			Log.Write("Confirm failed: " + ex.Message);
			// Nothing was asked, so nothing was agreed to.
			if (onNo != null)
			{
				try
				{
					onNo();
				}
				catch
				{
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
}
