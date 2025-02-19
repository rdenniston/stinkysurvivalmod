using Cairo;
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
using Vintagestory.Common;

namespace StinkySurvivalMod.gui
{
    public class GuiDialogBEFireplace : GuiDialogBlockEntity
    {
        long lastRedrawMs;
        EnumPosFlag screenPos;
        protected override double FloatyDialogPosition => 0.6;
        protected override double FloatyDialogAlign => 0.8;

        public GuiDialogBEFireplace(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, SyncedTreeAttribute tree, ICoreClientAPI capi) 
            : base(DialogTitle, Inventory, BlockEntityPosition, capi) 
        {
            if (IsDuplicate) return;
            tree.OnModified.Add(new TreeModifiedListener() { listener = OnAttributesModified });
            Attributes = tree;
            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            SetupDialog();
        }

        private void OnAttributesModified()
        {
            if (!IsOpened()) return;

            float ftemp = Attributes.GetFloat("currentTemperature");
            string fuelTemp = ftemp.ToString("#");
            fuelTemp += fuelTemp.Length > 0 ? "°C" : "";
            if (ftemp > 0 && ftemp <= 20) fuelTemp = Lang.Get("Cold");

            float gametime = Attributes.GetFloat("gameTimeRemaining");
            TimeSpan gtime = TimeSpan.FromSeconds(gametime);

       
            if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
            {
                if (SingleComposer != null)
                {
                    SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
                    SingleComposer.GetDynamicText("fueltemp").SetNewText(fuelTemp);
                    SingleComposer.GetDynamicText("gametime").SetNewText(Lang.Get("stinkysurvivalmod:gtime-remaining") + gtime.ToString(@"\~dd\d\:hh\h"));

                }

                lastRedrawMs = capi.ElapsedMilliseconds;
            }
        }

        private void OnInventorySlotModified(int slotid)
        {
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupfireplcdlg");
        }

        void SetupDialog()
        {
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }
            string newOutputText = Attributes.GetString("outputText", "");
            GuiElementDynamicText outputTextElem;

            string currentOutputText = newOutputText;

            ElementBounds stoveBounds = ElementBounds.Fixed(0, 0, 210, 250);

            ElementBounds fuelSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 85, 1, 1);
            ElementBounds outputSlotBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 140, 1, 1);

            // 2. Around all that is 10 pixel padding
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(stoveBounds);

            // 3. Finally Dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog
                .WithFixedAlignmentOffset(IsRight(screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0)
                .WithAlignment(IsRight(screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle)
            ;


            if (!capi.Settings.Bool["immersiveMouseMode"])
            {
                dialogBounds.fixedOffsetY += (stoveBounds.fixedHeight + 65 ) * YOffsetMul(screenPos);
                dialogBounds.fixedOffsetX += (stoveBounds.fixedWidth + 10) * XOffsetMul(screenPos);
            }

            SingleComposer = capi.Gui
                .CreateCompo("blockentityfireplace" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)
                    .AddDynamicCustomDraw(stoveBounds, OnBgDraw, "symbolDrawer")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, fuelSlotBounds, "fuelSlot")
                    .AddDynamicText("", CairoFont.WhiteDetailText(), fuelSlotBounds.RightCopy(17, 16).WithFixedSize(60, 30), "fueltemp")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, outputSlotBounds, "outputslot")
                    .AddDynamicText("", CairoFont.WhiteDetailText(), outputSlotBounds.RightCopy(5, 0).WithFixedSize(120, 60), "gametime")
                    .AddDynamicText("", CairoFont.WhiteDetailText(), ElementBounds.Fixed(0,200, 210, 45), "outputText")
                .EndChildElements()
                .Compose();

            lastRedrawMs = capi.ElapsedMilliseconds;

            if (hoveredSlot != null)
            {
                SingleComposer.OnMouseMove(new MouseEvent(capi.Input.MouseX, capi.Input.MouseY));
            }

            outputTextElem = SingleComposer.GetDynamicText("outputText");
            outputTextElem.SetNewText(currentOutputText, true);
            outputTextElem.Bounds.fixedOffsetY = 0;

            if (outputTextElem.QuantityTextLines > 2)
            {
                outputTextElem.Bounds.fixedOffsetY = -outputTextElem.Font.GetFontExtents().Height / RuntimeEnv.GUIScale * 0.65;
                outputTextElem.Font.WithFontSize(12);
                outputTextElem.RecomposeText();
            }
            outputTextElem.Bounds.CalcWorldBounds();
        }

        float inputBurnTime;
        float maxBurnTime;

        public void Update(float inputBurnTime, float maxBurnTime)
        {
            this.inputBurnTime = inputBurnTime;
            this.maxBurnTime = maxBurnTime;

            if (!IsOpened()) { return; }

            if (capi.ElapsedMilliseconds > lastRedrawMs)
            {
                if (SingleComposer != null) SingleComposer.GetCustomDraw("symbolDrawer").Redraw();
                lastRedrawMs = capi.ElapsedMilliseconds;
            }
        }

        private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds) 
        {
            double top = 0;

            ctx.Save();
            Matrix m = ctx.Matrix;
            m.Translate(GuiElement.scaled(5), GuiElement.scaled(25 + top));
            m.Scale(GuiElement.scaled(0.25), GuiElement.scaled(0.25));
            ctx.Matrix = m;
            capi.Gui.Icons.DrawFlame(ctx);

            double dy = 210 - 210 * (Attributes.GetFloat("fuelBurnTime", 0) / Attributes.GetFloat("maxFuelBurnTime", 1));
            ctx.Rectangle(0, dy, 200, 210 - dy);
            ctx.Clip();
            LinearGradient gradient = new LinearGradient(0, GuiElement.scaled(250), 0, 0);
            gradient.AddColorStop(0, new Color(1, 1, 0, 1));
            gradient.AddColorStop(1, new Color(1, 0, 0, 1));
            ctx.SetSource(gradient);
            capi.Gui.Icons.DrawFlame(ctx, 0, false, false);
            gradient.Dispose();
            ctx.Restore();
            
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

            SingleComposer.GetSlotGrid("fuelSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("outputslot").OnGuiClosed(capi);

            base.OnGuiClosed();
        }

    }
}
