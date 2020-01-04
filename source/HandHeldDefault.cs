using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomComponents;
using BattleTech;

namespace HandHeld
{
    [CustomComponent("HandHeldDefault")]
    public class HandHeldDefault : SimpleCustomChassis
    {
        public string Item1HID1 = null;
        public string Item1HID2 = null;
        public string Item2HID = null;

        public ComponentType Type1 = ComponentType.Upgrade;
        public ComponentType Type2 = ComponentType.Upgrade;
        public ComponentType TypeH = ComponentType.Upgrade;

    }
}
