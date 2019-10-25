using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Neolithic
{
    class BEBucketOverride : BlockEntityBucket
    {
        BlockBucket bucket;
        public double updateTime;
        long id;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            bucket = new BlockBucket();
            if (api.World.Side.IsServer())
            {
                if (updateTime == 0) updateTime = ResetTimer();
                id = RegisterGameTickListener(OnGameTick, 30);
            }
        }

        public double ResetTimer()
        {
            return api.World.Calendar.TotalHours + 42;
        }

        public override string GetBlockInfo(IPlayer forPlayer)
        {
            string a = "\n";
            ItemStack contents = bucket.GetContent(api.World, pos);
            if (contents != null)
            {
                if (contents.Item.FirstCodePart() == "milkportion")
                {
                    a += "Becomes Curds In " + (int)(updateTime - api.World.Calendar.TotalHours) + " Hours";
                }
            }
            return a + base.GetBlockInfo(forPlayer);
        }

        public void OnGameTick(float dt)
        {
            if (updateTime < api.World.Calendar.TotalHours)
            {
                ItemStack contents = bucket.GetContent(api.World, pos);
                if (contents != null)
                {
                    if (contents.Item.FirstCodePart() == "milkportion")
                    {
                        ItemStack curds = new ItemStack(api.World.GetItem(new AssetLocation("game:curdsportion")), 1);
                        curds.StackSize = contents.StackSize;
                        bucket.SetContent(api.World, pos, curds);
                    }
                }
                updateTime = ResetTimer();
            }
        }
    }
}
