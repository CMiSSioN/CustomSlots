using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using BattleTech.UI;
using CustomComponents;
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
                .Select(i => new { loc = i.location, slot = i.item.GetComponent<CustomSlotInfo>() })
                .Where(i => i.slot != null && i.slot.SlotName == slotname)
                .Select(i => new
                { loc = i.loc, count = i.slot.GetSlotsUsed(mech, inventory), sort = i.loc == target ? 1 : 0 })
                .Where(i => i.count > 0)
                .OrderBy(i => i.sort).ToList();

            foreach (var slot in slots)
            {
                int used = slot.count;
                while (used > 0)
                {
                    for (int i = 0; i < supports.Count; i++)
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
                }
            }

            return supports.Where(i => i.sort >= 2).Sum(i => i.count);

        }


        internal static void AutoFixMech(List<MechDef> mechDefs, SimGameState simgame)
        {
            foreach (var mechDef in mechDefs)
            {
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

                    AdjuctMechDefaults(mechDef, simgame, slotType.SlotName);
                }
            }
        }

        public static void AdjuctMechDefaults(MechDef mechDef, SimGameState simgame)
        {
            foreach (var slotType in Control.Instance.Settings.SlotTypes)
                AdjuctMechDefaults(mechDef, simgame, slotType.SlotName);

        }

        public static void AdjuctMechDefaults(MechDef mechDef, SimGameState simGame, string slotname)
        {
            var slotType = Control.Instance.Settings.SlotTypes.FirstOrDefault(i => i.SlotName == slotname);
            if (slotType == null)
                return;

            var sinfo = SlotsInfoDatabase.GetMechInfoByType(mechDef, slotType.SlotName);


            var inventory = mechDef.Inventory.ToList();
            var dinamics = inventory
                .Where(i => i.IsDefault() && i.IsSlot(slotType.SlotName) && i.Is<CustomSlotDynamic>())
                .ToList();

            foreach (var item in dinamics)
            {
                var d = item.GetComponent<CustomSlotDynamic>();
                inventory.Remove(item);
                inventory.RemoveAll(i => i.ComponentDefID == d.ExtentionID);
            }
            inventory.RemoveAll(i => i.IsDefault() && i.IsSlot(slotType.SlotName) && !i.Is<CustomSlotExtention>());

            int free_support = 0;
            if (slotType.HaveSupports)
            {
                var total_support = mechDef.Supports(slotType.SlotName);
                var used_support = mechDef.Inventory
                    .Where(i => i.IsSlot(slotType.SlotName))
                    .Select(i => i.GetComponent<IUseSlots>())
                    .Sum(i => i.GetSupportUsed(mechDef, mechDef.Inventory.ToInventory()));

                free_support = total_support - used_support;
            }

            foreach (var location in all_locations.Select(i => sinfo[i]).Where(i => i != null))
            {
                var max_slots = location.SlotCount;
                var free = max_slots - mechDef.Inventory
                    .Where(i => i.MountedLocation == location.Location && i.IsSlot(slotType.SlotName))
                    .Select(i => i.GetComponent<IUseSlots>())
                    .Sum(i => i.GetSlotsUsed(mechDef, mechDef.Inventory.ToInventory()));

                if (free <= 0)
                    continue;

                var free_support_location = 0;
                if (free_support > 0)
                {
                    free_support_location = GetSupportsForLocation(mechDef, slotType.SlotName, location.Location,
                        inventory.ToInventory());
                }

                int n = 0;
                while (free > 0)
                {
                    int use_slot;
                    int use_sup;
                    SlotDescriptor.location_info.def_info def;
                    while (true)
                    {
                        def = location.Defaults[n];
                        use_slot = def.info.GetSlotsUsed(mechDef);
                        use_sup = def.info.GetSupportUsed(mechDef);
                        if (use_slot <= free && use_sup <= free_support)
                            break;
                        n++;
                    }

                    free -= use_slot;
                    free_support -= use_sup;
                    var item = DefaultHelper.CreateRef(def.item.Description.Id, def.item.ComponentType,
                        UnityGameInstance.BattleTechGame.DataManager, simgame);
                    item.SetData(location.Location, 0, ComponentDamageLevel.Functional, true);
                    inventory.Add(item);

                    if (n + 1 < location.Defaults.Length)
                        n += 1;
                }

            }

            mechDef.SetInventory(inventory.ToArray());
        }


        internal static void ClearInventory(MechDef mech, List<MechComponentRef> result, SimGameState simgame)
        {
            if (mech == null)
                return;

            result.RemoveAll(i => i.Is<CustomSlotInfo>() && !i.IsFixed);
        }

        internal static void AdjustDefaultsMechlab(MechLabPanel mechLab)
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

        public static List<defrecord> GetDefaults(MechDef mech, SimGameState sim, ISpecialSlotDefaults sdef)
        {
            var result = new List<defrecord>();
            var dm = UnityGameInstance.BattleTechGame.DataManager;

            if (sdef != null && sdef.Defaults != null && sdef.Defaults.Length > 0)
                foreach (var record in sdef.Defaults)
                {
                    var item = DefaultHelper.CreateRef(record.id, record.type, dm, sim);
                    if (item != null && item.Is<CustomSlotInfo>(out var si))
                        result.Add(new defrecord() { item = item, si = si });
                }

            var id = (sdef == null || string.IsNullOrEmpty(sdef.DefaultSlotId))
                ? Control.Instance.Settings.SpecialDefaultSlotID
                : sdef.DefaultSlotId;


            var def = DefaultHelper.CreateRef(id, ComponentType.Upgrade, dm,
                sim);
            result.Add(new defrecord()
            {
                item = def,
                si = def.GetComponent<CustomSlotInfo>()
            });
            return result;
        }

        public static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors,
            MechValidationLevel validationlevel, MechDef mechdef)
        {
            var count = SlotsTotal(mechdef);
            int used = SlotsUsed(mechdef);
            if (used != count)
                errors[MechValidationType.InvalidInventorySlots]
                    .Add(new Text(Control.Instance.Settings.SpecialSlotError));
        }

        public static bool CanBeFielded(MechDef mechdef)
        {
            var count = SlotsTotal(mechdef);
            int used = SlotsUsed(mechdef);

            return count == used;
        }
    }
}