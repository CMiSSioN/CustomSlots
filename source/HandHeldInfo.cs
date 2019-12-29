using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BattleTech;
using BattleTech.UI;
using CustomComponents;
using Localize;
using MechEngineer.Features;
using MechEngineer.Features.ArmActuators;

namespace HandHeld
{
    [CustomComponent("HandHeld")]
    public class HandHeldInfo : SimpleCustomComponent, IMechLabFilter, IMechValidate, IPreValidateDrop
    {
        public int HandsUsed = 2;
        public int Tonnage = 5;



        public bool CheckFilter(MechLabPanel panel)
        {
            if (Control.Settings.Debug_IgnoreWeightFIlter || panel.activeMechDef == null)
                return true;

            var mechDef = panel.activeMechDef;

            float tonnage = CarryWeightTools.GetCarryWeight(mechDef);
            foreach (var i in mechDef.Inventory.Where(i => i.Is<HandHeldInfo>()).Select(i => i.GetComponent<HandHeldInfo>()))
            {
                tonnage -= i.Tonnage;
            }
            return tonnage >= -0.001;
        }

        public string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab)
        {
            var mechDef = mechlab.MechLab.activeMechDef;

            int hands = CarryWeightTools.NumOfHands(mechDef);
            float tonnage = CarryWeightTools.GetCarryWeight(mechDef);

            var other_item = mechDef.Inventory.FirstOrDefault(i => i.Is<HandHeldInfo>());
            HandHeldInfo other_info = other_item?.GetComponent<HandHeldInfo>();
            
            hands -= HandsUsed;
            if (other_info != null)
                hands -= other_info.HandsUsed;
            if (hands < 0)
                return string.Format(HandsUsed == 1 ? Control.Settings.OneHandMissed : Control.Settings.TwoHandMissed, Def.Description.Name);
            var t_left = tonnage - (other_info == null ? 0 : other_info.Tonnage);
            if (t_left + 0.001 < Tonnage)
                return string.Format(Control.Settings.WrongWeightMessage, Def.Description.Name, Tonnage, t_left);

            return string.Empty;
        }

        public void ValidateMech(Dictionary<MechValidationType, List<Text>> errors, MechValidationLevel validationLevel, MechDef mechDef, MechComponentRef componentRef)
        {
            int hands = CarryWeightTools.NumOfHands(mechDef);
            float tonnage = CarryWeightTools.GetCarryWeight(mechDef);
            
            
            foreach (var i in mechDef.Inventory.Where(i => i.Is<HandHeldInfo>()).Select(i => i.GetComponent<HandHeldInfo>()))
            {
                hands -= i.HandsUsed;
                tonnage -= i.Tonnage;
            }

            if (hands < 0)
                errors[MechValidationType.InvalidInventorySlots].Add(new Text(string.Format(Control.Settings.ValidateHands, -hands)));
            
            if (tonnage + 0.001 < 0)
                errors[MechValidationType.InvalidInventorySlots].Add(new Text(Control.Settings.ValidateTonnage, -tonnage));

        }

        public bool ValidateMechCanBeFielded(MechDef mechDef, MechComponentRef componentRef)
        {
            int hands = CarryWeightTools.NumOfHands(mechDef);
            float tonnage = CarryWeightTools.GetCarryWeight(mechDef);
            foreach (var i in mechDef.Inventory.Where(i => i.Is<HandHeldInfo>()).Select(i => i.GetComponent<HandHeldInfo>()))
            {
                hands -= i.HandsUsed;
                tonnage -= i.Tonnage;
            }
            return tonnage >= -0.001 && hands >= 0;
        }
    }
}
