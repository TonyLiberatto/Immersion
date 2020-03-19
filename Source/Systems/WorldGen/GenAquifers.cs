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
    public class GenAquifers : ModStdWorldGen
    {
        ICoreServerAPI api;
        MapLayerBase aquiferGen;
        int noiseSizeRiver;
        public ImmersionGlobalConfig config { get => api.ModLoader.GetModSystem<ModifyLakes>().config; }
        NormalizedSimplexNoise noise;

        public int chunksize2 { get => chunksize > 0 ? chunksize : 32; }
        public override double ExecuteOrder() => 0.1;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

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
                    Data = aquiferGen.GenLayer(regionX * noiseSizeRiver, regionZ * noiseSizeRiver, noiseSizeRiver + 1, noiseSizeRiver + 1),
                    BottomRightPadding = 1,
                    Size = noiseSizeRiver + 1
                }
                );
        }

        public void InitWorldGen()
        {
            long seed = api.WorldManager.Seed;
            aquiferGen = new MapLayerPerlin(seed + 46841, 6, 0.1f, 1, 255, new double[] { 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f });
            noiseSizeRiver = api.WorldManager.RegionSize / 16;
            noise = NormalizedSimplexNoise.FromDefaultOctaves(2, 0.1, 1.0, api.WorldManager.Seed + 1276);
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            IntMap riverMap = JsonUtil.FromBytes<IntMap>(chunks[0].MapChunk.MapRegion.ModData["rivermap"]);
            //ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

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
                    if (riverRel > 0.5) continue;

                    int sub = (int)Math.Round(riverRel * 8);
                    
                    int maxY = TerraGenConfig.seaLevel - 20 - (int)Math.Round(n * 2);
                    int minY = maxY - sub;

                    int dY = maxY;
                    int rockID = chunks[0].MapChunk.TopRockIdMap[z * chunksize + x];
                    Vec2i iMax = new Vec2i((maxY + 1) / chunksize2, (chunksize2 * ((maxY + 1) % chunksize2) + z) * chunksize2 + x);
                    Vec2i iMin = new Vec2i((minY - 1) / chunksize2, (chunksize2 * ((minY - 1) % chunksize2) + z) * chunksize2 + x);

                    if (chunks[iMax.X].Blocks[iMax.Y] == 0)
                    {
                        chunks[iMax.X].Blocks[iMax.Y] = rockID;
                    }
                        
                    if (chunks[iMin.X].Blocks[iMin.Y] == 0)
                    {
                        chunks[iMin.X].Blocks[iMin.Y] = rockID;
                    }

                    while (dY >= minY)
                    {
                        if (chunks[dY / chunksize2].Blocks[(chunksize2 * (dY % chunksize2) + z) * chunksize2 + x] == 0)
                        {
                            chunks[dY / chunksize2].Blocks[(chunksize2 * (dY % chunksize2) + z) * chunksize2 + x] = rockID;
                        }
                        if (riverRel < 0.45)
                        {
                            chunks[dY / chunksize2].Blocks[(chunksize2 * (dY % chunksize2) + z) * chunksize2 + x] = config.LakeWaterBlockId;
                        }

                        dY--;
                    }
                }
            }
        }
    }
}
