using BattleTech;
using BattleTech.UI;
using BattleTech.UI.TMProWrapper;
using CustomComponents;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomSlots.Patches
{
    [HarmonyPatch(typeof(MechLabStatBlockWidget))]
    [HarmonyPatch("SetData")]
    public static class MechLabStatBlockWidget_SetData
    {
        [HarmonyPostfix]
        public static void UpdateLabel(MechDef mechDef)
        {
            if (CarryWeightController.TextElement == null)
            {
                Control.Instance.LogError("Cannot Find Text Label!");
                return;
            }

            if (mechDef == null)
            {
                CarryWeightController.TextElement.text = string.Format(Control.Instance.Settings.LocationLabel, 0, 0);
                return;
            }

            var TotalTonage = CarryWeightController.GetCarryWeight(mechDef, mechDef.Inventory);
            var UsedTonnage = CarryWeightController.GetUsedWeight(mechDef, mechDef.Inventory);
                
            CarryWeightController.TextElement.text = string.Format(Control.Instance.Settings.LocationLabel, UsedTonnage, TotalTonage);

            foreach (var item in CarryWeightController.CenterTorso.LocalInventory)
                if (item.ComponentRef.Is<HandHeldInfo>(out var hh) && hh.HandsUsed)
                {
                    int hu = hh.hands_used(TotalTonage);
                    var traverse = new Traverse(item).Field<LocalizableText>("nameText");
                    traverse.Value.SetText($"{item.ComponentRef.Def.Description.UIName} ({hu}H)");
                }
        }
    }
}
