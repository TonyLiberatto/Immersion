using Newtonsoft.Json;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Immersion
{
    class BlockEntityDryingStation : BlockEntityContainer
    {
        public Block block { get => Api.World.BlockAccessor.GetBlock(Pos); }

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
            inventory.SlotModified += (i) => UpdateMesh(Api as ICoreClientAPI);
        }

        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);

            props = (Pos.GetBlock(Api) as BlockDryingStation)?.props;
            if (props == null) return;

            MarkDirty(true);

            RegisterGameTickListener(dt =>
            {
                bool recipeFound = false;
                foreach (var val in props)
                {
                    var recipe = val.Clone();

                    if (recipe?.Input?.Code == null) continue;
                    if (inventory[0]?.Itemstack?.Collectible?.Code?.ToString() == recipe.Input.Code.ToString() && recipe.Output != null)
                    {
                        recipeFound = true;

                        if (timeWhenDone == 0)
                        {
                            UpdateDoneTime(recipe.DryingTime ?? 0);
                            break;
                        }

                        if (recipe.Output != null)
                        {
                            if (Api.World.Calendar.TotalHours > timeWhenDone)
                            {
                                recipe.Output.Resolve(Api.World, "");

                                ItemStack tmpStack = recipe.Output.ResolvedItemstack;
                                tmpStack.StackSize = inventory[0].Itemstack.StackSize * recipe.Output.StackSize;
                                inventory[0].Itemstack = tmpStack;
                                inventory.MarkSlotDirty(0);
                                UpdateDoneTime(recipe.DryingTime ?? 0);
                            }
                            break;
                        }
                    }
                }
                if (!recipeFound) timeWhenDone = 0;
                MarkDirty(true);
            }, 5000);
        }

        public void UpdateMesh(ICoreClientAPI capi)
        {
            if (capi == null || props == null) return;

            capi.Tesselator.TesselateBlock(block, out mesh);

            if (inventory == null || inventory[0]?.Itemstack?.StackSize == null || inventory[0]?.Itemstack?.Collectible?.MaxStackSize == null) return;

            float? translateY = (((float?)inventory[0].Itemstack?.StackSize / inventory[0].Itemstack?.Collectible?.MaxStackSize) * 0.35f);
            float y = translateY ?? 0;
            y = GameMath.Clamp(y, 0.1f, 0.325f);

            foreach (var val in props)
            {
                var recipe = val.Clone();
                recipe.TextureSource.Resolve(capi.World, "");

                if (recipe?.Input?.Code == null) continue;
                if (inventory[0]?.Itemstack?.Collectible?.Code?.ToString() == recipe.Input.Code.ToString())
                {
                    Shape shape = null;

                    WaterTightContainableProps inContainerProps = BlockLiquidContainerBase.GetInContainerProps(inventory[0].Itemstack);
                    MeshData fillPlane = null;
                    JsonObject itemAttribute = inventory[0]?.Itemstack?.ItemAttributes?["inContainerTexture"];

                    if (inContainerProps?.TintIndex > 0)
                    {
                        shape = capi.Assets.TryGet("shapes/block/basic/liquid.json").ToObject<Shape>();
                    }
                    else shape = capi.Assets.TryGet("shapes/block/basic/flat.json").ToObject<Shape>();

                    if (inContainerProps != null) capi.Tesselator.TesselateShape("drying station", shape, out fillPlane, new ContainerTextureSource(capi, inventory[0].Itemstack, inContainerProps.Texture));
                    else if (itemAttribute != null)
                    {
                        capi.Tesselator.TesselateShape("drying station", shape, out fillPlane, new ContainerTextureSource(capi, inventory[0].Itemstack, itemAttribute.AsObject((CompositeTexture)null, "game")));
                    }
                    else capi.Tesselator.TesselateShape("drying station", shape, out fillPlane, new BlockTopTextureSource(capi, val.TextureSource.ResolvedItemstack.Block));

                    if (fillPlane != null)
                    {
                        fillPlane.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.9f, 1, 0.9f).Translate(0, y, 0);

                        if (inContainerProps != null && inContainerProps.TintIndex > 0)
                        {
                            int color = capi.ApplyColorTintOnRgba(inContainerProps.TintIndex, ColorUtil.WhiteArgb, 196, 128, false);
                            if (Pos != null) color = capi.ApplyColorTintOnRgba(inContainerProps.TintIndex, ColorUtil.WhiteArgb, Pos.X, Pos.Y, Pos.Z, false);
                            byte[] bgraBytes = ColorUtil.ToBGRABytes(color);

                            for (int index = 0; index < fillPlane.Rgba.Length; ++index)
                                fillPlane.Rgba[index] = (byte)((int)fillPlane.Rgba[index] * (int)bgraBytes[index % 4] / (int)byte.MaxValue);
                        }

                        mesh.AddMeshData(fillPlane);

                        mesh.CustomInts = new CustomMeshDataPartInt(mesh.FlagsCount);
                        mesh.CustomInts.Values.Fill<int>(67108864);
                        mesh.CustomInts.Count = mesh.FlagsCount;
                        mesh.CustomFloats = new CustomMeshDataPartFloat(mesh.FlagsCount * 2);
                        mesh.CustomFloats.Count = mesh.FlagsCount * 2;
                    }
                    break;
                }
            }
        }

        public new void MarkDirty(bool t = true)
        {
            UpdateMesh(Api as ICoreClientAPI);
            base.MarkDirty(t);
        }

        public void UpdateDoneTime(int dryingTime)
        {
            timeWhenDone = Api.World.Calendar.TotalHours + dryingTime;
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

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (mesh == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                if (capi != null) capi.Tesselator.TesselateBlock(block, out mesh);
            }
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
                    BlockBucket bucket = (BlockBucket)slot.Itemstack.Block;
                    if (byPlayer.Entity.Controls.Sneak)
                    {
                        if (!inventory[0].Empty && inventory[0].Itemstack.Item.Code.ToString().Contains("portion") && bucket.TryPutContent(world, slot.Itemstack, inventory[0].Itemstack, 1) > 0)
                        {
                            inventory[0].TakeOut(1);
                            timeWhenDone = 0;
                        }
                    }
                    else if (inventory[0].Empty || inventory[0].Itemstack.Item.Code.ToString().Contains("portion"))
                    {
                        DummySlot dummy = new DummySlot(bucket.TryTakeContent(world, slot.Itemstack, 1));
                        dummy.TryPutInto(world, inventory[0]);
                        timeWhenDone = 0;
                    }
                }
                else if (!inventory[0]?.Itemstack?.Item?.Code.ToString().Contains("portion") ?? false)
                {
                    inventory[0].TryPutInto(world, slot);
                    timeWhenDone = 0;
                }
            }

            inventory[0].MarkDirty();
            slot?.MarkDirty();
            MarkDirty(true);
        }

    }
}
