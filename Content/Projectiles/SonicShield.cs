using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Titan Speaker's Sonic Shield: a bubble of sound around the player for 6 seconds.
	// Enemy projectiles that touch it are shattered, enemies are pushed out of it and take damage while inside.
	public class SonicShield : ModProjectile
	{
		public const int Lifetime = 360;
		public const float Radius = 100f;

		private static Asset<Texture2D> ringTex, glowTex;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
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
			Projectile.localNPCHitCooldown = 20;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead) {
				Projectile.Kill();
				return;
			}
			Timer++;
			Projectile.Center = player.Center;

			// Shatter enemy projectiles that reach the bubble (only on this client: they can't hurt the owner any more).
			if (Projectile.owner == Main.myPlayer) {
				for (int i = 0; i < Main.maxProjectiles; i++) {
					Projectile proj = Main.projectile[i];
					if (proj.active && proj.hostile && proj.damage > 0 && Vector2.Distance(proj.Center, Projectile.Center) < Radius + proj.width / 2f) {
						for (int d = 0; d < 6; d++) {
							Dust.NewDustPerfect(proj.Center, DustID.GemRuby, Main.rand.NextVector2Circular(3f, 3f)).noGravity = true;
						}
						proj.active = false;
					}
				}
			}

			// Push enemies out (enemy positions belong to the server or single player).
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (!npc.active || npc.friendly || npc.boss || npc.knockBackResist <= 0f) {
						continue;
					}
					Vector2 away = npc.Center - Projectile.Center;
					float dist = away.Length();
					if (dist < Radius && dist > 1f) {
						npc.velocity = away / dist * 6f * npc.knockBackResist;
						npc.netUpdate = true;
					}
				}
			}
			Lighting.AddLight(Projectile.Center, 0.6f, 0.2f, 0.2f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
			float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.Distance(Projectile.Center, new Vector2(x, y)) < Radius;
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ringTex.Value;
			Texture2D glow = glowTex.Value;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * MathHelper.Clamp(Timer / 10f, 0f, 1f);
			float pulse = 1f + 0.05f * (float)System.Math.Sin(Timer * 0.4f);
			float scale = Radius * 2f / ring.Width * pulse;

			Main.EntitySpriteDraw(glow, center, null, new Color(255, 60, 60, 0) * 0.25f * fade, 0f, glow.Size() / 2f, Radius * 2.2f / glow.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, center, null, new Color(255, 90, 90, 0) * 0.9f * fade, 0f, ring.Size() / 2f, scale, SpriteEffects.None, 0);
			// Sound ripples moving outward inside the bubble.
			for (int i = 0; i < 3; i++) {
				float t = (Timer / 40f + i / 3f) % 1f;
				Main.EntitySpriteDraw(ring, center, null, new Color(255, 255, 255, 0) * (1f - t) * 0.5f * fade, 0f, ring.Size() / 2f, scale * t, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
