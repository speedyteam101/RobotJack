using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Nova Jack's eight abilities (see ElementAbilityTemplates.cs for what each template does).

	// Five homing stars.
	public class StarShot : BoltAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 20;
		protected override int UseTime => 20;
		protected override bool AutoReuse => true;
		protected override int Count => 5;
		protected override float Spread => 0.5f;
		protected override float Speed => 12f;
		protected override int Flags => ElementBolt.Homing;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// Hold left click: a blazing starlight beam from your arm cannon.
	public class SupernovaBeam : BeamAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 20;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// Seven pillars of starlight burst out of the ground across the cursor.
	public class CometPillars : EruptionAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 40;
		protected override int Count => 7;
		protected override float Spacing => 56f;
	}

	// Call down falling stars that explode where they land.
	public class Starfall : RainAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 35;
		protected override int Count => 10;
		protected override int Flags => ElementBolt.Explode | ElementBolt.Big;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// A colossal explosion of starlight around you. 10 second cooldown.
	public class BigBang : BlastAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 90;
		protected override float Radius => 420f;
		protected override int UseTime => 90;
		public override int CooldownTicks => 600;
	}

	// Streak toward the cursor like a comet.
	public class CometDash : DashAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 45;
		protected override float DashSpeed => 22f;
		protected override SoundStyle? Sound => SoundID.Item9;
	}

	// Eight stars circle you for 10 seconds.
	public class Constellation : OrbitAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BaseDamage => 20;
		protected override int Count => 8;
	}

	// Celestial Blessing for 10 seconds: +10 health per second and +15% damage.
	public class CelestialBlessingBoost : PowerAbility
	{
		public override JackElement Element => JackElement.Nova;
		protected override int BuffType => ModContent.BuffType<CelestialBlessing>();
		protected override SoundStyle? Sound => SoundID.Item4;
	}
}
