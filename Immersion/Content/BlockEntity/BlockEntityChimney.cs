using System;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Neolithic
{
    class BlockEntityChimney : BlockEntityParticleEmitter
    {
        private AssetLocation[][] lookFor;
        long listenerId;
        Block ownBlock;
        int searchRadius;

        public override void Initialize(ICoreAPI api)
        {
            ownBlock = api.World.BlockAccessor.GetBlock(pos) as Block;
            if (lookFor == null)
            {
                lookFor = ownBlock.Attributes["lookFor"].AsObject<AssetLocation[][]>();
            }

            searchRadius = ownBlock.Attributes["searchRadius"].AsInt();

            if (api.World is IClientWorldAccessor)
            {
                listenerId = api.World.RegisterGameTickListener(ticker, 5000);
            }
            base.Initialize(api);
        }

        public override void OnBlockRemoved()
        {
            api.World.UnregisterGameTickListener(listenerId);
            base.OnBlockRemoved();
        }

        public void ticker(float dt)
        {
            checkBelow(pos, api);
        }

        public void checkBelow(BlockPos pos, ICoreAPI api)
        {
            bool exists = false;
            Block check;
            foreach (var val in lookFor)
            {
                for (int y = 1; y < pos.Y; y++)
                {
                    for (int x = -searchRadius; x <= searchRadius; x++)
                    {
                        for (int z = -searchRadius; z <= searchRadius; z++)
                        {
                            check = api.World.BlockAccessor.GetBlock(pos + new BlockPos(x, -y, z));
                            if (new ItemStack(check).Collectible.WildCardMatch(val[0]))
                            {
                                exists = true;
                                break;
                            }
                        }
                    }
                }
                check = api.World.BlockAccessor.GetBlock(pos);
                if (exists && new ItemStack(check).Collectible.WildCardMatch(val[1]))
                {
                    api.World.BlockAccessor.SetBlock(api.World.GetBlock(val[2]).BlockId, pos);
                    break;
                }
                if (!exists && new ItemStack(check).Collectible.WildCardMatch(val[2]))
                {
                    api.World.BlockAccessor.SetBlock(api.World.GetBlock(val[1]).BlockId, pos);
                    break;
                }
            }
            return;
        }
    }
}
