using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

#nullable disable

namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptVines : BlockVines
    {
        private ICoreServerAPI sapi;
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
        { // corruptwildvine-code-side
            string[] array = Code.Path.Split('-');
            Block block = world.BlockAccessor.GetBlock(new AssetLocation("terrariacorruption", array[0] + "-" + array[^2].Replace("end", "section") + "-north"));
            return new ItemStack[1]
            {
            new ItemStack(block)
            };
        }

        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            string[] array = Code.Path.Split('-');
            return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation("terrariacorruption", array[0] + "-" + array[^2] + "-north")));
        }
    }
}