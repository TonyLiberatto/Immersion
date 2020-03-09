using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class GenDeepOreBits : ModStdWorldGen
    {
        ICoreServerAPI Api;
        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;
        public override double ExecuteOrder() => 0.9;
        IWorldGenBlockAccessor bA;
        public DepositVariant[] Deposits;
        public LCGRandom rand;
        Dictionary<int, int> surfaceBlocks = new Dictionary<int, int>();
        DeepOreGenProperties genProperties;
        NormalizedSimplexNoise sNoise;

        public override void StartServerSide(ICoreServerAPI Api)
        {
            this.Api = Api;
            if (DoDecorationPass)
            {
                foreach (var block in Api.World.Blocks)
                {
                    if (block is BlockOre)
                    {
                        int? id = Api.World.BlockAccessor.GetBlock(new AssetLocation("looseores".Apd(block.Variant["type"]).Apd(block.Variant["rock"])))?.Id;
                        if (id != null)
                        {
                            surfaceBlocks.Add(block.Id, (int)id);
                        }
                    }
                }
                genProperties = Api.Assets.Get("game:worldgen/deeporebits.json").ToObject<DeepOreGenProperties>();

                Api.Event.InitWorldGenerator(InitWorldGen, "standard");
                Api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
                Api.Event.GetWorldgenBlockAccessor(c => bA = c.GetBlockAccessor(true));
            }
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

            int regionChunkSize = Api.WorldManager.RegionSize / chunksize;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            for (int x = 0; x < chunksize; x++)
            {
                for (int z = 0; z < chunksize; z++)
                {
                    double noise = sNoise.Noise(rdx + x, rdz + z);

                    int y = heightMap[z * chunksize + x];
                    int chunkY = y / chunksize;
                    int lY = y % chunksize;
                    int index3d = (chunksize * lY + z) * chunksize + x;
                    int bID = chunks[chunkY].Blocks[index3d];
                    if (bID == 0 || bA.GetBlock(bID).LiquidCode != null) continue;

                    int tY = heightMap[z * chunksize + x] + 1;
                    int tChunkY = tY / chunksize;
                    int tlY = tY % chunksize;
                    int tIndex3d = (chunksize * tlY + z) * chunksize + x;

                    int rockID = chunks[0].MapChunk.TopRockIdMap[z * chunksize + x];
                    string rock = bA.GetBlock(rockID).Variant["rock"];

                    rand.InitPositionSeed(rdx + x, rdz + z);

                    var deposits = Deposits.Shuffle(rand);

                    for (int i = 0; i < Deposits.Length; i++)
                    {
                        double dnoise = sNoise.Noise(rdx + x + i + 4987, rdz + z + i + 15654);
                        if (dnoise > 0.9) continue;

                        DepositVariant variant = Deposits[i];
                        if (!variant.WithOreMap) continue;
                        float factor = variant.GetOreMapFactor(chunkX, chunkZ);
                        factor *= (float)genProperties.GlobalMult;

                        if (factor > 0 && factor > noise)
                        {
                            int? placed = bA.GetBlock(new AssetLocation(variant.Attributes.Token["placeblock"]["code"].ToString().Replace("{rock}", rock).Replace("*", "poor")))?.Id;
                            if (placed == null|| !surfaceBlocks.ContainsKey((int)placed)) continue;

                            chunks[tChunkY].Blocks[tIndex3d] = surfaceBlocks[(int)placed];
                            chunks[tChunkY].MarkModified();
                            break;
                        }
                    }
                }
            }
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(Api);
            sNoise = NormalizedSimplexNoise.FromDefaultOctaves(2, 4.0, 1.0, Api.WorldManager.Seed + 1492);
            Deposits = Api.ModLoader.GetModSystem<GenDeposits>().Deposits;
            rand = new LCGRandom(Api.WorldManager.Seed + 1546);
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    class DeepOreGenProperties
    {
        [JsonProperty]
        public double GlobalMult { get; set; }
    }
}
