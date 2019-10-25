using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Neolithic
{
    class BlockDryingStation : Block
    {
        public DryingProp[] props;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            props = Attributes["dryingprops"].AsObject<DryingProp[]>();
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            (blockSel.BlockEntity(world) as BlockEntityDryingStation)?.OnInteract(world, byPlayer, blockSel);
            return true;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder builder = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer));
            BlockEntityDryingStation craftingStation = (pos.BlockEntity(world) as BlockEntityDryingStation);
            ItemStack stack = craftingStation?.inventory?[0]?.Itemstack;
            builder = stack != null ? builder.AppendLine().AppendLine(stack.StackSize + "x " +
                Lang.Get("incontainer-" + stack.Class.ToString().ToLowerInvariant() + "-" + stack.Collectible.Code.Path)) : builder;
            return builder.ToString();
        }
    }

    class BlockEntityDryingStation : BlockEntityContainer, IBlockShapeSupplier
    {
        public Block block { get => api.World.BlockAccessor.GetBlock(pos); }

        internal InventoryGeneric inventory;
        public override InventoryBase Inventory { get => inventory; }
        public override string InventoryClassName { get => "dryingstation"; }
        public DryingProp[] props;
        public MeshData mesh;
        public double timeWhenDone = 0;

        public BlockEntityDryingStation()
        {
            inventory = new InventoryGeneric(1, null, null, (id, self) =>
            {
                return new ItemSlot(self);
            });
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            props = (pos.GetBlock(api) as BlockDryingStation)?.props;

            RegisterGameTickListener(dt =>
            {
                if (api?.World?.Side.IsClient() ?? false)
                {
                    ICoreClientAPI capi = api as ICoreClientAPI;
                    float? translateY = (((float?)inventory[0].Itemstack?.StackSize / inventory[0].Itemstack?.Collectible?.MaxStackSize) * 0.35f) + 0.01f;
                    float y = translateY ?? 0;

                    capi.Tesselator.TesselateBlock(block, out mesh);

                    foreach (var val in props)
                    {
                        if (val?.Input?.Code == null) continue;
                        if (inventory[0]?.Itemstack?.Collectible?.Code?.ToString() == val.Input.Code.ToString())
                        {
                            MeshData fillPlane = QuadMeshUtil.GetCustomQuad(0, 0, 0, 0.9f, 0.9f, 255, 255, 255, 255);
                            fillPlane.Rotate(new Vec3f(0, 0, 0), GameMath.DEG2RAD * -90, 0, 0).Translate(0.05f, y, 0.95f);
                            TextureAtlasPosition texPos = new TextureAtlasPosition();

                            texPos = val.TextureSource.Type == EnumItemClass.Block ? capi.BlockTextureAtlas.GetPosition(val.TextureSource.Code.GetBlock(api), "up")
                            : capi.ItemTextureAtlas.GetPosition(val.TextureSource.Code.GetItem(api));
                            if ((bool)val.TextureSource.Code.GetBlock(api).ShapeHasWaterTint) fillPlane.AddTintIndex(2);
                            fillPlane.SetUv(texPos);
                            mesh.AddMeshData(fillPlane);
                            break;
                        }
                    }
                    MarkDirty(true);

                }
                else if (api?.World?.Side.IsServer() ?? false)
                {
                    foreach (var val in props)
                    {
                        if (val?.Input?.Code == null) continue;
                        if (inventory[0]?.Itemstack?.Collectible?.Code?.ToString() == val.Input.Code.ToString() && val.Output != null)
                        {
                            if (api.World.Calendar.TotalHours > timeWhenDone && timeWhenDone != 0)
                            {
                                timeWhenDone = 0;
                                ItemStack tmpStack = val.Output.Type == EnumItemClass.Block ? new ItemStack(val.Output.Code.GetBlock(api)) : new ItemStack(val.Output.Code.GetItem(api));
                                tmpStack.StackSize = inventory[0].Itemstack.StackSize * val.Output.StackSize;
                                inventory[0].Itemstack = tmpStack;
                                inventory.MarkSlotDirty(0);
                                MarkDirty(true);
                            }
                            else
                            {
                                timeWhenDone = api.World.Calendar.TotalHours + (double)val.DryingTime;
                            }
                            break;
                        }
                    }
                }
            }, 30);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            timeWhenDone = tree.GetDouble("timewhendone", 0);
            base.FromTreeAtributes(tree, worldForResolving);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetDouble("timewhendone", timeWhenDone);
            base.ToTreeAttributes(tree);
        }

        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(mesh);
            return true;
        }

        public void OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (slot != null)
            {
                if (slot.Itemstack?.Block is BlockBucket)
                {
                    BlockBucket bucket = (slot.Itemstack.Block as BlockBucket);
                    if (byPlayer.Entity.Controls.Sneak)
                    {
                        if (bucket.TryPutContent(world, slot.Itemstack, inventory[0].Itemstack, 1) > 0)
                        {
                            inventory[0].TakeOut(1);
                        }
                    }
                    else
                    {
                        foreach (var val in props)
                        {
                            if (val.Input.Code.ToString() == bucket.GetContent(world, slot.Itemstack)?.Collectible?.Code?.ToString())
                            {
                                DummySlot dummy = new DummySlot(bucket.TryTakeContent(world, slot.Itemstack, 1));
                                dummy.TryPutInto(world, inventory[0]);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var val in props)
                    {
                        if (val.Input.Code.ToString() == slot.Itemstack?.Collectible?.Code?.ToString() && !val.Input.Code.ToString().Contains("portion"))
                        {
                            if (byPlayer.Entity.Controls.Sneak)
                            {
                                slot.TryPutInto(world, inventory[0]);
                            }
                            else
                            {
                                inventory[0].TryPutInto(world, slot);
                            }
                            return;
                        }
                    }
                    inventory[0].TryPutInto(world, slot);
                }
            }
        }

    }
}
