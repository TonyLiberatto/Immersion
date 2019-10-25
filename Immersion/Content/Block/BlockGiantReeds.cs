using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Neolithic
{
    public class BlockGiantReeds : BlockReeds
    {
        public bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
        {
            Block block = blockAccessor.GetBlock(pos.DownCopy());
            return block.Fertility > 0 || (Attributes?["stackable"]?.AsBool() == true && block.Attributes?["stackable"]?.AsBool() == true && block is BlockGiantReeds);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
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
                    world.SpawnItemEntity(drop, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                }
            }
            if (byPlayer != null && Variant["state"] == "normal" && byPlayer.InventoryManager.ActiveTool == EnumTool.Knife)
            {
                world.BlockAccessor.SetBlock(world.GetBlock(CodeWithParts("harvested")).BlockId, pos);
                return;
            }

            if (Variant["habitat"] != "free")
            {
                world.BlockAccessor.SetBlock(world.GetBlock(new AssetLocation("water-still-7")).BlockId, pos);
            }
            else
            {
                world.BlockAccessor.SetBlock(0, pos);
            }
        }
    }
}
