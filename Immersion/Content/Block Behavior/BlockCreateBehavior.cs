using System;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Neolithic
{
    class BlockCreateBehavior : BlockBehavior
    {
        private CreateBlocks[] createBlocks;
        public bool allowPlaceOn = false;
        ICoreAPI api;

        public BlockCreateBehavior(Block block) : base(block) { }

        public void PostOLInit(JsonObject properties)
        {
            try
            {
                createBlocks = properties["createBlocks"].AsObject<CreateBlocks[]>();
                allowPlaceOn = properties["allowPlaceOn"].AsBool(false);
            }
            catch (Exception)
            {
                createBlocks = null;
                api.World.Logger.Notification("CreateBlocks error in " + block.Code.ToString());
            }
            
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.api = api;
            PostOLInit(properties);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            var active = byPlayer.InventoryManager.ActiveHotbarSlot;

            if (active.Itemstack?.Collectible?.Code != null && createBlocks != null)
            {
                foreach (var val in createBlocks)
                {
                    if (active.Itemstack.Collectible.WildCardMatch(val.Takes.Code))
                    {
                        if (world.Side.IsClient() && secondsUsed != 0 && val.MakeTime != 0)
                        {
                            ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                            float animstep = (secondsUsed / val.MakeTime) * 1.0f;
                            api.ModLoader.GetModSystem<ShaderTest>().progressBar = animstep;
                        }

                        return secondsUsed < val.MakeTime;
                    }
                }
            }

            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            base.OnBlockInteractStop(secondsUsed, world, byPlayer, blockSel, ref handled);
            if (createBlocks == null)
            {
                world.Logger.Notification("CreateBlocks error in " + block.Code.ToString());
                return;
            }
            var active = byPlayer?.InventoryManager;
            BlockPos pos = blockSel?.Position;
            if (active?.ActiveHotbarSlot?.Itemstack?.Collectible?.Code != null && pos != null)
            {
                foreach (var val in createBlocks)
                {
                    if (api.Side.IsClient()) api.ModLoader.GetModSystem<ShaderTest>().progressBar = 0;

                    if (secondsUsed > val.MakeTime && active.ActiveHotbarSlot.Itemstack.Collectible.WildCardMatch(val.Takes.Code) && active.ActiveHotbarSlot.StackSize >= val.Takes.StackSize)
                    {
                        if (world.Side.IsServer())
                        {
                            world.PlaySoundAt(block.Sounds.Place, pos.X, pos.Y, pos.Z);
                            if (active?.ActiveHotbarSlot?.Itemstack?.Item?.Tool != null)
                            {
                                active.ActiveHotbarSlot.Itemstack.Collectible.DamageItem(world, byPlayer.Entity, active.ActiveHotbarSlot);
                            }
                        }

                        if (val.IntoInv)
                        {
                            if (!active.TryGiveItemstack(val.Makes))
                            {
                                world.SpawnItemEntity(val.Makes, pos.MidPoint(), new Vec3d(0.0, 0.1, 0.0));
                            }
                        }
                        else
                        {
                            world.SpawnItemEntity(val.Makes, pos.MidPoint(), new Vec3d(0.0, 0.1, 0.0));
                        }
                        active.ActiveHotbarSlot.TakeOut(val.Takes.StackSize);

                        try
                        {
                            if (world.Side.IsClient()) world.SpawnCubeParticles(pos.MidPoint(), active.ActiveHotbarSlot.Itemstack, 2, 16);
                        }
                        catch (Exception)
                        {
                            world.Logger.Error("Could not create particles, missing itemstack?");
                        }

                        active.ActiveHotbarSlot.MarkDirty();

                        if (val.RemoveOnFinish) world.BlockAccessor.SetBlock(0, pos);
                        handled = EnumHandling.PreventDefault;
                        return;
                    }
                }
            }

            ItemStack stack = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (stack != null && allowPlaceOn)
            {
                string r = "";
                BlockSelection newsel = blockSel.Clone();
                newsel.Position = blockSel.Position.Offset(blockSel.Face);
                Block block = stack.Block;

                if (block != null && block.TryPlaceBlock(world, byPlayer, stack, newsel, ref r))
                {
                    world.PlaySoundAt(stack.Block?.Sounds.Place, newsel.Position);
                }

            }
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            if (world.Side.IsClient()) api.ModLoader.GetModSystem<ShaderTest>().progressBar = 0;
            return false;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            return true;
        }
    }
}
