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

namespace Neolithic
{
    class ItemSwapBlocks : Item
    {
        Dictionary<AssetLocation, AssetLocation> swapMapping = new Dictionary<AssetLocation, AssetLocation>();
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
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
                BlockPos pos = blockSel.Position;

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
                    api.World.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                    api.World.BlockAccessor.SetBlock(api.World.GetBlock(toCode).BlockId, pos);
                }
            }
        }
    }
}
