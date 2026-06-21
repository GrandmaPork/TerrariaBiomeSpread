using System;

// Lets us use collections like List and HashSet.
// These are similar to arrays, but more flexible.
using System.Collections.Generic;
using System.Threading;
using Vintagestory.API.Client;



// Vintage Story API imports
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrariaCorruption
{
    /*
     * A ModSystem is a global manager for your mod.
     *
     * Unlike a Block class:
     * - there is only ONE ModSystem
     * - it exists the entire time the game is running
     * - it is good for timers and world-wide logic
     *
     * Think of it like a main controller program.
     */
    public class TerrariaCorruptionModSystem : ModSystem
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

        /*
         * We do this because repeatedly creating Random()
         * can produce repeated values.
         */
        private static readonly Random rnd = new Random(); // "static" means all instances share one generator.

        /*
         * HashSet:
         * 
         * Similar to an array, BUT:
         * - no duplicate entries
         * - faster searching
         *
         * We store all corruption block positions here.
         */

        public override void Start(ICoreAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptsoil", typeof(Blocks.BlockCorruptSoil));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptforestfloor", typeof(Blocks.BlockCorruptForestFloor));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptrock", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptgravel", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptmuddygravel", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptsand", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptlog", typeof(Blocks.BlockCorruptLog));
            //api.RegisterBlockClass(Mod.Info.ModID + ".corruptbambooleaves", typeof(Blocks.BlockCorruptLeavesWithMotion));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptbranchy", typeof(Blocks.BlockCorruptLeaves));
            //api.RegisterBlockClass(Mod.Info.ModID + ".corruptleavesbranchystatic", typeof(Blocks.BlockCorrupt));
            //api.RegisterBlockClass(Mod.Info.ModID + ".corruptfallenleaves", typeof(Blocks.BlockCorruptLeaves));
            //api.RegisterBlockClass(Mod.Info.ModID + ".corruptleavesnarrow", typeof(Blocks.BlockCorruptLeaves));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptleaves", typeof(Blocks.BlockCorruptLeaves));
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
            //api.Logger.Notification(api.World.ApplyColorMapOnRgba("climatePlantTint","seasonalGrass",0xFFFFFF,0, 0, 0).ToString());
            //api.Logger.Notification(api.World.ApplyColorMapOnRgba("corruptClimatePlantTint", "CorruptSeasonalGrass", 0xFFFFFF, 0, 0, 0).ToString());
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod server side: " + Lang.Get("terrariacorruption:hello"));
            // Save the server API into our variable
            sapi = api;

            /*
             * RegisterGameTickListener:
             *
             * Calls a function repeatedly every X milliseconds.
             *
             * Here:
             * - SpreadTimer is the function
             * - 3000 means every 3 seconds
             */
            //api.Event.RegisterGameTickListener(SpreadTimer, 3000);
        }
        public void spreadCorruption(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 2), rnd.Next(-1, 2));
            Block targetBlock = world.BlockAccessor.GetBlock(victim);
            string changePath = "corrupt" + targetBlock.Code.Path;

            AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);

            Block corruptBlock = world.GetBlock(changeCode);
            //corruptBlock = getCorruptedBlock(world = null, victim, extra);
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
        //public Block GetCorruptedBlock(IWorldAccessor world, BlockPos pos, object extra = null)
        //{
        //    BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 2), rnd.Next(-1, 2));
        //    Block targetBlock = world.BlockAccessor.GetBlock(victim);
        //    string changePath = "corrupt" + targetBlock.Code.Path;

        //    AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);

        //    Block corruptBlock = world.GetBlock(changeCode);
        //    if (corruptBlock == null) return;

        //    return changeCode;
        //}
    }
}