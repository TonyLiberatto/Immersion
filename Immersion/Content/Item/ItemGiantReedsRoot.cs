using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Neolithic
{
    public class ItemGiantReedsRoot : Item
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity.World == null || byEntity.Controls.Sneak )
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            bool waterBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position.AddCopy(blockSel.Face)).LiquidCode == "water";

            Block block = byEntity.World.GetBlock(new AssetLocation(waterBlock ? "giantreed-arundo-water-harvested" : "giantreed-arundo-free-harvested"));

            if (block == null)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
                return;
            }

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            blockSel = blockSel.Clone();
            blockSel.Position.Add(blockSel.Face);
            string f = "";

            bool ok = block.TryPlaceBlock(byEntity.World, byPlayer, itemslot.Itemstack, blockSel, ref f);

            if (ok)
            {
                itemslot.TakeOut(1);
                itemslot.MarkDirty();
                handHandling = EnumHandHandling.PreventDefaultAction;
            }
        }
    }
}