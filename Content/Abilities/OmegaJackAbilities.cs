using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Omega Jack's eight abilities: every Robot Jack's power at once. All of them still scale with bosses beaten.
	public abstract class OmegaAbility : RobotAbility
	{
		public override RobotFormType Form => RobotFormType.OmegaJack;

		// God Jack has all of Omega Jack's abilities too.
		public override bool AllowedIn(RobotFormType form) => form == RobotFormType.OmegaJack || form == RobotFormType.GodJack;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.noUseGraphic = true;
			Item.rare = ItemRarityID.Purple;
		}

		// Cycles through the five elements.
		protected static JackElement ElementAt(int i) => (JackElement)(i % Elements.Count);
	}

	// Hold left click: a colossal rainbow beam that cuts through blocks and applies every element.
	public class OmegaCannon : OmegaAbility
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.channel = true;
			Item.damage = 160;
			Item.knockBack = 3f;
			Item.UseSound = SoundID.Item122;
			Item.shoot = ModContent.ProjectileType<ElementBeam>();
			Item.shootSpeed = 1f;
		}

		protected override bool CanUseAbility(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI, ai1: 0f, ai2: 1f);
			return false;
		}
	}

	// Thirty homing, exploding bolts of every element burst out in all directions.
	public class PrismStorm : OmegaAbility
	{
		public const int Bolts = 30;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.autoReuse = true;
			Item.damage = 140;
			Item.knockBack = 5f;
			Item.UseSound = SoundID.Item9;
			Item.shoot = ModContent.ProjectileType<ElementBolt>();
			Item.shootSpeed = 12f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			for (int i = 0; i < Bolts; i++) {
				Vector2 vel = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / Bolts) * Item.shootSpeed;
				Projectile.NewProjectile(source, player.MountedCenter, vel, type, damage, knockback, player.whoAmI,
					ai0: (int)ElementAt(i), ai1: ElementBolt.Homing | ElementBolt.Explode | ElementBolt.Big);
			}
			return false;
		}
	}

	// Twenty-five eruptions of every element tear across the ground around the cursor. 12 second cooldown.
	public class ElementalCataclysm : OmegaAbility
	{
		public const int Pillars = 25;
		public const float Width = 1600f;

		public override int CooldownTicks => 12 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 40;
			Item.useAnimation = 40;
			Item.damage = 450;
			Item.knockBack = 8f;
			Item.UseSound = SoundID.Item74;
			Item.shoot = ModContent.ProjectileType<ElementPillar>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 cursor = Main.MouseWorld;
			for (int i = 0; i < Pillars; i++) {
				float x = cursor.X + MathHelper.Lerp(-Width / 2f, Width / 2f, i / (float)(Pillars - 1));
				Vector2 ground = OrbitalStrike.FindImpactPoint(new Vector2(x, cursor.Y - 80f));
				int delay = System.Math.Abs(i - Pillars / 2) * 3; // ripples outward from the cursor
				Projectile.NewProjectile(source, ground, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: (int)ElementAt(i), ai1: delay);
			}
			Main.instance.CameraModifiers.Add(new PunchCameraModifier(cursor, Vector2.UnitY, 14f, 10f, 45, 3000f, "RobotJack/Cataclysm"));
			return false;
		}
	}

	// Five satellites fire orbital strikes in a row across the cursor. 20 second cooldown.
	public class OmegaBarrage : OmegaAbility
	{
		public const int Strikes = 5;
		public const float Spacing = 180f;

		public override int CooldownTicks => 20 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.damage = 1500; // per hit; each beam hits about ten times
			Item.knockBack = 10f;
			Item.UseSound = SoundID.Item92;
			Item.shoot = ModContent.ProjectileType<OrbitalStrike>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 cursor = Main.MouseWorld;
			for (int i = 0; i < Strikes; i++) {
				float offset = (i - (Strikes - 1) / 2f) * Spacing;
				Vector2 target = OrbitalStrike.FindImpactPoint(cursor + new Vector2(offset, 0f));
				Projectile.NewProjectile(source, target, Vector2.Zero, type, damage, knockback, player.whoAmI);
			}
			return false;
		}
	}

	// Open a black hole at the cursor that drags in and shreds everything nearby (even bosses),
	// then collapses into five huge elemental explosions. 25 second cooldown.
	public class SingularityAbility : OmegaAbility
	{
		public override int CooldownTicks => 25 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.damage = 200; // core damage every 10 ticks; the collapse deals six times this
			Item.knockBack = 0f;
			Item.UseSound = SoundID.Item117 with { Pitch = -1f };
			Item.shoot = ModContent.ProjectileType<Singularity>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero, type, damage, knockback, player.whoAmI);
			return false;
		}
	}

	// Freeze every enemy on screen in place for 6 seconds (not bosses). 40 second cooldown.
	public class TimeStop : OmegaAbility
	{
		public const float Range = 1800f;
		public const int StopTicks = 6 * 60;

		public override int CooldownTicks => 40 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.UseSound = SoundID.Item29 with { Pitch = -0.8f, Volume = 1.4f };
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (npc.active && !npc.friendly && npc.lifeMax > 5 && Vector2.Distance(npc.Center, player.Center) < Range) {
						Stunned.TryApply(npc, StopTicks);
					}
				}
				// Visual: the rainbow transformation burst.
				Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<TransformBurst>(), 0, 0f, player.whoAmI, ai2: 6f);
			}
			for (int i = 0; i < 80; i++) {
				Dust dust = Dust.NewDustPerfect(player.Center, DustID.GemDiamond, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 20f), Scale: 1.5f);
				dust.noGravity = true;
			}
			return true;
		}
	}

	// A near-unstoppable dash toward the cursor: a second of invincibility and a line of explosions
	// of every element along the way.
	public class OmegaDash : OmegaAbility
	{
		public const int Blasts = 6;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 35;
			Item.useAnimation = 35;
			Item.DamageType = DamageClass.Melee;
			Item.damage = 400;
			Item.knockBack = 12f;
			Item.UseSound = SoundID.Item74;
			Item.shoot = ModContent.ProjectileType<ThrusterHitbox>();
			Item.shootSpeed = 1f;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
			player.velocity = dir * 26f;
			player.immune = true;
			player.immuneTime = 60;
			player.fallStart = (int)(player.position.Y / 16f);
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, damage, knockback, player.whoAmI, ai0: 1f + Main.rand.Next(Elements.Count));
			for (int i = 1; i <= Blasts; i++) {
				Projectile.NewProjectile(source, player.Center + dir * i * 90f, Vector2.Zero, ModContent.ProjectileType<ElementBlast>(), damage, knockback, player.whoAmI,
					ai0: (int)ElementAt(i), ai1: 130f);
			}
			return false;
		}
	}

	// Ascend for 15 seconds: every variant's power-up at once (Overheat, Cryo Armor, Overcharge,
	// Vampiric Shroud and Celestial Blessing). 60 second cooldown.
	public class Ascension : OmegaAbility
	{
		public const int DurationTicks = 15 * 60;

		public override int CooldownTicks => 60 * 60;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.UseSound = SoundID.Item4;
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				player.AddBuff(ModContent.BuffType<Overheat>(), DurationTicks);
				player.AddBuff(ModContent.BuffType<CryoArmor>(), DurationTicks);
				player.AddBuff(ModContent.BuffType<Overcharge>(), DurationTicks);
				player.AddBuff(ModContent.BuffType<VampiricShroud>(), DurationTicks);
				player.AddBuff(ModContent.BuffType<CelestialBlessing>(), DurationTicks);
			}
			for (int i = 0; i < Elements.Count; i++) {
				ElementFX.Burst(player.Center, (JackElement)i, 15, 8f, 1.6f);
			}
			return true;
		}
	}
}
