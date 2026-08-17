using HarmonyLib;
using System;

// Lets us use collections like List and HashSet.
// These are similar to arrays, but more flexible.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;



// Vintage Story API imports
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

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
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptsoildeposit", typeof(Blocks.BlockCorruptSoilDeposit));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptforestfloor", typeof(Blocks.BlockCorruptForestFloor));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptrock", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptgravel", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptmuddygravel", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptsand", typeof(Blocks.BlockCorruptRock));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptlog", typeof(Blocks.BlockCorruptLog));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptbranchy", typeof(Blocks.BlockCorruptLeaves));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptleaves", typeof(Blocks.BlockCorruptLeaves));
            api.RegisterBlockClass(Mod.Info.ModID + ".corruptwater", typeof(Blocks.BlockCorruptWater));

            api.RegisterBlockClass(Mod.Info.ModID + ".corruptknappingsurface", typeof(Blocks.BlockCorruptKnappingSurface));
            api.RegisterBlockClass(Mod.Info.ModID + ".shadoworb", typeof(Blocks.BlockShadowOrb)); // register orbs as blocks

            api.RegisterBlockEntityClass(Mod.Info.ModID + ".shadowspread", typeof(BlockEntities.BEShadowOrb)); // register orb block entity behavior

            api.RegisterItemClass(Mod.Info.ModID + ".corruptstone", typeof(Items.ItemCorruptStone));
            api.RegisterItemClass(Mod.Info.ModID + ".corruptflint", typeof(Items.ItemCorruptFlint));
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
            //api.Logger.Notification(api.World.ApplyColorMapOnRgba("climateCorruptPlantTint","seasonalCorruptGrass",0xFFFFFF,0, 0, 0).ToString());
            //api.Logger.Notification(api.World.ApplyColorMapOnRgba("climateCorruptPlantTint", "seasonalCorruptGrass", 0xFFFFFF, 0, 0, 0).ToString());
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod server side: " + Lang.Get("terrariacorruption:hello"));
            // Save the server API into our variable
            sapi = api;

            //api.Event.RegisterGameTickListener(SpreadTimer, 3000);

    //        api.ChatCommands.Create("treasure").RequiresPlayer()
    //.WithDescription("Place a treasure chest with random items")
    //.RequiresPrivilege(Privilege.controlserver)
    //.HandleWith(new OnCommandDelegate(PlaceTreasureChestInFrontOfPlayer));
    //    }
    //    private TextCommandResult PlaceTreasureChestInFrontOfPlayer(TextCommandCallingArgs args)
    //    {
    //        Block chest = sapi.World.GetBlock(new AssetLocation("chest-south"));
    //        chest.TryPlaceBlockForWorldGen(sapi.World.BlockAccessor,
    //            args.Caller.Player.Entity.Pos.HorizontalAheadCopy(2).AsBlockPos, BlockFacing.UP, null
    //        );
    //        GeneratedStructure shadowpit = sapi.World.GetOrCreateGeneratedStructure("terrariacorruption:shadowpit");
    //        return TextCommandResult.Success();
        }

        public void corruptionPosShort(BlockPos pos) // separate from spreadCorruption so OnGameTick can use the corruption spread function
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 2), rnd.Next(-1, 2)); // find random position in a short radius
            if (sapi.Side == EnumAppSide.Server)
            {
                spreadCorruption(victim);
            }
        }
        public void spreadCorruption(BlockPos victim)
        {
            AssetLocation waterOverride = new AssetLocation("terrariacorruption", "corruptwater-still-7");
            Block overrideCode = sapi.World.GetBlock(waterOverride);

            Block targetBlock = sapi.World.BlockAccessor.GetBlock(victim);
            string changePath = "corrupt" + targetBlock.Code.Path;
            AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);

            Block corruptBlock = sapi.World.GetBlock(changeCode);
            if (corruptBlock == null) return;

            while (targetBlock.Code.Path.StartsWith("aquatic-") || targetBlock.Code.Path.StartsWith("aquaticplant-")) // special condition
            {
                if (overrideCode == null) return; // just in case

                sapi.World.BlockAccessor.SetBlock(overrideCode.BlockId, victim); // place corrupt water
                sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim); // place corrupt block

                pillarCorruption(victim);
                victim.Y += 1;
            }

            sapi.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            sapi.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();

            while (targetBlock.Code.Path.StartsWith("log-") || (targetBlock.Code.Path.StartsWith("water-"))) // special feature
            {
                pillarCorruption(victim);
            }
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