using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items
{
	// Found, not crafted: underground Gold Chests (see RobotJackWorldGen) and a rare drop from
	// enemies killed underground (see RobotTriggerDrop). Use it to become Robot Jack, use it again to turn back.
	public class RobotTrigger : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.RobotJack;
		protected override int FormBuffType => ModContent.BuffType<RobotForm>();

		// A pillar of light and shockwave rings (smaller when turning back).
		protected override void SpawnTransformEffect(Player player, bool transforming) {
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero,
				ModContent.ProjectileType<TransformBurst>(), 0, 0f, player.whoAmI, ai1: transforming ? 0f : 1f);
		}

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;
		}
	}
}
