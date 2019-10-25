using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Neolithic
{
    class BlockLooseStonesModified : BlockLooseStones
    {
        public readonly string[] allowedbases = new string[]
        {
            "soil",
            "gravel",
            "sand"
        };

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "tamper")
                {
                    return true;
                }
            }
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "tamper")
                {
                    return HandAnimations.Hit(byPlayer.Entity, secondsUsed);
                }
            }
            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (slot.Itemstack.Collectible.FirstCodePart() == "tamper")
                {
                    if (world.Side.IsServer())
                    {
                        Block dBlock = blockSel.Position.DownCopy().GetBlock(world);

                        if (!allowedbases.Contains(dBlock.FirstCodePart())) return;
                        AssetLocation location = new AssetLocation("neolithicmod:3droad-" + Variant["rock"] + "-" + "stepping" + world.Rand.Next(1, 4));
                        Block nextBlock = location.GetBlock(api);
                        if (nextBlock == null) return;

                        world.PlaySoundAtWithDelay(nextBlock.Sounds.Place, blockSel.Position, 200);
                        world.PlaySoundAtWithDelay(new AssetLocation("sounds/block/chop"), blockSel.Position, 100);
                        world.BlockAccessor.SetBlock(nextBlock.BlockId, blockSel.Position);
                        slot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, slot);
                    }
                    return;
                }
            }
        }
    }
}
