using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Content.Abilities;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace RobotJack.Common
{
	// Shows the Energy Absorber's stored energy as a bar above the local player while it has energy
	// or is holding the Energy Absorber.
	public class EnergyBarSystem : ModSystem
	{
		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (index < 0) {
				return;
			}
			// Game scale, so world positions line up with the zoomed view.
			layers.Insert(index, new LegacyGameInterfaceLayer("RobotJack: Energy Bar", DrawBar, InterfaceScaleType.Game));
		}

		private static bool DrawBar() {
			Player player = Main.LocalPlayer;
			if (player.dead || !player.active) {
				return true;
			}
			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();
			bool holding = player.HeldItem.ModItem is EnergyAbsorber;
			if (modPlayer.energy < 1f && !holding) {
				return true;
			}

			const int width = 60;
			const int height = 6;
			Vector2 pos = player.Top - Main.screenPosition + new Vector2(-width / 2f, -30f);
			Rectangle back = new Rectangle((int)pos.X - 1, (int)pos.Y - 1, width + 2, height + 2);
			float fill = modPlayer.energy / RobotJackPlayer.MaxEnergy;

			Texture2D pixel = TextureAssets.MagicPixel.Value;
			Main.spriteBatch.Draw(pixel, back, Color.Black * 0.7f);
			Main.spriteBatch.Draw(pixel, new Rectangle((int)pos.X, (int)pos.Y, (int)(width * fill), height),
				Color.Lerp(new Color(150, 90, 255), new Color(120, 235, 255), fill));

			string text = $"{(int)modPlayer.energy} (x{RobotJackPlayer.BlastMultiplier} = {(int)modPlayer.energy * RobotJackPlayer.BlastMultiplier})";
			Vector2 size = FontAssets.MouseText.Value.MeasureString(text) * 0.7f;
			Utils.DrawBorderString(Main.spriteBatch, text, pos + new Vector2(width / 2f - size.X / 2f, -18f), Color.White, 0.7f);
			return true;
		}
	}
}
