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
