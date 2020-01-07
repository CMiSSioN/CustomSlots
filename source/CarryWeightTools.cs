using BattleTech;
using BattleTech.UI;
using CustomComponents;
using MechEngineer.Features.ArmActuators;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace HandHeld
{
    public static class CarryWeightTools
    {
        public static TextMeshProUGUI TextElement { get; internal set; }
        public static LocationHelper Location { get; internal set; }
        public static LocationHelper CenterTorso { get; internal set; }

        public static float GetCarryWeight(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            var tfactor = Control.Settings.CarryWeightFactor;
            float basetf = 0;

            foreach (var item in inventory)
            {
                if (item.Is<TSMInfoComponent>(out var info))
                {
                    if (Control.Settings.MultiplicativeTonnageFactor)
                        tfactor *= info.HandHeldFactor;
                    else
                        basetf += (info.HandHeldFactor - 1);
                    continue;
                }

                if (Control.Settings.TSMTags != null && Control.Settings.TSMTags.Length > 0)
                    foreach (var tag in Control.Settings.TSMTags)
                    {
                        if (item.Def.ComponentTags.Contains(tag.Tag) || item.IsCategory(tag.Tag))
                        {
                            if (Control.Settings.MultiplicativeTonnageFactor)
                                tfactor *= tag.Mul;
                            else
                                basetf += (tag.Mul - 1);
                        }
                    }
            }
            if (!Control.Settings.MultiplicativeTonnageFactor)
                tfactor *= 1 + basetf;

            return Mathf.Ceil(mech.Chassis.Tonnage * tfactor * 100) / 100f;
        }

        public static int NumOfHands(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
           return Control.Settings.UseHandTag ?
                inventory.Where(i => i.Def.ComponentTags.Contains(Control.Settings.HandsItemTag)).Count() :
                inventory.Where(i => i.Is<ArmActuator>(out var arm) && arm.Type.HasFlag(ArmActuatorSlot.Hand)).Count();

        }
    }
}
