using Newtonsoft.Json.Linq;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

#nullable disable

namespace TerrariaCorruption.Entities
{
    public class EntityMeteor : Entity
    {
        private ICoreServerAPI sapi;
        private IServerChunk testChunk = null;
        private IServerChunk chunksLoaded = null;
        private BlockPos[] test;
        private List<BlockPos> test2 = new List<BlockPos>();
        private List<BlockPos> chunkIndex = new List<BlockPos>();
        private int i = 0;
        private int j = 0;
        //public override void PreInitialize()
        //{

        //}
        private Block shadowOrb;
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            sapi = api as ICoreServerAPI;
            shadowOrb = World.GetBlock(new AssetLocation("terrariacorruption", "shadoworb"));
            PlayEntitySound("death");
        }
        /// <summary>
        /// do NOT run this every tick. change soon
        /// </summary>
        /// <param name="dt"></param>
        public override void OnGameTick(float dt)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                Vec3d direction = GetNormalDirection();
                LoadChunkFan(direction);
            }

            //Vec3d direction = Pos.Motion;
            //double destination = direction.Length() * 2;
            //IServerChunk lastChunk = testChunk;
            //test2 = new List<BlockPos>();
            //test2.Add(Pos.AsBlockPos + Pos.Motion.AsBlockPos);

            //if (test2[i] == null)
            //{
            //    //sapi.Mod.Logger.Notification("test is null")
            //    return;
            //}
            //testChunk = sapi.WorldManager.GetChunk(test2[i]);

            //if (testChunk == null || lastChunk == null)
            //{
            //    //sapi.Mod.Logger.Notification("testChunk is null")
            //    return;
            //}
            //if (testChunk == lastChunk) return;

            //i++;
            //sapi.WorldManager.LoadChunkColumnPriority(test2[i].X, test2[i].Z); // load chunks ahead (they need to be unloaded after)

            //double width = SelectionBox.XSize;

            //SplashParticleProps.BasePos.Set(Pos.X - width / 2, Pos.Y - width / 2, Pos.Z - width / 2);
            //SplashParticleProps.AddPos.Set(width, 0.5, width);

            //World.SpawnParticles(SplashParticleProps);
        }
        // <summary>
        // replace entity with shadow orb (works in current commit)
        // </summary>
        public override void OnCollided()
        {
            if (Api.Side == EnumAppSide.Server)
            {
                (Api as ICoreServerAPI).ModLoader.GetModSystem<MeteorsModSystem>()?.NotifyCrash(Pos.AsBlockPos);
            }
            PlayEntitySound("death");
            this.Die(EnumDespawnReason.Death, new DamageSource() { Type = EnumDamageType.Fire });
            World.BlockAccessor.SetBlock(shadowOrb.BlockId, Pos.AsBlockPos.AddCopy(0, 0, 0));
        }
        // <summary>
        // seperated from OnCollided in case entity never collides with block
        // </summary>
        // <param name="despawn"></param>
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            //base.OnEntityDespawn(despawn);
            for (i = 0; i < chunkIndex.Count; i++)
            {
                sapi.WorldManager.UnloadChunkColumn(chunkIndex[i].X, chunkIndex[i].Z); // unload previously force loaded chunks
            }
        }
        /// <summary>
        /// Gets the motion of meteor 
        /// </summary>
        /// <returns></returns>
        public Vec3d GetNormalDirection()
        {
            Vec3d direction = Pos.Motion.AddCopy(0, 0, 0);
            return direction;
        }
        /// <summary>
        /// Finds a direction 90 degree clockwise and another 90 degrees counter clockwise of direction vector
        /// </summary>
        /// <param name="direction"></param>
        public void LoadChunkFan(Vec3d direction)
        {
            //Vec2d testing = null;
            //testing.X = direction.X;
            //testing.Y = direction.Z;

            Vec2d cntrClockwise90 = new(0, 0)
            {
                X = direction.Y, //      |  0 1 | x | = y
                Y = direction.X * -1 //  | -1 0 | y | = -x
            };
            Vec2d clockwise90 = new(0, 0) // 0, 0 can possibly be removed
            {
                X = direction.Y * -1, // | 0 -1 | x | = -y
                Y = direction.X //       | 1  0 | y | =  x
            };
            direction += Pos.AsBlockPos.AsVec3i;
            clockwise90.X += Pos.AsBlockPos.AsVec3i.X;
            clockwise90.Y += Pos.AsBlockPos.AsVec3i.Z;
            cntrClockwise90.X += Pos.AsBlockPos.AsVec3i.X;
            cntrClockwise90.Y += Pos.AsBlockPos.AsVec3i.Z;
            
            if (Api.Side == EnumAppSide.Server)
            {
                sapi.WorldManager.LoadChunkColumnPriority((int)direction.X, (int)direction.Z);
                RedundancyCheck(direction);
                sapi.WorldManager.LoadChunkColumnPriority((int)clockwise90.X, (int)clockwise90.Y);
                RedundancyCheck(new Vec3d(clockwise90.X, direction.Y, clockwise90.Y));
                sapi.WorldManager.LoadChunkColumnPriority((int)cntrClockwise90.X, (int)cntrClockwise90.Y);
                RedundancyCheck(new Vec3d(cntrClockwise90.X, direction.Y, cntrClockwise90.Y));
            }
        }
        /// <summary>
        /// Chunks are loaded before this check is called to avoid null instances. The only purpose here is to add fewer chunks to chunkIndex so it may unload them faster
        /// </summary>
        /// <param name="direction"></param>
        public void RedundancyCheck(Vec3d direction)
        {
            IServerChunk lastChunk = testChunk;
            if (Api.Side == EnumAppSide.Server)
            {
                testChunk = sapi.WorldManager.GetChunk(direction.AsBlockPos);
            }

            if (testChunk == null || lastChunk == testChunk)
            {
                //sapi.Mod.Logger.Notification("testChunk is null")
                return;
            }
            chunkIndex.Add(Pos.AsBlockPos + direction.AsVec3i.AsBlockPos);

            j++;
        }
        //
    }
}