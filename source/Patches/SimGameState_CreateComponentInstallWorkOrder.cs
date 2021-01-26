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
            if (!mechComponent.Is<HandHeldInfo>(out var hh))
                return;

            if (newLocation != ChassisLocations.None && hh.HandsUsed)
            {
                var tr = Traverse.Create(__result);
                tr.Field<int>("Cost").Value = 1;
            }
        }
    }
}
