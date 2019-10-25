using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Neolithic
{
    public class BEMortarAndPestle : BlockEntityOpenableContainer, IBlockShapeSupplier
    {
        //static SimpleParticleProperties FlourParticles;

        static SimpleParticleProperties FlourDustParticles;

        static BEMortarAndPestle()
        {
            Vec3d CommonPos = new Vec3d(3 / 32f, 0, 3 / 32f);
            /*
            FlourParticles = new SimpleParticleProperties(1, 3, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1, 1, 0.1f, 0.3f, EnumParticleModel.Quad);
            FlourParticles.addPos.Set(CommonPos);
            FlourParticles.addQuantity = 20;
            FlourParticles.minVelocity.Set(-0.25f, 0, -0.25f);
            FlourParticles.addVelocity.Set(0.5f, 1, 0.5f);
            FlourParticles.WithTerrainCollision = true;
            FlourParticles.model = EnumParticleModel.Cube;
            FlourParticles.lifeLength = 1.5f;
            FlourParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.4f);
            */

            FlourDustParticles = new SimpleParticleProperties(1, 3, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1, 1, 0.1f, 0.3f, EnumParticleModel.Quad);
            FlourDustParticles.addPos.Set(CommonPos);
            FlourDustParticles.addQuantity = 5;
            FlourDustParticles.minVelocity.Set(-0.05f, 0, -0.05f);
            FlourDustParticles.addVelocity.Set(0.1f, 0.2f, 0.1f);
            FlourDustParticles.WithTerrainCollision = false;
            FlourDustParticles.model = EnumParticleModel.Quad;
            FlourDustParticles.lifeLength = 1.5f;
            FlourDustParticles.SelfPropelled = true;
            FlourDustParticles.gravityEffect = 0;
            FlourDustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 0.4f);
            FlourDustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
        }

        internal InventoryQuern inventory;

        // For how long the current ore has been grinding
        public float inputGrindTime;
        public float prevInputGrindTime;


        GuiDialogBlockEntityQuern clientDialog;

        Block ownBlock;
        PestleRenderer renderer;

        public string Material
        {
            get { return ownBlock.Variant["wood"]; }
        }

        // Server side only
        List<IPlayer> playersGrinding = new List<IPlayer>();
        // Client and serverside
        int quantityPlayersGrinding;

        public bool IsGrinding
        {
            get { return quantityPlayersGrinding > 0; }
        }

        public void SetPlayerGrinding(IPlayer player, bool playerGrinding)
        {
            bool beforeGrinding = IsGrinding;

            if (playerGrinding)
            {
                if (!playersGrinding.Contains(player))
                {
                    playersGrinding.Add(player);
                }
                //Console.WriteLine("added player");
            }
            else
            {
                playersGrinding.Remove(player);
                //Console.WriteLine("removed player");
            }

            quantityPlayersGrinding = playersGrinding.Count;

            updateGrindingState(beforeGrinding);
        }


        void updateGrindingState(bool beforeGrinding)
        {
            bool nowGrinding = IsGrinding;

            if (nowGrinding != beforeGrinding)
            {
                if (renderer != null)
                {
                    renderer.ShouldRotate = nowGrinding;
                }

                api.World.BlockAccessor.MarkBlockDirty(pos, OnRetesselated);

                if (api.Side == EnumAppSide.Server)
                {
                    MarkDirty();
                }
            }
        }



        MeshData quernBaseMesh
        {
            get
            {
                object value = null;
                api.ObjectCache.TryGetValue("quernbasemesh-" + Material, out value);
                return (MeshData)value;
            }
            set { api.ObjectCache["quernbasemesh-" + Material] = value; }
        }

        MeshData quernTopMesh
        {
            get
            {
                object value = null;
                api.ObjectCache.TryGetValue("querntopmesh-" + Material, out value);
                return (MeshData)value;
            }
            set { api.ObjectCache["querntopmesh-" + Material] = value; }
        }



        #region Config

        public virtual float SoundLevel
        {
            get { return 0.5f; }
        }

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
            get { return "Quern"; }
        }

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        #endregion


        private void OnRetesselated()
        {
            if (renderer == null) return; // Maybe already disposed

            //Console.WriteLine("did retesselate now, players using: {0}, grind flag: {1}", playersGrinding.Count, IsGrinding);
            renderer.ShouldRender = IsGrinding;
        }



        public BEMortarAndPestle()
        {
            inventory = new InventoryQuern(null, null);
            inventory.SlotModified += OnSlotModifid;
        }



        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            ownBlock = api.World.BlockAccessor.GetBlock(pos);

            inventory.LateInitialize("quern-1", api);


            RegisterGameTickListener(Every100ms, 100);
            RegisterGameTickListener(Every500ms, 500);

            if (api is ICoreClientAPI)
            {
                renderer = new PestleRenderer(api as ICoreClientAPI, pos, GenMesh("top"));

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


        private void OnSlotModifid(int slotid)
        {
            if (api is ICoreClientAPI && clientDialog != null)
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

        internal MeshData GenMesh(string type = "base")
        {
            Block block = api.World.BlockAccessor.GetBlock(pos);
            if (block.BlockId == 0) return null;


            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)api).Tesselator;

            mesher.TesselateShape(block, api.Assets.TryGet("neolithicmod:shapes/block/wood/mortarandpestle/" + type + ".json").ToObject<Shape>(), out mesh);

            return mesh;
        }


        private void Every100ms(float dt)
        {
            if (api.Side == EnumAppSide.Client)
            {
                if (!IsGrinding || InputStack == null) return;

                FlourDustParticles.color /*= FlourParticles.color*/ = InputStack.Collectible.GetRandomColor(api as ICoreClientAPI, InputStack);
                FlourDustParticles.color &= 0xffffff;
                FlourDustParticles.color |= (200 << 24);

                //FlourParticles.minPos.Set(pos.X+0.5, pos.Y + 0.5, pos.Z+0.5);
                FlourDustParticles.minPos.Set(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5);


                FlourDustParticles.minVelocity.Set(-0.1f, 0, -0.1f);
                FlourDustParticles.addVelocity.Set(0.2f, 0.2f, 0.2f);

                //api.World.SpawnParticles(FlourParticles);
                api.World.SpawnParticles(FlourDustParticles);

                return;
            }


            // Only tick on the server and merely sync to client

            // Use up fuel
            if (CanGrind() && IsGrinding)
            {
                inputGrindTime += dt;

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
            ItemStack grindedStack = InputGrindProps.GrindedStack.ResolvedItemstack;

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
            if (api is ICoreServerAPI && (IsGrinding || prevInputGrindTime != inputGrindTime))
            {
                MarkDirty();
            }

            prevInputGrindTime = inputGrindTime;
        }



        public bool CanGrind()
        {
            GrindingProperties grindProps = InputGrindProps;
            if (grindProps == null) return false;
            if (OutputSlot.Itemstack == null) return true;

            int mergableQuantity = OutputSlot.Itemstack.Collectible.GetMergableQuantity(OutputSlot.Itemstack, grindProps.GrindedStack.ResolvedItemstack);

            return mergableQuantity >= grindProps.GrindedStack.ResolvedItemstack.StackSize;
        }




        #region Events

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;

            if (api.World is IServerWorldAccessor)
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

                ((ICoreServerAPI)api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    pos.X, pos.Y, pos.Z,
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

            if (api != null)
            {
                Inventory.AfterBlocksLoaded(api.World);
            }


            inputGrindTime = tree.GetFloat("inputGrindTime");

            if (worldForResolving.Side == EnumAppSide.Client)
            {
                List<int> clientIds = new List<int>((tree["clientIdsGrinding"] as IntArrayAttribute).value);
                bool wasGrinding = quantityPlayersGrinding > 0;

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

                updateGrindingState(wasGrinding);
            }

            if (api?.Side == EnumAppSide.Client && clientDialog != null)
            {
                SetDialogValues(clientDialog.Attributes);
            }
        }


        void SetDialogValues(ITreeAttribute dialogTree)
        {
            if (dialogTree != null)
            {
                dialogTree.SetFloat("inputGrindTime", inputGrindTime);
                dialogTree.SetFloat("maxGrindTime", maxGrindingTime());
            }
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
                api.World.BlockAccessor.GetChunkAtBlockPos(pos.X, pos.Y, pos.Z).MarkModified();

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
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    BinaryReader stream = new BinaryReader((Stream)memoryStream);
                    stream.ReadString();
                    string DialogTitle = stream.ReadString();
                    TreeAttribute treeAttribute = new TreeAttribute();
                    treeAttribute.FromBytes(stream);
                    Inventory.FromTreeAttributes(treeAttribute);
                    Inventory.ResolveBlocksOrItems();
                    IClientWorldAccessor world = (IClientWorldAccessor)api.World;
                    SyncedTreeAttribute tree = new SyncedTreeAttribute();
                    SetDialogValues(tree);
                    clientDialog = new GuiDialogBlockEntityQuern(DialogTitle, Inventory, pos, tree, api as ICoreClientAPI);
                    clientDialog.TryOpen();
                    clientDialog.OnClosed += (Vintagestory.API.Common.Action)(() => clientDialog = null);
                }
            }

            if (packetid == (int)EnumBlockContainerPacketId.CloseInventory)
            {
                IClientWorldAccessor clientWorld = (IClientWorldAccessor)api.World;
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
            int q = Inventory.Count;
            for (int i = 0; i < q; i++)
            {
                ItemSlot slot = inventory[i];
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

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping)
        {
            int q = Inventory.Count;
            for (int i = 0; i < q; i++)
            {
                ItemSlot slot = inventory[i];
                if (slot.Itemstack == null) continue;
                if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                {
                    slot.Itemstack = null;
                }
            }
        }



        public bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (ownBlock == null) return false;
            string direc = ownBlock.Variant["side"];
            float yDeg = BlockFacing.FromCode(direc).HorizontalAngleIndex * 90;

            mesher.AddMeshData(
                    quernBaseMesh.Clone()
                    .Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0.0f, (yDeg - 90) * GameMath.DEG2RAD, 0.0f)
                );
            if (!IsGrinding)
            {

                mesher.AddMeshData(
                    quernTopMesh.Clone()
                    //.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, renderer.Angle * GameMath.DEG2RAD, 0)
                    //.Translate(0 / 16f, 11 / 16f, 0 / 16f)
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
