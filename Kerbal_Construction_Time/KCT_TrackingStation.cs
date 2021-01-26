
using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.UI;

namespace KerbalConstructionTime
{

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class KCT_Tracking_Station : KerbalConstructionTime
    {
        public Button.ButtonClickedEvent originalCallback, flyCallback;
        Vessel selectedVessel = null;

        public new void Start()
        {
            base.Start();
            if (KCT_GUI.PrimarilyDisabled)
                return;

            KCTDebug.Log("KCT_Flight, Start");
            SpaceTracking trackingStation = UnityEngine.Object.FindObjectOfType<SpaceTracking>();
            if (trackingStation != null)
            {
                originalCallback = trackingStation.RecoverButton.onClick;
                flyCallback = trackingStation.FlyButton.onClick;

                trackingStation.RecoverButton.onClick = new Button.ButtonClickedEvent();
                trackingStation.RecoverButton.onClick.AddListener(NewRecoveryFunctionTrackingStation);
            }
        }

        void Fly()
        {
            flyCallback.Invoke();
        }

        void KCT_Recovery()
        {
            DialogGUIBase[] options = new DialogGUIBase[2];
            options[0] = new DialogGUIButton("Go to Flight scene", Fly);
            options[1] = new DialogGUIButton("Cancel", Cancel);

            MultiOptionDialog diag = new MultiOptionDialog("scrapVesselPopup", "KCT can only recover vessels in the Flight scene", "Recover Vessel", null, options: options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);

        }
        public void RecoverToVAB()
        {
            //if (!KCT_Utilities.RecoverVesselToStorage(KCT_BuildListVessel.ListType.VAB, selectedVessel))
            {
                //PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "vesselRecoverErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);

                KCT_Recovery();
            }
        }

        public void RecoverToSPH()
        {
            //if (!KCT_Utilities.RecoverVesselToStorage(KCT_BuildListVessel.ListType.SPH, selectedVessel))
            {
                //PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "recoverShipErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);

                KCT_Recovery();
            }
        }

        public void DoNormalRecovery()
        {
            originalCallback.Invoke();
        }

        public void Cancel()
        {
            return;
        }

        public void NewRecoveryFunctionTrackingStation()
        {
            Debug.Log("NewRecoveryFunctionTrackingStation");
            selectedVessel = null;
            SpaceTracking trackingStation = (SpaceTracking)FindObjectOfType(typeof(SpaceTracking));
            if (trackingStation == null)
                Debug.Log("trackingStation is null");
            selectedVessel = trackingStation.SelectedVessel;

            if (selectedVessel == null)
            {
                Debug.Log("[KCT] Error! No Vessel selected.");
                return;
            }


            bool sph = (selectedVessel.IsRecoverable && selectedVessel.IsClearToSave() == ClearToSaveStatus.CLEAR);

            string reqTech = KCT_PresetManager.Instance.ActivePreset.generalSettings.VABRecoveryTech;
            bool vab =
                   selectedVessel.IsRecoverable &&
                   selectedVessel.IsClearToSave() == ClearToSaveStatus.CLEAR &&
                   (selectedVessel.situation == Vessel.Situations.PRELAUNCH ||
                    string.IsNullOrEmpty(reqTech) ||
                    ResearchAndDevelopment.GetTechnologyState(reqTech) == RDTech.State.Available);

            int cnt = 2;
            bool kerbInExtSeat = KCT_Utilities.KerbalInExternalSeat(selectedVessel, true);

            if (!selectedVessel.isEVA && !kerbInExtSeat)
            {
                if (sph) cnt++;
                if (vab) cnt++;
            }

            DialogGUIBase[] options = new DialogGUIBase[cnt];
            cnt = 0;
            string msg = "Do you want KCT to do the recovery?";
            if (!selectedVessel.isEVA && !kerbInExtSeat)
            {
                if (sph)
                {
                    options[cnt++] = new DialogGUIButton("Recover to SPH", RecoverToSPH);
                }
                if (vab)
                {
                    options[cnt++] = new DialogGUIButton("Recover to VAB", RecoverToVAB);
                }
                options[cnt++] = new DialogGUIButton("Normal recovery", DoNormalRecovery);
            }
            else
            {
                msg = "KCT cannot recover if any kerbals are in external seats";
                options[cnt++] = new DialogGUIButton("Recover", DoNormalRecovery);
            }

            options[cnt] = new DialogGUIButton("Cancel", Cancel);

            MultiOptionDialog diag = new MultiOptionDialog("scrapVesselPopup", msg, "Recover Vessel", null, options: options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
        }
    }

}
