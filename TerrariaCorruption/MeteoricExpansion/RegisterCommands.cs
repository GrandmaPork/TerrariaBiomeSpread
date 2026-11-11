using System.Runtime.CompilerServices;
using TerrariaCorruption.Entities;
using TerrariaCorruption.Entities.Behaviors;
using TerrariaCorruption.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace TerrariaCorruption
{
    class RegisterCommands : ModSystem
    {
        private ICoreServerAPI sapi;
        private ICoreClientAPI capi;

        private ICoreAPI api;
        /// <summary>Restricts this system to server-side loading.</summary>
        public override bool ShouldLoad(EnumAppSide side)
        {
            return side == EnumAppSide.Server;
        }
        //private void RegisterCommand() // idk how the hell this works so im just getting rid of it
        //{
        //    var parsers = sapi.ChatCommands.Parsers;

        //    sapi.ChatCommands
        //        .GetOrCreate("spawnmeteor")
        //        .IgnoreAdditionalArgs()
        //        .HandleWith(MeteoricExpansion)
        //        .RequiresPrivilege("controlserver")
        //        .WithPreCondition(loadWorkSpace)
        //        .WithDescription("Spawns meteor above the player")
        //        .Validate()
        //        ;
        //}
        public override void StartServerSide(ICoreServerAPI api)
        {
            //base.StartServerSide(api);
            //this.sapi = sapi;
            sapi = api;

            sapi.ChatCommands.GetOrCreate("debug")
                .BeginSubCommand("spawnmeteor")
                .WithDescription("Spawn meteor above the player")
                .RequiresPrivilege(Privilege.controlserver)

                .BeginSubCommand("pos")
                .RequiresPlayer()
                .HandleWith(CmdSpawnMeteor)
                .EndSubCommand()

                .EndSubCommand()
                ;
        }
        private TextCommandResult CmdSpawnMeteor(TextCommandCallingArgs args)
        {
            EntityProperties entityType = sapi.World.GetEntityType(new AssetLocation("terrariacorruption", "shadowmeteor"));
            Entity entity = (EntityFallingMeteor)sapi.World.ClassRegistry.CreateEntity(entityType);
            var player = args.Caller.Player as IServerPlayer;

            if (player == null) return TextCommandResult.Error("This command can only be used by a player.");

            var entityPos = new EntityPos(player.Entity.Pos.X, sapi.WorldManager.MapSizeY - 10, player.Entity.Pos.Z);

            entity.Pos.SetPos(entityPos);
            entity.Pos.SetFrom(entity.Pos);

            sapi.World.SpawnEntity(entity);

            if (entity == null)
            {
                return TextCommandResult.Error("Failed to spawn meteor.");
            }

            //System.Diagnostics.Debug.WriteLine("Spawned at: " + entity.Pos);
            //System.Diagnostics.Debug.WriteLine("Player at: " + player.Entity.Pos);
            return TextCommandResult.Success("Meteor spawned at: " + entity.Pos);
        }
        /// <summary>Initializes server commands; test commands are currently disabled.</summary>
        /*public override void StartServerSide(ICoreServerAPI api)
        {

            base.StartServerSide(api);
            //-- Registers a command that will spawn a random meteor 10 blocks above the player --//
            /*
            api.RegisterCommand("fallingmeteor", "Spawns a meteor for testing purposes.", "",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                EntityProperties entityType = api.World.GetEntityType(new AssetLocation("meteoricexpansion", "meteor-bismuthinite-andesite"));
                EntityFallingMeteor entity = (EntityFallingMeteor)api.World.ClassRegistry.CreateEntity(entityType);
                EntityPos entityPos = new EntityPos(player.Entity.ServerPos.X, api.WorldManager.MapSizeY - 10, player.Entity.ServerPos.Z);

                entity.ServerPos.SetPos(entityPos);
                entity.Pos.SetFrom(entity.ServerPos);

                api.World.SpawnEntity(entity);

                System.Diagnostics.Debug.WriteLine("Spawned at: " + entity.ServerPos);
                System.Diagnostics.Debug.WriteLine("Player at: " + player.Entity.ServerPos);
                
            }, Privilege.controlserver);
            //-- Registers a command that will spawn a random meteor 10 blocks above the player --//
            api.RegisterCommand("showermeteor", "Spawns a meteor for testing purposes.", "",
            (IServerPlayer player, int groupId, CmdArgs args) =>
            {
                EntityProperties entityType = api.World.GetEntityType(new AssetLocation("meteoricexpansion", "showermeteor-" + args[0]));
                EntityShowerMeteor entity = (EntityShowerMeteor)api.World.ClassRegistry.CreateEntity(entityType);
                EntityPos entityPos = new EntityPos(player.Entity.ServerPos.X, api.WorldManager.MapSizeY - 10, player.Entity.ServerPos.Z);

                entity.ServerPos.SetPos(entityPos);
                entity.Pos.SetFrom(entity.ServerPos);

                api.World.SpawnEntity(entity);

                entity.GetBehavior<EntityBehaviorShowerMeteorMotion>().SetMeteorTranslation(new Vec2f(20, 20));

                System.Diagnostics.Debug.WriteLine("Spawned at: " + entity.ServerPos);
                System.Diagnostics.Debug.WriteLine("Player at: " + player.Entity.ServerPos);

            }, Privilege.controlserver);

            api.RegisterCommand("testcrater", "Makes a crater below the player.", "",
                (IServerPlayer player, int groupId, CmdArgs args) =>
                {
                    IWorldAccessor world = player.Entity.World;
                    IBlockAccessor blockAccessor = world.GetBlockAccessorBulkUpdate(true, true);

                    Vec3i centerPos = new Vec3i((int)player.Entity.ServerPos.X, (int)player.Entity.ServerPos.Y - 1, (int)player.Entity.ServerPos.Z);
                    //BlockPos blockPos = new BlockPos();
                    int craterRadius = 3;

                    blockAccessor.WalkBlocks(new BlockPos(centerPos.X - craterRadius, centerPos.Y - craterRadius, centerPos.Z - craterRadius),
                        new BlockPos(centerPos.X + craterRadius, centerPos.Y + craterRadius, centerPos.Z + craterRadius), (block, xPos, yPos, zPos) => {
                            BlockPos blockPos = new BlockPos(xPos, yPos, zPos);

                            if (blockPos.DistanceTo(centerPos.ToBlockPos()) < craterRadius)
                            {
                                blockAccessor.SetBlock(0, blockPos);
                                blockAccessor.TriggerNeighbourBlockUpdate(blockPos);
                            }
                        }, false);

                    blockAccessor.Commit();
                }, Privilege.controlserver);
            */

        //}
    }
    abstract class SummonMeteor : ChatCommand
    {
        //public abstract void CallHandler(IServerPlayer player, int groupId, CmdArgs args) { }
    }
}
