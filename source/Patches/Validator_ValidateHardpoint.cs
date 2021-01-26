using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech.UI;
using CustomComponents;
using Harmony;

namespace CustomSlots
{
    [HarmonyPatch(typeof(Validator))]
    [HarmonyPatch("ValidateHardpoint")]
    public static class Validator_ValidateHardpoint
    {
        [HarmonyPrefix]
        public static bool CancelReplaceForHH(MechLabItemSlotElement drop_item, ref string __result)
        {
            if (drop_item.ComponentRef.Is<HandHeldInfo>())
            {
                __result = string.Empty;
                return false;
            }
            return true;
        }
    }
}
