using System.Collections.Generic;
using Immersion.Utility;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Immersion
{
    public class BlockPalmTree : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos Pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, Pos, byPlayer, dropQuantityMultiplier);
            ItemSlot itemslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (FirstCodePart() == "palmlog")
            {
                if (itemslot.Itemstack == null || !Code.ToString().Contains("grown") || !world.Side.IsServer()) return;
                if (itemslot.Itemstack.Collectible.Tool == EnumTool.Axe)
                {
                    for (int x = -2; x <= 2; x++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            for (int y = 0; y <= 16; y++)
                            {
                                BlockPos bPos = new BlockPos(Pos.X + x, Pos.Y + y, Pos.Z + z);
                                Block bBlock = world.BlockAccessor.GetBlock(bPos);
                                if (bBlock is BlockPalmTree || bBlock.FirstCodePart() == "palmfruits")
                                {
                                    if (itemslot.Itemstack == null) return;

                                    world.BlockAccessor.SetBlock(0, bPos);

                                    foreach (ItemStack item in GetDrops(world, bPos, byPlayer))
                                    {
                                        world.SpawnItemEntity(item, bPos.ToVec3d());
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
