using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items.Akutoku
{
	// Dropped by Akutoku-ō: become Spider Jack, a robot spider whose front legs turn into weapons.
	public class SpiderTrigger : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.SpiderJack;
		protected override int FormBuffType => ModContent.BuffType<SpiderJackForm>();
		protected override int TransformDust => Elements.Dust(JackElement.Blight);

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 10);
			Item.rare = ItemRarityID.Pink;
		}

		protected override void SpawnTransformEffect(Player player, bool transforming) {
			// TransformBurst colour codes 8 and up are 8 + an element.
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<TransformBurst>(),
				0, 0f, player.whoAmI, ai1: transforming ? 0f : 1f, ai2: 8f + (int)JackElement.Blight);
		}
	}
}
