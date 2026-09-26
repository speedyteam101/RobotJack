using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Put on players Akutoku-ō hits: while you have it, you deal 50% less damage to him,
	// and the damage he steals from your hits feeds his Stolen Power.
	public class Siphoned : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
		}
	}

	// Akutoku-ō's buff: +50% damage. He gets it whenever a Siphoned player hits him.
	public class StolenPower : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
		}
	}
}

namespace RobotJack.Content.Buffs
{
	// Cleansed Robitacons are fighting for you.
	public class RobitaconMinionBuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.buffNoSave[Type] = true;
			Main.buffNoTimeDisplay[Type] = true;
		}

		public override void Update(Player player, ref int buffIndex) {
			if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.RobitaconMinion>()] > 0) {
				player.buffTime[buffIndex] = 18000;
			}
			else {
				player.DelBuff(buffIndex);
				buffIndex--;
			}
		}
	}
}
