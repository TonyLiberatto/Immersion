using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    class BlockCheeseCloth : Block
    {
        ICoreAPI Api { get => this.api; }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null || slot.Itemstack.Collectible.Variant["contents"] == "curds" || slot.Itemstack.Collectible.Variant["contents"] == "cheese") return;
            Block selBlock = Api.World.BlockAccessor.GetBlock(blockSel.Position);
            if (selBlock is BlockBucket)
            {
                handling = EnumHandHandling.PreventDefault;
            }
        }

        public override bool OnHeldInteractStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (blockSelection == null || slot.Itemstack.Collectible.Variant["contents"] == "curds" || slot.Itemstack.Collectible.Variant["contents"] == "cheese") return false;
            Block selBlock = Api.World.BlockAccessor.GetBlock(blockSelection.Position);
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
            BlockPos Pos = blockSel.Position;
            Block selBlock = Api.World.BlockAccessor.GetBlock(Pos);

            if (Api.World.Side.IsServer())
            {
                if (selBlock is BlockBucket)
                {
                    BlockBucket bucket = selBlock as BlockBucket;
                    WaterTightContainableProps contentProps = bucket.GetContentProps(byEntity.World, Pos);
                    if (bucket.GetContent(byEntity.World, Pos) != null)
                    {
                        ItemStack contents = bucket.GetContent(byEntity.World, Pos);
                        if (contents.Item.Code.Path == "curdsportion" && slot.Itemstack.Collectible.Variant["contents"] == "none")
                        {
                            ItemStack curdsandwhey = new ItemStack(CodeWithPart("curdsandwhey", 2).GetBlock(Api), 1);

                            bucket.TryTakeContent(Api.World, Pos, 2);

                            TryGiveItem(curdsandwhey, slot, byEntity, contentProps, Pos);
                            return;
                        }
                    }
                    if ((bucket.GetContent(byEntity.World, Pos) == null || bucket.GetContent(byEntity.World, Pos).Item.Code.Path == "wheyportion") && slot.Itemstack.Collectible.Variant["contents"] == "curdsandwhey")
                    {
                        ItemStack curds = new ItemStack(CodeWithPart("curds", 2).GetBlock(Api), 1);
                        ItemStack wheyportion = new ItemStack(new AssetLocation("wheyportion").GetItem(Api), 1);
                        bucket.TryPutContent(Api.World, Pos, wheyportion, 1);

                        TryGiveItem(curds, slot, byEntity, contentProps, Pos);
                        return;
                    }
                }
            }
            slot.MarkDirty();
        }

        public void TryGiveItem(ItemStack stack, ItemSlot slot, EntityAgent byEntity, WaterTightContainableProps props, BlockPos Pos)
        {
            slot.TakeOut(1);
            if (!byEntity.TryGiveItemStack(stack))
            {
                Api.World.SpawnItemEntity(stack, Pos.ToVec3d());
            }
            Api.World.PlaySoundAt(props.FillSpillSound, Pos.X, Pos.Y, Pos.Z);
        }
    }
}
