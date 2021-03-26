using UnityEngine;
using KSP.UI.Screens;
using UnityEngine.UI;

namespace KerbalConstructionTime
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KCT_Flight : KerbalConstructionTime
    {
        public Button.ButtonClickedEvent originalCallback;

        public new void Start()
        {
            base.Start();
            if (KCT_GUI.PrimarilyDisabled)
                return;
            KCTDebug.Log("KCT_Flight, Start");
            var altimeter = UnityEngine.Object.FindObjectOfType<AltimeterSliderButtons>();
            if (altimeter != null)
            {
                originalCallback = altimeter.vesselRecoveryButton.onClick;

                altimeter.vesselRecoveryButton.onClick = new Button.ButtonClickedEvent();
                altimeter.vesselRecoveryButton.onClick.AddListener(RecoverVessel);
            }
        }

        public void RecoverToVAB()
        {
            if (!KCT_Utilities.RecoverActiveVesselToStorage(KCT_BuildListVessel.ListType.VAB))
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "vesselRecoverErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);
            }
        }

        public void RecoverToSPH()
        {
            if (!KCT_Utilities.RecoverActiveVesselToStorage(KCT_BuildListVessel.ListType.SPH))
            {
                PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "recoverShipErrorPopup", "Error!", "There was an error while recovering the ship. Sometimes reloading the scene and trying again works. Sometimes a vessel just can't be recovered this way and you must use the stock recover system.", "OK", false, HighLogic.UISkin);
            }
        }

        public void DoNormalRecovery()
        {
            originalCallback.Invoke();
            return;
        }
        public void Cancel()
        {
            return;
        }

        public void RecoverVessel()
        {
            bool sph = (FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.IsRecoverable && FlightGlobals.ActiveVessel.IsClearToSave() == ClearToSaveStatus.CLEAR);
            bool vab = KCT_Utilities.IsVabRecoveryAvailable();

            int cnt = 2;
            bool kerbInExtSeat = KCT_Utilities.KerbalInExternalSeat(FlightGlobals.ActiveVessel);
            if (!FlightGlobals.ActiveVessel.isEVA && !kerbInExtSeat)
            {
                if (sph) cnt++;
                if (vab) cnt++;
            }
            DialogGUIBase[] options = new DialogGUIBase[cnt];
            cnt = 0;
            string msg = "Do you want KCT to do the recovery?";

            if (!FlightGlobals.ActiveVessel.isEVA)
            {
                if (!kerbInExtSeat)
                {
                    if (sph)
                    {
                        options[cnt++] = new DialogGUIButton("Recover to SPH", RecoverToSPH);
                    }
                    if (vab)
                    {
                        options[cnt++] = new DialogGUIButton("Recover to VAB", RecoverToVAB);
                    }
                } 
                else
                {
                    msg = "KCT cannot recover if any kerbals are in external seats";
                }
                options[cnt++] = new DialogGUIButton("Normal recovery", DoNormalRecovery);
            } 
            else
                options[cnt++] = new DialogGUIButton("Recover", DoNormalRecovery);
            options[cnt] = new DialogGUIButton("Cancel", Cancel);

            MultiOptionDialog diag = new MultiOptionDialog("scrapVesselPopup", 
                msg, 
                "Kerbal Construction Time (KCT)", 
                null, options: options);
            PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), diag, false, HighLogic.UISkin);
        }
    }

}
