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
        public Dictionary<string, LootList> LootLists { get => BlockLootVessel.lootLists; }

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
                if (drops.ClearVanilla) LootLists.Clear();
                else ErrorCheckVessel(Api);

                foreach (var val in drops.vessels)
                {
                    LootLists[val.name] = LootList.Create(val.tries, val.drops.ToArray());
                }
            }
            ErrorCheckVessel(Api, true);
        }

        public void ErrorCheckVessel(ICoreAPI Api, bool verbose = false)
        {
            foreach (var vp in LootLists)
            {
                foreach (var li in vp.Value.lootItems)
                {
                    List<AssetLocation> validassets = new List<AssetLocation>();
                    string type = li.type.ToString().ToLower() + "type";

                    foreach (var c in li.codes)
                    {
                        if (Api.World.GetItem(c) != null || Api.World.GetBlock(c) != null) validassets.Add(c);
                        else if (verbose) Api.World.Logger.Error("Loot list " + type + " with the code " + c + " is not valid. Will remove from loot list.");
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
        public bool ClearVanilla { get; set; } = true;
        public List<Vessels> vessels { get; set; }
    }
}
