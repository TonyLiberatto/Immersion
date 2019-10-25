using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Neolithic
{

    public class POIEntity : BlockEntity, IPointOfInterest
    {
        public Vec3d Position => pos.ToVec3d().Add(0.5, 0.5, 0.5);
        public string Type => "genericpoi";
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
            }
        }

        public override void OnBlockRemoved()
        {
            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }

        public override void OnBlockUnloaded()
        {
            if (api.Side == EnumAppSide.Server)
            {
                api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
            }
        }
    }
}
