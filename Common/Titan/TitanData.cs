using Microsoft.Xna.Framework;
using RobotJack.Content.Items;
using Terraria.ModLoader;

namespace RobotJack.Common.Titan
{
	// Which trigger is merged into the Shift Trigger. Each fusion gives the titan its own element, look and nine abilities.
	public enum TitanFusion : byte
	{
		None,
		Robot,
		Blaze,
		Frost,
		Volt,
		Shadow,
		Nova,
		Omega,
		God,
	}

	// How the player has customized their titan in the Shift Trigger's menu. Each value is an index into the lists below.
	public struct TitanLook
	{
		public byte MainColor;
		public byte AccentColor;
		public byte CoreColor;
		public byte CoreShape;
		public byte HeadStyle;
		public byte WeaponArm;

		public static TitanLook Default => new TitanLook { MainColor = 0, AccentColor = 5, CoreColor = 3, CoreShape = 0, HeadStyle = 0, WeaponArm = 0 };

		public byte[] ToBytes() => new[] { MainColor, AccentColor, CoreColor, CoreShape, HeadStyle, WeaponArm };

		public static TitanLook FromBytes(byte[] b) {
			if (b == null || b.Length < 6) {
				return Default;
			}
			return new TitanLook {
				MainColor = Clamp(b[0], TitanData.Colors.Length), AccentColor = Clamp(b[1], TitanData.Colors.Length),
				CoreColor = Clamp(b[2], TitanData.Colors.Length), CoreShape = Clamp(b[3], TitanData.CoreShapes.Length),
				HeadStyle = Clamp(b[4], TitanData.HeadStyles.Length), WeaponArm = Clamp(b[5], TitanData.WeaponArms.Length),
			};
		}

		private static byte Clamp(byte value, int count) => (byte)(value < count ? value : 0);

		public bool Equals(TitanLook o) => MainColor == o.MainColor && AccentColor == o.AccentColor && CoreColor == o.CoreColor
			&& CoreShape == o.CoreShape && HeadStyle == o.HeadStyle && WeaponArm == o.WeaponArm;
	}

	public static class TitanData
	{
		// Colour swatches offered in the menu (armor main, armor accent and core all pick from these).
		public static readonly Color[] Colors = {
			new Color(200, 205, 215), // steel
			new Color(60, 64, 76),    // gunmetal
			new Color(235, 235, 240), // white
			new Color(80, 230, 255),  // cyan
			new Color(60, 110, 220),  // blue
			new Color(230, 60, 60),   // red
			new Color(255, 140, 40),  // orange
			new Color(255, 215, 70),  // gold
			new Color(80, 210, 110),  // green
			new Color(160, 80, 230),  // purple
			new Color(255, 120, 200), // pink
			new Color(30, 30, 36),    // black
		};

		public static readonly string[] CoreShapes = { "Round", "Diamond", "Star", "Square" };
		public static readonly string[] HeadStyles = { "Visor", "Horned", "Dome" };
		public static readonly string[] WeaponArms = { "Cannon", "Blade", "Claw" };

		public const int WeaponCannon = 0;
		public const int WeaponBlade = 1;
		public const int WeaponClaw = 2;

		// Titan and car hitbox sizes: 10x the player (20x42).
		public const int TitanWidth = 200;
		public const int TitanHeight = 420;
		public const int CarWidth = 400;
		public const int CarHeight = 160;

		public static JackElement Element(TitanFusion fusion) => fusion switch {
			TitanFusion.Robot => JackElement.Plasma,
			TitanFusion.Blaze => JackElement.Blaze,
			TitanFusion.Frost => JackElement.Frost,
			TitanFusion.Volt => JackElement.Volt,
			TitanFusion.Shadow => JackElement.Shadow,
			TitanFusion.Nova => JackElement.Nova,
			TitanFusion.Omega => JackElement.Prism,
			TitanFusion.God => JackElement.Holy,
			_ => JackElement.Tech,
		};

		// Damage multiplier for each fusion's abilities, rising with how late the merged trigger is obtained.
		public static float Tier(TitanFusion fusion) => fusion switch {
			TitanFusion.Robot => 2f,
			TitanFusion.Blaze or TitanFusion.Frost => 2f,
			TitanFusion.Volt or TitanFusion.Shadow => 3f,
			TitanFusion.Nova => 5f,
			TitanFusion.Omega => 7f,
			TitanFusion.God => 9f,
			_ => 1.5f,
		};

		public static string Name(TitanFusion fusion) => fusion switch {
			TitanFusion.None => "Shift Titan",
			TitanFusion.Robot => "Plasma Titan",
			TitanFusion.Blaze => "Inferno Titan",
			TitanFusion.Frost => "Glacier Titan",
			TitanFusion.Volt => "Storm Titan",
			TitanFusion.Shadow => "Void Titan",
			TitanFusion.Nova => "Star Titan",
			TitanFusion.Omega => "Omega Titan",
			_ => "Divine Titan",
		};

		// The trigger item merged in for a fusion (0 for none).
		public static int TriggerOf(TitanFusion fusion) => fusion switch {
			TitanFusion.Robot => ModContent.ItemType<RobotTrigger>(),
			TitanFusion.Blaze => ModContent.ItemType<BlazeTrigger>(),
			TitanFusion.Frost => ModContent.ItemType<FrostTrigger>(),
			TitanFusion.Volt => ModContent.ItemType<VoltTrigger>(),
			TitanFusion.Shadow => ModContent.ItemType<ShadowTrigger>(),
			TitanFusion.Nova => ModContent.ItemType<NovaTrigger>(),
			TitanFusion.Omega => ModContent.ItemType<OmegaTrigger>(),
			TitanFusion.God => ModContent.ItemType<GodTrigger>(),
			_ => 0,
		};

		// The fusion a trigger item gives when merged (null = that item can't be merged).
		public static TitanFusion? FusionOf(int itemType) {
			if (itemType == ModContent.ItemType<RobotTrigger>()) return TitanFusion.Robot;
			if (itemType == ModContent.ItemType<BlazeTrigger>()) return TitanFusion.Blaze;
			if (itemType == ModContent.ItemType<FrostTrigger>()) return TitanFusion.Frost;
			if (itemType == ModContent.ItemType<VoltTrigger>()) return TitanFusion.Volt;
			if (itemType == ModContent.ItemType<ShadowTrigger>()) return TitanFusion.Shadow;
			if (itemType == ModContent.ItemType<NovaTrigger>()) return TitanFusion.Nova;
			if (itemType == ModContent.ItemType<OmegaTrigger>()) return TitanFusion.Omega;
			if (itemType == ModContent.ItemType<GodTrigger>()) return TitanFusion.God;
			return null;
		}
	}
}
