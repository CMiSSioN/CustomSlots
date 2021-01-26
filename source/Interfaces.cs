using System.Collections.Generic;
using BattleTech;

namespace CustomSlots
{
    public interface IUseTonnage
    {
        public float GetTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory);
    }

    public interface IAddTonnage
    {
        public float GetAddTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory);
    }

    public interface IUseSlots
    {
        public string SlotName { get; }
        public int GetSlotsUsed(MechDef mech, IEnumerable<MechComponentRef> inventory);
        public int GetSupportUsed(MechDef mech, IEnumerable<MechComponentRef> inventory);
    }

    public interface ISlotSupport
    {
        public string SlotName { get; }
        public ChassisLocations Location { get; }
        public int GetSupportAdd(MechDef mech, IEnumerable<MechComponentRef> inventory);

    }

   


}