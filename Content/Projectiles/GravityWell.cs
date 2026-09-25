using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Common;
using RobotJack.Content.Abilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// The Gravity Arm's well. It follows the owner's cursor while left click is held and pulls every enemy
	// in range toward it, holding them at the centre. It deals no damage.
	// Bosses aren't pulled: dragging them around breaks their attack patterns.
	public class GravityWell : ModProjectile
	{
		public const float PullRadius = 400f;    // about 25 tiles around the cursor
		public const float MaxPullSpeed = 9f;    // pixels per tick
		public const float HoldRadius = 20f;     // enemies this close are held still

		private static readonly Color WellColor = new Color(170, 80, 255);
		private static readonly Color CoreColor = new Color(110, 255, 190);

		private static Asset<Texture2D> ringTex, glowTex, beamTex;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400; // the arm beam can stretch across the screen
			if (!Main.dedServ) {
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
				beamTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalBeam");
			}
		}

		public override void Unload() {
			ringTex = glowTex = beamTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 10;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public static bool CanPull(NPC npc) {
			return npc.active && !npc.friendly && !npc.boss && !npc.dontTakeDamage && npc.lifeMax > 5 && npc.realLife == -1;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];

			if (Projectile.owner == Main.myPlayer) {
				bool holding = player.channel && !player.noItems && !player.CCed && !player.dead
					&& player.HeldItem.type == ModContent.ItemType<GravityArm>() && player.GetModPlayer<RobotJackPlayer>().Transformed;
				if (!holding) {
					Projectile.Kill();
					return;
				}

				// Follow the cursor; tell other clients and the server when it moves.
				Vector2 mouse = Main.MouseWorld;
				if (Vector2.Distance(mouse, Projectile.Center) > 2f) {
					Projectile.Center = mouse;
					if (Timer % 4 == 0) {
						Projectile.netUpdate = true;
					}
				}
			}

			Timer++;
			Projectile.timeLeft = 10;

			// Aim the robot's arm at the well.
			Vector2 aim = (Projectile.Center - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
			Projectile.direction = aim.X >= 0f ? 1 : -1;
			player.ChangeDir(Projectile.direction);
			player.heldProj = Projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;
			player.itemRotation = (aim * Projectile.direction).ToRotation();

			if (Timer % 30 == 1) {
				SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -0.9f, Volume = 0.5f }, Projectile.Center);
			}

			// Enemy positions belong to the server (or single player), so only it moves them.
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				PullNPCs();
			}
			Effects(player);
		}

		private void PullNPCs() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!CanPull(npc)) {
					continue;
				}
				Vector2 toWell = Projectile.Center - npc.Center;
				float dist = toWell.Length();
				if (dist > PullRadius) {
					continue;
				}

				if (dist <= HoldRadius) {
					// Held in the middle of the well.
					npc.velocity = Vector2.Zero;
				}
				else {
					// Faster the further away they are, but never overshoot the centre.
					float speed = MathHelper.Min(MathHelper.Lerp(4f, MaxPullSpeed, dist / PullRadius), dist - HoldRadius * 0.5f);
					Vector2 move = toWell / dist * speed;
					if (!npc.noTileCollide) {
						move = Collision.TileCollision(npc.position, move, npc.width, npc.height, true, true);
					}
					npc.position += move;
					npc.velocity = move; // also cancels gravity and the enemy's own movement while held
				}
				if (Timer % 6 == 0) {
					npc.netUpdate = true;
				}
			}
		}

		private void Effects(Player player) {
			// Particles spiralling into the well.
			for (int i = 0; i < 3; i++) {
				Vector2 offset = Main.rand.NextVector2CircularEdge(PullRadius, PullRadius) * Main.rand.NextFloat(0.3f, 1f);
				Vector2 vel = -offset * 0.05f + offset.RotatedBy(MathHelper.PiOver2) * 0.025f;
				Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Electric, vel, Scale: 0.9f);
				dust.noGravity = true;
			}
			// Sparks along the arm beam.
			if (Main.rand.NextBool(2)) {
				Vector2 pos = Vector2.Lerp(player.MountedCenter, Projectile.Center, Main.rand.NextFloat());
				Dust.NewDustPerfect(pos, DustID.Electric, Main.rand.NextVector2Circular(1f, 1f), Scale: 0.7f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.6f, 0.3f, 1f);
		}

		// ------------------------------------------------------------------ drawing

		private static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Player player = Main.player[Projectile.owner];
			Vector2 center = Projectile.Center - Main.screenPosition;
			Vector2 arm = player.MountedCenter + (Projectile.Center - player.MountedCenter).SafeNormalize(Vector2.Zero) * 20f - Main.screenPosition;
			Texture2D ring = ringTex.Value;
			Texture2D glow = glowTex.Value;
			Texture2D beam = beamTex.Value;
			float appear = MathHelper.Clamp(Timer / 12f, 0f, 1f);
			float pulse = 1f + 0.1f * (float)System.Math.Sin(Timer * 0.25f);

			// Beam from the robot's arm to the well.
			DrawLine(beam, arm, center, 10f * pulse, Glow(WellColor, 0.6f));
			DrawLine(beam, arm, center, 4f, Glow(CoreColor, 0.9f));

			// Tethers to every enemy being pulled.
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (CanPull(npc) && Vector2.Distance(npc.Center, Projectile.Center) <= PullRadius) {
					DrawLine(beam, center, npc.Center - Main.screenPosition, 3f, Glow(WellColor, 0.5f));
				}
			}

			// Range boundary, then rings falling inward like a vortex.
			Main.EntitySpriteDraw(ring, center, null, Glow(WellColor, 0.2f), Timer * 0.01f, ring.Size() / 2f, PullRadius * 2f / ring.Width * appear, SpriteEffects.None, 0);
			for (int i = 0; i < 4; i++) {
				float t = (Timer / 50f + i / 4f) % 1f;
				float scale = MathHelper.Lerp(PullRadius * 2f, 20f, t) / ring.Width * appear;
				Main.EntitySpriteDraw(ring, center, null, Glow(i % 2 == 0 ? WellColor : CoreColor, 0.55f * t), Timer * 0.04f * (i % 2 == 0 ? 1 : -1), ring.Size() / 2f, scale, SpriteEffects.None, 0);
			}

			// Dark-edged glowing core.
			Main.EntitySpriteDraw(glow, center, null, Glow(WellColor, 0.9f), 0f, glow.Size() / 2f, 1.3f * pulse * appear, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Glow(CoreColor, 0.9f), 0f, glow.Size() / 2f, 0.6f * pulse * appear, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Glow(Color.White, 1f), 0f, glow.Size() / 2f, 0.25f * pulse * appear, SpriteEffects.None, 0);
			return false;
		}

		private static void DrawLine(Texture2D beam, Vector2 from, Vector2 to, float width, Color color) {
			Vector2 diff = to - from;
			float length = diff.Length();
			if (length < 1f) {
				return;
			}
			// The beam texture is a horizontal cross-section, so rotate it to run along the line.
			float rotation = diff.ToRotation() - MathHelper.PiOver2;
			Main.EntitySpriteDraw(beam, from, null, color, rotation, new Vector2(beam.Width / 2f, 0f),
				new Vector2(width / beam.Width, length / beam.Height), SpriteEffects.None, 0);
		}
	}
}
