using Vintagestory.API.Common;

namespace Neolithic
{
    class CraftingProp
    {
        public JsonItemStack input { get; set; }
        public JsonItemStack[] output { get; set; }
        public EnumTool? tool { get; set; } = EnumTool.Axe;
        public string craftSound { get; set; }
        public int craftTime { get; set; } = 500;
    }

    class DryingProp
    {
        public JsonItemStack Input { get; set; }
        public JsonItemStack Output { get; set; }
        public int? DryingTime { get; set; } = 12;
        public JsonItemStack TextureSource { get; set; }
    }
}