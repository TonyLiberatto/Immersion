using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Neolithic
{
    class BlockCraftingStation : Block
    {
        public CraftingProp[] craftingProps;
        public AnimProps animProps;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            craftingProps = Attributes["craftingprops"].AsObject<CraftingProp[]>();
            animProps = Attributes["animprops"].AsObject<AnimProps>();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            (blockSel.BlockEntity(world) as BlockEntityCraftingStation)?.OnInteract(world, byPlayer, blockSel);
            return true;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder builder = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer));
            BlockEntityCraftingStation craftingStation = (pos.BlockEntity(world) as BlockEntityCraftingStation);
            builder = craftingStation?.inventory?[0]?.Itemstack != null ? builder.AppendLine().AppendLine(craftingStation.inventory[0].StackSize + "x " + Lang.Get(craftingStation.inventory[0].Itemstack.Collectible.Code.ToString())) : builder;
            return builder.ToString();
        }
    }
}