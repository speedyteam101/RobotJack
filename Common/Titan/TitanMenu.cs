using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RobotJack.Content.Items;
using RobotJack.Content.Projectiles;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace RobotJack.Common.Titan
{
	// Shows the Shift Trigger's menu while it's open (client only).
	[Autoload(Side = ModSide.Client)]
	public class TitanMenuSystem : ModSystem
	{
		private UserInterface ui;
		private TitanMenu menu;

		public bool IsOpen => ui?.CurrentState != null;

		public override void PostSetupContent() {
			ui = new UserInterface();
			menu = new TitanMenu();
			menu.Activate();
		}

		public void Open(Item item) {
			menu.Target = item;
			ui.SetState(menu);
			SoundEngine.PlaySound(SoundID.MenuOpen);
		}

		public void Close() {
			if (ui?.CurrentState == null) {
				return;
			}
			ui.SetState(null);
			menu.Target = null;
			SoundEngine.PlaySound(SoundID.MenuClose);
		}

		public override void OnWorldUnload() {
			ui?.SetState(null);
			if (menu != null) {
				menu.Target = null;
			}
		}

		public override void UpdateUI(GameTime gameTime) {
			if (ui?.CurrentState == null) {
				return;
			}
			if (!menu.StillValid()) {
				Close();
				return;
			}
			ui.Update(gameTime);
		}

		public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
			int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
			if (mouseTextIndex != -1) {
				layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
					"RobotJack: Titan Menu",
					delegate {
						if (ui?.CurrentState != null) {
							ui.Draw(Main.spriteBatch, new GameTime());
						}
						return true;
					},
					InterfaceScaleType.UI));
			}
		}
	}

	// The Shift Trigger's menu: a live preview of the titan, the merge slot, colour swatches for the armor, accent
	// and core, the core shape, head and weapon arm, and the car mode button. Changes apply straight away.
	public class TitanMenu : UIState
	{
		public Item Target;
		private ShiftTrigger Trigger => Target?.ModItem as ShiftTrigger;

		private UIPanel panel;
		private const float PanelWidth = 560f;
		private const float PanelHeight = 430f;
		private const float Column = 232f; // left edge of the options column

		// A short message under the merge slot (e.g. when something that can't be merged is clicked in).
		private string notice;
		private int noticeTicks;

		public override void OnInitialize() {
			panel = new UIPanel();
			panel.SetPadding(0f);
			panel.Left.Set(-PanelWidth / 2f, 0.5f);
			panel.Top.Set(300f, 0f);
			panel.Width.Set(PanelWidth, 0f);
			panel.Height.Set(PanelHeight, 0f);
			panel.BackgroundColor = new Color(24, 34, 58) * 0.95f;
			panel.BorderColor = new Color(90, 230, 255);
			Append(panel);

			Add(new MenuLabel(() => "Shift Trigger: " + (Trigger != null ? TitanData.Name(Trigger.fusion) : ""), 1f, true), 0f, 8f, PanelWidth, 24f);
			Add(new TitanPreview(this), 16f, 40f, 200f, 300f);
			Add(new MenuButton(CarText, () => Main.LocalPlayer.GetModPlayer<TitanPlayer>().carMode, ToggleCar), 16f, 350f, 200f, 32f);
			Add(new MenuButton(() => "Close", () => false, () => ModContent.GetInstance<TitanMenuSystem>().Close()), 16f, 390f, 200f, 28f);

			// Merge slot.
			Add(new MenuLabel(() => "Merged trigger", 0.8f), Column, 40f, 300f, 18f);
			Add(new MergeSlot(this), Column, 60f, 52f, 52f);
			Add(new MenuLabel(MergeText, 0.75f), Column + 60f, 62f, 250f, 50f);

			float y = 122f;
			y = AddSwatches("Armor color", y, look => look.MainColor, (ref TitanLook look, byte v) => look.MainColor = v);
			y = AddSwatches("Accent color", y, look => look.AccentColor, (ref TitanLook look, byte v) => look.AccentColor = v);
			y = AddSwatches("Core color", y, look => look.CoreColor, (ref TitanLook look, byte v) => look.CoreColor = v);
			y = AddChoices("Core shape", y, TitanData.CoreShapes, look => look.CoreShape, (ref TitanLook look, byte v) => look.CoreShape = v);
			y = AddChoices("Head", y, TitanData.HeadStyles, look => look.HeadStyle, (ref TitanLook look, byte v) => look.HeadStyle = v);
			AddChoices("Weapon arm", y, TitanData.WeaponArms, look => look.WeaponArm, (ref TitanLook look, byte v) => look.WeaponArm = v);
		}

		private delegate void LookSetter(ref TitanLook look, byte value);

		private void Add(UIElement element, float left, float top, float width, float height) {
			element.Left.Set(left, 0f);
			element.Top.Set(top, 0f);
			element.Width.Set(width, 0f);
			element.Height.Set(height, 0f);
			panel.Append(element);
		}

		private float AddSwatches(string label, float y, Func<TitanLook, byte> get, LookSetter set) {
			Add(new MenuLabel(() => label, 0.8f), Column, y, 300f, 18f);
			for (int i = 0; i < TitanData.Colors.Length; i++) {
				byte index = (byte)i;
				MenuButton swatch = new MenuButton(() => "", () => Trigger != null && get(Trigger.look) == index, () => ChangeLook(set, index)) {
					Swatch = TitanData.Colors[i],
				};
				Add(swatch, Column + i * 26f, y + 18f, 22f, 22f);
			}
			return y + 48f;
		}

		private float AddChoices(string label, float y, string[] names, Func<TitanLook, byte> get, LookSetter set) {
			Add(new MenuLabel(() => label, 0.8f), Column, y, 300f, 18f);
			float width = (312f - (names.Length - 1) * 4f) / names.Length;
			for (int i = 0; i < names.Length; i++) {
				byte index = (byte)i;
				string name = names[i];
				Add(new MenuButton(() => name, () => Trigger != null && get(Trigger.look) == index, () => ChangeLook(set, index)),
					Column + i * (width + 4f), y + 18f, width, 24f);
			}
			return y + 50f;
		}

		private void ChangeLook(LookSetter set, byte value) {
			ShiftTrigger trigger = Trigger;
			if (trigger == null) {
				return;
			}
			set(ref trigger.look, value);
			ApplyToTitan();
		}

		// If the player is the titan right now, the change shows on them straight away.
		private void ApplyToTitan() {
			Player player = Main.LocalPlayer;
			if (player.GetModPlayer<TitanPlayer>().IsTitan) {
				Trigger.ApplyTo(player);
			}
		}

		private string CarText() {
			TitanPlayer titan = Main.LocalPlayer.GetModPlayer<TitanPlayer>();
			if (!titan.IsTitan) {
				return "Car Mode (transform first)";
			}
			return titan.carMode ? "Car Mode: ON" : "Car Mode: OFF";
		}

		private void ToggleCar() {
			Player player = Main.LocalPlayer;
			TitanPlayer titan = player.GetModPlayer<TitanPlayer>();
			if (!titan.IsTitan) {
				Notify("Use the Shift Trigger to become the titan first");
				return;
			}
			titan.carMode = !titan.carMode;
			SoundEngine.PlaySound(SoundID.Item113 with { Pitch = titan.carMode ? -0.3f : 0.2f }, player.Center);
			ElementFX.Burst(player.Center, titan.Element, 40, 10f, 1.6f);
		}

		private string MergeText() {
			if (noticeTicks > 0) {
				return notice;
			}
			ShiftTrigger trigger = Trigger;
			if (trigger == null || trigger.fusion == TitanFusion.None) {
				return "Empty. Click here holding any Jack\ntrigger to merge it in.";
			}
			return Lang.GetItemNameValue(TitanData.TriggerOf(trigger.fusion)) + "\nClick empty-handed to take it out.";
		}

		private void Notify(string text) {
			notice = text;
			noticeTicks = 150;
		}

		// Clicked the merge slot: swap the trigger on the mouse cursor with the merged one.
		internal void ClickMergeSlot() {
			ShiftTrigger trigger = Trigger;
			if (trigger == null) {
				return;
			}
			Item mouse = Main.mouseItem;
			TitanFusion? incoming = mouse.IsAir ? null : TitanData.FusionOf(mouse.type);
			if (!mouse.IsAir && incoming == null) {
				Notify(mouse.ModItem is ShiftTrigger ? "A Shift Trigger can't merge with itself!" : "Only Jack triggers can be merged.");
				SoundEngine.PlaySound(SoundID.MenuClose);
				return;
			}
			if (mouse.IsAir && trigger.fusion == TitanFusion.None) {
				return;
			}

			// Give back the trigger that was merged in, if any, and take the new one.
			Item returned = new Item();
			if (trigger.fusion != TitanFusion.None) {
				returned.SetDefaults(TitanData.TriggerOf(trigger.fusion));
			}
			trigger.fusion = incoming ?? TitanFusion.None;
			Main.mouseItem = returned;
			noticeTicks = 0;
			SoundEngine.PlaySound(SoundID.Item37);
			ElementFX.Burst(Main.LocalPlayer.Center, TitanData.Element(trigger.fusion), 25, 6f, 1.3f);
			ApplyToTitan();
		}

		// The menu closes if the Shift Trigger leaves the inventory, the inventory is closed, or the player dies.
		public bool StillValid() {
			Player player = Main.LocalPlayer;
			if (Target == null || Trigger == null || !Main.playerInventory || player.dead) {
				return false;
			}
			if (ReferenceEquals(Main.mouseItem, Target)) {
				return true;
			}
			foreach (Item item in player.inventory) {
				if (ReferenceEquals(item, Target)) {
					return true;
				}
			}
			return false;
		}

		public override void Update(GameTime gameTime) {
			base.Update(gameTime);
			if (noticeTicks > 0) {
				noticeTicks--;
			}
			// Clicks on the menu don't use the held item.
			if (panel.ContainsPoint(Main.MouseScreen)) {
				Main.LocalPlayer.mouseInterface = true;
			}
		}

		// ------------------------------------------------------------------ elements

		private static void Box(SpriteBatch spriteBatch, Rectangle box, Color fill, Color border) {
			Texture2D pixel = TextureAssets.MagicPixel.Value;
			spriteBatch.Draw(pixel, box, fill);
			spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, box.Width, 2), border);
			spriteBatch.Draw(pixel, new Rectangle(box.X, box.Bottom - 2, box.Width, 2), border);
			spriteBatch.Draw(pixel, new Rectangle(box.X, box.Y, 2, box.Height), border);
			spriteBatch.Draw(pixel, new Rectangle(box.Right - 2, box.Y, 2, box.Height), border);
		}

		private class MenuLabel : UIElement
		{
			private readonly Func<string> text;
			private readonly float scale;
			private readonly bool centered;

			public MenuLabel(Func<string> text, float scale, bool centered = false) {
				this.text = text;
				this.scale = scale;
				this.centered = centered;
				IgnoresMouseInteraction = true;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				CalculatedStyle d = GetDimensions();
				Vector2 at = centered ? new Vector2(d.X + d.Width / 2f, d.Y) : new Vector2(d.X, d.Y);
				foreach (string line in text().Split('\n')) {
					Utils.DrawBorderString(spriteBatch, line, at, Color.White, scale, centered ? 0.5f : 0f, 0f);
					at.Y += 26f * scale;
				}
			}
		}

		private class MenuButton : UIElement
		{
			private readonly Func<string> text;
			private readonly Func<bool> selected;
			public Color? Swatch;

			public MenuButton(Func<string> text, Func<bool> selected, Action onClick) {
				this.text = text;
				this.selected = selected;
				OnLeftClick += (evt, element) => {
					SoundEngine.PlaySound(SoundID.MenuTick);
					onClick();
				};
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				Rectangle box = GetDimensions().ToRectangle();
				bool on = selected();
				Color border = on ? Color.White : IsMouseHovering ? new Color(150, 220, 255) : new Color(20, 24, 36);
				Color fill = Swatch ?? (on ? new Color(40, 110, 160) : IsMouseHovering ? new Color(50, 70, 110) : new Color(36, 48, 80));
				Box(spriteBatch, box, fill, border);
				string label = text();
				if (label.Length > 0) {
					Utils.DrawBorderString(spriteBatch, label, box.Center.ToVector2() + new Vector2(0f, 3f), Color.White, 0.8f, 0.5f, 0.5f);
				}
			}
		}

		private class MergeSlot : UIElement
		{
			private readonly TitanMenu menu;

			public MergeSlot(TitanMenu menu) {
				this.menu = menu;
				OnLeftClick += (evt, element) => menu.ClickMergeSlot();
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				Rectangle box = GetDimensions().ToRectangle();
				ShiftTrigger trigger = menu.Trigger;
				TitanFusion fusion = trigger?.fusion ?? TitanFusion.None;
				Color glow = fusion == TitanFusion.Omega ? Main.DiscoColor : RobotJack.Common.Elements.Main(TitanData.Element(fusion));
				Box(spriteBatch, box, new Color(20, 26, 44), IsMouseHovering ? Color.White : glow);
				if (fusion != TitanFusion.None) {
					int type = TitanData.TriggerOf(fusion);
					Main.instance.LoadItem(type);
					Texture2D tex = TextureAssets.Item[type].Value;
					float scale = System.Math.Min(1f, 36f / System.Math.Max(tex.Width, tex.Height));
					spriteBatch.Draw(tex, box.Center.ToVector2(), null, Color.White, 0f, tex.Size() / 2f, scale, SpriteEffects.None, 0f);
				}
				if (IsMouseHovering) {
					Main.hoverItemName = fusion == TitanFusion.None ? "Merge a trigger" : Lang.GetItemNameValue(TitanData.TriggerOf(fusion));
				}
			}
		}

		// The titan (or its car) as it looks right now, walking in place.
		private class TitanPreview : UIElement
		{
			private readonly TitanMenu menu;
			private static readonly List<TitanSprite> sprites = new();

			public TitanPreview(TitanMenu menu) {
				this.menu = menu;
				IgnoresMouseInteraction = true;
			}

			protected override void DrawSelf(SpriteBatch spriteBatch) {
				Rectangle box = GetDimensions().ToRectangle();
				Box(spriteBatch, box, new Color(12, 16, 28), new Color(60, 80, 120));
				ShiftTrigger trigger = menu.Trigger;
				if (trigger == null) {
					return;
				}
				float time = Main.GameUpdateCount;
				Vector2 feet = new Vector2(box.Center.X, box.Bottom - 16);
				sprites.Clear();
				if (Main.LocalPlayer.GetModPlayer<TitanPlayer>().carMode) {
					TitanRenderer.BuildCar(sprites, trigger.look, trigger.fusion, feet, 1, 1.8f, Color.White, time * 0.08f, time);
				}
				else {
					TitanPose pose = new TitanPose { WalkPhase = time * 0.06f, WalkAmount = 0.5f, Time = time };
					TitanRenderer.BuildTitan(sprites, pose, trigger.look, trigger.fusion, feet, 1, 2.6f, Color.White);
				}
				foreach (TitanSprite sprite in sprites) {
					sprite.Draw(spriteBatch);
				}
			}
		}
	}
}
