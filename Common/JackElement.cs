using Microsoft.Xna.Framework;
using Terraria.ID;

namespace RobotJack.Common
{
	// The five Robot Jack variants' elements, plus God Jack's Holy light. Element abilities and projectiles take one
	// of these (usually stored in a projectile's ai[0]) to pick their colours, dust and debuff.
	public enum JackElement
	{
		Blaze,
		Frost,
		Volt,
		Shadow,
		Nova,
		Holy, // God Jack's golden light (not one of the five variants)
	}

	public static class Elements
	{
		// The five variants' elements (Holy isn't counted: Omega Jack's rainbow cycles through these five).
		public const int Count = 5;

		public static JackElement FromAI(float ai) => (JackElement)System.Math.Clamp((int)ai, 0, (int)JackElement.Holy);

		// Main colour (glows, beams, rings).
		public static Color Main(JackElement e) => e switch {
			JackElement.Blaze => new Color(255, 120, 35),
			JackElement.Frost => new Color(120, 215, 255),
			JackElement.Volt => new Color(255, 230, 70),
			JackElement.Shadow => new Color(155, 60, 230),
			JackElement.Holy => new Color(255, 215, 90),
			_ => new Color(255, 130, 215),
		};

		// Hot core colour (the bright middle of beams and orbs).
		public static Color Core(JackElement e) => e switch {
			JackElement.Blaze => new Color(255, 230, 150),
			JackElement.Frost => new Color(235, 250, 255),
			JackElement.Volt => new Color(255, 255, 220),
			JackElement.Shadow => new Color(230, 180, 255),
			JackElement.Holy => new Color(255, 252, 230),
			_ => new Color(255, 245, 200),
		};

		public static int Dust(JackElement e) => e switch {
			JackElement.Blaze => DustID.Torch,
			JackElement.Frost => DustID.IceTorch,
			JackElement.Volt => DustID.Electric,
			JackElement.Shadow => DustID.Corruption,
			JackElement.Holy => DustID.Enchanted_Gold,
			_ => DustID.Enchanted_Pink,
		};

		public static int Debuff(JackElement e) => e switch {
			JackElement.Blaze => BuffID.OnFire3,
			JackElement.Frost => BuffID.Frostburn,
			JackElement.Volt => BuffID.Electrified,
			JackElement.Shadow => BuffID.ShadowFlame,
			JackElement.Holy => BuffID.OnFire3, // holy fire
			_ => BuffID.Ichor,
		};

		public const int DebuffTicks = 180;

		public static RobotFormType Form(JackElement e) => e switch {
			JackElement.Blaze => RobotFormType.BlazeJack,
			JackElement.Frost => RobotFormType.FrostJack,
			JackElement.Volt => RobotFormType.VoltJack,
			JackElement.Shadow => RobotFormType.ShadowJack,
			JackElement.Holy => RobotFormType.GodJack,
			_ => RobotFormType.NovaJack,
		};
	}
}
