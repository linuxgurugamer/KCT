using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;

namespace KerbalConstructionTime
{
    /// <summary>
    /// Store STATIC localization strings
    /// </summary>
    public static class LocalCache
    {
        public static string KCT = Localizer.Format("#KCT_ModName");
        public static string str_Settings = Localizer.Format("#KCT_Settings");
        public static string WelcomeMsg = Localizer.Format("#KCT_WelcomeMsg");
        public static string Btn_PresetChoose = Localizer.Format("#KCT_Btn_PresetChoose");
        public static string Btn_SpendUpgrades = Localizer.Format("#KCT_Btn_SpendUpgrades");
        public static string Btn_CloseWindow = Localizer.Format("#KCT_Btn_CloseWindow");
        public static string str_Features = Localizer.Format("#KCT_Section_Features");
        public static string str_Enabled = Localizer.Format("#KCT_Section_Features_Enabled");
        public static string str_BuildTimes = Localizer.Format("#KCT_Section_Features_BuildTimes");
        public static string str_ReconditioningTimes = Localizer.Format("#KCT_Section_Features_ReconditioningTimes");
        public static string str_ReconditioningBlocksPad = Localizer.Format("#KCT_Section_Features_ReconditioningBlocksPad");
        public static string str_TechUnlockTimes = Localizer.Format("#KCT_Section_Features_TechUnlockTimes");
        public static string str_KSCUpgradeTimes = Localizer.Format("#KCT_Section_Features_KSCUpgradeTimes");
        public static string str_TechUpgrades = Localizer.Format("#KCT_Section_Features_TechUpgrades");
        public static string str_SharedUpgradePool = Localizer.Format("#KCT_Section_Features_SharedUpgradePool");
        public static string str_StartingUpgrades = Localizer.Format("#KCT_Section_Features_StartingUpgrades");
        public static string str_TimeSettings = Localizer.Format("#KCT_Section_TimeSettings");
        public static string str_OverallMultiplier = Localizer.Format("#KCT_Section_TimeSettings_OverallMultiplier");
        public static string str_MergingTimePercent = Localizer.Format("#KCT_Section_TimeSettings_MergingTimePercent");
        public static string str_BuildEffect = Localizer.Format("#KCT_Section_TimeSettings_BuildEffect");
        public static string str_InventoryEffect = Localizer.Format("#KCT_Section_TimeSettings_InventoryEffect");
        public static string str_ReconditioningEffect = Localizer.Format("#KCT_Section_TimeSettings_ReconditioningEffect");
        public static string str_MaxReconditioning = Localizer.Format("#KCT_Section_TimeSettings_MaxReconditioning");
        public static string str_RolloutReconditioningSplit = Localizer.Format("#KCT_Section_TimeSettings_RolloutReconditioningSplit");
        public static string str_AdvFormulaSettings = Localizer.Format("#KCT_Section_AdvFormulaSettings");
        public static string btn_ShowFormulas = Localizer.Format("#KCT_Section_AdvFormulaSettings_ShowFormulas");
        public static string btn_ViewWiki = Localizer.Format("#KCT_Section_AdvFormulaSettings_ViewWiki");
        public static string Btn_Save = Localizer.Format("#KCT_Btn_Save");
        public static string Btn_Cancel = Localizer.Format("#KCT_Btn_Cancel");
        public static string str_GeneralSettings = Localizer.Format("#KCT_Section_GeneralSettings");
        public static string str_NOTE = Localizer.Format("#KCT_Section_GeneralSettings_NOTE");
        public static string str_MaxTimewarp = Localizer.Format("#KCT_Section_GeneralSettings_MaxTimewarp");
        public static string btn_AutoStopTimeWarp = Localizer.Format("#KCT_Section_GeneralSettings_AutoStopTimeWarp");
        public static string btn_AutoKACAlarms = Localizer.Format("#KCT_Section_GeneralSettings_AutoKACAlarms");
        public static string btn_AutoStockAlarms = Localizer.Format("#KCT_Section_GeneralSettings_AutoStockAlarms");
        public static string btn_OverrideLaunchButton = Localizer.Format("#KCT_Section_GeneralSettings_OverrideLaunchButton");
        public static string btn_UseMessageSystem = Localizer.Format("#KCT_Section_GeneralSettings_UseMessageSystem");
        public static string btn_DebugLogging = Localizer.Format("#KCT_Section_GeneralSettings_DebugLogging");
        // Preset Save Window
        public static string str_PresetNameTitle = Localizer.Format("#KCT_PresetSave_title");
        public static string str_PresetName = Localizer.Format("#KCT_PresetSave_PresetName");
        public static string str_PresetShortName = Localizer.Format("#KCT_PresetSave_PresetShortName");
        public static string str_PresetAuthors = Localizer.Format("#KCT_PresetSave_Presetauthors");
        public static string str_PresetDescription = Localizer.Format("#KCT_PresetSave_PresetDescription");
        public static string str_saveCareer = Localizer.Format("#KCT_PresetSave_saveInCareer");
        public static string str_saveScience = Localizer.Format("#KCT_PresetSave_saveInScience");
        public static string str_saveSandbox = Localizer.Format("#KCT_PresetSave_saveInSandbox");
        public static string str_existWarning = Localizer.Format("#KCT_PresetSave_Warning");
        public static string str_Presets = Localizer.Format("#KCT_Presets_Presets");
        public static string btn_SaveAsNewPreset = Localizer.Format("#KCT_Presets_SaveAsNewPreset");
        public static string btn_DeletePreset = Localizer.Format("#KCT_Presets_DeletePreset");
        public static string btn_DeleteFile = Localizer.Format("#KCT_Presets_DeleteFile");
        public static string str_DeleteConfirmTitle = Localizer.Format("#KCT_Presets_DeleteConfirmTitle");
        public static string str_DeleteConfirmMsg = Localizer.Format("#KCT_Presets_DeleteConfirmMsg");
        public static string str_BuildList = Localizer.Format("#KCT_BuildList_title");
        public static string str_NoActiveProjects = Localizer.Format("#KCT_BuildList_NoActiveProjects");
        public static string str_ListNext = Localizer.Format("#KCT_BuildList_Next");
        public static string str_locTxtVAB = Localizer.Format("#KCT_BuildList_VAB");
        public static string str_locTxtSPH = Localizer.Format("#KCT_BuildList_SPH");
        public static string str_locTxtTech = Localizer.Format("#KCT_BuildList_Tech");
        public static string str_locTxtKSC = Localizer.Format("#KCT_BuildList_KSC");
        public static string str_Reconditioning = Localizer.Format("#KCT_BuildList_Reconditioning");
        public static string str_Rollout = Localizer.Format("#KCT_BuildList_Rollout");
        public static string str_Rollback = Localizer.Format("#KCT_BuildList_Rollback");
        public static string Btn_VAB = Localizer.Format("#KCT_Btn_VAB");
        public static string Btn_SPH = Localizer.Format("#KCT_Btn_SPH");
        public static string Btn_Tech = Localizer.Format("#KCT_Btn_Tech");
        public static string Btn_Plans = Localizer.Format("#KCT_Btn_Plans");
        public static string Btn_Upgrades = Localizer.Format("#KCT_Btn_Upgrades");
        public static string Btn_RD = Localizer.Format("#KCT_Btn_RD");
        public static string Btn_Yes = Localizer.Format("#KCT_Btn_Yes");
        public static string Btn_No = Localizer.Format("#KCT_Btn_No");
        // Build list window
        public static string str_BuildList_Name = Localizer.Format("#KCT_BuildList_Name");
        public static string str_BuildList_Progress = Localizer.Format("#KCT_BuildList_Progress");
        public static string str_BuildList_TimeLeft = Localizer.Format("#KCT_BuildList_TimeLeft");
        public static string btn_VAB_WarpTo = Localizer.Format("#KCT_BuildList_VAB_WarpTo");
        public static string str_VAB_Reconditioning = Localizer.Format("#KCT_BuildList_VAB_Reconditioning");
        public static string str_VAB_NoBuildMsg = Localizer.Format("#KCT_BuildList_VAB_NoBuild");
        public static string str_ScrapVessel_title = Localizer.Format("#KCT_BuildList_VAB_ScrapVessel_title");
        public static string str_ScrapVesselMsg = Localizer.Format("#KCT_BuildList_VAB_ScrapVesselMsg");
        public static string str_Estimate = Localizer.Format("#KCT_BuildList_Estimate");
        public static string str_VAB_Storage = Localizer.Format("#KCT_BuildList_VAB_Storage");
        public static string btn_RecoverActiveVessel = Localizer.Format("#KCT_BuildList_RecoverActiveVessel");
        public static string str_RecoverError_title = Localizer.Format("#KCT_BuildList_RecoverError_title");
        public static string str_RecoverErrorMsg = Localizer.Format("#KCT_BuildList_RecoverErrorMsg");
        public static string str_NoStorage = Localizer.Format("#KCT_BuildList_NoStorage");
        public static string str_status_InStorage = Localizer.Format("#KCT_BuildList_VAB_status_InStorage");
        public static string str_status_Recovering = Localizer.Format("#KCT_BuildList_status_Recovering");
        public static string btn_Rollout = Localizer.Format("#KCT_BuildList_VAB_Rollout");
        public static string str_RolloutError_title = Localizer.Format("#KCT_BuildList_VAB_RolloutError_title");
        public static string str_RolloutErrorMsg = Localizer.Format("#KCT_BuildList_VAB_RolloutErrorMsg");
        public static string Btn_Acknowledged = Localizer.Format("#KCT_Btn_Acknowledged");
        public static string str_LaunchError_title = Localizer.Format("#KCT_BuildList_LaunchError_title");
        public static string str_LaunchErrorMsg = Localizer.Format("#KCT_BuildList_VAB_LaunchErrorMsg");
        public static string str_LaunchErrorMsg2 = Localizer.Format("#KCT_BuildList_VAB_LaunchErrorMsg2");
        public static string str_LaunchErrorMsg3 = Localizer.Format("#KCT_BuildList_VAB_LaunchErrorMsg3");
        public static string str_LaunchErrorMsg5 = Localizer.Format("#KCT_BuildList_LaunchErrorMsg5");
        public static string btn_Launch = Localizer.Format("#KCT_BuildList_Launch");
        public static string btn_RepairsRequired = Localizer.Format("#KCT_BuildList_VAB_RepairsRequired");
        public static string btn_Reconditioning = Localizer.Format("#KCT_BuildList_VAB_btnReconditioning");
        public static string btn_RollBack = Localizer.Format("#KCT_BuildList_VAB_RollBack");
        public static string str_Current = Localizer.Format("#KCT_BuildList_VAB_strCurrent");
        public static string btn_Rename = Localizer.Format("#KCT_Btn_Rename");
        public static string btn_Dismantle = Localizer.Format("#KCT_BuildList_VAB_Dismantle");
        public static string btn_NewLaunchPad = Localizer.Format("#KCT_BuildList_VAB_NewLaunchPad");
        public static string str_NewLaunchPadName = Localizer.Format("#KCT_BuildList_VAB_NewLaunchPad_Name");
        public static string str_NewLaunchPad_Level = Localizer.Format("#KCT_BuildList_VAB_NewLaunchPad_Level");
        public static string str_SPH_NoBuild = Localizer.Format("#KCT_BuildList_SPH_NoBuild");
        public static string str_SPH_ScrapVessel_title = Localizer.Format("#KCT_BuildList_SPH_ScrapVessel_title");
        public static string str_SPH_Storage = Localizer.Format("#KCT_BuildList_SPH_Storage");
        public static string str_SPH_Status_Ready = Localizer.Format("#KCT_BuildList_SPH_Status_Ready");
        public static string str_SPH_PrepAirlaunch = Localizer.Format("#KCT_BuildList_SPH_PrepAirlaunch");
        public static string btn_SPH_Unmount = Localizer.Format("#KCT_BuildList_SPH_Unmount");
        public static string btn_SPH_Airlaunch = Localizer.Format("#KCT_BuildList_SPH_Airlaunch");
        public static string btn_SPH_Launch = Localizer.Format("#KCT_BuildList_SPH_Launch");
        public static string btn_SPH_RunwayNeedsRepair = Localizer.Format("#KCT_BuildList_SPH_ScreenMsg_RunwayNeedsRepair");
        // Build List - Tech
        public static string str_Tech_NoProjects = Localizer.Format("#KCT_BuildList_Tech_NoProjects");
        public static string btn_Tech_Warp = Localizer.Format("#KCT_BuildList_Tech_Warpbutton");
        public static string str_Tech_NoResearch = Localizer.Format("#KCT_BuildList_Tech_NoResearch");
        public static string str_Tech_StopResearch_title = Localizer.Format("#KCT_BuildList_Tech_StopResearch_title");
        public static string str_Tech_WaitingforPreReq = Localizer.Format("#KCT_BuildList_Tech_WaitingforPreReq");
        // Stock Alarm texts
        public static string str_Alarm_title_default = Localizer.Format("#KCT_Alarm_title_default");
        public static string str_Alarm_title_Recon = Localizer.Format("#KCT_Alarm_title_Recon");
        public static string str_Alarm_title_Rollout = Localizer.Format("#KCT_Alarm_title_Rollout");
        public static string str_Alarm_title_Rollback = Localizer.Format("#KCT_Alarm_title_Rollback");

        // side Options window
        public static string btn_Options_Wintitle = Localizer.Format("#KCT_Options_Windowtitle");
        public static string btn_Options_SelectLaunchSite = Localizer.Format("#KCT_Options_SelectLaunchSite");
        public static string str_SelectLaunchSite_error_title = Localizer.Format("#KCT_Options_SelectLaunchSite_error_title");
        public static string str_SelectLaunchSite_errorMsg = Localizer.Format("#KCT_Options_SelectLaunchSite_errorMsg");
        public static string btn_Options_Scrap = Localizer.Format("#KCT_Options_Scrap");
        public static string btn_Options_Edit = Localizer.Format("#KCT_Options_Edit");
        public static string btn_Options_Duplicate = Localizer.Format("#KCT_Options_Duplicate");
        public static string btn_Options_Rollback = Localizer.Format("#KCT_Options_Rollback");
        public static string btn_Options_WarpTo = Localizer.Format("#KCT_Options_WarpTo");
        public static string btn_Options_MovetoTop = Localizer.Format("#KCT_Options_MovetoTop");
        public static string btn_Options_RushBuild = Localizer.Format("#KCT_Options_RushBuild");
        public static string Btn_Close = Localizer.Format("#KCT_Btn_Close");

        // Select Crew window
        public static string str_SelectCrew_windowtitle = Localizer.Format("#KCT_SelectCrew_windowtitle");
        public static string btn_SelectCrew_RandomizeFilling = Localizer.Format("#KCT_SelectCrew_RandomizeFilling");
        public static string btn_SelectCrew_AutoHireFilling = Localizer.Format("#KCT_SelectCrew_AutoHireFilling");
        public static string btn_SelectCrew_FillAll = Localizer.Format("#KCT_SelectCrew_FillAll");
        public static string btn_SelectCrew_ClearAll = Localizer.Format("#KCT_SelectCrew_ClearAll");
        public static string btn_SelectCrew_Fill = Localizer.Format("#KCT_SelectCrew_Fill");
        public static string btn_SelectCrew_Clear = Localizer.Format("#KCT_SelectCrew_Clear");
        public static string btn_SelectCrew_Remove = Localizer.Format("#KCT_SelectCrew_Remove");
        public static string str_SelectCrew_Empty = Localizer.Format("#KCT_SelectCrew_Empty");
        public static string btn_SelectCrew_Add = Localizer.Format("#KCT_SelectCrew_Add");
        public static string btn_SelectCrew_HireNew = Localizer.Format("#KCT_SelectCrew_HireNew");
        public static string str_SelectCrew_NoCrew = Localizer.Format("#KCT_SelectCrew_NoCrew");
        public static string btn_SelectCrew_FillTanksLaunch = Localizer.Format("#KCT_SelectCrew_FillTanksLaunch");
        public static string KCT_SortBys_Name = Localizer.Format("#KCT_SortBys_Name");
        public static string KCT_SortBys_Type = Localizer.Format("#KCT_SortBys_Type");
        public static string KCT_SortBys_Level = Localizer.Format("#KCT_SortBys_Level");
        // Select Crew & Launch window
        public static string str_SelectCrewLaunch_windowTitle = Localizer.Format("#KCT_SelectCrewLaunch_windowTitle");
        public static string str_SelectCrewLaunch_Sort = Localizer.Format("#KCT_SelectCrewLaunch_Sortlabel");
        public static string str_SelectCrewLaunch_CrewName = Localizer.Format("#KCT_SelectCrewLaunch_CrewName");
        public static string str_SelectCrewLaunch_Courage = Localizer.Format("#KCT_SelectCrewLaunch_Courage");
        public static string str_SelectCrewLaunch_Stupidity = Localizer.Format("#KCT_SelectCrewLaunch_Stupidity");

        // Upgrades window
        public static string str_Upgrades_windowTitle = Localizer.Format("#KCT_Upgrades_windowTitle");
        public static string str_Upgrades_TotalPoints = Localizer.Format("#KCT_Upgrades_TotalPoints");
        public static string str_Upgrades_TotalScience = Localizer.Format("#KCT_Upgrades_TotalScience");
        public static string str_Upgrades_PointsinVAB = Localizer.Format("#KCT_Upgrades_PointsinVAB");
        public static string str_Upgrades_PointsinSPH = Localizer.Format("#KCT_Upgrades_PointsinSPH");
        public static string str_Upgrades_PointsinRD = Localizer.Format("#KCT_Upgrades_PointsinRD");
        public static string str_Upgrades_BuyOnePointLabel = Localizer.Format("#KCT_Upgrades_BuyOnePointLabel");
        public static string btn_Upgrades_BuyPointsbyScience = Localizer.Format("#KCT_Upgrades_BuyPointsbyScience");
        public static string btn_Upgrades_BuyPointsbyFunds = Localizer.Format("#KCT_Upgrades_BuyPointsbyFunds");
        public static string str_Upgrades_ResetUpgrades = Localizer.Format("#KCT_Upgrades_ResetUpgrades");
        public static string str_Upgrades_RDUpgrades = Localizer.Format("#KCT_Upgrades_RDUpgrades");
        public static string str_Upgrades_Upgrades = Localizer.Format("#KCT_Upgrades_strUpgrades");
        public static string str_Upgrades_Research = Localizer.Format("#KCT_Upgrades_Research");
        public static string str_Upgrades_Develepment = Localizer.Format("#KCT_Upgrades_Develepment");
        public static string str_Upgrades_sciPerDay = Localizer.Format("#KCT_Upgrades_sciPerDay");

        // New Launch Pad window
        public static string str_NewlaunchPad_windowTitle = Localizer.Format("#KCT_NewlaunchPad_windowTitle");
        public static string str_NewlaunchPad_ScreenMsg_AskName = Localizer.Format("#KCT_NewlaunchPad_ScreenMsg_AskName");
        public static string str_NewlaunchPad_ScreenMsg_NameExists = Localizer.Format("#KCT_NewlaunchPad_ScreenMsg_NameExists");
        public static string str_NewlaunchPad_ScreenMsg_NotFunds = Localizer.Format("#KCT_NewlaunchPad_ScreenMsg_NotFunds");
        public static string str_NewlaunchPad_Level_unlimited = Localizer.Format("#KCT_NewlaunchPad_Level_unlimited");

        // SOI change window
        public static string str_SOI_windowTitle = Localizer.Format("#KCT_SOI_windowTitle");
        public static string str_SOI_SoIStopWarp = Localizer.Format("#KCT_SOI_SoIStopWarp");
        public static string str_SOI_VesselName = Localizer.Format("#KCT_SOI_VesselName");

        // Launch site not clear! window
        public static string str_SiteNotClear_windowTitle = Localizer.Format("#KCT_SiteNotClear_windowTitle");
        public static string btn_SiteNotClear_RecoverProceed = Localizer.Format("#KCT_SiteNotClear_RecoverProceed");

        // Dismantle pad window
        public static string str_Dismantlepad_windowTitle = Localizer.Format("#KCT_Dismantlepad_windowTitle");
        public static string str_Dismantlepad_dismantleTip = Localizer.Format("#KCT_Dismantlepad_dismantleTip");
        public static string str_Dismantlepad_PrefixFailedScreenMsg = Localizer.Format("#KCT_Dismantlepad_PrefixFailedScreenMsg");
        public static string str_Dismantlepad_FailedScreenMsg1 = Localizer.Format("#KCT_Dismantlepad_FailedScreenMsg1");
        public static string str_Dismantlepad_FailedScreenMsg2 = Localizer.Format("#KCT_Dismantlepad_FailedScreenMsg2");

        // Select Site window
        public static string str_SelectSite_windowTitle = Localizer.Format("#KCT_SelectSite_windowTitle");

        // Building Plans & Construction window
        public static string str_BuildingPlansAndConstruction_windowTitle = Localizer.Format("#KCT_BuildingPlansAndConstruction_windowTitle");
        public static string str_BuildingPlansAndConstruction_CannotAddPlan = Localizer.Format("#KCT_BuildingPlansAndConstruction_CannotAddPlan");
        public static string str_BuildingPlansAndConstruction_CannotAddPlan_Msg1 = Localizer.Format("#KCT_BuildingPlansAndConstruction_CannotAddPlan_ScreenMsg1");
        public static string str_BuildingPlansAndConstruction_CannotAddPlan_Msg2 = Localizer.Format("#KCT_BuildingPlansAndConstruction_CannotAddPlan_ScreenMsg2");
        public static string btn_BuildingPlansAndConstruction_AddToPlans = Localizer.Format("#KCT_BuildingPlansAndConstruction_AddToPlans");
        public static string btn_BuildingPlansAndConstruction_Build = Localizer.Format("#KCT_BuildingPlansAndConstruction_Build");
        public static string btn_BuildingPlansAndConstruction_NoAvailable = Localizer.Format("#KCT_BuildingPlansAndConstruction_NoAvailable");
        public static string str_BuildingPlansAndConstruction_AvailableBuildingPlans = Localizer.Format("#KCT_BuildingPlansAndConstruction_AvailableBuildingPlans");
        public static string str_BuildingPlansAndConstruction_NoVesselsInPlans = Localizer.Format("#KCT_BuildingPlansAndConstruction_NoVesselsInPlans");
        public static string str_BuildingPlansAndConstruction_RemovePlanMsg = Localizer.Format("#KCT_BuildingPlansAndConstruction_RemovePlanMsg");
        public static string str_BuildingPlansAndConstruction_RemovePlanTitle = Localizer.Format("#KCT_BuildingPlansAndConstruction_RemovePlan_title");

        // Airlaunch window
        public static string str_Airlaunch_windowTitle = Localizer.Format("#KCT_Airlaunch_windowTitle");
        public static string str_Airlaunch_DistanceFromKSC = Localizer.Format("#KCT_Airlaunch_DistanceFromKSC");
        public static string str_Airlaunch_AzimuthFromKSC = Localizer.Format("#KCT_Airlaunch_AzimuthFromKSC");
        public static string str_Airlaunch_LaunchAzimuth = Localizer.Format("#KCT_Airlaunch_LaunchAzimuth");
        public static string str_Airlaunch_AltitudeFromSL = Localizer.Format("#KCT_Airlaunch_AltitudeFromSL");
        public static string str_Airlaunch_Velocity = Localizer.Format("#KCT_Airlaunch_Velocity");

        // Kerbal Construction Time window
        public static string str_KCT_BuildTimeAt = Localizer.Format("#KCT_KCT_BuildTimeAt");
        public static string str_KCT_BuildPointPerSec = Localizer.Format("#KCT_KCT_BuildPointPerSec");
        public static string str_KCT_InvalidBuildRate = Localizer.Format("#KCT_KCT_InvalidBuildRate");
        public static string str_KCT_RolloutTime = Localizer.Format("#KCT_KCT_RolloutTime");
        public static string str_KCT_IntegrationCost = Localizer.Format("#KCT_KCT_IntegrationCost");
        public static string str_KCT_RolloutCost = Localizer.Format("#KCT_KCT_RolloutCost");
        public static string btn_KCT_Build = Localizer.Format("#KCT_KCT_Build");
        public static string btn_KCT_ShowOHide = Localizer.Format("#KCT_KCT_ShowOHide");
        public static string str_KCT_OriginalBP = Localizer.Format("#KCT_KCT_OriginalBP");
        public static string str_KCT_Edited = Localizer.Format("#KCT_KCT_Edited");
        public static string btn_KCT_SaveEdits = Localizer.Format("#KCT_KCT_SaveEdits");
        public static string btn_KCT_CancelEdits = Localizer.Format("#KCT_KCT_CancelEdits");
        public static string btn_KCT_FillTanks = Localizer.Format("#KCT_KCT_FillTanks");
        public static string btn_KCT_MergeBuilt = Localizer.Format("#KCT_KCT_MergeBuilt");
        public static string btn_HideMergeSelection = Localizer.Format("#KCT_KCT_HideMergeSelection");
        public static string str_KCT_ChooseVessel = Localizer.Format("#KCT_KCT_ChooseVessel");
        public static string str_KCTVessel_string = Localizer.Format("#KCT_BuildVessel_string");
        public static string str_BuildPointPS = Localizer.Format("#KCT_BuildPointPS");
        public static string str_Rate = Localizer.Format("#KCT_Rate");

        // Messages
        public static string str_Messages_CompleteTitle = Localizer.Format("#KCT_Messages_CompleteTitle");
        public static string str_Messages_CompleteStart = Localizer.Format("#KCT_Messages_Complete_start");
        public static string str_Messages_FailedEditorChecksTitle = Localizer.Format("#KCT_Messages_FailedEditorChecksTitle");
        public static string str_Messages_FailedEditorChecksMsg = Localizer.Format("#KCT_Messages_FailedEditorChecksMsg");
        public static string str_Messages_FailedEditorChecksMsg2 = Localizer.Format("#KCT_Messages_FailedEditorChecksMsg2");
        public static string str_Messages_NotEnoughFundsBuild = Localizer.Format("#KCT_Messages_NotEnoughFundsBuild");
        public static string str_Messages_FailedReasons_PartNotAvailable = Localizer.Format("#KCT_Messages_FailedReasons_PartNotAvailable");
        public static string str_Messages_FailedReasons_PartExceeded = Localizer.Format("#KCT_Messages_FailedReasons_PartExceeded");
        public static string str_Messages_FailedReasons_SizeExceeded = Localizer.Format("#KCT_Messages_FailedReasons_SizeExceeded");
        public static string str_Messages_NotSwitchEditor_title = Localizer.Format("#KCT_Messages_NotSwitchEditor_title");
        public static string str_Messages_NotSwitchEditorMsg = Localizer.Format("#KCT_Messages_NotSwitchEditorMsg");
        public static string str_Messages_WaitForResearchMsg = Localizer.Format("#KCT_Messages_WaitForResearchMsg");
        public static string str_Messages_UpgradePointAdded = Localizer.Format("#KCT_Messages_UpgradePointAdded");
        public static string str_Messages_NodeResearched = Localizer.Format("#KCT_Messages_NodeResearched");
        public static string str_Messages_KCTRecover = Localizer.Format("#KCT_Messages_KCTRecover");
        public static string btn_RecoverToSPH = Localizer.Format("#KCT_btn_RecoverToSPH");
        public static string btn_NomalRecover = Localizer.Format("#KCT_btn_StockRecover");
        public static string btn_RecoverToVAB = Localizer.Format("#KCT_btn_RecoverToVAB");
        public static string btn_Recover = Localizer.Format("Recover");
        public static string str_Messages_ExternalSeatReconvery = Localizer.Format("#KCT_Messages_ExternalSeatReconvery");
        public static string str_Messages_Title = Localizer.Format("#KCT_Messages_Title");
        public static string btn_GotoFlight = Localizer.Format("#KCT_btn_GotoFlight");
        public static string str_Messages_RecoverInFlightTitle = Localizer.Format("#KCT_Messages_RecoverInFlight_title");
        public static string str_Messages_RecoverInFlight = Localizer.Format("#KCT_Messages_RecoverInFlight");
        public static string str_Messages_CanotUpgradeLaunchpadecoverInFlight = Localizer.Format("#KCT_Messages_CanotUpgradeLaunchpad");
        public static string str_Messages_UpgradeRequireTechMsg = Localizer.Format("#KCT_Messages_UpgradeRequireTechMsg");
        public static string str_Messages_UpgradeRequireTech_Title = Localizer.Format("#KCT_Messages_UpgradeRequireTech_Title");
        public static string str_Messages_UpgraderequestedFacility = Localizer.Format("#KCT_Messages_UpgraderequestedFacility");
        public static string str_Messages_NoFundsUpgradeFacility = Localizer.Format("#KCT_Messages_NoFundsUpgradeFacility");
        public static string str_Messages_FacilityAlreadyUpgraded = Localizer.Format("#KCT_Messages_FacilityAlreadyUpgraded");
        public static string str_Messages_LaunchpadUpgradetip = Localizer.Format("#KCT_Messages_LaunchpadUpgradetip");
        public static string str_Messages_LaunchpadUpgrade_title = Localizer.Format("#KCT_Messages_LaunchpadUpgrade_title");
        public static string str_Messages_NoFundsToWarp = Localizer.Format("#KCT_Messages_NoFundsToWarp");
        public static string str_Messages_AirlaunchCancelled = Localizer.Format("#KCT_Messages_AirlaunchCancelled");


    }
}
