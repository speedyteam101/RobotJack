using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Frozen in place by Frost Jack's ice. StunnedNPC does the freezing.
	// Never applied to bosses.
	public class Stunned : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		// Stuns the target unless it's a boss.
		public static void TryApply(NPC target, int ticks) {
			if (!target.boss) {
				target.AddBuff(ModContent.BuffType<Stunned>(), ticks);
			}
		}
	}
}
