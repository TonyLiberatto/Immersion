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
    class GenRiverMap : ModStdWorldGen
    {
        ICoreServerAPI api;
        IWorldGenBlockAccessor blockAccessor;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.api = api;
            api.Event.InitWorldGenerator(InitWorldGen, "standard");
            api.Event.ChunkColumnGeneration(OnChunkColumnGen, EnumWorldGenPass.Vegetation, "standard");
            api.Event.GetWorldgenBlockAccessor(c => blockAccessor = c.GetBlockAccessor(true));
        }

        public void InitWorldGen()
        {

        }

        private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ, ITreeAttribute chunkGenParams = null)
        {
            Vec3i climate = chunks[0].MapChunk.MapRegion.ClimateMap.ToClimateVec(chunkX, chunkZ, api.WorldManager.RegionSize, chunksize);
        }
    }
}
