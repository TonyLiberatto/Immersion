using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    public class TransitionConditions
    {
        public TransitionConditions(int RequiredSunlight)
        {
            this.RequiredSunlight = RequiredSunlight;
        }
        public int RequiredSunlight { get; set; } = -1;
    }

    public class BEBehaviorTransient : BlockEntityBehavior
    {
        double transitionAtTotalDays = -1;
        BlockPos Pos { get => Blockentity.Pos; }
        string fromCode;
        string toCode;
        public Block OwnBlock { get => Blockentity.Block; }
        public TransitionConditions conditions;
        long id;

        public BEBehaviorTransient(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            if (transitionAtTotalDays <= 0)
            {
                float hours = properties["inGameHours"].AsFloat(24);
                transitionAtTotalDays = api.World.Calendar.TotalDays + hours / 24;
            }
            fromCode = properties["convertFrom"].AsString()?.WithDomain(OwnBlock.Code.Domain);
            toCode = properties["convertTo"].AsString()?.WithDomain(OwnBlock.Code.Domain);
            conditions = properties["transitionConditions"]?.AsObject<TransitionConditions>(null);

            if (fromCode != null && toCode != null)
            {
                if (api.Side.IsServer())
                {
                    id = Blockentity.RegisterGameTickListener(CheckTransition, 2000);
                }
            }
            else api.World.BlockAccessor.RemoveBlockEntity(Pos);
        }

        public void CheckTransition(float dt)
        {
            if (transitionAtTotalDays > Api.World.Calendar.TotalDays) return;
            if (Api.World.BlockAccessor.GetLightLevel(this.Blockentity.Pos, EnumLightLevelType.OnlySunLight) < (conditions?.RequiredSunlight ?? -1)) return;
            
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            Block tblock;

            if (!toCode.Contains("*"))
            {
                tblock = Api.World.GetBlock(new AssetLocation(toCode));
                if (tblock == null) return;

                Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos);
                return;
            }

            AssetLocation blockCode = block.WildCardReplace(
                new AssetLocation(fromCode),
                new AssetLocation(toCode)
            );

            tblock = Api.World.GetBlock(blockCode);
            if (tblock == null) return;

            Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos);
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);

            transitionAtTotalDays = tree.GetDouble("transitionAtTotalDays");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetDouble("transitionAtTotalDays", transitionAtTotalDays);
        }

        public override void OnBlockUnloaded()
        {
            Blockentity?.UnregisterGameTickListener(id);
            base.OnBlockUnloaded();
        }

        public override void OnBlockRemoved()
        {
            Blockentity?.UnregisterGameTickListener(id);
            base.OnBlockRemoved();
        }
    }
}
