using System;
using System.Collections.Generic;
using BattleTech;
using BattleTech.UI;
using HBS.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace HandHeld
{
    public class TSMInfo
    {
        public string Tag { get; set; }
        public float Mul { get; set; }
    }

    public class HandHeldSettings
    {
        public LogLevel LogLevel = LogLevel.Debug;
        public TSMInfo[] TSMTags = null;

        public bool Debug_IgnoreWeightFIlter = false;

        public float CarryWeightFactor = 0.05f;
        public bool MultiplicativeTonnageFactor = true;

        //public bool UseHandTag = true;
        public string HandsItemTag = "Hand";
        public string WrongWeightMessage = "{1:0.00}t requre to carry {0}, {2:0.00} left free";
        public string TwoHandMissed = "Mech need two free hand actuators to use {0}";
        public string OneHandMissed = "Mech need one free hand actuators to use {0}";
        public string ValidateHands = "Mech need {0} free hand actuators to use hand helds";
        public string ValidateTonnage = "Mech need {0:0.00}t more carry weight to use hand helds";
        public string LocationLabel = "HandHeld {0:0.00}/{1:0.00}t";
    }

}
