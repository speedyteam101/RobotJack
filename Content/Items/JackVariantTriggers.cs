using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items
{
	// Transform items for the five Robot Jack variants. Each is crafted from a Robot Trigger plus themed materials,
	// but the Robot Trigger is not used up, so you keep normal Robot Jack too.
	public abstract class JackVariantTrigger : FormTrigger
	{
		public abstract JackElement Element { get; }

		public override RobotFormType Form => Elements.Form(Element);

		protected override int TransformDust => Elements.Dust(Element);

		// The same pillar-of-light transformation as Robot Jack, in this variant's colour.
		protected override void SpawnTransformEffect(Player player, bool transforming) {
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero,
				ModContent.ProjectileType<TransformBurst>(), 0, 0f, player.whoAmI,
				ai1: transforming ? 0f : 1f, ai2: (int)Element + 1);
		}

		// Starts a recipe that needs a Robot Trigger but gives it back (it isn't consumed).
		protected Recipe TriggerRecipe() {
			int trigger = ModContent.ItemType<RobotTrigger>();
			return CreateRecipe()
				.AddIngredient(trigger)
				.AddConsumeItemCallback((Recipe recipe, int type, ref int amount) => {
					if (type == trigger) {
						amount = 0;
					}
				});
		}
	}

	public class BlazeTrigger : JackVariantTrigger
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int FormBuffType => ModContent.BuffType<BlazeJackForm>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Orange;
		}

		public override void AddRecipes() {
			TriggerRecipe()
				.AddIngredient(ItemID.HellstoneBar, 12)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	public class FrostTrigger : JackVariantTrigger
	{
		public override JackElement Element => JackElement.Frost;
		protected override int FormBuffType => ModContent.BuffType<FrostJackForm>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Orange;
		}

		public override void AddRecipes() {
			TriggerRecipe()
				.AddIngredient(ItemID.IceBlock, 50)
				.AddIngredient(ItemID.FallenStar, 5)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}

	public class VoltTrigger : JackVariantTrigger
	{
		public override JackElement Element => JackElement.Volt;
		protected override int FormBuffType => ModContent.BuffType<VoltJackForm>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.LightPurple;
		}

		public override void AddRecipes() {
			TriggerRecipe()
				.AddIngredient(ItemID.SoulofLight, 12)
				.AddIngredient(ItemID.Wire, 50)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	public class ShadowTrigger : JackVariantTrigger
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int FormBuffType => ModContent.BuffType<ShadowJackForm>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 6);
			Item.rare = ItemRarityID.LightPurple;
		}

		public override void AddRecipes() {
			TriggerRecipe()
				.AddIngredient(ItemID.SoulofNight, 15)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	public class NovaTrigger : JackVariantTrigger
	{
		public override JackElement Element => JackElement.Nova;
		protected override int FormBuffType => ModContent.BuffType<NovaJackForm>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 20);
			Item.rare = ItemRarityID.Red;
		}

		public override void AddRecipes() {
			TriggerRecipe()
				.AddIngredient(ItemID.LunarBar, 10)
				.AddIngredient(ItemID.FallenStar, 20)
				.AddTile(TileID.LunarCraftingStation)
				.Register();
		}
	}
}
