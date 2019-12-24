using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class ItemMultiPropFood : Item
    {
        public FoodNutritionProperties[] props { get => Attributes["NutritionProps"].AsObject<FoodNutritionProperties[]>(); }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (byEntity.World.Side.IsServer())
            {
                EntityBehaviorHunger behaviorHunger = byEntity.GetBehavior<EntityBehaviorHunger>();
                if (behaviorHunger != null && behaviorHunger.Saturation < behaviorHunger.MaxSaturation)
                {
                    int i = 0;
                    foreach (var val in props)
                    {
                        if (ConsumeProp(secondsUsed, slot, byEntity, val)) i++;
                    }
                    if (i > 0) slot.Itemstack.StackSize--;
                    slot.MarkDirty();
                }
            }
        }

        public bool ConsumeProp(float secondsUsed, ItemSlot slot, EntityAgent byEntity, FoodNutritionProperties prop)
        {
            if (prop != null && secondsUsed >= 0.95f)
            {
                TransitionState state = UpdateAndGetTransitionState(api.World, slot, EnumTransitionType.Perish);
                float spoilState = state != null ? state.TransitionLevel : 0;

                float satLossMul = GlobalConstants.FoodSpoilageSatLossMul(spoilState, slot.Itemstack, byEntity);
                float healthLossMul = GlobalConstants.FoodSpoilageHealthLossMul(spoilState, slot.Itemstack, byEntity);

                byEntity.ReceiveSaturation(prop.Satiety * satLossMul, prop.FoodCategory);

                if (prop.EatenStack != null)
                {
                    IPlayer player = null;
                    if (byEntity is EntityPlayer) player = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

                    if (player == null || !player.InventoryManager.TryGiveItemstack(prop.EatenStack.ResolvedItemstack.Clone(), true))
                    {
                        byEntity.World.SpawnItemEntity(prop.EatenStack.ResolvedItemstack.Clone(), byEntity.LocalPos.XYZ);
                    }
                }

                float healthChange = prop.Health * healthLossMul;

                if (healthChange != 0)
                {
                    byEntity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Internal, Type = healthChange > 0 ? EnumDamageType.Heal : EnumDamageType.Poison }, Math.Abs(healthChange));
                }
                return true;
            }
            return false;
        }

        public override FoodNutritionProperties GetNutritionProperties(IWorldAccessor world, ItemStack itemstack, Entity forEntity)
        {
            return props.FirstOrDefault();
        }
    }
}
