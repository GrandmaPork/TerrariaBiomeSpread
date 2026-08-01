using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

#nullable disable

namespace TerrariaCorruption.Items
{
    public class ItemCorruptFlint : ItemFlint
    {
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer)
            {
                byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
            }

            if (byEntity.Controls.ShiftKey && blockSel != null)
            {
                IWorldAccessor world = byEntity.World;
                Block block = world.GetBlock(new AssetLocation("terrariacorruption", "corruptknappingsurface"));
                if (block == null)
                {
                    return;
                }

                if (!world.BlockAccessor.GetBlock(blockSel.Position).CanAttachBlockAt(byEntity.World.BlockAccessor, block, blockSel.Position, BlockFacing.UP))
                {
                    if (api.Side == EnumAppSide.Client)
                    {
                        (api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("Cannot place a knapping surface here"));
                    }

                    return;
                }

                BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
                if (!world.BlockAccessor.GetBlock(blockPos).IsReplacableBy(block))
                {
                    return;
                }

                BlockSelection blockSelection = blockSel.Clone();
                blockSelection.Position = blockPos;
                blockSelection.DidOffset = true;
                string failureCode = "";
                if (!block.TryPlaceBlock(world, byPlayer, slot.Itemstack, blockSelection, ref failureCode))
                {
                    (api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("placefailure-" + failureCode));
                    return;
                }

                world.BlockAccessor.TriggerNeighbourBlockUpdate(blockPos);
                if (block.Sounds != null)
                {
                    world.PlaySoundAt(block.Sounds.Place, blockPos, -0.5);
                }

                if (world.BlockAccessor.GetBlockEntity(blockPos) is BlockEntityKnappingSurface blockEntityKnappingSurface)
                {
                    blockEntityKnappingSurface.BaseMaterial = slot.Itemstack.Clone();
                    blockEntityKnappingSurface.BaseMaterial.StackSize = 1;
                    if (byEntity.World is IClientWorldAccessor)
                    {
                        blockEntityKnappingSurface.OpenDialog(world as IClientWorldAccessor, blockPos, slot.Itemstack);
                    }
                }

                slot.TakeOut(1);
                handling = EnumHandHandling.PreventDefaultAction;
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }
        }
    }
}