using Microsoft.Xna.Framework;
using RobotJack.Common;
using Terraria;
using Terraria.ID;

namespace RobotJack.Content.Buffs
{
	// Shared by Robot Jack and his five variants: jetpack flames while in the air, sparking in the form's element colour.
	public abstract class JackFormBuff : FormBuff
	{
		// Dust mixed into the jetpack flame (Robot Jack: electric sparks; variants: their element).
		protected virtual int JetSparkDust => Form.Element() is JackElement e ? Elements.Dust(e) : DustID.Electric;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);

			if (player.velocity.Y != 0f && !player.mount.Active && Main.netMode != NetmodeID.Server) {
				Vector2 nozzle = player.Center + new Vector2(-player.direction * 9f, 8f * player.gravDir);
				Dust fire = Dust.NewDustPerfect(nozzle + new Vector2(Main.rand.NextFloat(-2f, 2f), 0f), DustID.Torch,
					new Vector2(-player.velocity.X * 0.2f, 3f * player.gravDir), Scale: 1.3f);
				fire.noGravity = true;
				if (Main.rand.NextBool(2)) {
					Dust spark = Dust.NewDustPerfect(nozzle, JetSparkDust, new Vector2(0f, 2f * player.gravDir), Scale: 0.8f);
					spark.noGravity = true;
				}
				Lighting.AddLight(nozzle, 0.7f, 0.45f, 0.15f);
			}
		}
	}

	// The Robot Jack transformation (from the Robot Trigger).
	public class RobotForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.RobotJack;
		public override int DamageBonus => 25;
		public override int DefenseBonus => 20;
		public override int DamageReduction => 10;
		public override int MoveSpeedBonus => 30;
	}
}
