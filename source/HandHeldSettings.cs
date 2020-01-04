using System;
using System.Collections.Generic;
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

    public class HandHeldSettings
    {
        public LogLevel LogLevel = LogLevel.Debug;
        public TSMTagInfo[] TSMTags = null;

        public bool Debug_IgnoreWeightFIlter = false;

        public float CarryWeightFactor = 0.1f;
        public bool MultiplicativeTonnageFactor = true;

        public bool UseHandTag = true;
        public string HandsItemTag = "Hand";

        public string HandHeldSlotItemID = "Gear_HandHeld_Slot";

        public string TwoHandMissed = "Mech need two free hand actuators to use {0}";
        public string OneHandMissed = "Mech need one free hand actuators to use {0}";
        public string ValidateHands = "Mech need {0} free hand actuators to use installed hand helds";
        public string ValidateTonnage = "Mech need {0:0.00}t more carry weight to use installed hand helds";

        public string WrongWeightMessage = "{0} weight {1:0.00}t, mech can carry up to {2:0.00}t items";
        public string WrongWeightMessage1H = "{0} weight {1:0.00}t, mech can carry up to {2:0.00}t items in one hand";

        public string LocationLabel = "HandHeld {0:0.00}/{1:0.00}t";
    }

}
