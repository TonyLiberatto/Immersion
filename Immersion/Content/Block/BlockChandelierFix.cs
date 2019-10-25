using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace Neolithic
{
    class BlockChandelierFix : Block
    {
        public int CandleCount
        {
            get
            {
                switch (LastCodePart(0))
                {
                    case "candle0":
                        return 0;
                    case "candle1":
                        return 1;
                    case "candle2":
                        return 2;
                    case "candle3":
                        return 3;
                    case "candle4":
                        return 4;
                    case "candle5":
                        return 5;
                    case "candle6":
                        return 6;
                    case "candle7":
                        return 7;
                    case "candle8":
                        return 8;
                    default:
                        return -1;
                }
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemStack itemstack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
            if (itemstack == null || !itemstack.Collectible.Attributes["isCandle"].AsBool() || CandleCount == 8) return false;
            if (byPlayer != null && byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival) byPlayer.InventoryManager.ActiveHotbarSlot.TakeOut(1);

            Block block = world.GetBlock(CodeWithParts(GetNextCandleCount()));
            world.BlockAccessor.ExchangeBlock(block.BlockId, blockSel.Position);
            world.BlockAccessor.MarkBlockDirty(blockSel.Position);
            return true;
        }

        private string GetNextCandleCount()
        {
            if (CandleCount != 8)
                return string.Format("candle{0}", (object)(CandleCount + 1));
            return "";
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (CandleCount == 8) return null;
            return new WorldInteraction[1] { new WorldInteraction() { ActionLangCode = "blockhelp-chandelier-addcandle", MouseButton = EnumMouseButton.Right, Itemstacks = new ItemStack[] { new ItemStack(world.GetItem(new AssetLocation("candle-wax")), 1), new ItemStack(world.GetItem(new AssetLocation("candle-tallow")), 1) } } }.Append<WorldInteraction>(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
