using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Immersion
{
    class BlockEntityCraftingStation : BlockEntityContainer
    {
        private bool action = true;
        internal InventoryGeneric inventory;
        public BlockCraftingStation block;
        public CraftingProp[] props;
        BlockEntityAnimationUtil util;

        public override InventoryBase Inventory { get => inventory; }
        public override string InventoryClassName { get => "choppingblock"; }
        
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator) => true;

        public BlockEntityCraftingStation()
        {
            inventory = new InventoryGeneric(1, null, null, (id, self) =>
            {
                return new ItemSlot(self);
            });
        }

        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);
            block = Pos.GetBlock(Api) as BlockCraftingStation;
            props = block?.craftingProps;
            util = new BlockEntityAnimationUtil(Api, this);
            if (Api.Side.IsClient())
            {
                util.InitializeAnimators(new Vec3f(block.Shape.rotateX, block.Shape.rotateY, block.Shape.rotateZ), block.animProps.allAnims);

                RegisterGameTickListener(dt =>
                {
                    StopAllAnims();
                    if (!action)
                    {
                        util.StartAnimation(new AnimationMetaData() { Code = block.animProps.actionAnim });
                    }
                    else if (inventory[0].Itemstack?.StackSize > 0)
                    {
                        util.StartAnimation(new AnimationMetaData() { Code = block.animProps.hasContentAnim });
                    }
                    else
                    {
                        util.StartAnimation(new AnimationMetaData() { Code = block.animProps.idleAnim });
                    }
                }, 30);
            }
        }

        public void OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer?.InventoryManager.ActiveHotbarSlot;
            if (slot.Itemstack != null)
            {
                foreach (var val in props)
                {
                    if (action && slot.Itemstack.Item?.Tool == val.tool && inventory?[0]?.Itemstack?.Collectible?.WildCardMatch(val.input.Code) != null && inventory?[0]?.StackSize >= val.input.StackSize)
                    {
                        action = false;
                        inventory[0].TakeOut(val.input.StackSize);
                        Api.World.RegisterCallback(dt => action = true, val.craftTime);

                        world.SpawnItemEntity(val.output, Pos.MidPoint());

                        slot.Itemstack.Collectible.DamageItem(Api.World, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot, 1);

                        (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
                        (world as IServerWorldAccessor)?.PlaySoundAt(new AssetLocation(val.craftSound), blockSel.Position);
                        (world as IServerWorldAccessor)?.SpawnCubeParticles(Pos, Pos.MidPoint(), 1, 32, 0.5f);
                        MarkDirty();
                        break;
                    }
                    else if (slot.Itemstack.Collectible.WildCardMatch(val.input.Code))
                    {
                        slot.TryPutInto(world, inventory[0]);
                        break;
                    }
                }

            }
        }

        public void StopAllAnims()
        {
            foreach (var val in block?.animProps?.allAnims ?? new string[0])
            {
                util.StopAnimation(val);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            StopAllAnims();
        }
    }
}