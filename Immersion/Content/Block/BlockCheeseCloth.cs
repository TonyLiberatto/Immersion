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
    class BlockCheeseCloth : Block
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || slot.Itemstack.Collectible.Variant["contents"] == "curds" || slot.Itemstack.Collectible.Variant["contents"] == "cheese") return;

            Block selBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
            if (selBlock is BlockBucket)
            {
                handling = EnumHandHandling.PreventDefault;
            }
        }

        public override bool OnHeldInteractStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (blockSelection == null || slot.Itemstack.Collectible.Variant["contents"] == "curds" || slot.Itemstack.Collectible.Variant["contents"] == "cheese") return false;
            Block selBlock = api.World.BlockAccessor.GetBlock(blockSelection.Position);
            if (selBlock is BlockBucket)
            {
                return HandAnimations.Collect(byEntity, secondsPassed);
            }
            return false;
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null || slot.Itemstack.Collectible.Variant["contents"] == "curds" || slot.Itemstack.Collectible.Variant["contents"] == "cheese") return;
            BlockPos pos = blockSel.Position;
            Block selBlock = api.World.BlockAccessor.GetBlock(pos);

            if (api.World.Side.IsServer())
            {
                if (selBlock is BlockBucket)
                {
                    BlockBucket bucket = selBlock as BlockBucket;
                    WaterTightContainableProps contentProps = bucket.GetContentProps(byEntity.World, pos);
                    if (bucket.GetContent(byEntity.World, pos) != null)
                    {
                        ItemStack contents = bucket.GetContent(byEntity.World, pos);
                        if (contents.Item.Code.Path == "curdsportion" && slot.Itemstack.Collectible.Variant["contents"] == "none")
                        {
                            ItemStack curdsandwhey = new ItemStack(CodeWithPart("curdsandwhey", 2).GetBlock(api), 1);

                            bucket.TryTakeContent(api.World, pos, 2);

                            TryGiveItem(curdsandwhey, slot, byEntity, contentProps, pos);
                            return;
                        }
                    }
                    if ((bucket.GetContent(byEntity.World, pos) == null || bucket.GetContent(byEntity.World, pos).Item.Code.Path == "wheyportion") && slot.Itemstack.Collectible.Variant["contents"] == "curdsandwhey")
                    {
                        ItemStack curds = new ItemStack(CodeWithPart("curds", 2).GetBlock(api), 1);
                        ItemStack wheyportion = new ItemStack(new AssetLocation("wheyportion").GetItem(api), 1);
                        bucket.TryPutContent(api.World, pos, wheyportion, 1);

                        TryGiveItem(curds, slot, byEntity, contentProps, pos);
                        return;
                    }
                }
            }
            slot.MarkDirty();
        }

        public void TryGiveItem(ItemStack stack, ItemSlot slot, EntityAgent byEntity, WaterTightContainableProps props, BlockPos pos)
        {
            slot.TakeOut(1);
            if (!byEntity.TryGiveItemStack(stack))
            {
                api.World.SpawnItemEntity(stack, pos.ToVec3d());
            }
            api.World.PlaySoundAt(props.FillSpillSound, pos.X, pos.Y, pos.Z);
        }
    }
}
