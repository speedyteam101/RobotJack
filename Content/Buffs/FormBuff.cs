using Microsoft.Xna.Framework;
using RobotJack.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace RobotJack.Content.Buffs
{
	// Base class for the transformation buffs. A form never runs out: its transform item toggles it off,
	// and right-clicking the buff icon or dying also ends it. Only one form can be active at a time.
	public abstract class FormBuff : ModBuff
	{
		// Buff type for each form, filled in as the buffs load.
		public static readonly Dictionary<RobotFormType, int> BuffTypes = new();

		public abstract RobotFormType Form { get; }

		public abstract int DamageBonus { get; }      // percent, all damage
		public abstract int DefenseBonus { get; }
		public abstract int DamageReduction { get; }  // percent
		public abstract int MoveSpeedBonus { get; }   // percent
		public virtual int MaxLifeBonus => 0;
		public virtual int CritBonus => 0;            // percent, all damage

		// Values for "{0}".."{5}" in the buff description and the transform item's tooltip.
		public object[] StatArgs => new object[] { DamageBonus, DefenseBonus, DamageReduction, MoveSpeedBonus, MaxLifeBonus, CritBonus };

		public override void SetStaticDefaults() {
			Main.buffNoTimeDisplay[Type] = true;
			Main.buffNoSave[Type] = true;
			BuffTypes[Form] = Type;
		}

		public override void Unload() {
			BuffTypes.Clear();
		}

		public override void Update(Player player, ref int buffIndex) {
			// Keep the buff topped up so it lasts forever.
			player.buffTime[buffIndex] = 2;

			player.GetDamage(DamageClass.Generic) += DamageBonus / 100f;
			player.GetCritChance(DamageClass.Generic) += CritBonus;
			player.statDefense += DefenseBonus;
			player.endurance += DamageReduction / 100f;
			player.moveSpeed += MoveSpeedBonus / 100f;
			player.statLifeMax2 += MaxLifeBonus;
			player.noKnockback = true;
			player.noFallDmg = true;
			player.jumpSpeedBoost += Form.IsTitan() ? 3f : 2f;

			// Hover jets: hold jump while falling to glide down slowly.
			bool falling = player.velocity.Y * player.gravDir > 0f;
			if (player.controlJump && falling) {
				player.slowFall = true;
				if (Main.rand.NextBool(2)) {
					Vector2 jet = player.Bottom + new Vector2(Main.rand.NextFloat(-6f, 6f), -2f);
					Dust dust = Dust.NewDustPerfect(jet, DustID.Torch, new Vector2(0f, 3f * player.gravDir), Scale: 1.3f);
					dust.noGravity = true;
				}
			}

			Lighting.AddLight(player.Center, 0.1f, 0.35f, 0.45f);
		}

		public override void ModifyBuffText(ref string buffName, ref string tip, ref int rare) {
			tip = Description.Format(StatArgs);
		}
	}
}
