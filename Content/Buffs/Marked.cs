using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Locked on by Titan Camera's Target Lock: takes 50% more damage. MarkedNPC applies the bonus and draws the reticle.
	public class Marked : ModBuff
	{
		public const float DamageTakenMultiplier = 1.5f;

		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}
}
