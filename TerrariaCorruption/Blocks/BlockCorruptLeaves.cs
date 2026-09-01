using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

#nullable disable

namespace TerrariaCorruption.Blocks
{
    public class BlockCorruptLeaves : BlockLeaves
    {
       public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
        {
            extra = null;

            // corruption
            if ((api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CheckNeighbors(pos) ?? false) // check for corrupt neighbors
            {
                extra = new GrassTick()
                {
                    Grass = this,
                    TallGrass = null
                }; // used to break the game when removed, will check again later
                return true;
            }

            return offThreadRandom.NextDouble() < 0.15;
        }
        public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
        {
            //TreeAttribute treeAttribute = new TreeAttribute();
            //treeAttribute.SetInt("x", pos.X);
            //treeAttribute.SetInt("y", pos.Y);
            //treeAttribute.SetInt("z", pos.Z);
            //world.Api.Event.PushEvent("testForDecay", treeAttribute);

            //corruption
            if (api.Side == EnumAppSide.Server)
            {
                (api as ICoreServerAPI).ModLoader.GetModSystem<BiomeSpreadModSystem>()?.CorruptionNeighbor(pos);
            }
        }

        //thorns
        public override void OnEntityCollide(IWorldAccessor world, Entity entity, BlockPos pos, BlockFacing facing, Vec3d collideSpeed, bool isImpact) // I definitely forgot to come back to this
        {
            base.OnEntityCollide(world, entity, pos, facing, collideSpeed, isImpact);
            
        }

    }
}