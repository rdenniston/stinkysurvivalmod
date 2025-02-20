using Newtonsoft.Json;
using StinkySurvivalMod.Blocks;
using StinkySurvivalMod.gui;
using StinkySurvivalMod.Inventory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Vintagestory.Server.Timer;

namespace StinkySurvivalMod.BlockEntities
{
    public class BELeechingBasin: BlockEntityLiquidContainer
    {
        
        public override string InventoryClassName => "leechingbasininventory";
        public Dictionary<string, string[]> recipes = new Dictionary<string, string[]>();
        public long partId = -1;
        public long liqId = -1;
        public ThreadSafeRandom rand;
        public float particlesStart;
        public float particlesElapsed;
        public float liquidElapsed = 0;
        public float liquidTransferred = 0;
        public bool skip = false;
        public int soundSkip = 0;
        public float fillHeight = 0;
        public AssetLocation OpenSound;
        public AssetLocation CloseSound;
        public ItemSlot bucketSlot => inventory[2];

        GuiDialogBELeechingBasin clientDialog;
        protected GuiDialogBlockEntity invDialog;

        public virtual string DialogTitle
        {
            get { return Lang.Get("leechingbasin-diagtitle"); }
        }


        public SimpleParticleProperties dripParticles = new SimpleParticleProperties(
            1, 
            1000, 
            ColorUtil.ColorFromRgba(255, 123, 0, 90), 
            new Vec3d(), 
            new Vec3d(), 
            new Vec3f(), 
            new Vec3f()
            );

        public SimpleParticleProperties splashParticles = new SimpleParticleProperties(
            1,
            1000,
            ColorUtil.ColorFromRgba(255, 123, 0, 90),
            new Vec3d(),
            new Vec3d(),
            new Vec3f(),
            new Vec3f(),
            1,
            1,
            1,
            1.2f
            );

        public Vec3d[] splashMinPos = new Vec3d[4];
        public Vec3d[] splashMaxPos = new Vec3d[4];
        
        

        public BELeechingBasin() {

            splashMinPos[0] = new Vec3d(4.0 / 16.0, 9.0 / 16.0, 12.0 / 16.0);
            splashMinPos[1] = new Vec3d(3.0 / 16.0, 9.0 / 16.0, 4.0 / 16.0);
            splashMinPos[2] = new Vec3d(4.0 / 16.0, 9.0 / 16.0, 3.0 / 16.0);
            splashMinPos[3] = new Vec3d(12.0 / 16.0, 9.0 / 16.0, 4.0 / 16.0);

            splashMaxPos[0] = new Vec3d(12.0 / 16.0, 0, 0);
            splashMaxPos[1] = new Vec3d(0, 0, 12.0 / 16.0);
            splashMaxPos[2] = new Vec3d(12.0 / 16.0, 0, 0);
            splashMaxPos[3] = new Vec3d(0, 0, 12.0 / 16.0);
            //make top inventory items and liquid
            inventory = new InventoryGeneric(3, null, null, (id, self) =>
            {
                if (id == 0) return new ItemSlot(self);
                if (id == 2) return new ItemSlot(self);
                else return new ItemSlotLiquidOnly(self, 20);
            });
            inventory.SlotModified += Inventory_SlotModified;
            inventory.BaseWeight = 1;
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            Api.Logger.Notification("Block broken!");
            if (Api.World is IServerWorldAccessor)
            {
                Inventory.DropAll(Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            base.OnBlockBroken(byPlayer);
        }

        bool convertFlag = false;

        public bool TestIngredients()
        {
            string itemcode = Inventory[0]?.Itemstack?.Collectible?.Code?.ToString();
            string liq = Inventory[1]?.Itemstack?.Collectible?.Code?.ToString();
            //if we don't have everything false            
            if (itemcode == null || liq == null || bucketSlot.Empty ) return false;
            //recipes basically
    //        Api.Logger.Notification($"{itemcode}" + $"{liq}");
            if (itemcode == "stinkysurvivalmod:saltedthatch" && liq == "stinkysurvivalmod:lyeportion") return true;
            if (itemcode == "stinkysurvivalmod:woodash" && liq == "game:waterportion") return true;

            return false;
        }
        /* Provides the converted asset string- maybe one day this pulls from a recipe json but for now... 
         * not worth the effort
         */
        public string GetConversion()
        {
            string itemcode = Inventory[0]?.Itemstack?.Collectible?.Code?.ToString();
            string liq = Inventory[1]?.Itemstack?.Collectible?.Code?.ToString();            
            //if we don't have everything false            
            if (itemcode == null || liq == null) return string.Empty;
            //recipes basically
            if (itemcode == "stinkysurvivalmod:saltedthatch" && liq == "stinkysurvivalmod:lyeportion") return "stinkysurvivalmod:hydratedsaltpeterportion";
            if (itemcode == "stinkysurvivalmod:woodash" && liq == "game:waterportion") return "stinkysurvivalmod:lyeportion";

            return string.Empty;
        }

        void Inventory_SlotModified(int slotId) {
     //       Api.Logger.Notification("Slot {0} modified", slotId);
     //       Api.Logger.Notification("Inventory_SlotModified Slot 0: " + inventory[0]?.Itemstack?.StackSize);

            convertFlag = TestIngredients();
            MarkDirty(true);
            if (slotId == 1 || slotId == 0)
            {
                if (inventory[1]?.Itemstack != null )
                {
                        particlesStart = particlesElapsed = Api.World.Calendar.ElapsedSeconds + 5; // see next line for why +5
                        liquidElapsed = 0;
                        if (Api.Side == EnumAppSide.Server && liqId == -1)
                        {
                           liqId = RegisterGameTickListener(ProcessLiquids, 5000, 5000);
                            Api.Logger.Notification("ProcessLiquids {0}", liqId);
                        }
                        else if (Api.Side == EnumAppSide.Client && partId == -1)
                        {
                           partId = RegisterGameTickListener(ParticleStep, 500, 5000);
                        }
                    
                    if (inventory[1].Itemstack.StackSize <= 0)
                    {
                        UnregisterAllTickListeners();
                        liqId = partId = -1;
                        liquidTransferred = 0;
                    }

                }
                else
                {
                    UnregisterAllTickListeners();
                    liqId = partId = -1;
                    liquidTransferred = 0;
                }

            }
        /*   We don't care about slot 2 fuck it let it fall on the ground hahahaha
         *   
         *   if (slotId == 2) {
                
                BlockLiquidContainerBase cntBlock = bucketSlot?.Itemstack?.Collectible as BlockLiquidContainerBase;
                if (cntBlock != null) {
       //             Api.Logger.Notification("Bucket amount = " + cntBlock.GetCurrentLitres(bucketSlot.Itemstack).ToString());
                }
                else
                {
        //            Api.Logger.Notification("bucket removed");
                }
            };
        //    Api.Logger.Notification("Inventory_SlotModified Slot 0: " + inventory[0]?.Itemstack?.StackSize);
        */
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            OpenSound = AssetLocation.Create("game:sounds/block/barrelopen", Block.Code.Domain);
            CloseSound = AssetLocation.Create("game:sounds/block/barrelclose", Block.Code.Domain);
            rand = new ThreadSafeRandom((int)Api.World.Calendar.ElapsedSeconds);
            if (Block.LastCodePart() == "bottom" && api.Side == EnumAppSide.Client) { 
                dripParticles.ShouldDieInLiquid = true;
                dripParticles.Bounciness = 0.5f;
                dripParticles.MinPos = Pos.ToVec3d().AddCopy(0.5f, 12/16.0f, 0.5f);
                dripParticles.AddPos = new Vec3d(0, -.1, 0);
                dripParticles.Color = ColorUtil.ColorFromRgba(255, 123, 0, 90);

                
                splashParticles.ShouldDieInLiquid = true;
                splashParticles.Bounciness = 0.5f;
                splashParticles.Color = ColorUtil.ColorFromRgba(255, 123, 0, 90);


            }

            //start liquid processing on init if theres stuff to process
            if (Inventory[1]?.Itemstack != null && Inventory[1].Itemstack.StackSize > 0) {
                if (Api.Side == EnumAppSide.Server && liqId == -1)
                {
                    liqId = RegisterGameTickListener(ProcessLiquids, 5000, 5000);
                    api.Logger.Notification("ProcessLiquids {0}", liqId);
                }
                else if (Api.Side == EnumAppSide.Client && partId == -1)
                {
                    partId = RegisterGameTickListener(ParticleStep, 500, 5000);
                }
            }
        }

        //used for consuming items
        int amountTransfered = 0;
        public void ProcessLiquids(float dt)
        {
            //this func contains multithread hell but no other way I know in c# other than trap errors
            liquidElapsed += dt;
            //checks if items in the basin will convert
            convertFlag = TestIngredients();

            if (liquidElapsed > 5.0f) {
                
                ItemStack bucket = bucketSlot?.Itemstack;
                ItemStack liq = inventory[1]?.Itemstack?.Clone();
                BlockLiquidContainerBase bbucket = Inventory[2]?.Itemstack?.Collectible as BlockLiquidContainerBase;
              //  ItemStack bucketcontent = null;
              //  WaterTightContainableProps bucketliquidprops = null;


                /* 
                 * Maybe one day but this commented code (above and below) to get items per litre causes issues (null shit)
                 * when all portions as of now are 100 parts per liter
                */
               
                // if (bbucket != null) 
               // {
                  //  var bucketcontents = bbucket.GetContents(Api.World, bucketSlot.Itemstack);

                  //  bucketcontent = bucketcontents.Length > 0 ? bucketcontents[0] : null;
                 //   bucketliquidprops = bucketcontent?.Block?.Attributes["waterTightContainerProps"]?.AsObject<WaterTightContainableProps>(null, Block.Code.Domain);
                 //   basinliquidprops = liq.Block.Attributes["waterTightContainerProps"].AsObject<WaterTightContainableProps>(null, Block.Code.Domain);
              //  }

                while (liquidElapsed > 5.0f && bucket != null && liq != null)
                {
                    liquidElapsed -= 5.0f;
                    float desiredAmount = 0.10f;
                    if (liq != null && liq.StackSize > 0)
                    {
                        //default drain, tough titties lye users
                        liq.StackSize -= 10;
                        //if stacksize is less than 0.10 desired amount:
                        if (liq.StackSize < 10)
                        {
                            //see comment above for why 100
                            desiredAmount = (float)(liq.StackSize/100); 

                        }
                    }
                    else
                    {
                        //duh? if there ain't water stop it
                        UnregisterAllTickListeners();
                        liqId = partId = -1;
                        liquidTransferred = 0;
                    }


                    if (bucketSlot != null && bucketSlot.Empty == false)
                    {
                        //thread hell wrapper
                        try
                        {
                            //we'll be nice: don't consume hard earned items 
                            if (bbucket != null && !bbucket.IsFull(bucket))
                            {
                                //Api.Logger.Notification("liq stack size: " + liq.StackSize);

                                if (convertFlag)
                                {

                                    //Api.Logger.Notification("In conversion zone");
                                    ItemStack liqStack = new ItemStack(Api.World.GetItem(new AssetLocation(GetConversion())));
                                    liqStack.StackSize = liq.StackSize;
                                    int amount = bbucket.TryPutLiquid(bucket, liqStack, desiredAmount);
                                    if (amount == 0)
                                    {
                                        //check for mismatch liquids and if so play splash
                                        //TODO: send packet to trigger animation on client
                                        var bucketcontents = bbucket.GetContents(Api.World, bucketSlot.Itemstack);
                                        if (bucketcontents.Any() && bucketcontents[0].Collectible.Code != liqStack.Collectible.Code)
                                        {
                                            (Api as ICoreServerAPI).Network.BroadcastBlockEntityPacket(Pos, 8888);
                                            Api.Logger.Notification("Sending Splash packet");
                                            Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/water-fill1"), Pos, 0, null, true, 32, 5);
                                        }
                                    }
                                    amountTransfered += amount;
                                  //  Api.Logger.Notification("Amount transferred: {0} So far: {1}", amount, amountTransfered);
                                    
                                    //maybe we make this configurable but probably not
                                    //this consumes 2x amount produced so 20L of water/lye becomes 10L of lye/saltpeter
                                    if (amount > liq.StackSize)
                                    {
                                        liq.StackSize = 0;
                                    }
                                    else
                                    {
                                        liq.StackSize -= amount;

                                    }
                                    

                                    //one litre converted = 1 item consumed
                                    //consume one on empty liquid
                                    if (amountTransfered >= 100 || liq.StackSize == 0)
                                    {

                                        //we are in the conversion zone in a try block so fuck null checks
                                        inventory[0].Itemstack.StackSize--;
                                        if (inventory[0].Itemstack.StackSize == 0) inventory[0].TakeOutWhole();
                                        amountTransfered -= 100;
                                    }
                                }
                                else
                                {
                                    int amount = bbucket.TryPutLiquid(bucket, liq, desiredAmount);
                                }
                                

                               
                              //  Api.Logger.Notification("moved "+ moved.ToString() + "new size: "+ bbucket.GetCurrentLitres(bucket).ToString());
                            }

                        }
                        //if we throw here fuck it all and just move on we just lose the tick basically
                        catch (Exception ex)
                        {
                            Api.Logger.Error(ex);
                        }
                    }
                    
                }
                
                //only put replace if they exist
          //      if (bucketSlot?.Itemstack != null) { bucketSlot.Itemstack.StackSize = bucket.StackSize; }
                if (inventory[1]?.Itemstack != null) { 
                    inventory[1].Itemstack.StackSize = liq.StackSize;
                    if (inventory[1].Itemstack.StackSize <= 0) inventory[1].TakeOutWhole();
                }
                
                inventory[0].MarkDirty();
                inventory[1].MarkDirty();
                inventory[2].MarkDirty();
                MarkDirty(true);
            }
                  
            
        }
        public void ParticleStep(float dt)
        {
            particlesElapsed += dt;
            if (inventory[1]?.Itemstack == null || inventory[1].Itemstack.StackSize <=0) { UnregisterAllTickListeners(); liqId = partId = -1; }
         //   Api.Logger.Notification("qty: {0} {1} {2} {3}", qty, dt, particlesStart, Math.Sin((double)particlesStart));

            if (skip == false)
            {
                if (particlesElapsed - particlesStart < 20) { skip = true; }
                dripParticles.AddQuantity = (float)rand.Next(1,10);
                dripParticles.Color = TestIngredients() || Inventory[1]?.Itemstack?.Collectible?.LastCodePart() == "lyeportion"  ? ColorUtil.ColorFromRgba(226, 228, 228, 50) :  ColorUtil.ColorFromRgba(255, 123, 0, 50);
                Api.World.SpawnParticles(dripParticles);
                if (soundSkip % 8 == 0) {
                    BlockLiquidContainerBase bbucket = Inventory[2]?.Itemstack?.Collectible as BlockLiquidContainerBase;
                    ItemStack bucket = bucketSlot?.Itemstack;
                    if (bucket != null && bbucket != null)
                    {
                        if (bbucket.IsFull(bucket) && inventory[1].Itemstack != null && !inventory[1].Empty) {
                            for (int i = 0; i < 4; i++) {
                                Api.Logger.Notification("Spawning Splash");
                                splashParticles.Color = TestIngredients() ? ColorUtil.ColorFromRgba(226, 228, 228, 50) : ColorUtil.ColorFromRgba(255, 123, 0, 50);
                                splashParticles.AddQuantity = (float)rand.Next(10, 20);
                                splashParticles.MinPos = Pos.ToVec3d().AddCopy(splashMinPos[i]);
                                splashParticles.AddPos = splashMaxPos[i];
                                
                                Api.World.SpawnParticles(splashParticles);
                            }
                            Api.World.PlaySoundAt(new AssetLocation("game:sounds/effect/water-fill1"), Pos, 0, null, true, 32, 5);
                            
                        };
                    }
                }
                soundSkip += 1;
                
            }
            else
            {
                skip = false;
            }
        }

        public void InteractBasin(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                // Api.Logger.Notification("in fireplace OnPlayerRightClick on client side");
                toggleInventoryDialogClient(byPlayer);
            }

        }

        protected void toggleInventoryDialogClient(IPlayer byPlayer)
        {
            if (clientDialog == null)
            {
                ICoreClientAPI capi = Api as ICoreClientAPI;
                clientDialog = new GuiDialogBELeechingBasin(Lang.Get("stinkysurvivalmod:leechingbasin-diagtitle"), Inventory, Pos, Api as ICoreClientAPI);
                clientDialog.OnClosed += () =>
                {
                    clientDialog = null;
                    capi.Network.SendBlockEntityPacket(Pos, (int)EnumBlockEntityPacketId.Close, null);
                    capi.Network.SendPacketClient(Inventory.Close(byPlayer));
                };
                clientDialog.OpenSound = AssetLocation.Create("game:sounds/block/barrelopen", Block.Code.Domain);
                clientDialog.CloseSound = AssetLocation.Create("game:sounds/block/barrelclose", Block.Code.Domain);

                clientDialog.TryOpen();
                capi.Network.SendPacketClient(Inventory.Open(byPlayer));
                capi.Network.SendBlockEntityPacket(Pos, (int)EnumBlockEntityPacketId.Open, null);
            }
            else
            {
                clientDialog.TryClose();
            }
        }


        public void InteractBucket(IPlayer byPlayer, BlockSelection blockSel)
        {
            ItemSlot handslot = byPlayer.InventoryManager.ActiveHotbarSlot;
            ItemStack handstack = handslot.Itemstack;

            if (handslot.Empty && !bucketSlot.Empty) 
            {
                if (!byPlayer.InventoryManager.TryGiveItemstack(bucketSlot.TakeOutWhole(), true))
                {
                    Api.World.SpawnItemEntity(bucketSlot.Itemstack, Pos);
                }
                Api.Logger.Notification("Bucket slot after give: {0}, {1} ", bucketSlot.Empty, bucketSlot.StackSize);
                SetBucketState("nobucket");
                return;
            }

            if (handstack != null && handstack.Collectible is BlockLiquidContainerBase blockliqcont && blockliqcont.AllowHeldLiquidTransfer && blockliqcont.IsTopOpened && blockliqcont.CapacityLitres < 20 && bucketSlot.Empty)
            {

                bool moved = handslot.TryPutInto(Api.World, bucketSlot, 1) > 0;
                if (moved)
                {
                    handslot.MarkDirty();
                    MarkDirty(true);
                    SetBucketState("bucket");
                    Api.World.PlaySoundAt(handstack.Block.Sounds.Place, Pos, -0.5, byPlayer);
                }
            }

        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            if (Block == null) return false;
            string bucketState = Block.Variant["attachment"];
            string piece = Block.Variant["part"];
            if (piece == "bottom") 
            {
                bucketState = (bucketSlot.Empty || bucketSlot.StackSize == 0) ? "nobucket" : "bucket";
          //      Api.Logger.Notification("bucketstate bottom: " + bucketState);
            }
            //so in the main game (not creative) somehow random shit can be null since this is called often 
            //heres the shitty fix: a try/catchall
            try { 
                mesher.AddMeshData(getOrCreateMesh(bucketState, piece));
                ItemStack liq = Inventory[1]?.Itemstack;
                Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "leechingbasin-meshes", () => new Dictionary<string, MeshData>());
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                MeshData data = null;
                BlockLiquidContainerBase bbucket = Inventory[2]?.Itemstack?.Collectible as BlockLiquidContainerBase;
                ITesselatorAPI mesherer = ((ICoreClientAPI)Api).Tesselator;

                if (liq != null)
                {
                
                    if (liq.StackSize > 0)
                    {
                        string key = "liquid";
                        WaterTightContainableProps props = BlockLeechingBasin.GetContainableProps(Inventory[1].Itemstack);
                        if (!Meshes.TryGetValue(key+liq.ToString(), out data))
                        {
                            ITexPositionSource contentsource = null;
                            if (props != null && props.Texture != null)
                            {
                                contentsource = new ContainerTextureSource(Api as ICoreClientAPI, liq, props.Texture);
                                mesherer.TesselateShape("basinliquid",
                                    Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/basin/leechingbasin-" + key + ".json"),
                                    out data, contentsource);
                            
                            }
                            else
                            {
                                mesherer.TesselateShape(block, Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/basin/leechingbasin-liquid.json"), out data);
                            }

                            data.CustomInts = new CustomMeshDataPartInt(data.FlagsCount);
                            data.CustomInts.Values.Fill(0x4000000); // light foam only
                            data.CustomInts.Count = data.FlagsCount;

                            data.CustomFloats = new CustomMeshDataPartFloat(data.FlagsCount * 2);
                            data.CustomFloats.Count = data.FlagsCount * 2;

                            Meshes[key + liq.ToString()] = data;
                        }

                    
                        if (props != null && data != null)
                        {
                            data = data.Clone();
                            float fillheight = ((float)(liq.StackSize / props.ItemsPerLitre) / 20) * (0.3125f); //0.3125 == 5.0/16.0
                            data.Translate(0, fillheight + 1.0f, 0); //plus 1.0 because inventory only exists in bottom block
                        }
                    
                        if (data != null) mesher.AddMeshData(data);
                    }
                
                }

                if (bbucket != null)
                {
                    ItemStack[] liqs = bbucket.GetContents(Api.World, Inventory[2].Itemstack);
                   // Api.Logger.Notification("liqs any {0}", liqs.Any());
                    if (liqs.Any() && liqs[0] != null)
                    {
                        data = null;
                        var bucketliq = liqs[0];
                        //    Api.Logger.Notification("bucketliq.StackSize {0}", bucketliq.StackSize);
                        WaterTightContainableProps props = BlockLeechingBasin.GetContainableProps(bucketliq);

                    
                        if (bucketliq.StackSize > 0)
                        {
                            string key = "liquidbucket";
                            key += "-" + bucketliq.Collectible.LastCodePart();
                            //bucket liquid shapes can be retrieved off of main thread?? just do these ones the old way
                            if (!Meshes.TryGetValue(key, out data))
                            {

                                Shape ashape = Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/basin/leechingbasin-" + key + ".json") ??
                                      Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/basin/leechingbasin-liquidbucket.json");
                                mesherer.TesselateShape(block, ashape, out data);
                                data.CustomInts = new CustomMeshDataPartInt(data.FlagsCount);
                                data.CustomInts.Values.Fill(0x4000000); // light foam only
                                data.CustomInts.Count = data.FlagsCount;

                                data.CustomFloats = new CustomMeshDataPartFloat(data.FlagsCount * 2);
                                data.CustomFloats.Count = data.FlagsCount * 2;

                                Meshes[key + liq.ToString()] = data;

                                Meshes[key] = data;
                            }

                        

                            if (props != null && data != null)
                            {
                                data = data.Clone();
                                float fillheight = ((float)(bucketliq.StackSize / props.ItemsPerLitre) / bbucket.CapacityLitres) * (0.5f); //0.5 == 8.0/16.0
                                data.Translate(0, fillheight, 0); //plus 1.0 because inventory only exists in bottom block
                            }
                            if (data != null) mesher.AddMeshData(data);
                        }
                    }
                }

                if (Inventory[0]?.Itemstack != null && !Inventory[0].Empty)
                {
                    string code = Inventory[0].Itemstack.Collectible.Code.ToString();
                    string key = null;
                    data = null;
                    if (code == "stinkysurvivalmod:saltedthatch")  key = "saltthatch";
                    if (code == "stinkysurvivalmod:woodash") key = "ashes";
                    if (key != null)
                    {
                        if (!Meshes.TryGetValue(key, out data))
                        {
                            mesherer.TesselateShape(block, Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/basin/leechingbasin-" + key + ".json"), out data);
                            Meshes[key] = data;
                        }
                        if (data != null)
                        {
                            data = data.Clone();
                            data.Translate(0, 1.0f, 0);
                            mesher.AddMeshData(data);
                        }
                    }
                }
                return true;
            }
            catch (Exception e) { Api.Logger.Error(e); return true; }
        }

        public MeshData getOrCreateMesh(string bucketState, string piece)
        {
            Dictionary<string, MeshData> Meshes = ObjectCacheUtil.GetOrCreate(Api, "leechingbasin-meshes", () => new Dictionary<string, MeshData>());

            string key = bucketState + "-" + piece;
            Block block = Api.World.BlockAccessor.GetBlock(Pos);
            if (block.BlockId == 0) return null;
            // Api.Logger.Notification("mesh key: " + key);
            MeshData meshdata;
            if (!Meshes.TryGetValue(key, out meshdata))
            {
                
               // MeshData[] meshes = new MeshData[4];
                ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

                mesher.TesselateShape(block, Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/basin/leechingbasin-" + key + ".json"), out meshdata);
                Meshes[key] = meshdata;
            }

            return meshdata;
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            Api.Logger.Notification("ReceivedClient Packet: {0}", packetid);
            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
                return;
            }

            if (packetid == 1001)
            {
                player.InventoryManager?.CloseInventory(Inventory);
                data = SerializerUtil.Serialize(new OpenContainerLidPacket(player.Entity.EntityId, opened: false));
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)player);
            }

            if (packetid == 1000)
            {
                player.InventoryManager?.OpenInventory(Inventory);
                data = SerializerUtil.Serialize(new OpenContainerLidPacket(player.Entity.EntityId, opened: true));
                ((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)player);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            IClientWorldAccessor clientWorldAccessor = (IClientWorldAccessor)Api.World;
            if (packetid == 5000)
            {
                Api.Logger.Notification("Got Server packet id 5000 (uhhh maybe buggy?)");
                if (invDialog != null)
                {
                    GuiDialogBlockEntity guiDialogBlockEntity = clientDialog;
                    if (guiDialogBlockEntity != null && guiDialogBlockEntity.IsOpened())
                    {
                        invDialog.TryClose();
                    }

                    invDialog?.Dispose();
                    invDialog = null;
                    return;
                }

                BlockEntityContainerOpen blockEntityContainerOpen = BlockEntityContainerOpen.FromBytes(data);
                Inventory.FromTreeAttributes(blockEntityContainerOpen.Tree);
                Inventory.ResolveBlocksOrItems();
                invDialog = new GuiDialogBlockEntityInventory(DialogTitle, Inventory, Pos, blockEntityContainerOpen.Columns, Api as ICoreClientAPI);
                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                string text = block.Attributes?["openSound"]?.AsString();
                string text2 = block.Attributes?["closeSound"]?.AsString();
                AssetLocation assetLocation = ((text == null) ? null : AssetLocation.Create(text, block.Code.Domain));
                AssetLocation assetLocation2 = ((text2 == null) ? null : AssetLocation.Create(text2, block.Code.Domain));
                invDialog.OpenSound = assetLocation ?? OpenSound;
                invDialog.CloseSound = assetLocation2 ?? CloseSound;
                invDialog.TryOpen();
            }

            if (packetid == 5001)
            {
                Api.Logger.Notification("Got Server packet id 5001 (unimpl)");
        //        OpenContainerLidPacket openContainerLidPacket = SerializerUtil.Deserialize<OpenContainerLidPacket>(data);
         //       if (this is BlockEntityGenericTypedContainer blockEntityGenericTypedContainer)
        //        {
        //            if (openContainerLidPacket.Opened)
        //            {
        //                LidOpenEntityId.Add(openContainerLidPacket.EntityId);
        //                blockEntityGenericTypedContainer.OpenLid();
        //            }
        //            else
        //            {
        //                LidOpenEntityId.Remove(openContainerLidPacket.EntityId);
        //                if (LidOpenEntityId.Count == 0)
        //                {
        //                    blockEntityGenericTypedContainer.CloseLid();
        //                }
        //            }
         //       }
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                clientWorldAccessor.Player.InventoryManager.CloseInventory(Inventory);
                GuiDialogBlockEntity guiDialogBlockEntity2 = invDialog;
                if (guiDialogBlockEntity2 != null && guiDialogBlockEntity2.IsOpened())
                {
                    invDialog?.TryClose();
                }

                invDialog?.Dispose();
                invDialog = null;
            }

            if(packetid == 8888)
            {
                //bad mix signal from Process liquid
                Api.Logger.Notification("Received Splash Packet");
                for (int i = 0; i < 4; i++)
                {
                    Api.Logger.Notification("Spawning Splash");
                    splashParticles.Color = TestIngredients() ? ColorUtil.ColorFromRgba(226, 228, 228, 50) : ColorUtil.ColorFromRgba(255, 123, 0, 50);
                    splashParticles.AddQuantity = (float)rand.Next(10, 20);
                    splashParticles.MinPos = Pos.ToVec3d().AddCopy(splashMinPos[i]);
                    splashParticles.AddPos = splashMaxPos[i];

                    Api.World.SpawnParticles(splashParticles);
                }
            }
        }


        public void SetBucketState(string state)
        {
            AssetLocation loc = Block.CodeWithVariant("attached", state);
            Block block = Api.World.GetBlock(loc);
            if (block == null) return;
            Api.World.BlockAccessor.ExchangeBlock(block.Id, Pos);
            this.Block = block;
        }

    }
}
