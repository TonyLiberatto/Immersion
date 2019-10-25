using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace Neolithic
{
    public partial class TheNeolithicMod
    {
        public void RegisterClasses()
        {
            RegisterBlockBehaviors();
            RegisterBlocks();
            RegisterItems();
            RegisterBlockEntities();
            RegisterAiTasks();
            RegisterEntityBehaviors();
        }

        public void RegisterEntityBehaviors()
        {
            api.RegisterEntityBehaviorClass("milkable", typeof(BehaviorMilkable));
            api.RegisterEntityBehaviorClass("slaughterable", typeof(BehaviorSlaughterable));
            api.RegisterEntityBehaviorClass("featherpluck", typeof(BehaviorFeatherPluck));
            api.RegisterEntityBehaviorClass("damagenotify", typeof(BehaviorNotifyHerdOfDamage));
            api.RegisterEntityBehaviorClass("portiongrow", typeof(BehaviorPortionGrow));
        }

        public void RegisterAiTasks()
        {
            AiTaskRegistry.Register("sleep", typeof(AiTaskSleep));
            AiTaskRegistry.Register("neolithicseekfoodandeat", typeof(FixedAiTaskSeekFoodAndEat));
        }

        public void RegisterBlockEntities()
        {
            api.RegisterBlockEntityClass("NeolithicTransient", typeof(NeolithicTransient));
            api.RegisterBlockEntityClass("FixedBESapling", typeof(FixedBESapling));
            api.RegisterBlockEntityClass("BEMortarAndPestle", typeof(BEMortarAndPestle));
            api.RegisterBlockEntityClass("BlockEntityChimney", typeof(BlockEntityChimney));
            api.RegisterBlockEntityClass("BucketB", typeof(BEBucketOverride));
            api.RegisterBlockEntityClass("NeolithicRoads", typeof(BENeolithicRoads));
            api.RegisterBlockEntityClass("CraftingStation", typeof(BlockEntityCraftingStation));
            api.RegisterBlockEntityClass("DryingStation", typeof(BlockEntityDryingStation));
            api.RegisterBlockEntityClass("PalmTree", typeof(BEPalmTree));
            api.RegisterBlockEntityClass("Stairs", typeof(BlockEntityStairs));
        }

        public void RegisterItems()
        {
            api.RegisterItemClass("ItemSickle", typeof(ItemSickle));
            api.RegisterItemClass("ItemGiantReedsRoot", typeof(ItemGiantReedsRoot));
            api.RegisterItemClass("ItemAdze", typeof(ItemAdze));            
            api.RegisterItemClass("ItemChisel", typeof(ItemChiselFix));
            api.RegisterItemClass("ItemSwapBlocks", typeof(ItemSwapBlocks));
            api.RegisterItemClass("ItemSlaughteringAxe", typeof(ItemSlaughteringAxe));
        }

        public void RegisterBlocks()
        {
            api.RegisterBlockClass("BlockGiantReeds", typeof(BlockGiantReeds));
            api.RegisterBlockClass("BlockMortarAndPestle", typeof(BlockMortarAndPestle));
            api.RegisterBlockClass("BlockCheeseCloth", typeof(BlockCheeseCloth));
            api.RegisterBlockClass("BlockNeolithicRoads", typeof(BlockNeolithicRoads));
            api.RegisterBlockClass("BlockLooseStones", typeof(BlockLooseStonesModified)); //
            api.RegisterBlockClass("FixedStairs", typeof(FixedStairs));
            api.RegisterBlockClass("BlockChandelier", typeof(BlockChandelierFix));
            api.RegisterBlockClass("BlockToolMold", typeof(BlockToolMoldReturnBlock)); //
            api.RegisterBlockClass("BlockCraftingStation", typeof(BlockCraftingStation));
            api.RegisterBlockClass("BlockDryingStation", typeof(BlockDryingStation));
            api.RegisterBlockClass("BlockSeaweed", typeof(BlockSeaweedOverride)); //
            api.RegisterBlockClass("BlockPalmTree", typeof(BlockPalmTree));
        }

        public void RegisterBlockBehaviors()
        {
            api.RegisterBlockBehaviorClass("BlockCreateBehavior", typeof(BlockCreateBehavior));
            api.RegisterBlockBehaviorClass("BlockSwapBehavior", typeof(BlockSwapBehavior));
            api.RegisterBlockBehaviorClass("LampConnectorBehavior", typeof(LampConnectorBehavior));
            api.RegisterBlockBehaviorClass("LampPostBehavior", typeof(LampPostBehavior));
            api.RegisterBlockBehaviorClass("RotateNinety", typeof(RotateNinety));
            api.RegisterBlockBehaviorClass("ChimneyBehavior", typeof(ChimneyBehavior));
        }
    }
}
