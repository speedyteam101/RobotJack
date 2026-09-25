using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Hold left click: open an absorb field. Attacks that hit you or fly into it are neutralised and stored as energy,
	// enemies (and PvP opponents) are pulled in, and anything close enough is drained, which adds more energy.
	// Right click: release everything as an area blast dealing 10x the stored energy, and fire back every
	// attack the field recorded at 10x its damage.
	public class EnergyAbsorber : RobotAbility
	{
		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.damage = 18; // drain damage per hit
			Item.knockBack = 0f;
			Item.shoot = ModContent.ProjectileType<AbsorbField>();
			Item.shootSpeed = 1f;
		}

		public override bool AltFunctionUse(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (!base.CanUseItem(player)) {
				return false;
			}

			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();
			if (player.altFunctionUse == 2) {
				// Release: needs stored energy.
				Item.channel = false;
				Item.shoot = ModContent.ProjectileType<EnergyBlast>();
				Item.UseSound = null;
				return modPlayer.energy >= 1f;
			}

			Item.channel = true;
			Item.shoot = ModContent.ProjectileType<AbsorbField>();
			Item.UseSound = SoundID.Item15 with { Pitch = -0.7f };
			return player.ownedProjectileCounts[Item.shoot] == 0;
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			if (player.altFunctionUse != 2) {
				Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, damage, 0f, player.whoAmI);
				return false;
			}

			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();
			int blastDamage = (int)System.Math.Min(modPlayer.energy * RobotJackPlayer.BlastMultiplier, int.MaxValue / 2);
			Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, type, blastDamage, 12f, player.whoAmI);

			// Fire back every recorded attack, spread evenly around the player, at 10x the damage it had.
			List<AbsorbedAttack> attacks = modPlayer.recorded;
			int echoType = ModContent.ProjectileType<EnergyEcho>();
			for (int i = 0; i < attacks.Count; i++) {
				Vector2 dir = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / attacks.Count + Main.rand.NextFloat(-0.1f, 0.1f));
				Projectile.NewProjectile(source, player.MountedCenter, dir * 13f, echoType,
					attacks[i].Damage * RobotJackPlayer.BlastMultiplier, 6f, player.whoAmI, ai0: attacks[i].ProjectileType);
			}

			modPlayer.energy = 0f;
			attacks.Clear();
			return false;
		}
	}
}
