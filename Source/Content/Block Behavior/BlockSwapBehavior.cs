using Newtonsoft.Json;
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
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Immersion
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SwapMessage
    {
        public string SwapPairs;
    }

    class SwapSystem : ModSystem
    {
        IServerNetworkChannel sChannel;

        public override void StartClientSide(ICoreClientAPI Api)
        {
            Api.Network.RegisterChannel("swapPairs")
                .RegisterMessageType<SwapMessage>()
                .SetMessageHandler<SwapMessage>(a =>
                {
                    SwapPairs = JsonConvert.DeserializeObject<Dictionary<string, SwapBlocks>>(a.SwapPairs);
                });
        }

        public override void StartServerSide(ICoreServerAPI Api)
        {
            sChannel = Api.Network.RegisterChannel("swapPairs").RegisterMessageType<SwapMessage>();
            Api.Event.PlayerJoin += PlayerJoin;
        }

        private void PlayerJoin(IServerPlayer byPlayer)
        {
            sChannel.SendPacket(new SwapMessage() { SwapPairs = JsonConvert.SerializeObject(SwapPairs) }, byPlayer);
        }

        public Dictionary<string, SwapBlocks> SwapPairs { get; set; } = new Dictionary<string, SwapBlocks>();
    }

    class BlockSwapBehavior : BlockBehavior
    {
        ICoreAPI Api;
        Vec3d particleOrigin = new Vec3d(0.5, 0.5, 0.5);
        bool requireSneak = false;
        bool disabled = false;
        bool playSound = true;
        bool allowPlaceOn = false;
        int pRadius = 2;
        int pQuantity = 16;

        public BlockSwapBehavior(Block block) : base(block) { }

        public override void OnLoaded(ICoreAPI Api)
        {
            base.OnLoaded(Api);
            this.Api = Api;
            PostOLInit();
        }

        public void PostOLInit()
        {
            SwapSystem swapSystem = Api.ModLoader.GetModSystem<SwapSystem>();
            requireSneak = properties["requireSneak"].AsBool(requireSneak);
            particleOrigin = properties["particleOrigin"].Exists ? properties["particleOrigin"].AsObject<Vec3d>() : particleOrigin;
            pRadius = properties["particleRadius"].AsInt(pRadius);
            pQuantity = properties["particleQuantity"].AsInt(pQuantity);
            playSound = properties["playSound"].AsBool(true);
            allowPlaceOn = properties["allowPlaceOn"].AsBool(false);

            if (properties["allowedVariants"].Exists)
            {
                string[] allowed = properties["allowedVariants"].AsArray<string>().WithDomain();

                disabled = true;
                if (allowed.Contains(block.Code.ToString()))
                {
                    disabled = false;
                }
                else return;
            }
            if (properties["swapBlocks"].Exists)
            {
                if (Api.World.Side.IsServer())
                {
                    try
                    {
                        SwapBlocks[] swapBlocks = properties["swapBlocks"].AsObject<SwapBlocks[]>();
                        foreach (SwapBlocks val in swapBlocks)
                        {
                            if (swapSystem.SwapPairs.ContainsKey(GetKey(val.Tool))) continue;

                            if (val.Tool.Contains("*"))
                            {
                                SwapBlocks tmp = val.Copy();

                                foreach (var block in Api.World.Blocks)
                                {
                                    if (block.WildCardMatch(val.Tool))
                                    {
                                        tmp.Tool = block.Code.ToString();
                                        string key = GetKey(tmp.Tool);
                                        if (swapSystem.SwapPairs.ContainsKey(key)) continue;

                                        swapSystem.SwapPairs.Add(key, tmp.Copy());
                                    }
                                }
                                foreach (var item in Api.World.Items)
                                {
                                    if (item.WildCardMatch(val.Tool))
                                    {
                                        tmp.Tool = item.Code.ToString();
                                        string key = GetKey(tmp.Tool);
                                        if (swapSystem.SwapPairs.ContainsKey(key)) continue;

                                        swapSystem.SwapPairs.Add(key, tmp.Copy());
                                    }
                                }
                            }
                            else
                            {
                                swapSystem.SwapPairs.Add(GetKey(val.Tool), val);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        disabled = true;
                        Api.World.Logger.Notification("Deprecated or unsupported use of swapblocks in " + block.Code.ToString());
                    }
                }
            }
            else
            {
                disabled = true;
                return;
            }
        }

        public string GetKey(string holdingstack) => holdingstack.Apd(block.Code.ToString());

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            handled = EnumHandling.PreventDefault;
            SwapSystem swapSystem = Api.ModLoader.GetModSystem<SwapSystem>();
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (slot?.Itemstack?.Collectible?.Code == null) return false;
            string key = GetKey(slot?.Itemstack?.Collectible?.Code?.ToString()) ?? "";

            ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            if (swapSystem.SwapPairs.TryGetValue(key, out SwapBlocks swap))
            {
                if (world.Side.IsClient() && secondsUsed != 0 && swap.MakeTime != 0 && !block.HasBehavior<BlockCreateBehavior>())
                {
                    ((byPlayer.Entity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    float animstep = (secondsUsed / swap.MakeTime) * 1.0f;
                    //Api.ModLoader.GetModSystem<ShaderTest>().progressBar = animstep;
                }
                return secondsUsed < swap.MakeTime;
            }
            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            BlockPos Pos = blockSel?.Position;
            if (Pos == null) return;

            ModSystemBlockReinforcement bR = Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
            if (disabled || bR.IsReinforced(Pos) || bR.IsLockedForInteract(Pos, byPlayer)) return;

            SwapSystem swapSystem = Api.ModLoader.GetModSystem<SwapSystem>();
            handling = EnumHandling.PreventDefault;
            ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;


            if (!(requireSneak && !byPlayer.Entity.Controls.Sneak) && slot.Itemstack != null)
            {
                string key = GetKey(slot.Itemstack.Collectible.Code.ToString());

                if (swapSystem.SwapPairs.TryGetValue(key, out SwapBlocks swap))
                {
                    //if (world.Side.IsClient()) Api.ModLoader.GetModSystem<ShaderTest>().progressBar = 0;

                    if (swap.Takes != null && swap.Takes != block.Code.ToString() || secondsUsed < swap.MakeTime)
                    {
                        return;
                    }

                    AssetLocation asset = slot.Itemstack.Collectible.Code;
                    if (asset.ToString() == swap.Tool)
                    {
                        AssetLocation toAsset = new AssetLocation(swap.Makes.WithDomain());
                        Block toBlock = toAsset.GetBlock(world.Api);

                        int count = swap.Count;

                        if (count != 0)
                        {
                            if (count < 0)
                            {
                                ItemStack withCount = slot.Itemstack.Clone();
                                withCount.StackSize = Math.Abs(count);
                                if (!byPlayer.InventoryManager.TryGiveItemstack(withCount))
                                {
                                    world.SpawnItemEntity(withCount, Pos.ToVec3d().Add(0.5, 0.5, 0.5));
                                }
                            }
                            else if (slot.Itemstack.StackSize >= count)
                            {
                                if (byPlayer.WorldData.CurrentGameMode.IsSurvival()) slot.TakeOut(count);
                            }
                            else return;
                        }

                        if ((block.EntityClass != null && toBlock.EntityClass != null) && (toBlock.EntityClass == block.EntityClass))
                        {
                            world.BlockAccessor.ExchangeBlock(toBlock.BlockId, Pos);
                        }
                        else
                        {
                            world.BlockAccessor.SetBlock(toBlock.BlockId, Pos);
                        }
                        slot.MarkDirty();
                        PlaySoundDispenseParticles(world, Pos, slot);
                        return;
                    }
                }
            }
            
            ItemStack stack = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (stack != null && allowPlaceOn)
            {
                string r = "";
                BlockSelection newsel = blockSel.Clone();
                newsel.Position = newsel.Position.Offset(blockSel.Face);
                Block block = stack.Block;

                if (block != null && block.TryPlaceBlock(world, byPlayer, stack, newsel, ref r))
                {
                    world.PlaySoundAt(stack.Block?.Sounds.Place, newsel.Position);
                }

            }
        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
           // if (world.Side.IsClient()) Api.ModLoader.GetModSystem<ShaderTest>().progressBar = 0;
            return false;
        }

        public void PlaySoundDispenseParticles(IWorldAccessor world, BlockPos Pos, ItemSlot slot)
        {
            try
            {
                if (world.Side.IsServer())
                {
                    world.SpawnCubeParticles(Pos, Pos.ToVec3d().Add(particleOrigin), pRadius, pQuantity);
                    world.SpawnCubeParticles(Pos.ToVec3d().Add(particleOrigin), slot.Itemstack, pRadius, pQuantity);
                    if (playSound)
                    {
                        world.PlaySoundAt(block.Sounds.Place, Pos.X, Pos.Y, Pos.Z);
                    }
                }
            }
            catch (Exception) { }
        }
    }
}
