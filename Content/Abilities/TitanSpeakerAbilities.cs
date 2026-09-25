using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Titan Speaker's abilities: sound waves and a bass shockwave.

	// A huge sound wave that passes through everything, with heavy knockback.
	public class SonicBoom : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 35;
			Item.useAnimation = 35;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 80;
			Item.knockBack = 12f;
			Item.UseSound = SoundID.Item38 with { Pitch = -0.3f };
			Item.shoot = ModContent.ProjectileType<SoundWave>();
			Item.shootSpeed = 11f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// Fired from the speaker head.
			Vector2 head = player.MountedCenter + new Vector2(0f, -34f * player.gravDir);
			Projectile.NewProjectile(source, head, velocity, type, damage, knockback, player.whoAmI, ai0: 260f, ai1: SoundWave.StyleSpeaker);
			return false;
		}
	}

	// Slam out a ring of bass all around you that throws enemies away and confuses them.
	public class BassDrop : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 60;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 120;
			Item.knockBack = 14f;
			Item.shoot = ModContent.ProjectileType<SpeakerShockwave>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI);
			return false;
		}
	}

	// Rapid-fire small sound waves.
	public class SpeakerBarrage : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 10;
			Item.useAnimation = 10;
			Item.autoReuse = true;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 35;
			Item.knockBack = 4f;
			Item.UseSound = SoundID.Item38 with { Pitch = 0.6f, Volume = 0.5f };
			Item.shoot = ModContent.ProjectileType<SoundWave>();
			Item.shootSpeed = 14f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 head = player.MountedCenter + new Vector2(0f, -34f * player.gravDir);
			Projectile.NewProjectile(source, head, velocity.RotatedByRandom(MathHelper.ToRadians(6)), type, damage, knockback, player.whoAmI, ai0: 90f, ai1: SoundWave.StyleSpeaker);
			return false;
		}
	}
}

namespace RobotJack.Content.Abilities
{
	// ---- More Titan Speaker abilities ----

	// Summon two speaker drones that orbit you for 15 seconds and fire sound waves at enemies.
	public class SpeakerDrones : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 40;
			Item.UseSound = SoundID.Item44;
			Item.shoot = ModContent.ProjectileType<TitanDrone>();
			Item.shootSpeed = 1f;
		}

		protected override bool CanUseAbility(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			for (int i = 0; i < 2; i++) {
				Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, 0f, player.whoAmI, ai0: 0f, ai1: i);
			}
			return false;
		}
	}

	// A bubble of sound for 6 seconds: shatters enemy projectiles, pushes enemies out and hurts them.
	public class SonicShieldAbility : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;
		public override int CooldownTicks => 20 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 30;
			Item.UseSound = SoundID.Item29 with { Pitch = -0.5f };
			Item.shoot = ModContent.ProjectileType<SonicShield>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, 6f, player.whoAmI);
			return false;
		}
	}

	// Stomp the ground: two giant sound waves roll out along the floor, one each way.
	public class SubwooferQuake : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 45;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 95;
			Item.knockBack = 10f;
			Item.UseSound = SoundID.Item14 with { Pitch = -0.8f };
			Item.shoot = ModContent.ProjectileType<SoundWave>();
			Item.shootSpeed = 10f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 feet = player.Bottom - new Vector2(0f, 30f);
			foreach (int dir in new[] { -1, 1 }) {
				Projectile.NewProjectile(source, feet, new Vector2(dir * 10f, 0f), type, damage, knockback, player.whoAmI, ai0: 240f, ai1: SoundWave.StyleSpeaker);
			}
			for (int i = 0; i < 30; i++) {
				Dust.NewDustPerfect(player.Bottom, DustID.Smoke, new Vector2(Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-4f, -1f)), 100, default, 2f);
			}
			return false;
		}
	}

	// Twelve sound waves burst out in every direction at once.
	public class FeedbackLoop : RobotAbility
	{
		public const int Waves = 12;

		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 50;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 50;
			Item.knockBack = 7f;
			Item.UseSound = SoundID.Item38 with { Pitch = -0.1f };
			Item.shoot = ModContent.ProjectileType<SoundWave>();
			Item.shootSpeed = 10f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 head = player.MountedCenter + new Vector2(0f, -34f * player.gravDir);
			for (int i = 0; i < Waves; i++) {
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / Waves);
				Projectile.NewProjectile(source, head, dir * 10f, type, damage, knockback, player.whoAmI, ai0: 150f, ai1: SoundWave.StyleSpeaker);
			}
			return false;
		}
	}

	// Blast toward the cursor on a burst of bass, confusing everything you plough through.
	public class BoomDash : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Melee;
			Item.damage = 85;
			Item.knockBack = 10f;
			Item.UseSound = SoundID.Item38 with { Pitch = -0.6f };
			Item.shoot = ModContent.ProjectileType<ThrusterHitbox>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			player.velocity = dir * 18f;
			player.immune = true;
			player.immuneTime = 20;
			player.fallStart = (int)(player.position.Y / 16f);
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: 1f);
			// A wave left behind where you took off.
			Projectile.NewProjectile(source, player.Center, -dir * 6f, ModContent.ProjectileType<SoundWave>(), damage / 2, knockback, player.whoAmI, ai0: 160f, ai1: SoundWave.StyleSpeaker);
			return false;
		}
	}
}
