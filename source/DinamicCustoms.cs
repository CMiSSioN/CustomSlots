using System.Collections.Generic;
using BattleTech;
using CustomComponents;

namespace CustomSlots
{
    [CustomComponent("SlotExtention")]
    public class CustomSlotExtention : SimpleCustomComponent, IUseSlots 
    {
        public string SlotName { get; set; }
        public int GetSlotsUsed(MechDef mech)
        {
            return 1;
        }

        public int GetSupportUsed(MechDef mech)
        {
            return UseSupport ? 1 : 0;
        }

        public bool UseSupport { get; set; } = true;

        public int GetSlotsUsed(MechDef mech, IEnumerable<inventory_item> inventory)
        {
            return 1;
        }

        public int GetSupportUsed(MechDef mech, IEnumerable<inventory_item> inventory)
        {
            return UseSupport ? 1 : 0;
        }
    }

    public abstract class CustomSlotDynamic :  CustomSlotInfo
    {
        public string ExtentionID { get; set; }

        public abstract int ExtentionCount(MechDef mech, IEnumerable<inventory_item> inventory);
    }
}