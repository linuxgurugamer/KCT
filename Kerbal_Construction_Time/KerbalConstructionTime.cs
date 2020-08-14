using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using System.Collections;
using KSP.UI.Screens;
using KSP.UI;
using ToolbarControl_NS;

using UnityEngine.UI;
using KSP.Localization;


namespace KerbalConstructionTime
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KCT_SpaceCenter : KerbalConstructionTime
    {

    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KCT_Editor : KerbalConstructionTime
    {
        public bool isLaunchSiteControllerBound;
    }

    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.EDITOR, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION })]
    public class KerbalConstructionTimeData : ScenarioModule
    {
        public static Dictionary<string, string> techNameToTitle = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> techNameToParents = new Dictionary<string, List<string>>();

        protected void LoadTree()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX || HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                string fullPath = KSPUtil.ApplicationRootPath + HighLogic.CurrentGame.Parameters.Career.TechTreeUrl;
                KCTDebug.Log($"Loading tech tree from {fullPath}");

                ConfigNode fileNode = ConfigNode.Load(fullPath);
                if (fileNode != null && fileNode.HasNode("TechTree"))
                {
                    techNameToTitle.Clear();

                    ConfigNode treeNode = fileNode.GetNode("TechTree");
                    ConfigNode[] ns = treeNode.GetNodes("RDNode");
                    foreach (ConfigNode n in ns)
                    {
                        if (n.HasValue("id"))
                        {
                            string techID = n.GetValue("id");

                            if (n.HasValue("title"))
                                techNameToTitle[techID] = n.GetValue("title");

                            ConfigNode[] parents = n.GetNodes("Parent");
                            List<string> pList = new List<string>();
                            foreach (ConfigNode p in parents)
                            {
                                if (p.HasValue("parentID"))
                                    pList.Add(p.GetValue("parentID"));
                            }
                            techNameToParents[techID] = pList;
                        }
                    }
                }
            }
        }

        public override void OnSave(ConfigNode node)
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            // Boolean error = false;
            KCTDebug.Log("Writing to persistence.");
            base.OnSave(node);
            KCT_DataStorage kctVS = new KCT_DataStorage();
            node.AddNode(kctVS.AsConfigNode());
            foreach (KCT_KSC KSC in KCT_GameStates.KSCs)
            {
                if (KSC != null && KSC.KSCName != null && KSC.KSCName.Length > 0)
                    node.AddNode(KSC.AsConfigNode());
            }
            ConfigNode tech = new ConfigNode("TechList");
            foreach (KCT_TechItem techItem in KCT_GameStates.TechList)
            {
                KCT_TechStorageItem techNode = new KCT_TechStorageItem();
                techNode.FromTechItem(techItem);
                ConfigNode cnTemp = new ConfigNode("Tech");
                cnTemp = ConfigNode.CreateConfigFromObject(techNode, cnTemp);
                ConfigNode protoNode = new ConfigNode("ProtoNode");
                techItem.protoNode.Save(protoNode);
                cnTemp.AddNode(protoNode);
                tech.AddNode(cnTemp);
            }
            node.AddNode(tech);
        }

        public override void OnLoad(ConfigNode node)
        {

            base.OnLoad(node);
            LoadTree();

            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            KCTDebug.Log("Reading from persistence.");
            KCT_GameStates.KSCs.Clear();
            KCT_GameStates.ActiveKSC = null;
            //KCT_Utilities.SetActiveKSC("Stock");
            KCT_GameStates.InitAndClearTechList();
            KCT_GameStates.TechUpgradesTotal = 0;
            KCT_GameStates.SciPointsTotal = -1;

            KCT_DataStorage kctVS = new KCT_DataStorage();
            ConfigNode CN = node.GetNode(kctVS.GetType().Name);
            if (CN != null)
                ConfigNode.LoadObjectFromConfig(kctVS, CN);

            foreach (ConfigNode ksc in node.GetNodes("KSC"))
            {
                string name = ksc.GetValue("KSCName");
                KCT_KSC loaded_KSC = new KCT_KSC(name);
                loaded_KSC.FromConfigNode(ksc);
                if (loaded_KSC != null && loaded_KSC.KSCName != null && loaded_KSC.KSCName.Length > 0)
                {
                    loaded_KSC.RDUpgrades[1] = KCT_GameStates.TechUpgradesTotal;
                    if (KCT_GameStates.KSCs.Find(k => k.KSCName == loaded_KSC.KSCName) == null)
                        KCT_GameStates.KSCs.Add(loaded_KSC);
                }
            }
            KCT_Utilities.SetActiveKSCToRSS();


            ConfigNode tmp = node.GetNode("TechList");
            if (tmp != null)
            {
                foreach (ConfigNode techNode in tmp.GetNodes("Tech"))
                {
                    KCT_TechStorageItem techStorageItem = new KCT_TechStorageItem();
                    ConfigNode.LoadObjectFromConfig(techStorageItem, techNode);
                    KCT_TechItem techItem = techStorageItem.ToTechItem();
                    techItem.protoNode = new ProtoTechNode(techNode.GetNode("ProtoNode"));
                    KCT_GameStates.TechList.Add(techItem);
                }
            }

            //KCT_GUI.CheckToolbar();
            KCT_GameStates.erroredDuringOnLoad.OnLoadFinish();
            //KerbalConstructionTime.DelayedStart();
        }
    }

    //[KSPAddon(KSPAddon.Startup.EditorAny | KSPAddon.Startup.Flight | KSPAddon.Startup.SpaceCentre | KSPAddon.Startup.TrackingStation, false)]
    public class KerbalConstructionTime : MonoBehaviour
    {
        internal void FacilityContextMenuSpawn(KSCFacilityContextMenu menu)
        {
            if (KCT_GUI.PrimarilyDisabled) return;

            KCT_KSCContextMenuOverrider overrider = new KCT_KSCContextMenuOverrider(menu);
            StartCoroutine(overrider.OnContextMenuSpawn());
        }

        public bool editorRecalcuationRequired;
        public int updateRateThrottle;

        public static KerbalConstructionTime instance;

        public void OnDestroy()//more toolbar stuff
        {
            if (KCT_GameStates.toolbarControl != null)
            {
                KCT_GameStates.toolbarControl.OnDestroy();
                Destroy(KCT_GameStates.toolbarControl);
            }
            KCT_GUI.guiDataSaver.Save();
        }

        static bool GuiInit = false;
        private void OnGUI()
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            if (!GuiInit)
            {
                KCT_GUI.InitBuildListVars();
                KCT_GUI.InitBuildPlans();
                GuiInit = true;
            }
            KCT_GUI.SetGUIPositions();
        }

        public void Awake()
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            KCTDebug.Log("Awake called");
            KCT_GameStates.erroredDuringOnLoad.OnLoadStart();
            KCT_GameStates.PersistenceLoaded = false;

            instance = this;

            KCT_GameStates.settings.Load(); //Load the settings file, if it exists

            string SavedFile = KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/KCT_Settings.cfg";
            if (!System.IO.File.Exists(SavedFile))
            {
                KCT_GameStates.firstStart = true;
            }

            if (KCT_PresetManager.Instance == null)
            {
                KCT_PresetManager.Instance = new KCT_PresetManager();
            }
            KCT_PresetManager.Instance.SetActiveFromSaveData();

            // Create events for other mods
            if (!KCT_Events.instance.createdEvents)
            {
                KCT_Events.instance.CreateEvents();
            }

            var go = new GameObject();
            KCT_GameStates.toolbarControl = go.AddComponent<ToolbarControl>();
            KCT_GameStates.toolbarControl.AddToAllToolbars(null, null,//KCT_GUI.ClickOn, KCT_GUI.ClickOff,
                null, null, /* KCT_GUI.onHoverOn, KCT_GUI.onHoverOff, */ null, null,
                ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW | ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.TRACKSTATION | ApplicationLauncher.AppScenes.VAB,
                KCT_GameStates.MODID,
                "MainButton",
                "KerbalConstructionTime/PluginData/Icons/KCT_on-38",
                "KerbalConstructionTime/PluginData/Icons/KCT_off-38",
                "KerbalConstructionTime/PluginData/Icons/KCT_on-24",
                "KerbalConstructionTime/PluginData/Icons/KCT_off-24",
                KCT_GameStates.MODNAME
                );

            KCT_GameStates.toolbarControl.AddLeftRightClickCallbacks(KCT_GUI.ClickToggle, KCT_GUI.onRightClick);

            KCTDebug.Log("Awake finished");
        }

        public void Start()
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            KCTDebug.Log("Start called");

            // Subscribe to events from KSP and other mods
            if (!KCT_Events.instance.subscribedToEvents)
            {
                KCT_Events.instance.SubscribeToEvents();
            }

            KCT_GameStates.settings.Save(); //Save the settings file, with defaults if it doesn't exist
            KCT_PresetManager.Instance.SaveActiveToSaveData();

            // Ghetto event queue
            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                InvokeRepeating("EditorRecalculation", 1, 1);

                KCT_GUI.buildRateForDisplay = null;
                if (!KCT_GUI.PrimarilyDisabled)
                {
                    KCT_Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
                }
            }

            if (KCT_GUI.PrimarilyDisabled)
            {
                if (InputLockManager.GetControlLock("KCTLaunchLock") == ControlTypes.EDITOR_LAUNCH)
                    InputLockManager.RemoveControlLock("KCTLaunchLock");
            }

            KACWrapper.InitKACWrapper();

            if (!KCT_PresetManager.Instance.ActivePreset.generalSettings.Enabled)
            {
                if (InputLockManager.GetControlLock("KCTKSCLock") == ControlTypes.KSC_FACILITIES)
                    InputLockManager.RemoveControlLock("KCTKSCLock");
                return;
            }

            //Begin primary mod functions

            KCT_GameStates.UT = Planetarium.GetUniversalTime();

            KCT_GUI.guiDataSaver.Load();

            switch (HighLogic.LoadedScene)
            {
                case GameScenes.EDITOR:

                    //if (HighLogic.LoadedSceneIsEditor)
                    {
                        KCT_GUI.hideAll();
                        if (!KCT_GUI.PrimarilyDisabled)
                        {
                            KCT_GUI.showEditorGUI = KCT_GameStates.showWindows[1];
                            if (KCT_GUI.showEditorGUI)
                                KCT_GUI.ClickOn();
                            else
                                KCT_GUI.ClickOff();
                        }
                    }
                    break;
                case GameScenes.SPACECENTER:
                    //else if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    {
                        bool shouldStart = KCT_GUI.showFirstRun;
                        KCT_GUI.hideAll();
                        if (!shouldStart)
                        {
                            KCT_GUI.showBuildList = KCT_GameStates.showWindows[0];
                            if (KCT_GUI.showBuildList)
                                KCT_GUI.ClickOn();
                            else
                                KCT_GUI.ClickOff();
                        }
                        KCT_GUI.showFirstRun = shouldStart;
                    }
                    break;
                case GameScenes.FLIGHT:
                    if ( /*HighLogic.LoadedSceneIsFlight && */ FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH &&
                        FlightGlobals.ActiveVessel.GetCrewCount() == 0 && KCT_GameStates.launchedCrew.Count > 0)
                    {
                        KerbalRoster roster = HighLogic.CurrentGame.CrewRoster;

                        for (int i = 0; i < FlightGlobals.ActiveVessel.parts.Count; i++)
                        {
                            Part p = FlightGlobals.ActiveVessel.parts[i];
                            //KCTDebug.Log("craft: " + p.craftID);
                            KCTDebug.LogError("Part being tested: " + p.partInfo.title);
                            {
                                CrewedPart cP = KCT_GameStates.launchedCrew.Find(part => part.partID == p.craftID);
                                if (cP == null) continue;
                                List<ProtoCrewMember> crewList = cP.crewList;
                                KCTDebug.LogError("cP.crewList.Count: " + cP.crewList.Count);
                                foreach (ProtoCrewMember crewMember in crewList)
                                {
                                    if (crewMember != null)
                                    {
                                        ProtoCrewMember finalCrewMember = crewMember;
                                        if (crewMember.type == ProtoCrewMember.KerbalType.Crew)
                                        {
                                            finalCrewMember = roster.Crew.FirstOrDefault(c => c.name == crewMember.name);
                                        }
                                        else if (crewMember.type == ProtoCrewMember.KerbalType.Tourist)
                                        {
                                            finalCrewMember = roster.Tourist.FirstOrDefault(c => c.name == crewMember.name);
                                        }
                                        if (finalCrewMember == null)
                                        {
                                            KCTDebug.LogError("Error when assigning " + crewMember.name + " to " + p.partInfo.name + ". Cannot find Kerbal in list.");
                                            continue;
                                        }
                                        try
                                        {
                                            KCTDebug.Log("Assigning " + finalCrewMember.name + " to " + p.partInfo.name);
                                            if (p.AddCrewmember(finalCrewMember))//p.AddCrewmemberAt(finalCrewMember, crewList.IndexOf(crewMember)))
                                            {
                                                finalCrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
                                                if (finalCrewMember.seat != null)
                                                    finalCrewMember.seat.SpawnCrew();
                                            }
                                            else
                                            {
                                                KCTDebug.LogError("Error when assigning " + crewMember.name + " to " + p.partInfo.name);
                                                finalCrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                                                continue;
                                            }
                                        }
                                        catch
                                        {
                                            KCTDebug.LogError("Error when assigning " + crewMember.name + " to " + p.partInfo.name);
                                            finalCrewMember.rosterStatus = ProtoCrewMember.RosterStatus.Available;
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        KCT_GameStates.launchedCrew.Clear();
                    }
                    //if (HighLogic.LoadedSceneIsFlight)
                    {
                        KCT_GUI.hideAll();
                        if (KCT_GameStates.launchedVessel != null && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
                        {
                            KCT_GameStates.launchedVessel.KSC = null; //it's invalid now
                            KCTDebug.Log("Attempting to remove launched vessel from build list");
                            bool removed = KCT_GameStates.launchedVessel.RemoveFromBuildList();
                            if (removed) //Only do these when the vessel is first removed from the list
                            {
                                //Add the cost of the ship to the funds so it can be removed again by KSP
                                KCT_Utilities.AddFunds(KCT_GameStates.launchedVessel.cost, TransactionReasons.VesselRollout);
                                FlightGlobals.ActiveVessel.vesselName = KCT_GameStates.launchedVessel.shipName;
                            }

                            KCT_Recon_Rollout rollout = KCT_GameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.associatedID == KCT_GameStates.launchedVessel.id.ToString());
                            if (rollout != null)
                                KCT_GameStates.ActiveKSC.Recon_Rollout.Remove(rollout);

                            KCT_AirlaunchPrep alPrep = KCT_GameStates.ActiveKSC.AirlaunchPrep.FirstOrDefault(r => r.associatedID == KCT_GameStates.launchedVessel.id.ToString());
                            if (alPrep != null)
                                KCT_GameStates.ActiveKSC.AirlaunchPrep.Remove(alPrep);

                            AirlaunchParams alParams = KCT_GameStates.AirlaunchParams;
                            if (alParams != null && alParams.KCTVesselId == KCT_GameStates.launchedVessel.id &&
                                (!alParams.KSPVesselId.HasValue || alParams.KSPVesselId == FlightGlobals.ActiveVessel.id))
                            {
                                if (!alParams.KSPVesselId.HasValue) alParams.KSPVesselId = FlightGlobals.ActiveVessel.id;
                                StartCoroutine(AirlaunchRoutine(alParams, FlightGlobals.ActiveVessel.id));
                            }
                        }
                    }
                    break;
            }

            ratesUpdated = false;
            KCTDebug.Log("Start finished");

            wfsOne = new WaitForSeconds(1f);
            wfsTwo = new WaitForSeconds(2f);
            wfsHalf = new WaitForSeconds(0.5f);

            DelayedStart();

            UpdateTechlistIconColor();
            StartCoroutine(HandleEditorButton_Coroutine());
        }

        WaitForSeconds wfsHalf = null;
        WaitForSeconds wfsOne = null, wfsTwo = null;
        private IEnumerator AirlaunchRoutine(AirlaunchParams launchParams, Guid vesselId)
        {
            yield return wfsTwo;

            for (int i = 10; i > 0; i--)
            {
                if (FlightGlobals.ActiveVessel == null || FlightGlobals.ActiveVessel.id != vesselId)
                {
                    ScreenMessages.PostScreenMessage("[KCT] Airlaunch cancelled", 5f, ScreenMessageStyle.UPPER_CENTER, XKCDColors.Red);
                    yield break;
                }

                if (i == 1 && FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH)
                {
                    // Make sure that the vessel situation transitions from Prelaunch to Landed before airlaunching
                    FlightGlobals.ActiveVessel.situation = Vessel.Situations.LANDED;
                }

                ScreenMessages.PostScreenMessage($"[KCT] Launching in {i}...", 1f, ScreenMessageStyle.UPPER_CENTER, XKCDColors.Red);
                yield return wfsOne;
            }

            HyperEdit_Utilities.DoAirlaunch(launchParams);

            if (KCT_Utilities.IsPrincipiaInstalled)
                StartCoroutine(ClobberPrincipia());
        }

        /// <summary>
        /// Need to keep the vessel in Prelaunch state for a while if Principia is installed.
        /// Otherwise the vessel will spin out in a random way.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ClobberPrincipia()
        {
            if (FlightGlobals.ActiveVessel == null)
                yield return null;

            const int maxFramesWaited = 250;
            int i = 0;
            do
            {
                FlightGlobals.ActiveVessel.situation = Vessel.Situations.PRELAUNCH;
                yield return new WaitForFixedUpdate();
            } while (FlightGlobals.ActiveVessel.packed && i++ < maxFramesWaited);
        }

        private void EditorRecalculation()
        {
            if (editorRecalcuationRequired && !KCT_GUI.PrimarilyDisabled)
            {
                KCT_Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
                editorRecalcuationRequired = false;
            }
        }


        /// <summary>
        /// Coroutine to reset the launch button handlers every 1/2 second
        /// Needed because KSP seems to change them behind the scene sometimes
        /// </summary>
        /// <returns></returns>
        IEnumerator HandleEditorButton_Coroutine()
        {
 
            while (true)
            {
                if (HighLogic.LoadedSceneIsEditor)
                    KCT_Utilities.HandleEditorButton();
                yield return wfsHalf; // new WaitForSeconds(0.5f);
            }
        }

        private static int lvlCheckTimer = 0;
        private static bool ratesUpdated = false;
        public void FixedUpdate()
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            double lastUT = KCT_GameStates.UT > 0 ? KCT_GameStates.UT : Planetarium.GetUniversalTime();
            KCT_GameStates.UT = Planetarium.GetUniversalTime();
            try
            {
                // No need to set this true or false every single fixed upate
                // Actually not needed anymore not that the ToolbarController is handling it
#if false
                if (KCT_GUI.clicked && KCT_GameStates.toolbarControl != null)
                    KCT_GameStates.toolbarControl.SetTrue(false);
                else
                    KCT_GameStates.toolbarControl.SetFalse(false);
#endif
                if (!KCT_PresetManager.Instance.ActivePreset.generalSettings.Enabled)
                    return;

                if (!KCT_GameStates.erroredDuringOnLoad.AlertFired && KCT_GameStates.erroredDuringOnLoad.HasErrored())
                {
                    KCT_GameStates.erroredDuringOnLoad.FireAlert();
                }

                if (KCT_GameStates.UpdateLaunchpadDestructionState)
                {
                    KCTDebug.Log("Updating launchpad destruction state.");
                    KCT_GameStates.UpdateLaunchpadDestructionState = false;
                    KCT_GameStates.ActiveKSC.ActiveLPInstance.SetDestructibleStateFromNode();
                    if (KCT_GameStates.ActiveKSC.ActiveLPInstance.upgradeRepair)
                    {
                        //repair everything, then update the node
                        KCT_GameStates.ActiveKSC.ActiveLPInstance.RefreshDestructionNode();
                        KCT_GameStates.ActiveKSC.ActiveLPInstance.CompletelyRepairNode();
                        KCT_GameStates.ActiveKSC.ActiveLPInstance.SetDestructibleStateFromNode();
                    }

                }

                if (!ratesUpdated)
                {
                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
                    {
                        if (ScenarioUpgradeableFacilities.GetFacilityLevelCount(SpaceCenterFacility.VehicleAssemblyBuilding) >= 0)
                        {
                            ratesUpdated = true;
                            KCTDebug.Log("Updating build rates");
                            foreach (KCT_KSC KSC in KCT_GameStates.KSCs)
                            {
                                KSC?.RecalculateBuildRates();
                                KSC?.RecalculateUpgradedBuildRates();
                            }

                            KCTDebug.Log("Rates updated");

                            foreach (SpaceCenterFacility facility in Enum.GetValues(typeof(SpaceCenterFacility)))
                            {
                                KCT_GameStates.BuildingMaxLevelCache[facility.ToString()] = ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility);
                                KCTDebug.Log("Cached " + facility.ToString() + " max at " + KCT_GameStates.BuildingMaxLevelCache[facility.ToString()]);
                            }
                        }
                    }
                    else
                    {
                        ratesUpdated = true;
                    }
                }

                if (KCT_GameStates.ActiveKSC?.ActiveLPInstance != null && HighLogic.LoadedScene == GameScenes.SPACECENTER && KCT_Utilities.CurrentGameIsCareer())
                {
                    if (lvlCheckTimer++ > 30)
                    {
                        lvlCheckTimer = 0;
                        if (KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.LaunchPad) != KCT_GameStates.ActiveKSC.ActiveLPInstance.level)
                        {
                            KCT_GameStates.ActiveKSC.SwitchLaunchPad(KCT_GameStates.ActiveKSC.ActiveLaunchPadID, false);
                            KCT_GameStates.UpdateLaunchpadDestructionState = true;
                        }
                    }
                }
                //Warp code
                if (!KCT_GUI.PrimarilyDisabled && (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION))
                {
                    IKCTBuildItem ikctItem = KCT_Utilities.NextThingToFinish();
                    if (KCT_GameStates.targetedItem == null && ikctItem != null) KCT_GameStates.targetedItem = ikctItem;
                    double remaining = ikctItem != null ? ikctItem.GetTimeLeft() : -1;
                    double dT = TimeWarp.CurrentRate / (KCT_GameStates.UT - lastUT);
                    if (dT >= 20)
                        dT = 0.1;
                    //KCTDebug.Log("dt: " + dT);
                    int nBuffers = 1;
                    if (KCT_GameStates.canWarp && ikctItem != null && !ikctItem.IsComplete())
                    {
                        int warpRate = TimeWarp.CurrentRateIndex;
                        if (warpRate < KCT_GameStates.lastWarpRate) //if something else changes the warp rate then release control to them, such as Kerbal Alarm Clock
                        {
                            KCT_GameStates.canWarp = false;
                            KCT_GameStates.lastWarpRate = 0;
                        }
                        else
                        {
                            if (ikctItem == KCT_GameStates.targetedItem && warpRate > 0 && TimeWarp.fetch.warpRates[warpRate] * dT * nBuffers > Math.Max(remaining, 0))
                            {
                                int newRate = warpRate;
                                //find the first rate that is lower than the current rate
                                while (newRate > 0)
                                {
                                    if (TimeWarp.fetch.warpRates[newRate] * dT * nBuffers < remaining)
                                        break;
                                    newRate--;
                                }
                                KCTDebug.Log("Warping down to " + newRate + " (delta: " + (TimeWarp.fetch.warpRates[newRate] * dT) + ")");
                                TimeWarp.SetRate(newRate, true); //hopefully a faster warp down than before
                                warpRate = newRate;
                            }
                            else if (warpRate == 0 && KCT_GameStates.warpInitiated)
                            {
                                KCT_GameStates.canWarp = false;
                                KCT_GameStates.warpInitiated = false;
                                KCT_GameStates.targetedItem = null;

                            }
                            KCT_GameStates.lastWarpRate = warpRate;
                        }

                    }
                    else if (ikctItem != null && ikctItem == KCT_GameStates.targetedItem && (KCT_GameStates.warpInitiated || KCT_GameStates.settings.ForceStopWarp) && TimeWarp.CurrentRateIndex > 0 && (remaining < 1) && (!ikctItem.IsComplete())) //Still warp down even if we don't control the clock
                    {
                        TimeWarp.SetRate(0, true);
                        KCT_GameStates.warpInitiated = false;
                        KCT_GameStates.targetedItem = null;
                    }
                }

                if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
                {
                    KCT_Utilities.SetActiveKSCToRSS();
                }

                if (!KCT_GUI.PrimarilyDisabled && HighLogic.LoadedScene == GameScenes.SPACECENTER)
                {
                    if (KSP.UI.Screens.VesselSpawnDialog.Instance != null && KSP.UI.Screens.VesselSpawnDialog.Instance.Visible)
                    {
                        KSP.UI.Screens.VesselSpawnDialog.Instance.ButtonClose();
                        KCTDebug.Log("Attempting to close spawn dialog!");
                    }
                }

                if (!KCT_GUI.PrimarilyDisabled)
                    KCT_Utilities.ProgressBuildTime();

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        bool iconUpdated = false;
        public void LateUpdate()
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            if (KSP.UI.Screens.RDController.Instance != null && !iconUpdated)
            {
                UpdateTechlistIconColor();
                iconUpdated = true;
            }
            else
                iconUpdated = false;
        }

        public void UpdateTechlistIconColor()
        {
            if (KSP.UI.Screens.RDController.Instance != null)
            {
                for (int i = KSP.UI.Screens.RDController.Instance.nodes.Count; i-- > 0;)
                {
                    KSP.UI.Screens.RDNode node = KSP.UI.Screens.RDController.Instance.nodes[i];
                    if (node?.tech != null)
                    {
                        if (HasTechInList(node.tech.techID))
                        {
                            node.graphics?.SetIconColor(XKCDColors.KSPNotSoGoodOrange);
                        }
                        // else reset? Bleh, why bother.
                    }
                }
            }
        }

        protected bool HasTechInList(string id)
        {
            for (int i = KCT_GameStates.TechList.Count; i-- > 0;)
                if (KCT_GameStates.TechList[i].techID == id)
                    return true;

            return false;
        }

        public static void DelayedStart()
        {
            if (KCT_Utilities.CurrentGameIsMission())
            {
                return;
            }

            KCTDebug.Log("DelayedStart start");
            if (KCT_PresetManager.Instance?.ActivePreset == null || !KCT_PresetManager.Instance.ActivePreset.generalSettings.Enabled)
                return;

            if (KCT_GUI.PrimarilyDisabled) return;

            //The following should only be executed when fully enabled for the save

            if (KCT_GameStates.ActiveKSC == null)
            {
                KCT_Utilities.SetActiveKSCToRSS();
            }

            KCTDebug.Log("Checking vessels for missing parts.");
            //check that all parts are valid in all ships. If not, warn the user and disable that vessel (once that code is written)
            if (!KCT_GameStates.vesselErrorAlerted)
            {
                List<KCT_BuildListVessel> erroredVessels = new List<KCT_BuildListVessel>();
                foreach (KCT_KSC KSC in KCT_GameStates.KSCs) //this is faster on subsequent scene changes
                {
                    foreach (KCT_BuildListVessel blv in KSC.VABList)
                    {
                        if (!blv.allPartsValid)
                        {
                            //error!
                            KCTDebug.Log(blv.shipName + " contains invalid parts!");
                            erroredVessels.Add(blv);
                        }
                    }
                    foreach (KCT_BuildListVessel blv in KSC.VABWarehouse)
                    {
                        if (!blv.allPartsValid)
                        {
                            //error!
                            KCTDebug.Log(blv.shipName + " contains invalid parts!");
                            erroredVessels.Add(blv);
                        }
                    }
                    foreach (KCT_BuildListVessel blv in KSC.SPHList)
                    {
                        if (!blv.allPartsValid)
                        {
                            //error!
                            KCTDebug.Log(blv.shipName + " contains invalid parts!");
                            erroredVessels.Add(blv);
                        }
                    }
                    foreach (KCT_BuildListVessel blv in KSC.SPHWarehouse)
                    {
                        if (!blv.allPartsValid)
                        {
                            //error!
                            KCTDebug.Log(blv.shipName + " contains invalid parts!");
                            erroredVessels.Add(blv);
                        }
                    }
                }
                if (erroredVessels.Count > 0)
                    PopUpVesselError(erroredVessels);
                KCT_GameStates.vesselErrorAlerted = true;
            }

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (KCT_GameStates.EditorShipEditingMode)
                {
                    KCTDebug.Log("Editing " + KCT_GameStates.editedVessel.shipName);
                    EditorLogic.fetch.shipNameField.text = KCT_GameStates.editedVessel.shipName;
                }
            }
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                KCTDebug.Log("SP Start");
                if (!KCT_GUI.PrimarilyDisabled)
                {
                    if (ToolbarManager.ToolbarAvailable && KCT_GameStates.settings.PreferBlizzyToolbar)
                        if (KCT_GameStates.showWindows[0])
                            KCT_GUI.ClickOn();
                        else
                        {
                            //if (KCT_Events.instance != null && KCT_Events.instance.KCTButtonStock != null)
                            if (KCT_Events.instance != null && KCT_GameStates.toolbarControl != null)

                            {
                                if (KCT_GameStates.showWindows[0])
                                    KCT_GUI.ClickOn();
                            }
                        }
                    KCT_GUI.ResetBLWindow();
                }
                else
                {
                    KCT_GUI.showBuildList = false;
                    KCT_GameStates.showWindows[0] = false;
                }
                KCTDebug.Log("SP UI done");

                if (KCT_GameStates.firstStart)
                {
                    KCTDebug.Log("Showing first start.");
                    KCT_GameStates.firstStart = false;
                    KCT_GUI.showFirstRun = true;

                    //initialize the proper launchpad
                    KCT_GameStates.ActiveKSC.ActiveLPInstance.level = KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.LaunchPad);

                }

                KCTDebug.Log("SP switch starting");
                KCT_GameStates.ActiveKSC.SwitchLaunchPad(KCT_GameStates.ActiveKSC.ActiveLaunchPadID);
                KCTDebug.Log("SP switch done");

                foreach (KCT_KSC ksc in KCT_GameStates.KSCs)
                {
                    for (int i = 0; i < ksc.Recon_Rollout.Count; i++)
                    {
                        KCT_Recon_Rollout rr = ksc.Recon_Rollout[i];
                        if (rr.RRType != KCT_Recon_Rollout.RolloutReconType.Reconditioning && KCT_Utilities.FindBLVesselByID(new Guid(rr.associatedID)) == null)
                        {
                            KCTDebug.Log("Invalid Recon_Rollout at " + ksc.KSCName + ". ID " + rr.associatedID + " not found.");
                            ksc.Recon_Rollout.Remove(rr);
                            i--;
                        }
                    }

                    for (int i = 0; i < ksc.AirlaunchPrep.Count; i++)
                    {
                        KCT_AirlaunchPrep ap = ksc.AirlaunchPrep[i];
                        if (KCT_Utilities.FindBLVesselByID(new Guid(ap.associatedID)) == null)
                        {
                            KCTDebug.Log("Invalid KCT_AirlaunchPrep at " + ksc.KSCName + ". ID " + ap.associatedID + " not found.");
                            ksc.AirlaunchPrep.Remove(ap);
                            i--;
                        }
                    }
                }
                KCTDebug.Log("SP done");
            }
            KCTDebug.Log("DelayedStart finished");
        }

        public static void PopUpVesselError(List<KCT_BuildListVessel> errored)
        {
            DialogGUIBase[] options = new DialogGUIBase[2];
            options[0] = new DialogGUIButton("Understood", () => { });
            // new DialogGUIBase("Understood", () => { }); //do nothing and close the window
            options[1] = new DialogGUIButton("Delete Vessels", () =>
            {
                foreach (KCT_BuildListVessel blv in errored)
                {
                    blv.RemoveFromBuildList();
                    KCT_Utilities.AddFunds(blv.GetTotalCost(), TransactionReasons.VesselRollout);
                    //remove any associated recon_rollout
                }
            });

            string txt = "The following KCT vessels contain missing or invalid parts and have been quarantined. Either add the missing parts back into your game or delete the vessels. A file containing the ship names and missing parts has been added to your save folder.\n";
            string txtToWrite = "";
            foreach (KCT_BuildListVessel blv in errored)
            {
                txt += blv.shipName + "\n";
                txtToWrite += blv.shipName + "\n";
                txtToWrite += String.Join("\n", blv.MissingParts().ToArray());
                txtToWrite += "\n\n";
            }

            //HighLogic.SaveFolder
            //make new file for missing ships
            string filename = KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/missingParts.txt";
            System.IO.File.WriteAllText(filename, txtToWrite);


            //remove all rollout and recon items since they're invalid without the ships
            foreach (KCT_BuildListVessel blv in errored)
            {
                //remove any associated recon_rollout
                foreach (KCT_KSC ksc in KCT_GameStates.KSCs)
                {
                    for (int i = 0; i < ksc.Recon_Rollout.Count; i++)
                    {
                        KCT_Recon_Rollout rr = ksc.Recon_Rollout[i];
                        if (rr.associatedID == blv.id.ToString())
                        {
                            ksc.Recon_Rollout.Remove(rr);
                            i--;
                        }
                    }

                    for (int i = 0; i < ksc.AirlaunchPrep.Count; i++)
                    {
                        KCT_AirlaunchPrep ap = ksc.AirlaunchPrep[i];
                        if (ap.associatedID == blv.id.ToString())
                        {
                            ksc.AirlaunchPrep.Remove(ap);
                            i--;
                        }
                    }
                }
            }


            MultiOptionDialog diag = new MultiOptionDialog("missingPartsPopup", txt, "Vessels Contain Missing Parts", null, options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
            //PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Vessel Contains Missing Parts", "The KCT vessel " + errored.shipName + " contains missing or invalid parts. You will not be able to do anything with the vessel until the parts are available again.", "Understood", false, HighLogic.UISkin);
        }

        public static void ShowLaunchAlert(string launchSite)
        {
            KCTDebug.Log("Showing Launch Alert");
            if (KCT_GUI.PrimarilyDisabled)
            {
                EditorLogic.fetch.launchVessel();
            }
            else
            {
                KCT_Utilities.AddVesselToBuildList(launchSite);
                KCT_Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
            }
        }
    }
}
/*
Copyright (C) 2018  Michael Marvin, Zachary Eck

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
