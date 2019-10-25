using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Neolithic
{
    class BlockNeolithicRoads : Block
    {
		public string[] types = new string[] { "circle", "fish", "cobble", "bricks", "tightbricks", "squares", "tightsquares", "largesquare", "flat" };

		public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {

			ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                if (IsSettingHammer(slot))
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
                if (IsSettingHammer(slot))
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
                if (IsSettingHammer(slot))
                {
                    if (world.Side.IsServer())
                    {
                        uint index = (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BENeolithicRoads).index;
                        Block nextBlock;

                        if (byPlayer.Entity.Controls.Sneak) nextBlock = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + types.Prev(ref index)).GetBlock(api);
                        else nextBlock = new AssetLocation("neolithicmod:" + CodeWithoutParts(1) + "-" + types.Next(ref index)).GetBlock(api);

                        if (nextBlock == null) return;
                        (world.BlockAccessor.GetBlockEntity(blockSel.Position) as BENeolithicRoads).index = index;

                        world.PlaySoundAtWithDelay(nextBlock.Sounds.Place, blockSel.Position, 100);
                        world.PlaySoundAtWithDelay(new AssetLocation("sounds/effect/anvilhit"), blockSel.Position, 150);
                        world.BlockAccessor.ExchangeBlock(nextBlock.BlockId, blockSel.Position);
                        slot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, slot);
                    }
                    return;
                }
            }
        }

        public bool IsSettingHammer(ItemSlot slot) => slot.Itemstack.Collectible.FirstCodePart() == "settinghammer";
    }

    class BENeolithicRoads : BlockEntity
    {
        public uint index = 0;

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetInt("roadindex", (int)index);
            base.ToTreeAttributes(tree);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            index = (uint)tree.TryGetInt("roadindex");
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }
    }
}
