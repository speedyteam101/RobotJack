using RobotJack.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Spider Jack: you become a small robot spider built from Akutoku-ō's design. You can climb walls,
	// and you're immune to the world's infection (Cursed Inferno, Ichor) and to poison.
	public class SpiderJackForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.SpiderJack;
		public override int DamageBonus => 30;
		public override int DefenseBonus => 25;
		public override int DamageReduction => 12;
		public override int MoveSpeedBonus => 40;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			player.spikedBoots = 2; // climb walls
			player.buffImmune[BuffID.CursedInferno] = true;
			player.buffImmune[BuffID.Ichor] = true;
			player.buffImmune[BuffID.Poisoned] = true;
			player.buffImmune[BuffID.Venom] = true;
		}
	}

	// Spider Jack's Hive Mind power-up: +25% damage and +15% crit chance.
	public class HiveMind : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			player.GetDamage(DamageClass.Generic) += 0.25f;
			player.GetCritChance(DamageClass.Generic) += 15f;
		}
	}
}
