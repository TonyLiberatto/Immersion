using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    public class ItemIngotOverride : ItemIngot
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            if (blockSel == null || byEntity?.World == null || !byEntity.Controls.Sneak) return;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            if (byPlayer == null) return;

            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                itemslot.MarkDirty();
                return;
            }

            BlockIngotPileOverride block = byEntity.World.GetBlock(new AssetLocation("ingotpile")) as BlockIngotPileOverride;
            if (block == null) return;

            BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is IngotPileOverride)
            {
                IngotPileOverride pile = (IngotPileOverride)be;
                if (pile.OnPlayerInteract(byPlayer))
                {
                    handHandling = EnumHandHandling.PreventDefault;
                }
                return;
            }

            if (be is BlockEntityAnvil)
            {
                return;
            }

            BlockPos pos = blockSel.Position.AddCopy(blockSel.Face);
            if (byEntity.World.BlockAccessor.GetBlock(pos).Replaceable < 6000) return;

            be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
            if (be is IngotPileOverride)
            {
                IngotPileOverride pile = (IngotPileOverride)be;
                if (pile.OnPlayerInteract(byPlayer))
                {
                    handHandling = EnumHandHandling.PreventDefault;
                }
                return;
            }


            if (block.Construct(itemslot, byEntity.World, blockSel.Position.AddCopy(blockSel.Face), byPlayer))
            {
                handHandling = EnumHandHandling.PreventDefault;
            }

        }
    }
}
