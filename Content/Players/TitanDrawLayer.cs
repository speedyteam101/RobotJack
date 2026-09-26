using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Common.Titan;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Players
{
	// Draws the Shift Titan (or its car) in place of the player. The titan is built from separate parts by
	// TitanRenderer, posed from the player's movement and aimed along the item they're using.
	public class TitanDrawLayer : PlayerDrawLayer
	{
		private static readonly List<TitanSprite> sprites = new();

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			return !player.dead && player.GetModPlayer<RobotJackPlayer>().ActiveForm == RobotFormType.ShiftTitan;
		}

		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Torso, PlayerDrawLayers.OffhandAcc);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			TitanPlayer titan = player.GetModPlayer<TitanPlayer>();
			Vector2 feet = new Vector2(drawInfo.Position.X + player.width / 2f, drawInfo.Position.Y + player.height) - Main.screenPosition;
			int dir = player.direction;
			Color light = SampleLight(player) * (1f - drawInfo.shadow);

			sprites.Clear();
			bool car = titan.carMode && player.width >= TitanData.CarWidth;
			if (car) {
				TitanRenderer.BuildCar(sprites, titan.look, titan.fusion, feet, dir, TitanRenderer.ScaleFor(player, true), light, titan.wheelAngle, Main.GameUpdateCount);
			}
			else {
				TitanPose pose = new TitanPose {
					WalkPhase = titan.walkPhase,
					WalkAmount = MathHelper.Clamp(System.Math.Abs(player.velocity.X) / 3f, 0f, 1f),
					Airborne = player.velocity.Y != 0f,
					Time = Main.GameUpdateCount,
				};
				Item held = player.HeldItem;
				if (player.itemAnimation > 0 && !held.IsAir) {
					if (held.useStyle == ItemUseStyleID.Shoot) {
						// itemRotation is already in facing-right space for this use style.
						pose.Aiming = true;
						pose.Aim = player.itemRotation.ToRotationVector2();
					}
					else if (held.useStyle == ItemUseStyleID.HoldUp) {
						pose.HoldUp = true;
					}
				}
				TitanRenderer.BuildTitan(sprites, pose, titan.look, titan.fusion, feet, dir, TitanRenderer.ScaleFor(player, false), light);
			}

			float fade = 1f - drawInfo.shadow;
			foreach (TitanSprite sprite in sprites) {
				TitanSprite s = sprite;
				s.Color *= fade;
				drawInfo.DrawDataCache.Add(s.ToDrawData());
			}
		}

		// The titan is 26 blocks tall, so average the light at its feet, middle and head.
		private static Color SampleLight(Player player) {
			Vector3 sum = Vector3.Zero;
			foreach (Vector2 at in new[] { player.Bottom - new Vector2(0f, 8f), player.Center, player.Top + new Vector2(0f, 8f) }) {
				Point tile = (at / 16f).ToPoint();
				sum += Lighting.GetColor(tile.X, tile.Y).ToVector3();
			}
			return new Color(sum / 3f);
		}
	}
}
