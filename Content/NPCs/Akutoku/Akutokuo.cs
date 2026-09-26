using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using RobotJack.Common.Akutoku;
using RobotJack.Content.Buffs;
using RobotJack.Content.Items.Akutoku;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.NPCs.Akutoku
{
	// Akutoku-ō: a colossal robot spider. He created the world's Corruption (or Crimson), and then succumbed to its
	// infection himself. He walks on eight legs, and his two front legs turn into weapons (rocket launcher, laser
	// cannon, axe, sword, flamethrower, buzzsaw, tesla coil), changing between them with a transformation animation.
	// He births infected Robitacons, and every hit he lands Siphons you: you deal 50% less damage to him and he
	// steals that power for +50% damage. Early Hardmode boss, summoned with an Infected Core.
	[AutoloadBossHead]
	public class Akutokuo : ModNPC
	{
		public const float RigScale = 3f;
		public const int SwingTicks = 24;

		public readonly SpiderRig Rig = new SpiderRig { Scale = RigScale };

		// ai[0]: general timer. ai[1]: ticks until the weapons change. ai[2]: both weapons (slot 0 + slot 1 * 8).
		// ai[3]: 1 once in the second phase.
		private ref float Timer => ref NPC.ai[0];
		private ref float WeaponChangeTimer => ref NPC.ai[1];
		private bool SecondPhase {
			get => NPC.ai[3] == 1f;
			set => NPC.ai[3] = value ? 1f : 0f;
		}

		private LegWeapon SyncedWeapon(int slot) => (LegWeapon)(slot == 0 ? (int)NPC.ai[2] % 8 : (int)NPC.ai[2] / 8);

		// Server-side attack state per weapon leg.
		private readonly int[] cooldown = { 60, 90 };
		private readonly int[] flameTicks = new int[2];
		private int minionTimer = 300;

		public bool Empowered => NPC.HasBuff(ModContent.BuffType<StolenPower>());

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 1;
			NPCID.Sets.MPAllowedEnemies[Type] = true;
			NPCID.Sets.BossBestiaryPriority.Add(Type);
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
			NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
		}

		public override void SetDefaults() {
			NPC.width = 330;
			NPC.height = 150;
			NPC.damage = 50;
			NPC.defense = 30;
			NPC.lifeMax = 40000;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0f;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.value = Item.buyPrice(gold: 15);
			NPC.SpawnWithHigherTime(30);
			NPC.boss = true;
			NPC.npcSlots = 15f;
			NPC.aiStyle = -1;
			NPC.ai[2] = (int)LegWeapon.RocketLauncher + (int)LegWeapon.Sword * 8;
			if (!Main.dedServ) {
				Music = MusicID.Boss2;
			}
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				new FlavorTextBestiaryInfoElement("Mods.RobotJack.Bestiary.Akutokuo"),
			});
		}

		public override void BossLoot(ref int potionType) {
			potionType = ItemID.GreaterHealingPotion;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<AkutokuBag>()));
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AkutokuTrophy>(), 10));
			LeadingConditionRule notExpert = new LeadingConditionRule(new Conditions.NotExpert());
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<AkutokuMask>(), 7));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SpiderTrigger>()));
			notExpert.OnSuccess(ItemDropRule.Common(ModContent.ItemType<RobitaconParts>(), 1, 20, 30));
			notExpert.OnSuccess(ItemDropRule.OneFromOptions(1, AkutokuBag.LegWeaponItems()));
			npcLoot.Add(notExpert);
		}

		public override void OnKill() {
			NPC.SetEventFlagCleared(ref AkutokuSystem.downedAkutoku, -1);
			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.WorldData);
			}
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) {
			cooldownSlot = ImmunityCooldownID.Bosses;
			return true;
		}

		// ------------------------------------------------------------------ Siphon and Stolen Power

		private void Siphon(Player player, ref NPC.HitModifiers modifiers) {
			if (player != null && player.active && player.HasBuff(ModContent.BuffType<Siphoned>())) {
				modifiers.FinalDamage *= 0.5f;
				NPC.AddBuff(ModContent.BuffType<StolenPower>(), 5 * 60);
			}
		}

		public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers) => Siphon(player, ref modifiers);

		public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers) {
			if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers) {
				Siphon(Main.player[projectile.owner], ref modifiers);
			}
		}

		public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers) {
			if (Empowered) {
				modifiers.SourceDamage *= 1.5f;
			}
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) => AkutokuHit.OnHit(target);

		private int Damage(int baseDamage) => (int)(baseDamage * (Empowered ? 1.5f : 1f));

		// ------------------------------------------------------------------ AI

		public override void AI() {
			if (NPC.target < 0 || NPC.target == 255 || Main.player[NPC.target].dead || !Main.player[NPC.target].active) {
				NPC.TargetClosest();
			}
			Player player = Main.player[NPC.target];
			if (player.dead || !player.active || Vector2.Distance(player.Center, NPC.Center) > 6000f) {
				// Crawl away into the ground.
				NPC.velocity.Y += 0.2f;
				NPC.EncourageDespawn(10);
				UpdateRig(player);
				return;
			}

			Timer++;
			if (Timer == 1) {
				SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
			}
			if (!SecondPhase && NPC.life < NPC.lifeMax / 2) {
				EnterSecondPhase();
			}

			NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
			Move(player);
			UpdateRig(player);

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				ChangeWeapons();
				for (int slot = 0; slot < 2; slot++) {
					Attack(player, slot);
				}
				SpawnMinions();
				if (SecondPhase && Timer % 100 == 0) {
					Spit(player);
				}
			}

			// Infection oozes off him (more when empowered).
			if (Main.rand.NextBool(Empowered ? 1 : 3)) {
				Dust d = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, Elements.Dust(JackElement.Blight), 0f, 2f, Scale: 1.3f);
				d.noGravity = !Empowered;
			}
			Lighting.AddLight(NPC.Center, Elements.Main(JackElement.Blight).ToVector3() * 0.8f);
		}

		private void EnterSecondPhase() {
			SecondPhase = true;
			SoundEngine.PlaySound(SoundID.Roar with { Pitch = -0.5f }, NPC.Center);
			if (!Main.dedServ) {
				Main.instance.CameraModifiers.Add(new PunchCameraModifier(NPC.Center, Vector2.UnitY, 18f, 6f, 40, 2400f, FullName));
			}
			for (int i = 0; i < 80; i++) {
				Dust.NewDustPerfect(NPC.Center, Elements.Dust(JackElement.Blight), Main.rand.NextVector2Circular(14f, 14f), Scale: 2f).noGravity = true;
			}
			WeaponChangeTimer = 0;
			minionTimer = 30;
			NPC.netUpdate = true;
		}

		// Walks along the ground toward the player (keeping at range, or closing in with melee weapons),
		// climbs up after players high above, and hovers on its legs over gaps.
		private void Move(Player player) {
			bool melee = IsMelee(SyncedWeapon(0)) || IsMelee(SyncedWeapon(1));
			float keepAway = melee ? 160f : 380f;
			int side = player.Center.X >= NPC.Center.X ? -1 : 1;
			float desiredX = player.Center.X + side * keepAway;

			float? ground = FindGround(NPC.Center, 700f);
			float desiredY;
			if (ground.HasValue && player.Bottom.Y > ground.Value - 520f) {
				desiredY = ground.Value - 190f;
			}
			else {
				desiredY = player.Center.Y - 60f; // climbing after the player / crossing a chasm
			}

			float maxSpeed = SecondPhase ? 9f : 7f;
			float ax = (desiredX - NPC.Center.X) * 0.01f;
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + MathHelper.Clamp(ax, -0.35f, 0.35f), -maxSpeed, maxSpeed);
			if (System.Math.Abs(desiredX - NPC.Center.X) < 40f) {
				NPC.velocity.X *= 0.9f;
			}
			NPC.velocity.Y = MathHelper.Clamp((desiredY - NPC.Center.Y) * 0.06f, -10f, 10f);
		}

		private static float? FindGround(Vector2 from, float maxDown) {
			int x = (int)(from.X / 16f);
			for (int y = (int)(from.Y / 16f); y < (int)((from.Y + maxDown) / 16f); y++) {
				if (!WorldGen.InWorld(x, y, 2)) {
					return null;
				}
				Tile tile = Main.tile[x, y];
				if (tile.HasTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])) {
					return y * 16f;
				}
			}
			return null;
		}

		private void UpdateRig(Player player) {
			for (int slot = 0; slot < 2; slot++) {
				LegWeapon synced = SyncedWeapon(slot);
				if (Rig.Weapons[slot] != synced && !Rig.Swapping(slot)) {
					Rig.StartSwap(slot, synced);
				}
				// Aim smoothly at the player (a locked laser overrides this itself).
				Vector2 wanted = (player.Center - Rig.WeaponMount(slot)).SafeNormalize(Vector2.UnitX * NPC.direction);
				Rig.Aim[slot] = Vector2.Lerp(Rig.Aim[slot], wanted, 0.12f).SafeNormalize(wanted);
			}
			Rig.GlowColor = Elements.Main(JackElement.Blight);
			Rig.Update(NPC.Center + NPC.velocity, NPC.direction, RigScale, !Main.dedServ);
		}

		private static bool IsMelee(LegWeapon weapon) => weapon == LegWeapon.Axe || weapon == LegWeapon.Sword;

		// Every few seconds both front legs transform into new weapons (never the same two).
		private void ChangeWeapons() {
			if (--WeaponChangeTimer > 0) {
				return;
			}
			WeaponChangeTimer = SecondPhase ? 360 : 480;
			int w0 = Main.rand.Next(SpiderRig.WeaponCount);
			int w1;
			do {
				w1 = Main.rand.Next(SpiderRig.WeaponCount);
			} while (w1 == w0);
			NPC.ai[2] = w0 + w1 * 8;
			cooldown[0] = 90;
			cooldown[1] = 110;
			flameTicks[0] = flameTicks[1] = 0;
			NPC.netUpdate = true;
		}

		private void Attack(Player player, int slot) {
			if (Rig.Swapping(slot)) {
				return;
			}
			var source = NPC.GetSource_FromAI();
			Vector2 tip = Rig.WeaponTip(slot);
			Vector2 aim = Rig.Aim[slot];
			float distance = Vector2.Distance(player.Center, NPC.Center);
			LegWeapon weapon = Rig.Weapons[slot];

			// The flamethrower keeps spraying for a while once started.
			if (flameTicks[slot] > 0) {
				flameTicks[slot]--;
				if (flameTicks[slot] % 4 == 0) {
					Vector2 vel = aim.RotatedByRandom(0.15f) * Main.rand.NextFloat(9f, 12f);
					Projectile.NewProjectile(source, tip, vel, ModContent.ProjectileType<InfectedFlame>(), Damage(18), 0f, Main.myPlayer);
				}
				if (flameTicks[slot] % 12 == 0) {
					SoundEngine.PlaySound(SoundID.Item34, tip);
				}
				return;
			}
			if (--cooldown[slot] > 0) {
				return;
			}
			float faster = SecondPhase ? 0.75f : 1f;
			switch (weapon) {
				case LegWeapon.RocketLauncher: {
					int count = SecondPhase ? 3 : 1;
					for (int i = 0; i < count; i++) {
						Vector2 vel = aim.RotatedBy((i - (count - 1) / 2f) * 0.3f) * 8f;
						Projectile.NewProjectile(source, tip, vel, ModContent.ProjectileType<InfectedRocket>(), Damage(22), 0f, Main.myPlayer, ai0: player.whoAmI);
					}
					SoundEngine.PlaySound(SoundID.Item11 with { Pitch = -0.5f }, tip);
					Rig.Recoil[slot] = 8f;
					cooldown[slot] = (int)(75 * faster);
					break;
				}
				case LegWeapon.LaserCannon:
					Projectile.NewProjectile(source, tip, aim, ModContent.ProjectileType<InfectedLaser>(), Damage(30), 0f, Main.myPlayer, ai1: NPC.whoAmI, ai2: slot);
					cooldown[slot] = (int)(170 * faster);
					break;
				case LegWeapon.Axe:
				case LegWeapon.Sword:
					if (distance < 520f) {
						Projectile.NewProjectile(source, NPC.Center, Vector2.Zero, ModContent.ProjectileType<InfectedSwing>(),
							Damage(weapon == LegWeapon.Axe ? 38 : 30), 0f, Main.myPlayer, ai1: NPC.whoAmI, ai2: slot);
						SoundEngine.PlaySound(SoundID.Item71 with { Pitch = weapon == LegWeapon.Axe ? -0.6f : -0.2f }, tip);
						if (weapon == LegWeapon.Axe) {
							// The axe cleaves into the ground in front of him.
							float? ground = FindGround(tip, 500f);
							if (ground.HasValue) {
								Projectile.NewProjectile(source, new Vector2(tip.X + NPC.direction * 60f, ground.Value), Vector2.Zero,
									ModContent.ProjectileType<InfectedBlast>(), Damage(26), 0f, Main.myPlayer, ai0: 130f);
							}
						}
						cooldown[slot] = (int)((weapon == LegWeapon.Axe ? 80 : 55) * faster);
					}
					else if (weapon == LegWeapon.Sword) {
						Projectile.NewProjectile(source, tip, aim * 9f, ModContent.ProjectileType<InfectedSlashWave>(), Damage(24), 0f, Main.myPlayer);
						SoundEngine.PlaySound(SoundID.Item71, tip);
						cooldown[slot] = (int)(90 * faster);
					}
					else {
						cooldown[slot] = 20;
					}
					break;
				case LegWeapon.Flamethrower:
					if (distance < 650f) {
						flameTicks[slot] = 60;
						cooldown[slot] = (int)(100 * faster);
					}
					else {
						cooldown[slot] = 20;
					}
					break;
				case LegWeapon.Buzzsaw:
					Projectile.NewProjectile(source, tip, aim * 12f, ModContent.ProjectileType<InfectedSaw>(), Damage(28), 0f, Main.myPlayer, ai1: NPC.whoAmI, ai2: slot);
					cooldown[slot] = (int)(140 * faster);
					break;
				case LegWeapon.TeslaCoil: {
					Vector2 spot = player.Center + player.velocity * 20f;
					Projectile.NewProjectile(source, tip, spot, ModContent.ProjectileType<InfectedTesla>(), Damage(30), 0f, Main.myPlayer, ai1: NPC.whoAmI, ai2: slot);
					SoundEngine.PlaySound(SoundID.Item93, tip);
					cooldown[slot] = (int)(100 * faster);
					break;
				}
			}
		}

		// Infected bolts spat from the head (second phase).
		private void Spit(Player player) {
			Vector2 mouth = NPC.Center + new Vector2(NPC.direction * 200f, 0f);
			Vector2 aim = (player.Center - mouth).SafeNormalize(Vector2.UnitX * NPC.direction);
			for (int i = -2; i <= 2; i++) {
				Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, aim.RotatedBy(i * 0.18f) * 10f, ModContent.ProjectileType<InfectedBolt>(),
					Damage(18), 0f, Main.myPlayer, ai0: (int)JackElement.Blight, ai1: 1f);
			}
			SoundEngine.PlaySound(SoundID.NPCDeath13, mouth);
		}

		// Robitacons crawl out of his abdomen. Stronger robot forms join in as he gets weaker.
		private void SpawnMinions() {
			if (--minionTimer > 0) {
				return;
			}
			minionTimer = SecondPhase ? 420 : 600;
			int alive = 0;
			foreach (NPC other in Main.ActiveNPCs) {
				if (other.ModNPC is Robitacon) {
					alive++;
				}
			}
			int max = SecondPhase ? 8 : 6;
			int count = System.Math.Min(SecondPhase ? 3 : 2, max - alive);
			float life = NPC.life / (float)NPC.lifeMax;
			int options = life > 0.7f ? 4 : life > 0.35f ? 6 : 8;
			Vector2 abdomen = NPC.Center - new Vector2(NPC.direction * 150f, -20f);
			for (int i = 0; i < count; i++) {
				int type = Robitacon.Types[Main.rand.Next(options)];
				int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)abdomen.X + Main.rand.Next(-40, 41), (int)abdomen.Y, type);
				if (index < Main.maxNPCs) {
					Main.npc[index].velocity = new Vector2(Main.rand.NextFloat(-5f, 5f), -6f);
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.SyncNPC, number: index);
					}
				}
			}
			if (count > 0) {
				SoundEngine.PlaySound(SoundID.NPCDeath13 with { Pitch = -0.4f }, abdomen);
				for (int i = 0; i < 30; i++) {
					Dust.NewDustPerfect(abdomen, Elements.Dust(JackElement.Blight), Main.rand.NextVector2Circular(6f, 6f), Scale: 1.6f);
				}
			}
		}

		public override void SendExtraAI(BinaryWriter writer) {
			writer.Write(minionTimer);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			minionTimer = reader.ReadInt32();
		}

		public override void HitEffect(NPC.HitInfo hit) {
			for (int i = 0; i < 4; i++) {
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Silver, hit.HitDirection * 2f, -1f);
			}
			if (NPC.life <= 0) {
				for (int i = 0; i < 120; i++) {
					Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, i % 2 == 0 ? DustID.Smoke : Elements.Dust(JackElement.Blight),
						Main.rand.NextFloat(-10f, 10f), Main.rand.NextFloat(-10f, 4f), Scale: 2f);
				}
			}
		}

		// ------------------------------------------------------------------ drawing

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			if (NPC.IsABestiaryIconDummy) {
				return true; // the bestiary shows the preview picture
			}
			if (Empowered) {
				// Stolen Power: a pulsing aura around the body.
				Texture2D glow = ElementFX.Glow;
				float pulse = 0.6f + 0.2f * (float)System.Math.Sin(Main.GameUpdateCount * 0.2f);
				spriteBatch.Draw(glow, NPC.Center - screenPos, null, ElementFX.Additive(Elements.Main(JackElement.Blight), pulse), 0f, glow.Size() / 2f,
					new Vector2(760f / glow.Width, 420f / glow.Height), SpriteEffects.None, 0f);
			}
			Rig.GlowColor = Elements.Main(JackElement.Blight) * (Empowered ? 1.4f : 1f);
			Rig.Draw(spriteBatch, screenPos, drawColor);
			return false;
		}
	}
}
