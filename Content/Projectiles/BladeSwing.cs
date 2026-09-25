using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Titan TV's Energy Blades: a huge energy blade swung in a wide arc around the aim direction.
	// ai[0] = aim angle (set when used). Hits each target once per swing.
	public class BladeSwing : ModProjectile
	{
		public const int SwingTicks = 16;
		public const float BladeLength = 150f;
		public const float Arc = MathHelper.Pi * 1.1f; // total sweep, about 200 degrees

		private static readonly Color BladeColor = new Color(200, 90, 255);

		private static Asset<Texture2D> beamTex, glowTex;

		private float AimAngle => Projectile.ai[0];
		private ref float Timer => ref Projectile.ai[1];

		public override void SetStaticDefaults() {
			if (!Main.dedServ) {
				beamTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalBeam");
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
			}
		}

		public override void Unload() {
			beamTex = glowTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = SwingTicks;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.aiStyle = -1;
			Projectile.ownerHitCheck = true; // can't hit through walls
		}

		public override bool ShouldUpdatePosition() => false;

		// Blade angle at a given point of the swing (0..1). Swings top to bottom on the side the player faces.
		private float AngleAt(float progress) {
			int dir = System.Math.Cos(AimAngle) >= 0 ? 1 : -1;
			float eased = 1f - (1f - progress) * (1f - progress);
			return AimAngle + dir * (-Arc / 2f + Arc * eased);
		}

		private float Progress => MathHelper.Clamp(Timer / SwingTicks, 0f, 1f);

		public override void AI() {
			Timer++;
			Player player = Main.player[Projectile.owner];
			Projectile.Center = player.MountedCenter;

			float angle = AngleAt(Progress);
			Vector2 dir = angle.ToRotationVector2();
			Projectile.direction = System.Math.Cos(AimAngle) >= 0 ? 1 : -1;
			player.ChangeDir(Projectile.direction);
			player.heldProj = Projectile.whoAmI;
			player.itemRotation = (dir * Projectile.direction).ToRotation();

			Vector2 tip = Projectile.Center + dir * BladeLength;
			Dust.NewDustPerfect(tip, DustID.PinkFairy, dir.RotatedBy(MathHelper.PiOver2 * Projectile.direction) * 3f, Scale: 1.2f).noGravity = true;
			Lighting.AddLight(tip, 0.7f, 0.3f, 1f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			// Check the blade now and where it was last tick, so fast swings don't skip over small targets.
			float collisionPoint = 0f;
			foreach (float progress in new[] { Progress, MathHelper.Clamp((Timer - 1) / SwingTicks, 0f, 1f), MathHelper.Clamp((Timer - 0.5f) / SwingTicks, 0f, 1f) }) {
				Vector2 end = Projectile.Center + AngleAt(progress).ToRotationVector2() * BladeLength;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, 30f, ref collisionPoint)) {
					return true;
				}
			}
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D beam = beamTex.Value;
			Texture2D glow = glowTex.Value;
			Vector2 center = Projectile.Center - Main.screenPosition;
			Vector2 origin = new Vector2(beam.Width / 2f, 0f);
			float fade = Timer > SwingTicks - 4 ? (SwingTicks - Timer) / 4f : 1f;

			// Afterimages trailing behind the blade make the swing read as a crescent.
			const int trail = 8;
			for (int i = trail; i >= 0; i--) {
				float progress = MathHelper.Clamp((Timer - i * 0.6f) / SwingTicks, 0f, 1f);
				float angle = AngleAt(progress);
				float opacity = fade * (1f - i / (float)(trail + 1));
				float rotation = angle - MathHelper.PiOver2;
				Vector2 scaleOuter = new Vector2(34f / beam.Width, BladeLength / beam.Height);
				Vector2 scaleInner = new Vector2(12f / beam.Width, BladeLength / beam.Height);
				Main.EntitySpriteDraw(beam, center, null, new Color(BladeColor.R, BladeColor.G, BladeColor.B, 0) * opacity * 0.6f, rotation, origin, scaleOuter, SpriteEffects.None, 0);
				if (i == 0) {
					Main.EntitySpriteDraw(beam, center, null, new Color(255, 255, 255, 0) * opacity, rotation, origin, scaleInner, SpriteEffects.None, 0);
					Vector2 tip = center + angle.ToRotationVector2() * BladeLength;
					Main.EntitySpriteDraw(glow, tip, null, new Color(BladeColor.R, BladeColor.G, BladeColor.B, 0) * opacity, 0f, glow.Size() / 2f, 0.6f, SpriteEffects.None, 0);
				}
			}
			return false;
		}
	}
}
