using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// God Jack's entrance (visual only). Spawned at the player's feet when they use the God Trigger:
	//   0-20   a pillar of heavenly light slams down and a marble-and-gold gate fades in on a bed of clouds
	//   20-36  the closed doors glow at the seams
	//   36-66  the doors swing open and light floods out, with god rays turning behind the gate
	//   66     the player (hidden and frozen until now, see RobotJackPlayer) appears in the doorway as God Jack
	//   66-100 they walk out of the light
	//   100-150 the gate dissolves into golden sparkles
	// The frame texture is 96x144 (opening 64x104 at 16,32); each door is 32x104.
	public class HeavenlyGate : ModProjectile
	{
		public const int FadeInEnd = 20;
		public const int DoorsOpenStart = 36;
		public const int DoorsOpenEnd = 66;
		public const int AppearTick = 66;
		public const int StepOutEndTick = 100;
		public const int FadeOutStart = 100;
		public const int Lifetime = 150;

		private static readonly Color Gold = RobotFormTypeExtensions.GodGold;
		private static readonly Color Holy = new Color(255, 250, 220);

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
		}

		public override void SetDefaults() {
			Projectile.width = 96;
			Projectile.height = 144;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		// The gate stands on the ground where it was opened: Projectile.Center is the bottom-middle of the gate.
		public override void AI() {
			Timer++;
			Player player = Main.player[Projectile.owner];
			if (Timer == 1) {
				player.GetModPlayer<RobotJackPlayer>().StartGodEntrance();
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.6f, Volume = 1.2f }, Projectile.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, 8f, 6f, 25, 1500f, FullName));
				// Clouds rolling out at the base.
				for (int i = 0; i < 40; i++) {
					Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-70f, 70f), -4f), DustID.Cloud,
						new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-1.5f, 0f)), 80, default, Main.rand.NextFloat(1.5f, 2.5f));
				}
			}
			if (Timer == DoorsOpenStart) {
				SoundEngine.PlaySound(SoundID.Item4 with { Pitch = -0.3f, Volume = 1.2f }, Projectile.Center);
			}
			if (Timer == AppearTick) {
				SoundEngine.PlaySound(SoundID.Item9 with { Pitch = -0.4f, Volume = 1.3f }, Projectile.Center);
				for (int i = 0; i < 50; i++) {
					Dust dust = Dust.NewDustPerfect(Projectile.Center + new Vector2(0f, -50f), DustID.Enchanted_Gold, Main.rand.NextVector2Circular(8f, 8f), Scale: 1.5f);
					dust.noGravity = true;
				}
			}
			// Light pouring out of the open doorway.
			if (Timer > DoorsOpenStart && Timer < FadeOutStart && Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-30f, 30f), -Main.rand.NextFloat(10f, 100f)),
					DustID.Enchanted_Gold, new Vector2(player.direction * Main.rand.NextFloat(1f, 3f), Main.rand.NextFloat(-1f, 1f)), Scale: 1.1f);
				dust.noGravity = true;
			}
			// Dissolving upward into sparkles.
			if (Timer > FadeOutStart) {
				for (int i = 0; i < 3; i++) {
					Dust dust = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-48f, 48f), -Main.rand.NextFloat(0f, 144f)),
						DustID.Enchanted_Gold, new Vector2(0f, -Main.rand.NextFloat(1f, 3f)), Scale: 1.2f);
					dust.noGravity = true;
				}
			}
			float bright = GateOpacity * (0.6f + DoorOpen);
			Lighting.AddLight(Projectile.Center - new Vector2(0f, 70f), 1.6f * bright, 1.4f * bright, 0.8f * bright);
		}

		private float GateOpacity {
			get {
				if (Timer < FadeInEnd) {
					return Timer / FadeInEnd;
				}
				if (Timer > FadeOutStart) {
					return 1f - (Timer - FadeOutStart) / (Lifetime - FadeOutStart);
				}
				return 1f;
			}
		}

		// 0 = closed, 1 = fully open.
		private float DoorOpen {
			get {
				float t = MathHelper.Clamp((Timer - DoorsOpenStart) / (DoorsOpenEnd - DoorsOpenStart), 0f, 1f);
				return t * t * (3f - 2f * t);
			}
		}

		private static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Texture2D frame = TextureAssets.Projectile[Type].Value;
			Texture2D door = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/HeavenlyGateDoor").Value;
			Texture2D glow = ElementFX.Glow;
			Vector2 bottom = Projectile.Center - Main.screenPosition;
			Vector2 topLeft = bottom - new Vector2(frame.Width / 2f, frame.Height);
			Vector2 opening = topLeft + new Vector2(frame.Width / 2f, 32f + 52f); // middle of the doorway
			float opacity = GateOpacity;
			float open = DoorOpen;

			// Pillar of heavenly light from the sky, strongest as the gate arrives.
			float pillar = Timer < 30 ? MathHelper.Clamp(Timer / 6f, 0f, 1f) : MathHelper.Clamp(1f - (Timer - 30f) / 40f, 0f, 1f);
			if (pillar > 0f) {
				ElementFX.Line(new Vector2(bottom.X, -40f), bottom, 150f * pillar, Glow(Gold, 0.35f * pillar));
				ElementFX.Line(new Vector2(bottom.X, -40f), bottom, 60f * pillar, Glow(Holy, 0.7f * pillar));
			}

			// God rays turning slowly behind the gate once it opens.
			if (open > 0f) {
				for (int i = 0; i < 12; i++) {
					float angle = Timer * 0.01f + MathHelper.TwoPi * i / 12f;
					Vector2 end = opening + angle.ToRotationVector2() * 260f;
					ElementFX.Line(opening, end, 22f, Glow(Gold, 0.25f * open * opacity));
				}
			}
			// Halo of light around the whole gate.
			Main.EntitySpriteDraw(glow, opening, null, Glow(Gold, 0.5f * opacity * (0.4f + open)), 0f, glow.Size() / 2f, new Vector2(3.2f, 3.6f), SpriteEffects.None, 0);

			// Blinding light filling the doorway as the doors part.
			Main.EntitySpriteDraw(glow, opening, null, Glow(Holy, opacity * (0.15f + open)), 0f, glow.Size() / 2f, new Vector2(1.1f, 1.7f) * (0.6f + open * 0.6f), SpriteEffects.None, 0);

			// The gate itself (full-bright: it's made of light).
			Main.EntitySpriteDraw(frame, topLeft, null, Color.White * opacity, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

			// Doors: each swings open by narrowing toward its hinge on the pillar.
			float doorScaleX = MathHelper.Lerp(1f, 0.08f, open);
			Color doorColor = Color.Lerp(Color.White, Holy, open) * opacity;
			Vector2 leftHinge = topLeft + new Vector2(16f, 32f);
			Vector2 rightHinge = topLeft + new Vector2(80f, 32f);
			Main.EntitySpriteDraw(door, leftHinge, null, doorColor, 0f, Vector2.Zero, new Vector2(doorScaleX, 1f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(door, rightHinge, null, doorColor, 0f, new Vector2(door.Width, 0f), new Vector2(doorScaleX, 1f), SpriteEffects.FlipHorizontally, 0);

			// Glowing seam between the closed doors.
			if (Timer > FadeInEnd && open < 0.5f) {
				float seam = MathHelper.Clamp((Timer - FadeInEnd) / (DoorsOpenStart - FadeInEnd), 0f, 1f) * (1f - open * 2f);
				ElementFX.Line(topLeft + new Vector2(48f, 32f), topLeft + new Vector2(48f, 136f), 6f, Glow(Holy, seam * opacity));
			}
			// Flash as the player appears in the doorway.
			float flash = MathHelper.Clamp(1f - System.Math.Abs(Timer - AppearTick) / 12f, 0f, 1f);
			if (flash > 0f) {
				Main.EntitySpriteDraw(glow, opening, null, Glow(Color.White, flash), 0f, glow.Size() / 2f, 2.5f, SpriteEffects.None, 0);
			}
			return false;
		}
	}
}
