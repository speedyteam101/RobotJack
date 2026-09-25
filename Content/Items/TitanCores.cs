using RobotJack.Common;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items
{
	// Transform items for the three Titans. Hardmode: each needs the souls of one mechanical boss.

	public class TitanSpeakerCore : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.TitanSpeaker;
		protected override int FormBuffType => ModContent.BuffType<TitanSpeakerForm>();
		protected override int TransformDust => DustID.Torch;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Pink;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofMight, 10)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	public class TitanCameraCore : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.TitanCamera;
		protected override int FormBuffType => ModContent.BuffType<TitanCameraForm>();

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Pink;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofSight, 10)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	public class TitanTVCore : FormTrigger
	{
		public override RobotFormType Form => RobotFormType.TitanTV;
		protected override int FormBuffType => ModContent.BuffType<TitanTVForm>();
		protected override int TransformDust => DustID.PinkFairy;

		public override void SetDefaults() {
			base.SetDefaults();
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Pink;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient(ItemID.HallowedBar, 12)
				.AddIngredient(ItemID.SoulofFright, 10)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}
}
