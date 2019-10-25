using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace Neolithic
{
    class BehaviorMilkable : EntityBehavior
    {
        public Item milk;
        public WaterTightContainableProps milkProps;
        ITreeAttribute tree;
        long id;
        int defaultvalue;

        public int RemainingLiters
        {
            get { return tree.GetInt("remainingliters"); }
            set { tree.SetInt("remainingliters", value); entity.WatchedAttributes.MarkPathDirty("remainingliters"); }
        }

        public double NextTimeMilkable
        {
            get { return tree.GetDouble("milkingtime"); }
            set { tree.SetDouble("milkingtime", value); entity.WatchedAttributes.MarkPathDirty("milkingtime"); }
        }

        public BehaviorMilkable(Entity entity) : base(entity)
        {
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            milk = entity.World.GetItem(new AssetLocation("milkportion"));
            milkProps = milk.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>();
            defaultvalue = attributes["startingliters"].AsInt(24);
            tree = entity.WatchedAttributes.GetTreeAttribute("milkingprops");

            if (tree == null)
            {
                entity.WatchedAttributes.SetAttribute("milkingprops", tree = new TreeAttribute());
                RemainingLiters = defaultvalue;
            }

            if (NextTimeMilkable == 0)
            {
                NextTimeMilkable = GetNextTimeMilkable();
            }
            id = entity.World.RegisterGameTickListener(MilkListener, 1000);
        }

        public override string PropertyName()
        {
            return "milkable";
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
            infotext.AppendLine("Liters Of Milk Remaining: " + RemainingLiters / milkProps.ItemsPerLitre);
        }

        public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
        {
            if (itemslot.Itemstack == null) return;
            if (itemslot.Itemstack.Block is BlockBucket)
            {
                handled = EnumHandling.PreventDefault;
                ItemStack milkstack = new ItemStack(milk);
                BlockBucket bucket = itemslot.Itemstack.Block as BlockBucket;
                ItemStack contents = bucket.GetContent(byEntity.World, itemslot.Itemstack);
                if ((contents == null || contents.Item == milk) && RemainingLiters > 0)
                {
                    if (bucket.TryPutContent(byEntity.World, itemslot.Itemstack, milkstack, 1) > 0)
                    {
                        RemainingLiters -= 1;
                        if (byEntity.World.Side == EnumAppSide.Client)
                        {
                            byEntity.World.SpawnCubeParticles(entity.Pos.XYZ + new Vec3d(0, 0.5, 0), milkstack, 0.3f, 4, 0.5f, (byEntity as EntityPlayer)?.Player);
                        }
                        if (id == 0 && RemainingLiters < defaultvalue)
                        {
                            if (NextTimeMilkable == 0)
                            {
                                NextTimeMilkable = GetNextTimeMilkable();
                            }
                            id = entity.World.RegisterGameTickListener(MilkListener, 1000);
                        }
                        itemslot.MarkDirty();
                    }
                }
            }

            base.OnInteract(byEntity, itemslot, hitPosition, mode, ref handled);
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            base.OnEntityDeath(damageSourceForDeath);
            if (id != 0)
            {
                entity.World.UnregisterGameTickListener(id);
            }
        }

        public override void OnEntityDespawn(EntityDespawnReason despawn)
        {
            base.OnEntityDespawn(despawn);
            if (id != 0)
            {
                entity.World.UnregisterGameTickListener(id);
            }
        }

        public double GetNextTimeMilkable()
        {
            return entity.World.Calendar.TotalHours + 12 + (entity.World.Rand.NextDouble() * 8);
        }

        public void MilkListener(float dt)
        {
            if (entity.World.Calendar.TotalHours > NextTimeMilkable)
            {
                RemainingLiters = defaultvalue;
                NextTimeMilkable = GetNextTimeMilkable();
                entity.World.UnregisterGameTickListener(id);
                id = 0;
            }
        }

        public float GetSaturation()
        {
            ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
            if (tree == null) return 0;

            return tree.GetFloat("saturation", 0);
        }
    }
}
