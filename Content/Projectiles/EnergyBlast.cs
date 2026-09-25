using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// The Energy Absorber's release: an expanding sphere of stored energy centred on the player.
	// Its damage (set by EnergyAbsorber) is 10x the stored energy, and it hits each target once.
	public class EnergyBlast : ModProjectile
	{
		public const float MaxRadius = 560f;
		public const int GrowTicks = 20;
		public const int Lifetime = 40;

		private static readonly Color FieldColor = new Color(150, 90, 255);
		private static readonly Color CoreColor = new Color(120, 235, 255);

		private static Asset<Texture2D> ringTex, glowTex;

		private ref float Timer => ref Projectile.ai[0];

		private float Radius => MaxRadius * (1f - (1f - MathHelper.Clamp(Timer / GrowTicks, 0f, 1f)) * (1f - MathHelper.Clamp(Timer / GrowTicks, 0f, 1f)));

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxRadius + 300;
			if (!Main.dedServ) {
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
			}
		}

		public override void Unload() {
			ringTex = glowTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1; // each target is hit once
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Timer++;
			Projectile.Center = Main.player[Projectile.owner].MountedCenter;

			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.6f, Volume = 1.5f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.4f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = 0.2f }, Projectile.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 16f, 8f, 35, 2500f, FullName));

				for (int i = 0; i < 90; i++) {
					Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(6f, 20f);
					Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, vel, Scale: Main.rand.NextFloat(1.2f, 2f));
					dust.noGravity = true;
				}
			}

			if (Timer <= GrowTicks) {
				// Sparks riding the edge of the sphere.
				for (int i = 0; i < 8; i++) {
					Vector2 edge = Main.rand.NextVector2CircularEdge(Radius, Radius);
					Dust.NewDustPerfect(Projectile.Center + edge, DustID.Electric, edge.SafeNormalize(Vector2.Zero) * 3f, Scale: 1.3f).noGravity = true;
				}
			}
			float fade = 1f - Timer / Lifetime;
			Lighting.AddLight(Projectile.Center, 1.2f * fade, 0.9f * fade, 2f * fade);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Timer > GrowTicks + 2) {
				return false;
			}
			float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
			float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.Distance(Projectile.Center, new Vector2(x, y)) < Radius;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Electrified, 300);
		}

		private static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ringTex.Value;
			Texture2D glow = glowTex.Value;
			float fade = 1f - Timer / Lifetime;
			float radius = Radius;

			// Filled sphere, bright shell and a second trailing shell.
			Main.EntitySpriteDraw(glow, center, null, Glow(FieldColor, 0.6f * fade), 0f, glow.Size() / 2f, radius * 2.2f / glow.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, center, null, Glow(CoreColor, fade), Timer * 0.02f, ring.Size() / 2f, radius * 2f / ring.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, center, null, Glow(FieldColor, 0.8f * fade), -Timer * 0.03f, ring.Size() / 2f, radius * 1.4f / ring.Width, SpriteEffects.None, 0);

			// White flash at the start.
			float flash = MathHelper.Clamp(1f - Timer / 12f, 0f, 1f);
			Main.EntitySpriteDraw(glow, center, null, Glow(Color.White, flash), 0f, glow.Size() / 2f, 6f * flash + 1f, SpriteEffects.None, 0);
			return false;
		}
	}
}
