using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items
{
	// God Jack's transform item. Transforming opens a heavenly gate that the player steps out of as God Jack
	// (see HeavenlyGate). Crafted from the Omega Trigger (which you keep) and endgame materials.
	public class GodTrigger : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.GodJack;
		protected override int FormBuffType => ModContent.BuffType<GodJackForm>();
		protected override int TransformDust => DustID.Enchanted_Gold;

		protected override void SpawnTransformEffect(Player player, bool transforming) {
			if (transforming) {
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Bottom, Vector2.Zero,
					ModContent.ProjectileType<HeavenlyGate>(), 0, 0f, player.whoAmI);
			}
			else {
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero,
					ModContent.ProjectileType<TransformBurst>(), 0, 0f, player.whoAmI, ai1: 1f, ai2: 7f);
			}
		}

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(platinum: 5);
			Item.rare = ItemRarityID.Purple;
		}

		// Always drawn at full brightness, with a golden name.
		public override Color? GetAlpha(Color lightColor) => Color.White;

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			foreach (TooltipLine line in tooltips) {
				if (line.Mod == "Terraria" && line.Name == "ItemName") {
					line.OverrideColor = RobotFormTypeExtensions.GodGold;
				}
			}
		}

		public override void AddRecipes() {
			int omega = ModContent.ItemType<OmegaTrigger>();
			CreateRecipe()
				.AddIngredient(omega)
				.AddIngredient(ItemID.LunarBar, 30)
				.AddIngredient(ItemID.FragmentSolar, 20)
				.AddIngredient(ItemID.SoulofLight, 15)
				.AddTile(TileID.LunarCraftingStation)
				// The Omega Trigger isn't used up.
				.AddConsumeItemCallback((Recipe recipe, int type, ref int amount) => {
					if (type == omega) {
						amount = 0;
					}
				})
				.Register();
		}
	}
}
