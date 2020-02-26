using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    public class BlockFixedStairs : BlockStairs
    {
		string type, material, updown, outside, inside;
		Block north, south, east, west;

        public bool Side { get => Code.ToString().Contains("stair-side"); }
        public bool Corner { get => Code.ToString().Contains("stair-corner"); }

        public override void OnLoaded(ICoreAPI Api)
		{
			base.OnLoaded(Api);

			type = FirstCodePart(2);
			material = FirstCodePart(3);
			updown = LastCodePart(1);
			outside = "stair-corner-" + type + "-" + material + "-" + "outside" + "-" + updown;
			inside = "stair-corner-" + type + "-" + material + "-" + "inside" + "-" + updown;
			north = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-north").GetBlock(Api);
			south = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-south").GetBlock(Api);
			east = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-east").GetBlock(Api);
			west = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-west").GetBlock(Api);
		}

		public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos Pos, BlockPos neibpos)
        {
            Block nBlock = neibpos.GetBlock(world);
            if (!(nBlock is BlockFixedStairs)) return;

            if (world.Side.IsServer())
            {
                if (Side)
                {
                    StairsCheck(world, Pos);
                }
                else if (Corner)
                {
                    CornersCheck(world, Pos);
                }
            }
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);
            if (world.Side.IsServer())
            {
                if (Side)
                {
                    StairsCheck(world, blockPos);
                }
            }
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos Pos)
        {
			return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("stair-side-" + FirstCodePart(2) + "-" + FirstCodePart(3) + "-up-north")));
		}

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos Pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("stair-side-" + FirstCodePart(2) + "-" + FirstCodePart(3) + "-up-north"))) };
        }

        public void StairsCheck(IWorldAccessor world, BlockPos Pos)
        {
            AssetLocation[] cardinal = GetCardinal(world, Pos);
            var bA = world.BlockAccessor;

            if (!cardinal.Any(val => bA.GetBlock(val).WildCardMatch(new AssetLocation("*stair-side*")))) return;
            string cardinalN = bA.GetBlock(cardinal[0]).LastCodePart();
            string cardinalS = bA.GetBlock(cardinal[1]).LastCodePart();
            string cardinalE = bA.GetBlock(cardinal[2]).LastCodePart();
            string cardinalW = bA.GetBlock(cardinal[3]).LastCodePart();


            if (cardinalW == "east")
            {
                if (cardinalN == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-northeast")).BlockId, Pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-southeast")).BlockId, Pos);
                }
            }
            else if (cardinalW == "west")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-southwest")).BlockId, Pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-northwest")).BlockId, Pos);
                }
            }
            else if (cardinalE == "east")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-southeast")).BlockId, Pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-northeast")).BlockId, Pos);
                }
            }
            else if (cardinalE == "west")
            {
                if (cardinalN == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-northwest")).BlockId, Pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-southwest")).BlockId, Pos);
                }
            }
        }

        public void CornersCheck(IWorldAccessor world, BlockPos Pos)
        {
            var bA = world.BlockAccessor;
            AssetLocation[] cardinal = GetCardinal(world, Pos);

            string cardinalN = bA.GetBlock(cardinal[0]).LastCodePart();
            string cardinalS = bA.GetBlock(cardinal[1]).LastCodePart();
            string cardinalE = bA.GetBlock(cardinal[2]).LastCodePart();
            string cardinalW = bA.GetBlock(cardinal[3]).LastCodePart();

			switch (LastCodePart())
			{
				case "northeast":
					if (cardinalN == "north")
					{
						bA.SetBlock(north.BlockId, Pos);
					}
					else if (cardinalE == "east")
					{
						bA.SetBlock(east.BlockId, Pos);
					}
					else if (cardinalW == "east")
					{
						bA.SetBlock(east.BlockId, Pos);
					}
					else if (cardinalS == "north")
					{
						bA.SetBlock(north.BlockId, Pos);
					}
					break;
				case "southwest":
					if (cardinalS == "south")
					{
						bA.SetBlock(south.BlockId, Pos);
					}
					else if (cardinalE == "west")
					{
						bA.SetBlock(west.BlockId, Pos);
					}
					else if (cardinalN == "south")
					{
						bA.SetBlock(south.BlockId, Pos);
					}
					else if (cardinalW == "west")
					{
						bA.SetBlock(west.BlockId, Pos);
					}
					break;
				case "southeast":
					if (cardinalN == "south")
					{
						bA.SetBlock(south.BlockId, Pos);
					}
					else if (cardinalE == "east")
					{
						bA.SetBlock(east.BlockId, Pos);
					}
					else if (cardinalS == "south")
					{
						bA.SetBlock(south.BlockId, Pos);
					}
					else if (cardinalW == "east")
					{
						bA.SetBlock(east.BlockId, Pos);
					}
					break;
				case "northwest":
					if (cardinalW == "west")
					{
						bA.SetBlock(west.BlockId, Pos);
					}
					else if (cardinalS == "north")
					{
						bA.SetBlock(north.BlockId, Pos);
					}
					else if (cardinalE == "west")
					{
						bA.SetBlock(west.BlockId, Pos);
					}
					else if (cardinalN == "north")
					{
						bA.SetBlock(north.BlockId, Pos);
					}
					break;
				default:
					break;
			}
        }

        public AssetLocation[] GetCardinal(IWorldAccessor world, BlockPos Pos)
        {
            var bA = world.BlockAccessor;
            AssetLocation[] cardinal = {
                bA.GetBlock(Pos.NorthCopy()).Code,
                bA.GetBlock(Pos.SouthCopy()).Code,
                bA.GetBlock(Pos.EastCopy()).Code,
                bA.GetBlock(Pos.WestCopy()).Code,
            };
            return cardinal;
        }
    }
}
