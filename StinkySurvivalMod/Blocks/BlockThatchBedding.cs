using StinkySurvivalMod.BlockEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace StinkySurvivalMod.Blocks
{
    internal class BlockThatchBedding : Block
    {
        public override void OnBlockPlaced(IWorldAccessor world, BlockPos blockPos, ItemStack byItemStack = null)
        {
            api.Logger.Event("ThatchBedding Placed");
            base.OnBlockPlaced(world, blockPos, byItemStack);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            api.Logger.Event("ThatchBedding Broken");
            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            api.Logger.Notification("GetDrops called");
            BEThatchBedding bEThatchBedding = world.BlockAccessor.GetBlockEntity(pos) as BEThatchBedding;
            var basedrops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
            if (bEThatchBedding == null)
            {
                api.Logger.Notification("bethatchbedding null.... crap");
                return null;
            }
            var drops = bEThatchBedding.GetDrops(basedrops);
            return drops;
        }
    }
}
