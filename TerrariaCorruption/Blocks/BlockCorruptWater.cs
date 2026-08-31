using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

#nullable disable

namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptWater : BlockWater
    {

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            if (iceBlock != null)
            {
                world.BlockAccessor.SetBlock(iceBlock.Id, pos, 2); // change to corrupt ice later
            }
            // corruption
            (api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CorruptionNeighbor(pos);
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;

            if((api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CheckNeighbors(pos) ?? false)
            {
                return false;
            }

            if (!GlobalConstants.MeltingFreezingEnabled)
            {
                return false;
            }

            if (freezable && offThreadRandom.NextDouble() < 0.6 && world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y)
            {
                BlockPos pos2 = pos.Copy();
                for (int i = 0; i < BlockFacing.HORIZONTALS.Length; i++)
                {
                    BlockFacing.HORIZONTALS[i].IterateThruFacingOffsets(pos2);
                    if ((world.BlockAccessor.GetBlock(pos2, 2) is BlockLakeIce || world.BlockAccessor.GetBlock(pos2).Replaceable < 6000) && world.BlockAccessor.GetClimateAt(pos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature <= freezingPoint)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

}