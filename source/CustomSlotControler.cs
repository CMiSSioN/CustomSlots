using BattleTech;
using System;
using System.Collections.Generic;
using System.Linq;
using BattleTech.UI;
using CustomComponents;
using JetBrains.Annotations;
using Localize;

namespace CustomSlots
{

    public static class CustomSlotControler
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

        public static bool IsSupport(this MechComponentRef item, string slotname)
        {
            var items = item.GetComponents<ISlotSupport>();
            return items?.FirstOrDefault(i => i.SlotName == slotname) != null;
        }
        public static bool IsSupport(this MechComponentDef item, string slotname)
        {
            var items = item.GetComponents<ISlotSupport>();
            return items?.FirstOrDefault(i => i.SlotName == slotname) != null;
        }

        public static bool IsSupport(this MechComponentRef item, string slotname, out ISlotSupport support)
        {
            var items = item.GetComponents<ISlotSupport>();

            support = items?.FirstOrDefault(i => i.SlotName == slotname);

            return support != null;
        }
        public static bool IsSupport(this MechComponentDef item, string slotname, out ISlotSupport support)
        {
            var items = item.GetComponents<ISlotSupport>();

            support = items?.FirstOrDefault(i => i.SlotName == slotname);

            return support != null;
        }

        //for patching
        public static ISlotsOverride GetSlotsOverride(this MechDef mech, string type)
        {
            var customs = mech.GetComponents<ISlotsOverride>();
            return customs?.FirstOrDefault(i => i.SlotName == type);
        }

        public static bool IsSlot(this MechComponentRef item, string slotname, out IUseSlots result)
        {
            return item.Is<IUseSlots>(out result) && result.SlotName == slotname;
        }
        public static bool IsSlot(this MechComponentDef item, string slotname, out IUseSlots result)
        {
            return item.Is<IUseSlots>(out result) && result.SlotName == slotname;
        }

        public static bool IsSlot(this MechComponentRef item, string slotname)
        {
            return item.Is<IUseSlots>(out var result) && result.SlotName == slotname;
        }
        public static bool IsSlot(this MechComponentDef item, string slotname)
        {
            return item.Is<IUseSlots>(out var result) && result.SlotName == slotname;
        }

        public static int Supports(this MechDef mech, string slotname, ChassisLocations location)
        {
            int num = 0;

            foreach (var item in mech.Inventory)
            {
                if(item.IsSupport(slotname, out var support))
                    if (support.Location.HasFlag(location) ||
                        (support.Location == ChassisLocations.None && item.MountedLocation == location))
                        num += support.GetSupportAdd(mech, mech.Inventory);
            }

            return num;

        }


        internal static void AutoFixMech(List<MechDef> mechDefs, SimGameState simgame)
        {
            foreach (var mechDef in mechDefs)
            {
                AdjustDefaults(mechDef, simgame, true);
            }
        }

        private static void AdjustDefaults(MechDef mechDef, SimGameState simgame, bool with_empty_locations = false)
        {
            foreach (var slotType in Control.Instance.Settings.SlotTypes)
            {
                AdjustDefaults(mechDef, simgame, slotType.SlotName, true);
            }
        }

        private static void AdjustDefaults(MechDef mechDef, SimGameState simgame, string slotName, bool with_empty_locations = false)
        {
            var sinfo = SlotsInfoDatabase.GetMechInfoByType(mechDef, slotName);
            AdjustDefaults(mechDef, simgame, slotName, sinfo, with_empty_locations);
        }

        private static void AdjustDefaults(MechDef mechDef, SimGameState simgame, string slotName, SlotDescriptor sinfo, bool with_empty_locations)
        {
            var inventory = mechDef.Inventory.ToList();
            var changed = false;
            foreach (var location in all_locations)
            {
                var loc = sinfo?[location];
                if (loc != null || with_empty_locations)
                {
                    int max_slots = loc?.SlotCount ?? 0;
                    int used_slt_def = 0;
                    int used_slt_oth = 0;

                    int max_support = mechDef.Supports(slotName, location);
                    int used_sup_def = 0;
                    int used_sup_oth = 0;
                    List<MechComponentRef> defaults = new List<MechComponentRef>();

                    foreach (var item in inventory.Where(i => i.MountedLocation == location))
                    {
                        if (item.IsSlot(slotName, out var slot))
                        {
                            var isDefault = item.IsDefault();
                            var used_slots = slot.GetSlotsUsed(mechDef, inventory);


                            if (isDefault)
                            {
                                used_slt_def += used_slots;
                                defaults.Add(item);
                            }
                            else
                                used_slt_oth += used_slots;

                            if (sinfo.Descriptor.HaveSupports)
                            {
                                var used_support = slot.GetSupportUsed(mechDef, inventory);
                                if (isDefault)
                                    used_sup_def += used_support;
                                else
                                    used_sup_oth += used_support;
                            }
                        }
                    }

                    var used_total = used_slt_def + used_slt_oth;
                    var need_fix = used_total < max_slots || used_total > max_slots && used_slt_def > 0 ||
                                   !Control.Instance.Settings.CheckOnlyDefaultCountInAutofix && used_slt_def > 0;

                }
            }

            if (changed)
                mechDef.SetInventory(inventory.ToArray());
        }

        private static void AdjustDefaults(MechDef mechDef, SimGameState simgame, string slotName, SlotDescriptor sinfo, SlotDescriptor.location_info loc)
        {
        }

        //public static void AdjustDefaults(MechDef mechDef, SimGameState simgame, int max, ISpecialSlotDefaults sdef)
        //{
        //    var inventory = mechDef.Inventory.ToList();

        //    inventory.RemoveAll(i => i.IsDefault() && i.Is<CustomSlotInfo>());
        //    var defaults = GetDefaults(mechDef, simgame, sdef);
        //    int used = SlotsUsed(mechDef, inventory);

        //    int n = 0;
        //    while (used < max)
        //    {
        //        while (max - used < defaults[n].si.SpecSlotUsed)
        //            n += 1;
        //        DefaultHelper.AddInventory(defaults[n].item.ComponentDefID, mechDef, ChassisLocations.CenterTorso,
        //            defaults[n].item.ComponentDefType, simgame);
        //        used += defaults[n].si.SpecSlotUsed;

        //        if (n + 1 < defaults.Count)
        //            n += 1;
        //    }

        //    mechDef.SetInventory(inventory.ToArray());
        //}

        internal static void ClearInventory(MechDef mech, List<MechComponentRef> result, SimGameState simgame)
        {
            if (mech == null)
                return;

            result.RemoveAll(i => i.Is<CustomSlotInfo>() && !i.IsFixed);
            var used = SlotsUsed(mech, result);
            var max = SlotsTotal(mech, result);
            var sdef = GetDefInfo(mech);


            result.RemoveAll(i => i.IsDefault() && i.Is<CustomSlotInfo>());
            var defaults = GetDefaults(mech, simgame, sdef);

            int n = 0;
            while (used < max)
            {
                while (max - used < defaults[n].si.SpecSlotUsed)
                    n += 1;
                DefaultHelper.AddInventory(defaults[n].item.ComponentDefID, mech, ChassisLocations.CenterTorso,
                    defaults[n].item.ComponentDefType, simgame);
                used += defaults[n].si.SpecSlotUsed;

                if (n + 1 < defaults.Count)
                    n += 1;
            }
        }

        internal static void AdjustDefaultsMechlab(MechLabPanel mechLab)
        {
            var helper = new MechLabHelper(mechLab);
            var mech = mechLab.activeMechDef;

            var items_to_remove = mech.Inventory.Where(i => i.IsDefault() && i.Is<CustomSlotInfo>()).ToList();

            foreach (var item in items_to_remove)
                DefaultHelper.RemoveMechLab(item.ComponentDefID, item.ComponentDefType, helper, ChassisLocations.CenterTorso);
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
                DefaultHelper.AddMechLab(item.item.ComponentDefID, item.item.ComponentDefType, helper, ChassisLocations.CenterTorso);
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

        public static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors, MechValidationLevel validationlevel, MechDef mechdef)
        {
            var count = SlotsTotal(mechdef);
            int used = SlotsUsed(mechdef);
            if (used != count)
                errors[MechValidationType.InvalidInventorySlots].Add(new Text(Control.Instance.Settings.SpecialSlotError));
        }

        public static bool CanBeFielded(MechDef mechdef)
        {
            var count = SlotsTotal(mechdef);
            int used = SlotsUsed(mechdef);

            return count == used;
        }
    }
}