using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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

                        NormalizedSimplexNoise sNoise = NormalizedSimplexNoise.FromDefaultOctaves(16, 8.0, 0.5, api.WorldManager.Seed);
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
            int posX = chunkX * chunksize + x, posZ = chunkZ * chunksize + z;
            double sizeNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 2361).Noise(posX, y, posZ);

            if (a.blockLayerConfig.BeachLayer.BlockIdMapping.TryGetValue(topRock, out int sand))
            {
                if (chunks[chunkY].Blocks[i3d] == sand && noise > 0.84)
                {
                    int[] stretched = trunk.Stretch((int)(sizeNoise * maxTreeSize));
                    int i, jY, index3d;

                    jY = (y + 1) % chunksize;
                    index3d = (chunksize * jY + z) * chunksize + x;

                    if (((y % chunksize) + stretched.Length + 1) >= chunksize || chunks[chunkY].Blocks[index3d] != 0) return;

                    for (i = 0; i < stretched.Length; i++)
                    {
                        jY = (y + i + 1) % chunksize;
                        index3d = (chunksize * jY + z) * chunksize + x;
                        chunks[chunkY].Blocks[index3d] = stretched[i];
                    }

                    jY = (y + i + 1) % chunksize;
                    int posY = chunkY * chunksize + jY;

                    GenFrondAndFruits(chunks, chunkY, x, jY, z);

                    blockAccessor.SetBlock(tip, new BlockPos(posX, posY, posZ));
                    blockAccessor.SpawnBlockEntity("PalmTree", new BlockPos(posX, posY, posZ));
                }
            }
        }

        public void GenFrondAndFruits(IServerChunk[] chunks, int chunkY, int x, int y, int z)
        {
            double palmNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 6151).Noise(x, y, z);
            double fruitNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 4987).Noise(x, y, z);

            int fruit = (int)Math.Round(fruitNoise * 2.0);

            int palmi = (int)Math.Round((palmNoise * 3.0));
            for (int i = 0; i < cardinaloffsets.Length; i++)
            {
                int dX = x + cardinaloffsets[i].X, dY = y + cardinaloffsets[i].Y, dZ = z + cardinaloffsets[i].Z;

                int palm = blockAccessor.GetBlock(blockAccessor.GetBlock(frond[i]).CodeWithPart(palmi.ToString(), 1)).Id;

                int jY = dY % chunksize;
                int index3d = (chunksize * jY + dZ) * chunksize + dX;
                chunks[chunkY].Blocks[index3d] = palm;

                if ((chunksize * (jY - 1) + dZ) * chunksize + dX >= 0 && fruits[fruit] != null) chunks[chunkY].Blocks[(chunksize * (jY - 1) + dZ) * chunksize + dX] = fruits[fruit][i];
            }
        }

    }
}
