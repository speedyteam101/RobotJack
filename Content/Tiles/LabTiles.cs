using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Tiles
{
	// The laboratory's building blocks (only found in the lab; they don't drop items).

	// Steel wall plating.
	public class LabPlating : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			DustType = DustID.Silver;
			HitSound = SoundID.Tink;
			MinPick = 65;
			AddMapEntry(new Color(96, 104, 122));
		}
	}

	// Plating with a glowing light strip.
	public class LabLight : ModTile
	{
		public override void SetStaticDefaults() {
			Main.tileSolid[Type] = true;
			Main.tileBlockLight[Type] = true;
			Main.tileLighted[Type] = true;
			DustType = DustID.Electric;
			HitSound = SoundID.Tink;
			MinPick = 65;
			AddMapEntry(new Color(120, 235, 255));
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.45f;
			g = 0.9f;
			b = 1f;
		}
	}

	// Panelled wall behind everything.
	public class LabWall : ModWall
	{
		public override void SetStaticDefaults() {
			Main.wallHouse[Type] = true;
			DustType = DustID.Silver;
			AddMapEntry(new Color(44, 50, 64));
		}
	}

	// The glowing glass of the specimen tanks along the back of the lab.
	public class LabTankWall : ModWall
	{
		public override void SetStaticDefaults() {
			Main.wallHouse[Type] = true;
			Main.wallLight[Type] = true;
			DustType = DustID.Glass;
			AddMapEntry(new Color(60, 170, 170));
		}

		public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b) {
			r = 0.1f;
			g = 0.4f;
			b = 0.38f;
		}
	}
}
