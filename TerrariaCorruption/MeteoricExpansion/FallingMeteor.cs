using MeteoricExpansion.Renderers;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace TerrariaCorruption.Entities
{
    class EntityFallingMeteor : MeteorBase
    {
        /// <summary>Initializes a falling meteor and its inherited meteor state.</summary>
        public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
        {
            base.Initialize(properties, api, InChunkIndex3d);
        }

        /// <summary>Advances the meteor and despawns it when its lifetime ends or it impacts solid terrain.</summary>
        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);

            if (this.Api.Side == EnumAppSide.Server)
            {
                ElapsedLifetime = this.Api.World.ElapsedMilliseconds;

                if (ElapsedLifetime - MeteorSpawnTime > CurrentLifespan)
                {
                    //-- Fire, in this case, represents the meteor exploding in mid-air --//
                    this.Die(EnumDespawnReason.Death, new DamageSource() { Type = EnumDamageType.Fire });
                }
                else if (this.World.CollisionTester.IsColliding(this.World.BlockAccessor, this.CollisionBox, this.ServerPos.XYZ, false))
                {
                    EnumBlockMaterial material = this.World.BlockAccessor.GetBlock(this.ServerPos.XYZ.AsBlockPos, BlockLayersAccess.SolidBlocks).BlockMaterial;

                    //-- Don't kill the entity on plant matter, causing the meteor to explode. It just looks silly... --//
                    if (material != EnumBlockMaterial.Plant && material != EnumBlockMaterial.Wood && material != EnumBlockMaterial.Leaves && material != EnumBlockMaterial.Air)
                    {
                        //-- Gravity represents the meteor colliding with terrain and creating a crater --//
                        this.Die(EnumDespawnReason.Death, new DamageSource() { Type = EnumDamageType.Gravity });
                    }
                }
            }
        }

        /// <summary>
        /// Falling meteors do not produce ordinary block drops.
        /// </summary>
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            return null;
        }
    }
}
