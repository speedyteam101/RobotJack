"""Generates the Shift Titan's sprites, the laboratory tiles and the titan ability icons.

Run from the RobotJack folder:  python3 tools/generate_titan_sprites.py
Requires Pillow. Also writes tools/titan_preview_3x.png, a preview of the assembled titan and car.

The titan is drawn by TitanRenderer as a rig of separate parts (Content/Titan). Every part has four layers, all the
same size so they line up on the same pivot:
  _M  main armor plates, in greys (tinted with the armor colour chosen in the menu)
  _A  accent plates, in greys (tinted with the accent colour)
  _D  untinted details: outlines, joints, dark metal, visors
  _G  full-bright glow, in white (tinted with the core colour)
Fusion decorations only have _D (their own colours) and _G (tinted with the fusion's element colour).
Parts are drawn at 1x here and 4x in game, so the pivots in TitanRenderer.cs are in these pixels.
"""
import math
import os
from PIL import Image, ImageDraw, ImageChops, ImageFilter

import generate_sprites as gs
from titan_abilities import FUSIONS, KINDS, abilities

OUT = "Content/Titan/"
CLEAR = (0, 0, 0, 0)
OUTLINE = (18, 20, 28, 255)
JOINT = (70, 74, 86, 255)
JOINT_L = (110, 116, 130, 255)
METAL = (150, 156, 170, 255)
METAL_L = (205, 210, 222, 255)
METAL_D = (92, 97, 110, 255)
VISOR = (14, 18, 30, 255)
WHITE = (255, 255, 255, 255)

# Greys for the tinted layers: multiplied by the chosen colour in game.
G_HI = (255, 255, 255, 255)
G_BASE = (222, 222, 222, 255)
G_MID = (178, 178, 178, 255)
G_DARK = (132, 132, 132, 255)


def grey(v):
    return (v, v, v, 255)


class Part:
    """A part's four layers."""

    def __init__(self, name, w, h):
        self.name, self.w, self.h = name, w, h
        self.layers = {k: Image.new("RGBA", (w, h), CLEAR) for k in "MADG"}
        self.draw = {k: ImageDraw.Draw(v) for k, v in self.layers.items()}

    @property
    def m(self):
        return self.draw["M"]

    @property
    def a(self):
        return self.draw["A"]

    @property
    def d(self):
        return self.draw["D"]

    @property
    def g(self):
        return self.draw["G"]

    def save(self, outline=True):
        if outline:
            # A 1 px dark outline around everything, drawn into the detail layer.
            union = Image.new("L", (self.w, self.h), 0)
            for k in "MAD":
                union = ImageChops.lighter(union, self.layers[k].getchannel("A"))
            union = union.point(lambda v: 255 if v > 0 else 0)
            grown = union.filter(ImageFilter.MaxFilter(3))
            ring = ImageChops.subtract(grown, union)
            solid = Image.new("RGBA", (self.w, self.h), OUTLINE)
            self.layers["D"].paste(solid, (0, 0), ring)
        for k, img in self.layers.items():
            img.save(OUT + "%s_%s.png" % (self.name, k))
        print("wrote", OUT + self.name + "_[MADG].png", self.w, "x", self.h)


def plate(draw, box, radius=1):
    """A shaded grey plate: lit top-left edge, shadowed bottom-right edge."""
    x0, y0, x1, y1 = box
    draw.rounded_rectangle(box, radius=radius, fill=G_BASE)
    if x1 - x0 >= 3 and y1 - y0 >= 3:
        draw.line([x0 + 1, y0, x1 - 1, y0], fill=G_HI)
        draw.line([x0, y0 + 1, x0, y1 - 1], fill=G_HI)
        draw.line([x0 + 1, y1, x1 - 1, y1], fill=G_DARK)
        draw.line([x1, y0 + 1, x1, y1 - 1], fill=G_MID)


def poly_plate(draw, points):
    draw.polygon(points, fill=G_BASE)
    # Light the upper-left edges and shade the lower-right ones.
    n = len(points)
    for i in range(n):
        p, q = points[i], points[(i + 1) % n]
        mx, my = (p[0] + q[0]) / 2, (p[1] + q[1]) / 2
        cx = sum(pt[0] for pt in points) / n
        cy = sum(pt[1] for pt in points) / n
        lit = (mx - cx) + (my - cy) < 0
        draw.line([p, q], fill=G_HI if lit else G_DARK)


def joint(draw, x, y, r):
    draw.ellipse([x - r, y - r, x + r, y + r], fill=JOINT)
    draw.ellipse([x - r + 1, y - r + 1, x + r - 1, y + r - 1], fill=JOINT_L)
    draw.point((x, y), fill=JOINT)


# ---------------------------------------------------------------- body parts

def torso():
    p = Part("Torso", 35, 33)
    # Waist (dark metal) under the chest plate.
    p.d.rectangle([11, 22, 23, 32], fill=METAL_D)
    for y in (24, 27, 30):
        p.d.line([12, y, 22, y], fill=JOINT)
    # Chest plate: broad at the shoulders, narrowing to the waist.
    poly_plate(p.m, [(2, 3), (32, 3), (30, 17), (25, 25), (9, 25), (4, 17)])
    p.m.line([6, 5, 28, 5], fill=G_HI)
    # Pectoral plates either side of the core.
    plate(p.m, (4, 6, 11, 17), 2)
    plate(p.m, (23, 6, 30, 17), 2)
    # Accent: collar and a V down to the waist, and side trims.
    p.a.polygon([(10, 1), (24, 1), (22, 5), (12, 5)], fill=G_BASE)
    p.a.line([10, 1, 24, 1], fill=G_HI)
    p.a.line([12, 20, 17, 24], fill=G_BASE, width=2)
    p.a.line([22, 20, 17, 24], fill=G_BASE, width=2)
    p.a.rectangle([3, 18, 5, 21], fill=G_MID)
    p.a.rectangle([29, 18, 31, 21], fill=G_MID)
    # Core socket (the core itself is drawn on top in game).
    p.d.ellipse([11, 7, 23, 19], fill=OUTLINE)
    p.d.ellipse([12, 8, 22, 18], fill=VISOR)
    for a in range(0, 360, 45):
        r = math.radians(a)
        p.d.point((17 + round(math.cos(r) * 6), 13 + round(math.sin(r) * 6)), fill=METAL_L)
    # Panel lines and vents.
    p.d.line([4, 17, 9, 24], fill=JOINT)
    p.d.line([30, 17, 25, 24], fill=JOINT)
    for y in (9, 11, 13):
        p.d.line([6, y, 9, y], fill=JOINT)
        p.d.line([25, y, 28, y], fill=JOINT)
    # Glow: power lines down the sides of the chest.
    p.g.line([7, 19, 10, 23], fill=WHITE)
    p.g.line([27, 19, 24, 23], fill=WHITE)
    p.save()


def pelvis():
    p = Part("Pelvis", 27, 12)
    joint(p.d, 9, 4, 3)
    joint(p.d, 17, 4, 3)
    plate(p.m, (3, 1, 23, 8), 2)
    p.a.rectangle([4, 1, 22, 2], fill=G_BASE)
    p.a.line([4, 1, 22, 1], fill=G_HI)
    # Hip guards.
    poly_plate(p.m, [(1, 3), (6, 3), (6, 10), (2, 9)])
    poly_plate(p.m, [(20, 3), (25, 3), (24, 9), (20, 10)])
    p.d.rectangle([11, 3, 15, 7], fill=METAL_D)
    p.d.rectangle([12, 4, 14, 6], fill=METAL)
    p.g.point((13, 5), fill=WHITE)
    p.save()


def backpack():
    p = Part("Backpack", 16, 24)
    plate(p.m, (1, 1, 13, 20), 2)
    # Missile pod lids (accent) with glowing warheads.
    for y in (3, 9):
        p.a.rectangle([2, y, 9, y + 4], fill=G_BASE)
        p.a.line([2, y, 9, y], fill=G_HI)
        p.d.rectangle([3, y + 1, 8, y + 3], fill=VISOR)
        for x in (4, 7):
            p.g.point((x, y + 2), fill=WHITE)
    # Thruster nozzle.
    p.d.rectangle([4, 20, 10, 23], fill=METAL_D)
    p.d.line([4, 20, 10, 20], fill=METAL)
    p.g.rectangle([5, 22, 9, 23], fill=WHITE)
    p.d.line([11, 3, 11, 18], fill=JOINT)
    p.save()


def heads():
    # Visor: a sleek helmet with a glowing visor strip facing forward.
    p = Part("Head0", 22, 20)
    p.d.rectangle([7, 15, 15, 19], fill=METAL_D)  # neck
    plate(p.m, (2, 2, 19, 16), 4)
    p.a.polygon([(8, 0), (13, 0), (12, 7), (9, 7)], fill=G_BASE)
    p.a.line([8, 0, 13, 0], fill=G_HI)
    p.a.rectangle([3, 13, 7, 16], fill=G_MID)
    p.d.polygon([(7, 7), (20, 7), (20, 12), (9, 12)], fill=VISOR)
    p.g.line([10, 9, 19, 9], fill=WHITE)
    p.g.point((19, 10), fill=WHITE)
    p.d.line([14, 14, 18, 14], fill=JOINT)
    p.save()

    # Horned: a heavier helmet with two swept-back horns and twin eyes.
    p = Part("Head1", 22, 20)
    p.d.rectangle([7, 15, 15, 19], fill=METAL_D)
    p.a.polygon([(4, 7), (7, 4), (2, 0), (0, 1)], fill=G_BASE)
    p.a.polygon([(14, 4), (17, 6), (21, 0), (19, 0)], fill=G_BASE)
    p.a.line([2, 0, 7, 4], fill=G_HI)
    plate(p.m, (3, 3, 19, 17), 3)
    p.m.polygon([(15, 12), (20, 12), (20, 17), (15, 17)], fill=G_MID)  # jaw guard
    p.d.rectangle([11, 8, 19, 11], fill=VISOR)
    p.g.rectangle([12, 9, 13, 10], fill=WHITE)
    p.g.rectangle([16, 9, 17, 10], fill=WHITE)
    p.d.line([16, 14, 19, 14], fill=JOINT)
    p.save()

    # Dome: a round dome with one big cyclops eye.
    p = Part("Head2", 22, 20)
    p.d.rectangle([7, 15, 15, 19], fill=METAL_D)
    p.m.ellipse([2, 1, 19, 18], fill=G_BASE)
    p.m.arc([2, 1, 19, 18], 180, 270, fill=G_HI)
    p.m.arc([2, 1, 19, 18], 0, 90, fill=G_DARK)
    p.a.rectangle([2, 12, 19, 14], fill=G_BASE)
    p.a.line([2, 12, 19, 12], fill=G_HI)
    p.d.ellipse([10, 5, 18, 12], fill=VISOR)
    p.g.ellipse([12, 7, 16, 10], fill=WHITE)
    p.d.point((14, 8), fill=METAL_L)
    p.save()


def arms():
    p = Part("UpperArm", 11, 24)
    p.d.rectangle([3, 8, 7, 20], fill=METAL_D)
    plate(p.m, (2, 9, 8, 19), 1)
    joint(p.d, 5, 20, 3)
    # Big shoulder pad with an accent trim.
    p.m.ellipse([0, 0, 10, 10], fill=G_BASE)
    p.m.arc([0, 0, 10, 10], 180, 300, fill=G_HI)
    p.a.arc([0, 0, 10, 10], 20, 160, fill=G_BASE, width=2)
    p.d.point((5, 3), fill=JOINT)
    p.save()

    p = Part("Forearm", 11, 22)
    plate(p.m, (2, 1, 8, 15), 2)
    p.a.rectangle([2, 10, 8, 12], fill=G_BASE)
    p.d.rounded_rectangle([2, 15, 8, 21], radius=2, fill=METAL_D)  # fist
    p.d.line([3, 17, 7, 17], fill=JOINT)
    p.save()


def weapons():
    # Cannon: an armored housing with a long barrel and a glowing muzzle.
    p = Part("ArmCannon", 15, 34)
    plate(p.m, (1, 1, 13, 17), 2)
    p.a.rectangle([1, 12, 13, 14], fill=G_BASE)
    p.a.line([1, 12, 13, 12], fill=G_HI)
    p.d.rectangle([4, 17, 10, 30], fill=METAL_D)
    p.d.line([5, 17, 5, 30], fill=METAL)
    p.d.rectangle([3, 29, 11, 32], fill=METAL)
    p.d.rectangle([5, 31, 9, 33], fill=VISOR)
    p.g.rectangle([6, 32, 8, 33], fill=WHITE)
    for y in (5, 8):
        p.g.line([4, y, 10, y], fill=WHITE)
    p.save()

    # Blade: a long energy-edged sword blade.
    p = Part("ArmBlade", 13, 46)
    plate(p.m, (2, 1, 10, 13), 2)
    p.a.rectangle([2, 10, 10, 13], fill=G_BASE)
    p.d.rectangle([3, 13, 9, 15], fill=METAL_D)  # guard
    p.d.polygon([(4, 15), (8, 15), (8, 38), (6, 45), (4, 38)], fill=METAL_L)
    p.d.line([5, 16, 5, 38], fill=WHITE)
    p.d.line([8, 16, 8, 38], fill=METAL)
    p.g.line([4, 16, 4, 38], fill=WHITE)
    p.g.line([4, 38, 6, 44], fill=WHITE)
    p.save()

    # Claw: three curved talons.
    p = Part("ArmClaw", 17, 30)
    plate(p.m, (3, 1, 13, 14), 2)
    p.a.rectangle([3, 11, 13, 14], fill=G_BASE)
    p.d.rectangle([4, 14, 12, 17], fill=METAL_D)
    for x0, bend in ((4, -2), (8, 0), (12, 2)):
        p.d.line([x0, 17, x0 + bend // 2, 23], fill=METAL_L, width=2)
        p.d.line([x0 + bend // 2, 23, x0 + bend, 28], fill=METAL_L, width=1)
    for x in (5, 8, 11):
        p.g.point((x, 15), fill=WHITE)
    p.save()


def legs():
    p = Part("Thigh", 13, 28)
    poly_plate(p.m, [(1, 0), (11, 0), (10, 21), (3, 21)])
    p.a.line([9, 2, 9, 19], fill=G_BASE, width=2)
    joint(p.d, 6, 2, 3)
    joint(p.d, 6, 24, 3)
    p.save()

    p = Part("Shin", 13, 29)
    p.d.rectangle([4, 20, 8, 27], fill=METAL_D)
    poly_plate(p.m, [(2, 4), (11, 4), (10, 22), (3, 22)])
    p.a.polygon([(1, 0), (11, 0), (11, 6), (6, 8), (1, 6)], fill=G_BASE)  # knee guard
    p.a.line([1, 0, 11, 0], fill=G_HI)
    p.d.line([4, 12, 9, 12], fill=JOINT)
    p.g.line([8, 14, 8, 19], fill=WHITE)
    joint(p.d, 6, 26, 2)
    p.save()

    p = Part("Foot", 20, 8)
    poly_plate(p.m, [(0, 1), (13, 1), (19, 5), (19, 7), (0, 7)])
    p.a.polygon([(14, 2), (19, 5), (19, 7), (14, 7)], fill=G_BASE)  # toe cap
    p.d.line([0, 7, 19, 7], fill=METAL_D)
    p.d.rectangle([3, 0, 8, 2], fill=METAL_D)
    p.save()


def car():
    # A low sports car: the titan folded up, its core becoming the headlight at (95, 21).
    p = Part("CarBody", 100, 42)
    body = [(2, 22), (8, 18), (30, 14), (40, 6), (62, 5), (74, 13), (96, 16), (99, 21), (99, 31), (2, 31)]
    poly_plate(p.m, body)
    # Cockpit glass.
    p.d.polygon([(42, 8), (60, 7), (70, 14), (38, 15)], fill=VISOR)
    p.d.line([44, 9, 58, 8], fill=(90, 100, 130, 255))
    # Racing stripe and spoiler (accent).
    p.a.polygon([(4, 23), (98, 21), (98, 24), (4, 26)], fill=G_BASE)
    p.a.line([4, 23, 98, 21], fill=G_HI)
    p.a.polygon([(1, 12), (12, 12), (10, 16), (3, 16)], fill=G_BASE)
    p.d.line([6, 16, 8, 19], fill=METAL_D, width=2)
    # Wheel arches (dark) and side skirt.
    for cx in (21, 79):
        p.d.pieslice([cx - 12, 20, cx + 12, 44], 180, 360, fill=VISOR)
    p.d.rectangle([33, 30, 67, 33], fill=METAL_D)
    # Vents.
    for x in (80, 84, 88):
        p.d.line([x, 18, x - 2, 20], fill=JOINT)
    # Glow: tail lights, side power line, underglow.
    p.g.rectangle([2, 22, 4, 26], fill=WHITE)
    p.g.line([34, 27, 66, 27], fill=WHITE)
    p.g.line([10, 34, 90, 34], fill=WHITE)
    p.save()

    img = Image.new("RGBA", (19, 19), CLEAR)
    d = ImageDraw.Draw(img)
    d.ellipse([0, 0, 18, 18], fill=OUTLINE)
    d.ellipse([1, 1, 17, 17], fill=(34, 36, 44, 255))
    d.ellipse([4, 4, 14, 14], fill=METAL)
    d.ellipse([5, 5, 13, 13], fill=METAL_D)
    for a in range(0, 360, 72):
        r = math.radians(a)
        d.line([9, 9, 9 + math.cos(r) * 5, 9 + math.sin(r) * 5], fill=METAL_L)
    d.ellipse([7, 7, 11, 11], fill=METAL_L)
    d.point((9, 9), fill=JOINT)
    img.save(OUT + "Wheel_D.png")
    print("wrote", OUT + "Wheel_D.png")


def cores():
    # White shapes, tinted with the core colour in game (drawn full-bright over a glow).
    shapes = []
    img = Image.new("RGBA", (12, 12), CLEAR); d = ImageDraw.Draw(img)
    d.ellipse([1, 1, 10, 10], fill=grey(200)); d.ellipse([2, 2, 9, 9], fill=WHITE); d.ellipse([4, 3, 6, 5], fill=grey(255))
    shapes.append(img)
    img = Image.new("RGBA", (12, 12), CLEAR); d = ImageDraw.Draw(img)
    d.polygon([(5.5, 0), (11, 5.5), (5.5, 11), (0, 5.5)], fill=grey(200)); d.polygon([(5.5, 2), (9, 5.5), (5.5, 9), (2, 5.5)], fill=WHITE)
    shapes.append(img)
    img = Image.new("RGBA", (12, 12), CLEAR); d = ImageDraw.Draw(img)
    pts = []
    for i in range(10):
        r = 5.8 if i % 2 == 0 else 2.6
        a = math.radians(-90 + i * 36)
        pts.append((5.5 + math.cos(a) * r, 5.8 + math.sin(a) * r))
    d.polygon(pts, fill=WHITE); d.ellipse([4, 4, 7, 7], fill=grey(255))
    shapes.append(img)
    img = Image.new("RGBA", (12, 12), CLEAR); d = ImageDraw.Draw(img)
    d.rectangle([1, 1, 10, 10], fill=grey(200)); d.rectangle([2, 2, 9, 9], fill=WHITE); d.rectangle([4, 4, 7, 7], fill=grey(230))
    shapes.append(img)
    for i, img in enumerate(shapes):
        img.save(OUT + "Core%d.png" % i)
        print("wrote", OUT + "Core%d.png" % i)


# ---------------------------------------------------------------- fusion decorations
# DecorHead: 40x40, pivot (20, 29) = the neck; the head covers x 9..31, y 10..29 (facing right).
# DecorBack: 40x40, pivot (30, 20) = the upper back; it spreads left (behind the titan) and up.

ICE = (170, 225, 255, 255)
ICE_L = (235, 250, 255, 255)
ICE_D = (90, 150, 210, 255)
DARK = (40, 30, 56, 255)
DARK_L = (80, 62, 110, 255)
GOLD = (240, 200, 70, 255)
GOLD_L = (255, 235, 150, 255)
GOLD_D = (170, 125, 30, 255)
FEATHER = (245, 245, 250, 255)
FEATHER_D = (200, 205, 220, 255)


def decor_layers():
    head = {k: Image.new("RGBA", (40, 40), CLEAR) for k in "DG"}
    back = {k: Image.new("RGBA", (40, 40), CLEAR) for k in "DG"}
    return head, {k: ImageDraw.Draw(v) for k, v in head.items()}, back, {k: ImageDraw.Draw(v) for k, v in back.items()}


def outline_layer(img):
    a = img.getchannel("A").point(lambda v: 255 if v > 0 else 0)
    ring = ImageChops.subtract(a.filter(ImageFilter.MaxFilter(3)), a)
    out = Image.new("RGBA", img.size, CLEAR)
    out.paste(Image.new("RGBA", img.size, OUTLINE), (0, 0), ring)
    out.alpha_composite(img)
    return out


def flame(d, x, y, h, w, color):
    d.polygon([(x - w, y), (x - w // 2, y - h * 0.6), (x, y - h), (x + w // 2, y - h * 0.55), (x + w, y)], fill=color)


def decorations():
    for fusion in FUSIONS:
        head, hd, back, bd = decor_layers()
        if fusion == "None":
            hd["D"].line([13, 12, 10, 3], fill=METAL, width=1)
            hd["G"].ellipse([8, 1, 11, 4], fill=WHITE)
            bd["D"].polygon([(30, 12), (18, 10), (16, 16), (30, 18)], fill=METAL_D)
            for y in (12, 14, 16):
                bd["D"].line([19, y, 28, y], fill=JOINT)
        elif fusion == "Robot":
            for x0, x1 in ((13, 8), (17, 14)):
                hd["D"].line([x0, 11, x1, 1], fill=METAL)
                hd["G"].rectangle([x1 - 1, 0, x1 + 1, 2], fill=WHITE)
            hd["D"].polygon([(9, 18), (4, 12), (4, 22)], fill=METAL_D)
            hd["G"].line([5, 14, 5, 20], fill=WHITE)
            # Twin plasma thrusters with flames.
            for y in (10, 22):
                bd["D"].rectangle([16, y, 30, y + 6], fill=METAL_D)
                bd["D"].line([16, y, 30, y], fill=METAL)
                bd["D"].rectangle([12, y + 1, 16, y + 5], fill=VISOR)
                bd["G"].polygon([(12, y + 1), (2, y + 3), (12, y + 5)], fill=WHITE)
        elif fusion == "Blaze":
            flame(hd["G"], 20, 11, 11, 7, WHITE)
            flame(hd["G"], 13, 12, 8, 4, WHITE)
            hd["D"].polygon([(11, 12), (17, 10), (24, 11), (28, 13)], fill=(120, 40, 20, 255))
            # Flame wings.
            for i, (x, y, h) in enumerate(((22, 18, 17), (14, 22, 15), (8, 26, 11))):
                bd["D"].polygon([(30, 16 + i * 3), (x, y - h), (x - 6, y)], fill=(150, 50, 20, 255))
                bd["G"].polygon([(29, 17 + i * 3), (x + 1, y - h + 3), (x - 3, y - 1)], fill=WHITE)
        elif fusion == "Frost":
            for x, h in ((12, 8), (16, 12), (20, 15), (24, 11), (28, 7)):
                hd["D"].polygon([(x - 2, 12), (x, 12 - h), (x + 2, 12)], fill=ICE)
                hd["D"].line([x, 12 - h + 2, x, 11], fill=ICE_L)
                hd["G"].point((x, 12 - h + 1), fill=WHITE)
            for (x, y, h, w) in ((24, 16, 14, 3), (16, 18, 16, 4), (8, 22, 12, 3), (20, 26, 10, 3)):
                bd["D"].polygon([(30, y), (x, y - h), (x - w, y - h + 3), (28, y + 3)], fill=ICE)
                bd["D"].line([29, y + 1, x, y - h + 1], fill=ICE_L)
                bd["D"].line([28, y + 3, x - w, y - h + 3], fill=ICE_D)
                bd["G"].point((x, y - h), fill=WHITE)
        elif fusion == "Volt":
            for x0, x1 in ((12, 7), (27, 33)):
                hd["D"].line([x0, 12, x1, 3], fill=METAL, width=2)
                hd["D"].ellipse([x1 - 2, 1, x1 + 2, 5], fill=METAL_L)
            hd["G"].line([(7, 3), (13, 1), (17, 4), (23, 0), (27, 3), (33, 3)], fill=WHITE)
            # Tesla coils with arcs between them.
            for x in (24, 14):
                bd["D"].rectangle([x - 2, 8, x + 2, 26], fill=METAL_D)
                for y in range(9, 26, 3):
                    bd["D"].line([x - 2, y, x + 2, y], fill=(200, 140, 60, 255))
                bd["D"].ellipse([x - 4, 3, x + 4, 10], fill=METAL_L)
            bd["G"].line([(24, 6), (21, 3), (18, 7), (14, 5)], fill=WHITE)
            bd["G"].line([(14, 6), (9, 2), (6, 6), (2, 3)], fill=WHITE)
        elif fusion == "Shadow":
            for side in (0, 1):
                pts = [(12, 12), (6, 6), (3, -1), (8, 4), (15, 10)] if side == 0 else [(26, 11), (30, 5), (35, 0), (32, 7), (28, 12)]
                hd["D"].polygon(pts, fill=DARK)
                hd["D"].line(pts[:3], fill=DARK_L)
            hd["G"].line([20, 17, 30, 17], fill=WHITE)
            # Tattered void wings.
            pts = [(30, 14), (22, 2), (10, 0), (0, 6), (6, 10), (2, 16), (10, 18), (5, 26), (16, 24), (14, 33), (28, 22)]
            bd["D"].polygon(pts, fill=DARK)
            for a, b in ((22, 2), (10, 0)), ((6, 10), (2, 16)), ((10, 18), (5, 26)):
                bd["D"].line([30, 16, a[0], a[1]], fill=DARK_L)
            bd["G"].line([(22, 2), (10, 0), (0, 6)], fill=WHITE)
            bd["G"].line([(5, 26), (16, 24), (14, 33)], fill=WHITE)
        elif fusion == "Nova":
            hd["G"].ellipse([8, 1, 32, 7], outline=WHITE)
            for i in range(5):
                a = math.radians(i * 72 - 90)
                cx, cy = 20 + math.cos(a) * 12, 4 + math.sin(a) * 3
                hd["G"].polygon([(cx, cy - 2), (cx + 1, cy), (cx, cy + 2), (cx - 1, cy)], fill=WHITE)
            # Star wings: rays of light with stars at the ends.
            for i, (x, y) in enumerate(((6, 2), (2, 12), (4, 24), (14, 32))):
                bd["G"].line([30, 20, x + 2, y + 2], fill=WHITE)
                bd["G"].polygon([(x, y - 3), (x + 1, y - 1), (x + 3, y), (x + 1, y + 1), (x, y + 3), (x - 1, y + 1), (x - 3, y), (x - 1, y - 1)], fill=WHITE)
                bd["D"].line([30, 21, x + 3, y + 3], fill=(150, 60, 130, 255))
        elif fusion == "Omega":
            hd["D"].polygon([(9, 12), (9, 5), (13, 8), (16, 3), (20, 7), (24, 3), (27, 8), (31, 5), (31, 12)], fill=GOLD)
            hd["D"].line([9, 11, 31, 11], fill=GOLD_D)
            hd["D"].line([9, 5, 13, 8], fill=GOLD_L)
            for x in (12, 16, 20, 24, 28):
                hd["G"].rectangle([x, 8, x + 1, 9], fill=WHITE)
            # A fan of mechanical wing blades, each with a glowing edge.
            for i in range(5):
                a = math.radians(200 + i * 22)
                x, y = 30 + math.cos(a) * 30, 20 + math.sin(a) * 22
                bd["D"].polygon([(30, 18), (x, y), (x + 3, y + 3), (30, 22)], fill=(60, 56, 76, 255))
                bd["D"].line([30, 18, x, y], fill=GOLD)
                bd["G"].line([30 + (x - 30) * 0.4, 18 + (y - 18) * 0.4, x, y], fill=WHITE)
        elif fusion == "God":
            hd["G"].ellipse([9, 0, 31, 6], outline=WHITE, width=2)
            hd["D"].polygon([(9, 16), (3, 11), (5, 16), (2, 18), (9, 19)], fill=FEATHER)
            # Angel wings: layered white feathers.
            for row, (length, y0) in enumerate(((30, 4), (26, 12), (20, 20))):
                for f in range(4):
                    x = 30 - (f + 1) * length // 4
                    y = y0 + f * 2
                    bd["D"].polygon([(30, 16 + row * 3), (x, y), (x - 3, y + 5), (28, 20 + row * 3)], fill=FEATHER if row % 2 == 0 else FEATHER_D)
            bd["D"].line([30, 16, 2, 4], fill=FEATHER_D)
            bd["G"].line([(29, 15), (15, 6), (2, 3)], fill=WHITE)
        for slot, layers in (("DecorHead", head), ("DecorBack", back)):
            outline = outline_layer(layers["D"]) if layers["D"].getbbox() else layers["D"]
            outline.save(OUT + "%s%s_D.png" % (slot, fusion))
            layers["G"].save(OUT + "%s%s_G.png" % (slot, fusion))
        print("wrote", OUT + "Decor*%s" % fusion)


# ---------------------------------------------------------------- preview (mirrors TitanRenderer.BuildTitan, standing)

PIVOTS = dict(Torso=(17, 31), Pelvis=(13, 3), Head=(11, 19), Backpack=(14, 2), UpperArm=(5, 3), Forearm=(5, 3),
              Thigh=(6, 2), Shin=(6, 2), Foot=(6, 2), DecorHead=(20, 29), DecorBack=(30, 20))
WEAPON_PIVOTS = [(7, 3), (6, 3), (8, 3)]
HIP = 52


def tint(img, color):
    r, g, b, a = img.split()
    r = r.point(lambda v: v * color[0] // 255)
    g = g.point(lambda v: v * color[1] // 255)
    b = b.point(lambda v: v * color[2] // 255)
    return Image.merge("RGBA", (r, g, b, a))


def paste(canvas, name, joint, pivot, main, accent, glow, decor=False):
    x, y = round(joint[0] - pivot[0]), round(joint[1] - pivot[1])
    for layer, color in (("M", main), ("A", accent), ("D", None), ("G", glow)):
        path = OUT + "%s_%s.png" % (name, layer)
        if not os.path.exists(path):
            continue
        img = Image.open(path)
        if color is not None:
            img = tint(img, color)
        canvas.alpha_composite(img, (x, y))


def preview():
    colors = [(200, 205, 215), (230, 60, 60), (80, 230, 255)]
    looks = [(0, 5, 3, 0, 0, 0, "None"), (11, 7, 7, 2, 1, 1, "God"), (4, 6, 5, 1, 2, 2, "Blaze"),
             (9, 3, 3, 3, 0, 0, "Omega"), (2, 4, 3, 0, 2, 1, "Frost"), (1, 8, 8, 2, 1, 2, "Shadow")]
    palette = [(200, 205, 215), (60, 64, 76), (235, 235, 240), (80, 230, 255), (60, 110, 220), (230, 60, 60),
               (255, 140, 40), (255, 215, 70), (80, 210, 110), (160, 80, 230), (255, 120, 200), (30, 30, 36)]
    element_glow = {"None": (70, 225, 190), "God": (255, 215, 90), "Blaze": (255, 120, 35), "Omega": (255, 120, 220),
                    "Frost": (120, 215, 255), "Shadow": (155, 60, 230)}
    img = Image.new("RGBA", (80 * len(looks), 120 + 50), (26, 30, 44, 255))
    for n, (mc, ac, cc, shape, head, weapon, fusion) in enumerate(looks):
        main, accent, glow = palette[mc], palette[ac], palette[cc]
        feet = (40 + n * 80, 112)
        hip = (feet[0], feet[1] - HIP)

        def at(parent, pivot, attach):
            return (parent[0] + attach[0] - pivot[0], parent[1] + attach[1] - pivot[1])

        torso_p = PIVOTS["Torso"]
        eg = element_glow[fusion]
        # back to front, as in BuildTitan
        dd = at(hip, torso_p, (9, 8))
        for layer, color in (("D", None), ("G", eg)):
            im = Image.open(OUT + "DecorBack%s_%s.png" % (fusion, layer))
            if color:
                im = tint(im, color)
            img.alpha_composite(im, (dd[0] - 30, dd[1] - 20))
        for side, dark in ((-4, True), (4, False)):
            if side == 4:
                continue
            hj = (hip[0] + side, hip[1])
            m = tuple(int(c * 0.75) for c in main)
            a = tuple(int(c * 0.75) for c in accent)
            paste(img, "Thigh", hj, PIVOTS["Thigh"], m, a, glow)
            knee = at(hj, PIVOTS["Thigh"], (6, 24))
            paste(img, "Shin", knee, PIVOTS["Shin"], m, a, glow)
            paste(img, "Foot", at(knee, PIVOTS["Shin"], (6, 26)), PIVOTS["Foot"], m, a, glow)
        bs = at(hip, torso_p, (6, 6))
        paste(img, "UpperArm", bs, PIVOTS["UpperArm"], main, accent, glow)
        paste(img, "Forearm", at(bs, PIVOTS["UpperArm"], (5, 20)), PIVOTS["Forearm"], main, accent, glow)
        paste(img, "Backpack", at(hip, torso_p, (4, 6)), PIVOTS["Backpack"], main, accent, glow)
        paste(img, "Pelvis", hip, PIVOTS["Pelvis"], main, accent, glow)
        paste(img, "Torso", hip, torso_p, main, accent, glow)
        core = at(hip, torso_p, (17, 13))
        c = tint(Image.open(OUT + "Core%d.png" % shape), glow)
        img.alpha_composite(c, (core[0] - 6, core[1] - 6))
        neck = at(hip, torso_p, (17, 1))
        paste(img, "Head%d" % head, neck, PIVOTS["Head"], main, accent, glow)
        for layer, color in (("D", None), ("G", eg)):
            im = Image.open(OUT + "DecorHead%s_%s.png" % (fusion, layer))
            if color:
                im = tint(im, color)
            img.alpha_composite(im, (neck[0] - 20, neck[1] - 29))
        hj = (hip[0] + 4, hip[1])
        paste(img, "Thigh", hj, PIVOTS["Thigh"], main, accent, glow)
        knee = at(hj, PIVOTS["Thigh"], (6, 24))
        paste(img, "Shin", knee, PIVOTS["Shin"], main, accent, glow)
        paste(img, "Foot", at(knee, PIVOTS["Shin"], (6, 26)), PIVOTS["Foot"], main, accent, glow)
        fs = at(hip, torso_p, (28, 6))
        paste(img, "UpperArm", fs, PIVOTS["UpperArm"], main, accent, glow)
        wname = ["ArmCannon", "ArmBlade", "ArmClaw"][weapon]
        paste(img, wname, at(fs, PIVOTS["UpperArm"], (5, 20)), WEAPON_PIVOTS[weapon], main, accent, glow)
    # Car.
    bottom = (60, 165)
    paste(img, "CarBody", bottom, (50, 40), (200, 205, 215), (230, 60, 60), (80, 230, 255))
    wheel = Image.open(OUT + "Wheel_D.png")
    for wx in (21, 79):
        img.alpha_composite(wheel, (bottom[0] - 50 + wx - 9, bottom[1] - 40 + 32 - 9))
    img = img.resize((img.width * 3, img.height * 3), Image.NEAREST)
    img.save("tools/titan_preview_3x.png")
    print("wrote tools/titan_preview_3x.png")


# ---------------------------------------------------------------- items, buffs, ability icons

ELEMENT = {  # main, core, dark (matches Elements.cs)
    "Tech": ((70, 225, 190, 255), (210, 255, 245, 255), (25, 110, 95, 255)),
    "Plasma": ((90, 230, 255, 255), (225, 250, 255, 255), (30, 120, 170, 255)),
    "Holy": ((255, 215, 90, 255), (255, 252, 230, 255), (170, 125, 30, 255)),
    "Prism": ((255, 120, 220, 255), (255, 255, 255, 255), (110, 60, 150, 255)),
}
ELEMENT.update(gs.ELEMENT_COLORS)
RAINBOW = [c[0] for c in gs.ELEMENT_COLORS.values()]


def shift_trigger():
    img, d = gs.canvas(16, 16)
    main, core, dark = ELEMENT["Tech"]
    d.rounded_rectangle([3, 5, 12, 15], radius=2, fill=OUTLINE)
    d.rounded_rectangle([4, 6, 11, 14], radius=1, fill=(52, 58, 72, 255))
    d.line([4, 6, 4, 14], fill=(110, 120, 140, 255))
    d.rectangle([5, 0, 10, 5], fill=OUTLINE)
    d.rectangle([6, 1, 9, 4], fill=main)
    d.point((6, 1), fill=core)
    # The core, in the middle of the grip.
    d.ellipse([5, 8, 10, 13], fill=OUTLINE)
    d.ellipse([6, 9, 9, 12], fill=main)
    d.point((7, 10), fill=core)
    # Shift arrows either side.
    d.point((2, 9), fill=main); d.point((1, 10), fill=main); d.point((2, 11), fill=main)
    d.point((13, 9), fill=main); d.point((14, 10), fill=main); d.point((13, 11), fill=main)
    gs.save(img, "Content/Items/ShiftTrigger.png")

    def form(d):
        d.rounded_rectangle([3, 2, 12, 9], radius=2, fill=(200, 205, 215, 255))
        d.rectangle([6, 4, 12, 6], fill=VISOR); d.line([7, 5, 11, 5], fill=main)
        d.rectangle([2, 10, 13, 15], fill=(160, 166, 180, 255))
        d.ellipse([6, 10, 9, 13], fill=main); d.point((7, 11), fill=core)
    gs.buff_icon("Content/Buffs/ShiftTitanForm.png", form)


def titan_icons():
    """One symbol per ability kind, in the fusion's element colours, on a dark titan-core badge."""
    def badge(d, k):
        d.rounded_rectangle([0, 0, 15, 15], radius=3, fill=OUTLINE)
        d.rounded_rectangle([1, 1, 14, 14], radius=2, fill=(30, 34, 48, 255))

    def arm(d, m, c, k):
        d.rectangle([2, 4, 7, 11], fill=METAL); d.line([2, 4, 7, 4], fill=METAL_L)
        d.rectangle([7, 6, 12, 9], fill=m); d.line([7, 7, 13, 7], fill=c)
        d.polygon([(8, 12), (13, 13), (9, 14)], fill=METAL_L)

    def core_beam(d, m, c, k):
        d.ellipse([1, 4, 8, 11], fill=k); d.ellipse([2, 5, 7, 10], fill=m); d.point((4, 7), fill=c)
        d.rectangle([7, 6, 14, 9], fill=m); d.line([7, 7, 14, 7], fill=c); d.line([7, 8, 14, 8], fill=c)

    def stomp(d, m, c, k):
        d.rectangle([5, 1, 10, 8], fill=METAL); d.rectangle([4, 8, 11, 10], fill=METAL_L)
        d.line([1, 13, 14, 13], fill=(120, 80, 45, 255))
        d.arc([0, 9, 15, 17], 180, 360, fill=m); d.arc([3, 11, 12, 15], 180, 360, fill=c)

    def eruption(d, m, c, k):
        d.rectangle([1, 13, 14, 14], fill=(120, 80, 45, 255))
        for x, top in ((3, 5), (7, 2), (11, 6)):
            d.rectangle([x - 1, top, x + 1, 12], fill=m); d.line([x, top, x, 12], fill=c)

    def skyfall(d, m, c, k):
        for x, y in ((3, 2), (8, 4), (11, 1)):
            d.line([x, y, x + 2, y + 6], fill=m, width=2); d.ellipse([x + 1, y + 5, x + 4, y + 8], fill=c)

    def missiles(d, m, c, k):
        for x, y in ((3, 10), (7, 6), (11, 2)):
            d.line([x - 2, y + 3, x + 1, y], fill=m, width=2); d.point((x + 1, y), fill=c)
            d.point((x - 2, y + 4), fill=(255, 150, 40, 255))

    def satellites(d, m, c, k):
        d.ellipse([2, 2, 13, 13], outline=k); d.ellipse([6, 6, 9, 9], fill=m); d.point((7, 7), fill=c)
        for x, y in ((6, 1), (12, 6), (6, 12), (1, 6)):
            d.ellipse([x, y, x + 2, y + 2], fill=m); d.point((x + 1, y + 1), fill=c)

    def aura(d, m, c, k):
        d.ellipse([1, 1, 14, 14], outline=m); d.ellipse([3, 3, 12, 12], outline=k)
        d.rectangle([6, 4, 9, 11], fill=METAL); d.point((7, 6), fill=c)

    def overdrive(d, m, c, k):
        for a in range(0, 360, 45):
            r = math.radians(a)
            d.line([7.5 + math.cos(r) * 3, 7.5 + math.sin(r) * 3, 7.5 + math.cos(r) * 7, 7.5 + math.sin(r) * 7], fill=m)
        d.ellipse([4, 4, 11, 11], fill=m); d.ellipse([5, 5, 10, 10], fill=c)

    symbols = {"Arm": arm, "CoreBeam": core_beam, "Stomp": stomp, "Eruption": eruption, "Skyfall": skyfall,
               "MissilePods": missiles, "Satellites": satellites, "Aura": aura, "Overdrive": overdrive}
    for name, fusion, _base, suffix, _display, _tip in abilities():
        element = FUSIONS[fusion][1]
        main, core, dark = ELEMENT[element]
        img, d = gs.canvas(16, 16)
        badge(d, dark)
        symbols[suffix](d, main, core, dark)
        if element == "Prism":
            # Omega: a rainbow rim.
            for i, col in enumerate(RAINBOW):
                d.line([1 + i * 3, 14, 3 + i * 3, 14], fill=col)
        gs.save(img, "Content/Abilities/%s.png" % name)

    for name in ("TitanSlash", "TitanCarRam"):
        gs.invisible("Content/Projectiles/%s.png" % name)


# ---------------------------------------------------------------- laboratory tiles

def lab_tiles():
    os.makedirs("Content/Tiles", exist_ok=True)

    def tile_sheet(path, cell):
        # Every frame of the 16x15 tile sheet (16 px frames on an 18 px grid) gets the same cell, so the
        # blocks look like panels whatever frame the game picks.
        sheet = Image.new("RGBA", (288, 270), CLEAR)
        for fx in range(16):
            for fy in range(15):
                sheet.alpha_composite(cell, (fx * 18, fy * 18))
        sheet.save(path)
        print("wrote", path)

    cell = Image.new("RGBA", (16, 16), (96, 104, 122, 255)); d = ImageDraw.Draw(cell)
    d.rectangle([0, 0, 15, 15], outline=(58, 63, 78, 255))
    d.line([1, 1, 14, 1], fill=(150, 158, 176, 255)); d.line([1, 1, 1, 14], fill=(128, 136, 154, 255))
    d.line([1, 14, 14, 14], fill=(72, 78, 94, 255)); d.line([14, 1, 14, 14], fill=(72, 78, 94, 255))
    for x, y in ((3, 3), (12, 3), (3, 12), (12, 12)):
        d.point((x, y), fill=(170, 176, 190, 255))
    d.line([4, 8, 11, 8], fill=(80, 86, 102, 255))
    tile_sheet("Content/Tiles/LabPlating.png", cell)

    cell = Image.new("RGBA", (16, 16), (96, 104, 122, 255)); d = ImageDraw.Draw(cell)
    d.rectangle([0, 0, 15, 15], outline=(58, 63, 78, 255))
    d.rectangle([2, 5, 13, 10], fill=(40, 120, 150, 255))
    d.rectangle([3, 6, 12, 9], fill=(150, 240, 255, 255)); d.line([3, 7, 12, 7], fill=(235, 255, 255, 255))
    tile_sheet("Content/Tiles/LabLight.png", cell)

    def wall_sheet(path, cell):
        # 13x5 wall frames of 32 px on a 36 px grid.
        sheet = Image.new("RGBA", (468, 180), CLEAR)
        for fx in range(13):
            for fy in range(5):
                sheet.alpha_composite(cell, (fx * 36, fy * 36))
        sheet.save(path)
        print("wrote", path)

    cell = Image.new("RGBA", (32, 32), (44, 50, 64, 255)); d = ImageDraw.Draw(cell)
    for x0, y0 in ((0, 0), (16, 0), (0, 16), (16, 16)):
        d.rectangle([x0, y0, x0 + 15, y0 + 15], outline=(34, 38, 50, 255))
        d.line([x0 + 1, y0 + 1, x0 + 14, y0 + 1], fill=(58, 65, 82, 255))
    d.line([0, 15, 31, 15], fill=(30, 34, 44, 255))
    wall_sheet("Content/Tiles/LabWall.png", cell)

    cell = Image.new("RGBA", (32, 32), (40, 150, 150, 255)); d = ImageDraw.Draw(cell)
    for y in range(0, 32, 4):
        d.line([0, y, 31, y], fill=(52, 175, 170, 255))
    for x, y in ((6, 26), (20, 18), (12, 8), (26, 4)):
        d.ellipse([x - 1, y - 1, x + 1, y + 1], fill=(150, 250, 235, 255))
    wall_sheet("Content/Tiles/LabTankWall.png", cell)

    img, d = gs.canvas(16, 16)
    d.rounded_rectangle([1, 3, 14, 14], radius=2, fill=OUTLINE)
    d.rounded_rectangle([2, 4, 13, 13], radius=1, fill=(96, 104, 122, 255))
    d.rectangle([4, 6, 6, 12], fill=(40, 150, 150, 255)); d.rectangle([9, 6, 11, 12], fill=(40, 150, 150, 255))
    d.point((5, 8), fill=(150, 250, 235, 255)); d.point((10, 10), fill=(150, 250, 235, 255))
    d.line([7, 0, 7, 3], fill=OUTLINE); d.point((7, 0), fill=(90, 240, 255, 255))
    gs.save(img, "Content/Tiles/LabMapIcon.png")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    torso(); pelvis(); backpack(); heads(); arms(); weapons(); legs(); car(); cores(); decorations()
    shift_trigger(); titan_icons(); lab_tiles()
    preview()
