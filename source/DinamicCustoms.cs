using System.Collections.Generic;
using System.Linq;
using BattleTech;
using BattleTech.UI;
using CustomComponents;

namespace CustomSlots
{
    [CustomComponent("SlotExtention")]
    public class CustomSlotExtention : SimpleCustomComponent, IUseSlots
    {
        public string SlotName { get; set; }
        public bool UseSupport { get; set; } = true;

        public int GetSlotsUsed(MechDef mech)
        {
            return 1;
        }

        public int GetSupportUsed(MechDef mech)
        {
            return UseSupport ? 1 : 0;
        }


        public int GetSlotsUsed(MechDef mech, IEnumerable<inventory_item> inventory)
        {
            return 1;
        }

        public int GetSupportUsed(MechDef mech, IEnumerable<inventory_item> inventory)
        {
            return UseSupport ? 1 : 0;
        }

    }

    public abstract class CustomSlotDynamic : CustomSlotInfo
    {
        public string ExtentionID { get; set; }

        public abstract int ExtentionCount(MechDef mech, IEnumerable<inventory_item> inventory);
        public abstract int ExtentionCount(MechDef mech);
        public bool ForceAnotherLocation = false;

        protected class free_record
        {
            public ChassisLocations location { get; set; }
            public int free_slots { get; set; }
            public int free_supports { get; set; }
        }

        protected Dictionary<ChassisLocations, free_record> get_free_slots(MechDef mech, ChassisLocations base_location)
        {
            var result = new Dictionary<ChassisLocations, free_record>();
            var info = SlotsInfoDatabase.GetMechInfoByType(mech, SlotName);
            var use_support = GetSupportUsed(mech) > 0;

            foreach (var linfo in info)
            {
                if (ForceAnotherLocation && linfo.Location == base_location)
                    continue;


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

        public override void OnInstalled(WorkOrderEntry_InstallComponent order, SimGameState state, MechDef mech)
        {


            if (ExtentionCount(mech) > 0)
            {
                var free_slots = get_free_slots(mech, order.DesiredLocation);
            }

            base.OnInstalled(order, state, mech);
        }

        public override void OnItemGrabbed(IMechLabDraggableItem item, MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            if (ExtentionCount(mechLab.activeMechDef) > 0)
            {

            }

            base.OnItemGrabbed(item, mechLab, widget);
        }
    }
}