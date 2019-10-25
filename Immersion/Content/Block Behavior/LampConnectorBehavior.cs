using System.Text.RegularExpressions;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;


namespace Neolithic
{
    class LampConnectorBehavior : BlockBehavior
    {
        private bool flipable = true;

        public LampConnectorBehavior(Block block) : base(block)
        {
            // NOP
        }

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
        {
            handling = EnumHandling.PreventDefault;
            if (!world.BlockAccessor.GetBlock(blockSel.Position).IsReplacableBy(block))
            {
                return false;
            }
            if (blockSel.Face.IsVertical)
            {
                return false;
            }

            var dir = blockSel.Face.GetOpposite().Code;
            if (flipable)
            {
                var flip = blockSel.HitPosition.Y < 0.5 ? "down" : "up";
                world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(block.CodeWithParts(dir, flip)).BlockId, blockSel.Position);
            }
            else
            {
                var len = getCode(world, blockSel.Position, dir);
                world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(block.CodeWithParts(dir, len)).BlockId, blockSel.Position);
            }
            return true;
        }

        private static Regex dirRe = new Regex(@".*-(?<dir>north|south|east|west)(?:-.*)?");
        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handling)
        {
            if (!flipable)
            {
                var m = dirRe.Match(block.Code.Path);
                var dir = "north";
                var len = "short";
                if (m.Success)
                {
                    dir = m.Result("${dir}");
                    len = getCode(world, pos, dir);
                }

                world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(block.CodeWithParts(dir, len)).BlockId, pos);
            }
        }

        private string getCode(IWorldAccessor world, BlockPos pos, string dir)
        {
            bool solid;
            switch (dir)
            {
                case "north":
                    solid = world.BlockAccessor.GetBlock(pos.EastCopy()).Id != 0;
                    break;
                case "south":
                    solid = world.BlockAccessor.GetBlock(pos.WestCopy()).Id != 0;
                    break;
                case "east":
                    solid = world.BlockAccessor.GetBlock(pos.SouthCopy()).Id != 0;
                    break;
                case "west":
                    solid = world.BlockAccessor.GetBlock(pos.NorthCopy()).Id != 0;
                    break;
                default:
                    // Should be impossible.
                    solid = false;
                    break;
            }
            return solid ? "long" : "short";
        }

        private bool IsLantern(IWorldAccessor world, BlockPos blockPos)
        {
            return world.BlockAccessor.GetBlock(blockPos).Code.Path.Contains("lantern");
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            flipable = properties["type"].AsString("flippable") == "flippable";
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            if (flipable)
            {
                return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts("north", "up")));
            }
            return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts("north", "short")));
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            handling = EnumHandling.PreventDefault;
            if (flipable)
            {
                return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts("east", "up"))) };
            }
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts("east", "long"))) };
        }
    }
}