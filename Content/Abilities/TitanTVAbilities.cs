using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Titan TV's abilities: hypnotic screen waves, energy blades and a storm of static orbs.

	// Beam hypnotic waves from your screen: confuses enemies and stuns them briefly (bosses aren't stunned).
	public class HypnoScreen : RobotAbility
	{
		public const int Waves = 3;

		public override RobotFormType Form => RobotFormType.TitanTV;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 45;
			Item.useAnimation = 45;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 30;
			Item.knockBack = 1f;
			Item.UseSound = SoundID.Item117 with { Pitch = 0.3f };
			Item.shoot = ModContent.ProjectileType<SoundWave>();
			Item.shootSpeed = 9f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 screen = player.MountedCenter + new Vector2(player.direction * 6f, -34f * player.gravDir);
			for (int i = 0; i < Waves; i++) {
				float spread = MathHelper.Lerp(-0.3f, 0.3f, i / (float)(Waves - 1));
				Projectile.NewProjectile(source, screen, velocity.RotatedBy(spread), type, damage, knockback, player.whoAmI, ai0: 200f, ai1: SoundWave.StyleHypno);
			}
			return false;
		}
	}

	// Swing a giant energy blade in a wide arc toward the cursor.
	public class EnergyBlades : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanTV;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 22;
			Item.useAnimation = 22;
			Item.autoReuse = true;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Melee;
			Item.damage = 90;
			Item.knockBack = 7f;
			Item.UseSound = SoundID.Item71;
			Item.shoot = ModContent.ProjectileType<BladeSwing>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: velocity.ToRotation());
			return false;
		}
	}

	// Spray six homing purple static orbs.
	public class StaticStorm : RobotAbility
	{
		public const int Orbs = 6;

		public override RobotFormType Form => RobotFormType.TitanTV;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.autoReuse = true;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 30;
			Item.knockBack = 2f;
			Item.UseSound = SoundID.Item93;
			Item.shoot = ModContent.ProjectileType<EnergyOrb>();
			Item.shootSpeed = 10f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 screen = player.MountedCenter + new Vector2(player.direction * 6f, -34f * player.gravDir);
			for (int i = 0; i < Orbs; i++) {
				Vector2 vel = velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.7f, 1.2f);
				Projectile.NewProjectile(source, screen, vel, type, damage, knockback, player.whoAmI, ai0: 1f);
			}
			return false;
		}
	}
}
