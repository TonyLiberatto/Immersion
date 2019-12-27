using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    public class BlockGiantReeds : BlockReeds
    {
        public bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos Pos)
        {
            Block block = blockAccessor.GetBlock(Pos.DownCopy());
            return block.Fertility > 0 || (Attributes?["stackable"]?.AsBool() == true && block.Attributes?["stackable"]?.AsBool() == true && block is BlockGiantReeds);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos Pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                ItemStack drop = null;
                if (Variant["state"] == "normal")
                {
                    drop = new ItemStack(world.GetItem(new AssetLocation("reeds")), 10);
                }
                else
                {
                    drop = new ItemStack(world.GetItem(new AssetLocation("giantreedsroot")));
                }
                if (drop != null)
                {
                    world.SpawnItemEntity(drop, new Vec3d(Pos.X + 0.5, Pos.Y + 0.5, Pos.Z + 0.5), null);
                }
            }
            if (byPlayer != null && Variant["state"] == "normal" && byPlayer.InventoryManager.ActiveTool == EnumTool.Knife)
            {
                world.BlockAccessor.SetBlock(world.GetBlock(CodeWithParts("harvested")).BlockId, Pos);
                return;
            }

            if (Variant["habitat"] != "free")
            {
                world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water-still-7")).BlockId, Pos);
            }
            else
            {
                world.BlockAccessor.SetBlock(0, Pos);
            }
        }
    }
}
