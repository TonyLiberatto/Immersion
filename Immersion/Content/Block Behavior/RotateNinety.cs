// RotateNinety by Stroam
//
// To the extent possible under law, the person who associated CC0 with
// this project has waived all copyright and related or neighboring rights
// to this project.
//
// You should have received a copy of the CC0 legalcode along with this
// work.  If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Neolithic.Utility;

namespace Neolithic
{
    // Used for the post, should change shape based on side connections.
    class RotateNinety : BlockBehavior
    {
        public RotateNinety(Block block) : base(block)
        {
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            handling = EnumHandling.PreventDefault;
            if (!world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(block))
            {
                return false;
            }
            BlockFacing[] blockFacingArray = Block.SuggestedHVOrientation(byPlayer, blockSel);
            string orientation = blockFacingArray[0] == BlockFacing.NORTH || blockFacingArray[0] == BlockFacing.SOUTH ? "n" : "w";

            AssetLocation assetLocation = block.CodeWithParts(orientation);
            world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(assetLocation).BlockId, blockSel.Position);
            return true;
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            return new ItemStack((block.Code.Domain + ":" + block.CodeWithoutParts(1) + "-n").ToAsset().GetBlock(world.Api), 1);
        }

        public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handling)
        {

            string[] strArray = new string[2] { "w", "n" };
            int num = angle / 90;
            if (block.Variant["type"] == "n")
                ++num;
            return block.CodeWithParts( strArray[num % 2]);
        }

    }
}