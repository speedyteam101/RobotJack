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

namespace RobotJack.Content.Abilities
{
	// ---- More Titan TV abilities ----

	// Summon two TV drones that orbit you for 15 seconds and fire homing static orbs at enemies.
	public class TVDrones : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanTV;

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
				Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, 0f, player.whoAmI, ai0: 2f, ai1: i);
			}
			return false;
		}
	}

	// Switch channels: teleport to the cursor (if there's room), bursting static orbs out where you leave and arrive.
	public class ChannelSurf : RobotAbility
	{
		public const float MaxDistance = 900f;

		public override RobotFormType Form => RobotFormType.TitanTV;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 40;
			Item.useAnimation = 20;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 35;
			Item.knockBack = 3f;
			Item.UseSound = SoundID.Item8;
		}

		// Top-left position that would put the player's centre on the cursor.
		private static Vector2 Destination(Player player) => Main.MouseWorld - player.Size / 2f;

		protected override bool CanUseAbility(Player player) {
			if (player.whoAmI != Main.myPlayer) {
				return true;
			}
			Vector2 dest = Destination(player);
			return Vector2.Distance(dest, player.position) <= MaxDistance && !Collision.SolidCollision(dest, player.width, player.height);
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer) {
				return true;
			}
			Vector2 dest = Destination(player);
			if (Collision.SolidCollision(dest, player.width, player.height)) {
				return true;
			}
			Burst(player, player.Center);
			RobotJackPlayer.TeleportPlayer(player, dest);
			player.fallStart = (int)(player.position.Y / 16f);
			Burst(player, player.Center);
			return true;
		}

		private void Burst(Player player, Vector2 at) {
			int damage = player.GetWeaponDamage(Item);
			for (int i = 0; i < 5; i++) {
				Vector2 vel = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 5 + Main.rand.NextFloat(0.5f)) * 8f;
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), at, vel, ModContent.ProjectileType<EnergyOrb>(), damage, 2f, player.whoAmI, ai0: 1f);
			}
			for (int i = 0; i < 25; i++) {
				Dust.NewDustPerfect(at, DustID.PinkFairy, Main.rand.NextVector2Circular(5f, 5f), Scale: 1.3f).noGravity = true;
			}
		}
	}

	// Dash toward the cursor with a blade swing leading the way.
	public class BladeDash : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanTV;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 35;
			Item.useAnimation = 35;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Melee;
			Item.damage = 100;
			Item.knockBack = 8f;
			Item.UseSound = SoundID.Item71 with { Pitch = -0.3f };
			Item.shoot = ModContent.ProjectileType<ThrusterHitbox>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			player.velocity = dir * 20f;
			player.immune = true;
			player.immuneTime = 20;
			player.fallStart = (int)(player.position.Y / 16f);
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: 2f);
			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, ModContent.ProjectileType<BladeSwing>(), damage, knockback, player.whoAmI, ai0: dir.ToRotation());
			return false;
		}
	}

	// A crackling field around you for 8 seconds that zaps every enemy inside with lightning.
	public class StaticFieldAbility : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanTV;
		public override int CooldownTicks => 15 * 60;

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
			Item.UseSound = SoundID.Item93 with { Pitch = -0.4f };
			Item.shoot = ModContent.ProjectileType<StaticField>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, 0f, player.whoAmI);
			return false;
		}
	}

	// Hold left click: a purple beam broadcast from your screen that follows the cursor.
	public class BroadcastBeam : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanTV;

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
			Item.damage = 40;
			Item.knockBack = 1f;
			Item.UseSound = SoundID.Item117 with { Pitch = -0.2f };
			Item.shoot = ModContent.ProjectileType<CoreLaserBeam>();
			Item.shootSpeed = 1f;
		}

		protected override bool CanUseAbility(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI, ai1: 1f);
			return false;
		}
	}
}
