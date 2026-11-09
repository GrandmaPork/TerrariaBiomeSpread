using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TerrariaCorruption.Entities.Behaviors
{
    class EntityBehaviorShowerMeteorMotion : EntityBehaviorMeteorMotionBase
    {
        /// <summary>
        /// Returns the shower motion behavior name.
        /// </summary>
        public override string PropertyName() { return "showermeteormotion"; }

        /// <summary>
        /// Creates the shower meteor motion behavior.
        /// </summary>
        public EntityBehaviorShowerMeteorMotion(Entity entity) : base(entity) { }

        /// <summary>Initializes shower particle settings.</summary>
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            InitializeMeteorParticles();
        }
        /// <summary>Advances the meteor on the server and updates its trail.</summary>
        public override void OnGameTick(float deltaTime)
        {
            if (entity.Api.Side == EnumAppSide.Server)
            {
                CalculateEntityTransforms(deltaTime);
                UpdateMeteorParticles();
                SpawnMeteorParticles();

                base.OnGameTick(deltaTime);
            }
        }
        /// <summary>
        /// Calculates parallel motion using a shared shower direction.
        /// </summary>
        public void DetermineMeteorTranslation(Vec3d translationVector, int speedSeed)
        {
            Random speedRandom = new Random(speedSeed);

            int randomHorizontalSpeed = speedRandom.Next(HorizontalSpeed.Min, HorizontalSpeed.Max);
            int randomVerticalSpeed = speedRandom.Next(VerticalSpeed.Min, VerticalSpeed.Max);

            RandomTranslation.X = (float)translationVector.X * speedRandom.Next(0, randomHorizontalSpeed);
            RandomTranslation.Y = (float)translationVector.Y * -randomVerticalSpeed;
            RandomTranslation.Z = (float)translationVector.Z * (randomHorizontalSpeed - RandomTranslation.X);

            IsMovingSouth = Convert.ToBoolean(speedRandom.Next(0, 2));
            IsMovingEast = Convert.ToBoolean(speedRandom.Next(0, 2));

            if (IsMovingEast != false)
                RandomTranslation.X *= -1;

            if (IsMovingSouth != false)
                RandomTranslation.Z *= -1;
        }
        /// <summary>
        /// Find entity size over time
        /// </summary>
        /// <param name="deltaTime"></param>
        protected override void CalculateEntityTransforms(float deltaTime)
        {
            EntityTransforms.X += RandomTranslation.X * deltaTime;
            EntityTransforms.Y += RandomTranslation.Y * deltaTime;
            EntityTransforms.Z += RandomTranslation.Z * deltaTime;
        }
        /// <summary>
        /// determines gravity, size, color, and velocity
        /// </summary>
        protected override void InitializeMeteorParticles()
        {
            #region Meteor Particles Options
            MeteorParticles.GravityEffect = -0.01f;
            MeteorParticles.WindAffected = true;

            MeteorParticles.MinSize = .5f;
            MeteorParticles.MaxSize = 2.5f;
            MeteorParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -2);

            MeteorParticles.MinQuantity = 2;
            MeteorParticles.AddQuantity = 5;

            MeteorParticles.LifeLength = 1.5f;
            MeteorParticles.addLifeLength = 0.5f;

            MeteorParticles.ShouldDieInLiquid = true;

            DetermineTrailColour();

            MeteorParticles.VertexFlags = Rand.Next(150, 255);

            MeteorParticles.ParticleModel = EnumParticleModel.Quad;
            MeteorParticles.Async = true;

            #endregion
            UpdateMeteorParticles();
        }
        /// <summary>
        /// adds hardcoded vec3d transformation to meteor particles
        /// </summary>
        protected override void UpdateMeteorParticles()
        {
            MeteorParticles.MinPos = entity.Pos.XYZ + new Vec3d(-2, -2, -2);
            MeteorParticles.AddPos = new Vec3d(2, 2, 2);

            MeteorParticles.MinVelocity = new Vec3f(RandomTranslation.X * 0.1f, RandomTranslation.Y * 0.1f, RandomTranslation.Z * 0.1f);
        }
        /// <summary>
        /// This method is only used by the showermeteor command to test meteor shower meteors. Normally, translations are set by the spawner to ensure they all move in parallel.
        /// </summary>
        /// <param name="horizontalVector"></param>
        public void SetMeteorTranslation(Vec2f horizontalVector)
        {
            RandomTranslation.X = horizontalVector.X;
            RandomTranslation.Y = 0;
            RandomTranslation.Z = horizontalVector.Y;
        }
        /// <summary>
        /// sets the color of the fire trail
        /// </summary>
        private void DetermineTrailColour()
        {
            MeteorParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 255);

            //-- Violet --// // test name
            MeteorParticles.Color = ColorUtil.ColorFromRgba(255, 200, 255, Rand.Next(100, 255));
            MeteorParticles.BlueEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 255);
            MeteorParticles.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 150);
            MeteorParticles.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, Rand.Next(0, 150));
        }
    }
}
