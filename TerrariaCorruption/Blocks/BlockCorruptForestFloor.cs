using System;
using System.Linq;
using TerrariaCorruption.BlockEntities;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

#nullable disable

namespace TerrariaCorruption.Blocks
{
    /// <summary>
    /// Handles eventual long-term transition to standard soil via server ticks.
    /// </summary>
    public class BlockCorruptForestFloor : BlockForestFloor
    {
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

            // corruption
            if ((api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CheckNeighbors(pos) ?? false) // check for corrupt neighbors
            {
                extra = new GrassTick()
                {
                    Grass = this,
                    TallGrass = null
                };
                return true;
            }

            if (offThreadRandom.NextDouble() > growthChanceOnTick) return false;

            if (world.BlockAccessor.GetRainMapHeightAt(pos) > pos.Y + 1)
            {
                return false;
            }

            return extra != null;
        }
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            //corruption
            if (api.Side == EnumAppSide.Server)
            {
                (api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CorruptionNeighbor(pos);
            }
        }
    }
}