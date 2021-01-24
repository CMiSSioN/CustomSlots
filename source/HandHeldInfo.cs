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
    [CustomComponent("UseCarryWeight")]
    public class UseCarryWeight : SimpleCustomComponent, IUseTonnage
    {
        public float Tonnage { get; set; } = 0;
        public float GetTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            return Tonnage;
        }
    }

    [CustomComponent("HandHeld")]
    public class HandHeldInfo : SimpleCustomComponent, IMechLabFilter, IPreValidateDrop, IOnInstalled, IOnItemGrabbed,
        IReplaceValidateDrop, IUseTonnage
    {
        public bool HandsUsed = true;
        public float Tonnage { get; set; } = 5;
        public bool ForceTwoHand = false;

        public int hands_used(float tonnage) => ForceTwoHand ? 2 :(Tonnage < tonnage / 2 + 0.001 ? 1 : 2);

        //+
        public bool CheckFilter(MechLabPanel panel)
        {
            if (Control.Instance.Settings.Debug_IgnoreWeightFIlter || panel.activeMechDef == null)
                return true;

            var mechDef = panel.activeMechDef;

            float tonnage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);
            return tonnage - Tonnage >= -0.001;
        }

        //+
        public void OnInstalled(WorkOrderEntry_InstallComponent order, SimGameState state, MechDef mech)
        {
            HandHeldController.AdjustDefaults(mech, state);
        }

        //+
        public void OnItemGrabbed(IMechLabDraggableItem item, MechLabPanel mechLab, MechLabLocationWidget widget)
        {
            HandHeldController.AdjustDefaultsMechlab(mechLab);
        }

        //+
        public string PreValidateDrop(MechLabItemSlotElement item, LocationHelper location, MechLabHelper mechlab)
        {
            var mechDef = mechlab.MechLab.activeMechDef;

            float tonnage = CarryWeightTools.GetCarryWeight(mechDef, mechDef.Inventory);

            if (HandsUsed)
            {
                int hands = CarryWeightTools.NumOfHands(mechDef, mechDef.Inventory);
                int hands_need = hands_used(tonnage);
                if (hands_need > hands)
                    return string.Format(hands_need == 1 ? Control.Instance.Settings.OneHandMissed : Control.Instance.Settings.TwoHandMissed, Def.Description.Name);
            }

            if (tonnage + 0.001 < Tonnage)
                return string.Format(Control.Instance.Settings.WrongWeightMessage, Def.Description.Name, Tonnage, tonnage);

            return string.Empty;
        }


        public string ReplaceValidateDrop(MechLabItemSlotElement drop_item, LocationHelper location, List<IChange> changes)
        {
            var mech = location.mechLab.activeMechDef;

            Control.Instance.LogDebug("HandHeld Replacement");

            if (SlotSize == 2)
            {
                Control.Instance.LogDebug("- SlotSize = 2");
                foreach (var item in location.LocalInventory.Where(i => i.ComponentRef.Is<HandHeldInfo>()))
                {
                    Control.Instance.LogDebug($"-- removing {item.ComponentRef.ComponentDefID}");
                    if (item.ComponentRef.IsModuleFixed(mech))
                    {
                        Control.Instance.LogDebug($"--- fixed. break");
                        return (new Text($"Cannot replace {item.ComponentRef.Def.Description.Name}")).ToString();
                    }
                    changes.Add(new RemoveChange(location.widget.loadout.Location, item));
                }
                Control.Instance.LogDebug($"-- done");

            }
            else
            {
                Control.Instance.LogDebug("- SlotSize = 1");
                var tonnage = CarryWeightTools.GetCarryWeight(mech, mech.Inventory);
                var handhelds = location.LocalInventory.Where(i => i.ComponentRef.Is<HandHeldInfo>())
                    .Select(i => new
                    {
                        item = i,
                        cr = i.ComponentRef,
                        hh = i.ComponentRef.GetComponent<HandHeldInfo>(),
                        hu = i.ComponentRef.GetComponent<HandHeldInfo>().hands_used(tonnage)
                    }).ToList();


                if (handhelds.Count == 1)
                {
                    Control.Instance.LogDebug("- 1 item to replace");
                    if (handhelds[0].cr.IsModuleFixed(mech))
                    {
                        Control.Instance.LogDebug($"-- fixed. break");
                        return (new Text($"Cannot replace fixed {handhelds[0].cr.Def.Description.Name}")).ToString();
                    }
                    Control.Instance.LogDebug($"-- removing {handhelds[0].cr.ComponentDefID}");
                    changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[0].item));
                    var defaults = HandHeldController.GetDefaults(mech);
                    Control.Instance.LogDebug($"-- adding second default {defaults[0].id}");
                    changes.Add(new AddDefaultChange(location.widget.loadout.Location, DefaultHelper.CreateSlot(defaults[0].id, defaults[0].type, location.mechLab)));
                    Control.Instance.LogDebug($"-- done");
                }
                else if (handhelds.Count == 2)
                {
                    Control.Instance.LogDebug("- 2 item to replace");
                    var f0 = handhelds[0].cr.IsModuleFixed(mech);
                    var f1 = handhelds[1].cr.IsModuleFixed(mech);
                    if (f0 && f1)
                    {
                        Control.Instance.LogDebug($"-- both fixed. break");
                        return (new Text($"Cannot replace fixed equipment")).ToString();
                    }
                    if (f0 || f1)
                    {
                        Control.Instance.LogDebug($"-- one fixed. replacing {handhelds[f0 ? 1 : 0].cr.ComponentDefID}");
                        changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 1 : 0].item));
                        Control.Instance.LogDebug($"-- done");
                    }
                    else
                    {
                        var hands = CarryWeightTools.NumOfHands(mech, mech.Inventory);
                        var hands_need = hands_used(tonnage);

                        f0 = handhelds[0].cr.IsDefault();
                        f1 = handhelds[1].cr.IsDefault();

                        var defaults = HandHeldController.GetDefaults(mech);
                        if (f0 && f1)
                        {
                            Control.Instance.LogDebug($"-- both default. removing {handhelds[handhelds[0].cr.ComponentDefID == defaults[1].id ? 0 : 1].cr.ComponentDefID}");
                            changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[handhelds[0].cr.ComponentDefID == defaults[1].id ? 0 : 1].item));
                            Control.Instance.LogDebug($"-- done");
                        }
                        else if (f0 || f1)
                        {
                            Control.Instance.LogDebug($"-- one default");
                            if (HandsUsed)
                            {
                                var hh = handhelds[f0 ? 1 : 0];
                                if (hh.hh.HandsUsed && hh.hu + hands_need > hands)
                                {
                                    Control.Instance.LogDebug($"-- not enough hands for both. removing non default {handhelds[f0 ? 1 : 0].cr.ComponentDefID}");
                                    changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 1 : 0].item));
                                }
                                else
                                {
                                    Control.Instance.LogDebug($"-- enough hands for both. removing default {handhelds[f0 ? 0 : 1].cr.ComponentDefID}");
                                    changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1].item));
                                }

                            }
                            else
                            {
                                Control.Instance.LogDebug($"-- no handuseds. removing {handhelds[f0 ? 0 : 1].cr.ComponentDefID}");
                                changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1].item));

                            }
                            Control.Instance.LogDebug($"-- done");
                        }
                        else
                        {
                            f0 = !handhelds[0].hh.HandsUsed;
                            f1 = !handhelds[1].hh.HandsUsed;

                            if (f0 && f1 || !(f0 || f1))
                            {
                                Control.Instance.LogDebug($"-- no defaults, both same type. removing first {handhelds[0].cr.ComponentDefID}");
                                changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[0].item));
                            }
                            else
                            {
                                if (hands == 1)
                                    if (HandsUsed)
                                        changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 1 : 0].item));
                                    else
                                        changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1].item));
                                else
                                    changes.Add(new RemoveChange(location.widget.loadout.Location, handhelds[f0 ? 0 : 1].item));
                            }
                        }
                    }
                }
                else
                {
                    Control.Instance.LogError("WRONG HAND HELDS COUNT: " + handhelds.Count.ToString());
                    return "Invalid mech, check logs";
                }
            }

            return string.Empty;
        }

        public float GetTonnage(MechDef mech, IEnumerable<MechComponentRef> inventory)
        {
            return Tonnage;
        }
    }
}
