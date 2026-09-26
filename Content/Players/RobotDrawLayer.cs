using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace RobotJack.Content.Players
{
	// Draws the current form (Robot Jack or one of his variants) in place of the normal player body.
	// The sheets use the same 20-frame layout as the vanilla player sheet (40x56 per frame), so the game's own
	// legFrame and bodyFrame pick the pose (walking, jumping, aiming the arm while using an item).
	// RobotJackPlayer.HideDrawLayers hides the vanilla body layers at the same time.
	public class RobotDrawLayer : PlayerDrawLayer
	{
		private const string Path = "RobotJack/Content/Players/";
		private const int FrameWidth = 40;
		private const int FrameHeight = 56;

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			return !player.dead && player.GetModPlayer<RobotJackPlayer>().Transformed;
		}

		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Torso, PlayerDrawLayers.OffhandAcc);

		// Sprite sheet name prefix for each form.
		private static string SheetName(RobotFormType form) => form switch {
			RobotFormType.BlazeJack => "BlazeJack",
			RobotFormType.FrostJack => "FrostJack",
			RobotFormType.VoltJack => "VoltJack",
			RobotFormType.ShadowJack => "ShadowJack",
			RobotFormType.NovaJack => "NovaJack",
			RobotFormType.OmegaJack => "OmegaJack",
			RobotFormType.GodJack => "GodJack",
			_ => "Robot",
		};

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			RobotFormType form = player.GetModPlayer<RobotJackPlayer>().ActiveForm;
			string sheet = SheetName(form);

			Point tile = (drawInfo.Center / 16f).ToPoint();
			Color light = Lighting.GetColor(tile.X, tile.Y) * (1f - drawInfo.shadow);
			Color glow = Color.White * (1f - drawInfo.shadow);

			// Glowing afterimages (in the form's colour) when moving fast.
			if (drawInfo.shadow == 0f && player.velocity.Length() > 7f) {
				DrawAfterimages(ref drawInfo, player, sheet, form.GlowColor());
			}

			// Legs first, then body on top, each with a full-bright glow pass (visor, core, jets).
			DrawPart(ref drawInfo, sheet + "Legs", player.legFrame, player.legPosition, player.legRotation, light);
			DrawPart(ref drawInfo, sheet + "Legs_Glow", player.legFrame, player.legPosition, player.legRotation, glow);
			DrawPart(ref drawInfo, sheet + "Body", player.bodyFrame, player.bodyPosition, player.bodyRotation, light);
			DrawPart(ref drawInfo, sheet + "Body_Glow", player.bodyFrame, player.bodyPosition, player.bodyRotation, glow);

			// Charging Gods Wrath: the whole robot glows brighter and brighter gold.
			float wrath = player.GetModPlayer<RobotJackPlayer>().wrathGlow;
			if (wrath > 0f && drawInfo.shadow == 0f) {
				Color gold = new Color(255, 215, 90, 0) * (0.9f * wrath);
				DrawPart(ref drawInfo, sheet + "Legs", player.legFrame, player.legPosition, player.legRotation, gold);
				DrawPart(ref drawInfo, sheet + "Body", player.bodyFrame, player.bodyPosition, player.bodyRotation, gold);
			}
		}

		// Faded copies of the robot at its last few positions (additive, so they glow).
		private static void DrawAfterimages(ref PlayerDrawSet drawInfo, Player player, string sheet, Color glowColor) {
			Vector2[] trail = player.GetModPlayer<RobotJackPlayer>().trailPositions;
			for (int i = trail.Length - 1; i >= 1; i--) {
				if (trail[i] == Vector2.Zero) {
					continue;
				}
				Vector2 shift = trail[i] - player.position;
				if (shift.Length() < 4f || shift.Length() > 300f) {
					continue; // too close to see, or a teleport
				}
				float fade = 1f - i / (float)trail.Length;
				Color color = new Color(glowColor.R, glowColor.G, glowColor.B, 0) * (0.45f * fade);
				DrawPart(ref drawInfo, sheet + "Legs", player.legFrame, player.legPosition, player.legRotation, color, shift);
				DrawPart(ref drawInfo, sheet + "Body", player.bodyFrame, player.bodyPosition, player.bodyRotation, color, shift);
			}
		}

		// Same placement vanilla uses for the player's body and legs:
		// the frame is centred on the hitbox and its bottom sits 4 px below the feet.
		private static void DrawPart(ref PlayerDrawSet drawInfo, string texture, Rectangle frame, Vector2 offset, float rotation, Color color, Vector2 shift = default) {
			Player player = drawInfo.drawPlayer;
			Texture2D tex = ModContent.Request<Texture2D>(Path + texture).Value;

			int frameIndex = frame.Height > 0 ? frame.Y / frame.Height : 0;
			frameIndex = System.Math.Clamp(frameIndex, 0, 19);
			Rectangle source = new Rectangle(0, frameIndex * FrameHeight, FrameWidth, FrameHeight);

			Vector2 half = new Vector2(FrameWidth / 2, FrameHeight / 2);
			Vector2 position = new Vector2(
				(int)(drawInfo.Position.X - Main.screenPosition.X - FrameWidth / 2 + player.width / 2),
				(int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - FrameHeight + 4))
				+ offset + half + shift;

			drawInfo.DrawDataCache.Add(new DrawData(tex, position, source, color, rotation, half, 1f, drawInfo.playerEffect, 0));
		}
	}
}
