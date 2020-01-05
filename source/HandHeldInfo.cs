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
    public class HandHeldInfo : SimpleCustomComponent, IMechLabFilter, IPreValidateDrop, IOnInstalled, IOnItemGrabbed, IReplaceValidateDrop
    {
        public int HandsUsed = 2;
        public float Tonnage = 5;

        public bool CheckFilter(MechLabPanel panel)
        {
            if (Control.Settings.Debug_IgnoreWeightFIlter || panel.activeMechDef == null)
                return true;

            var mechDef = panel.activeMechDef;

            float tonnage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);
            return HandsUsed == 2 ? tonnage - Tonnage >= -0.001 : tonnage / 2 - Tonnage >= -0.001;
        }

        public void OnInstalled(WorkOrderEntry_InstallComponent order, SimGameState state, MechDef mech)
        {
            HandHeldHandler.AdjustDefaults(mech, state);
        }

        public void OnItemGrabbed(IMechLabDraggableItem item, MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            HandHeldHandler.AdjustDefaultsMechlab(mechLab);
        }

        public string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab)
        {
            var mechDef = mechlab.MechLab.activeMechDef;

            int hands = CarryWeightTools.NumOfHands(mechDef, mechDef.Inventory);
            float tonnage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);
            if (HandsUsed == 1)
                tonnage /= 2f;

            if (hands < HandsUsed)
                return string.Format(HandsUsed == 1 ? Control.Settings.OneHandMissed : Control.Settings.TwoHandMissed, Def.Description.Name);

            if (tonnage + 0.001 < Tonnage)
                return string.Format(HandsUsed == 1 ? Control.Settings.WrongWeightMessage1H : Control.Settings.WrongWeightMessage, Def.Description.Name, Tonnage, tonnage);

            return string.Empty;
        }

        public string ReplaceValidateDrop(MechLabItemSlotElement drop_item, LocationHelper location, List<IChange> changes)
        {
            if (HandsUsed == 2)
            {
                foreach (var item in location.LocalInventory.Where(i => i.ComponentRef.Is<HandHeldInfo>()))
                {
                    if (item.ComponentRef.IsFixed && !item.ComponentRef.IsDefault())
                        return (new Text($"Cannot replace {item.ComponentRef.Def.Description.Name}")).ToString();
                    changes.Add(new RemoveChange(location.widget.loadout.Location, item));
                }
            }
            else
            {
                var mech = location.mechLab.activeMechDef;
                var handhelds = location.LocalInventory.Where(i => i.ComponentRef.Is<HandHeldInfo>()).ToList();

                if (handhelds.Count == 1)
                {
                    if (handhelds[0].ComponentRef.IsModuleFixed(mech))
                        return (new Text($"Cannot replace fixed {handhelds[0].ComponentRef.Def.Description.Name}")).ToString();

                    changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[0]));
                    var defaults = HandHeldHandler.GetDefaults(mech);
                    changes.Add(new AddDefaultChange(location.widget.loadout.Location, DefaultHelper.CreateSlot(defaults[0].id, defaults[0].type, location.mechLab)));
                }
                else if (handhelds.Count == 2)
                {
                    var f0 = handhelds[0].ComponentRef.IsModuleFixed(mech);
                    var f1 = handhelds[1].ComponentRef.IsModuleFixed(mech);

                    if (f0 && f1)
                        return (new Text($"Cannot replace fixed equipment")).ToString();
                    if (f0 || f1)
                        changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 1 : 0]));
                    else
                    {
                        var hands = CarryWeightTools.NumOfHands(mech, mech.Inventory);
                        f0 = handhelds[0].ComponentRef.IsDefault();
                        f1 = handhelds[1].ComponentRef.IsDefault();
                        var defaults = HandHeldHandler.GetDefaults(mech);
                        if (f0 && f1)
                            changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[handhelds[0].ComponentRef.ComponentDefID == defaults[1].id ? 0 : 1]));
                        else if (f0 || f1)
                        {
                            if (handhelds[f0 ? 1 : 0].ComponentRef.GetComponent<HandHeldInfo>().HandsUsed + HandsUsed > hands)
                                changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 1 : 0]));
                            else
                                changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1]));
                        }
                        else
                        {
                            f0 = handhelds[0].ComponentRef.GetComponent<HandHeldInfo>().HandsUsed == 0;
                            f1 = handhelds[1].ComponentRef.GetComponent<HandHeldInfo>().HandsUsed == 0;
                            if (f0 && f1 || !(f0 || f1))
                                changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[0]));
                            else
                            {
                                if (hands == 1)
                                    if (HandsUsed == 1)
                                        changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 1 : 0]));
                                    else
                                        changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1]));
                                else
                                    changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1]));
                            }
                        }
                    }
                }
                else
                {
                    Control.LogError("WRONG HAND HELDS COUNT: " + handhelds.Count.ToString());
                    return "Invalid mech, check logs";
                }
            }

            return string.Empty;
        }
    }
}
