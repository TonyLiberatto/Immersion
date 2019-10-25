using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Immersion
{
    class LootVesselFix : ModSystem
    {
        public override void Start(ICoreAPI Api)
        {
            BlockLootVessel.lootLists.Clear();
            VesselDrops drops = Api.Assets.TryGet("config/vesseldrops.json").ToObject<VesselDrops>();
            foreach (var val in drops.vessels)
            {
                BlockLootVessel.lootLists[val.name] = LootList.Create(val.tries, val.drops.ToArray());
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
