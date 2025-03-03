﻿using StinkySurvivalMod.BlockEntities;
using StinkySurvivalMod.Blocks;
using StinkySurvivalMod.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace StinkySurvivalMod
{
    public class StinkySurvivalModModSystem : ModSystem
    {

        // Called on server and client
        // Useful for registering block/entity classes on both sides
        public override void Start(ICoreAPI api)
        {
            api.Logger.Notification("Hello from template mod: " + api.Side);
            api.RegisterBlockClass(Mod.Info.ModID + ".blockthatchbedding", typeof(BlockThatchBedding));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".bethatchbedding", typeof(BEThatchBedding));
            api.RegisterBlockClass(Mod.Info.ModID + ".blockfireplace", typeof(BlockFireplace));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".befireplace", typeof(BEFireplace));
            api.RegisterBlockClass(Mod.Info.ModID + ".blockleechingbasin", typeof(BlockLeechingBasin));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".beleechingbasin", typeof(BELeechingBasin));
            api.RegisterBlockClass(Mod.Info.ModID + ".blockcompostheap", typeof(BlockCompostHeap));
            api.RegisterBlockEntityClass(Mod.Info.ModID + ".becompostheap", typeof(BECompostHeap));

        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("stinkysurvivalmod:hello"));
            api.Event.OnEntitySpawn += (entity) => entity.addExtraBehaviors();
            api.Event.OnEntityLoaded += (entity) => entity.addExtraBehaviors();
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("stinkysurvivalmod:hello"));

        }

    }
}
