using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{

    public class BEScary : BlockEntityTransient, IPointOfInterest
    {

        public Vec3d Position => Pos.ToVec3d().Add(0.5,0.5,0.5);
        public string Type => "scary";
        public override void Initialize(ICoreAPI Api)
        {
            base.Initialize(Api);
            if (Api.Side == EnumAppSide.Server)
            {
                //Api.World.Logger.Notification("AddedPOI");
                Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
            }
        }

        public override void OnBlockRemoved()
        {
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
    }
}
