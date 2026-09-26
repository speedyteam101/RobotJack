using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Frost Jack's eight abilities (see ElementAbilityTemplates.cs for what each template does).

	// Fire three piercing ice shards.
	public class IceShards : BoltAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 18;
		protected override int UseTime => 18;
		protected override bool AutoReuse => true;
		protected override int Count => 3;
		protected override float Spread => 0.25f;
		protected override float Speed => 15f;
		protected override int Flags => ElementBolt.Pierce;
		protected override SoundStyle? Sound => SoundID.Item28;
	}

	// Hold left click: a freezing beam from your arm cannon.
	public class FreezeRay : BeamAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 16;
		protected override SoundStyle? Sound => SoundID.Item28;
	}

	// Five ice spires burst out of the ground across the cursor, freezing enemies.
	public class GlacierSpikes : EruptionAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 38;
		protected override int Count => 5;
	}

	// A storm of hail pelts the area around the cursor.
	public class Hailstorm : RainAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 18;
		protected override int Count => 12;
		protected override float Area => 320f;
		protected override SoundStyle? Sound => SoundID.Item28;
	}

	// A blizzard swirls around you for 8 seconds, freezing enemies inside.
	public class Blizzard : AuraAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 16;
		protected override SoundStyle? Sound => SoundID.Item28;
	}

	// A burst of cold around you that freezes enemies in place (not bosses).
	public class FrostNova : BlastAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 45;
		protected override float Radius => 240f;
		protected override int BlastFlags => ElementBlast.Huge | ElementBlast.Freeze;
		protected override int UseTime => 60;
	}

	// Four ice shards circle you for 10 seconds.
	public class IceOrbit : OrbitAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BaseDamage => 18;
		protected override int Count => 4;
	}

	// Cryo Armor for 10 seconds: +25 defense and 20% damage reduction.
	public class CryoArmorBoost : PowerAbility
	{
		public override JackElement Element => JackElement.Frost;
		protected override int BuffType => ModContent.BuffType<CryoArmor>();
		protected override SoundStyle? Sound => SoundID.Item28;
	}
}
