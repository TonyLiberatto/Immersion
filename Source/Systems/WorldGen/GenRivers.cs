using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

namespace Immersion
{
    public class GenRivers : ModStdWorldGen
    {
        ICoreServerAPI api;
        MapLayerBase riverGen;
        int noiseSizeRiver;
        public ImmersionGlobalConfig config { get => api.ModLoader.GetModSystem<ModifyLakes>().config; }
        NormalizedSimplexNoise noise;

        public int chunksize2 { get => chunksize > 0 ? chunksize : 32; }
        public override double ExecuteOrder() => 0.1;

        public override bool ShouldLoad(EnumAppSide forSide) => false;// forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.InitWorldGenerator(InitWorldGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.TerrainFeatures, "standard");
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            mapRegion.ModData["rivermap"] = JsonUtil.ToBytes(
                new IntMap() { 
                    Data = riverGen.GenLayer(regionX * noiseSizeRiver, regionZ * noiseSizeRiver, noiseSizeRiver + 1, noiseSizeRiver + 1),
                    BottomRightPadding = 1,
                    Size = noiseSizeRiver + 1
                });
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(api);
            long seed = api.WorldManager.Seed;
            riverGen = new MapLayerPerlin(seed + 46841, 6, 0.1f, 16, 255, new double[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f });
            noiseSizeRiver = api.WorldManager.RegionSize / 16;
            noise = NormalizedSimplexNoise.FromDefaultOctaves(2, 0.1, 1.0, api.WorldManager.Seed + 465814);
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

            IntMap riverMap = JsonUtil.FromBytes<IntMap>(chunks[0].MapChunk.MapRegion.ModData["rivermap"]);

            int regionChunkSize = api.WorldManager.RegionSize / chunksize2;
            int rdx = chunkX % regionChunkSize;
            int rdz = chunkZ % regionChunkSize;

            float riverStep = (float)riverMap.InnerSize / regionChunkSize;

            int riverUpLeft = riverMap.GetUnpaddedInt((int)(rdx * riverStep), (int)(rdz * riverStep));
            int riverUpRight = riverMap.GetUnpaddedInt((int)(rdx * riverStep + riverStep), (int)(rdz * riverStep));
            int riverBotLeft = riverMap.GetUnpaddedInt((int)(rdx * riverStep), (int)(rdz * riverStep + riverStep));
            int riverBotRight = riverMap.GetUnpaddedInt((int)(rdx * riverStep + riverStep), (int)(rdz * riverStep + riverStep));

            for (int x = 0; x < chunksize2; x++)
            {
                for (int z = 0; z < chunksize2; z++)
                {
                    double n = noise.Noise(rdx + x, rdx + z);

                    float riverRel = 1.0f - (GameMath.BiLerp(riverUpLeft, riverUpRight, riverBotLeft, riverBotRight, (float)x / chunksize2, (float)z / chunksize2) / 255f);

                    int y = heightMap[z * chunksize + x];
                    
                    //instead of breaking here, we should blend with terrain more
                    if (y > api.World.BlockAccessor.MapSizeY / 2) break;

                    int dy;
                    int chunkIndex, blockIndex;
                    int rockBlockId = chunks[0].MapChunk.TopRockIdMap[z * chunksize + x];
                    var config = api.ModLoader.GetModSystem<ModifyLakes>().config;
                    var lakebedLayerConfig = api.ModLoader.GetModSystem<GenPonds>().GetField<GenPonds, LakeBedLayerProperties>("lakebedLayerConfig");

                    if (riverRel < 0.55 || riverRel > 0.6)
                    {
                        if (riverRel > 0.54 && riverRel < 0.61)
                        {
                            //should be a less sharp U shape
                            for (dy = TerraGenConfig.seaLevel - 4 - (int)(4 * n); dy <= y + 1; dy++)
                            {
                                chunkIndex = dy / chunksize;
                                blockIndex = (chunksize * (dy % chunksize) + z) * chunksize + x;

                                //shouldn't happen next to sea water when disconnected from the rest of the "river"
                                if (chunks[chunkIndex].Blocks[blockIndex] == config.waterBlockId) continue;

                                chunks[chunkIndex].Blocks[blockIndex] = 
                                    dy >= TerraGenConfig.seaLevel ? 0 :
                                    dy < TerraGenConfig.seaLevel - (int)(4 * n) ? rockBlockId :
                                    lakebedLayerConfig.BlockCodeByMin[0].GetBlockForMotherRock(rockBlockId);
                            }
                        }
                        continue;
                    }

                    for (dy = y + 16 + (int)(4 * n); dy > TerraGenConfig.seaLevel - 4 - (int)(4 * n); dy--)
                    {
                        if (dy >= api.World.BlockAccessor.MapSizeY) break;
                        chunkIndex = dy / chunksize;
                        blockIndex = (chunksize * (dy % chunksize) + z) * chunksize + x;
                        if (chunks[chunkIndex].Blocks[blockIndex] == config.waterBlockId) continue;

                        chunks[chunkIndex].Blocks[blockIndex] = dy < TerraGenConfig.seaLevel ? config.LakeWaterBlockId : 0;
                    }

                    if (dy >= api.World.BlockAccessor.MapSizeY) continue;

                    chunkIndex = dy / chunksize;
                    blockIndex = (chunksize * (dy % chunksize) + z) * chunksize + x;

                    if (chunks[chunkIndex].Blocks[blockIndex] == config.waterBlockId) continue;

                    chunks[chunkIndex].Blocks[blockIndex] = lakebedLayerConfig.BlockCodeByMin[0].GetBlockForMotherRock(rockBlockId);

                }
            }
        }
    }
}
