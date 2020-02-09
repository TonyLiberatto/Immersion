using Newtonsoft.Json;
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
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
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
                int progress = (int)GameMath.Min((float)Math.Round(100 * well.MiningProgress), 100f);
                bdr.AppendLine("Depth: " + well.Depth)
                    .AppendLine("FoundLiquid: " + well.FoundLiquidCode)
                    .AppendLine("Mining Progress: " + progress + "%")
                    .AppendLine("Mining Difficulty: " + Math.Round(100 * (1.0f - well.Difficulty)) + "%");
            }

            return bdr.ToString();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Side.IsServer())
            {
                BlockEntityWell well = blockSel.Position.BlockEntity(world) as BlockEntityWell;
                well?.Interact(byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
            }
            else
            {
                (byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            }

            return true;
        }
    }

    class BlockWellPipe : Block
    {
    }

    class BlockEntityWell : BlockEntity
    {
        public string FoundLiquidCode { get => FoundLiquid ? BlockAtWellDepth.LiquidCode : ""; }
        public bool FoundLiquid { get => BlockAtWellDepth.IsLiquid(); }

        public int Depth { get; set; } = 1;
        public float MiningProgress { get; set; } = 0;
        public float Difficulty { get => FoundLiquid ? 0.0f : BlockAtWellDepth.Id == 0 ? 1.0f : GameMath.Clamp(1.5f / (Depth * 0.5f), 0.01f, 1.00f); }

        public BlockPos posAtWellDepth { get => new BlockPos(Pos.X, Pos.Y - Depth, Pos.Z);  }
        public Block BlockAtWellDepth { get => posAtWellDepth.GetBlock(Api);  }

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
                if (iPos.GetBlock(Api) is BlockWellPipe)
                {
                    Depth++;
                }
                else break;
            }
        }

        public void Interact(ItemSlot slot, EntityPlayer byEntity)
        {
            BlockPos position = new BlockPos(Pos.X, Pos.Y - Depth, Pos.Z);
            Block replacing = Api.World.BlockAccessor.GetBlock(position);
            if (!FoundLiquid && slot?.Itemstack?.Item?.Tool == EnumTool.Pickaxe)
            {
                if (MiningProgress < 1.0) MiningProgress += Difficulty;

                if (MiningProgress >= 1.0)
                {
                    Block cobbleBlock = null;
                    if (slot.Itemstack.Item.ToolTier >= replacing.RequiredMiningTier && slot.Inventory.Any(s =>
                    {
                        if (s?.Itemstack?.Block?.FirstCodePart() == "cobblestone")
                        {
                            cobbleBlock = s.Itemstack.Block;
                            s.TakeOut(1);
                            s.MarkDirty();
                            return true;
                        }
                        return false;
                    }))
                    {
                        Block block = Api.World.BlockAccessor.GetBlock(new AssetLocation("game:wellpipe".Apd(cobbleBlock.Variant["rock"])));
                        byEntity.World.PlaySoundAt(replacing.Sounds.GetBreakSound(byEntity.Player), position.X, position.Y, position.Z, null);
                        Api.World.BlockAccessor.BreakBlock(position, byEntity.Player);
                        Api.World.BlockAccessor.SetBlock(block.Id, position);
                        slot.Itemstack.Collectible.DamageItem(Api.World, byEntity, slot);
                        slot.MarkDirty();
                        byEntity.World.PlaySoundAt(new AssetLocation("sounds/tool/reinforce"), Pos.X, Pos.Y, Pos.Z, null);
                        
                        Depth++;
                        UpdateState();
                        MiningProgress = 0;
                    }
                }
            }
            else if (FoundLiquid && slot?.Itemstack?.Block is BlockLiquidContainerBase)
            {
                if (((BlockLiquidContainerBase)slot.Itemstack.Block).TryFillFromBlock(slot, byEntity, posAtWellDepth))
                    byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/water"), Pos.X, Pos.Y, Pos.Z, null);
            }
            MarkDirty();
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            Depth = tree.GetInt("depth", 1);
            MiningProgress = tree.GetFloat("miningprogress");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("depth", Depth);
            tree.SetFloat("miningprogress", MiningProgress);
        }
    }
}
