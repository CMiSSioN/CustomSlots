using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;

namespace HandHeld
{
    [CustomComponent("TSMInfo")]
    public class TSMInfoComponent : SimpleCustomComponent
    {
        public float HandHeldFactor = 2;
    }

    [CustomComponent("AddCarryWeight")]
    public class AddCarryWeight : SimpleCustomComponent, IAddTonnage
    {
        public float AddTonnage = 0;
        public float GetAddTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            return AddTonnage;
        }
    }
}
