using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class BlockWell : Block
    {
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            
            StringBuilder bdr = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer));
            BlockEntityWell well = pos.BlockEntity(world) as BlockEntityWell;
            if (well != null)
            {
                bdr.AppendLine("Depth: " + well.Depth);
                bdr.AppendLine("FoundLiquid: " + well.FoundLiquidCode);
            }

            return bdr.ToString();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side.IsServer())
            {
                BlockEntityWell well = blockSel.Position.BlockEntity(world) as BlockEntityWell;
                well?.Interact();
            }

            return true;
        }
    }

    class BlockEntityWell : BlockEntity
    {
        public string FoundLiquidCode { get => FoundLiquid ? BlockAtWellDepth.LiquidCode : ""; }
        public bool FoundLiquid { get => BlockAtWellDepth.IsLiquid(); }

        public int Depth { get; set; } = 1;
        public int MaxDepth = 32;
        public Block BlockAtWellDepth { get => new BlockPos(Pos.X, Pos.Y - Depth, Pos.Z).GetBlock(Api);  }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side.IsServer())
            {
                RegisterGameTickListener(dt => 
                {
                    UpdateState();
                    MarkDirty();
                }, 500);
            }
        }

        public void UpdateState()
        {
            Depth = 1;
            BlockPos iPos = new BlockPos(Pos.X, Pos.Y, Pos.Z);
            for (int i = 0; i < (Api as ICoreServerAPI).WorldManager.MapSizeY; i++)
            {
                iPos.Y--;
                if (iPos.GetBlock(Api).Code.ToString() == "game:wellpipe")
                {
                    Depth++;
                }
                else break;
            }
        }

        public void Interact()
        {
            if (!FoundLiquid)
            {
                BlockPos position = new BlockPos(Pos.X, Pos.Y - Depth, Pos.Z);

                Block replacing = Api.World.BlockAccessor.GetBlock(position);
                Block block = Api.World.BlockAccessor.GetBlock(new AssetLocation("game:wellpipe"));
                if (replacing.Id != 1) Api.World.BlockAccessor.SetBlock(block.Id, position);
                Depth++;
                UpdateState();
                MarkDirty();
            }
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            Depth = tree.GetInt("depth");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("depth", Depth);
        }
    }
}
