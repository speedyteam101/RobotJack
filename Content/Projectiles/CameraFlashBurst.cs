using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Titan Camera's Camera Flash: a blinding flash around the player. Everything nearby takes damage, is confused,
	// and (except bosses) is stunned in place for a few seconds.
	public class CameraFlashBurst : ModProjectile
	{
		public const float Radius = 400f;
		public const int StunTicks = 150;
		public const int Lifetime = 30;

		private static Asset<Texture2D> glowTex, ringTex;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Radius + 400;
			if (!Main.dedServ) {
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
			}
		}

		public override void Unload() {
			glowTex = ringTex = null;
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
			Player player = Main.player[Projectile.owner];
			// Flash from the camera lens (front of the head).
			Projectile.Center = player.MountedCenter + new Vector2(player.direction * 28f, -36f * player.gravDir);

			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Camera with { Volume = 1.5f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = 0.5f }, Projectile.Center);
				for (int i = 0; i < 50; i++) {
					Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.GemDiamond, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 14f), Scale: 1.4f);
					dust.noGravity = true;
				}
			}
			float fade = 1f - Timer / Lifetime;
			Lighting.AddLight(Projectile.Center, 3f * fade, 3f * fade, 3.2f * fade);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Timer > 4) {
				return false;
			}
			float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
			float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.Distance(Projectile.Center, new Vector2(x, y)) < Radius;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Confused, StunTicks);
			Stunned.TryApply(target, StunTicks);
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D glow = glowTex.Value;
			Texture2D ring = ringTex.Value;
			float t = Timer / Lifetime;
			float flash = 1f - t;

			// A huge white bloom that fades, plus a thin ring racing out to the edge of the flash.
			Main.EntitySpriteDraw(glow, center, null, new Color(255, 255, 255, 0) * flash, 0f, glow.Size() / 2f, Radius * 2.6f / glow.Width * (0.6f + 0.4f * flash), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, new Color(170, 220, 255, 0) * flash, 0f, glow.Size() / 2f, Radius * 1.2f / glow.Width, SpriteEffects.None, 0);
			float ringRadius = Radius * MathHelper.Clamp(t * 3f, 0f, 1f);
			Main.EntitySpriteDraw(ring, center, null, new Color(200, 235, 255, 0) * flash, 0f, ring.Size() / 2f, ringRadius * 2f / ring.Width, SpriteEffects.None, 0);
			return false;
		}
	}
}
