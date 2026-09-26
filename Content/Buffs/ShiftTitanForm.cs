using RobotJack.Common;
using RobotJack.Common.Titan;
using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// The Shift Titan: a giant robot 10x the player's size. Very tough; the merged trigger adds more damage
	// (see TitanData.Tier) and its element's immunity.
	public class ShiftTitanForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.ShiftTitan;
		public override int DamageBonus => 40;
		public override int DefenseBonus => 60;
		public override int DamageReduction => 30;
		public override int MoveSpeedBonus => 0;
		public override int MaxLifeBonus => 400;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			TitanPlayer titan = player.GetModPlayer<TitanPlayer>();
			// Each tier of merged trigger adds 5% more damage.
			player.GetDamage(DamageClass.Generic) += 0.05f * TitanData.Tier(titan.fusion);
			if (titan.fusion == TitanFusion.Omega || titan.fusion == TitanFusion.God) {
				// Every element's debuff.
				for (int i = 0; i < Elements.Count; i++) {
					player.buffImmune[Elements.Debuff((JackElement)i)] = true;
				}
			}
			else {
				player.buffImmune[Elements.Debuff(titan.Element)] = true;
			}
			// A giant needs a giant jump.
			player.jumpSpeedBoost += 2f;
		}
	}
}
