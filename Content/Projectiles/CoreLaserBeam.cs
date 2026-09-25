using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Common;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// A continuous beam toward the cursor while left click is held. It turns toward the cursor smoothly,
	// stops at solid blocks, and hits everything along it every 6 ticks.
	// ai[1] = style: 0 = Titan Camera's Core Laser (blue, from the chest core),
	// 1 = Titan TV's Broadcast Beam (purple, from the screen).
	public class CoreLaserBeam : ModProjectile
	{
		public const float MaxLength = 1400f;
		public const float BeamWidth = 26f;
		public const float TurnRate = 0.12f;

		private bool Broadcast => Projectile.ai[1] == 1f;
		private Color Outer => Broadcast ? new Color(150, 50, 255) : new Color(40, 120, 255);
		private Color Mid => Broadcast ? new Color(230, 140, 255) : new Color(90, 200, 255);

		private static Asset<Texture2D> beamTex, glowTex;

		private ref float Timer => ref Projectile.ai[0];

		// Length of the beam this tick (until it meets a block).
		private float length;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)MaxLength + 200;
			if (!Main.dedServ) {
				beamTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalBeam");
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
			}
		}

		public override void Unload() {
			beamTex = glowTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 10;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 6;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		// Where the beam starts: the chest core, or the TV screen.
		private Vector2 CorePosition(Player player) => Broadcast
			? player.MountedCenter + new Vector2(player.direction * 6f, -34f * player.gravDir)
			: player.MountedCenter + new Vector2(player.direction * 4f, -8f * player.gravDir);

		public override void AI() {
			Player player = Main.player[Projectile.owner];

			if (Projectile.owner == Main.myPlayer) {
				bool holding = player.channel && !player.noItems && !player.CCed && !player.dead
					&& player.HeldItem.shoot == Type && player.HeldItem.channel && player.GetModPlayer<RobotJackPlayer>().Transformed;
				if (!holding) {
					Projectile.Kill();
					return;
				}

				// Swing toward the cursor. velocity holds the unit aim direction.
				Vector2 wanted = (Main.MouseWorld - CorePosition(player)).SafeNormalize(Vector2.UnitX * player.direction);
				Vector2 current = Projectile.velocity.SafeNormalize(wanted);
				Vector2 aim = Vector2.Lerp(current, wanted, TurnRate).SafeNormalize(wanted);
				if (Vector2.Distance(aim, Projectile.velocity) > 0.001f) {
					Projectile.velocity = aim;
					if (Timer % 3 == 0) {
						Projectile.netUpdate = true;
					}
				}
			}

			Timer++;
			Projectile.timeLeft = 10;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
			Projectile.Center = CorePosition(player);

			Projectile.direction = dir.X >= 0f ? 1 : -1;
			player.ChangeDir(Projectile.direction);
			player.heldProj = Projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;
			player.itemRotation = (dir * Projectile.direction).ToRotation();

			length = MeasureLength(Projectile.Center, dir);

			if (Timer % 20 == 1) {
				SoundEngine.PlaySound(SoundID.Item15 with { Pitch = 0.3f, Volume = 0.7f }, Projectile.Center);
			}

			// Sparks where it hits.
			Vector2 end = Projectile.Center + dir * length;
			for (int i = 0; i < 2; i++) {
				Dust dust = Dust.NewDustPerfect(end, Broadcast ? DustID.PinkFairy : DustID.Electric, -dir.RotatedByRandom(1f) * Main.rand.NextFloat(2f, 6f), Scale: 1.2f);
				dust.noGravity = true;
			}
			for (float d = 0; d < length; d += 48f) {
				Lighting.AddLight(Projectile.Center + dir * d, Mid.ToVector3() * 0.8f);
			}
		}

		// Walks along the beam in 8 px steps until it reaches a solid block.
		private static float MeasureLength(Vector2 start, Vector2 dir) {
			for (float d = 0f; d < MaxLength; d += 8f) {
				Vector2 p = start + dir * d;
				int x = (int)(p.X / 16f);
				int y = (int)(p.Y / 16f);
				if (!WorldGen.InWorld(x, y, 2) || WorldGen.SolidTile(x, y)) {
					return d;
				}
			}
			return MaxLength;
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
				Projectile.Center + dir * length, BeamWidth, ref collisionPoint);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			target.AddBuff(BuffID.Electrified, 120);
		}

		private static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Texture2D beam = beamTex.Value;
			Texture2D glow = glowTex.Value;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			Vector2 start = Projectile.Center - Main.screenPosition;
			Vector2 end = start + dir * length;
			float pulse = 1f + 0.12f * (float)System.Math.Sin(Timer * 0.7f);
			float rotation = dir.ToRotation() - MathHelper.PiOver2;
			Vector2 origin = new Vector2(beam.Width / 2f, 0f);

			if (length > 1f) {
				foreach ((float width, Color color) in new[] {
					(BeamWidth * 2.2f, Glow(Outer, 0.45f)),
					(BeamWidth * 1.2f, Glow(Mid, 0.85f)),
					(BeamWidth * 0.5f, Glow(Color.White, 1f)),
				}) {
					Main.EntitySpriteDraw(beam, start, null, color, rotation, origin, new Vector2(width * pulse / beam.Width, length / beam.Height), SpriteEffects.None, 0);
				}
			}
			Main.EntitySpriteDraw(glow, start, null, Glow(Mid, 0.9f), 0f, glow.Size() / 2f, 0.8f * pulse, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, start, null, Glow(Color.White, 1f), 0f, glow.Size() / 2f, 0.35f * pulse, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, end, null, Glow(Mid, 0.9f), 0f, glow.Size() / 2f, 1f * pulse, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, end, null, Glow(Color.White, 1f), 0f, glow.Size() / 2f, 0.45f * pulse, SpriteEffects.None, 0);
			return false;
		}
	}
}
