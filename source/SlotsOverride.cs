using BattleTech;
using CustomComponents;
using System.Collections.Generic;
using System.Linq;

namespace CustomSlots
{
    public interface ISlotsOverride
    {
        string SlotName { get; }
        location_record[] Locations { get; }
        def_record Default { get; }
    }


    [CustomComponent("SlotsOverride")]
    public class SlotsOverride : SimpleCustomChassis, ISlotsOverride
    {
        public string SlotName { get; set; }


        public location_record[] Locations { get; set; }

        public def_record Default { get; set; } = null;

    }
}