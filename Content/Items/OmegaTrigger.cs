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
	// Omega Jack's transform item: every Robot Jack combined. Crafted from the Robot Trigger and all five variant
	// triggers (none of them are used up, so you keep every other form) plus post-Moon Lord materials.
	public class OmegaTrigger : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.OmegaJack;
		protected override int FormBuffType => ModContent.BuffType<OmegaJackForm>();
		protected override int TransformDust => Elements.Dust((JackElement)Main.rand.Next(Elements.Count));

		// The pillar-of-light transformation, bigger and in shifting rainbow colours.
		protected override void SpawnTransformEffect(Player player, bool transforming) {
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero,
				ModContent.ProjectileType<TransformBurst>(), 0, 0f, player.whoAmI, ai1: transforming ? 0f : 1f, ai2: 6f);
		}

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(platinum: 1);
			Item.rare = ItemRarityID.Purple;
		}

		// The name glows in shifting rainbow colours.
		public override Color? GetAlpha(Color lightColor) => Color.White;

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			foreach (TooltipLine line in tooltips) {
				if (line.Mod == "Terraria" && line.Name == "ItemName") {
					line.OverrideColor = Main.DiscoColor;
				}
			}
		}

		public override void AddRecipes() {
			List<int> triggers = new() {
				ModContent.ItemType<RobotTrigger>(),
				ModContent.ItemType<BlazeTrigger>(),
				ModContent.ItemType<FrostTrigger>(),
				ModContent.ItemType<VoltTrigger>(),
				ModContent.ItemType<ShadowTrigger>(),
				ModContent.ItemType<NovaTrigger>(),
			};
			Recipe recipe = CreateRecipe();
			foreach (int trigger in triggers) {
				recipe.AddIngredient(trigger);
			}
			recipe.AddIngredient(ItemID.LunarBar, 25)
				.AddIngredient(ItemID.FragmentSolar, 10)
				.AddTile(TileID.LunarCraftingStation)
				// The triggers aren't used up.
				.AddConsumeItemCallback((Recipe r, int type, ref int amount) => {
					if (triggers.Contains(type)) {
						amount = 0;
					}
				})
				.Register();
		}
	}
}
