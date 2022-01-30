using BattleTech;
using BattleTech.UI;
using CustomComponents;
using CustomComponents.Changes;
using Localize;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace CustomSlots {
  public interface IUseCarryTonnage {
    float Tonnage { get; }
  }
  public class TSMInfoComponent : SimpleCustomComponent {
    public float HandHeldFactor = 2f;
  }
  [CustomComponent("HandHeldDefault")]
  public class HandHeldDefault : SimpleCustomChassis {
    public string Item1H_ID1 = (string)null;
    public string Item1H_ID2 = (string)null;
    public string Item2H_ID = (string)null;
    public ComponentType Type1 = ComponentType.Upgrade;
    public ComponentType Type2 = ComponentType.Upgrade;
    public ComponentType TypeH = ComponentType.Upgrade;
  }
  public static class HandHeldHandler {
    internal static void ClearInventory(MechDef mech, List<MechComponentRef> result, SimGameState state) {
      result.RemoveAll((Predicate<MechComponentRef>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i) && !i.IsModuleFixed(mech)));
      int num = result.Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i))).Select<MechComponentRef, int>((Func<MechComponentRef, int>)(i => MechComponentRefExtensions.GetComponent<HandHeldInfo>(i).SlotSize)).Sum();
      HandHeldHandler.DefInfo[] defaults = HandHeldHandler.GetDefaults(mech);
      if (defaults[2] != null && num == 0) {
        MechComponentRef mechComponentRef = DefaultHelper.CreateRef(defaults[2].id, defaults[2].type, ChassisLocations.CenterTorso);
        mechComponentRef.SetData(ChassisLocations.CenterTorso, 0, ComponentDamageLevel.Functional, false);
        result.Add(mechComponentRef);
      } else {
        if (num >= 2)
          return;
        MechComponentRef mechComponentRef1 = DefaultHelper.CreateRef(defaults[0].id, defaults[0].type, ChassisLocations.CenterTorso);
        mechComponentRef1.SetData(ChassisLocations.CenterTorso, 0, ComponentDamageLevel.Functional, false);
        result.Add(mechComponentRef1);
        if (num == 0) {
          MechComponentRef mechComponentRef2 = DefaultHelper.CreateRef(defaults[0].id, defaults[1].type, ChassisLocations.CenterTorso);
          mechComponentRef2.SetData(ChassisLocations.CenterTorso, 0, ComponentDamageLevel.Functional, false);
          result.Add(mechComponentRef2);
        }
      }
    }

    internal static string PostValidator(MechLabItemSlotElement drop_item,MechDef mech,List<InvItem> new_inventory,List<IChange> changes) {
      List<MechComponentRef> list = new_inventory.Select<InvItem, MechComponentRef>((Func<InvItem, MechComponentRef>)(i => i.Item)).ToList<MechComponentRef>();
      float carryWeight = CarryWeightTools.GetCarryWeight(mech, (IEnumerable<MechComponentRef>)list);
      int num1 = CarryWeightTools.NumOfHands(mech, (IEnumerable<MechComponentRef>)list);
      IEnumerable<HandHeldInfo> handHeldInfos = new_inventory.Where<InvItem>((Func<InvItem, bool>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i.Item))).Select<InvItem, HandHeldInfo>((Func<InvItem, HandHeldInfo>)(i => MechComponentRefExtensions.GetComponent<HandHeldInfo>(i.Item)));
      float usedWeight = CarryWeightTools.GetUsedWeight(new_inventory.Select<InvItem, MechComponentRef>((Func<InvItem, MechComponentRef>)(i => i.Item)));
      int num2 = 0;
      foreach (HandHeldInfo handHeldInfo in handHeldInfos) {
        int num3 = handHeldInfo.HandsUsed ? handHeldInfo.hands_used(carryWeight) : 0;
        num2 += num3;
      }
      if (num2 > num1)
        return new Text(string.Format(Control.Instance.Settings.ValidateHands, (object)num2), Array.Empty<object>()).ToString(true);
      return (double)usedWeight > (double)carryWeight + 0.001 ? new Text(string.Format(Control.Instance.Settings.ValidateTonnage, (object)(float)((double)usedWeight - (double)carryWeight)), Array.Empty<object>()).ToString(true) : string.Empty;
    }

    internal static void ValidateMech(
      Dictionary<MechValidationType, List<Text>> errors,
      MechValidationLevel validationLevel,
      MechDef mechDef) {
      int num1 = CarryWeightTools.NumOfHands(mechDef, (IEnumerable<MechComponentRef>)mechDef.Inventory);
      float carryWeight = CarryWeightTools.GetCarryWeight(mechDef, (IEnumerable<MechComponentRef>)mechDef.Inventory);
      int num2 = 0;
      float usedWeight = CarryWeightTools.GetUsedWeight((IEnumerable<MechComponentRef>)mechDef.Inventory);
      foreach (HandHeldInfo handHeldInfo in ((IEnumerable<MechComponentRef>)mechDef.Inventory).Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i))).Select<MechComponentRef, HandHeldInfo>((Func<MechComponentRef, HandHeldInfo>)(i => MechComponentRefExtensions.GetComponent<HandHeldInfo>(i))))
        num2 += handHeldInfo.HandsUsed ? handHeldInfo.hands_used(carryWeight) : 0;
      if (num1 < num2)
        errors[MechValidationType.InvalidInventorySlots].Add(new Text(string.Format(Control.Instance.Settings.ValidateHands, (object)num2), Array.Empty<object>()));
      if ((double)carryWeight + 0.001 >= (double)usedWeight)
        return;
      errors[MechValidationType.InvalidInventorySlots].Add(new Text(Control.Instance.Settings.ValidateTonnage, new object[1]
      {
        (object) (float) ((double) usedWeight - (double) carryWeight)
      }));
    }

    internal static void AdjustDefaultsMechlab(MechLabPanel mechLab) {
      MechLabHelper mechLabHelper = new MechLabHelper(mechLab);
      MechDef activeMechDef = mechLab.activeMechDef;
      //TODO
      //foreach (MechComponentRef mechComponentRef in ((IEnumerable<MechComponentRef>)activeMechDef.Inventory).Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => DefaultHelper.IsDefault(i) && MechComponentRefExtensions.Is<HandHeldInfo>(i))).ToList<MechComponentRef>()) {
      //  DefaultHelper.RemoveMechLab(mechComponentRef.ComponentDefID, mechComponentRef.ComponentDefType, mechLabHelper, ChassisLocations.CenterTorso);
      //}
      //activeMechDef.SetInventory(((IEnumerable<MechComponentRef>)activeMechDef.Inventory).Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => !DefaultHelper.IsDefault(i) || !MechComponentRefExtensions.Is<HandHeldInfo>(i))).ToArray<MechComponentRef>());
      HandHeldInfo res;
      int num = ((IEnumerable<MechComponentRef>)activeMechDef.Inventory).Sum<MechComponentRef>((Func<MechComponentRef, int>)(i => !i.Is<HandHeldInfo>(out res) ? 0 : res.SlotSize));
      HandHeldHandler.DefInfo[] defaults = HandHeldHandler.GetDefaults(mechLab.activeMechDef);
      if (num == 0 && defaults[2] != null) {
        DefaultHelper.AddMechLab(defaults[2].id, defaults[2].type, ChassisLocations.CenterTorso);
      } else {
        if (num >= 2)
          return;
        DefaultHelper.AddMechLab(defaults[0].id, defaults[0].type, ChassisLocations.CenterTorso);
        if (num == 0)
          DefaultHelper.AddMechLab(defaults[1].id, defaults[1].type, ChassisLocations.CenterTorso);
      }
    }

    internal static void AdjustDefaults(MechDef mech, SimGameState state) {
      bool flag = mech.MechTags.Contains("hh_test_mech");
      if (flag)
        Control.Instance.LogDebug("- " + mech.Description.Id);
      //TODO //mech.SetInventory(((IEnumerable<MechComponentRef>)mech.Inventory).Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => !DefaultHelper.IsDefault(i) || !MechComponentRefExtensions.Is<HandHeldInfo>(i))).ToArray<MechComponentRef>());
      HandHeldInfo res;
      int num = ((IEnumerable<MechComponentRef>)mech.Inventory).Sum<MechComponentRef>((Func<MechComponentRef, int>)(i => !i.Is<HandHeldInfo>(out res) ? 0 : res.SlotSize));
      HandHeldHandler.DefInfo[] defaults = HandHeldHandler.GetDefaults(mech);
      if (num == 0 && defaults[2] != null) {
        if (flag)
          Control.Instance.LogDebug("-- Adding 2h default " + defaults[2].id);
        DefaultHelper.AddInventory(defaults[2].id, mech, ChassisLocations.CenterTorso, defaults[2].type, state);
      } else {
        if (num >= 2)
          return;
        DefaultHelper.AddInventory(defaults[0].id, mech, ChassisLocations.CenterTorso, defaults[0].type, state);
        if (flag)
          Control.Instance.LogDebug("-- Adding 1h default " + defaults[0].id);
        if (num == 0) {
          if (flag)
            Control.Instance.LogDebug("-- Adding 1h default " + defaults[1].id);
          DefaultHelper.AddInventory(defaults[1].id, mech, ChassisLocations.CenterTorso, defaults[1].type, state);
        }
      }
    }

    public static HandHeldHandler.DefInfo[] GetDefaults(MechDef mech) {
      HandHeldDefault res;
      return mech.Chassis.Is<HandHeldDefault>(out res) ? new HandHeldHandler.DefInfo[3]
      {
        new HandHeldHandler.DefInfo(res.Item1H_ID1, res.Type1),
        new HandHeldHandler.DefInfo(res.Item1H_ID2, res.Type2),
        string.IsNullOrEmpty(res.Item2H_ID) ? (HandHeldHandler.DefInfo) null : new HandHeldHandler.DefInfo(res.Item2H_ID, res.TypeH)
      } : new HandHeldHandler.DefInfo[3]
      {
        new HandHeldHandler.DefInfo(),
        new HandHeldHandler.DefInfo(),
        null
      };
    }

    internal static bool CanBeFielded(MechDef mechDef) {
      int num = CarryWeightTools.NumOfHands(mechDef, (IEnumerable<MechComponentRef>)mechDef.Inventory);
      float carryWeight = CarryWeightTools.GetCarryWeight(mechDef, (IEnumerable<MechComponentRef>)mechDef.Inventory);
      float usedWeight = CarryWeightTools.GetUsedWeight((IEnumerable<MechComponentRef>)mechDef.Inventory);
      foreach (HandHeldInfo handHeldInfo in ((IEnumerable<MechComponentRef>)mechDef.Inventory).Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i))).Select<MechComponentRef, HandHeldInfo>((Func<MechComponentRef, HandHeldInfo>)(i => MechComponentRefExtensions.GetComponent<HandHeldInfo>(i))))
        num -= handHeldInfo.HandsUsed ? handHeldInfo.hands_used(carryWeight) : 0;
      return (double)carryWeight - (double)usedWeight >= -0.001 && num >= 0;
    }

    internal static void AutoFixMech(List<MechDef> mechDefs, SimGameState simgame) {
      Control.Instance.LogDebug("AutoFixing start");
      foreach (MechDef mechDef in mechDefs) {
        try {
          HandHeldHandler.AdjustDefaults(mechDef, simgame);
        } catch (Exception ex) {
          Control.Instance.LogError("Error while fixing " + mechDef.Description.Id + ": ", ex);
        }
      }
      Control.Instance.LogDebug("AutoFixing done");
    }

    public class DefInfo {
      public string id;
      public ComponentType type;

      public DefInfo() {
        this.id = Control.Instance.Settings.HandHeldSlotItemID;
        this.type = ComponentType.Upgrade;
      }

      public DefInfo(string id, ComponentType type) {
        if (string.IsNullOrEmpty(id)) {
          this.id = Control.Instance.Settings.HandHeldSlotItemID;
          this.type = ComponentType.Upgrade;
        } else {
          this.id = id;
          this.type = type;
        }
      }
    }
  }
  public static class CarryWeightTools {
    public static TextMeshProUGUI TextElement { get; internal set; }

    public static LocationHelper Location { get; internal set; }

    public static LocationHelper CenterTorso { get; internal set; }

    public static float GetCarryWeight(MechDef mech, IEnumerable<MechComponentRef> inventory) {
      float carryWeightFactor = Control.Instance.Settings.CarryWeightFactor;
      float num1 = 0.0f;
      foreach (MechComponentRef mechComponentRef in inventory) {
        TSMInfoComponent res;
        if (mechComponentRef.Is<TSMInfoComponent>(out res)) {
          if (Control.Instance.Settings.MultiplicativeTonnageFactor)
            carryWeightFactor *= res.HandHeldFactor;
          else
            num1 += res.HandHeldFactor - 1f;
        } else if (Control.Instance.Settings.TSMTags != null && (uint)Control.Instance.Settings.TSMTags.Length > 0U) {
          foreach (TSMTagInfo tsmTag in Control.Instance.Settings.TSMTags) {
            if (mechComponentRef.Def.ComponentTags.Contains(tsmTag.Tag) || CategoriesExtentions.IsCategory(mechComponentRef, tsmTag.Tag)) {
              if (Control.Instance.Settings.MultiplicativeTonnageFactor)
                carryWeightFactor *= tsmTag.Mul;
              else
                num1 += tsmTag.Mul - 1f;
            }
          }
        }
      }
      if (!Control.Instance.Settings.MultiplicativeTonnageFactor)
        carryWeightFactor *= 1f + num1;
      float num2 = inventory.Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => MechComponentRefExtensions.Is<AddCarryWeight>(i))).Select<MechComponentRef, AddCarryWeight>((Func<MechComponentRef, AddCarryWeight>)(i => MechComponentRefExtensions.GetComponent<AddCarryWeight>(i))).Sum<AddCarryWeight>((Func<AddCarryWeight, float>)(i => i.AddTonnage));
      return Mathf.Ceil((float)((double)mech.Chassis.Tonnage * (double)carryWeightFactor * 100.0)) / 100f + num2;
    }

    public static float GetUsedWeight(IEnumerable<MechComponentRef> inventory) => inventory.Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => MechComponentRefExtensions.Is<IUseCarryTonnage>(i))).Select<MechComponentRef, IUseCarryTonnage>((Func<MechComponentRef, IUseCarryTonnage>)(i => MechComponentRefExtensions.GetComponent<IUseCarryTonnage>(i))).Sum<IUseCarryTonnage>((Func<IUseCarryTonnage, float>)(i => i.Tonnage));

    public static int NumOfHands(MechDef mech, IEnumerable<MechComponentRef> inventory) {
      return 2;
      //TODO
      //ArmActuator res;
      //return Control.Settings.UseHandTag ? inventory.Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => i.Def.ComponentTags.Contains(Control.Instance.Settings.HandsItemTag))).Count<MechComponentRef>() : inventory.Where<MechComponentRef>((Func<MechComponentRef, bool>)(i => i.Is<ArmActuator>(out res) && ((Enum)(object)res.get_Type()).HasFlag((Enum)(object)(ArmActuatorSlot)15))).Count<MechComponentRef>();
    }
  }

  [CustomComponent("HandHeld")]
  public class HandHeldInfo : SimpleCustomComponent, IMechLabFilter, IPreValidateDrop, /*IOnInstalled,*/ IOnItemGrab, IReplaceValidateDrop, IUseCarryTonnage {
    public bool HandsUsed = true;
    public int SlotSize = 1;
    public bool ForceTwoHand = false;

    public float Tonnage { get; set; } = 5f;

    public int hands_used(float tonnage) => !this.ForceTwoHand && (double)this.Tonnage < (double)tonnage / 2.0 + 0.001 ? 1 : 2;

    public bool CheckFilter(MechLabPanel panel) {
      if (Control.Instance.Settings.Debug_IgnoreWeightFIlter || panel.activeMechDef == null)
        return true;
      MechDef activeMechDef = panel.activeMechDef;
      return (double)CarryWeightTools.GetCarryWeight(activeMechDef, (IEnumerable<MechComponentRef>)activeMechDef.Inventory) - (double)this.Tonnage >= -0.001;
    }

    public void OnInstalled(
      WorkOrderEntry_InstallComponent order,
      SimGameState state,
      MechDef mech) {
      HandHeldHandler.AdjustDefaults(mech, state);
    }

    public void OnItemGrab(IMechLabDraggableItem item,MechLabPanel mechLab,MechLabLocationWidget widget) {
      HandHeldHandler.AdjustDefaultsMechlab(mechLab);
    }

    public string PreValidateDrop(MechLabItemSlotElement item,LocationHelper location) {
      MechDef activeMechDef = UIManager.Instance.UIRoot.gameObject.GetComponentInChildren<MechLabPanel>().activeMechDef;
      float carryWeight = CarryWeightTools.GetCarryWeight(activeMechDef, (IEnumerable<MechComponentRef>)activeMechDef.Inventory);
      if (this.HandsUsed) {
        int num1 = CarryWeightTools.NumOfHands(activeMechDef, (IEnumerable<MechComponentRef>)activeMechDef.Inventory);
        int num2 = this.hands_used(carryWeight);
        if (num2 > num1)
          return string.Format(num2 == 1 ? Control.Instance.Settings.OneHandMissed : Control.Instance.Settings.TwoHandMissed, (object)this.Def.Description.Name);
      }
      return (double)carryWeight + 0.001 < (double)this.Tonnage ? string.Format(Control.Instance.Settings.WrongWeightMessage, (object)this.Def.Description.Name, (object)this.Tonnage, (object)carryWeight) : string.Empty;
    }

    public string ReplaceValidateDrop(MechLabItemSlotElement drop_item,LocationHelper location, List<IChange> changes) {
      MechDef activeMechDef = location.mechLab.activeMechDef;
      Control.Instance.LogDebug("HandHeld Replacement");
      if (this.SlotSize == 2) {
        Control.Instance.LogDebug("- SlotSize = 2");
        foreach (MechLabItemSlotElement labItemSlotElement in location.LocalInventory.Where<MechLabItemSlotElement>((Func<MechLabItemSlotElement, bool>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i.ComponentRef)))) {
          Control.Instance.LogDebug("-- removing " + labItemSlotElement.ComponentRef.ComponentDefID);
          if (labItemSlotElement.ComponentRef.IsModuleFixed(activeMechDef)) {
            Control.Instance.LogDebug("--- fixed. break");
            return new Text("Cannot replace " + labItemSlotElement.ComponentRef.Def.Description.Name, Array.Empty<object>()).ToString(true);
          }
          changes.Add((IChange)new Change_Remove(labItemSlotElement.ComponentRef.ComponentDefID, location.widget.loadout.Location));
        }
        Control.Instance.LogDebug("-- done");
      } else {
        Control.Instance.LogDebug("- SlotSize = 1");
        float tonnage = CarryWeightTools.GetCarryWeight(activeMechDef, (IEnumerable<MechComponentRef>)activeMechDef.Inventory);
        var list = location.LocalInventory.Where<MechLabItemSlotElement>((Func<MechLabItemSlotElement, bool>)(i => MechComponentRefExtensions.Is<HandHeldInfo>(i.ComponentRef))).Select(i => new {
          item = i,
          cr = i.ComponentRef,
          hh = MechComponentRefExtensions.GetComponent<HandHeldInfo>(i.ComponentRef),
          hu = MechComponentRefExtensions.GetComponent<HandHeldInfo>(i.ComponentRef).hands_used(tonnage)
        }).ToList();
        if (list.Count == 1) {
          Control.Instance.LogDebug("- 1 item to replace");
          if (list[0].cr.IsModuleFixed(activeMechDef)) {
            Control.Instance.LogDebug("-- fixed. break");
            return new Text("Cannot replace fixed " + list[0].cr.Def.Description.Name, Array.Empty<object>()).ToString(true);
          }
          Control.Instance.LogDebug("-- removing " + list[0].cr.ComponentDefID);
          changes.Add((IChange)new Change_Remove(list[0].item.ComponentRef.ComponentDefID,location.widget.loadout.Location));
          HandHeldHandler.DefInfo[] defaults = HandHeldHandler.GetDefaults(activeMechDef);
          Control.Instance.LogDebug("-- adding second default " + defaults[0].id);
          //TODO //changes.Add((IChange)new AddDefaultChange(location.widget.loadout.Location, DefaultHelper.CreateSlot(defaults[0].id, defaults[0].type, location.mechLab)));
          Control.Instance.LogDebug("-- done");
        } else if (list.Count == 2) {
          Control.Instance.LogDebug("- 2 item to replace");
          bool flag1 = list[0].cr.IsModuleFixed(activeMechDef);
          bool flag2 = list[1].cr.IsModuleFixed(activeMechDef);
          if (flag1 & flag2) {
            Control.Instance.LogDebug("-- both fixed. break");
            return new Text("Cannot replace fixed equipment", Array.Empty<object>()).ToString(true);
          }
          if (flag1 | flag2) {
            Control.Instance.LogDebug("-- one fixed. replacing " + list[flag1 ? 1 : 0].cr.ComponentDefID);
            changes.Add((IChange)new Change_Remove(list[flag1 ? 1 : 0].item.ComponentRef.ComponentDefID, location.widget.loadout.Location));
            Control.Instance.LogDebug("-- done");
          } else {
            int num1 = CarryWeightTools.NumOfHands(activeMechDef, (IEnumerable<MechComponentRef>)activeMechDef.Inventory);
            int num2 = this.hands_used(tonnage);
            //TODO
            bool flag3 = false;//DefaultHelper.IsDefault(list[0].cr);
            bool flag4 = false;//DefaultHelper.IsDefault(list[1].cr);
            HandHeldHandler.DefInfo[] defaults = HandHeldHandler.GetDefaults(activeMechDef);
            if (flag3 & flag4) {
              Control.Instance.LogDebug("-- both default. removing " + list[list[0].cr.ComponentDefID == defaults[1].id ? 0 : 1].cr.ComponentDefID);
              changes.Add((IChange)new Change_Remove(list[list[0].cr.ComponentDefID == defaults[1].id ? 0 : 1].item.ComponentRef.ComponentDefID,location.widget.loadout.Location));
              Control.Instance.LogDebug("-- done");
            } else if (flag3 | flag4) {
              Control.Instance.LogDebug("-- one default");
              if (this.HandsUsed) {
                var data = list[flag3 ? 1 : 0];
                if (data.hh.HandsUsed && data.hu + num2 > num1) {
                  Control.Instance.LogDebug("-- not enough hands for both. removing non default " + list[flag3 ? 1 : 0].cr.ComponentDefID);
                  changes.Add((IChange)new Change_Remove(list[flag3 ? 1 : 0].item.ComponentRef.ComponentDefID,location.widget.loadout.Location));
                } else {
                  Control.Instance.LogDebug("-- enough hands for both. removing default " + list[flag3 ? 0 : 1].cr.ComponentDefID);
                  changes.Add((IChange)new Change_Remove(list[flag3 ? 0 : 1].item.ComponentRef.ComponentDefID, location.widget.loadout.Location));
                }
              } else {
                Control.Instance.LogDebug("-- no handuseds. removing " + list[flag3 ? 0 : 1].cr.ComponentDefID);
                changes.Add((IChange)new Change_Remove(list[flag3 ? 0 : 1].item.ComponentRef.ComponentDefID, location.widget.loadout.Location));
              }
              Control.Instance.LogDebug("-- done");
            } else {
              bool flag5 = !list[0].hh.HandsUsed;
              bool flag6 = !list[1].hh.HandsUsed;
              if (flag5 & flag6 || !(flag5 | flag6)) {
                Control.Instance.LogDebug("-- no defaults, both same type. removing first " + list[0].cr.ComponentDefID);
                changes.Add((IChange)new Change_Remove(list[0].item.ComponentRef.ComponentDefID, location.widget.loadout.Location));
              } else if (num1 == 1) {
                if (this.HandsUsed)
                  changes.Add((IChange)new Change_Remove(list[flag5 ? 1 : 0].item.ComponentRef.ComponentDefID,location.widget.loadout.Location));
                else
                  changes.Add((IChange)new Change_Remove(list[flag5 ? 0 : 1].item.ComponentRef.ComponentDefID,location.widget.loadout.Location));
              } else
                changes.Add((IChange)new Change_Remove(list[flag5 ? 0 : 1].item.ComponentRef.ComponentDefID,location.widget.loadout.Location));
            }
          }
        } else {
          Control.Instance.LogError("WRONG HAND HELDS COUNT: " + list.Count.ToString());
          return "Invalid mech, check logs";
        }
      }
      return string.Empty;
    }

    public string PreValidateDrop(MechLabItemSlotElement item, ChassisLocations location) {
      throw new System.NotImplementedException();
    }

    public string ReplaceValidateDrop(MechLabItemSlotElement drop_item, ChassisLocations location, Queue<IChange> changes) {
      throw new NotImplementedException();
    }

    public bool OnItemGrab(IMechLabDraggableItem item, MechLabPanel mechLab, out string error) {
      throw new NotImplementedException();
    }
  }
}