using BattleTech;
using CustomComponents;
using MechEngineer.Features.ArmActuators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace HandHeld
{
    public static class CarryWeightTools
    {
        public static TextMeshProUGUI TextElement { get; internal set; }

        public static float GetCarryWeight(MechDef mech)
        {
            var tfactor = Control.Settings.CarryWeightFactor * (2 + NumOfHands(mech)) / 2f;
            float basetf = 0;

            foreach (var item in mech.Inventory)
            {
                if (item.Is<TSMInfo>(out var info))
                {
                    if (Control.Settings.MultiplicativeTonnageFactor)
                        tfactor *= info.Mul;
                    else
                        basetf += (info.Mul - 1);
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

        public static int NumOfHands(MechDef mech)
        {
           return Control.Settings.UseHandTag ?
                mech.Inventory.Where(i => i.Def.ComponentTags.Contains(Control.Settings.HandsItemTag)).Count() :
                mech.Inventory.Where(i => i.Is<ArmActuator>(out var arm) && arm.Type.HasFlag(ArmActuatorSlot.Hand)).Count();

        }
    }
}
