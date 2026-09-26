using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Common.Akutoku;
using RobotJack.Common.Titan;
using RobotJack.Content.Abilities;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace RobotJack.Content.Players
{
	// Spider Jack's rig: a small copy of Akutoku-ō's body, walking on its own legs around the player's hitbox.
	// Its front legs turn into the weapon that matches the ability being held.
	public class SpiderPlayer : ModPlayer
	{
		public const float Scale = 0.55f;
		private SpiderRig rig;

		public SpiderRig Rig => rig ??= new SpiderRig { Scale = Scale, Infected = false };

		public bool IsSpider => Player.GetModPlayer<RobotJackPlayer>().ActiveForm == RobotFormType.SpiderJack;

		public override void PostUpdate() {
			if (Main.dedServ || !IsSpider) {
				return;
			}
			SpiderRig r = Rig;
			// Aim along the item being used, otherwise straight ahead.
			Vector2 aim = new Vector2(Player.direction, 0.15f);
			if (Player.itemAnimation > 0) {
				float rot = Player.itemRotation;
				aim = new Vector2((float)System.Math.Cos(rot) * Player.direction, (float)System.Math.Sin(rot) * Player.direction);
			}
			for (int slot = 0; slot < 2; slot++) {
				r.Aim[slot] = Vector2.Lerp(r.Aim[slot], aim.SafeNormalize(Vector2.UnitX), 0.3f);
			}
			// The front leg becomes the held ability's weapon (the other leg keeps a sword, or an axe).
			LegWeapon wanted = WeaponFor(Player.HeldItem.ModItem);
			if (r.Weapons[0] != wanted) {
				r.StartSwap(0, wanted);
			}
			LegWeapon second = wanted == LegWeapon.Sword ? LegWeapon.Axe : LegWeapon.Sword;
			if (r.Weapons[1] != second) {
				r.StartSwap(1, second);
			}
			r.GlowColor = Elements.Main(JackElement.Blight);
			r.Update(Player.Center + new Vector2(0f, 2f), Player.direction, Scale);
		}

		private static LegWeapon WeaponFor(ModItem item) => item switch {
			BoltAbility => LegWeapon.RocketLauncher,
			BeamAbility => LegWeapon.LaserCannon,
			EruptionAbility => LegWeapon.Axe,
			RainAbility => LegWeapon.TeslaCoil,
			BlastAbility => LegWeapon.Flamethrower,
			OrbitAbility => LegWeapon.Buzzsaw,
			_ => LegWeapon.Sword,
		};
	}

	public class SpiderDrawLayer : PlayerDrawLayer
	{
		private static readonly List<TitanSprite> sprites = new();

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			return !player.dead && player.GetModPlayer<RobotJackPlayer>().ActiveForm == RobotFormType.SpiderJack;
		}

		public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Torso, PlayerDrawLayers.OffhandAcc);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			SpiderRig rig = player.GetModPlayer<SpiderPlayer>().Rig;
			Point tile = (player.Center / 16f).ToPoint();
			Color light = Lighting.GetColor(tile.X, tile.Y);
			sprites.Clear();
			// Afterimages are drawn at the player's old position: shift the whole spider with them.
			Vector2 shift = drawInfo.Position - player.position;
			rig.Build(sprites, Main.screenPosition - shift, light, 1f - drawInfo.shadow);
			foreach (TitanSprite sprite in sprites) {
				drawInfo.DrawDataCache.Add(sprite.ToDrawData());
			}
		}
	}
}
