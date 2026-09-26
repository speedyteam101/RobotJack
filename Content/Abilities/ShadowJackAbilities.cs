using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Shadow Jack's eight abilities (see ElementAbilityTemplates.cs for what each template does).

	// Four homing shadow orbs.
	public class ShadowOrbs : BoltAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 20;
		protected override int UseTime => 22;
		protected override bool AutoReuse => true;
		protected override int Count => 4;
		protected override float Spread => 0.6f;
		protected override float Speed => 10f;
		protected override int Flags => ElementBolt.Homing;
		protected override SoundStyle? Sound => SoundID.Item104;
	}

	// Hold left click: a beam of pure void from your arm cannon.
	public class VoidBeam : BeamAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 18;
		protected override SoundStyle? Sound => SoundID.Item104;
	}

	// Five shadow tendrils burst out of the ground across the cursor.
	public class ShadowSpikes : EruptionAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 40;
		protected override int Count => 5;
	}

	// Dark orbs rain down onto the cursor.
	public class DarkRain : RainAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 22;
		protected override int Count => 10;
		protected override int Flags => ElementBolt.Explode;
		protected override SoundStyle? Sound => SoundID.Item104;
	}

	// A pulse of void energy bursts out around you.
	public class VoidPulse : BlastAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 55;
		protected override float Radius => 260f;
	}

	// Step through the shadows toward the cursor, invincible for longer.
	public class ShadowStep : DashAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 45;
		protected override int Invincibility => 40;
		protected override SoundStyle? Sound => SoundID.Item104;
	}

	// Six phantom blades circle you for 10 seconds.
	public class PhantomBlades : OrbitAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BaseDamage => 20;
		protected override int Count => 6;
	}

	// Vampiric Shroud for 10 seconds: your hits steal life.
	public class VampiricShroudBoost : PowerAbility
	{
		public override JackElement Element => JackElement.Shadow;
		protected override int BuffType => ModContent.BuffType<VampiricShroud>();
		protected override SoundStyle? Sound => SoundID.Item104;
	}
}
