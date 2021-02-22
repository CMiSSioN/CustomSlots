using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace CustomSlots
{
    [CustomComponent("TSMInfo")]
    public class CarryWeightCustoms : SimpleCustomComponent
    {
        public float HandHeldFactor = 2;
    }

    [CustomComponent("AddCarryWeight")]
    public class AddCarryWeight : SimpleCustomComponent, IAddTonnage
    {
        public float AddTonnage = 0;
        
        public float GetAddTonnage(MechDef mech, IEnumerable<InvItem> inventory)
        {
            return AddTonnage;
        }

        public float GetAddTonnage(MechDef mech)
        {
            return AddTonnage;
        }
    }

    [CustomComponent("UseCarryWeight")]
    public class UseCarryWeight : SimpleCustomComponent, IUseTonnage
    {
        public float UseTonnage { get; set; } = 0;
        public float GetTonnage(MechDef mech, IEnumerable<InvItem> inventory)
        {
            return UseTonnage;
        }

        public float GetTonnage(MechDef mech)
        {
            return UseTonnage;
        }
    }
}
