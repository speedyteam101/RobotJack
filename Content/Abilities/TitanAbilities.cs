using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Common.Titan;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// The Shift Titan's abilities. Every fusion (the trigger merged into the Shift Trigger, or none) has its own nine,
	// one of each kind below, in its element with its own bolt behaviour and damage tier (TitanData.Tier).
	// The concrete abilities are in TitanAbilityList.cs. They can't be used while in car mode.
	public abstract class TitanAbility : RobotAbility
	{
		public abstract TitanFusion Fusion { get; }

		public override RobotFormType Form => RobotFormType.ShiftTitan;

		public JackElement Element => TitanData.Element(Fusion);

		public override bool IsAllowed(Player player) => base.IsAllowed(player) && player.GetModPlayer<TitanPlayer>().fusion == Fusion;

		protected override bool CanUseAbility(Player player) => !player.GetModPlayer<TitanPlayer>().carMode;

		protected abstract int BaseDamage { get; }
		protected virtual int UseTime => 30;
		protected virtual bool AutoReuse => false;
		protected virtual float Knockback => 7f;
		protected virtual SoundStyle? Sound => null;
		protected virtual int UseStyle => ItemUseStyleID.HoldUp;
		protected virtual int ShootType => ModContent.ProjectileType<ElementBolt>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = UseStyle;
			Item.useTime = UseTime;
			Item.useAnimation = UseTime;
			Item.autoReuse = AutoReuse;
			Item.noUseGraphic = true;
			Item.damage = (int)(BaseDamage * TitanData.Tier(Fusion));
			Item.knockBack = Knockback;
			Item.UseSound = Sound;
			Item.shoot = ShootType;
			Item.shootSpeed = 16f;
			Item.rare = ItemRarityID.Yellow;
		}

		// How this fusion's bolts behave.
		protected int BoltFlags => Fusion switch {
			TitanFusion.Robot => ElementBolt.Pierce,
			TitanFusion.Blaze => ElementBolt.Explode,
			TitanFusion.Frost => ElementBolt.Pierce,
			TitanFusion.Volt => ElementBolt.Homing,
			TitanFusion.Shadow => ElementBolt.Pierce | ElementBolt.Homing,
			TitanFusion.Nova => ElementBolt.Explode | ElementBolt.Homing,
			TitanFusion.Omega or TitanFusion.God => ElementBolt.Explode | ElementBolt.Homing | ElementBolt.Pierce,
			_ => 0,
		};

		// Frost (and Omega, which has everything) freezes enemies with its blasts.
		protected int BlastFlags => ElementBlast.Huge | (Fusion == TitanFusion.Frost || Fusion == TitanFusion.Omega ? ElementBlast.Freeze : 0);

		protected static Vector2 AimFrom(Vector2 from, Player player) => (Main.MouseWorld - from).SafeNormalize(Vector2.UnitX * player.direction);

		protected void Pillar(IEntitySource source, Player player, Vector2 near, int damage, float knockback, int delay, float size) {
			Vector2 ground = OrbitalStrike.FindImpactPoint(near);
			Projectile.NewProjectile(source, ground, Vector2.Zero, ModContent.ProjectileType<ElementPillar>(), damage, knockback, player.whoAmI,
				ai0: (int)Element, ai1: delay, ai2: size);
		}
	}

	// 1. The weapon arm (chosen in the menu). Cannon: a spread of huge bolts. Blade: a huge sweeping slash.
	// Claw: three raking slashes, faster but a little weaker.
	public abstract class TitanArm : TitanAbility
	{
		protected override int BaseDamage => 30;
		protected override int UseTime => 18;
		protected override bool AutoReuse => true;
		protected override int UseStyle => ItemUseStyleID.Shoot;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			int weapon = player.GetModPlayer<TitanPlayer>().look.WeaponArm;
			Vector2 aim = AimFrom(TitanRenderer.ShoulderWorld(player), player);
			if (weapon == TitanData.WeaponCannon) {
				SoundEngine.PlaySound(SoundID.Item92 with { Pitch = -0.3f }, player.Center);
				Vector2 muzzle = TitanRenderer.MuzzleWorld(player, aim, weapon);
				for (int i = -1; i <= 1; i++) {
					Projectile.NewProjectile(source, muzzle, aim.RotatedBy(i * 0.12f) * 18f, type, damage, knockback, player.whoAmI,
						ai0: (int)Element, ai1: BoltFlags | ElementBolt.Big | ElementBolt.Huge);
				}
			}
			else {
				bool claw = weapon == TitanData.WeaponClaw;
				Projectile.NewProjectile(source, player.Center, aim, ModContent.ProjectileType<TitanSlash>(), (int)(damage * (claw ? 1.3f : 1.7f)),
					knockback * 1.5f, player.whoAmI, ai0: (int)Element, ai1: claw ? 1f : 0f, ai2: player.direction);
			}
			return false;
		}
	}

	// 2. Hold left click: a giant beam straight out of the titan's core.
	public abstract class TitanCoreBeam : TitanAbility
	{
		protected override int BaseDamage => 20;
		protected override int UseTime => 20;
		protected override int UseStyle => ItemUseStyleID.Shoot;
		protected override int ShootType => ModContent.ProjectileType<ElementBeam>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.channel = true;
			Item.shootSpeed = 1f;
		}

		protected override bool CanUseAbility(Player player) => base.CanUseAbility(player) && player.ownedProjectileCounts[Item.shoot] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 aim = AimFrom(TitanRenderer.CoreWorld(player), player);
			Projectile.NewProjectile(source, TitanRenderer.CoreWorld(player), aim, type, damage, knockback, player.whoAmI, ai1: (int)Element, ai2: 2f);
			return false;
		}
	}

	// 3. Stomp: a shockwave bursts out from the titan's feet and the ground erupts on both sides.
	public abstract class TitanStomp : TitanAbility
	{
		protected override int BaseDamage => 60;
		protected override int UseTime => 50;
		protected override float Knockback => 12f;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.8f, Volume = 1.3f }, player.Bottom);
			Projectile.NewProjectile(source, player.Bottom, Vector2.Zero, ModContent.ProjectileType<ElementBlast>(), damage, knockback, player.whoAmI,
				ai0: (int)Element, ai1: 520f, ai2: BlastFlags);
			for (int i = 1; i <= 4; i++) {
				foreach (int side in new[] { -1, 1 }) {
					Pillar(source, player, player.Bottom + new Vector2(side * (player.width / 2f + i * 110f), -80f), damage / 2, knockback, i * 4, 1.5f);
				}
			}
			return false;
		}
	}

	// 4. Eruption: eleven giant pillars of the element burst out of the ground across the cursor.
	public abstract class TitanEruption : TitanAbility
	{
		protected override int BaseDamage => 45;
		protected override int UseTime => 40;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 cursor = Main.MouseWorld;
			int dir = cursor.X >= player.Center.X ? 1 : -1;
			for (int i = 0; i < 11; i++) {
				float offset = (i - 5) * 95f * dir;
				Pillar(source, player, new Vector2(cursor.X + offset, cursor.Y - 100f), damage, knockback, i * 4, 2f);
			}
			return false;
		}
	}

	// 5. Skyfall: twenty huge exploding bolts rain down from the sky onto the cursor.
	public abstract class TitanSkyfall : TitanAbility
	{
		protected override int BaseDamage => 30;
		protected override int UseTime => 50;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 cursor = Main.MouseWorld;
			for (int i = 0; i < 20; i++) {
				Vector2 target = cursor + new Vector2(Main.rand.NextFloat(-350f, 350f), Main.rand.NextFloat(-40f, 40f));
				Vector2 start = target + new Vector2(Main.rand.NextFloat(-150f, 150f), -900f - i * 30f);
				Vector2 vel = (target - start).SafeNormalize(Vector2.UnitY) * 18f;
				Projectile.NewProjectile(source, start, vel, type, damage, knockback, player.whoAmI,
					ai0: (int)Element, ai1: BoltFlags | ElementBolt.Explode | ElementBolt.Big | ElementBolt.Huge | ElementBolt.Falling);
			}
			return false;
		}
	}

	// 6. Missile Pods: sixteen homing missiles launch out of the titan's backpack.
	public abstract class TitanMissiles : TitanAbility
	{
		protected override int BaseDamage => 25;
		protected override int UseTime => 45;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			SoundEngine.PlaySound(SoundID.Item61, player.Center);
			Vector2 pods = TitanRenderer.BackpackWorld(player);
			for (int i = 0; i < 16; i++) {
				Vector2 launch = new Vector2(-player.direction * Main.rand.NextFloat(1f, 6f), -Main.rand.NextFloat(10f, 16f));
				Projectile.NewProjectile(source, pods + Main.rand.NextVector2Circular(30f, 30f), launch, type, damage, knockback, player.whoAmI,
					ai0: (int)Element, ai1: ElementBolt.Homing | ElementBolt.Explode | ElementBolt.Big);
			}
			return false;
		}
	}

	// 7. Core Satellites: six orbs split off the core and circle the titan for 10 seconds. 15 second cooldown.
	public abstract class TitanSatellites : TitanAbility
	{
		protected override int BaseDamage => 20;
		public override int CooldownTicks => 15 * 60;
		protected override int ShootType => ModContent.ProjectileType<ElementOrbiter>();

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			for (int i = 0; i < 6; i++) {
				Projectile.NewProjectile(source, TitanRenderer.CoreWorld(player), Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: (int)Element, ai1: i, ai2: 6);
			}
			return false;
		}
	}

	// 8. Titan Aura: the element surrounds the whole titan for 8 seconds, hurting everything inside. 15 second cooldown.
	public abstract class TitanAura : TitanAbility
	{
		protected override int BaseDamage => 18;
		public override int CooldownTicks => 15 * 60;
		protected override int ShootType => ModContent.ProjectileType<ElementAura>();

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, 0f, player.whoAmI, ai0: (int)Element);
			return false;
		}
	}

	// 9. Core Overdrive: the core overloads in an enormous blast, the ground erupts all around and bolts fly
	// out in every direction. 45 second cooldown.
	public abstract class TitanOverdrive : TitanAbility
	{
		protected override int BaseDamage => 120;
		protected override int UseTime => 60;
		protected override float Knockback => 14f;
		public override int CooldownTicks => 45 * 60;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 core = TitanRenderer.CoreWorld(player);
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -1f, Volume = 1.5f }, core);
			SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.5f }, core);
			Projectile.NewProjectile(source, core, Vector2.Zero, ModContent.ProjectileType<ElementBlast>(), damage, knockback, player.whoAmI,
				ai0: (int)Element, ai1: 1000f, ai2: BlastFlags);
			for (int i = 1; i <= 6; i++) {
				foreach (int side in new[] { -1, 1 }) {
					Pillar(source, player, player.Bottom + new Vector2(side * (player.width / 2f + i * 140f), -100f), damage / 2, knockback, 6 + i * 3, 2f);
				}
			}
			for (int i = 0; i < 24; i++) {
				Vector2 dir = (MathHelper.TwoPi * i / 24f).ToRotationVector2();
				Projectile.NewProjectile(source, core + dir * 60f, dir * 15f, type, damage / 3, knockback, player.whoAmI,
					ai0: (int)Element, ai1: BoltFlags | ElementBolt.Big | ElementBolt.Huge);
			}
			return false;
		}
	}
}
