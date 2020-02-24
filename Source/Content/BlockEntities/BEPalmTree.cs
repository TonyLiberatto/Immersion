using System.Collections.Generic;
using System.Linq;
using Immersion;
using Vintagestory.ServerMods;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Immersion
{
    public class PosBlockPair
    {
        public PosBlockPair(BlockPos pos, int blockid)
        {
            this.pos = pos;
            this.blockid = blockid;
        }

        public BlockPos pos { get; set; }
        public int blockid { get; set; }
    }

    public class BEPalmTree : BlockEntity
    {
        public double nextGrowTime;
        public List<PosBlockPair> ConnectedFruits { get; set; }

        public List<PosBlockPair> ConnectedFronds { get; set; }


        public void UpdateConnected() { SetConnectedFronds(); SetConnectedFruits(); }
        public void SetConnectedFronds()
        {
            List<PosBlockPair> blocks = new List<PosBlockPair>();
            var vecs = new Vec2i[]
            {
                new Vec2i(0, 1), new Vec2i(0, -1), new Vec2i(-1, 0), new Vec2i(1, 0)
            };

            foreach (var val in vecs)
            {
                BlockPos newPos = new BlockPos(Pos.X + val.X, Pos.Y, Pos.Z + val.Y);
                blocks.Add(new PosBlockPair(newPos, Api.World.BlockAccessor.GetBlock(newPos).Id));
            }
            ConnectedFronds = blocks;
        }
        public void SetConnectedFruits()
        {
            List<PosBlockPair> blocks = new List<PosBlockPair>();
            var vecs = new Vec2i[]
            {
                new Vec2i(0, 1), new Vec2i(0, -1), new Vec2i(-1, 0), new Vec2i(1, 0)
            };

            foreach (var val in vecs)
            {
                BlockPos newPos = new BlockPos(Pos.X + val.X, Pos.Y - 1, Pos.Z + val.Y);
                blocks.Add(new PosBlockPair(newPos, Api.World.BlockAccessor.GetBlock(newPos).Id));
            }
            ConnectedFruits = blocks;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (ConnectedFruits == null) SetConnectedFruits();
            if (ConnectedFronds == null) SetConnectedFronds();

            if (!api.World.Side.IsServer()) return;

            RegisterGameTickListener(dt =>
            {
                if (api.World.Calendar.TotalHours > nextGrowTime)
                {
                    foreach (var val in ConnectedFronds)
                    {
                        if (api.World.BlockAccessor.GetBlock(val.pos).Id == 0 && val.blockid.GetBlock(api).FirstCodePart() == "palmfronds")
                        {
                            api.World.BlockAccessor.SetBlock(val.blockid, val.pos);
                        }
                    }

                    foreach (var val in ConnectedFruits)
                    {
                        if (api.World.BlockAccessor.GetBlock(val.pos).Id == 0 && val.blockid.GetBlock(api).FirstCodePart() == "palmfruits")
                        {
                            api.World.BlockAccessor.SetBlock(val.blockid, val.pos);
                        }
                    }

                    UpdateConnected();
                    UpdateGrowTime();
                }
            }, 500);
        }

        public void UpdateGrowTime() => nextGrowTime = Api.World.Calendar.TotalHours + 48;

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAtributes(tree, worldAccessForResolve);
            byte[] ftBytes = tree.GetBytes("myFruits");
            byte[] fdBytes = tree.GetBytes("myFronds");

            if (ftBytes != null) ConnectedFruits = JsonUtil.FromBytes<List<PosBlockPair>>(ftBytes);
            if (fdBytes != null) ConnectedFronds = JsonUtil.FromBytes<List<PosBlockPair>>(fdBytes);

            nextGrowTime = tree.GetDouble("nextGrowTime", worldAccessForResolve.Calendar.TotalHours + 48);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if (ConnectedFruits != null) tree.SetBytes("myFruits", JsonUtil.ToBytes(ConnectedFruits));
            if (ConnectedFronds != null) tree.SetBytes("myFronds", JsonUtil.ToBytes(ConnectedFronds));

            tree.SetDouble("nextGrowTime", nextGrowTime);
            base.ToTreeAttributes(tree);
        }
    }
}
