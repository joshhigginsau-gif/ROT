using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace WardensAndDragons;

internal static class Dragons
{
	private static readonly string[] Colours = new string[6] { "dragon_red", "dragon_gold", "dragon_smoke", "dragon_brown", "dragon_green", "dragon_black" };

	private static readonly string[,] Canon = new string[13, 2]
	{
		{ "Rhaenyra", "Syrax" },
		{ "Daemon", "Caraxes" },
		{ "Aemond", "Vhagar" },
		{ "Helaena", "Dreamfyre" },
		{ "Jacaerys", "Vermax" },
		{ "Lucerys", "Arrax" },
		{ "Rhaenys", "Meleys" },
		{ "Baela", "Moondancer" },
		{ "Joffrey", "Tyraxes" },
		{ "Daeron", "Tessarion" },
		{ "Laenor", "Seasmoke" },
		{ "Addam", "Seasmoke" },
		{ "Rhaena", "Morning" }
	};

	private static readonly string[] NamePool = new string[30]
	{
		"Vermaxes", "Aurion", "Qarlaxes", "Rhaegos", "Syrion", "Morvhae", "Valaxes", "Ossarys", "Tyrax", "Draqos",
		"Belarys", "Lyrraxes", "Nymaeris", "Solaxes", "Caelox", "Vhaezar", "Zaldrys", "Maerox", "Ghaelon", "Iksos",
		"Perzys", "Vaedar", "Onoxys", "Kastor", "Sirroxes", "Laenaxes", "Hae'rys", "Dohaeros", "Jaelox", "Uhrys"
	};

	private static MethodInfo _get;

	private static MethodInfo _set;

	private static PropertyInfo _elItem;

	private static ConstructorInfo _elCtor;

	private static Settlement _seat;

	private static bool _looked;

	internal static Settlement Seat
	{
		get
		{
			if (_looked)
			{
				return _seat;
			}
			_looked = true;
			try
			{
				if (!string.IsNullOrEmpty(Cfg.DragonstoneId))
				{
					_seat = Settlement.Find(Cfg.DragonstoneId);
				}
				if (_seat == null)
				{
					_seat = ((IEnumerable<Settlement>)Settlement.All).FirstOrDefault((Func<Settlement, bool>)((Settlement s) => s != null && (s.IsCastle || s.IsTown) && s.Name != (TextObject)null && string.Equals(((object)s.Name).ToString(), "Dragonstone", StringComparison.OrdinalIgnoreCase)));
				}
				Log.Write((_seat != null) ? string.Concat("Dragonstone found: ", _seat.Name, " (", ((MBObjectBase)_seat).StringId, ")") : "Dragonstone NOT found - set dragonstone_settlement_id in config.txt");
			}
			catch (Exception ex)
			{
				Log.Write("Dragonstone lookup failed: " + ex.Message);
			}
			return _seat;
		}
	}

	internal static List<DragonRec> All()
	{
		List<DragonRec> list = new List<DragonRec>();
		foreach (string item in Store.Keys("dr:d:"))
		{
			DragonRec dragonRec = DragonRec.Unpack(item.Substring(5), Store.Get(item));
			if (dragonRec != null)
			{
				list.Add(dragonRec);
			}
		}
		return list.OrderBy((DragonRec r) => r.Id).ToList();
	}

	internal static void Save(DragonRec r)
	{
		Titles.Invalidate();
		Store.Set("dr:d:" + r.Id, r.Pack());
	}

	private static DragonRec New(string name, string item, int born, int temper, string status, string rider)
	{
		int i = Store.GetI("dr:next", 1);
		Store.SetI("dr:next", i + 1);
		DragonRec dragonRec = new DragonRec();
		dragonRec.Id = "d" + i.ToString("000");
		dragonRec.Name = name;
		dragonRec.Item = item;
		dragonRec.Born = born;
		dragonRec.Temper = temper;
		dragonRec.Status = status;
		dragonRec.Rider = rider ?? "";
		DragonRec dragonRec2 = dragonRec;
		Save(dragonRec2);
		if (!string.IsNullOrEmpty(rider))
		{
			MarkRider(rider);
		}
		return dragonRec2;
	}

	private static void MarkRider(string heroId)
	{
		HashSet<string> hashSet = new HashSet<string>(from x in Store.Get("dr:riders", "").Split(new char[1] { ',' })
			where x.Length > 0
			select x);
		if (hashSet.Add(heroId))
		{
			Store.Set("dr:riders", string.Join(",", hashSet.ToArray()));
		}
	}

	private static bool EverRode(Hero h)
	{
		if (h == null)
		{
			return false;
		}
		return ("," + Store.Get("dr:riders", "") + ",").Contains("," + ((MBObjectBase)h).StringId + ",");
	}

	internal static int LivingCount()
	{
		return All().Count((DragonRec r) => r.Alive);
	}

	internal static int Deaths()
	{
		return Store.GetI("dr:deaths");
	}

	internal static void Kill(DragonRec d, string how)
	{
		try
		{
			if (d != null && d.Alive)
			{
				d.Status = "dead";
				d.Rider = "";
				Save(d);
				Store.SetI("dr:deaths", Store.GetI("dr:deaths") + 1);
				Store.AddDeed(Standing.Date() + "  " + d.Name + " is dead" + ((!string.IsNullOrEmpty(how)) ? (", " + how) : "") + ".");
				Log.Write("dragon dead: " + d.Name + " (" + how + "); the world dims to x" + Twilight().ToString("0.00"));
				Say(d.Name + " is dead. The world has one dragon fewer in it, and the eggs know.");
			}
		}
		catch (Exception ex)
		{
			Log.Write("a dragon could not be killed off: " + ex.Message);
		}
	}

	internal static void OnRiderDeath(Hero victim, bool violent)
	{
		try
		{
			if (victim == null)
			{
				return;
			}
			string id = ((MBObjectBase)victim).StringId;
			DragonRec dragonRec = All().FirstOrDefault((DragonRec r) => r.Alive && r.Rider == id);
			if (dragonRec == null)
			{
				return;
			}
			if (violent && MBRandom.RandomFloat * 100f < (float)Cfg.DragonFallsWithRider)
			{
				Kill(dragonRec, "fallen with " + victim.Name);
				return;
			}
			bool flag = victim.Clan == Clan.PlayerClan;
			dragonRec.Status = "riderless";
			dragonRec.Rider = "";
			Save(dragonRec);
			if (flag)
			{
				Say(string.Concat(dragonRec.Name, " has flown back to the Dragonmont. ", victim.Name, " will not ride again."));
			}
		}
		catch
		{
		}
	}

	internal static float Crowding()
	{
		return Math.Max(0.1f, 1f - (float)LivingCount() / (float)Math.Max(1, Cfg.WorldDragonCapacity));
	}

	internal static float Twilight()
	{
		return Math.Max((float)Cfg.TwilightFloor / 100f, 1f - (float)(Deaths() * Cfg.TwilightPerDeath) / 100f);
	}

	internal static float HatchChance()
	{
		return Math.Max(0f, Math.Min(0.99f, (float)Cfg.EggHatchChance / 100f * Crowding() * Twilight()));
	}

	internal static void EnsureSeeded()
	{
		try
		{
			if (Store.Get("dr:seeded") == "1")
			{
				return;
			}
			int num = CourtBehavior.Today();
			int daysPerYear = Cfg.DaysPerYear;
			HashSet<string> hashSet = new HashSet<string>();
			int num2 = 0;
			foreach (Hero item in ((IEnumerable<Hero>)Hero.AllAliveHeroes).ToList())
			{
				ItemObject val = MountOf(item);
				if (val != null && ((MBObjectBase)val).StringId != null && ((MBObjectBase)val).StringId.StartsWith("dragon_"))
				{
					string text = CanonicalName(item, ((MBObjectBase)val).StringId, hashSet);
					hashSet.Add(text);
					New(text, ((MBObjectBase)val).StringId, num - daysPerYear * Math.Max(10, (int)item.Age + 5), 50, "bonded", ((MBObjectBase)item).StringId);
					num2++;
					Log.Write(string.Concat("registry: ", item.Name, " rides ", text, " (", ((MBObjectBase)val).StringId, ")"));
				}
			}
			if (Cfg.SeedDanceRiderless)
			{
				Seed("Vermithor", "dragon_brown", 100, 60, "riderless", hashSet, num);
				Seed("Silverwing", "dragon_smoke", 80, 30, "riderless", hashSet, num);
				Seed("Seasmoke", "dragon_smoke", 25, 40, "riderless", hashSet, num);
				Seed("Sheepstealer", "dragon_brown", 40, 75, "wild", hashSet, num);
				Seed("Grey Ghost", "dragon_smoke", 60, 65, "wild", hashSet, num);
				DragonRec dragonRec = Seed("Cannibal", "dragon_black", 120, 95, "wild", hashSet, num);
				if (dragonRec != null)
				{
					dragonRec.Kills = 3;
					Save(dragonRec);
				}
			}
			Store.Set("dr:seeded", "1");
			Log.Write("registry seeded: " + num2 + " bonded dragon(s), " + (LivingCount() - num2) + " riderless or wild");
		}
		catch (Exception ex)
		{
			Log.Write("seeding failed: " + ex);
		}
	}

	private static DragonRec Seed(string name, string item, int ageYears, int temper, string status, HashSet<string> used, int today)
	{
		if (used.Contains(name))
		{
			return null;
		}
		used.Add(name);
		return New(name, item, today - Cfg.DaysPerYear * ageYears, temper, status, null);
	}

	private static string CanonicalName(Hero h, string item, HashSet<string> used)
	{
		string text = ((h.Name != (TextObject)null) ? ((object)h.Name).ToString() : "");
		if (text.Contains("Aegon"))
		{
			string text2 = ((item == "dragon_gold") ? "Sunfyre" : "Stormcloud");
			if (!used.Contains(text2))
			{
				return text2;
			}
		}
		for (int i = 0; i < Canon.GetLength(0); i++)
		{
			if (text.Contains(Canon[i, 0]) && !used.Contains(Canon[i, 1]))
			{
				return Canon[i, 1];
			}
		}
		return FreshName(used);
	}

	private static string FreshName(HashSet<string> used)
	{
		HashSet<string> taken = new HashSet<string>(from r in All()
			select r.Name);
		if (used != null)
		{
			taken.UnionWith(used);
		}
		List<string> list = NamePool.Where((string x) => !taken.Contains(x)).ToList();
		return (list.Count > 0) ? list[MBRandom.RandomInt(list.Count)] : ("Dragon " + (taken.Count + 1));
	}

	internal static bool IsRider(Hero h)
	{
		try
		{
			return h != null && (Rides(h) || All().Any((DragonRec r) => r.Rider == ((MBObjectBase)h).StringId && r.Alive));
		}
		catch
		{
			return false;
		}
	}

	internal static bool HasDragonBlood(Hero h)
	{
		try
		{
			return HasDragonBloodUncached(h);
		}
		catch
		{
			return false;
		}
	}

	private static bool HasDragonBloodUncached(Hero h)
	{
		if (h == null)
		{
			return false;
		}
		List<Hero> list = new List<Hero>();
		list.Add(h.Father);
		list.Add(h.Mother);
		List<Hero> list2 = list;
		Hero[] array = (Hero[])(object)new Hero[2] { h.Father, h.Mother };
		Hero[] array2 = array;
		foreach (Hero val in array2)
		{
			if (val != null)
			{
				list2.Add(val.Father);
				list2.Add(val.Mother);
				try
				{
					list2.AddRange(val.Siblings);
				}
				catch
				{
				}
			}
		}
		try
		{
			list2.AddRange(h.Siblings);
		}
		catch
		{
		}
		return list2.Any((Hero k) => k != null && (EverRode(k) || Rides(k)));
	}

	internal static bool CanClaim(Hero h, out string why)
	{
		try
		{
			return CanClaimUncached(h, out why);
		}
		catch (Exception ex)
		{
			why = "the mountain cannot be read just now";
			Log.Once("canclaim", "reading a claim failed: " + ex.Message);
			return false;
		}
	}

	private static bool CanClaimUncached(Hero h, out string why)
	{
		why = null;
		if (h == null || !h.IsAlive)
		{
			why = "dead";
			return false;
		}
		if (h.IsChild)
		{
			why = "too young";
			return false;
		}
		if (IsRider(h))
		{
			why = "already bonded to a dragon";
			return false;
		}
		if (h != Hero.MainHero && !HasDragonBlood(h))
		{
			why = "no dragon blood";
			return false;
		}
		int i = Store.GetI("claim:" + ((MBObjectBase)h).StringId, -99999);
		int num = Cfg.DaysPerYear - (CourtBehavior.Today() - i);
		if (num > 0)
		{
			why = "climbed within the year - " + num + " days";
			return false;
		}
		return true;
	}

	internal static void Reset()
	{
		_seat = null;
		_looked = false;
	}

	internal static ItemObject MountOf(Hero h)
	{
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		try
		{
			if (h == null || h.BattleEquipment == null)
			{
				return null;
			}
			Equipment battleEquipment = h.BattleEquipment;
			if (_get == null)
			{
				_get = ((object)battleEquipment).GetType().GetMethod("get_Item", new Type[1] { typeof(EquipmentIndex) });
			}
			object obj = _get.Invoke(battleEquipment, new object[1] { (object)(EquipmentIndex)10 });
			if (obj == null)
			{
				return null;
			}
			if (_elItem == null)
			{
				_elItem = obj.GetType().GetProperty("Item");
			}
			object obj2;
			if (_elItem != null)
			{
				object value = _elItem.GetValue(obj, null);
				obj2 = ((!(value is ItemObject)) ? null : value);
			}
			else
			{
				obj2 = null;
			}
			return (ItemObject)obj2;
		}
		catch (Exception ex)
		{
			Log.Once("mounterr", "reading a mount failed: " + ex.Message);
			return null;
		}
	}

	internal static bool SetMount(Hero h, ItemObject item)
	{
		try
		{
			if (h == null || h.BattleEquipment == null)
			{
				return false;
			}
			Equipment battleEquipment = h.BattleEquipment;
			if (_get == null)
			{
				_get = ((object)battleEquipment).GetType().GetMethod("get_Item", new Type[1] { typeof(EquipmentIndex) });
			}
			Type returnType = _get.ReturnType;
			if (_set == null)
			{
				_set = ((object)battleEquipment).GetType().GetMethods().FirstOrDefault((MethodInfo m) => m.Name == "set_Item" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(EquipmentIndex));
			}
			if (_elCtor == null)
			{
				_elCtor = returnType.GetConstructors().FirstOrDefault((ConstructorInfo c) => c.GetParameters().Length == 4 && c.GetParameters()[0].ParameterType == typeof(ItemObject));
			}
			if (_set == null || (item != null && _elCtor == null))
			{
				Log.Once("seterr", "cannot write mounts on this version");
				return false;
			}
			ItemObject val = MountOf(h);
			if (item != null && val != null && (((MBObjectBase)val).StringId == null || !((MBObjectBase)val).StringId.StartsWith("dragon_")) && h.PartyBelongedTo != null && h.PartyBelongedTo == MobileParty.MainParty)
			{
				try
				{
					ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
					MethodInfo methodInfo = ((object)itemRoster).GetType().GetMethods().FirstOrDefault((MethodInfo m) => m.Name == "AddToCounts" && m.GetParameters().Length == 2 && m.GetParameters()[0].ParameterType == typeof(ItemObject));
					if (methodInfo != null)
					{
						methodInfo.Invoke(itemRoster, new object[2] { val, 1 });
					}
				}
				catch
				{
				}
			}
			object obj2 = ((item == null) ? Activator.CreateInstance(returnType) : _elCtor.Invoke(new object[4] { item, null, null, false }));
			_set.Invoke(battleEquipment, new object[2]
			{
				(object)(EquipmentIndex)10,
				obj2
			});
			return true;
		}
		catch (Exception ex)
		{
			Log.Write("writing a mount failed: " + ex.Message);
			return false;
		}
	}

	private static ItemObject ItemById(string id)
	{
		try
		{
			return MBObjectManager.Instance.GetObject<ItemObject>(id);
		}
		catch
		{
			return null;
		}
	}

	internal static bool Rides(Hero h)
	{
		if (h == null || !h.IsAlive)
		{
			return false;
		}
		ItemObject val = MountOf(h);
		return val != null && ((MBObjectBase)val).StringId != null && ((MBObjectBase)val).StringId.StartsWith("dragon_");
	}

	internal static int LivingRidersIn(Clan c)
	{
		int num = 0;
		try
		{
			if (c != null)
			{
				foreach (Hero item in (List<Hero>)(object)c.Heroes)
				{
					if (Rides(item))
					{
						num++;
					}
				}
			}
		}
		catch
		{
		}
		return num;
	}

	private static Hero FindHero(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		try
		{
			return ((IEnumerable<Hero>)Hero.AllAliveHeroes).FirstOrDefault((Func<Hero, bool>)((Hero x) => ((MBObjectBase)x).StringId == id)) ?? ((IEnumerable<Hero>)Hero.DeadOrDisabledHeroes).FirstOrDefault((Func<Hero, bool>)((Hero x) => ((MBObjectBase)x).StringId == id));
		}
		catch
		{
			return null;
		}
	}

	internal static void Weekly()
	{
		try
		{
			if (Store.Get("dr:seeded") != "1")
			{
				return;
			}
			Dictionary<string, DragonRec> dictionary = new Dictionary<string, DragonRec>();
			foreach (DragonRec item in All())
			{
				if (!item.Alive)
				{
					continue;
				}
				Hero val = FindHero(item.Rider);
				if (!(item.Status == "bonded") && !(item.Status == "hatchling"))
				{
					continue;
				}
				if (val == null || !val.IsAlive)
				{
					string text = ((val != null) ? ((object)val.Name).ToString() : "its rider");
					item.Status = "riderless";
					item.Rider = "";
					Save(item);
					Log.Write(item.Name + " is riderless: " + text + " is dead");
					if (val != null && val.Clan == Clan.PlayerClan)
					{
						Say(item.Name + " has flown back to the Dragonmont. " + text + " will not ride again.");
					}
					continue;
				}
				if (item.Status == "hatchling" && val.Age >= (float)Cfg.RideableAge)
				{
					item.Status = "bonded";
					Save(item);
					if (val.Clan == Clan.PlayerClan)
					{
						Popup("Dragonrider", string.Concat(val.Name, " has grown, and so has ", item.Name, ". Today they flew together for the first time, and the whole of the realm saw it."));
					}
				}
				if (item.Status == "bonded")
				{
					dictionary[item.Rider] = item;
				}
			}
			foreach (Hero item2 in ((IEnumerable<Hero>)Hero.AllAliveHeroes).ToList())
			{
				DragonRec value;
				bool flag = dictionary.TryGetValue(((MBObjectBase)item2).StringId, out value);
				ItemObject val2 = MountOf(item2);
				bool flag2 = val2 != null && ((MBObjectBase)val2).StringId != null && ((MBObjectBase)val2).StringId.StartsWith("dragon_");
				if (flag && (val2 == null || ((MBObjectBase)val2).StringId != value.Item))
				{
					ItemObject val3 = ItemById(value.Item);
					if (val3 != null && SetMount(item2, val3))
					{
						Log.Write("returned " + value.Name + " to " + item2.Name);
					}
				}
				else if (!flag && flag2 && Cfg.EnforceBonds && SetMount(item2, null))
				{
					Log.Write(string.Concat("took a dragon from ", item2.Name, ": no bond to any dragon"));
				}
			}
		}
		catch (Exception ex)
		{
			Log.Once("dragonweekly", "dragon weekly failed: " + ex);
		}
	}

	internal static void OnBirth(IEnumerable<Hero> children)
	{
		try
		{
			if (children == null || Store.Get("dr:seeded") != "1")
			{
				return;
			}
			Settlement seat = Seat;
			foreach (Hero child in children)
			{
				if (child != null && child.Clan != null)
				{
					bool flag = child.Clan == Clan.PlayerClan;
					if ((flag || Cfg.EggsForAllHouses) && seat != null && seat.OwnerClan == child.Clan && HasDragonBlood(child))
					{
						CradleEgg(child, flag);
					}
				}
			}
		}
		catch (Exception ex)
		{
			Log.Write("birth hook failed: " + ex.Message);
		}
	}

	internal static DragonRec CradleEgg(Hero child, bool mine, bool forceHatch = false)
	{
		float num = HatchChance();
		bool flag = forceHatch || MBRandom.RandomFloat < num;
		Log.Write(string.Concat("cradle egg for ", child.Name, ": ", (num * 100f).ToString("0.0"), "% -> ", flag ? "HATCHED" : "cold"));
		if (!flag)
		{
			Store.Set("egg:" + ((MBObjectBase)child).StringId, "cold");
			if (mine)
			{
				Popup("A Cradle Egg", string.Concat("An egg was laid in ", child.Name, "'s cradle, as is the custom of the blood. It stays cold. The maesters say some never wake.\n\n", child.Name, " may still climb the Dragonmont when grown."));
			}
			return null;
		}
		DragonRec dragonRec = New(FreshName(null), Colours[MBRandom.RandomInt(Colours.Length)], CourtBehavior.Today(), 30 + MBRandom.RandomInt(50), "hatchling", ((MBObjectBase)child).StringId);
		Store.Set("egg:" + ((MBObjectBase)child).StringId, "hatched:" + dragonRec.Id);
		if (mine)
		{
			Store.AddDeed(string.Concat(Standing.Date(), "  ", child.Name, "'s cradle egg hatched: ", dragonRec.Name, "."));
			Popup("The Egg Hatches", string.Concat("The egg in ", child.Name, "'s cradle cracked in the night. By morning there was a dragon curled against the child, small and furious and warm.\n\nThey will call it ", dragonRec.Name, ". It will grow as ", child.Name, " grows, and they may fly together at ", Cfg.RideableAge, "."));
		}
		return dragonRec;
	}

	internal static void OnComesOfAge(Hero h)
	{
		try
		{
			if (h != null && h.Clan == Clan.PlayerClan && !IsRider(h) && HasDragonBlood(h))
			{
				string text = ((Seat != null && Seat.OwnerClan == Clan.PlayerClan) ? "The Dragonmont on Dragonstone awaits, if they dare it." : "Only the lord of Dragonstone may send them up the Dragonmont.");
				Popup("The Dragons Stir", string.Concat(h.Name, " has come of age with dragon blood. ", text));
			}
		}
		catch
		{
		}
	}

	internal static float ClaimChance(Hero h, DragonRec d, List<string> reasons)
	{
		float num = Cfg.ClaimBase;
		reasons.Add(Cfg.ClaimBase + "  base");
		int num2 = 0;
		try
		{
			num2 = h.GetTraitLevel(DefaultTraits.Valor);
		}
		catch
		{
		}
		if (num2 != 0)
		{
			num += (float)(num2 * Cfg.ClaimPerValor);
			reasons.Add(((num2 > 0) ? "+" : "") + num2 * Cfg.ClaimPerValor + "  valour");
		}
		int num3 = 0;
		try
		{
			num3 = h.GetSkillValue(DefaultSkills.Riding);
		}
		catch
		{
		}
		int num4 = num3 / 10 * Cfg.ClaimRidingPer10;
		if (num4 != 0)
		{
			num += (float)num4;
			reasons.Add("+" + num4 + "  riding " + num3);
		}
		int num5 = (50 - d.Temper) / 5;
		if (num5 != 0)
		{
			num += (float)num5;
			reasons.Add(((num5 > 0) ? "+" : "") + num5 + "  " + d.Name + "'s temper");
		}
		if (d.Kills > 0)
		{
			num -= (float)Cfg.ClaimKilledPenalty;
			reasons.Add("-" + Cfg.ClaimKilledPenalty + "  it has killed claimants before");
		}
		if (d.Status == "wild")
		{
			num -= (float)Cfg.ClaimWildPenalty;
			reasons.Add("-" + Cfg.ClaimWildPenalty + "  wild, never ridden");
		}
		float num6 = Crowding() * Twilight();
		reasons.Add("x" + num6.ToString("0.00") + "  the world's reluctance (" + LivingCount() + " dragons live)");
		return Math.Max(0.02f, Math.Min(0.9f, num / 100f * num6));
	}

	internal static float DeathChance(Hero h)
	{
		if (h == Hero.MainHero && !Cfg.PlayerClaimCanDie)
		{
			return 0f;
		}
		return (float)Cfg.ClaimDeathChance / 100f;
	}

	internal static void Resolve(Hero h, DragonRec d)
	{
		try
		{
			Store.SetI("claim:" + ((MBObjectBase)h).StringId, CourtBehavior.Today());
			List<string> reasons = new List<string>();
			float num = ClaimChance(h, d, reasons);
			bool flag = MBRandom.RandomFloat < num;
			d.LastTry = CourtBehavior.Today();
			Log.Write(string.Concat("claim: ", h.Name, " on ", d.Name, " at ", (num * 100f).ToString("0.0"), "% -> ", flag ? "BONDED" : "failed"));
			if (flag)
			{
				d.Status = "bonded";
				d.Rider = ((MBObjectBase)h).StringId;
				Save(d);
				MarkRider(((MBObjectBase)h).StringId);
				ItemObject val = ItemById(d.Item);
				if (val != null)
				{
					SetMount(h, val);
				}
				Standing.Change(0, Cfg.ClaimDread, string.Concat(h.Name, " claimed ", d.Name));
				Popup("A Dragon Bows", string.Concat(h.Name, " climbed into the smoke of the Dragonmont and did not come back down on foot. ", d.Name, " lowered its head, and let itself be mounted, and rose.\n\nThe realm will hear of it by nightfall."));
			}
			else if (MBRandom.RandomFloat < DeathChance(h))
			{
				d.Kills++;
				Save(d);
				Store.AddDeed(string.Concat(Standing.Date(), "  ", h.Name, " was burned on the Dragonmont by ", d.Name, "."));
				Popup("Fire on the Mountain", string.Concat(h.Name, " climbed toward ", d.Name, " and did not return. There was a sound like the sea, and then a light, and then nothing.\n\nWhat was found is not spoken of."));
				KillCharacterAction.ApplyByMurder(h, (Hero)null, true);
			}
			else
			{
				Save(d);
				Popup("Refused", string.Concat(d.Name, " would not have ", h.Name, ". It turned its back and would not be approached, and ", h.Name, " came down the mountain alive - which is more than many can say. Another year, perhaps."));
			}
		}
		catch (Exception ex)
		{
			Log.Write("claim failed: " + ex);
		}
	}

	internal static string SizeOf(DragonRec d)
	{
		float num = (float)(CourtBehavior.Today() - d.Born) / (float)Cfg.DaysPerYear;
		if (num < 5f)
		{
			return "a hatchling";
		}
		if (num < 20f)
		{
			return "young";
		}
		if (num < 60f)
		{
			return "grown";
		}
		if (num < 120f)
		{
			return "great";
		}
		return "ancient";
	}

	internal static string TemperOf(DragonRec d)
	{
		if (d.Temper >= 85)
		{
			return "savage";
		}
		if (d.Temper >= 65)
		{
			return "fierce";
		}
		if (d.Temper >= 40)
		{
			return "proud";
		}
		return "gentle";
	}

	internal static string CourtSummary()
	{
		List<DragonRec> list = (from r in All()
			where r.Alive
			select r).ToList();
		if (list.Count == 0)
		{
			return "No dragons are known to live.";
		}
		List<DragonRec> list2 = list.Where(delegate(DragonRec r)
		{
			Hero val2 = FindHero(r.Rider);
			return val2 != null && val2.Clan == Clan.PlayerClan;
		}).ToList();
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(list.Count).Append(" dragons live. ").Append(list.Count((DragonRec r) => r.Claimable))
			.Append(" are riderless or wild on the Dragonmont.");
		if (list2.Count > 0)
		{
			stringBuilder.Append("\nYour house: ");
			stringBuilder.Append(string.Join("; ", list2.Select(delegate(DragonRec r)
			{
				Hero val = FindHero(r.Rider);
				return r.Name + " (" + ((val != null) ? ((object)val.Name).ToString() : "?") + ((r.Status == "hatchling") ? ", still a hatchling" : "") + ")";
			}).ToArray()));
		}
		stringBuilder.Append("\nA cradle egg would hatch ").Append((HatchChance() * 100f).ToString("0.0")).Append("% of the time today.");
		return stringBuilder.ToString();
	}

	internal static Hero Find(string id)
	{
		return FindHero(id);
	}

	private static void Say(string text)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(text));
		}
		catch
		{
		}
		Log.Write(text);
	}

	internal static void Popup(string title, string text)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		try
		{
			InformationManager.ShowInquiry(new InquiryData(title, text, true, false, "So be it", (string)null, (Action)null, (Action)null, "", 0f, (Action)null, (Func<ValueTuple<bool, string>>)null, (Func<ValueTuple<bool, string>>)null), true, false);
		}
		catch
		{
			Say(text);
		}
		Log.Write(title + ": " + text.Replace("\n", " "));
	}
}
