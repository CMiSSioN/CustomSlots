using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using HBS.Logging;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomSlots
{

    public class def_record
    {
        public string id;
        public ComponentType type;
    }

    public class location_record
    {
        public ChassisLocations Location { get; set; }
        public int Count { get; set; } = 1;

        public def_record[] Defaults;
    }

    public class SlotInfo
    {
        public string UnitType { get; set; }
        public location_record[] Slots { get; set; }
    }

    public class SlotTypeDescriptor
    {
        public string SlotName { get; set; }
        public bool HaveSupports { get; set; } = false;

        public string SlotsErrorName { get; set; } = "Slots";
        public string SupportsErrorName { get; set; } = "Supports";

        public string AvaliableColor { get; set; } = "green";
        public string BlockedColor { get; set; } = "yellow";

        [JsonIgnore] public Color block_color;
        [JsonIgnore] public Color avail_color;

        public def_record DefaultItem { get; set; } = new def_record()
        { id = "Gear_Slot_Default", type = ComponentType.Upgrade };

        public SlotInfo[] UnitSlots;

        public void Complete()
        {
            if (HaveSupports)
            {
                if (!ColorUtility.TryParseHtmlString(AvaliableColor, out avail_color))
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


        public bool RunAutofixer = true;
        public bool QuickAutofix = true;

        public string ErrorNotEnoughSlots = "Not enough {0} installed  in {1}, try repackage mech to fix";
        public string ErrorTooManySlots = "Too many {0} installed  in {1}";
        public string ErrorNotEnoughSupport = "Need more {0) for installed equipment in {1}";

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
