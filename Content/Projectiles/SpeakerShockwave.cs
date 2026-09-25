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
	// Titan Speaker's Bass Drop: a ring of sound bursting out from the player that hits everything around
	// once, throwing it away from the player and confusing it.
	public class SpeakerShockwave : ModProjectile
	{
		public const float MaxRadius = 300f;
		public const int GrowTicks = 14;
		public const int Lifetime = 30;

		private static Asset<Texture2D> ringTex, glowTex;

		private ref float Timer => ref Projectile.ai[0];

		private float Radius {
			get {
				float t = MathHelper.Clamp(Timer / GrowTicks, 0f, 1f);
				return MaxRadius * (1f - (1f - t) * (1f - t));
			}
		}

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxRadius + 200;
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
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Timer++;
			Projectile.Center = Main.player[Projectile.owner].Center;

			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.9f, Volume = 1.4f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item38 with { Pitch = -0.5f }, Projectile.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, 14f, 10f, 30, 1800f, FullName));
				// Dust kicked up off the ground.
				for (int i = 0; i < 40; i++) {
					Vector2 vel = new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(-5f, -1f));
					Dust.NewDustPerfect(Main.player[Projectile.owner].Bottom, DustID.Smoke, vel, 100, default, Main.rand.NextFloat(1.4f, 2.2f));
				}
			}
			if (Timer <= GrowTicks) {
				for (int i = 0; i < 6; i++) {
					Vector2 edge = Main.rand.NextVector2CircularEdge(Radius, Radius);
					Dust.NewDustPerfect(Projectile.Center + edge, DustID.Torch, edge.SafeNormalize(Vector2.Zero) * 2f, Scale: 1.3f).noGravity = true;
				}
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Timer > GrowTicks + 2) {
				return false;
			}
			float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
			float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.Distance(Projectile.Center, new Vector2(x, y)) < Radius;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Confused, 180);
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ringTex.Value;
			Texture2D glow = glowTex.Value;
			float fade = 1f - Timer / Lifetime;
			float radius = Radius;

			Main.EntitySpriteDraw(glow, center, null, new Color(255, 60, 60, 0) * 0.35f * fade, 0f, glow.Size() / 2f, radius * 2f / glow.Width, SpriteEffects.None, 0);
			for (int i = 0; i < 3; i++) {
				float r = radius * (1f - i * 0.18f);
				Color color = i == 0 ? new Color(255, 255, 255, 0) : new Color(255, 90, 90, 0);
				Main.EntitySpriteDraw(ring, center, null, color * fade * (1f - i * 0.25f), 0f, ring.Size() / 2f, r * 2f / ring.Width, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
