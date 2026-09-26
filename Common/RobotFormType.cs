using Microsoft.Xna.Framework;
using Terraria;

namespace RobotJack.Common
{
	// Every transformation in the mod. Each has its own transform item, form buff, sprite and ability items.
	public enum RobotFormType
	{
		None,
		RobotJack,
		BlazeJack,
		FrostJack,
		VoltJack,
		ShadowJack,
		NovaJack,
		OmegaJack,
		GodJack,
	}

	public static class RobotFormTypeExtensions
	{
		// God Jack's divine gold.
		public static readonly Color GodGold = new Color(255, 215, 90);

		// Robot Jack, his five elemental variants and Omega Jack (same size and body, different colours and abilities).
		public static bool IsJack(this RobotFormType form) =>
			form == RobotFormType.RobotJack || form == RobotFormType.OmegaJack || form == RobotFormType.GodJack || form.Element() != null;

		// The element of a Robot Jack variant, or null for every other form.
		public static JackElement? Element(this RobotFormType form) => form switch {
			RobotFormType.BlazeJack => JackElement.Blaze,
			RobotFormType.FrostJack => JackElement.Frost,
			RobotFormType.VoltJack => JackElement.Volt,
			RobotFormType.ShadowJack => JackElement.Shadow,
			RobotFormType.NovaJack => JackElement.Nova,
			_ => null,
		};

		// Colour of the form's glow (afterimages, transformation burst). Robot Jack is cyan, Omega Jack cycles the rainbow.
		public static Color GlowColor(this RobotFormType form) {
			if (form == RobotFormType.OmegaJack) {
				return Main.DiscoColor;
			}
			if (form == RobotFormType.GodJack) {
				return GodGold;
			}
			JackElement? element = form.Element();
			return element.HasValue ? Elements.Main(element.Value) : new Color(90, 230, 255);
		}
	}
}
