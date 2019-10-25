using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Neolithic
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    class IWCSPacket
    {
        public EnumDataType DataType { get; set; }
        public string SerializedData { get; set; }
    }

    class InWorldCraftingSystem : ModSystem
    {
        ICoreServerAPI sapi;
        ICoreClientAPI capi;
        IServerNetworkChannel sChannel;
        IClientNetworkChannel cChannel;

        public Dictionary<AssetLocation, InWorldCraftingRecipe[]> InWorldCraftingRecipes { get; set; } = new Dictionary<AssetLocation, InWorldCraftingRecipe[]>();
        public override double ExecuteOrder() => 1;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            sChannel = api.Network.RegisterChannel("iwcr").RegisterMessageType<IWCSPacket>().SetMessageHandler<IWCSPacket>((a, b) => 
            {
                if (b.DataType == EnumDataType.Action)
                {
                    if (a?.CurrentBlockSelection?.Position == null) return;
                    if (api.World.Claims.TryAccess(a, a.CurrentBlockSelection.Position, EnumBlockAccessFlags.BuildOrBreak))
                    {
                        OnPlayerInteract(a, a.CurrentBlockSelection);
                    }
                }
            });
            api.Event.SaveGameLoaded += OnSaveGameLoaded;
            api.Event.PlayerJoin += SendCraftingRecipes;
        }

        private void SendCraftingRecipes(IServerPlayer byPlayer)
        {
            foreach (var val in InWorldCraftingRecipes)
            {
                string data = JsonConvert.SerializeObject(val, Formatting.None, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
                sChannel.SendPacket(new IWCSPacket() { DataType = EnumDataType.Recipes, SerializedData = data }, byPlayer);
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            api.Event.MouseDown += SendBlockAction;
            cChannel = api.Network.RegisterChannel("iwcr").RegisterMessageType<IWCSPacket>().SetMessageHandler<IWCSPacket>(h =>
            {
                if (h.DataType == EnumDataType.Recipes)
                {
                    try
                    {
                        var recipe = JsonConvert.DeserializeObject<KeyValuePair<AssetLocation, InWorldCraftingRecipe[]>>(h.SerializedData, new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore });
                        InWorldCraftingRecipes.Add(recipe.Key, recipe.Value);
                    }
                    catch (Exception ex)
                    {
                        api.World.Logger.Error("Exception thrown while receiving an In World Recipe packet: {0}, Data: {1}", ex, h?.SerializedData ?? "");
                        throw ex;
                    }
                }
            });
        }

        public override void Dispose()
        {
            if (capi != null)
            {
                InWorldCraftingRecipes.Clear();
            }
            base.Dispose();
        }

        private void SendBlockAction(MouseEvent e)
        {
            if (e.Button == EnumMouseButton.Right && capi.World.Player.Entity.Controls.Sneak)
            {
                if (OnPlayerInteract(capi.World.Player, capi.World.Player.CurrentBlockSelection))
                {
                    cChannel.SendPacket(new IWCSPacket() { DataType = EnumDataType.Action });
                    capi.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    e.Handled = true;
                }

            }
        }

        public void OnSaveGameLoaded()
        {
            InWorldCraftingRecipes = sapi.Assets.GetMany<InWorldCraftingRecipe[]>(sapi.Server.Logger, "recipes/inworld");
            sapi.World.Logger.Event("{0} in world recipes loaded", InWorldCraftingRecipes.Count);
            sapi.World.Logger.StoryEvent("Neolithic crafting...");
        }

        public bool OnPlayerInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos pos = blockSel?.Position;
            Block block = pos?.GetBlock(byPlayer.Entity.World);
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (block == null || slot?.Itemstack == null) return false;
            bool shouldbreak = false;

            foreach (var val in InWorldCraftingRecipes)
            {
                foreach (var recipe in val.Value)
                {
                    if (recipe.Disabled || (recipe.Takes.AllowedVariants != null && !block.WildCardMatch(recipe.Takes.AllowedVariants)) || (recipe.Tool.AllowedVariants != null && !slot.Itemstack.Collectible.WildCardMatch(recipe.Tool.AllowedVariants))
                        || (recipe.Takes.Attributes != null) && recipe.Takes.Attributes.Token.HasValues && block.Attributes.Token != recipe.Takes.Attributes.Token) continue;
                    if (block.WildCardMatch(recipe.Takes.Code))
                    {
                        if (IsValid(byPlayer, recipe, slot))
                        {
                            if (recipe.IsSwap)
                            {
                                var make = recipe.Makes[0].Clone();
                                make.Resolve(byPlayer.Entity.World, null);
                                if (make.IsBlock())
                                {
                                    if (recipe.Remove) byPlayer.Entity.World.BlockAccessor.SetBlock(0, pos);
                                    Block resolvedBlock = make.ResolvedItemstack.Block;
                                    byPlayer.Entity.World.BlockAccessor.SetBlock(resolvedBlock.BlockId, pos);
                                    resolvedBlock.OnBlockPlaced(byPlayer.Entity.World, pos);
                                    TakeOrDamage(recipe, slot, byPlayer);
                                    shouldbreak = true;
                                }
                            }
                            else if (recipe.IsCreate)
                            {
                                foreach (var make in recipe.Makes)
                                {
                                    var makeClone = make.Clone();
                                    makeClone.Resolve(byPlayer.Entity.World, null);
                                    byPlayer.Entity.World.SpawnItemEntity(makeClone.ResolvedItemstack, pos.MidPoint(), new Vec3d(0.0, 0.1, 0.0));
                                }
                                TakeOrDamage(recipe, slot, byPlayer);
                                if (recipe.Remove) byPlayer.Entity.World.BlockAccessor.SetBlock(0, pos);
                                shouldbreak = true;
                            }
                            if (byPlayer.Entity.World.Side.IsServer()) byPlayer.Entity.World.PlaySoundAt(recipe.CraftSound, pos);
                        }
                        else continue;

                        slot.MarkDirty();
                        break;
                    }
                }
                if (shouldbreak) return true;
            }
            return false;
        }

        public void TakeOrDamage(InWorldCraftingRecipe recipe, ItemSlot slot, IPlayer byPlayer)
        {
            if (recipe.IsTool)
            {
                slot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, slot);
            }
            else
            {
                slot.TakeOut(recipe.Tool.StackSize);
            }
        }

        public bool IsValid(IPlayer byPlayer, InWorldCraftingRecipe recipe, ItemSlot slot) =>
            (slot.Itemstack?.Collectible?.Code?.WildCardMatch(recipe?.Tool?.Code, slot.Itemstack.Collectible.ItemClass, byPlayer.Entity.World.Api) ?? false && slot.Itemstack?.StackSize >= recipe.Tool.StackSize) ||
            (recipe.Tool.Code.IsWildCard && recipe.Tool.Code.GetMatches(byPlayer.Entity.Api).Any(t => t.ToString() == slot.Itemstack?.Collectible?.Code?.ToString() && slot.Itemstack?.StackSize >= recipe.Tool.StackSize))
            && ((recipe.Tool.Attributes != null) && (recipe.Tool.Attributes == slot.Itemstack?.Collectible?.Attributes));
    }

    class InWorldCraftingRecipe
    {
        public EnumInWorldCraftingMode Mode { get; set; } = EnumInWorldCraftingMode.Swap;
        public JsonCraftingIngredient Takes { get; set; }
        public JsonCraftingIngredient Tool { get; set; }
        public JsonCraftingOutput[] Makes { get; set; }
        public AssetLocation CraftSound { get; set; } = new AssetLocation("sounds/block/planks");
        public bool IsTool { get; set; } = false;
        public bool Disabled { get; set; } = false;
        public bool Remove { get; set; } = false;
        public float MakeTime { get; set; } = 0f;

        public bool IsSwap { get => Mode == EnumInWorldCraftingMode.Swap; }
        public bool IsCreate { get => Mode == EnumInWorldCraftingMode.Create;  }
    }

    class JsonCraftingIngredient : JsonItemStack
    {
        public AssetLocation[] AllowedVariants { get; set; }
        public AssetLocation Name { get; set; }

        public int Count
        {
            get => StackSize;
            set => StackSize = value;
        }
    }

    class JsonCraftingOutput : JsonItemStack
    {
        public int Count
        {
            get => StackSize;
            set => StackSize = value;
        }
    }

    enum EnumInWorldCraftingMode
    {
        Swap, Create
    }

    enum EnumDataType
    {
        Action, Recipes
    }
}
