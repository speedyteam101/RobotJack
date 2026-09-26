using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// God Jack's Gods Wrath. Hold left click to charge (5 seconds): the camera zooms out, you rise and glow,
	// the air around you becomes a mini sun, and a golden ring appears around at least half the world.
	// Release when fully charged to destroy everything inside the ring, except players and town NPCs.
	// Releasing early fizzles (no cooldown). 60 second cooldown after a full release.
	public class GodsWrathAbility : RobotAbility
	{
		public const int Cooldown = 60 * 60;

		public override RobotFormType Form => RobotFormType.GodJack;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 32;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.noUseGraphic = true;
			Item.channel = true;
			Item.rare = ItemRarityID.Purple;
			Item.UseSound = SoundID.Item29 with { Pitch = -1f };
			Item.shoot = ModContent.ProjectileType<GodsWrathCharge>();
			Item.shootSpeed = 1f;
		}

		protected override bool CanUseAbility(Player player) =>
			player.ownedProjectileCounts[Item.shoot] == 0 && player.ownedProjectileCounts[ModContent.ProjectileType<GodsWrathBlast>()] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, player.Center, Vector2.Zero, type, 0, 0f, player.whoAmI);
			return false;
		}

		public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips) {
			base.ModifyTooltips(tooltips);
			tooltips.Add(new TooltipLine(Mod, "GodsWrathCooldown", $"{Cooldown / 60} second cooldown after a full release"));
		}
	}
}
