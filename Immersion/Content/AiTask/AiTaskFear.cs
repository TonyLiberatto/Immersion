using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    class AiTaskFear : AiTaskBase
    {
        POIRegistry poiRegistry;
        IPointOfFear runAwayFrom;
        Vec3d goTo;
        float moveSpeed;

        public AiTaskFear(EntityAgent entity) : base(entity)
        {
            poiRegistry = entity.Api.ModLoader.GetModSystem<POIRegistry>();
        }

        public override void LoadConfig(JsonObject taskConfig, JsonObject aiConfig)
        {
            if (taskConfig["movespeed"] != null)
            {
                moveSpeed = taskConfig["movespeed"].AsFloat(0.03f);
            }

            base.LoadConfig(taskConfig, aiConfig);
        }

        public override bool ShouldExecute()
        {
            runAwayFrom = null;

            runAwayFrom = (IPointOfFear)poiRegistry.GetNearestPoi(entity.ServerPos.XYZ, 1000, (poi) => 
            {
                float? fear = (poi as IPointOfFear)?.FearRadius;
                if (fear == null) return false;
                return poi.Position.DistanceTo(entity.Pos.XYZ) < fear && poi.Type == "scary";
            });

            return runAwayFrom != null;
        }

        public override bool ContinueExecute(float dt)
        {
            goTo = goTo ?? runAwayFrom.Position.AheadCopy(runAwayFrom.FearRadius + 5, 0, rand.NextDouble() * 360);
            while (goTo.AsBlockPos.GetBlock(entity.Api).Id != 0 && goTo.AsBlockPos.Y < world.BlockAccessor.MapSizeY)
            {
                goTo.Add(0, 1, 0);
            }
            pathTraverser.NavigateTo(goTo, moveSpeed, OnGoalReached, OnStuck);
            return ShouldExecute();
        }

        public void OnStuck()
        {
            goTo = null;
        }

        public void OnGoalReached()
        {
            goTo = null;
        }
    }
}
