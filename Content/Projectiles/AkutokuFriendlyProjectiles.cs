using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// The Spider Buzzsaw's thrown saw blade: flies out and comes back like a boomerang, bouncing off enemies.
	public class SpiderSawBlade : ModProjectile
	{
		public override string Texture => "RobotJack/Content/NPCs/Akutoku/SawBlade";

		public override void SetDefaults() {
			Projectile.width = 30;
			Projectile.height = 30;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = -1;
			Projectile.aiStyle = 3; // the vanilla boomerang AI
			AIType = ProjectileID.WoodenBoomerang;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, JackElement.Blight);
			for (int i = 0; i < 5; i++) {
				Dust.NewDustPerfect(target.Center, DustID.Electric, Main.rand.NextVector2Circular(4f, 4f), Scale: 0.9f).noGravity = true;
			}
		}
	}

	// A cleansed Robitacon fighting for you (Robitacon Remote). Each one wears a different Robot Jack form,
	// hovers near you on its jets and shoots bolts of its element at enemies.
	public class RobitaconMinion : ModProjectile
	{
		private static readonly string[] Sheets = { "Robot", "BlazeJack", "FrostJack", "VoltJack", "ShadowJack", "NovaJack" };
		private static readonly JackElement[] ElementsBySheet = { JackElement.Plasma, JackElement.Blaze, JackElement.Frost, JackElement.Volt, JackElement.Shadow, JackElement.Nova };

		private int Variant => Projectile.minionPos % Sheets.Length;
		private ref float ShootTimer => ref Projectile.ai[0];

		public override string Texture => "RobotJack/Content/Projectiles/ElementBolt"; // drawn from the player form sheets instead

		public override void SetStaticDefaults() {
			ProjectileID.Sets.MinionTargettingFeature[Type] = true;
			Main.projPet[Type] = true;
			ProjectileID.Sets.MinionSacrificable[Type] = true;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 42;
			Projectile.tileCollide = false;
			Projectile.friendly = true;
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
		}

		public override bool? CanCutTiles() => false;

		public override bool MinionContactDamage() => false;

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
			if (owner.dead || !owner.active) {
				owner.ClearBuff(ModContent.BuffType<RobitaconMinionBuff>());
				return;
			}
			if (owner.HasBuff(ModContent.BuffType<RobitaconMinionBuff>())) {
				Projectile.timeLeft = 2;
			}

			// Hover in a row behind the player.
			Vector2 idle = owner.Center + new Vector2(-owner.direction * (40 + Projectile.minionPos * 34), -50f - (Projectile.minionPos % 2) * 20f);
			Vector2 toIdle = idle - Projectile.Center;
			if (toIdle.Length() > 2000f) {
				Projectile.Center = idle;
				Projectile.netUpdate = true;
			}

			NPC target = FindTarget(owner, 800f);
			if (target != null) {
				// Keep some distance from the target and shoot at it.
				Vector2 spot = target.Center + new Vector2(target.Center.X > owner.Center.X ? -220f : 220f, -120f);
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, (spot - Projectile.Center) * 0.05f, 0.1f);
				Projectile.direction = target.Center.X >= Projectile.Center.X ? 1 : -1;
				if (++ShootTimer >= 40 && Projectile.owner == Main.myPlayer) {
					ShootTimer = 0;
					Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 13f;
					int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, aim, ModContent.ProjectileType<ElementBolt>(),
						Projectile.damage, Projectile.knockBack, Projectile.owner, ai0: (int)ElementsBySheet[Variant]);
					if (index < Main.maxProjectiles) {
						Main.projectile[index].DamageType = DamageClass.Summon;
					}
				}
			}
			else {
				Projectile.velocity = Vector2.Lerp(Projectile.velocity, toIdle * 0.08f, 0.1f);
				Projectile.direction = owner.direction;
			}
			if (Projectile.velocity.Length() > 14f) {
				Projectile.velocity = Vector2.Normalize(Projectile.velocity) * 14f;
			}
			// Jet flames.
			if (Main.rand.NextBool(2)) {
				Dust.NewDustPerfect(Projectile.Bottom, DustID.Torch, new Vector2(0f, 2f), Scale: 1f).noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, Elements.Main(ElementsBySheet[Variant]).ToVector3() * 0.4f);
		}

		private NPC FindTarget(Player owner, float range) {
			if (owner.HasMinionAttackTargetNPC) {
				NPC chosen = Main.npc[owner.MinionAttackTargetNPC];
				if (Vector2.Distance(chosen.Center, Projectile.Center) < range * 1.5f) {
					return chosen;
				}
			}
			NPC best = null;
			foreach (NPC npc in Main.ActiveNPCs) {
				float d = Vector2.Distance(npc.Center, Projectile.Center);
				if (npc.CanBeChasedBy(Projectile) && d < range) {
					range = d;
					best = npc;
				}
			}
			return best;
		}

		public override bool PreDraw(ref Color lightColor) {
			// Drawn like the player's robot forms: legs and body in the airborne pose (frame 5), with their glow.
			string sheet = "RobotJack/Content/Players/" + Sheets[Variant];
			Rectangle frame = new Rectangle(0, 5 * 56, 40, 56);
			Vector2 position = new Vector2(Projectile.Center.X, Projectile.Bottom.Y + 4f) - Main.screenPosition;
			Vector2 origin = new Vector2(20f, 56f);
			SpriteEffects fx = Projectile.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			foreach ((string part, bool glow) in new[] { ("Legs", false), ("Legs_Glow", true), ("Body", false), ("Body_Glow", true) }) {
				Texture2D tex = ModContent.Request<Texture2D>(sheet + part).Value;
				Main.EntitySpriteDraw(tex, position, frame, glow ? Color.White : lightColor, 0f, origin, 1f, fx, 0);
			}
			return false;
		}
	}
}
