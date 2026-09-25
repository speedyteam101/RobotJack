using RobotJack.Common;

namespace RobotJack.Content.Buffs
{
	// The Robot Jack transformation (from the Robot Trigger).
	public class RobotForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.RobotJack;
		public override int DamageBonus => 25;
		public override int DefenseBonus => 20;
		public override int DamageReduction => 10;
		public override int MoveSpeedBonus => 30;
	}
}
