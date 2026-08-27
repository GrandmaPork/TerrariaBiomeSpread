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
                if (targetBlock.Attributes == null || ((targetBlock.Attributes["isCorrupt"].AsBool() == true) && (targetBlock.BlockId == 0) && (pos.X == x && pos.Y == y && pos.Z == z))) return; // if block is not air and not corrupt, and not itself, return true
                    shouldSpreadNeighbor = true;
            });
            return shouldSpreadNeighbor;
        }
        public void CorruptionNeighbor(BlockPos pos) // separate from spreadCorruption so OnGameTick can use the corruption spread function. Is called by blocks instead of CheckNeighbors. Was originally corruptionPosShort
        {
            //if (CheckNeighbors(pos)) // while there are still non-corrupt blocks nearby
            //{
            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 2), rnd.Next(-1, 2)); // find random neighbor
            if (sapi.Side == EnumAppSide.Server)
            {
                spreadCorruption(victim);
            }
            //await Task.Delay(30000);
            //}
        }
        public void spreadCorruption(BlockPos victim)
        {
            AssetLocation waterOverride = new AssetLocation("terrariacorruption", "corruptwater-still-7"); // pls work
            Block overrideCode = sapi.World.GetBlock(waterOverride);

            Block targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
            string changePath = "corrupt" + targetBlock.Code.Path;
            AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);

            Block corruptBlock = sapi.World.GetBlock(changeCode);
            if (corruptBlock == null) return;

            // spaghetti code yayy
            //while (targetBlock.Code.Path.StartsWith("tallplant-coopersreed-water")) // special condition. probably doesn't work. we'll see ig
            //{
            //    if (overrideCode == null) return; // just in case

            //    sapi.World.BlockAccessor.SetBlock(overrideCode.BlockId, victim); // place corrupt water
            //    sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim); // place corrupt block

            //    pillarCorruption(victim);
            //    victim.Y += 1;
            //}

            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();

            //while (targetBlock.Code.Path.StartsWith("log-") || (targetBlock.Code.Path.StartsWith("water-"))) // special feature
            //{
            //    pillarCorruption(victim);
            //}
        }
        public void pillarCorruption(BlockPos victim)
        {
            victim.Y += 1;
            Block targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
            string changePath = "corrupt" + targetBlock.Code.Path;
            AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);
            Block corruptBlock = sapi.World.GetBlock(changeCode);
            if (corruptBlock == null) return;

            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
        }
    }
}