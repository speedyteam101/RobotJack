using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Omega Jack's Singularity: a black hole at the cursor. For 4 seconds it drags in every enemy nearby
	// (bosses too, more slowly), swallows enemy projectiles and shreds everything near its core,
	// then collapses into five huge elemental explosions.
	public class Singularity : ModProjectile
	{
		public const int Lifetime = 240;
		public const float PullRadius = 700f;
		public const float CoreRadius = 150f;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)PullRadius + 300;
		}

		public override void SetDefaults() {
			Projectile.width = 40;
			Projectile.height = 40;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 10;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Timer++;
			float grow = MathHelper.Clamp(Timer / 30f, 0f, 1f);

			if (Timer % 30 == 1) {
				SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -1f, Volume = 1.2f }, Projectile.Center);
			}

			// Enemy positions belong to the server (or single player). Bosses are pulled at a third of the speed.
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5) {
						continue;
					}
					Vector2 toCore = Projectile.Center - npc.Center;
					float dist = toCore.Length();
					if (dist > PullRadius || dist < 12f) {
						continue;
					}
					float speed = MathHelper.Lerp(12f, 3f, dist / PullRadius) * grow * (npc.boss ? 0.33f : 1f);
					Vector2 move = toCore / dist * System.Math.Min(speed, dist);
					if (!npc.noTileCollide) {
						move = Collision.TileCollision(npc.position, move, npc.width, npc.height, true, true);
					}
					npc.position += move;
					if (Timer % 6 == 0) {
						npc.netUpdate = true;
					}
				}
			}

			// Swallow enemy projectiles (on this client).
			if (Projectile.owner == Main.myPlayer) {
				for (int i = 0; i < Main.maxProjectiles; i++) {
					Projectile proj = Main.projectile[i];
					if (proj.active && proj.hostile && Vector2.Distance(proj.Center, Projectile.Center) < CoreRadius * 1.5f) {
						proj.active = false;
					}
				}
			}

			// Matter spiralling in from every direction.
			for (int i = 0; i < 5; i++) {
				Vector2 offset = Main.rand.NextVector2CircularEdge(PullRadius, PullRadius) * Main.rand.NextFloat(0.2f, 1f) * grow;
				Vector2 vel = -offset * 0.06f + offset.RotatedBy(MathHelper.PiOver2) * 0.04f;
				Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, Elements.Dust((JackElement)Main.rand.Next(Elements.Count)), vel, Scale: 1.2f);
				dust.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.6f, 0.3f, 0.9f);

			if (Timer % 20 == 0) {
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 3f, 12f, 20, 1600f, FullName));
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => ElementFX.CircleHits(Projectile.Center, CoreRadius, targetHitbox);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, (JackElement)Main.rand.Next(Elements.Count));
		}

		// Collapse: five huge elemental explosions, one after another, largest first.
		public override void OnKill(int timeLeft) {
			SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -0.8f, Volume = 1.6f }, Projectile.Center);
			SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.6f }, Projectile.Center);
			if (Projectile.owner != Main.myPlayer) {
				return;
			}
			for (int i = 0; i < Elements.Count; i++) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ElementBlast>(),
					Projectile.damage * 6, 14f, Projectile.owner, ai0: i, ai1: 650f - i * 90f, ai2: ElementBlast.Huge);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D glow = ElementFX.Glow;
			Texture2D ring = ElementFX.Ring;
			float grow = MathHelper.Clamp(Timer / 30f, 0f, 1f);
			float fade = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
			float pulse = 1f + 0.06f * (float)System.Math.Sin(Timer * 0.3f);

			// Accretion rings spiralling inward, in every element's colour.
			for (int i = 0; i < Elements.Count; i++) {
				float t = (Timer / 60f + i / (float)Elements.Count) % 1f;
				float scale = MathHelper.Lerp(PullRadius * 2f, CoreRadius * 0.8f, t) / ring.Width * grow;
				Main.EntitySpriteDraw(ring, center, null, ElementFX.Additive(Elements.Main((JackElement)i), 0.6f * t * fade), Timer * 0.05f * (i % 2 == 0 ? 1 : -1),
					ring.Size() / 2f, new Vector2(scale, scale * 0.85f), SpriteEffects.None, 0);
			}
			// Glowing event horizon, then the black core drawn normally (not additively) so it's truly dark.
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Main.DiscoColor, 0.9f * fade), 0f, glow.Size() / 2f, CoreRadius * 2.6f / glow.Width * grow * pulse, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Color.Black * 0.95f * fade, 0f, glow.Size() / 2f, CoreRadius * 1.4f / glow.Width * grow, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Color.Black * fade, 0f, glow.Size() / 2f, CoreRadius * 0.9f / glow.Width * grow, SpriteEffects.None, 0);
			return false;
		}
	}
}
