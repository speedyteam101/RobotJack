using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Common.Titan;
using RobotJack.Content.Buffs;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace RobotJack.Content.Items
{
	// The Shift Trigger, found in the laboratory. Use it to become the Shift Titan, a giant robot with a glowing core.
	// Right click it in the inventory to open the titan menu: merge another trigger into it (which changes the titan's
	// element, look and nine abilities), customize its colours, core, head and weapon arm, and switch to car mode.
	// The merged trigger and the customization are stored on this item.
	public class ShiftTrigger : FormTrigger
	{
		public TitanFusion fusion;
		public TitanLook look = TitanLook.Default;

		public override RobotFormType Form => RobotFormType.ShiftTitan;
		protected override int FormBuffType => ModContent.BuffType<ShiftTitanForm>();
		protected override int TransformDust => Elements.Dust(TitanData.Element(fusion));

		public override void SetDefaults() {
			base.SetDefaults();
			Item.width = 30;
			Item.height = 32;
			Item.value = Item.sellPrice(gold: 25);
			Item.rare = ItemRarityID.Yellow;
		}

		// Right click in the inventory: open the titan menu instead of using the item up.
		public override bool CanRightClick() => true;

		public override bool ConsumeItem(Player player) => false;

		public override void RightClick(Player player) {
			if (player.whoAmI == Main.myPlayer && !Main.dedServ) {
				ModContent.GetInstance<TitanMenuSystem>().Open(Item);
			}
		}

		// Copies this trigger's fusion and look onto the player's titan.
		public void ApplyTo(Player player) {
			TitanPlayer titan = player.GetModPlayer<TitanPlayer>();
			titan.fusion = fusion;
			titan.look = look;
		}

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				ApplyTo(player);
				player.GetModPlayer<TitanPlayer>().carMode = false;
			}
			return base.UseItem(player);
		}

		protected override void SpawnTransformEffect(Player player, bool transforming) {
			// TransformBurst colour codes 8 and up are 8 + the titan's element.
			Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, ModContent.ProjectileType<TransformBurst>(),
				0, 0f, player.whoAmI, ai1: transforming ? 0f : 1f, ai2: 8f + (int)TitanData.Element(fusion));
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			string merged = fusion == TitanFusion.None ? "nothing" : Lang.GetItemNameValue(TitanData.TriggerOf(fusion));
			Color color = fusion == TitanFusion.Omega ? Main.DiscoColor : Elements.Main(TitanData.Element(fusion));
			tooltips.Add(new TooltipLine(Mod, "TitanFusion", $"Titan: {TitanData.Name(fusion)} (merged: {merged})") { OverrideColor = color });
		}

		public override void SaveData(TagCompound tag) {
			tag["fusion"] = (byte)fusion;
			tag["look"] = look.ToBytes();
		}

		public override void LoadData(TagCompound tag) {
			byte value = tag.GetByte("fusion");
			fusion = value <= (byte)TitanFusion.God ? (TitanFusion)value : TitanFusion.None;
			look = TitanLook.FromBytes(tag.GetByteArray("look"));
		}

		public override void NetSend(BinaryWriter writer) {
			writer.Write((byte)fusion);
			writer.Write(look.ToBytes());
		}

		public override void NetReceive(BinaryReader reader) {
			byte value = reader.ReadByte();
			fusion = value <= (byte)TitanFusion.God ? (TitanFusion)value : TitanFusion.None;
			look = TitanLook.FromBytes(reader.ReadBytes(6));
		}
	}
}
