using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Neolithic
{
    public class FixedStairs : BlockStairs
    {
		string type, material, updown, outside, inside;
		Block north, south, east, west;

        public bool Side { get => Code.ToString().Contains("stair-side"); }
        public bool Corner { get => Code.ToString().Contains("stair-corner"); }

        public override void OnLoaded(ICoreAPI api)
		{
			base.OnLoaded(api);

			type = FirstCodePart(2);
			material = FirstCodePart(3);
			updown = LastCodePart(1);
			outside = "stair-corner-" + type + "-" + material + "-" + "outside" + "-" + updown;
			inside = "stair-corner-" + type + "-" + material + "-" + "inside" + "-" + updown;
			north = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-north").GetBlock(api);
			south = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-south").GetBlock(api);
			east = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-east").GetBlock(api);
			west = new AssetLocation("stair-side-" + type + "-" + material + "-" + updown + "-west").GetBlock(api);
		}

		public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            Block nBlock = neibpos.GetBlock(world);
            if (!(nBlock is FixedStairs)) return;

            if (Side)
            {
                StairsCheck(world, pos);
            }
            else if (Corner)
            {
                CornersCheck(world, pos);
            }
        }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(world, blockPos, byItemStack);

            if (Side)
            {
                StairsCheck(world, blockPos);
            }
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
			return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("stair-side-" + FirstCodePart(2) + "-" + FirstCodePart(3) + "-up-north")));
		}

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            return new ItemStack[] { new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("stair-side-" + FirstCodePart(2) + "-" + FirstCodePart(3) + "-up-north"))) };
        }

        public void StairsCheck(IWorldAccessor world, BlockPos pos)
        {
            AssetLocation[] cardinal = GetCardinal(world, pos);
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
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-northeast")).BlockId, pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-southeast")).BlockId, pos);
                }
            }
            else if (cardinalW == "west")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-southwest")).BlockId, pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-northwest")).BlockId, pos);
                }
            }
            else if (cardinalE == "east")
            {
                if (cardinalN == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-southeast")).BlockId, pos);
                }
                else if (cardinalS == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-northeast")).BlockId, pos);
                }
            }
            else if (cardinalE == "west")
            {
                if (cardinalN == "north")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(inside + "-northwest")).BlockId, pos);
                }
                else if (cardinalS == "south")
                {
                    bA.SetBlock(bA.GetBlock(new AssetLocation(outside + "-southwest")).BlockId, pos);
                }
            }
        }

        public void CornersCheck(IWorldAccessor world, BlockPos pos)
        {
            var bA = world.BlockAccessor;
            AssetLocation[] cardinal = GetCardinal(world, pos);

            string cardinalN = bA.GetBlock(cardinal[0]).LastCodePart();
            string cardinalS = bA.GetBlock(cardinal[1]).LastCodePart();
            string cardinalE = bA.GetBlock(cardinal[2]).LastCodePart();
            string cardinalW = bA.GetBlock(cardinal[3]).LastCodePart();

			switch (LastCodePart())
			{
				case "northeast":
					if (cardinalN == "north")
					{
						bA.SetBlock(north.BlockId, pos);
					}
					else if (cardinalE == "east")
					{
						bA.SetBlock(east.BlockId, pos);
					}
					else if (cardinalW == "east")
					{
						bA.SetBlock(east.BlockId, pos);
					}
					else if (cardinalS == "north")
					{
						bA.SetBlock(north.BlockId, pos);
					}
					break;
				case "southwest":
					if (cardinalS == "south")
					{
						bA.SetBlock(south.BlockId, pos);
					}
					else if (cardinalE == "west")
					{
						bA.SetBlock(west.BlockId, pos);
					}
					else if (cardinalN == "south")
					{
						bA.SetBlock(south.BlockId, pos);
					}
					else if (cardinalW == "west")
					{
						bA.SetBlock(west.BlockId, pos);
					}
					break;
				case "southeast":
					if (cardinalN == "south")
					{
						bA.SetBlock(south.BlockId, pos);
					}
					else if (cardinalE == "east")
					{
						bA.SetBlock(east.BlockId, pos);
					}
					else if (cardinalS == "south")
					{
						bA.SetBlock(south.BlockId, pos);
					}
					else if (cardinalW == "east")
					{
						bA.SetBlock(east.BlockId, pos);
					}
					break;
				case "northwest":
					if (cardinalW == "west")
					{
						bA.SetBlock(west.BlockId, pos);
					}
					else if (cardinalS == "north")
					{
						bA.SetBlock(north.BlockId, pos);
					}
					else if (cardinalE == "west")
					{
						bA.SetBlock(west.BlockId, pos);
					}
					else if (cardinalN == "north")
					{
						bA.SetBlock(north.BlockId, pos);
					}
					break;
				default:
					break;
			}
        }

        public AssetLocation[] GetCardinal(IWorldAccessor world, BlockPos pos)
        {
            var bA = world.BlockAccessor;
            AssetLocation[] cardinal = {
                bA.GetBlock(pos.NorthCopy()).Code,
                bA.GetBlock(pos.SouthCopy()).Code,
                bA.GetBlock(pos.EastCopy()).Code,
                bA.GetBlock(pos.WestCopy()).Code,
            };
            return cardinal;
        }
    }

    public class BlockEntityStairs : BlockEntity
    {
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(dt =>
            {
                FixedStairs fixedStairs = (pos.GetBlock(api) as FixedStairs);
                if (fixedStairs != null)
                {
                    if (fixedStairs.Corner) fixedStairs.CornersCheck(api.World, pos);
                    else if (fixedStairs.Side) fixedStairs.StairsCheck(api.World, pos);
                }
            }, 30);
        }
    }
}
