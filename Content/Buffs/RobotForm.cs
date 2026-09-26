using Microsoft.Xna.Framework;
using RobotJack.Common;
using Terraria;
using Terraria.ID;

namespace RobotJack.Content.Buffs
{
	// The Robot Jack transformation (from the Robot Trigger).
	public class RobotForm : FormBuff
	{
		public override RobotFormType Form => RobotFormType.RobotJack;
		public override int DamageBonus => 25;
		public override int DefenseBonus => 20;
		public override int DamageReduction => 10;
		public override int MoveSpeedBonus => 30;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);

			// Jetpack flames while in the air (not when riding a mount).
			if (player.velocity.Y != 0f && !player.mount.Active && Main.netMode != NetmodeID.Server) {
				Vector2 nozzle = player.Center + new Vector2(-player.direction * 9f, 8f * player.gravDir);
				Dust fire = Dust.NewDustPerfect(nozzle + new Vector2(Main.rand.NextFloat(-2f, 2f), 0f), DustID.Torch,
					new Vector2(-player.velocity.X * 0.2f, 3f * player.gravDir), Scale: 1.3f);
				fire.noGravity = true;
				if (Main.rand.NextBool(2)) {
					Dust spark = Dust.NewDustPerfect(nozzle, DustID.Electric, new Vector2(0f, 2f * player.gravDir), Scale: 0.6f);
					spark.noGravity = true;
				}
				Lighting.AddLight(nozzle, 0.7f, 0.45f, 0.15f);
			}
		}
	}
}
