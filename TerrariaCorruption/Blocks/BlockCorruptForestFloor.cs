using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;
using Vintagestory.GameContent;

#nullable disable

namespace TerrariaCorruption.Blocks
{

    /// <summary>
    /// Handles eventual long-term transition to standard soil via server ticks.
    /// </summary>
    public class BlockCorruptForestFloor : BlockForestFloor
    {
        protected string[] growthStages = new string[] { "0", "1", "2", "3", "4", "5", "6", "7" };
        protected int growthLightLevel;
        protected const int chunksize = GlobalConstants.ChunkSize;
        protected float growthChanceOnTick = 0.16f;
        protected int mapColorTextureSubId;
        protected CompositeTexture grassTex;


        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;

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

            if (offThreadRandom.NextDouble() > growthChanceOnTick) return false;

            if (world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1)
            {
                return false;
            }

            return extra != null;
        }
        //corruption
        Random rnd = new Random();
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {

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
    }
}