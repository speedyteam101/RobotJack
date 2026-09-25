using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.ModLoader;

namespace RobotJack.Common
{
	// Marked enemies take 50% more damage (except from the Orbital Cannon Strike, which is always exactly its damage)
	// and have a spinning red lock-on reticle drawn over them.
	public class MarkedNPC : GlobalNPC
	{
		private static bool IsMarked(NPC npc) => npc.HasBuff(ModContent.BuffType<Marked>());

		public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) {
			if (IsMarked(npc)) {
				modifiers.FinalDamage *= Marked.DamageTakenMultiplier;
			}
		}

		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) {
			if (IsMarked(npc) && projectile.type != ModContent.ProjectileType<OrbitalStrike>()) {
				modifiers.FinalDamage *= Marked.DamageTakenMultiplier;
			}
		}

		public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (!IsMarked(npc)) {
				return;
			}
			Texture2D reticle = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalStrike").Value;
			float size = System.Math.Max(npc.width, npc.height) + 30f;
			float pulse = 1f + 0.08f * (float)System.Math.Sin(Main.GameUpdateCount * 0.2f);
			spriteBatch.Draw(reticle, npc.Center - screenPos, null, new Color(255, 60, 60, 0) * 0.9f,
				Main.GameUpdateCount * 0.05f, reticle.Size() / 2f, size / reticle.Width * pulse, SpriteEffects.None, 0f);
		}
	}
}
