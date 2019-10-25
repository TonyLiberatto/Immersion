using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Neolithic
{
    public class BehaviorPortionGrow : EntityBehavior
    {
        ITreeAttribute growTree;
        JsonObject typeAttributes;
        long callbackId;

        internal float HoursToGrow
        {
            get { return typeAttributes["hoursToGrow"].AsFloat(96); }
        }

        internal float SatietyToGrow
        {
            get { return typeAttributes["portionsEatenForGrow"].AsFloat(30); }
        }

        internal AssetLocation[] AdultEntityCodes
        {
            get { return AssetLocation.toLocations(typeAttributes["adultEntityCodes"].AsArray(new string[0])); }
        }

        internal double TimeSpawned
        {
            get { return growTree.GetDouble("timeSpawned"); }
            set { growTree.SetDouble("timeSpawned", value); }
        }

        public BehaviorPortionGrow(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
        {
            base.Initialize(properties, typeAttributes);

            this.typeAttributes = typeAttributes;

            growTree = entity.WatchedAttributes.GetTreeAttribute("grow");

            if (growTree == null)
            {
                entity.WatchedAttributes.SetAttribute("grow", growTree = new TreeAttribute());
                TimeSpawned = entity.World.Calendar.TotalHours;
            }

            callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
        }


        private void CheckGrowth(float dt)
        {
            if (!entity.Alive) return;
            ITreeAttribute hungerTree = entity.WatchedAttributes.GetOrAddTreeAttribute("hunger");

            if (entity.World.Calendar.TotalHours >= TimeSpawned + HoursToGrow && hungerTree.GetFloat("saturation", 0) >= SatietyToGrow)
            {
                AssetLocation[] entityCodes = AdultEntityCodes;
                if (entityCodes.Length == 0) return;
                AssetLocation code = entityCodes[entity.World.Rand.Next(entityCodes.Length)];

                EntityProperties adultType = entity.World.GetEntityType(code);

                if (adultType == null)
                {
                    entity.World.Logger.Error("Misconfigured entity. Entity with code '{0}' is configured (via Grow behavior) to grow into '{1}', but no such entity type was registered.", entity.Code, code);
                    return;
                }

                Cuboidf collisionBox = adultType.SpawnCollisionBox;

                // Delay adult spawning if we're colliding
                if (entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, collisionBox, entity.ServerPos.XYZ, false))
                {
                    callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
                    return;
                }

                Entity adult = entity.World.ClassRegistry.CreateEntity(adultType);

                adult.ServerPos.SetFrom(entity.ServerPos);
                adult.Pos.SetFrom(adult.ServerPos);

                entity.Die(EnumDespawnReason.Expire, null);
                entity.World.SpawnEntity(adult);

                adult.WatchedAttributes.SetInt("generation", entity.WatchedAttributes.GetInt("generation", 0));
            }
            else
            {
                callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
            }

            entity.World.FrameProfiler.Mark("entity-checkgrowth");
        }


        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            entity.World.UnregisterCallback(callbackId);
        }

        public override string PropertyName() => "portionGrow";
    }
}
