using System.Collections.Generic;
using System.Linq;
using Immersion.Utility;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace Immersion
{
    public class BlockPalmTree : Block
    {
        public override void OnBlockBroken(IWorldAccessor world, BlockPos Pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            base.OnBlockBroken(world, Pos, byPlayer, dropQuantityMultiplier);
            ItemSlot itemslot = byPlayer?.InventoryManager?.ActiveHotbarSlot;
            if (FirstCodePart() == "palmlog")
            {
                if (itemslot?.Itemstack == null || !Code.ToString().Contains("grown") || !world.Side.IsServer()) return;
                if (itemslot?.Itemstack.Collectible.Tool == EnumTool.Axe)
                {
                    int numfelled = 0;
                    for (int x = -2; x <= 2; x++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            for (int y = 0; y <= 16; y++)
                            {
                                BlockPos bPos = new BlockPos(Pos.X + x, Pos.Y + y, Pos.Z + z);
                                Block bBlock = world.BlockAccessor.GetBlock(bPos);
                                if (bBlock is BlockPalmTree || bBlock.FirstCodePart() == "palmfruits")
                                {
                                    if (itemslot.Itemstack == null) return;

                                    foreach (ItemStack item in GetDrops(world, bPos, byPlayer))
                                    {
                                        world.SpawnItemEntity(item, bPos.ToVec3d());
                                    }
                                    world.BlockAccessor.SetBlock(0, bPos);
                                    numfelled++;
                                }
                            }
                        }
                    }
                    if (numfelled > 15)
                    {
                        Vec3d vec3d = Pos.MidPoint();
                        this.api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), vec3d.X, vec3d.Y, vec3d.Z, null, false, 32f, GameMath.Clamp((float)numfelled / 30, 0.25f, 1f));
                    }
                }
            }
        }

        public override void OnNeighourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
        {
            base.OnNeighourBlockChange(world, pos, neibpos);
            if (this.FirstCodePart() == "palmlog") return;

            foreach (var offset in AreaMethods.Cardinals)
            {
                Block block = world.BlockAccessor.GetBlock(pos.AddCopy(offset.X, offset.Y, offset.Z));
                if (block.FirstCodePart() == "palmlog")
                {
                    return;
                }
            }
            world.BlockAccessor.BreakBlock(pos, null);
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing)
        {
            Dictionary<string, CompositeTexture> textures = this.Textures;
            BakedCompositeTexture compositeTexture = textures != null ? textures.First<KeyValuePair<string, CompositeTexture>>().Value?.Baked : (BakedCompositeTexture)null;
            int randomColor = capi.BlockTextureAtlas.GetRandomColor(compositeTexture.TextureSubId);
            return this.FirstCodePart() == "palmfrond" ? capi.ApplyColorTintOnRgba(1, randomColor, pos.X, pos.Y, pos.Z, true) : randomColor;
        }

        public override int GetColor(ICoreClientAPI capi, BlockPos pos)
        {
            int gray = ColorUtil.ColorFromRgba(127, 127, 127, 255);
            return this.FirstCodePart() == "palmfrond" ? capi.ApplyColorTintOnRgba(1, gray, pos.X, pos.Y, pos.Z, false) : GetColorWithoutTint(capi, pos);
        }
    }
}
