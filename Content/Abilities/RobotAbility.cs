using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Common;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Abilities
{
	// Base class for Robot Jack's ability items.
	// RobotJackPlayer puts these in the player's inventory while their form is active and removes them afterwards.
	// They can't be kept: they vanish when the form ends or changes, or when they're dropped in the world.
	public abstract class RobotAbility : ModItem
	{
		// Every ability, in load order.
		public static readonly List<RobotAbility> All = new();

		// Extra damage multiplier unlocked by beating bosses (x1 at the start of a world, up to x3 after the Moon Lord).
		public static float ProgressionMultiplier() {
			float mult = 1f;
			if (NPC.downedBoss3) mult += 0.3f;         // Skeletron
			if (Main.hardMode) mult += 0.4f;           // Wall of Flesh
			if (NPC.downedMechBossAny) mult += 0.3f;   // any mechanical boss
			if (NPC.downedPlantBoss) mult += 0.4f;     // Plantera
			if (NPC.downedMoonlord) mult += 0.6f;      // Moon Lord
			return mult;
		}

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 0; // abilities can't be researched or duplicated
			All.Add(this);
		}

		public override void Unload() {
			All.Clear();
		}

		public override void SetDefaults() {
			Item.maxStack = 1;
			Item.value = 0;
			Item.rare = ItemRarityID.Cyan;
			Item.DamageType = DamageClass.Ranged;
			Item.noMelee = true;
		}

		// Which form grants this ability.
		public virtual RobotFormType Form => RobotFormType.RobotJack;

		// Whether this ability is given in a form. Normally only its own; Omega Jack's are also God Jack's.
		public virtual bool AllowedIn(RobotFormType form) => form == Form;

		public virtual bool IsAllowed(Player player) {
			return AllowedIn(player.GetModPlayer<RobotJackPlayer>().ActiveForm);
		}

		// Cooldown in ticks after each use (0 = none). Shown in the tooltip and on the inventory slot.
		public virtual int CooldownTicks => 0;

		// Extra conditions for abilities that use the shared cooldown (checked before the cooldown starts).
		protected virtual bool CanUseAbility(Player player) => true;

		public override bool CanUseItem(Player player) {
			if (!IsAllowed(player)) {
				return false;
			}
			RobotJackPlayer modPlayer = player.GetModPlayer<RobotJackPlayer>();
			if (modPlayer.CooldownLeft(Type) > 0 || !CanUseAbility(player)) {
				return false;
			}
			if (CooldownTicks > 0) {
				modPlayer.StartCooldown(Type, CooldownTicks);
			}
			return true;
		}

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			if (CooldownTicks > 0) {
				tooltips.Add(new TooltipLine(Mod, "RobotCooldown", $"{CooldownTicks / 60f:0.#} second cooldown"));
			}
		}

		// Darken the slot and show the seconds left while on cooldown.
		public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
			int left = Main.LocalPlayer.GetModPlayer<RobotJackPlayer>().CooldownLeft(Type);
			if (left <= 0) {
				return;
			}
			Vector2 size = frame.Size() * scale;
			Rectangle box = new Rectangle((int)(position.X - size.X / 2f), (int)(position.Y - size.Y / 2f), (int)size.X, (int)size.Y);
			spriteBatch.Draw(TextureAssets.MagicPixel.Value, box, Color.Black * 0.55f);
			string text = ((left + 59) / 60).ToString();
			Utils.DrawBorderString(spriteBatch, text, position, Color.White, 0.8f, 0.5f, 0.5f);
		}

		public override void UpdateInventory(Player player) {
			if (!IsAllowed(player)) {
				Item.TurnToAir();
			}
		}

		// Dropped in the world: disappear.
		public override void PostUpdate() {
			Item.TurnToAir();
		}

		public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
			damage *= ProgressionMultiplier();
		}
	}
}
