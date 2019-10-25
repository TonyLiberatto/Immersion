using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Neolithic
{
    class BehaviorFeatherPluck : EntityBehavior
    {
        private bool notplucking = true;
        DamageSource source = new DamageSource();
        public BehaviorFeatherPluck(Entity entity) : base(entity)
        {
        }

        public override string PropertyName()
        {
            return "featherpluck";
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            source.Source = EnumDamageSource.Player;
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (notplucking && itemslot.Empty)
            {
                notplucking = false;
                ItemStack feather = new ItemStack(entity.World.GetItem(new AssetLocation("game:feather")), 1);
                feather.StackSize = entity.World.Rand.Next(1, 2);

                source.sourcePos = hitPosition;
                source.SourceEntity = byEntity;

                entity.ReceiveDamage(source, (float)((entity.World.Rand.NextDouble() * 0.25) / 2));
                if (byEntity.World.Side.IsServer())
                {
                    if (!byEntity.TryGiveItemStack(feather))
                    {
                        entity.World.SpawnItemEntity(feather, entity.Pos.XYZ);
                    }
                }

                entity.World.RegisterCallback(dt =>
                {
                    notplucking = true;
                }, 2000);
            }
        }
    }
}
