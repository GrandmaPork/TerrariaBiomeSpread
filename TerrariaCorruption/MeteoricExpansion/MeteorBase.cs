using MeteoricExpansion.Utility;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

// all of this code was pulled directly from MeteoricExpansion with minimal changes. Full credit to Taska!

namespace TerrariaCorruption.Entities
{
    /// <summary>
    /// Base class for meteors, providing default behavior and attributes.
    /// </summary>
    abstract class MeteorBase: Entity
    {
        /// <summary>
        /// The hue, saturation, and value of the light emitted by the meteor.
        /// </summary>
        public override byte[] LightHsv { get; set; } = { 4, 4, 31 };

        protected Random rand;

        /// <summary>
        /// The current scale of the meteor.
        /// </summary>
        protected float CurrentScale { get; set; }

        /// <summary>
        /// The minimum possible scale of the meteor.
        /// </summary>
        protected virtual int MinimumScale { get; set; } = 1;

        /// <summary>
        /// The maximum possible scale of the meteor.
        /// </summary>
        protected virtual int MaximumScale { get; set; } = 3;

        /// <summary>
        /// The current lifespan of the meteor.
        /// </summary>
        protected float CurrentLifespan { get; set; }

        /// <summary>
        /// The minimum possible lifespan of the meteor, in seconds.
        /// </summary>
        protected int MinimumLifespan { get; set; }

        /// <summary>
        /// The maximum possible lifespan of the meteor, in seconds.
        /// </summary>
        protected int MaximumLifespan { get; set; }

        /// <summary>
        /// The time at which the meteor was spawned, in milliseconds since the world started.
        /// </summary>
        protected long MeteorSpawnTime { get; set; }

        /// <summary>
        /// The amount of time that has elapsed since the meteor was spawned.
        /// </summary>
        protected long ElapsedLifetime { get; set; }

        /// <summary>Initializes the base entity without applying meteor-specific scaling or lifespan.</summary>
        public void SkipMeteorInit(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
        }

        /// <summary>
        /// Initializes the meteor, including its random scale and server-side lifetime.
        /// </summary>
        /// <param name="properties">The entity properties.</param>
        /// <param name="api">The API instance.</param>
        /// <param name="InChunkIndex3d">The 3D index of the chunk where the meteor is located.</param>
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);

            rand = new Random((int)this.EntityId);

            CurrentScale = (float)rand.NextDouble() + rand.Next(1, this.Properties.Attributes["sizeVariance"].AsInt(1));

            properties.Client.Size *= CurrentScale;
            properties.CollisionBoxSize *= CurrentScale;

            if (api.Side == EnumAppSide.Server)
            {
                MinimumLifespan = api.World.Config.GetInt("MinimumMeteorLifespanInSeconds");
                MaximumLifespan = api.World.Config.GetInt("MaximumMeteorLifespanInSeconds");

                MeteorSpawnTime = api.World.ElapsedMilliseconds;
                CurrentLifespan = (float)(rand.Next(MinimumLifespan, MaximumLifespan) + rand.NextDouble()) * 1000;
            }
        }
    }
}
