using StinkySurvivalMod.BlockEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace StinkySurvivalMod.Blocks
{
    public class BlockLeechingBasin : BlockLiquidContainerBase
    {
        public override bool CanDrinkFrom => false;
        public override bool IsTopOpened => true;
        public override bool AllowHeldLiquidTransfer => false;
        public override float TransferSizeLitres => 1;

        public override int GetContainerSlotId(BlockPos pos) => 1;
        public override int GetContainerSlotId(ItemStack stack) => 1;

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            api.Logger.Notification("TryPlaceBlock BlockLeechingBasin");

            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return false;
            }

            if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
            {

                var secondBlockSel = new BlockSelection() { Position = blockSel.Position.UpCopy() };

                if (!CanPlaceBlock(world, byPlayer, secondBlockSel, ref failureCode)) { return false; }

                //place bottom
                Block orientedBlock = world.BlockAccessor.GetBlock(CodeWithParts("nobucket", "bottom"));
                orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
                //place top
                AssetLocation topBlock = CodeWithParts("nobucket", "top");
                orientedBlock = world.BlockAccessor.GetBlock(topBlock);
                orientedBlock.DoPlaceBlock(world, byPlayer, secondBlockSel, itemstack);
                return true;


            }

            return false;
        }


        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
        {
            string topbottom = LastCodePart();
            if (topbottom == "bottom")
            {
                world.BlockAccessor.SetBlock(0, pos.UpCopy());
            }
            else if (topbottom == "top")
            {
                BELeechingBasin be = world.BlockAccessor.GetBlockEntity<BELeechingBasin>(pos.DownCopy());
                be.OnBlockBroken();
                world.BlockAccessor.SetBlock(0, pos.DownCopy());
            }
            base.OnBlockRemoved(world, pos);
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            return new ItemStack(world.BlockAccessor.GetBlock(CodeWithParts("nobucket", "bottom")));
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            api.Logger.Notification("Held interacted");
            base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }
        
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            api.Logger.Notification("OnBlock interacted: {0}", Code);
            var whichblock = LastCodePart();
            var be = world.BlockAccessor.GetBlockEntity<BELeechingBasin>(whichblock == "bottom" ? blockSel.Position : blockSel.Position.DownCopy());

            if (be != null)
            {
                if (whichblock == "bottom")
                {
                    api.Logger.Notification("Bottom interacted");
                    be.InteractBucket(byPlayer, blockSel);
                    return true;
                }
                else if (whichblock == "top")
                {
                    api.Logger.Notification("Top interacted");
                    BlockSelection bottom = blockSel.Clone();
                    bottom.Position = blockSel.Position.DownCopy();
                    
                    bool handled = base.OnBlockInteractStart(world, byPlayer, bottom);
                    if (!handled)
                    {
                        be.InteractBasin(byPlayer, blockSel);
                    }
                    return true;

                }
                else
                {
                    api.Logger.Error("OnBlockInteractStart block is neither top or bottom");
                }

            }
            return false;
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            if(LastCodePart() == "top")
            {
                string info = "Leeching Basin Top\n";
                BELeechingBasin bebasin = world.BlockAccessor.GetBlockEntity<BELeechingBasin>(pos.DownCopy());
                if (bebasin?.Inventory.Empty != true) 
                {
                    if (bebasin?.Inventory[1]?.Itemstack != null)
                    {
                        info += "Contains " +  (bebasin.Inventory[1].Itemstack.StackSize / 100).ToString("0.#") + "L of " + bebasin.Inventory[1].GetStackName();
                    }
                    else
                    {
                        info += "Contains no liquids";
                    }
                }
                else
                {
                    info += "Contains no items";
                }
                return info;
            }
            else if (LastCodePart() == "bottom")
            {
                return "Leeching Basin Bottom";
            }
            else
            {
                return "Leeching Basin";
            }
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
        }

        #region ILiquidSink/Iliquidsource



        public override int TryPutLiquid(BlockPos pos, ItemStack liquidStack, float desiredLitres)
        {
            api.Logger.Notification("Tryputliquid");
            if (LastCodePart() == "bottom" || liquidStack == null) return 0;

            var props = GetContainableProps(liquidStack);

            int desiredItems = (int)(props.ItemsPerLitre * desiredLitres);
            float availItems = liquidStack.StackSize;
            float maxItems = CapacityLitres * props.ItemsPerLitre;

            ItemStack stack = GetContent(pos);
            if (stack == null)
            {
                if (props == null || !props.Containable) return 0;

                int placeableItems = (int)GameMath.Min(desiredItems, maxItems, availItems);
                int movedItems = Math.Min(desiredItems, placeableItems);

                ItemStack placedstack = liquidStack.Clone();
                placedstack.StackSize = movedItems;
                SetContent(pos, placedstack);

                return movedItems;
            }
            else
            {
                if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

                int placeableItems = (int)Math.Min(availItems, maxItems - (float)stack.StackSize);
                int movedItems = Math.Min(placeableItems, desiredItems);

                stack.StackSize += movedItems;
                api.World.BlockAccessor.GetBlockEntity(pos).MarkDirty(true);
                (api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityContainer).Inventory[1].MarkDirty();

                return movedItems;
            }
        }

        public override int TryPutLiquid(ItemStack containerStack, ItemStack liquidStack, float desiredLitres)
        {
            api.Logger.Notification("Tryputliquid");
            if (liquidStack == null || LastCodePart() == "bottom") return 0;

            var props = GetContainableProps(liquidStack);
            if (props == null) return 0;

            float epsilon = 0.00001f;
            int desiredItems = (int)(props.ItemsPerLitre * desiredLitres + epsilon);
            int availItems = liquidStack.StackSize;

            ItemStack stack = GetContent(containerStack);
            ILiquidSink sink = containerStack.Collectible as ILiquidSink;

            if (stack == null)
            {
                if (!props.Containable) return 0;

                int placeableItems = (int)(sink.CapacityLitres * props.ItemsPerLitre + epsilon);

                ItemStack placedstack = liquidStack.Clone();
                placedstack.StackSize = GameMath.Min(availItems, desiredItems, placeableItems);
                SetContent(containerStack, placedstack);

                return Math.Min(desiredItems, placeableItems);
            }
            else
            {
                if (!stack.Equals(api.World, liquidStack, GlobalConstants.IgnoredStackAttributes)) return 0;

                float maxItems = sink.CapacityLitres * props.ItemsPerLitre;
                int placeableItems = (int)(maxItems - (float)stack.StackSize);

                int moved = GameMath.Min(availItems, placeableItems, desiredItems);
                stack.StackSize += moved;
                return moved;
            }
        }

        #endregion
    }
}
