using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Invisible hitbox on Robot Jack's head. RobotJackPlayer keeps one alive while the player is transformed,
	// wearing the Robot Jetpack and moving fast. Bumping into an enemy (or a PvP opponent) with it deals damage
	// and heavy knockback, and sends them spinning for as long as the knockback lasts.
	public class HeadRam : ModProjectile
	{
		public const int BaseDamage = 40;
		public const float Knockback = 11f;
		public const float MinSpeed = 4f;

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 26;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 10;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public static bool CanRam(Player player) {
			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();
			return player.active && !player.dead && modPlayer.Transformed && modPlayer.jetpack && player.velocity.Length() >= MinSpeed;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (Projectile.owner == Main.myPlayer) {
				if (!CanRam(player)) {
					Projectile.Kill();
					return;
				}
				Projectile.timeLeft = 10;
			}

			// Sit on the robot's head, nudged toward the direction of travel so it leads with the head.
			Vector2 head = player.Top + new Vector2(player.direction * 3f, 4f * player.gravDir);
			Projectile.Center = head + player.velocity.SafeNormalize(Vector2.Zero) * 6f;
			Projectile.direction = player.velocity.X >= 0f ? 1 : -1;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			// Always knock the target away from the robot.
			modifiers.HitDirectionOverride = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			int spin = Spinning.DurationFor(hit.Knockback);
			if (spin > 0) {
				target.AddBuff(ModContent.BuffType<Spinning>(), spin);
			}
			Bonk(target.Center);
		}

		// PvP hits: the victim's RobotJackPlayer.OnHurt starts their spin.
		private void Bonk(Vector2 where) {
			Player player = Main.player[Projectile.owner];
			// Bounce back off the target a little.
			player.velocity *= -0.35f;
			SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -0.2f }, where);
			for (int i = 0; i < 10; i++) {
				Dust dust = Dust.NewDustPerfect(where, DustID.Electric, Main.rand.NextVector2Circular(5f, 5f), Scale: 1.1f);
				dust.noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}
}
