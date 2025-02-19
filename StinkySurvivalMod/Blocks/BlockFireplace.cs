using StinkySurvivalMod.BlockEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace StinkySurvivalMod.Blocks
{
    public class BlockFireplace : Block, IIgnitable, ISmokeEmitter
    {
        public bool IsExtinct;

        AdvancedParticleProperties[] ringParticles;
        Vec3f[] basePos;
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);


        }

    //    public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer) {
        
    //    }

        public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
        {
            bool val = base.ShouldReceiveClientParticleTicks(world, player, pos, out _);
            isWindAffected = true;

            return val;
        }

        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            if (world.Rand.NextDouble() < 0.05 && GetBlockEntity<BEFireplace>(pos)?.IsBurning == true)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.Fire, SourcePos = pos.ToVec3d() }, 0.5f);
            }

            base.OnEntityInside(world, entity, pos);
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            BEFireplace bef = api.World.BlockAccessor.GetBlockEntity(pos) as BEFireplace;
            if (bef != null && !bef.canIgniteFuel)
            {
                bef.canIgniteFuel = true;
                bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
            }

            handling = EnumHandling.PreventDefault;
        }

        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            BEFireplace bef = api.World.BlockAccessor.GetBlockEntity(pos) as BEFireplace;
            if (bef.IsBurning) return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
            return EnumIgniteState.NotIgnitable;
        }
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            BEFireplace bef = api.World.BlockAccessor.GetBlockEntity(pos) as BEFireplace;
            if (bef == null) return EnumIgniteState.NotIgnitable;
            return bef.GetIgnitableState(secondsIgniting);
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (IsExtinct)
            {
                base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
                return;
            }

            BEFireplace bef = manager.BlockAccess.GetBlockEntity(pos) as BEFireplace;
       //     if (bef != null )
        //    {
         //       for (int i = 0; i < ringParticles.Length; i++)
        //        {
        //            AdvancedParticleProperties bps = ringParticles[i];
        //            bps.WindAffectednesAtPos = windAffectednessAtPos;
        //            bps.basePos.X = pos.X + basePos[i].X;
        //            bps.basePos.Y = pos.Y + basePos[i].Y;
        //            bps.basePos.Z = pos.Z + basePos[i].Z;
                    //
        //            manager.Spawn(bps);
      //          }

       //         return;
       //     }

            base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return false;
            }
            api.Logger.Notification("OnblockInteractFireplace");
            ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
            BEFireplace bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BEFireplace;;
            if (bef != null) {
                bef.OnPlayerRightClick(byPlayer, blockSel);
                return true;
            }

            return false;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        public bool EmitsSmoke(BlockPos pos)
        {
            var befirepit = api.World.BlockAccessor.GetBlockEntity(pos) as BEFireplace;
            return befirepit?.IsBurning == true;
        }
    }
}
