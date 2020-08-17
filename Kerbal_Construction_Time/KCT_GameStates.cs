using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolbarControl_NS;

namespace KerbalConstructionTime
{
    public static class KCT_GameStates
    {
        public static double UT, lastUT = 0.0;
        public static bool canWarp = false, warpInitiated = false;
        public static int lastWarpRate = 0;
        public static string lastSOIVessel = "";
        public static List<VesselType> VesselTypesForSOI = new List<VesselType>() { VesselType.Base, VesselType.Lander, VesselType.Probe, VesselType.Ship, VesselType.Station };
        public static List<Orbit.PatchTransitionType> SOITransitions = new List<Orbit.PatchTransitionType> { Orbit.PatchTransitionType.ENCOUNTER, Orbit.PatchTransitionType.ESCAPE };
        public static KCT_Settings settings = new KCT_Settings();

        public static KCT_KSC ActiveKSC = null;
        public static List<KCT_KSC> KSCs = new List<KCT_KSC>();
        public static string activeKSCName = "";
        public static bool UpdateLaunchpadDestructionState = false;
        internal static int TechUpgradesTotal = 0;
        internal static float SciPointsTotal = -1f;

        public static bool Enabled {  get { return KCT_GUI.WorkingPreset.generalSettings.Enabled; } }
        public static float SciencePointsAllocated {  get { return SciPointsTotal; } }
        public static int TechUpgradePointsAllocated {  get { return TechUpgradesTotal; } }

        public static bool mergingAvailable;
        public static List<KCT_BuildListVessel> mergedVessels = new List<KCT_BuildListVessel>();

        public class KCT_TechItemIlist<T> : List<T>
        {
            // public event Action<int> Changed = delegate { };
            public event Action Updated = delegate { };

            public new void Add(T item) { base.Add(item); Updated(); }
            public new void Remove(T item) { base.Remove(item); Updated(); }
            public new void AddRange(IEnumerable<T> collection) { base.AddRange(collection); Updated(); }
            public new void RemoveRange(int index, int count) { base.RemoveRange(index, count); Updated(); }
            public new void Clear() { base.Clear(); Updated(); }
            public new void Insert(int index, T item) { base.Insert(index, item); Updated(); }
            public new void InsertRange(int index, IEnumerable<T> collection) { base.InsertRange(index, collection); Updated(); }
            public new void RemoveAll(Predicate<T> match) { base.RemoveAll(match); Updated(); }

            public new T this[int index]
            {
                get { return base[index]; }
                set { base[index] = value; Updated(); } // Changed(index); }
            }
        }

        public static KCT_TechItemIlist<KCT_TechItem> TechList;

        //public static List<KCT_TechItem> TechList = new List<KCT_TechItem>();

        public static List<int> PurchasedUpgrades = new List<int>() { 0, 0 };
        public static int MiscellaneousTempUpgrades = 0, LastKnownTechCount = 0;
        public static float InventorySaleUpgrades = 0, InventorySalesFigures = 0;
        public static int UpgradesResetCounter = 0;
        public static KCT_BuildListVessel launchedVessel, editedVessel, recoveredVessel;
        public static List<CrewedPart> launchedCrew = new List<CrewedPart>();
        //public static IButton kctToolbarButton;

        public static ToolbarControl toolbarControl;
        internal const string MODID = "KCT_NS";
        internal const string MODNAME = "Kerbal Construction Time";

        public static bool EditorShipEditingMode = false;
        public static bool firstStart = false;
        public static IKCTBuildItem targetedItem = null;
        public static double EditorBuildTime = 0;
        public static double EditorIntegrationTime = 0;
        public static double EditorRolloutCosts = 0;
        public static double EditorRolloutTime = 0;
        public static double EditorIntegrationCosts = 0;
        public static bool LaunchFromTS = false;
        public static List<AvailablePart> ExperimentalParts = new List<AvailablePart>();

        public static Dictionary<string, int> BuildingMaxLevelCache = new Dictionary<string, int>();

        public static List<bool> showWindows = new List<bool> { false, true }; //build list, editor
        public static string KACAlarmId = "";
        public static double KACAlarmUT = 0;

        public static KCT_OnLoadError erroredDuringOnLoad = new KCT_OnLoadError();

        public static int TemporaryModAddedUpgradesButReallyWaitForTheAPI = 0; //Reset when returned to the MainMenu
        public static int PermanentModAddedUpgradesButReallyWaitForTheAPI = 0; //Saved to the save file

        public static bool vesselErrorAlerted = false;
        public static bool PersistenceLoaded = false;
        public static bool isRefunding = false;

        public static AirlaunchParams AirlaunchParams;

        public static void reset()
        {
            firstStart = false;
            vesselErrorAlerted = false;

            PurchasedUpgrades = new List<int>() { 0, 0 };
            targetedItem = null;
            KCT_GUI.ResetFormulaRateHolders();

            InventorySaleUpgrades = 0;
            InventorySalesFigures = 0;

            ExperimentalParts.Clear();
            MiscellaneousTempUpgrades = 0;

            BuildingMaxLevelCache.Clear();

            lastUT = 0;

            InitAndClearTechList();        
        }

        public static void InitAndClearTechList()
        {
            if (TechList != null)
                TechList.Clear();
            else
                TechList = new KCT_TechItemIlist<KCT_TechItem>();
            TechList.Updated += KerbalConstructionTime.instance.UpdateTechlistIconColor;
        }
    }

    public class CrewedPart
    {
        public List<ProtoCrewMember> crewList;
        public uint partID;

        public CrewedPart(uint ID, List<ProtoCrewMember> crew)
        {
            partID = ID;
            crewList = crew;
        }

        public CrewedPart FromPart(Part part, List<ProtoCrewMember> crew)
        {
            partID = part.flightID;
            crewList = crew;
            return this;
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
