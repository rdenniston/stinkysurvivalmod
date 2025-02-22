using HarmonyLib;
using StinkySurvivalMod.Blocks;
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

        //tick variables
        Random random;
        int peeLevel;
        

        public int PeeLevel { get { return peeLevel; } }

        

        public BEThatchBedding() : base()
        {
            
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("peeLevel", peeLevel);

        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            peeLevel = tree.GetInt("peeLevel");

        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
         
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            UnregisterAllTickListeners();

        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            UnregisterAllTickListeners();
        }
        
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            random = new Random((int)Api.World.Calendar.ElapsedSeconds);
           var id =  RegisterGameTickListener(OnTick, 10000+random.Next(1000,10000));
            if (id > 0) api.Logger.Notification($"thatch bedding ticker on");
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

        
        public void PeeOnMe()
        {
            peeLevel++;
            Api.Logger.Notification($"Pee level now: {peeLevel}");
        }

        public void OnTick(float dt)
        {
            if (peeLevel > 4)
            {
                //reset timers

                //change the blocks
                Block thisblock = Api.World.BlockAccessor.GetBlock(Pos);
                Api.Logger.Notification("Block code path: " + thisblock.Code.Path.ToString());
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

                //lets get fancy - if a block changes state add 3 to neighbors who are lower 

                List<Block> blocks = new List<Block>();
                blocks.Append(Api.World.BlockAccessor.GetBlock(Pos.NorthCopy()));
                blocks.Append(Api.World.BlockAccessor.GetBlock(Pos.SouthCopy()));
                blocks.Append(Api.World.BlockAccessor.GetBlock(Pos.EastCopy()));
                blocks.Append(Api.World.BlockAccessor.GetBlock(Pos.WestCopy()));


                foreach (var b in blocks)
                {
                    var bt = b as BlockThatchBedding;
                    if (bt != null &&) { 
                        
                    }
                }
                

                
            }
        }
        
    }
}
