using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BattleTech;
using BattleTech.UI;
using CustomComponents;

namespace HandHeld
{
    [CustomComponent("SpecialSlot")]
    public class SpecialInfo : SimpleCustomComponent, IOnInstalled, IOnItemGrabbed, IReplaceValidateDrop, IPreValidateDrop
    {
        public int SpecSlotUsed { get; set; } = 1;
        public void OnInstalled(WorkOrderEntry_InstallComponent order, SimGameState state, MechDef mech)
        {
            var max = SpecialControler.SlotsUsed(mech);
            var sdef = SpecialControler.GetDefInfo(mech);

            SpecialControler.AdjustDefaults(mech, state, max, sdef);
        }

        public void OnItemGrabbed(IMechLabDraggableItem item, MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            SpecialControler.AdjustDefaultsMechlab(mechLab);
        }

        public string ReplaceValidateDrop(MechLabItemSlotElement drop_item, LocationHelper location, List<IChange> changes)
        {
            var mech = location.mechLab.activeMechDef;
//            var total = SpecialControler.SlotsTotal(mech);
            var sdef = SpecialControler.GetDefInfo(mech);

            //var used_fixed = mech.Inventory.Where(i => i.IsModuleFixed(mech)).Where(i => i.Is<SpecialInfo>())
            //    .Sum(i => i.GetComponent<SpecialInfo>().SpecSlotUsed);

            var installed = mech.Inventory
                .Where(i => i.Is<SpecialInfo>() && !i.IsModuleFixed(mech))
                .Select(i => new { item = i, spinfo = i.GetComponent<SpecialInfo>()} ).ToList();

            var defaults = installed.Where(i => i.item.IsDefault()).ToList();
            var notdefaults = installed.Where(i => !i.item.IsDefault()).ToList();

            var used = defaults.Sum(i => i.spinfo.SpecSlotUsed);
            
            //remove additional non defaults if need
            if (SpecSlotUsed > used)
            {
                
                foreach (var item_to_replace in notdefaults)
                {
                    used += item_to_replace.spinfo.SpecSlotUsed;

                    var slot_item = location.LocalInventory.FirstOrDefault(i => i.ComponentRef == item_to_replace.item);
                    changes.Add(new RemoveChange(ChassisLocations.CenterTorso, slot_item));
                    if(SpecSlotUsed <= used + used)
                        break;
                }
            }

            //remove defaults
            foreach (var def_item in defaults)
            {
                var slot_item = location.LocalInventory.FirstOrDefault(i => i.ComponentRef == def_item.item);
                changes.Add(new RemoveChange(ChassisLocations.CenterTorso, slot_item));
            }

            //add defaults if need
            if (SpecSlotUsed < used)
            {
                var to_fill = used - SpecSlotUsed;
                var defs_to_place = SpecialControler.GetDefaults(mech, location.mechLab.sim, sdef);

                int n = 0;
                while (to_fill > 0)
                {
                    while (to_fill < defs_to_place[n].si.SpecSlotUsed)
                        n += 1;
                    var def = defs_to_place[n];

                    var slot = DefaultHelper.CreateSlot(def.item.ComponentDefID, def.item.ComponentDefType,
                        location.mechLab);
                    changes.Add(new AddDefaultChange(ChassisLocations.CenterTorso, slot));
                        
                    if (n + 1 < defs_to_place.Count)
                        n += 1;
                }
            }

            return null;
        }

        public string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab)
        {
            var mech = location.mechLab.activeMechDef;
            var total = SpecialControler.SlotsTotal(mech);

            var used = mech.Inventory.Where(i => i.IsModuleFixed(mech)).Where(i => i.Is<SpecialInfo>())
                .Sum(i => i.GetComponent<SpecialInfo>().SpecSlotUsed);

            if (total < SpecSlotUsed + used)
                return string.Format(Control.Instance.Settings.NotEnoughSpecialSlots, Def.Description.Name);
            
            return null;
        }
    }
}