using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
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

namespace RobotJack.Content.Abilities
{
	// ---- More Titan Camera abilities ----

	// Summon an army of six Titan Camera soldiers for 20 seconds. They march along the ground, chase down enemies,
	// hit what they run into and shoot at enemies in range.
	public class CameraArmy : RobotAbility
	{
		public const int Soldiers = 6;

		public override RobotFormType Form => RobotFormType.TitanCamera;
		public override int CooldownTicks => 30 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Summon;
			Item.damage = 45;
			Item.knockBack = 4f;
			Item.UseSound = SoundID.Item44;
			Item.shoot = ModContent.ProjectileType<ArmySoldier>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// Line up on both sides of the Titan, dropping in from just above.
			for (int i = 0; i < Soldiers; i++) {
				float offset = (i - (Soldiers - 1) / 2f) * 36f;
				Vector2 spawn = player.Bottom + new Vector2(offset, -40f);
				Projectile.NewProjectile(source, spawn, new Vector2(0f, 2f), type, damage, knockback, player.whoAmI, ai0: 1f, ai1: i);
			}
			return false;
		}
	}

	// Lock on to every enemy near the cursor: they take 50% more damage for 8 seconds.
	public class TargetLock : RobotAbility
	{
		public const float Radius = 320f;
		public const int MarkTicks = 8 * 60;

		public override RobotFormType Form => RobotFormType.TitanCamera;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Camera;
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer) {
				return true;
			}
			Vector2 cursor = Main.MouseWorld;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && !npc.friendly && npc.lifeMax > 5 && Vector2.Distance(npc.Center, cursor) < Radius) {
					npc.AddBuff(ModContent.BuffType<Marked>(), MarkTicks);
				}
			}
			for (int i = 0; i < 30; i++) {
				Dust.NewDustPerfect(cursor + Main.rand.NextVector2CircularEdge(Radius, Radius), DustID.GemRuby, Vector2.Zero, Scale: 1.1f).noGravity = true;
			}
			return true;
		}
	}

	// A powerful piercing shot from the zoom lens.
	public class ZoomShot : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 50;
			Item.useAnimation = 50;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 220;
			Item.crit = 20;
			Item.knockBack = 6f;
			Item.UseSound = SoundID.Item125 with { Pitch = -0.5f };
			Item.shoot = ModContent.ProjectileType<PlasmaBolt>();
			Item.shootSpeed = 20f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 lens = player.MountedCenter + new Vector2(player.direction * 30f, -36f * player.gravDir);
			int index = Projectile.NewProjectile(source, lens, velocity, type, damage, knockback, player.whoAmI);
			if (index >= 0 && index < Main.maxProjectiles) {
				Main.projectile[index].penetrate = 6;
				Main.projectile[index].scale = 1.8f;
			}
			return false;
		}
	}

	// Jump back to where you were 3 seconds ago, and get back the health you had then (if it was more).
	public class Rewind : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;
		public override int CooldownTicks => 20 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.noUseGraphic = true;
			Item.UseSound = SoundID.Item6;
		}

		protected override bool CanUseAbility(Player player) {
			return player.GetModPlayer<RobotJackPlayer>().TryGetRewind(out _, out _);
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI != Main.myPlayer) {
				return true;
			}
			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();
			if (!modPlayer.TryGetRewind(out Vector2 position, out int life)) {
				return true;
			}
			for (int i = 0; i < 25; i++) {
				Dust.NewDustDirect(player.position, player.width, player.height, DustID.Electric, 0f, -2f).noGravity = true;
			}
			RobotJackPlayer.TeleportPlayer(player, position);
			if (life > player.statLife) {
				int heal = System.Math.Min(life, player.statLifeMax2) - player.statLife;
				player.statLife += heal;
				player.HealEffect(heal);
			}
			modPlayer.ClearRewind();
			return true;
		}
	}

	// Throw three flash grenades that pop into small camera flashes.
	public class FlashGrenades : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.noUseGraphic = true;
			Item.DamageType = DamageClass.Magic;
			Item.damage = 55;
			Item.knockBack = 2f;
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<FlashGrenade>();
			Item.shootSpeed = 11f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			for (int i = 0; i < 3; i++) {
				Vector2 vel = velocity.RotatedBy(MathHelper.Lerp(-0.25f, 0.25f, i / 2f)) * Main.rand.NextFloat(0.85f, 1.1f);
				Projectile.NewProjectile(source, player.MountedCenter, vel, type, damage, knockback, player.whoAmI);
			}
			return false;
		}
	}
}
