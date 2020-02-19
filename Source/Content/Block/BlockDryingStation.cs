using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class BlockDryingStation : Block
    {
        public DryingProp[] props;

        public override void OnLoaded(ICoreAPI Api)
        {
            base.OnLoaded(Api);
            props = Attributes["dryingprops"].AsObject<DryingProp[]>();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            (blockSel.BlockEntity(world) as BlockEntityDryingStation)?.OnInteract(world, byPlayer, blockSel);
            return true;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos Pos, IPlayer forPlayer)
        {
            StringBuilder builder = new StringBuilder(base.GetPlacedBlockInfo(world, Pos, forPlayer));
            BlockEntityDryingStation craftingStation = (Pos.BlockEntity(world) as BlockEntityDryingStation);
            ItemStack stack = craftingStation?.inventory?[0]?.Itemstack;
            builder = stack != null ? builder.AppendLine().AppendLine(stack.StackSize + "x " +
                Lang.Get("incontainer-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code.Path)) : builder;
            return builder.ToString();
        }
    }
}
