using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using HBS.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomSlots
{

    public class Slot
    {
        public ChassisLocations Location { get; set; }
        public int Count { get; set; }

        public string[] Defaults;
    }

    public class SlotInfo
    {
        public string UnitType { get; set; }
        public Slot[] Slots { get; set; }


    }

    public class SlotTypeDescriptor
    {
        public string SlotName { get; set; }
        public bool HaveSupports { get; set; } = false;

        public string AvaliableColor { get; set; } = "green";
        public string BlockedColor { get; set; } = "yellow";

        [JsonIgnore] public Color block_color;
        [JsonIgnore] public Color avail_color;

        public string DefaultItemsID { get; set; } = "Gear_Empty_Slot";

        public SlotInfo[] UnitSlots;

        public void Complete()
        {
            if (HaveSupports)
            {
                if(!ColorUtility.TryParseHtmlString(AvaliableColor, out avail_color))
                    avail_color = Color.green;

                if (!ColorUtility.TryParseHtmlString(BlockedColor, out block_color))
                    block_color = Color.yellow;
            }
        }
    }

    public class TSMTagInfo
    {
        public string Tag { get; set; }
        public float Mul { get; set; }
    }

    public class CustomSlotsSettings
    {
        public LogLevel LogLevel = LogLevel.Debug;
        public TSMTagInfo[] TSMTags = null;

        public bool Debug_IgnoreWeightFIlter = false;

        public float CarryWeightFactor = 0.1f;
        public int MaxSpecials = 2;
        public bool MultiplicativeTonnageFactor = true;

        public bool UseHandTag = true;
        public string HandsItemTag = "Hand";

        public string SpecialSlotError = "Wrong Special slot configuration, remove excess equipment or repackage mech";
        public string NotEnoughSpecialSlots = "Not enough space to install {0}";

        public string TwoHandMissed = "Mech need two free hand actuators to use {0}";
        public string OneHandMissed = "Mech need one free hand actuators to use {0}";
        public string ValidateHands = "Mech need {0} hand actuators to use installed hand helds";
        public string ValidateTonnage = "Mech need {0:0.00}t more carry weight to use installed hand helds";

        public string WrongWeightMessage = "{0} weight {1:0.00}t, mech can carry up to {2:0.00}t items";
        public string WrongWeightMessage1H = "{0} weight {1:0.00}t, mech can carry up to {2:0.00}t items in one hand";

        public string LocationLabel = "HandHeld {0:0.00}/{1:0.00}t";



        public SlotTypeDescriptor[] SlotTypes;

        public void Complete()
        {
            foreach (var slotTypeDescription in SlotTypes)
            {
                slotTypeDescription.Complete();
            }
        }
    }
}
