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
        RidgedNoise noise;
        BlockLayer soilLayer;
        LakeBedLayerProperties lakebedLayerConfig;
        ImmersionGlobalConfig immersionConfig;
        BlockLayerConfig blockLayerConfig;

        public int chunksize2 { get => chunksize > 0 ? chunksize : 32; }
        public override double ExecuteOrder() => 0.45;

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
            riverGen = new MapLayerRidged(seed + 46841, 1, 1.0f, 16, 255, new double[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f });
            
            noiseSizeRiver = api.WorldManager.RegionSize / 32;
            noise = RidgedNoise.FromDefaultOctaves(1, 0.1, 1.0, api.WorldManager.Seed + 465814);
            blockLayerConfig = BlockLayerConfig.GetInstance(api);
            lakebedLayerConfig = blockLayerConfig.LakeBedLayer;
            soilLayer = blockLayerConfig.Blocklayers.OrderByDescending(a => a.ID == "l1soilwithgrass").First();

            immersionConfig = api.ModLoader.GetModSystem<ModifyLakes>().config;
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
                    double n = noise.Noise(chunkX * chunksize + x, chunkZ * chunksize + z);
                    float riverRel = 1.0f - (GameMath.BiLerp(riverUpLeft, riverUpRight, riverBotLeft, riverBotRight, (float)x / chunksize2, (float)z / chunksize2) / 255f);

                    int y = heightMap[z * chunksize + x];

                    int dy;
                    int chunkIndex, blockIndex;
                    int rockBlockId = chunks[0].MapChunk.TopRockIdMap[z * chunksize + x];
                    int distanceFromSeaLevel = (y - TerraGenConfig.seaLevel);

                    int erosionIntensity = distanceFromSeaLevel + 1 + (int)(4 * n);
                    int lakeBed = lakebedLayerConfig.BlockCodeByMin[0].GetBlockForMotherRock(rockBlockId);
                    Vec3i climate = chunks[0].MapChunk.MapRegion.ClimateMap.ToClimateVec(chunkX, chunkZ, api.WorldManager.RegionSize, chunksize, (float)x / chunksize, (float)z / chunksize);

                    {
                        float riverMin = 0.55f, riverMax = 0.6f;
                        float shoreMin = 0.54f, shoreMax = 0.61f;
                        bool left = riverRel < riverMin, right = riverRel > riverMax;

                        if (left || right)
                        {
                            if (riverRel > shoreMin && riverRel < shoreMax)
                            {
                                float shore = ((riverRel - shoreMin) / (shoreMax - shoreMin));
                                shore = left ? 1.0f - shore : right ? shore : 0;
                                bool closeToRiver = shore < 0.925;

                                int height = closeToRiver ? 0 : (int)(distanceFromSeaLevel * shore);

                                int shoreHeight = (TerraGenConfig.seaLevel - 1) + height;

                                for (dy = y - erosionIntensity - 1; dy <= y + 1; dy++)
                                {
                                    chunkIndex = dy / chunksize;
                                    blockIndex = (chunksize * (dy % chunksize) + z) * chunksize + x;
                                    int replacing = chunks[chunkIndex].Blocks[blockIndex];
                                    if (replacing == config.waterBlockId) continue;
                                    if (dy < shoreHeight && replacing != 0) continue;
                                    float temp = TerraGenConfig.GetScaledAdjustedTemperatureFloat(climate.Z, dy - TerraGenConfig.seaLevel);
                                    float rain = TerraGenConfig.GetRainFall(climate.X, dy);
                                    int shorematerial = closeToRiver ? soilLayer.GetBlockId(n, temp, rain, 1.0f, 0) : lakeBed;
                                    chunks[chunkIndex].Blocks[blockIndex] = dy < TerraGenConfig.seaLevel && replacing == 0 ? rockBlockId : dy == shoreHeight ? shorematerial : 0;
                                    chunks[chunkIndex].MarkModified();
                                }
                                heightMap[z * chunksize + x] = (ushort)shoreHeight;
                            }
                            continue;
                        }

                        for (dy = y - erosionIntensity; dy <= y + 1; dy++)
                        {
                            if (dy >= api.World.BlockAccessor.MapSizeY) break;
                            chunkIndex = dy / chunksize;
                            blockIndex = (chunksize * (dy % chunksize) + z) * chunksize + x;
                            if (chunks[chunkIndex].Blocks[blockIndex] == config.waterBlockId && dy < TerraGenConfig.seaLevel) continue;

                            float temp = TerraGenConfig.GetScaledAdjustedTemperatureFloat(climate.Z, dy - TerraGenConfig.seaLevel);

                            int waterOrIce = temp < -5 ? immersionConfig.LakeIceBlockId : immersionConfig.LakeWaterBlockId;


                            chunks[chunkIndex].Blocks[blockIndex] = dy == y - erosionIntensity ? lakeBed : dy < TerraGenConfig.seaLevel ? waterOrIce : 0;
                            chunks[chunkIndex].MarkModified();
                        }
                        heightMap[z * chunksize + x] = (ushort)(TerraGenConfig.seaLevel - 1);
                    }
                }
            }
        }
    }
}
