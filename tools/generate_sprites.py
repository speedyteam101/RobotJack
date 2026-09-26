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
RJ_HALO = (90, 240, 255, 110)
RJ_GLOW = (90, 240, 255, 255)
RJ_GLOW_D = (30, 150, 200, 255)

# Extra details drawn on top of the body by the variants (None for Robot Jack), and behind it.
RJ_DECOR = None
RJ_DECOR_BACK = None


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
        g.line([mid, hand], fill=RJ_GLOW[:3] + (150,))
    # Fist / arm cannon muzzle
    d.rectangle([hand[0] - 1, hand[1] - 1, hand[0] + 1, hand[1] + 1], fill=OUTLINE)
    d.point(hand, fill=RJ_HI if not back else RJ_D)
    if not back:
        g.point(hand, fill=RJ_HALO)
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
    if RJ_DECOR_BACK is not None:
        RJ_DECOR_BACK(d, g, ox, y, frame)
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
    d.point((ox + 10, y + 13), fill=RJ_GLOW)
    g.rectangle([ox + 8, y + 11, ox + 12, y + 15], fill=RJ_GLOW[:3] + (45,))
    g.point((ox + 9, y + 13), fill=RJ_HALO); g.point((ox + 11, y + 13), fill=RJ_HALO)
    g.point((ox + 10, y + 12), fill=RJ_HALO); g.point((ox + 10, y + 14), fill=RJ_HALO)
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
    d.point((ox + 7, y + 0), fill=RJ_GLOW)
    g.point((ox + 7, y + 0), fill=WHITE)
    # Ear piece
    d.rectangle([ox + 6, y + 5, ox + 7, y + 6], fill=RJ_NAVY_L)
    g.point((ox + 7, y + 5), fill=RJ_HALO)
    # V-shaped visor (facing right): wide on top, narrowing below
    d.rectangle([ox + 9, y + 4, ox + 13, y + 6], fill=RJ_VISOR)
    d.line([ox + 9, y + 4, ox + 13, y + 4], fill=RJ_GLOW_D)
    d.line([ox + 10, y + 5, ox + 13, y + 5], fill=RJ_GLOW)
    d.point((ox + 12, y + 6), fill=RJ_GLOW_D)
    g.line([ox + 9, y + 4, ox + 13, y + 4], fill=RJ_GLOW[:3] + (160,))
    g.line([ox + 10, y + 5, ox + 13, y + 5], fill=WHITE)
    g.point((ox + 12, y + 6), fill=RJ_GLOW)
    g.point((ox + 14, y + 5), fill=RJ_GLOW[:3] + (90,))                 # glow spilling out of the visor
    # Jaw plate
    d.line([ox + 9, y + 7, ox + 12, y + 7], fill=RJ_MID)
    rj_scarf_knot(d, ox, y)
    if RJ_DECOR is not None:
        RJ_DECOR(d, g, ox, y, frame)

    rj_arm(d, g, ox, y, frame, back=False)


def draw_leg(d, g, hip, foot, back):
    knee = ((hip[0] + foot[0]) // 2 + 1, (hip[1] + foot[1]) // 2)
    base = RJ_MID if back else RJ
    thick_line(d, hip, knee, OUTLINE, 4)
    thick_line(d, knee, foot, OUTLINE, 4)
    thick_line(d, hip, knee, RJ_NAVY if back else RJ_NAVY_L, 2)        # navy thigh
    thick_line(d, knee, foot, base, 2)                                 # white shin guard
    if not back:
        g.point(((knee[0] + foot[0]) // 2, (knee[1] + foot[1]) // 2), fill=RJ_GLOW[:3] + (130,))
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



# ---------------------------------------------------------------- shared icons

def ability_icon(path, draw):
    img, d = canvas(16, 16)
    draw(d)
    save(img, path)


def stunned_buff():
    def stunned(d):
        for (x, y) in ((3, 4), (8, 2), (12, 5)):
            d.polygon([(x, y - 2), (x + 1, y), (x + 3, y), (x + 1, y + 1), (x, y + 3), (x - 1, y + 1), (x - 3, y), (x - 1, y)], fill=GOLD)
        d.ellipse([4, 8, 11, 14], fill=STEEL_L)
    buff_icon("Content/Buffs/Stunned.png", stunned)


# ---------------------------------------------------------------- Robot Jack variants
# Same body and animation as Robot Jack, recoloured, with a signature detail each.

def _decor_flame(d, g, ox, y, frame):
    # Flame crest flickering off the top of the helmet.
    flicker = frame % 3 == 0
    for i, x in enumerate((8, 10, 12)):
        top = y + (0 if i == 1 else (0 if flicker and i == 0 else 1))  # never above the frame's top row
        d.line([ox + x, top, ox + x, y + 2], fill=(255, 140, 40, 255))
        g.line([ox + x, top, ox + x, y + 2], fill=(255, 200, 80, 220))
        g.point((ox + x, top), fill=(255, 250, 200, 255))


def _decor_ice(d, g, ox, y, frame):
    # Ice crystal spikes on the helmet and the front shoulder.
    for (x0, y0, x1, y1) in ((8, 2, 7, 0), (11, 2, 12, 0), (12, 10, 14, 8)):
        d.line([ox + x0, y + y0, ox + x1, y + y1], fill=(235, 250, 255, 255))
        g.point((ox + x1, y + y1), fill=(200, 245, 255, 200))


def _decor_bolt(d, g, ox, y, frame):
    # Zig-zag lightning-bolt antenna.
    pts = [(ox + 7, y + 2), (ox + 8, y + 1), (ox + 7, y + 1), (ox + 8, y + 0)]
    d.line(pts, fill=(255, 240, 90, 255))
    g.line(pts, fill=(255, 255, 200, 255))


def _decor_horns(d, g, ox, y, frame):
    # Two small curved horns.
    for (x0, x1) in ((7, 6), (12, 13)):
        d.line([ox + x0, y + 2, ox + x1, y + 0], fill=OUTLINE, width=2)
        d.point((ox + x1, y + 0), fill=(200, 120, 255, 255))
        g.point((ox + x1, y + 0), fill=(220, 150, 255, 200))


def _decor_halo(d, g, ox, y, frame):
    # A glowing halo floating above the helmet and a star on the chest.
    g.ellipse([ox + 7, y + 0, ox + 13, y + 2], outline=(255, 235, 150, 255))
    g.point((ox + 10, y + 13), fill=WHITE)
    for (dx, dy) in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        g.point((ox + 10 + dx, y + 13 + dy), fill=(255, 200, 240, 230))


JACK_VARIANTS = {
    "BlazeJack": dict(RJ_HI=(255, 214, 170, 255), RJ=(228, 112, 52, 255), RJ_MID=(170, 70, 38, 255), RJ_D=(110, 40, 28, 255),
                      RJ_NAVY=(62, 28, 26, 255), RJ_NAVY_L=(118, 50, 38, 255), RJ_VISOR=(40, 14, 10, 255),
                      RJ_GLOW=(255, 175, 60, 255), RJ_GLOW_D=(200, 90, 20, 255), RJ_HALO=(255, 175, 60, 110),
                      SCARF=(255, 200, 50, 255), SCARF_L=(255, 240, 150, 255), SCARF_D=(190, 120, 20, 255), RJ_DECOR=_decor_flame),
    "FrostJack": dict(RJ_HI=(246, 252, 255, 255), RJ=(192, 226, 246, 255), RJ_MID=(130, 180, 215, 255), RJ_D=(80, 120, 165, 255),
                      RJ_NAVY=(40, 80, 130, 255), RJ_NAVY_L=(80, 140, 200, 255), RJ_VISOR=(14, 30, 50, 255),
                      RJ_GLOW=(150, 232, 255, 255), RJ_GLOW_D=(70, 160, 220, 255), RJ_HALO=(150, 232, 255, 110),
                      SCARF=(120, 200, 255, 255), SCARF_L=(210, 240, 255, 255), SCARF_D=(60, 120, 190, 255), RJ_DECOR=_decor_ice),
    "VoltJack": dict(RJ_HI=(255, 250, 205, 255), RJ=(240, 208, 60, 255), RJ_MID=(190, 150, 30, 255), RJ_D=(120, 90, 20, 255),
                     RJ_NAVY=(32, 32, 38, 255), RJ_NAVY_L=(72, 72, 84, 255), RJ_VISOR=(20, 20, 10, 255),
                     RJ_GLOW=(255, 240, 110, 255), RJ_GLOW_D=(210, 170, 30, 255), RJ_HALO=(255, 240, 110, 110),
                     SCARF=(60, 150, 255, 255), SCARF_L=(150, 210, 255, 255), SCARF_D=(30, 80, 170, 255), RJ_DECOR=_decor_bolt),
    "ShadowJack": dict(RJ_HI=(150, 128, 180, 255), RJ=(72, 56, 98, 255), RJ_MID=(50, 40, 68, 255), RJ_D=(32, 25, 44, 255),
                       RJ_NAVY=(22, 16, 32, 255), RJ_NAVY_L=(64, 32, 96, 255), RJ_VISOR=(10, 5, 16, 255),
                       RJ_GLOW=(195, 95, 255, 255), RJ_GLOW_D=(120, 40, 190, 255), RJ_HALO=(195, 95, 255, 110),
                       SCARF=(110, 30, 150, 255), SCARF_L=(170, 80, 210, 255), SCARF_D=(55, 12, 80, 255), RJ_DECOR=_decor_horns),
    "NovaJack": dict(RJ_HI=(255, 255, 238, 255), RJ=(250, 226, 145, 255), RJ_MID=(215, 170, 80, 255), RJ_D=(150, 110, 50, 255),
                     RJ_NAVY=(90, 40, 112, 255), RJ_NAVY_L=(150, 82, 172, 255), RJ_VISOR=(30, 15, 40, 255),
                     RJ_GLOW=(255, 150, 225, 255), RJ_GLOW_D=(200, 80, 170, 255), RJ_HALO=(255, 150, 225, 110),
                     SCARF=(255, 120, 200, 255), SCARF_L=(255, 200, 235, 255), SCARF_D=(180, 60, 140, 255), RJ_DECOR=_decor_halo),
}

ELEMENT_COLORS = {  # main, core, dark (matches Elements.cs)
    "Blaze": ((255, 120, 35, 255), (255, 230, 150, 255), (150, 55, 15, 255)),
    "Frost": ((120, 215, 255, 255), (235, 250, 255, 255), (50, 110, 170, 255)),
    "Volt": ((255, 230, 70, 255), (255, 255, 220, 255), (150, 120, 20, 255)),
    "Shadow": ((155, 60, 230, 255), (230, 180, 255, 255), (60, 20, 100, 255)),
    "Nova": ((255, 130, 215, 255), (255, 245, 200, 255), (150, 60, 130, 255)),
}


def robot_variant_sheets():
    base = {k: globals()[k] for k in list(JACK_VARIANTS["BlazeJack"]) + ["RJ_DECOR_BACK"]}
    preview = Image.new("RGBA", (FRAME_W * 3 * (len(JACK_VARIANTS) + 3), FRAME_H), (28, 32, 46, 255))
    x = 0
    for name, pal in [("Robot", base)] + list(JACK_VARIANTS.items()) + [("OmegaJack", OMEGA_PALETTE), ("GodJack", GOD_PALETTE)]:
        globals().update(base)  # start from Robot Jack's look so nothing leaks between palettes
        globals().update(pal)
        body, bd = canvas(FRAME_W, FRAME_H * FRAMES)
        body_glow, bg = canvas(FRAME_W, FRAME_H * FRAMES)
        legs, ld = canvas(FRAME_W, FRAME_H * FRAMES)
        legs_glow, lg = canvas(FRAME_W, FRAME_H * FRAMES)
        for f in range(FRAMES):
            draw_body_frame(bd, bg, 0, f * FRAME_H, f)
            draw_legs_frame(ld, lg, 0, f * FRAME_H, f)
        if name != "Robot":
            save(body, "Content/Players/%sBody.png" % name)
            save(body_glow, "Content/Players/%sBody_Glow.png" % name)
            save(legs, "Content/Players/%sLegs.png" % name)
            save(legs_glow, "Content/Players/%sLegs_Glow.png" % name)
        for f in (0, 3, 9):
            for sheet in (legs, legs_glow, body, body_glow):
                preview.alpha_composite(sheet.crop((0, f * FRAME_H, FRAME_W, (f + 1) * FRAME_H)), (x, 0))
            x += FRAME_W
    globals().update(base)
    save(preview, "tools/jack_variants_preview_6x.png", scale=6)


def jack_variant_icons():
    for element, (main, core, dark) in ELEMENT_COLORS.items():
        # Transform item: the Robot Trigger shape in the element's colours.
        img, d = canvas(14, 16)
        d.rounded_rectangle([3, 5, 10, 15], radius=2, fill=OUTLINE)
        d.rounded_rectangle([4, 6, 9, 14], radius=1, fill=dark)
        d.line([4, 6, 4, 14], fill=main)
        d.rectangle([5, 0, 8, 5], fill=OUTLINE)
        d.rectangle([6, 1, 7, 4], fill=main)
        d.point((6, 1), fill=core)
        d.rectangle([5, 8, 8, 10], fill=OUTLINE)
        d.point((6, 9), fill=core); d.point((7, 9), fill=main)
        save(img, "Content/Items/%sTrigger.png" % element)

        # Form buff: a small robot head with the element's visor.
        def form_inner(d, main=main, core=core, dark=dark):
            d.rounded_rectangle([3, 3, 12, 11], radius=2, fill=dark)
            d.rectangle([6, 6, 12, 7], fill=main); d.line([7, 6, 11, 6], fill=core)
            d.rectangle([5, 12, 10, 14], fill=STEEL_D)
        buff_icon("Content/Buffs/%sJackForm.png" % element, form_inner)

    # Power-up buffs.
    def power(path, element, symbol):
        main, core, dark = ELEMENT_COLORS[element]
        def inner(d):
            d.ellipse([2, 2, 13, 13], fill=dark)
            symbol(d, main, core)
        buff_icon(path, inner)
    power("Content/Buffs/Overheat.png", "Blaze", lambda d, m, c: (d.polygon([(7, 3), (10, 8), (8, 12), (5, 12), (4, 8)], fill=m), d.point((7, 9), fill=c)))
    power("Content/Buffs/CryoArmor.png", "Frost", lambda d, m, c: (d.polygon([(4, 4), (11, 4), (11, 9), (7, 12), (4, 9)], fill=m), d.line([7, 5, 7, 10], fill=c)))
    power("Content/Buffs/Overcharge.png", "Volt", lambda d, m, c: d.line([(9, 3), (6, 8), (9, 8), (6, 13)], fill=c, width=2))
    power("Content/Buffs/VampiricShroud.png", "Shadow", lambda d, m, c: (d.polygon([(7, 4), (10, 8), (7, 12), (4, 8)], fill=m), d.point((7, 8), fill=RED)))
    power("Content/Buffs/CelestialBlessing.png", "Nova", lambda d, m, c: (d.polygon([(7, 3), (8, 6), (11, 7), (8, 8), (7, 11), (6, 8), (3, 7), (6, 6)], fill=c), d.point((7, 7), fill=m)))

    # Ability icons: one symbol per ability template, drawn in the element's colours.
    def bolt(d, m, c, k):
        d.line([1, 14, 9, 6], fill=k, width=2); d.ellipse([8, 3, 14, 9], fill=m); d.ellipse([10, 5, 12, 7], fill=c)
    def beam(d, m, c, k):
        d.rectangle([0, 5, 5, 11], fill=OUTLINE); d.rectangle([1, 6, 4, 10], fill=STEEL)
        d.rectangle([5, 6, 15, 10], fill=m); d.line([5, 8, 15, 8], fill=c)
    def eruption(d, m, c, k):
        d.rectangle([0, 13, 15, 15], fill=(120, 80, 45, 255))
        for x in (2, 7, 12):
            d.rectangle([x - 1, 3 + (x % 3), x + 1, 12], fill=m); d.line([x, 3 + (x % 3), x, 12], fill=c)
    def rain(d, m, c, k):
        for (x, y) in ((3, 1), (8, 4), (12, 0), (5, 9)):
            d.line([x, y, x + 3, y + 5], fill=m, width=2); d.point((x + 3, y + 5), fill=c)
    def blast(d, m, c, k):
        d.ellipse([0, 0, 15, 15], outline=m); d.ellipse([3, 3, 12, 12], outline=c); d.ellipse([6, 6, 9, 9], fill=c)
        for (x, y) in ((1, 7), (14, 8), (7, 1), (8, 14)):
            d.point((x, y), fill=c)
    def dash(d, m, c, k):
        d.polygon([(15, 8), (8, 3), (8, 13)], fill=c)
        for y in (5, 8, 11):
            d.line([0, y, 7, y], fill=m)
    def aura(d, m, c, k):
        d.ellipse([0, 0, 15, 15], outline=m); d.ellipse([5, 4, 10, 12], fill=STEEL)
        for (x, y) in ((3, 4), (12, 5), (2, 11), (12, 12)):
            d.point((x, y), fill=c)
    def orbit(d, m, c, k):
        d.ellipse([1, 1, 14, 14], outline=k); d.ellipse([6, 6, 9, 9], fill=STEEL)
        for (x, y) in ((6, 0), (12, 6), (6, 12), (0, 6)):
            d.ellipse([x, y, x + 3, y + 3], fill=m); d.point((x + 1, y + 1), fill=c)
    def power_icon(d, m, c, k):
        d.ellipse([1, 1, 14, 14], fill=k)
        d.polygon([(7, 2), (12, 8), (9, 8), (9, 13), (6, 13), (6, 8), (3, 8)], fill=m); d.line([7, 4, 7, 12], fill=c)
    symbols = {"Bolt": bolt, "Beam": beam, "Eruption": eruption, "Rain": rain, "Blast": blast, "Dash": dash, "Aura": aura, "Orbit": orbit, "Power": power_icon}
    import json
    spec = json.load(open("tools/jack_abilities.json"))
    ability_colors = dict(ELEMENT_COLORS, Holy=((255, 215, 90, 255), (255, 252, 230, 255), (170, 125, 30, 255)))
    for name, (element, kind, _tip) in spec.items():
        main, core, dark = ability_colors[element]
        ability_icon("Content/Abilities/%s.png" % name, lambda d, f=symbols[kind], m=main, c=core, k=dark: f(d, m, c, k))

    for name in ("ElementBolt", "ElementBlast", "ElementPillar", "ElementAura", "ElementOrbiter"):
        invisible("Content/Projectiles/%s.png" % name)



def _decor_omega(d, g, ox, y, frame):
    # Every Robot Jack combined: Shadow's horns, Nova's halo and chest star, Frost's shoulder crystal,
    # and the chest core ringed in all five element colours.
    _decor_horns(d, g, ox, y, frame)
    _decor_halo(d, g, ox, y, frame)
    d.line([ox + 12, y + 10, ox + 14, y + 8], fill=(235, 250, 255, 255))
    g.point((ox + 14, y + 8), fill=(200, 245, 255, 220))
    for (dx, dy), col in zip(((-1, -1), (1, -1), (1, 1), (-1, 1), (0, -2)),
                              [c[0] for c in ELEMENT_COLORS.values()]):
        g.point((ox + 10 + dx, y + 13 + dy), fill=col[:3] + (230,))


OMEGA_PALETTE = dict(RJ_HI=(252, 220, 125, 255), RJ=(72, 68, 88, 255), RJ_MID=(50, 47, 64, 255), RJ_D=(30, 28, 40, 255),
                     RJ_NAVY=(190, 150, 55, 255), RJ_NAVY_L=(250, 215, 120, 255), RJ_VISOR=(8, 8, 14, 255),
                     RJ_GLOW=(255, 255, 255, 255), RJ_GLOW_D=(200, 200, 230, 255), RJ_HALO=(255, 255, 255, 110),
                     SCARF=(245, 245, 255, 255), SCARF_L=(255, 255, 255, 255), SCARF_D=(200, 170, 90, 255), RJ_DECOR=_decor_omega)


def omega_icons():
    rainbow = [c[0] for c in ELEMENT_COLORS.values()]
    gold, white = (252, 220, 125, 255), WHITE

    # Omega Trigger: a black and gold trigger with a rainbow button.
    img, d = canvas(14, 16)
    d.rounded_rectangle([3, 5, 10, 15], radius=2, fill=OUTLINE)
    d.rounded_rectangle([4, 6, 9, 14], radius=1, fill=(50, 47, 64, 255))
    d.line([4, 6, 4, 14], fill=gold)
    d.rectangle([4, 0, 9, 5], fill=OUTLINE)
    for i, col in enumerate(rainbow):
        d.point((5 + i % 4, 1 + i // 4 * 2), fill=col)
    d.rectangle([6, 2, 7, 3], fill=white)
    d.rectangle([5, 8, 8, 11], fill=OUTLINE)
    for i, col in enumerate(rainbow[:4]):
        d.point((5 + i % 2 * 3, 8 + i // 2 * 3), fill=col)
    d.rectangle([6, 9, 7, 10], fill=white)
    save(img, "Content/Items/OmegaTrigger.png")

    def form_inner(d):
        d.rounded_rectangle([3, 3, 12, 11], radius=2, fill=(50, 47, 64, 255))
        d.rectangle([6, 6, 12, 7], fill=white)
        for i, col in enumerate(rainbow):
            d.point((3 + i * 2, 2), fill=col)
        d.rectangle([5, 12, 10, 14], fill=gold)
    buff_icon("Content/Buffs/OmegaJackForm.png", form_inner)

    def ring(d):
        for i, col in enumerate(rainbow):
            d.arc([0, 0, 15, 15], i * 72, i * 72 + 72, fill=col)

    def cannon(d):
        d.rectangle([0, 4, 5, 12], fill=OUTLINE); d.rectangle([1, 5, 4, 11], fill=gold)
        for i, col in enumerate(rainbow):
            d.line([5, 4 + i * 2, 15, 4 + i * 2], fill=col)
        d.line([5, 8, 15, 8], fill=white)
    def prism(d):
        for i in range(10):
            a = math.radians(i * 36)
            col = rainbow[i % 5]
            d.line([8 + math.cos(a) * 3, 8 + math.sin(a) * 3, 8 + math.cos(a) * 7, 8 + math.sin(a) * 7], fill=col)
        d.ellipse([6, 6, 9, 9], fill=white)
    def cataclysm(d):
        d.rectangle([0, 13, 15, 15], fill=(120, 80, 45, 255))
        for i, col in enumerate(rainbow):
            x = 1 + i * 3
            d.rectangle([x, 2 + (i % 2) * 3, x + 1, 12], fill=col)
    def barrage(d):
        for i, col in enumerate(rainbow):
            x = 1 + i * 3
            d.line([x, 0, x, 13], fill=col); d.point((x, 14), fill=white)
        d.rectangle([4, 0, 11, 2], fill=STEEL)
    def singularity(d):
        ring(d)
        d.ellipse([3, 3, 12, 12], outline=(200, 120, 255, 255)); d.ellipse([5, 5, 10, 10], fill=OUTLINE)
    def timestop(d):
        d.ellipse([1, 1, 14, 14], fill=OUTLINE); d.ellipse([2, 2, 13, 13], fill=(230, 235, 255, 255))
        d.line([7, 7, 7, 3], fill=OUTLINE); d.line([7, 7, 10, 9], fill=OUTLINE)
        ring(d)
    def dash(d):
        d.polygon([(15, 8), (8, 2), (8, 14)], fill=white)
        for i, col in enumerate(rainbow):
            d.line([0, 4 + i * 2, 7, 4 + i * 2], fill=col)
    def ascension(d):
        ring(d)
        d.polygon([(7, 1), (12, 7), (9, 7), (9, 13), (6, 13), (6, 7), (3, 7)], fill=gold); d.line([7, 3, 7, 12], fill=white)
    for name, fn in (("OmegaCannon", cannon), ("PrismStorm", prism), ("ElementalCataclysm", cataclysm), ("OmegaBarrage", barrage),
                     ("SingularityAbility", singularity), ("TimeStop", timestop), ("OmegaDash", dash), ("Ascension", ascension)):
        ability_icon("Content/Abilities/%s.png" % name, fn)
    invisible("Content/Projectiles/Singularity.png")



# ---------------------------------------------------------------- God Jack and the heavenly gate

def _decor_god_wings(d, g, ox, y, frame):
    # Small angel wings folded out behind the back, flapping gently with the walk.
    flap = 0
    if 6 <= frame <= 19:
        flap = round(math.sin(walk_phase(frame) * 2))
    elif frame == 5:
        flap = -2
    feathers = [((6, 11), (1, 6 + flap)), ((6, 12), (0, 9 + flap)), ((6, 13), (1, 12 + flap)), ((6, 14), (2, 15 + flap))]
    for base, tip in feathers:
        tip = (max(0, tip[0]), max(0, tip[1]))
        d.line([ox + base[0], y + base[1], ox + tip[0], y + tip[1]], fill=OUTLINE, width=3)
    for base, tip in feathers:
        tip = (max(0, tip[0]), max(0, tip[1]))
        d.line([ox + base[0], y + base[1], ox + tip[0], y + tip[1]], fill=(250, 250, 255, 255))
        d.point((ox + tip[0], y + tip[1]), fill=(255, 215, 90, 255))
        g.point((ox + tip[0], y + tip[1]), fill=(255, 235, 150, 200))


def _decor_god(d, g, ox, y, frame):
    # A large blazing halo above the helmet and a golden sun on the chest.
    g.ellipse([ox + 6, y + 0, ox + 14, y + 2], outline=(255, 225, 120, 255))
    g.point((ox + 10, y + 0), fill=WHITE)
    d.point((ox + 10, y + 13), fill=(255, 215, 90, 255))
    g.point((ox + 10, y + 13), fill=WHITE)
    for (dx, dy) in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        g.point((ox + 10 + dx, y + 13 + dy), fill=(255, 215, 90, 230))


GOD_PALETTE = dict(RJ_HI=(255, 255, 255, 255), RJ=(236, 236, 244, 255), RJ_MID=(190, 190, 210, 255), RJ_D=(130, 130, 155, 255),
                   RJ_NAVY=(205, 160, 50, 255), RJ_NAVY_L=(255, 215, 90, 255), RJ_VISOR=(40, 30, 10, 255),
                   RJ_GLOW=(255, 225, 120, 255), RJ_GLOW_D=(220, 170, 50, 255), RJ_HALO=(255, 225, 120, 110),
                   SCARF=(255, 215, 90, 255), SCARF_L=(255, 245, 190, 255), SCARF_D=(190, 140, 40, 255),
                   RJ_DECOR=_decor_god, RJ_DECOR_BACK=_decor_god_wings)

MARBLE, MARBLE_D, MARBLE_DD = (244, 242, 236, 255), (205, 200, 190, 255), (160, 152, 140, 255)
GATE_GOLD, GATE_GOLD_D, GATE_GOLD_L = (240, 195, 70, 255), (185, 135, 35, 255), (255, 235, 150, 255)


def heavenly_gate():
    # Frame: 48x72 (96x144 in game). Opening x 8..39, rows 16..67 is left empty for the doors.
    img, d = canvas(48, 72)
    # Tympanum (the filled space under the arch) with a sun emblem.
    d.pieslice([6, 1, 41, 30], 180, 360, fill=GATE_GOLD_D)
    d.pieslice([9, 4, 38, 27], 180, 360, fill=(255, 225, 140, 255))
    cx, cy = 24, 14
    for a in range(0, 180, 20):
        r = math.radians(180 + a)
        d.line([cx + math.cos(r) * 3, cy + math.sin(r) * 3, cx + math.cos(r) * 9, cy + math.sin(r) * 9], fill=GATE_GOLD)
    d.pieslice([cx - 3, cy - 3, cx + 3, cy + 3], 180, 360, fill=WHITE)
    # Arch: gold band over the top.
    d.arc([2, 0, 45, 32], 180, 360, fill=OUTLINE, width=4)
    d.arc([3, 1, 44, 31], 180, 360, fill=GATE_GOLD, width=2)
    d.arc([3, 1, 44, 31], 200, 340, fill=GATE_GOLD_L, width=1)
    d.rectangle([22, 0, 25, 3], fill=OUTLINE); d.rectangle([23, 0, 24, 2], fill=(150, 230, 255, 255))  # keystone gem
    # Lintel across the top of the doorway.
    d.rectangle([4, 14, 43, 16], fill=OUTLINE); d.line([5, 15, 42, 15], fill=GATE_GOLD)
    # Pillars: fluted marble with gold capitals and bases.
    for x0 in (0, 39):
        d.rectangle([x0, 12, x0 + 8, 64], fill=OUTLINE)
        d.rectangle([x0 + 1, 17, x0 + 7, 60], fill=MARBLE)
        for fx in (x0 + 3, x0 + 5):
            d.line([fx, 18, fx, 59], fill=MARBLE_D)
        d.line([x0 + 7, 17, x0 + 7, 60], fill=MARBLE_DD)
        d.rectangle([x0 + 1, 13, x0 + 7, 16], fill=GATE_GOLD); d.line([x0 + 1, 13, x0 + 7, 13], fill=GATE_GOLD_L)
        d.rectangle([x0 + 1, 60, x0 + 7, 63], fill=GATE_GOLD); d.line([x0 + 1, 63, x0 + 7, 63], fill=GATE_GOLD_D)
    # Marble steps.
    d.rectangle([0, 64, 47, 69], fill=OUTLINE)
    d.rectangle([1, 65, 46, 66], fill=MARBLE); d.rectangle([2, 67, 45, 68], fill=MARBLE_D)
    # Clouds billowing around the base.
    for (x, y, r) in ((2, 68, 4), (9, 69, 3), (16, 70, 3), (24, 70, 3), (32, 70, 3), (39, 69, 3), (45, 68, 4)):
        d.ellipse([x - r, y - r, x + r, min(71, y + r)], fill=(250, 252, 255, 235))
        d.point((x - 1, y - r + 1), fill=WHITE)
    # Leave the doorway itself empty.
    px = img.load()
    for yy in range(17, 64):
        for xx in range(9, 39):
            px[xx, yy] = CLEAR
    save(img, "Content/Projectiles/HeavenlyGate.png")

    # One door leaf: 16x52 (32x104 in game), hinge on the left. The right door is the same leaf mirrored.
    img, d = canvas(16, 52)
    d.rectangle([0, 0, 15, 51], fill=OUTLINE)
    d.rectangle([1, 1, 14, 50], fill=(248, 244, 232, 255))
    d.rectangle([1, 1, 14, 2], fill=GATE_GOLD); d.rectangle([1, 49, 14, 50], fill=GATE_GOLD)
    for (y0, y1) in ((5, 22), (27, 46)):
        d.rectangle([3, y0, 12, y1], outline=GATE_GOLD)
        d.rectangle([4, y0 + 1, 11, y1 - 1], fill=(238, 232, 215, 255))
    # Star ornament and a gold handle on the inner edge.
    d.line([7, 9, 7, 18], fill=GATE_GOLD); d.line([4, 13, 10, 13], fill=GATE_GOLD)
    d.point((7, 13), fill=WHITE)
    d.rectangle([13, 24, 14, 27], fill=GATE_GOLD_D); d.point((13, 25), fill=GATE_GOLD_L)
    save(img, "Content/Projectiles/HeavenlyGateDoor.png")


def god_icons():
    gold, white = GATE_GOLD, WHITE
    # God Trigger: a white and gold trigger with a halo and little wings.
    img, d = canvas(16, 16)
    d.line([1, 7, 4, 9], fill=white, width=2); d.line([14, 7, 11, 9], fill=white, width=2)
    d.rounded_rectangle([4, 6, 11, 15], radius=2, fill=OUTLINE)
    d.rounded_rectangle([5, 7, 10, 14], radius=1, fill=(236, 236, 244, 255))
    d.line([5, 7, 5, 14], fill=gold)
    d.rectangle([6, 2, 9, 6], fill=OUTLINE); d.rectangle([7, 3, 8, 5], fill=gold)
    d.ellipse([4, 0, 11, 2], outline=(255, 225, 120, 255))
    d.rectangle([6, 9, 9, 11], fill=gold); d.point((7, 10), fill=white)
    save(img, "Content/Items/GodTrigger.png")

    def form_inner(d):
        d.ellipse([3, 0, 12, 3], outline=(255, 225, 120, 255))
        d.rounded_rectangle([3, 4, 12, 11], radius=2, fill=(236, 236, 244, 255))
        d.rectangle([6, 7, 12, 8], fill=gold)
        d.rectangle([5, 12, 10, 14], fill=gold)
    buff_icon("Content/Buffs/GodJackForm.png", form_inner)

    def wrath(d):
        d.ellipse([0, 0, 15, 15], outline=gold)
        for a in range(0, 360, 45):
            r = math.radians(a)
            d.line([8 + math.cos(r) * 4, 8 + math.sin(r) * 4, 8 + math.cos(r) * 6, 8 + math.sin(r) * 6], fill=(255, 150, 40, 255))
        d.ellipse([4, 4, 11, 11], fill=gold); d.ellipse([6, 6, 9, 9], fill=white)
    ability_icon("Content/Abilities/GodsWrathAbility.png", wrath)

    def shield(d):
        d.polygon([(8, 1), (14, 4), (13, 10), (8, 14), (3, 10), (2, 4)], fill=gold)
        d.polygon([(8, 3), (12, 5), (11, 9), (8, 12), (5, 9), (4, 5)], fill=(255, 252, 230, 255))
        d.line([8, 4, 8, 11], fill=gold); d.line([5, 7, 11, 7], fill=gold)
    buff_icon("Content/Buffs/DivineShield.png", shield)

    for name in ("GodsWrathCharge", "GodsWrathBlast"):
        invisible("Content/Projectiles/%s.png" % name)


if __name__ == "__main__":
    robot_sheets()
    robot_variant_sheets()
    jack_variant_icons()
    stunned_buff()
    omega_icons()
    heavenly_gate()
    god_icons()
    robot_trigger()
    orbital_icon(); plasma_icon(); missiles_icon(); thruster_icon()
    robot_form_buff(); orbital_cooldown_buff()
    plasma_bolt(); homing_missile(); invisible("Content/Projectiles/ThrusterHitbox.png")
    reticle(); satellite(); beam_strip(); radial_glow(); shock_ring()
    mod_icon()
    jetpack_item(); jetpack_wings(); absorber_icon(); spinning_buff(); gravity_arm_icon()
    for name in ("HeadRam", "AbsorbField", "EnergyBlast", "EnergyEcho", "GravityWell", "ElementBeam", "TransformBurst"):
        invisible("Content/Projectiles/%s.png" % name)
