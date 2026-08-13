using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

using TerrariaCorruption;

#nullable disable

namespace TerrariaCorruption.BlockEntities
{
    public class BEShadowOrb : BlockEntity
    {
        readonly Random rnd = new(); //initialize spread rnd
        private ICoreServerAPI sapi;
        public float timer;
        public override void Initialize(ICoreAPI api)
        {
            //sapi = api;
            base.Initialize(api);
            if (api is ICoreServerAPI)
            {
                RegisterGameTickListener(OnGameTick, 50);
            }
        }
        private void OnGameTick(float dt)
        {
            timer += dt;
            BlockPos testPos = Pos.AddCopy(0, 0, 0);


            if (timer >= 2)
            {
                BlockPos clearPos = Pos.AddCopy(rnd.Next(-2, 3), rnd.Next(-1, 2), rnd.Next(-2, 3)); // find random position in a short radius
                Block block = Api.World.BlockAccessor.GetBlock(clearPos); // get block at position

                if ((block.Attributes?["isCorrupt"]?.AsBool() != false) && (clearPos != Pos))
                {
                    block = Api.World.GetBlock(0);
                    Api.World.BlockAccessor.SetBlock(block.BlockId, clearPos);

                    Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(clearPos);
                    Api.World.BlockAccessor.MarkBlockDirty(clearPos);
                    Api.World.BlockAccessor.GetChunkAtBlockPos(clearPos)?.MarkModified();
                }
                else
                {
                    corruptionPosShort(Pos);
                }
                timer = 0;
            }
            else if (timer >= 1 && timer <= 1.2)
            {
                corruptionPos(testPos);
            }
        }
        private void corruptionPos(BlockPos pos) // separate from spreadCorruption so OnGameTick can use the corruption spread function
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-15, 16), rnd.Next(-1, 1), rnd.Next(-15, 16)); // find random position in a large thin plane
            if (Api.Side == EnumAppSide.Server)
            {
                (Api as ICoreServerAPI).ModLoader.GetModSystem<TerrariaCorruptionModSystem>()?.spreadCorruption(victim);
            }
            //sapi.ModLoader.GetModSystem<TerrariaCorruptionModSystem>()?.spreadCorruption(victim);
            //TerrariaCorruptionModSystem.spreadCorruption(victim);
        }
        private void corruptionPosShort(BlockPos pos) // separate from spreadCorruption so OnGameTick can use the corruption spread function
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 1), rnd.Next(-1, 2)); // find random position in a short radius
            if (Api.Side == EnumAppSide.Server)
            {
                (Api as ICoreServerAPI).ModLoader.GetModSystem<TerrariaCorruptionModSystem>()?.spreadCorruption(victim);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("timer", timer);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            timer = tree.GetFloat("timer");
        }
    }
}