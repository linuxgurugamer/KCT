using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace KerbalConstructionTime
{
    public static partial class KCT_GUI
    {
        private static List<string> launchSites = new List<string>();
        private static int MouseOnRolloutButton = -1;
        private static int MouseOnAirlaunchButton = -1;
        private static int listWindow = -1;
        private static bool VABSelected, SPHSelected, TechSelected;
        public static void SelectList(string list)
        {
            buildListWindowPosition.height = 1;
            switch (list)
            {
                case "VAB":
                    if (VABSelected)
                    {
                        listWindow = -1;
                        VABSelected = false;
                    }
                    else
                    {
                        listWindow = 0;
                        VABSelected = true;
                        SPHSelected = false;
                        TechSelected = false;
                    }
                    break;
                case "SPH":
                    if (SPHSelected)
                    {
                        listWindow = -1;
                        SPHSelected = false;
                    }
                    else
                    {
                        listWindow = 1;
                        VABSelected = false;
                        SPHSelected = true;
                        TechSelected = false;
                    }
                    break;
                case "Tech":
                    if (TechSelected)
                    {
                        listWindow = -1;
                        TechSelected = false;
                    }
                    else
                    {
                        listWindow = 2;
                        VABSelected = false;
                        SPHSelected = false;
                        TechSelected = true;
                    }
                    break;
                default:
                    listWindow = -1;
                    TechSelected = false;
                    VABSelected = false;
                    SPHSelected = false;
                    break;
            }
        }

        public enum VesselPadStatus { InStorage, RollingOut, RolledOut, RollingBack, Recovering };
        private static double costOfNewLP = -13;

        // these are private/static for efficiency, this way it only initialized them one time
        //private static bool buildListVarsInitted = false;
        private static GUIStyle redText, yellowText, greenText, normalButton, yellowButton, redButton, greenButton;

        internal static void InitBuildListVars()
        {
            Debug.Log("[KCT] InitBuildListVars");
            //buildListVarsInitted = true;
            redText = new GUIStyle(GUI.skin.label);
            redText.normal.textColor = Color.red;
            yellowText = new GUIStyle(GUI.skin.label);
            yellowText.normal.textColor = Color.yellow;
            greenText = new GUIStyle(GUI.skin.label);
            greenText.normal.textColor = Color.green;

            normalButton = new GUIStyle(GUI.skin.button);
            yellowButton = new GUIStyle(GUI.skin.button);
            yellowButton.normal.textColor = Color.yellow;
            yellowButton.hover.textColor = Color.yellow;
            yellowButton.active.textColor = Color.yellow;
            redButton = new GUIStyle(GUI.skin.button);
            redButton.normal.textColor = Color.red;
            redButton.hover.textColor = Color.red;
            redButton.active.textColor = Color.red;

            greenButton = new GUIStyle(GUI.skin.button);
            greenButton.normal.textColor = Color.green;
            greenButton.hover.textColor = Color.green;
            greenButton.active.textColor = Color.green;
        }

        public static void DrawBuildListWindow(int windowID)
        {
#if false
            if (!buildListVarsInitted)
                InitBuildListVars();
#endif
            int width1 = 120;
            int width2 = 100;
            int butW = 20;
            GUILayout.BeginVertical();
            //GUILayout.Label("Current KSC: " + KCT_GameStates.ActiveKSC.KSCName);
            //List next vessel to finish
            GUILayout.BeginHorizontal();
            GUILayout.Label("Next:", windowSkin.label);
            IKCTBuildItem buildItem = KCT_Utilities.NextThingToFinish();
            if (buildItem != null)
            {
                //KCT_BuildListVessel ship = (KCT_BuildListVessel)buildItem;

                string txt = buildItem.GetItemName(), locTxt = "VAB";
                if (buildItem.GetListType() == KCT_BuildListVessel.ListType.Reconditioning)
                {
                    KCT_Recon_Rollout reconRoll = buildItem as KCT_Recon_Rollout;
                    if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Reconditioning)
                    {
                        txt = "Reconditioning";
                        locTxt = reconRoll.launchPadID;
                    }
                    else if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Rollout)
                    {
                        KCT_BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.id.ToString() == reconRoll.associatedID);
                        txt = associated.shipName + " Rollout";
                        locTxt = reconRoll.launchPadID;
                    }
                    else if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Rollback)
                    {
                        KCT_BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.id.ToString() == reconRoll.associatedID);
                        txt = associated.shipName + " Rollback";
                        locTxt = reconRoll.launchPadID;
                    }
                    else
                    {
                        locTxt = "VAB";
                    }
                }
                else if (buildItem.GetListType() == KCT_BuildListVessel.ListType.VAB)
                {
                    locTxt = "VAB";
                }
                else if (buildItem.GetListType() == KCT_BuildListVessel.ListType.SPH)
                {
                    locTxt = "SPH";
                }
                else if (buildItem.GetListType() == KCT_BuildListVessel.ListType.TechNode)
                {
                    locTxt = "Tech";
                }
                else if (buildItem.GetListType() == KCT_BuildListVessel.ListType.KSC)
                {
                    locTxt = "KSC";
                }

                GUILayout.Label(txt);
                GUILayout.Label(locTxt, windowSkin.label);
                GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(buildItem.GetTimeLeft()));

                if (!HighLogic.LoadedSceneIsEditor && TimeWarp.CurrentRateIndex == 0 && GUILayout.Button("Warp to" + System.Environment.NewLine + "Complete"))
                {
                    KCT_GameStates.targetedItem = buildItem;
                    KCT_GameStates.canWarp = true;
                    KCT_Utilities.RampUpWarp();
                    KCT_GameStates.warpInitiated = true;
                }
                else if (!HighLogic.LoadedSceneIsEditor && TimeWarp.CurrentRateIndex > 0 && GUILayout.Button("Stop" + System.Environment.NewLine + "Warp"))
                {
                    KCT_GameStates.canWarp = false;
                    TimeWarp.SetRate(0, true);
                    KCT_GameStates.lastWarpRate = 0;
                }

                if (buildItem.GetTimeLeft() > 30)
                {
                    if (KCT_GameStates.settings.AutoKACAlarms && KACWrapper.APIReady) //don't check if less than 30 seconds to completion. Might fix errors people are seeing
                    {
                        DoKACAlarms(buildItem);
                    }
                    if (KCT_GameStates.settings.AutoStockAlarms) //don't check if less than 30 seconds to completion. Might fix errors people are seeing
                    {
                        DoStockAlarms(buildItem);
                    }
                }
            }
            else
            {
                GUILayout.Label("No Active Projects");
            }
            GUILayout.EndHorizontal();

            //Buttons for VAB/SPH lists
            // List<string> buttonList = new List<string> { "VAB", "SPH", "KSC" };
            //if (KCT_Utilities.CurrentGameHasScience() && !KCT_GameStates.settings.InstantTechUnlock) buttonList.Add("Tech");
            GUILayout.BeginHorizontal();
            //if (HighLogic.LoadedScene == GameScenes.SPACECENTER) { buttonList.Add("Upgrades"); buttonList.Add("Settings"); }
            //  int lastSelected = listWindow;
            // listWindow = GUILayout.Toolbar(listWindow, buttonList.ToArray());

            bool VABSelectedNew = GUILayout.Toggle(VABSelected, "VAB", GUI.skin.button);
            bool SPHSelectedNew = GUILayout.Toggle(SPHSelected, "SPH", GUI.skin.button);
            bool TechSelectedNew = false;
            if (KCT_Utilities.CurrentGameHasScience())
                TechSelectedNew = GUILayout.Toggle(TechSelected, "Tech", GUI.skin.button);
            if (VABSelectedNew != VABSelected)
                SelectList("VAB");
            else if (SPHSelectedNew != SPHSelected)
                SelectList("SPH");
            else if (TechSelectedNew != TechSelected)
                SelectList("Tech");
            if (GUILayout.Button("Plans"))
            {
                showBuildPlansWindow = !showBuildPlansWindow;
            }
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                if (GUILayout.Button("Upgrades"))
                {
                    showUpgradeWindow = true;
                    showBuildList = false;
                    showBLPlus = false;
                }
#if false
                if (GUILayout.Button("Settings"))
                {
                    showBuildList = false;
                    showBLPlus = false;
                    ShowSettings();
                }
#endif
            }
            GUILayout.EndHorizontal();
            //Content of lists
            if (listWindow == 0) //VAB Build List
            {
                List<KCT_BuildListVessel> buildList = KCT_GameStates.ActiveKSC.VABList;
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:");
                GUILayout.Label("Progress:", GUILayout.Width(width1 / 2));
                GUILayout.Label("Time Left:", GUILayout.Width(width2));
                GUILayout.EndHorizontal();

                foreach (KCT_Recon_Rollout reconditioning in KCT_GameStates.ActiveKSC.Recon_Rollout.FindAll(r => r.RRType == KCT_Recon_Rollout.RolloutReconType.Reconditioning))
                {
                    GUILayout.BeginHorizontal();
                    if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("Warp To", GUILayout.Width((butW + 4) * 3)))
                    {
                        KCT_GameStates.targetedItem = reconditioning;
                        KCT_GameStates.canWarp = true;
                        KCT_Utilities.RampUpWarp(reconditioning);
                        KCT_GameStates.warpInitiated = true;
                    }

                    GUILayout.Label("Reconditioning: " + reconditioning.launchPadID);
                    GUILayout.Label(reconditioning.ProgressPercent().ToString() + "%", GUILayout.Width(width1 / 2));
                    GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(reconditioning.GetTimeLeft()), GUILayout.Width(width2));
                    //GUILayout.Label(Math.Round(KCT_GameStates.ActiveKSC.GetReconditioning().BP, 2).ToString(), GUILayout.Width(width1 / 2 + 10));

                    //GUILayout.Space((butW + 4) * 3);
                    GUILayout.EndHorizontal();
                }

                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
                {
                    if (buildList.Count == 0)
                    {
                        GUILayout.Label("No vessels under construction! Go to the VAB to build more.");
                    }
                    for (int i = 0; i < buildList.Count; i++)
                    {
                        KCT_BuildListVessel b = buildList[i];
                        if (!b.allPartsValid)
                            continue;
                        GUILayout.BeginHorizontal();
                        //GUILayout.Label(b.shipName, GUILayout.Width(width1));

                        if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("*", GUILayout.Width(butW)))
                        {
                            if (IDSelected == b.id)
                                showBLPlus = !showBLPlus;
                            else
                                showBLPlus = true;
                            IDSelected = b.id;
                        }
                        else if (HighLogic.LoadedSceneIsEditor)
                        {
                            //GUILayout.Space(butW);
                            if (GUILayout.Button("X", GUILayout.Width(butW)))
                            {
                                InputLockManager.SetControlLock(ControlTypes.EDITOR_SOFT_LOCK, "KCTPopupLock");
                                IDSelected = b.id;
                                DialogGUIBase[] options = new DialogGUIBase[2];
                                options[0] = new DialogGUIButton("Yes", ScrapVessel);
                                options[1] = new DialogGUIButton("No", DummyVoid);
                                MultiOptionDialog diag = new MultiOptionDialog("scrapVesselPopup", "Are you sure you want to scrap this vessel?", "Scrap Vessel", null, options: options);
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                            }
                        }

                        if (i > 0 && GUILayout.Button("^", GUILayout.Width(butW)))
                        {
                            buildList.RemoveAt(i);
                            if (GameSettings.MODIFIER_KEY.GetKey())
                            {
                                buildList.Insert(0, b);
                            }
                            else
                            {
                                buildList.Insert(i - 1, b);
                            }
                        }
                        else if (i == 0)
                        {
                            //      GUILayout.Space(butW + 4);
                        }
                        if (i < buildList.Count - 1 && GUILayout.Button("v", GUILayout.Width(butW)))
                        {
                            buildList.RemoveAt(i);
                            if (GameSettings.MODIFIER_KEY.GetKey())
                            {
                                buildList.Add(b);
                            }
                            else
                            {
                                buildList.Insert(i + 1, b);
                            }
                        }
                        else if (i >= buildList.Count - 1)
                        {
                            //      GUILayout.Space(butW + 4);
                        }


                        GUILayout.Label(b.shipName);
                        GUILayout.Label(Math.Round(b.ProgressPercent(), 2).ToString() + "%", GUILayout.Width(width1 / 2));
                        if (b.buildRate > 0)
                            GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(b.timeLeft), GUILayout.Width(width2));
                        else
                            GUILayout.Label("Est: " + MagiCore.Utilities.GetColonFormattedTime((b.buildPoints + b.integrationPoints - b.progress) / KCT_Utilities.GetBuildRate(0, KCT_BuildListVessel.ListType.VAB, null)), GUILayout.Width(width2));
                        // GUILayout.Label(Math.Round(b.buildPoints, 2).ToString(), GUILayout.Width(width1 / 2 + 10));
                        GUILayout.EndHorizontal();
                    }

                    //ADD Storage here!
                    buildList = KCT_GameStates.ActiveKSC.VABWarehouse;
                    GUILayout.Label("__________________________________________________");
                    GUILayout.Label("VAB Storage");
                    if (KCT_Utilities.IsVabRecoveryAvailable() && GUILayout.Button("Recover Active Vessel"))
                    {
                        if (!KCT_Utilities.RecoverActiveVesselToStorage(KCT_BuildListVessel.ListType.VAB))
                        {
                            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "vesselRecoverErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);
                        }
                    }
                    if (buildList.Count == 0)
                    {
                        GUILayout.Label("No vessels in storage!\nThey will be stored here when they are complete.");
                    }

                    //KCT_Recon_Rollout rollback = KCT_GameStates.ActiveKSC.GetReconRollout(KCT_Recon_Rollout.RolloutReconType.Rollback);
                    bool rolloutEnabled = KCT_PresetManager.Instance.ActivePreset.generalSettings.ReconditioningTimes && KCT_PresetManager.Instance.ActivePreset.timeSettings.RolloutReconSplit > 0;
                    for (int i = 0; i < buildList.Count; i++)
                    {
                        KCT_BuildListVessel b = buildList[i];
                        if (!b.allPartsValid)
                            continue;
                        string launchSite = b.launchSite;
                        if (launchSite == "LaunchPad")
                        {
                            if (b.launchSiteID >= 0)
                                launchSite = KCT_GameStates.ActiveKSC.LaunchPads[b.launchSiteID].name;
                            else
                                launchSite = KCT_GameStates.ActiveKSC.ActiveLPInstance.name;
                        }
                        KCTDebug.Log("Rolling out, 2 to: " + launchSite);
                        KCT_Recon_Rollout rollout = KCT_GameStates.ActiveKSC.GetReconRollout(KCT_Recon_Rollout.RolloutReconType.Rollout, launchSite);
                        KCT_Recon_Rollout rollback = KCT_GameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.associatedID == b.id.ToString() && r.RRType == KCT_Recon_Rollout.RolloutReconType.Rollback);
                        KCT_Recon_Rollout recovery = KCT_GameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.associatedID == b.id.ToString() && r.RRType == KCT_Recon_Rollout.RolloutReconType.Recovery);
                        GUIStyle textColor = new GUIStyle(GUI.skin.label);
                        GUIStyle buttonColor = new GUIStyle(GUI.skin.button);

                        VesselPadStatus padStatus = VesselPadStatus.InStorage;
                        if (rollback != null)
                            padStatus = VesselPadStatus.RollingBack;
                        if (recovery != null)
                            padStatus = VesselPadStatus.Recovering;

                        string status = "In Storage";
                        if (rollout != null && rollout.associatedID == b.id.ToString())
                        {
                            padStatus = VesselPadStatus.RollingOut;
                            status = "Rolling Out to " + launchSite;
                            textColor = yellowText;
                            if (rollout.IsComplete())
                            {
                                padStatus = VesselPadStatus.RolledOut;
                                status = "At " + launchSite;
                                textColor = greenText;
                            }
                        }
                        else if (rollback != null)
                        {
                            status = "Rolling Back from " + launchSite;
                            textColor = yellowText;
                        }
                        else if (recovery != null)
                        {
                            status = "Recovering";
                            textColor = redText;
                        }

                        GUILayout.BeginHorizontal();
                        if (!HighLogic.LoadedSceneIsEditor && (padStatus == VesselPadStatus.InStorage || padStatus == VesselPadStatus.RolledOut))
                        {
                            if (GUILayout.Button("*", GUILayout.Width(butW)))
                            {
                                if (IDSelected == b.id)
                                    showBLPlus = !showBLPlus;
                                else
                                    showBLPlus = true;
                                IDSelected = b.id;
                            }
                        }
                        else
                            GUILayout.Space(butW + 4);

                        GUILayout.Label(b.shipName, textColor);
                        GUILayout.Label(status + "   ", textColor, GUILayout.ExpandWidth(false));
                        bool siteHasActiveRolloutOrRollback = rollout != null || KCT_GameStates.ActiveKSC.GetReconRollout(KCT_Recon_Rollout.RolloutReconType.Rollback, launchSite) != null;
                        if (rolloutEnabled && !HighLogic.LoadedSceneIsEditor && recovery == null && !siteHasActiveRolloutOrRollback) //rollout if the pad isn't busy
                        {
                            bool hasRecond = false;
                            bool isUpgrading = KCT_GameStates.KSCs.Find(ksc =>
                                ksc == KCT_GameStates.ActiveKSC
                                && ksc.KSCTech.Find(ub =>
                                    ub.isLaunchpad
                                    && ub.launchpadID == KCT_GameStates.ActiveKSC.LaunchPads.IndexOf(KCT_GameStates.ActiveKSC.ActiveLPInstance)) != null) != null;
                            GUIStyle btnColor = greenButton;
                            if (KCT_GameStates.ActiveKSC.ActiveLPInstance.destroyed || KCT_GameStates.ActiveKSC.ActiveLPInstance.upgradeRepair || isUpgrading)
                                btnColor = redButton;
                            else if (hasRecond = KCT_GameStates.ActiveKSC.GetReconditioning(KCT_GameStates.ActiveKSC.ActiveLPInstance.name) != null)
                                btnColor = yellowButton;
                            KCT_Recon_Rollout tmpRollout = new KCT_Recon_Rollout(b, KCT_Recon_Rollout.RolloutReconType.Rollout, b.id.ToString(), launchSite);
                            if (tmpRollout.cost > 0d)
                                GUILayout.Label("√" + tmpRollout.cost.ToString("N0"));
                            string rolloutText = (i == MouseOnRolloutButton ? MagiCore.Utilities.GetColonFormattedTime(tmpRollout.GetTimeLeft()) : "Rollout");
                            if (GUILayout.Button(rolloutText, btnColor, GUILayout.ExpandWidth(false)))
                            {
                                if (KCT_PresetManager.Instance.ActivePreset.generalSettings.ReconditioningBlocksPad && hasRecond)
                                {
                                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotRollOutReconditioningPopup", "Cannot Roll out!", "You must finish reconditioning the launchpad before you can roll out to it!", "Acknowledged", false, HighLogic.UISkin);
                                }
                                else
                                {
                                    List<string> facilityChecks = b.MeetsFacilityRequirements(false);
                                    if (facilityChecks.Count == 0)
                                    {
                                        if (!KCT_GameStates.ActiveKSC.ActiveLPInstance.destroyed)
                                        {
                                            if (!isUpgrading)
                                            {
                                                b.launchSiteID = KCT_GameStates.ActiveKSC.ActiveLaunchPadID;

                                                if (rollout != null)
                                                {
                                                    rollout.SwapRolloutType();
                                                }
                                                // tmpRollout.launchPadID = KCT_GameStates.ActiveKSC.ActiveLPInstance.name;
                                                KCT_GameStates.ActiveKSC.Recon_Rollout.Add(tmpRollout);
                                            }
                                            else
                                            {
                                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchUpgradePopup",
                                                    "Cannot Launch!",
                                                    "You must finish upgrading the launchpad before you can launch a vessel from it!",
                                                    "Acknowledged", false, HighLogic.UISkin);
                                            }
                                        }
                                        else
                                        {
                                            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchRepairPopup", "Cannot Launch!", "You must repair the launchpad before you can launch a vessel from it!", "Acknowledged", false, HighLogic.UISkin);
                                        }
                                    }
                                    else
                                    {
                                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchEditorChecksPopup", "Cannot Launch!", "Warning! This vessel did not pass the editor checks! Until you upgrade the VAB and/or Launchpad it cannot be launched. Listed below are the failed checks:\n" + String.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), "Acknowledged", false, HighLogic.UISkin);
                                    }
                                }
                            }
                            if (Event.current.type == EventType.Repaint)
                                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                    MouseOnRolloutButton = i;
                                else if (i == MouseOnRolloutButton)
                                    MouseOnRolloutButton = -1;
                        }
                        else if (rolloutEnabled && !HighLogic.LoadedSceneIsEditor && recovery == null && rollout != null && b.id.ToString() == rollout.associatedID && !rollout.IsComplete() && rollback == null &&
                            GUILayout.Button(MagiCore.Utilities.GetColonFormattedTime(rollout.GetTimeLeft()), GUILayout.ExpandWidth(false))) //swap rollout to rollback
                        {
                            rollout.SwapRolloutType();
                        }
                        else if (rolloutEnabled && !HighLogic.LoadedSceneIsEditor && recovery == null && rollback != null && !rollback.IsComplete())
                        {
                            if (rollout == null)
                            {
                                if (GUILayout.Button(MagiCore.Utilities.GetColonFormattedTime(rollback.GetTimeLeft()), GUILayout.ExpandWidth(false))) //switch rollback back to rollout
                                    rollback.SwapRolloutType();
                            }
                            else
                            {
                                GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(rollback.GetTimeLeft()), GUILayout.ExpandWidth(false));
                            }
                        }
                        else if (HighLogic.LoadedScene != GameScenes.TRACKSTATION && recovery == null && (!rolloutEnabled || (rollout != null && b.id.ToString() == rollout.associatedID && rollout.IsComplete())))
                        {
                            KCT_LaunchPad pad = KCT_GameStates.ActiveKSC.LaunchPads.Find(lp => lp.name == launchSite);
                            bool operational = pad != null ? !pad.destroyed : !KCT_GameStates.ActiveKSC.ActiveLPInstance.destroyed;
                            GUIStyle btnColor = greenButton;
                            string launchTxt = "Launch";
                            if (!operational)
                            {
                                launchTxt = "Repairs Required";
                                btnColor = redButton;
                            }
                            else if (KCT_Utilities.ReconditioningActive(null, launchSite))
                            {
                                launchTxt = "Reconditioning";
                                btnColor = yellowButton;
                            }
                            if (rolloutEnabled && GameSettings.MODIFIER_KEY.GetKey() && GUILayout.Button("Roll Back", GUILayout.ExpandWidth(false)))
                            {
                                rollout.SwapRolloutType();
                            }
                            else if (!GameSettings.MODIFIER_KEY.GetKey() && GUILayout.Button(launchTxt, btnColor, GUILayout.ExpandWidth(false)))
                            {
                                if (b.launchSiteID >= 0)
                                {
                                    KCT_GameStates.ActiveKSC.SwitchLaunchPad(b.launchSiteID);
                                }
                                b.launchSiteID = KCT_GameStates.ActiveKSC.ActiveLaunchPadID;

                                List<string> facilityChecks = b.MeetsFacilityRequirements(false);
                                if (facilityChecks.Count == 0)
                                {
                                    // bool operational = !KCT_GameStates.ActiveKSC.ActiveLPInstance.destroyed;// && KCT_Utilities.LaunchFacilityIntact(KCT_BuildListVessel.ListType.VAB);//new PreFlightTests.FacilityOperational("LaunchPad", "building").Test();
                                    if (!operational)
                                    {
                                        //ScreenMessages.PostScreenMessage("You must repair the launchpad prior to launch!", 4.0f, ScreenMessageStyle.UPPER_CENTER);
                                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchRepairPopup", "Cannot Launch!", "You must repair the launchpad before you can launch a vessel from it!", "Acknowledged", false, HighLogic.UISkin);
                                    }
                                    else if (KCT_Utilities.ReconditioningActive(null, launchSite))
                                    {
                                        //can't launch now
                                        ScreenMessage message = new ScreenMessage("[KCT] Cannot launch while LaunchPad is being reconditioned. It will be finished in "
                                            + MagiCore.Utilities.GetFormattedTime(((IKCTBuildItem)KCT_GameStates.ActiveKSC.GetReconditioning(launchSite)).GetTimeLeft()), 4.0f, ScreenMessageStyle.UPPER_CENTER);
                                        ScreenMessages.PostScreenMessage(message);
                                    }
                                    else
                                    {
                                        KCT_GameStates.launchedVessel = b;
                                        KCT_GameStates.launchedVessel.KSC = null;
                                        if (ShipConstruction.FindVesselsLandedAt(HighLogic.CurrentGame.flightState, b.launchSite).Count == 0)//  ShipConstruction.CheckLaunchSiteClear(HighLogic.CurrentGame.flightState, "LaunchPad", false))
                                        {
                                            showBLPlus = false;
                                            // buildList.RemoveAt(i);
                                            if (!IsCrewable(b.ExtractedParts))
                                                b.Launch();
                                            else
                                            {
                                                showBuildList = false;

                                                if (KCT_GameStates.toolbarControl != null)
                                                {
                                                    KCT_GameStates.toolbarControl.SetFalse();
                                                }

                                                centralWindowPosition.height = 1;
                                                AssignInitialCrew();
                                                showShipRoster = true;
                                            }
                                        }
                                        else
                                        {
                                            showBuildList = false;
                                            showClearLaunch = true;
                                        }
                                    }
                                }
                                else
                                {
                                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchEditorChecksPopup", "Cannot Launch!", "Warning! This vessel did not pass the editor checks! Until you upgrade the VAB and/or Launchpad it cannot be launched. Listed below are the failed checks:\n" + String.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), "Acknowledged", false, HighLogic.UISkin);
                                }
                            }
                        }
                        else if (!HighLogic.LoadedSceneIsEditor && recovery != null)
                        {
                            GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(recovery.GetTimeLeft()), GUILayout.ExpandWidth(false));
                        }

                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
                GUILayout.BeginHorizontal();
                int lpCount = KCT_GameStates.ActiveKSC.LaunchPadCount;
                if (lpCount > 1 && GUILayout.Button("<<", GUILayout.ExpandWidth(false)))
                {
                    KCT_GameStates.ActiveKSC.SwitchToPrevLaunchPad();
                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        KCT_Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
                    }
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label("Current: " + KCT_GameStates.ActiveKSC.ActiveLPInstance.name + " (" + (KCT_GameStates.ActiveKSC.ActiveLPInstance.level + 1) + ")");
                if (costOfNewLP == -13)
                    costOfNewLP = KCT_MathParsing.GetStandardFormulaValue("NewLaunchPadCost", new Dictionary<string, string> { { "N", KCT_GameStates.ActiveKSC.LaunchPads.Count.ToString() } });
                //  if (KCT_Utilities.KSCSwitcherInstalled) //todo
                //      costOfNewLP = -1; //disable purchasing additional launchpads when playing with KSC Switcher (until upgrades are properly per KSC)
                if (GUILayout.Button("Rename", GUILayout.ExpandWidth(false)))
                {
                    renamingLaunchPad = true;
                    newName = KCT_GameStates.ActiveKSC.ActiveLPInstance.name;
                    showDismantlePad = false;
                    showNewPad = false;
                    showRename = true;
                    showBuildList = false;
                    showBLPlus = false;
                }
                if (costOfNewLP >= 0 && GUILayout.Button("New", GUILayout.ExpandWidth(false)))
                {
                    newName = "LaunchPad " + (KCT_GameStates.ActiveKSC.LaunchPads.Count + 1);
                    showDismantlePad = false;
                    showNewPad = true;
                    showRename = false;
                    showBuildList = false;
                    showBLPlus = false;
                }
                if (lpCount > 1 && GUILayout.Button("Dismantle", GUILayout.ExpandWidth(false)))
                {
                    showDismantlePad = true;
                    showNewPad = false;
                    showRename = false;
                    showBuildList = false;
                    showBLPlus = false;
                }
                GUILayout.FlexibleSpace();
                if (lpCount > 1 && GUILayout.Button(">>", GUILayout.ExpandWidth(false)))
                {
                    KCT_GameStates.ActiveKSC.SwitchToNextLaunchPad();
                    if (HighLogic.LoadedSceneIsEditor)
                    {
                        KCT_Utilities.RecalculateEditorBuildTime(EditorLogic.fetch.ship);
                    }
                }
                GUILayout.EndHorizontal();
            }
            else if (listWindow == 1) //SPH Build List
            {
                List<KCT_BuildListVessel> buildList = KCT_GameStates.ActiveKSC.SPHList;
                GUILayout.BeginHorizontal();
                //  GUILayout.Space((butW + 4) * 3);
                GUILayout.Label("Name:");
                GUILayout.Label("Progress:", GUILayout.Width(width1 / 2));
                GUILayout.Label("Time Left:", GUILayout.Width(width2));
                //GUILayout.Label("BP:", GUILayout.Width(width1 / 2 + 10));
                GUILayout.EndHorizontal();
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));
                {
                    if (buildList.Count == 0)
                    {
                        GUILayout.Label("No vessels under construction! Go to the SPH to build more.");
                    }
                    for (int i = 0; i < buildList.Count; i++)
                    {
                        KCT_BuildListVessel b = buildList[i];
                        if (!b.allPartsValid)
                            continue;
                        GUILayout.BeginHorizontal();
                        if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("*", GUILayout.Width(butW)))
                        {
                            if (IDSelected == b.id)
                                showBLPlus = !showBLPlus;
                            else
                                showBLPlus = true;
                            IDSelected = b.id;
                        }
                        else if (HighLogic.LoadedSceneIsEditor)
                        {
                            //GUILayout.Space(butW);
                            if (GUILayout.Button("X", GUILayout.Width(butW)))
                            {
                                InputLockManager.SetControlLock(ControlTypes.EDITOR_SOFT_LOCK, "KCTPopupLock");
                                IDSelected = b.id;
                                DialogGUIBase[] options = new DialogGUIBase[2];
                                options[0] = new DialogGUIButton("Yes", ScrapVessel);
                                options[1] = new DialogGUIButton("No", DummyVoid);
                                MultiOptionDialog diag = new MultiOptionDialog("scrapConfirmPopup", "Are you sure you want to scrap " + b.shipName + "?", "Scrap Vessel", null, 300, options);
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                            }
                        }

                        if (i > 0 && GUILayout.Button("^", GUILayout.Width(butW)))
                        {
                            buildList.RemoveAt(i);
                            if (GameSettings.MODIFIER_KEY.GetKey())
                            {
                                buildList.Insert(0, b);
                            }
                            else
                            {
                                buildList.Insert(i - 1, b);
                            }
                        }
                        else if (i == 0)
                        {
                            //          GUILayout.Space(butW + 4);
                        }
                        if (i < buildList.Count - 1 && GUILayout.Button("v", GUILayout.Width(butW)))
                        {
                            buildList.RemoveAt(i);
                            if (GameSettings.MODIFIER_KEY.GetKey())
                            {
                                buildList.Add(b);
                            }
                            else
                            {
                                buildList.Insert(i + 1, b);
                            }
                        }
                        else if (i >= buildList.Count - 1)
                        {
                            //         GUILayout.Space(butW + 4);
                        }

                        GUILayout.Label(b.shipName);
                        GUILayout.Label(Math.Round(b.ProgressPercent(), 2).ToString() + "%", GUILayout.Width(width1 / 2));
                        if (b.buildRate > 0)
                            GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(b.timeLeft), GUILayout.Width(width2));
                        else
                            GUILayout.Label("Est: " + MagiCore.Utilities.GetColonFormattedTime((b.buildPoints + b.integrationPoints - b.progress) / KCT_Utilities.GetBuildRate(0, KCT_BuildListVessel.ListType.SPH, null)), GUILayout.Width(width2));
                        //GUILayout.Label(Math.Round(b.buildPoints, 2).ToString(), GUILayout.Width(width1 / 2 + 10));
                        GUILayout.EndHorizontal();
                    }

                    buildList = KCT_GameStates.ActiveKSC.SPHWarehouse;
                    GUILayout.Label("__________________________________________________");
                    GUILayout.Label("SPH Storage");
                    if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.IsRecoverable && FlightGlobals.ActiveVessel.IsClearToSave() == ClearToSaveStatus.CLEAR && GUILayout.Button("Recover Active Vessel"))
                    {
                        if (!KCT_Utilities.RecoverActiveVesselToStorage(KCT_BuildListVessel.ListType.SPH))
                        {
                            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "recoverShipErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);
                        }
                    }

                    for (int i = 0; i < buildList.Count; i++)
                    {
                        KCT_BuildListVessel b = buildList[i];
                        if (!b.allPartsValid)
                            continue;
                        string status = "";
                        GUIStyle textColor = new GUIStyle(GUI.skin.label);

                        KCT_Recon_Rollout recovery = KCT_GameStates.ActiveKSC.Recon_Rollout.FirstOrDefault(r => r.associatedID == b.id.ToString() && r.RRType == KCT_Recon_Rollout.RolloutReconType.Recovery);
                        if (recovery != null)
                            status = "Recovering";

                        KCT_AirlaunchPrep airlaunchPrep = KCT_GameStates.ActiveKSC.AirlaunchPrep.FirstOrDefault(r => r.associatedID == b.id.ToString());
                        if (airlaunchPrep != null)
                        {
                            if (airlaunchPrep.IsComplete())
                            {
                                status = "Ready";
                                textColor = greenText;
                            }
                            else
                            {
                                status = airlaunchPrep.GetItemName();
                                textColor = yellowText;
                            }
                        }

                        GUILayout.BeginHorizontal();
                        if (!HighLogic.LoadedSceneIsEditor && status == "")
                        {
                            if (GUILayout.Button("*", GUILayout.Width(butW)))
                            {
                                if (IDSelected == b.id)
                                    showBLPlus = !showBLPlus;
                                else
                                    showBLPlus = true;
                                IDSelected = b.id;
                            }
                        }
                        else
                            GUILayout.Space(butW + 4);

                        GUILayout.Label(b.shipName, textColor);
                        GUILayout.Label(status + "   ", GUILayout.ExpandWidth(false));
                        //ScenarioDestructibles.protoDestructibles["KSCRunway"].

                        if (HighLogic.LoadedScene != GameScenes.EDITOR && recovery == null && airlaunchPrep == null && AirlaunchTechLevel.AnyUnlocked())
                        {
                            var tmpPrep = new KCT_AirlaunchPrep(b, b.id.ToString());
                            if (tmpPrep.cost > 0d)
                                GUILayout.Label("√" + tmpPrep.cost.ToString("N0"));
                            string airlaunchText = i == MouseOnAirlaunchButton ? MagiCore.Utilities.GetColonFormattedTime(tmpPrep.GetTimeLeft()) : "Prep for airlaunch";
                            if (GUILayout.Button(airlaunchText, GUILayout.ExpandWidth(false)))
                            {
                                AirlaunchTechLevel lvl = AirlaunchTechLevel.GetCurrentLevel();
                                if (!lvl.CanLaunchVessel(b, out string failedReason))
                                {
                                    ScreenMessages.PostScreenMessage($"Vessel failed validation: {failedReason}", 6f, ScreenMessageStyle.UPPER_CENTER);
                                }
                                else
                                {
                                    KCT_GameStates.ActiveKSC.AirlaunchPrep.Add(tmpPrep);
                                }
                            }
                            if (Event.current.type == EventType.Repaint)
                                if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                                    MouseOnAirlaunchButton = i;
                                else if (i == MouseOnAirlaunchButton)
                                    MouseOnAirlaunchButton = -1;
                        }
                        else if (HighLogic.LoadedScene != GameScenes.EDITOR && recovery == null && airlaunchPrep != null)
                        {
                            string btnText = airlaunchPrep.IsComplete() ? "Unmount" : MagiCore.Utilities.GetColonFormattedTime(airlaunchPrep.GetTimeLeft());
                            if (GUILayout.Button(btnText, GUILayout.ExpandWidth(false)))
                            {
                                airlaunchPrep.SwitchDirection();
                            }
                        }

                        string launchBtnText = airlaunchPrep != null ? "Airlaunch" : "Launch";
                        if (HighLogic.LoadedScene != GameScenes.TRACKSTATION && recovery == null && (airlaunchPrep == null || airlaunchPrep.IsComplete()) && GUILayout.Button(launchBtnText, GUILayout.ExpandWidth(false)))
                        {
                            List<string> facilityChecks = b.MeetsFacilityRequirements(false);
                            if (facilityChecks.Count == 0)
                            {
                                bool operational = KCT_Utilities.LaunchFacilityIntact(KCT_BuildListVessel.ListType.SPH);//new PreFlightTests.FacilityOperational("Runway", "building").Test();
                                if (!operational)
                                {
                                    ScreenMessages.PostScreenMessage("You must repair the runway prior to launch!", 4.0f, ScreenMessageStyle.UPPER_CENTER);
                                }
                                else
                                {
                                    showBLPlus = false;
                                    KCT_GameStates.launchedVessel = b;
                                    KCT_GameStates.launchedVessel.KSC = null;

                                    if (ShipConstruction.FindVesselsLandedAt(HighLogic.CurrentGame.flightState, "Runway").Count == 0)
                                    {
                                        if (airlaunchPrep != null)
                                        {
                                            showBuildList = false;
                                            showAirlaunch = true;
                                        }
                                        else if (!IsCrewable(b.ExtractedParts))
                                        {
                                            b.Launch();
                                        }
                                        else
                                        {
                                            showBuildList = false;
                                            if (KCT_GameStates.toolbarControl != null)
                                            {
                                                KCT_GameStates.toolbarControl.SetFalse();
                                            }
                                            centralWindowPosition.height = 1;
                                            AssignInitialCrew();
                                            showShipRoster = true;
                                        }
                                    }
                                    else
                                    {
                                        showBuildList = false;
                                        showClearLaunch = true;
                                        showAirlaunch = airlaunchPrep != null;
                                    }
                                }
                            }
                            else
                            {
                                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotLaunchEditorChecksPopup", "Cannot Launch!", "Warning! This vessel did not pass the editor checks! Until you upgrade the SPH and/or Runway it cannot be launched. Listed below are the failed checks:\n" + String.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), "Acknowledged", false, HighLogic.UISkin);
                            }
                        }
                        else if (recovery != null)
                        {
                            GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(recovery.GetTimeLeft()), GUILayout.ExpandWidth(false));
                        }
                        GUILayout.EndHorizontal();
                    }
                    if (buildList.Count == 0)
                    {
                        GUILayout.Label("No vessels in storage!\nThey will be stored here when they are complete.");
                    }
                }
                GUILayout.EndScrollView();
            }
            else if (listWindow == 2) //Tech nodes
            {
                List<KCT_UpgradingBuilding> KSCList = KCT_GameStates.ActiveKSC.KSCTech;
                KCT_GameStates.KCT_TechItemIlist<KCT_TechItem> techList = KCT_GameStates.TechList;
                //GUILayout.Label("Tech Node Research");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:");
                GUILayout.Label("Progress:", GUILayout.Width(width1 / 2));
                GUILayout.Label("Time Left:", GUILayout.Width(width1));
                GUILayout.Space(70);
                GUILayout.EndHorizontal();
                scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));

                if (KCT_Utilities.CurrentGameIsCareer())
                {
                    if (KSCList.Count == 0)
                        GUILayout.Label("No KSC upgrade projects are currently underway.");
                    foreach (KCT_UpgradingBuilding KCTTech in KSCList)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(KCTTech.GetItemName());
                        GUILayout.Label(Math.Round(100 * KCTTech.progress / KCTTech.BP, 2) + " %", GUILayout.Width(width1 / 2));
                        GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(KCTTech.GetTimeLeft()), GUILayout.Width(width1));
                        if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("Warp", GUILayout.Width(70)))
                        {
                            KCT_GameStates.targetedItem = KCTTech;
                            KCT_GameStates.canWarp = true;
                            KCT_Utilities.RampUpWarp(KCTTech);
                            KCT_GameStates.warpInitiated = true;
                        }
                        else if (HighLogic.LoadedSceneIsEditor)
                            GUILayout.Space(70);
                        GUILayout.EndHorizontal();
                    }
                }

                if (techList.Count == 0)
                    GUILayout.Label("No tech nodes are being researched!\nBegin research by unlocking tech in the R&D building.");
                bool forceRecheck = false;
                int cancelID = -1;
                for (int i = 0; i < techList.Count; i++)
                {
                    KCT_TechItem t = techList[i];
                    GUILayout.BeginHorizontal();

                    if (GUILayout.Button("X", GUILayout.Width(butW)))
                    {
                        forceRecheck = true;
                        cancelID = i;
                        DialogGUIBase[] options = new DialogGUIBase[2];
                        options[0] = new DialogGUIButton("Yes", () => { CancelTechNode(cancelID); });
                        options[1] = new DialogGUIButton("No", DummyVoid);
                        MultiOptionDialog diag = new MultiOptionDialog("cancelNodePopup", "Are you sure you want to stop researching " + t.techName + "?\n\nThis will also cancel any dependent techs.", "Cancel Node?", null, 300, options);
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                    }

                    // Can move up if item above is not a parent.
                    List<string> parentList = KerbalConstructionTimeData.techNameToParents[t.techID];
                    bool canMoveUp = i > 0 && (parentList == null || !parentList.Contains(techList[i - 1].techID));

                    // Can move down if item below is not a child.
                    List<string> nextParentList = i < techList.Count - 1 ? KerbalConstructionTimeData.techNameToParents[techList[i + 1].techID] : null;
                    bool canMoveDown = nextParentList == null || !nextParentList.Contains(t.techID);

                    if (i > 0 && t.BuildRate != techList[0].BuildRate)
                    {
                        GUI.enabled = canMoveUp;
                        if (i > 0 && GUILayout.Button("^", GUILayout.Width(butW)))
                        {
                            techList.RemoveAt(i);
                            if (GameSettings.MODIFIER_KEY.GetKey())
                            {
                                // Find furthest postion tech can be moved to.
                                int newLocation = i - 1;
                                while (newLocation >= 0)
                                {
                                    if (parentList != null && parentList.Contains(techList[newLocation].techID))
                                        break;
                                    --newLocation;
                                }
                                ++newLocation;

                                techList.Insert(newLocation, t);
                            }
                            else
                            {
                                techList.Insert(i - 1, t);
                            }
                            forceRecheck = true;
                        }
                        GUI.enabled = true;
                    }
                    if ((i == 0 && t.BuildRate != techList[techList.Count - 1].BuildRate) || t.BuildRate != techList[techList.Count - 1].BuildRate)
                    {
                        GUI.enabled = canMoveDown;
                        if (i < techList.Count - 1 && GUILayout.Button("v", GUILayout.Width(butW)))
                        {
                            techList.RemoveAt(i);
                            if (GameSettings.MODIFIER_KEY.GetKey())
                            {
                                // Find furthest postion tech can be moved to.
                                int newLocation = i + 1;
                                while (newLocation < techList.Count)
                                {
                                    nextParentList = KerbalConstructionTimeData.techNameToParents[techList[newLocation].techID];
                                    if (nextParentList != null && nextParentList.Contains(t.techID))
                                        break;
                                    ++newLocation;
                                }

                                techList.Insert(newLocation, t);
                            }
                            else
                            {
                                techList.Insert(i + 1, t);
                            }
                            forceRecheck = true;
                        }
                        GUI.enabled = true;
                    }
                    if (forceRecheck)
                    {
                        forceRecheck = false;
                        for (int j = 0; j < techList.Count; j++)
                            techList[j].UpdateBuildRate(j);
                    }

                    string blockingPrereq = t.GetBlockingTech(techList);

                    GUILayout.Label(t.techName);
                    GUILayout.Label(Math.Round(100 * t.progress / t.scienceCost, 2) + " %", GUILayout.Width(width1 / 2));
                    if (t.BuildRate > 0)
                    {
                        if (blockingPrereq == null)
                            GUILayout.Label(MagiCore.Utilities.GetColonFormattedTime(t.TimeLeft), GUILayout.Width(width1));
                        else
                            GUILayout.Label("Waiting for PreReq", GUILayout.Width(width1));
                    }
                    else
                    {
                        GUILayout.Label("Est: " + MagiCore.Utilities.GetColonFormattedTime(t.EstimatedTimeLeft), GUILayout.Width(width1));
                    }
                    if (t.BuildRate > 0 && blockingPrereq == null)
                    {
                        if (!HighLogic.LoadedSceneIsEditor && GUILayout.Button("Warp", GUILayout.Width(45)))
                        {
                            KCT_GameStates.targetedItem = t;
                            KCT_GameStates.canWarp = true;
                            KCT_Utilities.RampUpWarp(t);
                            KCT_GameStates.warpInitiated = true;
                        }
                        else if (HighLogic.LoadedSceneIsEditor)
                            GUILayout.Space(45);
                    }
                    else
                        GUILayout.Space(45);

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }

            if (KCT_UpdateChecker.UpdateFound)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Current Version: " + KCT_UpdateChecker.CurrentVersion);
                GUILayout.Label("Latest: " + KCT_UpdateChecker.WebVersion);
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            // if (ToolbarManager.ToolbarAvailable && ToolbarManager.Instance != null && KCT_GameStates.settings.PreferBlizzyToolbar)
            if (!Input.GetMouseButtonDown(1) && !Input.GetMouseButtonDown(2))
                GUI.DragWindow();

            ClampWindow(ref buildListWindowPosition, strict: true);
        }

        static void DoStockAlarms(IKCTBuildItem buildItem)
        {
            double UT = Planetarium.GetUniversalTime();

            if (!KCT_Utilities.ApproximatelyEqual(KCT_GameStates.StockAlarmUT - UT, buildItem.GetTimeLeft()))
            {
                KCTDebug.Log("Stock Alarm being created!");
                KCT_GameStates.StockAlarmUT = (buildItem.GetTimeLeft() + UT);

                bool alarm = AlarmClockScenario.AlarmExists(KCT_GameStates.StockAlarmId);
                if (alarm)
                {
                    KCTDebug.Log("Removing existing alarm");
                    AlarmClockScenario.DeleteAlarm(KCT_GameStates.StockAlarmId);
                }

                if (!alarm)
                {
                    AlarmTypeBase alarmCheck = AlarmClockScenario.GetNextAlarm(UT);
                    AlarmTypeBase thisAlarm = alarmCheck;
                    while (thisAlarm != null)
                    {
                        if (thisAlarm.description.Contains("KCT:"))
                        {
                            KCTDebug.Log("Removing existing alarm");
                            AlarmClockScenario.DeleteAlarm(thisAlarm);
                            break;
                        }
                        else
                        {
                            alarmCheck = thisAlarm;
                        }
                        thisAlarm = AlarmClockScenario.GetNextAlarm(alarmCheck.ut);
                    }
                }

                string txt = "KCT: ";
                string finalTitle = "KCT Construction";
                if (buildItem.GetListType() == KCT_BuildListVessel.ListType.Reconditioning)
                {
                    KCT_Recon_Rollout reconRoll = buildItem as KCT_Recon_Rollout;
                    if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Reconditioning)
                    {
                        txt += reconRoll.launchPadID + " Reconditioning";
                        finalTitle = "KCT Reconditioning";
                    }
                    else if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Rollout)
                    {
                        KCT_BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.id.ToString() == reconRoll.associatedID);
                        txt += associated.shipName + " rollout at " + reconRoll.launchPadID;
                        finalTitle = "KCT Rollout";
                    }
                    else if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Rollback)
                    {
                        KCT_BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.id.ToString() == reconRoll.associatedID);
                        txt += associated.shipName + " rollback at " + reconRoll.launchPadID;
                        finalTitle = "KCT Rollback";
                    }
                    else
                    {
                        txt += buildItem.GetItemName() + " Complete";
                    }
                }
                else
                {
                    txt += buildItem.GetItemName() + " Complete";
                }
                AlarmTypeRaw alarmToSet = new AlarmTypeRaw
                {
                    title = finalTitle,
                    description = txt,
                    actions =
                            {
                                warp = AlarmActions.WarpEnum.KillWarp,
                                message = AlarmActions.MessageEnum.Yes
                            },
                    ut = KCT_GameStates.StockAlarmUT
                };
                AlarmClockScenario.AddAlarm(alarmToSet);
                KCT_GameStates.StockAlarmId = alarmToSet.Id;
                KCTDebug.Log("Stock Alarm created with ID: " + alarmToSet.Id);
            }
        }

        static void DoKACAlarms(IKCTBuildItem buildItem)
        {
            double UT = Planetarium.GetUniversalTime();
            if (!KCT_Utilities.ApproximatelyEqual(KCT_GameStates.KACAlarmUT - UT, buildItem.GetTimeLeft()))
            {
                KCTDebug.Log("KAC Alarm being created!");
                KCT_GameStates.KACAlarmUT = (buildItem.GetTimeLeft() + UT);
                KACWrapper.KACAPI.KACAlarm alarm = KACWrapper.KAC.Alarms.FirstOrDefault(a => a.ID == KCT_GameStates.KACAlarmId);
                if (alarm == null)
                {
                    alarm = KACWrapper.KAC.Alarms.FirstOrDefault(a => (a.Name.StartsWith("KCT: ")));
                }
                if (alarm != null)
                {
                    KCTDebug.Log("Removing existing alarm");
                    KACWrapper.KAC.DeleteAlarm(alarm.ID);
                }
                string txt = "KCT: ";
                if (buildItem.GetListType() == KCT_BuildListVessel.ListType.Reconditioning)
                {
                    KCT_Recon_Rollout reconRoll = buildItem as KCT_Recon_Rollout;
                    if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Reconditioning)
                    {
                        txt += reconRoll.launchPadID + " Reconditioning";
                    }
                    else if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Rollout)
                    {
                        KCT_BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.id.ToString() == reconRoll.associatedID);
                        txt += associated.shipName + " rollout at " + reconRoll.launchPadID;
                    }
                    else if (reconRoll.RRType == KCT_Recon_Rollout.RolloutReconType.Rollback)
                    {
                        KCT_BuildListVessel associated = reconRoll.KSC.VABWarehouse.FirstOrDefault(blv => blv.id.ToString() == reconRoll.associatedID);
                        txt += associated.shipName + " rollback at " + reconRoll.launchPadID;
                    }
                    else
                    {
                        txt += buildItem.GetItemName() + " Complete";
                    }
                }
                else
                    txt += buildItem.GetItemName() + " Complete";
                KCT_GameStates.KACAlarmId = KACWrapper.KAC.CreateAlarm(KACWrapper.KACAPI.AlarmTypeEnum.Raw, txt, KCT_GameStates.KACAlarmUT);
                KCTDebug.Log("KAC Alarm created with ID: " + KCT_GameStates.KACAlarmId);
            }
        }
        public static void CancelTechNode(int index)
        {

            if (KCT_GameStates.TechList.Count > index)
            {
                KCT_TechItem node = KCT_GameStates.TechList[index];
                KCTDebug.Log("Cancelling tech: " + node.techName);

                // cancel children
                for (int i = 0; i < KCT_GameStates.TechList.Count; i++)
                {
                    List<string> parentList = KerbalConstructionTimeData.techNameToParents[KCT_GameStates.TechList[i].techID];
                    string anyToUnlock = KerbalConstructionTimeData.techNameToAnyToUnlock[KCT_GameStates.TechList[i].techID];
                    bool delete = false;

                    if (!parentList.Contains(node.techID))
                        continue;

                    // if node has AnyToUnlock, needs to check for any still valid parent
                    if (anyToUnlock == "True")
                    {
                        delete = true;
                        foreach (string p in parentList)
                        {
                            if (p == node.techID) continue;

                            ProtoTechNode pn = ResearchAndDevelopment.Instance.GetTechState(p);
                            if ((KCT_GameStates.TechList.FindIndex(t => t.techID == p) >= 0))
                            {
                                delete = false;
                                break;
                            }
                            else if ((pn != null && pn.state == RDTech.State.Available))
                            {
                                delete = false;
                                break;
                            }
                        }
                    }
                    // otherwise, just cancel it
                    else
                        delete = true;

                    if (delete)
                    {
                        CancelTechNode(i);
                        // recheck list in case multiple levels of children were deleted.
                        i = -1;
                        index = KCT_GameStates.TechList.FindIndex(t => t.techID == node.techID);
                    }
                }

                if (KCT_Utilities.CurrentGameHasScience())
                {
                    bool valBef = KCT_GameStates.isRefunding;
                    KCT_GameStates.isRefunding = true;
                    try
                    {
                        ResearchAndDevelopment.Instance.AddScience(node.scienceCost, TransactionReasons.None); //Should maybe do tech research as the reason
                    }
                    finally
                    {
                        KCT_GameStates.isRefunding = valBef;
                    }
                }
                node.DisableTech();
                KCT_GameStates.TechList.RemoveAt(index);
            }
        }

        private static Guid IDSelected = new Guid();
        private static void DrawBLPlusWindow(int windowID)
        {
            //bLPlusPosition.xMax = buildListWindowPosition.xMin;
            //bLPlusPosition.width = 100;
            bLPlusPosition.yMin = buildListWindowPosition.yMin;
            bLPlusPosition.height = 225;
            //bLPlusPosition.height = bLPlusPosition.yMax - bLPlusPosition.yMin;
            KCT_BuildListVessel b = KCT_Utilities.FindBLVesselByID(IDSelected);
            GUILayout.BeginVertical();
            string launchSite = b.launchSite;

            if (launchSite == "LaunchPad")
            {
                if (b.launchSiteID >= 0)
                    launchSite = b.KSC.LaunchPads[b.launchSiteID].name;
                else
                    launchSite = b.KSC.ActiveLPInstance.name;
            }
            KCT_Recon_Rollout rollout = KCT_GameStates.ActiveKSC.GetReconRollout(KCT_Recon_Rollout.RolloutReconType.Rollout, launchSite);
            bool onPad = rollout != null && rollout.IsComplete() && rollout.associatedID == b.id.ToString();
            //This vessel is rolled out onto the pad

            // 1.4 Addition
            if (!onPad && GUILayout.Button("Select LaunchSite"))
            {
                launchSites = KCT_Utilities.GetLaunchSites(b.type == KCT_BuildListVessel.ListType.VAB);

                if (launchSites.Any())
                {
                    showBLPlus = false;
                    showLaunchSiteSelector = true;
                    centralWindowPosition.width = 300;
                }
                else
                {
                    PopupDialog.SpawnPopupDialog(new MultiOptionDialog("KCTNoLaunchsites", "No launch sites available to choose from. Try visiting an editor first.", "No Launch Sites", null, new DialogGUIButton("OK", () => { })), false, HighLogic.UISkin);
                }
            }

            if (!onPad && GUILayout.Button("Scrap"))
            {
                InputLockManager.SetControlLock(ControlTypes.KSC_ALL, "KCTPopupLock");
                DialogGUIBase[] options = new DialogGUIBase[2];
                options[0] = new DialogGUIButton("Yes", ScrapVessel);
                options[1] = new DialogGUIButton("No", DummyVoid);
                MultiOptionDialog diag = new MultiOptionDialog("scrapVesselConfirmPopup", "Are you sure you want to scrap this vessel?", "Scrap Vessel", null, 300, options);
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
                showBLPlus = false;
                ResetBLWindow();
            }
            if (!onPad && GUILayout.Button("Edit"))
            {
                showBLPlus = false;
                editorWindowPosition.height = 1;
                string tempFile = KSPUtil.ApplicationRootPath + "saves/" + HighLogic.SaveFolder + "/Ships/temp.craft";
                b.shipNode.Save(tempFile);
                GamePersistence.SaveGame("persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                KCT_GameStates.editedVessel = b;
                KCT_GameStates.editedVessel.KSC = null;
                KCT_GameStates.EditorShipEditingMode = true;
                KCT_GameStates.mergingAvailable = b.isFinished;

                InputLockManager.SetControlLock(ControlTypes.EDITOR_EXIT, "KCTEditExit");
                InputLockManager.SetControlLock(ControlTypes.EDITOR_LOAD, "KCTEditLoad");
                InputLockManager.SetControlLock(ControlTypes.EDITOR_NEW, "KCTEditNew");
                InputLockManager.SetControlLock(ControlTypes.EDITOR_LAUNCH, "KCTEditLaunch");

                EditorDriver.StartAndLoadVessel(tempFile, b.type == KCT_BuildListVessel.ListType.VAB ? EditorFacility.VAB : EditorFacility.SPH);
            }
            if (GUILayout.Button("Rename"))
            {
                centralWindowPosition.width = 360;
                centralWindowPosition.x = (Screen.width - 360) / 2;
                centralWindowPosition.height = 1;
                showBuildList = false;
                showBLPlus = false;
                showNewPad = false;
                showRename = true;
                newName = b.shipName;
                renamingLaunchPad = false;
                //newDesc = b.getShip().shipDescription;
            }
            if (GUILayout.Button("Duplicate"))
            {
                KCT_Utilities.AddVesselToBuildList(b.NewCopy(true));
            }
            if (KCT_GameStates.ActiveKSC.Recon_Rollout.Find(rr => rr.RRType == KCT_Recon_Rollout.RolloutReconType.Rollout && rr.associatedID == b.id.ToString()) != null && GUILayout.Button("Rollback"))
            {
                KCT_GameStates.ActiveKSC.Recon_Rollout.Find(rr => rr.RRType == KCT_Recon_Rollout.RolloutReconType.Rollout && rr.associatedID == b.id.ToString()).SwapRolloutType();
                showBLPlus = false;
            }
            if (!b.isFinished && GUILayout.Button("Warp To"))
            {
                KCT_GameStates.targetedItem = b;
                KCT_GameStates.canWarp = true;
                KCT_Utilities.RampUpWarp(b);
                KCT_GameStates.warpInitiated = true;
                showBLPlus = false;
            }
            if (!b.isFinished && GUILayout.Button("Move to Top"))
            {
                if (b.type == KCT_BuildListVessel.ListType.VAB)
                {
                    b.RemoveFromBuildList();
                    KCT_GameStates.ActiveKSC.VABList.Insert(0, b);
                }
                else if (b.type == KCT_BuildListVessel.ListType.SPH)
                {
                    b.RemoveFromBuildList();
                    KCT_GameStates.ActiveKSC.SPHList.Insert(0, b);
                }
            }
            if (!b.isFinished
                && (KCT_PresetManager.Instance.ActivePreset.generalSettings.MaxRushClicks == 0 || b.rushBuildClicks < KCT_PresetManager.Instance.ActivePreset.generalSettings.MaxRushClicks)
                && GUILayout.Button("Rush Build 10%\n√" + Math.Round(b.GetRushCost())))
            {
                b.DoRushBuild();
            }
#if false
            if ((b.type == KCT_BuildListVessel.ListType.SPH || b.type == KCT_BuildListVessel.ListType.VAB) &&
                string.IsNullOrEmpty(KCT_PresetManager.Instance.ActivePreset.generalSettings.VABRecoveryTech) &&    // Disabled in RP-1
                GUILayout.Button("Move to " + (b.type == KCT_BuildListVessel.ListType.SPH ? "VAB" : "SPH")))
            {
                if (b.type == KCT_BuildListVessel.ListType.VAB)
                {
                    b.RemoveFromBuildList();
                    b.type = KCT_BuildListVessel.ListType.SPH;
                    //b.ship.shipFacility = EditorFacility.SPH;
                    b.launchSite = "Runway";
                    KCT_GameStates.ActiveKSC.SPHList.Insert(0, b);
                }
                else if (b.type == KCT_BuildListVessel.ListType.SPH)
                {
                    b.RemoveFromBuildList();
                    b.type = KCT_BuildListVessel.ListType.VAB;
                    //b.ship.shipFacility = EditorFacility.VAB;
                    b.launchSite = "LaunchPad";
                    if (b.launchSiteID >= 0)
                        launchSite = b.KSC.LaunchPads[b.launchSiteID].name;
                    else
                        launchSite = b.KSC.ActiveLPInstance.name;
                    KCT_GameStates.ActiveKSC.VABList.Insert(0, b);
                }
            }
#endif
            if (GUILayout.Button("Close"))
            {
                showBLPlus = false;
            }
            GUILayout.EndVertical();
            float width = bLPlusPosition.width;
            bLPlusPosition.x = buildListWindowPosition.x - width;
            bLPlusPosition.width = width;
        }


        private static Vector2 launchSiteScrollView;
        public static void DrawLaunchSiteChooser(int windowID)
        {
            GUILayout.BeginVertical();
            launchSiteScrollView = GUILayout.BeginScrollView(launchSiteScrollView, GUILayout.Height((float)Math.Min(Screen.height * 0.75, 25 * launchSites.Count + 10)));

            foreach (string launchsite in launchSites)
            {
                if (GUILayout.Button(launchsite))
                {
                    //Set the chosen vessel's launch site to the selected site
                    KCT_BuildListVessel blv = KCT_Utilities.FindBLVesselByID(IDSelected);
                    blv.launchSite = launchsite;
                    showLaunchSiteSelector = false;
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            CenterWindow(ref centralWindowPosition);
        }
    }
}
