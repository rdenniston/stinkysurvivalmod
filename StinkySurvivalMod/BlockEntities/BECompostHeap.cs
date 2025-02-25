using StinkySurvivalMod.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace StinkySurvivalMod.BlockEntities
{
    public class BECompostHeap : BlockEntityContainer
    {
        internal InventoryCompostHeap inventory;

        public override string InventoryClassName { get { return "compostheap"; } }
        public override InventoryBase Inventory { get { return inventory; } }




        public BECompostHeap()
        {
            inventory = new InventoryCompostHeap(null, Api);
            
        }


        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            try
            {
                MeshData meshdata;
                Block block = Api?.World?.BlockAccessor?.GetBlock(Pos);
                if (block == null || block.BlockId == 0) return false;

                tesselator.TesselateShape(block, Shape.TryGet(Api, "stinkysurvivalmod:shapes/block/compostheap.json"), out meshdata);
                meshdata.Scale(new Vec3f(0.5f, 0, 0.5f), 1.7f, 1.7f, 1.7f);
                mesher.AddMeshData(meshdata);

                return true;
            }catch (Exception ex) { return false; }
        }
        
    }
}
