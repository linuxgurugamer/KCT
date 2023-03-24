using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalConstructionTime
{
    public class KCT_Recon_Rollout : IKCTBuildItem
    {
        [Persistent] private string name = "";
        [Persistent] public double BP = 0, progress = 0, cost = 0;
        [Persistent] public string associatedID = "";
        [Persistent] public string launchPadID = "LaunchPad";
        public enum RolloutReconType { Reconditioning, Rollout, Rollback, Recovery, None };
        private RolloutReconType RRTypeInternal = RolloutReconType.None;

        public RolloutReconType RRType
        {
            get
            {
                if (RRTypeInternal != RolloutReconType.None)
                    return RRTypeInternal;
                else
                {
                    if (name == "LaunchPad Reconditioning")
                        RRTypeInternal = RolloutReconType.Reconditioning;
                    else if (name == "Vessel Rollout")
                        RRTypeInternal = RolloutReconType.Rollout;
                    else if (name == "Vessel Rollback")
                        RRTypeInternal = RolloutReconType.Rollback;
                    else if (name == "Vessel Recovery")
                        RRTypeInternal = RolloutReconType.Recovery;
                    return RRTypeInternal;
                }
            }
            set
            {
                RRTypeInternal = value;
            }
        }

        public KCT_BuildListVessel associatedBLV
        {
            get
            {
                return KCT_Utilities.FindBLVesselByID(new Guid(associatedID));
            }
        }

        public KCT_KSC KSC
        {
            get
            {
                return KCT_GameStates.KSCs.FirstOrDefault(k => k.Recon_Rollout.Exists(r => r.associatedID == associatedID));
            }
        }

        public KCT_Recon_Rollout()
        {
            name = "LaunchPad Reconditioning";
            progress = 0;
            BP = 0;
            cost = 0;
            RRType = RolloutReconType.None;
            associatedID = "";
            launchPadID = "LaunchPad";
        }

        public KCT_Recon_Rollout(Vessel vessel, RolloutReconType type, string id, string launchSite)
        {
            RRType = type;
            associatedID = id;
            launchPadID = launchSite;
            KCTDebug.Log("New recon_rollout at launchsite: " + launchPadID);
            progress = 0;
            if (type == RolloutReconType.Reconditioning) 
            {
                try
                {
                    BP = KCT_MathParsing.ParseReconditioningFormula(new KCT_BuildListVessel(vessel), true);
                }
                catch
                {
                    KCTDebug.Log("Error while determining BP for recon_rollout");
                }
                finally
                {
                    name = "LaunchPad Reconditioning";
                }
            }
            else if (type == RolloutReconType.Rollout)
            {
                try
                {
                    BP = KCT_MathParsing.ParseReconditioningFormula(new KCT_BuildListVessel(vessel), false);
                }
                catch
                {
                    KCTDebug.Log("Error while determining BP for recon_rollout");
                }
                finally
                {
                    name = "Vessel Rollout";
                }
            }
            else if (type == RolloutReconType.Rollback)
            {
                try
                {
                    BP = KCT_MathParsing.ParseReconditioningFormula(new KCT_BuildListVessel(vessel), false);
                }
                catch
                {
                    KCTDebug.Log("Error while determining BP for recon_rollout");
                }
                finally
                {
                    name = "Vessel Rollback";
                    progress = BP;
                }
            }
            else if (type == RolloutReconType.Recovery)
            {
                try
                {
                    BP = KCT_MathParsing.ParseReconditioningFormula(new KCT_BuildListVessel(vessel), false);
                }
                catch
                {
                    KCTDebug.Log("Error while determining BP for recon_rollout");
                }
                finally
                {
                    name = "Vessel Recovery";
                    double KSCDistance = (float)SpaceCenter.Instance.GreatCircleDistance(SpaceCenter.Instance.cb.GetRelSurfaceNVector(vessel.latitude, vessel.longitude));
                    double maxDist = SpaceCenter.Instance.cb.Radius * Math.PI;
                    BP += BP * (KSCDistance / maxDist);
                }
            }
        }

        public KCT_Recon_Rollout(KCT_BuildListVessel vessel, RolloutReconType type, string id, string launchSite="")
        {
            RRType = type;
            associatedID = id;
            if (launchSite != "") //For when we add custom launchpads
                launchPadID = launchSite;
            else
                launchPadID = vessel.launchSite;
            //BP = vessel.GetTotalMass() * KCT_GameStates.timeSettings.ReconditioningEffect * KCT_GameStates.timeSettings.OverallMultiplier; //1 day per 50 tons (default) * overall multiplier
            //BP = KCT_MathParsing.GetStandardFormulaValue("Reconditioning", new Dictionary<string, string>() {{"M", vessel.GetTotalMass().ToString()}, {"O", KCT_PresetManager.Instance.ActivePreset.timeSettings.OverallMultiplier.ToString()},
            //    {"E", KCT_PresetManager.Instance.ActivePreset.timeSettings.ReconditioningEffect.ToString()}, {"X", KCT_PresetManager.Instance.ActivePreset.timeSettings.MaxReconditioning.ToString()}});
            //if (BP > KCT_GameStates.timeSettings.MaxReconditioning) BP = KCT_GameStates.timeSettings.MaxReconditioning;
            progress = 0;
            if (type == RolloutReconType.Reconditioning)
            {
                BP = KCT_MathParsing.ParseReconditioningFormula(vessel, true);
                //BP *= (1 - KCT_PresetManager.Instance.ActivePreset.timeSettings.RolloutReconSplit);
                name = "LaunchPad Reconditioning";
            }
            else if (type == RolloutReconType.Rollout)
            {
                BP = KCT_MathParsing.ParseReconditioningFormula(vessel, false);
                //BP *= KCT_PresetManager.Instance.ActivePreset.timeSettings.RolloutReconSplit;
                name = "Vessel Rollout";
                cost = KCT_MathParsing.ParseRolloutCostFormula(vessel);
            }
            else if (type == RolloutReconType.Rollback)
            {
                BP = KCT_MathParsing.ParseReconditioningFormula(vessel, false);
                //BP *= KCT_PresetManager.Instance.ActivePreset.timeSettings.RolloutReconSplit;
                progress = BP;
                name = "Vessel Rollback";
            }
            else if (type == RolloutReconType.Recovery)
            {
                BP = KCT_MathParsing.ParseReconditioningFormula(vessel, false);
                //BP *= KCT_PresetManager.Instance.ActivePreset.timeSettings.RolloutReconSplit;
                name = "Vessel Recovery";
                double maxDist = SpaceCenter.Instance.cb.Radius * Math.PI;
                BP += BP * (vessel.DistanceFromKSC / maxDist);
            }
        }

        public void SwapRolloutType()
        {
            if (RRType == RolloutReconType.Rollout)
            {
                RRType = RolloutReconType.Rollback;
                name = "Vessel Rollback";
            }
            else if (RRType == RolloutReconType.Rollback)
            {
                RRType = RolloutReconType.Rollout;
                name = "Vessel Rollout";
            }
        }

        public double ProgressPercent()
        {
            return Math.Round(100 * (progress / BP), 2);
        }

        public string GetItemName()
        {
            return name;
        }

        public double GetBuildRate()
        {
            double buildRate = 0;
            if (associatedBLV != null && associatedBLV.type == KCT_BuildListVessel.ListType.SPH)
                buildRate = KCT_Utilities.GetSPHBuildRateSum(KSC);
            else
                buildRate = KCT_Utilities.GetVABBuildRateSum(KSC);

            if (RRType == RolloutReconType.Rollback)
                buildRate *= -1;

            return buildRate;
        }

        public double GetTimeLeft()
        {
            double timeLeft = (BP - progress) / ((IKCTBuildItem)this).GetBuildRate();
            if (RRType == RolloutReconType.Rollback)
                timeLeft = (-progress) / ((IKCTBuildItem)this).GetBuildRate();
            return timeLeft;
        }

        public KCT_BuildListVessel.ListType GetListType()
        {
            return KCT_BuildListVessel.ListType.Reconditioning;
        }

        public bool IsComplete()
        {
            bool complete = progress >= BP;
            if (RRType == RolloutReconType.Rollback)
                complete = progress <= 0;
            return complete;
        }

        public void IncrementProgress(double UTDiff)
        {
            double progBefore = progress;
            progress += GetBuildRate() * UTDiff;
            if (progress > BP) progress = BP;

            if (KCT_Utilities.CurrentGameIsCareer() && RRType == RolloutReconType.Rollout && cost > 0)
            {
                int steps;
                if ((steps = (int)(Math.Floor(progress / BP * 10) - Math.Floor(progBefore / BP * 10))) > 0) //passed 10% of the progress
                {
                    if (Funding.Instance.Funds < cost / 10) //If they can't afford to continue the rollout, progress stops
                    {
                        progress = progBefore;
                        if (TimeWarp.CurrentRate > 1f && KCT_GameStates.warpInitiated && this == KCT_GameStates.targetedItem)
                        {
                            ScreenMessages.PostScreenMessage(LocalCache.str_Messages_NoFundsToWarp); // "Timewarp was stopped because there's insufficient funds to continue the rollout"
                            TimeWarp.SetRate(0, true);
                            KCT_GameStates.warpInitiated = false;
                        }
                    }
                    else
                        KCT_Utilities.SpendFunds(cost / 10 * steps, TransactionReasons.VesselRollout);
                }
            }
        }
    }
}
