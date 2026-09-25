namespace RobotJack.Common
{
	// Every transformation in the mod. Each has its own transform item, form buff, sprite and ability items.
	public enum RobotFormType
	{
		None,
		RobotJack,
		TitanSpeaker,
		TitanCamera,
		TitanTV,
	}

	public static class RobotFormTypeExtensions
	{
		// Titans are drawn twice as big and have a bigger hitbox.
		public static bool IsTitan(this RobotFormType form) => form >= RobotFormType.TitanSpeaker;
	}
}
