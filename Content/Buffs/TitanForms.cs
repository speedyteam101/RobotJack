using RobotJack.Common;

namespace RobotJack.Content.Buffs
{
	// The three Titan transformations. Titans are giant (see RobotJackPlayer.UpdateSize), so they're
	// tougher to make up for being easier to hit.

	public class TitanSpeakerForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;
		public override int DamageBonus => 30;
		public override int DefenseBonus => 40;
		public override int DamageReduction => 20;
		public override int MoveSpeedBonus => 15;
		public override int MaxLifeBonus => 150;
	}

	public class TitanCameraForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;
		public override int DamageBonus => 35;
		public override int DefenseBonus => 30;
		public override int DamageReduction => 15;
		public override int MoveSpeedBonus => 20;
		public override int MaxLifeBonus => 100;
		public override int CritBonus => 15;
	}

	public class TitanTVForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.TitanTV;
		public override int DamageBonus => 30;
		public override int DefenseBonus => 30;
		public override int DamageReduction => 15;
		public override int MoveSpeedBonus => 35;
		public override int MaxLifeBonus => 100;
	}
}
