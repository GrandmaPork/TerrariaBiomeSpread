using System;
using System.Threading;



// Lets us use collections like List and HashSet.
// These are similar to arrays, but more flexible.
using TerrariaCorruption.Entities;
using Vintagestory.API.Client;



// Vintage Story API imports
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrariaCorruption
{
    public class MeteorsModSystem : ModSystem
    {
        //public static BlockPos shadowOrb1; // make array at some point
        //public static BlockPos shadowOrb2;
        //public static BlockPos shadowOrb3;
        static BlockPos[] shadowOrb = new BlockPos[3]; // double check before using
        private ICoreServerAPI sapi;
        public static Vec3i spawn;
        private static readonly Random rnd = new Random(); // "static" means all instances share one generator.
        public float timer = 0;
        public int speed = 2; // not sure what the unit here will be. Probably blocks per second
        public static BlockPos regionalDestination;
        //public override void Start(ICoreAPI api)
        //{
        //    //Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
        //}
        //public override void StartClientSide(ICoreClientAPI api)
        //{
        //    //Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
        //}
        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod server side: " + Lang.Get("terrariacorruption:hello"));
            // Save the server API into our variable
            sapi = api;

            api.Event.RegisterGameTickListener(SpawnMeteors, 10000, 200000); // ms
            LoadFavoredShadowOrbs(); // Begin loading chunks with shadow orbs (they will register themselves when they are loaded with a limit of 3)
        }
        /// <summary>
        /// spawns a meteor above the player at y240 and calls <see cref="PointToDestination(Entity meteor, BlockPos pos)"/> to set the destination of the meteor
        /// </summary>
        /// <param name="api"></param>
        private void SpawnMeteors(float dt) // idk why float dt is needed but it wont work without it
        {
            //timer += dt;

            //if (timer <= 2) return; // 200 seconds x4
            //timer = 0;
            if (sapi.Side != EnumAppSide.Server) return; // only run on server side
            Mod.Logger.Notification("Spawn meteors is run");
            spawn = sapi.World.DefaultSpawnPosition.AsBlockPos.AsVec3i;

            string entityCode = typeof(EntityMeteor).ToString();
            if (entityCode == null) return;

            EntityProperties type = sapi.World.GetEntityType(new AssetLocation("terrariacorruption", "shadowmeteor"));
            if (type == null)
            {
                sapi.Logger.Warning("Meteor doesn't exist: " + entityCode); // inspired from MeteoricExpansion
                return;
            }

            EntityPos playerPos = sapi.World.NearestPlayer(spawn.X, spawn.Y, spawn.Z).Entity.Pos;
            if (playerPos == null) return;
            BlockPos playerBlock = playerPos.AsBlockPos;
            FindRegionalDestination(playerBlock);
            if (regionalDestination == null) return;
            Entity meteor = sapi.World.ClassRegistry.CreateEntity(type);

            // Generate a random position around the player at y 200
            BlockPos meteorPos = new BlockPos(playerBlock.X + rnd.Next(-1, 2), 200, playerBlock.Z + rnd.Next(-1, 2));
            //Mod.Logger.Notification("playerPos: " + playerPos);
            //Mod.Logger.Notification("meteorPos: " + meteorPos);

            //Vec3d aheadPos = pos.AheadCopy(1, meteor.Pos.Pitch + rndpitch, meteor.Pos.Yaw + rndyaw);
            Vec2d direction = PointToDestination(meteorPos);
            Vec3d velocity = new Vec3d(direction.X * speed, 0, direction.Y * speed);

            meteor.Pos.SetPos(meteorPos);
            meteor.Pos.SetFrom(meteor.Pos);
            meteor.Pos.Motion.Set(velocity); // m/s hopefully. If so, distance = speed * time. time is = sqrt(height/gravity * 2). gravity is unknown, will assume to be 10 m/s^2
            meteor.World = sapi.World;
            //entityToSpawn.PreInitialize();


            // Spawn a meteor at the generated position
            sapi.World.SpawnEntity(meteor);
        }
        /// <summary>
        /// Two ways of handling this: hardcode the height and predict the landing position which may cause the meteor to overshoot it's destination, or calculate a landing position and then calculate a height that will best allow the meteor to land at that position. There are some other fixes but for now I want the meteor to spawn the shadow orb just to make it more of an in-world event
        /// </summary>
        /// <param name="meteor"></param>
        /// <param name="pos"></param>
        public Vec2d PointToDestination(BlockPos pos)
        {
            Vec2d direction = new Vec2d(regionalDestination.X - pos.X, regionalDestination.Z - pos.Z).Normalize(); // point to spawn
            int destinationHeight = sapi.WorldManager.GetSurfacePosY(regionalDestination.X, regionalDestination.Z) ?? 100; // chunk needs to be loaded first. Also I probably need to forgo the surface pos and let entity collision logic handle this


            return direction;
        }
        /// <summary>
        /// Takes the player position and finds a blockpos that each shadow meteor will be aimed at
        /// </summary>
        /// <param name="playerPos"></param>
        public void FindRegionalDestination(BlockPos playerPos)
        {
            float difference = (spawn - playerPos.AsVec3i).Length();
            regionalDestination = playerPos.AddCopy((int)difference + 50, 60, (int)difference + 50);
            Mod.Logger.Notification("Meteor destination set to: " + regionalDestination);

            //sapi.WorldManager.LoadChunkColumn(regionalDestination.X, regionalDestination.Z); // Load the chunk where the meteor will land. Unloading the function needs to be handled when the shadow orb is destroyed, or if the maximum chunks allowed by this mod are exceeded! This is so the corruption spreads while you aren't near it
        }
        public void LoadFavoredShadowOrbs()
        {
            for (int i = 0; i < shadowOrb.Length; i++)
            {
                if (shadowOrb[i] != null)
                {
                    sapi.WorldManager.LoadChunkColumn(shadowOrb[i].X, shadowOrb[i].Z, true);
                    Mod.Logger.Notification("Loading chunk column for favored shadow orb at " + shadowOrb[i]);
                }
            }
            //sapi.WorldManager.LoadChunkColumn(shadowOrb2.X / 16, shadowOrb2.Z / 16);
            //sapi.WorldManager.LoadChunkColumn(shadowOrb3.X / 16, shadowOrb3.Z / 16);
        }
        /// <summary>
        /// Check for null before calling function
        /// </summary>
        /// <param name="orb"></param>
        public static void AskToBeFavored(BlockPos orb)
        {
            if (orb == null) return;

            int limit = 3; // Limit of 3 favored shadow orbs
            int ongoing = 0;
            if (ongoing < limit)
            {
                ongoing++;
                //shadowOrb1 = orb;
                shadowOrb[ongoing] = orb;
                //sapi.Logger.Notification("Favored shadow orb at: " + orb);
            }
            else
            {
                //if (Mod == null) return;
                //Mod.Logger.Notification("Favored shadow orb limit reached. Ignoring request for orb at " + orb); // ignore the request
            }

        }

        public void NotifyCrash(BlockPos pos)
        {
            Mod.Logger.Notification("A meteor has landed at: " + pos);
        }
        //
    }
}