using Microsoft.Xna.Framework;
using RobotJack.Content.Projectiles;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Common.Titan
{
	// The Shift Titan's state on a player: which trigger is merged in, how it's customized, whether it's in car mode,
	// its giant hitbox, and its animation. Fusion, look and car mode are synced to other players in multiplayer.
	public class TitanPlayer : ModPlayer
	{
		public TitanFusion fusion;
		public TitanLook look = TitanLook.Default;
		public bool carMode;

		// Animation (every client works these out for every player, from their movement).
		public float walkPhase;
		public float wheelAngle;

		private int noRoomMessageCooldown;

		public bool IsTitan => Player.GetModPlayer<RobotJackPlayer>().ActiveForm == RobotFormType.ShiftTitan;

		// Titan (or car) and actually at full size (there was room to grow).
		public bool IsFullSize => IsTitan && Player.width >= (carMode ? TitanData.CarWidth : TitanData.TitanWidth);

		public JackElement Element => TitanData.Element(fusion);

		public override void PreUpdate() {
			UpdateSize();
		}

		public override void UpdateDead() {
			carMode = false;
			UpdateSize();
		}

		// The titan's hitbox is truly 10x the player's (the car is 20x wide, 4x tall). It only grows when there's room,
		// keeping the feet where they were; until then you stay normal size (and are told why).
		private void UpdateSize() {
			bool titan = IsTitan && !Player.dead && !Player.mount.Active;
			int width = titan ? (carMode ? TitanData.CarWidth : TitanData.TitanWidth) : Player.defaultWidth;
			int height = titan ? (carMode ? TitanData.CarHeight : TitanData.TitanHeight) : Player.defaultHeight;
			if (Player.mount.Active) {
				height = Player.height;
			}
			if (Player.width == width && Player.height == height) {
				return;
			}

			Vector2 position = Player.position;
			position.X += (Player.width - width) / 2f;
			if (Player.gravDir == 1f) {
				position.Y += Player.height - height;
			}

			bool growing = width > Player.width || height > Player.height;
			if (growing && (Collision.SolidCollision(position, width, height) || !InsideWorld(position, width, height))) {
				if (Player.whoAmI == Main.myPlayer && noRoomMessageCooldown <= 0) {
					CombatText.NewText(Player.getRect(), Color.OrangeRed, carMode ? "Not enough room for the car!" : "Not enough room to become a titan!");
					noRoomMessageCooldown = 180;
				}
				return;
			}

			Player.position = position;
			Player.width = width;
			Player.height = height;
		}

		private static bool InsideWorld(Vector2 position, int width, int height) {
			return position.X > 16 * 42 && position.Y > 16 * 42
				&& position.X + width < (Main.maxTilesX - 42) * 16 && position.Y + height < (Main.maxTilesY - 42) * 16;
		}

		// Giant strides: when walking into a ledge up to 6 blocks high, step up onto it instead of stopping.
		public override void PreUpdateMovement() {
			if (!IsFullSize || Player.velocity.Y != 0f) {
				return;
			}
			int dir = Player.controlRight ? 1 : Player.controlLeft ? -1 : 0;
			if (dir == 0) {
				return;
			}
			Vector2 ahead = Player.position + new Vector2(dir * 6f, 0f);
			if (!Collision.SolidCollision(ahead, Player.width, Player.height)) {
				return;
			}
			for (int step = 1; step <= 6; step++) {
				Vector2 raised = ahead - new Vector2(0f, step * 16f);
				if (!Collision.SolidCollision(raised, Player.width, Player.height)) {
					Player.position.Y -= step * 16f;
					break;
				}
			}
		}

		// The car is fast; the titan walks a little slower but covers ground with huge strides.
		public override void PostUpdateRunSpeeds() {
			if (!IsTitan) {
				return;
			}
			if (carMode) {
				Player.maxRunSpeed = 16f;
				Player.accRunSpeed = 16f;
				Player.runAcceleration *= 3f;
				Player.runSlowdown *= 2f;
			}
			else {
				Player.maxRunSpeed *= 1.3f;
				Player.accRunSpeed *= 1.3f;
			}
		}

		public override void PostUpdate() {
			if (noRoomMessageCooldown > 0) {
				noRoomMessageCooldown--;
			}
			if (!IsTitan) {
				carMode = false;
				walkPhase = 0f;
				return;
			}

			// Stride and wheel animation follow actual movement.
			if (Player.velocity.Y == 0f) {
				walkPhase += System.Math.Abs(Player.velocity.X) * 0.018f;
			}
			wheelAngle += Player.velocity.X * 0.05f;

			if (Player.whoAmI == Main.myPlayer && carMode && IsFullSize) {
				UpdateCarRam();
				// Exhaust.
				if (System.Math.Abs(Player.velocity.X) > 2f && Main.rand.NextBool(2)) {
					Vector2 exhaust = new Vector2(Player.Center.X - Player.direction * Player.width * 0.48f, Player.Bottom.Y - 50f);
					Dust.NewDustPerfect(exhaust, DustID.Smoke, new Vector2(-Player.direction * 2f, -0.5f), 120, default, 1.8f);
				}
			}
		}

		// Driving fast rams whatever is in front of the car.
		private void UpdateCarRam() {
			int type = ModContent.ProjectileType<TitanCarRam>();
			if (!TitanCarRam.CanRam(Player) || Player.ownedProjectileCounts[type] > 0) {
				return;
			}
			int damage = (int)(Player.GetTotalDamage(DamageClass.Melee).ApplyTo(120f * TitanData.Tier(fusion) * Content.Abilities.RobotAbility.ProgressionMultiplier()));
			Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center, Vector2.Zero, type, damage, 14f, Player.whoAmI, ai0: (int)Element);
		}

		// ---------------------------------------------------------------- multiplayer sync

		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) {
			ModPacket packet = Mod.GetPacket();
			packet.Write((byte)RobotJackMod.MessageType.TitanSync);
			packet.Write((byte)Player.whoAmI);
			WriteState(packet);
			packet.Send(toWho, fromWho);
		}

		public void WriteState(BinaryWriter writer) {
			writer.Write((byte)fusion);
			writer.Write(look.ToBytes());
			writer.Write(carMode);
		}

		public void ReadState(BinaryReader reader) {
			fusion = (TitanFusion)reader.ReadByte();
			look = TitanLook.FromBytes(reader.ReadBytes(6));
			carMode = reader.ReadBoolean();
		}

		public override void CopyClientState(ModPlayer targetCopy) {
			TitanPlayer clone = (TitanPlayer)targetCopy;
			clone.fusion = fusion;
			clone.look = look;
			clone.carMode = carMode;
		}

		public override void SendClientChanges(ModPlayer clientPlayer) {
			TitanPlayer clone = (TitanPlayer)clientPlayer;
			if (clone.fusion != fusion || !clone.look.Equals(look) || clone.carMode != carMode) {
				SyncPlayer(-1, Main.myPlayer, false);
			}
		}
	}
}
