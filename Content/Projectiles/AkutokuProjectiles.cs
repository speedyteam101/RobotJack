using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using RobotJack.Common.Akutoku;
using RobotJack.Content.Buffs;
using RobotJack.Content.NPCs.Akutoku;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// Akutoku-ō's attacks (hostile projectiles). Everything he hits you with Siphons you.
	// Note: Terraria scales hostile projectile damage up on its own (more in Expert and Master), so the numbers
	// these are spawned with are roughly half of what they hit for in Normal mode.
	public static class AkutokuHit
	{
		public const int SiphonTicks = 8 * 60;

		public static void OnHit(Player target, bool siphon = true) {
			if (siphon) {
				target.AddBuff(ModContent.BuffType<Siphoned>(), SiphonTicks);
			}
			target.AddBuff(Elements.Debuff(JackElement.Blight), 180);
		}

		// The boss NPC a projectile belongs to (stored in ai[1]), if it's still alive.
		public static Akutokuo Boss(Projectile projectile) {
			int index = (int)projectile.ai[1];
			if (index < 0 || index >= Main.maxNPCs) {
				return null;
			}
			NPC npc = Main.npc[index];
			return npc.active && npc.ModNPC is Akutokuo boss ? boss : null;
		}

		public static void Glow(Vector2 at, Color color, float scale, float opacity = 1f) {
			Texture2D glow = ElementFX.Glow;
			Main.EntitySpriteDraw(glow, at, null, ElementFX.Additive(color, opacity), 0f, glow.Size() / 2f, scale, SpriteEffects.None, 0);
		}
	}

	// A glowing infected bolt (Robitacons fire these in their robot form's element; Akutoku-ō spits them).
	// ai[0] = element, ai[1] = 1 if it Siphons (the boss's own).
	public class InfectedBolt : ModProjectile
	{
		private JackElement Element => Elements.FromAI(Projectile.ai[0]);

		public override string Texture => "RobotJack/Content/Projectiles/ElementBolt";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 6;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults() {
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.rotation = Projectile.velocity.ToRotation();
			if (Main.rand.NextBool(3)) {
				Dust.NewDustPerfect(Projectile.Center, Elements.Dust(Element), -Projectile.velocity * 0.1f, Scale: 1f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(Element).ToVector3() * 0.4f);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) {
			target.AddBuff(Elements.Debuff(Element), 120);
			if (Projectile.ai[1] == 1f) {
				AkutokuHit.OnHit(target);
			}
		}

		public override void OnKill(int timeLeft) {
			ElementFX.Burst(Projectile.Center, Element, 8, 3f);
		}

		public override bool PreDraw(ref Color lightColor) {
			Color main = Elements.Main(Element);
			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] == Vector2.Zero) {
					continue;
				}
				float fade = 1f - i / (float)Projectile.oldPos.Length;
				AkutokuHit.Glow(Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, main, 0.28f * fade, 0.5f * fade);
			}
			Vector2 c = Projectile.Center - Main.screenPosition;
			Texture2D glow = ElementFX.Glow;
			Main.EntitySpriteDraw(glow, c, null, ElementFX.Additive(main, 1f), Projectile.rotation, glow.Size() / 2f, new Vector2(0.55f, 0.32f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, c, null, ElementFX.Additive(Elements.Core(Element), 1f), Projectile.rotation, glow.Size() / 2f, new Vector2(0.25f, 0.15f), SpriteEffects.None, 0);
			return false;
		}
	}

	// An infected explosion that hurts players inside it once. ai[0] = radius.
	public class InfectedBlast : ModProjectile
	{
		private const int Lifetime = 24;
		private float MaxRadius => Projectile.ai[0] <= 0f ? 90f : Projectile.ai[0];
		private ref float Timer => ref Projectile.localAI[0];
		private float Radius => MaxRadius * MathHelper.Clamp(Timer / 8f, 0f, 1f);

		public override string Texture => "RobotJack/Content/Projectiles/ElementBlast";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = Lifetime;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Timer++;
			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
				ElementFX.Burst(Projectile.Center, JackElement.Blight, 25, 8f, 1.4f);
				for (int i = 0; i < 8; i++) {
					Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, Main.rand.NextVector2Circular(4f, 4f), 100, default, 1.6f);
				}
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(JackElement.Blight).ToVector3() * (1f - Timer / Lifetime));
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
			Timer <= 10 && ElementFX.CircleHits(Projectile.Center, Radius, targetHitbox);

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			Vector2 c = Projectile.Center - Main.screenPosition;
			float fade = 1f - Timer / Lifetime;
			Texture2D ring = ElementFX.Ring;
			Texture2D glow = ElementFX.Glow;
			Color main = Elements.Main(JackElement.Blight);
			Main.EntitySpriteDraw(glow, c, null, ElementFX.Additive(main, 0.6f * fade), 0f, glow.Size() / 2f, Radius * 2.2f / glow.Width, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(ring, c, null, ElementFX.Additive(Elements.Core(JackElement.Blight), fade), 0f, ring.Size() / 2f, Radius * 2f / ring.Width, SpriteEffects.None, 0);
			return false;
		}
	}

	// Rocket Launcher leg: an infected rocket that curves toward its target, then explodes. ai[0] = target player.
	public class InfectedRocket : ModProjectile
	{
		private ref float Timer => ref Projectile.localAI[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Type] = 8;
			ProjectileID.Sets.TrailingMode[Type] = 0;
		}

		public override void SetDefaults() {
			Projectile.width = 18;
			Projectile.height = 18;
			Projectile.hostile = true;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Timer++;
			Player target = Main.player[(int)Projectile.ai[0]];
			if (Timer > 15 && Timer < 90 && target.active && !target.dead) {
				float speed = System.Math.Min(Projectile.velocity.Length() + 0.15f, 13f);
				Vector2 wanted = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * speed;
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, 0.05f);
			}
			Projectile.rotation = Projectile.velocity.ToRotation();
			Vector2 back = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.Zero) * 12f;
			Dust.NewDustPerfect(back, DustID.Torch, -Projectile.velocity * 0.2f, Scale: 1.3f).noGravity = true;
			if (Main.rand.NextBool(2)) {
				Dust.NewDustPerfect(back, DustID.Smoke, -Projectile.velocity * 0.1f, 120, default, 1.2f);
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override void OnKill(int timeLeft) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<InfectedBlast>(),
					Projectile.damage, 0f, Main.myPlayer, ai0: 90f);
			}
		}

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			Vector2 c = Projectile.Center - Main.screenPosition;
			for (int i = Projectile.oldPos.Length - 1; i >= 0; i--) {
				if (Projectile.oldPos[i] != Vector2.Zero) {
					float fade = 1f - i / (float)Projectile.oldPos.Length;
					AkutokuHit.Glow(Projectile.oldPos[i] + Projectile.Size / 2f - Main.screenPosition, new Color(255, 140, 50), 0.2f * fade, 0.5f * fade);
				}
			}
			Main.EntitySpriteDraw(tex, c, null, lightColor, Projectile.rotation, tex.Size() / 2f, 1f, SpriteEffects.None, 0);
			AkutokuHit.Glow(c - Projectile.velocity.SafeNormalize(Vector2.Zero) * 14f, new Color(255, 160, 60), 0.3f);
			return false;
		}
	}

	// Laser Cannon leg: a thin warning line, then a huge infected laser along it.
	// ai[1] = boss, ai[2] = weapon slot. velocity = direction (locked when the warning ends).
	public class InfectedLaser : ModProjectile
	{
		public const int WarnTicks = 45;
		public const int FireTicks = 34;
		private const float Length = 2000f;
		private ref float Timer => ref Projectile.localAI[0];
		private bool Firing => Timer > WarnTicks;

		public override string Texture => "RobotJack/Content/Projectiles/ElementBeam";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4200;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = WarnTicks + FireTicks;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Timer++;
			Akutokuo boss = AkutokuHit.Boss(Projectile);
			if (boss == null) {
				Projectile.Kill();
				return;
			}
			int slot = (int)Projectile.ai[2];
			Projectile.Center = boss.Rig.WeaponTip(slot);
			// While warning, the aim keeps tracking the boss's aim; then it's locked.
			if (!Firing) {
				Projectile.velocity = boss.Rig.Aim[slot];
			}
			else {
				boss.Rig.Aim[slot] = Projectile.velocity;
				boss.Rig.Recoil[slot] = 4f;
			}
			if (Timer == WarnTicks + 1) {
				SoundEngine.PlaySound(SoundID.Item122 with { Pitch = -0.6f, Volume = 0.9f }, Projectile.Center);
			}
			if (Firing) {
				Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
				for (float d = 0; d < Length; d += 80f) {
					Lighting.AddLight(Projectile.Center + dir * d, Elements.Main(JackElement.Blight).ToVector3());
				}
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (!Firing) {
				return false;
			}
			float point = 0f;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + dir * Length, 50f, ref point);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			Vector2 start = Projectile.Center - Main.screenPosition;
			Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
			Color main = Elements.Main(JackElement.Blight);
			if (!Firing) {
				float blink = 0.4f + 0.4f * (float)System.Math.Sin(Timer * 0.6f);
				ElementFX.Line(start, start + dir * Length, 4f, ElementFX.Additive(main, blink));
				AkutokuHit.Glow(start, main, 0.2f + Timer / WarnTicks * 0.5f, Timer / WarnTicks);
				return false;
			}
			float t = (Timer - WarnTicks) / FireTicks;
			float width = 70f * (float)System.Math.Sin(MathHelper.Clamp(t, 0f, 1f) * MathHelper.Pi) + 10f;
			ElementFX.Line(start, start + dir * Length, width * 2f, ElementFX.Additive(main, 0.5f));
			ElementFX.Line(start, start + dir * Length, width, ElementFX.Additive(Elements.Core(JackElement.Blight), 0.9f));
			ElementFX.Line(start, start + dir * Length, width * 0.35f, ElementFX.Additive(Color.White, 1f));
			AkutokuHit.Glow(start, main, 1.2f);
			return false;
		}
	}

	// Axe and Sword legs: the hitbox of a melee swing along the weapon, while the boss swings it.
	// ai[1] = boss, ai[2] = weapon slot.
	public class InfectedSwing : ModProjectile
	{
		public override string Texture => "RobotJack/Content/Projectiles/ElementBlast";

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = Akutokuo.SwingTicks;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Akutokuo boss = AkutokuHit.Boss(Projectile);
			if (boss == null) {
				Projectile.Kill();
				return;
			}
			int slot = (int)Projectile.ai[2];
			// Drives the swing animation on every client.
			boss.Rig.Swing[slot] = MathHelper.Clamp(1f - Projectile.timeLeft / (float)Akutokuo.SwingTicks, 0.01f, 1f);
			Projectile.Center = boss.Rig.WeaponMount(slot);
			Vector2 tip = boss.Rig.WeaponTip(slot);
			if (Main.rand.NextBool(2)) {
				Dust.NewDustPerfect(Vector2.Lerp(Projectile.Center, tip, Main.rand.NextFloat()), Elements.Dust(JackElement.Blight), Vector2.Zero, Scale: 1.4f).noGravity = true;
			}
		}

		public override void OnKill(int timeLeft) {
			Akutokuo boss = AkutokuHit.Boss(Projectile);
			if (boss != null) {
				boss.Rig.Swing[(int)Projectile.ai[2]] = 0f;
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			Akutokuo boss = AkutokuHit.Boss(Projectile);
			if (boss == null) {
				return false;
			}
			int slot = (int)Projectile.ai[2];
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), boss.Rig.WeaponMount(slot), boss.Rig.WeaponTip(slot), 60f, ref point);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			Akutokuo boss = AkutokuHit.Boss(Projectile);
			if (boss == null) {
				return false;
			}
			// A glowing trail along the blade.
			int slot = (int)Projectile.ai[2];
			ElementFX.Line(boss.Rig.WeaponMount(slot) - Main.screenPosition, boss.Rig.WeaponTip(slot) - Main.screenPosition, 40f,
				ElementFX.Additive(Elements.Main(JackElement.Blight), 0.45f));
			return false;
		}
	}

	// A slash wave the Sword leg sends flying at range.
	public class InfectedSlashWave : ModProjectile
	{
		public override string Texture => "RobotJack/Content/Projectiles/ElementBlast";

		public override void SetDefaults() {
			Projectile.width = 60;
			Projectile.height = 60;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = 90;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Projectile.rotation = Projectile.velocity.ToRotation();
			Projectile.velocity *= 1.02f;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			Vector2 c = Projectile.Center - Main.screenPosition;
			Texture2D glow = ElementFX.Glow;
			float fade = MathHelper.Clamp(Projectile.timeLeft / 20f, 0f, 1f);
			Main.EntitySpriteDraw(glow, c, null, ElementFX.Additive(Elements.Main(JackElement.Blight), fade), Projectile.rotation, glow.Size() / 2f, new Vector2(0.35f, 1.3f), SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, c, null, ElementFX.Additive(Color.White, fade), Projectile.rotation, glow.Size() / 2f, new Vector2(0.12f, 0.9f), SpriteEffects.None, 0);
			return false;
		}
	}

	// Flamethrower leg: one gout of infected flame (cursed flames in the Corruption, ichor fire in the Crimson).
	public class InfectedFlame : ModProjectile
	{
		private ref float Timer => ref Projectile.localAI[0];

		public override string Texture => "RobotJack/Content/Projectiles/ElementBlast";

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 40;
			Projectile.aiStyle = -1;
		}

		public override void AI() {
			Timer++;
			Projectile.velocity *= 0.96f;
			float size = 30f + Timer * 1.5f;
			Vector2 c = Projectile.Center;
			Projectile.width = Projectile.height = (int)size;
			Projectile.Center = c;
			if (Main.rand.NextBool(2)) {
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(size / 2f, size / 2f), Elements.Dust(JackElement.Blight), Projectile.velocity * 0.5f, Scale: 1.6f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(JackElement.Blight).ToVector3() * 0.8f);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			float fade = Projectile.timeLeft / 40f;
			Vector2 c = Projectile.Center - Main.screenPosition;
			AkutokuHit.Glow(c, Elements.Main(JackElement.Blight), Projectile.width / 90f, 0.7f * fade);
			AkutokuHit.Glow(c, Elements.Core(JackElement.Blight), Projectile.width / 200f, fade);
			return false;
		}
	}

	// Buzzsaw leg: a saw blade thrown at you that bounces off walls, then flies back to the boss.
	// ai[1] = boss, ai[2] = weapon slot.
	public class InfectedSaw : ModProjectile
	{
		private ref float Timer => ref Projectile.localAI[0];

		public override string Texture => "RobotJack/Content/NPCs/Akutoku/SawBlade";

		public override void SetDefaults() {
			Projectile.width = 60;
			Projectile.height = 60;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 400;
			Projectile.aiStyle = -1;
			Projectile.tileCollide = true;
			Projectile.scale = 2f;
		}

		public override void AI() {
			Timer++;
			Projectile.rotation += 0.5f;
			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Item22, Projectile.Center);
			}
			if (Timer > 90) {
				// Fly home.
				Projectile.tileCollide = false;
				Akutokuo boss = AkutokuHit.Boss(Projectile);
				if (boss == null) {
					Projectile.Kill();
					return;
				}
				Vector2 home = boss.Rig.WeaponMount((int)Projectile.ai[2]);
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, (home - Projectile.Center).SafeNormalize(Vector2.Zero) * 20f, 0.1f);
				if (Vector2.Distance(home, Projectile.Center) < 40f) {
					Projectile.Kill();
				}
			}
			if (Main.rand.NextBool(3)) {
				Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(28f, 28f), DustID.Electric, Vector2.Zero, Scale: 0.8f).noGravity = true;
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity) {
			if (Projectile.velocity.X != oldVelocity.X) {
				Projectile.velocity.X = -oldVelocity.X;
			}
			if (Projectile.velocity.Y != oldVelocity.Y) {
				Projectile.velocity.Y = -oldVelocity.Y;
			}
			SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
			return false;
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
			Vector2 c = Projectile.Center - Main.screenPosition;
			AkutokuHit.Glow(c, Elements.Main(JackElement.Blight), 0.6f, 0.5f);
			Main.EntitySpriteDraw(tex, c, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	// Tesla Coil leg: marks a spot, then a bolt of infected lightning jumps from the coil to it.
	// ai[1] = boss, ai[2] = weapon slot. velocity = the target spot (it doesn't move).
	public class InfectedTesla : ModProjectile
	{
		public const int WarnTicks = 40;
		private ref float Timer => ref Projectile.localAI[0];
		private Vector2 Target => Projectile.velocity;

		public override string Texture => "RobotJack/Content/Projectiles/ElementBlast";

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.hostile = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.timeLeft = WarnTicks + 14;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override void AI() {
			Timer++;
			Akutokuo boss = AkutokuHit.Boss(Projectile);
			if (boss == null) {
				Projectile.Kill();
				return;
			}
			Projectile.Center = boss.Rig.WeaponTip((int)Projectile.ai[2]);
			if (Timer == WarnTicks + 1) {
				SoundEngine.PlaySound(SoundID.Item122, Target);
				ElementFX.Burst(Target, JackElement.Blight, 20, 7f, 1.4f);
			}
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			if (Timer <= WarnTicks || Timer > WarnTicks + 6) {
				return false;
			}
			float point = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Target, 30f, ref point)
				|| ElementFX.CircleHits(Target, 70f, targetHitbox);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo info) => AkutokuHit.OnHit(target);

		public override bool PreDraw(ref Color lightColor) {
			Vector2 from = Projectile.Center - Main.screenPosition;
			Vector2 to = Target - Main.screenPosition;
			Color main = Elements.Main(JackElement.Blight);
			if (Timer <= WarnTicks) {
				// A crackling target ring on the spot.
				Texture2D ring = ElementFX.Ring;
				float k = Timer / WarnTicks;
				Main.EntitySpriteDraw(ring, to, null, ElementFX.Additive(main, 0.4f + 0.6f * k), Timer * 0.1f, ring.Size() / 2f, 140f * (1.4f - 0.4f * k) / ring.Width, SpriteEffects.None, 0);
				return false;
			}
			float fade = 1f - (Timer - WarnTicks) / 14f;
			Terraria.Utilities.UnifiedRandom rand = new Terraria.Utilities.UnifiedRandom((int)Timer / 2 + Projectile.whoAmI * 17);
			Vector2 prev = from;
			const int segments = 12;
			for (int s = 1; s <= segments; s++) {
				Vector2 next = Vector2.Lerp(from, to, s / (float)segments);
				if (s < segments) {
					next += (to - from).SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * rand.NextFloat(-30f, 30f);
				}
				ElementFX.Line(prev, next, 18f, ElementFX.Additive(main, fade));
				ElementFX.Line(prev, next, 6f, ElementFX.Additive(Color.White, fade));
				prev = next;
			}
			AkutokuHit.Glow(to, main, 1.5f, fade);
			return false;
		}
	}
}
