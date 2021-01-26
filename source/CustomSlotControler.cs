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
        public class defrecord
        {
            public MechComponentRef item;
            public CustomSlotInfo si;
        }

        //for patching
        public static ISlotsOverride GetSlotsOverride(MechDef mech, string type)
        {
            var customs = mech.GetComponents<ISlotsOverride>();
            if (customs != null)
                return customs.FirstOrDefault(i => i.SlotName == type);
            
            return null;

        }

        public static int SlotsTotal(MechDef mech, IEnumerable<MechComponentRef> inventory = null)
        {
            var sdef = GetDefInfo(mech);
            var slotinfo = Control.Instance.Settings.GetSpSlotInfo(mech);
            
            var count = slotinfo == null ? 0 : slotinfo.SpecialSlotCount;
            if (sdef != null && sdef.MaxSlots >= 0)
                count = sdef.MaxSlots;
            return count;
        }

        public static int SlotsUsed(MechDef mech, IEnumerable<MechComponentRef> inventory = null)
        {
            if (inventory == null)
                inventory = mech.Inventory;

            int used = 0;
            foreach (var item in inventory)
            {
                if (item.Is<IUseSlots>(out var s))
                    used += s.GetSlotsUsed(mech, inventory);
            }

            return used;
        }

        internal static void AutoFixMech(List<MechDef> mechDefs, SimGameState simgame)
        {
            foreach (var mechDef in mechDefs)
            {
                var sdef = GetDefInfo(mechDef);
                var count = SlotsTotal(mechDef);
                int used = SlotsUsed(mechDef);


                if (used != count)
                {
                    AdjustDefaults(mechDef, simgame, count, sdef);
                }
            }
        }

        public static void AdjustDefaults(MechDef mechDef, SimGameState simgame, int max, ISpecialSlotDefaults sdef)
        {
            var inventory = mechDef.Inventory.ToList();

            inventory.RemoveAll(i => i.IsDefault() && i.Is<CustomSlotInfo>());
            var defaults = GetDefaults(mechDef, simgame, sdef);
            int used = SlotsUsed(mechDef, inventory);

            int n = 0;
            while (used < max)
            {
                while (max - used < defaults[n].si.SpecSlotUsed)
                    n += 1;
                DefaultHelper.AddInventory(defaults[n].item.ComponentDefID, mechDef, ChassisLocations.CenterTorso, 
                    defaults[n].item.ComponentDefType, simgame);
                used += defaults[n].si.SpecSlotUsed;

                if (n + 1 < defaults.Count)
                    n += 1;
            }

            mechDef.SetInventory(inventory.ToArray());
        }

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
                    if(item != null && item.Is<CustomSlotInfo>(out var si))
                        result.Add(new defrecord() {item = item, si =  si});
                }

            var id = (sdef == null || string.IsNullOrEmpty(sdef.DefaultSlotId))
                ? Control.Instance.Settings.SpecialDefaultSlotID
                : sdef.DefaultSlotId;


            var def = DefaultHelper.CreateRef(id, ComponentType.Upgrade, dm,
                sim);
            result.Add(new defrecord() { 
                item = def,
                si = def.GetComponent<CustomSlotInfo>()
            });
            return result;

        }

        public static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors, MechValidationLevel validationlevel, MechDef mechdef)
        {
            var count = SlotsTotal(mechdef);
            int used = SlotsUsed(mechdef);
            if(used != count)
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