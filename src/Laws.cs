using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;

namespace WardensAndDragons
{
	// Your culture's succession law.
	//
	// Bellum Civile gives every culture one of eight - Agnatic Primogeniture,
	// Absolute Primogeniture, Tanistry, Elective Bloodright and the rest - and
	// enforces it on the heir screen, which is why most realms will only ever
	// offer you your eldest son. That is not a bug. It is the law doing
	// exactly what it says, and there is no way to change a culture's law from
	// inside the game.
	//
	// So this does not delete the law. It lets you BREAK it, and then makes
	// you pay for it, which is what actually happened every time somebody did.
	// Rhaenyra was named against Andal custom and the realm burned for it.
	internal static class Laws
	{
		private static bool _looked;

		private static Type _helper;

		private static MethodInfo _typeForClan;

		private static MethodInfo _lawName;

		private static MethodInfo _line;

		private static bool _unlocked;

		private static bool _listening;

		internal static void Reset()
		{
			_looked = false;
			_helper = null;
			_legal = null;
			_legalDay = -9999;
		}

		private static void Init()
		{
			if (_looked)
			{
				return;
			}
			_looked = true;
			try
			{
				_helper = AccessTools.TypeByName("BellumCivile.SuccessionLawHelper");
				if (_helper == null)
				{
					return;
				}
				foreach (MethodInfo m in _helper.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
				{
					ParameterInfo[] ps = m.GetParameters();
					if (m.Name == "GetSuccessionTypeForClan" && ps.Length == 1 && _typeForClan == null)
					{
						_typeForClan = m;
					}
					else if (m.Name == "GetSuccessionLawName" && ps.Length == 1 && _lawName == null)
					{
						_lawName = m;
					}
					else if (m.Name == "GetOrderedSuccessionLine" && ps.Length == 1 && _line == null)
					{
						_line = m;
					}
				}
				Log.Write("succession law: helper=" + (_helper != null) + " type=" + (_typeForClan != null) + " name=" + (_lawName != null) + " line=" + (_line != null));
			}
			catch (Exception e)
			{
				Log.Write("reading the succession law failed: " + e.Message);
			}
		}

		internal static bool Ready
		{
			get
			{
				Init();
				return _helper != null;
			}
		}

		// "Agnatic Primogeniture", and so on.
		internal static string Name()
		{
			try
			{
				Init();
				if (_typeForClan == null || _lawName == null || Clan.PlayerClan == null)
				{
					return null;
				}
				object t = _typeForClan.Invoke(null, new object[1] { Clan.PlayerClan });
				object n = _lawName.Invoke(null, new object[1] { t });
				return (n != null) ? n.ToString() : null;
			}
			catch
			{
				return null;
			}
		}

		private static Hero _legal;

		private static int _legalDay = -9999;

		// Who the law says it should be.
		//
		// Cached for the day: this reaches into Bellum by reflection, and the
		// succession screen asks it once per house per redraw, which added up
		// to the same answer computed dozens of times a frame.
		internal static Hero Legal()
		{
			try
			{
				int today = CourtBehavior.Today();
				if (_legalDay == today && (_legal == null || _legal.IsAlive))
				{
					return _legal;
				}
				_legalDay = today;
				_legal = LegalUncached();
				return _legal;
			}
			catch
			{
				return null;
			}
		}

		private static Hero LegalUncached()
		{
			try
			{
				Init();
				if (_line == null || Clan.PlayerClan == null)
				{
					return null;
				}
				IEnumerable order = _line.Invoke(null, new object[1] { Clan.PlayerClan }) as IEnumerable;
				if (order == null)
				{
					return null;
				}
				foreach (object o in order)
				{
					Hero h = o as Hero;
					if (h != null && h.IsAlive)
					{
						return h;
					}
					// The line may be a list of rows rather than heroes.
					if (o != null)
					{
						PropertyInfo p = o.GetType().GetProperty("Hero") ?? o.GetType().GetProperty("Candidate");
						Hero inner = (p != null) ? (p.GetValue(o, null) as Hero) : null;
						if (inner != null && inner.IsAlive)
						{
							return inner;
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("legalheir", "reading the legal heir failed: " + e.Message);
			}
			return null;
		}

		// Is the heir you named the one the law would have chosen?
		internal static bool Lawful()
		{
			Hero named = Succession.Named();
			Hero legal = Legal();
			return legal == null || named == null || named == legal;
		}

		// ------------------------------------------------------------------
		// breaking it

		// Bellum does not hide the other heirs. It force-SELECTS its own.
		//
		// The first attempt at this stood down two of Bellum's Harmony
		// prefixes, which made the screen clickable but left it looking like
		// your eldest son was the only candidate. He never was. The popup is
		// stock TaleWorlds and its list is exactly Clan.GetHeirApparents() -
		// every adult of your house. What was happening is that FIVE separate
		// pieces of Bellum converge on that screen, and we had disarmed two:
		//
		//   HeirSelectionPopupDefaultLegalHeirPatch   force-selects the legal
		//     heir and deselects everyone else, on every construction of the
		//     popup - which is what made it look like a list of one.
		//   HeirSelectionPopupLegalHeirSelectionChangedPatch  re-runs the
		//     check on every click you make.
		//   HeirSelectionConfirmButtonLegalHeirStatePatch  binds the OK
		//     button's IsEnabled to Bellum's verdict - and this one is a
		//     UIExtenderEx prefab patch, not a Harmony patch, so standing
		//     Harmony patches down could never have reached it.
		//
		// Chasing five patches was the wrong shape. All five ask the same two
		// questions of the same helper, so we answer the questions instead.
		// Two postfixes, no prefixes, nothing of Bellum's disabled: it still
		// computes its law, still shows its own reasoning, and simply has no
		// veto. That also puts this file back inside the mod's postfix-only
		// rule, which it was the one exception to.
		internal static void Unlock(Harmony h)
		{
			try
			{
				if (!Cfg.UnlockHeir)
				{
					Log.Write("heir choice left to your culture's law");
					return;
				}
				if (_unlocked || h == null)
				{
					return;
				}
				Type helper = AccessTools.TypeByName("BellumCivile.SuccessionLawHelper");
				if (helper == null)
				{
					Log.Write("heir choice: Bellum's succession helper was not found, so nothing was changed");
					return;
				}
				int n = 0;

				// "Is there a legally required heir?" Answering no means every
				// patch that would enforce one stands down of its own accord:
				// the law patch stops overwriting your choice, the confirm
				// patch stops blocking it, and the constructor patch stops
				// force-selecting.
				MethodInfo resolve = AccessTools.Method(helper, "TryResolveLegalPlayerHeir", (Type[])null, (Type[])null);
				if (resolve != null)
				{
					h.Patch((MethodBase)resolve, (HarmonyMethod)null,
						new HarmonyMethod(AccessTools.Method(typeof(Laws), "NoLegalHeir", (Type[])null, (Type[])null)),
						(HarmonyMethod)null, (HarmonyMethod)null);
					n++;
					Log.Write("  answered TryResolveLegalPlayerHeir: the law names nobody");
				}

				// "May this heir be confirmed?" Answering yes is what actually
				// lights the OK button, because the UIExtenderEx binding reads
				// its state from this call and cannot be unpatched.
				MethodInfo confirm = AccessTools.Method(helper, "CanConfirmSelectedPlayerHeir", (Type[])null, (Type[])null);
				if (confirm != null)
				{
					h.Patch((MethodBase)confirm, (HarmonyMethod)null,
						new HarmonyMethod(AccessTools.Method(typeof(Laws), "AnyHeirConfirms", (Type[])null, (Type[])null)),
						(HarmonyMethod)null, (HarmonyMethod)null);
					n++;
					Log.Write("  answered CanConfirmSelectedPlayerHeir: any of your blood may be named");
				}

				_unlocked = n > 0;
				Log.Write(_unlocked
					? ("heir choice unlocked: " + n + " answer(s) given to Bellum's law. Every adult of your house is selectable; naming against the law costs you instead.")
					: "heir choice: Bellum's law helper had neither method, so nothing was changed");
			}
			catch (Exception e)
			{
				Log.Write("unlocking the heir choice failed: " + e.Message);
			}
		}

		// Bellum has no legally required heir to enforce. It keeps its law and
		// its reasoning; it simply does not get to overrule you with them.
		internal static void NoLegalHeir(ref bool __result)
		{
			if (Cfg.UnlockHeir)
			{
				__result = false;
			}
		}

		// Whoever is highlighted may be confirmed. legalHeir is set to the
		// selection so any text Bellum builds from it still reads sensibly
		// rather than naming somebody the player did not pick.
		internal static void AnyHeirConfirms(ref bool __result, Hero selectedHeir, ref Hero legalHeir)
		{
			if (!Cfg.UnlockHeir)
			{
				return;
			}
			__result = true;
			if (selectedHeir != null)
			{
				legalHeir = selectedHeir;
			}
		}

		// Listen for the player's answer on the heir screen.
		//
		// Installed on its own, unconditionally. It has nothing to do with
		// Bellum's law - it listens to a stock SandBox handler - so gating it
		// behind unlock_heir_choice and behind Bellum being installed meant
		// that anyone playing without Bellum, or with the unlock turned off,
		// silently lost the ability to have their chosen heir actually seated.
		//
		// This is a PREFIX, and the one in this mod. That is not a relapse:
		// everything a prefix is dangerous for - returning false, rewriting
		// arguments - this one does not do. It returns void, touches nothing,
		// and writes a name down. It has to run before the body, because the
		// body kills the player: OnHeirSelectionOver leads to
		// ApplyHeirSelectionAction, which seats the chosen heir and THEN calls
		// the kill synchronously, and the kill re-runs the game's own leader
		// scoring and overwrites that choice. A postfix would record the
		// answer after the damage, which is to say too late to undo it.
		internal static void Listen(Harmony h)
		{
			try
			{
				if (_listening || h == null)
				{
					return;
				}
				MethodInfo over = AccessTools.Method("SandBox.CampaignBehaviors.HeirSelectionCampaignBehavior:OnHeirSelectionOver", (Type[])null, (Type[])null);
				if (over == null)
				{
					Log.Write("heir screen: its handler was not found, so the name on file will be used instead");
					return;
				}
				h.Patch((MethodBase)over,
					new HarmonyMethod(AccessTools.Method(typeof(Laws), "HeirChosen", (Type[])null, (Type[])null)),
					(HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_listening = true;
				Log.Write("heir screen: listening for your answer, so it survives the death that follows it");
			}
			catch (Exception e)
			{
				Log.Write("listening to the heir screen failed: " + e.Message);
			}
		}

		// Read only. Never returns false, never alters the argument.
		internal static void HeirChosen(Hero selectedHeir)
		{
			// With the unlock turned off the player has asked to be bound by
			// their culture's law, and Bellum has its own prefix on this same
			// method that rewrites the choice to the legal heir. Two prefixes
			// at equal priority run in module-load order, so recording here
			// would make the outcome depend on which mod the launcher happened
			// to load first. Stand aside instead: the law decides, and Legal()
			// is what gets seated.
			if (!Cfg.UnlockHeir)
			{
				return;
			}
			Succession.Remember(selectedHeir);
		}

		// What naming against the law costs you with each house. The realm
		// does not care what you want; it cares what it was promised.
		internal static float Penalty()
		{
			try
			{
				if (!Cfg.UnlockHeir || Lawful())
				{
					return 0f;
				}
				return Cfg.UnlawfulHeirPenalty;
			}
			catch
			{
				return 0f;
			}
		}

		internal static string Reading()
		{
			string law = Name();
			if (string.IsNullOrEmpty(law))
			{
				return null;
			}
			Hero legal = Legal();
			string s = "Your culture's law is " + law + ".";
			if (legal != null)
			{
				s += " By it the seat belongs to " + legal.Name + ".";
			}
			if (!Lawful())
			{
				s += "\n  You have named someone else, and every house in the realm knows it.";
			}
			return s;
		}
	}
}
