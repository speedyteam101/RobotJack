using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// God Jack's abilities besides Gods Wrath: eight holy powers built from the element ability templates
	// (see ElementAbilityTemplates.cs), in golden Holy light, with damage far beyond every other form.

	// Three huge homing lances of light that pierce through enemies.
	public class HolyLances : BoltAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 40;
		protected override int UseTime => 20;
		protected override bool AutoReuse => true;
		protected override int Count => 3;
		protected override float Spread => 0.3f;
		protected override float Speed => 16f;
		protected override int Flags => ElementBolt.Homing | ElementBolt.Pierce | ElementBolt.Big;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// Hold left click: a beam of pure heavenly light.
	public class HeavensBeam : BeamAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 25;
		protected override SoundStyle? Sound => SoundID.Item29;
	}

	// Nine pillars of holy light strike down across the cursor.
	public class DivineJudgement : EruptionAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 50;
		protected override int Count => 9;
		protected override float Spacing => 60f;
		protected override SoundStyle? Sound => SoundID.Item29;
	}

	// Sixteen exploding bolts of light fall from the heavens onto the cursor.
	public class RainOfHeaven : RainAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 35;
		protected override int Count => 16;
		protected override float Area => 400f;
		protected override int Flags => ElementBolt.Explode | ElementBolt.Big;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// A huge burst of radiance all around you.
	public class Radiance : BlastAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 70;
		protected override float Radius => 480f;
		protected override int UseTime => 60;
		protected override SoundStyle? Sound => SoundID.Item29;
	}

	// Dash toward the cursor on wings of light, invincible for a second.
	public class SeraphDash : DashAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 55;
		protected override float DashSpeed => 26f;
		protected override int Invincibility => 60;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// Eight holy blades circle you for 10 seconds.
	public class HaloOfBlades : OrbitAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BaseDamage => 30;
		protected override int Count => 8;
	}

	// A divine shield: you can't be hurt at all for 5 seconds. 60 second cooldown.
	public class DivineShieldAbility : PowerAbility
	{
		public override JackElement Element => JackElement.Holy;
		protected override int BuffType => ModContent.BuffType<DivineShield>();
		public override int CooldownTicks => 60 * 60;
		protected override int BuffTicks => 5 * 60;
		protected override SoundStyle? Sound => SoundID.Item4;
	}
}
