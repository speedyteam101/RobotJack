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

		// The active form, based on the form buffs themselves, so it's correct at any point in the update.
		public RobotFormType ActiveForm {
			get {
				foreach (var pair in FormBuff.BuffTypes) {
					if (Player.HasBuff(pair.Value)) {
						return pair.Key;
					}
				}
				return RobotFormType.None;
			}
		}

		// True in any form (Robot Jack or one of his variants).
		public bool Transformed => ActiveForm != RobotFormType.None;

		// Set by the Robot Jetpack each frame it's equipped.
		public bool jetpack;

		// Ticks left of the absorb field. Kept above 0 by AbsorbField while it's active (owner only).
		public int absorbTimer;
		public bool Absorbing => absorbTimer > 0;

		// Energy stored by the Energy Absorber, and the attacks it recorded. Local to the owning client.
		public float energy;
		public readonly List<AbsorbedAttack> recorded = new();

		private bool wasSpinning;

		// Robot Jack's afterimages: where the player was over the last few ticks (every 2 ticks, newest first).
		// Recorded for every player on every client, since it's only used for drawing.
		public const int TrailLength = 5;
		public readonly Vector2[] trailPositions = new Vector2[TrailLength];
		private int trailTick;

		// Ability cooldowns (item type -> ticks left). Local to the owning client.
		private readonly Dictionary<int, int> cooldowns = new();

		public int CooldownLeft(int itemType) => cooldowns.TryGetValue(itemType, out int ticks) ? ticks : 0;

		public void StartCooldown(int itemType, int ticks) => cooldowns[itemType] = ticks;


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
			wrathChargeTicks = 0;
			wrathGlow = System.Math.Max(0f, wrathGlow - 0.05f);
			RecordTrail();

			if (Player.whoAmI == Main.myPlayer) {
				if (absorbTimer > 0) {
					absorbTimer--;
				}
				TickCooldowns();
				if (lifeStealCooldown > 0) {
					lifeStealCooldown--;
				}
				UpdateAbilityItems();
				UpdateHeadRam();
			}
		}

		// ------------------------------------------------------------------ God Jack's gate entrance

		// Set by HeavenlyGate on every client: the game tick the gate appeared, and the player's facing direction then.
		public bool godEntrance;
		public uint godEntranceStart;
		public int godEntranceDirection = 1;

		private int EntranceTicks => godEntrance ? (int)(Main.GameUpdateCount - godEntranceStart) : -1;

		// Hidden inside the gate until its doors open...
		public bool HiddenInGate => EntranceTicks >= 0 && EntranceTicks < HeavenlyGate.AppearTick;

		// ...then walking out of it by themselves for a moment.
		public bool SteppingOutOfGate => EntranceTicks >= HeavenlyGate.AppearTick && EntranceTicks < HeavenlyGate.StepOutEndTick;

		public void StartGodEntrance() {
			godEntrance = true;
			godEntranceStart = Main.GameUpdateCount;
			godEntranceDirection = Player.direction;
		}

		public override void SetControls() {
			if (!godEntrance) {
				return;
			}
			if (EntranceTicks >= HeavenlyGate.StepOutEndTick) {
				godEntrance = false;
				return;
			}
			if (HiddenInGate || SteppingOutOfGate) {
				Player.controlLeft = SteppingOutOfGate && godEntranceDirection == -1;
				Player.controlRight = SteppingOutOfGate && godEntranceDirection == 1;
				Player.controlUp = Player.controlDown = Player.controlJump = false;
				Player.controlUseItem = Player.controlUseTile = false;
			}
		}

		// Gods Wrath: how brightly the player glows (0..1), and how long it has been charging (0 = not charging).
		// Both are set every tick by GodsWrathCharge and fade/reset here when it stops.
		public float wrathGlow;
		public int wrathChargeTicks;

		public override void PreUpdateMovement() {
			if (wrathChargeTicks > 0) {
				// Rise for the first second, then hang motionless in the air.
				Player.velocity = new Vector2(0f, wrathChargeTicks < 60 ? -1.2f : 0f);
				Player.fallStart = (int)(Player.position.Y / 16f);
			}
			if (HiddenInGate) {
				Player.velocity = Vector2.Zero;
				Player.immune = true;
				Player.immuneTime = System.Math.Max(Player.immuneTime, 10);
			}
			else if (SteppingOutOfGate) {
				// A slow, deliberate walk out of the light.
				Player.velocity.X = MathHelper.Clamp(Player.velocity.X, -1.6f, 1.6f);
			}
		}

		// ------------------------------------------------------------------ afterimages

		private void RecordTrail() {
			if (++trailTick % 2 != 0) {
				return;
			}
			for (int i = TrailLength - 1; i > 0; i--) {
				trailPositions[i] = trailPositions[i - 1];
			}
			trailPositions[0] = Player.position;
		}

		// ------------------------------------------------------------------ cooldowns

		private readonly List<int> cooldownKeys = new();

		private void TickCooldowns() {
			cooldownKeys.Clear();
			cooldownKeys.AddRange(cooldowns.Keys);
			foreach (int key in cooldownKeys) {
				if (--cooldowns[key] <= 0) {
					cooldowns.Remove(key);
				}
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
		// Power-up effects on hits: Overheat sets enemies ablaze, Vampiric Shroud steals life (at most every 10 ticks).
		private int lifeStealCooldown;

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if (Player.HasBuff(ModContent.BuffType<Overheat>())) {
				target.AddBuff(BuffID.OnFire3, 240);
			}
			if (Player.HasBuff(ModContent.BuffType<VampiricShroud>()) && lifeStealCooldown <= 0
				&& !target.immortal && target.lifeMax > 5 && !target.friendly) {
				int heal = System.Math.Min(System.Math.Clamp(damageDone / 10, 1, 12), Player.statLifeMax2 - Player.statLife);
				if (heal > 0) {
					Player.statLife += heal;
					Player.HealEffect(heal);
				}
				lifeStealCooldown = 10;
			}
		}

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

		// While transformed, the player is given the ability items of their current form.
		// Abilities of other forms (or all of them, when not transformed) are taken away.
		private void UpdateAbilityItems() {
			for (int i = 0; i < Player.inventory.Length; i++) {
				if (Player.inventory[i].ModItem is RobotAbility ability && !ability.IsAllowed(Player)) {
					Player.inventory[i].TurnToAir();
				}
			}
			if (Main.mouseItem.ModItem is RobotAbility held && !held.IsAllowed(Player)) {
				Main.mouseItem.TurnToAir();
			}

			if (!Transformed) {
				return;
			}
			foreach (RobotAbility ability in RobotAbility.All) {
				if (ability.IsAllowed(Player)) {
					GiveAbility(ability.Type);
				}
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
			// Inside the heavenly gate: draw nothing at all.
			if (HiddenInGate) {
				foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.DrawOrder) {
					layer.Hide();
				}
				return;
			}
			if (!Transformed || Player.dead) {
				return;
			}

			// The Shift Titan is far bigger than the player, so the held item and wings (drawn at player size) are hidden too.
			bool titan = ActiveForm == RobotFormType.ShiftTitan;
			PlayerDrawLayer robotLayer = ModContent.GetInstance<RobotDrawLayer>();
			PlayerDrawLayer titanLayer = ModContent.GetInstance<TitanDrawLayer>();
			PlayerDrawLayer spiderLayer = ModContent.GetInstance<SpiderDrawLayer>();
			foreach (PlayerDrawLayer layer in PlayerDrawLayerLoader.DrawOrder) {
				if (layer == robotLayer
					|| layer == titanLayer
					|| layer == spiderLayer
					|| (!titan && layer == PlayerDrawLayers.HeldItem)
					|| layer == PlayerDrawLayers.ProjectileOverArm
					|| (!titan && layer == PlayerDrawLayers.Wings)
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
