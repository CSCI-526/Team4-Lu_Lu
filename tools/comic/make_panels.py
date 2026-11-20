"""Draws the intro comic's final panels, in layers for the motion comic.

Each of the 5 panels follows the approved storyboard (``make_storyboard.py``, whose
drawing helpers it reuses) and adds the finish: grass, rocks, clouds, birds,
flowers, bark, light. A panel is split into the layers Unity moves on their own,
a full-frame background plus cropped layers for the cast, the speech bubbles,
and the small loops (a falling leaf, the hunger marks, the wave, the cobra's eyes).
Captions are not drawn into the art: Unity sets them in Baloo 2 on a plate, breaking
the line where the caption's newline is.

Writes into ``panels/``:
- ``Panel_<n>/<Layer>.svg`` and a 2x transparent ``.png`` (the background opaque)
- ``panels.json``: each layer's rect in the 1280x720 panel, the caption, and the motion
- ``preview/Panel_<n>.png``: the layers put together under the caption plate, and
  ``preview/overview.png`` with all five

Run: ``python tools/comic/make_panels.py`` (needs what make_characters.py needs).
"""

import base64
import json
import math
import random
import sys
import tempfile
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import make_storyboard as sb  # noqa: E402
from make_storyboard import (  # noqa: E402
    BROWN, CREAM, GOLD, H, ROUND, SHADOW, W, area, back_figure, bubble, bumps, cobra_hiding,
    cracks, dead_tree, dry_leaf, f1, fern, figure, giant_trunk, grumble, hand_point, leaves, merged,
    poly_d, ridge, sky, smooth, sun, tale_bubble, vine, wave_marks, with_shadow)
from PIL import Image  # noqa: E402

mc = sb.mc
OUT_DIR = HERE / "panels"
PNG_SCALE = 2
PAD = 2  # panel units kept around a cropped layer

DRY_GRASS, DRY_GRASS_DARK, ROCK, ROCK_LIGHT = "#D2A65A", "#B98A45", "#A88058", "#C9A27A"
GROUND_PATCH = "#AE7038"
# the caption plate, opaque so it reads the same on the dry panels and in the dark jungle:
# the olive the see-through Panel plate shows over the dry ground
PLATE = "#353D23"


# ---------------------------------------------------------------- finishing pieces

def ridge_y(x, y, amp, period, phase):
    """The height of sb.ridge(y, amp, period, phase) at x, to stand things on it."""
    return y + amp * math.sin(x / period + phase) + amp * 0.45 * math.sin(x / (period * 0.43) + phase * 2.3)


def circle_d(cx, cy, r):
    return f"M {f1(cx - r)} {f1(cy)} a {f1(r)} {f1(r)} 0 1 0 {f1(2 * r)} 0 a {f1(r)} {f1(r)} 0 1 0 {f1(-2 * r)} 0"


def tuft(x, y, s, seed, fill=DRY_GRASS):
    """A clump of dry grass, its blades fanned from one base."""
    rnd = random.Random(seed)
    blades = []
    for i in range(7):
        a = math.radians(-90 + (i - 3) * 15 + rnd.uniform(-5, 5))
        length = rnd.uniform(24, 40) * (1 - abs(i - 3) * 0.07)
        bx = (i - 3) * 4.5
        blades.append(("area", poly_d([(bx - 4.5, 0), (bx + math.cos(a) * length, math.sin(a) * length),
                                       (bx + 4.5, 0)], True)))
    return f'<g transform="translate({f1(x)} {f1(y)}) scale({s})">{merged(blades, fill, 3)}</g>'


def rock(x, y, s, fill=ROCK, light=ROCK_LIGHT):
    d = "M -32 0 C -34 -14 -20 -27 -4 -27 C 15 -27 32 -16 34 0 Z"
    return (f'<g transform="translate({f1(x)} {f1(y)}) scale({s})">{area(d, fill, 4.5)}'
            f'<path d="M -20 -15 C -14 -21 -4 -22 6 -21" fill="none" stroke="{light}" stroke-width="4" {ROUND}/>'
            f'</g>')


def pebbles(seed, count, x0, x1, y0, y1, fill=ROCK):
    rnd = random.Random(seed)
    return "".join(f'<ellipse cx="{f1(rnd.uniform(x0, x1))}" cy="{f1(rnd.uniform(y0, y1))}" '
                   f'rx="{f1(rnd.uniform(4, 8))}" ry="{f1(rnd.uniform(3, 5))}" fill="{fill}" '
                   f'stroke="{BROWN}" stroke-width="2.5"/>' for _ in range(count))


def patches(seed, count, y0, y1, fill=GROUND_PATCH):
    """Flat darker patches that break up a plain ground."""
    rnd = random.Random(seed)
    return "".join(f'<ellipse cx="{f1(rnd.uniform(0, W))}" cy="{f1(rnd.uniform(y0, y1))}" '
                   f'rx="{f1(rnd.uniform(60, 140))}" ry="{f1(rnd.uniform(10, 20))}" fill="{fill}"/>'
                   for _ in range(count))


def far_dead_tree(x, y, s, color):
    strokes = ("M 0 0 L 0 -40", "M 0 -22 L -14 -36 L -18 -46", "M 0 -30 L 12 -44", "M 0 -40 L -4 -50",
               "M 12 -44 L 18 -48")
    return (f'<g transform="translate({f1(x)} {f1(y)}) scale({s})">' +
            "".join(f'<path d="{d}" fill="none" stroke="{color}" stroke-width="4.5" {ROUND}/>' for d in strokes) +
            '</g>')


def haze(seed, count, y0, y1):
    """Heat wobble over the horizon."""
    rnd = random.Random(seed)
    out = []
    for _ in range(count):
        x, y = rnd.uniform(60, W - 160), rnd.uniform(y0, y1)
        out.append(f'<path d="M {f1(x)} {f1(y)} q 12 -6 24 0 t 24 0 t 24 0" fill="none" stroke="{CREAM}" '
                   f'stroke-width="3" opacity="0.45" {ROUND}/>')
    return "".join(out)


def cloud(x, y, s, fill, shade):
    puffs = ((-52, 2, 26), (-20, -14, 34), (20, -10, 30), (52, 4, 22))
    items = [("area", circle_d(cx, cy, r)) for cx, cy, r in puffs]
    items.append(("area", f"M -74 4 L 74 4 L 74 22 Q 74 26 70 26 L -70 26 Q -74 26 -74 22 Z"))
    under = f'<path d="M -72 14 L 72 14 L 72 22 Q 72 26 68 26 L -68 26 Q -72 26 -72 22 Z" fill="{shade}"/>'
    return f'<g transform="translate({f1(x)} {f1(y)}) scale({s})">{merged(items, fill, 4)}{under}</g>'


def bird(x, y, s):
    return (f'<path d="M {f1(x - 13 * s)} {f1(y)} Q {f1(x - 6 * s)} {f1(y - 9 * s)} {f1(x)} {f1(y)} '
            f'Q {f1(x + 6 * s)} {f1(y - 9 * s)} {f1(x + 13 * s)} {f1(y)}" fill="none" stroke="{BROWN}" '
            f'stroke-width="{f1(3.5 * s)}" {ROUND}/>')


def rays(x, y, r0, r1, angles, width, opacity):
    """Soft beams fanned up from a low sun."""
    out = []
    for a in angles:
        t = math.radians(a)
        p = [(x + math.cos(t - width) * r0, y + math.sin(t - width) * r0),
             (x + math.cos(t - width * 1.6) * r1, y + math.sin(t - width * 1.6) * r1),
             (x + math.cos(t + width * 1.6) * r1, y + math.sin(t + width * 1.6) * r1),
             (x + math.cos(t + width) * r0, y + math.sin(t + width) * r0)]
        out.append(f'<path d="{poly_d(p, True)}" fill="{CREAM}" opacity="{opacity}"/>')
    return "".join(out)


def contact(x, y, rx, ry=9):
    """The flat dark patch a figure sits or stands on."""
    return f'<ellipse cx="{f1(x)}" cy="{f1(y)}" rx="{f1(rx)}" ry="{f1(ry)}" fill="{SHADOW}" opacity="0.4"/>'


def bark_lines(x, y, s, lines, color="#6E5440"):
    return (f'<g transform="translate({x} {y}) scale({s})">' +
            "".join(f'<path d="{smooth(list(p))}" fill="none" stroke="{color}" stroke-width="5" {ROUND}/>'
                    for p in lines) + '</g>')


def flower(x, y, s, fill="#E8556B"):
    petals = [("area", circle_d(math.cos(math.radians(a)) * 11, math.sin(math.radians(a)) * 11, 9))
              for a in range(-90, 270, 72)]
    return (f'<g transform="translate({f1(x)} {f1(y)}) scale({s})">{merged(petals, fill, 3.5)}'
            f'<circle r="6" fill="{GOLD}" stroke="{BROWN}" stroke-width="3"/></g>')


def motes(seed, count, x0, x1, y0, y1):
    rnd = random.Random(seed)
    out = []
    for _ in range(count):
        r = rnd.uniform(2, 4.5)
        out.append(f'<circle cx="{f1(rnd.uniform(x0, x1))}" cy="{f1(rnd.uniform(y0, y1))}" r="{f1(r * 2.4)}" '
                   f'fill="#F4F28A" opacity="0.12"/>')
        out.append(f'<circle cx="{f1(rnd.uniform(x0, x1))}" cy="{f1(rnd.uniform(y0, y1))}" r="{f1(r)}" '
                   f'fill="#F4F28A" opacity="0.7"/>')
    return "".join(out)


def plate_caption(lines):
    """The caption as Unity will set it: Baloo 2 in Cream on the plate with a Gold rim."""
    h = 30 + 38 * len(lines)
    y = H - 22 - h
    text = "".join(f'<text x="{W / 2}" y="{y + 44 + 38 * i}" class="cap">{t}</text>' for i, t in enumerate(lines))
    arrow = area(poly_d([(1124, y + h - 34), (1144, y + h - 24), (1124, y + h - 14)], True), GOLD, 4)
    return (f'<rect x="120" y="{y}" width="1040" height="{h}" rx="22" fill="{PLATE}" stroke="{GOLD}" '
            f'stroke-width="4"/>{text}{arrow}')


# ---------------------------------------------------------------- the panels
# A panel is its caption, its layers back to front (name, svg, opaque), its motion (the
# camera eases from one view to another, a center and a zoom in panel units), and the cues
# of the layers that move on their own. A cue is one of IntroComicScreen's kinds:
#   Fall     drops from `start` to `end` (offsets, panel units, y down) over `period` s,
#            swaying `amount` units, and fades at the end; once
#   Pulse    scale grows by `amount` and back, every `period` s
#   Pop      hidden until `delay`, then pops in (overshoot) over `period` s
#   Walk     moves from `start` at `startScale` to `end` at `endScale` over `period` s,
#            bobbing `amount` units a step, pivoting at its feet, and fades at the end
#   Drift    moves from `start` to `end` over `period` s, bobbing `amount` units
#   Blink    shows and hides, `period` s each
#   Breathe  scale grows by `amount` and back, every `period` s, smoothly
#   Twinkle  alpha falls to `amount` and back, every `period` s
#   Reveal   hidden until `delay`, then blinks on twice, `period` s a blink, and stays
# Every cue waits `delay` s from the panel's start.


def cue(kind, delay=0.0, period=1.0, amount=0.0, start=(0, 0), end=(0, 0), start_scale=1.0, end_scale=1.0):
    return dict(kind=kind, delay=delay, period=period, amount=amount, start=list(start), end=list(end),
                startScale=start_scale, endScale=end_scale)


def panel_drought():
    far = (372, 16, 150, 0.4)
    bg = [sky(sb.SKY_NOON, (170, 300)), sun(220, 140, 64), haze(5, 6, 300, 350),
          ridge(*far, sb.HILL_FAR)]
    bg += [far_dead_tree(x, ridge_y(x, *far) + 4, k, "#B9844F") for x, k in ((96, 0.9), (318, 1.1), (1210, 0.8))]
    bg += [ridge(420, 12, 120, 2.0, sb.HILL_NEAR), rock(170, ridge_y(170, 420, 12, 120, 2.0) + 6, 0.7),
           ridge(468, 5, 200, 1.0, sb.GROUND), patches(4, 9, 500, 700), cracks(3, 11, 490, 700),
           dead_tree(1060, 585, 1.0),
           bark_lines(1060, 585, 1.0, [((-8, -20), (-12, -120), (-2, -220)), ((12, -40), (10, -150))]),
           dry_leaf(880, 300, -30, 0.9), dry_leaf(1210, 460, 70, 0.8),
           dry_leaf(1180, 600, 10, 0.9), dry_leaf(1230, 585, -40, 0.8)]
    bg += [tuft(x, y, k, i) for i, (x, y, k) in enumerate(
        ((70, 520, 1.0), (205, 640, 1.2), (330, 555, 0.8), (1180, 520, 0.9), (1240, 660, 1.2),
         (760, 690, 1.0), (430, 700, 1.1), (980, 640, 0.8)))]
    bg += [rock(130, 600, 1.0), rock(1010, 668, 0.75), pebbles(6, 14, 40, 1240, 500, 710)]
    family = [contact(x, 590, r) for x, r in ((470, 66), (620, 44), (770, 72), (945, 80))]
    family += [figure("Hero", "SitSlumped", 470, 585, 0.8), figure("Sister", "SitSlumped", 620, 585, 0.8),
               figure("Mother", "SitSlumped", 770, 585, 0.8), figure("Father", "SitSlumped", 945, 585, 0.8)]
    return dict(
        caption="The rains never came. The trees went bare,\nand the family went hungry.",
        layers=[("Background", "\n".join(bg), True), ("Family", "\n".join(family), False),
                ("Leaf", dry_leaf(1150, 330, 20), False)],
        motion=dict(seconds=6, start=dict(center=[640, 360], zoom=1.0), end=dict(center=[700, 420], zoom=1.12)),
        cues=dict(Leaf=cue("Fall", delay=0.8, period=4.0, amount=16, end=(-50, 250))))


def panel_sister():
    bg = [sky(sb.SKY_NOON, (150, 280)), haze(9, 4, 300, 330),
          ridge(350, 14, 150, 1.2, sb.HILL_FAR), far_dead_tree(790, ridge_y(790, 350, 14, 150, 1.2) + 4, 1.0, "#B9844F"),
          ridge(392, 10, 120, 0.3, sb.HILL_NEAR), ridge(430, 5, 200, 2.0, sb.GROUND),
          patches(8, 8, 460, 700), cracks(8, 8, 450, 700, 160, 1240)]
    bg += [tuft(x, y, k, 20 + i) for i, (x, y, k) in enumerate(
        ((240, 470, 0.8), (1220, 500, 1.0), (1150, 690, 1.2), (300, 690, 1.1), (820, 700, 0.9)))]
    bg += [rock(1200, 620, 0.9), pebbles(10, 10, 180, 1240, 460, 710),
           with_shadow(merged([("tube", [(70, H + 40), (58, 300), (64, -40)], 130),
                               ("tube", [(90, 220), (170, 150), (220, 140)], 26)], sb.DEAD_WOOD)),
           bark_lines(0, 0, 1.0, [((40, -20), (34, 200), (44, 420), (36, 700)), ((96, 60), (100, 260)),
                                  ((82, 380), (90, 600))]),
           dry_leaf(200, 600, 30, 1.0), dry_leaf(140, 650, -20, 0.9)]
    family = [contact(380, 590, 100), contact(600, 593, 64), contact(985, 597, 60), contact(728, 594, 56, 8)]
    family += [figure("Mother", "SitSlumped", 380, 585, 1.25), figure("Sister", "SitSlumped", 600, 588, 1.3),
               sb.bowl(728, 590, 1.0), figure("Hero", "StandHungry", 985, 592, 1.12)]
    return dict(
        caption="Little Sister hadn’t eaten in days.",
        layers=[("Background", "\n".join(bg), True), ("Family", "\n".join(family), False),
                ("Grumble", grumble(672, 494, 0.9), False)],
        motion=dict(seconds=5, start=dict(center=[640, 360], zoom=1.0), end=dict(center=[600, 420], zoom=1.12)),
        cues=dict(Grumble=cue("Pulse", period=0.5, amount=0.15)))


def panel_legend():
    far = (366, 10, 140, 0.6)
    bg = [sky(sb.SKY_DUSK, (190, 300)), cloud(870, 66, 0.9, "#F2B07A", "#E39A62"),
          cloud(1215, 292, 0.55, "#F2B07A", "#E39A62"), sun(640, 372, 84, rays=False),
          ridge(*far, sb.HILL_FAR, w=5)]
    bg += [far_dead_tree(x, ridge_y(x, *far) + 4, 0.9, "#B97A4A") for x in (480, 820, 1150)]
    bg += [ridge(388, 6, 200, 0.2, sb.HILL_NEAR, w=5), ridge(420, 6, 180, 1.4, sb.GROUND, w=5),
           patches(14, 6, 440, 560, "#A86A35"), cracks(13, 6, 440, 560),
           ridge(560, 26, 260, 2.6, sb.GROUND_DUSK), patches(15, 5, 600, 710, "#97602F")]
    bg += [tuft(x, y, k, 40 + i, "#C9944E") for i, (x, y, k) in enumerate(
        ((90, 600, 1.1), (560, 640, 1.0), (700, 600, 0.8), (1190, 620, 1.2), (1100, 690, 1.0), (40, 690, 1.2)))]
    bg += [rock(620, 470, 0.6, "#9A6E4A", "#B88A62"), rock(1220, 560, 1.0, "#9A6E4A", "#B88A62"),
           pebbles(16, 10, 40, 1240, 580, 710, "#9A6E4A")]
    return dict(
        caption="Father told of a faraway rainforest,\nheavy with fruit, but full of danger.",
        layers=[("Background", "\n".join(bg), True),
                ("Father", contact(300, 594, 70) + figure("Father", "Stand", 300, 590, 0.86), False),
                ("Hero", contact(930, 594, 64) + figure("Hero", "Determined", 930, 590, 1.0), False),
                ("TaleBubble", tale_bubble(60, 20, 640, 228, (318, 270)), False),
                ("HeroBubble", bubble(1090, 160, 230, 96, (1000, 266), "I’ll go!"), False)],
        motion=dict(seconds=7, start=dict(center=[500, 300], zoom=1.25), end=dict(center=[780, 330], zoom=1.25)),
        cues=dict(TaleBubble=cue("Pop", delay=0.4, period=0.35), HeroBubble=cue("Pop", delay=3.6, period=0.35)))


def panel_departure():
    path = (smooth([(540, H + 40), (640, 620), (820, 520), (940, 448), (1000, 404)]) + " " +
            smooth([(1016, 404), (968, 448), (888, 520), (780, 620), (900, H + 40)]).replace("M", "L", 1) + " Z")
    bg = [sky(sb.SKY_DAWN, (170, 290)), cloud(300, 104, 1.0, "#FBE3C4", "#F2C9A8"),
          cloud(640, 60, 0.7, "#FBE3C4", "#F2C9A8"), cloud(1170, 150, 0.6, "#FBE3C4", "#F2C9A8"),
          rays(1008, 404, 96, 300, (-160, -128, -96, -64, -32), 0.045, 0.1),
          sun(1008, 404, 76, rays=False),
          ridge(384, 10, 150, 0.9, "#E2AE84", w=5), ridge(404, 4, 200, 0.7, "#C99055", w=5),
          patches(18, 7, 430, 710, "#B98048"), cracks(17, 5, 430, 560, 40, 520),
          area(path, "#E5BC80", 5), pebbles(19, 16, 600, 960, 430, 710, "#C99B62")]
    bg += [tuft(x, y, k, 60 + i, "#D4A260") for i, (x, y, k) in enumerate(
        ((700, 450, 0.6), (1100, 470, 0.8), (1180, 560, 1.0), (1220, 680, 1.2), (980, 640, 1.0), (40, 470, 0.8)))]
    bg += [rock(1150, 500, 0.7, "#B98A5C", "#D2A87A"), rock(1060, 690, 1.0, "#B98A5C", "#D2A87A")]
    birds = "".join(bird(x, y, k) for x, y, k in ((1060, 240, 1.0), (1096, 218, 0.8), (1124, 250, 0.9)))
    family = [contact(205, 628, 96, 12), contact(405, 628, 90, 12), contact(580, 632, 60, 10),
              back_figure("Father", 205, 624, 1.1, head_rot=-3),
              back_figure("Mother", 405, 624, 1.1, head_rot=-6),
              back_figure("Sister", 580, 628, 1.1, arm_right=112, head_rot=-5)]
    hx, hy = hand_point("Sister", "shoulderRight", 112, 580, 628, 1.1, mirror=True)
    return dict(
        caption="At dawn he set off,\ncarrying his family’s hope on his back.",
        layers=[("Background", "\n".join(bg), True), ("Birds", birds, False),
                ("Hero", contact(956, 455, 26, 5) + figure("Hero", "BackBundle", 956, 452, 0.4), False),
                ("Family", "\n".join(family), False), ("WaveMarks", wave_marks(hx, hy, 1), False)],
        motion=dict(seconds=7, start=dict(center=[640, 360], zoom=1.0), end=dict(center=[700, 380], zoom=1.06)),
        cues=dict(Hero=cue("Walk", delay=0.3, period=6.5, amount=3, start=(-56, 53), end=(39, -40),
                           start_scale=1.35, end_scale=0.55),
                  Birds=cue("Drift", period=12, amount=4, end=(-80, 30)),
                  WaveMarks=cue("Blink", period=0.5)))


COBRA = (655, 470, 0.42)            # cobra_hiding's place and scale in panel 5
COBRA_EYES = ((-40, -15), (40, -15))  # the eyes on the kit's cobra head, in head units


def panel_tree():
    bg = [f'<rect x="0" y="0" width="{W}" height="{H}" fill="{sb.JUNGLE[0]}"/>',
          ridge(240, 30, 160, 0.4, "#0D2819", w=0, bottom=520)]
    for x, w in ((70, 54), (250, 36), (1010, 64), (1200, 42)):
        bg.append(f'<path d="M {x} -10 L {x + w} -10 L {x + w + 8} {H} L {x - 8} {H} Z" fill="{sb.JUNGLE[1]}"/>')
    bg += [leaves(71, 44, -80, W + 80, -90, 70, (40, 140), (140, 230), mc.BACK_LEAVES),
           '<path d="M 790 -10 L 950 -10 L 1010 430 L 720 430 Z" fill="url(#shaft)"/>',
           '<path d="M 300 -10 L 380 -10 L 400 360 L 250 360 Z" fill="url(#shaft)"/>',
           vine("Vine_A", 30, -30, 340, "vineDeep"), vine("Vine_B", 410, -30, 300, "vineDeep"),
           vine("Vine_Curl", 1100, -20, 280, "vineDeep"), vine("Vine_B", 900, -40, 250, "vineDeep"),
           bumps(-60, W + 60, 470, 60, sb.JUNGLE[2], 53, w=0),
           leaves(72, 26, -60, W + 60, 440, 520, (-160, -20), (100, 160), mc.BACK_LEAVES)]
    tree = [giant_trunk(),
            mc.leaf(760, 168, -150, 120, 34, mc.FRONT_LEAVES[0], "#5FA36E", edge=True),
            mc.leaf(990, 126, -60, 110, 32, mc.FRONT_LEAVES[1], "#5FA36E", edge=True)]
    canopy = [vine("Vine_Curl", 1092, 40, 250, "vineLit"),
              vine("Vine_A", 170, -40, 300, "vineLit"), vine("Vine_B", 1185, -40, 340, "vineLit"),
              leaves(73, 16, -80, W + 80, -70, -10, (50, 130), (150, 230), mc.FRONT_LEAVES, "#5FA36E", edge=True)]
    under = [bumps(-60, W + 60, 560, 46, sb.JUNGLE[3], 54, w=5),
             fern(110, 580, 0.95, 81), fern(1160, 588, 1.05, 82), fern(430, 592, 0.7, 83),
             flower(200, 548, 0.9), flower(236, 566, 0.7, "#F08A4B"), flower(1060, 560, 0.9),
             flower(1240, 590, 0.8, "#F08A4B"), flower(880, 580, 0.7)]
    front = [mc.leaf(x, y, a, 210, 56, mc.FRONT_LEAVES[0], "#5FA36E", edge=True)
             for x, y, a in ((-20, 700, -40), (-30, 640, -12), (60, 730, -70), (1300, 700, -140), (1290, 650, -170))]
    cx, cy, k = COBRA
    glow = ['<defs><radialGradient id="eyeGlow"><stop offset="0.25" stop-color="#F4F28A" stop-opacity="0.4"/>'
            '<stop offset="1" stop-color="#F4F28A" stop-opacity="0"/></radialGradient></defs>']
    for ex, ey in COBRA_EYES:
        gx, gy = cx + ex * k, cy + (ey + sb.HEAD_DROP) * k
        glow.append(f'<ellipse cx="{f1(gx)}" cy="{f1(gy)}" rx="30" ry="20" fill="url(#eyeGlow)"/>')
    return dict(
        caption="The fruit hung high in the giant tree...\nbut something in the shadows was watching.",
        layers=[("Background", "\n".join(bg), True), ("Tree", "\n".join(tree), False),
                ("Bananas", sb.banana_bunch(912, 214, 0.78, flip=True), False),
                ("Canopy", "\n".join(canopy), False), ("Undergrowth", "\n".join(under), False),
                ("Cobra", cobra_hiding(*COBRA, low=True), False), ("CobraGlow", "".join(glow), False),
                ("Hero", figure("Hero", "BackBundle", 290, 575, 0.6), False),
                ("Motes", motes(95, 14, 700, 1040, 60, 420), False),
                ("Foreground", "\n".join(front), False)],
        motion=dict(seconds=8, start=dict(center=[880, 240], zoom=1.35), end=dict(center=[640, 380], zoom=1.04)),
        cues=dict(Bananas=cue("Breathe", period=3.0, amount=0.03), Motes=cue("Twinkle", period=1.6, amount=0.35),
                  CobraGlow=cue("Reveal", delay=8.2, period=0.25)))


PANELS = [panel_drought, panel_sister, panel_legend, panel_departure, panel_tree]


# ---------------------------------------------------------------- export

def layer_page(svg, x0=0, y0=0, w=W, h=H):
    style = f"<style>{sb.STYLE}</style>" if 'class="' in svg else ""
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{w}" height="{h}" viewBox="{x0} {y0} {w} {h}">'
            f'{style}{sb.DEFS}\n{svg}\n</svg>\n')


def export_layer(folder, name, svg, opaque, tmp):
    """Draws a layer over the whole frame at 2x, then crops it to what it covers."""
    png = folder / f"{name}.png"
    sb.render(layer_page(svg), png, W, H, PNG_SCALE, tmp)
    if opaque:
        rect = [0, 0, W, H]
    else:
        image = Image.open(png)
        box = image.getchannel("A").getbbox()
        if not box:
            sys.exit(f"{folder.name}/{name} is empty")
        # even edges in panel units, so each side of the 2x PNG is a multiple of 4
        # and Unity can block-compress it
        even = lambda v: v - v % 2
        x0, y0 = max(0, even(box[0] // PNG_SCALE - PAD)), max(0, even(box[1] // PNG_SCALE - PAD))
        x1 = min(W, even(-(-box[2] // PNG_SCALE) + PAD + 1))
        y1 = min(H, even(-(-box[3] // PNG_SCALE) + PAD + 1))
        rect = [x0, y0, x1 - x0, y1 - y0]
        image.crop((x0 * PNG_SCALE, y0 * PNG_SCALE, x1 * PNG_SCALE, y1 * PNG_SCALE)).save(png, optimize=True)
    x0, y0, w, h = rect
    (folder / f"{name}.svg").write_text(layer_page(svg, x0, y0, w, h))
    return rect


def overview(previews, tmp):
    thumb_w, thumb_h, gap, top = 640, 360, 40, 110
    out = [f'<rect width="100%" height="100%" fill="{sb.BACKGROUND}"/>',
           f'<text x="{gap}" y="74" class="title">Intro Comic · Final Panels</text>']
    for i, png in enumerate(previews):
        x, y = gap + (i % 2) * (thumb_w + gap), top + (i // 2) * (thumb_h + gap)
        uri = "data:image/png;base64," + base64.b64encode(png.read_bytes()).decode()
        out.append(f'<rect x="{x + 5}" y="{y + 6}" width="{thumb_w}" height="{thumb_h}" fill="{SHADOW}"/>'
                   f'<image href="{uri}" x="{x}" y="{y}" width="{thumb_w}" height="{thumb_h}"/>'
                   f'<rect x="{x}" y="{y}" width="{thumb_w}" height="{thumb_h}" fill="none" stroke="{BROWN}" '
                   f'stroke-width="5"/><circle cx="{x + 30}" cy="{y + 30}" r="18" fill="{GOLD}" stroke="{BROWN}" '
                   f'stroke-width="4"/><text x="{x + 30}" y="{y + 39}" class="num">{i + 1}</text>')
    sw, sh = 2 * thumb_w + 3 * gap, top + 3 * (thumb_h + gap)
    page = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{sw}" height="{sh}" viewBox="0 0 {sw} {sh}">'
            f'<style>{sb.STYLE}</style>{"".join(out)}</svg>')
    sb.render(page, OUT_DIR / "preview" / "overview.png", sw, sh, 1.5, tmp)


def main():
    (OUT_DIR / "preview").mkdir(parents=True, exist_ok=True)
    data = dict(size=[W, H], pngScale=PNG_SCALE,
                note="rect is x, y, w, h in panel units from the top left; center is the layer's middle "
                     "from the panel's middle, y up, as a Unity anchoredPosition; layers are back to front; "
                     "a cue's start and end are offsets in panel units, y down (see make_panels.py)",
                panels=[])
    previews = []
    with tempfile.TemporaryDirectory() as tmp:
        tmp = Path(tmp)
        for n, make in enumerate(PANELS, 1):
            panel = make()
            folder = OUT_DIR / f"Panel_{n}"
            folder.mkdir(exist_ok=True)
            for old in folder.glob("*.*"):
                old.unlink()
            layers = []
            for name, svg, opaque in panel["layers"]:
                x, y, w, h = export_layer(folder, name, svg, opaque, tmp)
                layers.append(dict(name=name, file=f"Panel_{n}/{name}.png", rect=[x, y, w, h],
                                   center=[x + w / 2 - W / 2, H / 2 - (y + h / 2)],
                                   cue=panel.get("cues", {}).get(name)))
            unknown = set(panel.get("cues", {})) - {name for name, _, _ in panel["layers"]}
            if unknown:
                sys.exit(f"Panel {n} cues a layer it does not have: {sorted(unknown)}")
            data["panels"].append(dict(index=n, caption=panel["caption"], layers=layers, motion=panel["motion"]))
            # the preview: every layer, then the caption plate as Unity will draw it
            body = "\n".join(svg for _, svg, _ in panel["layers"]) + plate_caption(panel["caption"].split("\n"))
            png = OUT_DIR / "preview" / f"Panel_{n}.png"
            sb.render(layer_page(body), png, W, H, 1, tmp)
            previews.append(png)
        overview(previews, tmp)
    (OUT_DIR / "panels.json").write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n")
    count = sum(len(p["layers"]) for p in data["panels"])
    print(f"wrote {count} layers in {len(PANELS)} panels, panels.json and previews to {OUT_DIR}")


if __name__ == "__main__":
    main()
