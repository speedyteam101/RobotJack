using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// A glowing homing orb. ai[0] = colour: 0 = blue (Titan Camera's Lens Burst), 1 = purple (Titan TV's Static Storm).
	public class EnergyOrb : ModProjectile
	{
		private const float Speed = 13f;
		private const int LaunchTicks = 10;

		private static Asset<Texture2D> glowTex;

		private bool Purple => Projectile.ai[0] == 1f;
		private ref float Timer => ref Projectile.ai[1];

		private Color MainColor => Purple ? new Color(190, 80, 255) : new Color(70, 170, 255);

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 10;
			ProjectileID.Sets.TrailingMode[Type] = 0;
			if (!Main.dedServ) {
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
			}
		}

		public override void Unload() {
			glowTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 150;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Timer++;
			if (Timer > LaunchTicks) {
				NPC target = FindTarget(800f);
				if (target != null) {
					Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * Speed;
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.1f);
				}
			}

			if (Main.rand.NextBool(2)) {
				int dustType = Purple ? DustID.PinkFairy : DustID.Electric;
				Dust.NewDustPerfect(Projectile.Center, dustType, -Projectile.velocity * 0.1f, Scale: 0.8f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * 0.6f);
		}

		private NPC FindTarget(float maxDistance) {
			NPC best = null;
			float bestDist = maxDistance;
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

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Electrified, 90);
		}

		public override void OnKill(int timeLeft) {
			int dustType = Purple ? DustID.PinkFairy : DustID.Electric;
			for (int i = 0; i < 10; i++) {
				Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(3f, 3f), Scale: 1f).noGravity = true;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D glow = glowTex.Value;
			Vector2 origin = glow.Size() / 2f;
			Color main = new Color(MainColor.R, MainColor.G, MainColor.B, 0);
			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] == Vector2.Zero) {
					continue;
				}
				float fade = 1f - i / (float)Projectile.oldPos.Length;
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
				Main.EntitySpriteDraw(glow, pos, null, main * fade * 0.5f, 0f, origin, 0.35f * fade, SpriteEffects.None, 0);
			}
			Vector2 center = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(glow, center, null, main, 0f, origin, 0.5f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, new Color(255, 255, 255, 0), 0f, origin, 0.22f, SpriteEffects.None, 0);
			return false;
		}
	}
}
