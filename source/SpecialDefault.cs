using BattleTech;
using CustomComponents;

namespace HandHeld
{

    public interface ISpecialSlotDefaults
    {
        public int MaxSlots { get; }

        public SpecialDefault.record[] Defaults { get; }

        public string DefaultSlotId { get; }
    }


    [CustomComponent("SpecialDefaults")]
    public class SpecialDefault : SimpleCustomChassis, ISpecialSlotDefaults
    {
        public class record
        {
            public string id;
            public ComponentType type;
        }

        public int MaxSlots { get; set; } = -1;

        public record[] Defaults { get; set; }

        public string DefaultSlotId { get; set; } = "";
    }
}