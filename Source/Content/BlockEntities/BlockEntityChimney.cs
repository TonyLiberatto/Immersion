using System;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    class BlockEntityChimney : BlockEntityParticleEmitter
    {
        private AssetLocation[][] lookFor;
        Block ownBlock;
        int searchRadius;

        public override void Initialize(ICoreAPI Api)
        {
            ownBlock = Api.World.BlockAccessor.GetBlock(Pos) as Block;
            if (lookFor == null)
            {
                lookFor = ownBlock.Attributes["lookFor"].AsObject<AssetLocation[][]>();
            }

            searchRadius = ownBlock.Attributes["searchRadius"].AsInt();

            if (Api.World is IClientWorldAccessor)
            {
                RegisterGameTickListener(dt => checkBelow(Pos, Api), 5000);
            }
            base.Initialize(Api);
        }

        public void checkBelow(BlockPos Pos, ICoreAPI Api)
        {
            bool exists = false;
            Block check;
            foreach (var val in lookFor)
            {
                for (int y = 1; y < Pos.Y; y++)
                {
                    for (int x = -searchRadius; x <= searchRadius; x++)
                    {
                        for (int z = -searchRadius; z <= searchRadius; z++)
                        {
                            check = Api.World.BlockAccessor.GetBlock(Pos + new BlockPos(x, -y, z));
                            if (new ItemStack(check).Collectible.WildCardMatch(val[0]))
                            {
                                exists = true;
                                break;
                            }
                        }
                    }
                }
                check = Api.World.BlockAccessor.GetBlock(Pos);
                if (exists && new ItemStack(check).Collectible.WildCardMatch(val[1]))
                {
                    Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(val[2]).BlockId, Pos);
                    break;
                }
                if (!exists && new ItemStack(check).Collectible.WildCardMatch(val[2]))
                {
                    Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(val[1]).BlockId, Pos);
                    break;
                }
            }
            return;
        }
    }
}
