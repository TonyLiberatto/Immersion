using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace Immersion
{
    public class BlockEntityMortarAndPestle : BlockEntityOpenableContainer
    {
        static SimpleParticleProperties FlourDustParticles;

        static BlockEntityMortarAndPestle()
        {
            // 1..5 per tick
            FlourDustParticles = new SimpleParticleProperties(1, 3, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1, 1, 0.1f, 0.3f, EnumParticleModel.Quad);
            FlourDustParticles.AddQuantity = 5;
            FlourDustParticles.MinVelocity.Set(-0.05f, 0, -0.05f);
            FlourDustParticles.AddVelocity.Set(0.1f, 0.2f, 0.1f);
            FlourDustParticles.WithTerrainCollision = false;
            FlourDustParticles.LifeLength = 1.5f;
            FlourDustParticles.SelfPropelled = true;
            FlourDustParticles.GravityEffect = 0;
            FlourDustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
            FlourDustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine("Right click inside center to access inventory.");
            base.GetBlockInfo(forPlayer, dsc);
        }

        internal InventoryQuern inventory;

        // For how long the current ore has been grinding
        public float inputGrindTime;
        public float prevInputGrindTime;


        GuiDialogBlockEntityQuern clientDialog;
        PestleRenderer renderer;


        // Server side only
        List<IPlayer> playersGrinding = new List<IPlayer>();
        // Client and serverside
        int quantityPlayersGrinding;

        #region Getters

        public string Material
        {
            get { return Block.LastCodePart(); }
        }

        public float GrindSpeed
        {
            get
            {
                if (quantityPlayersGrinding > 0) return 1;
                return 0;
            }
        }


        MeshData quernBaseMesh
        {
            get
            {
                object value = null;
                Api.ObjectCache.TryGetValue("mpbasemesh-" + Material, out value);
                return (MeshData)value;
            }
            set { Api.ObjectCache["mpbasemesh-" + Material] = value; }
        }

        MeshData quernTopMesh
        {
            get
            {
                object value = null;
                Api.ObjectCache.TryGetValue("mptopmesh-" + Material, out value);
                return (MeshData)value;
            }
            set { Api.ObjectCache["mptopmesh-" + Material] = value; }
        }

        #endregion

        #region Config

        // seconds it requires to melt the ore once beyond melting point
        public virtual float maxGrindingTime()
        {
            return 4;
        }

        public override string InventoryClassName
        {
            get { return "quern"; }
        }

        public virtual string DialogTitle
        {
            get { return Lang.Get("Quern"); }
        }

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        #endregion


        public BlockEntityMortarAndPestle()
        {
            inventory = new InventoryQuern(null, null);
            inventory.SlotModified += OnSlotModifid;
        }



        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            inventory.LateInitialize("quern-1", api);

            RegisterGameTickListener(Every100ms, 100);
            RegisterGameTickListener(Every500ms, 500);

            if (api is ICoreClientAPI)
            {
                renderer = new PestleRenderer(api as ICoreClientAPI, Pos, GenMesh("top"));

                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque);

                if (quernBaseMesh == null)
                {
                    quernBaseMesh = GenMesh("base");
                }
                if (quernTopMesh == null)
                {
                    quernTopMesh = GenMesh("top");
                }
            }
        }

        private void Every100ms(float dt)
        {
            float grindSpeed = GrindSpeed;

            if (Api.Side == EnumAppSide.Client)
            {
                if (InputStack != null)
                {
                    float x = (float)Api.World.Rand.NextDouble(), z = (float)Api.World.Rand.NextDouble();
                    float dustMinQ = 1 * grindSpeed;
                    float dustAddQ = 5 * grindSpeed;
                    float flourPartMinQ = 1 * grindSpeed;
                    float flourPartAddQ = 20 * grindSpeed;

                    FlourDustParticles.Color = InputStack.Collectible.GetRandomColor(Api as ICoreClientAPI, InputStack);
                    FlourDustParticles.Color &= 0xffffff;
                    FlourDustParticles.Color |= (200 << 24);
                    FlourDustParticles.MinQuantity = dustMinQ;
                    FlourDustParticles.AddQuantity = dustAddQ;
                    FlourDustParticles.MinPos.Set(Pos.MidPoint().Add(0, 0.1, 0));
                    FlourDustParticles.MinVelocity.Set(-0.1f, 0, -0.1f);
                    FlourDustParticles.AddVelocity.Set(0.2f, 0.2f, 0.2f);
                    Api.World.SpawnParticles(FlourDustParticles);
                }

                return;
            }


            // Only tick on the server and merely sync to client

            // Use up fuel
            if (CanGrind() && grindSpeed > 0)
            {
                inputGrindTime += dt * grindSpeed;

                if (inputGrindTime >= maxGrindingTime())
                {
                    grindInput();
                    inputGrindTime = 0;
                    MarkDirty();
                }

            }
        }

        private void grindInput()
        {
            ItemStack grindedStack = InputGrindProps.GroundStack.ResolvedItemstack;

            if (OutputSlot.Itemstack == null)
            {
                OutputSlot.Itemstack = grindedStack.Clone();
            }
            else
            {
                OutputSlot.Itemstack.StackSize += grindedStack.StackSize;
            }

            InputSlot.TakeOut(1);
            InputSlot.MarkDirty();
            OutputSlot.MarkDirty();
        }


        // Sync to client every 500ms
        private void Every500ms(float dt)
        {
            if (Api.Side == EnumAppSide.Server && (GrindSpeed > 0 || prevInputGrindTime != inputGrindTime))
            {
                MarkDirty();
            }

            prevInputGrindTime = inputGrindTime;
        }





        public void SetPlayerGrinding(IPlayer player, bool playerGrinding)
        {
            if (playerGrinding)
            {
                if (!playersGrinding.Contains(player))
                {
                    playersGrinding.Add(player);
                }
            }
            else
            {
                playersGrinding.Remove(player);
            }

            quantityPlayersGrinding = playersGrinding.Count;

            updateGrindingState();
        }

        bool beforeGrinding;
        void updateGrindingState()
        {
            if (Api?.World == null) return;

            bool nowGrinding = quantityPlayersGrinding > 0;

            if (nowGrinding != beforeGrinding)
            {
                if (renderer != null)
                {
                    renderer.ShouldRotate = quantityPlayersGrinding > 0;
                }

                Api.World.BlockAccessor.MarkBlockDirty(Pos, OnRetesselated);

                if (Api.Side == EnumAppSide.Server)
                {
                    MarkDirty();
                }
            }

            beforeGrinding = nowGrinding;
        }




        private void OnSlotModifid(int slotid)
        {
            if (Api is ICoreClientAPI && clientDialog?.Attributes != null)
            {
                SetDialogValues(clientDialog.Attributes);
            }

            if (slotid == 0)
            {
                inputGrindTime = 0.0f; //reset the progress to 0 if the item is removed.
                MarkDirty();

                if (clientDialog != null && clientDialog.IsOpened())
                {
                    clientDialog.SingleComposer.ReCompose();
                }
            }
        }


        private void OnRetesselated()
        {
            if (renderer == null) return; // Maybe already disposed

            renderer.ShouldRender = quantityPlayersGrinding > 0;
        }




        internal MeshData GenMesh(string type = "base")
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.BlockId == 0) return null;

            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

            mesher.TesselateShape(block, Api.Assets.TryGet("immersion:shapes/block/wood/mortarandpestle/" + type + ".json").ToObject<Shape>(), out mesh);

            return mesh;
        }




        public bool CanGrind()
        {
            GrindingProperties grindProps = InputGrindProps;
            if (grindProps == null) return false;
            if (OutputSlot.Itemstack == null) return true;

            int mergableQuantity = OutputSlot.Itemstack.Collectible.GetMergableQuantity(OutputSlot.Itemstack, grindProps.GroundStack.ResolvedItemstack);

            return mergableQuantity >= grindProps.GroundStack.ResolvedItemstack.StackSize;
        }




        #region Events

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;

            if (Api.World is IServerWorldAccessor)
            {
                byte[] data;

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write("BEMortarAndPestle");
                    writer.Write(DialogTitle);
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }

                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBlockStovePacket.OpenGUI,
                    data
                );

                byPlayer.InventoryManager.OpenInventory(inventory);
            }

            return true;
        }


        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }


            inputGrindTime = tree.GetFloat("inputGrindTime");

            if (worldForResolving.Side == EnumAppSide.Client)
            {
                List<int> clientIds = new List<int>((tree["clientIdsGrinding"] as IntArrayAttribute).value);

                quantityPlayersGrinding = clientIds.Count;

                for (int i = 0; i < playersGrinding.Count; i++)
                {
                    IPlayer plr = playersGrinding[i];
                    if (!clientIds.Contains(plr.ClientId))
                    {
                        playersGrinding.Remove(plr);
                        i--;
                    }
                    else
                    {
                        clientIds.Remove(plr.ClientId);
                    }
                }

                for (int i = 0; i < clientIds.Count; i++)
                {
                    IPlayer plr = worldForResolving.AllPlayers.FirstOrDefault(p => p.ClientId == clientIds[i]);
                    if (plr != null) playersGrinding.Add(plr);
                }

                updateGrindingState();
            }



            if (Api?.Side == EnumAppSide.Client && clientDialog?.Attributes != null && clientDialog.IsOpened())
            {
                SetDialogValues(clientDialog.Attributes);
            }
        }


        void SetDialogValues(ITreeAttribute dialogTree)
        {
            dialogTree.SetFloat("inputGrindTime", inputGrindTime);
            dialogTree.SetFloat("maxGrindTime", maxGrindingTime());
        }




        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("inputGrindTime", inputGrindTime);
            tree["clientIdsGrinding"] = new IntArrayAttribute(playersGrinding.Select(p => p.ClientId).ToArray());
        }




        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (renderer != null)
            {
                renderer.Unregister();
                renderer = null;
            }
        }

        public override void OnBlockBroken()
        {
            base.OnBlockBroken();
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

                return;
            }

            if (packetid == (int)EnumBlockStovePacket.CloseGUI)
            {
                if (player.InventoryManager != null)
                {
                    player.InventoryManager.CloseInventory(Inventory);
                }
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumBlockStovePacket.OpenGUI)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);

                    string dialogClassName = reader.ReadString();
                    string dialogTitle = reader.ReadString();

                    TreeAttribute tree = new TreeAttribute();
                    tree.FromBytes(reader);
                    Inventory.FromTreeAttributes(tree);
                    Inventory.ResolveBlocksOrItems();

                    IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;

                    SyncedTreeAttribute dtree = new SyncedTreeAttribute();
                    SetDialogValues(dtree);

                    if (clientDialog == null || !clientDialog.IsOpened())
                    {
                        clientDialog = new GuiDialogBlockEntityQuern(dialogTitle, Inventory, Pos, dtree, Api as ICoreClientAPI);
                        clientDialog.TryOpen();
                        clientDialog.OnClosed += () => clientDialog = null;
                    }
                }
            }

            if (packetid == (int)EnumBlockContainerPacketId.CloseInventory)
            {
                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
                clientWorld.Player.InventoryManager.CloseInventory(Inventory);
            }
        }

        #endregion

        #region Helper getters


        public ItemSlot InputSlot
        {
            get { return inventory[0]; }
        }

        public ItemSlot OutputSlot
        {
            get { return inventory[1]; }
        }

        public ItemStack InputStack
        {
            get { return inventory[0].Itemstack; }
            set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
        }

        public ItemStack OutputStack
        {
            get { return inventory[1].Itemstack; }
            set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
        }


        public GrindingProperties InputGrindProps
        {
            get
            {
                ItemSlot slot = inventory[0];
                if (slot.Itemstack == null) return null;
                return slot.Itemstack.Collectible.GrindingProps;
            }
        }

        #endregion


        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;

                if (slot.Itemstack.Class == EnumItemClass.Item)
                {
                    itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
                }
                else
                {
                    blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
                }
            }
        }


        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;
                if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                {
                    slot.Itemstack = null;
                }
            }
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (Block == null) return false;
            float yDeg = BlockFacing.FromCode(Block.Variant["side"]).HorizontalAngleIndex * 90;
            mesher.AddMeshData(quernBaseMesh);

            if (quantityPlayersGrinding == 0)
            {
                mesher.AddMeshData(
                    quernTopMesh.Clone()
                    .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0.0f, 0.0f, 0.0f)
                    .Translate(0.1f, 0.0f, 0.0f)
                    .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0.0f, (yDeg - 90) * GameMath.DEG2RAD, -45.0f * GameMath.DEG2RAD)
                    .Translate(0.0f, 0.5f, 0.0f)
                );
            }


            return true;
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            renderer?.Unregister();
        }
    }
}
