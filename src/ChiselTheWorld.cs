using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ChiselTheWorld
{
    class ChiselTheWorld : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            api.RegisterItemClass("ItemWorldMinimizer", typeof(ItemWorldMinimizer));        
        }
    }


}
