using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using RobotJack.Common;
using RobotJack.Content.Abilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Projectiles
{
	// The Energy Absorber's field. Stays on the player while left click is held. Each tick it:
	//  - marks the owner as absorbing, so every hit they take is neutralised into energy (RobotJackPlayer.FreeDodge),
	//  - pulls hostile projectiles in and swallows them, recording them for the release blast,
	//  - pulls enemies (by their knockback resistance, so most bosses won't budge) and PvP opponents toward the player,
	//  - drains anything inside the inner circle: damage (normal hits, so it works in PvP too), plus mana from players.
	public class AbsorbField : ModProjectile
	{
		public const float PullRadius = 420f;
		public const float DrainRadius = 110f;
		public const float SwallowRadius = 36f;
		public const float NpcPullSpeed = 3.5f;    // pixels per tick, times knockback resistance
		public const float PlayerPullAccel = 0.45f;

		private static readonly Color FieldColor = new Color(150, 90, 255);
		private static readonly Color CoreColor = new Color(120, 235, 255);

		private static Asset<Texture2D> ringTex, glowTex, beamTex;

		private ref float Timer => ref Projectile.ai[0];

		public override void SetStaticDefaults() {
			ProjectileID.Sets.DrawScreenCheckFluff[Type] = (int)PullRadius + 200;
			if (!Main.dedServ) {
				ringTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalRing");
				glowTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalGlow");
				beamTex = ModContent.Request<Texture2D>("RobotJack/Content/Projectiles/OrbitalBeam");
			}
		}

		public override void Unload() {
			ringTex = glowTex = beamTex = null;
		}

		public override void SetDefaults() {
			Projectile.width = 20;
			Projectile.height = 20;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 10;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 12;
			Projectile.aiStyle = -1;
		}

		public override bool ShouldUpdatePosition() => false;

		public override bool? CanCutTiles() => false;

		public override void AI() {
			Player player = Main.player[Projectile.owner];
			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();

			if (Projectile.owner == Main.myPlayer) {
				bool holding = player.channel && !player.noItems && !player.CCed && !player.dead
					&& player.HeldItem.type == ModContent.ItemType<EnergyAbsorber>() && modPlayer.Transformed;
				if (!holding) {
					Projectile.Kill();
					return;
				}
				modPlayer.absorbTimer = 2;
			}

			Timer++;
			Projectile.timeLeft = 10;
			Projectile.Center = player.MountedCenter;
			player.heldProj = Projectile.whoAmI;
			player.itemTime = 2;
			player.itemAnimation = 2;

			if (Timer % 24 == 1) {
				SoundEngine.PlaySound(SoundID.Item15 with { Pitch = -0.8f, Volume = 0.6f }, Projectile.Center);
			}

			if (Main.netMode != NetmodeID.MultiplayerClient) {
				PullNPCs();
			}
			PullLocalPlayer(player);
			if (Projectile.owner == Main.myPlayer) {
				SwallowProjectiles(player, modPlayer);
				DrainPlayersForEnergy(player, modPlayer);
			}
			Effects();
		}

		// Enemy positions belong to the server (or single player), so only it moves them.
		private void PullNPCs() {
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.lifeMax <= 5 || npc.knockBackResist <= 0f) {
					continue;
				}
				Vector2 toPlayer = Projectile.Center - npc.Center;
				float dist = toPlayer.Length();
				if (dist > PullRadius || dist < DrainRadius * 0.5f) {
					continue;
				}
				Vector2 move = toPlayer / dist * NpcPullSpeed * npc.knockBackResist;
				if (!npc.noTileCollide) {
					move = Collision.TileCollision(npc.position, move, npc.width, npc.height);
				}
				npc.position += move;
				if (Timer % 6 == 0) {
					npc.netUpdate = true;
				}
			}
		}

		// Players move themselves, so every client pulls its own player if it's a PvP opponent of the owner.
		private void PullLocalPlayer(Player owner) {
			if (Main.netMode == NetmodeID.Server || Projectile.owner == Main.myPlayer) {
				return;
			}
			Player me = Main.LocalPlayer;
			if (!IsOpponent(owner, me)) {
				return;
			}
			Vector2 toOwner = Projectile.Center - me.Center;
			float dist = toOwner.Length();
			if (dist > PullRadius) {
				return;
			}
			if (dist > DrainRadius * 0.5f) {
				me.velocity += toOwner / dist * PlayerPullAccel;
			}
			if (dist < DrainRadius && me.statMana > 0) {
				me.statMana = System.Math.Max(0, me.statMana - 1); // drained of mana too
				me.manaRegenDelay = 60;
			}
		}

		public static bool IsOpponent(Player owner, Player other) {
			return other.active && !other.dead && other.whoAmI != owner.whoAmI
				&& owner.hostile && other.hostile && (owner.team == 0 || other.team != owner.team);
		}

		// Hostile projectiles (and PvP opponents' projectiles) are dragged in and swallowed.
		// Only removed on this client: they can't hurt this player any more, but other players may still see them.
		private void SwallowProjectiles(Player owner, RobotJackPlayer modPlayer) {
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (!proj.active || proj.damage <= 0 || proj.whoAmI == Projectile.whoAmI) {
					continue;
				}
				bool enemyShot = proj.hostile || (proj.friendly && proj.owner != owner.whoAmI && proj.owner < Main.maxPlayers && IsOpponent(owner, Main.player[proj.owner]));
				if (!enemyShot) {
					continue;
				}

				Vector2 toPlayer = Projectile.Center - proj.Center;
				float dist = toPlayer.Length();
				if (dist > PullRadius) {
					continue;
				}
				if (dist < SwallowRadius + proj.width / 2f) {
					modPlayer.AddEnergy(proj.damage);
					modPlayer.Record(proj.type, proj.damage);
					for (int d = 0; d < 6; d++) {
						Dust.NewDustPerfect(proj.Center, DustID.Electric, Main.rand.NextVector2Circular(2f, 2f)).noGravity = true;
					}
					proj.active = false;
					continue;
				}
				float speed = System.Math.Max(proj.velocity.Length(), 8f);
				proj.velocity = Vector2.Lerp(proj.velocity, toPlayer / dist * speed, 0.15f);
			}
		}

		// Draining a PvP opponent gives energy too (enemy drain energy comes from OnHitNPC).
		private void DrainPlayersForEnergy(Player owner, RobotJackPlayer modPlayer) {
			if (Timer % Projectile.localNPCHitCooldown != 0) {
				return;
			}
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player other = Main.player[i];
				if (IsOpponent(owner, other) && Vector2.Distance(other.Center, Projectile.Center) < DrainRadius) {
					modPlayer.AddEnergy(Projectile.damage);
				}
			}
		}

		// Only things inside the drain circle take damage.
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
			float x = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
			float y = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
			return Vector2.Distance(Projectile.Center, new Vector2(x, y)) < DrainRadius;
		}

		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			Main.player[Projectile.owner].GetModPlayer<RobotJackPlayer>().AddEnergy(damageDone);
			for (int i = 0; i < 5; i++) {
				Vector2 from = target.Center + Main.rand.NextVector2Circular(target.width / 2f, target.height / 2f);
				Dust dust = Dust.NewDustPerfect(from, DustID.Electric, (Projectile.Center - from) * 0.08f, Scale: 1.1f);
				dust.noGravity = true;
			}
		}

		private void Effects() {
			// Particles spiralling in from the edge of the field.
			for (int i = 0; i < 3; i++) {
				Vector2 offset = Main.rand.NextVector2CircularEdge(PullRadius, PullRadius) * Main.rand.NextFloat(0.4f, 1f);
				Vector2 vel = -offset * 0.04f + offset.RotatedBy(MathHelper.PiOver2) * 0.02f;
				Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Electric, vel, Scale: 0.8f);
				dust.noGravity = true;
			}
			Lighting.AddLight(Projectile.Center, 0.5f, 0.3f, 0.9f);
		}

		// ------------------------------------------------------------------ drawing

		private static Color Glow(Color color, float opacity) => new Color(color.R, color.G, color.B, 0) * opacity;

		public override bool PreDraw(ref Color lightColor) {
			Vector2 center = Projectile.Center - Main.screenPosition;
			Texture2D ring = ringTex.Value;
			Texture2D glow = glowTex.Value;
			Texture2D beam = beamTex.Value;
			float appear = MathHelper.Clamp(Timer / 15f, 0f, 1f);
			float energy = Main.player[Projectile.owner].GetModPlayer<RobotJackPlayer>().energy / RobotJackPlayer.MaxEnergy;

			// Outer pull boundary and rings shrinking inward, like a vortex.
			float outerScale = PullRadius * 2f / ring.Width * appear;
			Main.EntitySpriteDraw(ring, center, null, Glow(FieldColor, 0.25f), Timer * 0.01f, ring.Size() / 2f, outerScale, SpriteEffects.None, 0);
			for (int i = 0; i < 3; i++) {
				float t = (Timer / 60f + i / 3f) % 1f;
				float scale = MathHelper.Lerp(PullRadius * 2f, 30f, t) / ring.Width * appear;
				Main.EntitySpriteDraw(ring, center, null, Glow(FieldColor, 0.5f * t), -Timer * 0.03f, ring.Size() / 2f, scale, SpriteEffects.None, 0);
			}

			// Drain circle.
			float pulse = 1f + 0.08f * (float)System.Math.Sin(Timer * 0.3f);
			Main.EntitySpriteDraw(ring, center, null, Glow(CoreColor, 0.7f), Timer * 0.05f, ring.Size() / 2f, DrainRadius * 2f / ring.Width * pulse * appear, SpriteEffects.None, 0);

			// Tethers to everything being drained.
			for (int i = 0; i < Main.maxNPCs; i++) {
				NPC npc = Main.npc[i];
				if (npc.active && !npc.friendly && npc.lifeMax > 5 && Vector2.Distance(npc.Center, Projectile.Center) < DrainRadius + npc.width / 2f) {
					DrawTether(beam, center, npc.Center - Main.screenPosition);
				}
			}
			Player owner = Main.player[Projectile.owner];
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player other = Main.player[i];
				if (IsOpponent(owner, other) && Vector2.Distance(other.Center, Projectile.Center) < DrainRadius) {
					DrawTether(beam, center, other.Center - Main.screenPosition);
				}
			}

			// Core that grows with stored energy.
			float core = (0.5f + energy * 1.5f) * pulse * appear;
			Main.EntitySpriteDraw(glow, center, null, Glow(FieldColor, 0.8f), 0f, glow.Size() / 2f, core * 1.6f, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Glow(CoreColor, 0.9f), 0f, glow.Size() / 2f, core, SpriteEffects.None, 0);
			Main.EntitySpriteDraw(glow, center, null, Glow(Color.White, 1f), 0f, glow.Size() / 2f, core * 0.4f, SpriteEffects.None, 0);
			return false;
		}

		private void DrawTether(Texture2D beam, Vector2 from, Vector2 to) {
			Vector2 diff = to - from;
			float length = diff.Length();
			if (length < 1f) {
				return;
			}
			float width = 6f + 3f * (float)System.Math.Sin(Timer * 0.5f);
			// The beam texture is a horizontal cross-section, so rotate it to run along the tether.
			float rotation = diff.ToRotation() - MathHelper.PiOver2;
			Main.EntitySpriteDraw(beam, from, null, Glow(CoreColor, 0.8f), rotation, new Vector2(beam.Width / 2f, 0f),
				new Vector2(width / beam.Width, length / beam.Height), SpriteEffects.None, 0);
		}
	}
}
