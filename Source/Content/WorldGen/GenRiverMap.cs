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
        IWorldGenBlockAccessor blockAccessor;
        MapLayerBase riverGen;
        int noiseSizeRiver;
        public ImmersionGlobalConfig config { get => api.ModLoader.GetModSystem<ModifyLakes>().config; }

        public int chunksize2 { get => chunksize > 0 ? chunksize : 32; }

        public override bool ShouldLoad(EnumAppSide forSide) => false;// forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.MapRegionGeneration(OnMapRegionGen, "standard");
            api.Event.InitWorldGenerator(InitWorldGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            api.Event.GetWorldgenBlockAccessor(c => blockAccessor = c.GetBlockAccessor(true));
        }

        private void OnMapRegionGen(IMapRegion mapRegion, int regionX, int regionZ)
        {
            mapRegion.ModData["rivermap"] = JsonUtil.ToBytes(
                new IntMap() { 
                    Data = riverGen.GenLayer(regionX * noiseSizeRiver, regionZ * noiseSizeRiver, noiseSizeRiver + 1, noiseSizeRiver + 1),
                    BottomRightPadding = 1,
                    Size = noiseSizeRiver + 1
                }
                );
        }

        public void InitWorldGen()
        {
            long seed = api.WorldManager.Seed;
            riverGen = new MapLayerPerlin(seed + 1, 6, 0.9f, TerraGenConfig.beachMapScale / 16, 255, new double[] { 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f });
            noiseSizeRiver = api.WorldManager.RegionSize / TerraGenConfig.beachMapScale;
        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            IntMap riverMap = JsonUtil.FromBytes<IntMap>(chunks[0].MapChunk.MapRegion.ModData["rivermap"]);
            Vec3i climate = chunks[0].MapChunk.MapRegion.ClimateMap.ToClimateVec(chunkX, chunkZ, api.WorldManager.RegionSize, chunksize2);
            ushort[] heightMap = chunks[0].MapChunk.RainHeightMap;

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
                    NormalizedSimplexNoise noise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 1.0, api.WorldManager.Seed);
                    int posY = (int)(TerraGenConfig.seaLevel - 8 - (noise.Noise(z, x) * 2));
                    float riverRel = GameMath.BiLerp(riverUpLeft, riverUpRight, riverBotLeft, riverBotRight, (float)x / chunksize2, (float)z / chunksize2) / 255f;
                    int posYAlt = posY;
                    
                    while (posYAlt > posY - 4 - (noise.Noise(x, z) * 2))
                    {
                        GenRiver(x, posYAlt, z, chunks, riverRel);
                        posYAlt--;
                    }
                }
            }
        }

        private void GenRiver(int x, int posY, int z, IServerChunk[] chunks, float riverRel)
        {
            if (riverRel > 0.5 || chunks.Count() - 1 < posY / chunksize2) return;
            chunks[posY / chunksize2].Blocks[(chunksize2 * (posY % chunksize2) + z) * chunksize2 + x] = config.LakeWaterBlockId;
        }
    }
}
