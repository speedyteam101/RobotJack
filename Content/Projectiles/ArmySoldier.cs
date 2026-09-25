using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// One soldier of a Titan's summoned army. It marches along the ground for 20 seconds, runs at the nearest enemy,
	// jumps over obstacles, hits enemies it touches and shoots at them from range.
	// ai[0] = style: 0 = speaker soldier (sound waves), 1 = camera soldier (plasma bolts), 2 = TV soldier (static orbs).
	// The texture is a grid: one column per style, four rows of animation (idle, walk 1, walk 2, jump).
	public class ArmySoldier : ModProjectile
	{
		public const int Lifetime = 20 * 60;
		public const float SightRange = 700f;
		public const float ShootRange = 420f;
		public const int FireEvery = 50;
		public const float WalkSpeed = 4.5f;

		private const int FrameWidth = 32;
		private const int FrameHeight = 44;

		private int Style => System.Math.Clamp((int)Projectile.ai[0], 0, 2);
		private ref float Timer => ref Projectile.localAI[0];
		private ref float WalkAnim => ref Projectile.localAI[1];

		private RobotFormType FormForStyle => Style switch {
			0 => RobotFormType.TitanSpeaker,
			1 => RobotFormType.TitanCamera,
			_ => RobotFormType.TitanTV,
		};

		private Color AccentColor => Style switch {
			0 => new Color(255, 70, 70),
			1 => new Color(70, 180, 255),
			_ => new Color(200, 90, 255),
		};

		public override void SetStaticDefaults() {
			ProjectileID.Sets.CultistIsResistantTo[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 38;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.penetrate = -1;
			Projectile.tileCollide = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
			Projectile.aiStyle = -1;
		}

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead || player.GetModPlayer<RobotJackPlayer>().ActiveForm != FormForStyle) {
				Projectile.Kill();
				return;
			}
			Timer++;

			// Too far from the Titan (fell behind, stuck): warp back next to them.
			if (Vector2.Distance(Projectile.Center, player.Center) > 1400f) {
				Projectile.Center = player.Center + new Vector2(Main.rand.NextFloat(-60f, 60f), -20f);
				Projectile.velocity = Vector2.Zero;
				Projectile.netUpdate = true;
				SpawnDust();
			}

			NPC target = FindTarget();
			float goalX = target != null
				? target.Center.X
				: player.Center.X + (Projectile.ai[1] - 2.5f) * 28f; // form up around the Titan when idle
			float dx = goalX - Projectile.Center.X;
			bool onGround = Projectile.velocity.Y == 0f;

			if (System.Math.Abs(dx) > 12f) {
				Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, System.Math.Sign(dx) * WalkSpeed, 0.15f);
			}
			else {
				Projectile.velocity.X *= 0.8f;
			}

			// Jump when blocked by a wall, or to reach an enemy (or the Titan) that's well above.
			bool blocked = onGround && System.Math.Abs(Projectile.velocity.X) < 0.5f && System.Math.Abs(dx) > 20f;
			float targetY = target != null ? target.Center.Y : player.Center.Y;
			bool needsHeight = onGround && targetY < Projectile.Center.Y - 80f && Main.rand.NextBool(20);
			if (blocked || needsHeight) {
				Projectile.velocity.Y = -8.5f;
			}

			Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + 0.4f, 12f); // gravity

			if (System.Math.Abs(Projectile.velocity.X) > 0.3f) {
				Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;
			}

			// Shoot at the target (the owner decides; new projectiles sync like any other).
			if (target != null && Projectile.owner == Main.myPlayer && (Timer + Projectile.ai[1] * 8) % FireEvery == 0
				&& Vector2.Distance(target.Center, Projectile.Center) < ShootRange) {
				Shoot(target);
			}

			// Animation.
			if (!onGround) {
				Projectile.frame = 3;
			}
			else if (System.Math.Abs(Projectile.velocity.X) > 0.5f) {
				WalkAnim += System.Math.Abs(Projectile.velocity.X) * 0.08f;
				Projectile.frame = 1 + (int)WalkAnim % 2;
			}
			else {
				Projectile.frame = 0;
			}

			Lighting.AddLight(Projectile.Center, AccentColor.ToVector3() * 0.25f);
		}

		public override void OnSpawn(Terraria.DataStructures.IEntitySource source) {
			SpawnDust();
		}

		private void SpawnDust() {
			int dustType = Style switch { 0 => DustID.GemRuby, 1 => DustID.Electric, _ => DustID.PinkFairy };
			for (int i = 0; i < 12; i++) {
				Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, 0f, -2f).noGravity = true;
			}
		}

		private NPC FindTarget() {
			NPC best = null;
			float bestDist = SightRange;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy(Projectile)) {
					continue;
				}
				float dist = Vector2.Distance(npc.Center, Projectile.Center);
				if (dist < bestDist) {
					bestDist = dist;
					best = npc;
				}
			}
			return best;
		}

		private void Shoot(NPC target) {
			Vector2 from = Projectile.Center + new Vector2(0f, -10f);
			Vector2 dir = (target.Center - from).SafeNormalize(Vector2.UnitX * Projectile.spriteDirection);
			var source = Projectile.GetSource_FromThis();
			int damage = (int)(Projectile.damage * 0.8f);
			switch (Style) {
				case 0:
					Projectile.NewProjectile(source, from, dir * 11f, ModContent.ProjectileType<SoundWave>(), damage, 4f, Projectile.owner, ai0: 70f, ai1: SoundWave.StyleSpeaker);
					SoundEngine.PlaySound(SoundID.Item38 with { Pitch = 0.9f, Volume = 0.3f }, from);
					break;
				case 1:
					Projectile.NewProjectile(source, from, dir * 15f, ModContent.ProjectileType<PlasmaBolt>(), damage, 2f, Projectile.owner);
					SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.3f }, from);
					break;
				default:
					Projectile.NewProjectile(source, from, dir * 9f, ModContent.ProjectileType<EnergyOrb>(), damage, 2f, Projectile.owner, ai0: 1f);
					SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.3f }, from);
					break;
			}
		}

		// Keep walking into walls and along the floor instead of dying on contact with blocks.
		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = 0f;
			}
			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = 0f;
			}
			return false;
		}

		// Walk over platforms instead of falling through them.
		public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac) {
			fallThrough = false;
			return true;
		}

		public override void OnKill(int timeLeft) {
			SpawnDust();
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			Rectangle frame = new Rectangle(Style * FrameWidth, Projectile.frame * FrameHeight, FrameWidth, FrameHeight);
			// Feet on the bottom of the hitbox. The sheet faces right.
			Vector2 pos = Projectile.Bottom - Main.screenPosition + new Vector2(0f, 2f);
			SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
			Main.EntitySpriteDraw(tex, pos, frame, lightColor * fade, 0f, new Vector2(FrameWidth / 2f, FrameHeight), 1f, effects, 0);
			return false;
		}
	}
}
