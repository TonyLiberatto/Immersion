using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Priority_Queue;
using Cairo;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace TheNeolithicMod
{
    class CostTest : BlockBehavior
    {
        BlockPos targetPos;
        public HashSet<BlockPos> testBlockPos = new HashSet<BlockPos>();
        public IWorldAccessor lworld;
        long listenerId;
        public CostTest(Block block) : base(block) { }

        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ref EnumHandling handled)
        {
            testBlockPos.Clear();
            lworld = world;
            if (world.Side != EnumAppSide.Server) return;
            BlockPos pos = blockPos;
            targetPos = world.GetNearestEntity(pos.ToVec3d(),64,64).Pos.AsBlockPos;
            Vec3d posv = pos.ToVec3d();
            Vec3d tgtv = targetPos.ToVec3d();
            BlockPos mP = ((posv.Add(tgtv)) / 2).AsBlockPos;
            int sR = (int)mP.DistanceTo(pos) + 4;
            var grid = new CubeGrid(sR * 2, sR * 2, sR * 2);

            if (!grid.isInBounds(world, targetPos) || !grid.isPassable(world, targetPos) || !grid.isWalkable(world, targetPos))
            {
                return;
            }
            for (int x = mP.X - sR; x <= mP.X + sR; x++)
            {
                for (int y = mP.Y - sR; y <= mP.Y + sR; y++)
                {
                    for (int z = mP.Z - sR; z <= mP.Z + sR; z++)
                    {
                        BlockPos currentPos = new BlockPos(x, y, z);
                        if (grid.isInBounds(world, currentPos) && grid.isPassable(world, currentPos) && grid.isWalkable(world, currentPos))
                        {
                            grid.air.Add(currentPos);
                        }
                        else
                        {
                            grid.walls.Add(currentPos);
                        }
                    }
                }
            }
            var astar = new AStarSearch(world, grid, pos, targetPos);
            DrawGrid(world, astar, mP, sR);
            world.BlockAccessor.SetBlock(2278, pos);
            //world.BlockAccessor.SetBlock(2279, mP);
            world.BlockAccessor.SetBlock(2278, targetPos);
            testBlockPos.Add(targetPos);
            testBlockPos.Add(pos);
            //testBlockPos.Add(mP);
            return;
        }

        private void DrawGrid(IWorldAccessor world, AStarSearch astar, BlockPos pos, int sR)
        {
            for (int x = pos.X - sR; x < pos.X + sR; x++)
            {
                for (int y = pos.Y - sR; y < pos.Y + sR; y++)
                {
                    for (int z = pos.Z - sR; z < pos.Z + sR; z++)
                    {
                        BlockPos id = new BlockPos(x, y, z);
                        if (!astar.cameFrom.TryGetValue(id, out BlockPos ptr))
                        {
                            ptr = id;
                        }
                        else
                        {
                            world.BlockAccessor.SetBlock(2277, ptr);
                            testBlockPos.Add(ptr);
                        }
                    }
                }
            }
            listenerId = world.RegisterGameTickListener(Ticker, 2500);
        }

        private void Ticker(float dt)
        {
            Clear(lworld, testBlockPos);
            lworld.UnregisterGameTickListener(listenerId);
            return;
        }

        private void Clear(IWorldAccessor world, HashSet<BlockPos> blocks)
        {
            foreach (var val in blocks)
            {
                world.BlockAccessor.SetBlock(0, val);
            }
        }

    }

    public class MakePath
    {
        public List<BlockPos> currentPath = new List<BlockPos>();
        public int positionIndex = 0;

        public BlockPos NextPathPos(IWorldAccessor world, BlockPos targetPos, BlockPos startPos)
        {
            BlockPos nextPos = startPos;
            List<BlockPos> path = MakeGrid(world, targetPos, startPos);
            if (positionIndex < path.Count)
            {
                positionIndex += 1;
            }
            else
            {
                positionIndex = 0;
            }

            return path[positionIndex];
        }

        public List<BlockPos> MakeGrid(IWorldAccessor world, BlockPos targetPos, BlockPos startPos)
        {
            BlockPos pos = startPos;
            Vec3d posv = pos.ToVec3d();
            Vec3d tgtv = targetPos.ToVec3d();
            BlockPos mP = ((posv.Add(tgtv)) / 2).AsBlockPos;
            int sR = (int)mP.DistanceTo(pos) + 4;
            var grid = new CubeGrid(sR * 2, sR * 2, sR * 2);

            if (!grid.isInBounds(world, targetPos) || !grid.isPassable(world, targetPos) || !grid.isWalkable(world, targetPos))
            {
                return new List<BlockPos> { startPos };
            }
            for (int x = mP.X - sR; x <= mP.X + sR; x++)
            {
                for (int y = mP.Y - sR; y <= mP.Y + sR; y++)
                {
                    for (int z = mP.Z - sR; z <= mP.Z + sR; z++)
                    {
                        BlockPos currentPos = new BlockPos(x, y, z);
                        if (grid.isInBounds(world, currentPos) && grid.isPassable(world, currentPos) && grid.isWalkable(world, currentPos))
                        {
                            grid.air.Add(currentPos);
                        }
                        else
                        {
                            grid.walls.Add(currentPos);
                        }
                    }
                }
            }
            var astar = new AStarSearch(world, grid, pos, targetPos);
            return CurrentPath(world, astar, mP, sR);
        }

        public List<BlockPos> CurrentPath(IWorldAccessor world, AStarSearch astar, BlockPos pos, int sR)
        {
            currentPath.Clear();
            for (int x = pos.X - sR; x < pos.X + sR; x++)
            {
                for (int y = pos.Y - sR; y < pos.Y + sR; y++)
                {
                    for (int z = pos.Z - sR; z < pos.Z + sR; z++)
                    {
                        BlockPos id = new BlockPos(x, y, z);
                        if (!astar.cameFrom.TryGetValue(id, out BlockPos ptr))
                        {
                            ptr = id;
                        }
                        else
                        {
                            currentPath.Add(ptr);
                        }
                    }
                }
            }
            return currentPath;
        }
    }

    public class AStarSearch
    {
        public Dictionary<BlockPos, BlockPos> cameFrom = new Dictionary<BlockPos, BlockPos>();
        public Dictionary<BlockPos, int> currentCost = new Dictionary<BlockPos, int>();


        static public int Heuristic(BlockPos a, BlockPos b)
        {
            return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
        }

        public AStarSearch(IWorldAccessor world, IWeightedGraph<BlockPos> graph, BlockPos from, BlockPos to)
        {
            cameFrom.Clear();
            currentCost.Clear();
            var f = new SimplePriorityQueue<BlockPos>();
            f.Enqueue(from, 0);
            int max = 128;
            cameFrom[from] = from;
            currentCost[from] = 0;
            while (f.Count > 0 && max > 0)
            {
                max -= 1;
                var current = f.Dequeue();
                if (current.Equals(to))
                {
                    break;
                }
                foreach (var next in graph.Neighbors(world, current))
                {
                    int newCost = currentCost[current];
                    if (!currentCost.ContainsKey(next) || newCost < currentCost[next])
                    {
                        currentCost[next] = newCost;
                        int priority = newCost + Heuristic(next, to);
                        f.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }
        }
    }
    public interface IWeightedGraph<L>
    {
        int Cost(BlockPos a, BlockPos b);
        IEnumerable<BlockPos> Neighbors(IWorldAccessor world, BlockPos id);
    }

    public class CubeGrid : IWeightedGraph<BlockPos>
    {
        public HashSet<BlockPos> DIRS = new HashSet<BlockPos>();
        public int width, height, length;

        public CubeGrid(int width, int height, int length)
        {
            this.width = width;
            this.height = height;
            this.length = length;

            DIRS.Clear();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -4; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        DIRS.Add(new BlockPos(x, y, z));

                    }
                }
            }
        }
        public HashSet<BlockPos> walls = new HashSet<BlockPos>();
        public HashSet<BlockPos> air = new HashSet<BlockPos>();

        public bool isInBounds(IWorldAccessor world, BlockPos pos)
        {
            return pos.Y >= 1 && pos.Y <= world.BlockAccessor.MapSizeY;
        }

        public bool isPassable(IWorldAccessor world, BlockPos pos)
        {
            if (world.BlockAccessor.GetBlock(pos).CollisionBoxes == null)
            {
                if (world.BlockAccessor.GetBlock(pos).WildCardMatch(new AssetLocation("game:water*")) || world.BlockAccessor.GetBlock(pos).WildCardMatch(new AssetLocation("game:lava*")))
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public bool isWalkable(IWorldAccessor world, BlockPos pos)
        {
            BlockPos offset = new BlockPos(0, -1, 0);
            if (world.BlockAccessor.GetBlock(pos + offset).CollisionBoxes == null)
            {
                if (world.BlockAccessor.GetBlock(pos + offset).WildCardMatch(new AssetLocation("game:water*")))
                {
                    return true;
                }
                return false;
            }
            return true;
        }

        public int Cost(BlockPos a, BlockPos b)
        {
            return (int)a.DistanceTo(b);
        }

        public IEnumerable<BlockPos> Neighbors(IWorldAccessor world, BlockPos id)
        {
            foreach (var dir in DIRS)
            {
                BlockPos next = new BlockPos(id.X + dir.X, id.Y + dir.Y, id.Z + dir.Z);
                if (isInBounds(world, next) && isPassable(world, next) && isWalkable(world, next))
                {
                    yield return next;
                }
            }
        }
    }
}
