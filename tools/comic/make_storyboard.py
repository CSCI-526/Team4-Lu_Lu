"""Draws the intro comic's storyboard draft: 5 rough 1280x720 panels and one review sheet.

The characters come from the approved kit (``make_characters.py``), posed by the
same rig; the few poses the panels need beyond the kit (a hungry stand, a point,
a wave) are added here. Backgrounds are rough blocks in the kit's style, flat
fills under a dark brown outline, meant only to judge staging, pacing, and the
captions before the final panels are drawn.

Writes ``storyboard/Panel_1.png`` .. ``Panel_5.png`` and ``storyboard/storyboard.png``.
Run: ``python tools/comic/make_storyboard.py`` (needs what make_characters.py needs).
"""

import base64
import math
import random
import re
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import make_characters as mc  # noqa: E402
from PIL import Image  # noqa: E402

OUT_DIR = HERE / "storyboard"
W, H = 1280, 720
BROWN, SHADOW, GOLD, CREAM, PANEL, BACKGROUND = mc.BROWN, mc.SHADOW, mc.GOLD, mc.CREAM, mc.PANEL, mc.BACKGROUND
ROUND = 'stroke-linecap="round" stroke-linejoin="round"'

# backgrounds
SKY_NOON = ["#F2A65A", "#F5BD6E", "#F8D38C"]
SKY_DUSK = ["#D9774A", "#EB9A55", "#F5C070"]
SKY_DAWN = ["#E89A7E", "#F2B98A", "#F9DCA6"]
HILL_FAR, HILL_NEAR, GROUND, GROUND_DUSK = "#D9A066", "#C98A4E", "#B97A40", "#A86A35"
DEAD_WOOD, TRUNK, BARK = "#8C6B50", "#6E4A2E", "#4E3220"
FOREST_BACK, FOREST_FRONT, FOREST_LIGHT = "#1E5B3B", "#2C7C4A", "#3E9A55"
JUNGLE = ["#0A1F12", "#0F2E1C", "#15402A", "#1B5236"]
BANANA = "#F7D04A"

# ---------------------------------------------------------------- extra poses

mc.POSES.update({
    "StandHungry": dict(mirror=False, items=[
        ("Tail", "tail", 14, (0, 0)),
        ("LegRight", "hipRight", 4, (0, 0)),
        ("LegLeft", "hipLeft", -4, (0, 0)),
        ("ArmRight", "shoulderRight", 8, (0, 0)),
        ("ArmLeft", "shoulderLeft", -8, (0, 0)),
        ("Body", "body", 0, (0, 0)),
        ("Head", "neck", 7, (0, 7)),
        ("Face_Hungry", "on:Head", 0, (0, 4)),
    ]),
})

CHAR = {c["name"]: c for c in mc.CHARACTERS}
PARTS = {name: mc.part_set(c) for name, c in CHAR.items()}
# the family watches him go from behind, so every member gets the hero's back parts
for _name, _c in CHAR.items():
    PARTS[_name].setdefault("HeadBack", mc.head(_c, back=True))
    PARTS[_name].setdefault("BodyBack", mc.body(_c, back=True))


def place(svg, x, y, s, ground):
    return f'<g transform="translate({x} {y}) scale({s}) translate(0 {-ground:.1f})">\n{svg}\n</g>'


def figure(name, pose, x, y, s):
    """A family member standing (or sitting) on the ground point (x, y), s times the kit size."""
    c = CHAR[name]
    ground = (40 if pose.startswith("Sit") else 53) * c["body"]
    return place(mc.pose_svg(c, PARTS[name], pose), x, y, s, ground)


def back_figure(name, x, y, s, arm_right=24, arm_left=-24, head_rot=0):
    """A family member from behind, standing still, with the props that show from behind."""
    c = CHAR[name]
    items = [dict(part=p, at=a, rot=r, nudge=(0, 0)) for p, a, r in (
        ("LegRight", "hipRight", 4), ("LegLeft", "hipLeft", -4),
        ("ArmRight", "shoulderRight", arm_right), ("ArmLeft", "shoulderLeft", arm_left),
        ("BodyBack", "body", 0), ("Tail", "tailBack", 24), ("HeadBack", "neck", head_rot))]
    for prop in mc.PROPS[name]:
        if prop["part"] == "Cane":
            items.insert(3, dict(part="Cane", at="hand:ArmLeft", rot=0, nudge=(0, 0)))
        elif prop["at"] == "on:Head":
            items.append(dict(part=prop["part"], at="on:HeadBack", rot=0, nudge=prop["nudge"]))
    return place(mc.compose(c, PARTS[name], items, mirror=True), x, y, s, 53 * c["body"])


def hand_point(name, at, rot, x, y, s, mirror=False):
    """Where a hand lands in the panel (its arm at a socket, turned rot), for marks beside it."""
    c = CHAR[name]
    sx, sy = mc.socket(c, at)
    r, k = math.radians(rot), mc.HAND_Y * c["body"]
    hx, hy = sx - math.sin(r) * k, sy + math.cos(r) * k
    return x + s * (-hx if mirror else hx), y + s * (hy - 53 * c["body"])


def wave_marks(x, y, side):
    out = []
    for r, a0, a1 in ((26, -60, 10), (40, -55, 5)):
        pts = [(x + side * r * math.cos(math.radians(a)), y + r * math.sin(math.radians(a)))
               for a in (a0, (a0 + a1) / 2, a1)]
        out.append(f'<path d="{smooth(pts)}" fill="none" stroke="{BROWN}" stroke-width="4.5" {ROUND}/>')
    return "".join(out)


HEAD_DROP = 36  # how far the head sits down in its leaves, in kit units


def cobra_hiding(x, y, s, low=False):
    """The kit's cobra head peeking from leaves at the tree's foot. Unlike Cobra_InBush there
    is no dark bush under it: the leaves fan out wide so the head melts into the undergrowth.
    low keeps the leaves down by the head's sides, a wide low clump with nothing tall above it."""
    rnd = random.Random(91)
    out = [] if low else [mc.twig(-118, 70, -52, -205, -28, 9), mc.twig(172, 60, 132, -120, 22, 7)]
    # an outer ring along the ground, leaning outward, so the clump spreads into the undergrowth
    ring = (((-300, 220, -170), (-230, 205, -152), (-150, 190, -142), (150, 190, -38),
             (230, 205, -28), (300, 220, -10), (-260, 240, 176), (260, 240, 4)) if low else
            ((-300, 210, -160), (-230, 190, -132), (-150, 170, -112), (150, 170, -68),
             (230, 190, -48), (300, 210, -20), (-260, 240, 176), (260, 240, 4)))
    for bx, by, a in ring:
        L = rnd.uniform(230, 300)
        out.append(mc.leaf(bx, by, a + rnd.uniform(-6, 6), L, L * 0.3, rnd.choice(mc.BACK_LEAVES), "#2C7C4A"))
    for a in range(-176, 0, 16):  # behind the head, fanned all round
        L = rnd.uniform(240, 330)
        if low:  # short where it would stand up over the head, long out to the sides
            L = 100 + 190 * abs(math.cos(math.radians(a))) + (L - 285) / 4
        out.append(mc.leaf(rnd.uniform(-30, 30), rnd.uniform(60, 100), a + rnd.uniform(-5, 5), L, L * 0.28,
                           rnd.choice(mc.BACK_LEAVES), "#2C7C4A"))
    # a fill behind the chin, so no bark shows between the front leaves
    for a, tones in ((-122, mc.BACK_LEAVES[1]), (-102, mc.BACK_LEAVES[2]), (-78, mc.BACK_LEAVES[0]),
                     (-58, mc.BACK_LEAVES[3]), (-90, mc.BACK_LEAVES[1])):
        out.append(mc.leaf(0, 280, a, 220, 64, tones, "#2C7C4A"))
    out.append(f'<g transform="translate(0 {HEAD_DROP})">{mc.cobra_head()}</g>')  # sunk into the leaves
    for lx, ly, a, length, width, tones, edge in (  # in front: over the head's lower edge, out to the sides
            (-120, 250, 186, 260, 70, mc.BACK_LEAVES[2], False), (120, 250, -6, 260, 70, mc.BACK_LEAVES[0], False),
            (-40, 226, -168, 300, 72, mc.FRONT_LEAVES[1], True), (40, 226, -12, 300, 72, mc.FRONT_LEAVES[1], True),
            (-216, 19, 22, 190, 50, mc.FRONT_LEAVES[1], True), (219, 14, 156, 192, 50, mc.FRONT_LEAVES[1], True),
            (-20, 244, -102, 150, 42, mc.FRONT_LEAVES[0], True), (20, 244, -78, 150, 42, mc.FRONT_LEAVES[0], True),
            (-6, 182, -137, 142, 44, mc.FRONT_LEAVES[0], True), (-2, 182, -36, 143, 44, mc.FRONT_LEAVES[0], True),
            (0, 254, -150, 250, 60, mc.FRONT_LEAVES[0], True), (0, 254, -30, 250, 60, mc.FRONT_LEAVES[0], True)):
        out.append(mc.leaf(lx, ly, a, length, width, tones, "#438B50", edge=edge))
    return f'<g transform="translate({x} {y}) scale({s})">\n{"".join(out)}\n</g>'


# ---------------------------------------------------------------- drawing

def f1(v):
    return f"{v:.1f}".rstrip("0").rstrip(".")


def poly_d(pts, close=False):
    return "M " + " L ".join(f"{f1(x)} {f1(y)}" for x, y in pts) + (" Z" if close else "")


def smooth(pts):
    """A Catmull-Rom curve through the points, as cubic Beziers."""
    d = f"M {f1(pts[0][0])} {f1(pts[0][1])}"
    for i in range(len(pts) - 1):
        p0, p1, p2, p3 = pts[max(i - 1, 0)], pts[i], pts[i + 1], pts[min(i + 2, len(pts) - 1)]
        c1 = (p1[0] + (p2[0] - p0[0]) / 6, p1[1] + (p2[1] - p0[1]) / 6)
        c2 = (p2[0] - (p3[0] - p1[0]) / 6, p2[1] - (p3[1] - p1[1]) / 6)
        d += f" C {f1(c1[0])} {f1(c1[1])} {f1(c2[0])} {f1(c2[1])} {f1(p2[0])} {f1(p2[1])}"
    return d


def area(d, fill, w=6, extra=""):
    stroke = f' stroke="{BROWN}" stroke-width="{w}" {ROUND}' if w else ""
    return f'<path d="{d}" fill="{fill}"{stroke}{extra}/>'


def with_shadow(svg, dx=6, dy=7):
    flat = re.sub(r'(fill|stroke)="#[0-9A-Fa-f]{6}"', lambda m: f'{m.group(1)}="{SHADOW}"', svg)
    return f'<g transform="translate({dx} {dy})">{flat}</g>{svg}'


def merged(items, fill, w=6):
    """Shapes that share one outline: every outline first, then every fill on top.
    An item is ("tube", points, width) or ("area", path d)."""
    under, over = [], []
    for item in items:
        if item[0] == "tube":
            d = poly_d(item[1]) if not isinstance(item[1], str) else item[1]
            under.append(f'<path d="{d}" fill="none" stroke="{BROWN}" stroke-width="{item[2] + 2 * w}" {ROUND}/>')
            over.append(f'<path d="{d}" fill="none" stroke="{fill}" stroke-width="{item[2]}" {ROUND}/>')
        else:
            under.append(f'<path d="{item[1]}" fill="{BROWN}" stroke="{BROWN}" stroke-width="{2 * w}" {ROUND}/>')
            over.append(f'<path d="{item[1]}" fill="{fill}"/>')
    return "\n".join(under + over)


def sky(colors, edges):
    """Flat bands, top to the horizon, each edge gently wavy."""
    out = [f'<rect x="0" y="0" width="{W}" height="{H}" fill="{colors[0]}"/>']
    for color, y in zip(colors[1:], edges):
        out.append(ridge(y, 6, 230, y * 0.01, color, w=0))
    return "\n".join(out)


def ridge(y, amp, period, phase, fill, w=6, bottom=H + 40, extra=""):
    pts = [(x, y + amp * math.sin(x / period + phase) + amp * 0.45 * math.sin(x / (period * 0.43) + phase * 2.3))
           for x in range(-60, W + 61, 40)]
    return area(smooth(pts) + f" L {W + 60} {bottom} L -60 {bottom} Z", fill, w, extra)


def bumps(x0, x1, y, r, fill, seed, bottom=H + 40, w=6, down=False, extra=""):
    """A row of round crowns along y, closed down to bottom (or hanging from it, down=True)."""
    rnd = random.Random(seed)
    d, x = f"M {f1(x0)} {f1(bottom)} L {f1(x0)} {f1(y)}", x0
    while x < x1:
        bw, bh = r * rnd.uniform(1.4, 2.4), r * rnd.uniform(0.6, 1.1)
        nx, ny = min(x + bw, x1), y + rnd.uniform(-r * 0.2, r * 0.2)
        d += f" A {f1(bw / 2)} {f1(bh)} 0 0 {0 if down else 1} {f1(nx)} {f1(ny)}"
        x = nx
    return area(d + f" L {f1(x1)} {f1(bottom)} Z", fill, w, extra)


def sun(x, y, r, rays=True):
    out = []
    if rays:
        for i in range(12):
            a = math.radians(i * 30 + 15)
            pts = [(x + math.cos(a + t) * (r + 18), y + math.sin(a + t) * (r + 18)) for t in (-0.13, 0.13)]
            tip = (x + math.cos(a) * (r + 54), y + math.sin(a) * (r + 54))
            out.append(area(poly_d([pts[0], tip, pts[1]], True), GOLD, 5))
    out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#FFE29A" stroke="{BROWN}" stroke-width="6"/>')
    out.append(f'<circle cx="{x}" cy="{y}" r="{r - 16}" fill="#FFF3D4"/>')
    return "\n".join(out)


def cracks(seed, count, y0, y1, x0=20, x1=W - 20):
    rnd = random.Random(seed)
    out = []
    for _ in range(count):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        pts = [(x, y)]
        for _ in range(rnd.randint(3, 5)):
            x += rnd.uniform(18, 46) * rnd.choice((-1, 1))
            y += rnd.uniform(-10, 10)
            pts.append((x, y))
        out.append(f'<path d="{poly_d(pts)}" fill="none" stroke="{BROWN}" stroke-width="3.5" {ROUND}/>')
        bx, by = pts[len(pts) // 2]
        out.append(f'<path d="{poly_d([(bx, by), (bx + rnd.uniform(-20, 20), by + rnd.uniform(8, 18))])}" '
                   f'fill="none" stroke="{BROWN}" stroke-width="3" {ROUND}/>')
    return "\n".join(out)


DEAD_TREE = [
    ([(0, 0), (-4, -120), (6, -230)], 54),
    ([(6, -230), (10, -300), (0, -360)], 30),
    ([(4, -200), (70, -270), (140, -300)], 24),
    ([(140, -300), (185, -345)], 12),
    ([(100, -284), (118, -340)], 10),
    ([(2, -170), (-70, -240), (-130, -262)], 22),
    ([(-130, -262), (-172, -300)], 10),
    ([(-90, -250), (-100, -310)], 10),
    ([(0, -360), (-30, -410)], 12),
    ([(0, -360), (32, -402)], 10),
]


def dead_tree(x, y, s):
    items = [("area", poly_d([(-56, 6), (-24, -22), (24, -22), (60, 6)], True))]
    items += [("tube", pts, w) for pts, w in DEAD_TREE]
    return f'<g transform="translate({x} {y}) scale({s})">{with_shadow(merged(items, DEAD_WOOD))}</g>'


def dry_leaf(x, y, angle, s=1.0):
    d = "M 0,0 C 8,-9 22,-9 30,0 C 22,9 8,9 0,0 Z"
    return (f'<g transform="translate({x} {y}) rotate({angle}) scale({s})">'
            f'{area(d, "#B88A51", 3.5)}<path d="M 3,0 L 26,0" stroke="{BROWN}" stroke-width="2"/></g>')


def far_forest(x0, x1, y, scale, seed, giant_x=None):
    """The rainforest on the horizon: two rows of crowns, one giant tree, a mist strip."""
    out = []
    if giant_x is not None:
        top = y - 210 * scale
        out.append(merged([("tube", [(giant_x, y), (giant_x + 6 * scale, top + 30 * scale)], 22 * scale)],
                          "#5A3A24", 5))
        crown = [("area", f"M {f1(giant_x + dx * scale)} {f1(top + dy * scale)} m -{f1(r * scale)} 0 "
                          f"a {f1(r * scale)} {f1(r * scale)} 0 1 0 {f1(2 * r * scale)} 0 "
                          f"a {f1(r * scale)} {f1(r * scale)} 0 1 0 -{f1(2 * r * scale)} 0")
                 for dx, dy, r in ((-40, 6, 44), (0, -18, 52), (44, 4, 44), (6, 22, 40))]
        out.append(merged(crown, FOREST_FRONT, 5))
        for dx, dy in ((-30, 0), (10, -26), (36, 10), (-8, 20), (22, -4)):
            out.append(f'<circle cx="{f1(giant_x + dx * scale)}" cy="{f1(top + dy * scale)}" '
                       f'r="{f1(6 * scale)}" fill="{BANANA}" stroke="{BROWN}" stroke-width="2.5"/>')
    out.append(bumps(x0, x1, y - 40 * scale, 34 * scale, FOREST_BACK, seed, bottom=y + 30, w=5))
    out.append(bumps(x0 - 20, x1 + 20, y - 8 * scale, 30 * scale, FOREST_FRONT, seed + 1, bottom=y + 30, w=5))
    out.append(ridge(y + 14, 3, 120, seed, CREAM, w=0, bottom=y + 34, extra=' opacity="0.3"'))
    return "\n".join(out)


def bowl(x, y, s):
    shell = area("M -50,0 A 50 36 0 0 0 50,0 Z", TRUNK, 5)
    rim = (f'<ellipse cx="0" cy="0" rx="50" ry="11" fill="#EAD9B0" stroke="{BROWN}" stroke-width="5"/>'
           f'<ellipse cx="0" cy="1" rx="40" ry="6" fill="{DEAD_WOOD}"/>')
    return f'<g transform="translate({x} {y}) scale({s})">{with_shadow(shell + rim, 5, 6)}</g>'


def grumble(x, y, s=1.0):
    """Comic hunger marks: three short curves."""
    out = []
    for i, (dx, dy) in enumerate(((0, 0), (12, 16), (0, 32))):
        out.append(f'<path d="M {dx},{dy} q 10,-6 20,0" fill="none" stroke="{BROWN}" stroke-width="4" {ROUND}/>')
    return f'<g transform="translate({x} {y}) scale({s})">{"".join(out)}</g>'


# the game's banana (Sprites/ItemBanana): long curved fingers in a hand, brown stem and
# tips, a darker underside; here in the kit's flat fills and brown outline
BANANA_SHADE, BANANA_LIGHT, BANANA_TIP, BANANA_NECK = "#E2A72E", "#FFEB8A", "#4A3020", "#8C7A36"
BANANA_FINGER = "M 0 0 C -40 50 -120 52 -150 6 L -157 -4 C -118 24 -46 22 -4 -26 Z"
BANANA_UNDER = "M 0 0 C -40 50 -120 52 -150 6 C -116 34 -44 32 -2 -8 Z"


def banana(x, y, angle, s):
    """One finger, its stem end at (0, 0), its tip to the left."""
    return (f'<g transform="translate({f1(x)} {f1(y)}) rotate({angle}) scale({s})">'
            f'<path d="{BANANA_FINGER}" fill="{BANANA}"/><path d="{BANANA_UNDER}" fill="{BANANA_SHADE}"/>'
            f'<path d="M -24 -12 C -54 10 -102 16 -134 3" fill="none" stroke="{BANANA_LIGHT}" stroke-width="6" {ROUND}/>'
            f'<path d="{BANANA_FINGER}" fill="none" stroke="{BROWN}" stroke-width="5" {ROUND}/>'
            f'<path d="M -150 6 L -157 -4 L -146 -7 Z" fill="{BANANA_TIP}" stroke="{BANANA_TIP}" stroke-width="5" {ROUND}/>'
            f'<path d="M 0 0 L -4 -26 L 9 -21 L 11 -4 Z" fill="{BANANA_NECK}" stroke="{BROWN}" stroke-width="4" {ROUND}/>'
            f'</g>')


def banana_bunch(x, y, s, flip=False, glow=True, stalk=60):
    """A hand of four fingers fanned from one crown, hanging on a stalk from (0, -stalk)."""
    fruit = [merged([("tube", [(6, -2), (10, -stalk)], 14)], BANANA_NECK, 4)]
    for i, (angle, k) in enumerate(((18, 0.84), (8, 0.92), (-2, 1.0), (-12, 1.06))):
        fruit.append(banana(2 * i, -6 + 17 * i, angle, k))
    fruit.append(f'<ellipse cx="8" cy="8" rx="13" ry="30" fill="{BANANA_NECK}" stroke="{BROWN}" stroke-width="4"/>')
    body = "".join(fruit)
    if flip:
        body = f'<g transform="scale(-1 1)">{body}</g>'
    halo = ""
    if glow:
        cx = 70 if flip else -70
        halo = (f'<circle cx="{cx}" cy="30" r="150" fill="{CREAM}" opacity="0.10"/>'
                f'<circle cx="{cx}" cy="30" r="100" fill="{CREAM}" opacity="0.14"/>')
    return f'<g transform="translate({x} {y}) scale({s})">{halo}{with_shadow(body, 5, 6)}</g>'


# the loading page's vines (Sprites/TitleScreen), as they are, or recolored for depth
SPRITES = mc.REPO / "Assets" / "_Monkobra" / "Sprites" / "TitleScreen"
_SPRITE_CACHE = {}


def vine(name, x, y, h, tint=None):
    if name not in _SPRITE_CACHE:
        path = SPRITES / f"{name}.png"
        _SPRITE_CACHE[name] = (base64.b64encode(path.read_bytes()).decode(), Image.open(path).size)
    data, (pw, ph) = _SPRITE_CACHE[name]
    filt = f' filter="url(#{tint})"' if tint else ""
    return (f'<image href="data:image/png;base64,{data}" x="{x}" y="{y}" width="{f1(h * pw / ph)}" '
            f'height="{h}" preserveAspectRatio="none"{filt}/>')


DEFS = (f'<defs>'
        f'<filter id="vineLit"><feFlood flood-color="#3E8A55"/><feComposite in2="SourceAlpha" operator="in"/></filter>'
        f'<filter id="vineDeep"><feFlood flood-color="#17432B"/><feComposite in2="SourceAlpha" operator="in"/></filter>'
        f'<linearGradient id="shaft" x1="0" y1="0" x2="0" y2="1">'
        f'<stop offset="0" stop-color="{CREAM}" stop-opacity="0.16"/>'
        f'<stop offset="1" stop-color="{CREAM}" stop-opacity="0"/></linearGradient>'
        f'</defs>')


def leaves(seed, count, x0, x1, y0, y1, angles, length, tones, rib="#2E6B47", edge=False):
    """A scatter of the kit's leaves, for canopy and undergrowth."""
    rnd = random.Random(seed)
    out = []
    for _ in range(count):
        L = rnd.uniform(*length)
        out.append(mc.leaf(rnd.uniform(x0, x1), rnd.uniform(y0, y1), rnd.uniform(*angles), L, L * 0.3,
                           rnd.choice(tones), rib, edge=edge, veins=edge))
    return "\n".join(out)


def fern(x, y, s, seed, tones=None):
    rnd = random.Random(seed)
    out = []
    for a in range(-170, -5, 20):
        L = rnd.uniform(110, 150)
        out.append(mc.leaf(0, 0, a + rnd.uniform(-6, 6), L, L * 0.2, tones or mc.FRONT_LEAVES[1], "#5FA36E",
                           edge=True, veins=False))
    return f'<g transform="translate({x} {y}) scale({s})">{"".join(out)}</g>'


def giant_trunk():
    """The rainforest giant: plank roots, bark, one branch to the right, a liana across."""
    left = smooth([(420, H + 30), (476, 640), (540, 470), (560, 300), (566, 120), (570, -30)])
    right = smooth([(752, -30), (750, 120), (756, 300), (782, 470), (850, 640), (910, H + 30)])
    trunk = left + " " + right.replace("M", "L", 1) + " Z"
    roots = ["M 546 430 C 520 540 420 640 250 " + str(H + 30) + " L 520 " + str(H + 30) + " Z",
             "M 778 420 C 800 540 900 640 1060 " + str(H + 30) + " L 800 " + str(H + 30) + " Z"]
    items = [("area", trunk)] + [("area", r) for r in roots]
    items.append(("tube", smooth([(740, 200), (820, 160), (900, 150), (990, 128)]), 30))
    out = [merged(items, TRUNK)]
    for pts in (((600, -30), (596, 140), (606, 330), (590, 560)), ((660, -30), (668, 200), (660, 420)),
                ((712, 10), (706, 230), (724, 470), (760, 640)), ((520, 560), (420, 680)),
                ((810, 540), (900, 660)), ((632, 360), (640, 600), (630, 700))):
        out.append(f'<path d="{smooth(list(pts))}" fill="none" stroke="{BARK}" stroke-width="6" {ROUND}/>')
    for pts in (((560, 40), (640, 70), (700, 110), (754, 150)), ((552, 300), (630, 330), (720, 390), (776, 440))):
        out.append(merged([("tube", smooth(list(pts)), 13)], "#2C7C4A", 4))
    for x, y, a in ((612, 62, -120), (690, 104, 60), (600, 322, -110), (700, 380, 70), (742, 410, -40)):
        out.append(mc.leaf(x, y, a, 44, 16, mc.FRONT_LEAVES[0], "#5FA36E", edge=True, veins=False))
    return "\n".join(out)


def rounded(x, y, w, h, r):
    return (f"M {x + r} {y} h {w - 2 * r} a {r} {r} 0 0 1 {r} {r} v {h - 2 * r} a {r} {r} 0 0 1 {-r} {r} "
            f"h {-(w - 2 * r)} a {r} {r} 0 0 1 {-r} {-r} v {-(h - 2 * r)} a {r} {r} 0 0 1 {r} {-r} Z")


def tale_bubble(x, y, w, h, tail):
    """The father's speech bubble, holding a picture of the rainforest he tells of."""
    tx, ty = tail
    base = tx - 14
    tail_d = poly_d([(base - 26, y + h - 8), (tx, ty), (base + 26, y + h - 8)], True)
    frame = with_shadow(merged([("area", rounded(x, y, w, h, 34)), ("area", tail_d)], CREAM, 5), 5, 6)
    ix, iy, iw, ih = x + 14, y + 14, w - 28, h - 28
    v = [f'<rect width="{iw}" height="{ih}" fill="#D5EFD0"/>',
         f'<rect y="{ih * 0.45}" width="{iw}" height="{ih}" fill="#EAF6DA"/>',
         bumps(-20, iw + 20, ih * 0.5, 22, "#9BCB97", 61, bottom=ih + 20, w=0),
         merged([("tube", [(iw * 0.5, ih + 10), (iw * 0.5 + 4, ih * 0.22)], 24)], TRUNK, 4),
         merged([("area", f"M {iw * 0.5 + dx - r} {ih * 0.2 + dy} a {r} {r} 0 1 0 {2 * r} 0 a {r} {r} 0 1 0 {-2 * r} 0")
                 for dx, dy, r in ((-46, 8, 36), (0, -14, 44), (46, 6, 36), (4, 18, 34))], FOREST_FRONT, 4),
         bumps(-20, iw + 20, ih * 0.62, 20, FOREST_BACK, 62, bottom=ih + 20, w=4),
         bumps(-20, iw + 20, ih * 0.78, 22, FOREST_FRONT, 63, bottom=ih + 20, w=4),
         ridge(ih * 0.86, 2, 60, 1.0, CREAM, w=0, bottom=ih * 0.92, extra=' opacity="0.35"'),
         vine("Vine_Curl", 14, -14, 120, "vineLit"), vine("Vine_A", iw - 70, -18, 140, "vineLit")]
    # the fruit, too far off to make out: yellow dots on the crown
    for dx, dy in ((-50, 10), (-22, -10), (12, 2), (44, 16), (-34, 30), (2, 30), (30, -14), (60, -2)):
        v.insert(5, f'<circle cx="{f1(iw * 0.5 + dx)}" cy="{f1(ih * 0.2 + dy)}" r="6.5" fill="{BANANA}" '
                    f'stroke="{BROWN}" stroke-width="2.5"/>')
    clip = f'<clipPath id="tale"><path d="{rounded(0, 0, iw, ih, 24)}"/></clipPath>'
    inner = (f'<g transform="translate({ix} {iy})"><defs>{clip}</defs><g clip-path="url(#tale)">{"".join(v)}</g>'
             f'<path d="{rounded(0, 0, iw, ih, 24)}" fill="none" stroke="{BROWN}" stroke-width="4"/></g>')
    return frame + inner


def caption(lines):
    h = 30 + 38 * len(lines)
    y = H - 22 - h
    text = "".join(f'<text x="{W / 2}" y="{y + 44 + 38 * i}" class="cap">{t}</text>' for i, t in enumerate(lines))
    arrow = area(poly_d([(1124, y + h - 34), (1144, y + h - 24), (1124, y + h - 14)], True), GOLD, 4)
    return (f'<rect x="120" y="{y}" width="1040" height="{h}" rx="22" fill="{PANEL}" opacity="0.78"/>'
            f'<rect x="120" y="{y}" width="1040" height="{h}" rx="22" fill="none" stroke="{GOLD}" stroke-width="4"/>'
            f'{text}{arrow}')


def bubble(x, y, w, h, tail, text):
    tx, ty = tail
    body = f"M {x - w / 2 + 30} {y - h / 2} " \
           f"h {w - 60} a 30 30 0 0 1 30 30 v {h - 60} a 30 30 0 0 1 -30 30 h {-(w - 60)} " \
           f"a 30 30 0 0 1 -30 -30 v {-(h - 60)} a 30 30 0 0 1 30 -30 Z"
    base = x + (tx - x) * 0.3
    tail_d = poly_d([(base - 22, y + h / 2 - 6), (tx, ty), (base + 22, y + h / 2 - 6)], True)
    shape = merged([("area", body), ("area", tail_d)], CREAM, 5)
    return with_shadow(shape, 5, 6) + f'<text x="{x}" y="{y + 15}" class="say">{text}</text>'


# ---------------------------------------------------------------- the panels

def panel_drought():
    out = [sky(SKY_NOON, (170, 300)), sun(220, 140, 64),
           ridge(372, 16, 150, 0.4, HILL_FAR), ridge(420, 12, 120, 2.0, HILL_NEAR),
           ridge(468, 5, 200, 1.0, GROUND), cracks(3, 11, 490, 700),
           dead_tree(1060, 585, 1.0)]
    out += [dry_leaf(*a) for a in ((1150, 330, 20), (880, 300, -30, 0.9), (1210, 460, 70, 0.8))]
    out += [figure("Hero", "SitSlumped", 470, 585, 0.8), figure("Sister", "SitSlumped", 620, 585, 0.8),
            figure("Mother", "SitSlumped", 770, 585, 0.8), figure("Father", "SitSlumped", 945, 585, 0.8)]
    out.append(caption(["The rains never came. The trees went bare, and the family went hungry."]))
    return out


def panel_sister():
    out = [sky(SKY_NOON, (150, 280)), ridge(350, 14, 150, 1.2, HILL_FAR), ridge(392, 10, 120, 0.3, HILL_NEAR),
           ridge(430, 5, 200, 2.0, GROUND), cracks(8, 8, 450, 700, 160, 1240),
           with_shadow(merged([("tube", [(70, H + 40), (58, 300), (64, -40)], 130),
                               ("tube", [(90, 220), (170, 150), (220, 140)], 26)], DEAD_WOOD))]
    out += [figure("Mother", "SitSlumped", 380, 585, 1.25), figure("Sister", "SitSlumped", 600, 588, 1.3),
            bowl(728, 590, 1.0), grumble(672, 494, 0.9), figure("Hero", "StandHungry", 985, 592, 1.12)]
    out.append(caption(["Little Sister hadn’t eaten in days."]))
    return out


def panel_legend():
    out = [sky(SKY_DUSK, (190, 300)), sun(640, 372, 84, rays=False),
           ridge(366, 10, 140, 0.6, HILL_FAR, w=5), ridge(388, 6, 200, 0.2, HILL_NEAR, w=5),
           ridge(420, 6, 180, 1.4, GROUND, w=5), cracks(13, 6, 440, 560),
           ridge(560, 26, 260, 2.6, GROUND_DUSK)]
    out += [figure("Father", "Stand", 300, 590, 0.86), figure("Hero", "Determined", 930, 590, 1.0),
            tale_bubble(60, 20, 640, 228, (318, 270)),
            bubble(1090, 160, 230, 96, (1000, 266), "I’ll go!")]
    out.append(caption(["Father told of a faraway rainforest, heavy with fruit...",
                        "and full of danger."]))
    return out


def panel_departure():
    path = (smooth([(540, H + 40), (640, 620), (820, 520), (940, 448), (1000, 404)]) + " " +
            smooth([(1016, 404), (968, 448), (888, 520), (780, 620), (900, H + 40)]).replace("M", "L", 1) + " Z")
    out = [sky(SKY_DAWN, (170, 290)), sun(1008, 404, 76, rays=False),
           ridge(384, 10, 150, 0.9, "#E2AE84", w=5), ridge(404, 4, 200, 0.7, "#C99055", w=5),
           cracks(17, 5, 430, 560, 40, 520), area(path, "#E5BC80", 5),
           figure("Hero", "BackBundle", 956, 452, 0.4),
           back_figure("Father", 205, 624, 1.1, head_rot=-3),
           back_figure("Mother", 405, 624, 1.1, head_rot=-6),
           back_figure("Sister", 580, 628, 1.1, arm_right=112, head_rot=-5)]
    out.append(wave_marks(*hand_point("Sister", "shoulderRight", 112, 580, 628, 1.1, mirror=True), 1))
    out.append(caption(["At dawn he set off, carrying his family’s hope on his back."]))
    return out


def panel_tree():
    out = [f'<rect x="0" y="0" width="{W}" height="{H}" fill="{JUNGLE[0]}"/>',
           ridge(240, 30, 160, 0.4, "#0D2819", w=0, bottom=520)]
    for x, w in ((70, 54), (250, 36), (1010, 64), (1200, 42)):
        out.append(f'<path d="M {x} -10 L {x + w} -10 L {x + w + 8} {H} L {x - 8} {H} Z" fill="{JUNGLE[1]}"/>')
    out += [leaves(71, 44, -80, W + 80, -90, 70, (40, 140), (140, 230), mc.BACK_LEAVES),
            '<path d="M 790 -10 L 950 -10 L 1010 430 L 720 430 Z" fill="url(#shaft)"/>',
            '<path d="M 300 -10 L 380 -10 L 400 360 L 250 360 Z" fill="url(#shaft)"/>',
            vine("Vine_A", 30, -30, 340, "vineDeep"), vine("Vine_B", 410, -30, 300, "vineDeep"),
            vine("Vine_Curl", 1100, -20, 280, "vineDeep"), vine("Vine_B", 900, -40, 250, "vineDeep"),
            bumps(-60, W + 60, 470, 60, JUNGLE[2], 53, w=0),
            leaves(72, 26, -60, W + 60, 440, 520, (-160, -20), (100, 160), mc.BACK_LEAVES),
            giant_trunk(),
            mc.leaf(760, 168, -150, 120, 34, mc.FRONT_LEAVES[0], "#5FA36E", edge=True),
            mc.leaf(990, 126, -60, 110, 32, mc.FRONT_LEAVES[1], "#5FA36E", edge=True),
            banana_bunch(912, 214, 0.78, flip=True),
            vine("Vine_Curl", 1092, 40, 250, "vineLit"),
            vine("Vine_A", 170, -40, 300, "vineLit"), vine("Vine_B", 1185, -40, 340, "vineLit"),
            leaves(73, 16, -80, W + 80, -70, -10, (50, 130), (150, 230), mc.FRONT_LEAVES, "#5FA36E", edge=True),
            bumps(-60, W + 60, 560, 46, JUNGLE[3], 54, w=5),
            fern(110, 580, 0.95, 81), fern(1160, 588, 1.05, 82), fern(430, 592, 0.7, 83),
            cobra_hiding(655, 470, 0.42),
            figure("Hero", "BackBundle", 290, 575, 0.6)]
    for x, y, a in ((-20, 700, -40), (-30, 640, -12), (60, 730, -70), (1300, 700, -140), (1290, 650, -170)):
        out.append(mc.leaf(x, y, a, 210, 56, mc.FRONT_LEAVES[0], "#5FA36E", edge=True))
    out.append(caption(["The fruit hung high in the giant tree...",
                        "but something in the shadows was watching."]))
    return out


PANELS = [
    ("The Drought", panel_drought),
    ("Little Sister", panel_sister),
    ("“I’ll go!”", panel_legend),
    ("Setting Off", panel_departure),
    ("The Giant Tree", panel_tree),
]

STYLE = (mc.font_face("B2", "Baloo2-SemiBold.ttf") + mc.font_face("B2X", "Baloo2-ExtraBold.ttf") +
         f".cap{{font-family:B2;font-size:30px;fill:{CREAM};text-anchor:middle}}"
         f".say{{font-family:B2X;font-size:44px;fill:{BROWN};text-anchor:middle}}"
         f".title{{font-family:B2X;font-size:44px;fill:{GOLD};stroke:{BROWN};stroke-width:8px;paint-order:stroke}}"
         f".label{{font-family:B2X;font-size:28px;fill:{CREAM}}}"
         f".num{{font-family:B2X;font-size:26px;fill:{BROWN};text-anchor:middle}}"
         f".note{{font-family:B2;font-size:24px;fill:{CREAM}}}")


def page(body, w, h):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="0 0 {w} {h}">'
            f'<style>{STYLE}</style>{DEFS}\n{body}\n</svg>\n')


def sheet(bodies):
    thumb_w, thumb_h, gap, top = 640, 360, 40, 120
    row = thumb_h + 64
    out = [f'<rect width="100%" height="100%" fill="{BACKGROUND}"/>',
           f'<text x="{gap}" y="80" class="title">Intro Comic · Storyboard Draft</text>']
    for i, ((name, _), body) in enumerate(zip(PANELS, bodies)):
        x, y = gap + (i % 2) * (thumb_w + gap), top + (i // 2) * row
        out.append(f'<rect x="{x + 5}" y="{y + 6}" width="{thumb_w}" height="{thumb_h}" fill="{SHADOW}"/>')
        out.append(f'<svg x="{x}" y="{y}" width="{thumb_w}" height="{thumb_h}" viewBox="0 0 {W} {H}" '
                   f'overflow="hidden">{body}</svg>')
        out.append(f'<rect x="{x}" y="{y}" width="{thumb_w}" height="{thumb_h}" fill="none" '
                   f'stroke="{BROWN}" stroke-width="5"/>')
        out.append(f'<circle cx="{x + 18}" cy="{y + thumb_h + 34}" r="17" fill="{GOLD}" '
                   f'stroke="{BROWN}" stroke-width="4"/>'
                   f'<text x="{x + 18}" y="{y + thumb_h + 43}" class="num">{i + 1}</text>'
                   f'<text x="{x + 46}" y="{y + thumb_h + 44}" class="label">{name}</text>')
    x, y = gap + thumb_w + gap, top + 2 * row
    notes = ["Loading page → 1 → 2 → 3 → 4 → 5 → Title Screen",
             "Next panel: click, tap, Space, E or Enter",
             "Skip: a button in the corner, any panel",
             "Motion: slow pan or zoom per panel,",
             "plus one small loop (see the notes)"]
    out.append(f'<rect x="{x}" y="{y}" width="{thumb_w}" height="{thumb_h}" rx="22" fill="{PANEL}"/>'
               f'<rect x="{x}" y="{y}" width="{thumb_w}" height="{thumb_h}" rx="22" fill="none" '
               f'stroke="{GOLD}" stroke-width="4"/>'
               f'<text x="{x + 32}" y="{y + 60}" class="label">Flow</text>')
    out += [f'<text x="{x + 32}" y="{y + 112 + 44 * i}" class="note">{t}</text>' for i, t in enumerate(notes)]
    return 2 * thumb_w + 3 * gap, top + 3 * row, "\n".join(out)


def render(svg_text, png, w, h, scale, tmp):
    """Headless Chrome can leave the bottom of a window-sized shot unpainted, so the
    window gets spare room and the picture is cropped back to its size."""
    page = tmp / f"{png.stem}.svg"
    page.write_text(svg_text)
    mc.shoot(page, png, w, h + 160, scale)
    Image.open(png).crop((0, 0, round(w * scale), round(h * scale))).save(png, optimize=True)


def main():
    OUT_DIR.mkdir(exist_ok=True)
    bodies = ["\n".join(make()) for _, make in PANELS]
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        for i, body in enumerate(bodies, 1):
            render(page(body, W, H), OUT_DIR / f"Panel_{i}.png", W, H, 1, tmp)
        sw, sh, body = sheet(bodies)
        render(page(body, sw, sh), OUT_DIR / "storyboard.png", sw, sh, 1.5, tmp)
    print(f"wrote {len(bodies)} panels and storyboard.png to {OUT_DIR}")


if __name__ == "__main__":
    main()
