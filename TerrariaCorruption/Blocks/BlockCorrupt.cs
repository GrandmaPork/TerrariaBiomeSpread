using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TerrariaCorruption.BlockEntities;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.ServerMods;

#nullable disable

namespace TerrariaCorruption.Blocks
{
    public class BlockCorrupt : Block // was originally BlockCorruptRock but had no reason to be limited to rocks
    {
        public bool shouldCheckNeighbors = false;
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            base.OnServerGameTick(world, pos, extra);
            (api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CorruptionNeighbor(pos);
        }
        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;
            return (api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CheckNeighbors(pos) ?? false;
        }
    }

}