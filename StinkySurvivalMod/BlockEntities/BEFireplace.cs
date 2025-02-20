using StinkySurvivalMod.gui;
using StinkySurvivalMod.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace StinkySurvivalMod.BlockEntities
{
    internal class BEFireplace : BlockEntityOpenableContainer, IHeatSource, IFirePit
    {
        internal InventoryFireplace inventory;
        public float fuelBurnTime;
        bool clientSidePrevBurning;
        public float maxFuelBurnTime;
        public float previousTemperature;
        public float currentTemperature;
        public int maxTemperature;
        public bool canIgniteFuel;
        public double extinguishedTotalHours;
        public float smokeLevel;
        public float cachedFuel;
        bool shouldRedraw;

        GuiDialogBEFireplace clientDialog;
        bool clientSidePreventBurning;

        public bool IsHot => IsBurning;
        public float emptyFireplaceBurnTimeMulBonus = 4f;

        #region Config

        public virtual bool BurnsAllFuel { get { return true; } }

        public virtual float BurnDurationModifier { get { return 6.0f; }
        }
        public virtual string DialogTitle { get { return Lang.Get("Fireplace"); } }

        public override string InventoryClassName { get { return "fireplace"; } }

        public override InventoryBase Inventory { get { return inventory; } }

        public virtual int environmentTemperature(){ return 20; }
        #endregion



        public bool IsBurning { get { return this.fuelBurnTime > 0; } }
        public bool IsSmoldering => canIgniteFuel;

        public BEFireplace()
        {
            inventory = new InventoryFireplace(null, null);
            inventory.SlotModified += OnSlotModified;
            currentTemperature = previousTemperature = environmentTemperature();

        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            inventory.Pos = Pos;
            inventory.LateInitialize("fireplace-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

            RegisterGameTickListener(OnBurnTick, 100);
            RegisterGameTickListener(On500msTick, 500);

            if (api is ICoreClientAPI) 
            {
            //todo: maybe impl renderer
            }

        }


        private void OnSlotModified(int slotid)
        {
            Block = Api.World.BlockAccessor.GetBlock(Pos);

            //UpdateRenderer();
            MarkDirty(Api.Side == EnumAppSide.Server);
            shouldRedraw = true;

            if (Api is ICoreClientAPI && clientDialog != null) 
            {
                SetDialogValues(clientDialog.Attributes);

            }

            Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
        }

        private void On500msTick(float dt) 
        { 
            if(Api is ICoreServerAPI && (IsBurning || previousTemperature != currentTemperature))
            {
                MarkDirty();
            }

            previousTemperature = currentTemperature;
        }

        private void OnBurnTick(float dt)
        {
            if (Block.Code.Path.EndsWith("unlit")) return;

            if (Api is ICoreClientAPI)
            {
                //maybe renderer
                return;
            }

            if (fuelBurnTime > 0)
            {

                fuelBurnTime -= dt;

                if (fuelBurnTime <= 0)
                {
                    fuelBurnTime = 0;
                    maxFuelBurnTime = 0;
                    extinguishedTotalHours = Api.World.Calendar.TotalHours;
                    setBlockState("extinct");
                }
            
                
            }

            if (!IsBurning && Block.Variant["burnstate"] == "extinct" && Api.World.Calendar.TotalHours - extinguishedTotalHours > 3)
            {
                canIgniteFuel = false;
                setBlockState("cold");
            }

            if (IsBurning)
            {
                currentTemperature = changeTemperature(currentTemperature, maxTemperature, dt);
            }

            if(!IsBurning && canIgniteFuel)
            {
                if (fuelStack != null)
                {
                    igniteFuel();
                }
            }

            if (!IsBurning)
            {
                currentTemperature = changeTemperature(currentTemperature, environmentTemperature(), dt);
            }

        }

        private void updateAshes()
        {
            
            if (outputSlot.Itemstack == null)
            {
                ItemStack ashStack = new ItemStack(Api.World.GetItem(new AssetLocation("stinkysurvivalmod:woodash")));
                ashStack.StackSize = 1;
                outputSlot.Itemstack = ashStack;
            }
            else
            {
                outputSlot.Itemstack.StackSize += 1;
            }

        }

        public EnumIgniteState GetIgnitableState(float secondsIgniting)
        {
            if (fuelSlot.Empty) return EnumIgniteState.NotIgnitablePreventDefault;
            if (IsBurning) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > 3 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
        }

        public float changeTemperature(float fromTemp, float toTemp, float dt) 
        {
            float diff = Math.Abs(fromTemp - toTemp);

            dt = dt + dt * (diff / 28);

            if (diff < dt)
            {
                return toTemp;
            }

            if (fromTemp > toTemp)
            {
                dt = -dt;
            }

            if (Math.Abs(fromTemp - toTemp) < 1)
            {
                return toTemp;
            }

            return fromTemp + dt;
        }

        public void CoolNow(float amountRel)
        {
            Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/extinguish"), Pos, -0.5, null, false, 16);

            fuelBurnTime -= (float)amountRel / 10f;
            
            if (Api.World.Rand.NextDouble() < amountRel/5f || fuelBurnTime <= 0)
            {
                setBlockState("cold");
                extinguishedTotalHours = -99;
                canIgniteFuel = false;
                fuelBurnTime = 0;
                maxFuelBurnTime = 0;
            }

            MarkDirty(true);


        }

        public void igniteFuel()
        {
            igniteWithFuel(fuelStack);

            fuelStack.StackSize -= 1;
            updateAshes();
            if (fuelStack.StackSize <= 0)
            {
                fuelStack = null;
            }
        }

        public void igniteWithFuel(IItemStack stack)
        {
            if (stack == null) {  return; }
            CombustibleProperties fuelCopts = stack.Collectible.CombustibleProps;

            maxFuelBurnTime = fuelBurnTime = fuelCopts.BurnDuration * BurnDurationModifier;
            maxTemperature = (int)(fuelCopts.BurnTemperature);
            smokeLevel = fuelCopts.SmokeLevel * 2;
            setBlockState("lit");
            MarkDirty(true);
        }


        public void setBlockState(string state)
        {
            AssetLocation loc = Block.CodeWithVariant("burnstate", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;

            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;
        }


        #region Events

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {

           // Api.Logger.Notification("in fireplace OnPlayerRightClick");

            if (Api.Side == EnumAppSide.Client)
            {
               // Api.Logger.Notification("in fireplace OnPlayerRightClick on client side");
                toggleInventoryDialogClient(byPlayer, () => {
                    SyncedTreeAttribute dtree = new SyncedTreeAttribute();
                    SetDialogValues(dtree);
                    clientDialog = new GuiDialogBEFireplace(DialogTitle, Inventory, Pos, dtree, Api as ICoreClientAPI);
                    return clientDialog;
                });
            }

            return true;
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            //Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory")); - why twice? its already done in the base method Tyron 5.nov 2024

            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }


            currentTemperature = tree.GetFloat("currentTemperature");
            maxTemperature = tree.GetInt("maxTemperature");
            fuelBurnTime = tree.GetFloat("fuelBurnTime");
            maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
            extinguishedTotalHours = tree.GetDouble("extinguishedTotalHours");
            canIgniteFuel = tree.GetBool("canIgniteFuel", true);
            cachedFuel = tree.GetFloat("cachedFuel", 0);

           if (Api?.Side == EnumAppSide.Client)
            {
            //    UpdateRenderer();

                if (clientDialog != null) SetDialogValues(clientDialog.Attributes);
            }


            if (Api?.Side == EnumAppSide.Client && (clientSidePrevBurning != IsBurning || shouldRedraw))
            {
                GetBehavior<BEBehaviorFirepitAmbient>()?.ToggleAmbientSounds(IsBurning);
                clientSidePrevBurning = IsBurning;
                MarkDirty(true);
                shouldRedraw = false;
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("currentTemperature", currentTemperature);
            tree.SetInt("maxTemperature", maxTemperature);
            tree.SetFloat("fuelBurnTime", fuelBurnTime);
            tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            tree.SetDouble("extinguishedTotalHours", extinguishedTotalHours);
            tree.SetBool("canIgniteFuel", canIgniteFuel);
            tree.SetFloat("cachedFuel", cachedFuel);
        }


        void SetDialogValues(ITreeAttribute dialogTree)
        {
            //Api.Logger.Notification("Dialog Values: {0},{1},{2},{3},{4}", currentTemperature, maxTemperature, maxFuelBurnTime, fuelBurnTime, inventory.GetOutputText());
            dialogTree.SetFloat("currentTemperature", currentTemperature);
            float realtimeRemaining;
            if (!fuelSlot.Empty)
            {
               realtimeRemaining = (fuelSlot.Itemstack.StackSize * maxFuelBurnTime + fuelBurnTime);

            }
            else
            {
               realtimeRemaining = fuelBurnTime;
            }
            float gametimeRemaining = (realtimeRemaining * Api.World.Calendar.SpeedOfTime);
            dialogTree.SetFloat("fuelBurnTime", fuelBurnTime);
            dialogTree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            dialogTree.SetFloat("gameTimeRemaining", gametimeRemaining);
            dialogTree.SetString("outputText", inventory.GetOutputText());
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            if (clientDialog != null)
            {
                clientDialog.TryClose();
                clientDialog?.Dispose();
                clientDialog = null;
            }
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken();
        }

        #endregion

        #region Helper getters

        public ItemSlot fuelSlot
        {
            get { return inventory[0]; }
        }

        public ItemSlot outputSlot
        {
            get { return inventory[1]; }
        }

        public ItemStack fuelStack
        {
            get { return inventory[0].Itemstack; }
            set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
        }

        public ItemStack outputStack
        {
            get { return inventory[1].Itemstack; }
            set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
        }

        public CombustibleProperties fuelCombustibleOpts
        {
            get { return getCombustibleOpts(0); }
        }

        public CombustibleProperties getCombustibleOpts(int slotid)
        {
            ItemSlot slot = inventory[slotid];
            if (slot.Itemstack == null) return null;
            return slot.Itemstack.Collectible.CombustibleProps;
        }

        #endregion


        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;

                if (slot.Itemstack.Class == EnumItemClass.Item)
                {
                    itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
                }
                else
                {
                    blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
                }

                slot.Itemstack.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
            }
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);

        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();

          //  renderer?.Dispose();
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            if (Block == null ) return false;


            
            string burnState = Block.Variant["burnstate"];
            string fuelState = Block.Variant["fuelstate"];
            string ashState = Block.Variant["ashstate"];
           // Api.Logger.Notification("On Tesselation Current State: "+burnState+" "+fuelState+" "+ashState);
            if (burnState == "cold" && fuelSlot.Empty) { burnState = "extinct"; fuelState = "empty"; }
            if (!fuelSlot.Empty)
            {
                var amount = fuelSlot.StackSize;
                if (amount == 1) { fuelState = "one"; }
                else if (amount == 2) { fuelState = "two"; }
                else if (amount >= 3) { fuelState = "full"; }
                else { fuelState = "empty"; }

            }
            else { fuelState = "empty"; }

            ashState = outputSlot.Empty ? "noashes" : "ashes";

          //  Api.Logger.Notification("On Tesselation after State: " + burnState + " " + fuelState + " " + ashState);
            if (burnState == null) return true;
            MeshData data = getOrCreateMesh(burnState, fuelState, ashState);
            
            mesher.AddMeshData(data);

            return true;
        }




        public MeshData getOrCreateMesh(string burnstate, string fuelstate, string ashstate)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "fireplace-meshes", () => new Dictionary<string, MeshData>());

            string key = Block.FirstCodePart(1)+"-"+burnstate +"-"+fuelstate+"-"+ashstate;
         //   Api.Logger.Notification("mesh key: "+ key);
            MeshData meshdata;
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.BlockId == 0) return null;
            if (!Meshes.TryGetValue(key, out meshdata))
            {

                MeshData[] meshes = new MeshData[24];
                ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;
                
                mesher.TesselateShape(block, Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/fireplace/fireplace-" + key + ".json"), out meshdata);
                meshdata.Rotate(new Vec3f(0.5f, 0, 0.5f), 0, 3.141593f, 0); //radians?
                Meshes[key] = meshdata;
            }


            return meshdata;
        }

   //     public EnumFirepitModel CurrentModel { get; private set; }

        public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
        {
            return IsBurning ? 10 : (IsSmoldering ? 0.25f : 0);
        }
    }
}
