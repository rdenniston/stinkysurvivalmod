using Cairo;
using StinkySurvivalMod.EntityBehaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace StinkySurvivalMod.Utility
{
    public static class EntityExtensions
    {
        public static string[] lilanimals = new string[] { "player","hare", "raccoon", "fox", "chicken", "gazelle", "deer", "goat", "elk", "hyena", "pig", "sheep", "wolf" };
        public static string[] biganimals = new string[] {  "moose", "bear",};
        public static void addExtraBehaviors(this Entity entity) {

            if (entity is EntityPlayer || entity is EntityAgent) 
            {

                    if (lilanimals.Contains(entity.Code.FirstCodePart()) || biganimals.Contains(entity.Code.FirstCodePart()))
                    {
                        entity.AddBehavior(new BehaviorCanPoo(entity));
                    }
            }

        }

    }
}
