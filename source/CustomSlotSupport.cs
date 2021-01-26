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
        public int Support { get; set; } = 1;
        public int GetSupportAdd(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            return Support;
        }
    }
}