using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Common.Titan;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Common.Akutoku
{
	// The weapons Akutoku-ō's two front legs can turn into.
	public enum LegWeapon : byte
	{
		RocketLauncher,
		LaserCannon,
		Axe,
		Sword,
		Flamethrower,
		Buzzsaw,
		TeslaCoil,
	}

	// A walking leg's foot: planted on the ground, or stepping from one spot to the next.
	public class SpiderLeg
	{
		public Vector2 Foot;
		public Vector2 StepFrom;
		public Vector2 StepTo;
		public int StepTimer; // counts down while stepping
		public bool Initialized;
	}

	// Draws a robot spider (Akutoku-ō, or Spider Jack at a much smaller scale) from separate parts, with 8 walking
	// legs that plant their feet on the ground and step as the body moves (inverse kinematics), and 2 front legs
	// that hold weapons, aim them, swing them, and transform from one weapon into another.
	// Art is authored facing right in "art pixels"; Scale is how many screen pixels one art pixel is.
	// Everything here is visual only (client side); the owner decides where the spider is and what it attacks.
	public class SpiderRig
	{
		public const int WeaponCount = 7;
		public const int SwapTicks = 60;
		public const int SwapMidTick = 30; // the old weapon is gone and the new one appears
		private const int StepTicks = 10;
		private const string Path = "RobotJack/Content/NPCs/Akutoku/";

		public float Scale = 3f;
		public int Dir = 1;
		public Vector2 Center;
		public Color GlowColor = Color.White;
		public bool Infected = true; // draws the pulsing infection glow on the body

		public readonly SpiderLeg[] Legs = new SpiderLeg[8]; // 0-3 near side front to back, 4-7 far side
		public readonly LegWeapon[] Weapons = { LegWeapon.RocketLauncher, LegWeapon.Sword };
		private readonly LegWeapon[] nextWeapon = new LegWeapon[2];
		public readonly int[] SwapTimer = new int[2];     // 0 = not transforming
		public readonly Vector2[] Aim = { Vector2.UnitX, Vector2.UnitX }; // world-space aim of each weapon leg
		public readonly float[] Swing = new float[2];     // melee swing progress 0..1 (0 = at rest)
		public readonly float[] Recoil = new float[2];    // pushes the weapon back after firing, decays
		private float time;

		public SpiderRig() {
			for (int i = 0; i < Legs.Length; i++) {
				Legs[i] = new SpiderLeg();
			}
		}

		public bool Swapping(int slot) => SwapTimer[slot] > 0;

		// Starts turning weapon leg `slot` into another weapon (the animation takes SwapTicks).
		public void StartSwap(int slot, LegWeapon to) {
			if (Swapping(slot) || Weapons[slot] == to) {
				return;
			}
			nextWeapon[slot] = to;
			SwapTimer[slot] = 1;
		}

		// ------------------------------------------------------------------ geometry (art px, facing right, from Center)

		private static readonly Vector2 ThoraxPivot = new(32, 20), ThoraxAt = new(22, 0);
		private static readonly Vector2 AbdomenPivot = new(74, 24), AbdomenAt = new(-4, 0);
		private static readonly Vector2 HeadPivot = new(4, 14), HeadAt = new(50, -2);
		private static readonly float[] HipX = { 38, 28, 16, 4 };
		private static readonly float[] RestFootX = { 58, 30, -18, -50 }; // where each foot wants to be, relative to its hip
		private const float Femur = 46f, Tibia = 56f;
		private static readonly Vector2 WeaponHip = new(46, 4);
		private const float WeaponUpper = 30f, WeaponLower = 30f;
		private const float SegmentUpperLength = 40f, SegmentLowerLength = 50f; // texture pivot-to-end lengths

		// Pivot (left end, where it attaches) and muzzle/tip of each weapon texture.
		private static readonly Vector2[] WeaponPivot = { new(2, 9), new(2, 8), new(2, 20), new(2, 7), new(2, 10), new(2, 17), new(2, 11) };
		private static readonly Vector2[] WeaponTipArt = { new(40, 9), new(44, 8), new(34, 20), new(60, 7), new(38, 10), new(30, 17), new(36, 11) };
		private static readonly string[] WeaponTexture = { "WeaponRocket", "WeaponLaser", "WeaponAxe", "WeaponSword", "WeaponFlamer", "WeaponSaw", "WeaponTesla" };

		private Vector2 W(Vector2 local) => Center + new Vector2(local.X * Dir, local.Y) * Scale;

		private Vector2 Hip(int leg) {
			bool far = leg >= 4;
			return W(new Vector2(HipX[leg % 4] - (far ? 3 : 0), far ? 6 : 10));
		}

		public Vector2 WeaponHipWorld(int slot) => W(WeaponHip + (slot == 1 ? new Vector2(-4, -3) : Vector2.Zero));

		// Weapon leg joints: elbow and the mount at the end (where the weapon attaches), and the weapon's rotation.
		private void WeaponPose(int slot, out Vector2 hip, out Vector2 elbow, out Vector2 mount, out float weaponRot, out float retract) {
			hip = WeaponHipWorld(slot);
			float aimRot = Aim[slot].ToRotation();
			// Melee swings arc the weapon from high above down through the aim.
			if (Swing[slot] > 0f) {
				aimRot += (-1.7f + 2.9f * Swing[slot]) * Dir;
			}
			retract = 0f;
			int t = SwapTimer[slot];
			if (t > 0) {
				retract = t < 18 ? t / 18f : t < 42 ? 1f : 1f - (t - 42) / 18f;
			}
			// The upper segment rises from the hip, the lower one points along the aim. Folding in while transforming.
			float upperRot = MathHelper.Lerp(aimRot - 0.7f * Dir, -MathHelper.PiOver2 - 0.3f * Dir, retract);
			float lowerRot = MathHelper.Lerp(aimRot, MathHelper.PiOver2 + 1.2f * Dir, retract);
			elbow = hip + upperRot.ToRotationVector2() * WeaponUpper * Scale;
			mount = elbow + lowerRot.ToRotationVector2() * WeaponLower * Scale * (1f - 0.35f * retract);
			mount -= Aim[slot] * Recoil[slot] * Scale;
			weaponRot = lowerRot;
		}

		// World position of a weapon's muzzle or blade tip.
		public Vector2 WeaponTip(int slot) {
			WeaponPose(slot, out _, out _, out Vector2 mount, out float rot, out _);
			int w = (int)Weapons[slot];
			Vector2 local = WeaponTipArt[w] - WeaponPivot[w];
			return mount + new Vector2(local.X, local.Y * Dir).RotatedBy(rot) * Scale;
		}

		public Vector2 WeaponMount(int slot) {
			WeaponPose(slot, out _, out _, out Vector2 mount, out _, out _);
			return mount;
		}

		// ------------------------------------------------------------------ update

		// Call once per tick (client side) after the owner has moved.
		public void Update(Vector2 center, int dir, float scale, bool playSounds = true) {
			bool turned = dir != Dir;
			Center = center;
			Dir = dir;
			Scale = scale;
			time++;
			UpdateLegs(turned);
			for (int slot = 0; slot < 2; slot++) {
				Recoil[slot] *= 0.8f;
				if (SwapTimer[slot] > 0) {
					UpdateSwap(slot, playSounds);
				}
			}
		}

		private void UpdateSwap(int slot, bool playSounds) {
			int t = ++SwapTimer[slot];
			Vector2 mount = WeaponMount(slot);
			if (t == 2 && playSounds) {
				SoundEngine.PlaySound(SoundID.Item22 with { Pitch = -0.4f, Volume = 0.8f }, mount);
			}
			if (t >= 18 && t < 42) {
				// Sparks and bits of plating flying off while the weapon reshapes itself.
				if (Main.rand.NextBool(2)) {
					Dust spark = Dust.NewDustPerfect(mount, DustID.Electric, Main.rand.NextVector2Circular(4f, 4f) * Scale / 3f, Scale: 0.9f + Scale * 0.15f);
					spark.noGravity = true;
				}
				if (Main.rand.NextBool(5)) {
					Dust.NewDustPerfect(mount, DustID.Silver, Main.rand.NextVector2Circular(3f, 3f) - Vector2.UnitY * 2f, Scale: 1f + Scale * 0.1f);
				}
			}
			if (t == SwapMidTick) {
				Weapons[slot] = nextWeapon[slot];
				if (playSounds) {
					SoundEngine.PlaySound(SoundID.Item37 with { Pitch = -0.3f }, mount);
					SoundEngine.PlaySound(SoundID.Item113 with { Pitch = 0.4f, Volume = 0.6f }, mount);
				}
				for (int i = 0; i < 20; i++) {
					Dust d = Dust.NewDustPerfect(mount, DustID.Electric, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 7f) * Scale / 3f, Scale: 1.3f);
					d.noGravity = true;
				}
			}
			if (t >= SwapTicks) {
				SwapTimer[slot] = 0;
			}
		}

		private void UpdateLegs(bool turned) {
			for (int i = 0; i < Legs.Length; i++) {
				SpiderLeg leg = Legs[i];
				Vector2 hip = Hip(i);
				Vector2 desired = DesiredFoot(i, hip);
				if (!leg.Initialized || turned) {
					leg.Foot = desired;
					leg.StepTimer = 0;
					leg.Initialized = true;
					continue;
				}
				if (leg.StepTimer > 0) {
					leg.StepTimer--;
					float t = 1f - leg.StepTimer / (float)StepTicks;
					Vector2 p = Vector2.Lerp(leg.StepFrom, leg.StepTo, t);
					p.Y -= (float)System.Math.Sin(t * MathHelper.Pi) * 16f * Scale;
					leg.Foot = p;
					if (leg.StepTimer == 0 && Scale >= 2f && Main.rand.NextBool(2)) {
						Dust.NewDustPerfect(leg.Foot, DustID.Smoke, new Vector2(Main.rand.NextFloat(-1f, 1f), -1f), 100, default, 1.2f);
					}
					continue;
				}
				// Step when the foot has fallen too far behind (or the leg is overstretched).
				// Legs step in alternating groups, like a real spider, so it never stands on one side only.
				float stride = 26f * Scale;
				bool overstretched = Vector2.Distance(hip, leg.Foot) > (Femur + Tibia) * Scale * 0.98f;
				if ((Vector2.Distance(leg.Foot, desired) > stride || overstretched) && !NeighbourStepping(i)) {
					leg.StepFrom = leg.Foot;
					// Aim a little past the resting spot, in the direction of travel.
					leg.StepTo = desired + (desired - leg.Foot).SafeNormalize(Vector2.Zero) * stride * 0.4f;
					leg.StepTo = GroundAt(leg.StepTo, hip);
					leg.StepTimer = StepTicks;
				}
			}
		}

		private bool NeighbourStepping(int i) {
			int side = i / 4 * 4;
			int k = i % 4;
			return (k > 0 && Legs[side + k - 1].StepTimer > 0) || (k < 3 && Legs[side + k + 1].StepTimer > 0)
				|| Legs[(i + 4) % 8].StepTimer > 0;
		}

		private Vector2 DesiredFoot(int i, Vector2 hip) {
			float x = hip.X + (RestFootX[i % 4] + (i >= 4 ? 8f : 0f)) * Dir * Scale;
			return GroundAt(new Vector2(x, hip.Y), hip);
		}

		// The ground under a point, within reach of the hip; if there's none, the foot hangs down.
		private Vector2 GroundAt(Vector2 at, Vector2 hip) {
			float reach = (Femur + Tibia) * Scale * 0.95f;
			int x = (int)(at.X / 16f);
			int startY = (int)((hip.Y - 20f * Scale) / 16f);
			int endY = (int)((hip.Y + reach) / 16f);
			for (int y = startY; y <= endY; y++) {
				if (!WorldGen.InWorld(x, y, 2)) {
					break;
				}
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])) {
					return new Vector2(at.X, y * 16f);
				}
			}
			return new Vector2(at.X, hip.Y + reach * 0.75f);
		}

		// Two-bone inverse kinematics with the knee bent upward (spider knees stick up above the body line).
		private static Vector2 Knee(Vector2 hip, Vector2 foot, float upper, float lower) {
			Vector2 diff = foot - hip;
			float d = MathHelper.Clamp(diff.Length(), 1f, upper + lower - 0.01f);
			float a = diff.ToRotation();
			float cos = MathHelper.Clamp((upper * upper + d * d - lower * lower) / (2f * upper * d), -1f, 1f);
			float alpha = (float)System.Math.Acos(cos);
			Vector2 k1 = hip + (a - alpha).ToRotationVector2() * upper;
			Vector2 k2 = hip + (a + alpha).ToRotationVector2() * upper;
			return k1.Y < k2.Y ? k1 : k2;
		}

		// ------------------------------------------------------------------ drawing

		private static readonly Dictionary<string, Asset<Texture2D>> cache = new();

		private static Texture2D Tex(string name) {
			if (!cache.TryGetValue(name, out Asset<Texture2D> asset)) {
				asset = ModContent.Request<Texture2D>(Path + name, AssetRequestMode.ImmediateLoad);
				cache[name] = asset;
			}
			return asset.Value;
		}

		public static void Unload() => cache.Clear();

		// Sprites are collected into a list, so the boss can draw them straight away and a player draw layer can
		// turn them into DrawData.
		private List<TitanSprite> output;

		private void Emit(Texture2D tex, Vector2 at, Color color, float rot, Vector2 origin, Vector2 scale, SpriteEffects fx) {
			output.Add(new TitanSprite { Texture = tex, Position = at, Color = color, Rotation = rot, Origin = origin, Scale = 1f, NonUniformScale = scale, Effects = fx });
		}

		private void Emit(Texture2D tex, Vector2 at, Color color, float rot, Vector2 origin, float scale, SpriteEffects fx) {
			output.Add(new TitanSprite { Texture = tex, Position = at, Color = color, Rotation = rot, Origin = origin, Scale = scale, Effects = fx });
		}

		private static readonly List<TitanSprite> drawList = new();

		// Draws the spider straight away (the sprite batch must already be begun).
		public void Draw(SpriteBatch sb, Vector2 screenPos, Color light, float opacity = 1f) {
			drawList.Clear();
			Build(drawList, screenPos, light, opacity);
			foreach (TitanSprite sprite in drawList) {
				sprite.Draw(sb);
			}
		}

		private SpriteEffects Flip => Dir == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

		private Vector2 Origin(Texture2D tex, Vector2 pivot) => Dir == 1 ? pivot : new Vector2(tex.Width - pivot.X, pivot.Y);

		// A body part and its glow.
		private void Part(SpriteBatch sb, string name, Vector2 at, Vector2 pivot, Color light, Color glow, float rot = 0f) {
			Texture2D tex = Tex(name);
			Emit(tex, at, light, rot, Origin(tex, pivot), Scale, Flip);
			Emit(Tex(name + "_Glow"), at, glow, rot, Origin(tex, pivot), Scale, Flip);
		}

		// A leg segment stretched from one joint to the next (the textures point down from their pivot).
		private void Segment(SpriteBatch sb, string name, Vector2 from, Vector2 to, float length, Color light, Color glow) {
			Texture2D tex = Tex(name);
			Vector2 diff = to - from;
			float rot = diff.ToRotation() - MathHelper.PiOver2;
			Vector2 scale = new Vector2(Scale, diff.Length() / length);
			Vector2 origin = new Vector2(tex.Width / 2f, 4f);
			Emit(tex, from, light, rot, origin, scale, SpriteEffects.None);
			Emit(Tex(name + "_Glow"), from, glow, rot, origin, scale, SpriteEffects.None);
		}

		private void DrawLeg(SpriteBatch sb, int i, Vector2 screen, Color light, Color glow) {
			Vector2 hip = Hip(i);
			Vector2 foot = Legs[i].Initialized ? Legs[i].Foot : DesiredFoot(i, hip);
			Vector2 knee = Knee(hip, foot, Femur * Scale, Tibia * Scale);
			Segment(sb, "LegUpper", hip - screen, knee - screen, SegmentUpperLength, light, glow);
			Segment(sb, "LegLower", knee - screen, foot - screen, SegmentLowerLength, light, glow);
			Texture2D joint = Tex("Joint");
			Emit(joint, knee - screen, light, 0f, joint.Size() / 2f, Scale, SpriteEffects.None);
		}

		private void DrawWeaponLeg(SpriteBatch sb, int slot, Vector2 screen, Color light, Color glow) {
			WeaponPose(slot, out Vector2 hip, out Vector2 elbow, out Vector2 mount, out float rot, out float retract);
			Segment(sb, "LegUpper", hip - screen, elbow - screen, SegmentUpperLength, light, glow);
			Segment(sb, "WeaponArm", elbow - screen, mount - screen, SegmentUpperLength, light, glow);
			Texture2D joint = Tex("Joint");
			Emit(joint, elbow - screen, light, 0f, joint.Size() / 2f, Scale, SpriteEffects.None);

			// Transforming: the old weapon shrinks and spins away into the leg, the new one unfolds out of it.
			int t = SwapTimer[slot];
			float size = 1f, spin = 0f;
			if (t > 0 && t < SwapMidTick) {
				size = 1f - MathHelper.Clamp((t - 12) / 18f, 0f, 1f);
				spin = (1f - size) * 3f * Dir;
			}
			else if (t >= SwapMidTick) {
				size = MathHelper.Clamp((t - SwapMidTick) / 14f, 0f, 1f);
				size = size < 1f ? size * (1.25f - 0.25f * size) : 1f; // slight overshoot as it snaps open
				spin = (1f - size) * -3f * Dir;
			}
			if (size > 0.02f) {
				int w = (int)Weapons[slot];
				Texture2D tex = Tex(WeaponTexture[w]);
				// Weapons are drawn along the lower segment; when facing left the texture is flipped vertically
				// (it's rotated by ~180 degrees) so its top stays on top.
				SpriteEffects fx = Dir == 1 ? SpriteEffects.None : SpriteEffects.FlipVertically;
				Vector2 origin = Dir == 1 ? WeaponPivot[w] : new Vector2(WeaponPivot[w].X, tex.Height - WeaponPivot[w].Y);
				float r = rot + spin;
				Emit(tex, mount - screen, light, r, origin, Scale * size, fx);
				Emit(Tex(WeaponTexture[w] + "_Glow"), mount - screen, glow, r, origin, Scale * size, fx);
				if (Weapons[slot] == LegWeapon.Buzzsaw) {
					Texture2D blade = Tex("SawBlade");
					Vector2 hub = mount + new Vector2(20f, 0f).RotatedBy(r) * Scale * size;
					Emit(blade, hub - screen, light, time * 0.5f, blade.Size() / 2f, Scale * size, SpriteEffects.None);
				}
			}
			// A flash and a spinning gear ring while the weapon reshapes.
			if (t >= 14 && t < 46) {
				float k = 1f - System.Math.Abs(t - SwapMidTick) / 16f;
				Texture2D ring = ElementFX.Ring;
				Texture2D glowTex = ElementFX.Glow;
				Vector2 at = mount - screen;
				Emit(ring, at, ElementFX.Additive(glow, k), time * 0.3f, ring.Size() / 2f, Scale * 0.12f * (1.5f - k * 0.5f), SpriteEffects.None);
				Emit(glowTex, at, ElementFX.Additive(Color.White, k * 0.9f), 0f, glowTex.Size() / 2f, Scale * 0.12f * k, SpriteEffects.None);
			}
			if (retract > 0.9f && t < SwapMidTick) {
				// Panels open on the leg's end.
				Texture2D panel = Tex("Panel");
				Vector2 at = mount - screen;
				for (int s = -1; s <= 1; s += 2) {
					Emit(panel, at, light, rot + s * 0.8f, new Vector2(0f, panel.Height / 2f), Scale, SpriteEffects.None);
				}
			}
		}

		// light: the lighting colour at the spider; glow: its full-bright glow colour (infection or form colour).
		public void Build(List<TitanSprite> list, Vector2 screenPos, Color light, float opacity = 1f) {
			output = list;
			SpriteBatch sb = null;
			Color far = new Color((int)(light.R * 0.6f), (int)(light.G * 0.6f), (int)(light.B * 0.6f), light.A) * opacity;
			Color near = light * opacity;
			float pulse = Infected ? 0.75f + 0.25f * (float)System.Math.Sin(time * 0.08f) : 1f;
			Color glow = GlowColor * (pulse * opacity);
			Color farGlow = glow * 0.6f;

			for (int i = 4; i < 8; i++) {
				DrawLeg(sb, i, screenPos, far, farGlow);
			}
			DrawWeaponLeg(sb, 1, screenPos, far, farGlow);
			Part(sb, "Abdomen", W(AbdomenAt) - screenPos, AbdomenPivot, near, glow);
			Part(sb, "Thorax", W(ThoraxAt) - screenPos, ThoraxPivot, near, glow);
			Part(sb, "Head", W(HeadAt) - screenPos, HeadPivot, near, glow);
			for (int i = 0; i < 4; i++) {
				DrawLeg(sb, i, screenPos, near, glow);
			}
			DrawWeaponLeg(sb, 0, screenPos, near, glow);
		}
	}
}
