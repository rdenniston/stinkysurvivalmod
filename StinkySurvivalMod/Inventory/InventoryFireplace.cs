using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace StinkySurvivalMod.Inventory
{
    public class InventoryFireplace : InventoryBase, ISlotProvider
    {
        ItemSlot[] slots;
        public ItemSlot[] Slots { get { return slots; } }

        public InventoryFireplace(string inventoryID, ICoreAPI api) : base(inventoryID, api) 
        {

            //slot 0 input
            //slot 1 output
            slots = GenEmptySlots(2);
        }

        public InventoryFireplace(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api) 
        {
            slots = GenEmptySlots(2);
        }

        public override int Count
        {
            get { return 2; }
        }

        public override ItemSlot this[int slotId] 
        {
            get
            {
                if (slotId < 0 || slotId >= Count) return null;
                return slots[slotId];
            }
            set 
            {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                if (value == null) throw new ArgumentNullException(nameof(value));
                slots[slotId] = value;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }

        protected override ItemSlot NewSlot(int i)
        {
            return new ItemSlotSurvival(this);
        }

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == slots[0] && (sourceSlot.Itemstack.Collectible.CombustibleProps != null || sourceSlot.Itemstack.Collectible.CombustibleProps.BurnTemperature >= 0)) return 4f;
            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        }

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            return slots[0];
        }

        public string GetOutputText()
        {
            ItemStack inputStack = slots[0].Itemstack;
            if (inputStack == null) { return Lang.Get("stinkysurvivalmod:fireplace-gui-instructions"); }
            if (inputStack.Collectible.CombustibleProps != null)
            {
                if (inputStack.Collectible.CombustibleProps.BurnDuration != null && inputStack.Collectible.CombustibleProps.BurnDuration > 10)
                {
                    return Lang.Get("stinkysurvivalmod:fireplace-gui-willcreate", inputStack.StackSize) +" "+ Lang.Get("stinkysurvivalmod:fireplace-gui-ashes-name");
                }
            }
            return Lang.Get("stinkysurvivalmod:fireplace-gui-cantuse");
           

        }
    }
}
