using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Blaze Jack's eight abilities (see ElementAbilityTemplates.cs for what each template does).

	// Rapid fireballs that burst into small explosions.
	public class FlameBlaster : BoltAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 22;
		protected override int UseTime => 12;
		protected override bool AutoReuse => true;
		protected override float Speed => 14f;
		protected override int Flags => ElementBolt.Explode;
		protected override SoundStyle? Sound => SoundID.Item20;
	}

	// Hold left click: a roaring stream of fire from your arm cannon.
	public class InfernoBeam : BeamAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 18;
		protected override SoundStyle? Sound => SoundID.Item34;
	}

	// Five fire geysers burst out of the ground across the cursor.
	public class Eruption : EruptionAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 40;
		protected override int Count => 5;
	}

	// Call down seven meteors that explode where they land.
	public class MeteorShower : RainAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 35;
		protected override int Count => 7;
		protected override int Flags => ElementBolt.Explode | ElementBolt.Big;
		protected override SoundStyle? Sound => SoundID.Item88;
	}

	// A wave of heat bursts out around you.
	public class HeatNova : BlastAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 55;
		protected override float Radius => 260f;
	}

	// Rocket toward the cursor in a trail of fire.
	public class BlazeDash : DashAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 40;
		protected override SoundStyle? Sound => SoundID.Item74;
	}

	// Five fireballs circle you for 10 seconds.
	public class FlameRing : OrbitAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BaseDamage => 20;
		protected override int Count => 5;
	}

	// Overheat for 10 seconds: +30% damage and every hit sets enemies ablaze.
	public class OverheatBoost : PowerAbility
	{
		public override JackElement Element => JackElement.Blaze;
		protected override int BuffType => ModContent.BuffType<Overheat>();
		protected override SoundStyle? Sound => SoundID.Item74;
	}
}
