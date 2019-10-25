using System;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Immersion
{
    class ChimneyBehavior : BlockBehavior
    {
        private AssetLocation[][] lookFor;
        public BlockPos Pos;
        public IWorldAccessor world;
        long listenerId;
        public ChimneyBehavior(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            lookFor = properties["lookFor"].AsObject<AssetLocation[][]>();
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            Pos = blockSel.Position;
            this.world = world;
            listenerId = world.RegisterGameTickListener(ticker, 5000);
            return true;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos Pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            world.UnregisterGameTickListener(listenerId);
        }

        public void ticker(float dt)
        {
            checkBelow(Pos, world);
        }

        public void checkBelow(BlockPos Pos, IWorldAccessor world)
        {
            bool exists = false;
            Block check;
            foreach (var val in lookFor)
            {
                for (int y = 1; y < Pos.Y; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            check = world.BlockAccessor.GetBlock(Pos + new BlockPos(x, -y, z));
                            if (new ItemStack(check).Collectible.WildCardMatch(val[0]))
                            {
                                exists = true;
                                break;
                            }
                        }
                    }
                }
                check = world.BlockAccessor.GetBlock(Pos);
                if (exists && new ItemStack(check).Collectible.WildCardMatch(val[1]))
                {
                    world.BlockAccessor.SetBlock(world.GetBlock(val[2]).BlockId, Pos);
                    break;
                }
                if (!exists && new ItemStack(check).Collectible.WildCardMatch(val[2]))
                {
                    world.BlockAccessor.SetBlock(world.GetBlock(val[1]).BlockId, Pos);
                    break;
                }
            }
            return;
        }
    }
}
