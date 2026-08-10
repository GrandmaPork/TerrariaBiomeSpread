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

#nullable disable

namespace TerrariaCorruption.BlockEntities
{
    public class BEShadowOrb : BlockEntity
    {
        readonly Random rnd = new(); //initialize spread rnd
        public float timer;
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            RegisterGameTickListener(OnGameTick, 50);
        }
        public void OnGameTick(float dt)
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
                timer = 2;
            }
        }
        public void corruptionPos(BlockPos pos) // seperate from spreadCorruption so OnGameTick can use the corruption spread function
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-7, 8), rnd.Next(-1, 1), rnd.Next(-7, 8)); // find random position in a large thin plane
            spreadCorruption(victim, null);
        }
        public void corruptionPosShort(BlockPos pos) // seperate from spreadCorruption so OnGameTick can use the corruption spread function
        {
            BlockPos victim = pos.AddCopy(rnd.Next(-1, 2), rnd.Next(-1, 1), rnd.Next(-1, 2)); // find random position in a large thin plane
            spreadCorruption(victim, null);
        }
        public void spreadCorruption(BlockPos victim, object extra = null)
        {
            Block targetBlock = Api.World.BlockAccessor.GetBlock(victim); // find block at that position

            string changePath = "corrupt" + targetBlock.Code.Path;

            AssetLocation changeCode = new AssetLocation("terrariacorruption", changePath);

            Block corruptBlock = Api.World.GetBlock(changeCode);
            //corruptBlock = getCorruptedBlock(world = null, victim, extra);
            if (corruptBlock == null) return;

            Api.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
            Api.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();

            while (targetBlock.Code.Path.StartsWith("log-")) // change later
            {
                victim.Y += 1;
                targetBlock = Api.World.BlockAccessor.GetBlock(victim);
                changePath = "corrupt" + targetBlock.Code.Path;
                changeCode = new AssetLocation("terrariacorruption", changePath);
                corruptBlock = Api.World.GetBlock(changeCode);
                if (corruptBlock == null) return;

                Api.World.BlockAccessor.SetBlock(corruptBlock.BlockId, victim);
                Api.World.BlockAccessor.GetChunkAtBlockPos(victim)?.MarkModified();
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