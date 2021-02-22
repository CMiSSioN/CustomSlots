using System.Collections.Generic;
using BattleTech;
using CustomComponents;

namespace CustomSlots
{
    [CustomComponent("SlotSupport", true)]
    public class CustomSlotSupport : SimpleCustomComponent, ISlotSupport
    {
        public string SlotName { get; set; }
        public ChassisLocations Location { get; set; } = ChassisLocations.None;
        public int GetSupportAdd(MechDef mech, IEnumerable<InvItem> inventory)
        {
            return Support;
        }

        public int GetSupportAdd(MechDef mech)
        {
            return Support;
        }

        public int Support { get; set; } = 1;
    }
}