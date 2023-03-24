using KSP.UI;
using KSP.UI.Screens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace KerbalConstructionTime
{
    /// <summary>
    /// This class attempts to override the KSC Upgrade buttons so that KCT can implement it's own form of KSC upgrading
    /// </summary>
    public class KCT_KSCContextMenuOverrider
    {
        protected static Dictionary<string, Dictionary<int, string>> techGatings = null;

        private KSCFacilityContextMenu _menu = null;

        public KCT_KSCContextMenuOverrider(KSCFacilityContextMenu menu)
        {
            _menu = menu;
        }

        public IEnumerator OnContextMenuSpawn()
        {
            yield return new WaitForFixedUpdate();
            if (KCT_PresetManager.Instance.ActivePreset.generalSettings.KSCUpgradeTimes && _menu != null)
            {
                SpaceCenterBuilding hostBuilding = getMember<SpaceCenterBuilding>("host");
                KCTDebug.Log("Trying to override upgrade button of menu for " + hostBuilding.facilityName);
                Button button = getMember<Button>("UpgradeButton");
                if (button == null)
                {
                    KCTDebug.Log("Could not find UpgradeButton by name, using index instead.", true);
                    button = getMember<UnityEngine.UI.Button>(2);
                }

                if (button != null)
                {
                    KCTDebug.Log("Found upgrade button, overriding it.");
                    button.onClick = new Button.ButtonClickedEvent();    //Clear existing KSP listener
                    button.onClick.AddListener(HandleUpgrade);

                    if (KCT_PresetManager.Instance.ActivePreset.generalSettings.DisableLPUpgrades &&
                        GetFacilityID().ToLower().Contains("launchpad"))
                    {
                        button.interactable = false;
                        var hov = button.gameObject.GetComponent<UIOnHover>();
                        hov.gameObject.DestroyGameObject();

                        _menu.levelStatsText.text = $"<color=red><b>{LocalCache.str_Messages_CanotUpgradeLaunchpadecoverInFlight}</b></color>"; //Launchpads cannot be upgraded. Build a new launchpad from the KCT menu instead. 
                    }
                }
                else
                {
                    throw new Exception("UpgradeButton not found. Cannot override.");
                }
            }
        }

        internal T getMember<T>(string name)
        {
            
            MemberInfo member = _menu.GetType().GetMember(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)?.FirstOrDefault();
            if (member == null)
            {
                KCTDebug.Log($"Member was null when trying to find '{name}'", true);
                return default(T);
            }
            object o = KCT_Utilities.GetMemberInfoValue(member, _menu);
            if (o is T)
            {
                return (T)o;
            }
            return default(T);
        }

        internal T getMember<T>(int index)
        {
            IEnumerable<MemberInfo> memberList = _menu.GetType().GetMembers(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy).Where(m => m.ToString().Contains(typeof(T).ToString()));
            KCTDebug.Log($"Found {memberList.Count()} matches for {typeof(T)}");
            MemberInfo member = memberList.Count() >= index ? memberList.ElementAt(index) : null;
            if (member == null)
            {
                KCTDebug.Log($"Member was null when trying to find element at index {index} for type '{typeof(T).ToString()}'", true);
                return default(T);
            }
            object o = KCT_Utilities.GetMemberInfoValue(member, _menu);
            if (o is T)
            {
                return (T)o;
            }
            return default(T);
        }

        protected static void CheckLoadDict()
        {
            if (techGatings != null)
                return;

            techGatings = new Dictionary<string, Dictionary<int, string>>();
            ConfigNode node = null;
            foreach (ConfigNode n in GameDatabase.Instance.GetConfigNodes("KCTBUILDINGTECHS"))
                node = n;

            if (node == null)
                return;

            foreach (ConfigNode n in node.nodes)
            {
                string fac = "SpaceCenter/" + n.name;
                Dictionary<int, string> lst = new Dictionary<int, string>();

                foreach (ConfigNode.Value v in n.values)
                    lst.Add(int.Parse(v.name), v.value);

                techGatings.Add(fac, lst);
            }
        }

        protected string GetTechGate(string facId, int level)
        {
            CheckLoadDict();
            if (techGatings == null)
                return string.Empty;

            if (techGatings.TryGetValue(facId, out var d))
                if (d.TryGetValue(level, out string node))
                    return node;

            return string.Empty;
        }

        internal void ProcessUpgrade()
        {
            int oldLevel = getMember<int>("level");
            KCTDebug.Log($"Upgrading from level {oldLevel}");

            string facilityID = GetFacilityID();
            SpaceCenterFacility? facilityType = GetFacilityType();

            string gate = GetTechGate(facilityID, oldLevel + 1);
            KCTDebug.Log("[KCTT] Gate for " + facilityID + "? " + gate);
            if (!string.IsNullOrEmpty(gate))
            {
                if (ResearchAndDevelopment.GetTechnologyState(gate) != RDTech.State.Available)
                {
                    PopupDialog.SpawnPopupDialog(new MultiOptionDialog("kctUpgradePadConfirm",
                            LocalCache.str_Messages_UpgradeRequireTechMsg, // "Can't upgrade this facility. Requires " + KerbalConstructionTimeData.techNameToTitle[gate] + "."
                            LocalCache.str_Messages_UpgradeRequireTech_Title, // "Lack Tech to Upgrade"
                            HighLogic.UISkin,
                            new DialogGUIButton("Ok", stub)),
                            false,
                            HighLogic.UISkin);

                    return;
                }
            }

            KCT_UpgradingBuilding upgrading = new KCT_UpgradingBuilding(facilityType, facilityID, oldLevel + 1, oldLevel, facilityID.Split('/').Last());

            upgrading.isLaunchpad = facilityID.ToLower().Contains("launchpad");
            if (upgrading.isLaunchpad)
            {
                upgrading.launchpadID = KCT_GameStates.ActiveKSC.ActiveLaunchPadID;
                if (upgrading.launchpadID > 0)
                    upgrading.commonName += KCT_GameStates.ActiveKSC.ActiveLPInstance.name;
            }

            if (!upgrading.AlreadyInProgress())
            {
                float cost = getMember<float>("upgradeCost");

                if (Funding.CanAfford(cost))
                {
                    KCT_Utilities.SpendFunds(cost, TransactionReasons.Structures);
                    KCT_GameStates.ActiveKSC.KSCTech.Add(upgrading);
                    upgrading.SetBP(cost);
                    upgrading.cost = cost;

                    try
                    {
                        KCT_Events.onFacilityUpgradeQueued?.Fire(upgrading);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    ScreenMessages.PostScreenMessage(LocalCache.str_Messages_UpgraderequestedFacility, 4.0f, ScreenMessageStyle.UPPER_CENTER); // "Facility upgrade requested!"
                    KCTDebug.Log($"Facility {facilityID} upgrade requested to lvl {oldLevel + 1} for {cost} funds, resulting in a BP of {upgrading.BP}");
                }
                else
                {
                    KCTDebug.Log("Couldn't afford to upgrade.");
                    ScreenMessages.PostScreenMessage(LocalCache.str_Messages_NoFundsUpgradeFacility, 4.0f, ScreenMessageStyle.UPPER_CENTER); // "Not enough funds to upgrade facility!"
                }
            }
            else if (oldLevel + 1 != upgrading.currentLevel)
            {
                ScreenMessages.PostScreenMessage(LocalCache.str_Messages_FacilityAlreadyUpgraded, 4.0f, ScreenMessageStyle.UPPER_CENTER); // "Facility is already being upgraded!"
                KCTDebug.Log($"Facility {facilityID} tried to upgrade to lvl {oldLevel + 1} but already in list!");
            }
        }

        void stub() { }

        internal void HandleUpgrade()
        {
            if (GetFacilityID().ToLower().Contains("launchpad"))
            {
                PopupDialog.SpawnPopupDialog(new MultiOptionDialog("kctUpgradePadConfirm",
                            LocalCache.str_Messages_LaunchpadUpgradetip, // "Upgrading this launchpad will render it unusable until the upgrade finishes.\n\nAre you sure you want to?"
                            LocalCache.str_Messages_LaunchpadUpgrade_title, // "Upgrade Launchpad?"
                            HighLogic.UISkin,
                            new DialogGUIButton(LocalCache.Btn_Yes, ProcessUpgrade), // "Yes"
                            new DialogGUIButton(LocalCache.Btn_No, stub)), // "No"
                            false,
                            HighLogic.UISkin);
            }
            else
                ProcessUpgrade();

            _menu.Dismiss(KSCFacilityContextMenu.DismissAction.None);
        }

        public string GetFacilityID()
        {
            return getMember<SpaceCenterBuilding>("host").Facility.id;
        }

        public SpaceCenterFacility? GetFacilityType()
        {
            var scb = getMember<SpaceCenterBuilding>("host");
            if (scb is AdministrationFacility) return SpaceCenterFacility.Administration;
            if (scb is AstronautComplexFacility) return SpaceCenterFacility.AstronautComplex;
            if (scb is LaunchSiteFacility && ((LaunchSiteFacility)scb).facilityType == EditorFacility.VAB) return SpaceCenterFacility.LaunchPad;
            if (scb is LaunchSiteFacility && ((LaunchSiteFacility)scb).facilityType == EditorFacility.SPH) return SpaceCenterFacility.Runway;
            if (scb is MissionControlBuilding) return SpaceCenterFacility.MissionControl;
            if (scb is RnDBuilding) return SpaceCenterFacility.ResearchAndDevelopment;
            if (scb is SpacePlaneHangarBuilding) return SpaceCenterFacility.SpaceplaneHangar;
            if (scb is TrackingStationBuilding) return SpaceCenterFacility.TrackingStation;
            if (scb is VehicleAssemblyBuilding) return SpaceCenterFacility.VehicleAssemblyBuilding;

            // Some mods define custom facilities
            return null;
        }
    }
}
