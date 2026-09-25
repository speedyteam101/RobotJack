using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Titan Camera's Flash Grenade: a thrown flash bulb that pops into a small camera flash
	// (damage, confusion and a short stun) when it hits something or after a second.
	public class FlashGrenade : ModProjectile
	{
		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 60;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.velocity.Y += 0.3f; // arcs like a thrown grenade
			Projectile.rotation += Projectile.velocity.X * 0.05f;
			if (Main.rand.NextBool(2)) {
				Dust.NewDustPerfect(Projectile.Center, DustID.GemDiamond, Vector2.Zero, Scale: 0.8f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.6f, 0.7f, 0.9f);
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			return true; // pop on impact
		}

		public override void OnKill(int timeLeft) {
			if (Projectile.owner == Main.myPlayer) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
					ModContent.ProjectileType<CameraFlashBurst>(), Projectile.damage, 2f, Projectile.owner, ai1: 1f);
			}
		}
	}
}
