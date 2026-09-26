using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons
{
	// Your house's culture, and your house's style.
	//
	// A house that came out of the Vale is Andal by the game's reckoning
	// forever, and everything downstream reads that: Bellum Civile's
	// succession law, what the realm is called, whose customs apply. A house
	// that has taken Dragonstone and flies dragons may reasonably decide it is
	// Valyrian now. This is that decision.
	//
	// It can only choose from the cultures the loaded mods define. A new
	// culture from nothing needs its own troops, names and clothing in XML,
	// which is a module of its own rather than something a menu can make.
	internal static class Heritage
	{
		// Every culture a house could be, the main ones first. Bandit cultures
		// are left out: a house of looters is a different mod.
		internal static List<CultureObject> Choices()
		{
			List<CultureObject> list = new List<CultureObject>();
			try
			{
				foreach (CultureObject c in MBObjectManager.Instance.GetObjectTypeList<CultureObject>())
				{
					if (c != null && !c.IsBandit && c.Name != null && c.Name.ToString().Trim().Length > 0)
					{
						list.Add(c);
					}
				}
			}
			catch (Exception e)
			{
				Log.Once("cultures", "the cultures could not be listed: " + e.Message);
			}
			return list.OrderByDescending((CultureObject c) => c.IsMainCulture)
				.ThenByDescending((CultureObject c) => Valyrian(c))
				.ThenBy((CultureObject c) => c.Name.ToString()).ToList();
		}

		internal static bool Valyrian(CultureObject c)
		{
			string id = ((MBObjectBase)c).StringId.ToLowerInvariant();
			string name = c.Name.ToString().ToLowerInvariant();
			return id.Contains("valyr") || name.Contains("valyr");
		}

		internal static CultureObject Find(string text)
		{
			string t = (text ?? "").Trim().ToLowerInvariant();
			List<CultureObject> all = Choices();
			return all.FirstOrDefault((CultureObject c) => ((MBObjectBase)c).StringId.ToLowerInvariant() == t || c.Name.ToString().ToLowerInvariant() == t)
				?? all.FirstOrDefault((CultureObject c) => ((MBObjectBase)c).StringId.ToLowerInvariant().Contains(t) || c.Name.ToString().ToLowerInvariant().Contains(t));
		}

		// Court -> House and heirs -> Your house's culture.
		internal static void Pick(Action after)
		{
			try
			{
				Clan mine = Clan.PlayerClan;
				List<InquiryElement> els = new List<InquiryElement>();
				foreach (CultureObject c in Choices())
				{
					bool now = mine != null && mine.Culture == c;
					els.Add(new InquiryElement(c, c.Name + "   (" + ((MBObjectBase)c).StringId + ")" + (now ? "   - your house now" : "") + (Valyrian(c) ? "   - of Old Valyria" : ""),
						null, !now,
						c.IsMainCulture
							? "One of the great cultures. Its succession law and its customs become your house's."
							: "A lesser culture of the world. It may have no troops or succession law of its own."));
				}
				if (els.Count == 0)
				{
					Flow.Notify("No cultures could be read from the loaded mods.");
					after();
					return;
				}
				Inquiry.Select("Your House's Culture",
					"What your house is, by blood and by custom. It decides the succession law Bellum Civile holds you to, and how the realm reads you.\n\n" +
					"Only the cultures your mods define can be chosen. " + (els.Any((InquiryElement e) => Valyrian((CultureObject)e.Identifier))
						? "Valyrian is among them."
						: "None of them is Valyrian - your mods do not define one."),
					els, 1, 1, "Take it", "Keep what we are",
					delegate(List<InquiryElement> chosen)
					{
						CultureObject c = (chosen != null && chosen.Count > 0) ? (chosen[0].Identifier as CultureObject) : null;
						if (c == null)
						{
							after();
							return;
						}
						Inquiry.Confirm("How Far",
							"Your house becomes " + c.Name + ": you, your blood, and " + (Succession.Rules() ? "the realm you rule." : "your house.") +
							"\n\nYour holdings can follow too - the towns, castles and villages, and the notables in them, whose culture decides what troops they raise for you.",
							"The house and the holdings", "The house only",
							delegate
							{
								Apply(c, true);
								after();
							},
							delegate
							{
								Apply(c, false);
								after();
							});
					},
					after);
			}
			catch (Exception e)
			{
				Log.Write("choosing a culture failed: " + e.Message);
				after();
			}
		}

		internal static string Apply(CultureObject c, bool holdings)
		{
			try
			{
				Clan mine = Clan.PlayerClan;
				Hero you = Hero.MainHero;
				if (c == null || mine == null || you == null)
				{
					return "Nothing to change.";
				}
				string was = (mine.Culture != null) ? mine.Culture.Name.ToString() : "nothing";
				mine.Culture = c;
				// You and your blood. Not those who married in, and not hired
				// swords: they are of your household, not your house.
				int people = 0;
				foreach (Hero h in mine.Heroes)
				{
					if (h != null && h.IsAlive && (h == you || Succession.IsBlood(h, you)))
					{
						h.Culture = c;
						people++;
					}
				}
				// The realm, when it is yours. Kingdom.Culture has a private
				// setter, so it is written the way the rest of the mod writes
				// private clan fields.
				Kingdom realm = Succession.Realm();
				bool realmToo = false;
				if (realm != null)
				{
					try
					{
						PropertyInfo p = AccessTools.Property(typeof(Kingdom), "Culture");
						MethodInfo set = (p != null) ? p.GetSetMethod(true) : null;
						if (set != null)
						{
							set.Invoke(realm, new object[1] { c });
							realmToo = true;
						}
					}
					catch (Exception ke)
					{
						Log.Once("realmculture", "the realm's culture would not change: " + ke.Message);
					}
				}
				int places = 0;
				int notables = 0;
				if (holdings)
				{
					foreach (Settlement s in mine.Settlements.ToList())
					{
						if (s == null)
						{
							continue;
						}
						s.Culture = c;
						places++;
						foreach (Hero n in s.Notables)
						{
							if (n != null && n.IsAlive)
							{
								n.Culture = c;
								notables++;
							}
						}
						// A castle or town's villages are listed on it, not
						// always on the clan.
						if (s.BoundVillages != null)
						{
							foreach (TaleWorlds.CampaignSystem.Settlements.Village v in s.BoundVillages)
							{
								if (v != null && v.Settlement != null && v.Settlement.Culture != c)
								{
									v.Settlement.Culture = c;
									places++;
									foreach (Hero n in v.Settlement.Notables)
									{
										if (n != null && n.IsAlive)
										{
											n.Culture = c;
											notables++;
										}
									}
								}
							}
						}
					}
				}
				Titles.Invalidate();
				Store.AddDeed(Standing.Date() + "  " + mine.Name + " put off " + was + " and took up " + c.Name + ".");
				string said = mine.Name + " is " + c.Name + " now: " + people + " of your blood" + (realmToo ? ", the realm" : "") +
					(holdings ? (", " + places + " holding(s) and " + notables + " notable(s)") : "") + ". It was " + was + ".";
				Log.Write("culture changed: " + said);
				Flow.Notify(said);
				return said;
			}
			catch (Exception e)
			{
				Log.Write("changing the culture failed: " + e);
				return "It would not change: " + e.Message;
			}
		}

		// Court -> House and heirs -> Your house's style. A style is only ever
		// granted to a house by a realm; this is the way to take one off your
		// own house, or wear the one you want.
		internal static void Style(Action after)
		{
			try
			{
				Clan mine = Clan.PlayerClan;
				string now = Styles.Of(mine);
				Inquiry.Text("Your House's Style",
					((now != null) ? ("The head of your house is styled " + now + ".") : "The head of your house wears no style.") +
					"\n\nWrite the style you want - Dragonlord of Valyria, Lord of Dragonstone, anything - or leave it empty to wear none.",
					now ?? "", "Proclaim it", "Leave it",
					delegate(string text)
					{
						string style = (text ?? "").Replace("{", "").Replace("}", "").Trim();
						if (style.Length > 48)
						{
							style = style.Substring(0, 48).Trim();
						}
						Styles.Set(mine, (style.Length > 0) ? style : null);
						Flow.Notify((style.Length > 0) ? ("The head of your house is styled " + style + ".") : "Your house wears no style.");
						after();
					},
					after);
			}
			catch (Exception e)
			{
				Log.Write("setting the style failed: " + e.Message);
				after();
			}
		}
	}
}
