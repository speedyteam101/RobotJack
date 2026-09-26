"""Generates the sprites for Akutoku-ō, his Robitacons, the Robo-Mechanic, and their items.

Run from the RobotJack folder:  python3 tools/generate_akutoku_sprites.py   (needs Pillow)
Also writes tools/akutoku_preview_3x.png.

Akutoku-ō is drawn by Common/Akutoku/SpiderRig.cs from the parts in Content/NPCs/Akutoku (1 texture pixel =
one "art pixel"; the boss is drawn 3x, Spider Jack 0.55x). Each body part, leg segment and weapon has a _Glow
layer in white and greys: it's tinted in game with the infection colour (purple Corruption / red Crimson).
The Robitacon sheets are built from the Robot Jack form sheets in Content/Players, darkened and scarred, so
re-run this after changing those.
"""
import math
import os
import random
from PIL import Image, ImageDraw, ImageChops, ImageFilter, ImageEnhance

import generate_sprites as gs

NPC = "Content/NPCs/Akutoku/"
CLEAR = (0, 0, 0, 0)
OUT = (16, 16, 22, 255)
DARK = (40, 42, 52, 255)
METAL = (70, 74, 88, 255)
PLATE = (98, 103, 120, 255)
HI = (146, 152, 172, 255)
RUST = (92, 60, 58, 255)
WHITE = (255, 255, 255, 255)
GLOW_MID = (200, 200, 200, 255)
GLOW_DIM = (130, 130, 130, 255)


def img(w, h):
    i = Image.new("RGBA", (w, h), CLEAR)
    return i, ImageDraw.Draw(i)


def outline(im):
    a = im.getchannel("A").point(lambda v: 255 if v > 0 else 0)
    ring = ImageChops.subtract(a.filter(ImageFilter.MaxFilter(3)), a)
    out = Image.new("RGBA", im.size, CLEAR)
    out.paste(Image.new("RGBA", im.size, OUT), (0, 0), ring)
    out.alpha_composite(im)
    return out


def save_pair(name, base, glow):
    outline(base).save(NPC + name + ".png")
    glow.save(NPC + name + "_Glow.png")
    print("wrote", NPC + name, base.size)


def veins(d, box, rnd, count, color=GLOW_MID):
    """Branching infection veins inside a box (drawn on the glow layer)."""
    x0, y0, x1, y1 = box
    for _ in range(count):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        a = rnd.uniform(0, math.tau)
        for _ in range(rnd.randint(3, 6)):
            nx, ny = x + math.cos(a) * rnd.uniform(2, 5), y + math.sin(a) * rnd.uniform(2, 5)
            nx, ny = min(max(nx, x0), x1), min(max(ny, y0), y1)
            d.line([x, y, nx, ny], fill=color)
            x, y = nx, ny
            a += rnd.uniform(-0.9, 0.9)


def mask_to(glow, base):
    """Keep glow pixels only where the base part has pixels."""
    a = base.getchannel("A")
    ga = ImageChops.multiply(glow.getchannel("A"), a.point(lambda v: 255 if v else 0))
    glow.putalpha(ga)
    return glow


# ---------------------------------------------------------------- body parts

def thorax(rnd):
    b, d = img(64, 40)
    g, gd = img(64, 40)
    d.ellipse([4, 6, 60, 36], fill=METAL)
    d.ellipse([8, 8, 56, 30], fill=PLATE)
    d.arc([8, 8, 56, 30], 190, 320, fill=HI, width=2)
    # Armour ridges and bolts.
    for x in (18, 32, 46):
        d.line([x, 9, x - 3, 31], fill=DARK, width=1)
    for x, y in ((12, 18), (52, 18), (32, 10)):
        d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=HI)
    # Leg sockets along the underside.
    for x in (42, 32, 20, 8):
        d.ellipse([x - 4, 26, x + 4, 34], fill=DARK)
    d.ellipse([46, 18, 56, 28], fill=DARK)  # weapon leg socket
    # Infection: cracks through the plating and a glowing seam.
    veins(gd, (8, 8, 56, 32), rnd, 5)
    gd.line([10, 22, 54, 22], fill=GLOW_DIM)
    return b, mask_to(g, b)


def abdomen(rnd):
    b, d = img(80, 56)
    g, gd = img(80, 56)
    d.ellipse([2, 2, 78, 54], fill=METAL)
    d.ellipse([6, 5, 74, 48], fill=PLATE)
    d.arc([6, 5, 74, 48], 200, 300, fill=HI, width=2)
    # Segment bands.
    for x in (20, 36, 52):
        d.arc([x - 30, 2, x + 30, 54], 280, 80, fill=DARK, width=2)
    # The infected egg sacs (glowing) bursting out of the plating, and the spinnerets at the back.
    for (x, y, r) in ((24, 18, 7), (44, 14, 6), (34, 34, 8), (56, 32, 5), (16, 36, 5)):
        d.ellipse([x - r, y - r, x + r, y + r], fill=RUST)
        gd.ellipse([x - r + 1, y - r + 1, x + r - 1, y + r - 1], fill=GLOW_DIM)
        gd.ellipse([x - r + 3, y - r + 3, x + r - 3, y + r - 3], fill=WHITE)
    d.polygon([(2, 24), (-2, 28), (4, 32)], fill=DARK)
    veins(gd, (6, 6, 74, 48), rnd, 7)
    return b, mask_to(g, b)


def head(rnd):
    b, d = img(34, 28)
    g, gd = img(34, 28)
    d.polygon([(0, 6), (18, 1), (30, 6), (33, 14), (28, 24), (10, 26), (0, 20)], fill=METAL)
    d.polygon([(3, 8), (18, 4), (27, 8), (29, 14), (25, 21), (10, 22), (3, 18)], fill=PLATE)
    d.line([6, 7, 18, 4], fill=HI)
    # Mandibles.
    d.polygon([(26, 20), (34, 24), (30, 27), (24, 24)], fill=HI)
    d.polygon([(20, 22), (27, 27), (22, 28), (18, 24)], fill=PLATE)
    # Eight eyes (sockets on the body, glowing on the glow layer).
    eyes = [(22, 9, 3), (27, 11, 2), (16, 10, 2), (24, 15, 2), (29, 16, 1), (19, 15, 1), (13, 13, 1), (26, 6, 1)]
    for x, y, r in eyes:
        d.ellipse([x - r, y - r, x + r, y + r], fill=OUT)
        gd.ellipse([x - r, y - r, x + r, y + r], fill=WHITE)
    veins(gd, (3, 8, 16, 22), rnd, 2, GLOW_DIM)
    return b, mask_to(g, b)


def leg_segment(name, w, h, tip, rnd, width_top, width_bottom):
    """Leg segments point down from their pivot at (w/2, 4)."""
    b, d = img(w, h)
    g, gd = img(w, h)
    cx = w / 2
    end = h - 1 if not tip else h - 1
    d.polygon([(cx - width_top / 2, 1), (cx + width_top / 2, 1), (cx + width_bottom / 2, end - (6 if tip else 2)),
               (cx, end) if tip else (cx + width_bottom / 2, end), (cx - width_bottom / 2, end - (6 if tip else 2))], fill=PLATE)
    d.line([cx - width_top / 2 + 1, 2, cx - width_bottom / 2 + 1, end - 6], fill=HI)
    d.line([cx + width_top / 2 - 1, 2, cx + width_bottom / 2 - 1, end - 6], fill=METAL)
    for y in range(10, h - 8, 10):
        d.line([cx - width_top / 2 + 1, y, cx + width_top / 2 - 1, y], fill=DARK)
    if tip:
        d.polygon([(cx - 2, end - 8), (cx + 2, end - 8), (cx, end)], fill=HI)  # claw
    gd.line([cx, 6, cx, h - 10], fill=GLOW_DIM)
    veins(gd, (cx - width_top / 2 + 1, 4, cx + width_top / 2 - 1, h - 10), rnd, 1)
    save_pair(name, b, mask_to(g, b))


def small_parts():
    j, d = img(10, 10)
    d.ellipse([0, 0, 9, 9], fill=OUT)
    d.ellipse([1, 1, 8, 8], fill=METAL)
    d.ellipse([3, 3, 6, 6], fill=HI)
    j.save(NPC + "Joint.png")
    p, d = img(10, 6)
    d.rectangle([0, 0, 9, 5], fill=OUT)
    d.rectangle([1, 1, 8, 4], fill=PLATE)
    d.line([1, 1, 8, 1], fill=HI)
    p.save(NPC + "Panel.png")
    s, d = img(20, 20)
    teeth = []
    for i in range(24):
        a = math.tau * i / 24
        r = 9.8 if i % 2 == 0 else 7.6
        teeth.append((9.5 + math.cos(a) * r, 9.5 + math.sin(a) * r))
    d.polygon(teeth, fill=HI)
    d.ellipse([3, 3, 16, 16], fill=(170, 176, 192, 255))
    d.ellipse([7, 7, 12, 12], fill=DARK)
    for i in range(4):
        a = math.tau * i / 4 + 0.4
        d.line([9.5 + math.cos(a) * 3, 9.5 + math.sin(a) * 3, 9.5 + math.cos(a) * 6, 9.5 + math.sin(a) * 6], fill=METAL)
    outline(s).save(NPC + "SawBlade.png")
    print("wrote joint, panel, saw blade")


# ---------------------------------------------------------------- weapons (pivot on the left, pointing right)

def weapons(rnd):
    # Rocket launcher: a boxy launcher with three tubes.
    b, d = img(42, 18); g, gd = img(42, 18)
    d.rectangle([2, 3, 34, 15], fill=PLATE); d.line([2, 3, 34, 3], fill=HI)
    for y in (5, 9, 13):
        d.rectangle([30, y - 1, 41, y + 1], fill=METAL)
        d.point((41, y), fill=OUT)
        gd.point((40, y), fill=WHITE)
    d.rectangle([8, 5, 22, 13], fill=DARK)
    gd.rectangle([10, 7, 20, 11], fill=GLOW_DIM); gd.line([10, 9, 20, 9], fill=WHITE)
    save_pair("WeaponRocket", b, mask_to(g, b))

    # Laser cannon: long focusing barrel with glowing coils.
    b, d = img(46, 16); g, gd = img(46, 16)
    d.rectangle([2, 3, 20, 13], fill=PLATE); d.line([2, 3, 20, 3], fill=HI)
    d.rectangle([20, 5, 44, 11], fill=METAL)
    d.rectangle([42, 4, 45, 12], fill=HI)
    for x in (24, 30, 36):
        d.rectangle([x, 4, x + 2, 12], fill=DARK)
        gd.rectangle([x, 5, x + 2, 11], fill=WHITE)
    gd.line([21, 8, 45, 8], fill=GLOW_MID)
    save_pair("WeaponLaser", b, mask_to(g, b))

    # Axe: a handle with a huge double-bitted head.
    b, d = img(38, 40); g, gd = img(38, 40)
    d.rectangle([2, 18, 28, 22], fill=METAL); d.line([2, 18, 28, 18], fill=HI)
    d.polygon([(24, 20), (30, 2), (37, 6), (35, 20), (37, 34), (30, 38)], fill=PLATE)
    d.line([30, 2, 37, 6], fill=HI); d.line([30, 38, 37, 34], fill=HI)
    d.line([36, 7, 36, 33], fill=(200, 205, 220, 255))
    gd.line([35, 8, 35, 32], fill=WHITE)
    veins(gd, (25, 8, 33, 32), rnd, 2)
    save_pair("WeaponAxe", b, mask_to(g, b))

    # Sword: a long blade with an infected edge.
    b, d = img(62, 14); g, gd = img(62, 14)
    d.rectangle([2, 4, 10, 10], fill=METAL)
    d.rectangle([10, 1, 13, 13], fill=PLATE)
    d.polygon([(13, 4), (54, 4), (61, 7), (54, 10), (13, 10)], fill=(176, 182, 200, 255))
    d.line([13, 4, 54, 4], fill=(225, 230, 240, 255))
    d.line([14, 7, 55, 7], fill=HI)
    gd.line([14, 10, 54, 10], fill=WHITE)
    gd.line([54, 10, 61, 7], fill=WHITE)
    save_pair("WeaponSword", b, mask_to(g, b))

    # Flamethrower: fuel tank and a wide nozzle with a pilot flame.
    b, d = img(40, 20); g, gd = img(40, 20)
    d.ellipse([2, 2, 18, 18], fill=PLATE); d.arc([2, 2, 18, 18], 200, 300, fill=HI)
    d.rectangle([16, 6, 32, 14], fill=METAL)
    d.polygon([(32, 4), (39, 2), (39, 18), (32, 16)], fill=DARK)
    gd.ellipse([6, 6, 14, 14], fill=GLOW_DIM)
    gd.polygon([(36, 7), (39, 9), (39, 11), (36, 13)], fill=WHITE)
    save_pair("WeaponFlamer", b, mask_to(g, b))

    # Buzzsaw: an arm housing holding the spinning blade (the blade itself is SawBlade, drawn at x = 22).
    b, d = img(34, 34); g, gd = img(34, 34)
    d.rectangle([2, 13, 22, 21], fill=METAL); d.line([2, 13, 22, 13], fill=HI)
    d.pieslice([10, 5, 34, 29], 180, 270, fill=PLATE)
    d.ellipse([19, 14, 25, 20], fill=DARK)
    gd.ellipse([20, 15, 24, 19], fill=WHITE)
    save_pair("WeaponSaw", b, mask_to(g, b))

    # Tesla coil: stacked coil rings with a glowing ball on the end.
    b, d = img(38, 22); g, gd = img(38, 22)
    d.rectangle([2, 8, 24, 14], fill=METAL)
    for x in range(6, 24, 4):
        d.rectangle([x, 4, x + 2, 18], fill=(170, 120, 60, 255))
    d.ellipse([24, 3, 37, 18], fill=PLATE)
    gd.ellipse([27, 6, 34, 15], fill=WHITE)
    for x in range(7, 24, 4):
        gd.point((x, 11), fill=GLOW_MID)
    save_pair("WeaponTesla", b, mask_to(g, b))


# ---------------------------------------------------------------- preview / bestiary picture

def assemble(scale=1):
    """A standing Akutoku-ō assembled from the parts (for the bestiary and the preview)."""
    W, H = 220, 130
    canvas = Image.new("RGBA", (W, H), CLEAR)
    cx, cy = 104, 58

    def paste(name, at, pivot, glow_color=(170, 90, 235)):
        base = Image.open(NPC + name + ".png")
        canvas.alpha_composite(base, (int(at[0] - pivot[0]), int(at[1] - pivot[1])))
        gpath = NPC + name + "_Glow.png"
        if os.path.exists(gpath):
            gl = Image.open(gpath)
            r, g, b, a = gl.split()
            tint = Image.merge("RGBA", (r.point(lambda v: v * glow_color[0] // 255), g.point(lambda v: v * glow_color[1] // 255),
                                        b.point(lambda v: v * glow_color[2] // 255), a))
            canvas.alpha_composite(tint, (int(at[0] - pivot[0]), int(at[1] - pivot[1])))

    d = ImageDraw.Draw(canvas)
    ground = 124

    def leg(hx, hy, fx, dark):
        kx, ky = (hx + fx) / 2, min(hy, ground) - 36
        col = (54, 57, 68, 255) if dark else PLATE
        d.line([hx, hy, kx, ky], fill=OUT, width=7); d.line([kx, ky, fx, ground], fill=OUT, width=6)
        d.line([hx, hy, kx, ky], fill=col, width=5); d.line([kx, ky, fx, ground], fill=col, width=4)
        d.ellipse([kx - 3, ky - 3, kx + 3, ky + 3], fill=METAL)

    for (hx, fx) in ((cx + 35, cx + 90), (cx + 25, cx + 55), (cx + 13, cx - 5), (cx + 1, cx - 50)):
        leg(hx - 3, cy + 6, fx + 8, True)
    paste("Abdomen", (cx - 4, cy), (74, 24))
    paste("Thorax", (cx + 22, cy), (32, 20))
    paste("Head", (cx + 50, cy - 2), (4, 14))
    for (hx, fx) in ((cx + 38, cx + 96), (cx + 28, cx + 58), (cx + 16, cx - 2), (cx + 4, cx - 46)):
        leg(hx, cy + 10, fx, False)
    # Weapon legs raised: a sword and a rocket launcher.
    d.line([cx + 46, cy + 4, cx + 62, cy - 26], fill=OUT, width=6); d.line([cx + 46, cy + 4, cx + 62, cy - 26], fill=PLATE, width=4)
    paste("WeaponSword", (cx + 62, cy - 26), (2, 7))
    d.line([cx + 42, cy + 1, cx + 52, cy - 34], fill=OUT, width=6); d.line([cx + 42, cy + 1, cx + 52, cy - 34], fill=(54, 57, 68, 255), width=4)
    paste("WeaponRocket", (cx + 52, cy - 34), (2, 9))
    return canvas


def boss_images():
    pic = assemble()
    pic.save(NPC + "Akutokuo.png")
    print("wrote", NPC + "Akutokuo.png", pic.size)
    # Boss head icon for the map: the spider's head.
    icon = Image.open(NPC + "Head.png").crop((0, 0, 34, 28))
    tinted = Image.open(NPC + "Head_Glow.png")
    icon.alpha_composite(Image.merge("RGBA", (tinted.split()[0].point(lambda v: v * 170 // 255), tinted.split()[1].point(lambda v: v * 90 // 255),
                                               tinted.split()[2].point(lambda v: v * 235 // 255), tinted.split()[3])))
    icon = outline(icon)
    icon.save(NPC + "Akutokuo_Head_Boss.png")
    print("wrote boss head")
    prev = Image.new("RGBA", pic.size, (26, 30, 44, 255))
    prev.alpha_composite(pic)
    prev.resize((pic.width * 3, pic.height * 3), Image.NEAREST).save("tools/akutoku_preview_3x.png")
    print("wrote tools/akutoku_preview_3x.png")


# ---------------------------------------------------------------- Robitacons and the Robo-Mechanic (from the form sheets)

SHEETS = {"Robot": "Robot", "Blaze": "BlazeJack", "Frost": "FrostJack", "Volt": "VoltJack", "Shadow": "ShadowJack",
          "Nova": "NovaJack", "Omega": "OmegaJack", "God": "GodJack"}


def form_frames(sheet):
    base = "Content/Players/" + sheet
    legs, body = Image.open(base + "Legs.png").convert("RGBA"), Image.open(base + "Body.png").convert("RGBA")
    lg, bg = Image.open(base + "Legs_Glow.png").convert("RGBA"), Image.open(base + "Body_Glow.png").convert("RGBA")
    out = Image.new("RGBA", legs.size, CLEAR)
    out.alpha_composite(legs); out.alpha_composite(body)
    glow = Image.new("RGBA", legs.size, CLEAR)
    glow.alpha_composite(lg); glow.alpha_composite(bg)
    return out, glow


def robitacons():
    for name, sheet in SHEETS.items():
        rnd = random.Random(name)
        base, glow = form_frames(sheet)
        # Infected: darker, desaturated, with scars; the original glow turns into the infection's glow.
        inf = ImageEnhance.Color(base).enhance(0.45)
        inf = ImageEnhance.Brightness(inf).enhance(0.62)
        g_out = Image.new("RGBA", base.size, CLEAR)
        gray = glow.convert("LA").convert("RGBA")
        r, g, b, a = gray.split()
        g_out = Image.merge("RGBA", (r.point(lambda v: min(255, v + 90)), g.point(lambda v: min(255, v + 90)), b.point(lambda v: min(255, v + 90)), a))
        gd = ImageDraw.Draw(g_out)
        dd = ImageDraw.Draw(inf)
        alpha = base.getchannel("A")
        for f in range(20):
            y0 = f * 56
            # Veins over the torso and a cracked eye glow.
            for _ in range(3):
                x, y = rnd.uniform(14, 26), rnd.uniform(y0 + 20, y0 + 38)
                for _ in range(3):
                    nx, ny = x + rnd.uniform(-3, 3), y + rnd.uniform(-3, 3)
                    if alpha.getpixel((int(nx) % 40, int(ny))) and alpha.getpixel((int(x) % 40, int(y))):
                        gd.line([x, y, nx, ny], fill=GLOW_MID)
                        dd.line([x, y, nx, ny], fill=(30, 20, 30, 255))
                    x, y = nx, ny
        g_out = mask_to(g_out, base)
        inf.save(NPC + "Robitacon%s.png" % name)
        g_out.save(NPC + "Robitacon%s_Glow.png" % name)
        print("wrote", NPC + "Robitacon" + name)


def robo_mechanic():
    base, glow = form_frames("Robot")
    r, g, b, a = base.split()
    # Repaint the armour in mechanic yellow and orange.
    base = Image.merge("RGBA", (r.point(lambda v: min(255, int(v * 1.05))), g.point(lambda v: int(v * 0.8)), b.point(lambda v: int(v * 0.38)), a))
    base.alpha_composite(glow)
    order = [0, 5] + list(range(6, 20)) + [0] * 5 + [3] * 4  # 25 town NPC frames from the 20 player frames
    sheet = Image.new("RGBA", (40, 56 * 25), CLEAR)
    for i, f in enumerate(order):
        sheet.alpha_composite(base.crop((0, f * 56, 40, (f + 1) * 56)), (0, i * 56))
    # A tool belt and a wrench on the back.
    d = ImageDraw.Draw(sheet)
    for i in range(25):
        y = i * 56
        d.line([13, y + 38, 26, y + 38], fill=(90, 60, 30, 255))
    sheet.save("Content/NPCs/RoboMechanic.png")
    head = base.crop((10, 2, 30, 22)).resize((16, 16), Image.NEAREST)
    head.save("Content/NPCs/RoboMechanic_Head.png")
    print("wrote Robo-Mechanic")


# ---------------------------------------------------------------- items, buffs, icons, tiles

BLIGHT = ((170, 90, 235, 255), (220, 255, 140, 255), (70, 30, 110, 255))


def items():
    os.makedirs("Content/Items/Akutoku", exist_ok=True)
    P = "Content/Items/Akutoku/"
    m, c, k = BLIGHT

    i, d = gs.canvas(16, 16)  # Infected Core
    d.ellipse([2, 2, 13, 13], fill=OUT); d.ellipse([3, 3, 12, 12], fill=METAL)
    d.ellipse([5, 5, 10, 10], fill=m); d.point((7, 7), fill=c)
    for x, y in ((3, 7), (12, 8), (7, 3), (8, 12)):
        d.point((x, y), fill=(230, 60, 70, 255))
    gs.save(i, P + "InfectedCore.png")

    i, d = gs.canvas(14, 14)  # Robitacon Parts
    d.polygon([(1, 8), (6, 3), (12, 5), (11, 11), (4, 12)], fill=OUT)
    d.polygon([(2, 8), (6, 4), (11, 6), (10, 10), (4, 11)], fill=PLATE)
    d.ellipse([5, 6, 8, 9], fill=DARK); d.point((6, 7), fill=m)
    d.line([2, 8, 6, 4], fill=HI)
    gs.save(i, P + "RobitaconParts.png")

    i, d = gs.canvas(16, 16)  # Treasure bag
    d.polygon([(3, 5), (12, 5), (14, 14), (1, 14)], fill=OUT)
    d.polygon([(4, 6), (11, 6), (13, 13), (2, 13)], fill=(80, 40, 110, 255))
    d.rectangle([5, 2, 10, 5], fill=OUT); d.rectangle([6, 3, 9, 4], fill=(120, 70, 150, 255))
    d.ellipse([6, 8, 9, 11], fill=m); d.point((7, 9), fill=c)
    gs.save(i, P + "AkutokuBag.png")

    i, d = gs.canvas(16, 16)  # Trophy item
    d.rectangle([0, 0, 15, 15], fill=(120, 85, 60, 255)); d.rectangle([1, 1, 14, 14], fill=(160, 120, 80, 255))
    d.ellipse([4, 4, 11, 10], fill=METAL); d.point((6, 6), fill=m); d.point((9, 6), fill=m)
    for x0, x1 in ((4, 1), (11, 14)):
        d.line([x0, 9, x1, 13], fill=DARK)
    gs.save(i, P + "AkutokuTrophy.png")

    # Trophy tile: 3x3 wall hanging, 16 px frames on an 18 px grid.
    big = Image.new("RGBA", (48, 48), CLEAR); d = ImageDraw.Draw(big)
    d.rectangle([0, 0, 47, 47], fill=(110, 78, 55, 255)); d.rectangle([2, 2, 45, 45], fill=(150, 110, 75, 255))
    head = Image.open(NPC + "Head.png")
    big.alpha_composite(head, (7, 10))
    tile = Image.new("RGBA", (54, 54), CLEAR)
    for fx in range(3):
        for fy in range(3):
            tile.alpha_composite(big.crop((fx * 16, fy * 16, fx * 16 + 16, fy * 16 + 16)), (fx * 18, fy * 18))
    tile.save("Content/Tiles/AkutokuTrophyTile.png")

    i, d = gs.canvas(12, 12)  # Mask item
    d.polygon([(1, 4), (6, 1), (11, 4), (10, 10), (2, 10)], fill=OUT)
    d.polygon([(2, 5), (6, 2), (10, 5), (9, 9), (3, 9)], fill=PLATE)
    for x, y in ((4, 5), (7, 5), (5, 7), (8, 7)):
        d.point((x, y), fill=m)
    gs.save(i, P + "AkutokuMask.png")

    # Mask worn: a spider helmet over the player's head, frame by frame (40x56 frames, head bobs in the walk cycle).
    sheet = Image.new("RGBA", (40, 56 * 20), CLEAR); d = ImageDraw.Draw(sheet)
    for f in range(20):
        y = f * 56 + (-2 if f in (7, 8, 9, 14, 15, 16) else 0)
        d.polygon([(11, y + 10), (18, y + 4), (28, y + 6), (30, y + 14), (27, y + 21), (13, y + 21)], fill=OUT)
        d.polygon([(12, y + 10), (18, y + 5), (27, y + 7), (29, y + 14), (26, y + 20), (14, y + 20)], fill=PLATE)
        d.line([13, y + 9, 18, y + 5], fill=HI)
        for ex, ey in ((22, 10), (25, 11), (21, 14), (26, 15)):
            d.point((ex, y + ey), fill=m)
        d.polygon([(26, y + 19), (31, y + 22), (27, y + 23)], fill=HI)  # mandible
    sheet.save(P + "AkutokuMask_Head.png")

    i, d = gs.canvas(16, 16)  # Robot Assembly Kit
    d.rectangle([1, 5, 14, 14], fill=OUT); d.rectangle([2, 6, 13, 13], fill=(200, 150, 60, 255))
    d.rectangle([5, 2, 10, 5], fill=OUT); d.rectangle([6, 3, 9, 4], fill=METAL)
    d.rectangle([4, 8, 11, 11], fill=PLATE); d.point((6, 9), fill=(90, 230, 255, 255)); d.point((9, 9), fill=(90, 230, 255, 255))
    gs.save(i, P + "RobotAssemblyKit.png")

    i, d = gs.canvas(16, 16)  # Robitacon Remote
    d.rounded_rectangle([4, 2, 11, 15], radius=1, fill=OUT); d.rounded_rectangle([5, 3, 10, 14], radius=1, fill=METAL)
    d.line([7, 0, 7, 2], fill=OUT); d.point((7, 0), fill=(255, 80, 80, 255))
    d.rectangle([6, 4, 9, 7], fill=(90, 230, 255, 255))
    for y in (9, 11):
        d.point((6, y), fill=HI); d.point((9, y), fill=HI)
    gs.save(i, P + "RobitaconRemote.png")

    # Leg weapons as items: the boss's weapon art, scaled to item size.
    for item, part, size in (("SpiderRocketLauncher", "WeaponRocket", None), ("SpiderLaserCannon", "WeaponLaser", None),
                             ("SpiderFlamethrower", "WeaponFlamer", None), ("SpiderTeslaCoil", "WeaponTesla", None)):
        base = Image.open(NPC + part + ".png")
        glow = Image.open(NPC + part + "_Glow.png")
        r, g, b, a = glow.split()
        base.alpha_composite(Image.merge("RGBA", (r.point(lambda v: v * m[0] // 255), g.point(lambda v: v * m[1] // 255), b.point(lambda v: v * m[2] // 255), a)))
        base.save(P + item + ".png")
    # Axe and sword: swung, so drawn diagonally (handle bottom-left).
    for item, part in (("SpiderAxe", "WeaponAxe"), ("SpiderSword", "WeaponSword")):
        base = Image.open(NPC + part + ".png")
        glow = Image.open(NPC + part + "_Glow.png")
        r, g, b, a = glow.split()
        base.alpha_composite(Image.merge("RGBA", (r.point(lambda v: v * m[0] // 255), g.point(lambda v: v * m[1] // 255), b.point(lambda v: v * m[2] // 255), a)))
        rot = base.rotate(45, expand=True, resample=Image.NEAREST)
        rot.crop(rot.getbbox()).save(P + item + ".png")
    Image.open(NPC + "SawBlade.png").resize((30, 30), Image.NEAREST).save(P + "SpiderBuzzsaw.png")

    i, d = gs.canvas(14, 16)  # Spider Trigger
    d.rounded_rectangle([3, 5, 10, 15], radius=2, fill=OUT); d.rounded_rectangle([4, 6, 9, 14], radius=1, fill=DARK)
    d.line([4, 6, 4, 14], fill=m)
    d.rectangle([5, 0, 8, 5], fill=OUT); d.rectangle([6, 1, 7, 4], fill=m); d.point((6, 1), fill=c)
    for (x0, y0, x1, y1) in ((3, 8, 0, 6), (3, 11, 0, 13), (10, 8, 13, 6), (10, 11, 13, 13)):
        d.line([x0, y0, x1, y1], fill=HI)
    d.point((6, 9), fill=c); d.point((7, 9), fill=m)
    gs.save(i, P + "SpiderTrigger.png")
    print("wrote items")


def buffs_and_icons():
    m, c, k = BLIGHT

    def siphon(d):
        d.ellipse([2, 2, 13, 13], fill=k)
        d.polygon([(5, 4), (11, 8), (5, 12)], fill=m)
        d.line([3, 8, 12, 8], fill=(230, 60, 70, 255))
    gs.buff_icon("Content/Buffs/Siphoned.png", siphon)

    def stolen(d):
        d.ellipse([2, 2, 13, 13], fill=(90, 20, 30, 255))
        d.polygon([(7, 2), (11, 8), (8, 8), (8, 13), (6, 13), (6, 8), (3, 8)], fill=(235, 60, 70, 255))
    gs.buff_icon("Content/Buffs/StolenPower.png", stolen)

    def minions(d):
        for x in (3, 9):
            d.rounded_rectangle([x, 4, x + 4, 9], radius=1, fill=(200, 205, 220, 255)); d.point((x + 3, 6), fill=(90, 230, 255, 255))
            d.rectangle([x + 1, 10, x + 3, 13], fill=(130, 135, 150, 255))
    gs.buff_icon("Content/Buffs/RobitaconMinionBuff.png", minions)

    def spider(d):
        d.ellipse([4, 5, 11, 11], fill=PLATE); d.point((9, 7), fill=m); d.point((10, 8), fill=m)
        for (x0, y0, x1, y1) in ((5, 9, 1, 13), (6, 10, 3, 14), (9, 10, 12, 14), (10, 9, 14, 13), (5, 7, 1, 4), (10, 7, 14, 4)):
            d.line([x0, y0, x1, y1], fill=HI)
    gs.buff_icon("Content/Buffs/SpiderJackForm.png", spider)

    def hive(d):
        for (x, y) in ((5, 5), (10, 5), (7, 9), (4, 10), (11, 10)):
            d.regular_polygon((x, y, 2.5), 6, fill=m)
            d.point((x, y), fill=c)
    gs.buff_icon("Content/Buffs/HiveMind.png", hive)

    # Spider Jack's ability icons.
    def bolt(d):
        for dy in (-3, 0, 3):
            d.line([1, 12 + dy, 9, 6 + dy], fill=k, width=2); d.ellipse([9, 3 + dy, 13, 7 + dy], fill=m); d.point((11, 5 + dy), fill=c)
    def beam(d):
        d.rectangle([0, 5, 5, 11], fill=OUT); d.rectangle([1, 6, 4, 10], fill=PLATE)
        d.rectangle([5, 6, 15, 10], fill=m); d.line([5, 8, 15, 8], fill=c)
    def spikes(d):
        d.rectangle([0, 13, 15, 15], fill=(80, 50, 60, 255))
        for x, t in ((2, 6), (6, 2), (10, 5), (14, 8)):
            d.polygon([(x - 2, 13), (x, t), (x + 2, 13)], fill=m); d.line([x, t + 2, x, 12], fill=c)
    def rain(d):
        for (x, y) in ((3, 1), (8, 4), (12, 0), (5, 9)):
            d.ellipse([x, y, x + 3, y + 4], fill=m); d.point((x + 1, y + 1), fill=c)
    def web(d):
        for a in range(0, 360, 45):
            r = math.radians(a)
            d.line([8, 8, 8 + math.cos(r) * 7, 8 + math.sin(r) * 7], fill=(230, 230, 240, 255))
        for rr in (3, 6):
            d.ellipse([8 - rr, 8 - rr, 8 + rr, 8 + rr], outline=(200, 200, 215, 255))
        d.point((8, 8), fill=m)
    def pounce(d):
        d.polygon([(15, 8), (8, 3), (8, 13)], fill=c)
        for y in (5, 8, 11):
            d.line([0, y, 7, y], fill=m)
    def saws(d):
        d.ellipse([1, 1, 14, 14], outline=k)
        for (x, y) in ((6, 0), (12, 6), (6, 12), (0, 6)):
            d.ellipse([x, y, x + 3, y + 3], fill=HI); d.point((x + 1, y + 1), fill=m)
    def hivemind(d):
        d.ellipse([1, 1, 14, 14], fill=k)
        hive(d)
    for name, fn in (("VenomSpit", bolt), ("InfectionLaser", beam), ("BlightSpikes", spikes), ("AcidRain", rain),
                     ("WebBurst", web), ("Pounce", pounce), ("BuzzsawRing", saws), ("HiveMindAbility", hivemind)):
        gs.ability_icon("Content/Abilities/%s.png" % name, fn)

    # Rocket projectile.
    i, d = gs.canvas(20, 8)
    d.rectangle([2, 2, 15, 5], fill=PLATE); d.polygon([(15, 1), (19, 3.5), (15, 6)], fill=(230, 60, 70, 255))
    d.polygon([(2, 2), (0, 0), (5, 2)], fill=DARK); d.polygon([(2, 5), (0, 7), (5, 5)], fill=DARK)
    d.line([3, 3, 14, 3], fill=HI)
    outline(i).save("Content/Projectiles/InfectedRocket.png")
    print("wrote buffs and icons")


if __name__ == "__main__":
    os.makedirs(NPC, exist_ok=True)
    rnd = random.Random(7)
    for name, fn in (("Thorax", thorax), ("Abdomen", abdomen), ("Head", head)):
        b, g = fn(rnd)
        save_pair(name, b, g)
    leg_segment("LegUpper", 12, 48, False, rnd, 9, 7)
    leg_segment("LegLower", 10, 58, True, rnd, 7, 4)
    leg_segment("WeaponArm", 12, 48, False, rnd, 10, 8)
    small_parts()
    weapons(rnd)
    boss_images()
    robitacons()
    robo_mechanic()
    items()
    buffs_and_icons()
