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
    public class BlockCorruptForestFloor : Block, IBlockForestFloor   //WithGrassOverlay
    {
        protected string[] growthStages = new string[] { "0", "1", "2", "3", "4", "5", "6", "7" };
        protected int growthLightLevel;
        protected const int chunksize = GlobalConstants.ChunkSize;
        protected float growthChanceOnTick = 0.16f;
        protected int mapColorTextureSubId;
        protected CompositeTexture grassTex;


        public int CurrentLevel()
        {
            return ForestFloorHelper.MaxStage - (Code.Path[Code.Path.Length - 1] - '0');
        }


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api is ICoreClientAPI)
            {
                Block fullCoverBlock = api.World.GetBlock(this.CodeWithParts("7"));
                mapColorTextureSubId = fullCoverBlock.Textures["specialSecondTexture"].Baked.TextureSubId;

                var soilBlock = api.World.GetBlock(new AssetLocation("soil-low-normal"));
                if (soilBlock.Textures == null || !soilBlock.Textures.TryGetValue("specialSecondTexture", out grassTex))
                {
                    grassTex = soilBlock.Textures?.First().Value;
                }
            }
        }

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

        protected bool isSmotheringBlock(IWorldAccessor world, BlockPos pos)
        {
            Block block = world.BlockAccessor.GetBlock(pos, BlockLayersAccess.Fluid);
            if (block is BlockLakeIce || block.LiquidLevel > 1) return true;
            block = world.BlockAccessor.GetBlock(pos);
            return block.SideSolid[BlockFacing.DOWN.Index] && block.SideOpaque[BlockFacing.DOWN.Index] || block is BlockLava;
        }

        protected Block tryGetBlockForGrowing(IWorldAccessor world, BlockPos pos)
        {
            return null;
        }

        protected Block tryGetBlockForDying(IWorldAccessor world)
        {
            return null;
        }


        protected int getClimateSuitedGrowthStage(IWorldAccessor world, BlockPos pos, ClimateCondition climate)
        {
            return CurrentLevel();
        }


        public override int GetColor(ICoreClientAPI capi, BlockPos pos)
        {
            float grassLevel = Variant["grass"].ToInt() / 7f;

            if (grassLevel == 0) return base.GetColorWithoutTint(capi, pos);

            int? textureSubId = grassTex?.Baked.TextureSubId;
            if (textureSubId == null)
            {
                return ColorUtil.WhiteArgb;
            }

            int grassColor = capi.BlockTextureAtlas.GetAverageColor((int)textureSubId);

            if (ClimateColorMapResolved != null)
            {
                grassColor = capi.World.ApplyColorMapOnRgba(ClimateColorMapResolved, SeasonColorMapResolved, grassColor, pos.X, pos.Y, pos.Z, false);
            }

            int soilColor = capi.BlockTextureAtlas.GetAverageColor((int)Textures["up"].Baked.TextureSubId);

            return ColorUtil.ColorOverlay(soilColor, grassColor, grassLevel);
        }


        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            if (facing == BlockFacing.UP)
            {
                return capi.World.ApplyColorMapOnRgba(ClimateColorMap, SeasonColorMap, capi.BlockTextureAtlas.GetRandomColor(mapColorTextureSubId, rndIndex), pos.X, pos.Y, pos.Z);
            }
            return base.GetRandomColor(capi, pos, facing, rndIndex);
        }
    }
}