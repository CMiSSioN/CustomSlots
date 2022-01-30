﻿using BattleTech;
using BattleTech.UI;
using CustomComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using Localize;
using TMPro;
using UnityEngine;

namespace CustomSlots
{
    public static class CarryWeightController
    {
        public static TextMeshProUGUI TextElement { get; internal set; }
        public static LocationHelper Location { get; internal set; }
        public static LocationHelper CenterTorso { get; internal set; }

        public static float GetCarryWeight(MechDef mech, IEnumerable<MechComponentRef> inventory = null)
        {

            if (inventory == null)
                inventory = mech.Inventory;
            var tfactor = Control.Instance.Settings.CarryWeightFactor;
            float basetf = 0;

            foreach (var item in inventory)
            {
                if (item.Is<CarryWeightCustoms>(out var info))
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

        public static float GetUsedWeight(MechDef mech, IEnumerable<MechComponentRef> inventory = null)
        {
            if (inventory == null)
                inventory = mech.Inventory;

            return inventory.Where(i => i.Is<IUseTonnage>()).Select(i => i.GetComponent<IUseTonnage>())
                .Sum(i => i.GetTonnage(mech, inventory.ToInventory()));
        }

        public static void ValidateMech(Dictionary<MechValidationType, List<Text>> errors, MechValidationLevel validationlevel, MechDef mechdef)
        {
            var total = GetCarryWeight(mechdef);

            var used = GetUsedWeight(mechdef);
            if (total + 0.01 > used)
                errors[MechValidationType.Overweight].Add(new Text(Control.Instance.Settings.ErrorOverweight, total, used));
        }

        internal static bool CanBeFielded(MechDef mechDef)
        {
            var total = GetCarryWeight(mechDef);

            var used = GetUsedWeight(mechDef);
            return total + 0.01 > used;
        }


    }
}
