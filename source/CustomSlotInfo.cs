using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BattleTech;
using BattleTech.UI;
using CustomComponents;

namespace CustomSlots
{
    [CustomComponent("SlotInfo", AllowArray = true)]
    public class CustomSlotInfo : SimpleCustomComponent,
        IUseSlots,
        IOnInstalled, IOnItemGrabbed, 
        IPreValidateDrop
    {
        public int SlotsUsed { get; set; } = 1;
        public int SupportUsed { get; set; } = 0;

        public string  SlotName { get; set; }

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

        public virtual void OnItemGrabbed(IMechLabDraggableItem item, MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            var d = SlotsInfoDatabase.GetMechInfoByType(mechLab.activeMechDef, SlotName);
            CustomSlotControler.AdjustDefaultsMechlab(mechLab, widget, d);
        }

        public string ReplaceValidateDrop(MechLabItemSlotElement drop_item, LocationHelper location, List<IChange> changes)
        {
            var mech = location.mechLab.activeMechDef;
            var sdef = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);
            var mountlocation = location.widget.loadout.Location;
            
            if(changes.Count > 1)
                return String.Empty;

            var total = sdef[mountlocation]?.SlotCount ?? 0;
            if (total == 0)
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots,
                    drop_item.ComponentRef.Def.Description.Name, location.LocationName);


            var replacable = mech.Inventory
                .Where(i => i.MountedLocation == mountlocation)
                .Select(i => new {item = i, slot = i.GetComponent<IUseSlots>()})
                .Where(i => i.slot != null && i.slot.SlotName == SlotName)
                .Select(i => new
                {
                    item = i.item,
                    slot = i.slot,
                    used_slot = i.slot.GetSlotsUsed(mech),
                    used_sup = sdef.Descriptor.HaveSupports ? i.slot.GetSupportUsed(mech) : 0
                })
                .ToList();
            var max_support = sdef.Descriptor.HaveSupports ? CustomSlotControler.GetSupportsForLocation(mech, SlotName, mountlocation) : 0;

            var used_slots = replacable.Sum(i => i.used_slot);
            var used_sups = sdef.Descriptor.HaveSupports ? replacable.Sum(i => i.used_sup) : 0;
            var slot_need = GetSlotsUsed(mech);
            var supp_need = GetSupportUsed(mech);


            return null;
        }

        public virtual string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab)
        {
            var mech = location.mechLab.activeMechDef;
            var info = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);
            if (info == null)
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots, item.ComponentRef.Def.Description.Name, location.LocationName);

            var linfo = info[location.widget.loadout.Location];
            if(linfo == null || linfo.SlotCount < GetSlotsUsed(mech))
                return string.Format(Control.Instance.Settings.ErrorMechLab_Slots, item.ComponentRef.Def.Description.Name, location.LocationName);

            return null;
        }
    }
}