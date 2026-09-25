using Microsoft.Xna.Framework;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Hold left click: a gravity well follows your cursor and pulls enemies toward it.
	public class GravityArm : RobotAbility
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.damage = 0; // pure utility, no damage
			Item.UseSound = SoundID.Item117 with { Pitch = -0.5f };
			Item.shoot = ModContent.ProjectileType<GravityWell>();
			Item.shootSpeed = 1f;
		}

		public override bool CanUseItem(Player player) {
			return base.CanUseItem(player) && player.ownedProjectileCounts[Item.shoot] == 0;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// Shoot only runs for the player using the item, so reading the mouse here is safe in multiplayer.
			Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, 0, 0f, player.whoAmI);
			return false;
		}
	}
}
