using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using RobotJack.Content.Items.Akutoku;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.NPCs.Akutoku
{
	// Robitacons: robots infected by the world's evil, wearing the shells and powers of the Robot Jack forms.
	// Akutoku-ō births them during his fight (stronger forms as he weakens); the weaker ones also roam the
	// Corruption and Crimson in Hardmode. They walk like zombies and shoot bolts of their form's element.
	// Their sprites are the robot form sheets, darkened and scarred, with glowing infection.
	public abstract class Robitacon : ModNPC
	{
		// NPC types in order of strength: Robot, Blaze, Frost, Volt, Shadow, Nova, Omega, God.
		public static readonly int[] Types = new int[8];

		protected abstract int Tier { get; }                // 0..7
		protected abstract JackElement Element { get; }

		private ref float ShootTimer => ref NPC.localAI[0];
		private ref float ShootPose => ref NPC.localAI[1];

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 20;
			Types[Tier] = Type;
		}

		public override void SetDefaults() {
			NPC.width = 20;
			NPC.height = 42;
			NPC.aiStyle = -1;
			NPC.damage = 30 + Tier * 6;
			NPC.defense = 12 + Tier * 3;
			NPC.lifeMax = 300 + Tier * 120;
			NPC.knockBackResist = System.Math.Max(0.1f, 0.45f - Tier * 0.05f);
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.value = Item.buyPrice(silver: 10 + Tier * 10);
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				new FlavorTextBestiaryInfoElement("Mods.RobotJack.Bestiary.Robitacon"),
			});
		}

		// The first four also roam the Corruption and Crimson in Hardmode.
		public override float SpawnChance(NPCSpawnInfo spawnInfo) {
			if (Tier > 3 || !Main.hardMode || !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson)) {
				return 0f;
			}
			return 0.025f;
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<RobitaconParts>(), 2, 1, 2 + Tier / 2));
		}

		// Walks at the target, jumping over obstacles and gaps. Faster forms move faster.
		public override void AI() {
			NPC.TargetClosest();
			Player target = Main.player[NPC.target];
			float speed = 2.2f + Tier * 0.25f + (Element == JackElement.Volt ? 1f : 0f);
			int dir = target.Center.X >= NPC.Center.X ? 1 : -1;
			NPC.direction = NPC.spriteDirection = dir;
			NPC.velocity.X = MathHelper.Clamp(NPC.velocity.X + dir * 0.12f, -speed, speed);
			bool onGround = NPC.velocity.Y == 0f;
			if (onGround && (NPC.collideX || target.Bottom.Y < NPC.Top.Y - 40f && System.Math.Abs(target.Center.X - NPC.Center.X) < 160f)) {
				NPC.velocity.Y = -8.5f;
			}
			// Keep jumping over walls it runs into.
			if (NPC.collideX && NPC.velocity.Y < 0f) {
				NPC.velocity.X = dir * speed * 0.5f;
			}
		}

		public override void PostAI() {
			if (ShootPose > 0f) {
				ShootPose--;
			}
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				return;
			}
			Player target = Main.player[NPC.target];
			if (!target.active || target.dead) {
				return;
			}
			ShootTimer++;
			int rate = 150 - Tier * 10;
			if (ShootTimer < rate || Vector2.Distance(target.Center, NPC.Center) > 700f
				|| !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)) {
				return;
			}
			ShootTimer = 0;
			ShootPose = 15;
			NPC.netUpdate = true;
			Vector2 from = NPC.Center + new Vector2(NPC.direction * 12f, -6f);
			Vector2 aim = (target.Center - from).SafeNormalize(Vector2.UnitX * NPC.direction);
			int count = Tier >= 6 ? 3 : Tier >= 4 ? 2 : 1;
			int damage = 14 + Tier * 3;
			for (int i = 0; i < count; i++) {
				// Omega Robitacons fire a different element with every bolt.
				JackElement element = Tier == 6 ? (JackElement)Main.rand.Next(Elements.Count) : Element;
				Vector2 vel = aim.RotatedBy((i - (count - 1) / 2f) * 0.2f) * (8f + Tier * 0.5f);
				Projectile.NewProjectile(NPC.GetSource_FromAI(), from, vel, ModContent.ProjectileType<InfectedBolt>(), damage, 0f, Main.myPlayer, ai0: (int)element);
			}
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Item12 with { Pitch = -0.3f }, from);
		}

		public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo) {
			target.AddBuff(Elements.Debuff(Element), 120);
		}

		public override void HitEffect(NPC.HitInfo hit) {
			int count = NPC.life <= 0 ? 25 : 4;
			for (int i = 0; i < count; i++) {
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, i % 2 == 0 ? DustID.Silver : Elements.Dust(JackElement.Blight), hit.HitDirection * 2f, -2f);
			}
		}

		// Same frame layout as the player sheet: 0 idle, 3 arm forward (shooting), 5 in the air, 6-19 walking.
		public override void FindFrame(int frameHeight) {
			int frame;
			if (NPC.velocity.Y != 0f) {
				frame = 5;
			}
			else if (ShootPose > 0f) {
				frame = 3;
			}
			else if (System.Math.Abs(NPC.velocity.X) > 0.1f) {
				NPC.frameCounter += System.Math.Abs(NPC.velocity.X);
				frame = 6 + (int)(NPC.frameCounter / 6) % 14;
			}
			else {
				frame = 0;
			}
			NPC.frame.Y = frame * frameHeight;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor) {
			Texture2D tex = TextureAssets.Npc[Type].Value;
			Texture2D glow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
			Rectangle frame = NPC.frame;
			frame.Height = tex.Height / Main.npcFrameCount[Type];
			Vector2 position = new Vector2(NPC.Center.X, NPC.Bottom.Y + 4f + NPC.gfxOffY) - screenPos;
			Vector2 origin = new Vector2(frame.Width / 2f, frame.Height);
			SpriteEffects fx = NPC.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			spriteBatch.Draw(tex, position, frame, NPC.GetAlpha(drawColor), 0f, origin, 1f, fx, 0f);
			// The infection glows through the cracks, pulsing (purple in the Corruption, red in the Crimson).
			float pulse = 0.7f + 0.3f * (float)System.Math.Sin(Main.GameUpdateCount * 0.1f + NPC.whoAmI);
			spriteBatch.Draw(glow, position, frame, Elements.Main(JackElement.Blight) * pulse, 0f, origin, 1f, fx, 0f);
			return false;
		}
	}

	public class RobitaconRobot : Robitacon { protected override int Tier => 0; protected override JackElement Element => JackElement.Plasma; }
	public class RobitaconBlaze : Robitacon { protected override int Tier => 1; protected override JackElement Element => JackElement.Blaze; }
	public class RobitaconFrost : Robitacon { protected override int Tier => 2; protected override JackElement Element => JackElement.Frost; }
	public class RobitaconVolt : Robitacon { protected override int Tier => 3; protected override JackElement Element => JackElement.Volt; }
	public class RobitaconShadow : Robitacon { protected override int Tier => 4; protected override JackElement Element => JackElement.Shadow; }
	public class RobitaconNova : Robitacon { protected override int Tier => 5; protected override JackElement Element => JackElement.Nova; }
	public class RobitaconOmega : Robitacon { protected override int Tier => 6; protected override JackElement Element => JackElement.Prism; }
	public class RobitaconGod : Robitacon { protected override int Tier => 7; protected override JackElement Element => JackElement.Holy; }
}
