using RobotJack.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// The five Robot Jack variants. Same size and body as Robot Jack, each with an elemental perk.

	// Blaze Jack: immune to burning and lava.
	public class BlazeJackForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.BlazeJack;
		public override int DamageBonus => 30;
		public override int DefenseBonus => 18;
		public override int DamageReduction => 10;
		public override int MoveSpeedBonus => 30;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			player.buffImmune[BuffID.OnFire] = true;
			player.buffImmune[BuffID.OnFire3] = true;
			player.buffImmune[BuffID.Burning] = true;
			player.lavaImmune = true;
			player.fireWalk = true;
		}
	}

	// Frost Jack: immune to cold, doesn't slip on ice, tougher.
	public class FrostJackForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.FrostJack;
		public override int DamageBonus => 25;
		public override int DefenseBonus => 30;
		public override int DamageReduction => 15;
		public override int MoveSpeedBonus => 25;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			player.buffImmune[BuffID.Frostburn] = true;
			player.buffImmune[BuffID.Chilled] = true;
			player.iceSkate = true;
		}
	}

	// Volt Jack: much faster.
	public class VoltJackForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.VoltJack;
		public override int DamageBonus => 30;
		public override int DefenseBonus => 18;
		public override int DamageReduction => 10;
		public override int MoveSpeedBonus => 60;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			player.buffImmune[BuffID.Electrified] = true;
			player.GetAttackSpeed(DamageClass.Generic) += 0.1f;
		}
	}

	// Shadow Jack: harder hitting, and dodges some attacks.
	public class ShadowJackForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.ShadowJack;
		public override int DamageBonus => 40;
		public override int DefenseBonus => 20;
		public override int DamageReduction => 10;
		public override int MoveSpeedBonus => 35;
		public override int CritBonus => 10;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			player.buffImmune[BuffID.ShadowFlame] = true;
			player.blackBelt = true; // chance to dodge attacks
		}
	}

	// Nova Jack: the strongest variant, with health regeneration.
	public class NovaJackForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.NovaJack;
		public override int DamageBonus => 50;
		public override int DefenseBonus => 40;
		public override int DamageReduction => 20;
		public override int MoveSpeedBonus => 40;
		public override int MaxLifeBonus => 100;
		public override int CritBonus => 10;

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			player.lifeRegen += 10; // +5 health per second
		}
	}

	// Omega Jack: every Robot Jack combined. All the variants' perks, and far stronger stats.
	public class OmegaJackForm : JackFormBuff
	{
		public override RobotFormType Form => RobotFormType.OmegaJack;
		public override int DamageBonus => 80;
		public override int DefenseBonus => 60;
		public override int DamageReduction => 30;
		public override int MoveSpeedBonus => 60;
		public override int MaxLifeBonus => 200;
		public override int CritBonus => 20;

		// Jetpack sparks in every element.
		protected override int JetSparkDust => Elements.Dust((JackElement)Main.rand.Next(Elements.Count));

		public override void Update(Player player, ref int buffIndex) {
			base.Update(player, ref buffIndex);
			// Blaze
			player.buffImmune[BuffID.OnFire] = true;
			player.buffImmune[BuffID.OnFire3] = true;
			player.buffImmune[BuffID.Burning] = true;
			player.lavaImmune = true;
			player.fireWalk = true;
			// Frost
			player.buffImmune[BuffID.Frostburn] = true;
			player.buffImmune[BuffID.Chilled] = true;
			player.iceSkate = true;
			// Volt
			player.buffImmune[BuffID.Electrified] = true;
			player.GetAttackSpeed(DamageClass.Generic) += 0.2f;
			// Shadow
			player.buffImmune[BuffID.ShadowFlame] = true;
			player.blackBelt = true;
			// Nova
			player.lifeRegen += 20; // +10 health per second

			Lighting.AddLight(player.Center, Main.DiscoColor.ToVector3() * 0.5f);
		}
	}
}
