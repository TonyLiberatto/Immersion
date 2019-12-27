using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API;
using Vintagestory.GameContent;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Immersion
{
    class ItemSwapBlocks : Item
    {
        ICoreAPI Api { get => this.api; }
        Dictionary<AssetLocation, AssetLocation> swapMapping = new Dictionary<AssetLocation, AssetLocation>();
        public override void OnLoaded(ICoreAPI Api)
        {
            base.OnLoaded(Api);
            AssetLocation[][] arrSwap = Attributes["swapBlocks"].AsObject<AssetLocation[][]>();
            foreach (var val in arrSwap)
            {
                swapMapping[val[0]] = val[1];
            }
        }
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (blockSel != null)
            {
                int swapRate = slot.Itemstack.Collectible.Attributes["swapRate"].AsInt(0);
                Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                BlockPos Pos = blockSel.Position;

                if (!swapMapping.TryGetValue(block.Code, out AssetLocation toCode))
                {
                    return;
                }

                if (slot.Itemstack.StackSize >= swapRate)
                {
                    if (swapRate > 0)
                    {
                        slot.TakeOut(swapRate);
                    }
                    Api.World.PlaySoundAt(block.Sounds.Place, Pos.X, Pos.Y, Pos.Z);
                    Api.World.BlockAccessor.SetBlock(Api.World.GetBlock(toCode).BlockId, Pos);
                }
            }
        }
    }
}
