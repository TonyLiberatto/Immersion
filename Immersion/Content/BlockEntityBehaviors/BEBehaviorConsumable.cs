using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Immersion
{
    public class BEBehaviorConsumable : BlockEntityBehavior, IAnimalFoodSource
    {
        public BEBehaviorConsumable(BlockEntity blockentity) : base(blockentity)
        {
        }

        public Vec3d Position { get => Blockentity.Pos.MidPoint(); }
        public Block OwnBlock { get => Blockentity.Block; }

        public ContentConfig config;
        public AssetLocation eatenTo;

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            config = properties["contentConfig"].AsObject<ContentConfig>();
            eatenTo = properties["eatenTo"].AsString()?.WithDomain(OwnBlock.Code.Domain)?.ToAsset();
            if (api.Side.IsServer())
            {
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
            }
        }

        public string Type => "food";

        public float ConsumeOnePortion()
        {
            Block toBlock = eatenTo?.GetBlock(Api);
            if (config == null || toBlock == null) return 0f;
            Api.World.BlockAccessor.SetBlock(toBlock.BlockId, Blockentity.Pos);
            return 1f;
        }

        public bool IsSuitableFor(Entity entity)
        {
            if (config == null) return false;
            for (int index = 0; index < config.Foodfor.Length; ++index)
            {
                if (WildcardUtil.Match(config.Foodfor[index], entity.Code)) return true;
            }
            return false;
        }
    }
}
