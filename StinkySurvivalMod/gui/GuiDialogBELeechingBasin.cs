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

namespace StinkySurvivalMod.gui
{
    internal class GuiDialogBELeechingBasin : GuiDialogBlockEntity
    {
        protected override double FloatyDialogPosition => 0.6;
        protected ItemStack bucketslot;
        protected BELeechingBasin be;
        protected override double FloatyDialogAlign => 0.8;
        long lastRedrawMs;
    
        public GuiDialogBELeechingBasin(string DialogTitle, InventoryBase Inventory, BlockPos pos, ICoreClientAPI capi)
            :base(DialogTitle, Inventory, pos, capi) 
        {
            if (IsDuplicate) return;
            be = capi.World.BlockAccessor.GetBlockEntity<BELeechingBasin>(pos);
            SyncedTreeAttribute tree = new SyncedTreeAttribute();
            tree.OnModified.Add(new TreeModifiedListener()
            {
                listener = OnAttributesModified
            });
            Attributes = tree;
            capi.World.Player.InventoryManager.OpenInventory(Inventory);
            SetupDialog();
        }

        private void OnAttributesModified()
        {
            if (!IsOpened()) return;
            if (capi.ElapsedMilliseconds - lastRedrawMs > 100)
            {
                if (SingleComposer != null)
                {
                    SingleComposer.GetDynamicText("outputText").SetNewText(Attributes.GetString("outputStr",""));
                    //SingleComposer.GetDynamicText("gametime").SetNewText(Lang.Get("stinkysurvivalmod:gtime-remaining") + gtime.ToString(@"\~dd\d\:hh\h"));

                }

                lastRedrawMs = capi.ElapsedMilliseconds;
            }
        }

            private void OnInventorySlotModified(int slotid)
        {
            //stinkysurvivalmod:saltedthatch stinkysurvivalmod:woodash
            //lazy - maybe if leeching basin gets popular I convert to recipes

            string itemcode = Inventory[slotid]?.Itemstack?.Collectible?.Code?.ToString();
            if ( itemcode != null && slotid ==0)
            {
                BlockLiquidContainerBase cntBlock = Inventory[2]?.Itemstack?.Collectible as BlockLiquidContainerBase;
                int numitems = Inventory[slotid].Itemstack.StackSize;
                if (cntBlock != null)
                {
                    
                    
                    bucketslot = Inventory[2]?.Itemstack;
                    var contents = cntBlock.GetContents(capi.World, bucketslot);
                    if (contents != null) capi.Logger.Notification("Code: " + contents[0]?.Collectible?.Code?.ToString());
                }

                if (itemcode == "stinkysurvivalmod:saltedthatch")
                {
                    Attributes.SetString("outputStr", Lang.Get("stinkysurvivalmod:saltedthatch-gui-recipe", (numitems*2).ToString(), numitems.ToString(), numitems.ToString()));
                    
                }
                else
                if (itemcode == "stinkysurvivalmod:woodash")
                {
                    Attributes.SetString("outputStr", Lang.Get("stinkysurvivalmod:woodash-gui-recipe", (numitems * 2).ToString(), numitems.ToString(), numitems.ToString()));
                }
                else { Attributes.SetString("outputStr", ""); Attributes.SetString("warningStr", "");  }
            } else { Attributes.SetString("outputStr", ""); ; Attributes.SetString("warningStr", "");  }

           // capi.Event.EnqueueMainThreadTask(SetupDialog, "setupleechdlg");

        }

        void SetupDialog()
        {
            var outputStr = "";
            var warningStr = "";
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            string itemcode = Inventory[0]?.Itemstack?.Collectible?.Code?.ToString();
            BlockLiquidContainerBase cntBucket = Inventory[2]?.Itemstack?.Collectible as BlockLiquidContainerBase;


            bucketslot = Inventory[2]?.Itemstack;
            if (cntBucket != null && bucketslot != null) 
            {
                
                var contents = cntBucket.GetContents(capi.World, bucketslot);
                if (cntBucket.IsFull(bucketslot))
                {
                    warningStr = "Warning: Attached container is full! Leech liquids will be lost!";
                }
                
                if (contents != null && itemcode != null) 
                {
                    if (contents[0]?.Collectible?.Code?.ToString() != "game:waterportion") { }
                }
            }
            else
            {
                warningStr = "Warning: No Liquid Container Attached!";
            }
            if (itemcode != null)
            {
                int numitems = Inventory[0].Itemstack.StackSize;
                if (itemcode == "stinkysurvivalmod:saltedthatch")
                {
                    outputStr = Lang.Get("stinkysurvivalmod:saltedthatch-gui-recipe", (numitems * 2).ToString(), numitems.ToString(), numitems.ToString());


                }
                else
                if (itemcode == "stinkysurvivalmod:woodash")
                {
                    outputStr = Lang.Get("stinkysurvivalmod:woodash-gui-recipe", (numitems * 2).ToString(), numitems.ToString(), numitems.ToString());

                }
            }

            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }

            capi.Logger.Notification("SetupDialog outputstr: "+outputStr);
            ElementBounds leechBounds = ElementBounds.Fixed(0, 0, 150, 190);
            ElementBounds leechDesc = ElementBounds.Fixed(0,15,150, 50);
            ElementBounds outputBounds = ElementBounds.Fixed(0, 120, 300, 80);
            ElementBounds warnings = ElementBounds.Fixed(0, 200, 300, 40);
            ElementBounds inputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.CenterMiddle, 0, -10, 1, 1);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(leechBounds);

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo("blockentityleechingbasin" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddStaticTextAutoBoxSize("Add wood ash to make lye or salted thatch \n(then add lye) to make hydrated saltpeter", CairoFont.WhiteDetailText(),EnumTextOrientation.Center, leechDesc)
                    .AddItemSlotGridExcl(Inventory, SendInvPacket,1,new int[] {1,2}, inputSlotBounds, "inputSlot")
                    .AddDynamicText(outputStr, CairoFont.WhiteDetailText(), outputBounds, "outputText")
                    .AddDynamicText(Attributes.GetString("warningStr", ""), CairoFont.WhiteDetailText(), outputBounds, "warnings")
                .EndChildElements()
                .Compose();

            lastRedrawMs = capi.ElapsedMilliseconds;
        }


        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            Inventory.SlotModified += OnInventorySlotModified;

        }

        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;
            GuiElementItemSlotGridExcl ele = SingleComposer.GetSlotGridExcl("inputSlot");
            ele.OnGuiClosed(capi);

            base.OnGuiClosed();
        }
    }
}
