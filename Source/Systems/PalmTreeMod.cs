using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Immersion;
using Immersion.Utility;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Immersion
{
    public class GenPalms : ModStdWorldGen
    {
        ICoreServerAPI api;
        IWorldGenBlockAccessor blockAccessor;
        public override double ExecuteOrder() => 0.1;
        public static BlockPos[] bottomOffsets;
        public static BlockPos[] offsets;
        public static BlockPos[] cardinaloffsets;

        LCGRandom rand;

        static readonly string[] parts = new string[]
        {
            "bottom", "middle", "top"
        };
        static readonly string[] directions = new string[]
        {
            "north", "west", "south", "east"
        };
        static readonly int maxTreeSize = parts.Length + 10;

        int[] trunk;
        public int[] frond;
        public int[][] fruits;

        int tip;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            chunksize = api.WorldManager.ChunkSize;
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            api.Event.InitWorldGenerator(() => SetupPalm(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmlog-bottom-grown"))), "standard");
            api.Event.GetWorldgenBlockAccessor(c => blockAccessor = c.GetBlockAccessor(true));
        }

        private void SetupPalm(Block palmBase)
        {
            bottomOffsets = AreaMethods.AreaBelowOffsetList().ToArray();
            offsets = AreaMethods.AreaAroundOffsetList().ToArray();
            cardinaloffsets = AreaMethods.CardinalOffsetList().ToArray();

            rand = new LCGRandom(api.World.Seed);

            List<int> trunkblocks = new List<int>();
            List<int> frondblocks = new List<int>();

            List<int> nannerblocks = new List<int>();
            List<int> cocoblocks = new List<int>();

            for (int i = 0; i < parts.Length; i++)
            {
                trunkblocks.Add(api.World.BlockAccessor.GetBlock(palmBase.CodeWithPart(parts[i], 1)).Id);
            }
            trunk = trunkblocks.ToArray();

            for (int i = 0; i < directions.Length; i++)
            {
                frondblocks.Add(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfrond-1-grown-" + directions[i])).Id);
                nannerblocks.Add(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-bananna-" + directions[i])).Id);
                cocoblocks.Add(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-coconut-" + directions[i])).Id);
            }
            frond = frondblocks.ToArray();
            fruits = new int[][] { nannerblocks.ToArray(), cocoblocks.ToArray(), null };

            tip = api.World.BlockAccessor.GetBlock(palmBase.CodeWithPart("tip", 1)).Id;
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams)
        {
            IntMap beachMap = chunks[0].MapChunk.MapRegion.BeachMap;
            Vec3i climate = chunks[0].MapChunk.MapRegion.ClimateMap.ToClimateVec(chunkX, chunkZ, api.WorldManager.RegionSize, chunksize);

            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            float beachStep = (float)beachMap.InnerSize / regionChunkSize;

            int beachUpLeft = beachMap.GetUnpaddedInt((int)(rdx * beachStep), (int)(rdz * beachStep));
            int beachUpRight = beachMap.GetUnpaddedInt((int)(rdx * beachStep + beachStep), (int)(rdz * beachStep));
            int beachBotLeft = beachMap.GetUnpaddedInt((int)(rdx * beachStep), (int)(rdz * beachStep + beachStep));
            int beachBotRight = beachMap.GetUnpaddedInt((int)(rdx * beachStep + beachStep), (int)(rdz * beachStep + beachStep));

            for (int x = 1; x < chunksize - 1; x++)
            {
                for (int z = 1; z < chunksize - 1; z++)
                {
                    int y = heightMap[z * chunksize + x];
                    float beachRel = GameMath.BiLerp(beachUpLeft, beachUpRight, beachBotLeft, beachBotRight, (float)x / chunksize, (float)z / chunksize) / 255f;
                    float tempRel = climate.Z / 255f;
                    if (beachRel > 0.5 && tempRel > 0.5)
                    {
                        int rockID = chunks[0].MapChunk.TopRockIdMap[z * chunksize + x];

                        SimplexNoise sNoise = SimplexNoise.FromDefaultOctaves(16, 4.0, 0.5, api.WorldManager.Seed);
                        double noise = sNoise.Noise(rdx + x, rdx + z);

                        int chunkY = y / chunksize;
                        int lY = y % chunksize;
                        int index3d = (chunksize * lY + z) * chunksize + x;
                        

                        GenPalmTree(chunks, chunkY, chunkX, chunkZ, index3d, x, y, z, rockID, noise);
                    }
                }
            }
        }

        private void GenPalmTree(IServerChunk[] chunks, int chunkY, int chunkX, int chunkZ, int i3d, int x, int y, int z, int topRock, double noise)
        {
            var a = api.ModLoader.GetModSystem<GenBlockLayers>();
            
            if (a.blockLayerConfig.BeachLayer.BlockIdMapping.TryGetValue(topRock, out int sand))
            {
                if (chunks[chunkY].Blocks[i3d] == sand && noise > 0.8)
                {
                    int[] stretched = trunk.Stretch((int)(((1.0 - noise) * 2) * maxTreeSize));
                    int i, jY, index3d;

                    for (i = 0; i < stretched.Length; i++)
                    {
                        jY = (y + i + 1) % chunksize;
                        index3d = (chunksize * jY + z) * chunksize + x;
                        chunks[chunkY].Blocks[index3d] = stretched[i];
                    }

                    jY = (y + i + 1) % chunksize;
                    int posX = chunkX * chunksize + x, posY = chunkY * chunksize + jY, posZ = chunkZ * chunksize + z;

                    GenFrondAndFruits(chunks, chunkY, x, jY, z, GameMath.Max(0, ((1.0 - noise) * 2)));

                    blockAccessor.SetBlock(tip, new BlockPos(posX, posY, posZ));
                    blockAccessor.SpawnBlockEntity("PalmTree", new BlockPos(posX, posY, posZ));
                }
            }
        }

        public void GenFrondAndFruits(IServerChunk[] chunks, int chunkY, int x, int y, int z, double noise)
        {
            SimplexNoise sNoise = SimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 6151);
            noise = GameMath.Max(0, sNoise.Noise(x, y, z));

            SimplexNoise fNoise = SimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 4987);
            double fruitNoise = GameMath.Max(0, fNoise.Noise(x, y, z));

            int fruit = (int)Math.Round(fruitNoise * 2.0);

            int palmi = (int)Math.Round((noise * 3.0));
            for (int i = 0; i < cardinaloffsets.Length; i++)
            {
                int dX = x + cardinaloffsets[i].X, dY = y + cardinaloffsets[i].Y, dZ = z + cardinaloffsets[i].Z;

                int palm = blockAccessor.GetBlock(blockAccessor.GetBlock(frond[i]).CodeWithPart(palmi.ToString(), 1)).Id;

                int jY = dY % chunksize;
                int index3d = (chunksize * jY + dZ) * chunksize + dX;
                chunks[chunkY].Blocks[index3d] = palm;
                if (fruits[fruit] != null) chunks[chunkY].Blocks[(chunksize * (jY - 1) + dZ) * chunksize + dX] = fruits[fruit][i];
            }
        }

    }
    public class BlockPalmTree : Block
    {
        public static BlockPos[] bottomOffsets;
        public static BlockPos[] offsets;
        public static BlockPos[] cardinaloffsets;
        ICoreAPI Api { get => this.api; }

        static readonly string[] parts = new string[]
        {
            "bottom", "middle", "top"
        };
        static readonly string[] directions = new string[]
        {
            "north", "west", "south", "east"
        };
        public Block[] frond;
        public Block[][] fruits;

        public override void OnLoaded(ICoreAPI Api)
        {
            base.OnLoaded(Api);

            bottomOffsets = AreaMethods.AreaBelowOffsetList().ToArray();
            offsets = AreaMethods.AreaAroundOffsetList().ToArray();
            cardinaloffsets = AreaMethods.CardinalOffsetList().ToArray();

            List<Block> trunkblocks = new List<Block>();
            List<Block> frondblocks = new List<Block>();

            List<Block> nannerblocks = new List<Block>();
            List<Block> cocoblocks = new List<Block>();

            for (int i = 0; i < directions.Length; i++)
            {
                frondblocks.Add(Api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfrond-1-grown-" + directions[i])));
                nannerblocks.Add(Api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-bananna-" + directions[i])));
                cocoblocks.Add(Api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-coconut-" + directions[i])));
            }
            frond = frondblocks.ToArray();
            fruits = new Block[][] { nannerblocks.ToArray(), cocoblocks.ToArray(), null };
        }

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
                                Block bBlock = Api.World.BlockAccessor.GetBlock(bPos);
                                if (bBlock is BlockPalmTree)
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

    public class BEPalmTree : BlockEntity
    {
        public BlockPos[] myTree;
        public BlockPos[] myFronds;
        public BlockPos[] myFruits;
        public Block myFruit;
        public BlockPalmTree palmtree;
        public int fruitnum;

        public double nextGrowTime;

        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);
            UpdateTreeSize();
            UpdateFruitsFronds();

            palmtree = Api.World.BlockAccessor.GetBlock(Pos) as BlockPalmTree;
            if (Api.World.Side.IsServer()) RegisterGameTickListener(CheckTree, 1000);
            if (Api.World.Calendar.TotalHours > nextGrowTime) UpdateGrowTime();

            if (myFruits == null || myFruits[0] == null) return;

            myFruit = myFruit ?? Api.World.BlockAccessor.GetBlock(0);

            for (int i = 0; i < palmtree.fruits.Length; i++)
            {
                if (palmtree.fruits[i] == null) return;

                if (palmtree.fruits[i].FirstOrDefault().FirstCodePart(1) == myFruit.FirstCodePart(1))
                {
                    fruitnum = i;
                    break;
                }
            }
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            tree.SetBytes("myFruit", JsonUtil.ToBytes(myFruit));
            tree.SetDouble("nextGrowTime", nextGrowTime);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            byte[] bytes = tree.GetBytes("myFruit");
            if (bytes != null) myFruit = JsonUtil.FromBytes<Block>(tree.GetBytes("myFruit"));
            nextGrowTime = tree.GetDouble("nextGrowTime");
        }

        public void CheckTree(float dt)
        {
            if (myTree == null || myTree.Length == 0)
            {
                Api.World.BlockAccessor.RemoveBlockEntity(Pos);
                return;
            }

            for (int i = 0; i < myTree.Length; i++)
            {
                if (myTree[i] == null) continue;

                Block checkedBlock = Api.World.BlockAccessor.GetBlock(myTree[i]);
                if (!(checkedBlock is BlockPalmTree))
                {
                    UpdateTreeSize();
                    break;
                }
            }

            if (nextGrowTime < Api.World.Calendar.TotalHours)
            {
                for (int i = 0; i < BlockPalmTree.cardinaloffsets.Length; i++)
                {
                    BlockPos v = BlockPalmTree.cardinaloffsets[i];
                    BlockPos palmPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y, Pos.Z + v.Z);
                    BlockPos fruitPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y - 1, Pos.Z + v.Z);
                    Block fruitspace = Api.World.BlockAccessor.GetBlock(fruitPos);
                    Block palmspace = Api.World.BlockAccessor.GetBlock(palmPos);

                    if (fruitspace.Id == 0) Api.World.BlockAccessor.SetBlock(palmtree.fruits[fruitnum][i].BlockId, fruitPos);
                    if (palmspace.Id == 0) Api.World.BlockAccessor.SetBlock(palmtree.frond[i].BlockId, palmPos);
                }
                UpdateGrowTime();
            }
        }

        public void UpdateGrowTime()
        {
            nextGrowTime = Api.World.Calendar.TotalHours + 48;
        }

        public void UpdateTreeSize()
        {
            List<BlockPos> mytree = new List<BlockPos>();

            for (int x = -2; x <= 2; x++)
            {
                for (int y = 0; y < 20; y++)
                {
                    for (int z = -2; z <= 2; z++)
                    {
                        BlockPos bPos = new BlockPos(Pos.X + x, Pos.Y - y, Pos.Z + z);
                        Block bBlock = Api.World.BlockAccessor.GetBlock(bPos);

                        if (bBlock is BlockPalmTree)
                        {
                            mytree.Add(bPos);
                        }
                    }
                }
            }
            myTree = mytree.ToArray();
        }

        public void UpdateFruitsFronds()
        {
            if (myFruits == null || myFronds == null)
            {
                for (int i = 0; i < BlockPalmTree.cardinaloffsets.Length; i++)
                {
                    BlockPos v = BlockPalmTree.cardinaloffsets[i];
                    BlockPos frondPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y, Pos.Z + v.Z);
                    BlockPos fruitPos = new BlockPos(Pos.X + v.X, Pos.Y + v.Y - 1, Pos.Z + v.Z);

                    Block frondBlock = Api.World.BlockAccessor.GetBlock(frondPos);
                    Block fruitBlock = Api.World.BlockAccessor.GetBlock(fruitPos);

                    if (frondBlock.FirstCodePart() == "palmfrond")
                    {
                        if (myFronds == null) myFronds = new BlockPos[4];
                        myFronds[i] = frondPos;
                    }
                    if (fruitBlock.FirstCodePart() == "palmfruits")
                    {
                        if (myFruits == null) myFruits = new BlockPos[4];
                        myFruits[i] = fruitPos;
                    }
                }
            }
        }
    }
}
