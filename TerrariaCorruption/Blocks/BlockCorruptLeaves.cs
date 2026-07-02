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
        string climateColorMapInt;
        string seasonColorMapInt;

        public override string ClimateColorMapForMap => climateColorMapInt;
        public override string SeasonColorMapForMap => seasonColorMapInt;


        //public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
        //{
        //    base.OnCollectTextures(api, textureDict);

        //    climateColorMapInt = ClimateColorMap;
        //    seasonColorMapInt = SeasonColorMap;
        //    string grown = Code.SecondCodePart();
        //    if (grown.StartsWithOrdinal("grown"))
        //    {
        //        if (!int.TryParse(grown.Substring(5), out ExtraColorBits)) ExtraColorBits = 0;
        //    }

        //    // Branchy leaves - guard against shapes that have no elements
        //    if (api.Side == EnumAppSide.Client && SeasonColorMap == null)
        //    {
        //        var clientApi = api as ICoreClientAPI;
        //        var shape = clientApi?.TesselatorManager.GetCachedShape(Shape.Base);
        //        if (shape?.Elements != null && shape.Elements.Length > 0)
        //        {
        //            var elem = shape.Elements[0];
        //            if (!string.IsNullOrEmpty(elem.ClimateColorMap)) climateColorMapInt = elem.ClimateColorMap;
        //            if (!string.IsNullOrEmpty(elem.SeasonColorMap)) seasonColorMapInt = elem.SeasonColorMap;
        //        }
        //    }
        //}


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


        //public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        //{
        //    return new ItemStack(world.GetBlock(CodeWithParts("placed", LastCodePart())));
        //}


        //public override bool DisplacesLiquids(IBlockAccessor blockAccess, BlockPos pos)
        //{
        //    return false;  //Needed for branchy leaves (which have solid sides, perhaps they shouldn't?)
        //}

        ////thorns
        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact)
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);

        }

    }
}