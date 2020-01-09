using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Immersion
{
    class LootVesselFix : ModSystem
    {
        public override void StartServerSide(ICoreServerAPI Api)
        {
            Api.Event.SaveGameLoaded += () => LootVesselRemap(Api);
        }

        public override void StartClientSide(ICoreClientAPI Api)
        {
            Api.Event.BlockTexturesLoaded += () => LootVesselRemap(Api);
        }

        public void LootVesselRemap(ICoreAPI Api)
        {
            VesselDrops drops = Api.Assets.TryGet("config/vesseldrops.json")?.ToObject<VesselDrops>();
            if (drops != null)
            {
                BlockLootVessel.lootLists.Clear();

                foreach (var val in drops.vessels)
                {
                    BlockLootVessel.lootLists[val.name] = LootList.Create(val.tries, val.drops.ToArray());
                }
            }
            foreach (var vp in BlockLootVessel.lootLists)
            {
                foreach (var li in vp.Value.lootItems)
                {
                    List<AssetLocation> validassets = new List<AssetLocation>();
                    foreach (var c in li.codes)
                    {
                        if (Api.World.GetItem(c) != null || Api.World.GetBlock(c) != null)
                        {
                            validassets.Add(c);
                        }
                    }
                    li.codes = validassets.ToArray();
                }
            }

        }
    }

    class Vessels
    {
        public string name { get; set; }
        public float tries { get; set; }
        public List<LootItem> drops { get; set; }
    }

    class VesselDrops
    {
        public List<Vessels> vessels { get; set; }
    }
}
