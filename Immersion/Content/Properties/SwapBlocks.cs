using Vintagestory.API.Common;

namespace Neolithic
{
    class SwapBlocks
    {
        public string Takes { get; set; } = "";
        public string Makes { get; set; } = "";
        public string Tool { get; set; } = null;
        public int Count { get; set; } = 0;
        public float MakeTime { get; set; } = 0;

        public SwapBlocks(string takes, string makes, string tool, int count)
        {
            Takes = takes; Makes = makes; Tool = tool; Count = count;
        }

        public SwapBlocks Copy()
        {
            SwapBlocks copy = new SwapBlocks(Takes, Makes, Tool, Count);
            return copy;
        }
    }

    class CreateBlocks
    {
        public JsonItemStack Takes { get; set; }
        public JsonItemStack[] Makes { get; set; }
        public float MakeTime { get; set; } = 0;
        public bool IntoInv { get; set; }
        public bool RemoveOnFinish { get; set; } = false;
    }
}
