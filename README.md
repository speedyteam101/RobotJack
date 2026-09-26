# Robot Jack — a tModLoader mod for Terraria

A small standalone mod for **tModLoader 1.4.4**: find the **Robot Trigger**, transform into a sci-fi combat robot (or one of five elemental variants, Omega Jack, all of them combined, or God Jack), and use ability items, headlined by the **Orbital Cannon Strike**.

## Installing (from source)

1. Clone or download this repository into your tModLoader `ModSources` folder as `RobotJack`, so you have `ModSources/RobotJack/build.txt`.
   (On Windows that is usually `Documents/My Games/Terraria/tModLoader/ModSources`.)
2. In tModLoader: **Workshop → Develop Mods → Robot Jack → Build + Reload**.

## Getting the Robot Trigger

It can't be crafted; you have to find it:

- **Underground Gold Chests:** roughly 1 in 4 of them in a **newly generated** world has one, and every new world gets at least one.
- **Enemy drop:** any enemy killed below the surface has a **1 in 150** chance to drop one. Bosses, critters and statue-spawned enemies don't count. This is how you get one in a world that existed before you installed the mod.

## The transformation

Use the Robot Trigger to become Robot Jack; use it again to turn back. The form has no time limit or cooldown and also ends if you right-click the buff or die.

- +25% damage, +20 defense, 10% damage reduction, +30% movement speed
- Higher jumps, no knockback, no fall damage
- **Hover jets:** hold jump while falling to glide down
- Your character is drawn as a white-armored hero robot with a glowing cyan V-visor and chest core, a back jetpack and a red scarf that hangs down when you stand still and streams out behind you when you run or jump. It animates with your walking, jumping and aiming, and the glowing parts stay lit in the dark.
- **Transformation effect:** energy rushes in, a pillar of light slams down from the sky onto you, and shockwave rings burst out with a small screen shake (a smaller version plays when you turn back).
- **Afterimages:** when you move fast (dashing, falling, flying) you leave glowing cyan afterimages behind you.
- **Jetpack flames** burn from your back whenever you're in the air.

## Abilities

While transformed, the ability items are put into your inventory automatically (first empty slots, hotbar first). They vanish when you turn back or if you drop them. If your inventory is full you won't get them until you free up slots.

All ability damage except the Orbital Cannon Strike grows as you beat bosses: ×1.3 after Skeletron, then +0.4 after the Wall of Flesh, +0.3 after any mechanical boss, +0.4 after Plantera and +0.6 after the Moon Lord, up to **×3**. The form's +25% damage applies on top of that. The Orbital Cannon Strike always deals exactly 10,000,000 per hit: no bonuses, crits, random spread or enemy defense change it.

| Ability | Base damage | What it does |
| --- | --- | --- |
| **Orbital Cannon Strike** | 10,000,000 per hit (flat) | Click anywhere on screen. A satellite slides into view high above and a spinning red reticle locks onto the ground below your cursor (about 1.2 s, with a flickering guide laser and speeding-up beeps). Then a huge cyan-white laser slams down from the satellite: screen shake, a shockwave, sparks, smoke and light. The beam lasts about 1.3 s and hits everything in the column and near the impact point every 8 ticks, inflicting Electrified and Hellfire. It passes through blocks without breaking them. **8 second cooldown.** |
| **Plasma Cannon** | 28 | Rapid-fire plasma bolts from your arm cannon (auto-fire, pierces 1 enemy). |
| **Missile Barrage** | 45 each | Fires a fan of 6 micro-missiles that home in on the nearest enemies and explode. |
| **Thruster Dash** | 60 (melee) | Rocket toward the cursor with brief invincibility, burning everything you pass through. |
| **Energy Absorber** | 18 per drain hit | **Hold left click** to open an absorb field around you. While it's up, **every hit you take is neutralised** and its damage is stored as energy. Enemy projectiles (and PvP opponents' projectiles) within ~26 tiles are pulled in and swallowed, also adding energy. Enemies in range are pulled toward you (by their knockback resistance, so most bosses won't budge), and PvP opponents are pulled too. Anything within ~7 tiles is **drained**: it takes damage, which adds to your energy, and players also lose mana. **Right click** to release: an area blast (~35 tiles across) that deals **10× your stored energy** to everything it touches. It also fires back every different attack you absorbed (up to 12 kinds), as glowing copies dealing **10× the damage they had**. Energy caps at 3,000 (a 30,000 damage blast) and is lost when you die. A bar above your head shows how much you have. |
| **Gravity Arm** | none | **Hold left click** to open a gravity well that follows your cursor. Every enemy within ~25 tiles of it is pulled toward the cursor (faster when further away) and held in the middle, with tethers and a beam from your arm. It deals no damage: drag enemies into a group, then hit them with something else. Bosses, worm body segments and invincible enemies aren't pulled. |

## Robot Jack variants

Five elemental versions of Robot Jack. They're the same size as Robot Jack, with their own colors, a signature detail (flame crest, ice spikes, lightning antenna, horns, halo) and **eight abilities each**. Each variant's trigger is **crafted from a Robot Trigger, which you keep**, so you can still be normal Robot Jack. Transforming shows the pillar-of-light effect in the variant's color, and fast movement leaves afterimages in that color.

| Variant | Transform item | Stats | Perk |
| --- | --- | --- | --- |
| **Blaze Jack** | Blaze Trigger: Robot Trigger + 12 Hellstone Bar @ Anvil (pre-Hardmode) | +30% damage, +18 defense, 10% DR, +30% speed | Immune to burning and lava; walk safely on hot blocks |
| **Frost Jack** | Frost Trigger: Robot Trigger + 50 Ice Block + 5 Fallen Star @ Anvil (pre-Hardmode) | +25% damage, +30 defense, 15% DR, +25% speed | Immune to cold; no slipping on ice |
| **Volt Jack** | Volt Trigger: Robot Trigger + 12 Soul of Light + 50 Wire @ Mythril/Orichalcum Anvil (Hardmode) | +30% damage, +18 defense, 10% DR, +60% speed, +10% attack speed | Much faster, +10% attack speed, immune to Electrified |
| **Shadow Jack** | Shadow Trigger: Robot Trigger + 15 Soul of Night @ Mythril/Orichalcum Anvil (Hardmode) | +40% damage, +10% crit, +20 defense, 10% DR, +35% speed | Chance to dodge attacks, immune to Shadowflame |
| **Nova Jack** | Nova Trigger: Robot Trigger + 10 Luminite Bar + 20 Fallen Star @ Ancient Manipulator (post-Moon Lord) | +50% damage, +10% crit, +40 defense, 20% DR, +40% speed, +100 max life, +5 HP/s | +5 health per second |

Their abilities are built from these kinds, each in the variant's element (fire sets enemies ablaze, frost gives Frostburn and sometimes freezes, volt electrifies, shadow gives Shadowflame, nova applies Ichor). Every variant has bolts, a beam, an eruption, sky rain, a blast and a power-up; the other two slots differ (see the list below):

| Kind | What it does |
| --- | --- |
| Bolts | Fire bolts from your arm cannon (auto-fire). Each variant's differ: exploding fireballs, 3 piercing ice shards, very fast piercing arcs, 4 homing shadow orbs, 5 homing stars. |
| Beam | Hold left click for a continuous element beam from your arm cannon that follows the cursor. |
| Eruption | Pillars burst out of the ground in a row across the cursor: fire geysers, freezing ice spires, lightning strikes from the sky, shadow tendrils, starlight pillars. |
| Sky rain | Bolts rain down onto the cursor: meteors, hail, storm bolts, dark orbs, falling stars. |
| Blast | An element explosion all around you (Frost Nova also freezes enemies; Nova's Big Bang is huge, with a 10 s cooldown). |
| Dash | Blaze, Volt, Shadow, Nova: dash toward the cursor, hitting everything on the way (Volt is faster, Shadow Step keeps you invincible longer). |
| Aura | Frost (Blizzard), Volt (Tesla Field): hurts everything around you for 8 s. 15 s cooldown. |
| Orbit | Blaze, Frost, Shadow, Nova: orbs or blades circle you for 10 s, hitting what they touch. 12 s cooldown. |
| Power-up | A 10 s buff (30 s cooldown): Overheat (+30% damage, hits set enemies ablaze), Cryo Armor (+25 defense, 20% DR), Overcharge (+40% speed, +25% attack speed), Vampiric Shroud (hits steal life), Celestial Blessing (+10 HP/s, +15% damage). |

| Variant | Its eight abilities |
| --- | --- |
| **Blaze Jack** | Flame Blaster, Inferno Beam, Eruption, Meteor Shower, Heat Nova, Blaze Dash, Flame Ring, Overheat |
| **Frost Jack** | Ice Shards, Freeze Ray, Glacier Spikes, Hailstorm, Blizzard, Frost Nova, Ice Orbit, Cryo Armor |
| **Volt Jack** | Arc Pistol, Lightning Beam, Thunder Strike, Storm Call, Tesla Field, Static Discharge, Volt Dash, Overcharge |
| **Shadow Jack** | Shadow Orbs, Void Beam, Shadow Spikes, Dark Rain, Void Pulse, Shadow Step, Phantom Blades, Vampiric Shroud |
| **Nova Jack** | Star Shot, Supernova Beam, Comet Pillars, Starfall, Big Bang, Comet Dash, Constellation, Celestial Blessing |

Base damage is higher for the later variants (Volt and Shadow ×2, Nova ×3.5 of Blaze/Frost's), and everything still scales with bosses beaten.

## Omega Jack

**Every Robot Jack combined.** The **Omega Trigger** is crafted from the Robot Trigger and all five variant triggers (none of them are used up, so you keep every form) plus **25 Luminite Bar and 10 Solar Fragment** at the Ancient Manipulator (post-Moon Lord).

- **Look:** black and gold armor with a white visor and scarf, Shadow Jack's horns, Nova Jack's halo, Frost Jack's shoulder crystal and a chest core ringed in all five element colors. Its afterimages, jetpack sparks and bigger transformation burst cycle through the rainbow.
- **Stats:** +80% damage, +20% crit, +60 defense, 30% damage reduction, +60% movement speed, +200 max life.
- **Every variant's perk:** immune to fire, lava, cold, electricity and shadowflame, no slipping on ice, +20% attack speed, a chance to dodge attacks, +10 health per second.

| Ability | Base damage | What it does |
| --- | --- | --- |
| **Omega Cannon** | 160 every 6 ticks | **Hold left click**: a colossal rainbow beam, three times as wide as the variants' beams and twice as long. It **cuts through blocks** and applies every element's debuff. |
| **Prism Storm** | 140 ×30 | Thirty homing, exploding bolts of every element burst out in all directions (auto-fire). |
| **Elemental Cataclysm** | 450 per pillar | Twenty-five eruptions of every element ripple out across 100 tiles of ground around the cursor. **12 s cooldown.** |
| **Omega Barrage** | 1,500 per hit | Five satellites fire orbital strikes in a row across the cursor, each beam hitting about ten times. **20 s cooldown.** |
| **Singularity** | 200 every 10 ticks, collapse 1,200 ×5 | A black hole at the cursor drags in everything within ~44 tiles, **bosses included** (at a third of the speed), swallows enemy projectiles and shreds anything near its core. After 4 s it collapses into five huge elemental explosions. **25 s cooldown.** |
| **Time Stop** | none | Freezes every enemy within ~110 tiles in place for 6 s. Bosses aren't frozen. **40 s cooldown.** |
| **Omega Dash** | 400 (melee) | Dash toward the cursor, invincible for a second, leaving a line of six explosions of every element. |
| **Ascension** | none | For 15 s, all five variant power-ups at once: +45% damage, +25 defense, 20% damage reduction, +40% speed, +25% attack speed, burning hits, life steal and +10 health per second. **60 s cooldown.** |

All of them still scale with bosses beaten, on top of Omega Jack's +80% damage.

## God Jack

**Beyond Omega Jack.** The **God Trigger** is crafted from the Omega Trigger (which you keep) plus **30 Luminite Bar, 20 Solar Fragment and 15 Soul of Light** at the Ancient Manipulator.

**The entrance:** using the God Trigger doesn't just transform you. A pillar of heavenly light slams down and a marble-and-gold **heavenly gate** rises on a bed of clouds where you stand. You vanish inside it (frozen and invincible), its doors swing open in a flood of light with god rays turning behind it, and you **walk out of the doorway as God Jack** while the gate dissolves into golden sparkles.

- **Look:** white and gold armor, a gold visor, a blazing halo, small angel wings on the back, and a golden glow with drifting motes. Golden afterimages and jetpack sparks.
- **Stats:** +120% damage, +25% crit, +90 defense, 40% damage reduction, +80% movement speed, +300 max life.
- **Every perk:** immune to fire, lava, cold, electricity and shadowflame, no slipping on ice, +25% attack speed, a chance to dodge attacks, +20 health per second.
- **Abilities:** **Gods Wrath**, plus all eight of Omega Jack's abilities.

### Gods Wrath

1. **Hold left click to charge (5 seconds).** You rise into the air and hang there, glowing brighter and brighter gold. The air around you ignites into a growing **mini sun** with a white-hot core and turning rays, and the camera **slowly zooms out** (to about 80% of the normal view; the game doesn't draw the world much further than the screen, so zooming out more would show black edges). A giant **golden ring of judgement** appears around at least **half the world**: its diameter is half the world's width. It's far bigger than the screen, so you'll see it as a golden line when you're near its edge, and **it's drawn on the world map and minimap** so you can see everything it covers.
2. **When it's fully charged** the sun pulses white and the screen shakes.
3. **Release:** the screen whites out, the sun goes nova, and a golden shockwave races out to the edge of the ring over about 2.5 seconds. **Everything it passes is killed outright, bosses included**, and enemy projectiles are wiped out. **Spared:** all players, town NPCs (the Guide and friends), critters and target dummies.

Releasing before it's fully charged lets it fizzle out (no cooldown). After a full release there's a **60 second cooldown**.

## Robot Jetpack

An accessory worn in the **wings slot**. Anyone can wear it, transformed or not.

- **Flight:** 2.5 seconds of thrust (like mid-tier wings), speed 8, with flame and smoke from the nozzles.
- **Head ram:** while you're transformed (any form) and wearing it, moving fast (flying, dashing, falling) makes your robot head a weapon. Bumping into an enemy or a PvP opponent deals 40 base melee damage (scales with bosses beaten and melee bonuses) and heavy knockback, bounces you back, and sends them **spinning for as long as the knockback lasts** (longer knockback, longer spin; up to 1.5 s). Targets that ignore knockback, such as most bosses, take the damage but don't spin.
- **Recipe:** 15 Iron/Lead Bar, 10 Fallen Star, Rocket Boots @ Anvil.

## Art

All sprites are placeholders drawn by `tools/generate_sprites.py` (needs Pillow: `pip install pillow`; run it from this folder).
Replace any PNG with hand-made art at the same size whenever you like. The robot sheets (`Content/Players/RobotBody*.png`, `RobotLegs*.png`) use the vanilla player sheet layout: 20 frames of 40×56 stacked vertically (0 idle, 1–4 arm aiming up → down, 5 jump, 6–19 walk cycle). The `_Glow` sheets are drawn full-bright on top.

`tools/robot_preview_8x.png` is an enlarged preview of a few robot frames, and `tools/jack_variants_preview_6x.png` shows Robot Jack next to his five variants, Omega Jack and God Jack, and `tools/god_jack_preview.png` the heavenly gate opening, whose sheets (`Content/Players/<Variant>Body*.png`, `<Variant>Legs*.png`) use the same layout.
