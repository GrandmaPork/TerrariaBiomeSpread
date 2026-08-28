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
            //Mod.Logger.Notification("victim2: " + victim.AddCopy(0, 0, 0));
            AssetLocation waterOverride = new AssetLocation("terrariacorruption", "corruptwater-still-7"); // pls work
            Block overrideCode = sapi.World.GetBlock(waterOverride);

            Block targetBlock = sapi.World.BlockAccessor.GetBlock(victim); // targetBlock found here instead of inside NewCorruptBlock to avoid multiple calls to GetBlock

            // spaghetti code yayy
            //while (targetBlock.Code.Path.StartsWith("tallplant-coopersreed-water")) // special condition. probably doesn't work. we'll see ig
            //{
            //    if (overrideCode == null) return; // just in case

            //    sapi.World.BlockAccessor.SetBlock(overrideCode.BlockId, victim); // place corrupt water
            //    sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim); // place corrupt block

            //    pillarCorruption(victim);
            //    victim.Y += 1;
            //}

            Block corruptBlock = NewCorruptBlock(victim, targetBlock);
            if (corruptBlock == targetBlock) return;

            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();

            if (targetBlock.Code.Path.StartsWith("log-") || targetBlock.Code.Path.StartsWith("water-")) // special feature
            {
                Mod.Logger.Notification("pillarCorruption triggered: " + targetBlock.Code.Path);
                pillarCorruption(victim, targetBlock);
            }

        }
        public void pillarCorruption(BlockPos victim, Block targetBlock)
        {
            while (targetBlock.Code.Path.StartsWith("log-") || targetBlock.Code.Path.StartsWith("water-"))
            {
                victim.Y += 1;
                targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
                Block corruptBlock = NewCorruptBlock(victim, targetBlock);
                if (corruptBlock == targetBlock) return;

                sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
            }
        }
        public Block NewCorruptBlock(BlockPos victim, Block targetBlock) // optimized
        {
            AssetLocation findCode = new AssetLocation("terrariacorruption", "corrupt" + targetBlock.Code.Path);
            Block corruptBlock = sapi.World.GetBlock(findCode);
            if (corruptBlock == null) return targetBlock;
            return corruptBlock;
        }
    }
}