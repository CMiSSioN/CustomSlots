using System.Collections.Generic;
using BattleTech;

namespace HandHeld
{
    public interface IUseTonnage
    {
        public float GetTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory);
    }

    public interface IUseSlots
    {
        public int GetSlotsUsed(MechDef mech, IEnumerable<MechComponentRef> inventory);
    }

    public interface IAddTonnage
    {
        public float GetAddTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory);
    }
}