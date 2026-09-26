using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Self buffs from each variant's power-up ability. They last 10 seconds.
	// Overheat's burning hits and Vampiric Shroud's life steal are handled in RobotJackPlayer.
	public abstract class JackPowerBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
		}
	}

	// Blaze Jack's Overheat: +30% damage, and every hit sets enemies on fire.
	public class Overheat : JackPowerBuff
	{
		public override void Update(Player player, ref int buffIndex) {
			player.GetDamage(DamageClass.Generic) += 0.3f;
		}
	}

	// Frost Jack's Cryo Armor: +25 defense and 20% damage reduction.
	public class CryoArmor : JackPowerBuff
	{
		public override void Update(Player player, ref int buffIndex) {
			player.statDefense += 25;
			player.endurance += 0.2f;
		}
	}

	// Volt Jack's Overcharge: +40% movement speed and +25% attack speed.
	public class Overcharge : JackPowerBuff
	{
		public override void Update(Player player, ref int buffIndex) {
			player.moveSpeed += 0.4f;
			player.GetAttackSpeed(DamageClass.Generic) += 0.25f;
		}
	}

	// Shadow Jack's Vampiric Shroud: hits steal life.
	public class VampiricShroud : JackPowerBuff
	{
	}

	// Nova Jack's Celestial Blessing: +10 health per second and +15% damage.
	public class CelestialBlessing : JackPowerBuff
	{
		public override void Update(Player player, ref int buffIndex) {
			player.lifeRegen += 20;
			player.GetDamage(DamageClass.Generic) += 0.15f;
		}
	}
}
