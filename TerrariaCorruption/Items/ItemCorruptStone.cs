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
    public class ItemCorruptStone : ItemStone
    {
        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            Block block = ((blockSel == null) ? null : byEntity.World.BlockAccessor.GetBlock(blockSel.Position));
            if (block is BlockDisplayCase || block is BlockSign || block is BlockBloomery)
            {
                base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                handling = EnumHandHandling.NotHandled;
                return;
            }

            EnumHandHandling handHandling = EnumHandHandling.NotHandled;
            CollectibleBehavior[] collectibleBehaviors = CollectibleBehaviors;
            foreach (CollectibleBehavior obj in collectibleBehaviors)
            {
                EnumHandling handling2 = EnumHandling.PassThrough;
                obj.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling, ref handling2);
                if (handling2 == EnumHandling.PreventSubsequent)
                {
                    break;
                }
            }

            if (handHandling != EnumHandHandling.NotHandled)
            {
                handling = handHandling;
                return;
            }

            bool flag = itemslot.Itemstack.Collectible.Attributes != null && itemslot.Itemstack.Collectible.Attributes["knappable"].AsBool();
            bool flag2 = false;
            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (byEntity.Controls.ShiftKey && blockSel != null)
            {
                Block block2 = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
                flag2 = block2.Code.PathStartsWith("corruptloosestones") && block2.FirstCodePart(1).Equals(itemslot.Itemstack.Collectible.FirstCodePart(1));
            }

            if (flag2)
            {
                if (!flag)
                {
                    if (byEntity.World.Side == EnumAppSide.Client)
                    {
                        (api as ICoreClientAPI).TriggerIngameError(this, "toosoft", Lang.Get("This type of stone is too soft to be used for knapping."));
                    }

                    return;
                }

                if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
                {
                    itemslot.MarkDirty();
                    return;
                }

                IWorldAccessor world = byEntity.World;
                Block block3 = world.GetBlock(new AssetLocation("terrariacorruption", "corruptknappingsurface"));
                if (block3 == null)
                {
                    return;
                }

                string failureCode = "";
                BlockPos position = blockSel.Position;
                block3.CanPlaceBlock(world, player, blockSel, ref failureCode);
                if (failureCode == "entityintersecting")
                {
                    bool selfBlocked = false;
                    string text = ((world.GetIntersectingEntities(position, block3.GetCollisionBoxes(world.BlockAccessor, position), delegate (Entity e)
                    {
                        selfBlocked = e == byEntity;
                        return !(e is EntityItem);
                    }).Length == 0) ? Lang.Get("Cannot place a knapping surface here") : (selfBlocked ? Lang.Get("Cannot place a knapping surface here, too close to you") : Lang.Get("Cannot place a knapping surface here, to close to another player or creature.")));
                    (api as ICoreClientAPI).TriggerIngameError(this, "cantplace", text);
                    return;
                }

                world.BlockAccessor.SetBlock(block3.BlockId, position);
                world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
                (api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                if (block3.Sounds != null)
                {
                    world.PlaySoundAt(block3.Sounds.Place, blockSel.Position, -0.5);
                }

                if (world.BlockAccessor.GetBlockEntity(position) is BlockEntityKnappingSurface blockEntityKnappingSurface)
                {
                    blockEntityKnappingSurface.BaseMaterial = itemslot.Itemstack.Clone();
                    blockEntityKnappingSurface.BaseMaterial.StackSize = 1;
                    if (byEntity.World is IClientWorldAccessor)
                    {
                        blockEntityKnappingSurface.OpenDialog(world as IClientWorldAccessor, position, itemslot.Itemstack);
                    }
                }

                handling = EnumHandHandling.PreventDefault;
                byEntity.Attributes.SetInt("aimingCancel", 1);
            }
            else
            {
                if (blockSel == null || byEntity?.World == null || !byEntity.Controls.ShiftKey)
                {
                    return;
                }

                IWorldAccessor world2 = byEntity.World;
                Block block4 = world2.GetBlock(CodeWithPath("corruptloosestones-" + LastCodePart() + "-free"));
                if (block4 == null)
                {
                    block4 = world2.GetBlock(CodeWithPath("corruptloosestones-" + LastCodePart(1) + "-" + LastCodePart() + "-free"));
                }

                if (block4 == null)
                {
                    return;
                }

                BlockPos blockPos = blockSel.Position.AddCopy(blockSel.Face);
                blockPos.Y--;
                if (!world2.BlockAccessor.GetMostSolidBlock(blockPos).CanAttachBlockAt(world2.BlockAccessor, block4, blockPos, BlockFacing.UP))
                {
                    return;
                }

                blockPos.Y++;
                BlockSelection blockSelection = blockSel.Clone();
                blockSelection.Position = blockPos;
                blockSelection.DidOffset = true;
                string failureCode2 = "";
                if (!block4.TryPlaceBlock(world2, player, itemslot.Itemstack, blockSelection, ref failureCode2))
                {
                    if (api.Side == EnumAppSide.Client)
                    {
                        (api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("placefailure-" + failureCode2));
                    }

                    return;
                }

                world2.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
                if (block4.Sounds != null)
                {
                    world2.PlaySoundAt(block4.Sounds.Place, blockSel.Position, -0.5);
                }

                (api as ICoreClientAPI)?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                itemslot.Itemstack.StackSize--;
                handling = EnumHandHandling.PreventDefault;
                byEntity.Attributes.SetInt("aimingCancel", 1);
            }
        }
    }
}