using StinkySurvivalMod.BlockEntities;
using StinkySurvivalMod.Blocks;
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

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Logger.Notification("Hello from template mod server side: " + Lang.Get("stinkysurvivalmod:hello"));
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            api.Logger.Notification("Hello from template mod client side: " + Lang.Get("stinkysurvivalmod:hello"));
        }

    }
}
