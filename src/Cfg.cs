using System;
using System.IO;
using System.Reflection;

namespace WardensAndDragons
{
internal static class Cfg
{
	internal static int StartHonour = 50;

	internal static int StartDread = 10;

	internal static int DriftPerSeason = 1;

	internal static int DaysPerSeason = 21;

	internal static int ExecuteHonour = 10;

	internal static int ExecuteDread = 10;

	internal static bool CapitalOnly = false;

	internal static bool Notify = true;

	internal static int LedgerLength = 12;

	internal static int DaysPerYear = 84;

	internal static string HarrenhalId = "";

	internal static float CursePerYear = 8f;

	internal static float CurseDecayPerYear = 10f;

	internal static float CurseSpeed = 1f;

	internal static int SleeplessDread = 5;

	internal static int KinDeathChance = 10;

	internal static int FireChance = 30;

	internal static int FireLossPercent = 10;

	internal static bool CanKillPlayerFamily = true;

	internal static string LoadedFrom = "(not read)";

	internal static bool StepRename = true;

	internal static bool StepOath = true;

	internal static bool StepVassals = true;

	internal static bool AllowRevoke = true;

	internal static bool RelaxEligibility = true;

	internal static bool AllowSuzerainty = true;

	internal static bool SuzeraintyAlwaysAccepted = false;

	internal static bool AllowAbsorb = true;

	internal static bool AbsorbAllowUnknownLiberty = false;

	internal static int AbsorbGold = 1000000;

	internal static int AbsorbInfluence = 1000;

	internal static int AbsorbRelation = 20;

	internal static float AbsorbMaxLiberty = 50f;

	internal static int TributeFealty = 1000;

	internal static int TributeTribute = 3000;

	internal static int TributeMarriage = 500;

	internal static int TributeDuress = 5000;

	internal static int LibertyCap = 40;

	internal static int LightenHonour = 2;

	internal static int ReleaseHonour = 5;

	internal static int RevokeHonour = 10;

	internal static int RevokeDread = 5;

	internal static int DragonThreatPerRider = 10;

	internal static int DragonThreatCap = 40;

	internal static string DragonstoneId = "";

	internal static int WorldDragonCapacity = 20;

	internal static int EggHatchChance = 30;

	// What changing a name you have already given costs you with every house.
	internal static float RenameHeirCost = 6f;

	// Put the heir you named on the seat, over the game's own scoring - which
	// prefers a male spouse who married in to a daughter of your own blood.
	internal static bool EnforceHeir = true;

	// ---------- Baseborn children ----------
	internal static bool Baseborn = true;
	// Available while married. The scandal is rather the point, but it can be
	// turned off for a cleaner campaign.
	internal static bool BaseWhileMarried = true;
	internal static int BaseNightCost = 500;
	internal static int BaseNightMinAge = 18;
	// Days between nights, and how many children you can end up accounting for.
	internal static int BaseCooldown = 84;
	internal static int BaseMax = 4;
	// How long before word reaches you, and how old they are when it does.
	internal static int BaseYearsUntil = 3;
	internal static int BaseChildMinAge = 14;
	internal static int BaseChildMaxAge = 20;
	internal static int BaseOtherMinAge = 20;
	internal static int BaseOtherMaxAge = 35;
	// The chance your spouse hears about it, and what it costs when they do.
	internal static int BaseWhisperChance = 35;
	internal static int BaseWhisperRelation = 15;
	internal static int BaseWhisperHonour = 3;
	// Writing a baseborn child into the book: what it costs with each of your
	// trueborn kin, and with the realm.
	internal static int LegitKinRelation = 20;
	internal static int LegitStanding = 10;

	// How much of your sworn strength goes over, by what you actually did.
	internal static float ShareIgnored = 0.10f;
	internal static float ShareAcknowledged = 0.20f;
	internal static float ShareArmed = 0.33f;
	internal static float ShareBoth = 0.50f;

	// ---------- The Bastard's Banner ----------
	// Offered once, on the death of your house's ruler, and never unbidden.
	internal static bool Bastard = true;
	// When you have no baseborn child at all, a stranger with your face turns
	// up at the funeral instead. false = a ruler who never went looking for
	// this simply dies, and nothing happens.
	internal static bool BastardStranger = true;
	// The realm declares war the moment it is proclaimed.
	internal static bool BastardWar = true;
	// How much of your sworn strength goes over, as a fraction.
	internal static float BastardShare = 0.33f;
	// Your house must hold at least this many fortifications to be divided.
	internal static int BastardMinFiefs = 2;
	// How old he is, worked back from the dead ruler's age.
	internal static int BastardBornWhen = 22;
	internal static int BastardMinAge = 20;
	internal static int BastardMaxAge = 45;
	// The tier his house starts at.
	internal static int BastardTier = 3;

	internal static int TwilightPerDeath = 3;

	internal static int TwilightFloor = 20;

	// Chance a dragon falls with a rider who dies violently. A rider who dies
	// in bed always leaves his dragon riderless instead.
	internal static int DragonFallsWithRider = 60;

	internal static int RideableAge = 19;

	internal static int ClaimBase = 35;

	internal static int ClaimPerValor = 5;

	internal static int ClaimRidingPer10 = 1;

	internal static int ClaimKilledPenalty = 10;

	internal static int ClaimWildPenalty = 10;

	internal static int ClaimDeathChance = 80;

	internal static int ClaimDread = 5;

	internal static bool PlayerClaimCanDie = true;

	internal static bool EnforceBonds = true;

	internal static bool SeedDanceRiderless = true;

	internal static bool EggsForAllHouses = false;

	internal static bool Titles = true;

	internal static bool TitleWardens = true;

	internal static bool TitleDragonriders = true;

	internal static bool TitleSuffix = false;

	internal static bool CouncilDuties = true;

	internal static bool CouncilLean = true;

	internal static bool LeanBellum = true;

	internal static bool CouncilAiDuties = true;

	internal static float LeanPerYear = 12f;

	internal static int IronBankLoan = 120000;

	internal static int KeeperTemperPerWeek = 1;

	internal static int KeeperTemperFloor = 25;

	internal static int ShipCost = 9000;

	internal static bool OfficeNames = true;

	internal static bool CourtAttention = true;

	internal static int CourtAttentionMax = 6;

	internal static float ControversyWarnAt = 40f;

	internal static int CouncilNagAfter = 3;

	internal static int WhisperNegotiation = 8;

	internal static int RavenNegotiation = 5;

	internal static bool CouncilControversy = true;

	internal static float ControversyScale = 1f;

	internal static bool UnlockHeir = true;

	internal static float UnlawfulHeirPenalty = 20f;

	internal static bool Pretender = true;

	internal static bool PretenderWhileAlive = true;

	internal static float ClaimAcknowledge = 30f;

	internal static float ClaimFief = 20f;

	internal static float ClaimCommand = 12f;

	internal static float ClaimDragon = 25f;

	internal static float ClaimSeat = 15f;

	internal static int CommandGold = 15000;

	internal static int AcknowledgeHonour = 3;

	internal static float PretenderClaimNeeded = 55f;

	internal static float PretenderClaimOnDeath = 25f;

	internal static float PretenderStandingBelow = 40f;

	internal static bool Succession = true;

	internal static bool CouncilInPerson = false;

	internal static int CouncilCooldown = 420;

	internal static int CouncilMaxInHall = 12;

	internal static int CouncilSitsFor = 14;

	internal static float SuccessionDrift = 4f;

	internal static float CouncilIgnored = 6f;

	internal static float CouncilPromise = 18f;

	internal static float CouncilThreat = 10f;

	internal static float CouncilPlain = 8f;

	internal static int CouncilPromiseGold = 20000;

	internal static int SuccessionBreak = 35;

	internal static bool Wardship = true;

	internal static int HostageHonour = 4;

	internal static int HostageDread = 8;

	internal static int HostageRelation = 15;

	internal static int WardHonour = 4;

	internal static int WardRelation = 10;

	internal static int WardXp = 60;

	internal static int WardReleaseHonour = 6;

	internal static int WardReleaseDread = 3;

	internal static int WardReleaseRelation = 20;

	internal static int ExecuteForfeitDread = 15;

	internal static int WardExecuteHonour = 25;

	internal static int WardExecuteDread = 12;

	internal static bool Plots = true;

	internal static float PlotSpeed = 4f;

	internal static float PlotExposure = 9f;

	internal static int PlotMax = 4;

	internal static int PlotRelation = -20;

	internal static int PlotBuyoff = 25000;

	internal static int PlotConfrontHonour = 3;

	internal static int PlotKnifeHonour = 5;

	internal static int PlotKnifeDread = 6;

	internal static int PlotMurderChance = 55;

	internal static int KnifeCost = 30000;

	internal static int KnifeBase = 35;

	internal static int KnifeCooldown = 84;

	internal static int KnifeHonour = 6;

	internal static int KnifeDread = 5;

	internal static int KnifeTraceChance = 40;

	internal static int KnifeTracedHonour = 8;

	internal static string Describe()
	{
		return "start_honour=" + StartHonour + " start_dread=" + StartDread + " drift=" + DriftPerSeason + "/" + DaysPerSeason + "d curse_per_year=" + CursePerYear + " curse_decay_per_year=" + CurseDecayPerYear + " curse_test_speed=" + CurseSpeed + " kin_death=" + KinDeathChance + "% fire=" + FireChance + "% loss=" + FireLossPercent + "% days_per_year=" + DaysPerYear + " harrenhal_id='" + HarrenhalId + "' can_kill_family=" + CanKillPlayerFamily;
	}

	internal static void Load()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			if (directoryName == null)
			{
				return;
			}
			string path = (LoadedFrom = Path.Combine(directoryName, "config.txt"));
			if (!File.Exists(path))
			{
				File.WriteAllText(path, Default());
				return;
			}
			AppendSection(path, "curse_per_year", "# ---------- Harrenhal", "# ---------- Wardens and oaths");
			AppendSection(path, "tribute_fealty", "# ---------- Wardens and oaths", "# ---------- Dragons");
			AppendSection(path, "world_dragon_capacity", "# ---------- Dragons", "# ---------- Names and titles");
			AppendSection(path, "titles_enabled", "# ---------- Names and titles", "# ---------- The council's names");
			AppendSection(path, "council_office_names", "# ---------- The council's names", "# ---------- Hostages and wards");
			AppendSection(path, "wardship_enabled", "# ---------- Hostages and wards", "# ---------- Your heir");
			AppendSection(path, "succession_enabled", "# ---------- Your heir", "# ---------- Baseborn children");
			AppendSection(path, "baseborn_children", "# ---------- Baseborn children", "# ---------- The Bastard");
			AppendSection(path, "bastards_banner", "# ---------- The Bastard's Banner", null);
			string[] array = File.ReadAllLines(path);
			foreach (string text in array)
			{
				string text2 = text.Trim().ToLowerInvariant().Replace(" ", "");
				if (text2.StartsWith("#") || text2.Length == 0 || text2.IndexOf('=') < 0)
				{
					continue;
				}
				string text3 = text2.Substring(0, text2.IndexOf('='));
				string text4 = text2.Substring(text2.IndexOf('=') + 1);
				bool flag = int.TryParse(text4, out var result);
				bool flag2 = !(text4 == "false") && !(text4 == "0") && !(text4 == "no");
				switch (text3)
				{
				case "start_honour":
					if (flag)
					{
						StartHonour = Clamp(result);
					}
					break;
				case "start_dread":
					if (flag)
					{
						StartDread = Clamp(result);
					}
					break;
				case "drift_per_season":
					if (flag)
					{
						DriftPerSeason = Math.Max(0, result);
					}
					break;
				case "days_per_season":
					if (flag)
					{
						DaysPerSeason = Math.Max(1, result);
					}
					break;
				case "execute_honour_loss":
					if (flag)
					{
						ExecuteHonour = Math.Max(0, result);
					}
					break;
				case "execute_dread_gain":
					if (flag)
					{
						ExecuteDread = Math.Max(0, result);
					}
					break;
				case "court_capital_only":
					CapitalOnly = flag2;
					break;
				case "notify_changes":
					Notify = flag2;
					break;
				case "ledger_length":
					if (flag)
					{
						LedgerLength = Math.Max(1, Math.Min(50, result));
					}
					break;
				case "days_per_year":
					if (flag)
					{
						DaysPerYear = Math.Max(1, result);
					}
					break;
				case "harrenhal_settlement_id":
					HarrenhalId = text.Substring(text.IndexOf('=') + 1).Trim();
					break;
				case "curse_per_year":
					if (flag)
					{
						CursePerYear = Math.Max(0, result);
					}
					break;
				case "curse_decay_per_year":
					if (flag)
					{
						CurseDecayPerYear = Math.Max(0, result);
					}
					break;
				case "curse_test_speed":
					if (flag)
					{
						CurseSpeed = Math.Max(1, Math.Min(200, result));
					}
					break;
				case "sleepless_dread":
					if (flag)
					{
						SleeplessDread = Math.Max(0, result);
					}
					break;
				case "kin_death_chance":
					if (flag)
					{
						KinDeathChance = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "fire_chance":
					if (flag)
					{
						FireChance = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "fire_prosperity_loss_percent":
					if (flag)
					{
						FireLossPercent = Math.Max(0, Math.Min(90, result));
					}
					break;
				case "curse_can_kill_player_family":
					CanKillPlayerFamily = flag2;
					break;
				case "step_rename":
					StepRename = flag2;
					break;
				case "step_oath":
					StepOath = flag2;
					break;
				case "step_vassals":
					StepVassals = flag2;
					break;
				case "allow_revoke":
					AllowRevoke = flag2;
					break;
				case "relax_eligibility":
					RelaxEligibility = flag2;
					break;
				case "allow_suzerainty":
					AllowSuzerainty = flag2;
					break;
				case "suzerainty_always_accepted":
					SuzeraintyAlwaysAccepted = flag2;
					break;
				case "allow_absorb":
					AllowAbsorb = flag2;
					break;
				case "absorb_allow_unknown_liberty":
					AbsorbAllowUnknownLiberty = flag2;
					break;
				case "absorb_gold":
					if (flag)
					{
						AbsorbGold = Math.Max(0, result);
					}
					break;
				case "absorb_influence":
					if (flag)
					{
						AbsorbInfluence = Math.Max(0, result);
					}
					break;
				case "absorb_max_liberty":
					if (flag)
					{
						AbsorbMaxLiberty = result;
					}
					break;
				case "tribute_fealty":
					if (flag)
					{
						TributeFealty = Math.Max(0, result);
					}
					break;
				case "tribute_tribute":
					if (flag)
					{
						TributeTribute = Math.Max(0, result);
					}
					break;
				case "tribute_marriage":
					if (flag)
					{
						TributeMarriage = Math.Max(0, result);
					}
					break;
				case "tribute_duress":
					if (flag)
					{
						TributeDuress = Math.Max(0, result);
					}
					break;
				case "liberty_cap":
					if (flag)
					{
						LibertyCap = Math.Max(0, result);
					}
					break;
				case "lighten_honour":
					if (flag)
					{
						LightenHonour = Math.Max(0, result);
					}
					break;
				case "release_honour":
					if (flag)
					{
						ReleaseHonour = Math.Max(0, result);
					}
					break;
				case "revoke_honour_loss":
					if (flag)
					{
						RevokeHonour = Math.Max(0, result);
					}
					break;
				case "revoke_dread_gain":
					if (flag)
					{
						RevokeDread = Math.Max(0, result);
					}
					break;
				case "dragon_threat_per_rider":
					if (flag)
					{
						DragonThreatPerRider = Math.Max(0, result);
					}
					break;
				case "dragon_threat_cap":
					if (flag)
					{
						DragonThreatCap = Math.Max(0, result);
					}
					break;
				case "dragonstone_settlement_id":
					DragonstoneId = text.Substring(text.IndexOf('=') + 1).Trim();
					break;
				case "world_dragon_capacity":
					if (flag)
					{
						WorldDragonCapacity = Math.Max(1, result);
					}
					break;
				case "egg_hatch_chance":
					if (flag)
					{
						EggHatchChance = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "enforce_named_heir":
					EnforceHeir = flag2;
					break;
				case "baseborn_children":
					Baseborn = flag2;
					break;
				case "baseborn_while_married":
					BaseWhileMarried = flag2;
					break;
				case "baseborn_night_cost":
					if (flag) { BaseNightCost = Math.Max(0, result); }
					break;
				case "baseborn_cooldown_days":
					if (flag) { BaseCooldown = Math.Max(1, result); }
					break;
				case "baseborn_max":
					if (flag) { BaseMax = Math.Max(1, result); }
					break;
				case "baseborn_years_until":
					if (flag) { BaseYearsUntil = Math.Max(0, result); }
					break;
				case "baseborn_child_min_age":
					if (flag) { BaseChildMinAge = Math.Max(1, result); }
					break;
				case "baseborn_child_max_age":
					if (flag) { BaseChildMaxAge = Math.Max(1, result); }
					break;
				case "baseborn_night_min_age":
					if (flag) { BaseNightMinAge = Math.Max(16, result); }
					break;
				case "baseborn_other_min_age":
					if (flag) { BaseOtherMinAge = Math.Max(16, result); }
					break;
				case "baseborn_other_max_age":
					if (flag) { BaseOtherMaxAge = Math.Max(16, result); }
					break;
				case "baseborn_whisper_honour":
					if (flag) { BaseWhisperHonour = Math.Max(0, result); }
					break;
				case "baseborn_whisper_chance":
					if (flag) { BaseWhisperChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "baseborn_whisper_relation":
					if (flag) { BaseWhisperRelation = Math.Max(0, result); }
					break;
				case "legitimise_kin_relation":
					if (flag) { LegitKinRelation = Math.Max(0, result); }
					break;
				case "legitimise_standing":
					if (flag) { LegitStanding = Math.Max(0, result); }
					break;
				case "share_ignored":
					if (flag) { ShareIgnored = Math.Max(0, Math.Min(100, result)) / 100f; }
					break;
				case "share_acknowledged":
					if (flag) { ShareAcknowledged = Math.Max(0, Math.Min(100, result)) / 100f; }
					break;
				case "share_armed":
					if (flag) { ShareArmed = Math.Max(0, Math.Min(100, result)) / 100f; }
					break;
				case "share_both":
					if (flag) { ShareBoth = Math.Max(0, Math.Min(100, result)) / 100f; }
					break;
				case "bastard_stranger":
					BastardStranger = flag2;
					break;
				case "bastards_banner":
					Bastard = flag2;
					break;
				case "bastard_declares_war":
					BastardWar = flag2;
					break;
				case "bastard_vassal_share":
					if (flag)
					{
						BastardShare = Math.Max(0f, Math.Min(100f, result)) / 100f;
					}
					break;
				case "bastard_min_fiefs":
					if (flag)
					{
						BastardMinFiefs = Math.Max(1, result);
					}
					break;
				case "bastard_born_years_before":
					if (flag)
					{
						BastardBornWhen = Math.Max(16, result);
					}
					break;
				case "bastard_min_age":
					if (flag)
					{
						BastardMinAge = Math.Max(16, result);
					}
					break;
				case "bastard_max_age":
					if (flag)
					{
						BastardMaxAge = Math.Max(16, result);
					}
					break;
				case "bastard_house_tier":
					if (flag)
					{
						BastardTier = Math.Max(0, Math.Min(6, result));
					}
					break;
				case "rename_heir_cost":
					if (flag)
					{
						RenameHeirCost = Math.Max(0, result);
					}
					break;
				case "twilight_per_death":
					if (flag)
					{
						TwilightPerDeath = Math.Max(0, result);
					}
					break;
				case "twilight_floor":
					if (flag)
					{
						TwilightFloor = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "dragon_falls_with_rider":
					if (flag)
					{
						DragonFallsWithRider = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "rideable_age":
					if (flag)
					{
						RideableAge = Math.Max(0, result);
					}
					break;
				case "claim_base":
					if (flag)
					{
						ClaimBase = Math.Max(0, result);
					}
					break;
				case "claim_per_valor":
					if (flag)
					{
						ClaimPerValor = result;
					}
					break;
				case "claim_riding_per_10":
					if (flag)
					{
						ClaimRidingPer10 = result;
					}
					break;
				case "claim_killed_penalty":
					if (flag)
					{
						ClaimKilledPenalty = Math.Max(0, result);
					}
					break;
				case "claim_wild_penalty":
					if (flag)
					{
						ClaimWildPenalty = Math.Max(0, result);
					}
					break;
				case "claim_death_chance":
					if (flag)
					{
						ClaimDeathChance = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "claim_dread":
					if (flag)
					{
						ClaimDread = Math.Max(0, result);
					}
					break;
				case "player_claim_can_die":
					PlayerClaimCanDie = flag2;
					break;
				case "enforce_bonds":
					EnforceBonds = flag2;
					break;
				case "seed_dance_riderless":
					SeedDanceRiderless = flag2;
					break;
				case "eggs_for_all_houses":
					EggsForAllHouses = flag2;
					break;
				case "titles_enabled":
					Titles = flag2;
					break;
				case "title_wardens":
					TitleWardens = flag2;
					break;
				case "title_dragonriders":
					TitleDragonriders = flag2;
					break;
				case "title_position":
					TitleSuffix = text4 == "suffix" || text4 == "after" || text4 == "behind";
					break;
				case "council_duties":
					CouncilDuties = flag2;
					break;
				case "council_lean":
					CouncilLean = flag2;
					break;
				case "lean_bellum_duties":
					LeanBellum = flag2;
					break;
				case "council_ai_duties":
					CouncilAiDuties = flag2;
					break;
				case "lean_per_year":
					if (flag)
					{
						LeanPerYear = Math.Max(0, result);
					}
					break;
				case "iron_bank_loan":
					if (flag)
					{
						IronBankLoan = Math.Max(0, result);
					}
					break;
				case "keeper_temper_per_week":
					if (flag)
					{
						KeeperTemperPerWeek = Math.Max(0, result);
					}
					break;
				case "court_attention":
					CourtAttention = flag2;
					break;
				case "court_attention_max":
					if (flag)
					{
						CourtAttentionMax = Math.Max(1, result);
					}
					break;
				case "controversy_warn_at":
					if (flag)
					{
						ControversyWarnAt = Math.Max(0, result);
					}
					break;
				case "whisper_negotiation_bonus":
					if (flag)
					{
						WhisperNegotiation = Math.Max(0, result);
					}
					break;
				case "raven_negotiation_bonus":
					if (flag)
					{
						RavenNegotiation = Math.Max(0, result);
					}
					break;
				case "council_nag_after_years":
					if (flag)
					{
						CouncilNagAfter = Math.Max(0, result);
					}
					break;
				case "council_controversy":
					CouncilControversy = flag2;
					break;
				case "controversy_scale":
					if (flag)
					{
						ControversyScale = Math.Max(0, result);
					}
					break;
				case "council_office_names":
					OfficeNames = flag2;
					break;
				case "unlock_heir_choice":
					UnlockHeir = flag2;
					break;
				case "unlawful_heir_penalty":
					if (flag)
					{
						UnlawfulHeirPenalty = Math.Max(0, result);
					}
					break;
				case "pretender_enabled":
					Pretender = flag2;
					break;
				case "pretender_while_alive":
					PretenderWhileAlive = flag2;
					break;
				case "pretender_claim_needed":
					if (flag)
					{
						PretenderClaimNeeded = Math.Max(0, result);
					}
					break;
				case "pretender_claim_on_death":
					if (flag)
					{
						PretenderClaimOnDeath = Math.Max(0, result);
					}
					break;
				case "pretender_standing_below":
					if (flag)
					{
						PretenderStandingBelow = Math.Max(0, result);
					}
					break;
				case "command_gold":
					if (flag)
					{
						CommandGold = Math.Max(0, result);
					}
					break;
				case "succession_enabled":
					Succession = flag2;
					break;
				case "council_in_person":
					CouncilInPerson = flag2;
					break;
				case "great_council_cooldown_days":
					if (flag)
					{
						CouncilCooldown = Math.Max(0, result);
					}
					break;
				case "council_sits_for_days":
					if (flag)
					{
						CouncilSitsFor = Math.Max(1, result);
					}
					break;
				case "council_max_in_hall":
					if (flag)
					{
						CouncilMaxInHall = Math.Max(0, result);
					}
					break;
				case "succession_drift_per_year":
					if (flag)
					{
						SuccessionDrift = Math.Max(0, result);
					}
					break;
				case "council_ignored_cost":
					if (flag)
					{
						CouncilIgnored = Math.Max(0, result);
					}
					break;
				case "council_promise_gold":
					if (flag)
					{
						CouncilPromiseGold = Math.Max(0, result);
					}
					break;
				case "succession_break_at":
					if (flag)
					{
						SuccessionBreak = Math.Max(0, result);
					}
					break;
				case "wardship_enabled":
					Wardship = flag2;
					break;
				case "hostage_dread":
					if (flag)
					{
						HostageDread = Math.Max(0, result);
					}
					break;
				case "hostage_honour_loss":
					if (flag)
					{
						HostageHonour = Math.Max(0, result);
					}
					break;
				case "ward_honour":
					if (flag)
					{
						WardHonour = Math.Max(0, result);
					}
					break;
				case "ward_xp_per_week":
					if (flag)
					{
						WardXp = Math.Max(0, result);
					}
					break;
				case "ward_release_honour":
					if (flag)
					{
						WardReleaseHonour = Math.Max(0, result);
					}
					break;
				case "execute_forfeit_dread":
					if (flag)
					{
						ExecuteForfeitDread = Math.Max(0, result);
					}
					break;
				case "execute_no_cause_honour_loss":
					if (flag)
					{
						WardExecuteHonour = Math.Max(0, result);
					}
					break;
				case "plots_enabled":
					Plots = flag2;
					break;
				case "plot_speed":
					if (flag)
					{
						PlotSpeed = Math.Max(0, result);
					}
					break;
				case "plot_exposure":
					if (flag)
					{
						PlotExposure = Math.Max(0, result);
					}
					break;
				case "plot_max":
					if (flag)
					{
						PlotMax = Math.Max(0, result);
					}
					break;
				case "plot_relation":
					if (flag)
					{
						PlotRelation = result;
					}
					break;
				case "plot_buyoff":
					if (flag)
					{
						PlotBuyoff = Math.Max(0, result);
					}
					break;
				case "plot_murder_chance":
					if (flag)
					{
						PlotMurderChance = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "knife_cost":
					if (flag)
					{
						KnifeCost = Math.Max(0, result);
					}
					break;
				case "knife_base":
					if (flag)
					{
						KnifeBase = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "knife_cooldown_days":
					if (flag)
					{
						KnifeCooldown = Math.Max(0, result);
					}
					break;
				case "knife_trace_chance":
					if (flag)
					{
						KnifeTraceChance = Math.Max(0, Math.Min(100, result));
					}
					break;
				case "ship_cost":
					if (flag)
					{
						ShipCost = Math.Max(0, result);
					}
					break;
				case "keeper_temper_floor":
					if (flag)
					{
						KeeperTemperFloor = Math.Max(0, Math.Min(100, result));
					}
					break;
				}
			}
		}
		catch
		{
		}
		finally
		{
			// In a finally, not at the end of the parse loop. Load() has three
			// exits - no directory, a first run that writes the template and
			// returns, and a throw part-way through - and an invariant that
			// only holds on one of them is not an invariant.
			Settle();
		}
	}

	// Cross-checks that a single key cannot make on its own.
	//
	// Each age is clamped as it is read, but nothing stopped a min above its
	// max - and MBRandom.RandomInt throws on an inverted range, which would
	// have made a child arrive, throw, and be silently deleted by the catch
	// that was meant to be protecting the campaign.
	private static void Settle()
	{
		BaseChildMaxAge = Math.Max(BaseChildMinAge, BaseChildMaxAge);
		BaseOtherMaxAge = Math.Max(BaseOtherMinAge, BaseOtherMaxAge);
		BastardMaxAge = Math.Max(BastardMinAge, BastardMaxAge);
	}

	private static void AppendSection(string path, string marker, string from, string until)
	{
		try
		{
			if (File.ReadAllText(path).IndexOf(marker, StringComparison.OrdinalIgnoreCase) < 0)
			{
				string text = Default();
				int num = text.IndexOf(from);
				if (num >= 0)
				{
					int num2 = ((until == null) ? (-1) : text.IndexOf(until, num + 1));
					File.AppendAllText(path, Environment.NewLine + ((num2 <= num) ? text.Substring(num) : text.Substring(num, num2 - num)));
				}
			}
		}
		catch
		{
		}
	}

	private static int Clamp(int v)
	{
		return (v >= 0) ? ((v <= 100) ? v : 100) : 0;
	}

	private static string Default()
	{
		string newLine = Environment.NewLine;
		return "# Wardens & Dragons" + newLine + "" + newLine + "# Where a ruler starts, and where both numbers drift back to." + newLine + "start_honour=50" + newLine + "start_dread=10" + newLine + "" + newLine + "# Seasonal drift. A Bannerlord year is 84 days, so a season is 21." + newLine + "drift_per_season=1" + newLine + "days_per_season=21" + newLine + "" + newLine + "# Executing a captured lord." + newLine + "execute_honour_loss=10" + newLine + "execute_dread_gain=10" + newLine + "" + newLine + "# false = hold court at any settlement your house owns." + newLine + "# true  = only at your house's home seat." + newLine + "court_capital_only=false" + newLine + "" + newLine + "# Show a message whenever Honour or Dread changes." + newLine + "notify_changes=true" + newLine + "" + newLine + "# How many deeds the court remembers." + newLine + "ledger_length=12" + newLine + "" + newLine + "# The court tells you what is actually waiting on a decision, and says" + newLine + "# nothing at all when nothing is." + newLine + "court_attention=true" + newLine + "court_attention_max=6" + newLine + "" + newLine + "# ---------- Harrenhal ----------" + newLine + "# Leave blank to find Harrenhal by name. Set a settlement id if it is not found." + newLine + "harrenhal_settlement_id=" + newLine + "# The curse builds on whoever holds it, 0 to 100, and fades once they let it go." + newLine + "curse_per_year=8" + newLine + "curse_decay_per_year=10" + newLine + "# FOR TESTING ONLY: multiplies how fast the curse builds and fades." + newLine + "# 50 reaches every stage within a few weeks. Put it back to 1 to play." + newLine + "curse_test_speed=1" + newLine + "# Dread gained when you first cannot sleep there." + newLine + "sleepless_dread=5" + newLine + "# At 75 and above, each year: chance (percent) of a death in the family," + newLine + "# and of a fire costing Harrenhal this percent of its prosperity." + newLine + "kin_death_chance=10" + newLine + "fire_chance=30" + newLine + "fire_prosperity_loss_percent=10" + newLine + "# false = the curse never kills members of YOUR house." + newLine + "curse_can_kill_player_family=true" + newLine + "# A Bannerlord year is 84 days." + newLine + "days_per_year=84" + newLine + "" + newLine + "# ---------- Wardens and oaths ----------" + newLine + "# Tribute each year, per fief the house or realm holds." + newLine + "tribute_fealty=1000" + newLine + "tribute_tribute=3000" + newLine + "tribute_marriage=500" + newLine + "tribute_duress=5000" + newLine + "# The most an oath can move a client's liberty, up or down." + newLine + "liberty_cap=40" + newLine + "lighten_honour=2" + newLine + "release_honour=5" + newLine + "revoke_honour_loss=10" + newLine + "revoke_dread_gain=5" + newLine + "# Each dragon rider in your house adds this to a threat, up to the cap." + newLine + "dragon_threat_per_rider=10" + newLine + "dragon_threat_cap=40" + newLine + "# Naming a warden: turn individual steps off if one misbehaves." + newLine + "step_rename=true" + newLine + "step_oath=true" + newLine + "step_vassals=true" + newLine + "allow_revoke=true" + newLine + "# Lets you grant a realm to a house holding no land inside it yet." + newLine + "relax_eligibility=true" + newLine + "# Asking foreign rulers to become your clients." + newLine + "allow_suzerainty=true" + newLine + "suzerainty_always_accepted=false" + newLine + "# Bringing a client realm into your empire." + newLine + "allow_absorb=true" + newLine + "absorb_gold=1000000" + newLine + "absorb_influence=1000" + newLine + "absorb_max_liberty=50" + newLine + "absorb_allow_unknown_liberty=false" + newLine + "" + newLine + "# ---------- Dragons ----------" + newLine + "# Leave blank to find Dragonstone by name." + newLine + "dragonstone_settlement_id=" + newLine + "# Every egg and claim is scaled by (1 - living dragons / this). The Dance" + newLine + "# begins with around 17 dragons, so at 20 the odds start very low and" + newLine + "# rise as the war thins them. Raise it to make dragons easier to get." + newLine + "world_dragon_capacity=20" + newLine + "egg_hatch_chance=30" + newLine + "# Every dragon death permanently dims hatching by this percent, to the floor." + newLine + "twilight_per_death=3" + newLine + "twilight_floor=20" + newLine + "# Chance a dragon falls with a rider killed in battle. A rider who dies in" + newLine + "# bed leaves his dragon riderless instead." + newLine + "dragon_falls_with_rider=60" + newLine + "rideable_age=19" + newLine + "claim_base=35" + newLine + "claim_per_valor=5" + newLine + "claim_riding_per_10=1" + newLine + "claim_killed_penalty=10" + newLine + "claim_wild_penalty=10" + newLine + "claim_death_chance=80" + newLine + "# false = a failed claim can never kill YOU, only your kin." + newLine + "player_claim_can_die=true" + newLine + "claim_dread=5" + newLine + "# Take dragons from anyone not bonded to them, once a week." + newLine + "enforce_bonds=true" + newLine + "seed_dance_riderless=true" + newLine + "# true = AI houses holding Dragonstone get cradle eggs too." + newLine + "eggs_for_all_houses=false" + newLine + "" + newLine + "# ---------- Names and titles ----------" + newLine + "# Put a style in front of a name, in conversation and the encyclopedia." + newLine + "# The name in your save file is never changed - only what is shown." + newLine + "titles_enabled=true" + newLine + "# The head of a house granted a realm wears its style: Warden of the North." + newLine + "title_wardens=true" + newLine + "# Anyone bonded to a dragon is styled Dragon Rider. A warden who also" + newLine + "# rides is styled by his realm, not his dragon." + newLine + "title_dragonriders=true" + newLine + "# ROT already names heroes Lord, Lady, Ser or King. In front of the name," + newLine + "# a lesser rank is replaced by the style - Dragon Rider Daemon Targaryen -" + newLine + "# and a royal one is left alone, because a king is styled by nothing else." + newLine + "# Set this to suffix for the other reading: Lord Daemon Targaryen, Dragon Rider." + newLine + "title_position=prefix" + newLine + "" + newLine + "# ---------- The council's names ----------" + newLine + "# Bellum Civile runs the privy council; this mod no longer adds duties to" + newLine + "# it. All that is left here is the naming: the six seats are shown with" + newLine + "# their Westerosi titles - Hand of the King, Master of Coin, Ships, Laws" + newLine + "# and Whisperers, and the Grand Maester. Only what is shown changes; the" + newLine + "# ids and your save are untouched." + newLine + "council_office_names=true" + newLine + "" + newLine + "# ---------- Hostages and wards ----------" + newLine + "wardship_enabled=true" + newLine + "# Taking a hostage. A ward costs nothing and earns a little." + newLine + "hostage_dread=8" + newLine + "hostage_honour_loss=4" + newLine + "ward_honour=4" + newLine + "ward_xp_per_week=60" + newLine + "# Sending them home." + newLine + "ward_release_honour=6" + newLine + "# The axe. Forfeit means their house broke faith while you held them," + newLine + "# and costs you no Honour at all. Without cause is the other thing." + newLine + "execute_forfeit_dread=15" + newLine + "execute_no_cause_honour_loss=25" + newLine + "" + newLine + "# ---------- Your heir ----------" + newLine + "succession_enabled=true" + newLine + "# Bellum gives every culture a succession law and enforces it on the heir" + newLine + "# screen, which is why most realms only ever offer you your eldest son." + newLine + "# true = you may name anyone of your blood instead. The law still stands;" + newLine + "# you are simply allowed to break it." + newLine + "unlock_heir_choice=true" + newLine + "unlawful_heir_penalty=20" + newLine + "# Changing a name you have already given. Naming one for the first time" + newLine + "# is free." + newLine + "rename_heir_cost=6" + newLine + "# The game picks a new clan leader by score, and gives +10 for being male," + newLine + "# +5 for being oldest and +5 for best skills, with nothing at all to stop a" + newLine + "# spouse who married in. A husband beats a daughter every time, and since a" + newLine + "# kingdom's leader IS its ruling clan's leader, that is the crown as well." + newLine + "# true = the heir you chose takes the seat regardless." + newLine + "enforce_named_heir=true" + newLine + "" + newLine + "# ---------- Baseborn children ----------" + newLine + "# A settlement menu option lets you take a room for the night. Nothing" + newLine + "# happens then; three years later a woman comes to your gate with a child" + newLine + "# who has your face. They are created outright rather than born, on purpose:" + newLine + "# RoT Dynasty files every real newborn as bastard-or-trueborn permanently," + newLine + "# and that verdict outranks everything, so acknowledging them could never show." + newLine + "baseborn_children=true" + newLine + "# The scandal is rather the point, but it can be turned off." + newLine + "baseborn_while_married=true" + newLine + "baseborn_night_cost=500" + newLine + "baseborn_cooldown_days=84" + newLine + "baseborn_max=4" + newLine + "baseborn_years_until=3" + newLine + "baseborn_child_min_age=14" + newLine + "baseborn_child_max_age=20" + newLine + "baseborn_night_min_age=18" + newLine + "# The other parent's age when you meet them." + newLine + "baseborn_other_min_age=20" + newLine + "baseborn_other_max_age=35" + newLine + "# The chance your spouse hears, and what it costs when they do." + newLine + "baseborn_whisper_chance=35" + newLine + "baseborn_whisper_relation=15" + newLine + "baseborn_whisper_honour=3" + newLine + newLine + "# Writing one into the book: what it costs with each of your trueborn" + newLine + "# kin, and with the realm. It gives them a claim your heir must answer for." + newLine + "legitimise_kin_relation=20" + newLine + "legitimise_standing=10" + newLine + newLine + "# How much of your sworn strength goes over when you die, by what you" + newLine + "# actually did for them. Only the last two found a kingdom or start a war." + newLine + "share_ignored=10" + newLine + "share_acknowledged=20" + newLine + "share_armed=33" + newLine + "share_both=50" + newLine + newLine + "# ---------- The Bastard's Banner ----------" + newLine + "# When the ruler of your house dies you are asked, once, whether a child" + newLine + "# they never acknowledged comes out of the dark. Say no and nothing ever" + newLine + "# happens. Say yes and they take one of your castles at random, a share of" + newLine + "# your sworn houses, and found a kingdom flying your arms with the colours" + newLine + "# reversed - and then you choose which of the two you play as." + newLine + "bastards_banner=true" + newLine + "# With no baseborn child at all, a stranger with your face turns up at the" + newLine + "# funeral instead. false = a ruler who never went looking for this simply" + newLine + "# dies, and nothing happens." + newLine + "bastard_stranger=true" + newLine + "# They declare war the morning they are proclaimed." + newLine + "bastard_declares_war=true" + newLine + "# How much of your sworn strength goes over, as a percentage." + newLine + "bastard_vassal_share=33" + newLine + "# Your house must hold at least this many towns or castles to be divided -" + newLine + "# otherwise you would be handing over your only seat." + newLine + "bastard_min_fiefs=2" + newLine + "# How old they are, worked back from the age of the ruler who sired them." + newLine + "bastard_born_years_before=22" + newLine + "bastard_min_age=20" + newLine + "bastard_max_age=45" + newLine + "# The tier their house starts at." + newLine + "bastard_house_tier=3" + newLine;
	}
}
}
