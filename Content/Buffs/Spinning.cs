using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Knocked into a spin by Robot Jack's head ram. Lasts as long as the knockback does.
	// The spinning itself is done by SpinningNPC (enemies) and RobotJackPlayer (players).
	public class Spinning : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}

		// Spin duration in ticks for a hit with this much knockback. No knockback (e.g. most bosses) means no spin.
		public static int DurationFor(float knockback) => knockback < 0.5f ? 0 : (int)System.Math.Min(20f + knockback * 5f, 90f);
	}
}
