using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Spider Jack's eight abilities, in the world's infection (Blight: purple Corruption or red Crimson),
	// built from the element ability templates. His front legs change weapon to match the ability in use.

	// Three piercing bolts of infected venom.
	public class VenomSpit : BoltAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 24;
		protected override int UseTime => 18;
		protected override bool AutoReuse => true;
		protected override int Count => 3;
		protected override float Spread => 0.25f;
		protected override int Flags => ElementBolt.Pierce;
		protected override SoundStyle? Sound => SoundID.Item17;
	}

	// Hold left click: an infection laser from the leg cannon.
	public class InfectionLaser : BeamAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 15;
		protected override SoundStyle? Sound => SoundID.Item12;
	}

	// Seven spikes of infection burst from the ground across the cursor.
	public class BlightSpikes : EruptionAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 30;
		protected override int Count => 7;
		protected override float Spacing => 60f;
		protected override SoundStyle? Sound => SoundID.NPCDeath13;
	}

	// Twelve exploding globs of acid rain down onto the cursor.
	public class AcidRain : RainAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 20;
		protected override int Count => 12;
		protected override float Area => 320f;
		protected override int Flags => ElementBolt.Explode;
	}

	// A burst of sticky web around you that holds enemies in place for a moment.
	public class WebBurst : BlastAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 36;
		protected override float Radius => 300f;
		protected override int BlastFlags => ElementBlast.Huge | ElementBlast.Freeze;
		protected override SoundStyle? Sound => SoundID.Item17;
	}

	// Pounce at the cursor, slicing through everything on the way.
	public class Pounce : DashAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 36;
		protected override float DashSpeed => 22f;
		protected override SoundStyle? Sound => SoundID.Item71;
	}

	// Six buzzsaw blades circle you for 10 seconds.
	public class BuzzsawRing : OrbitAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BaseDamage => 20;
		protected override int Count => 6;
	}

	// Hive Mind: +25% damage and +15% crit chance for 10 seconds. 30 second cooldown.
	public class HiveMindAbility : PowerAbility
	{
		public override JackElement Element => JackElement.Blight;
		protected override int BuffType => ModContent.BuffType<HiveMind>();
		protected override SoundStyle? Sound => SoundID.Item4;
	}
}
