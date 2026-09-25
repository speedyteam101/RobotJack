using RobotJack.Common;
using RobotJack.Content.Buffs;
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

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;
		}
	}
}
