using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Common;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Robot Jack's transformation effect (visual only, no damage). Energy rushes in toward the player,
	// a pillar of light slams down from the sky onto them, shockwave rings burst out and sparks fly.
	// ai[1] = 1: the smaller version played when turning back.
	// ai[2] = colour: 0 = Robot Jack's cyan, 1-5 = a variant's element (JackElement + 1).
	public class TransformBurst : ModProjectile
	{
		public const int Lifetime = 45;

		private Color Cyan => Projectile.ai[2] >= 1f ? Elements.Main(Elements.FromAI(Projectile.ai[2] - 1f)) : new Color(90, 230, 255);
		private int SparkDust => Projectile.ai[2] >= 1f ? Elements.Dust(Elements.FromAI(Projectile.ai[2] - 1f)) : DustID.Electric;
		private static Asset<Texture2D> beamTex, glowTex, ringTex;

		private ref float Timer => ref Projectile.ai[0];
		private bool TurningBack => Projectile.ai[1] == 1f;
		private float Size => TurningBack ? 0.6f : 1f;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400; // the light pillar reaches up past the screen
			if (!Main.dedServ) {
				beamTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalBeam");
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
			}
		}

		public override void Unload() {
			beamTex = glowTex = ringTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Timer++;
			Player player = Main.player[Projectile.owner];
			Projectile.Center = player.Center;

			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = TurningBack ? 0.4f : -0.2f, Volume = 0.8f }, Projectile.Center);
				if (!TurningBack) {
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, 6f, 8f, 20, 1200f, FullName));
				}
			}

			// Energy rushing in from all around for the first few ticks...
			if (Timer < 14) {
				for (int i = 0; i < 4; i++) {
					Vector2 offset = Main.rand.NextVector2CircularEdge(160f, 160f) * Size;
					Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, SparkDust, -offset / 12f, Scale: 1.1f);
					dust.noGravity = true;
				}
			}
			// ...then a burst of sparks out of the player.
			if (Timer == 14) {
				for (int i = 0; i < (TurningBack ? 20 : 45); i++) {
					Dust dust = Dust.NewDustPerfect(Projectile.Center, SparkDust, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 11f) * Size, Scale: 1.4f);
					dust.noGravity = true;
				}
				for (int i = 0; i < 12; i++) {
					Dust.NewDustPerfect(player.Bottom, DustID.Smoke, new Vector2(Main.rand.NextFloat(-5f, 5f), Main.rand.NextFloat(-2f, 0f)), 120, default, 1.5f);
				}
			}
			float light = MathHelper.Clamp(1f - Timer / Lifetime, 0f, 1f) * 1.5f * Size;
			Lighting.AddLight(Projectile.Center, Cyan.ToVector3() * light);
		}

		private static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Texture2D beam = beamTex.Value;
			Texture2D glow = glowTex.Value;
			Texture2D ring = ringTex.Value;
			Player player = Main.player[Projectile.owner];
			Vector2 center = Projectile.Center - Main.screenPosition;
			Vector2 feet = player.Bottom - Main.screenPosition;

			// Pillar of light from above the screen down onto the player: slams in wide, then narrows away.
			if (Timer >= 10 && Timer < 36) {
				float t = (Timer - 10) / 26f;
				float width = MathHelper.Lerp(110f, 0f, t) * Size;
				float top = -40f;
				float length = feet.Y - top;
				if (length > 0f && width > 0.5f) {
					Vector2 origin = new Vector2(beam.Width / 2f, 0f);
					Main.EntitySpriteDraw(beam, new Vector2(feet.X, top), null, Glow(Cyan, 0.5f), 0f, origin, new Vector2(width * 1.8f / beam.Width, length / beam.Height), SpriteEffects.None, 0);
					Main.EntitySpriteDraw(beam, new Vector2(feet.X, top), null, Glow(Color.White, 0.9f), 0f, origin, new Vector2(width * 0.6f / beam.Width, length / beam.Height), SpriteEffects.None, 0);
				}
			}

			// Shockwave rings along the ground and around the body.
			for (int i = 0; i < 2; i++) {
				float rt = (Timer - 14 - i * 6) / 24f;
				if (rt >= 0f && rt <= 1f) {
					float scale = (0.3f + rt * 2.4f) * Size;
					Main.EntitySpriteDraw(ring, center, null, Glow(Cyan, 1f - rt), 0f, ring.Size() / 2f, scale, SpriteEffects.None, 0);
					Main.EntitySpriteDraw(ring, feet, null, Glow(Color.White, (1f - rt) * 0.8f), 0f, ring.Size() / 2f, new Vector2(scale * 1.4f, scale * 0.3f), SpriteEffects.None, 0);
				}
			}

			// Flash on the body when the pillar lands.
			float flash = MathHelper.Clamp(1f - System.Math.Abs(Timer - 14f) / 10f, 0f, 1f);
			if (flash > 0f) {
				Main.EntitySpriteDraw(glow, center, null, Glow(Cyan, flash), 0f, glow.Size() / 2f, 2.2f * Size, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(glow, center, null, Glow(Color.White, flash), 0f, glow.Size() / 2f, 1f * Size, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
