using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Immersion.Utility
{
    class AreaMethods
    {
        public static BlockPos[] FullAreaAround(BlockPos Pos)
        {
            BlockPos[] Positions = AreaBelow(Pos).Concat(AreaAbove(Pos)).ToArray();
            return Positions.Concat(AreaAround(Pos)).ToArray();
        }

        public static BlockPos[] AreaAround(BlockPos Pos)
        {
            return new BlockPos[]
            {
               Pos,
               Pos.NorthCopy(),
               Pos.SouthCopy(),
               Pos.EastCopy(),
               Pos.WestCopy(),
               Pos.NorthCopy().EastCopy(),
               Pos.SouthCopy().WestCopy(),
               Pos.EastCopy().SouthCopy(),
               Pos.WestCopy().NorthCopy(),
            };
        }

        public static BlockPos[] AreaBelow(BlockPos Pos)
        {
            BlockPos aPos = Pos.DownCopy();
            return new BlockPos[]
            {
               aPos,
               aPos.NorthCopy(),
               aPos.SouthCopy(),
               aPos.EastCopy(),
               aPos.WestCopy(),
               aPos.NorthCopy().EastCopy(),
               aPos.SouthCopy().WestCopy(),
               aPos.EastCopy().SouthCopy(),
               aPos.WestCopy().NorthCopy(),
            };
        }

        public static List<BlockPos> AreaBelowOffsetList()
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Positions.Add(new BlockPos(x, -1, z));
                }
            }
            return Positions;
        }

        public static List<BlockPos> AreaBelowCardinalOffsetList()
        {
            List<BlockPos> Positions = new List<BlockPos> {
                new BlockPos(-1,-1,0),
                new BlockPos(1,-1,0),
                new BlockPos(0,-1,-1),
                new BlockPos(0,-1,1),
            };
            return Positions;
        }

        public static BlockPos[] LargeAreaBelow(BlockPos Pos)
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    Positions.Add(Pos.AddCopy(x, -1, z));
                }
            }
            return Positions.ToArray();
        }

        public static List<BlockPos> LargeAreaBelowOffsetList()
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -3; x <= 3; x++)
            {
                for (int z = -3; z <= 3; z++)
                {
                    Positions.Add(new BlockPos(x, -1, z));
                }
            }
            return Positions;
        }

        public static List<BlockPos> AreaAroundOffsetList()
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        Positions.Add(new BlockPos(x, y, z));
                    }
                }
            }
            return Positions;
        }

        public static List<BlockPos> SphericalOffsetList(int radius)
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        if ((x*x+y*y+z*z) <= (radius*radius))
                        {
                            Positions.Add(new BlockPos(x, y, z));
                        }
                    }
                }
            }
            return Positions;
        }

        public static List<BlockPos> CircularOffsetList(int radius)
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if ((x * x + z * z) <= (radius * radius))
                    {
                        Positions.Add(new BlockPos(x, 0, z));
                    }
                }

            }
            return Positions;
        }

        public static List<BlockPos> PlanarAreaAroundOffsetList()
        {
            List<BlockPos> Positions = new List<BlockPos>();
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    Positions.Add(new BlockPos(x, 0, z));
                }

            }
            return Positions;
        }

        public static List<BlockPos> CardinalOffsetList()
        {
            return new List<BlockPos>
            {
                new BlockPos(0,0,1),
                new BlockPos(1,0,0),
                new BlockPos(0,0,-1),
                new BlockPos(-1,0,0),
            };
        }

        public static BlockPos[] AreaAbove(BlockPos Pos)
        {
            BlockPos aPos = Pos.UpCopy();
            return new BlockPos[]
            {
               aPos,
               aPos.NorthCopy(),
               aPos.SouthCopy(),
               aPos.EastCopy(),
               aPos.WestCopy(),
               aPos.NorthCopy().EastCopy(),
               aPos.SouthCopy().WestCopy(),
               aPos.EastCopy().SouthCopy(),
               aPos.WestCopy().NorthCopy(),
            };
        }

        public static BlockPos[] Cardinal(BlockPos Pos)
        {
            return new BlockPos[]
            {
               Pos.NorthCopy(),
               Pos.SouthCopy(),
               Pos.EastCopy(),
               Pos.WestCopy(),
            };
        }


        public static BlockPos[] FullCardinal(BlockPos Pos)
        {
            return new BlockPos[]
            {
               Pos.UpCopy(),
               Pos.DownCopy(),
               Pos.NorthCopy(),
               Pos.SouthCopy(),
               Pos.EastCopy(),
               Pos.WestCopy(),
            };
        }

        public static Block[] BlockCardinal(IWorldAccessor world, BlockPos Pos)
        {
            List<Block> blocks = new List<Block>();
            foreach (var val in Cardinal(Pos))
            {
                blocks.Add(world.BulkBlockAccessor.GetBlock(val));
            }
            return blocks.ToArray();
        }

        public static Block[] BlockFullCardinal(IWorldAccessor world, BlockPos Pos)
        {
            List<Block> blocks = new List<Block>();
            foreach (var val in FullCardinal(Pos))
            {
                blocks.Add(world.BulkBlockAccessor.GetBlock(val));
            }
            return blocks.ToArray();
        }

        public static int CardinalCount(IWorldAccessor world, BlockPos Pos)
        {
            List<Block> blocks = new List<Block>();
            int count = 0;
            Block block = world.BulkBlockAccessor.GetBlock(Pos);
            foreach (var val in BlockCardinal(world, Pos))
            {
                if (!val.IsReplacableBy(block))
                {
                    count++;
                }
            }
            return count;
        }

        public static Dictionary<BlockPos, string> CardinalDict(BlockPos Pos)
        {
            return new Dictionary<BlockPos, string>
            {
                {   Pos.NorthCopy(), "north" },
                {   Pos.SouthCopy(), "south" },
                {   Pos.EastCopy(), "east" },
                {   Pos.WestCopy(), "west" }
            };
        }

        public static Dictionary<string, BlockPos> DirectionDict(BlockPos Pos)
        {
            return new Dictionary<string, BlockPos>
            {
                {   "north", Pos.NorthCopy() },
                {   "south", Pos.SouthCopy() },
                {   "east", Pos.EastCopy() },
                {   "west", Pos.WestCopy() }
            };
        }

        public static Dictionary<int, string> CountString()
        {
            return new Dictionary<int, string> {
                { 0, "zero" },
                { 1, "one" },
                { 2, "two" },
                { 3, "three" },
                { 4, "four" },
                { 5, "five" },
                { 6, "six" },
                { 7, "seven" },
                { 8, "eight" },
                { 9, "nine" },
            };
        }

        public static Dictionary<string, string> CornerMatches()
        {
            return new Dictionary<string, string>
            {
                {"north","east" },
                {"south","west" },
            };
        }
	}
}
