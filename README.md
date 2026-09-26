# Robot Jack — a tModLoader mod for Terraria

A small standalone mod for **tModLoader 1.4.4**: find the **Robot Trigger**, transform into a sci-fi combat robot, and use ability items, headlined by the **Orbital Cannon Strike**. In Hardmode, craft cores to become one of three giant **Titans**: Titan Speaker, Titan Camera and Titan TV.

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

## Titan forms

Three more forms, each with its own transform item (use to transform, use again to turn back; no time limit or cooldown). Only one form can be active at a time: using any transform item ends the current one. Each Titan has **only its own abilities**, not Robot Jack's.

**Titans are giant:** drawn at twice the player's size, and their hitbox is too (36×84 instead of 20×42, about 2 blocks wide and 5 tall). You won't fit through small gaps. If there isn't room when you transform, you stay normal size until there is. While riding a mount you're normal size. To make up for being easier to hit, Titans are tougher. All Titans also get knockback and fall damage immunity, higher jumps and the hover jets, and the Robot Jetpack head ram works for them too.

| Form | Transform item (Mythril/Orichalcum Anvil) | Stats |
| --- | --- | --- |
| **Titan Speaker** | Titan Speaker Core: 12 Hallowed Bar, 10 Soul of Might | +30% damage, +40 defense, 20% damage reduction, +15% speed, +150 max life |
| **Titan Camera** | Titan Camera Core: 12 Hallowed Bar, 10 Soul of Sight | +35% damage, +15% crit, +30 defense, 15% damage reduction, +20% speed, +100 max life |
| **Titan TV** | Titan TV Core: 12 Hallowed Bar, 10 Soul of Fright | +30% damage, +30 defense, 15% damage reduction, +35% speed, +100 max life |

Titan ability damage scales with bosses beaten like Robot Jack's.

Each Titan has **8 abilities**. They all go into your inventory when you transform, so keep 8 slots free. Some have cooldowns: the slot darkens and counts down the seconds while you wait.

| Titan | Ability | Base damage | What it does |
| --- | --- | --- | --- |
| Speaker | **Sonic Boom** | 80 | A huge sound wave from your speaker head that grows as it flies and passes through everything. Heavy knockback. |
| Speaker | **Bass Drop** | 120 | A ring of bass bursts out around you: hits everything nearby once, throws it away from you and confuses it. Screen shake. |
| Speaker | **Speaker Barrage** | 35 | Rapid-fire small sound waves (auto-fire). |
| Speaker | **Speaker Army** | 45 (summon) | Summon **six speaker-headed soldiers** for 20 s. They march along the ground, jump over obstacles, chase the nearest enemy, hit what they run into and fire small sound waves at enemies in range. **30 s cooldown.** |
| Speaker | **Sonic Shield** | 30 | A bubble of sound around you for 6 s: shatters enemy projectiles that touch it, pushes enemies out and hurts them. **20 s cooldown.** |
| Speaker | **Subwoofer Quake** | 95 | Stomp: two giant sound waves roll out along the ground, one each way. |
| Speaker | **Feedback Loop** | 50 ×12 | Twelve sound waves burst out in every direction. |
| Speaker | **Boom Dash** | 85 (melee) | Dash toward the cursor on a burst of bass, confusing everything you hit, leaving a sound wave behind you. |
| Camera | **Core Laser** | 45 every 6 ticks | **Hold left click**: a continuous blue laser from your chest core. It turns to follow the cursor and stops at blocks. |
| Camera | **Camera Flash** | 60 | A blinding flash from your lens: damages and confuses everything nearby and **stuns** enemies in place for 2.5 s. Bosses aren't stunned. |
| Camera | **Lens Burst** | 40 ×3 | Three homing blue orbs (auto-fire). |
| Camera | **Camera Army** | 45 (summon) | Summon **six camera-headed soldiers** for 20 s that march, chase and fight like the Speaker Army, shooting plasma bolts. **30 s cooldown.** |
| Camera | **Target Lock** | none | Lock on to every enemy near the cursor: a red reticle marks them, and they take **50% more damage** from everything for 8 s (except the Orbital Cannon Strike, which always does exactly its damage). |
| Camera | **Zoom Shot** | 220 | A big, fast plasma shot from your zoom lens that pierces up to 6 enemies. |
| Camera | **Rewind** | none | Jump back to where you were 3 seconds ago, and get back the health you had then if it was more. **20 s cooldown.** |
| Camera | **Flash Grenades** | 55 ×3 | Throw three flash bulbs that pop into small camera flashes: damage, confusion, 1.5 s stun (not bosses). |
| TV | **Hypno Screen** | 30 ×3 | Three hypnotic waves from your screen: confuse enemies for 5 s and stun them for 1.5 s. Bosses aren't stunned. |
| TV | **Energy Blades** | 90 (melee) | Swing a giant energy blade in a wide arc toward the cursor (auto-swing). |
| TV | **Static Storm** | 30 ×6 | Spray six homing purple orbs (auto-fire). |
| TV | **TV Army** | 45 (summon) | Summon **six TV-headed soldiers** for 20 s that march, chase and fight like the Speaker Army, firing homing static orbs. **30 s cooldown.** |
| TV | **Channel Surf** | 35 per orb | Teleport to the cursor (up to ~56 tiles away, only if there's room), bursting five static orbs out where you leave and five where you arrive. |
| TV | **Blade Dash** | 100 (melee) | Dash toward the cursor with an energy blade swing leading the way. |
| TV | **Static Field** | 40 | A crackling field around you for 8 s that zaps every enemy inside with lightning three times a second. **15 s cooldown.** |
| TV | **Broadcast Beam** | 40 every 6 ticks | **Hold left click**: a purple beam from your screen that follows the cursor and stops at blocks. |

## Robot Jetpack

An accessory worn in the **wings slot**. Anyone can wear it, transformed or not.

- **Flight:** 2.5 seconds of thrust (like mid-tier wings), speed 8, with flame and smoke from the nozzles.
- **Head ram:** while you're transformed (any form) and wearing it, moving fast (flying, dashing, falling) makes your robot head a weapon. Bumping into an enemy or a PvP opponent deals 40 base melee damage (scales with bosses beaten and melee bonuses) and heavy knockback, bounces you back, and sends them **spinning for as long as the knockback lasts** (longer knockback, longer spin; up to 1.5 s). Targets that ignore knockback, such as most bosses, take the damage but don't spin.
- **Recipe:** 15 Iron/Lead Bar, 10 Fallen Star, Rocket Boots @ Anvil.

## Art

All sprites are placeholders drawn by `tools/generate_sprites.py` (needs Pillow: `pip install pillow`; run it from this folder).
Replace any PNG with hand-made art at the same size whenever you like. The robot sheets (`Content/Players/RobotBody*.png`, `RobotLegs*.png`) use the vanilla player sheet layout: 20 frames of 40×56 stacked vertically (0 idle, 1–4 arm aiming up → down, 5 jump, 6–19 walk cycle). The `_Glow` sheets are drawn full-bright on top.

`tools/robot_preview_8x.png` is an enlarged preview of a few robot frames, and `tools/titan_preview_4x.png` shows Robot Jack next to the three Titans. The Titan sheets (`Content/Players/Titan*.png`) use the same layout at twice the size: 80×112 per frame.
