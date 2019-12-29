using BattleTech;
using BattleTech.UI;
using CustomComponents;
using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandHeld.Patches
{
    [HarmonyPatch(typeof(MechLabStatBlockWidget))]
    [HarmonyPatch("SetData")]
    public static class MechLabStatBlockWidget_SetData
    {
        [HarmonyPostfix]
        public static void UpdateLabel(MechDef mechDef)
        {
            if (CarryWeightTools.TextElement == null)
            {
                Control.LogError("Cannot Find Text Label!");
                return;
            }

            if (mechDef == null)
            {
                CarryWeightTools.TextElement.text = string.Format(Control.Settings.LocationLabel, 0, 0);
                return;
            }
            var TotalTonage = CarryWeightTools.GetCarryWeight(mechDef);
            var UsedTonnage = 0f;
            foreach (var item in mechDef.Inventory.Where(i => i.Is<HandHeldInfo>()).Select(i => i.GetComponent<HandHeldInfo>()))
            {
                UsedTonnage += item.Tonnage;
            }
            CarryWeightTools.TextElement.text = string.Format(Control.Settings.LocationLabel, UsedTonnage, TotalTonage);
        }
    }
}
