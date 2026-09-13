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
        //public override void PreInitialize()
        //{

        //}
        private Block shadowOrb;
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
            shadowOrb = World.GetBlock(new AssetLocation("terrariacorruption", "shadoworb"));
        }
        public override void OnGameTick(float dt)
        {
            double width = SelectionBox.XSize;

            SplashParticleProps.BasePos.Set(Pos.X - width / 2, Pos.Y - width / 2, Pos.Z - width / 2);
            SplashParticleProps.AddPos.Set(width, 0.5, width);
            
            World.SpawnParticles(SplashParticleProps);
        }
        /// <summary>
        /// replace entity with shadow orb (works in current commit)
        /// </summary>
        public override void OnCollided()
        {
            this.Die(EnumDespawnReason.Death, new DamageSource() { Type = EnumDamageType.Fire});
            World.BlockAccessor.SetBlock(shadowOrb.BlockId, Pos.AsBlockPos.AddCopy(0,0,0));
        }
    }
}