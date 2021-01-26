using BattleTech;
using CustomComponents;
using System.Collections.Generic;
using System.Linq;

namespace CustomSlots
{

    public class def_record
    {
        public string id;
        public ComponentType type;
    }

    public class location_record
    {
        public ChassisLocations Location;
        public int MaxSlots = -1;
        public def_record[] Defaults;
    }

    public interface ISpecialSlotDefaults
    {
        public string SlotName { get; }

        public Dictionary<ChassisLocations, location_record> LocationDict { get; }
        public string DefaultID { get; }
    }


    [CustomComponent("SlotsOverride")]
    public class SlotsOverride : SimpleCustomChassis, ISpecialSlotDefaults, IAfterLoad
    {
        public string SlotName { get; set; }

        public Dictionary<ChassisLocations, location_record> LocationDict { get; set; }

        public location_record[] Locations;

        public string DefaultID { get; set; } = null;

        public void OnLoaded(Dictionary<string, object> values)
        {
            LocationDict = (Locations == null || Locations.Length == 0) ?
                new Dictionary<ChassisLocations, location_record>() :
                Locations.ToDictionary(i => i.Location);

        }
    }
}