using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using CustomComponents;

namespace CustomSlots
{
    public class SlotDescriptor
    {
        public class location_info
        {
            public class def_info
            {
                public MechComponentDef item;
                public CustomSlotInfo info;
            }

            public ChassisLocations Location { get; set; }
            public int SlotCount { get; set; }
            public def_info[] Defaults;
        }

        public string UnitType { get; set; }

        public location_info this[ChassisLocations location] => Locations.FirstOrDefault(i => i.Location.HasFlag(location));

        public SlotTypeDescriptor Descriptor { get; set; }


        List<location_info> Locations;

        public SlotDescriptor(SlotTypeDescriptor desc, SlotInfo item)
        {
            void add_to_defs(List<location_info.def_info> list, string id)
            {

                if (UnityGameInstance.BattleTechGame.DataManager.UpgradeDefs.TryGet(id, out var item))
                {
                    var csi = item.GetComponent<CustomSlotInfo>();
                    if (csi == null)
                    {
                        Control.Instance.LogError($"{id} not slot item, cannot be used as default!");
                        return;
                    }
                    else if (csi.SlotName != desc.SlotName)
                    {
                        Control.Instance.LogError($"{id} has slot type {csi.SlotName} but used as {desc.SlotName}!");
                        return;
                    }

                    list.Add(new location_info.def_info { info = csi, item = item });
                }
                else
                    Control.Instance.LogError($"{id} not found!");

            }

            Descriptor = desc;
            UnitType = item.UnitType;
            Locations = new List<location_info>();
            foreach(var location in item.Slots)
            {
                var l = new location_info {
                    Location = location.Location,
                    SlotCount = location.Count,
                };
                var defs = new List<location_info.def_info>();
                if (location.Defaults != null)
                    foreach (var id in location.Defaults)
                        add_to_defs(defs, id);
                add_to_defs(defs, desc.DefaultItemsID);
            }
        }

    }

    public static class SlotsInfoDatabase
    {
        private static Dictionary<string, SlotDescriptor.location_info.def_info> defaults_cache;
        private static Dictionary<string, List<SlotDescriptor>> common_slots;
        private static Dictionary<string, Dictionary<string, SlotTypeDescriptor>> slots_cache;
            

        private static void BuildCommonSlots()
        {
            common_slots = new Dictionary<string, List<SlotDescriptor>>();

            foreach(var desc in Control.Instance.Settings.SlotTypes)
            {
                var list = new List<SlotDescriptor>();
                foreach (var item in desc.UnitSlots)
                    list.Add(new SlotDescriptor(desc, item));
                common_slots[desc.SlotName] = list;    
            }
           
        }
    }
}
