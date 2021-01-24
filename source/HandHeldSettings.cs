using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using HBS.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace HandHeld
{
    public class TSMTagInfo
    {
        public string Tag { get; set; }
        public float Mul { get; set; }
    }

    public class HHSlotInfo
    {
        public class Slot
        {
            public ChassisLocations Location { get; set; }
            public int Count { get; set; }
        }

        public string UnitType { get; set; }
        public Slot[] Slots { get; set; }
    }

    public class SpSlotInfo
    {
        public string UnitType { get; set; }
        public int SpecialSlotCount = 2;
    }

    public class HandHeldSettings
    {
        public LogLevel LogLevel = LogLevel.Debug;
        public TSMTagInfo[] TSMTags = null;

        public bool Debug_IgnoreWeightFIlter = false;

        public float CarryWeightFactor = 0.1f;
        public int MaxSpecials = 2;
        public bool MultiplicativeTonnageFactor = true;

        public bool UseHandTag = true;
        public string HandsItemTag = "Hand";

        public string HandHeldSlotItemID = "Gear_HandHeld_Slot";
        public string SpecialDefaultSlotID = "Gear_Special_Slot";

        public string SpecialSlotError = "Wrong Special slot configuration, remove excess equipment or repackage mech";
        public string NotEnoughSpecialSlots = "Not enough space to install {0}";

        public string TwoHandMissed = "Mech need two free hand actuators to use {0}";
        public string OneHandMissed = "Mech need one free hand actuators to use {0}";
        public string ValidateHands = "Mech need {0} hand actuators to use installed hand helds";
        public string ValidateTonnage = "Mech need {0:0.00}t more carry weight to use installed hand helds";

        public string WrongWeightMessage = "{0} weight {1:0.00}t, mech can carry up to {2:0.00}t items";
        public string WrongWeightMessage1H = "{0} weight {1:0.00}t, mech can carry up to {2:0.00}t items in one hand";

        public string LocationLabel = "HandHeld {0:0.00}/{1:0.00}t";



        public HHSlotInfo[] HHSlotDefs =
        {
            new HHSlotInfo
            {
                UnitType = "Mech",
                Slots = new HHSlotInfo.Slot[]
                {
                    new HHSlotInfo.Slot {Location = ChassisLocations.LeftArm, Count = 1},
                    new HHSlotInfo.Slot {Location = ChassisLocations.RightArm, Count = 1}

                }
            }
        };



        public SpSlotInfo[] SpSlotDefs =
        {
            new SpSlotInfo() { UnitType = "Mech", SpecialSlotCount = 2 },
            new SpSlotInfo() { UnitType = "Mech", SpecialSlotCount = 0 }
        };

        public SpSlotInfo GetSpSlotInfo(MechDef mech)
        {
            var types = CustomComponents.UnitTypeDatabase.Instance.GetUnitTypes(mech);
            if (types == null || types.Length == 0)
                return null;

            foreach (var slotInfo in SpSlotDefs)
            {
                if (types.Contains(slotInfo.UnitType))
                    return slotInfo;
            }

            return null;
        }


        public HHSlotInfo GetHHSlotInfo(MechDef mech)
        {
            var types = CustomComponents.UnitTypeDatabase.Instance.GetUnitTypes(mech);
            if (types == null || types.Length == 0)
                return null;

            foreach (var slotInfo in HHSlotDefs)
            {
                if (types.Contains(slotInfo.UnitType))
                    return slotInfo;
            }

            return null;
        }
    }
}
