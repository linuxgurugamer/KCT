using System;
using System.Linq;

namespace KerbalConstructionTime
{
    public class KCT_AirlaunchPrep : IKCTBuildItem
    {
        [Persistent] private string name = string.Empty;
        [Persistent] public double BP = 0, progress = 0, cost = 0;
        [Persistent] public string associatedID = string.Empty;

        public const string Name_Mount = "Mounting to carrier";
        public const string Name_Unmount = "Unmounting";

        public enum PrepDirection { Mount, Unmount };
        public PrepDirection Direction = PrepDirection.Mount;

        public KCT_BuildListVessel AssociatedBLV
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
                return KCT_GameStates.KSCs.FirstOrDefault(k => k.AirlaunchPrep.Exists(r => r.associatedID == associatedID));
            }
        }

        public KCT_AirlaunchPrep()
        {
            name = Name_Mount;
            progress = 0;
            BP = 0;
            cost = 0;
            Direction = PrepDirection.Mount;
            associatedID = "";
        }

        public KCT_AirlaunchPrep(KCT_BuildListVessel vessel, string id)
        {
            Direction = PrepDirection.Mount;
            associatedID = id;
            progress = 0;

            BP = KCT_MathParsing.ParseAirlaunchTimeFormula(vessel);
            cost = KCT_MathParsing.ParseAirlaunchCostFormula(vessel);
            name = Name_Mount;
        }

        public double GetBuildRate()
        {
            double buildRate = KCT_Utilities.GetSPHBuildRateSum(KSC);

            if (Direction == PrepDirection.Unmount)
                buildRate *= -1;

            return buildRate;
        }

        public string GetItemName()
        {
            return name;
        }

        public KCT_BuildListVessel.ListType GetListType()
        {
            return KCT_BuildListVessel.ListType.SPH;
        }

        public double GetTimeLeft()
        {
            double timeLeft = (BP - progress) / GetBuildRate();
            if (Direction == PrepDirection.Unmount)
                timeLeft = (-progress) / GetBuildRate();
            return timeLeft;
        }

        public bool IsComplete()
        {
            bool complete = progress >= BP;
            if (Direction == PrepDirection.Unmount)
                complete = progress <= 0;
            return complete;
        }

        public void IncrementProgress(double UTDiff)
        {
            double progBefore = progress;
            progress += GetBuildRate() * UTDiff;
            if (progress > BP) progress = BP;

            if (KCT_Utilities.CurrentGameIsCareer() && Direction == PrepDirection.Mount && cost > 0)
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

        public void SwitchDirection()
        {
            if (Direction == PrepDirection.Mount)
            {
                Direction =  PrepDirection.Unmount;
                name = Name_Unmount;
            }
            else
            {
                Direction = PrepDirection.Mount;
                name = Name_Mount;
            }
        }
    }
}
