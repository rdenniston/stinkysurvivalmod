using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using StinkySurvivalMod.Utility;
using Vintagestory.GameContent;
using Vintagestory.API.MathTools;
using StinkySurvivalMod.BlockEntities;

namespace StinkySurvivalMod.EntityBehaviors
{
    internal class BehaviorCanPoo:EntityBehavior
    {
        int interval = 100;
        float elapsed = 0;
        double peeInterval = 6;
        double pooInterval = 18; //hours
        ICoreAPI api;
        

        public override string PropertyName() => "stinkysurvivalmod:canpoo";
        private string pooType;

        double timeLastPoo
        {
            get => entity.WatchedAttributes.GetDouble("timeLastPoo", 0);
            set => entity.WatchedAttributes.SetDouble("timeLastPoo", value);
        }

        double timeLastPee
        {
            get => entity.WatchedAttributes.GetDouble("timeLastPee", 0);
            set => entity.WatchedAttributes.SetDouble("timeLastPee", value);
        }



        public BehaviorCanPoo(Entity entity) : base(entity)
        {
            api = entity.World.Api;
            if (EntityExtensions.biganimals.Contains(entity.Code.FirstCodePart())) {
                pooType = $"poo-big";
            } else if (entity.Code.FirstCodePart() == "chicken") {
                pooType = $"poo-chicken";
            } else {
                pooType = $"poo-lil";
            }
            //entity.Api.Logger.Notification($"{pooType} : {entity.Code}");

            //check about once an hour
            interval = 1 + api.World.Rand.Next(30);
            ensureHasPooed();
        }

        
        public override void OnGameTick(float deltaTime)
        {
            //once per day
            elapsed += deltaTime;
            if (elapsed > interval)
            {
                elapsed = 0;
                
                var hours = entity.World.Calendar.TotalHours - timeLastPoo;
                //time to poo
                if (hours >= pooInterval) {
                    if (TryPoo())
                    {
                        timeLastPoo = entity.World.Calendar.TotalHours;
                    }
                    else
                    {
                        api.Logger.Notification("Can't Poo! Something in the way?");
                    }
                }
                hours = entity.World.Calendar.TotalHours - timeLastPee;
                if (hours >= peeInterval)
                {
                    //chickens don't pee bro
                    if (entity.Code.FirstCodePart() != "chicken" && TryPee())
                    {
                        timeLastPee = entity.World.Calendar.TotalHours;
                    }
                }
            }
        }

        public bool TryPee()
        {

            //should be simple - look for thatch bedding and pee on it.
            BlockPos entityBlockPos = new BlockPos((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z, entity.Pos.Dimension).Down();
            //get the blockentity its on
            BlockEntity be = api.World.BlockAccessor.GetBlockEntity(entityBlockPos);
            var bethatch = be as BEThatchBedding;
            if (bethatch != null) 
            {
                bethatch.PeeOnMe();
                bethatch.MarkDirty(true);
                return true;
            }
            //api.Logger.Notification("Nothing to pee on! (null)");
            return false;
            
        }
        public bool TryPoo()
        {
            Item poo = entity.World.GetItem(new AssetLocation("stinkysurvivalmod", $"{pooType}-{entity.Api.World.Rand.Next(1, 4)}"));
            if (poo == null)
            {
                entity.Api.Logger.Warning($"Item stinkysurvivalmod:{pooType}-(1,2 or 3) not found. {entity.FirstCodePart()} cant poo.");
                return false; 
            }

            var pooStack = new ItemStack(poo);

            //checks
            if (!entity.OnGround) return false;

            //get the block its standing on
            BlockPos entityBlockPos = new BlockPos((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z, entity.Pos.Dimension).Down();
            //check replaceable fuckin chickens breakin fences w their shit
            
            
            //get the blockentity its on
            BlockEntity be = api.World.BlockAccessor.GetBlockEntity(entityBlockPos);
            //get the one its in
            BlockEntity beAbove = api.World.BlockAccessor.GetBlockEntity(entityBlockPos.UpCopy());
            

            //api.Logger.Notification($"bes: {be} {beAbove}");

            var storageBe = (be as BlockEntityGroundStorage) ?? (beAbove as BlockEntityGroundStorage);
            
            //no ground storage so put one if we can
            if (storageBe == null)
            {
                if (api.World.BlockAccessor.GetBlock(entityBlockPos.UpCopy()).Replaceable < 6000)  return false; 
                //just gonna take a hint from ground storable behavior
                BlockGroundStorage blockgs = entity.World.GetBlock(new AssetLocation("game:groundstorage")) as BlockGroundStorage;
                if (blockgs == null)
                {
                    entity.Api.Logger.Warning($"BlockGroundStorage asset location not {entity.FirstCodePart()} cant poo.");
                    return false;
                }
                api.World.BlockAccessor.SetBlock(blockgs.BlockId, entityBlockPos.UpCopy());
                storageBe = api.World.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(entityBlockPos.UpCopy());
            }
            if (storageBe != null && storageBe is BlockEntityGroundStorage)
            {
                GroundStorageProperties groundStorageProperties = new GroundStorageProperties();
                groundStorageProperties.Layout = EnumGroundStorageLayout.Quadrants;
                groundStorageProperties.CollisionBox = new Cuboidf(0.125,0.875,0,0.0625,0.125,0.875);


                storageBe.MeshAngle = 0;
                storageBe.AttachFace = BlockFacing.UP;
                storageBe.clientsideFirstPlacement = true;
                
                storageBe.ForceStorageProps(groundStorageProperties);

                ItemSlot pooSlot = null;
                bool placedPoo = false; 
                for (int slotNum = 0; slotNum < 4; slotNum++) {
                    BlockSelection blockSel = GetSelection(slotNum);
                    pooSlot = storageBe.GetSlotAt(blockSel);
                    if (pooSlot == null) api.Logger.Notification($"Something went wrong pooSlot is null: {slotNum}");
                    if (pooSlot.StackSize == 0)
                    {
                        api.Logger.Notification($"who poo'd: {entity.Code.FirstCodePart()} - placing poo in slot {slotNum}");
                        pooSlot.Itemstack = pooStack;
                        placedPoo = true;
                        break;
                    }
                    else
                    {
                        //hmmm we got something there... is it poo? and is it the last slot?
                        if (pooSlot.Itemstack.Collectible.Code.FirstCodePart() == "poo" && slotNum == 3)
                        {
                            pooSlot.Itemstack.StackSize += 1;
                            placedPoo = true;
                            break;
                        }
                    }
                }
                if (placedPoo) 
                {
                    //play sound

                    storageBe.MarkDirty(true);
                    return true;
                }
                

            }

            api.Logger.Notification("Couldn't place poo");

            return false;



        }

        public BlockSelection GetSelection(int slotNum)
        {
            if (slotNum < 0 || slotNum > 3) return GetSelection(0);
            //we just need to set the HitPosition so we can get that sweet inventory slot
            BlockSelection bs = new BlockSelection();
            switch (slotNum)
            {
                case 0:
                    bs.HitPosition = new Vec3d(0, 0, 0);
                    break;
                case 1:
                    bs.HitPosition = new Vec3d(0, 0, 0.9d);
                    break;
                case 2:
                    bs.HitPosition = new Vec3d(0.9d, 0, 0);
                    break;
                case 3:
                    bs.HitPosition = new Vec3d(0.9d, 0, 0.9d);
                    break;  
            }
            return bs;
            
        }

        public void ensureHasPooed()
        {
            if (!entity.WatchedAttributes.HasAttribute("timeLastPoo"))
            {
                entity.WatchedAttributes.SetDouble("timeLastPoo", entity.World.Calendar.TotalHours - entity.World.Rand.Next(24));
            }
            if (!entity.WatchedAttributes.HasAttribute("timeLastPee"))
            {
                entity.WatchedAttributes.SetDouble("timeLastPee", entity.World.Calendar.TotalHours - entity.World.Rand.Next(6));
            }
        }


    }
}
