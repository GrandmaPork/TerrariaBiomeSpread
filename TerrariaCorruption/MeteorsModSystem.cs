using HarmonyLib;
using System;
using System.Collections;


// Lets us use collections like List and HashSet.
// These are similar to arrays, but more flexible.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using TerrariaCorruption.Entities;
using Vintagestory.API.Client;



// Vintage Story API imports
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;
using static System.Net.Mime.MediaTypeNames;

namespace TerrariaCorruption
{
    public class MeteorsModSystem : ModSystem
    {
        //public static BlockPos shadowOrb1; // make array at some point
        //public static BlockPos shadowOrb2;
        //public static BlockPos shadowOrb3;
        static BlockPos[] shadowOrb = new BlockPos[3]; // double check before using
        private ICoreServerAPI sapi;

        /*
         * We do this because repeatedly creating Random()
         * can produce repeated values.
         */
        private static readonly Random rnd = new Random(); // "static" means all instances share one generator.
        public override void Start(ICoreAPI api)
        {
            //Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
        }
        public override void StartClientSide(ICoreClientAPI api)
        {
            //Mod.Logger.Notification("Hello from terrariacorruption mod: " + api.Side);
        }
        public override void StartServerSide(ICoreServerAPI api)
        {
            Mod.Logger.Notification("Hello from terrariacorruption mod server side: " + Lang.Get("terrariacorruption:hello"));
            // Save the server API into our variable
            sapi = api;
            api.Event.RegisterGameTickListener(SpawnMeteors, 10000); // ticks per second is 20, so this is every 500 seconds (8.3 minutes). (will turn off at some point)
            LoadFavoredShadowOrbs(); // Begin loading chunks with shadow orbs (they will register themselves when they are loaded with a limit of 3)
        }
        /// <summary>
        /// spawns a meteor above the player at y240 and calls <see cref="SetMeteorDestination(Entity meteor, BlockPos pos)"/> to set the destination of the meteor
        /// </summary>
        /// <param name="api"></param>
        private void SpawnMeteors(float dt) // idk why float dt is needed but it wont work without it
        {
            //EntityPos Position;
            //meteor.Pos.SetPosWithDimension(api.World.NearestPlayer(0, 0, 0).Entity.Pos.AsBlockPos);

            string entityCode = typeof(EntityMeteor).ToString();
            if (entityCode == null) return;
            EntityProperties type = sapi.World.GetEntityType(new AssetLocation("terrariacorruption") + entityCode);
            if (type == null)
            {
                sapi.Logger.Warning("Meteor doesn't exist: " + entityCode); // inspired from MeteoricExpansion
                return;
            }

            Entity meteor = sapi.World.ClassRegistry.CreateEntity(new AssetLocation("terrariacorruption", entityCode));

            // Generate a random position around the player at y 200
            BlockPos meteorPos = new BlockPos((int)meteor.Pos.X + rnd.Next(-10, 10), 240, (int)meteor.Pos.Z + rnd.Next(-10, 10));

            Entity entityToSpawn = sapi.World.ClassRegistry.CreateEntity(type);
            //var entitymeteor = entityToSpawn as IProjectile;
            //entityarrow.FiredBy = api.World.Player.Entity;
            //entitymeteor.Damage = 350;
            //entitymeteor.DamageTier = Attributes["damageTier"].AsInt(7);
            //entityarrow.ProjectileStack = stack;
            //entityarrow.DropOnImpactChance = 1 - breakChance;
            //entitymeteor.IgnoreInvFrames = Attributes["ignoreInvFrames"].AsBool(true);
            //entityarrow.WeaponStack = slot.Itemstack;

            //Vec3d pos = meteor.Pos;
            //Vec3d aheadPos = pos.AheadCopy(1, meteor.Pos.Pitch + rndpitch, meteor.Pos.Yaw + rndyaw);
            Vec2d direction = SetMeteorDestination(meteorPos);
            Vec3d velocity = new Vec3d(direction.X * 100, 0, direction.Y * 100);

            //entityToSpawn.Pos.SetPosWithDimension(byEntity.Pos.BehindCopy(0.21).XYZ.Add(0, byEntity.LocalEyePos.Y, 0));
            entityToSpawn.Pos.Motion.Set(velocity);
            entityToSpawn.World = sapi.World;
            //entityToSpawn.PreInitialize();


            // Spawn a meteor at the generated position
            sapi.World.SpawnEntity(entityToSpawn);

        }
        /// <summary>
        /// Two ways of handling this: hardcode the height and predict the landing position which may cause the meteor to overshoot it's destination, or calculate a landing position and then calculate a height that will best allow the meteor to land at that position. There are some other fixes but for now I want the meteor to spawn the shadow orb just to make it more of an in-world event
        /// </summary>
        /// <param name="meteor"></param>
        /// <param name="pos"></param>
        public Vec2d SetMeteorDestination(BlockPos pos)
        {
            // Set the destination for the meteor
            //meteor.GetBehavior<EntityBehaviorTaskAI>().TaskManager.GetTask<AiTaskSeekEntity>("seekdestination").TargetPos = destination.ToVec3d();



            int spawnX = sapi.WorldManager.DefaultSpawnPosition[0];
            //int spawnY = sapi.WorldManager.DefaultSpawnPosition[1]; // not needed
            int spawnZ = sapi.WorldManager.DefaultSpawnPosition[2];

            Vec2d direction = new Vec2d(pos.X - spawnX, pos.Z - spawnZ).Normalize();
            int speed = 100; // not sure what the unit here will be. Probably blocks per second

            double distance = direction.Length() * speed;

            int destinationHeight = sapi.WorldManager.GetSurfacePosY(spawnX + (int)(direction.X * distance), spawnZ + (int)(direction.Y * distance)) ?? 100; // chunk needs to be loaded first. Also I probably need to forgo the surface pos and let entity collision logic handle this


            BlockPos destination = new BlockPos(spawnX + (int)(direction.X * distance), destinationHeight, spawnZ + (int)(direction.Y * distance));
            Mod.Logger.Notification("Meteor destination set to: " + destination);
            sapi.WorldManager.LoadChunkColumn(destination.X / 16, destination.Z / 16); // Load the chunk where the meteor will land. Unloading the function needs to be handled when the shadow orb is destroyed, or if the maximum chunks allowed by this mod are exceeded! This is so the corruption spreads while you aren't near it

            return direction;
        }
        public void LoadFavoredShadowOrbs()
        {
            for (int i = 0; i < shadowOrb.Length; i++)
            {
                if (shadowOrb[i] != null)
                {
                    sapi.WorldManager.LoadChunkColumn(shadowOrb[i].X / 16, shadowOrb[i].Z / 16);
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
            }
            else
            {
                //if (Mod == null) return;
                //Mod.Logger.Notification("Favored shadow orb limit reached. Ignoring request for orb at " + orb); // ignore the request
            }

        }
    }
}