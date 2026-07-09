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

namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptSoilDeposit : BlockSoilDeposit
    {
        //private int soilBlockId;

        //protected override int MaxStage => 1;
        Random rnd = new Random();
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            //base.OnServerGameTick(world, pos, extra);

            GrassTick grassTick = extra as GrassTick;

            world.BlockAccessor.SetBlock(grassTick.Grass.BlockId, pos);
            if (grassTick.TallGrass != null && world.BlockAccessor.GetBlock(pos.UpCopy()).BlockId == 0)
            {
                world.BlockAccessor.SetBlock(grassTick.TallGrass.BlockId, pos.UpCopy());
            }

            //corruption

            float val = rnd.Next();

            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 2), rnd.Next(-1, 2));
            Block targetBlock = world.BlockAccessor.GetBlock(victim);
            string changePath = "corrupt" + targetBlock.Code.Path;

            AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);

            Block corruptBlock = world.GetBlock(changeCode);
            if (corruptBlock == null) return;

            world.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            world.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();

            while (targetBlock.Code.Path.StartsWith("log-"))
            {
                victim.Y += 1;
                targetBlock = world.BlockAccessor.GetBlock(victim);
                changePath = "corrupt" + targetBlock.Code.Path;
                changeCode = new AssetLocation("terrariacorruption", changePath);
                corruptBlock = world.GetBlock(changeCode);
                if (corruptBlock == null) return;

                world.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                world.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
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
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        for (int k = -1; k <= 1; k++)
                        {
                            if (i == 0 && j == 0 && k == 0) continue;
                            BlockPos victim = pos.AddCopy(i, j, k);
                            Block targetBlock = world.BlockAccessor.GetBlock(victim);
                            if ((targetBlock.Attributes?["isCorrupt"]?.AsBool() != true) && (targetBlock.BlockId != 0)) //if block is not air and not corrupt, tick the block
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
                }
            }

            return extra != null;
        }
    }
}