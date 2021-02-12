using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using BattleTech.UI;
using CustomComponents;
using FluffyUnderware.DevTools.Extensions;
using JetBrains.Annotations;
using Localize;
using Steamworks;

namespace CustomSlots
{
    public static partial class CustomSlotControler
    {
        public static readonly ChassisLocations[] all_locations = new ChassisLocations[]
        {
            ChassisLocations.LeftArm,
            ChassisLocations.RightArm,
            ChassisLocations.Head,
            ChassisLocations.LeftTorso,
            ChassisLocations.RightTorso,
            ChassisLocations.CenterTorso,
            ChassisLocations.LeftLeg,
            ChassisLocations.RightLeg
        };

        public class defrecord
        {
            public MechComponentRef item;
            public CustomSlotInfo si;
        }

        public static int GetSupportsForLocation(MechDef mech, string slotname, ChassisLocations target, IEnumerable<inventory_item> inventory = null)
        {
            int get_sort(ChassisLocations locations)
            {
                // support is only target location;
                if (locations == target)
                    return 3;
                // support is have target location as option
                if (locations.HasFlag(target))
                    return 2;
                // support is single non target location
                if (all_locations.Contains(locations))
                    return 0;
                // support is multy non target location
                return 1;
            }

            if (inventory == null)
                inventory = mech.Inventory.ToInventory();

            var supports = inventory.Where(i => i.item.IsSupport(slotname)).Select(i => new
            {
                item = i.item,
                location = i.location,
                support = i.item.GetSupport(slotname)
            })
                .Select(i => new
                {
                    count = i.support.GetSupportAdd(mech, inventory),
                    location = i.support.Location == ChassisLocations.None ? i.location : i.support.Location,
                    sort = get_sort(i.support.Location == ChassisLocations.None ? i.location : i.support.Location)
                })
                .OrderBy(i => i.sort)
                .ToList();

            if (supports.Count == 0)
                return 0;

            var slots = inventory
                .Select(i => new { loc = i.location, slot = i.item.GetComponent<IUseSlots>() })
                .Where(i => i.slot != null && i.slot.SlotName == slotname)
                .GroupBy(i => i.loc)
                .Select(i => new
                { loc = i.Key, count = i.Sum(i => i.slot.GetSupportUsed(mech, inventory)), sort = i.Key == target ? 1 : 0 })
                .Where(i => i.count > 0)
                .OrderBy(i => i.sort).ToList();

            foreach (var slot in slots)
            {
                int used = slot.count;

                for (int i = 0; i < supports.Count; i++)
                {
                    while (used > 0 && i < supports.Count)
                        if (supports[i].location.HasFlag(slot.loc))
                            if (used >= supports[i].count)
                            {
                                used -= supports[i].count;
                                supports.RemoveAt(i);
                            }
                            else
                            {
                                var t = supports[i];
                                supports[i] = new { count = t.count - used, location = t.location, sort = t.sort };
                                used = 0;
                            }

                    if (used == 0)
                        break;
                }

                if (used > 0 && slot.sort == 1)
                    return -used;
            }

            return supports.Where(i => i.sort >= 2).Sum(i => i.count);
        }

        public class slot_record
        {
            public MechComponentRef item { get; set; }
            public IUseSlots slot { get; set; }
            public int slot_used { get; set; }
            public int sup_used { get; set; }
            public bool is_default { get; set; }
        }


        public class extention_record
        {
            public string id { get; set; }
            public int need { get; set; }
            public int have { get; set; }
        }

        public class free_record
        {
            public ChassisLocations location { get; set; }
            public int free_slots { get; set; }
            public int free_supports { get; set; }
        }

        public static Dictionary<ChassisLocations, free_record> get_free_slots(MechDef mech, ChassisLocations base_location, bool use_support, string SlotName)
        {
            var result = new Dictionary<ChassisLocations, free_record>();
            var info = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);

            foreach (var linfo in info)
            {


                result[linfo.Location] = new free_record
                {
                    location = linfo.Location,
                    free_slots = linfo.SlotCount,
                    free_supports = use_support ? CustomSlotControler.GetSupportsForLocation(mech, SlotName, linfo.Location) : 0
                };
            }

            var slots = mech.Inventory
                .Select(i => new { item = i, s = i.GetComponent<IUseSlots>() })
                .Where(i => i.s?.SlotName == SlotName)
                .Where(i => !i.item.IsDefault() || i.item.Is<CustomSlotExtention>());

            foreach (var item in slots)
                if (result.TryGetValue(item.item.MountedLocation, out var record))
                    record.free_slots -= item.s.GetSlotsUsed(mech);

            return result;
        }


        public static List<extention_record> GetExtentions(MechDef mech,
            IEnumerable<inventory_item> inventory = null)
        {

            var inv = inventory == null ? mech.Inventory.ToInventory().ToArray() : inventory.ToArray();

            var result = inv.Select(i => new
            {
                din = i.item.GetComponent<CustomSlotDynamic>(),
                ext = i.item.GetComponent<CustomSlotExtention>()
            })
                .Where(i => i.din != null || i.ext != null)
                .Select(i => new extention_record
                {
                    id = i.din == null ? i.ext.Def.Description.Id : i.din.ExtentionID,
                    need = i.ext == null ? 0 : 1,
                    have = i.din == null ? 0 : i.din.ExtentionCount(mech, inv)
                })
                .GroupBy(i => i.id)
                .Select(i => i.Aggregate(
                        new extention_record { id = i.Key, have = 0, need = 0 },
                        (total, next) =>
                        {
                            total.need += next.need;
                            total.have += next.need;
                            return total;
                        })
                    )
                .ToList();

            return result;

        }

        internal static void AutoFixMech(List<MechDef> mechDefs, SimGameState simgame)
        {
            foreach (var mechDef in mechDefs)
            {
                var dinamics = GetExtentions(mechDef);
                AdjustDynamics(mechDef, dinamics);


                foreach (var slotType in Control.Instance.Settings.SlotTypes)
                {
                    var sinfo = SlotsInfoDatabase.GetMechInfoByType(mechDef, slotType.SlotName);

                    if (Control.Instance.Settings.QuickAutofix)
                    {
                        var total_slots = all_locations
                            .Select(i => sinfo[i])
                            .Where(i => i != null)
                            .Sum(i => i.SlotCount);
                        var used_slots = mechDef.Inventory
                            .Where(i => i.IsSlot(slotType.SlotName))
                            .Select(i => i.GetComponent<IUseSlots>())
                            .Sum(i => i.GetSlotsUsed(mechDef, mechDef.Inventory.ToInventory()));

                        var need_fix = total_slots != used_slots;
                        if (slotType.HaveSupports)
                        {
                            var total_support = mechDef.Supports(slotType.SlotName);
                            var used_support = mechDef.Inventory
                                .Where(i => i.IsSlot(slotType.SlotName))
                                .Select(i => i.GetComponent<IUseSlots>())
                                .Sum(i => i.GetSupportUsed(mechDef, mechDef.Inventory.ToInventory()));

                            need_fix = need_fix || total_support < used_support;
                        }

                        if (!need_fix)
                            continue;
                    }

                    var inv = mechDef.Inventory.ToList();

                    AdjustMechDefaults(mechDef, simgame, slotType.SlotName, inv);
                    mechDef.SetInventory(inv.ToArray());
                }
            }
        }

        private static ChassisLocations AdjustDynamics(MechDef mechDef, List<extention_record> dinamics, SimGameState simgame,
            List<MechComponentRef> inv)
        {
            ChassisLocations result = ChassisLocations.None;
            foreach (var er in dinamics)
            {
                if (er.need == er.have)
                    continue;

                foreach (var mechComponentRef in inv.Where(i => i.ComponentDefID == er.id))
                    result.Set(mechComponentRef.MountedLocation);

                inv.RemoveAll(i => i.ComponentDefID == er.id);
                foreach (var item in inv.Where(i => i.Is<CustomSlotDynamic>(out var d) && d.ExtentionID == er.id))
                {
                    var dinamic = item.GetComponent<CustomSlotDynamic>();
                    if (dinamic.ExtentionCount(mechDef, inv.ToInventory()) > 0)
                    {

                    }
                }
            }

            return result;
        }

        public static bool AdjustMechDefaults(MechDef mechDef, SimGameState simgame, List<MechComponentRef> inventory)
        {

            var changed = false;
            foreach (var slotType in Control.Instance.Settings.SlotTypes)
                changed = AdjustMechDefaults(mechDef, simgame, slotType.SlotName, inventory) && changed;
            return changed;

        }

        public static bool AdjustMechDefaults(MechDef mechDef, SimGameState simGame, string slotname, List<MechComponentRef> inventory)
        {
            var slotType = Control.Instance.Settings.SlotTypes.FirstOrDefault(i => i.SlotName == slotname);
            if (slotType == null)
                return false;

            var sinfo = SlotsInfoDatabase.GetMechInfoByType(mechDef, slotType.SlotName);

            var slots = inventory.Select(i => new { item = i, slot = i.GetComponent<IUseSlots>() })
                .Where(i => i.slot != null && i.slot.SlotName == slotname)
                .Select(i => new slot_record
                {
                    item = i.item,
                    slot = i.slot,
                    is_default = !i.item.Is<CustomSlotExtention>() && i.item.IsDefault(),
                    slot_used = i.slot.GetSlotsUsed(mechDef, inventory.ToInventory()),
                    sup_used = sinfo.Descriptor.HaveSupports ? i.slot.GetSlotsUsed(mechDef, inventory.ToInventory()) : 0

                }).GroupBy(i => i.item.MountedLocation)
                .ToDictionary(i => i.Key, i => i.ToArray());

            var changed = false;

            foreach (var location in all_locations)
            {
                slots.TryGetValue(location, out var sr);

                changed = AdjustMechDefaults(mechDef, simGame, slotname, inventory, location,
                    sinfo, sr) || changed;
            }

            return changed;
        }

        public static bool AdjustMechDefaults(MechDef mechDef, SimGameState simGame, string slotname,
            List<MechComponentRef> inventory, ChassisLocations location, SlotDescriptor slotinfo, slot_record[] list)
        {
            var linfo = slotinfo[location];
            var used_slots = list?.Sum(i => i.slot_used) ?? 0;
            var used_defaults = list?.Where(i => i.is_default).Sum(i => i.slot_used) ?? 0;

            var total_slots = linfo?.SlotCount ?? 0;

            //cannot fix
            if (used_defaults == 0)
                return false;

            //if remove all defaults will be enough to fix
            if (used_slots - total_slots >= used_defaults)
            {
                if (used_defaults <= 0) return false;

                foreach (var itemRecord in list.Where(i => i.is_default))
                    inventory.Remove(itemRecord.item);
                return true;
            }

            var support = slotinfo.Descriptor.HaveSupports
                ? GetSupportsForLocation(mechDef, slotname, location, inventory.ToInventory())
                : 0;

            var defaults = list.Where(i => i.is_default).ToList();
            var used_sup_defaults = slotinfo.Descriptor.HaveSupports
                ? defaults.Sum(i => i.sup_used)
                : 0;

            var need_defaults = linfo.GetDefaults(mechDef, inventory.ToInventory(),
                total_slots - (used_slots - used_defaults), support + used_sup_defaults);

            bool need_fix = false;

            foreach (var def in need_defaults)
            {
                var item = defaults.FirstOrDefault(i => i.item.ComponentDefID == def.item.Description.Id);
                if (item == null)
                {
                    need_fix = true;
                    break;
                }

                defaults.Remove(item);
            }

            if (!need_fix && defaults.Count == 0)
                return false;
            foreach (var itemRecord in list.Where(i => i.is_default))
                inventory.Remove(itemRecord.item);
            foreach (var needDefault in need_defaults)
            {
                var r_item = DefaultHelper.CreateRef(needDefault.item.Description.Id,
                    needDefault.item.ComponentType, UnityGameInstance.BattleTechGame.DataManager, simGame);
                r_item.SetData(location, 0 , ComponentDamageLevel.Functional, true);
                inventory.Add(r_item);
            }

            return true;


        }

        public static bool AdjustMechDefaults(MechDef mechDef, SimGameState simGame, string slotname,
            List<MechComponentRef> inventory, ChassisLocations location)
        {
            var slotType = Control.Instance.Settings.SlotTypes.FirstOrDefault(i => i.SlotName == slotname);
            if (slotType == null)
                return false;

            var sinfo = SlotsInfoDatabase.GetMechInfoByType(mechDef, slotType.SlotName);

            var slots = inventory
                .Where(i => i.MountedLocation == location)
                .Select(i => new { item = i, slot = i.GetComponent<IUseSlots>() })
                .Where(i => i.slot != null && i.slot.SlotName == slotname)
                .Select(i => new slot_record
                {
                    item = i.item,
                    slot = i.slot,
                    is_default = !i.item.Is<CustomSlotExtention>() && i.item.IsDefault(),
                    slot_used = i.slot.GetSlotsUsed(mechDef, inventory.ToInventory()),
                    sup_used = sinfo.Descriptor.HaveSupports ? i.slot.GetSlotsUsed(mechDef, inventory.ToInventory()) : 0

                }).ToArray();

            return AdjustMechDefaults(mechDef, simGame, slotname, inventory, location, sinfo, slots);
        }


        internal static void ClearInventory(MechDef mech, List<MechComponentRef> result, SimGameState simgame)
        {
            if (mech == null)
                return;

            result.RemoveAll(i => i.Is<CustomSlotInfo>() && !i.IsFixed);
            var dinamics = GetExtentions(mech, result.ToInventory());
            AdjustDynamics(mech, dinamics, simgame, result);
            AdjustMechDefaults(mech, simgame, result);
        }

        internal static void AdjustDefaultsMechlab(MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            var helper = new MechLabHelper(mechLab);
            var mech = mechLab.activeMechDef;

            var items_to_remove = mech.Inventory.Where(i => i.IsDefault() && i.Is<CustomSlotInfo>()).ToList();

            foreach (var item in items_to_remove)
                DefaultHelper.RemoveMechLab(item.ComponentDefID, item.ComponentDefType, helper,
                    ChassisLocations.CenterTorso);
            //RemoveMechLab(item);

            mech.SetInventory(mech.Inventory.Where(i => !(i.IsDefault() && i.Is<HandHeldInfo>())).ToArray());

            var slot_used = SlotsUsed(mech);
            var slot_max = SlotsTotal(mech);
            var sdef = GetDefInfo(mech);

            var defaults = GetDefaults(mech, mechLab.sim, sdef);

            int n = 0;
            while (slot_used < slot_max)
            {
                while (slot_max - slot_used < defaults[n].si.SpecSlotUsed)
                    n += 1;
                var item = defaults[n];
                DefaultHelper.AddMechLab(item.item.ComponentDefID, item.item.ComponentDefType, helper,
                    ChassisLocations.CenterTorso);
                slot_used += defaults[n].si.SpecSlotUsed;

                if (n + 1 < defaults.Count)
                    n += 1;
            }
        }

        public static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors,
            MechValidationLevel validationlevel, MechDef mechdef)
        {
            var s = Control.Instance.Settings;
            var info = SlotsInfoDatabase.GetMechInfo(mechdef);
            foreach (var slotTypeDescriptor in Control.Instance.Settings.SlotTypes)
            {
                var slots = mechdef.Inventory
                    .Select(i => new { location = i.MountedLocation, slot = i.GetComponent<IUseSlots>() })
                    .Where(i => i.slot != null && i.slot.SlotName == slotTypeDescriptor.SlotName)
                    .GroupBy(i => i.location)
                    .Select(i => new
                    {
                        location = i.Key,
                        slots = i.Sum(s => s.slot.GetSlotsUsed(mechdef, mechdef.Inventory.ToInventory())),
                        supports = i.Sum(s => s.slot.GetSupportUsed(mechdef, mechdef.Inventory.ToInventory()))
                    })
                    .ToList();

                var slot_info = info[slotTypeDescriptor.SlotName];
                foreach (var linfo in all_locations.Select(i => slot_info[i]).Where(i => i != null))
                {
                    var slot = slots.FirstOrDefault(i => i.location == linfo.Location);
                    int slot_num = slot?.slots ?? 0;
                    if (slot_num != linfo.SlotCount)
                        errors[MechValidationType.InvalidInventorySlots].Add(linfo.SlotCount < slot_num
                            ? new Text(s.ErrorTooManySlots, slotTypeDescriptor.SlotsErrorName, linfo.Location)
                            : new Text(s.ErrorNotEnoughSlots, slotTypeDescriptor.SlotsErrorName, linfo.Location));
                    if (slotTypeDescriptor.HaveSupports)
                    {
                        int num_sup = slot?.supports ?? 0;
                        if (num_sup > 0)
                        {
                            var free_sup = GetSupportsForLocation(mechdef, slotTypeDescriptor.SlotName, linfo.Location);
                            if (free_sup < 0)
                                errors[MechValidationType.InvalidInventorySlots]
                                    .Add(new Text(s.ErrorNotEnoughSupport, slotTypeDescriptor.SupportsErrorName, linfo.Location));
                        }
                    }
                }
            }
        }

        public static bool CanBeFielded(MechDef mechdef)
        {
            var info = SlotsInfoDatabase.GetMechInfo(mechdef);
            foreach (var slotTypeDescriptor in Control.Instance.Settings.SlotTypes)
            {
                var slots = mechdef.Inventory
                    .Select(i => new { location = i.MountedLocation, slot = i.GetComponent<IUseSlots>() })
                    .Where(i => i.slot != null && i.slot.SlotName == slotTypeDescriptor.SlotName)
                    .GroupBy(i => i.location)
                    .Select(i => new
                    {
                        location = i.Key,
                        slots = i.Sum(s => s.slot.GetSlotsUsed(mechdef, mechdef.Inventory.ToInventory())),
                        supports = i.Sum(s => s.slot.GetSupportUsed(mechdef, mechdef.Inventory.ToInventory()))
                    })
                    .ToList();

                var slot_info = info[slotTypeDescriptor.SlotName];
                foreach (var linfo in all_locations.Select(i => slot_info[i]).Where(i => i != null))
                {
                    var slot = slots.FirstOrDefault(i => i.location == linfo.Location);
                    int slot_num = slot?.slots ?? 0;
                    if (slot_num != linfo.SlotCount)
                        return false;

                    if (slotTypeDescriptor.HaveSupports)
                    {
                        int num_sup = slot?.supports ?? 0;
                        if (num_sup > 0)
                        {
                            var free_sup = GetSupportsForLocation(mechdef, slotTypeDescriptor.SlotName, linfo.Location);
                            if (free_sup < 0)
                                return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}