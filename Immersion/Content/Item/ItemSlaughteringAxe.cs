using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace Neolithic
{
    class ItemSlaughteringAxe : Item
    {
        public bool notslaughtering = true;
        DamageSource source = new DamageSource();

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            source.Source = EnumDamageSource.Player;
        }

        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            handling = EnumHandHandling.PreventDefault;
        }

        public override bool OnHeldAttackStep(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (notslaughtering && entitySel != null)
            {
                Entity entity = entitySel.Entity;
                if (entity.HasBehavior("slaughterable"))
                {
                    return HandAnimations.Slaughter(byEntity, secondsPassed);
                }
            }
            return false;
        }

        public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
        {
            if (entitySel != null)
            {
                Entity entity = entitySel.Entity;
                if (entity.HasBehavior("slaughterable") && notslaughtering)
                {
                    notslaughtering = false;
                    if (byEntity.World.Side.IsServer()) { entitySel.Entity.Die(EnumDespawnReason.Death, source); }

                    slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, slot);
                    slot.MarkDirty();

                    entity.World.RegisterCallback(dt =>
                    {
                        notslaughtering = true;
                    }, 2000);
                }
            }
        }
    }
}
