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
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class IngotPileOverride : BlockEntityItemPile, ITexPositionSource
    {
        Dictionary<AssetLocation, MeshData[]> meshesByType
        {
            get
            {
                object value = null;
                Api.ObjectCache.TryGetValue("ingotpile-meshes", out value);
                return (Dictionary<AssetLocation, MeshData[]>)value;
            }
            set { Api.ObjectCache["ingotpile-meshes"] = value; }
        }


        Block tmpBlock;
        AssetLocation tmpMetal;
        ITexPositionSource tmpTextureSource;

        internal AssetLocation soundLocation = new AssetLocation("sounds/block/ingot");

        public override AssetLocation SoundLocation { get { return soundLocation; } }
        public override string BlockCode { get { return "ingotpile"; } }
        public override int MaxStackSize { get { return 64; } }

        internal void EnsureMeshExists()
        {
            if (meshesByType == null) meshesByType = new Dictionary<AssetLocation, MeshData[]>();
            if (MetalType == null || meshesByType.ContainsKey(new AssetLocation(MetalType))) return;
            if (Api.Side != EnumAppSide.Client) return;

            tmpBlock = Api.World.BlockAccessor.GetBlock(Pos);
            tmpTextureSource = ((ICoreClientAPI)Api).Tesselator.GetTexSource(tmpBlock);
            string path = Block.Attributes["pileshapes"]?["*"]?.AsString("shapes/block/metal/ingotpile.json");
            MetalProperty metals = Api.Assets.TryGet("worldproperties/block/metal.json").ToObject<MetalProperty>();
            List<MetalPropertyVariant> variants = metals.Variants.ToList();
            var a = Api.Assets.TryGet("config/immersionproperties/customingots.json")?.ToObject<List<MetalPropertyVariant>>();

            if (a != null) variants.AddRange(a);

            metals.Variants = variants.ToArray();

            for (int i = 0; i < metals.Variants.Length; i++)
            {
                if (!metals.Variants[i].Code.Path.Equals(MetalType)) continue;

                ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
                MeshData[] meshes = new MeshData[65];

                tmpMetal = metals.Variants[i].Code;
                if (tmpMetal != null) path = Block.Attributes["pileshapes"]?[tmpMetal.Path]?.AsString(path);
                Shape shape = Api.Assets.TryGet(path + ".json").ToObject<Shape>();

                for (int j = 0; j <= 64; j++)
                {
                    mesher.TesselateShape("ingotPile", shape, out meshes[j], this, null, 0, 0, 0, j);
                }

                meshesByType[tmpMetal] = meshes;
            }

            tmpTextureSource = null;
            tmpMetal = null;
            tmpBlock = null;
        }

        public override bool TryPutItem(IPlayer player)
        {
            bool result = base.TryPutItem(player);
            return result;
        }



        public TextureAtlasPosition this[string textureCode]
        {
            get { return tmpTextureSource[tmpMetal.Path]; }
        }


        public IngotPileOverride() : base()
        {
            inventory = new InventoryGeneric(1, BlockCode, null, null, null);
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            inventory.ResolveBlocksOrItems();
        }


        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
        }

        public string MetalType
        {
            get { return inventory?[0]?.Itemstack?.Collectible?.LastCodePart(); }
        }

        public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
        {
            lock (inventoryLock)
            {
                if (inventory[0].Itemstack == null) return true;

                EnsureMeshExists();
                MeshData[] mesh = null;
                if (MetalType != null && meshesByType.TryGetValue(new AssetLocation(MetalType), out mesh))
                {
                    meshdata.AddMeshData(mesh[inventory[0].StackSize]);
                }
            }

            return true;
        }
    }

    public class BlockIngotPileOverride : Block
    {
        public Cuboidf[][] CollisionBoxesByFillLevel;

        public BlockIngotPileOverride()
        {
            CollisionBoxesByFillLevel = new Cuboidf[9][];

            for (int i = 0; i <= 8; i++)
            {
                CollisionBoxesByFillLevel[i] = new Cuboidf[] { new Cuboidf(0, 0, 0, 1, i * 0.125f, 1) };
            }
        }

        public int FillLevel(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockEntity be = blockAccessor.GetBlockEntity(pos);
            if (be is IngotPileOverride)
            {
                return (int)Math.Ceiling(((IngotPileOverride)be).OwnStackSize / 8.0);
            }

            return 1;
        }

        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return CollisionBoxesByFillLevel[FillLevel(blockAccessor, pos)];
        }

        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            return CollisionBoxesByFillLevel[FillLevel(blockAccessor, pos)];
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (be is IngotPileOverride)
            {
                IngotPileOverride pile = (IngotPileOverride)be;
                return pile.OnPlayerInteract(byPlayer);
            }

            return false;
        }



        internal bool Construct(ItemSlot slot, IWorldAccessor world, BlockPos pos, IPlayer player)
        {
            Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
            if (!belowBlock.SideSolid[BlockFacing.UP.Index] && (belowBlock != this || FillLevel(world.BlockAccessor, pos.DownCopy()) != 8)) return false;


            world.BlockAccessor.SetBlock(BlockId, pos);

            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is IngotPileOverride)
            {
                IngotPileOverride pile = (IngotPileOverride)be;
                if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
                {
                    pile.inventory[0].Itemstack = slot.Itemstack.Clone();
                    pile.inventory[0].Itemstack.StackSize = 1;
                }
                else
                {
                    pile.inventory[0].Itemstack = slot.TakeOut(player.Entity.Controls.Sprint ? pile.BulkTakeQuantity : pile.DefaultTakeQuantity);
                }

                pile.MarkDirty();
                world.BlockAccessor.MarkBlockDirty(pos);
                world.PlaySoundAt(new AssetLocation("sounds/block/ingot"), pos.X, pos.Y, pos.Z, player, false);
            }


            if (CollisionTester.AabbIntersect(
                GetCollisionBoxes(world.BlockAccessor, pos)[0],
                pos.X, pos.Y, pos.Z,
                player.Entity.CollisionBox,
                player.Entity.SidedPos.XYZ
            ))
            {
                player.Entity.SidedPos.Y += GetCollisionBoxes(world.BlockAccessor, pos)[0].Y2;
            }

            (player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

            return true;
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[0];
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        {
            // Handled by BlockEntityItemPile
            return new ItemStack[0];
        }


        public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
            if (!belowBlock.SideSolid[BlockFacing.UP.Index] && (belowBlock != this || FillLevel(world.BlockAccessor, pos.DownCopy()) < 8))
            {
                world.BlockAccessor.BreakBlock(pos, null);
                //world.PlaySoundAt(new AssetLocation("sounds/block/ingot"), pos.X, pos.Y, pos.Z, null, false);
            }
        }


        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
        {
            IngotPileOverride be = capi.World.BlockAccessor.GetBlockEntity(pos) as IngotPileOverride;
            if (be == null) return base.GetRandomColor(capi, pos, facing);
            string metalType = be.MetalType;
            if (metalType == null) return base.GetRandomColor(capi, pos, facing);

            return capi.BlockTextureAtlas.GetRandomColor(Textures[be.MetalType].Baked.TextureSubId);
        }


        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            BlockEntity be = world.BlockAccessor.GetBlockEntity(pos);
            if (be is IngotPileOverride)
            {
                IngotPileOverride pile = (IngotPileOverride)be;
                ItemStack stack = pile.inventory[0].Itemstack;
                if (stack != null)
                {
                    ItemStack pickstack = stack.Clone();
                    pickstack.StackSize = 1;
                    return pickstack;
                }
            }

            return new ItemStack(this);
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-ingotpile-add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    Itemstacks = new ItemStack[] { new ItemStack(this) },
                    GetMatchingStacks = (wi, bs, es) =>
                    {
                        IngotPileOverride pile = world.BlockAccessor.GetBlockEntity(bs.Position) as IngotPileOverride;
                        if (pile != null && pile.MaxStackSize > pile.inventory[0].StackSize && pile.inventory[0].Itemstack != null)
                        {
                            ItemStack displaystack = pile.inventory[0].Itemstack.Clone();
                            displaystack.StackSize = pile.DefaultTakeQuantity;
                            return new  ItemStack[] { displaystack };
                        }
                        return null;
                    }
                },
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-ingotpile-remove",
                    MouseButton = EnumMouseButton.Right
                },


                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-ingotpile-4add",
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCodes = new string[] {"sprint", "sneak" },
                    Itemstacks = new ItemStack[] { new ItemStack(this) },
                    GetMatchingStacks = (wi, bs, es) =>
                    {
                        IngotPileOverride pile = world.BlockAccessor.GetBlockEntity(bs.Position) as IngotPileOverride;
                        if (pile != null && pile.MaxStackSize > pile.inventory[0].StackSize && pile.inventory[0].Itemstack != null)
                        {
                            ItemStack displaystack = pile.inventory[0].Itemstack.Clone();
                            displaystack.StackSize = pile.BulkTakeQuantity;
                            return new  ItemStack[] { displaystack };
                        }
                        return null;
                    }
                },
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-ingotpile-4remove",
                    HotKeyCode = "sprint",
                    MouseButton = EnumMouseButton.Right
                }

            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
