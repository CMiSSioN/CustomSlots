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
                Control.Instance.LogError("Cannot Find Text Label!");
                return;
            }

            if (mechDef == null)
            {
                CarryWeightTools.TextElement.text = string.Format(Control.Instance.Settings.LocationLabel, 0, 0);
                return;
            }

            var TotalTonage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);
            var UsedTonnage = CarryWeightTools.GetUsedWeight(mechDef, mechDef.Inventory);
                
            CarryWeightTools.TextElement.text = string.Format(Control.Instance.Settings.LocationLabel, UsedTonnage, TotalTonage);

            foreach (var item in CarryWeightTools.CenterTorso.LocalInventory)
                if (item.ComponentRef.Is<HandHeldInfo>(out var hh) && hh.HandsUsed)
                {
                    int hu = hh.hands_used(TotalTonage);
                    var traverse = new Traverse(item).Field<LocalizableText>("nameText");
                    traverse.Value.SetText($"{item.ComponentRef.Def.Description.UIName} ({hu}H)");
                }
        }
    }
}
