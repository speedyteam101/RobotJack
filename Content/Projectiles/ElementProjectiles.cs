using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Shared by the element projectiles below: textures, colours and the element's hit effect.
	public static class ElementFX
	{
		private const string Path = "RobotJack/Content/Projectiles/";

		public static Texture2D Glow => ModContent.Request<Texture2D>(Path + "OrbitalGlow").Value;
		public static Texture2D Ring => ModContent.Request<Texture2D>(Path + "OrbitalRing").Value;
		public static Texture2D Beam => ModContent.Request<Texture2D>(Path + "OrbitalBeam").Value;

		// Colour with alpha 0 draws additively with the normal sprite batch, so it glows.
		public static Color Additive(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		// The element's debuff; Frost also briefly freezes (stuns) non-boss enemies when freeze is set.
		public static void Hit(NPC target, JackElement element, bool freeze = false) {
			target.AddBuff(Elements.Debuff(element), Elements.DebuffTicks);
			if (freeze || (element == JackElement.Frost && Main.rand.NextBool(4))) {
				Stunned.TryApply(target, 60);
			}
		}

		public static void Burst(Vector2 at, JackElement element, int count, float speed, float scale = 1.2f) {
			for (int i = 0; i < count; i++) {
				Dust dust = Dust.NewDustPerfect(at, Elements.Dust(element), Main.rand.NextVector2Circular(speed, speed), Scale: scale);
				dust.noGravity = true;
			}
		}

		// Draws a textured line (the beam texture's cross-section stretched from one point to another).
		public static void Line(Vector2 from, Vector2 to, float width, Color color) {
			Vector2 diff = to - from;
			float length = diff.Length();
			if (length < 1f) {
				return;
			}
			Texture2D beam = Beam;
			Main.EntitySpriteDraw(beam, from, null, color, diff.ToRotation() - MathHelper.PiOver2, new Vector2(beam.Width / 2f, 0f),
				new Vector2(width / beam.Width, length / beam.Height), SpriteEffects.None, 0);
		}

		public static bool CircleHits(Vector2 center, float radius, Rectangle target) {
			float x = MathHelper.Clamp(center.X, target.Left, target.Right);
			float y = MathHelper.Clamp(center.Y, target.Top, target.Bottom);
			return Vector2.Distance(center, new Vector2(x, y)) < radius;
		}
	}

	// A glowing element bolt. ai[0] = element. ai[1] = flags (see the constants).
	// Used for fireballs, ice shards, arcs, shadow orbs, stars, meteors, hail...
	public class ElementBolt : ModProjectile
	{
		public const int Homing = 1;     // curves toward the nearest enemy
		public const int Explode = 2;    // bursts into a small ElementBlast when it hits
		public const int Pierce = 4;     // passes through up to 4 enemies
		public const int Big = 8;        // drawn bigger
		public const int Falling = 16;   // ignores blocks until it's near where it was aimed (sky rain)

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);
		private int Flags => (int)Projectile.ai[1];
		private bool Has(int flag) => (Flags & flag) != 0;
		private ref float Timer => ref Projectile.localAI[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 180;
			Projectile.aiStyle = -1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override void AI() {
			Timer++;
			if (Timer == 1 && Has(Pierce)) {
				Projectile.penetrate = 4;
			}
			// Sky rain passes through blocks for the first part of its fall, so it can come down from off screen.
			// Set every tick on every client, since flags in ai[1] are synced but tileCollide isn't.
			Projectile.tileCollide = !Has(Falling) || Timer > 40;
			if (Has(Homing) && Timer > 8) {
				NPC target = FindTarget(700f);
				if (target != null) {
					float speed = System.Math.Max(Projectile.velocity.Length(), 10f);
					Projectile.velocity = Vector2.Lerp(Projectile.velocity, (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * speed, 0.1f);
				}
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (Main.rand.NextBool(2)) {
				Dust dust = Dust.NewDustPerfect(Projectile.Center, Elements.Dust(Element), -Projectile.velocity * 0.15f, Scale: Has(Big) ? 1.4f : 1f);
				dust.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(Element).ToVector3() * 0.5f);
		}

		private NPC FindTarget(float range) {
			NPC best = null;
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				float dist = Vector2.Distance(npc.Center, Projectile.Center);
				if (npc.CanBeChasedBy(Projectile) && dist < range) {
					range = dist;
					best = npc;
				}
			}
			return best;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element);
		}

		public override void OnKill(int timeLeft) {
			ElementFX.Burst(Projectile.Center, Element, 12, 4f);
			if (Has(Explode) && Projectile.owner == Main.myPlayer) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ElementBlast>(),
					Projectile.damage / 2, 4f, Projectile.owner, ai0: (int)Element, ai1: Has(Big) ? 110f : 70f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D glow = ElementFX.Glow;
			Vector2 origin = glow.Size() / 2f;
			float size = Has(Big) ? 1.6f : 1f;
			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] == Vector2.Zero) {
					continue;
				}
				float fade = 1f - i / (float)Projectile.oldPos.Length;
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
				Main.EntitySpriteDraw(glow, pos, null, ElementFX.Additive(Elements.Main(Element), 0.5f * fade), 0f, origin, 0.3f * fade * size, SpriteEffects.None, 0);
			}
			Vector2 center = Projectile.Center - Main.screenPosition;
			// Stretched along the direction of travel so it reads as a fast bolt.
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Elements.Main(Element), 1f), Projectile.rotation, origin, new Vector2(0.6f, 0.35f) * size, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Elements.Core(Element), 1f), Projectile.rotation, origin, new Vector2(0.3f, 0.18f) * size, SpriteEffects.None, 0);
			return false;
		}
	}

	// An element explosion: an expanding ring that hits everything inside once.
	// ai[0] = element, ai[1] = radius. ai[2] = flags: 1 = stays centred on the owner, 2 = freezes (stuns) non-boss enemies,
	// 4 = big (screen shake and more sparks).
	public class ElementBlast : ModProjectile
	{
		public const int FollowOwner = 1;
		public const int Freeze = 2;
		public const int Huge = 4;

		public const int GrowTicks = 12;
		public const int Lifetime = 26;

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);
		private float MaxRadius => Projectile.ai[1] <= 0f ? 80f : Projectile.ai[1];
		private bool Has(int flag) => ((int)Projectile.ai[2] & flag) != 0;
		private ref float Timer => ref Projectile.localAI[0];

		private float Radius {
			get {
				float t = MathHelper.Clamp(Timer / GrowTicks, 0f, 1f);
				return MaxRadius * (1f - (1f - t) * (1f - t));
			}
		}

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 900;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
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

		public override void AI() {
			Timer++;
			if (Has(FollowOwner)) {
				Projectile.Center = Main.player[Projectile.owner].Center;
			}
			if (Timer == 1) {
				SoundEngine.PlaySound((Has(Huge) ? SoundID.Item14 : SoundID.Item62) with { Volume = Has(Huge) ? 1.2f : 0.6f, Pitch = Has(Huge) ? -0.4f : 0.2f }, Projectile.Center);
				ElementFX.Burst(Projectile.Center, Element, Has(Huge) ? 60 : 20, Has(Huge) ? 14f : 7f, 1.4f);
				if (Has(Huge)) {
					Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Main.rand.NextVector2Unit(), 12f, 8f, 30, 2000f, FullName));
				}
			}
			float fade = 1f - Timer / Lifetime;
			Lighting.AddLight(Projectile.Center, Elements.Main(Element).ToVector3() * fade * (Has(Huge) ? 2f : 1f));
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			return Timer <= GrowTicks + 2 && ElementFX.CircleHits(Projectile.Center, Radius, targetHitbox);
		}

		public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
			modifiers.HitDirectionOverride = target.Center.X >= Projectile.Center.X ? 1 : -1;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element, Has(Freeze));
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ElementFX.Ring;
			Texture2D glow = ElementFX.Glow;
			float fade = 1f - Timer / Lifetime;
			float radius = Radius;
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Elements.Main(Element), 0.55f * fade), 0f, glow.Size() / 2f, radius * 2.3f / glow.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, center, null, ElementFX.Additive(Elements.Core(Element), fade), 0f, ring.Size() / 2f, radius * 2f / ring.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, center, null, ElementFX.Additive(Elements.Main(Element), 0.8f * fade), 0f, ring.Size() / 2f, radius * 1.5f / ring.Width, SpriteEffects.None, 0);
			float flash = MathHelper.Clamp(1f - Timer / 8f, 0f, 1f);
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Color.White, flash), 0f, glow.Size() / 2f, radius * 0.9f / glow.Width + 0.3f, SpriteEffects.None, 0);
			return false;
		}
	}

	// An eruption from the ground: a short warning, then a column of the element bursts up and hits everything in it.
	// ai[0] = element, ai[1] = delay before it erupts (ticks). Spawned on the ground surface.
	// Blaze: fire geyser. Frost: ice spire (freezes). Volt: lightning bolt from the sky. Shadow: void tendril. Nova: star pillar.
	public class ElementPillar : ModProjectile
	{
		public const int WarnTicks = 18;
		public const int EruptTicks = 26;
		public const float Width = 56f;
		public const float Height = 230f;

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);
		private int Delay => (int)Projectile.ai[1];
		private ref float Timer => ref Projectile.localAI[0];
		private float EruptAge => Timer - Delay - WarnTicks;
		private bool Erupting => EruptAge >= 0f && EruptAge < EruptTicks;

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 120;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Timer++;
			if (EruptAge >= EruptTicks) {
				Projectile.Kill();
				return;
			}
			if (EruptAge == 0f) {
				SoundStyle sound = Element switch {
					JackElement.Blaze => SoundID.Item74,
					JackElement.Frost => SoundID.Item28,
					JackElement.Volt => SoundID.Item122,
					JackElement.Shadow => SoundID.Item104,
					_ => SoundID.Item9,
				};
				SoundEngine.PlaySound(sound with { Volume = 0.8f }, Projectile.Center);
				for (int i = 0; i < 25; i++) {
					Vector2 vel = new Vector2(Main.rand.NextFloat(-3f, 3f), -Main.rand.NextFloat(4f, 14f));
					Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-Width / 2f, Width / 2f), 0f), Elements.Dust(Element), vel, Scale: 1.5f).noGravity = true;
				}
			}
			else if (!Erupting && Timer > Delay && Main.rand.NextBool(2)) {
				// Warning: sparks bubbling out of the ground.
				Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-Width / 2f, Width / 2f), 0f), Elements.Dust(Element), new Vector2(0f, -2f), Scale: 0.9f).noGravity = true;
			}
			if (Erupting) {
				Lighting.AddLight(Projectile.Center - new Vector2(0f, Height / 2f), Elements.Main(Element).ToVector3() * 1.2f);
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (!Erupting || EruptAge > 10f) {
				return false;
			}
			Rectangle column = new Rectangle((int)(Projectile.Center.X - Width / 2f), (int)(Projectile.Center.Y - Height), (int)Width, (int)Height + 8);
			return column.Intersects(targetHitbox);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element, Element == JackElement.Frost);
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 ground = Projectile.Center - Main.screenPosition;
			Color main = Elements.Main(Element);
			Color core = Elements.Core(Element);
			Texture2D glow = ElementFX.Glow;

			if (!Erupting) {
				if (Timer > Delay) {
					float warn = (Timer - Delay) / WarnTicks;
					Main.EntitySpriteDraw(glow, ground, null, ElementFX.Additive(main, 0.7f * warn), 0f, glow.Size() / 2f, new Vector2(Width * 1.6f / glow.Width, 0.25f), SpriteEffects.None, 0);
				}
				return false;
			}

			// Shoots up fast, then fades.
			float grow = MathHelper.Clamp(EruptAge / 5f, 0f, 1f);
			float fade = 1f - MathHelper.Clamp((EruptAge - 10f) / (EruptTicks - 10f), 0f, 1f);
			float height = Height * grow;
			Vector2 top = ground - new Vector2(0f, height);

			if (Element == JackElement.Volt) {
				// A jagged lightning bolt from high above down to the ground.
				Terraria.Utilities.UnifiedRandom rand = new Terraria.Utilities.UnifiedRandom((int)(EruptAge / 3) + Projectile.whoAmI * 31);
				Vector2 from = ground - new Vector2(0f, 900f);
				const int segments = 12;
				for (int s = 1; s <= segments; s++) {
					Vector2 next = Vector2.Lerp(ground - new Vector2(0f, 900f), ground, s / (float)segments);
					if (s < segments) {
						next.X += rand.NextFloat(-22f, 22f);
					}
					ElementFX.Line(from, next, 16f, ElementFX.Additive(main, fade));
					ElementFX.Line(from, next, 6f, ElementFX.Additive(Color.White, fade));
					from = next;
				}
			}
			else {
				float width = Width * (Element == JackElement.Frost ? 0.8f : 1f);
				ElementFX.Line(top, ground, width * 1.6f, ElementFX.Additive(main, 0.5f * fade));
				ElementFX.Line(top, ground, width, ElementFX.Additive(main, 0.9f * fade));
				ElementFX.Line(top, ground, width * 0.4f, ElementFX.Additive(core, fade));
				Main.EntitySpriteDraw(glow, top, null, ElementFX.Additive(core, fade), 0f, glow.Size() / 2f, width * 1.4f / glow.Width, SpriteEffects.None, 0);
			}
			Main.EntitySpriteDraw(glow, ground, null, ElementFX.Additive(main, fade), 0f, glow.Size() / 2f, new Vector2(Width * 2.4f / glow.Width, 0.5f), SpriteEffects.None, 0);
			return false;
		}
	}

	// An element aura around the player for 8 seconds that hurts every enemy inside it three times a second.
	// ai[0] = element.
	public class ElementAura : ModProjectile
	{
		public const int Lifetime = 480;
		public const float Radius = 250f;

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);
		private ref float Timer => ref Projectile.localAI[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)Radius + 200;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
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
			// Particles swirling around inside the aura.
			for (int i = 0; i < 3; i++) {
				Vector2 offset = Main.rand.NextVector2Circular(Radius, Radius);
				Vector2 swirl = offset.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * 2f;
				Dust.NewDustPerfect(Projectile.Center + offset, Elements.Dust(Element), swirl, Scale: 1f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(Element).ToVector3() * 0.6f);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => ElementFX.CircleHits(Projectile.Center, Radius, targetHitbox);

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element);
		}

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ElementFX.Ring;
			Texture2D glow = ElementFX.Glow;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f) * MathHelper.Clamp(Timer / 12f, 0f, 1f);
			float scale = Radius * 2f / ring.Width;
			Color main = Elements.Main(Element);
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(main, 0.18f * fade), 0f, glow.Size() / 2f, Radius * 2.2f / glow.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, center, null, ElementFX.Additive(main, 0.7f * fade), Timer * 0.02f, ring.Size() / 2f, scale, SpriteEffects.None, 0);
			// Pulses rippling outward.
			for (int i = 0; i < 2; i++) {
				float t = (Timer / 50f + i * 0.5f) % 1f;
				Main.EntitySpriteDraw(ring, center, null, ElementFX.Additive(Elements.Core(Element), (1f - t) * 0.5f * fade), 0f, ring.Size() / 2f, scale * t, SpriteEffects.None, 0);
			}
			return false;
		}
	}

	// Element orbs (or blades) circling the player for 10 seconds, hitting what they touch.
	// ai[0] = element, ai[1] = this orbiter's index, ai[2] = how many there are.
	public class ElementOrbiter : ModProjectile
	{
		public const int Lifetime = 600;
		public const float OrbitRadius = 90f;

		private JackElement Element => Elements.FromAI(Projectile.ai[0]);
		private ref float Timer => ref Projectile.localAI[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults() {
			Projectile.width = 22;
			Projectile.height = 22;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = Lifetime;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
			Projectile.aiStyle = -1;
		}

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			if (!player.active || player.dead) {
				Projectile.Kill();
				return;
			}
			Timer++;
			float count = System.Math.Max(Projectile.ai[2], 1f);
			float angle = Timer * 0.08f + MathHelper.TwoPi * Projectile.ai[1] / count;
			float radius = OrbitRadius + (float)System.Math.Sin(Timer * 0.05f) * 12f;
			Vector2 target = player.Center + angle.ToRotationVector2() * radius;
			Projectile.velocity = target - Projectile.Center;
			Projectile.rotation = angle + MathHelper.PiOver2;
			Lighting.AddLight(Projectile.Center, Elements.Main(Element).ToVector3() * 0.5f);
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, Element);
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D glow = ElementFX.Glow;
			Vector2 origin = glow.Size() / 2f;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] == Vector2.Zero) {
					continue;
				}
				float t = 1f - i / (float)Projectile.oldPos.Length;
				Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition;
				Main.EntitySpriteDraw(glow, pos, null, ElementFX.Additive(Elements.Main(Element), 0.4f * t * fade), 0f, origin, 0.35f * t, SpriteEffects.None, 0);
			}
			Vector2 center = Projectile.Center - Main.screenPosition;
			// A blade-like streak along the orbit plus a bright core.
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Elements.Main(Element), fade), Projectile.rotation, origin, new Vector2(0.25f, 0.6f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, ElementFX.Additive(Elements.Core(Element), fade), Projectile.rotation, origin, new Vector2(0.12f, 0.3f), SpriteEffects.None, 0);
			return false;
		}
	}
}
