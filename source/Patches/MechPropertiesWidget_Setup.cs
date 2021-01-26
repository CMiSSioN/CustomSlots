using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using Harmony;
using MechEngineer.Features.MechLabSlots;
using TMPro;
using CustomComponents;

namespace CustomSlots.Patches
{
    [HarmonyPatch(typeof(MechPropertiesWidget))]
    [HarmonyPatch("Setup")]
    public static class MechPropertiesWidget_Setup
    {
        [HarmonyPostfix]
        public static void GetText(MechLabLocationWidget ___PropertiesWidget)
        {
            var text = ___PropertiesWidget.transform.GetChild("layout_locationText").GetChild("txt_location").GetComponent<TextMeshProUGUI>();
            CarryWeightTools.TextElement = text;
            CarryWeightTools.Location = new LocationHelper(___PropertiesWidget);
            CarryWeightTools.CenterTorso = new LocationHelper(new Traverse(___PropertiesWidget).Field<MechLabPanel>("mechLab").Value.GetLocationWidget(BattleTech.ArmorLocation.CenterTorso));
        }
    }
}
