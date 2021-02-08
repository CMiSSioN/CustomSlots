using System.Collections.Generic;
using System.Linq;
using BattleTech;
using CustomComponents;
using JetBrains.Annotations;

namespace CustomSlots
{
    public static partial class CustomSlotControler
    {
        #region extention for patching

        public static IEnumerable<inventory_item> ToInventory(this IEnumerable<MechComponentRef> items)
        {
            return items.Select(i => new inventory_item {item = i, location = i.MountedLocation});
        }

        public static ISlotsOverride GetSlotsOverride(this MechDef mech, string type)
        {
            var customs = mech.GetComponents<ISlotsOverride>();
            return customs?.FirstOrDefault(i => i.SlotName == type);
        }
        #endregion

        #region SupportExtentions

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

        public static ISlotSupport GetSupport(this MechComponentRef item, string slotname)
        {
            var items = item.GetComponents<ISlotSupport>();

            return items?.FirstOrDefault(i => i.SlotName == slotname);
        }

        public static ISlotSupport GetSupport(this MechComponentDef item, string slotname)
        {
            var items = item.GetComponents<ISlotSupport>();

            return items?.FirstOrDefault(i => i.SlotName == slotname);
        }


        public static int Supports(this MechDef mech, string slotname, ChassisLocations location, IEnumerable<inventory_item> inventory = null)
        {
            int num = 0;
            if (inventory == null)
                inventory = mech.Inventory.ToInventory();

            foreach (var item in inventory)
            {
                if (item.item.IsSupport(slotname, out var support))
                    if (support.Location.HasFlag(location) ||
                        (support.Location == ChassisLocations.None && item.location == location))
                        num += support.GetSupportAdd(mech, inventory);
            }

            return num;
        }

        public static int Supports(this MechDef mech, string slotname, IEnumerable<inventory_item> inventory = null)
        {
            if (inventory == null)
                inventory = mech.Inventory.ToInventory();

            return inventory.Where(i => i.item.IsSupport(slotname)).Select(i => i.item.GetSupport(slotname))
                .Sum(i => i.GetSupportAdd(mech, inventory));
        }

        #endregion

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

        public static IEnumerable<IUseSlots> AllSlots(this IEnumerable<MechComponentRef> inv, string slotname)
        {
            return inv.Select(i => i.GetComponent<IUseSlots>()).Where(i => i?.SlotName == slotname);

        }

    }
}