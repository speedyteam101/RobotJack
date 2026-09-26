using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Content.Items;
using RobotJack.Content.Tiles;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Generation;
using Terraria.ID;
using Terraria.IO;
using Terraria.Map;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;
using Terraria.Utilities;
using Terraria.WorldBuilding;

namespace RobotJack.Common.Titan
{
	// The laboratory: a big steel hall hidden in the caverns, tall enough for the Shift Titan to stand up in, with a
	// shaft and rope down from the surface. Its gold chest holds the Shift Trigger. It's generated with new worlds,
	// and added to existing worlds the first time they're loaded with this mod. It's marked on the map.
	public class LabSystem : ModSystem
	{
		// Size in tiles, including the 3-tile-thick shell.
		public const int Width = 84;
		public const int Height = 36;
		private const int Shell = 3;

		public static bool generated;
		public static Point labPosition; // top-left tile; (0, 0) = no lab

		public static bool HasLab => labPosition != Point.Zero;

		// The middle of the lab's floor, in tiles (for the map marker).
		public static Vector2 LabCenterTiles => new Vector2(labPosition.X + Width / 2f, labPosition.Y + Height / 2f);

		public override void ClearWorld() {
			generated = false;
			labPosition = Point.Zero;
		}

		public override void SaveWorldData(TagCompound tag) {
			tag["labGenerated"] = generated;
			tag["labX"] = labPosition.X;
			tag["labY"] = labPosition.Y;
		}

		public override void LoadWorldData(TagCompound tag) {
			generated = tag.GetBool("labGenerated");
			labPosition = new Point(tag.GetInt("labX"), tag.GetInt("labY"));
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write(generated);
			writer.Write(labPosition.X);
			writer.Write(labPosition.Y);
		}

		public override void NetReceive(BinaryReader reader) {
			generated = reader.ReadBoolean();
			labPosition = new Point(reader.ReadInt32(), reader.ReadInt32());
		}

		public override void ModifyWorldGenTasks(List<GenPass> tasks, ref double totalWeight) {
			int index = tasks.FindIndex(pass => pass.Name.Equals("Final Cleanup"));
			PassLegacy pass = new PassLegacy("Robot Jack Laboratory", (progress, configuration) => {
				progress.Message = "Building a secret laboratory";
				Generate(WorldGen.genRand);
			});
			if (index != -1) {
				tasks.Insert(index, pass);
			}
			else {
				tasks.Add(pass);
			}
		}

		// Worlds made before this mod had the lab get one the first time they're played.
		public override void PostUpdateWorld() {
			if (generated || Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}
			if (Generate(WorldGen.genRand) && Main.netMode == NetmodeID.Server) {
				SendArea(labPosition.X, labPosition.Y - ShaftLength, Width, Height + ShaftLength);
				NetMessage.SendData(MessageID.WorldData);
			}
		}

		private static int ShaftLength;

		// Sends a changed area to every client, in pieces small enough for tile square packets.
		private static void SendArea(int x, int y, int width, int height) {
			const int chunk = 16;
			for (int cx = x; cx < x + width; cx += chunk) {
				for (int cy = y; cy < y + height; cy += chunk) {
					NetMessage.SendTileSquare(-1, cx, cy, System.Math.Min(chunk, x + width - cx), System.Math.Min(chunk, y + height - cy));
				}
			}
		}

		// ------------------------------------------------------------------ generation

		private static bool Generate(UnifiedRandom rand) {
			generated = true; // only ever try once per world
			int yMin = (int)Main.rockLayer + 20;
			int yMax = System.Math.Min(Main.maxTilesY - 260 - Height, (int)Main.rockLayer + 260);
			if (yMax <= yMin) {
				return false;
			}
			for (int attempt = 0; attempt < 3000; attempt++) {
				int x = rand.Next(200, Main.maxTilesX - 200 - Width);
				if (System.Math.Abs(x + Width / 2 - Main.spawnTileX) < 200) {
					continue; // not right under spawn
				}
				int y = rand.Next(yMin, yMax);
				if (!AreaIsFree(x - 4, y - 4, Width + 8, Height + 8)) {
					continue;
				}
				Build(x, y, rand);
				labPosition = new Point(x, y);
				return true;
			}
			return false;
		}

		// Keeps the lab out of the dungeon, the jungle temple and any chests (clearing a chest's tiles would break it).
		private static bool AreaIsFree(int x, int y, int width, int height) {
			for (int i = x; i < x + width; i++) {
				for (int j = y; j < y + height; j++) {
					if (!WorldGen.InWorld(i, j, 10)) {
						return false;
					}
					Tile tile = Main.tile[i, j];
					if (!tile.HasTile) {
						continue;
					}
					ushort type = tile.TileType;
					if (type == TileID.BlueDungeonBrick || type == TileID.GreenDungeonBrick || type == TileID.PinkDungeonBrick
						|| type == TileID.LihzahrdBrick || type == TileID.Containers || type == TileID.Containers2
						|| type == ModContent.TileType<LabPlating>()) {
						return false;
					}
				}
			}
			return true;
		}

		private static void Build(int x, int y, UnifiedRandom rand) {
			int plating = ModContent.TileType<LabPlating>();
			int lightTile = ModContent.TileType<LabLight>();
			int wall = ModContent.WallType<LabWall>();
			int tank = ModContent.WallType<LabTankWall>();

			// Hollow steel box with a panelled wall inside.
			for (int i = x; i < x + Width; i++) {
				for (int j = y; j < y + Height; j++) {
					Main.tile[i, j].ClearEverything();
					bool shell = i < x + Shell || i >= x + Width - Shell || j < y + Shell || j >= y + Height - Shell;
					if (shell) {
						WorldGen.PlaceTile(i, j, plating, mute: true, forced: true);
					}
					if (i > x && i < x + Width - 1 && j > y && j < y + Height - 1) {
						WorldGen.PlaceWall(i, j, wall, true);
					}
				}
			}

			// Light strips along the ceiling and floor edges.
			int ceiling = y + Shell - 1;
			int floor = y + Height - Shell; // first floor row
			for (int i = x + 6; i < x + Width - 6; i += 8) {
				SetTile(i, ceiling, lightTile);
				SetTile(i + 1, ceiling, lightTile);
			}

			// Glowing specimen tanks along the back wall, each on a lit plinth. The middle is left clear for the chest.
			foreach (int offset in new[] { 8, 20, Width - 26, Width - 14 }) {
				int left = x + offset;
				for (int i = left; i < left + 6; i++) {
					for (int j = floor - 14; j < floor - 1; j++) {
						Tile tile = Main.tile[i, j];
						tile.WallType = (ushort)tank;
					}
				}
				for (int i = left - 1; i < left + 7; i++) {
					SetTile(i, floor - 1, lightTile);
				}
			}

			// Shelves of lab equipment on the side walls.
			foreach (int side in new[] { x + Shell, x + Width - Shell - 6 }) {
				for (int level = 0; level < 2; level++) {
					int shelfY = floor - 18 - level * 7;
					for (int i = side; i < side + 6; i++) {
						WorldGen.PlaceTile(i, shelfY, TileID.Platforms, mute: true, forced: true);
					}
					for (int i = side + 1; i < side + 5; i += 2) {
						WorldGen.PlaceTile(i, shelfY - 1, TileID.Bottles, mute: true, style: rand.Next(3));
					}
				}
			}

			// A work table and chair, and a raised pedestal for the chest in the middle.
			WorldGen.PlaceTile(x + 31, floor - 1, TileID.Tables, mute: true);
			WorldGen.PlaceTile(x + 34, floor - 1, TileID.Chairs, mute: true);
			int middle = x + Width / 2;
			for (int i = middle - 3; i <= middle + 3; i++) {
				SetTile(i, floor - 1, lightTile);
			}
			PlaceTriggerChest(middle - 1, floor - 2);

			// A shaft up to the surface with a rope, so the lab can be found and reached.
			DigShaft(middle + 8, y);

			// Re-frame the edges so the blocks around the lab join up with it.
			for (int i = x - 1; i <= x + Width; i++) {
				WorldGen.SquareTileFrame(i, y - 1);
				WorldGen.SquareTileFrame(i, y + Height);
			}
			for (int j = y - 1; j <= y + Height; j++) {
				WorldGen.SquareTileFrame(x - 1, j);
				WorldGen.SquareTileFrame(x + Width, j);
			}
		}

		private static void SetTile(int i, int j, int type) {
			Main.tile[i, j].ClearTile();
			WorldGen.PlaceTile(i, j, type, mute: true, forced: true);
		}

		private static void PlaceTriggerChest(int x, int y) {
			const int goldChestStyle = 1;
			int chestIndex = WorldGen.PlaceChest(x, y, TileID.Containers, false, goldChestStyle);
			if (chestIndex < 0) {
				return;
			}
			Chest chest = Main.chest[chestIndex];
			chest.item[0].SetDefaults(ModContent.ItemType<ShiftTrigger>());
			chest.item[1].SetDefaults(ItemID.GreaterHealingPotion);
			chest.item[1].stack = 5;
			chest.item[2].SetDefaults(ItemID.Wire);
			chest.item[2].stack = 100;
		}

		// A 4-wide shaft from the lab ceiling up to open air, walled in plating, with a rope down the middle.
		private static void DigShaft(int x, int labTop) {
			int plating = ModContent.TileType<LabPlating>();
			int wall = ModContent.WallType<LabWall>();
			int top = labTop;
			// Go up until there's open sky above the shaft (or give up at the top of the world's surface layer).
			for (int j = labTop - 1; j > 60; j--) {
				bool open = j < Main.worldSurface;
				for (int i = x - 2; i <= x + 3 && open; i++) {
					for (int k = j; k > j - 6; k--) {
						if (Main.tile[i, k].HasTile) {
							open = false;
							break;
						}
					}
				}
				top = j;
				if (open) {
					break;
				}
			}
			ShaftLength = labTop - top;

			for (int j = top; j < labTop + Shell; j++) {
				for (int i = x - 1; i <= x + 2; i++) {
					Tile tile = Main.tile[i, j];
					tile.ClearTile();
					tile.LiquidAmount = 0;
					if (j >= Main.worldSurface) {
						WorldGen.PlaceWall(i, j, wall, true);
					}
				}
				// Plating down both sides (below the surface).
				if (j > Main.worldSurface) {
					foreach (int side in new[] { x - 2, x + 3 }) {
						if (!Main.tile[side, j].HasTile) {
							WorldGen.PlaceTile(side, j, plating, mute: true, forced: true);
						}
					}
				}
				WorldGen.PlaceTile(x, j, TileID.Rope, mute: true, forced: true);
			}
			// The rope carries on down into the lab.
			for (int j = labTop + Shell; j < labTop + Height - Shell - 1; j++) {
				WorldGen.PlaceTile(x, j, TileID.Rope, mute: true, forced: true);
			}
		}
	}

	// Marks the laboratory on the full map and minimap.
	public class LabMapLayer : ModMapLayer
	{
		public override void Draw(ref MapOverlayDrawContext context, ref string text) {
			if (!LabSystem.HasLab) {
				return;
			}
			Texture2D icon = ModContent.Request<Texture2D>("RobotJack/Content/Tiles/LabMapIcon").Value;
			if (context.Draw(icon, LabSystem.LabCenterTiles, Color.White, new SpriteFrame(1, 1, 0, 0), 1f, 1.3f, Alignment.Center).IsMouseOver) {
				text = "Laboratory";
			}
		}
	}
}
