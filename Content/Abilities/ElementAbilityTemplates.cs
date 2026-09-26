using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Building blocks for the Robot Jack variants' abilities. Each variant ability picks a template and an element,
	// and sets a few numbers. Base damage values are for the early variants (Blaze, Frost); later variants
	// multiply them (see TierMultiplier), and all of them still scale with bosses beaten like every ability.
	public abstract class ElementAbility : RobotAbility
	{
		public abstract JackElement Element { get; }

		public override RobotFormType Form => Elements.Form(Element);

		// Blaze and Frost are pre-Hardmode, Volt and Shadow Hardmode, Nova post-Moon Lord.
		public float TierMultiplier => Element switch {
			JackElement.Volt or JackElement.Shadow => 2f,
			JackElement.Nova => 3.5f,
			_ => 1f,
		};

		protected abstract int BaseDamage { get; }
		protected virtual int UseTime => 30;
		protected virtual bool AutoReuse => false;
		protected virtual float Knockback => 4f;
		protected virtual SoundStyle? Sound => null;
		protected virtual int UseStyle => ItemUseStyleID.Shoot;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = UseStyle;
			Item.useTime = UseTime;
			Item.useAnimation = UseTime;
			Item.autoReuse = AutoReuse;
			Item.noUseGraphic = true;
			Item.damage = (int)(BaseDamage * TierMultiplier);
			Item.knockBack = Knockback;
			Item.UseSound = Sound;
			Item.shoot = ProjectileID.None;
			Item.shootSpeed = 10f;
			SetShoot();
		}

		// Sets Item.shoot (and shootSpeed) for templates that fire through Shoot().
		protected virtual void SetShoot() {
		}

		// The robot's arm cannon, a little in front of the body.
		protected static Vector2 ArmPosition(Player player, Vector2 aim) => player.MountedCenter + aim.SafeNormalize(Vector2.UnitX * player.direction) * 14f;
	}

	// Fires bolts from the arm cannon: Count of them, fanned out over Spread radians.
	public abstract class BoltAbility : ElementAbility
	{
		protected virtual int Count => 1;
		protected virtual float Spread => 0f;
		protected virtual float Speed => 14f;
		protected virtual int Flags => 0;

		protected override void SetShoot() {
			Item.shoot = ModContent.ProjectileType<ElementBolt>();
			Item.shootSpeed = Speed;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 from = ArmPosition(player, velocity);
			for (int i = 0; i < Count; i++) {
				float angle = Count == 1 ? Main.rand.NextFloat(-0.03f, 0.03f) : MathHelper.Lerp(-Spread / 2f, Spread / 2f, i / (float)(Count - 1));
				Projectile.NewProjectile(source, from, velocity.RotatedBy(angle), type, damage, knockback, player.whoAmI, ai0: (int)Element, ai1: Flags);
			}
			return false;
		}
	}

	// Hold left click for a continuous element beam from the arm cannon.
	public abstract class BeamAbility : ElementAbility
	{
		protected override int UseTime => 20;

		protected override void SetShoot() {
			Item.channel = true;
			Item.shoot = ModContent.ProjectileType<ElementBeam>();
			Item.shootSpeed = 1f;
		}

		protected override bool CanUseAbility(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI, ai1: (int)Element);
			return false;
		}
	}

	// Count eruptions burst out of the ground in a row across the cursor, one after another.
	public abstract class EruptionAbility : ElementAbility
	{
		protected virtual int Count => 5;
		protected virtual float Spacing => 64f;
		protected override int UseTime => 40;
		protected override int UseStyle => ItemUseStyleID.HoldUp;

		protected override void SetShoot() {
			Item.shoot = ModContent.ProjectileType<ElementPillar>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 cursor = Main.MouseWorld;
			int dir = cursor.X >= player.Center.X ? 1 : -1;
			for (int i = 0; i < Count; i++) {
				float offset = (i - (Count - 1) / 2f) * Spacing * dir;
				Vector2 ground = OrbitalStrike.FindImpactPoint(new Vector2(cursor.X + offset, cursor.Y - 60f));
				Projectile.NewProjectile(source, ground, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: (int)Element, ai1: i * 5);
			}
			return false;
		}
	}

	// Count bolts rain down from the sky onto the area around the cursor.
	public abstract class RainAbility : ElementAbility
	{
		protected virtual int Count => 8;
		protected virtual float Area => 260f;
		protected virtual int Flags => 0;
		protected override int UseTime => 50;
		protected override int UseStyle => ItemUseStyleID.HoldUp;

		protected override void SetShoot() {
			Item.shoot = ModContent.ProjectileType<ElementBolt>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 cursor = Main.MouseWorld;
			for (int i = 0; i < Count; i++) {
				Vector2 target = cursor + new Vector2(Main.rand.NextFloat(-Area / 2f, Area / 2f), Main.rand.NextFloat(-30f, 30f));
				Vector2 start = target + new Vector2(Main.rand.NextFloat(-120f, 120f), -700f - i * 25f);
				Vector2 vel = (target - start).SafeNormalize(Vector2.UnitY) * 16f;
				Projectile.NewProjectile(source, start, vel, type, damage, knockback, player.whoAmI, ai0: (int)Element, ai1: Flags | ElementBolt.Falling);
			}
			return false;
		}
	}

	// A blast of the element bursting out all around the player.
	public abstract class BlastAbility : ElementAbility
	{
		protected virtual float Radius => 260f;
		protected virtual int BlastFlags => ElementBlast.Huge;
		protected override int UseTime => 50;
		protected override float Knockback => 9f;
		protected override int UseStyle => ItemUseStyleID.HoldUp;

		protected override void SetShoot() {
			Item.shoot = ModContent.ProjectileType<ElementBlast>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI,
				ai0: (int)Element, ai1: Radius, ai2: BlastFlags | ElementBlast.FollowOwner);
			return false;
		}
	}

	// Dash toward the cursor, hitting everything on the way, with a small element burst where you took off.
	public abstract class DashAbility : ElementAbility
	{
		protected virtual float DashSpeed => 19f;
		protected virtual int Invincibility => 20;
		protected override int UseTime => 40;
		protected override float Knockback => 8f;

		protected override void SetShoot() {
			Item.DamageType = DamageClass.Melee;
			Item.shoot = ModContent.ProjectileType<ThrusterHitbox>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			player.velocity = dir * DashSpeed;
			player.immune = true;
			player.immuneTime = Invincibility;
			player.fallStart = (int)(player.position.Y / 16f);
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: (int)Element + 1);
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<ElementBlast>(), damage / 2, knockback, player.whoAmI,
				ai0: (int)Element, ai1: 90f);
			return false;
		}
	}

	// An aura around the player for 8 seconds that hurts everything inside. 15 second cooldown.
	public abstract class AuraAbility : ElementAbility
	{
		public override int CooldownTicks => 15 * 60;
		protected override int UseStyle => ItemUseStyleID.HoldUp;

		protected override void SetShoot() {
			Item.shoot = ModContent.ProjectileType<ElementAura>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, 0f, player.whoAmI, ai0: (int)Element);
			return false;
		}
	}

	// Count element orbs circle the player for 10 seconds. 12 second cooldown.
	public abstract class OrbitAbility : ElementAbility
	{
		protected virtual int Count => 5;
		public override int CooldownTicks => 12 * 60;
		protected override int UseStyle => ItemUseStyleID.HoldUp;

		protected override void SetShoot() {
			Item.shoot = ModContent.ProjectileType<ElementOrbiter>();
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			for (int i = 0; i < Count; i++) {
				Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: (int)Element, ai1: i, ai2: Count);
			}
			return false;
		}
	}

	// Power up: gives the player a buff for 10 seconds. 30 second cooldown.
	public abstract class PowerAbility : ElementAbility
	{
		protected abstract int BuffType { get; }
		protected override int BaseDamage => 0;
		public override int CooldownTicks => 30 * 60;
		protected override int UseStyle => ItemUseStyleID.HoldUp;

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				player.AddBuff(BuffType, 10 * 60);
			}
			ElementFX.Burst(player.Center, Element, 30, 6f, 1.5f);
			return true;
		}
	}
}
