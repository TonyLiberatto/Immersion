using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace Immersion
{
    public class ImmersionContentConfig : ContentConfig
    {
    }

    public class ImmersionTransient : BlockEntityTransient, IAnimalFoodSource
    {
        public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);
        public string Type => "food";
        ImmersionContentConfig[] nltConfig;
        Block ownBlock;
        public string contentCode = "";
        double transitionAtTotalDays = -1;
        public static SimpleParticleProperties Flies;
        public bool flies = true;

        static ImmersionTransient()
        {

            Flies = new SimpleParticleProperties(
                1, 1,
                ColorUtil.ToRgba(100, 0, 0, 0),
                new Vec3d(), new Vec3d(),
                new Vec3f(-1f, -1f, -1f),
                new Vec3f(1f, 1f, 1f),
                1.0f,
                0.01f,
                0.25f, 0.30f,
                EnumParticleModel.Cube
            );
            Flies.glowLevel = 1;
            Flies.addPos.Set(1 / 16f, 0, 1 / 16f);
            Flies.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.05f);
        }

        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);
            ownBlock = Api.World.BlockAccessor.GetBlock(Pos);

            ownBlock = ownBlock == null ? Api.World.BlockAccessor.GetBlock(Pos) : ownBlock;
            flies = ownBlock.Attributes["flies"].AsBool(true);


            if (nltConfig == null)
            {
                nltConfig = ownBlock.Attributes["contentConfig"].AsObject<ImmersionContentConfig[]>();
            }

            if (transitionAtTotalDays <= 0)
            {
                float hours = ownBlock.Attributes["inGameHours"].AsFloat(24);
                transitionAtTotalDays = Api.World.Calendar.TotalDays + hours / 24;
            }

            if (Api.Side == EnumAppSide.Server)
            {
                Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
                RegisterGameTickListener(CheckTransition, 2000);
            }
            if (Api.Side == EnumAppSide.Client && flies)
            {
                RegisterGameTickListener(FliesTick, 100);
            }

        }

        private void FliesTick(float dt)
        {
            if (Api.Side == EnumAppSide.Client && Api.World.Calendar.DayLightStrength > 0.5 )
            {
                double modx = Api.World.Rand.NextDouble();
                double modz = Api.World.Rand.NextDouble();
                double mody = Api.World.Rand.NextDouble();

                int modc = (int)((modx + mody + modz / 3) * 25);

                Flies.minPos.Set(Pos.X + modx, Pos.Y + mody, Pos.Z + modz);
                Flies.color = ColorUtil.ToRgba(100, 0, modc, modc);
                Flies.glowLevel = (byte)(Api.World.Calendar.DayLightStrength * 50);

                Api.World.SpawnParticles(Flies);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (Api.Side == EnumAppSide.Server)
            {
                Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            if (Api.Side == EnumAppSide.Server)
            {
                Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }

        public bool IsSuitableFor(Entity entity)
        {
            ContentConfig contentConfig = ((IEnumerable<ContentConfig>)nltConfig).FirstOrDefault<ContentConfig>(c => c.Code == contentCode);
            if (contentConfig == null)
                return false;
            for (int index = 0; index < contentConfig.Foodfor.Length; ++index)
            {
                if (WildcardUtil.Match(contentConfig.Foodfor[index], entity.Code))
                    return true;
            }
            return false;
        }

        public float ConsumeOnePortion()
        {
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            Block tblock;

            if (block.Attributes == null) return 1f;

            string fromCode = block.Attributes["convertFrom"].AsString();
            string toCode = block.Attributes["eatenTo"].AsString();
            if (fromCode == null || toCode == null) return 1f;

            if (fromCode.IndexOf(":") == -1) fromCode = block.Code.Domain + ":" + fromCode;
            if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;


            if (fromCode == null || !toCode.Contains("*"))
            {
                tblock = Api.World.GetBlock(new AssetLocation(toCode));
                if (tblock == null) return 1f;

                Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos);
                return 1f;
            }

            AssetLocation blockCode = block.WildCardReplace(new AssetLocation(fromCode), new AssetLocation(toCode));

            tblock = Api.World.GetBlock(blockCode);
            if (tblock == null) return 1f;

            Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos);
            MarkDirty(true);
            return 1f;
        }
    }
}
