using RobotJack.Common;
using RobotJack.Content.Buffs;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace RobotJack.Content.Items
{
	// Base class for transform items. Use one to take its form (ending any other form); use it again to turn back.
	public abstract class FormTrigger : ModItem
	{
		public abstract RobotFormType Form { get; }

		protected abstract int FormBuffType { get; }

		// Dust used for the transformation burst.
		protected virtual int TransformDust => DustID.Electric;

		public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(((FormBuff)BuffLoader.GetBuff(FormBuffType)).StatArgs);

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 32;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
		}

		public override bool? UseItem(Player player) {
			bool transforming = !player.HasBuff(FormBuffType);

			if (player.whoAmI == Main.myPlayer) {
				// Only one form at a time.
				foreach (int buff in FormBuff.BuffTypes.Values) {
					player.ClearBuff(buff);
				}
				if (transforming) {
					player.AddBuff(FormBuffType, 2);
				}
			}

			SoundEngine.PlaySound(transforming ? SoundID.Item113 : SoundID.Item94, player.Center);
			for (int i = 0; i < 30; i++) {
				Dust dust = Dust.NewDustDirect(player.position, player.width, player.height, TransformDust,
					Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(-3f, 3f));
				dust.noGravity = true;
			}

			return true;
		}
	}
}
