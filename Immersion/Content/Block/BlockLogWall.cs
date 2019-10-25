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
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Neolithic
{
    class BlockLogWall : Block
    {
        ICoreClientAPI capi;
        public string Wood { get => Variant["wood"]; }
        public string Key { get => FirstCodePart() + Wood; }

        public string WallType { get => Variant.TryGetValue("type"); }
        public string Bark { get => Variant.TryGetValue("style"); }
        public string Hor { get => Variant.TryGetValue("horizontal"); }
        public string Vert { get => Variant.TryGetValue("vertical"); }

        WallSystem wallSystem;

        public override void OnLoaded(ICoreAPI api)
        {
            capi = api as ICoreClientAPI;
            base.OnLoaded(api);
            wallSystem = api.ModLoader.GetModSystem<WallSystem>();
            if (!wallSystem.styles.ContainsKey(Key))
            {
                WallStyle style = new WallStyle();
                foreach (var val in api.World.Blocks)
                {
                    BlockLogWall tmp = (val as BlockLogWall);
                    if (tmp?.Key == Key)
                    {
                        if (tmp.WallType != null) style.types.Add(tmp.WallType);
                        if (tmp.Hor != null) style.hors.Add(tmp.Hor);
                        if (tmp.Vert != null) style.verts.Add(tmp.Vert);
                    }
                    if (tmp?.FirstCodePart() != null) style.firstcodeparts.Add(tmp.FirstCodePart());
                }
                wallSystem.styles.Add(Key, style);
            }
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityLogwall be = (blockSel.BlockEntity(api) as BlockEntityLogwall);
            be?.OnInteract(world, byPlayer, blockSel);
            base.OnBlockInteractStart(world, byPlayer, blockSel);
            return true;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            StringBuilder builder = new StringBuilder(base.GetPlacedBlockInfo(world, pos, forPlayer)).AppendLine();
            builder = capi.Settings.Bool["extendedDebugInfo"] ? builder.AppendLine("Code: " + Code.ToString()) : builder;
            return builder.ToString();
        }
    }

    class WallIndexing
    {
        public uint typeIndex = 0;
        public uint vertIndex = 0;
        public uint horIndex = 0;
        public uint rampIndex = 0;

        public WallIndexing Clone()
        {
            return new WallIndexing(typeIndex, vertIndex, horIndex, rampIndex);
        }

        public WallIndexing(uint typeIndex = 0, uint vertIndex = 0, uint horIndex = 0, uint rampIndex = 0)
        {
            this.typeIndex = typeIndex; this.vertIndex = vertIndex; this.horIndex = horIndex; this.rampIndex = rampIndex;
        }
    }

    class WallStyle
    {
        public HashSet<string> types = new HashSet<string>();
        public HashSet<string> verts = new HashSet<string>();
        public HashSet<string> hors = new HashSet<string>();
        public HashSet<string> firstcodeparts = new HashSet<string>();
    }

    class WallSystem : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterBlockClass("BlockLogWall", typeof(BlockLogWall));
            api.RegisterBlockEntityClass("LogWall", typeof(BlockEntityLogwall));
        }

        public Dictionary<string, WallStyle> styles = new Dictionary<string, WallStyle>();
    }

    class BlockEntityLogwall : BlockEntity
    {
        BlockLogWall OwnBlock { get => api.World.BlockAccessor.GetBlock(pos) as BlockLogWall; }
        WallIndexing indexing;
        bool interact = true;

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            indexing = JsonConvert.DeserializeObject<WallIndexing>(tree.GetString("wallindexing"));
            base.FromTreeAtributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            tree.SetString("wallindexing", JsonConvert.SerializeObject(indexing));
            base.ToTreeAttributes(tree);
        }

        public void OnInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            indexing = indexing ?? new WallIndexing();
            if (interact)
            {
                interact = false;
                WallSystem wallSystem = api.ModLoader.GetModSystem<WallSystem>();

                if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Item?.Tool == EnumTool.Hammer)
                {
                    if (world.Side.IsServer())
                    {
                        if (wallSystem.styles.TryGetValue(OwnBlock.Key, out WallStyle val))
                        {
                            string type = OwnBlock.WallType, wood = OwnBlock.Wood, style = OwnBlock.Bark, vert = OwnBlock.Vert, hor = OwnBlock.Hor;

                            if (byPlayer.Entity.Controls.Sneak && val.types.Count > 0) type = val.types.Next(ref indexing.typeIndex);
                            else if (byPlayer.Entity.Controls.Sprint && val.verts.Count > 0) vert = val.verts.Next(ref indexing.vertIndex);
                            else if (val.hors.Count > 0) hor = val.hors.Next(ref indexing.horIndex);

                            string code = OwnBlock.Code.Domain + ":" + OwnBlock.FirstCodePart().Apd(type).Apd(wood).Apd(style);

                            if (vert != null) code = code.Apd(vert);
                            if (hor != null) code = code.Apd(hor);

                            world.BlockAccessor.ExchangeBlock(code.ToBlock(api).Id, pos);
                            world.PlaySoundAt(OwnBlock.Sounds.Place, pos);
                            world.SpawnCubeParticles(pos, pos.MidPoint(), 2, 32);
                            byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                        }
                    }
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }
                else if (byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Item?.Tool == EnumTool.Axe)
                {
                    if (world.Side.IsServer() && wallSystem.styles.TryGetValue(OwnBlock?.Key, out WallStyle style))
                    {
                        while (true)
                        {
                            AssetLocation asset = OwnBlock.CodeWithPart(style.firstcodeparts.Next(ref indexing.rampIndex));
                            Block nextBlock = asset.GetBlock(api);
                            if (nextBlock != null)
                            {
                                world.BlockAccessor.ExchangeBlock(nextBlock.Id, pos);
                                world.PlaySoundAt(OwnBlock.Sounds.Place, pos);
                                world.SpawnCubeParticles(pos, pos.MidPoint(), 2, 32);
                                byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack.Collectible.DamageItem(api.World, byPlayer.Entity, byPlayer.InventoryManager.ActiveHotbarSlot);
                                break;
                            }
                        }
                    }
                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                }
                world.RegisterCallback(dt => interact = true, 30);
            }
        }
    }
}
