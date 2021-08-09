using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ChiselTheWorld
{
    class ItemWorldMinimizer : Item
    {

        public BlockPos selectedPosition = null;
        public BlockPos miniOriginBlock = null;

        public ChiselTheWorld modSystem;

        private ICoreAPI api;

        public override void OnLoaded(ICoreAPI api)
        {
            modSystem = api.ModLoader.GetModSystem<ChiselTheWorld>();
            this.api = api;
        }


        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            // Set the location relative to which the world should be miniaturized
            if (blockSel?.Position == null) return;
            selectedPosition = blockSel.Position.Copy();
            miniOriginBlock = null;
            handling = EnumHandHandling.PreventDefaultAction;
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;

            if (blockSel?.Position == null) return;
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
                return;
            }

            if (!IsChiselingAllowedFor(block, byPlayer))
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }


            if (blockSel == null)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            if (block is BlockChisel)
            {
                OnBlockInteract(byEntity.World, byPlayer, blockSel, ref handling);
                handling = EnumHandHandling.PreventDefaultAction;
                return;
            }
        }


        public void OnBlockInteract(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandHandling handling)
        {
            // Do nothing if no location is selected that should be minified
            if(selectedPosition == null)
            {
                return;
            }

            BlockEntityChisel bec = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
            if (bec != null)
            {
                // If  there is no origin block position of the miniature set yet, set this block as the origin block
                if(miniOriginBlock == null)
                {
                    miniOriginBlock = blockSel.Position.Copy();
                }

                // Calculate which part of the world should be miniturized
                BlockPos miniBlockOffset = miniOriginBlock.Copy().Sub(blockSel.Position.Copy());
                miniBlockOffset = new BlockPos(miniBlockOffset.X * 16, miniBlockOffset.Y * 16, miniBlockOffset.Z * 16);
                

                // Actual copying to miniature
                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 16; y++)
                    {
                        for (int z = 0; z < 16; z++)
                        {
                            Block block = world.BulkBlockAccessor.GetBlock(selectedPosition.AddCopy(x, y, z).SubCopy(miniBlockOffset));

                            if (block.BlockId == 0)
                            {
                                bec.SetVoxel(new Vec3i(x, y, z), false, byPlayer, 0,1);
                            } else
                            {
                                bec.SetVoxel(new Vec3i(x, y, z), true, byPlayer, 0);
                            }
                            
                        }
                    }

                }
                if (api.Side == EnumAppSide.Client)
                {
                    bec.RegenMesh();
                }


                handling = EnumHandHandling.PreventDefaultAction;

                // The Chisel does this after chiseling, so I do it too 
                bec.RegenSelectionBoxes(byPlayer);
                bec.MarkDirty(true);

                // Remove the block entirely if every voxel of it is gone
                if (bec.VoxelCuboids.Count == 0)
                {
                    api.World.BlockAccessor.SetBlock(0, bec.Pos);
                    return;
                }
            }
        }


        // Simply copied from Chisel class
        public bool IsChiselingAllowedFor(Block block, IPlayer player)
        {
            if (block is BlockChisel) return true;

            // First priority: microblockChiseling disabled
            ITreeAttribute worldConfig = api.World.Config;
            string mode = worldConfig.GetString("microblockChiseling");
            if (mode == "off") return false;


            // Second priority: canChisel flag
            bool canChiselSet = block.Attributes?["canChisel"].Exists == true;
            bool canChisel = block.Attributes?["canChisel"].AsBool(false) == true;

            if (canChisel) return true;
            if (canChiselSet && !canChisel) return false;


            // Third prio: Never non cubic blocks
            if (block.DrawType != EnumDrawType.Cube) return false;

            // Fourth prio: Never tinted blocks (because then the chiseled block would have the wrong colors)
            if (block.SeasonColorMap != null || block.ClimateColorMap != null) return false;

            // Otherwise if in creative mode, sure go ahead
            if (player?.WorldData.CurrentGameMode == EnumGameMode.Creative) return true;


            // Lastly go by the config value
            if (mode == "stonewood")
            {
                // Saratys definitely required Exception to the rule #312
                if (block.Code.Path.Contains("mudbrick")) return true;

                return block.BlockMaterial == EnumBlockMaterial.Wood || block.BlockMaterial == EnumBlockMaterial.Stone || block.BlockMaterial == EnumBlockMaterial.Ore || block.BlockMaterial == EnumBlockMaterial.Ceramic;
            }

            return true;
        }
    }
}
