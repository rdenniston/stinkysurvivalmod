using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace StinkySurvivalMod.BlockEntities
{
    internal class BEThatchBedding : BlockEntity
    {
        //tick id
        long id;
        float stage;

        public BEThatchBedding()
        {
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("stage", stage);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            stage = tree.GetFloat("stage");
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
         
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            Api.Logger.Notification(this.Block.Code.ToString());
            UnregisterGameTickListener(this.id);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            
            UnregisterGameTickListener(this.id);
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            api.Logger.Notification("BEThatchBedding init");
            this.id = RegisterGameTickListener(OnTick, 10000);
            
        }

        public ItemStack[] GetDrops(ItemStack[] drops)
        {
            //Api.Logger.Notification("drops length " + drops.Length.ToString());
            if (drops?.Length > 0)
            {
                //Api.Logger.Notification("drops[0] matcher: " + drops[0].ToString());
                if (drops[0].Block.Code == "stinkysurvivalmod:thatchbedding-ready")
                {
                    //Api.Logger.Notification("drops matched");
                    ItemStack saltedThatch = new ItemStack(Api.World.GetItem(new AssetLocation("stinkysurvivalmod:saltedthatch")));
                    saltedThatch.StackSize = 4;
                    ItemStack[] newdrops = new ItemStack[1];
                    newdrops[0] = saltedThatch;
                    return newdrops;
                };
            }
            return drops;
        }
        public void OnTick(float dt)
        {
            Api.Logger.Notification($"Tick {dt}");
            var entities = Api.World.GetEntitiesAround(Pos.ToVec3d(), 5.0f, 1.5f, e => e.IsCreature && e.Alive );
            Api.Logger.Notification("found " + entities.Length + " entities");
            foreach (var entity in entities)
            {
               var code = entity.Code.ToString();
                Api.Logger.Notification("Received code: " + code);
               if (code == "game:sheep-bighorn-female" || code == "game:sheep-bighorn-male" || code == "game:sheep-bighorn-lamb" 
                    || code == "game:pig-wild-female" || code == "game:pig-wild-male" || code == "game:pig-wild-piglet" || code == "game:player")
                {
                    Api.Logger.Notification("Matched! " + code);
                    Block thisblock = Api.World.BlockAccessor.GetBlock(Pos);
                    Api.Logger.Notification("Block code path: "+thisblock.Code.Path.ToString());
                    if (thisblock.Code.Path.EndsWith("init")) 
                    {
                        thisblock = Api.World.GetBlock(thisblock.CodeWithParts("growth"));
                    } 
                    else if (thisblock.Code.Path.EndsWith("growth"))
                    {
                        thisblock = Api.World.GetBlock(thisblock.CodeWithParts("mature"));
                    }
                    else if (thisblock.Code.Path.EndsWith("mature"))
                    {
                        thisblock = Api.World.GetBlock(thisblock.CodeWithParts("ready"));
                    }
                    Api.Logger.Notification("New Block code path: " + thisblock.Code.Path.ToString());
                    Api.World.BlockAccessor.SetBlock(thisblock.BlockId, Pos);
                }
            }
        }
        
    }
}
