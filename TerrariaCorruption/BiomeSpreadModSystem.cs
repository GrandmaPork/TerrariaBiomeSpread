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
        public void spreadCorruption(BlockPos victim)
        {
            Block corruptBlock;
            Block corruptFluid = null;
            Block targetBlock = sapi.World.BlockAccessor.GetBlock(victim); // targetBlock found here instead of inside NewCorruptBlock to avoid multiple calls to GetBlock
                                                                           //var specialTest = targetBlock.Code.Path.Split('-');

            //Mod.Logger.Notification("switchcase test: " + targetBlock.Code.Path.Split('-')[0]); // test was successful
            switch (targetBlock.Code.Path.Split('-')[0]) // check for specific blocktypes. more readable than a giant if statement
            {
                case "looseflints":
                case "looseores":
                case "loosestones":
                case "aquaticplant":
                case "aquatic":
                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    if (corruptBlock == null) return;
                    corruptFluid = NewCorruptFluid(victim, targetBlock);
                    if (corruptFluid != null)
                    {
                        sapi.World.BlockAccessor.SetBlock(corruptFluid.BlockId, victim, Fluid);
                    }
                    sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                    sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
                    break;
                case "log":
                case "water":
                case "crop":
                case "mushroom":
                case "farmland":
                    //Mod.Logger.Notification("special condition triggered: " + targetBlock.Code.Path);
                    specialConditions(victim, targetBlock);
                    break;
                default:
                    if (targetBlock.Code.Path.Contains("-aged-")) return;

                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    if (corruptBlock == null) return;

                    // needs to be optimized, possibly a second case statement
                    if (targetBlock.Code.Path.StartsWith("tallplant-coopersreed-") ||
                        targetBlock.Code.Path.StartsWith("tallplant-tule-") ||
                        targetBlock.Code.Path.StartsWith("tallplant-papyrus-") ||
                        targetBlock.Code.Path.StartsWith("leaves"))
                    {
                        corruptFluid = NewCorruptFluid(victim, targetBlock);
                    }

                    SetBlockCorruption(victim, corruptBlock, corruptFluid);
                    break;
            }
        }
        public void pillarCorruption(BlockPos victim, Block targetBlock) // needs to include a check for aquatic plants
        {
            while (targetBlock.Code.Path.StartsWith("log-") || 
                targetBlock.Code.Path.StartsWith("water-") || 
                targetBlock.Code.Path.StartsWith("aquatic")) // technically never called for aquatic plants
            {
                victim.Y += 1;
                targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
                Block corruptBlock = NewCorruptBlock(victim, targetBlock);
                //Block corruptFluid = NewCorruptFluid(victim, targetBlock);
                if (corruptBlock == null) return;

                SetBlockCorruption(victim, corruptBlock);
            }
        }
        public Block NewCorruptBlock(BlockPos victim, Block targetBlock) // optimized
        {
            AssetLocation findCode = new AssetLocation("terrariacorruption", "corrupt" + targetBlock.Code.Path);
            Block corruptBlock = sapi.World.GetBlock(findCode);
            if (corruptBlock == null) return null;
            return corruptBlock;
        }
        public Block NewCorruptFluid(BlockPos victim, Block targetBlock)
        {
            Block check = sapi.World.BlockAccessor.GetBlock(victim, Fluid); // check water layer
            Mod.Logger.Notification("check: " + check.Code.Path);
            Mod.Logger.Notification("check.BlockId: " + check.BlockId);

            Block waterOverride = sapi.World.GetBlock(new AssetLocation("terrariacorruption", "corrupt" + check.Code.Path));
            if (check.BlockId == 0) return null;
            return waterOverride;
            //if (waterOverride == null) return; // moved out of function, function used to be a void function

            //if (check.BlockId != 0)
            //{
            //    sapi.World.BlockAccessor.SetBlock(waterOverride.BlockId, victim, Fluid); 
            //}
        }
        public void SetBlockCorruption(BlockPos victim, Block corruptBlock, Block corruptFluid) // to be added in soon
        {
            if (corruptBlock == null) return;
            if (corruptFluid != null)
            {
                sapi.World.BlockAccessor.SetBlock(corruptFluid.BlockId, victim, Fluid);
            }
            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
        }
        public void SetBlockCorruption(BlockPos victim, Block corruptBlock) // to be added in soon
        {
            if (corruptBlock == null) return;
            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
        }
        public void specialConditions(BlockPos victim, Block targetBlock)
        {
            AssetLocation specialCode;
            Block corruptBlock;

            // eventually make following code a switch case statement
            switch (targetBlock.Code.Path.Split('-')[0])
            {
                case "mushroom":
                    if (targetBlock.Code.Path.EndsWith("-normal")) // almost optimized
                    {
                        specialCode = new AssetLocation("terrariacorruption", "corruptmushroom-witchhat-" + targetBlock.Code.Path.Split('-')[2]); // concat state into code
                        corruptBlock = sapi.World.GetBlock(specialCode);

                        if (corruptBlock != null)
                        {
                            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
                        }
                    }
                    else // mushroom-type-state-direction
                    {
                        //Mod.Logger.Notification("specialConditions `mushroom-` triggered: " + targetBlock.Code.Path);
                        var parts = targetBlock.Code.Path.Split('-'); // split at dashes
                        specialCode = new AssetLocation("terrariacorruption", "corruptmushroom-tinderhoof-" + parts[2] + "-" + parts[3]);
                        corruptBlock = sapi.World.GetBlock(specialCode);
                        if (corruptBlock != null)
                        {
                            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
                        }
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
                    if (corruptBlock != targetBlock)
                    {
                        sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                        sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
                    }
                    break;
                case "crop":
                    corruptBlock = sapi.World.GetBlock(new AssetLocation("terrariacorruption", "deadcrop"));
                    if (corruptBlock != null)
                    {
                        sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                        sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
                    }
                    break;
                case "water":
                case "log":
                    //Mod.Logger.Notification("pillarCorruption triggered: " + targetBlock.Code.Path);
                    corruptBlock = NewCorruptBlock(victim, targetBlock);
                    sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                    pillarCorruption(victim, targetBlock);
                    break;
                default:
                    Mod.Logger.Notification("specialConditions default triggered: " + targetBlock.Code.Path);
                    break;
            }
        }
    }

}