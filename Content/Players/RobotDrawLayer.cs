using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace RobotJack.Content.Players
{
	// Draws the current form (Robot Jack or a Titan) in place of the normal player body.
	// The sheets use the same 20-frame layout as the vanilla player sheet, so the game's own
	// legFrame and bodyFrame pick the pose (walking, jumping, aiming the arm while using an item).
	// Robot Jack's frames are 40x56 like the vanilla sheet; the Titans' are twice that (80x112).
	// RobotJackPlayer.HideDrawLayers hides the vanilla body layers at the same time.
	public class RobotDrawLayer : PlayerDrawLayer
	{
		private const string Path = "RobotJack/Content/Players/";

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			return !player.dead && player.GetModPlayer<RobotJackPlayer>().Transformed;
		}

		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Torso, PlayerDrawLayers.OffhandAcc);

		// Sprite sheet name prefix for each form.
		private static string SheetName(RobotFormType form) => form switch {
			RobotFormType.TitanSpeaker => "TitanSpeaker",
			RobotFormType.TitanCamera => "TitanCamera",
			RobotFormType.TitanTV => "TitanTV",
			_ => "Robot",
		};

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			RobotFormType form = player.GetModPlayer<RobotJackPlayer>().ActiveForm;
			string sheet = SheetName(form);
			int scale = form.IsTitan() ? 2 : 1;

			Point tile = (drawInfo.Center / 16f).ToPoint();
			Color light = Lighting.GetColor(tile.X, tile.Y) * (1f - drawInfo.shadow);
			Color glow = Color.White * (1f - drawInfo.shadow);

			// Legs first, then body on top, each with a full-bright glow pass (visor, cores, screens, jets).
			DrawPart(ref drawInfo, sheet + "Legs", scale, player.legFrame, player.legPosition, player.legRotation, light);
			DrawPart(ref drawInfo, sheet + "Legs_Glow", scale, player.legFrame, player.legPosition, player.legRotation, glow);
			DrawPart(ref drawInfo, sheet + "Body", scale, player.bodyFrame, player.bodyPosition, player.bodyRotation, light);
			DrawPart(ref drawInfo, sheet + "Body_Glow", scale, player.bodyFrame, player.bodyPosition, player.bodyRotation, glow);
		}

		// Same placement vanilla uses for the player's body and legs, scaled up for the Titans:
		// the frame is centred on the hitbox and its bottom sits 4 px (8 for Titans) below the feet.
		private static void DrawPart(ref PlayerDrawSet drawInfo, string texture, int scale, Rectangle frame, Vector2 offset, float rotation, Color color) {
			Player player = drawInfo.drawPlayer;
			Texture2D tex = ModContent.Request<Texture2D>(Path + texture).Value;

			int frameWidth = 40 * scale;
			int frameHeight = 56 * scale;
			int frameIndex = frame.Height > 0 ? frame.Y / frame.Height : 0;
			frameIndex = System.Math.Clamp(frameIndex, 0, 19);
			Rectangle source = new Rectangle(0, frameIndex * frameHeight, frameWidth, frameHeight);

			Vector2 half = new Vector2(frameWidth / 2, frameHeight / 2);
			Vector2 position = new Vector2(
				(int)(drawInfo.Position.X - Main.screenPosition.X - frameWidth / 2 + player.width / 2),
				(int)(drawInfo.Position.Y - Main.screenPosition.Y + player.height - frameHeight + 4 * scale))
				+ offset * scale + half;

			drawInfo.DrawDataCache.Add(new DrawData(tex, position, source, color, rotation, half, 1f, drawInfo.playerEffect, 0));
		}
	}
}
