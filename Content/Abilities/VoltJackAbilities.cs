using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Volt Jack's eight abilities (see ElementAbilityTemplates.cs for what each template does).

	// Very fast piercing electric bolts.
	public class ArcPistol : BoltAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 20;
		protected override int UseTime => 7;
		protected override bool AutoReuse => true;
		protected override float Speed => 20f;
		protected override int Flags => ElementBolt.Pierce;
		protected override SoundStyle? Sound => SoundID.Item12;
	}

	// Hold left click: a crackling lightning beam from your arm cannon.
	public class LightningBeam : BeamAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 18;
		protected override SoundStyle? Sound => SoundID.Item93;
	}

	// Three lightning bolts strike the ground across the cursor.
	public class ThunderStrike : EruptionAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 60;
		protected override int Count => 3;
		protected override float Spacing => 90f;
		protected override int UseTime => 45;
	}

	// Call a storm of electric bolts down onto the cursor.
	public class StormCall : RainAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 30;
		protected override int Count => 8;
		protected override int Flags => ElementBolt.Explode;
		protected override SoundStyle? Sound => SoundID.Item122;
	}

	// A tesla field surrounds you for 8 seconds, shocking enemies inside.
	public class TeslaField : AuraAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 18;
		protected override SoundStyle? Sound => SoundID.Item93;
	}

	// Discharge a burst of static around you.
	public class StaticDischarge : BlastAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 50;
		protected override float Radius => 280f;
	}

	// Zip toward the cursor at lightning speed.
	public class VoltDash : DashAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BaseDamage => 40;
		protected override float DashSpeed => 24f;
		protected override SoundStyle? Sound => SoundID.Item93;
	}

	// Overcharge for 10 seconds: +40% movement speed and +25% attack speed.
	public class OverchargeBoost : PowerAbility
	{
		public override JackElement Element => JackElement.Volt;
		protected override int BuffType => ModContent.BuffType<Overcharge>();
		protected override SoundStyle? Sound => SoundID.Item93;
	}
}
