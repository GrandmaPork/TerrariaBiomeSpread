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

    internal class GrassTick
    {
        public Block Grass;
        public Block TallGrass;
    }
    public class BlockCorruptSoil : BlockSoil
    {
        protected List<AssetLocation> tallGrassCodes = new List<AssetLocation>();
        protected string[] growthStages = new string[] { "none", "verysparse", "sparse", "normal" };
        protected string[] tallGrassGrowthStages = new string[] { "veryshort", "short", "mediumshort", "medium", "tall", "verytall" };

        protected int growthLightLevel;
        protected string growthBlockLayer;
        protected float tallGrassGrowthChance;
        protected BlockLayerConfig blocklayerconfig;
        protected const int chunksize = GlobalConstants.ChunkSize;

        Random rnd = new Random();

        protected float growthChanceOnTick = 0.16f;

        public bool growOnlyWhereRainfallExposed = false;

        protected virtual int MaxStage => 3;
        GenBlockLayers genBlockLayers;

        private const int FullyGrownStage = 3;
        int GrowthStage(string stage)
        {
            if (stage == "normal") return FullyGrownStage;
            if (stage == "sparse") return 2;
            if (stage == "verysparse") return 1;
            return 0;
        }

        protected int currentStage;
        public int CurrentStage => currentStage;



        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            growthLightLevel = Attributes?["growthLightLevel"] != null ? Attributes["growthLightLevel"].AsInt(7) : 7;
            growthBlockLayer = Attributes?["growthBlockLayer"]?.AsString("l1soilwithgrass");
            tallGrassGrowthChance = Attributes?["tallGrassGrowthChance"] != null ? Attributes["tallGrassGrowthChance"].AsFloat(0.3f) : 0.3f;
            growthChanceOnTick = Attributes?["growthChanceOnTick"] != null ? Attributes["growthChanceOnTick"].AsFloat(0.33f) : 0.33f;
            growOnlyWhereRainfallExposed = Attributes?["growOnlyWhereRainfallExposed"] != null ? Attributes["growOnlyWhereRainfallExposed"].AsBool(false) : false;

            tallGrassCodes.Add(new AssetLocation("terrariacorruption:corrupttallgrass-veryshort-free"));
            tallGrassCodes.Add(new AssetLocation("terrariacorruption:corrupttallgrass-short-free"));
            tallGrassCodes.Add(new AssetLocation("terrariacorruption:corrupttallgrass-mediumshort-free"));
            tallGrassCodes.Add(new AssetLocation("terrariacorruption:corrupttallgrass-medium-free"));
            tallGrassCodes.Add(new AssetLocation("terrariacorruption:corrupttallgrass-tall-free"));
            tallGrassCodes.Add(new AssetLocation("terrariacorruption:corrupttallgrass-verytall-free"));

            if (api.Side == EnumAppSide.Server)
            {
                (api as ICoreServerAPI).Event.ServerRunPhase(EnumServerRunPhase.RunGame, () =>
                {
                    genBlockLayers = api.ModLoader.GetModSystem<GenBlockLayers>();
                    blocklayerconfig = genBlockLayers.blockLayerConfig;
                });
            }

            currentStage = GrowthStage(Variant["grasscoverage"]);
        }

        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            //base.OnServerGameTick(world, pos, extra);

            GrassTick tick = extra as GrassTick;
            if (tick == null) return;
            world.BlockAccessor.ExchangeBlock(tick.Grass.BlockId, pos);

            BlockPos upPos = pos.UpCopy();

            if (tick.TallGrass != null && world.BlockAccessor.GetBlock(upPos).BlockId == 0)
            {
                world.BlockAccessor.SetBlock(tick.TallGrass.BlockId, upPos);
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

            if (offThreadRandom.NextDouble() > growthChanceOnTick) return false;

            if (growOnlyWhereRainfallExposed && world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1)
            {
                return false;
            }

            //bool nested = false;

            bool isGrowing = false;

            Block grass = null;
            BlockPos upPos = pos.UpCopy();

            bool lowLightLevel = world.BlockAccessor.GetLightLevel(pos, EnumLightLevelType.MaxLight) < growthLightLevel && (world.BlockAccessor.GetLightLevel(upPos, EnumLightLevelType.MaxLight) < growthLightLevel || world.BlockAccessor.GetBlock(upPos).SideSolid[BlockFacing.DOWN.Index]);
            bool smothering = isSmotheringBlock(world, upPos);

            int overheatingAmount = 0;
            world.BlockAccessor.WalkBlocks(pos.AddCopy(-3, 0, -3), pos.AddCopy(3, 1, 3), (block, x, y, z) =>
            {
                if (block.Attributes == null) return;
                overheatingAmount = Math.Max(overheatingAmount, (block.Attributes["killPlantRadius"].AsInt(0) - Math.Max(0, (int)pos.DistanceTo(x, y, z) - 1)));
            });

            bool die =
                (overheatingAmount >= 1 && currentStage == 3) ||
                (overheatingAmount >= 2 && currentStage == 2) ||
                (overheatingAmount >= 3 && currentStage == 1)
            ;


            if ((lowLightLevel || smothering || die) && currentStage > 0)
            {
                grass = tryGetBlockForDying(world);
            }
            else
            {
                if (overheatingAmount <= 0 && !smothering && !lowLightLevel && currentStage < MaxStage)
                {
                    isGrowing = true;
                    grass = tryGetBlockForGrowing(world, pos);
                }
            }
            

            if (grass != null)
            {
                extra = new GrassTick()
                {
                    Grass = grass,
                    TallGrass = isGrowing ? getTallGrassBlock(world, upPos, offThreadRandom) : null
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