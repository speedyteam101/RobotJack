using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.UI;

namespace RobotJack.Common
{
	// Client-side presentation for God Jack's Gods Wrath: the slow camera zoom-out while charging,
	// and the giant ring of judgement (also drawn on the world map, see GodsWrathMapLayer).
	public class GodsWrathSystem : ModSystem
	{
		// How far the camera can zoom out: the game only draws the world a little past the normal screen,
		// so going much further would show unrendered black edges.
		public const float MaxZoomOut = 0.8f;

		// Set every tick by the local player's charging Gods Wrath; they fade out on their own when it stops.
		private static float targetZoom = 1f;
		private static float zoom = 1f;
		private static int ringTicks;
		public static Vector2 RingCenter;
		public static float RingRadius;
		public static float RingCharge; // 0..1

		public static bool RingVisible => ringTicks > 0;

		// The ring encloses at least half the world: its diameter is half the world's width.
		public static float WorldRingRadius => System.Math.Max(Main.maxTilesX * 16f / 4f, 3000f);

		public static void SetCharge(Vector2 center, float charge) {
			targetZoom = MathHelper.Lerp(1f, MaxZoomOut, charge);
			RingCenter = center;
			RingRadius = WorldRingRadius;
			RingCharge = charge;
			ringTicks = 3;
		}

		public override void PostUpdateEverything() {
			if (ringTicks > 0) {
				ringTicks--;
			}
			else {
				targetZoom = 1f;
			}
			// Slowly toward the target: out while charging, back in afterwards.
			zoom = MathHelper.Lerp(zoom, targetZoom, 0.03f);
		}

		public override void ModifyTransformMatrix(ref SpriteViewMatrix Transform) {
			if (zoom < 0.999f && !Main.gameMenu) {
				Transform.Zoom *= zoom;
			}
		}

		public override void OnWorldUnload() {
			zoom = targetZoom = 1f;
			ringTicks = 0;
		}
	}

	// Draws the ring of judgement on the full-screen map and minimap while Gods Wrath is charging,
	// so you can see how much of the world it covers.
	public class GodsWrathMapLayer : ModMapLayer
	{
		public override Position GetDefaultPosition() => new Before(IMapLayer.Pings);

		public override void Draw(ref MapOverlayDrawContext context, ref string text) {
			if (!GodsWrathSystem.RingVisible) {
				return;
			}
			Texture2D dot = ElementFX.Glow;
			Vector2 centerTiles = GodsWrathSystem.RingCenter / 16f;
			float radiusTiles = GodsWrathSystem.RingRadius / 16f;
			float pulse = 0.8f + 0.2f * (float)System.Math.Sin(Main.GameUpdateCount * 0.2f);
			Color color = new Color(255, 215, 90) * (0.5f + 0.5f * GodsWrathSystem.RingCharge) * pulse;
			const int dots = 240;
			for (int i = 0; i < dots; i++) {
				float angle = MathHelper.TwoPi * i / dots;
				Vector2 pos = centerTiles + angle.ToRotationVector2() * radiusTiles;
				context.Draw(dot, pos, color, new SpriteFrame(1, 1, 0, 0), 0.12f, 0.12f, Alignment.Center);
			}
			// And a bright mark where the sun is.
			context.Draw(dot, centerTiles, color, new SpriteFrame(1, 1, 0, 0), 0.3f, 0.3f, Alignment.Center);
		}
	}
}
