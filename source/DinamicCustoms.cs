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