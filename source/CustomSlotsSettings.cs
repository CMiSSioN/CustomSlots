using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech;
using HBS.Logging;

namespace CustomSlots
{



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
        public string ErrorOverweight = "OVERWEIGHT: Used {1} of {0} carry weight";


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
