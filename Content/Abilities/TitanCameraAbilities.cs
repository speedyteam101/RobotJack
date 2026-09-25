using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Titan Camera's abilities: a core laser, a stunning flash and homing lens orbs.

	// Hold left click: a continuous laser from your chest core that follows the cursor.
	public class CoreLaser : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 45;
			Item.knockBack = 1f;
			Item.UseSound = SoundID.Item12 with { Pitch = -0.4f };
			Item.shoot = ModContent.ProjectileType<CoreLaserBeam>();
			Item.shootSpeed = 1f;
		}

		public override bool CanUseItem(Player player) {
			return base.CanUseItem(player) && player.ownedProjectileCounts[Item.shoot] == 0;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI);
			return false;
		}
	}

	// A blinding flash: damages, confuses and stuns everything nearby (bosses aren't stunned).
	public class CameraFlash : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 90;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 60;
			Item.knockBack = 2f;
			Item.shoot = ModContent.ProjectileType<CameraFlashBurst>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
			return false;
		}
	}

	// Three homing blue orbs from the lens.
	public class LensBurst : RobotAbility
	{
		public const int Orbs = 3;

		public override RobotFormType Form => RobotFormType.TitanCamera;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 24;
			Item.useAnimation = 24;
			Item.autoReuse = true;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 40;
			Item.knockBack = 3f;
			Item.UseSound = SoundID.Item125;
			Item.shoot = ModContent.ProjectileType<EnergyOrb>();
			Item.shootSpeed = 11f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 lens = player.MountedCenter + new Vector2(player.direction * 30f, -36f * player.gravDir);
			for (int i = 0; i < Orbs; i++) {
				float spread = MathHelper.Lerp(-0.35f, 0.35f, i / (float)(Orbs - 1));
				Projectile.NewProjectile(source, lens, velocity.RotatedBy(spread), type, damage, knockback, player.whoAmI, ai0: 0f);
			}
			return false;
		}
	}
}
