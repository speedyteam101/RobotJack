using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using RobotJack.Content.Abilities;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Gods Wrath, part 1: charging. Stays on the player while left click is held.
	// The camera slowly zooms out, the player rises and starts to glow, the air around them ignites into a
	// growing mini sun, and a giant golden ring appears around at least half the world.
	// Release once it's fully charged (5 seconds) to unleash GodsWrathBlast; releasing early lets it fizzle.
	public class GodsWrathCharge : ModProjectile
	{
		public const int ChargeTicks = 300;
		public const float SunRadius = 190f;

		private static readonly Color Gold = RobotFormTypeExtensions.GodGold;
		private static readonly Color SunCore = new Color(255, 250, 225);
		private static readonly Color SunEdge = new Color(255, 140, 30);

		private ref float Timer => ref Projectile.ai[0];

		public float Charge => MathHelper.Clamp(Timer / ChargeTicks, 0f, 1f);

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 60000; // the ring is enormous
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

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();

			if (Projectile.owner == Main.myPlayer) {
				bool holding = player.channel && !player.noItems && !player.CCed && !player.dead
					&& player.HeldItem.type == ModContent.ItemType<GodsWrathAbility>() && modPlayer.ActiveForm == RobotFormType.GodJack;
				if (!holding) {
					if (Charge >= 1f && !player.dead) {
						Release(player, modPlayer);
					}
					else {
						Fizzle();
					}
					Projectile.Kill();
					return;
				}
				GodsWrathSystem.SetCharge(player.Center, Charge);
			}

			Timer++;
			Projectile.timeLeft = 10;
			Projectile.Center = player.Center;
			// Glow, and rise slowly then hang in the air (RobotJackPlayer applies both).
			modPlayer.wrathGlow = Charge;
			modPlayer.wrathChargeTicks = (int)Timer;
			player.heldProj = Projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;

			if (Timer % 40 == 1 && Charge < 1f) {
				SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -1f + Charge, Volume = 0.6f + Charge }, Projectile.Center);
			}
			if (Timer == ChargeTicks) {
				// Fully charged.
				SoundEngine.PlaySound(SoundID.Item29 with { Pitch = -0.3f, Volume = 1.5f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item4, Projectile.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitY, 6f, 10f, 30, 3000f, FullName));
			}
			if (Timer % 30 == 0) {
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 1.5f + Charge * 3f, 12f, 20, 3000f, FullName));
			}

			// The air igniting: embers rushing in and flares licking off the sun.
			float radius = SunRadius * Charge;
			for (int i = 0; i < 4; i++) {
				Vector2 offset = Main.rand.NextVector2CircularEdge(1f, 1f) * (radius + 200f);
				Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, i % 2 == 0 ? DustID.Enchanted_Gold : DustID.Torch, -offset * 0.04f, Scale: 1.4f);
				dust.noGravity = true;
			}
			if (Charge > 0.3f) {
				Vector2 flare = Main.rand.NextVector2CircularEdge(radius, radius);
				Dust.NewDustPerfect(Projectile.Center + flare, DustID.Torch, flare * 0.03f, Scale: 2f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 2f * Charge + 0.5f, 1.8f * Charge + 0.4f, 0.8f * Charge);
		}

		private void Release(Player player, RobotJackPlayer modPlayer) {
			Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero, ModContent.ProjectileType<GodsWrathBlast>(),
				0, 0f, player.whoAmI, ai1: GodsWrathSystem.WorldRingRadius);
			modPlayer.StartCooldown(ModContent.ItemType<GodsWrathAbility>(), GodsWrathAbility.Cooldown);
		}

		private void Fizzle() {
			SoundEngine.PlaySound(SoundID.Item8 with { Pitch = -0.8f }, Projectile.Center);
			for (int i = 0; i < 30; i++) {
				Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.6f);
			}
		}

		private static Color Additive(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D glow = ElementFX.Glow;
			Texture2D ring = ElementFX.Ring;
			float charge = Charge;
			float pulse = 1f + 0.05f * (float)System.Math.Sin(Timer * 0.35f);
			float radius = SunRadius * charge * pulse;

			DrawJudgementRing(charge);

			if (radius > 1f) {
				// The mini sun: a wide corona, a boiling orange body, a white-hot core, and slowly turning rays.
				for (int i = 0; i < 16; i++) {
					float angle = Timer * 0.006f + MathHelper.TwoPi * i / 16f;
					float length = radius * (1.8f + 0.4f * (float)System.Math.Sin(Timer * 0.1f + i));
					ElementFX.Line(center, center + angle.ToRotationVector2() * length, radius * 0.18f, Additive(Gold, 0.35f * charge));
				}
				Main.EntitySpriteDraw(glow, center, null, Additive(SunEdge, 0.45f), 0f, glow.Size() / 2f, radius * 4.2f / glow.Width, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(glow, center, null, Additive(Gold, 0.8f), 0f, glow.Size() / 2f, radius * 2.6f / glow.Width, SpriteEffects.None, 0);
				Main.EntitySpriteDraw(glow, center, null, Additive(SunCore, 0.9f), 0f, glow.Size() / 2f, radius * 1.6f / glow.Width, SpriteEffects.None, 0);
				// Surface shimmer.
				Main.EntitySpriteDraw(ring, center, null, Additive(SunEdge, 0.5f * charge), Timer * 0.02f, ring.Size() / 2f, radius * 2f / ring.Width, SpriteEffects.None, 0);
			}
			if (charge >= 1f && Timer % 30 < 15) {
				// Fully charged: the sun pulses white.
				Main.EntitySpriteDraw(glow, center, null, Additive(Color.White, 0.6f), 0f, glow.Size() / 2f, radius * 2f / glow.Width, SpriteEffects.None, 0);
			}
			return false;
		}

		// The ring of judgement around (at least) half the world. It's far bigger than the screen, so only the
		// segments near the screen are drawn. It burns in brighter as the charge builds.
		private void DrawJudgementRing(float charge) {
			float radius = GodsWrathSystem.WorldRingRadius;
			Vector2 center = Projectile.Center;
			Rectangle view = new Rectangle((int)Main.screenPosition.X - 600, (int)Main.screenPosition.Y - 600, Main.screenWidth + 1200, Main.screenHeight + 1200);
			int segments = (int)(MathHelper.TwoPi * radius / 48f);
			float width = 24f + 24f * charge;
			Color outer = Additive(Gold, 0.5f * charge);
			Color inner = Additive(Color.White, 0.8f * charge);
			Vector2 previous = center + new Vector2(radius, 0f);
			for (int i = 1; i <= segments; i++) {
				Vector2 point = center + (MathHelper.TwoPi * i / segments).ToRotationVector2() * radius;
				if (view.Contains(point.ToPoint()) || view.Contains(previous.ToPoint())) {
					ElementFX.Line(previous - Main.screenPosition, point - Main.screenPosition, width * 2.5f, outer);
					ElementFX.Line(previous - Main.screenPosition, point - Main.screenPosition, width * 0.5f, inner);
				}
				previous = point;
			}
		}
	}

	// Gods Wrath, part 2: the release. A blinding flash, then a golden shockwave races out from where the player
	// stood to the edge of the ring. Everything it passes is killed outright (bosses included), except players,
	// town NPCs (the Guide and friends), critters and target dummies. Enemy projectiles inside are wiped out too.
	// ai[1] = ring radius.
	public class GodsWrathBlast : ModProjectile
	{
		public const int ExpandTicks = 150;
		public const int Lifetime = 200;

		private static readonly Color Gold = RobotFormTypeExtensions.GodGold;

		private ref float Timer => ref Projectile.ai[0];
		private float RingRadius => Projectile.ai[1] <= 0f ? 30000f : Projectile.ai[1];

		private float WaveRadius {
			get {
				float t = MathHelper.Clamp(Timer / ExpandTicks, 0f, 1f);
				return RingRadius * (1f - (1f - t) * (1f - t) * (1f - t));
			}
		}

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 60000;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
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

		// Who survives the wrath.
		public static bool IsSpared(NPC npc) => npc.friendly || npc.townNPC || npc.immortal || npc.lifeMax <= 5; // critters have 5 life

		public override void AI() {
			Timer++;
			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -1f, Volume = 2f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item62 with { Pitch = -0.8f, Volume = 2f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.9f, Volume = 1.5f }, Projectile.Center);
				SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.5f }, Projectile.Center);
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 30f, 6f, 90, -1f, FullName));
				for (int i = 0; i < 200; i++) {
					Vector2 vel = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(8f, 40f);
					Dust dust = Dust.NewDustPerfect(Projectile.Center, i % 2 == 0 ? DustID.Enchanted_Gold : DustID.Torch, vel, Scale: Main.rand.NextFloat(1.5f, 3f));
					dust.noGravity = true;
				}
			}

			float wave = WaveRadius;
			// Judgement is passed by the server (or in single player), then synced to everyone.
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (npc.active && !IsSpared(npc) && Vector2.Distance(npc.Center, Projectile.Center) <= wave) {
						for (int d = 0; d < 20; d++) {
							Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Enchanted_Gold, 0f, -4f, Scale: 1.6f).noGravity = true;
						}
						npc.StrikeInstantKill();
					}
				}
				for (int i = 0; i < Main.maxProjectiles; i++) {
					Projectile proj = Main.projectile[i];
					if (proj.active && proj.hostile && Vector2.Distance(proj.Center, Projectile.Center) <= wave) {
						proj.Kill();
					}
				}
			}
			Lighting.AddLight(Projectile.Center, 3f, 2.6f, 1.2f);
		}

		private static Color Additive(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Texture2D glow = ElementFX.Glow;
			Texture2D ring = ElementFX.Ring;
			Vector2 center = Projectile.Center - Main.screenPosition;
			float fade = MathHelper.Clamp((Lifetime - Timer) / 50f, 0f, 1f);

			// Whiteout: the whole screen flashes, then clears.
			float flash = MathHelper.Clamp(1f - Timer / 45f, 0f, 1f);
			if (flash > 0f) {
				Rectangle screen = new Rectangle(-400, -400, Main.screenWidth + 800, Main.screenHeight + 800);
				Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, screen, new Color(255, 250, 225) * flash);
			}

			// The sun going nova at the centre.
			float nova = MathHelper.Clamp(1f - Timer / 90f, 0f, 1f);
			Main.EntitySpriteDraw(glow, center, null, Additive(Gold, nova), 0f, glow.Size() / 2f, 14f * (1f - nova * 0.5f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Additive(Color.White, nova), 0f, glow.Size() / 2f, 6f, SpriteEffects.None, 0);

			// The expanding shockwave: drawn as segments near the screen, like the ring of judgement.
			float wave = WaveRadius;
			if (Timer <= ExpandTicks + 20 && wave > 10f) {
				if (wave < 1500f) {
					Main.EntitySpriteDraw(ring, center, null, Additive(Gold, fade), 0f, ring.Size() / 2f, wave * 2f / ring.Width, SpriteEffects.None, 0);
				}
				else {
					Rectangle view = new Rectangle((int)Main.screenPosition.X - 600, (int)Main.screenPosition.Y - 600, Main.screenWidth + 1200, Main.screenHeight + 1200);
					int segments = (int)(MathHelper.TwoPi * wave / 48f);
					Vector2 previous = Projectile.Center + new Vector2(wave, 0f);
					for (int i = 1; i <= segments; i++) {
						Vector2 point = Projectile.Center + (MathHelper.TwoPi * i / segments).ToRotationVector2() * wave;
						if (view.Contains(point.ToPoint()) || view.Contains(previous.ToPoint())) {
							ElementFX.Line(previous - Main.screenPosition, point - Main.screenPosition, 160f, Additive(Gold, 0.6f * fade));
							ElementFX.Line(previous - Main.screenPosition, point - Main.screenPosition, 50f, Additive(Color.White, fade));
						}
						previous = point;
					}
				}
			}
			return false;
		}
	}
}
