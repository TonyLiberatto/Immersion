using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;
using Vintagestory.GameContent;
using Vintagestory.API.Server;
using Vintagestory.API.Config;

namespace Immersion
{
    class ItemPropick : ItemProspectingPick
    {
        public Dictionary<string, bool> PreventDuplicates = new Dictionary<string, bool>();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!firstEvent) return;

            handling = EnumHandHandling.PreventDefaultAction;
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            return base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            if (api.Side.IsServer() && blockSel?.Position != null)
            {
                string uid = (byEntity as EntityPlayer).PlayerUID;
                if (!PreventDuplicates.ContainsKey((byEntity as EntityPlayer).PlayerUID)) PreventDuplicates[uid] = true;

                if (PreventDuplicates[uid])
                {
                    (api as ICoreServerAPI).SendMessage(api.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID), 0, GetHighestOre(blockSel.Position), EnumChatType.OwnMessage);
                    slot.Itemstack.Collectible.DamageItem(api.World, byEntity, slot);
                    slot.MarkDirty();

                    PreventDuplicates[uid] = false;
                    api.Event.RegisterCallback(dt => PreventDuplicates[uid] = true, 500);
                }
            }
        }

        public string GetHighestOre(BlockPos pos)
        {
            List<int> Ores = new List<int>();
            string amount;
            int occurance = 0;
            string ore = "";
            if (pos.GetBlock(api).BlockMaterial == EnumBlockMaterial.Ore)
            {
                ore = Lang.Get(pos.GetBlock(api).Variant["type"]);
                ore = char.ToUpper(ore[0]) + ore.Substring(1);

                return Lang.Get("immersion:propick-found", ore);
            }

            api.World.BlockAccessor.WalkBlocks(pos.AddCopy(12, 12, 12), pos.AddCopy(-12, -12, -12), (b, bp) =>
            {
                if (b.BlockMaterial == EnumBlockMaterial.Ore && !b.Code.ToString().Contains("quartz"))
                {
                    Ores.Add(b.Id);
                }
            });
            if (Ores.Count() == 0) return Lang.Get("immersion:propick-nothing");

            int most = Ores.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            foreach (var o in Ores)
            {
                if (o == most) occurance++;
            }

            ore = Lang.Get(api.World.GetBlock(most).Variant["type"]);
            ore = char.ToUpper(ore[0]) + ore.Substring(1);

            if (occurance < 10)
            {
                amount = Lang.Get("immersion:propick-traces", ore);
            }
            else if (occurance < 20)
            {
                amount = Lang.Get("immersion:propick-small", ore);
            }
            else if (occurance < 40)
            {
                amount = Lang.Get("immersion:propick-medium", ore);
            }
            else if (occurance < 80)
            {
                amount = Lang.Get("immersion:propick-large", ore);
            }
            else
            {
                amount = Lang.Get("immersion:propick-verylarge", ore);
            }
            
            return amount;
        }
    }
}
