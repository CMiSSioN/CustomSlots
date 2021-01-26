using BattleTech;
using BattleTech.UI;
using CustomComponents;
using MechEngineer.Features.ArmActuators;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CustomSlots
{
    public static class CarryWeightTools
    {
        public static TextMeshProUGUI TextElement { get; internal set; }
        public static LocationHelper Location { get; internal set; }
        public static LocationHelper CenterTorso { get; internal set; }

        public static float GetCarryWeight(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            var tfactor = Control.Instance.Settings.CarryWeightFactor;
            float basetf = 0;

            foreach (var item in inventory)
            {
                if (item.Is<TSMInfoComponent>(out var info))
                {
                    if (Control.Instance.Settings.MultiplicativeTonnageFactor)
                        tfactor *= info.HandHeldFactor;
                    else
                        basetf += (info.HandHeldFactor - 1);
                    continue;
                }

                if (Control.Instance.Settings.TSMTags != null && Control.Instance.Settings.TSMTags.Length > 0)
                    foreach (var tag in Control.Instance.Settings.TSMTags)
                    {
                        if (item.Def.ComponentTags.Contains(tag.Tag) || item.IsCategory(tag.Tag))
                        {
                            if (Control.Instance.Settings.MultiplicativeTonnageFactor)
                                tfactor *= tag.Mul;
                            else
                                basetf += (tag.Mul - 1);
                        }
                    }
            }
            if (!Control.Instance.Settings.MultiplicativeTonnageFactor)
                tfactor *= 1 + basetf;

            var addtonnage = inventory
                .Where(i => i.Is<AddCarryWeight>())
                .Select(i => i.GetComponent<AddCarryWeight>())
                .Sum(i => i.AddTonnage);

            return Mathf.Ceil(mech.Chassis.Tonnage * tfactor * 100) / 100f + addtonnage;
        }


        public static float GetUsedWeight(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {

            return inventory.Where(i => i.Is<IUseTonnage>()).Select(i => i.GetComponent<IUseTonnage>())
                .Sum(i => i.GetTonnage(mech, inventory));

        }

        public static int NumOfHands(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            return Control.Instance.Settings.UseHandTag ?
                inventory.Where(i => i.Def.ComponentTags.Contains(Control.Instance.Settings.HandsItemTag)).Count() :
                inventory.Where(i => i.Is<ArmActuator>(out var arm) && arm.Type.HasFlag(ArmActuatorSlot.Hand)).Count();

        }
    }
}
