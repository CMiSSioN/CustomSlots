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
        }

        public static int GetSupportsForLocation(MechDef mech, string slotname, ChassisLocations target, IEnumerable<InvItem> inventory = null)
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


        public static List<free_record> get_free_slots(MechDef mech,
            ChassisLocations base_location, string SlotName, IEnumerable<InvItem> inv = null)
        {
            if (inv == null)
                inv = mech.Inventory.ToInventory();
            var result = new Dictionary<ChassisLocations, free_record>();
            var info = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);

            foreach (var linfo in info)
            {
                result[linfo.Location] = new free_record
                {
                    location = linfo.Location,
                    free_slots = linfo.SlotCount,
                };
            }

            var slots = inv
                .Select(i => new { item = i.item, s = i.item.GetComponent<IUseSlots>(), l = i.location })
                .Where(i => i.s?.SlotName == SlotName)
                .Where(i => !i.item.IsDefault() || i.item.Is<CustomSlotExtenstion>());

            foreach (var item in slots)
                if (result.TryGetValue(item.l, out var record))
                    record.free_slots -= item.s.GetSlotsUsed(mech);

            return result.Values.ToList();
        }


        public static List<extention_record> GetExtentions(MechDef mech,
            IEnumerable<InvItem> inventory = null)
        {

            var inv = inventory == null ? mech.Inventory.ToInventory().ToArray() : inventory.ToArray();

            var result = inv.Select(i => new
            {
                din = i.item.GetComponent<CustomSlotDynamic>(),
                ext = i.item.GetComponent<CustomSlotExtenstion>()
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
                            total.have += next.have;
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
                var inv = mechDef.Inventory.ToList();
                var changed = AdjustDynamics(mechDef, GetExtentions(mechDef, inv.ToInventory()), simgame, inv) != ChassisLocations.None;

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

                    changed = AdjustMechDefaults(mechDef, simgame, slotType.SlotName, inv) || changed;
                }
                if (changed)
                    mechDef.SetInventory(inv.ToArray());
            }
        }

        public static ChassisLocations AdjustDynamics(MechDef mechDef, List<extention_record> dinamics, SimGameState simgame,
            List<MechComponentRef> inv)
        {
            void add_default(free_record freeRecord, CustomSlotDynamic dinamic, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    var comref = DefaultHelper.CreateRef(dinamic.ExtentionID, dinamic.ExtentionType,
                        UnityGameInstance.BattleTechGame.DataManager, simgame);
                    comref.SetData(freeRecord.location, 0, ComponentDamageLevel.Functional, true);
                    inv.Add(comref);
                }
            }

            ChassisLocations result = ChassisLocations.None;
            foreach (var er in dinamics)
            {
                if (er.need == er.have)
                    continue;

                foreach (var mechComponentRef in inv.Where(i => i.ComponentDefID == er.id))
                    result.Set(mechComponentRef.MountedLocation);

                inv.RemoveAll(i => i.ComponentDefID == er.id);
                var items = inv.Where(i => i.Is<CustomSlotDynamic>(out var d) && d.ExtentionID == er.id).ToList();

                if (items.Count == 0)
                    continue;

                var d = items[0].GetComponent<CustomSlotDynamic>();

                var component = SlotsInfoDatabase.GetComponentDef(d.ExtentionID, d.ExtentionType);
                if (component == null)
                {
                    CustomComponents.Control.LogError($"Cannot find {d.ExtentionID} of type {d.ExtentionType}");
                    continue;
                }

                if (!component.IsDefault())
                {
                    Control.Instance.LogError($"{component.Description.Id} is not default, cannot be used as extenstion");
                    continue;
                }

                var extinfo = component.GetComponent<CustomSlotExtenstion>();
                if (extinfo == null)
                {
                    Control.Instance.LogError($"{component.Description.Id} is not extenstion");
                    continue;
                }

                foreach (var item in items)
                {
                    var dynamic = item.GetComponent<CustomSlotDynamic>();
                    if (extinfo.SlotName != dynamic.SlotName)
                    {
                        Control.Instance.LogError($"{component.Description.Id} extenstion is not same slot type as dynamic {item.ComponentDefID}");
                        continue;
                    }

                    var slotinfo = SlotsInfoDatabase.GetMechInfoByType(mechDef, dynamic.SlotName);

                    var use_support = slotinfo.Descriptor.HaveSupports && extinfo.UseSupport;

                    var extention = dynamic.ExtentionCount(mechDef, inv.ToInventory());
                    if (extention > 0)
                    {
                        var free_slots = get_free_slots(mechDef, item.MountedLocation, dynamic.SlotName);
                        if (dynamic.ForceAnotherLocation)
                            free_slots.RemoveAll(i => i.location == item.MountedLocation);


                        foreach (var freeRecord in free_slots)
                        {
                            var max = use_support
                                ? GetSupportsForLocation(mechDef, dynamic.SlotName, freeRecord.location,
                                    inv.ToInventory()) : 9999;

                            max = freeRecord.free_slots < max ? freeRecord.free_slots : max;
                            if (max == 0)
                                continue;

                            result.Set(freeRecord.location);
                            if (max >= extention)
                            {
                                add_default(freeRecord, dynamic, extention);
                                break;
                            }
                            else
                            {
                                add_default(freeRecord, dynamic, max);
                                extention -= max;
                            }
                        }
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
                    is_default = !i.item.Is<CustomSlotExtenstion>() && i.item.IsDefault(),
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
            if (used_defaults == 0 && used_slots >= total_slots)
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
                r_item.SetData(location, 0, ComponentDamageLevel.Functional, true);
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
                    is_default = !i.item.Is<CustomSlotExtenstion>() && i.item.IsDefault(),
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

        internal static void AdjustDefaultsMechlab(MechLabPanel mechLab, MechLabLocationWidget widget, SlotDescriptor slotType)
        {
            var helper = new MechLabHelper(mechLab);
            var whelper = new LocationHelper(widget);
            var mech = mechLab.activeMechDef;
            var slotname = slotType.Descriptor.SlotName;
            var location = widget.loadout.Location;

            var slots = whelper.LocalInventory
                .Select(i => new { item = i, slot = i.ComponentRef.GetComponent<IUseSlots>() })
                .Where(i =>
                {
                    return i.slot != null && i.slot.SlotName == slotname;
                })
                .Select(i => new
                {
                    item = i.item,
                    slot = i.slot,
                    is_default = !i.item.ComponentRef.Is<CustomSlotExtenstion>() && i.item.ComponentRef.IsDefault(),
                    slot_used = i.slot.GetSlotsUsed(mechLab.activeMechDef),
                    sup_used = slotType.Descriptor.HaveSupports ? i.slot.GetSlotsUsed(mechLab.activeMechDef) : 0
                }).ToArray();

            var linfo = slotType[location];
            var used_slots = slots?.Sum(i => i.slot_used) ?? 0;
            var used_defaults = slots?.Where(i => i.is_default).Sum(i => i.slot_used) ?? 0;

            var total_slots = linfo?.SlotCount ?? 0;

            //cannot fix
            if (used_defaults == 0 && used_slots >= total_slots)
                return;

            //if remove all defaults will be enough to fix
            if (used_slots - total_slots >= used_defaults)
            {
                if (used_defaults <= 0) return;
                foreach (var slot in slots.Where(i => i.is_default).Select(i => i.item))
                    DefaultHelper.RemoveMechLab(whelper, slot, helper);
            }

            var support = slotType.Descriptor.HaveSupports
                ? GetSupportsForLocation(mech, slotname, location, mech.Inventory.ToInventory())
                : 0;

            var defaults = slots.Where(i => i.is_default).ToList();
            var used_sup_defaults = slotType.Descriptor.HaveSupports
                ? defaults.Sum(i => i.sup_used)
                : 0;

            var need_defaults = linfo.GetDefaults(mech, mech.Inventory.ToInventory(),
                total_slots - (used_slots - used_defaults), support + used_sup_defaults);
            bool need_fix = false;

            foreach (var def in need_defaults)
            {
                var item = defaults.FirstOrDefault(i => i.item.ComponentRef.ComponentDefID == def.item.Description.Id);
                if (item == null)
                {
                    need_fix = true;
                    break;
                }

                defaults.Remove(item);
            }

            if (!need_fix && defaults.Count == 0)
                return;
            foreach (var itemRecord in slots.Where(i => i.is_default))
                DefaultHelper.RemoveMechLab(whelper, itemRecord.item, helper);
            foreach (var needDefault in need_defaults)
            {
                DefaultHelper.AddMechLab(needDefault.item.Description.Id, needDefault.item.ComponentType, helper, location);
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

            var dynamics = GetExtentions(mechdef);

            if (dynamics.Any(i => i.have != i.need))
                errors[MechValidationType.InvalidInventorySlots].Add(new Text("Invalid extended slots, refit mech to fix"));
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

            var dynamics = GetExtentions(mechdef);
            if (dynamics.Any(i => i.have != i.need))
                return false;

            return true;
        }
    }
}