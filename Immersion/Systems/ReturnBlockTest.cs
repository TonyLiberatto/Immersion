using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace Immersion
{
    class BlockToolMoldReturnBlock : BlockToolMold
    {
        ICoreAPI Api { get => this.api; }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel == null) return false;
            BlockEntityToolMold be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityToolMold;
            if (be != null)
            {
                if (be.IsFull && be.IsHardened && world.Side.IsServer())
                {
                    if (Attributes["returnBlock"].Exists)
                    {
                        world.BlockAccessor.SetBlock(Attributes["returnBlock"].AsString().ToBlock(Api).BlockId, blockSel.Position);
                    }
                }
                return be.OnPlayerInteract(byPlayer, blockSel.Face, blockSel.HitPosition);
            } 
            return false;
        }
    }
}
