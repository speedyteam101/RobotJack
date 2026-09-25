using Microsoft.Xna.Framework;
using RobotJack.Content.Abilities;
using RobotJack.Content.Buffs;
using RobotJack.Content.Players;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Common
{
	// One attack the Energy Absorber has taken in: which projectile it was and how hard it hit.
	public struct AbsorbedAttack
	{
		public int ProjectileType;
		public int Damage;
	}

	// Tracks the Robot Jack transformation, the jetpack head ram, spinning, and the Energy Absorber's stored energy.
	public class RobotJackPlayer : ModPlayer
	{
		// Most energy the absorber can hold. The release blast deals 10x the stored energy.
		public const int MaxEnergy = 3000;
		public const int BlastMultiplier = 10;
		// How many different absorbed attacks are remembered and fired back by the release blast.
		public const int MaxRecorded = 12;

		// True while the Robot Form buff is active. Based on the buff itself, so it's correct at any point in the update.
		public bool Transformed => Player.HasBuff(ModContent.BuffType<RobotForm>());

		// Set by the Robot Jetpack each frame it's equipped.
		public bool jetpack;

		// Ticks left of the absorb field. Kept above 0 by AbsorbField while it's active (owner only).
		public int absorbTimer;
		public bool Absorbing => absorbTimer > 0;

		// Energy stored by the Energy Absorber, and the attacks it recorded. Local to the owning client.
		public float energy;
		public readonly List<AbsorbedAttack> recorded = new();

		private bool wasSpinning;

		public override void ResetEffects() {
			jetpack = false;
		}

		public override void UpdateDead() {
			energy = 0f;
			recorded.Clear();
			absorbTimer = 0;
		}

		public override void PostUpdate() {
			UpdateSpin();

			if (Player.whoAmI == Main.myPlayer) {
				if (absorbTimer > 0) {
					absorbTimer--;
				}
				UpdateAbilityItems();
				UpdateHeadRam();
			}
		}

		// ------------------------------------------------------------------ energy

		public void AddEnergy(float amount) {
			energy = MathHelper.Clamp(energy + amount, 0f, MaxEnergy);
		}

		// Remembers an absorbed attack so the release blast can fire it back. Keeps the hardest hit per projectile type.
		public void Record(int projectileType, int damage) {
			if (projectileType <= 0 || damage <= 0) {
				return;
			}
			for (int i = 0; i < recorded.Count; i++) {
				if (recorded[i].ProjectileType == projectileType) {
					if (damage > recorded[i].Damage) {
						recorded[i] = new AbsorbedAttack { ProjectileType = projectileType, Damage = damage };
					}
					return;
				}
			}
			if (recorded.Count >= MaxRecorded) {
				recorded.RemoveAt(0); // forget the oldest
			}
			recorded.Add(new AbsorbedAttack { ProjectileType = projectileType, Damage = damage });
		}

		// While the absorb field is up, every hit is neutralised and turned into stored energy.
		public override bool FreeDodge(Player.HurtInfo info) {
			if (!Absorbing) {
				return false;
			}

			int damage = System.Math.Max(info.SourceDamage, 1);
			AddEnergy(damage);
			if (info.DamageSource != null) {
				Record(info.DamageSource.SourceProjectileType, damage);
			}

			Player.SetImmuneTimeForAllTypes(20);
			CombatText.NewText(Player.getRect(), new Color(120, 230, 255), "Absorbed " + damage);
			SoundEngine.PlaySound(SoundID.Item93 with { Pitch = 0.3f }, Player.Center);
			for (int i = 0; i < 12; i++) {
				Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2CircularEdge(40f, 40f), DustID.Electric, Vector2.Zero, Scale: 1.2f);
				dust.velocity = (Player.Center - dust.position) * 0.1f;
				dust.noGravity = true;
			}
			return true;
		}

		// ------------------------------------------------------------------ head ram and spinning

		// Keeps a HeadRam hitbox on the robot's head while it's transformed, wearing the jetpack and moving fast.
		private void UpdateHeadRam() {
			int type = ModContent.ProjectileType<HeadRam>();
			if (!HeadRam.CanRam(Player) || Player.ownedProjectileCounts[type] > 0) {
				return;
			}
			int damage = (int)Player.GetTotalDamage(DamageClass.Melee).ApplyTo(HeadRam.BaseDamage * RobotAbility.ProgressionMultiplier());
			Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Top, Vector2.Zero, type, damage, HeadRam.Knockback, Player.whoAmI);
		}

		// Rammed in PvP: this runs on the victim's own client, which then syncs its buffs.
		public override void OnHurt(Player.HurtInfo info) {
			if (info.PvP && info.DamageSource != null && info.DamageSource.SourceProjectileType == ModContent.ProjectileType<HeadRam>()) {
				int spin = Spinning.DurationFor(System.Math.Max(info.Knockback, HeadRam.Knockback * 0.5f));
				if (spin > 0) {
					Player.AddBuff(ModContent.BuffType<Spinning>(), spin);
				}
			}
		}

		private void UpdateSpin() {
			if (Player.HasBuff(ModContent.BuffType<Spinning>())) {
				int dir = Player.velocity.X >= 0f ? 1 : -1;
				Player.fullRotation += 0.45f * dir;
				Player.fullRotationOrigin = Player.Size / 2f;
				wasSpinning = true;
			}
			else if (wasSpinning) {
				Player.fullRotation = 0f;
				wasSpinning = false;
			}
		}

		// ------------------------------------------------------------------ ability items

		// While transformed, the player is given every ability item. When the form ends they are taken away again.
		private void UpdateAbilityItems() {
			bool transformed = Transformed;

			// Remove abilities when not transformed, including one held on the cursor.
			if (!transformed) {
				for (int i = 0; i < Player.inventory.Length; i++) {
					if (Player.inventory[i].ModItem is RobotAbility) {
						Player.inventory[i].TurnToAir();
					}
				}
				if (Main.mouseItem.ModItem is RobotAbility) {
					Main.mouseItem.TurnToAir();
				}
				return;
			}

			foreach (RobotAbility ability in RobotAbility.All) {
				GiveAbility(ability.Type);
			}
		}

		private void GiveAbility(int type) {
			if (Player.HasItem(type) || Main.mouseItem.type == type) {
				return;
			}

			// First empty slot of the main inventory (hotbar first). Coins/ammo slots start at 50.
			for (int i = 0; i < 50; i++) {
				if (Player.inventory[i].IsAir) {
					Player.inventory[i].SetDefaults(type);
					return;
				}
			}
		}

		// While transformed, hide the normal player body so only the robot is drawn.
		// Held items, mounts, wings and debuff effects stay visible.
		public override void HideDrawLayers(PlayerDrawSet drawInfo) {
			if (!Transformed || Player.dead) {
				return;
			}

			PlayerDrawLayer robotLayer = ModContent.GetInstance<RobotDrawLayer>();
			foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.DrawOrder) {
				if (layer == robotLayer
					|| layer == PlayerDrawLayers.HeldItem
					|| layer == PlayerDrawLayers.ProjectileOverArm
					|| layer == PlayerDrawLayers.Wings
					|| layer == PlayerDrawLayers.MountBack
					|| layer == PlayerDrawLayers.MountFront
					|| layer == PlayerDrawLayers.FrozenOrWebbedDebuff
					|| layer == PlayerDrawLayers.WebbedDebuffBack
					|| layer == PlayerDrawLayers.ElectrifiedDebuffBack
					|| layer == PlayerDrawLayers.ElectrifiedDebuffFront
					|| layer == PlayerDrawLayers.IceBarrier) {
					continue;
				}
				layer.Hide();
			}
		}
	}
}
