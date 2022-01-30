using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using CustomComponents.Changes;

namespace CustomSlots
{
    [CustomComponent("SlotInfo", AllowArray = true)]
    public class CustomSlotInfo : SimpleCustomComponent,IUseSlots, /*IOnInstalled,*/ IOnItemGrab, IPreValidateDrop
    {
        public int SlotsUsed { get; set; } = 1;
        public int SupportUsed { get; set; } = 0;

        public string SlotName { get; set; }

        public virtual int GetSlotsUsed(MechDef mech, IEnumerable<InvItem> inventory)
        {
            return SlotsUsed;
        }

        public virtual int GetSupportUsed(MechDef mech, IEnumerable<InvItem> inventory)
        {
            return SupportUsed;
        }

        public virtual int GetSlotsUsed(MechDef mech)
        {
            return SlotsUsed;
        }

        public virtual int GetSupportUsed(MechDef mech)
        {
            return SupportUsed;
        }


        public virtual void OnInstalled(WorkOrderEntry_InstallComponent order, SimGameState state, MechDef mech)
        {
            var inventory = mech.Inventory.ToList();
            CustomSlotControler.AdjustMechDefaults(mech, state, SlotName, inventory);
            mech.SetInventory(inventory.ToArray());
        }

        public virtual void OnItemGrab(IMechLabDraggableItem item, MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            var d = SlotsInfoDatabase.GetMechInfoByType(mechLab.activeMechDef, SlotName);
            CustomSlotControler.AdjustDefaultsMechlab(mechLab, widget, d);
        }

        public string ReplaceValidateDrop(MechLabItemSlotElement drop_item, LocationHelper location, List<IChange> changes)
        {
            var mech = location.mechLab.activeMechDef;
            var sdef = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);
            var mountlocation = location.widget.loadout.Location;
            var havesupports = sdef.Descriptor.HaveSupports;
            if (changes.Count > 1)
                return String.Empty;

            var total = sdef[mountlocation]?.SlotCount ?? 0;

            if (total == 0)
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots,
                    drop_item.ComponentRef.Def.Description.Name, location.LocationName);



            var replacable = mech.Inventory
                .Where(i => i.MountedLocation == mountlocation && !i.IsFixed)
                .Select(i => new { item = i, slot = i.GetComponent<IUseSlots>() })
                .Where(i => i.slot != null && i.slot.SlotName == SlotName)
                .Select(i => new
                {
                    item = i.item,
                    slot = i.slot,
                    used_slot = i.slot.GetSlotsUsed(mech),
                    used_sup = havesupports ? i.slot.GetSupportUsed(mech) : 0
                })
                .ToList();

            var fixitem = mech.Inventory
                .Where(i => i.MountedLocation == mountlocation && i.IsModuleFixed(mech))
                .Select(i => i.GetComponent<IUseSlots>())
                .Where(i => i != null && i.SlotName == SlotName)
                .Select(i => new
                {
                    slots = i.GetSlotsUsed(mech),
                    supps = havesupports ? i.GetSupportUsed(mech) : 0

                })
                .Aggregate(
                    new { slots = 0, supps = 0 },
                    (total_loc, next) => new { slots = total_loc.slots + next.slots, supps = total_loc.supps + next.supps }
                    );

            var max_support = havesupports ? CustomSlotControler.GetSupportsForLocation(mech, SlotName, mountlocation) : 0 - fixitem.supps;
            var max_slots = total - fixitem.slots;

            var used_slots = replacable.Sum(i => i.used_slot);
            var used_sups = havesupports ? replacable.Sum(i => i.used_sup) : 0;

            var slot_need = GetSlotsUsed(mech);
            var supp_need = GetSupportUsed(mech);

            // not enough slots
            if (max_slots - slot_need < 0)
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots,
                    drop_item.ComponentRef.Def.Description.Name, location.LocationName);

            // not enough supports
            if (havesupports && max_support - supp_need < 0)
                return string.Format(Control.Instance.Settings.ErrorNotEnoughSupport,
                    drop_item.ComponentRef.Def.Description.Name, location.LocationName);

            //no replace need
            if (slot_need <= max_slots - used_slots && supp_need < max_support - used_sups)
                return null;

            var need_free_slots = slot_need - (max_slots - used_slots);
            var need_free_supps = havesupports ? supp_need - (max_support - used_sups) : 0;

            var to_delete = replacable.ToList();
            to_delete.Clear();

            if (need_free_supps > 0)
            {
                int i = 0;
                while (need_free_supps > 0 && i < replacable.Count)
                {
                    var item = replacable[i];

                    if (item.used_sup > 0)
                    {
                        need_free_supps -= item.used_sup;
                        need_free_slots -= item.used_slot;
                        to_delete.Add(item);
                        replacable.RemoveAt(i);
                    }
                    else
                        i += 1;
                }
            }

            for (int i = 0; i < replacable.Count && need_free_slots > 0; i++)
            {
                var item = replacable[i];
                need_free_slots -= item.used_slot;
                to_delete.Add(item);
            }

            foreach (var item in to_delete)
            {
                var item_to_remove = location.LocalInventory.FirstOrDefault(i => i.ComponentRef == item.item);
                if (item_to_remove != null)
                    changes.Add(new Change_Remove(item_to_remove.ComponentRef, item_to_remove.MountedLocation));
            }

            return null;
        }

        public virtual string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location)
        {
            var mech = location.mechLab.activeMechDef;
            var info = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);
            if (info == null)
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots, item.ComponentRef.Def.Description.Name, location.LocationName);

            var linfo = info[location.widget.loadout.Location];
            if (linfo == null || linfo.SlotCount < GetSlotsUsed(mech))
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots, item.ComponentRef.Def.Description.Name, location.LocationName);

            return null;
        }

    public string PreValidateDrop(MechLabItemSlotElement item, ChassisLocations location) {
      throw new NotImplementedException();
    }

    public bool OnItemGrab(IMechLabDraggableItem item, MechLabPanel mechLab, out string error) {
      throw new NotImplementedException();
    }
  }
}