using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// A travelling wave that grows as it flies and passes through everything, hitting each target once.
	// ai[0] = full size in pixels. ai[1] = style: 0 = Titan Speaker sound wave (heavy knockback),
	// 1 = Titan TV hypno wave (confuses and stuns).
	public class SoundWave : ModProjectile
	{
		public const int Lifetime = 45;
		public const int StyleSpeaker = 0;
		public const int StyleHypno = 1;

		private static Asset<Texture2D> ringTex;

		private float FullSize => Projectile.ai[0] <= 0f ? 120f : Projectile.ai[0];
		private int Style => (int)Projectile.ai[1];

		// Grows from 30% to full size over its life.
		private float Size => FullSize * MathHelper.Lerp(0.3f, 1f, 1f - Projectile.timeLeft / (float)Lifetime);

		public override void SetStaticDefaults() {
			if (!Main.dedServ) {
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
			}
		}

		public override void Unload() {
			ringTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.aiStyle = -1;
		}

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (Main.rand.NextBool(3)) {
				Vector2 side = Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-0.5f, 0.5f) * Size;
				int dustType = Style == StyleHypno ? DustID.PinkFairy : DustID.Smoke;
				Dust dust = Dust.NewDustPerfect(Projectile.Center + side, dustType, Projectile.velocity * 0.3f, 100, default, 1.1f);
				dust.noGravity = true;
			}
			Color light = Style == StyleHypno ? new Color(170, 80, 255) : new Color(255, 200, 200);
			Lighting.AddLight(Projectile.Center, light.ToVector3() * 0.4f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			float size = Size;
			Rectangle area = new Rectangle((int)(Projectile.Center.X - size / 2f), (int)(Projectile.Center.Y - size / 2f), (int)size, (int)size);
			return area.Intersects(targetHitbox);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			// Push along the wave's direction of travel.
			modifiers.HitDirectionOverride = Projectile.velocity.X >= 0f ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Style == StyleHypno) {
				target.AddBuff(BuffID.Confused, 300);
				Stunned.TryApply(target, 90);
			}
			else {
				target.AddBuff(BuffID.Confused, 60);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D ring = ringTex.Value;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
			Color outer = Style == StyleHypno ? new Color(170, 80, 255, 0) : new Color(255, 120, 120, 0);
			Color inner = Style == StyleHypno ? new Color(255, 170, 255, 0) : new Color(255, 255, 255, 0);
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float size = Size;

			// A squashed ring reads as a ")" wave front; a few fading copies trail behind it.
			for (int i = 2; i >= 0; i--) {
				Vector2 pos = Projectile.Center - dir * i * size * 0.12f - Main.screenPosition;
				Vector2 scale = new Vector2(size * 0.35f / ring.Width, size / ring.Height) * (1f - i * 0.12f);
				float opacity = fade * (1f - i * 0.3f);
				Main.EntitySpriteDraw(ring, pos, null, outer * opacity, Projectile.rotation, ring.Size() / 2f, scale * 1.1f, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(ring, pos, null, inner * opacity, Projectile.rotation, ring.Size() / 2f, scale, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
