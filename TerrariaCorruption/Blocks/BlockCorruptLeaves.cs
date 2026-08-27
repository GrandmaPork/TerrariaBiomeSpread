using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.API.Common.Entities;

#nullable disable

namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptLeaves : BlockLeaves
    {
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
                        if (targetBlock.Attributes?["isCorrupt"]?.AsBool() != true)
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

            return offThreadRandom.NextDouble() < 0.15;
        }


        Random rnd = new Random();
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            //Random offThreadRandom = rnd;

            //if (offThreadRandom.NextDouble() < 0.15)
            //{
            //    TreeAttribute tree = new TreeAttribute();
            //    tree.SetInt("x", pos.X);
            //    tree.SetInt("y", pos.Y);
            //    tree.SetInt("z", pos.Z);
            //    world.Api.Event.PushEvent("testForDecay", tree);
            //}

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

        //thorns
        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact) // I definitely forgot to come back to this
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            
        }

    }
}