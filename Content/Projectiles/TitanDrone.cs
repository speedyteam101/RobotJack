using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// A small drone that hovers around its Titan for 15 seconds and shoots the nearest enemy it can see.
	// ai[0] = style: 0 = speaker drone (sound waves), 1 = camera drone (plasma bolts), 2 = TV drone (static orbs).
	// ai[1] = which drone of the pair (0 or 1), so they orbit on opposite sides.
	// The texture has one 32x32 frame per style, stacked vertically.
	public class TitanDrone : ModProjectile
	{
		public const int Lifetime = 900;
		public const int FireEvery = 40;
		public const float Range = 650f;

		private int Style => (int)Projectile.ai[0];
		private ref float Timer => ref Projectile.localAI[0];

		private RobotFormType FormForStyle => Style switch {
			0 => RobotFormType.TitanSpeaker,
			1 => RobotFormType.TitanCamera,
			_ => RobotFormType.TitanTV,
		};

		private Color GlowColor => Style switch {
			0 => new Color(255, 70, 70),
			1 => new Color(70, 180, 255),
			_ => new Color(200, 90, 255),
		};

		public override void SetStaticDefaults() {
			Main.projFrames[Type] = 3;
		}

		public override void SetDefaults() {
			Projectile.width = 26;
			Projectile.height = 26;
			Projectile.friendly = false; // the drone itself doesn't hit anything; its shots do
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead || player.GetModPlayer<RobotJackPlayer>().ActiveForm != FormForStyle) {
				Projectile.Kill();
				return;
			}
			Timer++;

			// Orbit above the player's shoulders, bobbing.
			float angle = Timer * 0.03f + Projectile.ai[1] * MathHelper.Pi;
			Vector2 home = player.Center + new Vector2((float)System.Math.Cos(angle) * 70f, -60f + (float)System.Math.Sin(angle * 2f) * 10f);
			Projectile.velocity = (home - Projectile.Center) * 0.15f;
			Projectile.rotation = Projectile.velocity.X * 0.03f;
			Projectile.frame = System.Math.Clamp(Style, 0, 2);

			// Shoot: the owner decides, the new projectile is synced like any other.
			if (Projectile.owner == Main.myPlayer && (Timer + Projectile.ai[1] * FireEvery / 2) % FireEvery == 0) {
				NPC target = FindTarget();
				if (target != null) {
					Shoot(target);
				}
			}

			if (Main.rand.NextBool(4)) {
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Torch, new Vector2(0f, 2f), Scale: 0.9f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, GlowColor.ToVector3() * 0.4f);
		}

		private NPC FindTarget() {
			NPC best = null;
			float bestDist = Range;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.CanBeChasedBy(Projectile)) {
					continue;
				}
				float dist = Vector2.Distance(npc.Center, Projectile.Center);
				if (dist < bestDist && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1)) {
					bestDist = dist;
					best = npc;
				}
			}
			return best;
		}

		private void Shoot(NPC target) {
			Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
			var source = Projectile.GetSource_FromThis();
			switch (Style) {
				case 0:
					Projectile.NewProjectile(source, Projectile.Center, dir * 12f, ModContent.ProjectileType<SoundWave>(), Projectile.damage, 5f, Projectile.owner, ai0: 80f, ai1: SoundWave.StyleSpeaker);
					SoundEngine.PlaySound(SoundID.Item38 with { Pitch = 0.8f, Volume = 0.4f }, Projectile.Center);
					break;
				case 1:
					Projectile.NewProjectile(source, Projectile.Center, dir * 16f, ModContent.ProjectileType<PlasmaBolt>(), Projectile.damage, 2f, Projectile.owner);
					SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.4f }, Projectile.Center);
					break;
				default:
					Projectile.NewProjectile(source, Projectile.Center, dir * 10f, ModContent.ProjectileType<EnergyOrb>(), Projectile.damage, 2f, Projectile.owner, ai0: 1f);
					SoundEngine.PlaySound(SoundID.Item93 with { Volume = 0.4f }, Projectile.Center);
					break;
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = TextureAssets.Projectile[Type].Value;
			int frameHeight = tex.Height / Main.projFrames[Type];
			Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, tex.Width, frameHeight);
			Vector2 pos = Projectile.Center - Main.screenPosition;
			Main.EntitySpriteDraw(tex, pos, frame, lightColor, Projectile.rotation, frame.Size() / 2f, 1f, SpriteEffects.None, 0);
			// Full-bright eye.
			Texture2D glow = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow").Value;
			Main.EntitySpriteDraw(glow, pos, null, new Color(GlowColor.R, GlowColor.G, GlowColor.B, 0) * 0.8f, 0f, glow.Size() / 2f, 0.25f, SpriteEffects.None, 0);
			return false;
		}
	}
}
