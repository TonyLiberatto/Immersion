using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace Immersion
{
    class BlockWashable : Block
    {
        JsonItemStack[] OutputStacks;
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            OutputStacks = Attributes["outputStacks"].AsObject<JsonItemStack[]>();
        }

        public override void OnGroundIdle(EntityItem entityItem)
        {
            base.OnGroundIdle(entityItem);

            IWorldAccessor world = entityItem.World;
            if (world.Side != EnumAppSide.Server) return;

            if (entityItem.Swimming && world.Rand.NextDouble() < 0.01)
            {
                ItemStack[] stacks = OutputStacks.ResolvedStacks(world);

                for (int i = 0; i < stacks.Length; i++)
                {
                    world.SpawnItemEntity(stacks[i], entityItem.ServerPos.XYZ);
                }
            }
        }
    }
}
