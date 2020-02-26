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

namespace Immersion
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    class IWCSPacket
    {
        public EnumDataType DataType { get; set; }
        public byte[] SerializedData { get; set; }
    }

    class InWorldCraftingSystem : ModSystem
    {
        ICoreServerAPI sapi;
        ICoreClientAPI capi;
        IClientNetworkChannel cChannel;
        IServerNetworkChannel sChannel;

        public Dictionary<AssetLocation, InWorldCraftingRecipe[]> InWorldCraftingRecipes { get; set; } = new Dictionary<AssetLocation, InWorldCraftingRecipe[]>();
        public override double ExecuteOrder() => 1;

        public override void StartServerSide(ICoreServerAPI Api)
        {
            this.sapi = Api;
            sChannel = Api.Network.RegisterChannel("iwcr").RegisterMessageType<IWCSPacket>().SetMessageHandler<IWCSPacket>((a, b) => 
            {
                if (b.DataType == EnumDataType.Action)
                {
                    if (a?.CurrentBlockSelection?.Position == null) return;
                    if (Api.World.Claims.TryAccess(a, a.CurrentBlockSelection.Position, EnumBlockAccessFlags.BuildOrBreak))
                    {
                        OnPlayerInteract(a, a.CurrentBlockSelection);
                    }
                }
            });
            Api.Event.SaveGameLoaded += OnSaveGameLoaded;
            Api.Event.PlayerJoin += (p) => sChannel.SendPacket(new IWCSPacket() { DataType = EnumDataType.Recipes, SerializedData = JsonUtil.ToBytes(InWorldCraftingRecipes)}, p);
        }

        public override void StartClientSide(ICoreClientAPI Api)
        {
            this.capi = Api;
            Api.Input.InWorldAction += SendBlockAction;
            cChannel = Api.Network.RegisterChannel("iwcr").RegisterMessageType<IWCSPacket>().SetMessageHandler<IWCSPacket>((a) => 
            {
                if (a.DataType == EnumDataType.Recipes)
                {
                    InWorldCraftingRecipes = JsonUtil.FromBytes<Dictionary<AssetLocation, InWorldCraftingRecipe[]>>(a.SerializedData);
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

        private void SendBlockAction(EnumEntityAction action, ref EnumHandling handled)
        {
            var controls = capi.World.Player.Entity.Controls;
            if (action == EnumEntityAction.RightMouseDown && controls.Sneak)
            {
                if (OnPlayerInteract(capi.World.Player, capi.World.Player.CurrentBlockSelection))
                {
                    cChannel.SendPacket(new IWCSPacket() { DataType = EnumDataType.Action });
                    capi.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    handled = EnumHandling.PreventDefault;
                }
            }
        }

        public void OnSaveGameLoaded()
        {
            InWorldCraftingRecipes = sapi.Assets.GetMany<InWorldCraftingRecipe[]>(sapi.Server.Logger, "recipes/inworld");
            sapi.World.Logger.Event("{0} in world recipes loaded", InWorldCraftingRecipes.Count);
            sapi.World.Logger.StoryEvent("Immersion crafting...");
        }

        public bool OnPlayerInteract(IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockPos Pos = blockSel?.Position;
            Block block = Pos?.GetBlock(byPlayer.Entity.World);
            ItemSlot slot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (block == null || slot?.Itemstack == null) return false;
            bool shouldbreak = false;

            foreach (var val in InWorldCraftingRecipes)
            {
                foreach (var rawRec in val.Value)
                {
                    var recipe = rawRec.Clone();

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
                                    if (recipe.Remove) (byPlayer as IServerPlayer)?.Entity.World.BlockAccessor.SetBlock(0, Pos);
                                    Block resolvedBlock = make.ResolvedItemstack.Block;
                                    (byPlayer as IServerPlayer)?.Entity.World.BlockAccessor.SetBlock(resolvedBlock.BlockId, Pos);
                                    resolvedBlock.OnBlockPlaced(byPlayer.Entity.World, Pos);
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
                                    (byPlayer as IServerPlayer)?.Entity.World.SpawnItemEntity(makeClone.ResolvedItemstack, Pos.MidPoint(), new Vec3d(0.0, 0.1, 0.0));
                                }
                                TakeOrDamage(recipe, slot, byPlayer);
                                if (recipe.Remove) (byPlayer as IServerPlayer)?.Entity.World.BlockAccessor.SetBlock(0, Pos);
                                shouldbreak = true;
                            }
                            (byPlayer as IServerPlayer)?.Entity.World.PlaySoundAt(recipe.CraftSound, Pos);
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
            if (!(byPlayer is IServerPlayer)) return;

            if (recipe.IsTool)
            {
                slot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, slot);
            }
            else
            {
                slot.TakeOut(recipe.Tool.StackSize);
            }
            if (recipe.Returns != null)
            {
                foreach (var val in recipe.Returns)
                {
                    val.Resolve(byPlayer.Entity.World, "");
                    if (val.ResolvedItemstack != null)
                    {
                        if (!byPlayer.InventoryManager.TryGiveItemstack(val.ResolvedItemstack))
                        {
                            byPlayer.Entity.World.SpawnItemEntity(val.ResolvedItemstack, byPlayer.Entity.LocalPos.XYZ);
                        }
                    }
                }
            }
        }

        public bool IsValid(IPlayer byPlayer, InWorldCraftingRecipe recipe, ItemSlot slot)
        {
            bool toolMatchesCheck1 = slot.Itemstack?.Collectible?.Code?.WildCardMatch(recipe?.Tool?.Code, slot.Itemstack.Collectible.ItemClass, byPlayer.Entity.World.Api) ?? false;
            bool toolMatchesCheck2 = recipe.Tool.Code.IsWildCard && recipe.Tool.Clone().Code.GetMatches(byPlayer.Entity.Api).Any(t => t.ToString() == slot.Itemstack?.Collectible?.Code?.ToString());
            bool validStackSize = slot.Itemstack?.StackSize >= recipe.Tool.Clone().StackSize;
            bool attributesCheck = (recipe.Tool.Clone().Attributes == null || recipe.Tool.Clone().Attributes == slot.Itemstack?.Collectible?.Attributes);

            return (validStackSize && (toolMatchesCheck1 || toolMatchesCheck2) && attributesCheck);
        }
    }

    class InWorldCraftingRecipe
    {
        public InWorldCraftingRecipe(EnumInWorldCraftingMode mode, JsonCraftingIngredient takes, JsonCraftingIngredient tool, JsonCraftingOutput[] returns, JsonCraftingOutput[] makes, AssetLocation craftSound, bool isTool, bool disabled, bool remove, float makeTime)
        {
            Mode = mode;
            Takes = takes;
            Tool = tool;
            Returns = returns;
            Makes = makes;
            CraftSound = craftSound;
            IsTool = isTool;
            Disabled = disabled;
            Remove = remove;
            MakeTime = makeTime;
        }

        public InWorldCraftingRecipe Clone()
        {
            return new InWorldCraftingRecipe(Mode, Takes.Clone(), Tool.Clone(), Returns.DeepClone(), Makes.DeepClone(), CraftSound, IsTool, Disabled, Remove, MakeTime);
        }

        public EnumInWorldCraftingMode Mode { get; set; } = EnumInWorldCraftingMode.Swap;
        public JsonCraftingIngredient Takes { get; set; }
        public JsonCraftingIngredient Tool { get; set; }
        public JsonCraftingOutput[] Returns { get; set; }
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
        public JsonCraftingIngredient(AssetLocation[] allowedVariants, AssetLocation name, int count)
        {
            AllowedVariants = allowedVariants;
            Name = name;
            Count = count;
        }

        public new JsonCraftingIngredient Clone()
        {
            JsonCraftingIngredient ingredient = new JsonCraftingIngredient(AllowedVariants, Name, Count);
            ingredient.Attributes = Attributes?.Token?.HasValues ?? false ? Attributes : null;
            ingredient.Code = Code;
            ingredient.Name = Name;
            ingredient.StackSize = StackSize;
            ingredient.Type = Type;

            return ingredient;
        }

        public AssetLocation[] AllowedVariants { get; set; }
        public AssetLocation Name { get; set; }

        public int Count
        {
            get => StackSize;
            set => StackSize = value;
        }
    }

    public class JsonCraftingOutput : JsonItemStack
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
