using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using Localize;

namespace HandHeld
{
    public static class HandHeldHandler
    {
        public class DefInfo
        {
            public string id;
            public ComponentType type;

            public DefInfo()
            {
                this.id = Control.Settings.HandHeldSlotItemID;
                this.type = ComponentType.Upgrade;
            }

            public DefInfo(string id, ComponentType type)
            {
                if (string.IsNullOrEmpty(id))
                {
                    this.id = Control.Settings.HandHeldSlotItemID;
                    this.type = ComponentType.Upgrade;
                }
                else
                {
                    this.id = id;
                    this.type = type;
                }
            }

        }

        internal static void ClearInventory(MechDef mech, List<MechComponentRef> result, SimGameState state)
        {
            result.RemoveAll(i => i.Is<HandHeldInfo>());
            var defaults = GetDefaults(mech);
            result.Add(DefaultHelper.CreateRef(defaults[0].id, defaults[0].type, UnityGameInstance.BattleTechGame.DataManager, state));
            result.Add(DefaultHelper.CreateRef(defaults[1].id, defaults[1].type, UnityGameInstance.BattleTechGame.DataManager, state));

        }

        internal static string PostValidator(MechLabItemSlotElement drop_item, MechDef mech, List<InvItem> new_inventory, List<IChange> changes)
        {
            var handhelds = new_inventory.Where(i => i.item.Is<HandHeldInfo>())
                .Select(i => i.item.GetComponent<HandHeldInfo>())
                .Select(i => new { t = i.Tonnage, h = i.HandsUsed })
                .Aggregate(new { t = 0f, h = 0 }, (sum, val) => new { t = sum.t + val.t, h = sum.h + val.h });
            var inventory = new_inventory.Select(i => i.item).ToList();
            var hands = CarryWeightTools.NumOfHands(mech, inventory);
            var tonnage = CarryWeightTools.GetCarryWeight(mech, inventory);



            return string.Empty;
        }

        internal static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors, MechValidationLevel validationLevel, MechDef mechDef)
        {
            int hands = CarryWeightTools.NumOfHands(mechDef, mechDef.Inventory);
            float tonnage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);
            var t1 = tonnage / 2;

            foreach (var i in mechDef.Inventory.Where(i => i.Is<HandHeldInfo>()).Select(i => i.GetComponent<HandHeldInfo>()))
            {
                hands -= i.HandsUsed;
                tonnage -= i.Tonnage;
                if(i.HandsUsed == 1 && t1 - i.Tonnage < -0.001)
                    errors[MechValidationType.InvalidInventorySlots].Add(new Text(string.Format(Control.Settings.WrongWeightMessage1H, i.Def.Description.Name, i.Tonnage, t1)));
            }

            if (hands < 0)
                errors[MechValidationType.InvalidInventorySlots].Add(new Text(string.Format(Control.Settings.ValidateHands, -hands)));

            if (tonnage + 0.001 < 0)
                errors[MechValidationType.InvalidInventorySlots].Add(new Text(Control.Settings.ValidateTonnage, -tonnage));

        }

        internal static void AdjustDefaultsMechlab(MechLabPanel mechLab)
        {
            var helper = new MechLabHelper(mechLab);
            var mech = mechLab.activeMechDef;
            var items_to_remove = mech.Inventory.Where(i => i.IsDefault() && i.Is<HandHeldInfo>()).ToList();
            foreach (var item in items_to_remove)
                DefaultHelper.RemoveMechLab(item.ComponentDefID, item.Def.ComponentType, helper, item.MountedLocation);

            var slot_used = mech.Inventory.Sum(i => i.Is<HandHeldInfo>(out var hh) ? (hh.HandsUsed <= 1 ? 1 : hh.HandsUsed) : 0);
            var items_to_add = GetDefaults(mechLab.activeMechDef);

            if (slot_used == 0 && items_to_add[2] != null)
            {
                DefaultHelper.AddMechLab(items_to_add[2].id, items_to_add[2].type, helper, ChassisLocations.CenterTorso);
            }
            else if (slot_used < 2)
            {
                DefaultHelper.AddMechLab(items_to_add[0].id, items_to_add[1].type, helper, ChassisLocations.CenterTorso);
                if (slot_used == 0)
                    DefaultHelper.AddMechLab(items_to_add[0].id, items_to_add[1].type, helper, ChassisLocations.CenterTorso);
            }
        }

        internal static void AdjustDefaults(MechDef mech, SimGameState state)
        {
            var inventory = mech.Inventory.Where(i => !(i.IsDefault() && i.Is<HandHeldInfo>())).ToList();
            var slot_used = inventory.Sum(i => i.Is<HandHeldInfo>(out var hh) ? (hh.HandsUsed <= 1 ? 1 : hh.HandsUsed) : 0);
            var items = GetDefaults(mech);
            if (slot_used == 0 && items[2] != null)
            {
                DefaultHelper.AddInventory(items[2].id, mech, ChassisLocations.CenterTorso, items[2].type, state);
            }
            else if (slot_used < 2)
            {
                DefaultHelper.AddInventory(items[0].id, mech, ChassisLocations.CenterTorso, items[0].type, state);
                if (slot_used == 0)
                    DefaultHelper.AddInventory(items[1].id, mech, ChassisLocations.CenterTorso, items[1].type, state);
            }
            mech.SetInventory(inventory.ToArray());
        }

        public static DefInfo[] GetDefaults(MechDef mech)
        {
            if (mech.Chassis.Is<HandHeldDefault>(out var hhd))
                return new DefInfo[] {
                    new DefInfo(hhd.Item1HID1, hhd.Type1),
                    new DefInfo(hhd.Item1HID1, hhd.Type1),
                    string.IsNullOrEmpty(hhd.Item2HID) ? null : new DefInfo(hhd.Item2HID, hhd.TypeH)};
            else
                return new DefInfo[] { new DefInfo(), new DefInfo(), null };
        }

        internal static bool CanBeFielded(MechDef mechDef)
        {
            int hands = CarryWeightTools.NumOfHands(mechDef, mechDef.Inventory);
            float tonnage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);
            foreach (var i in mechDef.Inventory.Where(i => i.Is<HandHeldInfo>()).Select(i => i.GetComponent<HandHeldInfo>()))
            {
                hands -= i.HandsUsed;
                tonnage -= i.Tonnage;
            }
            return tonnage >= -0.001 && hands >= 0;
        }

        internal static void AutoFixMech(List<MechDef> mechDefs, SimGameState simgame)
        {
            foreach (var mech in mechDefs)
                AdjustDefaults(mech, simgame);
        }
    }
}
