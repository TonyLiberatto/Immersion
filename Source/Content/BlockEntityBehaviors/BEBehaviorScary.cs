using System;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Immersion
{

    public class BEBehaviorScary : BlockEntityBehavior, IPointOfFear
    {
        public BEBehaviorScary(BlockEntity blockentity) : base(blockentity)
        {
        }

        public Vec3d Position => Blockentity.Pos.MidPoint();
        public string Type => "scary";
        public float FearRadius { get; set; } = 48;

        POIRegistry registry { get => Api.ModLoader.GetModSystem<POIRegistry>(); }

        public override void Initialize(ICoreAPI Api, JsonObject properties)
        {
            base.Initialize(Api, properties);
            FearRadius = properties["fearRadius"].AsFloat(48);
            if (Api.Side == EnumAppSide.Server)
            {
                registry.AddPOI(this);
            }
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Server)
            {
                registry.RemovePOI(this);
            }
        }
    }

    public class BEBehaviorFirepitScary : BlockEntityBehavior, IPointOfFear
    {
        POIRegistry registry { get => Api.ModLoader.GetModSystem<POIRegistry>(); }
        public BEBehaviorFirepitScary(BlockEntity blockentity) : base(blockentity)
        {
        }

        public Vec3d Position => Blockentity.Pos.MidPoint();
        public string Type => "scary";
        public bool IsBurning { get => (Blockentity as BlockEntityFirepit)?.IsBurning ?? false; }
        public float FearRadius { get; set; } = 48;

        public override void Initialize(ICoreAPI Api, JsonObject properties)
        {
            base.Initialize(Api, properties);
            FearRadius = properties["fearRadius"].AsFloat(48);
            if (Api.Side.IsServer())
            {
                Blockentity.RegisterGameTickListener(dt =>
                {
                    if (IsBurning) registry.AddPOI(this);
                    else registry.RemovePOI(this);
                }, 5000);
            }
        }

        public override void OnBlockRemoved()
        {
            if (Api.Side == EnumAppSide.Server && IsBurning)
            {
                registry.RemovePOI(this);
            }
        }
    }

    public interface IPointOfFear : IPointOfInterest
    {
        float FearRadius { get; set; }
    }
}
