using MeteoricExpansion.Utility;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerrariaCorruption.Entities.Behaviors
{
    abstract class EntityBehaviorMeteorMotionBase : EntityBehavior
    {
        protected SimpleParticleProperties MeteorParticles { get; set; } = new SimpleParticleProperties();
        protected Random Rand { get; set; }
        protected EntityPos EntityTransforms { get; set; }
        protected MinMaxTuple HorizontalSpeed { get; set; } = new MinMaxTuple();
        protected MinMaxTuple VerticalSpeed { get; set; } = new MinMaxTuple();
        protected Vec3f RandomTranslation { get; set; } = new Vec3f();
        protected Vec3f RandomRotation { get; set; } = new Vec3f();

        protected bool IsMovingSouth { get; set; }
        protected bool IsMovingEast { get; set; }


        /// <summary>Returns the base motion behavior name.</summary>
        public override string PropertyName() { return "meteormotion"; }
        /// <summary>Creates the base meteor motion behavior.</summary>
        public EntityBehaviorMeteorMotionBase(Entity entity) : base(entity) { }
        /// <summary>Reads speed settings and initializes the entity transform.</summary>
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            InitializeRandomValues();

            HorizontalSpeed.Min = attributes["horizontalSpeed"]["min"].AsInt();
            HorizontalSpeed.Max = attributes["horizontalSpeed"]["max"].AsInt();

            VerticalSpeed.Min = attributes["verticalSpeed"]["min"].AsInt();
            VerticalSpeed.Max = attributes["verticalSpeed"]["max"].AsInt();

            EntityTransforms = entity.ServerPos;
            entity.Pos.SetFrom(entity.ServerPos);
        }
        /// <summary>Seeds the behavior's random number generator from the entity ID.</summary>
        protected void InitializeRandomValues()
        {
            Rand = new Random((int)entity.EntityId);
        }
        /// <summary>Copies calculated transforms to the entity each tick.</summary>
        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            entity.ServerPos.SetFrom(EntityTransforms);
            entity.Pos.SetFrom(entity.ServerPos);
        }
        /// <summary>Calculates the next entity transform.</summary>
        protected abstract void CalculateEntityTransforms(float deltaTime);
        /// <summary>Configures particle properties for the meteor type.</summary>
        protected abstract void InitializeMeteorParticles();
        /// <summary>Updates meteor particle position and velocity for the current tick.</summary>
        protected abstract void UpdateMeteorParticles();
        /// <summary>Spawns the configured meteor trail particles.</summary>
        protected void SpawnMeteorParticles()
        {
            this.entity.World.SpawnParticles(MeteorParticles);
        }
    }
}
