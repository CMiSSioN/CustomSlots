using System.Linq;
using BattleTech;
using CustomComponents;
using Harmony;

namespace CustomSlots
{
    [HarmonyPatch(typeof(SimGameState), "CreateComponentInstallWorkOrder")]
    public static class SimGameState_CreateComponentInstallWorkOrder
    {
        [HarmonyPriority(Priority.Last)]
        [HarmonyPostfix]
        public static void FixHHCost(
                MechComponentRef mechComponent,
                ChassisLocations newLocation,
                WorkOrderEntry_InstallComponent __result)
        {
            if (newLocation == ChassisLocations.None)
                return;


            if (!mechComponent.Is<IUseTonnage>(out var hh))
                return;

            var us = mechComponent.GetComponent<IUseSlots>();

            if (us == null || !Control.Instance.Settings.QuickInstall.Contains(us.SlotName))
                return;

            var tr = Traverse.Create(__result);
            tr.Field<int>("Cost").Value = 1;
        }
    }
}
