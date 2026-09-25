using Microsoft.Xna.Framework;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Common
{
	// Stunned enemies skip their AI and hang frozen in place, with sparkles over their head.
	public class StunnedNPC : GlobalNPC
	{
		public override bool PreAI(NPC npc) {
			if (npc.boss || !npc.HasBuff(ModContent.BuffType<Stunned>())) {
				return true;
			}
			npc.velocity = Vector2.Zero;
			if (Main.rand.NextBool(6)) {
				Dust dust = Dust.NewDustPerfect(npc.Top + new Vector2(Main.rand.NextFloat(-10f, 10f), -6f), DustID.GemDiamond, new Vector2(0f, -0.5f), Scale: 0.9f);
				dust.noGravity = true;
			}
			return false;
		}
	}
}
