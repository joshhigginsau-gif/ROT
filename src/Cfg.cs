using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

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
	internal static bool CourtAnywhere = true;
	internal static TaleWorlds.InputSystem.InputKey? CourtFieldKey = TaleWorlds.InputSystem.InputKey.J;

	// ---------- Attainder ----------
	internal static bool Attainder = true;
	internal static bool AttainderOnMassacre = true;
	internal static int AttainderDread = 5;
	internal static int AttainderExecuteDread = 2;
	internal static int AttainderExecuteHonour = 1;

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

	// ---------- Tourneys ----------
	internal static bool Tourneys = true;
	// The purse for each of the three sizes of tourney, and what the feast
	// costs on top of it, as a percentage of the purse.
	internal static int TourneyPurseModest = 5000;
	internal static int TourneyPurseGreat = 20000;
	internal static int TourneyPurseLavish = 50000;
	internal static int TourneyFeastPercent = 40;
	internal static int TourneyCooldown = 84;
	// How many houses come, and how many of their young lords ride.
	internal static int TourneyGuests = 8;
	internal static int TourneyGuestRiders = 6;
	// What hosting earns, per size of tourney (x1, x2, x3).
	internal static int TourneyHostRelation = 3;
	internal static int TourneyHostRenown = 20;
	// The lists are not safe. Percent per lord riding; one death and one
	// maiming at most, each tourney.
	internal static int TourneyDeathChance = 2;
	internal static int TourneyMaimChance = 8;
	internal static bool TourneyKinCanDie = true;
	// What a death at YOUR tourney costs with the dead man's house.
	internal static int TourneyDeathRelation = 20;
	internal static int TourneyWinHonour = 2;
	internal static int TourneyHeirRelation = 5;
	// Crowning a Queen of Love and Beauty, and the chance a night after the
	// feast is found out by her house.
	internal static bool TourneyQueen = true;
	internal static int TourneyScandalChance = 40;
	// A baseborn child of yours who wins: this much more of your realm goes
	// with them per win (percent, three wins at most), and this many wins
	// crowns them even if you never gave them the sword or your name.
	internal static int TourneyBastardShare = 5;
	internal static int TourneyBastardCrowning = 2;

	// ---------- The King's Justice ----------
	internal static bool Law = true;
	// Lords who hate you bring charges - true or not - once in a while.
	internal static bool LawAiAccusations = true;
	internal static int LawHatred = -40;
	internal static int LawAccusationChance = 15;
	// A fine, per degree of the crime (1 to 3).
	internal static int LawFine = 5000;
	// Buying witnesses, and the chance the truth comes out once judged.
	internal static int LawFabricate = 20000;
	internal static int LawExposed = 35;
	// "I am the law": the chance the accuser's house rebels.
	internal static int LawRebelChance = 35;
	// Everyone who falls in a trial - you included - has this chance to die.
	internal static int TrialDeathChance = 50;
	internal static bool TrialPlayerCanDie = true;
	internal static int TrialHealth = 225;
	// A trial of seven takes this long to gather, and a lord must like you
	// this much to stand in it.
	internal static int TrialSevenGatherHours = 24;
	internal static int TrialSevenFriend = 30;

	// ---------- The Kingsguard ----------
	internal static bool Kingsguard = true;
	internal static int KgSize = 7;
	// The lowest tier of soldier who can be knighted into it.
	internal static int KgCommonerTier = 4;
	internal static int KgDismissHonour = 5;
	// Days an errand takes before the ride there and back is added.
	internal static int KgErrandDays = 4;
	internal static int KgLoot = 150;
	internal static int KgErrandDeath = 10;
	// When a bastard rises: the chance a knight who likes them better goes over.
	internal static int KgDefectChance = 50;
	// Dress a new knight in the white armour at the ceremony.
	internal static bool KgArmour = true;
	// Sworn knights walk the streets and the hall in the white armour.
	internal static bool KgArmourInTown = true;

	// ---------- Ravens ----------
	internal static bool Ravens = true;
	// Percent chance each week that a letter comes.
	internal static int RavensLetterChance = 25;
	internal static int RavensFeastDays = 5;
	internal static int RavensFeastCost = 5000;
	internal static int TreacheryCostBase = 30000;
	internal static int TreacheryCostPerGuest = 8000;
	internal static int TreacheryPrepDays = 7;
	// Honour ceiling lost for good after a massacre under your roof.
	internal static int TreacheryHonourCap = 20;

	// ---------- The small council and hosts ----------
	internal static bool Council = true;
	internal static int CouncilSitDays = 3;
	internal static int CouncilCooldownDays = 14;
	internal static int HostPriceLevy = 40;
	internal static int HostPriceMen = 80;
	internal static int HostPriceVeteran = 200;
	internal static int HostMaxMen = 30000;
	internal static int HostDays = 84;
	internal static int HostRenewPercent = 25;
	internal static bool AiHosts = true;
	internal static int AiHostWeeklyChance = 10;
	internal static int AiHostSpendPercent = 40;
	internal static int AiHostMinMen = 2000;
	internal static int AiHostMaxPerRealm = 1;

	// ---------- Parley ----------
	internal static bool Parley = true;
	internal static int ParleyTruceDays = 30;
	internal static int ParleyTruceHonour = 20;
	internal static int ParleyBreakHonour = 15;
	internal static int ParleyBreakDread = 10;
	internal static int ParleyBuyCastle = 1500000;
	internal static int ParleyBuyTown = 3000000;
	internal static int ParleyRenegeHonourable = 5;
	internal static int ParleyRenegeNeutral = 20;
	internal static int ParleyRenegeDishonourable = 40;

	// ---------- Knights ----------
	internal static bool Knights = true;
	internal static int KnightCost = 15000;
	internal static int KnightHonour = 1;
	internal static int KnightHouseTier = 1;
	internal static int KnightStartingMen = 20;
	internal static int KnightContractDays = 42;
	internal static int KnightLeaveRelation = -10;
	internal static int KnightLeaveChance = 15;
	internal static int KnightRehireCost = 5000;

	// ---------- The Iron Bank ----------
	internal static bool Bank = true;
	internal static int BankBaseCredit = 200000;
	internal static int BankCreditPerTier = 150000;
	internal static int BankRate = 20;
	internal static int BankPaymentDays = 21;
	internal static int BankPenaltyPercent = 10;
	internal static int BankFundEveryDays = 42;
	internal static int BankFundCap = 5000000;
	internal static bool BankAi = true;
	internal static int BankAiLoanCap = 1500000;
	internal static int BankAiDays = 84;

	// ---------- Exile ----------
	internal static bool Exile = true;
	internal static int ExileYearsMin = 10;
	internal static int ExileYearsMax = 20;
	internal static int ExileStartMen = 1500;
	internal static int ExileGrowthMen = 250;
	internal static int ExileMaxMen = 8000;
	internal static int ExileFundCap = 3000000;
	internal static int ExileMaxGenerations = 5;
	internal static bool ExileHireable = true;

	// ---------- Generals ----------
	internal static bool Generals = true;
	internal static int HostUpkeepPercent = 20;
	internal static int AmbushMinPercent = 8;
	internal static int AmbushMaxPercent = 20;
	internal static int ScorpionBase = 15;
	internal static int ScorpionDorneBonus = 20;
	internal static int ScorpionMax = 75;
	internal static int DragonBurnMin = 20;
	internal static int DragonBurnMax = 45;
	internal static int DragonRiderFall = 50;
	internal static bool AiGenerals = true;
	internal static bool AiDragonStrikes = true;

	// ---------- Abdication ----------
	internal static bool Abdication = true;
	internal static int AbdicationCoin = 1000;
	internal static int AbdicationUnlawfulShare = 15;
	internal static int AbdicationWarRelation = 10;
	internal static int AbdicationOldHouseRelation = 100;

	// ---------- Sworn houses ----------
	internal static bool Sworn = true;
	internal static bool SwornAi = true;
	internal static int SwornRaiseCost = 50000;
	internal static int SwornInviteCost = 15000;
	internal static int SwornAiRaiseCost = 30000;
	internal static int SwornManorIncomePercent = 50;
	internal static int SwornWardenCooldown = 168;
	internal static int SwornWorldCap = 0;
	internal static int SwornFollowChance = 75;

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
			AppendSection(path, "bastards_banner", "# ---------- The Bastard's Banner", "# ---------- Tourneys");
			AppendSection(path, "tourneys_enabled", "# ---------- Tourneys", "# ---------- The King's Justice");
			AppendSection(path, "law_enabled", "# ---------- The King's Justice", "# ---------- The Kingsguard");
			AppendSection(path, "kingsguard_enabled", "# ---------- The Kingsguard", "# ---------- Ravens");
			AppendSection(path, "ravens_enabled", "# ---------- Ravens", "# ---------- The small council");
			AppendSection(path, "council_enabled", "# ---------- The small council", "# ---------- Parley");
			AppendSection(path, "parley_enabled", "# ---------- Parley", "# ---------- Knights");
			AppendSection(path, "knights_enabled", "# ---------- Knights", "# ---------- The Iron Bank");
			AppendSection(path, "bank_enabled", "# ---------- The Iron Bank", "# ---------- Exile");
			AppendSection(path, "exile_enabled", "# ---------- Exile", "# ---------- Generals");
			AppendSection(path, "generals_enabled", "# ---------- Generals", "# ---------- Abdication");
			AppendSection(path, "abdication_enabled", "# ---------- Abdication", "# ---------- Sworn houses");
			AppendSection(path, "sworn_enabled", "# ---------- Sworn houses", "# ---------- Attainder");
			AppendSection(path, "attainder_enabled", "# ---------- Attainder", null);
			AppendMissingKeys(path);
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
				case "court_anywhere":
					CourtAnywhere = flag2;
					break;
				case "court_field_key":
				{
					TaleWorlds.InputSystem.InputKey k;
					string v = (text4 ?? "").Trim();
					CourtFieldKey = (v.Length > 0 && !v.Equals("none", StringComparison.OrdinalIgnoreCase) && Enum.TryParse<TaleWorlds.InputSystem.InputKey>(v, true, out k)) ? new TaleWorlds.InputSystem.InputKey?(k) : null;
					break;
				}
				case "attainder_enabled":
					Attainder = flag2;
					break;
				case "attainder_on_massacre":
					AttainderOnMassacre = flag2;
					break;
				case "attainder_dread":
					if (flag) { AttainderDread = Math.Max(0, result); }
					break;
				case "attainder_execute_dread":
					if (flag) { AttainderExecuteDread = Math.Max(0, result); }
					break;
				case "attainder_execute_honour":
					if (flag) { AttainderExecuteHonour = Math.Max(0, result); }
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
				case "tourneys_enabled":
					Tourneys = flag2;
					break;
				case "tourney_purse_modest":
					if (flag) { TourneyPurseModest = Math.Max(0, result); }
					break;
				case "tourney_purse_great":
					if (flag) { TourneyPurseGreat = Math.Max(0, result); }
					break;
				case "tourney_purse_lavish":
					if (flag) { TourneyPurseLavish = Math.Max(0, result); }
					break;
				case "tourney_feast_percent":
					if (flag) { TourneyFeastPercent = Math.Max(0, result); }
					break;
				case "tourney_cooldown_days":
					if (flag) { TourneyCooldown = Math.Max(0, result); }
					break;
				case "tourney_guests":
					if (flag) { TourneyGuests = Math.Max(0, Math.Min(30, result)); }
					break;
				case "tourney_guest_riders":
					if (flag) { TourneyGuestRiders = Math.Max(0, Math.Min(20, result)); }
					break;
				case "tourney_host_relation":
					if (flag) { TourneyHostRelation = Math.Max(0, result); }
					break;
				case "tourney_host_renown":
					if (flag) { TourneyHostRenown = Math.Max(0, result); }
					break;
				case "tourney_death_chance":
					if (flag) { TourneyDeathChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "tourney_maim_chance":
					if (flag) { TourneyMaimChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "tourney_kin_can_die":
					TourneyKinCanDie = flag2;
					break;
				case "tourney_death_relation":
					if (flag) { TourneyDeathRelation = Math.Max(0, result); }
					break;
				case "tourney_win_honour":
					if (flag) { TourneyWinHonour = Math.Max(0, result); }
					break;
				case "tourney_heir_relation":
					if (flag) { TourneyHeirRelation = Math.Max(0, result); }
					break;
				case "tourney_queen":
					TourneyQueen = flag2;
					break;
				case "tourney_scandal_chance":
					if (flag) { TourneyScandalChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "tourney_bastard_share":
					if (flag) { TourneyBastardShare = Math.Max(0, Math.Min(30, result)); }
					break;
				case "tourney_bastard_crowning_wins":
					if (flag) { TourneyBastardCrowning = Math.Max(0, result); }
					break;
				case "law_enabled":
					Law = flag2;
					break;
				case "law_ai_accusations":
					LawAiAccusations = flag2;
					break;
				case "law_hatred":
					if (flag) { LawHatred = Math.Max(-100, Math.Min(100, result)); }
					break;
				case "law_accusation_chance":
					if (flag) { LawAccusationChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "law_fine":
					if (flag) { LawFine = Math.Max(0, result); }
					break;
				case "law_fabricate_cost":
					if (flag) { LawFabricate = Math.Max(0, result); }
					break;
				case "law_exposed_chance":
					if (flag) { LawExposed = Math.Max(0, Math.Min(100, result)); }
					break;
				case "law_rebel_chance":
					if (flag) { LawRebelChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "trial_death_chance":
					if (flag) { TrialDeathChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "trial_player_can_die":
					TrialPlayerCanDie = flag2;
					break;
				case "trial_health":
					if (flag) { TrialHealth = Math.Max(50, Math.Min(1000, result)); }
					break;
				case "kingsguard_enabled":
					Kingsguard = flag2;
					break;
				case "kingsguard_size":
					if (flag) { KgSize = Math.Max(1, Math.Min(20, result)); }
					break;
				case "kingsguard_commoner_tier":
					if (flag) { KgCommonerTier = Math.Max(1, Math.Min(7, result)); }
					break;
				case "kingsguard_dismiss_honour":
					if (flag) { KgDismissHonour = Math.Max(0, result); }
					break;
				case "kingsguard_errand_days":
					if (flag) { KgErrandDays = Math.Max(1, result); }
					break;
				case "kingsguard_loot":
					if (flag) { KgLoot = Math.Max(0, result); }
					break;
				case "kingsguard_errand_death":
					if (flag) { KgErrandDeath = Math.Max(0, Math.Min(100, result)); }
					break;
				case "kingsguard_defect_chance":
					if (flag) { KgDefectChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "trial_seven_gather_hours":
					if (flag) { TrialSevenGatherHours = Math.Max(1, Math.Min(240, result)); }
					break;
				case "trial_seven_friend":
					if (flag) { TrialSevenFriend = Math.Max(-100, Math.Min(100, result)); }
					break;
				case "kingsguard_armour_in_town":
					KgArmourInTown = flag2;
					break;
				case "council_enabled":
					Council = flag2;
					break;
				case "council_sit_days":
					if (flag) { CouncilSitDays = Math.Max(1, Math.Min(30, result)); }
					break;
				case "council_cooldown_days":
					if (flag) { CouncilCooldownDays = Math.Max(0, Math.Min(365, result)); }
					break;
				case "host_price_levy":
					if (flag) { HostPriceLevy = Math.Max(1, result); }
					break;
				case "host_price_men":
					if (flag) { HostPriceMen = Math.Max(1, result); }
					break;
				case "host_price_veteran":
					if (flag) { HostPriceVeteran = Math.Max(1, result); }
					break;
				case "host_max_men":
					if (flag) { HostMaxMen = Math.Max(100, result); }
					break;
				case "host_days":
					if (flag) { HostDays = Math.Max(1, result); }
					break;
				case "host_renew_percent":
					if (flag) { HostRenewPercent = Math.Max(0, Math.Min(1000, result)); }
					break;
				case "ai_hosts_enabled":
					AiHosts = flag2;
					break;
				case "ai_host_weekly_chance":
					if (flag) { AiHostWeeklyChance = Clamp(result); }
					break;
				case "ai_host_spend_percent":
					if (flag) { AiHostSpendPercent = Clamp(result); }
					break;
				case "ai_host_min_men":
					if (flag) { AiHostMinMen = Math.Max(100, result); }
					break;
				case "ai_host_max_per_realm":
					if (flag) { AiHostMaxPerRealm = Math.Max(0, Math.Min(10, result)); }
					break;
				case "parley_enabled":
					Parley = flag2;
					break;
				case "parley_truce_days":
					if (flag) { ParleyTruceDays = Math.Max(0, Math.Min(365, result)); }
					break;
				case "parley_truce_honour":
					if (flag) { ParleyTruceHonour = Clamp(result); }
					break;
				case "parley_break_honour":
					if (flag) { ParleyBreakHonour = Clamp(result); }
					break;
				case "parley_break_dread":
					if (flag) { ParleyBreakDread = Clamp(result); }
					break;
				case "parley_buy_castle":
					if (flag) { ParleyBuyCastle = Math.Max(0, result); }
					break;
				case "parley_buy_town":
					if (flag) { ParleyBuyTown = Math.Max(0, result); }
					break;
				case "parley_renege_chance_honourable":
					if (flag) { ParleyRenegeHonourable = Clamp(result); }
					break;
				case "parley_renege_chance_neutral":
					if (flag) { ParleyRenegeNeutral = Clamp(result); }
					break;
				case "parley_renege_chance_dishonourable":
					if (flag) { ParleyRenegeDishonourable = Clamp(result); }
					break;
				case "knights_enabled":
					Knights = flag2;
					break;
				case "knight_cost":
					if (flag) { KnightCost = Math.Max(0, result); }
					break;
				case "knight_honour":
					if (flag) { KnightHonour = Clamp(result); }
					break;
				case "knight_house_tier":
					if (flag) { KnightHouseTier = Math.Max(0, Math.Min(6, result)); }
					break;
				case "knight_starting_men":
					if (flag) { KnightStartingMen = Math.Max(0, Math.Min(500, result)); }
					break;
				case "knight_contract_days":
					if (flag) { KnightContractDays = Math.Max(1, result); }
					break;
				case "knight_leave_relation":
					if (flag) { KnightLeaveRelation = Math.Max(-100, Math.Min(100, result)); }
					break;
				case "knight_leave_chance":
					if (flag) { KnightLeaveChance = Clamp(result); }
					break;
				case "knight_rehire_cost":
					if (flag) { KnightRehireCost = Math.Max(0, result); }
					break;
				case "bank_enabled":
					Bank = flag2;
					break;
				case "bank_base_credit":
					if (flag) { BankBaseCredit = Math.Max(0, result); }
					break;
				case "bank_credit_per_tier":
					if (flag) { BankCreditPerTier = Math.Max(0, result); }
					break;
				case "bank_rate":
					if (flag) { BankRate = Math.Max(0, Math.Min(200, result)); }
					break;
				case "bank_payment_days":
					if (flag) { BankPaymentDays = Math.Max(1, result); }
					break;
				case "bank_penalty_percent":
					if (flag) { BankPenaltyPercent = Math.Max(0, Math.Min(100, result)); }
					break;
				case "bank_fund_every_days":
					if (flag) { BankFundEveryDays = Math.Max(1, result); }
					break;
				case "bank_fund_cap":
					if (flag) { BankFundCap = Math.Max(0, result); }
					break;
				case "bank_ai_borrowing":
					BankAi = flag2;
					break;
				case "bank_ai_loan_cap":
					if (flag) { BankAiLoanCap = Math.Max(0, result); }
					break;
				case "bank_ai_days":
					if (flag) { BankAiDays = Math.Max(1, result); }
					break;
				case "exile_enabled":
					Exile = flag2;
					break;
				case "exile_years_min":
					if (flag) { ExileYearsMin = Math.Max(0, Math.Min(100, result)); }
					break;
				case "exile_years_max":
					if (flag) { ExileYearsMax = Math.Max(0, Math.Min(100, result)); }
					break;
				case "exile_start_men":
					if (flag) { ExileStartMen = Math.Max(0, result); }
					break;
				case "exile_growth_men":
					if (flag) { ExileGrowthMen = Math.Max(0, result); }
					break;
				case "exile_max_men":
					if (flag) { ExileMaxMen = Math.Max(0, result); }
					break;
				case "exile_fund_cap":
					if (flag) { ExileFundCap = Math.Max(0, result); }
					break;
				case "exile_max_generations":
					if (flag) { ExileMaxGenerations = Math.Max(1, Math.Min(50, result)); }
					break;
				case "exile_company_hireable":
					ExileHireable = flag2;
					break;
				case "generals_enabled":
					Generals = flag2;
					break;
				case "host_upkeep_percent":
					if (flag) { HostUpkeepPercent = Math.Max(0, Math.Min(500, result)); }
					break;
				case "ambush_min_percent":
					if (flag) { AmbushMinPercent = Math.Max(0, Math.Min(90, result)); }
					break;
				case "ambush_max_percent":
					if (flag) { AmbushMaxPercent = Math.Max(0, Math.Min(90, result)); }
					break;
				case "scorpion_base":
					if (flag) { ScorpionBase = Math.Max(0, Math.Min(100, result)); }
					break;
				case "scorpion_dorne_bonus":
					if (flag) { ScorpionDorneBonus = Math.Max(0, Math.Min(100, result)); }
					break;
				case "scorpion_max":
					if (flag) { ScorpionMax = Math.Max(0, Math.Min(100, result)); }
					break;
				case "dragon_burn_min":
					if (flag) { DragonBurnMin = Math.Max(0, Math.Min(95, result)); }
					break;
				case "dragon_burn_max":
					if (flag) { DragonBurnMax = Math.Max(0, Math.Min(95, result)); }
					break;
				case "dragon_rider_fall":
					if (flag) { DragonRiderFall = Math.Max(0, Math.Min(100, result)); }
					break;
				case "ai_generals":
					AiGenerals = flag2;
					break;
				case "ai_dragon_strikes":
					AiDragonStrikes = flag2;
					break;
				case "sworn_enabled":
					Sworn = flag2;
					break;
				case "sworn_ai":
					SwornAi = flag2;
					break;
				case "sworn_raise_cost":
					if (flag) { SwornRaiseCost = Math.Max(0, result); }
					break;
				case "sworn_invite_cost":
					if (flag) { SwornInviteCost = Math.Max(0, result); }
					break;
				case "sworn_ai_raise_cost":
					if (flag) { SwornAiRaiseCost = Math.Max(0, result); }
					break;
				case "sworn_manor_income_percent":
					if (flag) { SwornManorIncomePercent = Math.Max(0, Math.Min(100, result)); }
					break;
				case "sworn_warden_cooldown_days":
					if (flag) { SwornWardenCooldown = Math.Max(0, result); }
					break;
				case "sworn_world_cap":
					if (flag) { SwornWorldCap = Math.Max(0, result); }
					break;
				case "sworn_follow_chance":
					if (flag) { SwornFollowChance = Math.Max(0, Math.Min(100, result)); }
					break;
				case "abdication_enabled":
					Abdication = flag2;
					break;
				case "abdication_coin":
					if (flag) { AbdicationCoin = Math.Max(0, result); }
					break;
				case "abdication_unlawful_share":
					if (flag) { AbdicationUnlawfulShare = Math.Max(0, Math.Min(100, result)); }
					break;
				case "abdication_war_relation":
					if (flag) { AbdicationWarRelation = Math.Max(0, Math.Min(100, result)); }
					break;
				case "abdication_old_house_relation":
					if (flag) { AbdicationOldHouseRelation = Math.Max(-100, Math.Min(100, result)); }
					break;
				case "ravens_enabled":
					Ravens = flag2;
					break;
				case "ravens_letter_chance":
					if (flag) { RavensLetterChance = Clamp(result); }
					break;
				case "ravens_feast_days":
					if (flag) { RavensFeastDays = Math.Max(1, Math.Min(60, result)); }
					break;
				case "ravens_feast_cost":
					if (flag) { RavensFeastCost = Math.Max(0, result); }
					break;
				case "treachery_cost_base":
					if (flag) { TreacheryCostBase = Math.Max(0, result); }
					break;
				case "treachery_cost_per_guest":
					if (flag) { TreacheryCostPerGuest = Math.Max(0, result); }
					break;
				case "treachery_prep_days":
					if (flag) { TreacheryPrepDays = Math.Max(0, Math.Min(60, result)); }
					break;
				case "treachery_honour_cap":
					if (flag) { TreacheryHonourCap = Clamp(result); }
					break;
				case "kingsguard_armour":
					KgArmour = flag2;
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

	// Keys added to a section that already exists.
	//
	// AppendSection works per section: once a section's marker is in the file
	// it is never touched again, so a key added to it in a later version never
	// reached anybody's config. The defaults still applied, so nothing broke -
	// but the setting could not be found without deleting the file. This adds
	// each missing key with the comment lines that explain it.
	private static void AppendMissingKeys(string path)
	{
		try
		{
			string[] have = File.ReadAllLines(path);
			HashSet<string> keys = new HashSet<string>();
			foreach (string line in have)
			{
				string t = line.Trim().ToLowerInvariant().Replace(" ", "");
				if (!t.StartsWith("#") && t.IndexOf('=') > 0)
				{
					keys.Add(t.Substring(0, t.IndexOf('=')));
				}
			}
			StringBuilder add = new StringBuilder();
			List<string> comments = new List<string>();
			foreach (string raw in Default().Split(new string[1] { Environment.NewLine }, StringSplitOptions.None))
			{
				string line = raw.Trim();
				if (line.StartsWith("# ----------") || line.Length == 0)
				{
					comments.Clear();
					continue;
				}
				if (line.StartsWith("#"))
				{
					comments.Add(line);
					continue;
				}
				int eq = line.IndexOf('=');
				if (eq > 0 && !keys.Contains(line.Substring(0, eq).ToLowerInvariant()))
				{
					foreach (string c in comments)
					{
						add.Append(c).Append(Environment.NewLine);
					}
					add.Append(line).Append(Environment.NewLine);
				}
				comments.Clear();
			}
			if (add.Length > 0)
			{
				File.AppendAllText(path, Environment.NewLine + "# ---------- Added by a newer version ----------" + Environment.NewLine + add);
			}
		}
		catch
		{
		}
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
		return "# Wardens & Dragons" + newLine + "" + newLine + "# Where a ruler starts, and where both numbers drift back to." + newLine + "start_honour=50" + newLine + "start_dread=10" + newLine + "" + newLine + "# Seasonal drift. A Bannerlord year is 84 days, so a season is 21." + newLine + "drift_per_season=1" + newLine + "days_per_season=21" + newLine + "" + newLine + "# Executing a captured lord." + newLine + "execute_honour_loss=10" + newLine + "execute_dread_gain=10" + newLine + "" + newLine + "# false = hold court at any settlement your house owns." + newLine + "# true  = only at your house's home seat." + newLine + "court_capital_only=false" + newLine + "# true = hold court in any town, castle or village, and in the field." + newLine + "court_anywhere=true" + newLine + "# The key that holds court in your tent on the campaign map (none = off)." + newLine + "court_field_key=J" + newLine + "" + newLine + "# Show a message whenever Honour or Dread changes." + newLine + "notify_changes=true" + newLine + "" + newLine + "# How many deeds the court remembers." + newLine + "ledger_length=12" + newLine + "" + newLine + "# The court tells you what is actually waiting on a decision, and says" + newLine + "# nothing at all when nothing is." + newLine + "court_attention=true" + newLine + "court_attention_max=6" + newLine + "" + newLine + "# ---------- Harrenhal ----------" + newLine + "# Leave blank to find Harrenhal by name. Set a settlement id if it is not found." + newLine + "harrenhal_settlement_id=" + newLine + "# The curse builds on whoever holds it, 0 to 100, and fades once they let it go." + newLine + "curse_per_year=8" + newLine + "curse_decay_per_year=10" + newLine + "# FOR TESTING ONLY: multiplies how fast the curse builds and fades." + newLine + "# 50 reaches every stage within a few weeks. Put it back to 1 to play." + newLine + "curse_test_speed=1" + newLine + "# Dread gained when you first cannot sleep there." + newLine + "sleepless_dread=5" + newLine + "# At 75 and above, each year: chance (percent) of a death in the family," + newLine + "# and of a fire costing Harrenhal this percent of its prosperity." + newLine + "kin_death_chance=10" + newLine + "fire_chance=30" + newLine + "fire_prosperity_loss_percent=10" + newLine + "# false = the curse never kills members of YOUR house." + newLine + "curse_can_kill_player_family=true" + newLine + "# A Bannerlord year is 84 days." + newLine + "days_per_year=84" + newLine + "" + newLine + "# ---------- Wardens and oaths ----------" + newLine + "# Tribute each year, per fief the house or realm holds." + newLine + "tribute_fealty=1000" + newLine + "tribute_tribute=3000" + newLine + "tribute_marriage=500" + newLine + "tribute_duress=5000" + newLine + "# The most an oath can move a client's liberty, up or down." + newLine + "liberty_cap=40" + newLine + "lighten_honour=2" + newLine + "release_honour=5" + newLine + "revoke_honour_loss=10" + newLine + "revoke_dread_gain=5" + newLine + "# Each dragon rider in your house adds this to a threat, up to the cap." + newLine + "dragon_threat_per_rider=10" + newLine + "dragon_threat_cap=40" + newLine + "# Naming a warden: turn individual steps off if one misbehaves." + newLine + "step_rename=true" + newLine + "step_oath=true" + newLine + "step_vassals=true" + newLine + "allow_revoke=true" + newLine + "# Lets you grant a realm to a house holding no land inside it yet." + newLine + "relax_eligibility=true" + newLine + "# Asking foreign rulers to become your clients." + newLine + "allow_suzerainty=true" + newLine + "suzerainty_always_accepted=false" + newLine + "# Bringing a client realm into your empire." + newLine + "allow_absorb=true" + newLine + "absorb_gold=1000000" + newLine + "absorb_influence=1000" + newLine + "absorb_max_liberty=50" + newLine + "absorb_allow_unknown_liberty=false" + newLine + "" + newLine + "# ---------- Dragons ----------" + newLine + "# Leave blank to find Dragonstone by name." + newLine + "dragonstone_settlement_id=" + newLine + "# Every egg and claim is scaled by (1 - living dragons / this). The Dance" + newLine + "# begins with around 17 dragons, so at 20 the odds start very low and" + newLine + "# rise as the war thins them. Raise it to make dragons easier to get." + newLine + "world_dragon_capacity=20" + newLine + "egg_hatch_chance=30" + newLine + "# Every dragon death permanently dims hatching by this percent, to the floor." + newLine + "twilight_per_death=3" + newLine + "twilight_floor=20" + newLine + "# Chance a dragon falls with a rider killed in battle. A rider who dies in" + newLine + "# bed leaves his dragon riderless instead." + newLine + "dragon_falls_with_rider=60" + newLine + "rideable_age=19" + newLine + "claim_base=35" + newLine + "claim_per_valor=5" + newLine + "claim_riding_per_10=1" + newLine + "claim_killed_penalty=10" + newLine + "claim_wild_penalty=10" + newLine + "claim_death_chance=80" + newLine + "# false = a failed claim can never kill YOU, only your kin." + newLine + "player_claim_can_die=true" + newLine + "claim_dread=5" + newLine + "# Take dragons from anyone not bonded to them, once a week." + newLine + "enforce_bonds=true" + newLine + "seed_dance_riderless=true" + newLine + "# true = AI houses holding Dragonstone get cradle eggs too." + newLine + "eggs_for_all_houses=false" + newLine + "" + newLine + "# ---------- Names and titles ----------" + newLine + "# Put a style in front of a name, in conversation and the encyclopedia." + newLine + "# The name in your save file is never changed - only what is shown." + newLine + "titles_enabled=true" + newLine + "# The head of a house granted a realm wears its style: Warden of the North." + newLine + "title_wardens=true" + newLine + "# Anyone bonded to a dragon is styled Dragon Rider. A warden who also" + newLine + "# rides is styled by his realm, not his dragon." + newLine + "title_dragonriders=true" + newLine + "# ROT already names heroes Lord, Lady, Ser or King. In front of the name," + newLine + "# a lesser rank is replaced by the style - Dragon Rider Daemon Targaryen -" + newLine + "# and a royal one is left alone, because a king is styled by nothing else." + newLine + "# Set this to suffix for the other reading: Lord Daemon Targaryen, Dragon Rider." + newLine + "title_position=prefix" + newLine + "" + newLine + "# ---------- The council's names ----------" + newLine + "# Bellum Civile runs the privy council; this mod no longer adds duties to" + newLine + "# it. All that is left here is the naming: the six seats are shown with" + newLine + "# their Westerosi titles - Hand of the King, Master of Coin, Ships, Laws" + newLine + "# and Whisperers, and the Grand Maester. Only what is shown changes; the" + newLine + "# ids and your save are untouched." + newLine + "council_office_names=true" + newLine + "" + newLine + "# ---------- Hostages and wards ----------" + newLine + "wardship_enabled=true" + newLine + "# Taking a hostage. A ward costs nothing and earns a little." + newLine + "hostage_dread=8" + newLine + "hostage_honour_loss=4" + newLine + "ward_honour=4" + newLine + "ward_xp_per_week=60" + newLine + "# Sending them home." + newLine + "ward_release_honour=6" + newLine + "# The axe. Forfeit means their house broke faith while you held them," + newLine + "# and costs you no Honour at all. Without cause is the other thing." + newLine + "execute_forfeit_dread=15" + newLine + "execute_no_cause_honour_loss=25" + newLine + "" + newLine + "# ---------- Your heir ----------" + newLine + "succession_enabled=true" + newLine + "# Bellum gives every culture a succession law and enforces it on the heir" + newLine + "# screen, which is why most realms only ever offer you your eldest son." + newLine + "# true = you may name anyone of your blood instead. The law still stands;" + newLine + "# you are simply allowed to break it." + newLine + "unlock_heir_choice=true" + newLine + "unlawful_heir_penalty=20" + newLine + "# Changing a name you have already given. Naming one for the first time" + newLine + "# is free." + newLine + "rename_heir_cost=6" + newLine + "# The game picks a new clan leader by score, and gives +10 for being male," + newLine + "# +5 for being oldest and +5 for best skills, with nothing at all to stop a" + newLine + "# spouse who married in. A husband beats a daughter every time, and since a" + newLine + "# kingdom's leader IS its ruling clan's leader, that is the crown as well." + newLine + "# true = the heir you chose takes the seat regardless." + newLine + "enforce_named_heir=true" + newLine + "" + newLine + "# ---------- Baseborn children ----------" + newLine + "# A settlement menu option lets you take a room for the night. Nothing" + newLine + "# happens then; three years later a woman comes to your gate with a child" + newLine + "# who has your face. They are created outright rather than born, on purpose:" + newLine + "# RoT Dynasty files every real newborn as bastard-or-trueborn permanently," + newLine + "# and that verdict outranks everything, so acknowledging them could never show." + newLine + "baseborn_children=true" + newLine + "# The scandal is rather the point, but it can be turned off." + newLine + "baseborn_while_married=true" + newLine + "baseborn_night_cost=500" + newLine + "baseborn_cooldown_days=84" + newLine + "baseborn_max=4" + newLine + "baseborn_years_until=3" + newLine + "baseborn_child_min_age=14" + newLine + "baseborn_child_max_age=20" + newLine + "baseborn_night_min_age=18" + newLine + "# The other parent's age when you meet them." + newLine + "baseborn_other_min_age=20" + newLine + "baseborn_other_max_age=35" + newLine + "# The chance your spouse hears, and what it costs when they do." + newLine + "baseborn_whisper_chance=35" + newLine + "baseborn_whisper_relation=15" + newLine + "baseborn_whisper_honour=3" + newLine + newLine + "# Writing one into the book: what it costs with each of your trueborn" + newLine + "# kin, and with the realm. It gives them a claim your heir must answer for." + newLine + "legitimise_kin_relation=20" + newLine + "legitimise_standing=10" + newLine + newLine + "# How much of your sworn strength goes over when you die, by what you" + newLine + "# actually did for them. Only the last two found a kingdom or start a war." + newLine + "share_ignored=10" + newLine + "share_acknowledged=20" + newLine + "share_armed=33" + newLine + "share_both=50" + newLine + newLine + "# ---------- The Bastard's Banner ----------" + newLine + "# When the ruler of your house dies you are asked, once, whether a child" + newLine + "# they never acknowledged comes out of the dark. Say no and nothing ever" + newLine + "# happens. Say yes and they take one of your castles at random, a share of" + newLine + "# your sworn houses, and found a kingdom flying your arms with the colours" + newLine + "# reversed - and then you choose which of the two you play as." + newLine + "bastards_banner=true" + newLine + "# With no baseborn child at all, a stranger with your face turns up at the" + newLine + "# funeral instead. false = a ruler who never went looking for this simply" + newLine + "# dies, and nothing happens." + newLine + "bastard_stranger=true" + newLine + "# They declare war the morning they are proclaimed." + newLine + "bastard_declares_war=true" + newLine + "# How much of your sworn strength goes over, as a percentage." + newLine + "bastard_vassal_share=33" + newLine + "# Your house must hold at least this many towns or castles to be divided -" + newLine + "# otherwise you would be handing over your only seat." + newLine + "bastard_min_fiefs=2" + newLine + "# How old they are, worked back from the age of the ruler who sired them." + newLine + "bastard_born_years_before=22" + newLine + "bastard_min_age=20" + newLine + "bastard_max_age=45" + newLine + "# The tier their house starts at." + newLine + "bastard_house_tier=3" + newLine + "" + newLine + "# ---------- Tourneys ----------" + newLine + "# Call one from your court, in a town your house holds. You choose the purse" + newLine + "# and who rides for your house; guest houses send their young lords. Ride in" + newLine + "# it yourself or let it be decided without you - either way the lists have" + newLine + "# consequences." + newLine + "tourneys_enabled=true" + newLine + "# The purse for a modest, great and lavish tourney, and the feast on top of it." + newLine + "tourney_purse_modest=5000" + newLine + "tourney_purse_great=20000" + newLine + "tourney_purse_lavish=50000" + newLine + "tourney_feast_percent=40" + newLine + "# One a year." + newLine + "tourney_cooldown_days=84" + newLine + "# How many houses come, and how many of their young lords are brought to ride." + newLine + "tourney_guests=8" + newLine + "tourney_guest_riders=6" + newLine + "# What hosting earns with each guest house and in renown, per size (x1, x2, x3)." + newLine + "tourney_host_relation=3" + newLine + "tourney_host_renown=20" + newLine + "# The lists are not safe. Percent chance per lord riding, at a tourney you host" + newLine + "# or ride in yourself. One death and one maiming at most per tourney." + newLine + "tourney_death_chance=2" + newLine + "tourney_maim_chance=8" + newLine + "# false = your own blood is never killed in the lists. Companions never are." + newLine + "tourney_kin_can_die=true" + newLine + "# A death at a tourney YOU hosted: what it costs with the dead man's house." + newLine + "tourney_death_relation=20" + newLine + "# Winning, and your heir winning (relation with every guest house)." + newLine + "tourney_win_honour=2" + newLine + "tourney_heir_relation=5" + newLine + "# Win and you crown a Queen (or King) of Love and Beauty. Crown the wrong one" + newLine + "# and there may be a night after the feast - and a chance her house finds out." + newLine + "tourney_queen=true" + newLine + "tourney_scandal_chance=40" + newLine + "# A baseborn child of yours who wins takes this much more of your realm with" + newLine + "# them per win (percent, three wins at most). This many wins and the crowds" + newLine + "# make them a king even if you never gave them the sword." + newLine + "tourney_bastard_share=5" + newLine + "tourney_bastard_crowning_wins=2" + newLine + "" + newLine + "# ---------- The King's Justice ----------" + newLine + "# Crimes are written down as they happen - treason, murder, kinslaying, the" + newLine + "# killing of captives - and whoever holds the court can hear them. The" + newLine + "# accused may demand trial by combat, or for grave charges a trial of seven," + newLine + "# fought for real in a town's arena when you are in it. And the charge can" + newLine + "# come to you: from your liege, or from your own lords if you are the crown." + newLine + "law_enabled=true" + newLine + "# Lords at this relation or worse may accuse you, true or not, with this" + newLine + "# percent chance every four weeks. One charge against you at a time." + newLine + "law_ai_accusations=true" + newLine + "law_hatred=-40" + newLine + "law_accusation_chance=15" + newLine + "# A fine, per degree of the crime (1 to 3)." + newLine + "law_fine=5000" + newLine + "# Buying witnesses, and the chance the truth comes out once it is judged." + newLine + "law_fabricate_cost=20000" + newLine + "law_exposed_chance=35" + newLine + "# \"I am the law\": the chance the accuser's house rebels." + newLine + "law_rebel_chance=35" + newLine + "# Everyone who falls in a trial - you included - has this percent chance to" + newLine + "# die. Whoever loses, the verdict goes against their side." + newLine + "trial_death_chance=50" + newLine + "# false = you are carried out alive; everyone else still takes their chance." + newLine + "trial_player_can_die=true" + newLine + "# Health of every fighter in a trial. 225 is the game's own duel." + newLine + "trial_health=225" + newLine + "# A trial of seven takes this many hours to gather, and a lord must be at" + newLine + "# this relation or better to stand with you." + newLine + "trial_seven_gather_hours=24" + newLine + "trial_seven_friend=30" + newLine + "" + newLine + "# ---------- The Kingsguard ----------" + newLine + "# Seven knights sworn for life: no lands, no marriage, no inheritance. They" + newLine + "# follow you in settlements and fight beside you, and ride out on the crown's" + newLine + "# errands. A knight who marries or leaves is an oathbreaker." + newLine + "kingsguard_enabled=true" + newLine + "kingsguard_size=7" + newLine + "# The lowest tier of soldier who can be knighted into it." + newLine + "kingsguard_commoner_tier=4" + newLine + "# Honour lost for taking back a white cloak." + newLine + "kingsguard_dismiss_honour=5" + newLine + "# An errand's length before the ride there and back, the plunder per camp," + newLine + "# and the chance a knight who fails dies of it." + newLine + "kingsguard_errand_days=4" + newLine + "kingsguard_loot=150" + newLine + "kingsguard_errand_death=10" + newLine + "# When a bastard rises, the chance a knight who likes them better goes over." + newLine + "kingsguard_defect_chance=50" + newLine + "# Dress a newly sworn knight in the white armour at the ceremony." + newLine + "kingsguard_armour=true" + newLine + "# Sworn knights walk the streets and your hall in their armour." + newLine + "kingsguard_armour_in_town=true" + newLine + "" + newLine + "# ---------- Ravens ----------" + newLine + "# Letters between houses: marriages, feasts, word from kin, a feast to end" + newLine + "# a war. Some are false, more often the more a house hates you, the more" + newLine + "# you are feared and the less honour you have shown. Accept one and you" + newLine + "# are expected at their hall; if it was false, the doors close behind you." + newLine + "ravens_enabled=true" + newLine + "# Percent chance each week that a raven comes." + newLine + "ravens_letter_chance=25" + newLine + "# Days between a letter accepted and the feast." + newLine + "ravens_feast_days=5" + newLine + "# What an honest feast of your own costs." + newLine + "ravens_feast_cost=5000" + newLine + "# Your own false feast: gold up front (base, plus per guest expected), and" + newLine + "# days to prepare, each of which the plot may leak." + newLine + "treachery_cost_base=30000" + newLine + "treachery_cost_per_guest=8000" + newLine + "treachery_prep_days=7" + newLine + "# How far your Honour can ever rise again is lowered by this, for good." + newLine + "treachery_honour_cap=20" + newLine + "" + newLine + "# ---------- The small council and hosts ----------" + newLine + "# While you rule, Bellum's privy council can be summoned to your hall, where" + newLine + "# each of them can be spoken to and set to work." + newLine + "council_enabled=true" + newLine + "council_sit_days=3" + newLine + "council_cooldown_days=14" + newLine + "# Hosts bought with gold, commanded by one of your sworn knights. Price per" + newLine + "# man for levies (tier 1-2), men-at-arms (2-4) and veterans (4-6)." + newLine + "host_price_levy=40" + newLine + "host_price_men=80" + newLine + "host_price_veteran=200" + newLine + "host_max_men=30000" + newLine + "# Days a host serves before going home, and what renewing costs as a" + newLine + "# percentage of the first price." + newLine + "host_days=84" + newLine + "host_renew_percent=25" + newLine + "# Other rulers buy hosts too: a ruler at war rolls this percent each week" + newLine + "# (three times as likely if an enemy of theirs already has a host out)," + newLine + "# spends this share of their gold, needs at least this many men, and" + newLine + "# keeps at most this many hosts at once." + newLine + "ai_hosts_enabled=true" + newLine + "ai_host_weekly_chance=10" + newLine + "ai_host_spend_percent=40" + newLine + "ai_host_min_men=2000" + newLine + "ai_host_max_per_realm=1" + newLine + "" + newLine + "# ---------- Parley ----------" + newLine + "# Ride out under a banner of parley from your siege lines. Terms need" + newLine + "# them starving; gold must buy a lord's honour outright; talking them round" + newLine + "# takes three arguments in a row. Single combat is the one easy road." + newLine + "parley_enabled=true" + newLine + "# Lose the single combat and you swear not to come back for this long;" + newLine + "# coming back anyway costs this much Honour." + newLine + "parley_truce_days=30" + newLine + "parley_truce_honour=20" + newLine + "# Seizing the lords who marched out under your word." + newLine + "parley_break_honour=15" + newLine + "parley_break_dread=10" + newLine + "# The price of a castle, or a town, before prosperity is added." + newLine + "parley_buy_castle=1500000" + newLine + "parley_buy_town=3000000" + newLine + "# The chance a castle keeps its gates shut after its champion loses," + newLine + "# by the honour of the champion's house." + newLine + "parley_renege_chance_honourable=5" + newLine + "parley_renege_chance_neutral=20" + newLine + "parley_renege_chance_dishonourable=40" + newLine + "" + newLine + "# ---------- Knights ----------" + newLine + "# A ruler may knight anyone: a soldier, a companion, a wanderer, a younger" + newLine + "# child of the house. The knight founds a house with no land, which rides for" + newLine + "# your realm as a free company and may leave whenever it likes." + newLine + "knights_enabled=true" + newLine + "knight_cost=15000" + newLine + "knight_honour=1" + newLine + "knight_house_tier=1" + newLine + "knight_starting_men=20" + newLine + "# Days of the first contract; after that they stay or go as they please." + newLine + "knight_contract_days=42" + newLine + "# Below this relation with you they are likely to leave; this is the" + newLine + "# weekly chance, lower when they are content." + newLine + "knight_leave_relation=-10" + newLine + "knight_leave_chance=15" + newLine + "knight_rehire_cost=5000" + newLine + "" + newLine + "# ---------- The Iron Bank ----------" + newLine + "# Borrow from Braavos (Court -> The Iron Bank). Your credit grows with your" + newLine + "# house's tier, towns, castles and renown, and with the Bank's opinion." + newLine + "bank_enabled=true" + newLine + "bank_base_credit=200000" + newLine + "bank_credit_per_tier=150000" + newLine + "# Interest, before the Bank's opinion of you moves it (10-30% in practice)." + newLine + "bank_rate=20" + newLine + "# Days between payments; what a missed one adds to the debt; after two" + newLine + "# missed payments the Bank funds your enemies this often, up to this much." + newLine + "bank_payment_days=21" + newLine + "bank_penalty_percent=10" + newLine + "bank_fund_every_days=42" + newLine + "bank_fund_cap=5000000" + newLine + "# Rulers borrow too, to raise hosts they cannot afford, and must pay it" + newLine + "# back in this many days - or the Bank finances their enemies." + newLine + "bank_ai_borrowing=true" + newLine + "bank_ai_loan_cap=1500000" + newLine + "bank_ai_days=84" + newLine + "" + newLine + "# ---------- Exile ----------" + newLine + "# Beat the bastard and he takes ship; across the sea he founds a sellsword" + newLine + "# company. Years later it lands with his son at its head. A Bannerlord year" + newLine + "# is days_per_year days (84 by default)." + newLine + "exile_enabled=true" + newLine + "exile_years_min=10" + newLine + "exile_years_max=20" + newLine + "# The company's strength: at the start, what it gains each season, its cap." + newLine + "exile_start_men=1500" + newLine + "exile_growth_men=250" + newLine + "exile_max_men=8000" + newLine + "# The most the son's backer will spend on the landing." + newLine + "exile_fund_cap=3000000" + newLine + "# How many times the claim can come back." + newLine + "exile_max_generations=5" + newLine + "# Other rulers may hire the company while it waits (never you)." + newLine + "exile_company_hireable=true" + newLine + "" + newLine + "# ---------- Generals ----------" + newLine + "# Tactical hosts: ambushes, raids, feints, screens, dragon strikes and the" + newLine + "# scorpions that answer them. Hosts are paid for food and upkeep once a" + newLine + "# year (this percent of what they cost to raise); unpaid, they desert." + newLine + "generals_enabled=true" + newLine + "host_upkeep_percent=20" + newLine + "# What a sprung ambush costs the enemy before the battle, in percent." + newLine + "ambush_min_percent=8" + newLine + "ambush_max_percent=20" + newLine + "# A host's chance to bring a dragon down with scorpions; Dornish hosts are" + newLine + "# better at it." + newLine + "scorpion_base=15" + newLine + "scorpion_dorne_bonus=20" + newLine + "scorpion_max=75" + newLine + "# What a dragon that is not brought down burns, in percent of the host." + newLine + "dragon_burn_min=20" + newLine + "dragon_burn_max=45" + newLine + "# Chance the rider dies when the dragon falls (else captured)." + newLine + "dragon_rider_fall=50" + newLine + "# Other rulers' hosts think for themselves; their riders strike your hosts." + newLine + "ai_generals=true" + newLine + "ai_dragon_strikes=true" + newLine + "" + newLine + "# ---------- Abdication ----------" + newLine + "# Court -> House and heirs -> Set down the crown. The house passes to your" + newLine + "# heir; you (or a grown younger child) found a new house with nothing but" + newLine + "# this much coin." + newLine + "abdication_enabled=true" + newLine + "abdication_coin=1000" + newLine + "# If the heir is not the one the law names, this share of the realm's" + newLine + "# houses leave it." + newLine + "abdication_unlawful_share=15" + newLine + "# A crown passed while the enemy is inside the realm: every house's" + newLine + "# relation with the heir falls this much." + newLine + "abdication_war_relation=10" + newLine + "# Your relation with the heir afterwards." + newLine + "abdication_old_house_relation=100" + newLine + "" + newLine + "# ---------- Sworn houses ----------" + newLine + "# Wardens (a county or more in Bellum, or a warden's style) gather lesser" + newLine + "# houses: cadets, raised knights, invited landless houses. Each holds a" + newLine + "# manor - one of the warden's villages - and takes this share of its taxes" + newLine + "# each season. AI wardens grow slowly: one house in the whole world per" + newLine + "# season at most, each warden no more often than the cooldown." + newLine + "sworn_enabled=true" + newLine + "sworn_ai=true" + newLine + "sworn_raise_cost=50000" + newLine + "sworn_invite_cost=15000" + newLine + "sworn_ai_raise_cost=30000" + newLine + "sworn_manor_income_percent=50" + newLine + "sworn_warden_cooldown_days=168" + newLine + "# 0 = one house per village in the world; a number above 0 lowers it." + newLine + "sworn_world_cap=0" + newLine + "# Chance a sworn house follows its warden into another realm." + newLine + "sworn_follow_chance=75" + newLine + "" + newLine + "# ---------- Attainder ----------" + newLine + "# The King's Justice -> Attaint a house that wronged you. The house is cast" + newLine + "# out of your realm and at war with you; you may order every lord of it" + newLine + "# your houses take put to death. Surviving a barred-doors feast attaints" + newLine + "# the host's house at once." + newLine + "attainder_enabled=true" + newLine + "attainder_on_massacre=true" + newLine + "attainder_dread=5" + newLine + "# Per head." + newLine + "attainder_execute_dread=2" + newLine + "attainder_execute_honour=1" + newLine;
	}
}
}
