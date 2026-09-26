using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Common.Titan;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// A huge sweep of the Shift Titan's blade or claw arm, from above down through the aim direction.
	// ai[0] = element, ai[1] = 0 blade (one wide arc) / 1 claw (three raking arcs), ai[2] = facing direction.
	// velocity = aim direction (the projectile doesn't move; it stays on the titan's shoulder).
	public class TitanSlash : ModProjectile
	{
		private const int SweepTicks = 10;
		private const int Lifetime = 18;
		private const float HalfArc = 1.3f;

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);
		private bool Claw => Projectile.ai[1] == 1f;
		private int Dir => Projectile.ai[2] < 0f ? -1 : 1;
		private ref float Timer => ref Projectile.localAI[0];

		private Player Owner => Main.player[Projectile.owner];
		private float Reach => 75f * TitanRenderer.ScaleFor(Owner, false);

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		// Angle of the sweep at progress t (0..1).
		private float AngleAt(float t) {
			float aim = Projectile.velocity.ToRotation();
			return aim + (-HalfArc + 2f * HalfArc * MathHelper.Clamp(t, 0f, 1f)) * Dir;
		}

		public override void AI() {
			Timer++;
			Projectile.Center = TitanRenderer.ShoulderWorld(Owner);
			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item71 with { Pitch = Claw ? 0.2f : -0.4f, Volume = 1.1f }, Projectile.Center);
			}
			if (Timer <= SweepTicks) {
				Vector2 tip = Projectile.Center + AngleAt(Timer / SweepTicks).ToRotationVector2() * Reach;
				ElementFX.Burst(tip, Element, 3, 4f, 1.6f);
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(Element).ToVector3());
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Timer > SweepTicks + 1) {
				return false;
			}
			// Check a few angles between last tick's and this tick's, so nothing slips between them.
			for (int i = 0; i <= 3; i++) {
				float t = (Timer - 1f + i / 3f) / SweepTicks;
				Vector2 end = Projectile.Center + AngleAt(t).ToRotationVector2() * Reach;
				float point = 0f;
				if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Claw ? 60f : 44f, ref point)) {
					return true;
				}
			}
			return false;
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element, Element == JackElement.Frost);
			ElementFX.Burst(target.Center, Element, 10, 6f, 1.4f);
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			float progress = Timer / SweepTicks;
			float fade = MathHelper.Clamp((Lifetime - Timer) / 8f, 0f, 1f);
			Color main = Elements.Main(Element);
			Color core = Elements.Core(Element);
			// The trail behind the swing: a fan of glowing streaks that fades out toward its tail.
			const int samples = 14;
			for (int i = 0; i < samples; i++) {
				float t = progress - 0.5f * i / samples;
				if (t < 0f) {
					break;
				}
				float tail = 1f - i / (float)samples;
				Vector2 dir = AngleAt(t).ToRotationVector2();
				if (Claw) {
					foreach (float r in new[] { 0.72f, 0.86f, 1f }) {
						ElementFX.Line(center + dir * Reach * (r - 0.18f), center + dir * Reach * r, 14f, ElementFX.Additive(main, 0.7f * tail * fade));
					}
				}
				else {
					ElementFX.Line(center + dir * Reach * 0.35f, center + dir * Reach, 30f * tail, ElementFX.Additive(main, 0.45f * tail * fade));
					ElementFX.Line(center + dir * Reach * 0.6f, center + dir * Reach, 12f * tail, ElementFX.Additive(core, 0.8f * tail * fade));
				}
			}
			return false;
		}
	}

	// Invisible hitbox on the front of the Shift Titan's car. TitanPlayer keeps one alive while the car drives fast;
	// running into enemies deals damage and throws them away from the car.
	public class TitanCarRam : ModProjectile
	{
		public const float MinSpeed = 5f;

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);

		public override void SetDefaults() {
			Projectile.width = 150;
			Projectile.height = 150;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 10;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 30;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public static bool CanRam(Player player) {
			TitanPlayer titan = player.GetModPlayer<TitanPlayer>();
			return player.active && !player.dead && titan.carMode && titan.IsFullSize && System.Math.Abs(player.velocity.X) >= MinSpeed;
		}

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (Projectile.owner == Main.myPlayer) {
				if (!CanRam(player)) {
					Projectile.Kill();
					return;
				}
				Projectile.timeLeft = 10;
			}
			int dir = player.velocity.X >= 0f ? 1 : -1;
			Projectile.Center = new Vector2(player.Center.X + dir * (player.width / 2f - 40f), player.Center.Y);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.HitDirectionOverride = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element);
			SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -0.5f }, target.Center);
			ElementFX.Burst(target.Center, Element, 14, 7f, 1.4f);
		}

		public override bool PreDraw(ref Color lightColor) => false;
	}
}
