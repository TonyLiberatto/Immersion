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
                (api as ICoreServerAPI).SendMessage(api.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID), 0, GetHighestOre(blockSel.Position), EnumChatType.OwnMessage);
                slot.Itemstack.Collectible.DamageItem(api.World, byEntity, slot);
                slot.MarkDirty();
            }
        }

        public string GetHighestOre(BlockPos pos)
        {
            List<int> Ores = new List<int>();
            string amount = "a very large sample";
            int occurance = 0;
            string ore = "";
            if (pos.GetBlock(api).BlockMaterial == EnumBlockMaterial.Ore)
            {
                ore = pos.GetBlock(api).Variant["type"];
                return "Found " + char.ToUpper(ore[0]) + ore.Substring(1) + ".";
            }

            api.World.BlockAccessor.WalkBlocks(pos.AddCopy(12, 12, 12), pos.AddCopy(-12, -12, -12), (b, bp) =>
            {
                if (b.BlockMaterial == EnumBlockMaterial.Ore && !b.Code.ToString().Contains("quartz"))
                {
                    Ores.Add(b.Id);
                }
            });
            if (Ores.Count() == 0) return "Found nothing of interest.";

            int most = Ores.GroupBy(i => i).OrderByDescending(grp => grp.Count()).Select(grp => grp.Key).First();

            Ores.Any(o =>
            {
                if (o == most) occurance++;
                return false;
            });

            if (occurance < 10)
            {
                amount = "traces";
            }
            else if (occurance < 20)
            {
                amount = "a small sample";
            }
            else if (occurance < 40)
            {
                amount = "a medium sample";
            }
            else if (occurance < 40)
            {
                amount = "a large sample";
            }

            ore = api.World.GetBlock(most).Variant["type"];
            return string.Format("Found {0} of " + char.ToUpper(ore[0]) + ore.Substring(1) + ".", amount);
        }
    }
}
