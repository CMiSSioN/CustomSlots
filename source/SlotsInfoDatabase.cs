using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech.Data;
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
                public IUseSlots info;
            }

            public ChassisLocations Location { get; set; }
            public int SlotCount { get; set; }
            public def_info[] Defaults;
        }

        public string UnitType { get; set; }

        public location_info this[ChassisLocations location] => Locations.FirstOrDefault(i => i.Location.HasFlag(location));

        public SlotTypeDescriptor Descriptor { get; set; }


        List<location_info> Locations;

        private void add_to_defs(List<location_info.def_info> list, def_record def)
        {
            var item = SlotsInfoDatabase.GetDefault(def);
            if (item != null)
                list.Add(item);
        }

        public SlotDescriptor(SlotTypeDescriptor desc, SlotInfo item)
        {


            Descriptor = desc;
            UnitType = item.UnitType;
            Locations = new List<location_info>();
            foreach (var location in item.Slots)
            {
                var l = new location_info
                {
                    Location = location.Location,
                    SlotCount = location.Count,
                };
                var defs = new List<location_info.def_info>();
                if (location.Defaults != null)
                    foreach (var definfo in location.Defaults)
                        add_to_defs(defs, definfo);
                add_to_defs(defs, desc.DefaultItem);
                l.Defaults = defs.ToArray();
                Locations.Add(l);
            }
        }

        public SlotDescriptor(SlotDescriptor source, ISlotsOverride slot_ovveride)
        {
            def_record defid;
            UnitType = "CustomOverrideUnit";

            if (source == null)
            {
                defid = slot_ovveride.Default;
                Descriptor = Control.Instance.Settings.SlotTypes.FirstOrDefault(i => i.SlotName == slot_ovveride.SlotName);
            }
            else
            {
                Descriptor = source.Descriptor;

                defid = slot_ovveride.Default ?? source.Descriptor.DefaultItem;
            }


            if (slot_ovveride.Locations == null && slot_ovveride.Default == null)
            {
                if (source != null)
                    Locations = source.Locations;
            }

            else if (slot_ovveride.Locations == null && slot_ovveride.Default != null)
            {
                Locations = new List<location_info>();

                if (source?.Locations != null)
                    foreach (var locationInfo in source.Locations)
                    {
                        var l = new location_info
                        {
                            Location = locationInfo.Location,
                            SlotCount = Locations.Count,
                        };
                        var defs = new List<location_info.def_info>();
                        for (int i = 0; i < locationInfo.Defaults.Length - 1; i++)
                            defs.Add(locationInfo.Defaults[i]);
                        add_to_defs(defs, defid);
                        l.Defaults = defs.ToArray();
                        Locations.Add(l);
                    }
            }
            else if (slot_ovveride.Locations != null)
            {
                Locations = new List<location_info>();
                foreach (var location in slot_ovveride.Locations)
                {
                    var l = new location_info
                    {
                        Location = location.Location,
                        SlotCount = location.Count,
                    };
                    var defs = new List<location_info.def_info>();
                    if (location.Defaults != null)
                        foreach (var definfo in location.Defaults)
                            add_to_defs(defs, definfo);
                    add_to_defs(defs, defid);
                    l.Defaults = defs.ToArray();
                    Locations.Add(l);
                }
            }
        }
    }

    public static class SlotsInfoDatabase
    {
        private static Dictionary<string, SlotDescriptor.location_info.def_info> defaults_cache = null;
        private static Dictionary<string, List<SlotDescriptor>> common_slots = null;
        private static Dictionary<string, Dictionary<string, SlotDescriptor>> slots_cache = null;

        private static void BuildCommonSlots()
        {
            foreach (var desc in Control.Instance.Settings.SlotTypes)
            {
                var list = new List<SlotDescriptor>();
                foreach (var item in desc.UnitSlots)
                    list.Add(new SlotDescriptor(desc, item));
                common_slots[desc.SlotName] = list;
            }

        }

        public static SlotDescriptor.location_info.def_info GetDefault(def_record def)
        {
            if (defaults_cache.TryGetValue(def.id, out var result))
            {
                return result;
            }

            MechComponentDef item;

            var dm = UnityGameInstance.BattleTechGame.DataManager;
            switch (def.type)
            {
                case ComponentType.Weapon:
                    dm.WeaponDefs.TryGet(def.id, out var weapon);
                    item = weapon;
                    break;
                case ComponentType.AmmunitionBox:
                    dm.AmmoBoxDefs.TryGet(def.id, out var ammobox);
                    item = ammobox;
                    break;
                case ComponentType.HeatSink:
                    dm.HeatSinkDefs.TryGet(def.id, out var hs);
                    item = hs;
                    break;
                case ComponentType.JumpJet:
                    dm.JumpJetDefs.TryGet(def.id, out var jj);
                    item = jj;
                    break;
                case ComponentType.Upgrade:
                    dm.UpgradeDefs.TryGet(def.id, out var upgrade);
                    item = upgrade;
                    break;
                default:
                    CustomComponents.Control.LogError($"Cannot find {def.id} of type {def.type}");
                    return null;
            }

            var csi = item.GetComponent<IUseSlots>();
            if (csi == null)
            {
                Control.Instance.LogError($"{def.id} not slot item, cannot be used as default!");
                return null;
            }

            var info = new SlotDescriptor.location_info.def_info { info = csi, item = item };
            defaults_cache[def.id] = info;
            return info;
        }

        public static SlotDescriptor GetMechInfoByType(MechDef mech, string slot_type)
        {
            var dictionary = GetMechInfo(mech);
            if (dictionary.TryGetValue(slot_type, out var result))
                return result;
            else
                Control.Instance.LogError($"{slot_type} not valid Slot Type");

            return null;
        }

        public static Dictionary<string, SlotDescriptor> GetMechInfo(MechDef mech)
        {
            if (mech == null)
                return null;

            var id = mech.Description.Id;

            if (common_slots == null)
                Init();

            if (slots_cache.TryGetValue(id, out var result))
                return result;

            FillMechInfo(mech);

            return slots_cache[id];
        }

        private static void FillMechInfo(MechDef mech)
        {
            var dictionary = new Dictionary<string, SlotDescriptor>();
            foreach (var slotTypeDescriptor in Control.Instance.Settings.SlotTypes)
            {
                var slotName = slotTypeDescriptor.SlotName;
                var slot_override = CustomSlotControler.GetSlotsOverride(mech, slotName);
                var list = common_slots[slotName];
                var unittypes = CustomComponents.UnitTypeDatabase.Instance.GetUnitTypes(mech);
                SlotDescriptor common = null;
                foreach (var item in list)
                {
                    if (unittypes.Contains(item.UnitType))
                        common = item;
                }

                if (slot_override == null)
                    dictionary[slotName] = common;
                else
                    dictionary[slotName] = new SlotDescriptor(common, slot_override);
            }

            slots_cache[mech.Description.Id] = dictionary;
        }

        private static void Init()
        {
            defaults_cache = new Dictionary<string, SlotDescriptor.location_info.def_info>();
            common_slots = new Dictionary<string, List<SlotDescriptor>>();
            slots_cache = new Dictionary<string, Dictionary<string, SlotDescriptor>>();
            BuildCommonSlots();
        }
    }
}
