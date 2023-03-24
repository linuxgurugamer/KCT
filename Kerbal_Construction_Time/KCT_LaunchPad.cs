using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;

namespace KerbalConstructionTime
{
    public class KCT_LaunchPad : ConfigNodeStorage
    {
        [Persistent] public int level = 0;
        [Persistent] public string name = "LaunchPad";
        public ConfigNode DestructionNode = new ConfigNode("DestructionState");
        public bool upgradeRepair = false;

        public bool destroyed
        {
            get
            {
                string nodeStr = level == 2 ? "SpaceCenter/LaunchPad/Facility/LaunchPadMedium/ksp_pad_launchPad" : "SpaceCenter/LaunchPad/Facility/building";
                ConfigNode mainNode = DestructionNode.GetNode(nodeStr);
                if (mainNode == null)
                    return false;
                else
                    return !bool.Parse(mainNode.GetValue("intact"));
            }
        }

        public const string LPID = "SpaceCenter/LaunchPad";

        public KCT_LaunchPad(string LPName, int lvl=0)
        {
            name = LPName;
            level = lvl;
        }

        public void Upgrade(int lvl)
        {
            //sets the new level, assumes completely repaired
            level = lvl;

            KCT_GameStates.UpdateLaunchpadDestructionState = true;
            upgradeRepair = true;
        }

        public bool Delete(out string failReason)
        {
            bool found = false;
            foreach (KCT_KSC ksc in KCT_GameStates.KSCs)
            {
                int idx = KCT_GameStates.ActiveKSC.LaunchPads.IndexOf(this);
                if (idx < 0) continue;

                var rr = ksc.Recon_Rollout.FirstOrDefault(r => r.launchPadID == name);
                if (rr != null)
                {
                    failReason = rr.IsComplete() ? LocalCache.str_Dismantlepad_FailedScreenMsg1 : LocalCache.str_Dismantlepad_FailedScreenMsg2; // "a vessel is currently on the pad""pad has ongoing rollout or reconditioning"
                    return false;
                }

                foreach (KCT_BuildListVessel vessel in ksc.VABWarehouse)
                {
                    if (vessel.launchSiteID > idx) vessel.launchSiteID--;
                }
                foreach (KCT_BuildListVessel vessel in ksc.VABList)
                {
                    if (vessel.launchSiteID > idx) vessel.launchSiteID--;
                }
                foreach (KCT_UpgradingBuilding building in ksc.KSCTech)
                {
                    if (building.facilityType == SpaceCenterFacility.LaunchPad && building.launchpadID > idx) building.launchpadID--;
                }

                ksc.LaunchPads.RemoveAt(idx);

                if (ksc == KCT_GameStates.ActiveKSC)
                {
                    ksc.SwitchLaunchPad(0);
                }
            }

            failReason = null;
            return !found;
        }

        public void Rename(string newName)
        {
            //find everything that references this launchpad by name and update the name reference
            foreach (KCT_KSC ksc in KCT_GameStates.KSCs)
            {
                if (ksc.LaunchPads.Contains(this))
                {
                    if (ksc.LaunchPads.Exists(lp => string.Equals(lp.name, newName, StringComparison.OrdinalIgnoreCase)))
                        return; //can't name it something that already is named that

                    foreach (KCT_Recon_Rollout rr in ksc.Recon_Rollout)
                    {
                        if (rr.launchPadID == name)
                        {
                            rr.launchPadID = newName;
                        }
                    }
                    foreach (KCT_UpgradingBuilding up in ksc.KSCTech)
                    {
                        if (up.isLaunchpad && up.launchpadID == ksc.LaunchPads.IndexOf(this))
                        {
                            up.commonName = newName;
                        }
                    }

                    break;
                }
            }
            name = newName;
        }

        public void SetActive()
        {
            try
            {
                KCTDebug.Log("Switching to LaunchPad: "+name+ " lvl: "+level+" destroyed? "+destroyed);
                KCT_GameStates.ActiveKSC.ActiveLaunchPadID = KCT_GameStates.ActiveKSC.LaunchPads.IndexOf(this);

                //set the level to this level
                if (KCT_Utilities.CurrentGameIsCareer())
                {
                    foreach (Upgradeables.UpgradeableFacility facility in GetUpgradeableFacilityReferences())
                    {
                        KCT_Events.allowedToUpgrade = true;
                        facility.SetLevel(level);
                    }
                }

                //set the destroyed state to this destroyed state
                //might need to do this one frame later?
             //   RefreshDesctructibleState();
                KCT_GameStates.UpdateLaunchpadDestructionState = true;
                upgradeRepair = false;
            }
            catch (Exception e)
            {
                KCTDebug.Log("Error while calling SetActive: " + e.Message + e.StackTrace);
            }
        }

        public void SetDestructibleStateFromNode()
        {
            foreach (DestructibleBuilding facility in GetDestructibleFacilityReferences())
            {
                ConfigNode aNode = DestructionNode.GetNode(facility.id);
                if (aNode != null)
                    facility.Load(aNode);
            }
        }

        public void RefreshDestructionNode()
        {
            DestructionNode = new ConfigNode("DestructionState");
            foreach (DestructibleBuilding facility in GetDestructibleFacilityReferences())
            {
                ConfigNode aNode = new ConfigNode(facility.id);
                facility.Save(aNode);
                DestructionNode.AddNode(aNode);
            }
        }

        public void CompletelyRepairNode()
        {
            foreach (ConfigNode node in DestructionNode.GetNodes())
            {
                if (node.HasValue("intact"))
                    node.SetValue("intact", "True");
            }
        }

        public List<Upgradeables.UpgradeableFacility> GetUpgradeableFacilityReferences()
        {
            return ScenarioUpgradeableFacilities.protoUpgradeables[LPID].facilityRefs;
        }

        List<DestructibleBuilding> GetDestructibleFacilityReferences()
        {

            List<DestructibleBuilding> destructibles = new List<DestructibleBuilding>();
            foreach (KeyValuePair<string, ScenarioDestructibles.ProtoDestructible> kvp in ScenarioDestructibles.protoDestructibles)
            {
                if (kvp.Key.Contains("LaunchPad"))
                {
                    destructibles.AddRange(kvp.Value.dBuildingRefs);
                }
            }
            return destructibles;
        }
    }
}
