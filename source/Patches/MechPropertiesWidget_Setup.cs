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
    [HarmonyPatch(typeof(CustomWidgetsFixMechLab))]
    [HarmonyPatch("Setup")]
    public static class CustomWidgetsFixMechLab_Setup {
        [HarmonyPostfix]
        public static void GetText(MechLabLocationWidget ___TopLeftWidget)
        {
            var text = ___TopLeftWidget.transform.GetChild("layout_locationText").GetChild("txt_location").GetComponent<TextMeshProUGUI>();
            CarryWeightController.TextElement = text;
            CarryWeightController.Location = new LocationHelper(___TopLeftWidget);
            CarryWeightController.CenterTorso = new LocationHelper(new Traverse(___TopLeftWidget).Field<MechLabPanel>("mechLab").Value.GetLocationWidget(BattleTech.ArmorLocation.CenterTorso));
        }
    }
}
