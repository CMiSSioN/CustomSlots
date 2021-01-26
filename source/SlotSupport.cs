using CustomComponents;

namespace CustomSlots
{
    [CustomComponent("SlotSupport")]
    public class SlotSupport : SimpleCustomComponent
    {
        public string Type { get; set; }
        public int Count { get; set; } = 0;
    }
}