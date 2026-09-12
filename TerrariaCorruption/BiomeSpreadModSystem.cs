using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace TerrariaCorruption
{
    public class BiomeSpreadModSystem : ModSystem
    {
        /*
         * This stores the server API.
         *
         * The server API lets us:
         * - access the world
         * - place blocks
         * - register timers
         * - access players
         */
        private ICoreServerAPI sapi;
        private const int Fluid = 2;
        private static readonly Random rnd = new Random(); // "static" means all instances share one generator.
        /// <summary>
        /// Called when the mod system starts on either side (client or server).
        /// Use this for general initialization that does not require side-specific APIs.
        /// </summary>
        /// <param name="api">The core API for the current side.</param>
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from biomespread modsystem: " + api.Side);
        }
        /// <summary>
        /// Called when the mod system starts on the client side.
        /// Use this to register client-only resources such as renderers or client events.
        /// </summary>
        /// <param name="api">The client API.</param>
        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from biomespread modsystem: " + api.Side);
        }
        /// <summary>
        /// Called when the mod system starts on the server side.
        /// Save the server API here for world and block operations.
        /// </summary>
        /// <param name="api">The server API.</param>
        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from biomespread modsystem server side: " + Lang.Get("biomespread:hello"));
            // Save the server API into our variable
            sapi = api;
        }
        /// <summary>
        /// Check the 3x3x3 neighborhood around <paramref name="pos"/> and
        /// return true if there is any non-corrupt, non-air block (excluding the center position).
        /// Note: this method only inspects neighbors; it does not perform spreading. Call
        /// <see cref="CorruptionNeighbor(BlockPos)"/> (for example from OnGameTick) or an equivalent
        /// tick/event handler to actually trigger corruption spreading.
        /// </summary>
        /// <param name="pos">The center position to check around.</param>
        /// <returns>True when at least one valid neighbor block exists to spread to; otherwise false.</returns>
        public bool CheckNeighbors(BlockPos pos) // returns true if non-corrupt block detected
        {
            bool shouldSpreadNeighbor = false;

            sapi.World.BlockAccessor.WalkBlocks(pos.AddCopy(-1, -1, -1), pos.AddCopy(1, 1, 1), (targetBlock, x, y, z) =>
            {
                if (targetBlock.Attributes == null) return;
                if ((targetBlock.Attributes["isCorrupt"].AsBool() == true) || (targetBlock.BlockId == 0) || (pos.X == x && pos.Y == y && pos.Z == z)) return; // if block is not air and not corrupt, and not itself, return true
                shouldSpreadNeighbor = true;
            });
            return shouldSpreadNeighbor;
        }
        /// <summary>
        /// Pick a random neighboring position around <paramref name="pos"/> and attempt to spread corruption there.
        /// This is a lightweight wrapper used by blocks and tick handlers to trigger spreading without additional checks.
        /// </summary>
        /// <param name="pos">The source position to pick a neighboring target from.</param>
        public void CorruptionNeighbor(BlockPos pos) // separate from spreadCorruption so OnGameTick can use the corruption spread function. Is called by blocks instead of CheckNeighbors. Was originally corruptionPosShort
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 2), rnd.Next(-1, 2)); // find random neighbor
            //Mod.Logger.Notification("victim1: " + victim.AddCopy(0, 0, 0));
            //Mod.Logger.Notification("pos: " + pos.AddCopy(0, 0, 0));

            if (sapi.Side == EnumAppSide.Server)
            {
                spreadCorruption(victim);
            }
        }
        /// <summary>
        /// Resolve and return the corrupt replacement block for the given <paramref name="targetBlock"/>.
        /// The asset code is looked up in the terrariacorruption mod by prefixing the target code path with "corrupt".
        /// </summary>
        /// <param name="victim">The position where the block will be replaced (not used for lookup but kept for parity).</param>
        /// <param name="targetBlock">The original block to be replaced.</param>
        /// <returns>The corrupt replacement block, or null if none exists.</returns>
        public Block NewCorruptBlock(BlockPos victim, Block targetBlock) // optimized
        {
            AssetLocation findCode = new AssetLocation("terrariacorruption", "corrupt" + targetBlock.Code.Path);
            Block corruptBlock = sapi.World.GetBlock(findCode);
            if (corruptBlock == null) return null;
            return corruptBlock;
        }
        /// <summary>
        /// Check the fluid layer at <paramref name="victim"/> and return a corrupt fluid block if one exists.
        /// This handles waterlogged or fluid-replaced variants by looking up a "corrupt" prefixed asset for the fluid.
        /// </summary>
        /// <param name="victim">The block position to check the fluid layer for.</param>
        /// <returns>The corrupt fluid block, or null if none is present.</returns>
        // after messing with code for quite a few hours, it seems like it would be better to add the corruptFluid check into SetCorruptBlock
        // since no edge cases will be missed and no blocks will have a waterlogging issue, but it comes at the cost of a second call to GetBlock.
        // This will hit performance, but it will be more readable and easier to maintain.
        public Block NewCorruptFluid(BlockPos victim)
        {
            Block check = sapi.World.BlockAccessor.GetBlock(victim, Fluid); // check water layer
            if (check.BlockId == 0) return null;
            //Mod.Logger.Notification("check: " + check.Code.Path);
            //Mod.Logger.Notification("check.BlockId: " + check.BlockId);

            Block waterOverride = sapi.World.GetBlock(new AssetLocation("terrariacorruption", "corrupt" + check.Code.Path));
            return waterOverride;
        }
        /// <summary>
        /// Continue corrupting upward through a vertical column of blocks that match pillar-like types
        /// (logs, water, aquatic blocks). For each matching block the corresponding corrupt block (and
        /// corrupt fluid if present) is applied and the position moves up one Y level.
        /// </summary>
        /// <param name="victim">Starting position for the pillar corruption.</param>
        /// <param name="targetBlock">The block at the starting position.</param>
        public void pillarCorruption(BlockPos victim, Block targetBlock)
        {
            while (targetBlock.Code.Path.StartsWith("log-") ||
                targetBlock.Code.Path.StartsWith("water-") ||
                targetBlock.Code.Path.StartsWith("aquatic"))
            {
                victim.Y += 1;
                targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
                Block corruptBlock = NewCorruptBlock(victim, targetBlock);
                if (corruptBlock == null) return;

                Block corruptFluid = NewCorruptFluid(victim);
                SetCorruptBlock(victim, corruptBlock, corruptFluid);
            }
        }
        /// <summary>
        /// Handle special-case corruption behavior for specific block types such as mushrooms, farmland,
        /// crops, aquatic plants, logs and similar. This method performs custom replacements and additional
        /// actions (for example replacing crop above farmland with a dead plant) before setting corrupt blocks.
        /// </summary>
        /// <param name="victim">The position to corrupt.</param>
        /// <param name="targetBlock">The block currently at <paramref name="victim"/>.</param>
        public void specialConditions(BlockPos victim, Block targetBlock)
        {
            AssetLocation specialCode;
            Block corruptBlock;
            Block corruptFluid;

            switch (targetBlock.Code.Path.Split('-')[0])
            {
                case "mushroom":
                    if (targetBlock.Code.Path.EndsWith("-normal")) // almost optimized
                    {
                        specialCode = new AssetLocation("terrariacorruption", "corruptmushroom-witchhat-" + targetBlock.Code.Path.Split('-')[2]); // concat state into code
                        corruptBlock = sapi.World.GetBlock(specialCode);

                        SetCorruptBlock(victim, corruptBlock);
                    }
                    else // mushroom-type-state-direction
                    {
                        //Mod.Logger.Notification("specialConditions `mushroom-` triggered: " + targetBlock.Code.Path);
                        var parts = targetBlock.Code.Path.Split('-'); // split at dashes
                        specialCode = new AssetLocation("terrariacorruption", "corruptmushroom-funeralbell-" + parts[2] + "-" + parts[3]);
                        corruptBlock = sapi.World.GetBlock(specialCode);

                        SetCorruptBlock(victim, corruptBlock);
                    }
                    break;

                case "farmland":
                    Block checkAbove = sapi.World.BlockAccessor.GetBlock(victim.AddCopy(0, 1, 0)); // find above block
                    Block deadPlantBlock = sapi.World.GetBlock(new AssetLocation("deadcrop"));
                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    if ((checkAbove.BlockId != 0) && (deadPlantBlock != null)) // check for air first
                    {
                        sapi.World.BlockAccessor.SetBlock(deadPlantBlock.BlockId, victim.AddCopy(0, 1, 0));
                    }

                    SetCorruptBlock(victim, corruptBlock);
                    break;

                case "crop":
                    corruptBlock = sapi.World.GetBlock(new AssetLocation("deadcrop"));
                    if (corruptBlock == null) return;
                    //Mod.Logger.Notification("Replace " + targetBlock.Code.Path + " with " + corruptBlock.Code.Path);
                    SetCorruptBlock(victim, corruptBlock);
                    break;

                case "aquatic":
                case "aquaticplant":
                case "log":
                case "wildvine":
                case "water":
                    //Mod.Logger.Notification("pillarCorruption triggered: " + targetBlock.Code.Path);
                    corruptFluid = NewCorruptFluid(victim);
                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    if (corruptBlock == null) return;
                    SetCorruptBlock(victim, corruptBlock, corruptFluid);
                    pillarCorruption(victim, targetBlock);
                    break;

                default:
                    Mod.Logger.Error("specialConditions default triggered??: " + targetBlock.Code.Path);
                    break;
            }
        }
        /// <summary>
        /// Main entry point for attempting to spread corruption to the block at <paramref name="victim"/>.
        /// This method inspects the target block's type and applies the appropriate corrupt replacement,
        /// handling special cases and fluid replacements where necessary.
        /// </summary>
        /// <param name="victim">The target position for corruption.</param>
        public void spreadCorruption(BlockPos victim)
        {
            Block corruptBlock;
            Block corruptFluid = null;
            Block targetBlock = sapi.World.BlockAccessor.GetBlock(victim); // targetBlock found here instead of inside NewCorruptBlock to avoid multiple calls to GetBlock

            switch (targetBlock.Code.Path.Split('-')[0]) // check for specific blocktypes. more readable than a giant if statement
            {
                case "fruittree":
                    while (targetBlock.Code.Path.Split('-')[0] == "fruitree")
                    {
                        sapi.World.BlockAccessor.SetBlock(0, victim); // not worth corrupting at the moment
                        sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();

                        victim.Y += 1;
                        targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
                    }
                    break;
                case "looseflints":
                case "looseboulders":
                case "looseores":
                case "loosestones":
                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    corruptFluid = NewCorruptFluid(victim);
                    SetCorruptBlock(victim, corruptBlock, corruptFluid);
                    break;

                case "aquatic":
                case "aquaticplant":
                case "crop":
                case "farmland":
                case "log":
                case "mushroom":
                case "water":
                case "wildvine":
                    //Mod.Logger.Notification("special condition triggered: " + targetBlock.Code.Path);
                    specialConditions(victim, targetBlock);
                    break;

                default:
                    //Mod.Logger.Notification("default condition triggered: " + targetBlock.Code.Path);

                    if (targetBlock.Code.Path.Contains("-aged-")) return; // don't touch any aged (might need specific checks in the future)

                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    if (corruptBlock == null) return; // should be kept even though SetCorruptBlock protects against null inputs

                    // needs to be optimized, possibly a second case statement
                    if (targetBlock.Code.Path.StartsWith("tallplant-coopersreed-") ||
                        targetBlock.Code.Path.StartsWith("tallplant-tule-") ||
                        targetBlock.Code.Path.StartsWith("tallplant-papyrus-") ||
                        targetBlock.Code.Path.StartsWith("leaves")) // can be moved to case statement above
                    {
                        //Mod.Logger.Notification("fluid condition triggered: " + targetBlock.Code.Path);
                        corruptFluid = NewCorruptFluid(victim);
                    }

                    SetCorruptBlock(victim, corruptBlock, corruptFluid); // corruptFluid only present because of the previous if statement
                    break;
            }
        }
        /// <summary>
        /// Set a corrupt block and optionally a corrupt fluid at <paramref name="victim"/>.
        /// If <paramref name="corruptFluid"/> is non-null the fluid layer will be set first.
        /// The chunk is marked modified when changes are applied.
        /// </summary>
        /// <param name="victim">Position to modify.</param>
        /// <param name="corruptBlock">The corrupt block to set (may be null).</param>
        /// <param name="corruptFluid">Optional corrupt fluid to set in the fluid layer.</param>
        public void SetCorruptBlock(BlockPos victim, Block corruptBlock, Block corruptFluid) 
        {
            if (corruptFluid != null)
            {
                sapi.World.BlockAccessor.SetBlock(corruptFluid.BlockId, victim, Fluid);
                sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified(); // so the chunk is updated in case corruptBlock is null
            }
            if (corruptBlock == null) return; // after corruptFluid check in case block is corrupt but water isn't
            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
        }
        /// <summary>
        /// Set a corrupt block at <paramref name="victim"/> and mark the chunk modified.
        /// This overload does not modify the fluid layer.
        /// </summary>
        /// <param name="victim">Position to modify.</param>
        /// <param name="corruptBlock">The corrupt block to set (may be null).</param>
        public void SetCorruptBlock(BlockPos victim, Block corruptBlock) 
        {
            if (corruptBlock == null) return;
            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
        }
        //
    }
}