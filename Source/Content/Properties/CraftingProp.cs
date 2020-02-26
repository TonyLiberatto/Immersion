using Vintagestory.API.Common;

namespace Immersion
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
        public DryingProp(JsonItemStack input, JsonItemStack output, int? dryingTime, JsonItemStack textureSource)
        {
            Input = input;
            Output = output;
            DryingTime = dryingTime;
            TextureSource = textureSource;
        }

        public DryingProp Clone()
        {
            return new DryingProp(Input, Output, DryingTime, TextureSource);
        }

        public JsonItemStack Input { get; set; }
        public JsonItemStack Output { get; set; }
        public int? DryingTime { get; set; } = 12;
        public JsonItemStack TextureSource { get; set; }
    }
}