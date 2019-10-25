using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Immersion
{
    public class FixedBESapling : BlockEntity
    {
        static Random rand = new Random();

        public static Dictionary<string, string> FixedTreeGenMapping = new Dictionary<string, string>
        {
            { "birch", "silverbirch" },
            { "oak", "englishoak" },
            { "maple", "sugarmaple" },
            { "pine", "scotspine" },
            { "acacia", "truemulga" },
            { "kapok", "kapok" },
            { "bamboogreen", "bamboo-grown-green" },
            { "bamboobrown", "bamboo-grown-brown" }
        };


        double totalHoursTillGrowth;
        long growListenerId;

        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);

            if (Api is ICoreServerAPI)
            {
                growListenerId = RegisterGameTickListener(OurCheckGrow, 2000);
            }
        }


        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            totalHoursTillGrowth = Api.World.Calendar.TotalHours + (5 + 3 * rand.NextDouble()) * 24;

        }


        private void OurCheckGrow(float dt)
        {
            if (Api.World.Calendar.TotalHours < totalHoursTillGrowth) return;


            string treeCode = Api.World.BlockAccessor.GetBlock(Pos).Variant["wood"];

            string treeGenCode = null;
            if (!FixedTreeGenMapping.TryGetValue(treeCode, out treeGenCode))
            {
                Api.Event.UnregisterGameTickListener(growListenerId);
                return;
            }

            AssetLocation code = new AssetLocation(treeGenCode);
            ICoreServerAPI sapi = Api as ICoreServerAPI;

            ITreeGenerator gen = null;
            if (!sapi.World.TreeGenerators.TryGetValue(code, out gen))
            {
                Api.Event.UnregisterGameTickListener(growListenerId);
                return;
            }

            Api.World.BlockAccessor.SetBlock(0, Pos);
            Api.World.BulkBlockAccessor.ReadFromStagedByDefault = true;
            float size = 0.6f + (float)Api.World.Rand.NextDouble() * 0.5f;
            sapi.World.TreeGenerators[code].GrowTree(Api.World.BulkBlockAccessor, Pos.DownCopy(), size);

            Api.World.BulkBlockAccessor.Commit();
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetDouble("totalHoursTillGrowth", totalHoursTillGrowth);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);

            totalHoursTillGrowth = tree.GetDouble("totalHoursTillGrowth", 0);
        }

    }
}
