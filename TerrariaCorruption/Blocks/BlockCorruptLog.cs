using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

#nullable disable


namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptLog : Block, IBlockLog, ICustomChiselMaterialName
    {
        //public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        //{
        //    return Drops[0].ResolvedItemstack.Clone();
        //}


        public override void AddMiningTierInfo(StringBuilder sb, IWorldAccessor world, BlockPos pos)
        {
            if (Code.PathStartsWith("log-grown"))
            {
                // stone axe can cut normal wood (woodtier 3) cannot cut tropical woods except Kapok (which is soft); copper/scrap axe cannot cut ebony
                int woodTier = Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
                int requiredMiningTier = GetRequiredMiningTier(world, pos);
                woodTier += requiredMiningTier - 4;
                if (woodTier < requiredMiningTier) woodTier = requiredMiningTier;

                string tierName = "?";
                if (woodTier < miningTierNames.Length)
                {
                    tierName = miningTierNames[woodTier];
                }

                sb.AppendLine(Lang.Get("Requires tool tier {0} ({1}) to break", woodTier, tierName == "?" ? tierName : Lang.Get(tierName)));
            }
            else
            {
                base.AddMiningTierInfo(sb, world, pos);
            }
        }

        string ICustomChiselMaterialName.GetName(ItemStack itemStack)
        {
            return Lang.Get("chiselmaterialnamewithorientation-" + Variant["rotation"], itemStack.GetName());
        }

        //corruption
        Random rnd = new Random();
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            base.OnServerGameTick(world, pos, extra);

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
            //TerrariaCorruptionModSystem.spreadCorruption(world, victim, extra);
        }

        public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;


            //bool nested = false;

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

            return extra != null;
        }
    }
}