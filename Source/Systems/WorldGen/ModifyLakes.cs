using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    public class ModifyLakes : ModSystem
    {
        ICoreServerAPI Api;
        public override double ExecuteOrder() => 0;
        public override bool ShouldLoad(EnumAppSide forSide) => forSide.IsServer();
        long[] ids = new long[2];
        public ImmersionGlobalConfig config;

        public override void StartServerSide(ICoreServerAPI Api)
        {
            this.Api = Api;
            
            Api.Event.ServerRunPhase(EnumServerRunPhase.LoadGamePre, ChangeLakeWater);
        }

        public void ChangeLakeWater()
        {
            config = Api.Assets.Get("worldgen/global.json").ToObject<ImmersionGlobalConfig>();
            config.SetApi(Api);
            ids[0] = Api.Event.RegisterGameTickListener(dt => 
            {
                if (Api.ModLoader.GetModSystem<GenPonds>().GlobalConfig != null)
                {
                    Api.ModLoader.GetModSystem<GenPonds>().GlobalConfig.waterBlockCode = config.lakeWaterBlockCode;
                    Api.ModLoader.GetModSystem<GenPonds>().GlobalConfig.waterBlockId = config.LakeWaterBlockId;
                    Api.Event.UnregisterGameTickListener(ids[0]);
                }
            }, 30);
            ids[1] = Api.Event.RegisterGameTickListener(dt =>
            {
                if (Api.ModLoader.GetModSystem<GenRivulets>().GlobalConfig != null)
                {
                    Api.ModLoader.GetModSystem<GenRivulets>().GlobalConfig.waterBlockCode = config.lakeWaterBlockCode;
                    Api.ModLoader.GetModSystem<GenRivulets>().GlobalConfig.waterBlockId = config.LakeWaterBlockId;
                    Api.Event.UnregisterGameTickListener(ids[1]);
                }
            }, 30);
        }
    }

    public class BlockSeaweedOverride : BlockSeaweed
    {
        public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos Pos, BlockFacing onBlockFace, LCGRandom worldGenRand)
        {
            bool gen = false;
            BlockPos Pos1 = Pos.DownCopy(1);
            if (blockAccessor.GetBlock(Pos1).LiquidCode == "seawater")
            {
                blockAccessor.GetBlock(Pos1).LiquidCode = "water";
                gen = base.TryPlaceBlockForWorldGen(blockAccessor, Pos, onBlockFace, worldGenRand);
                blockAccessor.GetBlock(Pos1).LiquidCode = "seawater";
            }
            return gen;
        }
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class ImmersionGlobalConfig
    {
        [JsonProperty]
        public AssetLocation waterBlockCode;

        [JsonProperty]
        public AssetLocation lakeWaterBlockCode;

        [JsonProperty]
        public AssetLocation lakeIceBlockCode;

        [JsonProperty]
        public AssetLocation glacierIceBlockCode;

        [JsonProperty]
        public AssetLocation lavaBlockCode;

        [JsonProperty]
        public AssetLocation basaltBlockCode;

        [JsonProperty]
        public AssetLocation mantleBlockCode;

        [JsonProperty]
        public AssetLocation defaultRockCode;

        private ICoreAPI Api;

        public int waterBlockId { get => waterBlockCode.GetID(Api); }
        public int LakeWaterBlockId { get => lakeWaterBlockCode.GetID(Api); }
        public int LakeIceBlockId { get => lakeIceBlockCode.GetID(Api); }
        public int GlacierIceBlockId { get => glacierIceBlockCode.GetID(Api); }
        public int LavaBlockId { get => lavaBlockCode.GetID(Api); }
        public int BasaltBlockId { get => basaltBlockCode.GetID(Api); }
        public int MantleBlockId { get => mantleBlockCode.GetID(Api); }
        public int DefaultRockId { get => defaultRockCode.GetID(Api); }

        public void SetApi(ICoreAPI Api)
        {
            this.Api = Api;
        }
    }
}
