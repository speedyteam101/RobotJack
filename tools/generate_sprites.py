"""Generates the placeholder pixel-art sprites for Robot Jack.

Run from the RobotJack folder:  python3 tools/generate_sprites.py
Requires Pillow (pip install pillow). Pixel-art sprites are drawn at half size and
scaled 2x with nearest-neighbour, which matches Terraria's pixel style. The beam
textures are drawn at full size so they stay smooth when stretched.
Replace any PNG with hand-made art at the same size whenever you like.
"""
import math
from PIL import Image, ImageDraw

CLEAR = (0, 0, 0, 0)
OUTLINE = (20, 22, 30, 255)
STEEL_D = (60, 68, 84, 255)
STEEL = (110, 122, 145, 255)
STEEL_L = (175, 188, 210, 255)
NAVY = (40, 60, 110, 255)
BLUE = (60, 110, 190, 255)
BLUE_L = (110, 165, 235, 255)
CYAN = (90, 240, 255, 255)
CYAN_D = (30, 150, 200, 255)
WHITE = (250, 255, 255, 255)
ORANGE = (255, 150, 40, 255)
ORANGE_L = (255, 220, 120, 255)
RED = (230, 40, 50, 255)
RED_D = (140, 20, 30, 255)
GOLD = (240, 200, 60, 255)

FRAME_W, FRAME_H = 20, 28  # half size of a vanilla player frame (40x56)
FRAMES = 20


def canvas(w, h):
    img = Image.new("RGBA", (w, h), CLEAR)
    return img, ImageDraw.Draw(img)


def save(img, path, scale=2):
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    img.save(path)
    print("wrote", path, img.width, "x", img.height)


def thick_line(d, p0, p1, color, width):
    d.line([p0, p1], fill=color, width=width)


# ---------------------------------------------------------------- player sheets
# Frame layout follows the vanilla player sheet, so the game's own bodyFrame/legFrame pick the pose:
# 0 = idle, 1-4 = arm pointing up / up-forward / forward / down-forward (item use), 5 = jump, 6-19 = walk cycle.

ARM_ANGLES = {1: -90, 2: -45, 3: 0, 4: 45}  # degrees, 0 = forward (right), positive = down


def walk_phase(frame):
    return (frame - 6) / 14 * math.tau


# Robot Jack: white hero armour with navy accents, a glowing cyan V-visor and chest core,
# a back jetpack and a red scarf that streams out behind him. Drawn on the 20x28 grid, saved 2x.
RJ_HI = (248, 250, 255, 255)
RJ = (212, 218, 232, 255)
RJ_MID = (158, 168, 190, 255)
RJ_D = (100, 110, 135, 255)
RJ_NAVY = (34, 48, 96, 255)
RJ_NAVY_L = (62, 88, 160, 255)
RJ_VISOR = (18, 24, 44, 255)
SCARF = (225, 45, 55, 255)
SCARF_L = (255, 110, 110, 255)
SCARF_D = (140, 20, 35, 255)
CYAN_HALO = (90, 240, 255, 110)


def rj_scarf_tail(d, ox, y, frame):
    """Scarf tail behind the neck: hangs down the back when standing, streams out behind when moving."""
    if 6 <= frame <= 19:
        wave = round(math.sin(walk_phase(frame) * 2) * 1.2)
        pts = [(7, 10), (4, 10 + wave), (1, 11 + wave), (0, 13 + wave), (2, 13 + wave), (4, 12 + wave), (7, 12)]
    elif frame == 5:
        # Airborne: it flies up and back.
        pts = [(7, 10), (5, 8), (2, 6), (1, 7), (3, 9), (5, 11), (7, 12)]
    else:
        pts = [(7, 10), (5, 10), (4, 12), (4, 16), (6, 16), (6, 13), (7, 12)]
    pts = [(ox + max(0, x), y + yy) for x, yy in pts]
    d.polygon(pts, fill=OUTLINE)
    # Fill one pixel inside the outline by drawing the same shape's spine in colour.
    for a, b in zip(pts[1:4], pts[2:5]):
        d.line([a, b], fill=SCARF)
    d.line([pts[0], pts[1]], fill=SCARF_L)
    d.line([pts[4], pts[5]], fill=SCARF_D)


def rj_scarf_knot(d, ox, y):
    """Scarf wrapped around the neck, at the base of the helmet."""
    d.rectangle([ox + 6, y + 9, ox + 12, y + 11], fill=OUTLINE)
    d.line([ox + 7, y + 9, ox + 11, y + 9], fill=SCARF_L)
    d.line([ox + 7, y + 10, ox + 11, y + 10], fill=SCARF)
    d.point((ox + 7, y + 11), fill=SCARF_D)


def rj_arm(d, g, ox, y, frame, back):
    shoulder = (ox + (8 if back else 11), y + 12)
    if frame in ARM_ANGLES:
        a = math.radians(ARM_ANGLES[frame])
        hand = (shoulder[0] + round(math.cos(a) * 7), shoulder[1] + round(math.sin(a) * 7))
    else:
        swing = 0
        if 6 <= frame <= 19:
            swing = round(2 * math.sin(walk_phase(frame)) * (-1 if back else 1))
        elif frame == 5:
            swing = -2 if back else 2
        hand = (shoulder[0] + swing, shoulder[1] + 6)
    base = RJ_MID if back else RJ
    thick_line(d, shoulder, hand, OUTLINE, 4)
    thick_line(d, shoulder, hand, base, 2)
    if not back:
        # Navy forearm guard with a glowing strip
        mid = ((shoulder[0] + hand[0]) // 2, (shoulder[1] + hand[1]) // 2)
        d.line([mid, hand], fill=RJ_NAVY_L, width=2)
        g.line([mid, hand], fill=(90, 240, 255, 150))
    # Fist / arm cannon muzzle
    d.rectangle([hand[0] - 1, hand[1] - 1, hand[0] + 1, hand[1] + 1], fill=OUTLINE)
    d.point(hand, fill=RJ_HI if not back else RJ_D)
    if not back:
        g.point(hand, fill=CYAN_HALO)
    # Rounded shoulder pad
    sx, sy = shoulder
    d.rectangle([sx - 2, sy - 2, sx + 2, sy + 1], fill=OUTLINE)
    d.rectangle([sx - 1, sy - 1, sx + 1, sy], fill=RJ_D if back else RJ)
    if not back:
        d.point((sx - 1, sy - 1), fill=RJ_HI)
        d.point((sx + 1, sy), fill=RJ_NAVY_L)


def draw_body_frame(d, g, ox, oy, frame):
    bob = 0
    if 6 <= frame <= 19 and abs(math.sin(walk_phase(frame))) > 0.7:
        bob = 1  # dip down; moving up would spill into the frame above
    y = oy + bob
    rj_arm(d, g, ox, y, frame, back=True)

    # Jetpack: two tanks with glowing nozzles
    d.rectangle([ox + 2, y + 10, ox + 6, y + 17], fill=OUTLINE)
    d.rectangle([ox + 3, y + 11, ox + 5, y + 16], fill=RJ_MID)
    d.line([ox + 3, y + 11, ox + 3, y + 15], fill=RJ)
    d.line([ox + 4, y + 13, ox + 5, y + 13], fill=RJ_NAVY)
    for nx in (ox + 3, ox + 5):
        d.point((nx, y + 17), fill=RJ_D)
        g.point((nx, y + 18), fill=ORANGE_L)
        g.point((nx, y + 19), fill=(255, 150, 40, 120))
    rj_scarf_tail(d, ox, y, frame)

    # Torso: white chest plate over a navy undersuit
    d.rectangle([ox + 5, y + 10, ox + 14, y + 18], fill=OUTLINE)
    d.rectangle([ox + 6, y + 11, ox + 13, y + 17], fill=RJ_NAVY)
    d.polygon([(ox + 6, y + 11), (ox + 13, y + 11), (ox + 12, y + 15), (ox + 7, y + 15)], fill=RJ)
    d.line([ox + 6, y + 11, ox + 13, y + 11], fill=RJ_HI)
    d.line([ox + 7, y + 15, ox + 12, y + 15], fill=RJ_MID)
    d.rectangle([ox + 7, y + 16, ox + 12, y + 17], fill=RJ_D)       # belt
    d.point((ox + 10, y + 16), fill=GOLD)
    # Chest core
    d.rectangle([ox + 9, y + 12, ox + 11, y + 14], fill=OUTLINE)
    d.point((ox + 10, y + 13), fill=CYAN)
    g.rectangle([ox + 8, y + 11, ox + 12, y + 15], fill=(90, 240, 255, 45))
    g.point((ox + 9, y + 13), fill=CYAN_HALO); g.point((ox + 11, y + 13), fill=CYAN_HALO)
    g.point((ox + 10, y + 12), fill=CYAN_HALO); g.point((ox + 10, y + 14), fill=CYAN_HALO)
    g.point((ox + 10, y + 13), fill=WHITE)

    # Helmet: rounded white dome with a navy crest and a glowing V visor
    d.rounded_rectangle([ox + 5, y + 2, ox + 14, y + 9], radius=2, fill=OUTLINE)
    d.rounded_rectangle([ox + 6, y + 3, ox + 13, y + 8], radius=1, fill=RJ)
    d.line([ox + 7, y + 3, ox + 12, y + 3], fill=RJ_HI)
    d.line([ox + 6, y + 8, ox + 13, y + 8], fill=RJ_MID)
    d.line([ox + 6, y + 3, ox + 6, y + 7], fill=RJ_MID)                # back of the helmet in shade
    # Crest fin running over the top and an antenna
    d.line([ox + 8, y + 2, ox + 11, y + 2], fill=RJ_NAVY)
    d.line([ox + 7, y + 1, ox + 7, y + 2], fill=RJ_D)
    d.point((ox + 7, y + 0), fill=CYAN)
    g.point((ox + 7, y + 0), fill=WHITE)
    # Ear piece
    d.rectangle([ox + 6, y + 5, ox + 7, y + 6], fill=RJ_NAVY_L)
    g.point((ox + 7, y + 5), fill=CYAN_HALO)
    # V-shaped visor (facing right): wide on top, narrowing below
    d.rectangle([ox + 9, y + 4, ox + 13, y + 6], fill=RJ_VISOR)
    d.line([ox + 9, y + 4, ox + 13, y + 4], fill=CYAN_D)
    d.line([ox + 10, y + 5, ox + 13, y + 5], fill=CYAN)
    d.point((ox + 12, y + 6), fill=CYAN_D)
    g.line([ox + 9, y + 4, ox + 13, y + 4], fill=(90, 240, 255, 160))
    g.line([ox + 10, y + 5, ox + 13, y + 5], fill=WHITE)
    g.point((ox + 12, y + 6), fill=CYAN)
    g.point((ox + 14, y + 5), fill=(90, 240, 255, 90))                 # glow spilling out of the visor
    # Jaw plate
    d.line([ox + 9, y + 7, ox + 12, y + 7], fill=RJ_MID)
    rj_scarf_knot(d, ox, y)

    rj_arm(d, g, ox, y, frame, back=False)


def draw_leg(d, g, hip, foot, back):
    knee = ((hip[0] + foot[0]) // 2 + 1, (hip[1] + foot[1]) // 2)
    base = RJ_MID if back else RJ
    thick_line(d, hip, knee, OUTLINE, 4)
    thick_line(d, knee, foot, OUTLINE, 4)
    thick_line(d, hip, knee, RJ_NAVY if back else RJ_NAVY_L, 2)        # navy thigh
    thick_line(d, knee, foot, base, 2)                                 # white shin guard
    if not back:
        g.point(((knee[0] + foot[0]) // 2, (knee[1] + foot[1]) // 2), fill=(90, 240, 255, 130))
    # Knee cap
    d.rectangle([knee[0] - 1, knee[1] - 1, knee[0], knee[1]], fill=RJ_HI if not back else RJ_D)
    # Boot with a lighter toe
    d.rectangle([foot[0] - 1, foot[1] - 1, foot[0] + 2, foot[1]], fill=OUTLINE)
    d.line([foot[0] - 1, foot[1] - 1, foot[0] + 1, foot[1] - 1], fill=RJ_NAVY if back else RJ_NAVY_L)
    d.point((foot[0] + 2, foot[1] - 1), fill=RJ_D if back else RJ_HI)


def draw_legs_frame(d, g, ox, oy, frame):
    hip_y = oy + 18
    ground = oy + 25
    back_hip = (ox + 8, hip_y)
    front_hip = (ox + 11, hip_y)
    if frame == 5:
        back_foot = (ox + 6, ground - 2)
        front_foot = (ox + 13, ground - 3)
    elif 6 <= frame <= 19:
        s = math.sin(walk_phase(frame))
        lift_front = 1 if s > 0.3 else 0
        lift_back = 1 if s < -0.3 else 0
        front_foot = (ox + 11 + round(3 * s), ground - lift_front)
        back_foot = (ox + 8 - round(3 * s), ground - lift_back)
    else:
        back_foot = (ox + 8, ground)
        front_foot = (ox + 11, ground)
    draw_leg(d, g, back_hip, back_foot, True)
    draw_leg(d, g, front_hip, front_foot, False)
    # Hip plate with a glowing buckle line
    d.rectangle([ox + 6, hip_y - 1, ox + 13, hip_y], fill=RJ_NAVY)
    d.line([ox + 7, hip_y - 1, ox + 12, hip_y - 1], fill=RJ_NAVY_L)
    # Boot jets glow at the heels
    for foot in (back_foot, front_foot):
        g.point((foot[0] - 1, foot[1]), fill=(255, 160, 60, 130))


def robot_sheets():
    body, bd = canvas(FRAME_W, FRAME_H * FRAMES)
    body_glow, bg = canvas(FRAME_W, FRAME_H * FRAMES)
    legs, ld = canvas(FRAME_W, FRAME_H * FRAMES)
    legs_glow, lg = canvas(FRAME_W, FRAME_H * FRAMES)
    for f in range(FRAMES):
        draw_body_frame(bd, bg, 0, f * FRAME_H, f)
        draw_legs_frame(ld, lg, 0, f * FRAME_H, f)
    save(body, "Content/Players/RobotBody.png")
    save(body_glow, "Content/Players/RobotBody_Glow.png")
    save(legs, "Content/Players/RobotLegs.png")
    save(legs_glow, "Content/Players/RobotLegs_Glow.png")
    # Big preview of a few frames (idle, the four aim poses, jump, walk), glow layers on top like in game.
    preview = Image.new("RGBA", (FRAME_W * 8, FRAME_H), (28, 32, 46, 255))
    for i, f in enumerate([0, 1, 2, 3, 4, 5, 9, 16]):
        for sheet in (legs, legs_glow, body, body_glow):
            preview.alpha_composite(sheet.crop((0, f * FRAME_H, FRAME_W, (f + 1) * FRAME_H)), (i * FRAME_W, 0))
    save(preview, "tools/robot_preview_8x.png", scale=8)


# ---------------------------------------------------------------- items and buffs

def robot_trigger():
    img, d = canvas(14, 16)
    d.rounded_rectangle([3, 5, 10, 15], radius=2, fill=OUTLINE)
    d.rounded_rectangle([4, 6, 9, 14], radius=1, fill=STEEL)
    d.line([4, 6, 4, 14], fill=STEEL_L)
    d.rectangle([5, 0, 8, 5], fill=OUTLINE)       # big button housing
    d.rectangle([6, 1, 7, 4], fill=RED)
    d.point((6, 1), fill=(255, 140, 140, 255))
    d.rectangle([5, 8, 8, 9], fill=CYAN_D)       # screen
    d.point((6, 8), fill=CYAN)
    d.point((6, 11), fill=GOLD); d.point((7, 12), fill=GOLD)
    save(img, "Content/Items/RobotTrigger.png")


def orbital_icon():
    img, d = canvas(16, 16)
    d.rectangle([0, 1, 4, 3], fill=BLUE); d.rectangle([11, 1, 15, 3], fill=BLUE)
    d.line([0, 2, 15, 2], fill=NAVY)
    d.rectangle([5, 0, 10, 4], fill=OUTLINE); d.rectangle([6, 1, 9, 3], fill=STEEL_L)
    d.rectangle([6, 5, 9, 15], fill=CYAN_D)
    d.rectangle([7, 5, 8, 15], fill=WHITE)
    d.line([4, 15, 11, 15], fill=CYAN)
    save(img, "Content/Abilities/OrbitalCannonStrike.png")


def plasma_icon():
    img, d = canvas(16, 12)
    d.rounded_rectangle([0, 2, 10, 9], radius=2, fill=OUTLINE)
    d.rounded_rectangle([1, 3, 9, 8], radius=1, fill=STEEL)
    d.line([1, 3, 9, 3], fill=STEEL_L)
    d.rectangle([10, 4, 14, 7], fill=OUTLINE); d.rectangle([10, 5, 13, 6], fill=STEEL_D)
    d.point((15, 5), fill=CYAN); d.point((15, 6), fill=CYAN)
    d.rectangle([3, 5, 6, 6], fill=CYAN_D); d.point((4, 5), fill=CYAN)
    save(img, "Content/Abilities/PlasmaCannon.png")


def missile(d, x, y):
    d.rectangle([x, y + 1, x + 2, y + 7], fill=OUTLINE)
    d.point((x + 1, y), fill=RED)
    d.line([x + 1, y + 1, x + 1, y + 6], fill=STEEL_L)
    d.point((x, y + 7), fill=RED_D); d.point((x + 2, y + 7), fill=RED_D)
    d.point((x + 1, y + 8), fill=ORANGE)


def missiles_icon():
    img, d = canvas(16, 16)
    missile(d, 1, 6); missile(d, 6, 1); missile(d, 11, 5)
    d.point((7, 11), fill=ORANGE_L); d.point((2, 15), fill=ORANGE_L); d.point((12, 14), fill=ORANGE_L)
    save(img, "Content/Abilities/MissileBarrage.png")


def thruster_icon():
    img, d = canvas(16, 16)
    d.rectangle([9, 3, 15, 12], fill=OUTLINE); d.rectangle([10, 4, 14, 11], fill=STEEL)
    d.line([10, 4, 14, 4], fill=STEEL_L)
    d.polygon([(9, 5), (0, 7), (0, 8), (9, 10)], fill=ORANGE)
    d.polygon([(9, 6), (3, 7), (3, 8), (9, 9)], fill=ORANGE_L)
    d.point((11, 7), fill=CYAN); d.point((12, 7), fill=CYAN)
    save(img, "Content/Abilities/ThrusterDash.png")


def buff_icon(path, inner):
    img, d = canvas(16, 16)
    d.rectangle([0, 0, 15, 15], fill=OUTLINE)
    d.rectangle([1, 1, 14, 14], fill=NAVY)
    inner(d)
    save(img, path)


def robot_form_buff():
    def inner(d):
        d.rectangle([3, 3, 12, 11], fill=STEEL)
        d.rectangle([5, 6, 11, 7], fill=CYAN)
        d.line([4, 1, 4, 3], fill=STEEL_D); d.point((4, 1), fill=RED)
        d.rectangle([5, 12, 10, 13], fill=STEEL_D)
    buff_icon("Content/Buffs/RobotForm.png", inner)


def orbital_cooldown_buff():
    def inner(d):
        d.rectangle([4, 2, 11, 5], fill=STEEL_D)
        d.rectangle([2, 3, 3, 4], fill=BLUE); d.rectangle([12, 3, 13, 4], fill=BLUE)
        d.rectangle([7, 6, 8, 13], fill=RED_D)
        d.ellipse([4, 8, 11, 14], outline=RED)
    buff_icon("Content/Buffs/OrbitalCannonCooldown.png", inner)


# ---------------------------------------------------------------- projectiles

def plasma_bolt():
    img, d = canvas(10, 5)
    d.ellipse([0, 0, 9, 4], fill=(40, 170, 255, 170))
    d.ellipse([2, 1, 9, 3], fill=CYAN)
    d.line([4, 2, 8, 2], fill=WHITE)
    save(img, "Content/Projectiles/PlasmaBolt.png")


def homing_missile():
    # Points up; the projectile adds PiOver2 to its rotation.
    img, d = canvas(5, 11)
    d.rectangle([1, 1, 3, 8], fill=OUTLINE)
    d.point((2, 0), fill=RED)
    d.line([2, 1, 2, 7], fill=STEEL_L)
    d.point((1, 2), fill=RED); d.point((3, 2), fill=RED)
    d.rectangle([0, 7, 0, 9], fill=RED_D); d.rectangle([4, 7, 4, 9], fill=RED_D)
    d.point((2, 9), fill=ORANGE); d.point((2, 10), fill=ORANGE_L)
    save(img, "Content/Projectiles/HomingMissile.png")


def invisible(path):
    img, _ = canvas(1, 1)
    save(img, path, scale=1)


def reticle():
    # Orbital lock-on reticle (also the OrbitalStrike projectile's own texture). Drawn white, tinted in code.
    img, d = canvas(40, 40)
    c = 19.5
    d.ellipse([3, 3, 36, 36], outline=WHITE, width=2)
    d.ellipse([12, 12, 27, 27], outline=(255, 255, 255, 170), width=1)
    for a in range(0, 360, 90):
        r = math.radians(a)
        x0, y0 = c + math.cos(r) * 11, c + math.sin(r) * 11
        x1, y1 = c + math.cos(r) * 20, c + math.sin(r) * 20
        d.line([x0, y0, x1, y1], fill=WHITE, width=2)
    for a in range(45, 360, 90):
        r = math.radians(a)
        x, y = c + math.cos(r) * 17, c + math.sin(r) * 17
        d.rectangle([x - 1, y - 1, x + 1, y + 1], fill=WHITE)
    d.rectangle([19, 19, 20, 20], fill=WHITE)
    save(img, "Content/Projectiles/OrbitalStrike.png")


def satellite():
    img, d = canvas(48, 28)
    # Solar panels
    for x0 in (0, 31):
        d.rectangle([x0, 6, x0 + 16, 14], fill=OUTLINE)
        d.rectangle([x0 + 1, 7, x0 + 15, 13], fill=NAVY)
        for x in range(x0 + 1, x0 + 16, 4):
            d.line([x, 7, x, 13], fill=BLUE)
        d.line([x0 + 1, 10, x0 + 15, 10], fill=BLUE)
        d.point((x0 + 3, 8), fill=BLUE_L)
    d.line([16, 10, 31, 10], fill=STEEL_D, width=2)
    # Body
    d.rectangle([17, 2, 30, 17], fill=OUTLINE)
    d.rectangle([18, 3, 29, 16], fill=STEEL)
    d.line([18, 3, 29, 3], fill=STEEL_L)
    d.rectangle([20, 6, 27, 8], fill=GOLD)
    d.point((21, 12), fill=RED); d.point((26, 12), fill=CYAN)
    # Antenna
    d.line([23, 0, 23, 2], fill=STEEL_D); d.point((24, 0), fill=RED)
    # Downward cannon
    d.rectangle([20, 18, 27, 21], fill=OUTLINE); d.rectangle([21, 18, 26, 20], fill=STEEL_D)
    d.rectangle([21, 22, 26, 25], fill=OUTLINE); d.rectangle([22, 22, 25, 24], fill=STEEL)
    d.rectangle([22, 26, 25, 27], fill=CYAN_D); d.line([23, 27, 24, 27], fill=CYAN)
    save(img, "Content/Projectiles/OrbitalSatellite.png")


def beam_strip():
    # Horizontal cross-section of the beam: white, soft edges. Stretched vertically in code.
    w, h = 64, 4
    img = Image.new("RGBA", (w, h), CLEAR)
    px = img.load()
    for x in range(w):
        t = abs((x + 0.5) / w * 2 - 1)  # 0 at centre, 1 at edges
        a = max(0.0, 1 - t) ** 1.6
        for y in range(h):
            px[x, y] = (255, 255, 255, int(255 * a))
    save(img, "Content/Projectiles/OrbitalBeam.png", scale=1)


def radial_glow():
    s = 96
    img = Image.new("RGBA", (s, s), CLEAR)
    px = img.load()
    c = (s - 1) / 2
    for y in range(s):
        for x in range(s):
            r = math.hypot(x - c, y - c) / c
            a = max(0.0, 1 - r) ** 2
            px[x, y] = (255, 255, 255, int(255 * a))
    save(img, "Content/Projectiles/OrbitalGlow.png", scale=1)


def shock_ring():
    s = 128
    img = Image.new("RGBA", (s, s), CLEAR)
    px = img.load()
    c = (s - 1) / 2
    for y in range(s):
        for x in range(s):
            r = math.hypot(x - c, y - c) / c
            a = max(0.0, 1 - abs(r - 0.85) / 0.15)
            px[x, y] = (255, 255, 255, int(255 * a ** 1.5))
    save(img, "Content/Projectiles/OrbitalRing.png", scale=1)


def mod_icon():
    img, d = canvas(40, 40)
    d.rectangle([0, 0, 39, 39], fill=(18, 24, 44, 255))
    for i in range(0, 40, 5):
        d.point((i * 7 % 40, i), fill=(200, 220, 255, 255))
    d.rectangle([17, 0, 22, 39], fill=(60, 200, 255, 90))
    d.rectangle([19, 0, 20, 39], fill=(220, 250, 255, 200))
    d.rectangle([7, 12, 32, 33], fill=OUTLINE)
    d.rectangle([9, 14, 30, 31], fill=STEEL)
    d.line([9, 14, 30, 14], fill=STEEL_L)
    d.rectangle([12, 19, 28, 23], fill=CYAN_D)
    d.line([13, 20, 27, 20], fill=CYAN)
    d.rectangle([13, 27, 26, 28], fill=STEEL_D)
    d.line([12, 7, 12, 11], fill=STEEL_D); d.rectangle([11, 5, 13, 7], fill=RED)
    save(img, "icon.png")


# ---------------------------------------------------------------- jetpack, absorber, spin

def jetpack_item():
    img, d = canvas(14, 16)
    for x0 in (1, 8):
        d.rounded_rectangle([x0, 1, x0 + 4, 11], radius=2, fill=OUTLINE)
        d.rounded_rectangle([x0 + 1, 2, x0 + 3, 10], radius=1, fill=STEEL)
        d.line([x0 + 1, 2, x0 + 1, 9], fill=STEEL_L)
        d.rectangle([x0 + 1, 11, x0 + 3, 12], fill=STEEL_D)
        d.point((x0 + 2, 13), fill=ORANGE); d.point((x0 + 2, 14), fill=ORANGE_L)
    d.rectangle([5, 4, 8, 7], fill=BLUE)
    d.point((6, 5), fill=CYAN)
    save(img, "Content/Items/RobotJetpack.png")


def jetpack_wings():
    # Worn on the back like wings: 4 frames stacked vertically, 20x28 each (40x56 in game).
    # Frame 0 = idle (no flame), 1-3 = flame flicker. Drawn facing right; the pack sits in the middle of the frame.
    frames = 4
    img, d = canvas(20, 28 * frames)
    for f in range(frames):
        oy = f * 28
        for x0 in (6, 10):
            d.rounded_rectangle([x0, oy + 8, x0 + 3, oy + 17], radius=1, fill=OUTLINE)
            d.rectangle([x0 + 1, oy + 9, x0 + 2, oy + 16], fill=STEEL)
            d.point((x0 + 1, oy + 9), fill=STEEL_L)
            d.rectangle([x0 + 1, oy + 17, x0 + 2, oy + 18], fill=STEEL_D)
            if f > 0:
                length = [0, 4, 6, 5][f]
                d.line([x0 + 1, oy + 19, x0 + 1, oy + 18 + length], fill=ORANGE)
                d.line([x0 + 2, oy + 19, x0 + 2, oy + 18 + length - 1], fill=ORANGE)
                d.point((x0 + 1, oy + 19), fill=ORANGE_L)
                d.point((x0 + 2, oy + 19), fill=WHITE)
        d.rectangle([9, oy + 11, 10, oy + 13], fill=BLUE)
    save(img, "Content/Items/RobotJetpack_Wings.png")


def absorber_icon():
    img, d = canvas(16, 16)
    d.ellipse([0, 0, 15, 15], outline=(160, 90, 255, 255))
    d.ellipse([3, 3, 12, 12], outline=CYAN)
    d.ellipse([6, 6, 9, 9], fill=WHITE)
    for (x, y) in [(1, 7), (14, 8), (7, 1), (8, 14)]:
        d.point((x, y), fill=CYAN)
    save(img, "Content/Abilities/EnergyAbsorber.png")


def spinning_buff():
    def inner(d):
        d.arc([3, 3, 12, 12], 200, 520, fill=ORANGE_L)
        d.polygon([(12, 5), (14, 8), (10, 8)], fill=ORANGE_L)
        d.ellipse([6, 6, 9, 9], fill=STEEL_L)
    buff_icon("Content/Buffs/Spinning.png", inner)



def gravity_arm_icon():
    img, d = canvas(16, 16)
    # Robot forearm reaching up-right, with a small gravity well at the palm.
    d.line([1, 14, 7, 8], fill=OUTLINE, width=4)
    d.line([1, 14, 7, 8], fill=STEEL, width=2)
    d.rectangle([6, 7, 9, 10], fill=OUTLINE); d.point((8, 8), fill=CYAN)
    d.ellipse([8, 0, 15, 7], outline=(170, 80, 255, 255))
    d.ellipse([10, 2, 13, 5], fill=(110, 255, 190, 255))
    d.point((11, 3), fill=WHITE)
    save(img, "Content/Abilities/GravityArm.png")



# ---------------------------------------------------------------- Titan sheets
# Titans are drawn on a 40x56 grid per frame (the vanilla frame's real pixel size) and saved 2x,
# so each frame is 80x112: twice the player's size, matching the Titans' bigger hitbox.
# Same 20-frame layout as the robot sheets. Feet stand on row 51; the hitbox covers rows 10-51.

TW, TH = 40, 56

def _shade(c, f):
    """Scale an RGBA colour's brightness by f."""
    return tuple(max(0, min(255, int(v * f))) for v in c[:3]) + (255,)


def _alpha(c, a):
    return tuple(c[:3]) + (a,)


def make_palette(armor, accent, head_main, head_dark):
    return dict(
        hi=_shade(armor, 1.6), light=_shade(armor, 1.25), base=armor, mid=_shade(armor, 0.75), dark=_shade(armor, 0.5),
        metal_hi=(215, 220, 230, 255), metal=(150, 156, 170, 255), metal_d=(90, 95, 108, 255),
        accent=accent, accent_l=_shade(accent, 1.35), accent_d=_shade(accent, 0.6),
        head=head_main, head_hi=_shade(head_main, 1.5), head_d=head_dark,
    )


TITANS = {
    "TitanSpeaker": make_palette((52, 54, 64, 255), (255, 55, 55, 255), (34, 34, 40, 255), (18, 18, 22, 255)),
    "TitanCamera": make_palette((38, 50, 82, 255), (60, 175, 255, 255), (160, 166, 180, 255), (95, 100, 114, 255)),
    "TitanTV": make_palette((62, 46, 80, 255), (205, 85, 255, 255), (70, 58, 82, 255), (38, 30, 46, 255)),
}


def plate(d, box, pal, base="base", radius=1, bevel=True):
    """Outlined armour plate with a lit top-left edge and a shaded bottom-right edge."""
    x0, y0, x1, y1 = box
    d.rounded_rectangle([x0, y0, x1, y1], radius=radius, fill=OUTLINE)
    d.rounded_rectangle([x0 + 1, y0 + 1, x1 - 1, y1 - 1], radius=max(0, radius - 1), fill=pal[base])
    if bevel and x1 - x0 > 3 and y1 - y0 > 3:
        light = {"base": "light", "mid": "base", "dark": "mid"}.get(base, "light")
        shadow = {"base": "mid", "mid": "dark", "dark": "dark"}.get(base, "mid")
        d.line([x0 + 1 + radius // 2, y0 + 1, x1 - 2, y0 + 1], fill=pal[light])
        d.line([x0 + 1, y0 + 1 + radius // 2, x0 + 1, y1 - 2], fill=pal[light])
        d.line([x0 + 2, y1 - 1, x1 - 1, y1 - 1], fill=pal[shadow])
        d.line([x1 - 1, y0 + 2, x1 - 1, y1 - 1], fill=pal[shadow])


def limb(d, a, b, width, pal, back):
    """Thick outlined limb segment with a highlight along its upper edge."""
    base = pal["mid"] if back else pal["base"]
    hi = pal["base"] if back else pal["light"]
    thick_line(d, a, b, OUTLINE, width + 3)
    thick_line(d, a, b, base, width)
    off = max(1, width // 3)
    thick_line(d, (a[0] - off + 1, a[1] - off + 1), (b[0] - off + 1, b[1] - off + 1), hi, 1)


def joint(d, p, r, pal, back):
    d.ellipse([p[0] - r - 1, p[1] - r - 1, p[0] + r + 1, p[1] + r + 1], fill=OUTLINE)
    d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=pal["metal_d"] if back else pal["metal"])
    if not back:
        d.point((p[0] - 1, p[1] - 1), fill=pal["metal_hi"])


def glow_core(d, g, c, r, pal):
    """A glowing energy core: dark socket, accent ring, bright centre, soft halo on the glow layer."""
    cx, cy = c
    d.ellipse([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + 2], fill=OUTLINE)
    d.ellipse([cx - r - 1, cy - r - 1, cx + r + 1, cy + r + 1], fill=pal["metal_d"])
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=pal["accent_d"])
    g.ellipse([cx - r - 2, cy - r - 2, cx + r + 2, cy + r + 2], fill=_alpha(pal["accent"], 70))
    g.ellipse([cx - r, cy - r, cx + r, cy + r], fill=pal["accent"])
    if r >= 2:
        g.ellipse([cx - r + 1, cy - r + 1, cx + r - 1, cy + r - 1], fill=pal["accent_l"])
    g.point((cx, cy), fill=WHITE)
    g.point((cx - 1, cy - 1), fill=WHITE)


# ---------------------------------------------------------------- heads (facing right)

def speaker_head(d, g, ox, y, pal):
    # Cabinet
    plate(d, (ox + 7, y + 1, ox + 33, y + 19), dict(pal, base=pal["head"], light=pal["head_hi"], mid=pal["head_d"]), radius=2)
    # Recessed front baffle
    d.rectangle([ox + 10, y + 4, ox + 28, y + 17], fill=pal["head_d"])
    d.line([ox + 10, y + 17, ox + 28, y + 17], fill=_shade(pal["head"], 1.2))
    # Main woofer: silver rim, rubber surround, cone, dust cap
    cx, cy = ox + 19, y + 11
    for r, col in ((6, pal["metal"]), (5, OUTLINE), (4, (44, 44, 50, 255)), (2, (66, 66, 74, 255)), (1, (110, 110, 120, 255))):
        d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=col)
    d.point((cx - 5, cy - 3), fill=pal["metal_hi"]); d.point((cx - 4, cy - 4), fill=pal["metal_hi"])
    d.point((cx - 2, cy - 2), fill=(90, 90, 100, 255))
    g.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], outline=_alpha(pal["accent"], 150))
    g.ellipse([cx - 6, cy - 6, cx + 6, cy + 6], outline=_alpha(pal["accent_l"], 90))
    # Tweeter
    tx, ty = ox + 26, y + 6
    d.ellipse([tx - 2, ty - 2, tx + 2, ty + 2], fill=pal["metal"])
    d.ellipse([tx - 1, ty - 1, tx + 1, ty + 1], fill=OUTLINE)
    g.point((tx, ty), fill=pal["accent"])
    # Screw heads in the corners of the baffle
    for (sx, sy) in ((ox + 11, y + 5), (ox + 27, y + 16), (ox + 11, y + 16)):
        d.point((sx, sy), fill=pal["metal"])
    # Side panel with vents (facing right) and an LED strip under the woofer
    d.rectangle([ox + 29, y + 3, ox + 31, y + 17], fill=_shade(pal["head"], 0.8))
    for vy in range(y + 5, y + 17, 2):
        d.line([ox + 29, vy, ox + 31, vy], fill=OUTLINE)
    for lx in range(ox + 13, ox + 26, 2):
        g.point((lx, y + 18), fill=pal["accent_l"])


def camera_head(d, g, ox, y, pal):
    headpal = dict(pal, base=pal["head"], light=pal["head_hi"], mid=pal["head_d"], dark=_shade(pal["head_d"], 0.7))
    # Carry handle on top: a grip on two posts
    d.rectangle([ox + 12, y + 1, ox + 23, y + 3], fill=OUTLINE)
    d.line([ox + 13, y + 2, ox + 22, y + 2], fill=pal["metal_d"])
    d.line([ox + 14, y + 2, ox + 19, y + 2], fill=pal["metal"])
    for px in (ox + 13, ox + 22):
        d.rectangle([px - 1, y + 3, px + 1, y + 5], fill=OUTLINE)
        d.line([px, y + 3, px, y + 5], fill=pal["metal_d"])
    # Viewfinder at the back
    plate(d, (ox + 4, y + 6, ox + 10, y + 12), dict(headpal, base=pal["head_d"]), radius=1)
    d.rectangle([ox + 5, y + 8, ox + 6, y + 10], fill=OUTLINE)
    # Body
    plate(d, (ox + 8, y + 5, ox + 29, y + 18), headpal, radius=2)
    d.rectangle([ox + 10, y + 14, ox + 27, y + 16], fill=pal["head_d"])          # lower band
    d.line([ox + 10, y + 12, ox + 27, y + 12], fill=pal["accent_d"])             # brand stripe
    g.line([ox + 10, y + 12, ox + 20, y + 12], fill=_alpha(pal["accent"], 170))
    # REC light and buttons
    d.point((ox + 11, y + 8), fill=RED)
    g.point((ox + 11, y + 8), fill=(255, 110, 110, 255))
    g.point((ox + 12, y + 8), fill=(255, 80, 80, 90))
    for bx in (ox + 15, ox + 18):
        d.rectangle([bx, y + 7, bx + 1, y + 8], fill=pal["metal_d"])
    # Lens barrel with focus rings
    d.rectangle([ox + 28, y + 6, ox + 35, y + 17], fill=OUTLINE)
    for i, col in enumerate((pal["metal_d"], (40, 42, 48, 255), pal["metal"], (40, 42, 48, 255), pal["metal_d"], (30, 32, 38, 255))):
        d.line([ox + 29 + i, y + 7, ox + 29 + i, y + 16], fill=col)
    d.line([ox + 29, y + 7, ox + 34, y + 7], fill=pal["metal_hi"])
    # Glass
    d.ellipse([ox + 33, y + 7, ox + 38, y + 16], fill=OUTLINE)
    d.ellipse([ox + 34, y + 8, ox + 37, y + 15], fill=pal["accent_d"])
    g.ellipse([ox + 32, y + 6, ox + 39, y + 17], fill=_alpha(pal["accent"], 70))
    g.ellipse([ox + 34, y + 8, ox + 37, y + 15], fill=pal["accent"])
    g.ellipse([ox + 35, y + 10, ox + 36, y + 13], fill=pal["accent_l"])
    g.point((ox + 35, y + 9), fill=WHITE); g.point((ox + 35, y + 10), fill=WHITE)


def tv_head(d, g, ox, y, pal):
    headpal = dict(pal, base=pal["head"], light=pal["head_hi"], mid=pal["head_d"], dark=_shade(pal["head_d"], 0.7))
    # Antennas with glowing tips
    for (x0, x1) in ((ox + 16, ox + 11), (ox + 22, ox + 27)):
        d.line([x0, y + 3, x1, y + 0], fill=OUTLINE, width=2)
        d.line([x0, y + 3, x1, y + 0], fill=pal["metal"])
        d.point((x1, y + 0), fill=pal["accent_l"])
        g.point((x1, y + 0), fill=WHITE)
    d.rectangle([ox + 15, y + 2, ox + 23, y + 4], fill=OUTLINE)
    d.rectangle([ox + 16, y + 3, ox + 22, y + 3], fill=pal["metal_d"])
    # Cabinet
    plate(d, (ox + 5, y + 4, ox + 35, y + 20), headpal, radius=3)
    # Screen bezel and curved CRT screen
    d.rounded_rectangle([ox + 8, y + 6, ox + 28, y + 18], radius=3, fill=OUTLINE)
    d.rounded_rectangle([ox + 9, y + 7, ox + 27, y + 17], radius=2, fill=_shade(pal["accent"], 0.35))
    # Glowing screen: gradient, scanlines, hypno spiral
    g.rounded_rectangle([ox + 9, y + 7, ox + 27, y + 17], radius=2, fill=_alpha(pal["accent_d"], 230))
    g.rounded_rectangle([ox + 11, y + 8, ox + 25, y + 16], radius=2, fill=_alpha(pal["accent"], 235))
    g.ellipse([ox + 14, y + 9, ox + 22, y + 15], fill=_alpha(pal["accent_l"], 240))
    # Hypno spiral: an Archimedean spiral squashed to the screen's shape
    cx, cy = ox + 18, y + 12
    t = 0.0
    while t < 4.2 * math.pi:
        r = 0.55 * t
        px, py = round(cx + math.cos(t) * r * 1.35), round(cy + math.sin(t) * r * 0.85)
        if ox + 10 <= px <= ox + 26 and y + 8 <= py <= y + 16:
            g.point((px, py), fill=WHITE)
        t += 0.12
    g.point((cx, cy), fill=WHITE)
    # Faint scanlines on the dark base only, and a glare in the top-left corner
    for sy in range(y + 8, y + 17, 2):
        d.line([ox + 10, sy, ox + 26, sy], fill=_shade(pal["accent"], 0.28))
    g.point((ox + 11, y + 8), fill=(255, 255, 255, 200)); g.point((ox + 12, y + 8), fill=(255, 255, 255, 140))
    g.point((ox + 11, y + 9), fill=(255, 255, 255, 120))
    # Control panel: two knobs and a speaker grille
    d.rectangle([ox + 29, y + 7, ox + 33, y + 18], fill=pal["head_d"])
    for ky in (y + 9, y + 13):
        d.ellipse([ox + 30, ky - 1, ox + 32, ky + 1], fill=pal["metal"])
        d.point((ox + 30, ky - 1), fill=pal["metal_hi"])
    for gy in range(y + 16, y + 18):
        for gx in range(ox + 30, ox + 33, 2):
            d.point((gx, gy), fill=OUTLINE)
    g.point((ox + 31, y + 16), fill=pal["accent_l"])


HEADS = {"TitanSpeaker": speaker_head, "TitanCamera": camera_head, "TitanTV": tv_head}


# ---------------------------------------------------------------- body and legs

def titan_arm(d, g, ox, oy, frame, back, pal):
    shoulder = (ox + (15 if back else 25), oy + 22)
    if frame in ARM_ANGLES:
        a = math.radians(ARM_ANGLES[frame])
        v = (math.cos(a), math.sin(a))
        elbow = (shoulder[0] + round(v[0] * 7), shoulder[1] + round(v[1] * 7))
        hand = (shoulder[0] + round(v[0] * 15), shoulder[1] + round(v[1] * 15))
    else:
        swing = 0
        if 6 <= frame <= 19:
            swing = round(4 * math.sin(walk_phase(frame)) * (-1 if back else 1))
        elif frame == 5:
            swing = -4 if back else 4
        elbow = (shoulder[0] + swing // 2, shoulder[1] + 7)
        hand = (shoulder[0] + swing + 1, shoulder[1] + 14)
    limb(d, shoulder, elbow, 4, pal, back)
    limb(d, elbow, hand, 5, pal, back)
    joint(d, elbow, 1, pal, back)
    if not back:
        # Energy line along the forearm
        g.line([elbow, hand], fill=_alpha(pal["accent"], 170))
    # Gauntlet
    d.rounded_rectangle([hand[0] - 3, hand[1] - 3, hand[0] + 3, hand[1] + 3], radius=1, fill=OUTLINE)
    d.rounded_rectangle([hand[0] - 2, hand[1] - 2, hand[0] + 2, hand[1] + 2], radius=1, fill=pal["mid"] if back else pal["metal"])
    if not back:
        d.line([hand[0] - 2, hand[1] - 2, hand[0] + 1, hand[1] - 2], fill=pal["metal_hi"])
        d.point((hand[0], hand[1] + 1), fill=pal["accent_d"])
        g.point((hand[0], hand[1] + 1), fill=pal["accent_l"])
    # Pauldron (big shoulder armour)
    sx, sy = shoulder
    if back:
        plate(d, (sx - 4, sy - 4, sx + 3, sy + 3), pal, base="mid", radius=2)
    else:
        plate(d, (sx - 5, sy - 5, sx + 5, sy + 3), pal, radius=3)
        d.line([sx - 3, sy + 1, sx + 3, sy + 1], fill=pal["accent_d"])
        g.line([sx - 3, sy + 1, sx + 3, sy + 1], fill=_alpha(pal["accent"], 200))


def titan_body_frame(d, g, ox, oy, frame, name, pal):
    bob = 1 if 6 <= frame <= 19 and abs(math.sin(walk_phase(frame))) > 0.7 else 0
    y = oy + bob + 1
    titan_arm(d, g, ox, y, frame, True, pal)

    # Twin back thrusters
    for tx in (ox + 5, ox + 8):
        plate(d, (tx, y + 20, tx + 3, y + 31), pal, base="mid", radius=1, bevel=False)
        d.rectangle([tx + 1, y + 31, tx + 2, y + 32], fill=pal["metal_d"])
        g.point((tx + 1, y + 32), fill=ORANGE_L); g.point((tx + 2, y + 32), fill=ORANGE)
        g.point((tx + 1, y + 33), fill=_alpha(ORANGE, 120))

    # Abdomen segments, then the chest plate over them
    for i, (x0, x1) in enumerate(((14, 26), (15, 25))):
        plate(d, (ox + x0, y + 27 + i * 3, ox + x1, y + 30 + i * 3), pal, base="mid", radius=1)
    # Belt with glowing buckle
    d.rectangle([ox + 13, y + 32, ox + 27, y + 35], fill=OUTLINE)
    d.rectangle([ox + 14, y + 33, ox + 26, y + 34], fill=pal["dark"])
    d.rectangle([ox + 19, y + 32, ox + 22, y + 35], fill=pal["metal"])
    g.point((ox + 20, y + 33), fill=pal["accent_l"]); g.point((ox + 21, y + 34), fill=pal["accent"])
    # Chest: broad plate tapering to the waist
    d.polygon([(ox + 10, y + 19), (ox + 31, y + 19), (ox + 29, y + 28), (ox + 12, y + 28)], fill=OUTLINE)
    d.polygon([(ox + 11, y + 20), (ox + 30, y + 20), (ox + 28, y + 27), (ox + 13, y + 27)], fill=pal["base"])
    d.line([ox + 11, y + 20, ox + 30, y + 20], fill=pal["hi"])
    d.line([ox + 12, y + 21, ox + 29, y + 21], fill=pal["light"])
    d.line([ox + 13, y + 27, ox + 28, y + 27], fill=pal["mid"])
    # Pectoral plate seams
    d.line([ox + 16, y + 22, ox + 16, y + 26], fill=pal["mid"])
    d.line([ox + 25, y + 22, ox + 26, y + 26], fill=pal["mid"])
    # Collar and neck cables
    d.rectangle([ox + 16, y + 16, ox + 25, y + 20], fill=OUTLINE)
    d.rectangle([ox + 17, y + 17, ox + 24, y + 19], fill=pal["metal_d"])
    d.line([ox + 18, y + 17, ox + 18, y + 19], fill=pal["dark"]); d.line([ox + 23, y + 17, ox + 23, y + 19], fill=pal["dark"])
    # Chest core with energy lines running to the shoulders
    glow_core(d, g, (ox + 21, y + 24), 2, pal)
    g.line([ox + 18, y + 23, ox + 13, y + 21], fill=_alpha(pal["accent"], 150))
    g.line([ox + 24, y + 23, ox + 28, y + 21], fill=_alpha(pal["accent"], 150))

    HEADS[name](d, g, ox, y, pal)
    titan_arm(d, g, ox, y, frame, False, pal)


def titan_leg(d, g, hip, foot, pal, back):
    knee = ((hip[0] + foot[0]) // 2 + 2, (hip[1] + foot[1]) // 2)
    ankle = (foot[0], foot[1] - 4)
    limb(d, hip, knee, 6, pal, back)       # thigh
    limb(d, knee, ankle, 5, pal, back)     # shin
    # Shin guard
    if not back:
        mid = ((knee[0] + ankle[0]) // 2, (knee[1] + ankle[1]) // 2)
        d.line([knee[0] + 2, knee[1] + 2, mid[0] + 2, mid[1] + 1], fill=pal["light"])
        g.line([mid[0] + 1, mid[1] - 1, mid[0] + 1, mid[1] + 1], fill=_alpha(pal["accent"], 160))
    # Knee cap
    plate(d, (knee[0] - 3, knee[1] - 2, knee[0] + 3, knee[1] + 3), pal, base="mid" if back else "base", radius=1)
    # Boot: heel, sole and a pointed toe (facing right)
    fx, fy = foot
    d.polygon([(fx - 4, fy - 4), (fx + 3, fy - 4), (fx + 7, fy - 1), (fx + 7, fy), (fx - 4, fy)], fill=OUTLINE)
    d.polygon([(fx - 3, fy - 3), (fx + 3, fy - 3), (fx + 6, fy - 1), (fx - 3, fy - 1)], fill=pal["mid"] if back else pal["metal"])
    if not back:
        d.line([fx - 3, fy - 3, fx + 2, fy - 3], fill=pal["metal_hi"])
    d.line([fx - 4, fy, fx + 7, fy], fill=pal["dark"])
    g.point((fx - 4, fy - 2), fill=_alpha(ORANGE, 140))


def titan_legs_frame(d, g, ox, oy, frame, pal):
    hip_y = oy + 37
    ground = oy + 51
    back_hip, front_hip = (ox + 17, hip_y), (ox + 24, hip_y)
    if frame == 5:
        back_foot, front_foot = (ox + 12, ground - 4), (ox + 27, ground - 6)
    elif 6 <= frame <= 19:
        s = math.sin(walk_phase(frame))
        front_foot = (ox + 23 + round(6 * s), ground - (2 if s > 0.3 else 0))
        back_foot = (ox + 16 - round(6 * s), ground - (2 if s < -0.3 else 0))
    else:
        back_foot, front_foot = (ox + 15, ground), (ox + 23, ground)
    titan_leg(d, g, back_hip, back_foot, pal, True)
    titan_leg(d, g, front_hip, front_foot, pal, False)
    # Hip armour skirt
    plate(d, (ox + 12, hip_y - 3, ox + 29, hip_y + 2), pal, base="mid", radius=1)
    plate(d, (ox + 21, hip_y - 2, ox + 28, hip_y + 4), pal, radius=1)
    d.line([ox + 14, hip_y, ox + 19, hip_y], fill=pal["dark"])


def titan_sheets():
    for name, pal in TITANS.items():
        body, bd = canvas(TW, TH * FRAMES)
        body_glow, bg = canvas(TW, TH * FRAMES)
        legs, ld = canvas(TW, TH * FRAMES)
        legs_glow, lg = canvas(TW, TH * FRAMES)
        for f in range(FRAMES):
            titan_body_frame(bd, bg, 0, f * TH, f, name, pal)
            titan_legs_frame(ld, lg, 0, f * TH, f, pal)
        save(body, "Content/Players/%sBody.png" % name)
        save(body_glow, "Content/Players/%sBody_Glow.png" % name)
        save(legs, "Content/Players/%sLegs.png" % name)
        save(legs_glow, "Content/Players/%sLegs_Glow.png" % name)

    # Preview: Robot Jack next to the three Titans (idle, aiming forward, aiming up and walking),
    # with the glow layers on top like in game. Saved 2x (4x the pixel-art scale).
    def layered(prefix, f, w, h):
        cell = Image.new("RGBA", (w, h), CLEAR)
        for part in ("Legs", "Legs_Glow", "Body", "Body_Glow"):
            sheet = Image.open("Content/Players/%s%s.png" % (prefix, part))
            cell.alpha_composite(sheet.crop((0, f * h, w, (f + 1) * h)))
        return cell

    frames = (0, 3, 1, 9)
    preview = Image.new("RGBA", (80 + 3 * len(frames) * 80, 112), (28, 32, 46, 255))
    preview.alpha_composite(layered("Robot", 0, 40, 56), (20, 56))
    x = 80
    for name in TITANS:
        for f in frames:
            preview.alpha_composite(layered(name, f, 80, 112), (x, 0))
            x += 80
    save(preview, "tools/titan_preview_4x.png", scale=2)



# ---------------------------------------------------------------- Titan items, abilities and buffs

def titan_core_item(path, head):
    img, d = canvas(14, 16)
    # A glowing core in a metal frame with a tiny version of the Titan's head on top.
    d.ellipse([2, 5, 11, 14], fill=OUTLINE)
    d.ellipse([3, 6, 10, 13], fill=STEEL)
    col = {"speaker": (255, 60, 60, 255), "camera": (60, 170, 255, 255), "tv": (200, 80, 255, 255)}[head]
    d.ellipse([5, 8, 8, 11], fill=col)
    d.point((6, 9), fill=WHITE)
    if head == "speaker":
        d.rectangle([3, 0, 10, 5], fill=OUTLINE); d.ellipse([5, 1, 8, 4], outline=STEEL_L)
    elif head == "camera":
        d.rectangle([3, 1, 9, 5], fill=OUTLINE); d.rectangle([4, 2, 8, 4], fill=STEEL_L); d.rectangle([10, 2, 12, 4], fill=col)
    else:
        d.line([5, 0, 4, -1], fill=STEEL_L); d.line([8, 0, 9, -1], fill=STEEL_L)
        d.rectangle([2, 1, 11, 5], fill=OUTLINE); d.rectangle([4, 2, 9, 4], fill=col)
    save(img, path)


def ability_icon(path, draw):
    img, d = canvas(16, 16)
    draw(d)
    save(img, path)


def titan_icons():
    titan_core_item("Content/Items/TitanSpeakerCore.png", "speaker")
    titan_core_item("Content/Items/TitanCameraCore.png", "camera")
    titan_core_item("Content/Items/TitanTVCore.png", "tv")

    red, blue, purple, pink = (255, 90, 90, 255), (60, 170, 255, 255), (190, 80, 255, 255), (255, 170, 255, 255)

    def sonic(d):
        for r in (3, 6, 9):
            d.arc([7 - r, 8 - r, 7 + r, 8 + r], -50, 50, fill=WHITE if r == 9 else red, width=1)
        d.rectangle([0, 5, 4, 11], fill=OUTLINE); d.ellipse([1, 6, 3, 10], fill=STEEL_L)
    def bass(d):
        d.ellipse([1, 1, 14, 14], outline=red); d.ellipse([4, 4, 11, 11], outline=WHITE)
        d.ellipse([6, 6, 9, 9], fill=OUTLINE)
    def barrage(d):
        for y0 in (2, 7, 12):
            d.arc([4, y0 - 3, 10, y0 + 3], -60, 60, fill=red)
            d.arc([8, y0 - 3, 14, y0 + 3], -60, 60, fill=WHITE)
    def laser(d):
        d.ellipse([0, 5, 5, 10], fill=blue); d.point((2, 7), fill=WHITE)
        d.rectangle([5, 6, 15, 9], fill=(40, 120, 255, 255)); d.line([5, 7, 15, 7], fill=WHITE)
    def flash(d):
        d.polygon([(8, 0), (10, 6), (16, 8), (10, 10), (8, 16), (6, 10), (0, 8), (6, 6)], fill=WHITE)
        d.ellipse([6, 6, 9, 9], fill=blue)
    def lens(d):
        for (x, y) in ((2, 10), (7, 3), (11, 9)):
            d.ellipse([x, y, x + 4, y + 4], fill=blue); d.point((x + 1, y + 1), fill=WHITE)
    def hypno(d):
        d.rectangle([0, 1, 15, 14], fill=OUTLINE); d.rectangle([1, 2, 14, 13], fill=purple)
        d.arc([3, 3, 12, 12], 0, 300, fill=WHITE); d.arc([5, 5, 10, 10], 180, 480, fill=pink)
    def blades(d):
        d.arc([-6, 0, 14, 20], 270, 360, fill=purple, width=3); d.arc([-6, 0, 14, 20], 275, 355, fill=WHITE, width=1)
        d.rectangle([0, 12, 3, 15], fill=STEEL)
    def storm(d):
        for (x, y) in ((1, 2), (8, 1), (11, 8), (4, 10), (9, 12)):
            d.ellipse([x, y, x + 3, y + 3], fill=purple); d.point((x + 1, y + 1), fill=pink)
    for name, fn in (("SonicBoom", sonic), ("BassDrop", bass), ("SpeakerBarrage", barrage),
                     ("CoreLaser", laser), ("CameraFlash", flash), ("LensBurst", lens),
                     ("HypnoScreen", hypno), ("EnergyBlades", blades), ("StaticStorm", storm)):
        ability_icon("Content/Abilities/%s.png" % name, fn)

    def form_buff(path, col):
        def inner(d):
            d.rectangle([3, 3, 12, 10], fill=OUTLINE); d.rectangle([4, 4, 11, 9], fill=col)
            d.rectangle([5, 11, 10, 14], fill=STEEL_D); d.point((7, 12), fill=col)
        buff_icon(path, inner)
    form_buff("Content/Buffs/TitanSpeakerForm.png", red)
    form_buff("Content/Buffs/TitanCameraForm.png", blue)
    form_buff("Content/Buffs/TitanTVForm.png", purple)

    def stunned(d):
        for (x, y) in ((3, 4), (8, 2), (12, 5)):
            d.polygon([(x, y - 2), (x + 1, y), (x + 3, y), (x + 1, y + 1), (x, y + 3), (x - 1, y + 1), (x - 3, y), (x - 1, y)], fill=GOLD)
        d.ellipse([4, 8, 11, 14], fill=STEEL_L)
    buff_icon("Content/Buffs/Stunned.png", stunned)

    for name in ("SoundWave", "SpeakerShockwave", "CoreLaserBeam", "CameraFlashBurst", "EnergyOrb", "BladeSwing"):
        invisible("Content/Projectiles/%s.png" % name)



def titan_more_icons():
    red, blue, purple, pink = (255, 90, 90, 255), (60, 170, 255, 255), (190, 80, 255, 255), (255, 170, 255, 255)

    def soldier(d, ox, oy, frame, kind, col):
        """A small suited soldier with a mini Titan head, facing right. Cell 16x22; feet on row 21."""
        suit, suit_l, suit_d = (40, 42, 50, 255), (70, 74, 86, 255), (24, 25, 30, 255)
        bob = 1 if frame in (1, 2) else 0
        y = oy + bob
        # Legs: 0 idle, 1/2 walk (legs apart, swapped), 3 jump (tucked)
        legs = {0: ((6, 21), (9, 21)), 1: ((4, 21), (11, 21)), 2: ((11, 21), (4, 21)), 3: ((5, 19), (10, 20))}[frame]
        for k, (fx, fy) in enumerate(legs):
            hip = (ox + (7 if k == 0 else 9), y + 15)
            foot = (ox + fx, oy + fy)
            d.line([hip, foot], fill=OUTLINE, width=3)
            d.line([hip, foot], fill=suit_d if k == 0 else suit)
            d.line([foot[0] - 1, foot[1], foot[0] + 2, foot[1]], fill=OUTLINE)
        # Body: suit jacket with a white shirt and a tie in the Titan's colour
        d.rectangle([ox + 5, y + 9, ox + 11, y + 15], fill=OUTLINE)
        d.rectangle([ox + 6, y + 10, ox + 10, y + 14], fill=suit)
        d.line([ox + 6, y + 10, ox + 10, y + 10], fill=suit_l)
        d.line([ox + 8, y + 10, ox + 8, y + 12], fill=WHITE)
        d.point((ox + 8, y + 11), fill=col)
        # Arm, swinging with the walk
        hand = {0: (10, 15), 1: (12, 14), 2: (9, 15), 3: (12, 11)}[frame]
        d.line([ox + 9, y + 10, ox + hand[0], y + hand[1]], fill=OUTLINE, width=2)
        d.point((ox + hand[0], y + hand[1]), fill=STEEL_L)
        # Head
        if kind == "speaker":
            d.rectangle([ox + 4, y + 1, ox + 12, y + 8], fill=OUTLINE)
            d.rectangle([ox + 5, y + 2, ox + 11, y + 7], fill=(34, 34, 40, 255))
            d.ellipse([ox + 6, y + 3, ox + 10, y + 7], fill=STEEL)
            d.ellipse([ox + 7, y + 4, ox + 9, y + 6], fill=(30, 30, 34, 255))
            d.point((ox + 10, y + 2), fill=col)
        elif kind == "camera":
            d.rectangle([ox + 4, y + 2, ox + 11, y + 8], fill=OUTLINE)
            d.rectangle([ox + 5, y + 3, ox + 10, y + 7], fill=(160, 166, 180, 255))
            d.line([ox + 5, y + 3, ox + 10, y + 3], fill=(215, 220, 230, 255))
            d.rectangle([ox + 11, y + 3, ox + 14, y + 7], fill=OUTLINE)
            d.rectangle([ox + 12, y + 4, ox + 13, y + 6], fill=col)
            d.point((ox + 6, y + 4), fill=RED)
        else:
            d.line([ox + 7, y + 1, ox + 5, y - 0], fill=STEEL_L); d.line([ox + 10, y + 1, ox + 12, y - 0], fill=STEEL_L)
            d.rectangle([ox + 3, y + 1, ox + 13, y + 8], fill=OUTLINE)
            d.rectangle([ox + 4, y + 2, ox + 12, y + 7], fill=(70, 58, 82, 255))
            d.rectangle([ox + 5, y + 3, ox + 10, y + 6], fill=col)
            d.point((ox + 7, y + 4), fill=WHITE); d.point((ox + 8, y + 5), fill=(255, 220, 255, 255))
            d.point((ox + 11, y + 4), fill=GOLD)

    # Army soldier sheet: one column per Titan (speaker, camera, TV), four rows (idle, walk 1, walk 2, jump).
    # 16x22 cells -> 32x44 in game, 96x176 in total.
    kinds = ((red, "speaker"), (blue, "camera"), (purple, "tv"))
    img, d = canvas(16 * 3, 22 * 4)
    for c, (col, kind) in enumerate(kinds):
        for f in range(4):
            soldier(d, c * 16, f * 22 + 1, f, kind, col)
    save(img, "Content/Projectiles/ArmySoldier.png")

    grenade, gd = canvas(7, 7)
    gd.ellipse([0, 0, 6, 6], fill=OUTLINE); gd.ellipse([1, 1, 5, 5], fill=STEEL_L)
    gd.ellipse([2, 2, 4, 4], fill=blue); gd.point((2, 2), fill=WHITE)
    save(grenade, "Content/Projectiles/FlashGrenade.png")

    def army_icon(col, kind):
        # Two soldiers: a darker one behind, one in front.
        def f(d):
            cell, cd = canvas(16, 22)
            soldier(cd, 0, 1, 0, kind, col)
            small = cell.resize((11, 15), Image.NEAREST)
            dark = Image.eval(small, lambda v: v)  # copy
            px = dark.load()
            for yy in range(dark.height):
                for xx in range(dark.width):
                    r, g_, b, a = px[xx, yy]
                    px[xx, yy] = (r * 6 // 10, g_ * 6 // 10, b * 6 // 10, a)
            d._image.alpha_composite(dark, (5, 0))
            d._image.alpha_composite(small, (0, 1))
        return f

    def shield(d):
        d.ellipse([0, 0, 15, 15], outline=red); d.ellipse([2, 2, 13, 13], outline=(255, 180, 180, 255))
        d.ellipse([5, 5, 10, 10], fill=OUTLINE); d.ellipse([6, 6, 9, 9], outline=STEEL_L)
    def quake(d):
        d.rectangle([0, 13, 15, 15], fill=(120, 80, 45, 255))
        d.arc([-4, 4, 6, 20], 270, 340, fill=red); d.arc([10, 4, 20, 20], 200, 270, fill=red)
        d.rectangle([6, 3, 9, 12], fill=STEEL_D); d.rectangle([5, 10, 10, 12], fill=STEEL)
    def loop(d):
        for a in range(0, 360, 45):
            r = math.radians(a)
            d.line([8 + math.cos(r) * 3, 8 + math.sin(r) * 3, 8 + math.cos(r) * 7, 8 + math.sin(r) * 7], fill=red if a % 90 else WHITE)
        d.ellipse([6, 6, 9, 9], fill=OUTLINE)
    def boom_dash(d):
        d.polygon([(15, 8), (7, 3), (7, 13)], fill=STEEL_L)
        for x in (1, 4):
            d.arc([x - 3, 3, x + 3, 13], 90, 270, fill=red)
    def target(d):
        d.ellipse([1, 1, 14, 14], outline=red); d.ellipse([5, 5, 10, 10], outline=red)
        d.line([8, 0, 8, 4], fill=red); d.line([8, 11, 8, 15], fill=red); d.line([0, 8, 4, 8], fill=red); d.line([11, 8, 15, 8], fill=red)
    def zoom(d):
        d.rectangle([0, 5, 9, 10], fill=OUTLINE); d.rectangle([1, 6, 8, 9], fill=STEEL)
        d.ellipse([8, 4, 14, 11], fill=OUTLINE); d.ellipse([9, 5, 13, 10], fill=blue); d.point((10, 6), fill=WHITE)
        d.line([14, 7, 15, 7], fill=WHITE)
    def rewind(d):
        d.polygon([(1, 8), (7, 3), (7, 13)], fill=blue); d.polygon([(8, 8), (14, 3), (14, 13)], fill=blue)
        d.point((3, 8), fill=WHITE); d.point((10, 8), fill=WHITE)
    def grenades(d):
        for (x, y) in ((1, 8), (6, 2), (10, 9)):
            d.ellipse([x, y, x + 5, y + 5], fill=OUTLINE); d.ellipse([x + 1, y + 1, x + 4, y + 4], fill=STEEL_L)
            d.point((x + 2, y + 2), fill=blue)
    def surf(d):
        d.rectangle([0, 3, 9, 12], fill=OUTLINE); d.rectangle([1, 4, 8, 11], fill=purple)
        d.line([10, 7, 15, 7], fill=pink); d.polygon([(15, 7), (12, 4), (12, 10)], fill=pink)
    def blade_dash(d):
        d.line([2, 13, 14, 1], fill=purple, width=3); d.line([2, 13, 14, 1], fill=WHITE)
        d.line([0, 9, 4, 9], fill=pink); d.line([1, 12, 4, 12], fill=pink)
    def field(d):
        d.ellipse([0, 0, 15, 15], outline=purple)
        d.line([3, 3, 6, 7, 5, 9, 8, 12], fill=WHITE); d.line([12, 2, 10, 6, 12, 8, 10, 13], fill=pink)
    def broadcast(d):
        d.rectangle([0, 3, 6, 11], fill=OUTLINE); d.rectangle([1, 4, 5, 10], fill=purple)
        d.rectangle([6, 6, 15, 8], fill=(200, 90, 255, 255)); d.line([6, 7, 15, 7], fill=WHITE)

    for name, fn in (("SpeakerArmy", army_icon(red, "speaker")), ("SonicShieldAbility", shield), ("SubwooferQuake", quake),
                     ("FeedbackLoop", loop), ("BoomDash", boom_dash),
                     ("CameraArmy", army_icon(blue, "camera")), ("TargetLock", target), ("ZoomShot", zoom),
                     ("Rewind", rewind), ("FlashGrenades", grenades),
                     ("TVArmy", army_icon(purple, "tv")), ("ChannelSurf", surf), ("BladeDash", blade_dash),
                     ("StaticFieldAbility", field), ("BroadcastBeam", broadcast)):
        ability_icon("Content/Abilities/%s.png" % name, fn)

    def marked(d):
        d.ellipse([2, 2, 13, 13], outline=red); d.line([8, 1, 8, 5], fill=red); d.line([8, 10, 8, 14], fill=red)
        d.line([1, 8, 5, 8], fill=red); d.line([10, 8, 14, 8], fill=red); d.point((8, 8), fill=WHITE)
    buff_icon("Content/Buffs/Marked.png", marked)

    for name in ("SonicShield", "StaticField", "TransformBurst"):
        invisible("Content/Projectiles/%s.png" % name)


if __name__ == "__main__":
    robot_sheets()
    titan_sheets()
    titan_icons()
    titan_more_icons()
    robot_trigger()
    orbital_icon(); plasma_icon(); missiles_icon(); thruster_icon()
    robot_form_buff(); orbital_cooldown_buff()
    plasma_bolt(); homing_missile(); invisible("Content/Projectiles/ThrusterHitbox.png")
    reticle(); satellite(); beam_strip(); radial_glow(); shock_ring()
    mod_icon()
    jetpack_item(); jetpack_wings(); absorber_icon(); spinning_buff(); gravity_arm_icon()
    for name in ("HeadRam", "AbsorbField", "EnergyBlast", "EnergyEcho", "GravityWell"):
        invisible("Content/Projectiles/%s.png" % name)
