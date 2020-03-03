using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Immersion.Utility;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace Immersion
{
    public class PalmTreeGenerator : ITreeGenerator
    {
        ICoreServerAPI sapi;
        GenPalms GenPalms { get => sapi.ModLoader.GetModSystem<GenPalms>(); }
        string fruit;

        public PalmTreeGenerator(ICoreServerAPI sapi, string fruit)
        {
            this.sapi = sapi;
            this.fruit = fruit;
        }

        public void GrowTree(IBlockAccessor blockAccessor, BlockPos pos, float sizeModifier = 1, float vineGrowthChance = 0, float otherblockChance = 1)
        {
            if (GenPalms.sNoise == null) GenPalms.SetupPalm(blockAccessor.GetBlock(new AssetLocation("immersion:palmlog-bottom-grown")));
            GenPalms.GenPalmTree(blockAccessor, pos.Copy(), null, null, null, fruit == "palmcoconut" ? 0 : fruit == "palmbanana" ? 1 : 2);
        }
    }

    public class GenPalms : ModStdWorldGen
    {
        ICoreServerAPI api;
        IWorldGenBlockAccessor blockAccessor;
        public override double ExecuteOrder() => 0.1;
        public static BlockPos[] bottomOffsets;
        public static BlockPos[] offsets;
        public static BlockPos[] cardinaloffsets;

        public NormalizedSimplexNoise sNoise;
        NormalizedSimplexNoise sizeNoise;
        NormalizedSimplexNoise frondNoise;
        NormalizedSimplexNoise fruitNoise;

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
        int[] saplings;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.RegisterTreeGenerator(new AssetLocation("immersion:palm"), new PalmTreeGenerator(api, "palm"));
            api.RegisterTreeGenerator(new AssetLocation("immersion:palmcoconut"), new PalmTreeGenerator(api, "palmcoconut"));
            api.RegisterTreeGenerator(new AssetLocation("immersion:palmbanana"), new PalmTreeGenerator(api, "palmbanana"));

            api.RegisterCommand("genpalm", "genpalm", "", (p, g, a) =>
            {
                var pos = p.CurrentBlockSelection?.Position?.AddCopy(0, 1, 0);
                if (sNoise == null) SetupPalm(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmlog-bottom-grown")));
                if (pos != null)
                {
                    GenPalmTree(api.World.BlockAccessor, pos);
                }
            }, Privilege.ban);

            chunksize = api.WorldManager.ChunkSize;
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            api.Event.InitWorldGenerator(() => SetupPalm(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmlog-bottom-grown"))), "standard");
            api.Event.GetWorldgenBlockAccessor(c => blockAccessor = c.GetBlockAccessor(true));
        }

        public void SetupPalm(Block palmBase)
        {
            bottomOffsets = AreaMethods.AreaBelowOffsetList().ToArray();
            offsets = AreaMethods.AreaAroundOffsetList().ToArray();
            cardinaloffsets = AreaMethods.CardinalOffsetList().ToArray();
            sNoise = NormalizedSimplexNoise.FromDefaultOctaves(16, 8.0, 0.5, api.WorldManager.Seed + 6514);
            sizeNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 2361);
            frondNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 6151);
            fruitNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 2.0, 1.0, api.WorldManager.Seed + 4987);

            List<int> trunkblocks = new List<int>();
            List<int> frondblocks = new List<int>();

            List<int> bananablocks = new List<int>();
            List<int> coconutblocks = new List<int>();

            for (int i = 0; i < parts.Length; i++)
            {
                trunkblocks.Add(api.World.BlockAccessor.GetBlock(palmBase.CodeWithPart(parts[i], 1)).Id);
            }
            trunk = trunkblocks.ToArray();

            for (int i = 0; i < directions.Length; i++)
            {
                frondblocks.Add(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfrond-1-grown-" + directions[i])).Id);
                bananablocks.Add(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-bananna-" + directions[i])).Id);
                coconutblocks.Add(api.World.BlockAccessor.GetBlock(new AssetLocation("immersion:palmfruits-coconut-" + directions[i])).Id);
            }
            frond = frondblocks.ToArray();
            fruits = new int[][] { bananablocks.ToArray(), coconutblocks.ToArray(), null };

            tip = api.World.BlockAccessor.GetBlock(palmBase.CodeWithPart("tip", 1)).Id;
            int c = api.World.BlockAccessor.GetBlock(new AssetLocation("game:sapling-palmcoconut")).Id;
            int b = api.World.BlockAccessor.GetBlock(new AssetLocation("game:sapling-palmbanana")).Id;
            int p = api.World.BlockAccessor.GetBlock(new AssetLocation("game:sapling-palm")).Id;

            saplings = new int[] { c, b, p };
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams)
        {
            IntMap beachMap = chunks[0].MapChunk.MapRegion.BeachMap;
            Vec3i climate = chunks[0].MapChunk.MapRegion.ClimateMap.ToClimateVec(chunkX, chunkZ, api.WorldManager.RegionSize, chunksize);
            BlockPos pos = new BlockPos();

            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

            int regionChunkSize = api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            float beachStep = (float)beachMap.InnerSize / regionChunkSize;

            int beachUpLeft = beachMap.GetUnpaddedInt((int)(rdx * beachStep), (int)(rdz * beachStep));
            int beachUpRight = beachMap.GetUnpaddedInt((int)(rdx * beachStep + beachStep), (int)(rdz * beachStep));
            int beachBotLeft = beachMap.GetUnpaddedInt((int)(rdx * beachStep), (int)(rdz * beachStep + beachStep));
            int beachBotRight = beachMap.GetUnpaddedInt((int)(rdx * beachStep + beachStep), (int)(rdz * beachStep + beachStep));

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {
                    int y = heightMap[z * chunksize + x];
                    
                    float beachRel = GameMath.BiLerp(beachUpLeft, beachUpRight, beachBotLeft, beachBotRight, (float)x / chunksize, (float)z / chunksize) / 255f;
                    float tempRel = climate.Z / 255f;
                    if (beachRel > 0.5 && tempRel > 0.5)
                    {
                        pos.X = chunkX * chunksize + x;
                        pos.Y = y + 1;
                        pos.Z = chunkZ * chunksize + z;

                        var a = api.ModLoader.GetModSystem<GenBlockLayers>();
                        int rockID = chunks[0].MapChunk.TopRockIdMap[z * chunksize + x];
                        
                        if (a.blockLayerConfig.BeachLayer.BlockIdMapping.TryGetValue(rockID, out int sand))
                        {
                            int lY = y % chunksize;
                            int i3d = (chunksize * lY + z) * chunksize + x;
                            int chunkY = y / chunksize;

                            double noise = sNoise.Noise(rdx + x, rdx + z);

                            if (chunks[chunkY].Blocks[i3d] == sand && noise > 0.84) GenPalmTree(blockAccessor, pos, sizeNoise.Noise(pos.X, pos.Y, pos.Z), fruitNoise.Noise(pos.X, pos.Y, pos.Z), frondNoise.Noise(pos.X, pos.Y, pos.Z));
                        }
                    }
                }
            }
        }

        public void GenPalmTree(IBlockAccessor bA, BlockPos pos, double? sizeRnd1 = null, double? fruitRnd1 = null, double? frondRnd1 = null, int? fruitNum = null)
        {
            double sizeRnd = sizeRnd1 ?? api.World.Rand.NextDouble();
            double fruitRnd = fruitRnd1 ?? api.World.Rand.NextDouble();
            double frondRnd = frondRnd1 ?? api.World.Rand.NextDouble();
            int fruiti = fruitNum ?? (int)Math.Round(fruitRnd * 2.0);

            while (!CanGenPalm(bA, pos.UpCopy(), sizeRnd))
            {
                sizeRnd -= 0.01;
                if (sizeRnd < 0)
                {
                    bA.SetBlock(saplings[fruiti], pos.UpCopy());
                    return;
                }
            }

            int[] stretched = trunk.Stretch((int)(sizeRnd * maxTreeSize));
            for (int i = 0; i < stretched.Length; i++)
            {
                if (bA.GetBlock(pos).IsReplacableBy(bA.GetBlock(tip))) bA.SetBlock(stretched[i], pos);
                pos.Y++;
            }
            bA.SetBlock(tip, pos);
            int frondi = (int)Math.Round(frondRnd * 3.0);

            for (int i = 0; i < cardinaloffsets.Length; i++)
            {
                var c = cardinaloffsets[i];
                BlockPos offset = pos.AddCopy(c.X, c.Y, c.Z);
                if (bA.GetBlock(offset).Id != 0) continue;

                int frondID = bA.GetBlock(bA.GetBlock(frond[i]).CodeWithPart(frondi.ToString(), 1)).Id;

                bA.SetBlock(frondID, offset);
                if (fruits[fruiti] != null) bA.SetBlock(fruits[fruiti][i], offset.Add(0, -1, 0));
            }
        }

        public bool CanGenPalm(IBlockAccessor bA, BlockPos pos, double? sizeRnd1 = null)
        {
            double sizeRnd = sizeRnd1 ?? api.World.Rand.NextDouble();

            int[] stretched = trunk.Stretch((int)(sizeRnd * maxTreeSize));
            Block block = bA.GetBlock(pos);

            for (int i = 0; i < stretched.Length; i++)
            {
                block = bA.GetBlock(pos);

                if (block.Id != 0) return false;
                pos.Y++;
            }
            pos.Y--;

            block = bA.GetBlock(pos);

            if (block.Id != 0) return false;

            for (int i = 0; i < cardinaloffsets.Length; i++)
            {
                var c = cardinaloffsets[i];
                BlockPos offset = pos.AddCopy(c.X, c.Y, c.Z);
                block = bA.GetBlock(offset);

                if (block.Id != 0) return false;
            }

            return true;
        }

    }
}
