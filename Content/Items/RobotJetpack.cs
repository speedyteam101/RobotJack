using Microsoft.Xna.Framework;
using RobotJack.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items
{
	// A jetpack worn in the wings slot: it lets you fly. Anyone can wear it.
	// While you're transformed into Robot Jack, flying into things head-first rams them (see HeadRam).
	[AutoloadEquip(EquipType.Wings)]
	public class RobotJetpack : ModItem
	{
		public const int FlightTicks = 150;  // 2.5 seconds of thrust
		public const float FlySpeed = 8f;

		public override void SetStaticDefaults() {
			ArmorIDs.Wing.Sets.Stats[Item.wingSlot] = new WingStats(FlightTicks, FlySpeed, 2f);
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 32;
			Item.value = Item.sellPrice(gold: 3);
			Item.rare = ItemRarityID.Orange;
			Item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.GetModPlayer<RobotJackPlayer>().jetpack = true;
		}

		public override void VerticalWingSpeeds(Player player, ref float ascentWhenFalling, ref float ascentWhenRising,
			ref float maxCanAscendMultiplier, ref float maxAscentMultiplier, ref float constantAscend) {
			ascentWhenFalling = 0.85f;
			ascentWhenRising = 0.16f;
			maxCanAscendMultiplier = 1f;
			maxAscentMultiplier = 3f;
			constantAscend = 0.135f;
		}

		// Frame 0 = no flame, 1-3 = flickering flame while thrusting (or a small flame while gliding).
		public override bool WingUpdate(Player player, bool inUse) {
			if (inUse) {
				player.wingFrameCounter++;
				if (player.wingFrameCounter >= 3) {
					player.wingFrameCounter = 0;
					player.wingFrame = player.wingFrame >= 3 ? 1 : player.wingFrame + 1;
				}

				Vector2 nozzle = player.Center + new Vector2(-player.direction * 10f, 12f * player.gravDir);
				for (int i = 0; i < 2; i++) {
					Dust fire = Dust.NewDustPerfect(nozzle + new Vector2(Main.rand.NextFloat(-4f, 4f), 0f), DustID.Torch,
						new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), 4f * player.gravDir), Scale: 1.5f);
					fire.noGravity = true;
				}
				if (Main.rand.NextBool(3)) {
					Dust.NewDustPerfect(nozzle, DustID.Smoke, new Vector2(0f, 2f * player.gravDir), 150, default, 1.1f).noGravity = true;
				}
				Lighting.AddLight(nozzle, 0.8f, 0.45f, 0.1f);
			}
			else {
				player.wingFrame = player.controlJump && player.velocity.Y != 0f ? 1 : 0;
			}
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddRecipeGroup(RecipeGroupID.IronBar, 15)
				.AddIngredient(ItemID.FallenStar, 10)
				.AddIngredient(ItemID.RocketBoots)
				.AddTile(TileID.Anvils)
				.Register();
		}
	}
}
