using RobotJack.Content.Buffs;
using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Common
{
	// Spins enemies that have the Spinning debuff, then puts their rotation back when it ends.
	public class SpinningNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		private bool wasSpinning;

		public override void PostAI(NPC npc) {
			if (npc.HasBuff(ModContent.BuffType<Spinning>())) {
				// Spin the way it's flying.
				int dir = npc.velocity.X >= 0f ? 1 : -1;
				npc.rotation += 0.45f * dir;
				wasSpinning = true;
			}
			else if (wasSpinning) {
				npc.rotation = 0f;
				wasSpinning = false;
			}
		}
	}
}
