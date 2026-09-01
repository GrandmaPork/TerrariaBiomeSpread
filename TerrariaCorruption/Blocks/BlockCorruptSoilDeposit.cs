using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

using Vintagestory.API.Util;

namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptSoilDeposit : BlockSoilDeposit
    {
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            GrassTick grassTick = extra as GrassTick;
            world.BlockAccessor.SetBlock(grassTick.Grass.BlockId, pos);
            if (grassTick.TallGrass != null && world.BlockAccessor.GetBlock(pos.UpCopy()).BlockId == 0)
            {
                world.BlockAccessor.SetBlock(grassTick.TallGrass.BlockId, pos.UpCopy());
            }

            //corruption
            if (api.Side == EnumAppSide.Server)
            {
                (api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CorruptionNeighbor(pos);
            }
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;
            bool flag = false;
            BlockPos blockPos = pos.UpCopy();
            Block block;
            if (world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) < growthLightLevel || isSmotheringBlock(world, blockPos))
            {
                block = tryGetBlockForDying(world);
            }
            else
            {
                flag = true;
                block = tryGetBlockForGrowing(world, pos);
            }

            if (block != null)
            {
                extra = new GrassTick
                {
                    Grass = block,
                    TallGrass = (flag ? getTallGrassBlock(world, blockPos, offThreadRandom) : null)
                };
            }
            else
            {
                // corruption
                if (api.Side == EnumAppSide.Server)
                {
                    if ((api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CheckNeighbors(pos) ?? false) // check for corrupt neighbors
                    {
                        extra = new GrassTick()
                        {
                            Grass = this,
                            TallGrass = null
                        };
                        return true;
                    }
                }
            }

            return extra != null;
        }
    }
}