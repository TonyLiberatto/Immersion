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
        BlockLayer soilLayer;
        LakeBedLayerProperties lakebedLayerConfig;
        ImmersionGlobalConfig immersionConfig;
        BlockLayerConfig blockLayerConfig;

        public int chunksize2 { get => chunksize > 0 ? chunksize : 32; }
        public override double ExecuteOrder() => 0.11;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            if (!ImmersionWorldgenConfig.GenRivers) return;

            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.InitWorldGenerator(InitWorldGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Terrain, "standard");
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            var data = riverGen.GenLayer(regionX * noiseSizeRiver, regionZ * noiseSizeRiver, noiseSizeRiver + 1, noiseSizeRiver + 1);
            mapRegion.ModData["rivermap"] = JsonUtil.ToBytes(
                new IntMap() { 
                    Data = data,
                    BottomRightPadding = 1,
                    Size = noiseSizeRiver + 1
                });
        }

        public void InitWorldGen()
        {
            LoadGlobalConfig(api);
            long seed = api.WorldManager.Seed;
            riverGen = new MapLayerRidged(seed + 46841, 1, 1.0f, 4, 255, new double[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f });
            
            noiseSizeRiver = api.WorldManager.RegionSize / 32;
            blockLayerConfig = BlockLayerConfig.GetInstance(api);
            lakebedLayerConfig = blockLayerConfig.LakeBedLayer;
            soilLayer = blockLayerConfig.Blocklayers.OrderByDescending(a => a.ID == "l1soilwithgrass").First();

            immersionConfig = api.ModLoader.GetModSystem<ModifyLakes>().config;
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;
            var modData = chunks[0].MapChunk.MapRegion.ModData;
            byte[] riverMapData = modData.ContainsKey("rivermap") ? modData["rivermap"] : null;
            if (riverMapData == null) return;
            IntMap riverMap = JsonUtil.FromBytes<IntMap>(riverMapData);

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
                    float riverRel = GameMath.BiLerp(riverUpLeft, riverUpRight, riverBotLeft, riverBotRight, (float)x / chunksize2, (float)z / chunksize2) / 255f;
                    riverRel += 1;

                    int y = heightMap[z * chunksize + x];
                    int distanceFromSeaLevel = (y - TerraGenConfig.seaLevel);
                    int erosionIntensity = distanceFromSeaLevel + 1 + (int)(4 * riverRel);

                    int chunkIndex = y / chunksize;
                    int blockIndex = (chunksize * (y % chunksize) + z) * chunksize + x;

                    for (int dy = y; dy > y - erosionIntensity; dy--)
                    {
                        if (dy > api.WorldManager.MapSizeY) continue;

                        chunkIndex = dy / chunksize;
                        blockIndex = (chunksize * (dy % chunksize) + z) * chunksize + x;
                        chunks[chunkIndex].Blocks[blockIndex] = 0;
                    }

                    heightMap[z * chunksize + x] = (ushort)(y - erosionIntensity);
                }
            }
        }
    }
}
