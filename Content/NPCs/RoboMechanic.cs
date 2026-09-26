using RobotJack.Common.Akutoku;
using RobotJack.Content.Items;
using RobotJack.Content.Items.Akutoku;
using RobotJack.Content.Projectiles;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Bestiary;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace RobotJack.Content.NPCs
{
	// The Robo-Mechanic: a town robot built from Robitacon parts (with a Robot Assembly Kit). Once built it moves
	// into a free house like any town NPC, sells robot gear, and defends town with fire bolts.
	[AutoloadHead]
	public class RoboMechanic : ModNPC
	{
		public const string ShopName = "Shop";
		private static readonly Condition DownedAkutoku = new("Mods.RobotJack.Conditions.DownedAkutoku", () => AkutokuSystem.downedAkutoku);

		public override void SetStaticDefaults() {
			Main.npcFrameCount[Type] = 25;
			NPCID.Sets.ExtraFramesCount[Type] = 9;
			NPCID.Sets.AttackFrameCount[Type] = 4;
			NPCID.Sets.DangerDetectRange[Type] = 700;
			NPCID.Sets.AttackType[Type] = 1; // shooting
			NPCID.Sets.AttackTime[Type] = 60;
			NPCID.Sets.AttackAverageChance[Type] = 20;
			NPCID.Sets.HatOffsetY[Type] = 4;
		}

		public override void SetDefaults() {
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = NPCAIStyleID.Passive;
			NPC.damage = 10;
			NPC.defense = 25;
			NPC.lifeMax = 400;
			NPC.HitSound = SoundID.NPCHit4;
			NPC.DeathSound = SoundID.NPCDeath14;
			NPC.knockBackResist = 0.4f;
			AnimationType = NPCID.Guide;
		}

		public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry) {
			bestiaryEntry.Info.AddRange(new List<IBestiaryInfoElement> {
				new FlavorTextBestiaryInfoElement("Mods.RobotJack.Bestiary.RoboMechanic"),
			});
		}

		public override bool CanTownNPCSpawn(int numTownNPCs) => AkutokuSystem.mechanicBuilt;

		public override List<string> SetNPCNameList() => new() { "Bolt", "Sprocket", "Gizmo", "Rivet", "Cog-9", "Unit 42", "Widget", "Servo" };

		public override string GetChat() {
			WeightedRandom<string> chat = new();
			for (int i = 1; i <= 5; i++) {
				chat.Add(Language.GetTextValue("Mods.RobotJack.Dialogue.RoboMechanic.Line" + i));
			}
			if (!AkutokuSystem.downedAkutoku) {
				chat.Add(Language.GetTextValue("Mods.RobotJack.Dialogue.RoboMechanic.Warning"), 2.0);
			}
			return chat;
		}

		public override void SetChatButtons(ref string button, ref string button2) {
			button = Language.GetTextValue("LegacyInterface.28"); // "Shop"
		}

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton) {
				shop = ShopName;
			}
		}

		public override void AddShops() {
			new NPCShop(Type, ShopName)
				.Add<RobotJetpack>()
				.Add<RobitaconParts>(DownedAkutoku)
				.Add<InfectedCore>(DownedAkutoku)
				.Add(new Item(ModContent.ItemType<RobotTrigger>()) { shopCustomPrice = Item.buyPrice(gold: 50) })
				.Add(ItemID.Wire)
				.Add(ItemID.Cog)
				.Register();
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			damage = 30;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 25;
			randExtraCooldown = 20;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay) {
			projType = ModContent.ProjectileType<ElementBolt>(); // ai[0] = 0: fire (Blaze) bolts
			attackDelay = 1;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset) {
			multiplier = 12f;
			randomOffset = 1f;
		}

		public override void HitEffect(NPC.HitInfo hit) {
			for (int i = 0; i < (NPC.life <= 0 ? 20 : 3); i++) {
				Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Silver, hit.HitDirection * 2f, -1f);
			}
		}
	}
}
