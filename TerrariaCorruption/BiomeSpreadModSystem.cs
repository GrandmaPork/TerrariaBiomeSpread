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
        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from biomespread modsystem: " + api.Side);
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from biomespread modsystem: " + api.Side);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from biomespread modsystem server side: " + Lang.Get("biomespread:hello"));
            // Save the server API into our variable
            sapi = api;
        }
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
        public Block NewCorruptBlock(BlockPos victim, Block targetBlock) // optimized
        {
            AssetLocation findCode = new AssetLocation("terrariacorruption", "corrupt" + targetBlock.Code.Path);
            Block corruptBlock = sapi.World.GetBlock(findCode);
            if (corruptBlock == null) return null;
            return corruptBlock;
        }
        public Block NewCorruptFluid(BlockPos victim) // after messing with code for quite a few hours, it seems like it would be better to add the corruptFluid check into SetCorruptBlock since no edge cases will be missed and no blocks will have a waterlogging issue, but it comes at the cost of a second call to GetBlock. This will hit performance, but it will be more readable and easier to maintain. 
        {
            Block check = sapi.World.BlockAccessor.GetBlock(victim, Fluid); // check water layer
            if (check.BlockId == 0) return null;
            //Mod.Logger.Notification("check: " + check.Code.Path);
            //Mod.Logger.Notification("check.BlockId: " + check.BlockId);

            Block waterOverride = sapi.World.GetBlock(new AssetLocation("terrariacorruption", "corrupt" + check.Code.Path));
            return waterOverride;
        }
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
        public void SetCorruptBlock(BlockPos victim, Block corruptBlock, Block corruptFluid) // to be added in soon
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
        public void SetCorruptBlock(BlockPos victim, Block corruptBlock) // to be added in soon
        {
            if (corruptBlock == null) return;
            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
        }
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
                    Mod.Logger.Notification("Replace " + targetBlock.Code.Path + " with " + corruptBlock.Code.Path);
                    SetCorruptBlock(victim, corruptBlock);
                    break;

                case "aquatic":
                case "aquaticplant":
                case "log":
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
        //
    }
}