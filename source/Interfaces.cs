using System.Collections.Generic;
using BattleTech;

namespace CustomSlots
{
    public class inventory_item
    {
        public MechComponentRef item { get; set; }
        public ChassisLocations location { get; set; }
    }

    public interface IUseTonnage
    {
        public float GetTonnage(MechDef mech, IEnumerable<inventory_item> inventory);
        public float GetTonnage(MechDef mech);
    }

    public interface IAddTonnage
    {
        public float GetAddTonnage(MechDef mech, IEnumerable<inventory_item> inventory);
        public float GetAddTonnage(MechDef mech);
    }

    public interface IUseSlots
    {
        public string SlotName { get; }
        public int GetSlotsUsed(MechDef mech);
        public int GetSupportUsed(MechDef mech);
        public int GetSlotsUsed(MechDef mech, IEnumerable<inventory_item> inventory);
        public int GetSupportUsed(MechDef mech, IEnumerable<inventory_item> inventory);
    }

    public interface ISlotSupport
    {
        public string SlotName { get; }
        public ChassisLocations Location { get; }
        public int GetSupportAdd(MechDef mech, IEnumerable<inventory_item> inventory);
        public int GetSupportAdd(MechDef mech);

    }




}