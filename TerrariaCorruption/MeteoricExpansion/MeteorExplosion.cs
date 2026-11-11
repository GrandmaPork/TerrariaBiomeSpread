using TerrariaCorruption.Utility;
using System;
using System.Collections.Generic;
using TerrariaCorruption.BlockEntities;
using TerrariaCorruption.Blocks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace TerrariaCorruption
{
    class EntityBehaviorMeteorExplosion : EntityBehavior
    {
        ICoreAPI sidedAPI;
        ICoreServerAPI sapi;

        //-- IBulkBlockAccessor doesn't seem to relight chunks when it should? --//
        IBlockAccessor blockAccessor;

        public SimpleParticleProperties solidExplosionParticles, quadExplosionParticles;

        Random explosionRand;
        DamageSource meteorDamageSource;

        private int explosionRadius;
        private int crackedCraterRadius = 1;
        private int injuryRadius;
        private float injuryBaseDamage = 5.0f;

        private double explosionRadiusModifier;
        private double itemStackVelocityModifier = 1.0;
        private double injuryDamageModifier = 1.0;

        private int minMeteorDrops = 5;
        private int maxMeteorDrops = 16;

        //-- Percent Chances --//
        private int terrainDropChance = 5;
        private int ashBlockChance = 20;

        private bool fillWithLiquid = false;
        private int fillHeight;
        AssetLocation liquidAsset;
        private Block shadowOrb;

        private List<BlockPos> positionsChanged = new List<BlockPos>(); // not sure why this isn't used

        /// <summary>Returns the explosion behavior name.</summary>
        public override string PropertyName() { return "meteorexplosion"; }
        /// <summary>Creates the explosion behavior.</summary>
        public EntityBehaviorMeteorExplosion(Entity entity) : base(entity) { }
        /// <summary>Initializes explosion particles and server-side configuration.</summary>
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            sidedAPI = this.entity.Api;

            explosionRand = new Random();

            quadExplosionParticles = new SimpleParticleProperties(1, 1, 1, new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());
            solidExplosionParticles = new SimpleParticleProperties(1, 1, 1, new Vec3d(), new Vec3d(), new Vec3f(), new Vec3f());

            if (sidedAPI.Side == EnumAppSide.Server)
            {
                sapi = (ICoreServerAPI)sidedAPI;
                explosionRadiusModifier = sapi.World.Config.GetDouble("CraterSizeMultiplier", 1.5);

                shadowOrb = sapi.World.GetBlock(new AssetLocation("terrariacorruption", "shadoworb"));

                meteorDamageSource = new DamageSource
                {
                    Source = EnumDamageSource.Unknown,
                };
            }
        }
        /// <summary>
        /// Handles meteor despawn effects and selects the appropriate explosion.
        /// </summary>
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            base.OnEntityDespawn(despawn);

            explosionRadius = (int)((entity.Properties.Client.Size + 0.5f) * explosionRadiusModifier);

            if (sidedAPI.Side == EnumAppSide.Server)
            {
                injuryRadius = explosionRadius * 2;
                injuryDamageModifier = this.entity.Properties.Client.Size * injuryDamageModifier;
            }
            if (despawn != null)
            {
                if (despawn.Reason != EnumDespawnReason.OutOfRange)
                {
                    SpawnExplosionParticles();

                    if (despawn.DamageSourceForDeath != null)
                        switch (despawn.DamageSourceForDeath.Type)
                        {
                            case EnumDamageType.Fire:
                                ExplodeInAir();
                                break;
                            case EnumDamageType.Gravity:
                                ExplodeOnLand();
                                break;
                            default:
                                break;
                        }
                }
            }

        }
        /// <summary>
        /// Explodes the meteor in the air and injures nearby entities when destructive mode is enabled.
        /// </summary>
        private void ExplodeInAir()
        {
            if (sidedAPI.Side == EnumAppSide.Server)
            {
                this.entity.World.PlaySoundAt(new AssetLocation("terrariacorruption", "sounds/effect/air_meteor_explosion_layered"), this.entity, null, true, 512, 1.0f);

                //if (sapi.World.Config.GetBool("Destructive") == true) // will remove config option
                //{
                InjureEntities(this.entity.Pos.XYZ);
                //}
            }
        }
        /// <summary>
        /// Creates the impact explosion, crater, injuries, and meteor drops.
        /// </summary>
        private void ExplodeOnLand() // change to pit structure placement
        {
            if (sidedAPI.Side == EnumAppSide.Server)
            {
                this.entity.World.PlaySoundAt(new AssetLocation("terrariacorruption", "sounds/effect/land_meteor_explosion_layered"), this.entity, null, true, 512, 1.0f);

                //-- entity.PreviousServerPos is never set in VS 1.17 for some reason! Therefore, the direction vector is stored in an attribute on the entity and used instead of calculating it from currentPos - previousPos --//

                Vec3d currentPos = this.entity.Pos.XYZ;

                Vec3i meteorDirectionInt = entity.Attributes.GetVec3i("direction", Vec3i.Zero);
                Vec3d meteorDirection = new Vec3d(meteorDirectionInt.X, meteorDirectionInt.Y, meteorDirectionInt.Z).Normalize();

                Vec3d shrapnelDirection = MeteoricExpansionHelpers.InvertVector(meteorDirection);

                //if (sapi.World.Config.GetBool("Destructive") == true) // will remove config option
                //{
                CreateCrater(meteorDirection, shrapnelDirection);
                InjureEntities(currentPos);
                //}

                //-- The meteor has a chance to 'break apart' on impact and throw out its own drops on impact, amount based on its size --// // maybe i'll repurpose this
                //foreach (BlockDropItemStack itemStack in entity.Properties.Drops)
                //    for (int i = 0; i < (int)(entity.Properties.Client.Size * explosionRand.Next(minMeteorDrops, maxMeteorDrops)); i++)
                //    {
                //        entity.World.SpawnItemEntity(itemStack.GetNextItemStack(), entity.Pos.XYZ, GetNewItemStackVector(shrapnelDirection, itemStackVelocityModifier));
                //    }
            }
        }

        //-- Creates a spherical crater where the meteor made contact with the ground --//
        private void CreateCrater(Vec3d meteorDirection, Vec3d shrapnelDirection)
        {
            List<BlockPos> ashPositions = new List<BlockPos>();

            blockAccessor = sapi.World.GetBlockAccessor(true, true, false);

            Vec3i centerPos = this.entity.Pos.XYZInt;

            BlockPos craterPos = new BlockPos(0);

            //-- Initial scan to see if the meteor crater should fill with liquid instead of air --//
            blockAccessor.SearchFluidBlocks(new BlockPos(centerPos.X - explosionRadius, centerPos.Y - explosionRadius, centerPos.Z - explosionRadius, 0),
                new BlockPos(centerPos.X + explosionRadius, centerPos.Y + explosionRadius, centerPos.Z + explosionRadius, 0), (block, bPos) =>
                {
                    if (block.BlockMaterial == EnumBlockMaterial.Water || block.BlockMaterial == EnumBlockMaterial.Lava)
                    {
                        liquidAsset = new AssetLocation(block.Code.Domain, block.Code.Path);

                        fillWithLiquid = true;

                        if (fillHeight < bPos.Y)
                            fillHeight = bPos.Y;
                    }

                    return true;
                });


            //-- Scans every block in a cube determined by the explosion radius and determines whether that block fits within the explosion sphere --//
            blockAccessor.WalkBlocks(new BlockPos(centerPos.X - explosionRadius - crackedCraterRadius, centerPos.Y - explosionRadius - crackedCraterRadius, centerPos.Z - explosionRadius - crackedCraterRadius, 0),
                new BlockPos(centerPos.X + explosionRadius + crackedCraterRadius, centerPos.Y + explosionRadius + crackedCraterRadius, centerPos.Z + explosionRadius + crackedCraterRadius, 0), (block, xPos, yPos, zPos) =>
                {
                    BlockPos blockPos = new BlockPos(xPos, yPos, zPos, 0);

                    if (!IsPositionClaimed(blockPos))
                    {
                        float distanceToCenter = blockPos.DistanceTo(centerPos.ToBlockPos());

                        if (distanceToCenter < explosionRadius)
                        {
                            ExplodeBlock(block, blockPos, shrapnelDirection);

                            if (fillWithLiquid == false)
                            {
                                if (centerPos.Y - blockPos.Y == explosionRadius - 1)
                                {
                                    if (explosionRand.Next(0, 100) < ashBlockChance)
                                        ashPositions.Add(blockPos);
                                }
                            }
                        }
                        else if (distanceToCenter < explosionRadius + crackedCraterRadius + 0.1)
                        {
                            if (block.Code.Equals(new AssetLocation("game", "rock-" + block.Code.EndVariant())))
                            {
                                blockAccessor.SetBlock(sapi.World.GetBlock(new AssetLocation("game", "crackedrock-" + block.Code.EndVariant())).Id, blockPos);
                            }
                        }
                    }
                });
            blockAccessor.Commit();

            //PlaceMeteor(meteorDirection, shrapnelDirection, centerPos, explosionRadius); // replace with place structure
            //Block shadoworb = sapi.World.GetBlock(new AssetLocation("terrariacorruption" + "shadoworb")); // temporary patch
            if (shadowOrb != null)
            {
                sapi.World.BlockAccessor.SetBlock(shadowOrb.BlockId, centerPos.AsBlockPos.AddCopy(0, 0, 0));
                positionsChanged.Add(centerPos.AsBlockPos.Copy());
            }

            blockAccessor.Commit();
        }

        private void ExplodeBlock(Block blockToExplode, BlockPos explosionPos, Vec3d shrapnelDirection)
        {
            AssetLocation blockCode = blockToExplode.Code;

            switch (blockToExplode.BlockMaterial)
            {
                case EnumBlockMaterial.Lava:
                case EnumBlockMaterial.Water:
                case EnumBlockMaterial.Air:
                case EnumBlockMaterial.Mantle:
                case EnumBlockMaterial.Meta:
                    break;
                default:
                    //-- If a block being destroyed is an inventory, then throw all the contents of it on the ground. Otherwise, for terrain blocks, only spawn items based on a % chance --//
                    if (blockToExplode is BlockGenericTypedContainer || blockToExplode is BlockShelf || blockToExplode is BlockDisplayCase || blockToExplode is BlockMoldRack)
                    {
                        BlockEntityContainer entityContainer = blockAccessor.GetBlockEntity(explosionPos) as BlockEntityContainer;

                        foreach (ItemSlot slot in entityContainer.Inventory)
                        {
                            if (!slot.Empty)
                                entity.World.SpawnItemEntity(slot.Itemstack, explosionPos.ToVec3d(), GetNewItemStackVector(shrapnelDirection, itemStackVelocityModifier));
                        }
                    }
                    else if (blockToExplode is BlockToolRack)
                    {
                        BlockEntityToolrack blockEntityToolrack = blockAccessor.GetBlockEntity(explosionPos) as BlockEntityToolrack;

                        foreach (ItemSlot slot in blockEntityToolrack.inventory)
                        {
                            if (!slot.Empty)
                                entity.World.SpawnItemEntity(slot.Itemstack, explosionPos.ToVec3d(), GetNewItemStackVector(shrapnelDirection, itemStackVelocityModifier));
                        }
                    }
                    else if (blockToExplode is BlockFruitTreePart)
                        break;
                    else
                    {
                        foreach (BlockDropItemStack itemStack in blockAccessor.GetBlock(explosionPos, BlockLayersAccess.SolidBlocks).Drops)
                            if (explosionRand.Next(0, 101) < terrainDropChance)
                            {
                                entity.World.SpawnItemEntity(itemStack.GetNextItemStack(), explosionPos.ToVec3d(), GetNewItemStackVector(shrapnelDirection, itemStackVelocityModifier));
                            }
                    }

                    if (fillWithLiquid == false)
                        blockAccessor.SetBlock(0, explosionPos);
                    else
                    {
                        blockAccessor.SetBlock(0, explosionPos, BlockLayersAccess.Solid);

                        if (explosionPos.Y <= fillHeight)
                            blockAccessor.SetBlock(sapi.WorldManager.GetBlockId(liquidAsset), explosionPos, BlockLayersAccess.Fluid);
                    }
                    break;

            }
        }
        //-- Do damage to any entities within the vicinity of the explosion --//
        private void InjureEntities(Vec3d explosionPos)
        {
            injuryRadius = explosionRadius * 2;
            injuryDamageModifier = this.entity.Properties.Client.Size * injuryDamageModifier;

            Entity[] affectedEntities = this.entity.World.GetEntitiesAround(this.entity.Pos.XYZ, injuryRadius, injuryRadius);

            if (affectedEntities.Length > 0)
            {
                foreach (Entity entity in affectedEntities)
                {
                    entity.ReceiveDamage(meteorDamageSource, injuryBaseDamage * this.entity.Properties.Client.Size);
                }
            }
        }
        /// <summary>Returns a randomized velocity for an ejected item stack.</summary>
        private Vec3d GetNewItemStackVector(Vec3d shrapnelDirection, double velocityModifier)
        {
            Vec3d itemStackVector = new Vec3d
            {
                X = shrapnelDirection.X * explosionRand.NextDouble() * velocityModifier,
                Y = shrapnelDirection.Y * explosionRand.NextDouble() * velocityModifier,
                Z = shrapnelDirection.Z * explosionRand.NextDouble() * velocityModifier
            };

            return itemStackVector;
        }
        /// <summary>Creates and spawns the visual explosion particles.</summary>
        private void SpawnExplosionParticles()
        {
            #region Quad Particle Options
            Vec3f quadVelocityRand = new Vec3f((float)(explosionRand.Next(0, 2) + explosionRand.NextDouble()), (float)(explosionRand.Next(0, 2) + explosionRand.NextDouble()), (float)(explosionRand.Next(0, 2) + explosionRand.NextDouble()));

            quadExplosionParticles.MinPos = this.entity.ServerPos.XYZ + new Vec3d(-this.entity.Properties.Client.Size / 2, -this.entity.Properties.Client.Size / 2, -this.entity.Properties.Client.Size / 2);
            quadExplosionParticles.AddPos = new Vec3d(this.entity.Properties.Client.Size, this.entity.Properties.Client.Size, this.entity.Properties.Client.Size);

            quadExplosionParticles.MinVelocity = new Vec3f(-quadVelocityRand.X, -quadVelocityRand.Y, -quadVelocityRand.Z);

            quadExplosionParticles.AddVelocity = quadVelocityRand * 2;

            quadExplosionParticles.GravityEffect = 0.0f;

            quadExplosionParticles.MinSize = 1.0f;
            quadExplosionParticles.MaxSize = 10.0f;
            quadExplosionParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -2);

            quadExplosionParticles.MinQuantity = 20;
            quadExplosionParticles.AddQuantity = 25;

            quadExplosionParticles.LifeLength = 1.5f;
            quadExplosionParticles.addLifeLength = .5f;

            quadExplosionParticles.ShouldDieInLiquid = true;

            quadExplosionParticles.Color = ColorUtil.ColorFromRgba(255, 255, 255, explosionRand.Next(100, 255));
            quadExplosionParticles.OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 255);
            quadExplosionParticles.BlueEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, explosionRand.Next(0, 150));
            quadExplosionParticles.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, explosionRand.Next(150, 255));
            quadExplosionParticles.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 255);

            quadExplosionParticles.VertexFlags = explosionRand.Next(150, 255);

            quadExplosionParticles.ParticleModel = EnumParticleModel.Quad;
            quadExplosionParticles.Async = true;
            #endregion

            if (sidedAPI.Side == EnumAppSide.Server)
            {
                #region Cuboid Particle Options
                Vec3f velocityRand = new Vec3f((float)(explosionRand.Next(0, 5) + explosionRand.NextDouble()), (float)(explosionRand.Next(0, 5) + explosionRand.NextDouble()), (float)(explosionRand.Next(0, 5) + explosionRand.NextDouble()));

                solidExplosionParticles.MinPos = this.entity.Pos.XYZ + new Vec3d(-this.entity.Properties.Client.Size / 2, -this.entity.Properties.Client.Size / 2, -this.entity.Properties.Client.Size / 2);
                solidExplosionParticles.AddPos = new Vec3d(this.entity.Properties.Client.Size, this.entity.Properties.Client.Size, this.entity.Properties.Client.Size);

                solidExplosionParticles.MinVelocity = new Vec3f(-velocityRand.X, -velocityRand.Y, -velocityRand.Z);

                solidExplosionParticles.AddVelocity = velocityRand * 2;

                solidExplosionParticles.GravityEffect = 0.1f;

                solidExplosionParticles.MinSize = 1.0f;
                solidExplosionParticles.MaxSize = 3.0f;
                solidExplosionParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARNULLIFY, -2.5f);

                solidExplosionParticles.MinQuantity = 100;
                solidExplosionParticles.AddQuantity = 50;

                solidExplosionParticles.LifeLength = 2.0f;
                solidExplosionParticles.addLifeLength = 2.5f;

                solidExplosionParticles.ShouldDieInLiquid = true;

                solidExplosionParticles.WithTerrainCollision = true;

                solidExplosionParticles.Color = ColorUtil.ColorFromRgba(255, 255, 255, 255);

                solidExplosionParticles.BlueEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, explosionRand.Next(0, 150));
                solidExplosionParticles.GreenEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, explosionRand.Next(150, 255));
                solidExplosionParticles.RedEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARREDUCE, 250);

                solidExplosionParticles.VertexFlags = explosionRand.Next(150, 255);
                solidExplosionParticles.Async = true;

                #endregion

                this.entity.World.SpawnParticles(solidExplosionParticles);
            }

            this.entity.World.SpawnParticles(quadExplosionParticles);

        }
        private bool IsPositionClaimed(BlockPos blockPos)
        {
            if (sapi.World.Config.GetBool("ClaimsProtected") == true) // the corruption doesn't care about your claims
                foreach (LandClaim claim in sapi.World.Claims.All)
                {
                    if (claim.PositionInside(blockPos))
                        return true;
                }

            return false;
        }
    }
}
