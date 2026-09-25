using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Titan TV's Static Field: a crackling field around the player for 8 seconds that zaps every enemy inside it
	// with lightning three times a second.
	public class StaticField : ModProjectile
	{
		public const int Lifetime = 480;
		public const float Radius = 260f;

		private static Asset<Texture2D> ringTex, beamTex;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Radius + 200;
			if (!Main.dedServ) {
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
				beamTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalBeam");
			}
		}

		public override void Unload() {
			ringTex = beamTex = null;
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
			if (Timer % 20 == 1) {
				SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.35f, Pitch = Main.rand.NextFloat(-0.2f, 0.3f) }, Projectile.Center);
			}
			for (int i = 0; i < 2; i++) {
				Vector2 offset = Main.rand.NextVector2Circular(Radius, Radius);
				Dust.NewDustPerfect(Projectile.Center + offset, DustID.PinkFairy, Main.rand.NextVector2Circular(1f, 1f), Scale: 0.9f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.5f, 0.2f, 0.8f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
			float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.Distance(Projectile.Center, new Vector2(x, y)) < Radius;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Electrified, 120);
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ringTex.Value;
			Texture2D beam = beamTex.Value;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * MathHelper.Clamp(Timer / 10f, 0f, 1f);
			Color purple = new Color(200, 90, 255, 0);

			Main.EntitySpriteDraw(ring, center, null, purple * 0.5f * fade, Timer * 0.02f, ring.Size() / 2f, Radius * 2f / ring.Width, SpriteEffects.None, 0);

			// Jagged lightning to every enemy inside, redrawn with new kinks every few ticks.
			Terraria.Utilities.UnifiedRandom rand = new Terraria.Utilities.UnifiedRandom((int)(Timer / 4) + Projectile.whoAmI * 97);
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.lifeMax <= 5 || Vector2.Distance(npc.Center, Projectile.Center) > Radius) {
					continue;
				}
				Vector2 from = center;
				Vector2 to = npc.Center - Main.screenPosition;
				const int segments = 6;
				for (int s = 1; s <= segments; s++) {
					Vector2 next = Vector2.Lerp(center, to, s / (float)segments);
					if (s < segments) {
						next += rand.NextVector2Circular(14f, 14f);
					}
					DrawSegment(beam, from, next, 5f, purple * fade);
					DrawSegment(beam, from, next, 2f, new Color(255, 255, 255, 0) * fade);
					from = next;
				}
			}
			return false;
		}

		private static void DrawSegment(Texture2D beam, Vector2 from, Vector2 to, float width, Color color) {
			Vector2 diff = to - from;
			float length = diff.Length();
			if (length < 1f) {
				return;
			}
			Main.EntitySpriteDraw(beam, from, null, color, diff.ToRotation() - MathHelper.PiOver2, new Vector2(beam.Width / 2f, 0f),
				new Vector2(width / beam.Width, length / beam.Height), SpriteEffects.None, 0);
		}
	}
}
