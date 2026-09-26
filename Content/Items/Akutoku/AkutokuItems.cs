using Microsoft.Xna.Framework;
using RobotJack.Common;
using RobotJack.Common.Akutoku;
using RobotJack.Content.Buffs;
using RobotJack.Content.NPCs.Akutoku;
using RobotJack.Content.Projectiles;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace RobotJack.Content.Items.Akutoku
{
	// Summons Akutoku-ō. Only works in the Corruption or Crimson (his infection's home).
	public class InfectedCore : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.SortingPriorityBossSpawns[Type] = 12;
		}

		public override void SetDefaults() {
			Item.width = 24;
			Item.height = 24;
			Item.maxStack = 20;
			Item.value = Item.sellPrice(gold: 2);
			Item.rare = ItemRarityID.LightRed;
			Item.useAnimation = 30;
			Item.useTime = 30;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.consumable = true;
		}

		public override bool CanUseItem(Player player) =>
			(player.ZoneCorrupt || player.ZoneCrimson) && !NPC.AnyNPCs(ModContent.NPCType<Akutokuo>());

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				SoundEngine.PlaySound(SoundID.Roar, player.position);
				int type = ModContent.NPCType<Akutokuo>();
				if (Main.netMode != NetmodeID.MultiplayerClient) {
					NPC.SpawnOnPlayer(player.whoAmI, type);
				}
				else {
					NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: type);
				}
			}
			return true;
		}

		public override void AddRecipes() {
			foreach (int flesh in new[] { ItemID.RottenChunk, ItemID.Vertebrae }) {
				CreateRecipe()
					.AddIngredient(flesh, 15)
					.AddIngredient(ItemID.SoulofNight, 6)
					.AddRecipeGroup(RecipeGroupID.IronBar, 10)
					.AddTile(TileID.MythrilAnvil)
					.Register();
			}
		}
	}

	// Scrap from infected robots: used to build robot NPCs and gear.
	public class RobitaconParts : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 25;
		}

		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 22;
			Item.maxStack = Item.CommonMaxStack;
			Item.value = Item.sellPrice(silver: 20);
			Item.rare = ItemRarityID.LightRed;
		}
	}

	// Akutoku-ō's treasure bag (Expert and Master).
	public class AkutokuBag : ModItem
	{
		public static int[] LegWeaponItems() => new[] {
			ModContent.ItemType<SpiderRocketLauncher>(), ModContent.ItemType<SpiderLaserCannon>(), ModContent.ItemType<SpiderAxe>(),
			ModContent.ItemType<SpiderSword>(), ModContent.ItemType<SpiderFlamethrower>(), ModContent.ItemType<SpiderBuzzsaw>(),
			ModContent.ItemType<SpiderTeslaCoil>(),
		};

		public override void SetStaticDefaults() {
			ItemID.Sets.BossBag[Type] = true;
			Item.ResearchUnlockCount = 3;
		}

		public override void SetDefaults() {
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.width = 32;
			Item.height = 32;
			Item.rare = ItemRarityID.Purple;
			Item.expert = true;
		}

		public override bool CanRightClick() => true;

		public override void ModifyItemLoot(ItemLoot itemLoot) {
			itemLoot.Add(ItemDropRule.NotScalingWithLuck(ModContent.ItemType<AkutokuMask>(), 7));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpiderTrigger>()));
			itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RobitaconParts>(), 1, 25, 35));
			// Two leg weapons (they can occasionally be the same one).
			itemLoot.Add(ItemDropRule.OneFromOptions(1, LegWeaponItems()));
			itemLoot.Add(ItemDropRule.OneFromOptions(1, LegWeaponItems()));
			itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<Akutokuo>()));
		}
	}

	public class AkutokuTrophy : ModItem
	{
		public override void SetDefaults() {
			Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.AkutokuTrophyTile>());
			Item.width = 32;
			Item.height = 32;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(gold: 1);
		}
	}

	[AutoloadEquip(EquipType.Head)]
	public class AkutokuMask : ModItem
	{
		public override void SetDefaults() {
			Item.width = 22;
			Item.height = 22;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.sellPrice(silver: 75);
			Item.vanity = true;
			Item.maxStack = 1;
		}
	}

	// Build the Robo-Mechanic: once used, it moves into a free house (like other town NPCs).
	public class RobotAssemblyKit : ModItem
	{
		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.maxStack = 20;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.consumable = true;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.sellPrice(gold: 1);
			Item.UseSound = SoundID.Item37;
		}

		public override bool CanUseItem(Player player) => !AkutokuSystem.mechanicBuilt;

		public override bool? UseItem(Player player) {
			if (player.whoAmI == Main.myPlayer) {
				if (Main.netMode == NetmodeID.MultiplayerClient) {
					ModPacket packet = Mod.GetPacket();
					packet.Write((byte)RobotJackMod.MessageType.BuildMechanic);
					packet.Send();
				}
				else {
					AkutokuSystem.mechanicBuilt = true;
				}
				Main.NewText("The Robo-Mechanic has been built! It will move into a free house.", 120, 230, 255);
			}
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<RobitaconParts>(20)
				.AddRecipeGroup(RecipeGroupID.IronBar, 10)
				.AddIngredient(ItemID.Wire, 25)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	// Summons cleansed Robitacons that fight for you, each in a different robot form.
	public class RobitaconRemote : ModItem
	{
		public override void SetStaticDefaults() {
			ItemID.Sets.GamepadWholeScreenUseRange[Type] = true;
			ItemID.Sets.LockOnIgnoresCollision[Type] = true;
		}

		public override void SetDefaults() {
			Item.damage = 34;
			Item.knockBack = 3f;
			Item.mana = 10;
			Item.width = 28;
			Item.height = 28;
			Item.useTime = 36;
			Item.useAnimation = 36;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.value = Item.sellPrice(gold: 8);
			Item.rare = ItemRarityID.Pink;
			Item.UseSound = SoundID.Item113;
			Item.noMelee = true;
			Item.DamageType = DamageClass.Summon;
			Item.buffType = ModContent.BuffType<RobitaconMinionBuff>();
			Item.shoot = ModContent.ProjectileType<RobitaconMinion>();
		}

		public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback) {
			position = Main.MouseWorld;
			player.LimitPointToPlayerReachableArea(ref position);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			player.AddBuff(Item.buffType, 2);
			return true;
		}

		public override void AddRecipes() {
			CreateRecipe()
				.AddIngredient<RobitaconParts>(25)
				.AddIngredient(ItemID.SoulofNight, 8)
				.AddTile(TileID.MythrilAnvil)
				.Register();
		}
	}

	// ---------------------------------------------------------------------- the leg weapons

	// Rocket Launcher: homing micro-rockets that explode.
	public class SpiderRocketLauncher : ModItem
	{
		public override void SetDefaults() {
			Item.DamageType = DamageClass.Ranged;
			Item.damage = 48;
			Item.knockBack = 5f;
			Item.width = 44;
			Item.height = 22;
			Item.useTime = 28;
			Item.useAnimation = 28;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.shoot = ModContent.ProjectileType<HomingMissile>();
			Item.shootSpeed = 11f;
			Item.UseSound = SoundID.Item11;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-6f, 0f);
	}

	// Laser Cannon: hold to fire a continuous infected laser.
	public class SpiderLaserCannon : ModItem
	{
		public override void SetDefaults() {
			Item.DamageType = DamageClass.Magic;
			Item.damage = 26;
			Item.mana = 4;
			Item.width = 44;
			Item.height = 20;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.noUseGraphic = false;
			Item.channel = true;
			Item.shoot = ModContent.ProjectileType<ElementBeam>();
			Item.shootSpeed = 1f;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] == 0;

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			// ai[2] = 3: the item version of the element beam (see ElementBeam).
			Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(Vector2.UnitX * player.direction), type, damage, knockback, player.whoAmI,
				ai1: (int)JackElement.Blight, ai2: 3f);
			return false;
		}
	}

	// Axe: a huge infected axe that also chops trees.
	public class SpiderAxe : ModItem
	{
		public override void SetDefaults() {
			Item.DamageType = DamageClass.Melee;
			Item.damage = 72;
			Item.knockBack = 8f;
			Item.width = 48;
			Item.height = 48;
			Item.scale = 1.4f;
			Item.useTime = 22;
			Item.useAnimation = 26;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.useTurn = true;
			Item.axe = 30; // 150% axe power
			Item.UseSound = SoundID.Item1;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, JackElement.Blight);
		}
	}

	// Sword: a long blade that also fires piercing infected bolts.
	public class SpiderSword : ModItem
	{
		public override void SetDefaults() {
			Item.DamageType = DamageClass.Melee;
			Item.damage = 58;
			Item.knockBack = 5f;
			Item.width = 50;
			Item.height = 50;
			Item.scale = 1.3f;
			Item.useTime = 20;
			Item.useAnimation = 20;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.autoReuse = true;
			Item.UseSound = SoundID.Item1;
			Item.shoot = ModContent.ProjectileType<ElementBolt>();
			Item.shootSpeed = 12f;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			Projectile.NewProjectile(source, position, velocity, type, damage * 2 / 3, knockback, player.whoAmI,
				ai0: (int)JackElement.Blight, ai1: ElementBolt.Pierce | ElementBolt.Big);
			return false;
		}

		public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) {
			ElementFX.Hit(target, JackElement.Blight);
		}
	}

	// Flamethrower: a stream of fire (no gel needed).
	public class SpiderFlamethrower : ModItem
	{
		public override void SetDefaults() {
			Item.DamageType = DamageClass.Ranged;
			Item.damage = 30;
			Item.knockBack = 1f;
			Item.width = 44;
			Item.height = 20;
			Item.useTime = 6;
			Item.useAnimation = 30;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.shoot = ProjectileID.Flames;
			Item.shootSpeed = 7.5f;
			Item.UseSound = SoundID.Item34;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override Vector2? HoldoutOffset() => new Vector2(-4f, 0f);
	}

	// Buzzsaw: throw a spinning saw blade that comes back to you.
	public class SpiderBuzzsaw : ModItem
	{
		public override void SetDefaults() {
			Item.DamageType = DamageClass.Melee;
			Item.damage = 55;
			Item.knockBack = 4f;
			Item.width = 30;
			Item.height = 30;
			Item.useTime = 18;
			Item.useAnimation = 18;
			Item.useStyle = ItemUseStyleID.Swing;
			Item.noMelee = true;
			Item.noUseGraphic = true;
			Item.shoot = ModContent.ProjectileType<SpiderSawBlade>();
			Item.shootSpeed = 14f;
			Item.UseSound = SoundID.Item22;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] < 2;
	}

	// Tesla Coil: homing bolts of lightning.
	public class SpiderTeslaCoil : ModItem
	{
		public override void SetStaticDefaults() {
			Item.staff[Type] = true;
		}

		public override void SetDefaults() {
			Item.DamageType = DamageClass.Magic;
			Item.damage = 40;
			Item.mana = 8;
			Item.knockBack = 3f;
			Item.width = 40;
			Item.height = 40;
			Item.useTime = 16;
			Item.useAnimation = 16;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.noMelee = true;
			Item.autoReuse = true;
			Item.shoot = ModContent.ProjectileType<ElementBolt>();
			Item.shootSpeed = 13f;
			Item.UseSound = SoundID.Item93;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.sellPrice(gold: 6);
		}

		public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
			for (int i = -1; i <= 1; i++) {
				Projectile.NewProjectile(source, position, velocity.RotatedBy(i * 0.15f), type, damage, knockback, player.whoAmI,
					ai0: (int)JackElement.Volt, ai1: ElementBolt.Homing);
			}
			return false;
		}
	}
}
