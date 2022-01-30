using System.Collections.Generic;
using BattleTech;
using CustomComponents;

namespace CustomSlots
{
    //    public class inventory_item
    //    {
    //        public MechComponentRef item { get; set; }
    //        public ChassisLocations location { get; set; }
    //    }

    public interface IUseTonnage
    {
        float GetTonnage(MechDef mech, IEnumerable<InvItem> inventory);
        float GetTonnage(MechDef mech);
    }

    public interface IAddTonnage
    {
        float GetAddTonnage(MechDef mech, IEnumerable<InvItem> inventory);
        float GetAddTonnage(MechDef mech);
    }

    public interface IUseSlots
    {
        string SlotName { get; }
        int GetSlotsUsed(MechDef mech);
        int GetSupportUsed(MechDef mech);
        int GetSlotsUsed(MechDef mech, IEnumerable<InvItem> inventory);
        int GetSupportUsed(MechDef mech, IEnumerable<InvItem> inventory);
    }

    public interface ISlotSupport
    {
        string SlotName { get; }
        ChassisLocations Location { get; }
        int GetSupportAdd(MechDef mech, IEnumerable<InvItem> inventory);
        int GetSupportAdd(MechDef mech);
    }

}