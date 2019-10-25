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

namespace Neolithic
{
    public class NeolithicContentConfig : ContentConfig
    {
    }

    public class NeolithicTransient : BlockEntityTransient, IAnimalFoodSource
    {
        public Vec3d Position => pos.ToVec3d().Add(0.5, 0.5, 0.5);
        public string Type => "food";
        NeolithicContentConfig[] nltConfig;
        Block ownBlock;
        public string contentCode = "";
        double transitionAtTotalDays = -1;
        public static SimpleParticleProperties Flies;
        public bool flies = true;

        static NeolithicTransient()
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

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            ownBlock = api.World.BlockAccessor.GetBlock(pos);

            ownBlock = ownBlock == null ? api.World.BlockAccessor.GetBlock(pos) : ownBlock;
            flies = ownBlock.Attributes["flies"].AsBool(true);


            if (nltConfig == null)
            {
                nltConfig = ownBlock.Attributes["contentConfig"].AsObject<NeolithicContentConfig[]>();
            }

            if (transitionAtTotalDays <= 0)
            {
                float hours = ownBlock.Attributes["inGameHours"].AsFloat(24);
                transitionAtTotalDays = api.World.Calendar.TotalDays + hours / 24;
            }

            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
                RegisterGameTickListener(CheckTransition, 2000);
            }
            if (api.Side == EnumAppSide.Client && flies)
            {
                RegisterGameTickListener(FliesTick, 100);
            }

        }

        private void FliesTick(float dt)
        {
            if (api.Side == EnumAppSide.Client && api.World.Calendar.DayLightStrength > 0.5 )
            {
                double modx = api.World.Rand.NextDouble();
                double modz = api.World.Rand.NextDouble();
                double mody = api.World.Rand.NextDouble();

                int modc = (int)((modx + mody + modz / 3) * 25);

                Flies.minPos.Set(pos.X + modx, pos.Y + mody, pos.Z + modz);
                Flies.color = ColorUtil.ToRgba(100, 0, modc, modc);
                Flies.glowLevel = (byte)(api.World.Calendar.DayLightStrength * 50);

                api.World.SpawnParticles(Flies);
            }
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
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
            Block block = api.World.BlockAccessor.GetBlock(pos);
            Block tblock;

            if (block.Attributes == null) return 1f;

            string fromCode = block.Attributes["convertFrom"].AsString();
            string toCode = block.Attributes["eatenTo"].AsString();
            if (fromCode == null || toCode == null) return 1f;

            if (fromCode.IndexOf(":") == -1) fromCode = block.Code.Domain + ":" + fromCode;
            if (toCode.IndexOf(":") == -1) toCode = block.Code.Domain + ":" + toCode;


            if (fromCode == null || !toCode.Contains("*"))
            {
                tblock = api.World.GetBlock(new AssetLocation(toCode));
                if (tblock == null) return 1f;

                api.World.BlockAccessor.SetBlock(tblock.BlockId, pos);
                return 1f;
            }

            AssetLocation blockCode = block.WildCardReplace(new AssetLocation(fromCode), new AssetLocation(toCode));

            tblock = api.World.GetBlock(blockCode);
            if (tblock == null) return 1f;

            api.World.BlockAccessor.SetBlock(tblock.BlockId, pos);
            MarkDirty(true);
            return 1f;
        }
    }
}
