using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using KSP.UI;
using KSP.UI.Screens;
using UnityEngine;
using ToolbarControl_NS;
using KSP.Localization;

namespace KerbalConstructionTime
{
    public static class KCT_Utilities
    {
        public static Dictionary<String, int> PartListToDict(List<String> list)
        {
            Dictionary<String, int> newInv = new Dictionary<String, int>();
            foreach (String s in list)
            {
                if (newInv.Keys.Contains(s))
                    newInv[s]++;
                else
                    newInv.Add(s, 1);
            }
            return newInv;
        }

        public static Dictionary<String, int> PartListToDictAlternating(List<String> list)
        {
            Dictionary<String, int> newInv = new Dictionary<String, int>();
            int length = list.Count;
            for (int i = 0; i < length; i += 2)
            {
                KCT_Utilities.AddToDict(newInv, list[i], int.Parse(list[i + 1]));
            }
            return newInv;
        }

        public static List<String> PartDictToList(Dictionary<String, int> dict)
        {
            List<String> ret = new List<string>();
            for (int i = 0; i < dict.Count; i++)
            {
                ret.Add(dict.Keys.ElementAt(i));
                ret.Add(dict.Values.ElementAt(i).ToString());
            }
            return ret;
        }

        public static AvailablePart GetAvailablePartByName(string partName)
        {
            return PartLoader.getPartInfoByName(partName);
        }

        /// <summary>
        /// This is actually the cost in BPs which can in turn be used to calculate the ingame time it takes to build the vessel.
        /// </summary>
        /// <param name="parts"></param>
        /// <returns></returns>
        public static double GetBuildTime(List<Part> parts)
        {
            double totalEffectiveCost = GetEffectiveCost(parts);
            return GetBuildTime(totalEffectiveCost);
        }

        public static double GetBuildTime(List<ConfigNode> parts)
        {
            double totalEffectiveCost = GetEffectiveCost(parts);
            return GetBuildTime(totalEffectiveCost);
        }

        public static double GetBuildTime(double totalEffectiveCost)
        {
            var formulaParams = new Dictionary<string, string>()
            {
                { "E", totalEffectiveCost.ToString() },
                { "O", KCT_PresetManager.Instance.ActivePreset.timeSettings.OverallMultiplier.ToString() }
            };
            double finalBP = KCT_MathParsing.GetStandardFormulaValue("BP", formulaParams);
            return finalBP;
        }

        public static double GetEffectiveCost(List<Part> parts)
        {
            //get list of parts that are in the inventory
            IList<Part> inventorySample = ScrapYardWrapper.GetPartsInInventory(parts, ScrapYardWrapper.ComparisonStrength.STRICT) ?? new List<Part>();

            double totalEffectiveCost = 0;

            List<string> globalVariables = new List<string>();

            foreach (Part p in parts)
            {
                string name = p.partInfo.name;
                double effectiveCost = 0;
                double cost = GetPartCosts(p);
                double dryCost = GetPartCosts(p, false);

                double drymass = p.mass;
                double wetmass = p.GetResourceMass() + drymass;

                double PartMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetPartVariable(name);
                bool doRes;
                double ModuleMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetModuleVariable(p.Modules, out doRes);
                double ResourceMultiplier = 1d;
                if (doRes)
                    ResourceMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetResourceVariable(p.Resources);
                KCT_PresetManager.Instance.ActivePreset.partVariables.SetGlobalVariables(globalVariables, p.Modules);

                double InvEff = (inventorySample.Contains(p) ? KCT_PresetManager.Instance.ActivePreset.timeSettings.InventoryEffect : 0);
                int builds = ScrapYardWrapper.GetBuildCount(p);
                int used = ScrapYardWrapper.GetUseCount(p);
                //C=cost, c=dry cost, M=wet mass, m=dry mass, U=part tracker, O=overall multiplier, I=inventory effect (0 if not in inv), B=build effect

                effectiveCost = KCT_MathParsing.GetStandardFormulaValue("EffectivePart",
                    new Dictionary<string, string>()
                    {
                        {"C", cost.ToString()},
                        {"c", dryCost.ToString()},
                        {"M", wetmass.ToString()},
                        {"m", drymass.ToString()},
                        {"U", builds.ToString()},
                        {"u", used.ToString() },
                        {"O", KCT_PresetManager.Instance.ActivePreset.timeSettings.OverallMultiplier.ToString()},
                        {"I", InvEff.ToString()},
                        {"B", KCT_PresetManager.Instance.ActivePreset.timeSettings.BuildEffect.ToString()},
                        {"PV", PartMultiplier.ToString()},
                        {"RV", ResourceMultiplier.ToString()},
                        {"MV", ModuleMultiplier.ToString()}
                    });

                if (InvEff != 0)
                {
                    inventorySample.Remove(p);
                }

                if (effectiveCost < 0) effectiveCost = 0;
                totalEffectiveCost += effectiveCost;
            }

            double globalMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetGlobalVariable(globalVariables);

            return totalEffectiveCost * globalMultiplier;
        }

        public static double GetEffectiveCost(List<ConfigNode> parts)
        {
            //get list of parts that are in the inventory
            IList<ConfigNode> inventorySample = ScrapYardWrapper.GetPartsInInventory(parts, ScrapYardWrapper.ComparisonStrength.STRICT) ?? new List<ConfigNode>();

            double totalEffectiveCost = 0;
            List<string> globalVariables = new List<string>();
            foreach (ConfigNode p in parts)
            {
                string name = PartNameFromNode(p);
                string raw_name = name;
                double effectiveCost = 0;
                double cost;
                float dryCost, fuelCost;
                float dryMass, fuelMass;
                float wetMass;

                ShipConstruction.GetPartCostsAndMass(p, GetAvailablePartByName(name), out dryCost, out fuelCost, out dryMass, out fuelMass);
                cost = dryCost + fuelCost;
                wetMass = dryMass + fuelMass;

                double PartMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetPartVariable(raw_name);
                List<string> moduleNames = new List<string>();
                bool hasResourceCostMult = true;
                foreach (ConfigNode modNode in GetModulesFromPartNode(p))
                {
                    string s = modNode.GetValue("name");
                    if (s == "ModuleTagNoResourceCostMult")
                        hasResourceCostMult = false;
                    moduleNames.Add(s);
                }
                double ModuleMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetModuleVariable(moduleNames);

                double ResourceMultiplier = 1d;
                if (hasResourceCostMult)
                {
                    List<string> resourceNames = new List<string>();
                    foreach (ConfigNode rNode in GetResourcesFromPartNode(p))
                        resourceNames.Add(rNode.GetValue("name"));
                    ResourceMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetResourceVariable(resourceNames);
                }

                KCT_PresetManager.Instance.ActivePreset.partVariables.SetGlobalVariables(globalVariables, moduleNames);

                double InvEff = (inventorySample.Contains(p) ? KCT_PresetManager.Instance.ActivePreset.timeSettings.InventoryEffect : 0);
                int builds = ScrapYardWrapper.GetBuildCount(p);
                int used = ScrapYardWrapper.GetUseCount(p);
                //C=cost, c=dry cost, M=wet mass, m=dry mass, U=part tracker, O=overall multiplier, I=inventory effect (0 if not in inv), B=build effect

                effectiveCost = KCT_MathParsing.GetStandardFormulaValue("EffectivePart",
                    new Dictionary<string, string>()
                    {
                        {"C", cost.ToString()},
                        {"c", dryCost.ToString()},
                        {"M", wetMass.ToString()},
                        {"m", dryMass.ToString()},
                        {"U", builds.ToString()},
                        {"u", used.ToString()},
                        {"O", KCT_PresetManager.Instance.ActivePreset.timeSettings.OverallMultiplier.ToString()},
                        {"I", InvEff.ToString()},
                        {"B", KCT_PresetManager.Instance.ActivePreset.timeSettings.BuildEffect.ToString()},
                        {"PV", PartMultiplier.ToString()},
                        {"RV", ResourceMultiplier.ToString()},
                        {"MV", ModuleMultiplier.ToString()}
                    });

                if (InvEff != 0)
                {
                    inventorySample.Remove(p);
                }

                if (effectiveCost < 0) effectiveCost = 0;
                totalEffectiveCost += effectiveCost;
            }

            double globalMultiplier = KCT_PresetManager.Instance.ActivePreset.partVariables.GetGlobalVariable(globalVariables);

            return totalEffectiveCost * globalMultiplier;
        }

        public static string PartNameFromNode(ConfigNode part)
        {
            string name = part.GetValue("part");
            if (name != null)
                name = name.Split('_')[0];
            else
                name = part.GetValue("name");
            return name;
        }

        public static double GetPartCosts(Part part, bool includeFuel = true)
        {
            double cost = 0;
            cost = part.partInfo.cost + part.GetModuleCosts(part.partInfo.cost);
            foreach (PartResource rsc in part.Resources)
            {
                PartResourceDefinition def = PartResourceLibrary.Instance.GetDefinition(rsc.resourceName);
                if (!includeFuel)
                {
                    cost -= rsc.maxAmount * def.unitCost;
                }
                else //accounts for if you remove some fuel from a tank
                {
                    cost -= (rsc.maxAmount - rsc.amount) * def.unitCost;
                }
            }
            return cost;
        }

        public static ConfigNode[] GetModulesFromPartNode(ConfigNode partNode)
        {
            var n = partNode.GetNodes("MODULE").ToList();
            for (int i = n.Count - 1; i >= 0; i--)
            {
                ConfigNode cn = n[i];

                string s = null;
                var b = cn.TryGetValue("name", ref s);
                if (b == false || s == null || s == "")
                    n.Remove(cn);
            }
            return n.ToArray();
            //return partNode.GetNodes("MODULE");
        }

        public static ConfigNode[] GetResourcesFromPartNode(ConfigNode partNode)
        {
            return partNode.GetNodes("RESOURCE");
        }

        public static double GetBuildRate(int index, KCT_BuildListVessel.ListType type, KCT_KSC KSC, bool UpgradedRate = false)
        {
            return GetBuildRate(index, type, KSC, UpgradedRate ? 1 : 0);
        }

        public static double GetBuildRate(int index, KCT_BuildListVessel.ListType type, KCT_KSC KSC, int upgradeDelta)
        {
            if (KSC == null) KSC = KCT_GameStates.ActiveKSC;
            double ret = 0;
            if (type == KCT_BuildListVessel.ListType.VAB)
            {
                if (upgradeDelta == 0 && KSC.VABRates.Count > index)
                {
                    return KSC.VABRates[index];
                }
                else if (upgradeDelta == 1 && KSC.UpVABRates.Count > index)
                {
                    return KSC.UpVABRates[index];
                }
                else if (upgradeDelta > 1)
                {
                    return KCT_MathParsing.ParseBuildRateFormula(KCT_BuildListVessel.ListType.VAB, index, KSC, upgradeDelta);
                }
                else
                {
                    return 0;
                }
            }
            else if (type == KCT_BuildListVessel.ListType.SPH)
            {
                if (upgradeDelta == 0 && KSC.SPHRates.Count > index)
                {
                    return KSC.SPHRates[index];
                }
                else if (upgradeDelta == 1 && KSC.UpSPHRates.Count > index)
                {
                    return KSC.UpSPHRates[index];
                }
                else if (upgradeDelta > 1)
                {
                    return KCT_MathParsing.ParseBuildRateFormula(KCT_BuildListVessel.ListType.SPH, index, KSC, upgradeDelta);
                }
                else
                {
                    return 0;
                }
            }
            else if (type == KCT_BuildListVessel.ListType.TechNode)
            {
                ret = KCT_GameStates.TechList[index].BuildRate;
            }
            return ret;
        }

        public static double GetBuildRate(KCT_BuildListVessel ship)
        {
            if (ship.type == KCT_BuildListVessel.ListType.None)
                ship.FindTypeFromLists();

            if (ship.type == KCT_BuildListVessel.ListType.VAB)
                return GetBuildRate(ship.KSC.VABList.IndexOf(ship), ship.type, ship.KSC);
            else if (ship.type == KCT_BuildListVessel.ListType.SPH)
                return GetBuildRate(ship.KSC.SPHList.IndexOf(ship), ship.type, ship.KSC);
            else
                return 0;
        }

        public static List<double> BuildRatesVAB(KCT_KSC KSC)
        {
            if (KSC == null) KSC = KCT_GameStates.ActiveKSC;
            return KSC.VABRates;
        }

        public static List<double> BuildRatesSPH(KCT_KSC KSC)
        {
            if (KSC == null) KSC = KCT_GameStates.ActiveKSC;
            return KSC.SPHRates;
        }

        public static double GetVABBuildRateSum(KCT_KSC KSC)
        {
            double rateTotal = 0;
            List<double> rates = BuildRatesVAB(KSC);
            for (int i = 0; i < rates.Count; i++)
            {
                double rate = rates[i];
                rateTotal += rate;
            }
            return rateTotal;
        }

        public static double GetSPHBuildRateSum(KCT_KSC KSC)
        {
            double rateTotal = 0;
            List<double> rates = BuildRatesSPH(KSC);
            for (int i = 0; i < rates.Count; i++)
            {
                double rate = rates[i];
                rateTotal += rate;
            }
            return rateTotal;
        }

        public static double GetBothBuildRateSum(KCT_KSC KSC)
        {
            double rateTotal = GetSPHBuildRateSum(KSC);
            rateTotal += GetVABBuildRateSum(KSC);

            return rateTotal;
        }

        public static void ProgressBuildTime()
        {
            double UT = 0;
            if (HighLogic.LoadedSceneIsEditor) //support for EditorTime
                UT = HighLogic.CurrentGame.flightState.universalTime;
            else
                UT = Planetarium.GetUniversalTime();
            if (KCT_GameStates.lastUT == 0)
                KCT_GameStates.lastUT = UT;
            double UTDiff = UT - KCT_GameStates.lastUT;
            if (UTDiff > 0 && (HighLogic.LoadedSceneIsEditor || UTDiff < (TimeWarp.fetch.warpRates[TimeWarp.fetch.warpRates.Length - 1] * 2)))
            {
                foreach (KCT_KSC ksc in KCT_GameStates.KSCs)
                {
                    for (int i = 0; i < ksc.VABList.Count; i++)
                    {
                        ksc.VABList[i].IncrementProgress(UTDiff);
                    }
                    for (int i = 0; i < ksc.SPHList.Count; i++)
                    {
                        ksc.SPHList[i].IncrementProgress(UTDiff);
                    }

                    foreach (KCT_Recon_Rollout rr in ksc.Recon_Rollout)
                    {
                        rr.IncrementProgress(UTDiff);
                    }
                    //Reset the associated launchpad id when rollback completes
                    ksc.Recon_Rollout.ForEach(delegate(KCT_Recon_Rollout rr)
                    {
                        if (rr.RRType == KCT_Recon_Rollout.RolloutReconType.Rollback && rr.IsComplete())
                        {
                            KCT_BuildListVessel blv = KCT_Utilities.FindBLVesselByID(new Guid(rr.associatedID));
                            if (blv != null)
                                blv.launchSiteID = -1;
                        }
                    });
                    ksc.Recon_Rollout.RemoveAll(rr => !KCT_PresetManager.Instance.ActivePreset.generalSettings.ReconditioningTimes || (rr.RRType != KCT_Recon_Rollout.RolloutReconType.Rollout && rr.IsComplete()));

                    foreach (KCT_AirlaunchPrep ap in ksc.AirlaunchPrep)
                    {
                        ap.IncrementProgress(UTDiff);
                    }
                    ksc.AirlaunchPrep.RemoveAll(ap => ap.Direction != KCT_AirlaunchPrep.PrepDirection.Mount && ap.IsComplete());

                    foreach (KCT_UpgradingBuilding kscTech in ksc.KSCTech)
                    {
                        kscTech.IncrementProgress(UTDiff);
                    }
                    if (HighLogic.LoadedScene == GameScenes.SPACECENTER) ksc.KSCTech.RemoveAll(ub => ub.UpgradeProcessed);

                }
                for (int i = 0; i < KCT_GameStates.TechList.Count; i++)
                {
                    KCT_TechItem tech = KCT_GameStates.TechList[i];
                    tech.IncrementProgress(UTDiff);
                }
            }
            if (KCT_GameStates.targetedItem != null && KCT_GameStates.targetedItem.IsComplete())
            {
                TimeWarp.SetRate(0, true);
                KCT_GameStates.targetedItem = null;
                KCT_GameStates.warpInitiated = false;
            }
            KCT_GameStates.lastUT = UT;

        }

        public static float GetTotalVesselCost(ProtoVessel vessel, bool includeFuel = true)
        {
            float total = 0, totalDry = 0;
            foreach (ProtoPartSnapshot part in vessel.protoPartSnapshots)
            {
                float dry, wet;
                total += ShipConstruction.GetPartCosts(part, part.partInfo, out dry, out wet);
                totalDry += dry;
            }
            if (includeFuel)
                return total;
            else
                return totalDry;
        }

        public static float GetTotalVesselCost(ConfigNode vessel, bool includeFuel = true)
        {
            float total = 0;
            foreach (ConfigNode part in vessel.GetNodes("PART"))
            {
                total += GetPartCostFromNode(part, includeFuel);
            }
            return total;
        }

        public static float GetPartCostFromNode(ConfigNode part, bool includeFuel = true)
        {
            string name = PartNameFromNode(part);
            AvailablePart aPart = GetAvailablePartByName(name);
            if (aPart == null)
                return 0;
            float dryCost, fuelCost;
            float dryMass, fuelMass;
            ShipConstruction.GetPartCostsAndMass(part, aPart, out dryCost, out fuelCost, out dryMass, out fuelMass);
            //float total = ShipConstruction.GetPartCosts(part, aPart, out dry, out wet);
            
            if (includeFuel)
                return dryCost+fuelCost;
            else
                return dryCost;
        }

        public static float GetPartMassFromNode(ConfigNode part, bool includeFuel = true, bool includeClamps = true)
        {
            AvailablePart aPart = GetAvailablePartByName(PartNameFromNode(part));

            if (aPart == null || (!includeClamps && aPart.partPrefab != null && aPart.partPrefab.Modules.Contains<LaunchClamp>()))
                return 0;
            //total = ShipConstruction.GetPartTotalMass(part, aPart, out dry, out wet);
            float dryCost, fuelCost;
            float dryMass, fuelMass;
            ShipConstruction.GetPartCostsAndMass(part, aPart, out dryCost, out fuelCost, out dryMass, out fuelMass);
            if (includeFuel)
                return dryMass+fuelMass;
            else
                return dryMass;
        }

        public static float GetShipMass(this ShipConstruct sc, bool excludeClamps, out float dryMass, out float fuelMass)
        {
            dryMass = 0f;
            fuelMass = 0f;
            int partCount = sc.parts.Count;
            while (partCount-- > 0)
            {
                Part part = sc.parts[partCount];
                AvailablePart partInfo = part.partInfo;

                if (excludeClamps && part.partInfo.partPrefab.Modules.Contains<LaunchClamp>())
                    continue;

                float partDryMass = partInfo.partPrefab.mass + part.GetModuleMass(partInfo.partPrefab.mass, ModifierStagingSituation.CURRENT);
                float partFuelMass = 0.0f;
                int resCount = part.Resources.Count;
                while (resCount-- > 0)
                {
                    PartResource resource = part.Resources[resCount];
                    PartResourceDefinition info = resource.info;
                    partFuelMass += info.density * (float)resource.amount;
                }
                dryMass += partDryMass;
                fuelMass += partFuelMass;
            }
            return dryMass + fuelMass;
        }

        public static string GetTweakScaleSize(ProtoPartSnapshot part)
        {
            string partSize = "";
            if (part.modules != null)
            {
                ProtoPartModuleSnapshot tweakscale = part.modules.Find(mod => mod.moduleName == "TweakScale");
                if (tweakscale != null)
                {
                    ConfigNode tsCN = tweakscale.moduleValues;
                    string defaultScale = tsCN.GetValue("defaultScale");
                    string currentScale = tsCN.GetValue("currentScale");
                    if (!defaultScale.Equals(currentScale))
                        partSize = "," + currentScale;
                }
            }
            return partSize;
        }

        public static string GetTweakScaleSize(ConfigNode part)
        {
            string partSize = "";
            if (part.HasNode("MODULE"))
            {
                ConfigNode[] Modules = part.GetNodes("MODULE");
                if (Modules.Length > 0 && Modules.FirstOrDefault(mod => mod.GetValue("name") == "TweakScale") != null)
                {
                    ConfigNode tsCN = Modules.First(mod => mod.GetValue("name") == "TweakScale");
                    string defaultScale = tsCN.GetValue("defaultScale");
                    string currentScale = tsCN.GetValue("currentScale");
                    if (!defaultScale.Equals(currentScale))
                        partSize = "," + currentScale;
                }
            }
            return partSize;
        }

        public static string GetTweakScaleSize(Part part)
        {
            string partSize = "";
            if (part.Modules != null && part.Modules.Contains("TweakScale"))
            {
                PartModule tweakscale = part.Modules["TweakScale"];
                //ConfigNode tsCN = tweakscale.snapshot.moduleValues;

                object defaultScale = tweakscale.Fields.GetValue("defaultScale");//tsCN.GetValue("defaultScale");
                object currentScale = tweakscale.Fields.GetValue("currentScale");//tsCN.GetValue("currentScale");
                if (!defaultScale.Equals(currentScale))
                    partSize = "," + currentScale.ToString();
            }
            return partSize;
        }

        /*
         * Tests to see if two ConfigNodes have the same information. Currently requires same ordering of subnodes
         * */
        public static Boolean ConfigNodesAreEquivalent(ConfigNode node1, ConfigNode node2)
        {
            //Check that the number of subnodes are equal
            if (node1.GetNodes().Length != node2.GetNodes().Length)
                return false;
            //Check that all the values are identical
            foreach (string valueName in node1.values.DistinctNames())
            {
                if (!node2.HasValue(valueName))
                    return false;
                if (node1.GetValue(valueName) != node2.GetValue(valueName))
                    return false;
            }

            //Check all subnodes for equality
            for (int index = 0; index < node1.GetNodes().Length; ++index)
            {
                if (!ConfigNodesAreEquivalent(node1.nodes[index], node2.nodes[index]))
                    return false;
            }

            //If all these tests pass, we consider the nodes to be equivalent
            return true;
        }

        private static DateTime startedFlashing;

        static string KCT_OFF_38_str = "KerbalConstructionTime/PluginData/Icons/KCT_off-38";
        static string KCT_On_38_str = "KerbalConstructionTime/PluginData/Icons/KCT_on-38";
 

        public static string GetStockButtonTexturePath()
        {

            if (KCT_Events.instance.KCTButtonStockImportant && (DateTime.Now.CompareTo(startedFlashing.AddSeconds(0))) > 0 && DateTime.Now.Millisecond < 500)
                return KCT_OFF_38_str;
            else if (KCT_Events.instance.KCTButtonStockImportant && (DateTime.Now.CompareTo(startedFlashing.AddSeconds(3))) > 0)
            {
                KCT_Events.instance.KCTButtonStockImportant = false;
                return KCT_On_38_str;
            }
            //The normal icon
            else
                return KCT_On_38_str;
        }


        public static String GetButtonTexturePath()
        {
            String textureReturn;
           
            if (!KCT_PresetManager.Instance.ActivePreset.generalSettings.Enabled)
                return "KerbalConstructionTime/PluginData/Icons/KCT_off-24";

            // replace KCT_GameStates.kctToolbarButton.Important with KCT_Events.instance.KCTButtonStockImportant
            
            //Flash for up to 3 seconds, at half second intervals per icon
            if (KCT_Events.instance.KCTButtonStockImportant && (DateTime.Now.CompareTo(startedFlashing.AddSeconds(3))) < 0 && DateTime.Now.Millisecond < 500)
                textureReturn = "KerbalConstructionTime/PluginData/Icons/KCT_off";
            //If it's been longer than 3 seconds, set Important to false and stop flashing
            else if (KCT_Events.instance.KCTButtonStockImportant && (DateTime.Now.CompareTo(startedFlashing.AddSeconds(3))) > 0)
            {
                KCT_Events.instance.KCTButtonStockImportant = false;
                textureReturn = "KerbalConstructionTime/PluginData/Icons/KCT_on";
            }
            //The normal icon
            else
                textureReturn = "KerbalConstructionTime/PluginData/Icons/KCT_on";
            
            return textureReturn + "-24";
        }

        public static bool CurrentGameHasScience()
        {
            return HighLogic.CurrentGame.Mode == Game.Modes.CAREER || HighLogic.CurrentGame.Mode == Game.Modes.SCIENCE_SANDBOX;
        }
        public static bool CurrentGameIsSandbox()
        {
            return HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX;
        }
        public static bool CurrentGameIsCareer()
        {
            return HighLogic.CurrentGame.Mode == Game.Modes.CAREER;
        }

        public static bool CurrentGameIsMission()
        {
            return HighLogic.CurrentGame.Mode == Game.Modes.MISSION || HighLogic.CurrentGame.Mode == Game.Modes.MISSION_BUILDER;
        }

        public static string AddScienceWithMessage(float science, TransactionReasons reason)
        {
            if (science > 0)
            {
                //ResearchAndDevelopment.Instance.Science += science;
                ResearchAndDevelopment.Instance.AddScience(science, reason);
                ScienceSubject subject = new ScienceSubject("KCT_Construction", "Kerbal Construction Research", 1.0f, 1.0f, 9999999f);
                GameEvents.OnScienceRecieved.Fire(science, subject, null, false);
                var message = new ScreenMessage(Localizer.Format("#KCT_Messages_AddScience", science), 4.0f, ScreenMessageStyle.UPPER_LEFT); // "[KCT] " + science + " science added."
                ScreenMessages.PostScreenMessage(message);
                return message.ToString();
            }
            return "";
        }

        public static void MoveVesselToWarehouse(KCT_BuildListVessel ship)
        {
            if (ship.type == KCT_BuildListVessel.ListType.None)
                ship.FindTypeFromLists();

            if (ship.type == KCT_BuildListVessel.ListType.VAB)
                MoveVesselToWarehouse(ship.type, ship.KSC.VABList.IndexOf(ship), ship.KSC);
            else if (ship.type == KCT_BuildListVessel.ListType.SPH)
                MoveVesselToWarehouse(ship.type, ship.KSC.SPHList.IndexOf(ship), ship.KSC);
        }

        public static void MoveVesselToWarehouse(KCT_BuildListVessel.ListType ListIdentifier, int index, KCT_KSC KSC)
        {
            if (KSC == null) KSC = KCT_GameStates.ActiveKSC;

            KCT_Events.instance.KCTButtonStockImportant = true;
            startedFlashing = DateTime.Now; //Set the time to start flashing

            StringBuilder Message = new StringBuilder();
            Message.AppendLine(LocalCache.str_Messages_CompleteStart); // "The following vessel is complete:"
            KCT_BuildListVessel vessel = null;
            if (ListIdentifier == KCT_BuildListVessel.ListType.VAB)
            {
                vessel = KSC.VABList[index];
                KSC.VABList.RemoveAt(index);
                KSC.VABWarehouse.Add(vessel);
                
                Message.AppendLine(vessel.shipName);
                Message.AppendLine(Localizer.Format("#KCT_Messages_Complete_VABcheck", KSC.KSCName)); // "Please check the VAB Storage at "+KSC.KSCName+" to launch it."
            
            }
            else if (ListIdentifier == KCT_BuildListVessel.ListType.SPH)
            {
                vessel = KSC.SPHList[index];
                KSC.SPHList.RemoveAt(index);
                KSC.SPHWarehouse.Add(vessel);

                Message.AppendLine(vessel.shipName);
                Message.AppendLine(Localizer.Format("#KCT_Messages_Complete_SPHcheck", KSC.KSCName )); // "Please check the SPH Storage at " + KSC.KSCName + " to launch it."
            }

            if ((KCT_GameStates.settings.ForceStopWarp || vessel == KCT_GameStates.targetedItem) && TimeWarp.CurrentRateIndex != 0)
            {
                TimeWarp.SetRate(0, true);
                KCT_GameStates.warpInitiated = false;
            }

            //Assign science based on science rate
            if (CurrentGameHasScience() && !vessel.cannotEarnScience)
            {
                double rate = KCT_MathParsing.GetStandardFormulaValue("Research", new Dictionary<string, string>() { { "N", KSC.RDUpgrades[0].ToString() }, { "R", KCT_Utilities.BuildingUpgradeLevel(SpaceCenterFacility.ResearchAndDevelopment).ToString() } });
                if (rate > 0)
                {
                    Message.AppendLine(AddScienceWithMessage((float)(rate * vessel.buildPoints), TransactionReasons.None));
                }
            }

            //Add parts to the tracker
            if (!vessel.cannotEarnScience) //if the vessel was previously completed, then we shouldn't register it as a new build
            {
                ScrapYardWrapper.RecordBuild(vessel.ExtractedPartNodes);
            }

            string stor = ListIdentifier == 0 ? "VAB" : "SPH";
            KCTDebug.Log("Moved vessel " + vessel.shipName + " to " +KSC.KSCName + "'s " + stor + " storage.");


            //TODO: Can't allow recalculations since the inventory doesn't work that way as of right now
            //foreach (KCT_KSC KSC_iterator in KCT_GameStates.KSCs)
            //{
            //    foreach (KCT_BuildListVessel blv in KSC_iterator.VABList)
            //    {
            //        double newTime = KCT_Utilities.GetBuildTime(blv.ExtractedPartNodes, true, blv.InventoryParts); //Use only the parts that were originally used when recalculating
            //        if (newTime < blv.buildPoints)
            //        {
            //            blv.buildPoints = blv.buildPoints - ((blv.buildPoints - newTime) * (100 - blv.ProgressPercent()) / 100.0); //If progress=0% then set to new build time, 100%=no change, 50%=half of difference.
            //        }
            //    }
            //    foreach (KCT_BuildListVessel blv in KSC_iterator.SPHList)
            //    {
            //        double newTime = KCT_Utilities.GetBuildTime(blv.ExtractedPartNodes, true, blv.InventoryParts);
            //        if (newTime < blv.buildPoints)
            //        {
            //            blv.buildPoints = blv.buildPoints - ((blv.buildPoints - newTime) * (100 - blv.ProgressPercent()) / 100.0); //If progress=0% then set to new build time, 100%=no change, 50%=half of difference.
            //        }
            //    }
            //}
            KCT_GUI.ResetBLWindow(false);
            if (!KCT_GameStates.settings.DisableAllMessages)
            {
                DisplayMessage(LocalCache.str_Messages_CompleteTitle, Message, MessageSystemButton.MessageButtonColor.GREEN, MessageSystemButton.ButtonIcons.COMPLETE); // "Vessel Complete!"
            }
        }

        public static double SpendFunds(double toSpend, TransactionReasons reason)
        {
            if (!CurrentGameIsCareer())
                return 0;
            KCTDebug.Log($"Removing funds: {toSpend}, New total: {Funding.Instance.Funds - toSpend}");
            if (toSpend < Funding.Instance.Funds)
                Funding.Instance.AddFunds(-toSpend, reason);
            return Funding.Instance.Funds;
        }

        public static double AddFunds(double toAdd, TransactionReasons reason)
        {
            if (!CurrentGameIsCareer())
                return 0;
            KCTDebug.Log($"Adding funds: {toAdd}, New total: {Funding.Instance.Funds + toAdd}");
            Funding.Instance.AddFunds(toAdd, reason);
            return Funding.Instance.Funds;
        }

        public static void ProcessSciPointTotalChange(float changeDelta)
        {
            // Earned point totals shouldn't decrease. This would only make sense when done through the cheat menu.
            if (changeDelta <= 0f || KCT_GameStates.isRefunding) return;

            bool addSavePts = KCT_GameStates.SciPointsTotal == -1f;
            EnsureCurrentSaveHasSciTotalsInitialized(changeDelta);

            float pointsBef;
            if (addSavePts)
                pointsBef = 0f;
            else
                pointsBef = KCT_GameStates.SciPointsTotal;

            KCT_GameStates.SciPointsTotal += changeDelta;
            KCTDebug.Log("Total sci points earned is now: " + KCT_GameStates.SciPointsTotal);

            double upgradesBef = KCT_MathParsing.GetStandardFormulaValue("UpgradesForScience", new Dictionary<string, string>() { { "N", pointsBef.ToString() } });
            double upgradesAft = KCT_MathParsing.GetStandardFormulaValue("UpgradesForScience", new Dictionary<string, string>() { { "N", KCT_GameStates.SciPointsTotal.ToString() } });
            KCTDebug.Log($"Upg points bef: {upgradesBef}; aft: {upgradesAft}");

            int upgradesToAdd = (int)upgradesAft - (int)upgradesBef;
            if (upgradesToAdd > 0)
            {
                // now done in TotalUpgradePoints
                //KCT_GameStates.PurchasedUpgrades[1] += upgradesToAdd;
                KCTDebug.Log($"Added {upgradesToAdd} upgrade points");
                ScreenMessages.PostScreenMessage(Localizer.Format("#KCT_Messages_AddUpgradePoint", upgradesToAdd), 8.0f, ScreenMessageStyle.UPPER_LEFT); //$"{upgradesToAdd} KCT Upgrade Point{(upgradesToAdd > 1 ? "s" : string.Empty)} Added!" 
            }
        }

        public static void EnsureCurrentSaveHasSciTotalsInitialized(float changeDelta)
        {
            if (KCT_GameStates.SciPointsTotal == -1f)
            {
                KCTDebug.Log("Trying to determine total science points for current save...");

                float totalSci = 0f;
                foreach (KCT_TechItem t in KCT_GameStates.TechList)
                {
                    KCTDebug.Log($"Found tech in KCT list: {t.protoNode.techID} | {t.protoNode.state} | {t.protoNode.scienceCost}");
                    if (t.protoNode.state == RDTech.State.Available) continue;

                    totalSci += t.protoNode.scienceCost;
                }

                var techIDs = KerbalConstructionTimeData.techNameToTitle.Keys;
                foreach (var techId in techIDs)
                {
                    var ptn = ResearchAndDevelopment.Instance.GetTechState(techId);
                    if (ptn == null)
                    {
                        KCTDebug.Log($"Failed to find tech with id {techId}");
                        continue;
                    }

                    KCTDebug.Log($"Found tech {ptn.techID} | {ptn.state} | {ptn.scienceCost}");
                    if (ptn.techID == "unlockParts") continue;    // This node in RP-1 is unlocked automatically but has a high science cost
                    if (ptn.state != RDTech.State.Available) continue;

                    totalSci += ptn.scienceCost;
                }

                totalSci += ResearchAndDevelopment.Instance.Science - changeDelta;

                KCTDebug.Log("Calculated total: " + totalSci);
                KCT_GameStates.SciPointsTotal = totalSci;
            }
        }

        public static KCT_BuildListVessel AddVesselToBuildList()
        {
            return AddVesselToBuildList(EditorLogic.fetch.launchSiteName);
        }

        public static KCT_BuildListVessel AddVesselToBuildList(string launchSite)
        {
            if (string.IsNullOrEmpty(launchSite))
            {
                launchSite = EditorLogic.fetch.launchSiteName;
            }

            double effCost = GetEffectiveCost(EditorLogic.fetch.ship.Parts);
            double bp = GetBuildTime(effCost);
            KCT_BuildListVessel blv = new KCT_BuildListVessel(EditorLogic.fetch.ship, launchSite, effCost, bp, EditorLogic.FlagURL);
            blv.shipName = EditorLogic.fetch.shipNameField.text;
     
            return AddVesselToBuildList(blv);
        }

        public static KCT_BuildListVessel AddVesselToBuildList(KCT_BuildListVessel blv)
        {
            if (SpaceTuxUtility.HasMod.hasMod("EngineDecay"))
            {
                KCTDebug.Log("Setting isKCTBuilt flags as P2P mod is present");
                KCT_Utilities.SetIsKCTBuiltFlags(blv.shipNode);
            }

            if (CurrentGameIsCareer())
            {
                //Check upgrades
                //First, mass limit
                List<string> facilityChecks = blv.MeetsFacilityRequirements(true);
                if (facilityChecks.Count != 0)
                {
                    PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "editorChecksFailedPopup", 
                        LocalCache.str_Messages_FailedEditorChecksTitle, // "Failed editor checks!"
                        LocalCache.str_Messages_FailedEditorChecksMsg + string.Join("\n", facilityChecks.Select(s => $"• {s}").ToArray()), // "Warning! This vessel did not pass the editor checks! It will still be built, but you will not be able to launch it without upgrading. Listed below are the failed checks:\n"
                        LocalCache.Btn_Acknowledged, false, HighLogic.UISkin);// "Acknowledged"
                }


                double totalCost = blv.GetTotalCost();
                double prevFunds = Funding.Instance.Funds;
                if (totalCost > prevFunds)
                {
                    KCTDebug.Log("Tried to add " + blv.shipName + " to build list but not enough funds.");
                    KCTDebug.Log("Vessel cost: " + GetTotalVesselCost(blv.shipNode) + ", Current funds: " + prevFunds);
                    var msg = new ScreenMessage(LocalCache.str_Messages_NotEnoughFundsBuild, 4.0f, ScreenMessageStyle.UPPER_CENTER); // "Not Enough Funds To Build!"
                    ScreenMessages.PostScreenMessage(msg);
                    return null;
                }
                else
                {
                    SpendFunds(totalCost, TransactionReasons.VesselRollout);
                }
            }

            string type = "";
            if (blv.type == KCT_BuildListVessel.ListType.VAB)
            {
                blv.launchSite = "LaunchPad";
                KCT_GameStates.ActiveKSC.VABList.Add(blv);
                type = LocalCache.str_locTxtVAB; // "VAB"
            }
            else if (blv.type == KCT_BuildListVessel.ListType.SPH)
            {
                blv.launchSite = "Runway";
                KCT_GameStates.ActiveKSC.SPHList.Add(blv);
                type = LocalCache.str_locTxtSPH; // "SPH"
            }

            ScrapYardWrapper.ProcessVessel(blv.ExtractedPartNodes);

            KCTDebug.Log($"Added {blv.shipName} to {type} build list at KSC {KCT_GameStates.ActiveKSC.KSCName}. Cost: {blv.cost}. IntegrationCost: {blv.integrationCost}");
            KCTDebug.Log("Launch site is " + blv.launchSite);
            //KCTDebug.Log("Cost Breakdown (total, parts, fuel): " + blv.totalCost + ", " + blv.dryCost + ", " + blv.fuelCost);
            var message = new ScreenMessage(Localizer.Format("#KCT_Messages_AddToBuildList", blv.shipName, type), 4.0f, ScreenMessageStyle.UPPER_CENTER); // $"[KCT] Added {blv.shipName} to {type} build list."
            ScreenMessages.PostScreenMessage(message);
            return blv;
        }

        public static IKCTBuildItem NextThingToFinish()
        {
            IKCTBuildItem thing = null;
            if (KCT_GameStates.ActiveKSC == null)
                return null;
            double shortestTime = double.PositiveInfinity;
            foreach (KCT_KSC KSC in KCT_GameStates.KSCs)
            {
                foreach (IKCTBuildItem blv in KSC.VABList)
                {
                    double time = blv.GetTimeLeft();
                    if (time < shortestTime)
                    {
                        thing = blv;
                        shortestTime = time;
                    }
                }
                foreach (IKCTBuildItem blv in KSC.SPHList)
                {
                    double time = blv.GetTimeLeft();
                    if (time < shortestTime)
                    {
                        thing = blv;
                        shortestTime = time;
                    }
                }
                
                foreach (IKCTBuildItem rr in KSC.Recon_Rollout)
                {
                    if (rr.IsComplete())
                        continue;
                    double time = rr.GetTimeLeft();
                    if (time < shortestTime)
                    {
                        thing = rr;
                        shortestTime = time;
                    }
                }

                foreach (IKCTBuildItem ap in KSC.AirlaunchPrep)
                {
                    if (ap.IsComplete())
                        continue;
                    double time = ap.GetTimeLeft();
                    if (time < shortestTime)
                    {
                        thing = ap;
                        shortestTime = time;
                    }
                }

                foreach (IKCTBuildItem ub in KSC.KSCTech)
                {
                    if (ub.IsComplete())
                        continue;
                    double time = ub.GetTimeLeft();
                    if (time < shortestTime)
                    {
                        thing = ub;
                        shortestTime = time;
                    }
                }
            }
            foreach (KCT_TechItem tech in KCT_GameStates.TechList)
            {
                // Ignore items that are blocked
                if (tech.GetBlockingTech(KCT_GameStates.TechList) == null)
                {
                    double time = ((IKCTBuildItem)tech).GetTimeLeft();
                    if (time < shortestTime)
                    {
                        thing = tech;
                        shortestTime = time;
                    }
                }
            }
            return thing;
        }

        public static void RampUpWarp()
        {
            //KCT_BuildListVessel ship = KCT_Utilities.NextShipToFinish();
            IKCTBuildItem ship = KCT_Utilities.NextThingToFinish();
            RampUpWarp(ship);
        }

        public static void RampUpWarp(IKCTBuildItem item)
        {
            int newRate = TimeWarp.CurrentRateIndex;
            double timeLeft = item.GetTimeLeft();
            if (double.IsPositiveInfinity(timeLeft))
                timeLeft = KCT_Utilities.NextThingToFinish().GetTimeLeft();
            while ((newRate + 1 < TimeWarp.fetch.warpRates.Length) && (timeLeft > TimeWarp.fetch.warpRates[newRate + 1]*Planetarium.fetch.fixedDeltaTime) && (newRate < KCT_GameStates.settings.MaxTimeWarp))
            {
                newRate++;
            }
            TimeWarp.SetRate(newRate, true);
          //  Debug.Log("Fixed Delta Time: " + Planetarium.fetch.fixedDeltaTime);
        }

        public static void DisableModFunctionality()
        {
            InputLockManager.RemoveControlLock("KCTLaunchLock");
            KCT_GUI.hideAll();
        }


        public static object GetMemberInfoValue(System.Reflection.MemberInfo member, object sourceObject)
        {
            object newVal;
            if (member is System.Reflection.FieldInfo)
                newVal = ((System.Reflection.FieldInfo)member).GetValue(sourceObject);
            else
                newVal = ((System.Reflection.PropertyInfo)member).GetValue(sourceObject, null);
            return newVal;
        }

        public static int TotalSpentUpgrades(KCT_KSC ksc = null)
        {
            if (ksc == null) ksc = KCT_GameStates.ActiveKSC;
            int spentPoints = 0;
            if (KCT_PresetManager.Instance.ActivePreset.generalSettings.SharedUpgradePool)
            {
                for (int j = 0; j < KCT_GameStates.KSCs.Count; j++)
                {
                    KCT_KSC KSC = KCT_GameStates.KSCs[j];
                    for (int i = 0; i < KSC.VABUpgrades.Count; i++) spentPoints += KSC.VABUpgrades[i];
                    for (int i = 0; i < KSC.SPHUpgrades.Count; i++) spentPoints += KSC.SPHUpgrades[i];
                    spentPoints += KSC.RDUpgrades[0];
                }
                spentPoints += ksc.RDUpgrades[1]; //only count this once, all KSCs share this value
            }
            else
            {
                for (int i = 0; i < ksc.VABUpgrades.Count; i++) spentPoints += ksc.VABUpgrades[i];
                for (int i = 0; i < ksc.SPHUpgrades.Count; i++) spentPoints += ksc.SPHUpgrades[i];
                for (int i = 0; i < ksc.RDUpgrades.Count; i++) spentPoints += ksc.RDUpgrades[i];
            }
            return spentPoints;
        }

        public static int SpentUpgradesFor(SpaceCenterFacility facility, KCT_KSC ksc = null)
        {
            if (ksc == null) ksc = KCT_GameStates.ActiveKSC;
            int spentPoints = 0;
            switch (facility)
            {
                case SpaceCenterFacility.ResearchAndDevelopment:
                    if (KCT_PresetManager.Instance.ActivePreset.generalSettings.SharedUpgradePool)
                    {
                        for (int j = 0; j < KCT_GameStates.KSCs.Count; j++)
                        {
                            KCT_KSC KSC = KCT_GameStates.KSCs[j];
                            spentPoints += KSC.RDUpgrades[0];
                        }
                        spentPoints += ksc.RDUpgrades[1]; //only count this once, all KSCs share this value
                    }
                    else
                    {
                        for (int i = 0; i < ksc.RDUpgrades.Count; i++) spentPoints += ksc.RDUpgrades[i];
                    }
                    break;
                case SpaceCenterFacility.SpaceplaneHangar:
                    if (KCT_PresetManager.Instance.ActivePreset.generalSettings.SharedUpgradePool)
                    {
                        for (int j = 0; j < KCT_GameStates.KSCs.Count; j++)
                        {
                            KCT_KSC KSC = KCT_GameStates.KSCs[j];
                            for (int i = 0; i < KSC.SPHUpgrades.Count; i++) spentPoints += KSC.SPHUpgrades[i];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ksc.SPHUpgrades.Count; i++) spentPoints += ksc.SPHUpgrades[i];
                    }
                    break;
                case SpaceCenterFacility.VehicleAssemblyBuilding:
                    if (KCT_PresetManager.Instance.ActivePreset.generalSettings.SharedUpgradePool)
                    {
                        for (int j = 0; j < KCT_GameStates.KSCs.Count; j++)
                        {
                            KCT_KSC KSC = KCT_GameStates.KSCs[j];
                            for (int i = 0; i < KSC.VABUpgrades.Count; i++) spentPoints += KSC.VABUpgrades[i];
                        }
                    }
                    else
                    {
                        for (int i = 0; i < ksc.VABUpgrades.Count; i++) spentPoints += ksc.VABUpgrades[i];
                    }
                    break;
                default:
                    throw new ArgumentException("invalid facility");
            }

            return spentPoints;
        }

        public static List<string> GetLaunchSites(bool isVAB)
        {
            EditorDriver.editorFacility = isVAB ? EditorFacility.VAB : EditorFacility.SPH;
            typeof(EditorDriver).GetMethod("setupValidLaunchSites", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
            return EditorDriver.ValidLaunchSites;
        }

        private static bool? _KSCSwitcherInstalled = null;
        public static bool KSCSwitcherInstalled
        {
            get
            {
                if (_KSCSwitcherInstalled == null)
                {
                    Type Switcher = null;
                    AssemblyLoader.loadedAssemblies.TypeOperation(t =>
                    {
                        if (t.FullName == "regexKSP.KSCSwitcher")
                        {
                            Switcher = t;
                        }
                    });

                    _KSCSwitcherInstalled = (Switcher != null);

                    //KCTDebug.Log("KSCSwitcher status: " + _KSCSwitcherInstalled);
                }
                return (_KSCSwitcherInstalled == null ? false : (bool)_KSCSwitcherInstalled);
            }
        }

        private static bool? _isPrincipiaInstalled = null;
        public static bool IsPrincipiaInstalled
        {
            get
            {
                if (!_isPrincipiaInstalled.HasValue)
                {
                    _isPrincipiaInstalled = AssemblyLoader.loadedAssemblies.Any(a => string.Equals(a.name, "ksp_plugin_adapter", StringComparison.OrdinalIgnoreCase));
                }
                return _isPrincipiaInstalled.Value;
            }
        }

        public static string GetActiveRSSKSC()
        {
            if (!KSCSwitcherInstalled) return "Stock";

            //get the LastKSC.KSCLoader.instance object
            //check the Sites object (KSCSiteManager) for the lastSite, if "" then get defaultSite
            Type Loader = null;
            AssemblyLoader.loadedAssemblies.TypeOperation(t =>
            {
                if (t.FullName == "regexKSP.KSCLoader")
                {
                    Loader = t;
                }
            });
            object LoaderInstance = GetMemberInfoValue(Loader.GetMember("instance")[0], null);
            if (LoaderInstance == null)
                return "Stock";
            object SitesObj = GetMemberInfoValue(Loader.GetMember("Sites")[0], LoaderInstance);
            string lastSite = (string)GetMemberInfoValue(SitesObj.GetType().GetMember("lastSite")[0], SitesObj);

            if (lastSite == "")
            {
                string defaultSite = (string)GetMemberInfoValue(SitesObj.GetType().GetMember("defaultSite")[0], SitesObj);
                return defaultSite;
            }
            return lastSite;
        }

        public static void SetActiveKSCToRSS()
        {
            string site = GetActiveRSSKSC();
            SetActiveKSC(site);
        }

        public static void SetActiveKSC(string site)
        {
            if (site == "") site = "Stock";
            if (KCT_GameStates.ActiveKSC == null || site != KCT_GameStates.ActiveKSC.KSCName)
            {
                KCTDebug.Log("Setting active site to " + site);
                KCT_KSC setActive = KCT_GameStates.KSCs.FirstOrDefault(ksc => ksc.KSCName == site);
                if (setActive != null)
                {
                    KCT_GameStates.ActiveKSC = setActive;
                }
                else
                {
                    setActive = new KCT_KSC(site);
                    if (CurrentGameIsCareer())
                        setActive.ActiveLPInstance.level = 0;
                    KCT_GameStates.KSCs.Add(setActive);
                    KCT_GameStates.ActiveKSC = setActive;
                }
            }
            KCT_GameStates.activeKSCName = site;
        }

        public static PQSCity FindKSC(CelestialBody home)
        {
            if (home != null)
            {
                if (home.pqsController != null && home.pqsController.transform != null)
                {
                    Transform t = home.pqsController.transform.Find("KSC");
                    if (t != null)
                    {
                        PQSCity KSC = (PQSCity)t.GetComponent(typeof(PQSCity));
                        if (KSC != null) { return KSC; }
                    }
                }
            }

            PQSCity[] cities = Resources.FindObjectsOfTypeAll<PQSCity>();
            foreach (PQSCity c in cities)
            {
                if (c.name == "KSC")
                {
                    return c;
                }
            }

            return null;
        }

        public static void DisplayMessage(String title, StringBuilder text, MessageSystemButton.MessageButtonColor color, MessageSystemButton.ButtonIcons icon)
        {
            
            MessageSystem.Message m = new MessageSystem.Message(title, text.ToString(), color, icon);
            MessageSystem.Instance.AddMessage(m);
        }

        public static bool LaunchFacilityIntact(KCT_BuildListVessel.ListType type)
        {
            bool intact = true;
            if (type == KCT_BuildListVessel.ListType.VAB)
            {
                //intact = new PreFlightTests.FacilityOperational("LaunchPad", "building").Test();
                intact = new PreFlightTests.FacilityOperational("LaunchPad", "LaunchPad").Test();
            }
            else if (type == KCT_BuildListVessel.ListType.SPH)
            {
                if (!new PreFlightTests.FacilityOperational("Runway", "Runway").Test())
                    intact = false;
            }
            return intact;
        }

        public static void RecalculateEditorBuildTime(ShipConstruct ship)
        {
            if (!HighLogic.LoadedSceneIsEditor)
            {
                return;
            }

            double effCost = GetEffectiveCost(ship.Parts);
            KCT_GameStates.EditorBuildTime = GetBuildTime(effCost);
            var kctVessel = new KCT_BuildListVessel(ship, EditorLogic.fetch.launchSiteName, effCost, KCT_GameStates.EditorBuildTime, EditorLogic.FlagURL);

            KCT_GameStates.EditorIntegrationTime = KCT_MathParsing.ParseIntegrationTimeFormula(kctVessel);
            KCT_GameStates.EditorRolloutCosts = KCT_MathParsing.ParseRolloutCostFormula(kctVessel);
            KCT_GameStates.EditorIntegrationCosts = KCT_MathParsing.ParseIntegrationCostFormula(kctVessel);
            KCT_GameStates.EditorRolloutTime = KCT_MathParsing.ParseReconditioningFormula(kctVessel, false);
        }

        public static bool ApproximatelyEqual(double d1, double d2, double error = 0.01 )
        {
            return (1-error) <= (d1 / d2) && (d1 / d2) <= (1+error);
        }

        public static float GetParachuteDragFromPart(AvailablePart parachute)
        {
            foreach (AvailablePart.ModuleInfo mi in parachute.moduleInfos)
            {
                if (mi.info.Contains("Fully-Deployed Drag"))
                {
                    string[] split = mi.info.Split(new Char[] {':', '\n'});
                    //TODO: Get SR code and put that in here, maybe with TryParse instead of Parse
                    for (int i=0; i<split.Length; i++)
                    {
                        if (split[i].Contains("Fully-Deployed Drag"))
                        {
                            float drag = 500;
                            if (!float.TryParse(split[i+1], out drag))
                            {
                                string[] split2 = split[i + 1].Split('>');
                                if (!float.TryParse(split2[1], out drag))
                                {
                                    Debug.Log("[KCT] Failure trying to read parachute data. Assuming 500 drag.");
                                    drag = 500;
                                }
                            }
                            return drag;
                        }
                    }
                }
            }
            return 0;
        }

        public static bool IsUnmannedCommand(AvailablePart part)
        {
            foreach (AvailablePart.ModuleInfo mi in part.moduleInfos)
            {
                if (mi.info.Contains("Unmanned")) return true;
            }
            return false;
        }

        public static bool ReconditioningActive(KCT_KSC KSC, string launchSite = "LaunchPad")
        {
            if (KSC == null) KSC = KCT_GameStates.ActiveKSC;

            KCT_Recon_Rollout recon = KSC.GetReconditioning(launchSite);
            return (recon != null);
        }

        public static KCT_BuildListVessel FindBLVesselByID(Guid id)
        {
            foreach (KCT_KSC ksc in KCT_GameStates.KSCs)
            {
                var v = FindBLVesselByID(id, ksc);
                if (v != null) return v;
            }

            return null;
        }

        public static KCT_BuildListVessel FindBLVesselByID(Guid id, KCT_KSC ksc)
        {
            if (ksc != null)
            {
                KCT_BuildListVessel ves = ksc.VABWarehouse.Find(blv => blv.id == id);
                if (ves != null)
                    return ves;

                ves = ksc.VABList.Find(blv => blv.id == id);
                if (ves != null)
                    return ves;

                ves = ksc.SPHWarehouse.Find(blv => blv.id == id);
                if (ves != null)
                    return ves;

                ves = ksc.SPHList.Find(blv => blv.id == id);
                if (ves != null)
                    return ves;
            }

            return null;
        }

        /**
         * Don't actually use this!
         * */
        public static ConfigNode ProtoVesselToCraftFile(ProtoVessel vessel)
        {
            ConfigNode craft = new ConfigNode("ShipNode");
            ConfigNode pvNode = new ConfigNode();
            vessel.Save(pvNode);
            //KCTDebug.Log(pvNode);

            craft.AddValue("ship", pvNode.GetValue("name"));
            craft.AddValue("version", Versioning.GetVersionString());
            craft.AddValue("description", "Craft file converted automatically by Kerbal Construction Time.");
            craft.AddValue("type", "VAB");
            ConfigNode[] parts = pvNode.GetNodes("PART");
            foreach (ConfigNode part in parts)
            {
                ConfigNode newPart = new ConfigNode("PART");
                newPart.AddValue("part", part.GetValue("name") + "_" + part.GetValue("uid"));
                newPart.AddValue("partName", "Part");
                newPart.AddValue("pos", part.GetValue("position"));
                newPart.AddValue("rot", part.GetValue("rotation"));
                newPart.AddValue("attRot", part.GetValue("rotation"));
                newPart.AddValue("mir", part.GetValue("mirror"));
                newPart.AddValue("istg", part.GetValue("istg"));
                newPart.AddValue("dstg", part.GetValue("dstg"));
                newPart.AddValue("sidx", part.GetValue("sidx"));
                newPart.AddValue("sqor", part.GetValue("sqor"));
                newPart.AddValue("attm", part.GetValue("attm"));
                newPart.AddValue("modCost", part.GetValue("modCost"));

                foreach (string attn in part.GetValues("attN"))
                {
                    string attach_point = attn.Split(',')[0];
                    if (attach_point == "None")
                        continue;
                    int attachedIndex = int.Parse(attn.Split(',')[1]);
                    string attached = parts[attachedIndex].GetValue("name") + "_" + parts[attachedIndex].GetValue("uid");
                    newPart.AddValue("link", attached);
                    newPart.AddValue("attN", attach_point + "," + attached);
                }

                newPart.AddNode(part.GetNode("EVENTS"));
                newPart.AddNode(part.GetNode("ACTIONS"));
                foreach (ConfigNode mod in part.GetNodes("MODULE"))
                    newPart.AddNode(mod);
                foreach (ConfigNode rsc in part.GetNodes("RESOURCE"))
                    newPart.AddNode(rsc);
                craft.AddNode(newPart);
            }


            return craft;
        }

        public static void AddToDict(Dictionary<string, int> dict, string key, int value)
        {
            if (value <= 0) return;
            if (!dict.ContainsKey(key))
                dict.Add(key, value);
            else
                dict[key] += value;
        }

        public static bool RemoveFromDict(Dictionary<string, int> dict, string key, int value)
        {
            if (!dict.ContainsKey(key))
                return false;
            else if (dict[key] < value)
                return false;
            else
            {
                dict[key] -= value;
                return true;
            }

        }

        public static bool PartIsUnlocked(ConfigNode partNode)
        {
            string partName = PartNameFromNode(partNode);
            return PartIsUnlocked(partName);
        }

        public static bool PartIsUnlocked(string partName)
        {
            if (partName == null) return false;

            AvailablePart partInfoByName = PartLoader.getPartInfoByName(partName);
            if (partInfoByName == null) return false;

            ProtoTechNode techState = ResearchAndDevelopment.Instance.GetTechState(partInfoByName.TechRequired);
            bool partIsUnlocked = techState != null && techState.state == RDTech.State.Available &&
                                  RUIutils.Any(techState.partsPurchased, (a => a.name == partName));

            bool isExperimental = ResearchAndDevelopment.IsExperimentalPart(partInfoByName);

            return partIsUnlocked || isExperimental;
        }

        public static bool PartIsProcedural(ConfigNode part)
        {
            ConfigNode[] modules = part.GetNodes("MODULE");
            if (modules == null)
                return false;
            foreach (ConfigNode mod in modules)
            {
                if (mod.HasValue("name") && mod.GetValue("name").ToLower().Contains("procedural"))
                    return true;
            }
            return false;
        }

        public static bool PartIsProcedural(ProtoPartSnapshot part)
        {
            if (part.modules != null)
                return part.modules.Find(m => m != null && m.moduleName != null && m.moduleName.ToLower().Contains("procedural")) != null;
            return false;
        }

        public static bool PartIsProcedural(Part part)
        {
            if (part != null && part.Modules != null)
            {
                for (int i = 0; i < part.Modules.Count; i++ )
                {
                    if (part.Modules[i] != null && part.Modules[i].moduleName != null && part.Modules[i].moduleName.ToLower().Contains("procedural"))
                        return true;
                }
            }
            return false;
        }

        public static string ConstructLockedPartsWarning(Dictionary<AvailablePart, int> lockedPartsOnShip)
        {
            if (lockedPartsOnShip == null || lockedPartsOnShip.Count == 0)
                return null;

            StringBuilder sb = new StringBuilder();
            sb.Append(LocalCache.str_Messages_FailedReasons_PartNotAvailable); // "This vessel contains parts which are not available at the moment:\n"

            foreach (KeyValuePair<AvailablePart, int> kvp in lockedPartsOnShip)
            {
                sb.Append($" <color=orange><b>{kvp.Value}x {kvp.Key.title}</b></color>\n");
            }

            return sb.ToString();
        }

        public static int BuildingUpgradeLevel(SpaceCenterFacility facility)
        {
            int lvl = BuildingUpgradeMaxLevel(facility);
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                lvl = (int)Math.Round((lvl * ScenarioUpgradeableFacilities.GetFacilityLevel(facility)));
            }
            return lvl;
        }

        public static int BuildingUpgradeLevel(string facilityID)
        {
            int lvl = BuildingUpgradeMaxLevel(facilityID);
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                lvl = (int)Math.Round((lvl * ScenarioUpgradeableFacilities.GetFacilityLevel(facilityID))); //let's not store discrete things with integers! No! Let's use floats! -Squad
            }
            return lvl;
        }

        public static int BuildingUpgradeMaxLevel(string facilityID)
        {
            int lvl = ScenarioUpgradeableFacilities.GetFacilityLevelCount(facilityID);
            if (lvl < 0)
            {
                if (!KCT_GameStates.BuildingMaxLevelCache.TryGetValue(facilityID.Split('/').Last(), out lvl))
                {
                    //screw it, let's call it 2
                    lvl = 2;
                    KCTDebug.Log($"Couldn't get actual max level or cached one for {facilityID}. Assuming 2.");
                }
            }
            return lvl;
        }

        public static int BuildingUpgradeMaxLevel(SpaceCenterFacility facility)
        {
            int lvl = ScenarioUpgradeableFacilities.GetFacilityLevelCount(facility);
            if (lvl < 0)
            {
                if (!KCT_GameStates.BuildingMaxLevelCache.TryGetValue(facility.ToString(), out lvl))
                {
                    //screw it, let's call it 2
                    lvl = 2;
                    KCTDebug.Log($"Couldn't get actual max level or cached one for {facility}. Assuming 2.");
                }
            }
            return lvl;
        }

        public static int TotalUpgradePoints()
        {
            int total = 0;
            //Starting points
            total += KCT_PresetManager.Instance.StartingUpgrades(HighLogic.CurrentGame.Mode);
            //R&D
            if (KCT_PresetManager.Instance.ActivePreset.generalSettings.TechUpgrades)
            {
                //Completed tech nodes
                if (CurrentGameHasScience())
                {
                    total += KCT_GameStates.LastKnownTechCount;
                    if (KCT_GameStates.LastKnownTechCount == 0)
                        total += ResearchAndDevelopment.Instance != null ? ResearchAndDevelopment.Instance.snapshot.GetData().GetNodes("Tech").Length : 0;
                }

                //In progress tech nodes
                total += KCT_GameStates.TechList.Count;
            }
            total += (int)KCT_MathParsing.GetStandardFormulaValue("UpgradesForScience", new Dictionary<string, string>() { { "N", KCT_GameStates.SciPointsTotal.ToString() } });
            //Purchased funds
            total += KCT_GameStates.PurchasedUpgrades[0];
            //Purchased science
            total += KCT_GameStates.PurchasedUpgrades[1];
            //Inventory sales
            total += (int)KCT_GameStates.InventorySaleUpgrades;
            //Temp upgrades (currently for when tech nodes finish)
            total += KCT_GameStates.MiscellaneousTempUpgrades;
            
            //Misc. (when API)
            total += KCT_GameStates.TemporaryModAddedUpgradesButReallyWaitForTheAPI;
            total += KCT_GameStates.PermanentModAddedUpgradesButReallyWaitForTheAPI;


            return total;
        }
#if false
        public static bool RecoverVesselToStorage(KCT_BuildListVessel.ListType listType, Vessel v)
        {
            Debug.Log("RecoverVesselToStorage 1");

            ConfigNode nodeToSave = new ConfigNode();
            //save vessel
            ConfigNode vesselNode = new ConfigNode("VESSEL");
            ProtoVessel pVessel = v.BackupVessel();
            pVessel.vesselRef = v;
            pVessel.Save(vesselNode);
            nodeToSave.AddNode("VESSEL", vesselNode);
            nodeToSave.Save("vesselToSave");

            ShipConstruct test = new ShipConstruct();
            try
            {
                KCTDebug.Log("Attempting to recover active vessel to storage.  listType: " + listType);
                GamePersistence.SaveGame("KCT_Backup", HighLogic.SaveFolder, SaveMode.OVERWRITE);
#if false
                ProtoVessel VesselToSave = HighLogic.CurrentGame.AddVessel(nodeToSave);
                if (VesselToSave.vesselRef == null)
                {
                    Debug.Log("Vessel reference is null!");
                    return false;
                }
                try
                {
                    string ShipName = VesselToSave.vesselName;

                    //Vessel FromFlight = FlightGlobals.Vessels.Find(v => v.id == VesselToSave.vesselID);
                    try
                    {
                        VesselToSave.vesselRef.Load();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Debug.Log("Attempting to continue.");
                    }
                }
#endif
                KCT_GameStates.recoveredVessel = new KCT_BuildListVessel(pVessel, vesselNode, listType);


                //KCT_GameStates.recoveredVessel.type = listType;
                if (listType == KCT_BuildListVessel.ListType.VAB)
                    KCT_GameStates.recoveredVessel.launchSite = "LaunchPad";
                else
                    KCT_GameStates.recoveredVessel.launchSite = "Runway";

                //check for symmetry parts and remove those references if they can't be found
                RemoveMissingSymmetry(KCT_GameStates.recoveredVessel.shipNode);
                Debug.Log("RecoverVesselToStorage 1");

                // debug, save to a file
                KCT_GameStates.recoveredVessel.shipNode.Save("KCTVesselSave_trackingstation");

                Debug.Log("RecoverVesselToStorage 2");
                if (test == null)
                    Debug.Log("test is null");
                if (KCT_GameStates.recoveredVessel == null)
                    Debug.Log("KCT_GameStates.recoveredVessel is null");
                if (KCT_GameStates.recoveredVessel.shipNode == null)
                    Debug.Log("KCT_GameStates.recoveredVessel.shipNode is null");

                Debug.Log("RecoverVesselToStorage 2.1");
                //test if we can actually convert it

                bool success = test.LoadShip(KCT_GameStates.recoveredVessel.shipNode);
                Debug.Log("RecoverVesselToStorage 3, success: " + success);
                //return false;
         
                if (success)
                    ShipConstruction.CreateBackup(test);
                KCTDebug.Log("Load test reported success = " + success);
                if (!success)
                {
                    KCT_GameStates.recoveredVessel = null;
                    return false;
                }

               

                // Recovering the vessel in a coroutine was generating an exception insideKSP if a mod had added
                // modules to the vessel or it's parts at runtime.
                //
                // This is the way KSP does it
                //
                GameEvents.OnVesselRecoveryRequested.Fire(FlightGlobals.ActiveVessel);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[KCT] Error while recovering craft into inventory.");
                Debug.LogError("[KCT] error: " + ex.Message);
                KCT_GameStates.recoveredVessel = null;
                ShipConstruction.ClearBackups();
                return false;
            }
            return false;
        }
#endif

        public static bool RecoverActiveVesselToStorage(KCT_BuildListVessel.ListType listType)
        {
            ShipConstruct test = new ShipConstruct();
            try
            {
                KCTDebug.Log("Attempting to recover active vessel to storage.  listType: " + listType);
                GamePersistence.SaveGame("KCT_Backup", HighLogic.SaveFolder, SaveMode.OVERWRITE);
  
                KCT_GameStates.recoveredVessel = new KCT_BuildListVessel(FlightGlobals.ActiveVessel, listType);
  
                //KCT_GameStates.recoveredVessel.type = listType;
                if (listType == KCT_BuildListVessel.ListType.VAB)
                    KCT_GameStates.recoveredVessel.launchSite = "LaunchPad";
                else
                    KCT_GameStates.recoveredVessel.launchSite = "Runway";

                //check for symmetry parts and remove those references if they can't be found
                RemoveMissingSymmetry(KCT_GameStates.recoveredVessel.shipNode);

                // debug, save to a file
                KCT_GameStates.recoveredVessel.shipNode.Save("KCTVesselSave");

                //test if we can actually convert it
                bool success = test.LoadShip(KCT_GameStates.recoveredVessel.shipNode);

                if (success)
                    ShipConstruction.CreateBackup(test);
                KCTDebug.Log("Load test reported success = " + success);
                if (!success)
                {
                    KCT_GameStates.recoveredVessel = null;
                    return false;
                }

                // Recovering the vessel in a coroutine was generating an exception insideKSP if a mod had added
                // modules to the vessel or it's parts at runtime.
                //
                // This is the way KSP does it
                //
                GameEvents.OnVesselRecoveryRequested.Fire(FlightGlobals.ActiveVessel);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[KCT] Error while recovering craft into inventory.");
                Debug.LogError("[KCT] error: " + ex.Message);
                KCT_GameStates.recoveredVessel = null;
                ShipConstruction.ClearBackups();
                return false;
            }
        }

        public static void RemoveMissingSymmetry(ConfigNode ship)
        {
            //loop through, find all sym = lines and find the part they reference
            int referencesRemoved = 0;
            foreach (ConfigNode partNode in ship.GetNodes("PART"))
            {
                List<string> toRemove = new List<string>();
                foreach (string symPart in partNode.GetValues("sym"))
                {
                    //find the part in the ship
                    if (ship.GetNodes("PART").FirstOrDefault(cn => cn.GetValue("part") == symPart) == null)
                        toRemove.Add(symPart);
                }

                foreach (string remove in toRemove)
                {
                    foreach (ConfigNode.Value val in partNode.values)
                    {
                        if (val.value == remove)
                        {
                            referencesRemoved++;
                            partNode.values.Remove(val);
                            break;
                        }
                    }
                }
            }
            KCTDebug.Log("Removed " + referencesRemoved + " invalid symmetry references.");
        }

        /// <summary>
        /// Overrides or disables the editor's launch button (and individual site buttons) depending on settings
        /// </summary>
        public static void HandleEditorButton()
        {
            if (KCT_GUI.PrimarilyDisabled)
            {
                return;
            }

            //also set the editor ui to 1 height
            KCT_GUI.editorWindowPosition.height = 1;

            var kctInstance = (KCT_Editor)KerbalConstructionTime.instance;

            if (KCT_GameStates.settings.OverrideLaunchButton)
            {
                if (KCT_GameStates.EditorShipEditingMode)
                {
                    // Prevent switching between VAB and SPH in edit mode.
                    // Bad things will happen if the edits are saved in another mode than the initial one.
                    EditorLogic.fetch.switchEditorBtn.onClick.RemoveAllListeners();
                    EditorLogic.fetch.switchEditorBtn.onClick.AddListener(() =>
                    {
                        PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "cannotSwitchEditor",
                            LocalCache.str_Messages_NotSwitchEditor_title, // "Cannot switch editor!"
                            LocalCache.str_Messages_NotSwitchEditorMsg, // "Switching between VAB and SPH is not allowed while editing a vessel."
                            LocalCache.Btn_Acknowledged, false, HighLogic.UISkin); // "Acknowledged"
                    });
                }

                KCTDebug.Log("Attempting to take control of launch button");

                // EditorLogic.fetch.launchBtn.onClick = new UnityEngine.UI.Button.ButtonClickedEvent(); //delete all other listeners (sorry :( )
                EditorLogic.fetch.launchBtn.onClick.RemoveAllListeners();
                EditorLogic.fetch.launchBtn.onClick.AddListener(() => { KerbalConstructionTime.ShowLaunchAlert(null); });

                if (!kctInstance.isLaunchSiteControllerBound)
                {
                    kctInstance.isLaunchSiteControllerBound = true;
                    KCTDebug.Log("Attempting to take control of launchsite specific buttons");
                    //delete listeners to the launchsite specific buttons
                    UILaunchsiteController controller = UnityEngine.Object.FindObjectOfType<UILaunchsiteController>();
                    if (controller == null)
                        KCTDebug.Log("HandleEditorButton.controller is null");
                    else
                    {
                        //
                        // Need to use the try/catch because if multiple launch sites are disabled, then this would generate
                        // the following error:
                        //                          Cannot cast from source type to destination type
                        // which happens because the private member "launchPadItems" is a list, and if it is null, then it is
                        // not castable to a IEnumerable
                        //
                        try
                        {
                            IEnumerable list = controller.GetType().GetPrivateMemberValue("launchPadItems", controller, 4) as IEnumerable;

                            if (list != null)
                            {
                                foreach (object site in list)
                                {
                                    //find and disable the button
                                    //why isn't EditorLaunchPadItem public despite all of its members being public?
                                    UnityEngine.UI.Button button = site.GetType().GetPublicValue<UnityEngine.UI.Button>("buttonLaunch", site);
                                    if (button != null)
                                    {
                                        //button.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                                        button.onClick.RemoveAllListeners();
                                        string siteName = site.GetType().GetPublicValue<string>("siteName", site);
                                        button.onClick.AddListener(() => { KerbalConstructionTime.ShowLaunchAlert(siteName); });
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            KCTDebug.Log("HandleEditorButton: Exception: " + ex.Message);
                        }
                    }
                }
            }
            else
            {
                InputLockManager.SetControlLock(ControlTypes.EDITOR_LAUNCH, "KCTLaunchLock");
                if (!kctInstance.isLaunchSiteControllerBound)
                {
                    kctInstance.isLaunchSiteControllerBound = true;
                    KCTDebug.Log("Attempting to disable launchsite specific buttons");
                    UILaunchsiteController controller = UnityEngine.Object.FindObjectOfType<UILaunchsiteController>();
                    if (controller != null)
                    {
                        controller.locked = true;
                    }
                }
            }
        }

        public static bool IsVabRecoveryAvailable()
        {
            string reqTech = KCT_PresetManager.Instance.ActivePreset.generalSettings.VABRecoveryTech;
            return HighLogic.LoadedSceneIsFlight && FlightGlobals.ActiveVessel != null &&
                   FlightGlobals.ActiveVessel.IsRecoverable &&
                   FlightGlobals.ActiveVessel.IsClearToSave() == ClearToSaveStatus.CLEAR && 
                   (FlightGlobals.ActiveVessel.situation == Vessel.Situations.PRELAUNCH ||
                    string.IsNullOrEmpty(reqTech) ||
                    ResearchAndDevelopment.GetTechnologyState(reqTech) == RDTech.State.Available);
        }

        public static void SetIsKCTBuiltFlags(ConfigNode shipNode)
        {
            foreach (ConfigNode partNode in shipNode.nodes)
            {
                if (partNode.name == "PART")
                {
                    foreach (ConfigNode partModuleNode in partNode.nodes)       // Not all of .nodes are nodes of modules, but nobody cares
                    {
                        partModuleNode.SetValue("isKCTBuilt", true, false);
                    }
                }
            }
        }

        static public bool KerbalInExternalSeat(Vessel v, bool unloaded = false)
        {
            if (!unloaded)
            {
                var kerbalSeats = v.FindPartModulesImplementing<KerbalSeat>();
                foreach (var m in kerbalSeats)
                {
                    if (m.Occupant != null)
                        return true;
                }
            } else
            {
                foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots)
                {
                    if (p.partName.StartsWith("kerbalEVA"))
                        return true;
                }
            }
            return false;
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
