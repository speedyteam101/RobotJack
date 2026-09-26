using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace RobotJack.Common.Titan
{
	// One sprite to draw (the renderer builds a list of these; the player draw layer and the menu preview draw them).
	public struct TitanSprite
	{
		public Texture2D Texture;
		public Vector2 Position;
		public Color Color;
		public float Rotation;
		public Vector2 Origin;
		public float Scale;
		public SpriteEffects Effects;
		public Vector2 NonUniformScale; // used instead of Scale when not zero (stretched lines)

		public Vector2 ScaleVector => NonUniformScale != Vector2.Zero ? NonUniformScale : new Vector2(Scale);

		public void Draw(SpriteBatch spriteBatch) {
			spriteBatch.Draw(Texture, Position, null, Color, Rotation, Origin, ScaleVector, Effects, 0f);
		}

		public DrawData ToDrawData() {
			DrawData data = new DrawData(Texture, Position, null, Color, Rotation, Origin, 1f, Effects, 0);
			data.scale = ScaleVector;
			return data;
		}
	}

	// The titan's pose this frame.
	public struct TitanPose
	{
		public float WalkPhase;
		public float WalkAmount;   // 0 = standing, 1 = full stride
		public bool Airborne;
		public bool Aiming;
		public Vector2 Aim;        // aim direction in facing-right space
		public bool HoldUp;        // using an item held overhead
		public float Time;         // for pulsing glows
	}

	// Draws the Shift Titan as a rig of separate parts (it's far too big for a normal sprite sheet), so the legs can
	// stride, the weapon arm can aim, parts can be recoloured and heads, cores, weapons and fusion decorations swapped.
	// Art is authored facing right in "art pixels" and drawn 4x at full size. Every part has four layers:
	// _M (tinted with the main colour), _A (tinted with the accent colour), _D (untinted details and outlines)
	// and _G (full-bright glow, tinted with the core colour).
	public static class TitanRenderer
	{
		public const float FullScale = 4f;
		private const string Path = "RobotJack/Content/Titan/";

		private static readonly Dictionary<string, Asset<Texture2D>> cache = new();

		public static Texture2D Tex(string name) {
			if (!cache.TryGetValue(name, out Asset<Texture2D> asset)) {
				asset = ModContent.Request<Texture2D>(Path + name, AssetRequestMode.ImmediateLoad);
				cache[name] = asset;
			}
			return asset.Value;
		}

		public static void Unload() => cache.Clear();

		// Part pivots and attachment points, in art pixels within each part's texture.
		private static readonly Vector2 TorsoPivot = new(17, 31), NeckOnTorso = new(17, 1), CoreOnTorso = new(17, 13);
		private static readonly Vector2 BackShoulder = new(6, 6), FrontShoulder = new(28, 6), BackpackOnTorso = new(4, 6);
		private static readonly Vector2 PelvisPivot = new(13, 3), HeadPivot = new(11, 19), BackpackPivot = new(14, 2);
		private static readonly Vector2 UpperArmPivot = new(5, 3), ElbowOnUpperArm = new(5, 20);
		private static readonly Vector2 ThighPivot = new(6, 2), KneeOnThigh = new(6, 24), ShinPivot = new(6, 2), AnkleOnShin = new(6, 26), FootPivot = new(6, 2);
		private static readonly Vector2 HeadDecorPivot = new(20, 29), BackDecorPivot = new(30, 20), BackDecorOnTorso = new(9, 8);
		private const float HipHeight = 52f; // art pixels from the feet up to the hips

		public static readonly string[] WeaponParts = { "ArmCannon", "ArmBlade", "ArmClaw" };
		public static readonly Vector2[] WeaponPivots = { new(7, 3), new(6, 3), new(8, 3) };
		public static readonly Vector2[] WeaponTips = { new(7, 31), new(6, 43), new(8, 27) };

		// ------------------------------------------------------------------ world positions (for abilities)

		// How big a player's titan is drawn: 4x at full size, smaller while there's no room to grow.
		public static float ScaleFor(Player player, bool car) =>
			car ? FullScale * player.width / TitanData.CarWidth : FullScale * player.height / TitanData.TitanHeight;

		// The core in the middle of the titan's chest, in world space.
		public static Vector2 CoreWorld(Player player) {
			float scale = ScaleFor(player, false);
			return player.Bottom - new Vector2(0f, (HipHeight + TorsoPivot.Y - CoreOnTorso.Y) * scale);
		}

		// The weapon arm's shoulder, in world space.
		public static Vector2 ShoulderWorld(Player player) {
			float scale = ScaleFor(player, false);
			Vector2 local = FrontShoulder - TorsoPivot;
			return player.Bottom + new Vector2(local.X * player.direction, local.Y - HipHeight) * scale;
		}

		// The tip of the weapon arm when it's aimed along aim, in world space.
		public static Vector2 MuzzleWorld(Player player, Vector2 aim, int weapon) {
			float scale = ScaleFor(player, false);
			float reach = (ElbowOnUpperArm.Y - UpperArmPivot.Y) + (WeaponTips[weapon].Y - WeaponPivots[weapon].Y);
			return ShoulderWorld(player) + aim.SafeNormalize(Vector2.UnitX * player.direction) * reach * scale;
		}

		// The shoulder-mounted missile pods on the backpack, in world space.
		public static Vector2 BackpackWorld(Player player) {
			float scale = ScaleFor(player, false);
			Vector2 local = BackpackOnTorso - TorsoPivot;
			return player.Bottom + new Vector2(local.X * player.direction, local.Y - HipHeight) * scale;
		}

		// ------------------------------------------------------------------ titan

		private struct Ctx
		{
			public List<TitanSprite> List;
			public int Dir;
			public float Scale;
			public Color Light, Main, Accent, Glow;
		}

		// Local (facing right, art px) offset -> world offset.
		private static Vector2 W(Ctx c, Vector2 local) => new Vector2(local.X * c.Dir, local.Y) * c.Scale;

		// Where a child attaches: the parent's joint plus the attachment point rotated with the parent.
		private static Vector2 Child(Ctx c, Vector2 parentJoint, Vector2 parentPivot, float parentRot, Vector2 attach) =>
			parentJoint + W(c, (attach - parentPivot).RotatedBy(parentRot));

		private static void Add(Ctx c, Texture2D tex, Vector2 joint, Vector2 pivot, float rot, Color color) {
			if (tex == null) {
				return;
			}
			c.List.Add(new TitanSprite {
				Texture = tex,
				Position = joint,
				Color = color,
				Rotation = rot * c.Dir,
				Origin = c.Dir == 1 ? pivot : new Vector2(tex.Width - pivot.X, pivot.Y),
				Scale = c.Scale,
				Effects = c.Dir == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally,
			});
		}

		// All four layers of a part.
		private static void Part(Ctx c, string name, Vector2 joint, Vector2 pivot, float rot) {
			Add(c, Tex(name + "_M"), joint, pivot, rot, Multiply(c.Main, c.Light));
			Add(c, Tex(name + "_A"), joint, pivot, rot, Multiply(c.Accent, c.Light));
			Add(c, Tex(name + "_D"), joint, pivot, rot, c.Light);
			Add(c, Tex(name + "_G"), joint, pivot, rot, c.Glow);
		}

		private static Color Multiply(Color a, Color b) => new Color(a.R * b.R / 255, a.G * b.G / 255, a.B * b.B / 255, b.A);

		public static Color GlowColor(TitanLook look) => TitanData.Colors[look.CoreColor];

		// feet = bottom-middle of the titan in screen space.
		public static void BuildTitan(List<TitanSprite> list, TitanPose pose, TitanLook look, TitanFusion fusion, Vector2 feet, int dir, float scale, Color light) {
			Ctx c = new Ctx {
				List = list, Dir = dir, Scale = scale, Light = light,
				Main = TitanData.Colors[look.MainColor], Accent = TitanData.Colors[look.AccentColor], Glow = GlowColor(look),
			};

			// Legs: a stride that follows movement, or tucked in the air.
			float swing = (float)System.Math.Sin(pose.WalkPhase) * 0.55f * pose.WalkAmount;
			float frontThigh, backThigh, frontKnee, backKnee;
			if (pose.Airborne) {
				frontThigh = -0.7f; frontKnee = 0.9f; backThigh = 0.35f; backKnee = 0.6f;
			}
			else {
				frontThigh = -swing; backThigh = swing;
				frontKnee = 0.15f + 0.45f * System.Math.Max(0f, (float)System.Math.Cos(pose.WalkPhase)) * pose.WalkAmount;
				backKnee = 0.15f + 0.45f * System.Math.Max(0f, -(float)System.Math.Cos(pose.WalkPhase)) * pose.WalkAmount;
			}
			float bob = pose.Airborne ? 0f : System.Math.Abs((float)System.Math.Sin(pose.WalkPhase)) * 2f * pose.WalkAmount;
			Vector2 hip = feet - new Vector2(0f, (HipHeight - bob) * scale);

			// Arms: swing opposite the legs, the front (weapon) arm aims while attacking.
			float backArm = -frontThigh * 0.7f;
			float frontArm = frontThigh * 0.7f;
			float frontForearm = 0.2f;
			if (pose.HoldUp) {
				frontArm = -2.6f;
				frontForearm = 0f;
			}
			else if (pose.Aiming) {
				frontArm = pose.Aim.ToRotation() - MathHelper.PiOver2;
				frontForearm = 0f;
			}

			// Draw back to front.
			DrawDecor(c, "DecorBack", fusion, Child(c, hip, TorsoPivot, 0f, BackDecorOnTorso), BackDecorPivot, 0f);
			DrawLimb(c, hip + W(c, new Vector2(-4, 0)), backThigh, backKnee, back: true);
			Vector2 backShoulder = Child(c, hip, TorsoPivot, 0f, BackShoulder);
			Part(c, "UpperArm", backShoulder, UpperArmPivot, backArm);
			Part(c, "Forearm", Child(c, backShoulder, UpperArmPivot, backArm, ElbowOnUpperArm), UpperArmPivot, backArm + 0.2f);
			Part(c, "Backpack", Child(c, hip, TorsoPivot, 0f, BackpackOnTorso), BackpackPivot, 0f);
			Part(c, "Pelvis", hip, PelvisPivot, 0f);
			Part(c, "Torso", hip, TorsoPivot, 0f);
			DrawCore(c, look, Child(c, hip, TorsoPivot, 0f, CoreOnTorso), pose.Time, 1f);
			Vector2 neck = Child(c, hip, TorsoPivot, 0f, NeckOnTorso);
			Part(c, "Head" + look.HeadStyle, neck, HeadPivot, 0f);
			DrawDecor(c, "DecorHead", fusion, neck, HeadDecorPivot, 0f);
			DrawLimb(c, hip + W(c, new Vector2(4, 0)), frontThigh, frontKnee, back: false);
			Vector2 frontShoulder = Child(c, hip, TorsoPivot, 0f, FrontShoulder);
			Part(c, "UpperArm", frontShoulder, UpperArmPivot, frontArm);
			int weapon = look.WeaponArm;
			Part(c, WeaponParts[weapon], Child(c, frontShoulder, UpperArmPivot, frontArm, ElbowOnUpperArm), WeaponPivots[weapon], frontArm + frontForearm);
		}

		private static void DrawLimb(Ctx c, Vector2 hipJoint, float thigh, float knee, bool back) {
			Ctx limb = c;
			if (back) {
				// The far leg is a little darker.
				limb.Light = new Color((int)(c.Light.R * 0.75f), (int)(c.Light.G * 0.75f), (int)(c.Light.B * 0.75f), c.Light.A);
			}
			Part(limb, "Thigh", hipJoint, ThighPivot, thigh);
			Vector2 kneeJoint = Child(limb, hipJoint, ThighPivot, thigh, KneeOnThigh);
			Part(limb, "Shin", kneeJoint, ShinPivot, thigh + knee);
			Vector2 ankle = Child(limb, kneeJoint, ShinPivot, thigh + knee, AnkleOnShin);
			Part(limb, "Foot", ankle, FootPivot, 0f);
		}

		// Fusion decorations are drawn in their own colours (lit) plus a full-bright glow layer.
		private static void DrawDecor(Ctx c, string slot, TitanFusion fusion, Vector2 joint, Vector2 pivot, float rot) {
			string name = slot + fusion;
			Add(c, Tex(name + "_D"), joint, pivot, rot, c.Light);
			Color glow = fusion == TitanFusion.Omega ? Main.DiscoColor : Elements.Main(TitanData.Element(fusion));
			Add(c, Tex(name + "_G"), joint, pivot, rot, glow);
		}

		// The glowing core, like a titan's heart: a pulsing halo and the chosen shape, full-bright in the core colour.
		private static void DrawCore(Ctx c, TitanLook look, Vector2 at, float time, float size) {
			Color core = GlowColor(look);
			float pulse = 1f + 0.12f * (float)System.Math.Sin(time * 0.12f);
			Texture2D glow = ElementFX.Glow;
			c.List.Add(new TitanSprite {
				Texture = glow, Position = at, Color = ElementFX.Additive(core, 0.7f), Origin = glow.Size() / 2f,
				Scale = c.Scale * 0.22f * pulse * size, Effects = SpriteEffects.None,
			});
			Texture2D shape = Tex("Core" + look.CoreShape);
			c.List.Add(new TitanSprite {
				Texture = shape, Position = at, Color = core, Origin = shape.Size() / 2f,
				Scale = c.Scale * size, Effects = SpriteEffects.None,
			});
			c.List.Add(new TitanSprite {
				Texture = glow, Position = at, Color = ElementFX.Additive(Color.White, 0.6f), Origin = glow.Size() / 2f,
				Scale = c.Scale * 0.06f * pulse * size, Effects = SpriteEffects.None,
			});
		}

		// ------------------------------------------------------------------ car

		private static readonly Vector2 CarPivot = new(50, 40);
		private static readonly Vector2[] Wheels = { new(21, 32), new(79, 32) };
		private static readonly Vector2 WheelPivot = new(9, 9), Headlight = new(95, 21);

		// bottom = bottom-middle of the car in screen space.
		public static void BuildCar(List<TitanSprite> list, TitanLook look, TitanFusion fusion, Vector2 bottom, int dir, float scale, Color light, float wheelAngle, float time) {
			Ctx c = new Ctx {
				List = list, Dir = dir, Scale = scale, Light = light,
				Main = TitanData.Colors[look.MainColor], Accent = TitanData.Colors[look.AccentColor], Glow = GlowColor(look),
			};
			Part(c, "CarBody", bottom, CarPivot, 0f);
			foreach (Vector2 wheel in Wheels) {
				Add(c, Tex("Wheel_D"), bottom + W(c, wheel - CarPivot), WheelPivot, wheelAngle * dir, light);
			}
			// The core becomes the headlight, with a beam of light ahead.
			Vector2 lamp = bottom + W(c, Headlight - CarPivot);
			Color core = GlowColor(look);
			ElementFX.LineTo(list, lamp, lamp + new Vector2(dir * 90f * scale, 6f * scale), 14f * scale, ElementFX.Additive(core, 0.25f));
			DrawCore(c, look, lamp, time, 0.7f);
		}
	}
}
