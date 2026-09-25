using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// An absorbed attack fired back by the Energy Absorber's release, as a glowing copy of the original projectile.
	// ai[0] holds the original projectile type (only used for drawing); the damage is set by EnergyAbsorber.
	public class EnergyEcho : ModProjectile
	{
		private int OriginalType => (int)Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults() {
			Projectile.width = 24;
			Projectile.height = 24;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 3;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 75;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
			if (Main.rand.NextBool(2)) {
				Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.1f, Scale: 0.9f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.4f, 0.4f, 0.9f);
		}

		public override bool PreDraw(ref Color lightColor) {
			int type = OriginalType;
			if (type <= 0 || type >= ProjectileLoader.ProjectileCount) {
				type = Type;
			}
			if (type < ProjectileID.Count) {
				Main.instance.LoadProjectile(type); // vanilla textures load on demand
			}
			Texture2D tex = TextureAssets.Projectile[type].Value;
			// Many projectile textures are sprite sheets: draw only the first frame.
			int frames = System.Math.Max(Main.projFrames[type], 1);
			Rectangle frame = new Rectangle(0, 0, tex.Width, tex.Height / frames);
			Vector2 origin = frame.Size() / 2f;
			float scale = System.Math.Min(1.4f, 48f / System.Math.Max(frame.Width, frame.Height) + 0.4f);

			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] == Vector2.Zero) {
					continue;
				}
				float fade = 1f - i / (float)Projectile.oldPos.Length;
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
				Main.EntitySpriteDraw(tex, pos, frame, new Color(150, 90, 255, 0) * fade * 0.5f, Projectile.rotation, origin, scale * fade, SpriteEffects.None, 0);
			}
			Vector2 center = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(tex, center, frame, new Color(120, 235, 255, 0), Projectile.rotation, origin, scale * 1.2f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(tex, center, frame, Color.White, Projectile.rotation, origin, scale, SpriteEffects.None, 0);
			return false;
		}
	}
}
