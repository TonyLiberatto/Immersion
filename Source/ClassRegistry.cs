using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace Immersion
{
    public partial class Immersion
    {
        public void RegisterClasses()
        {
            RegisterBlockBehaviors();
            RegisterBlocks();
            RegisterItems();
            RegisterBlockEntities();
            RegisterBlockEntityBehaviors();
            RegisterAiTasks();
            RegisterEntityBehaviors();
        }

        public void RegisterEntityBehaviors()
        {
            Api.RegisterEntityBehaviorClass("milkable", typeof(BehaviorMilkable));
            Api.RegisterEntityBehaviorClass("slaughterable", typeof(BehaviorSlaughterable));
            Api.RegisterEntityBehaviorClass("featherpluck", typeof(BehaviorFeatherPluck));
            Api.RegisterEntityBehaviorClass("damagenotify", typeof(BehaviorNotifyHerdOfDamage));
            Api.RegisterEntityBehaviorClass("portiongrow", typeof(BehaviorPortionGrow));
        }

        public void RegisterAiTasks()
        {
            AiTaskRegistry.Register("sleep", typeof(AiTaskSleep));
            AiTaskRegistry.Register("fear", typeof(AiTaskFear));
        }

        public void RegisterBlockEntities()
        {
            Api.RegisterBlockEntityClass("BEMortarAndPestle", typeof(BlockEntityMortarAndPestle));
            Api.RegisterBlockEntityClass("BlockEntityChimney", typeof(BlockEntityChimney));
            Api.RegisterBlockEntityClass("ImmersionRoads", typeof(BEImmersionRoads));
            Api.RegisterBlockEntityClass("CraftingStation", typeof(BlockEntityCraftingStation));
            Api.RegisterBlockEntityClass("DryingStation", typeof(BlockEntityDryingStation));
            Api.RegisterBlockEntityClass("PalmTree", typeof(BEPalmTree));
            Api.RegisterBlockEntityClass("Stairs", typeof(BlockEntityStairs));
            Api.RegisterBlockEntityClass("IngotPileOverride", typeof(IngotPileOverride));
            Api.RegisterBlockEntityClass("Well", typeof(BlockEntityWell));
        }

        public void RegisterBlockEntityBehaviors()
        {
            Api.RegisterBlockEntityBehaviorClass("Consumable", typeof(BEBehaviorConsumable));
            Api.RegisterBlockEntityBehaviorClass("Transient", typeof(BEBehaviorTransient));
            Api.RegisterBlockEntityBehaviorClass("Scary", typeof(BEBehaviorScary));
            Api.RegisterBlockEntityBehaviorClass("FirepitScary", typeof(BEBehaviorFirepitScary));
        }

        public void RegisterItems()
        {
            Api.RegisterItemClass("ItemSickle", typeof(ItemSickle));
            Api.RegisterItemClass("ItemGiantReedsRoot", typeof(ItemGiantReedsRoot));
            Api.RegisterItemClass("ItemAdze", typeof(ItemAdze));            
            Api.RegisterItemClass("ItemChisel", typeof(ItemChiselFix));
            Api.RegisterItemClass("ItemSwapBlocks", typeof(ItemSwapBlocks));
            Api.RegisterItemClass("ItemSlaughteringAxe", typeof(ItemSlaughteringAxe));
            Api.RegisterItemClass("ItemFoodMultiProp", typeof(ItemMultiPropFood));
            Api.RegisterItemClass("ItemProspectingPick", typeof(ItemPropick));
            Api.RegisterItemClass("ItemIngot", typeof(ItemIngotOverride));
        }

        public void RegisterBlocks()
        {
            Api.RegisterBlockClass("BlockGiantReeds", typeof(BlockGiantReeds));
            Api.RegisterBlockClass("BlockMortarAndPestle", typeof(BlockMortarAndPestle));
            Api.RegisterBlockClass("BlockCheeseCloth", typeof(BlockCheeseCloth));
            Api.RegisterBlockClass("BlockImmersionRoads", typeof(BlockImmersionRoads));
            Api.RegisterBlockClass("BlockLooseStones", typeof(BlockLooseStonesModified)); //
            Api.RegisterBlockClass("FixedStairs", typeof(FixedStairs));
            Api.RegisterBlockClass("BlockChandelier", typeof(BlockChandelierFix));
            Api.RegisterBlockClass("BlockToolMold", typeof(BlockToolMoldReturnBlock)); //
            Api.RegisterBlockClass("BlockCraftingStation", typeof(BlockCraftingStation));
            Api.RegisterBlockClass("BlockDryingStation", typeof(BlockDryingStation));
            Api.RegisterBlockClass("BlockSeaweed", typeof(BlockSeaweedOverride)); //
            Api.RegisterBlockClass("BlockPalmTree", typeof(BlockPalmTree));
            Api.RegisterBlockClass("BlockIngotPile", typeof(BlockIngotPileOverride)); //
            Api.RegisterBlockClass("BlockWell", typeof(BlockWell));
            Api.RegisterBlockClass("BlockWellPipe", typeof(BlockWellPipe));
        }

        public void RegisterBlockBehaviors()
        {
            Api.RegisterBlockBehaviorClass("BlockCreateBehavior", typeof(BlockCreateBehavior));
            Api.RegisterBlockBehaviorClass("BlockSwapBehavior", typeof(BlockSwapBehavior));
            Api.RegisterBlockBehaviorClass("LampConnectorBehavior", typeof(LampConnectorBehavior));
            Api.RegisterBlockBehaviorClass("LampPostBehavior", typeof(LampPostBehavior));
            Api.RegisterBlockBehaviorClass("RotateNinety", typeof(RotateNinety));
            Api.RegisterBlockBehaviorClass("ChimneyBehavior", typeof(ChimneyBehavior));
        }
    }
}
