"""Draws the intro comic's character kit: the monkey family and the cobra.

The intro motion comic (4 to 5 panels between the loading page and the title)
stars a family of four: father, mother, the older brother (the hero) and his
little sister. This kit draws them and the cobra, no scenes, nothing in Unity
yet. Every piece is SVG in the logo's look, a thick Brown outline, flat fills
and a Shadow colored drop shadow down right (``docs/ui-style-doc.md``), saved
as a transparent PNG at 2x by headless Chrome.

The faces, the parents' marks of age, the props on the family and the cobra's
face are the team's own drawings in ``reference/``, traced into
``reference/traced.json`` by ``trace_reference.py``; the bodies, the heads and
the poses are drawn here.

Writes into ``tools/comic/characters/``:

- ``parts/``, every rig part as SVG + PNG, one set per family member (one
  drawing, resized), their props (``Hero_Bundle``, ``Father_BowTie``,
  ``Father_Cane``, ``Mother_Flower``, ``Sister_Bow``) and ``Cobra_Head``
- ``poses/``, the parts composed into poses, the head portraits per
  expression, and the cobra in the bush, as SVG + PNG
- ``rig.json``, each part's pivot, the body's sockets, the props and every pose
- ``overview.png``, the review sheet of all of it

The rig: each part is drawn around its pivot, the point it turns about, at
(0, 0). The body is the root; the head hangs on its ``neck`` socket, the arms
on ``shoulderLeft`` / ``shoulderRight``, the legs on ``hipLeft`` /
``hipRight``, the tail on ``tail`` (``tailBack`` from behind), the bundle and
the bow tie on ``back`` and ``neck``. A face part shares the head's pivot and
canvas, so it sits on the head with the head's own transform; a head prop sits
at its offset from the head's pivot, the cane upright in the left hand. Left
and right are the character's own: in the front view its left is on the
viewer's right; the back view mirrors the whole rig. Parts carry no shadow, a
pose adds one under its whole silhouette, so a limb never casts on the body.

Run from the repository root::

    python tools/comic/make_characters.py

Needs Pillow and Google Chrome (set ``CHROME`` to its path if it is not found).
"""

import base64
import json
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
OUT_DIR = HERE / "characters"
FONT_DIR = REPO / "Assets" / "_Monkobra" / "Fonts" / "Baloo2"
PNG_SCALE = 2

# palette, docs/ui-style-doc.md
BACKGROUND = "#0A1F12"
GOLD = "#EFB64D"
GOLD_DARK = "#D99A35"
BROWN = "#352016"
SHADOW = "#89501F"
CREAM = "#FFF2D2"
PANEL = "#102C1B"

OUTLINE = 6.0          # the outline band of every silhouette, in SVG units
FEATURE = 4.0          # nose and mouth lines
SHADOW_OFFSET = (6.0, 7.0)

TRACED = json.loads((HERE / "reference" / "traced.json").read_text())
FUR, EAR = "#805033", "#E3AF66"
PROP_BAND = 4.5        # the outline of a prop, thinner than a body's

# one drawing, four monkeys: head and body scale, girth widens the body only;
# faces name the traced expressions each one wears
HERO = dict(name="Hero", head=1.0, body=1.0, girth=1.0,
            faces={"Normal": "Hero_Face_Normal", "Hungry": "Face_Hungry", "Determined": "Face_Determined"})
FATHER = dict(name="Father", head=1.12, body=1.36, girth=1.06, marks="Father_Marks",
              faces={"Normal": "Father_Face_Normal", "Hungry": "Face_Hungry"})
MOTHER = dict(name="Mother", head=1.07, body=1.22, girth=1.0, marks="Mother_Marks", hair="Mother_Hair",
              faces={"Normal": "Mother_Face_Normal", "Hungry": "Face_Hungry"})
SISTER = dict(name="Sister", head=0.84, body=0.66, girth=1.0,
              faces={"Normal": "Sister_Face_Normal", "Hungry": "Face_HungrySister"})
CHARACTERS = [FATHER, MOTHER, HERO, SISTER]
for _c in CHARACTERS:
    _c.update(fur=FUR, face=CREAM, ear=EAR, hand=EAR)

# sockets on the body, in hero units, the body's pivot (the pelvis) at (0, 0)
SOCKETS = {
    "neck": (0, -76),
    "shoulderLeft": (32, -56),
    "shoulderRight": (-32, -56),
    "hipLeft": (18, 4),
    "hipRight": (-18, 4),
    "tail": (20, 2),
    "tailBack": (0, 8),
    "back": (0, -56),
}


# ---------------------------------------------------------------- drawing

def num(v):
    s = f"{v:.2f}".rstrip("0").rstrip(".")
    return "0" if s in ("-0", "") else s


_TOKEN = re.compile(r"[MLCQZ]|-?\d+(?:\.\d+)?")


def scale_path(d, sx, sy):
    """Scales an absolute M/L/C/Q/Z path, whose numbers are all x, y pairs."""
    out, is_x = [], True
    for t in _TOKEN.findall(d):
        if t.isalpha():
            out.append(t)
            continue
        out.append(num(float(t) * (sx if is_x else sy)))
        is_x = not is_x
    return " ".join(out)


def ink(outline=True, width=OUTLINE):
    if not outline:
        return ""
    return f' stroke="{BROWN}" stroke-width="{num(width)}" stroke-linejoin="round"'


class Pen:
    """Collects SVG elements; geometry is scaled, outline widths never are."""

    def __init__(self, sx=1.0, sy=None):
        self.sx = sx
        self.sy = sx if sy is None else sy
        self.s = (self.sx + self.sy) / 2
        self.items = []

    def xy(self, x, y):
        return x * self.sx, y * self.sy

    def shape(self, d, fill, outline=True):
        self.items.append(f'<path d="{scale_path(d, self.sx, self.sy)}" fill="{fill}"{ink(outline)}/>')

    def ellipse(self, cx, cy, rx, ry, fill, outline=True, rotate=0, opacity=None):
        cx, cy = self.xy(cx, cy)
        turn = f' transform="rotate({num(rotate)} {num(cx)} {num(cy)})"' if rotate else ""
        alpha = f' opacity="{num(opacity)}"' if opacity is not None else ""
        self.items.append(
            f'<ellipse cx="{num(cx)}" cy="{num(cy)}" rx="{num(rx * self.sx)}" ry="{num(ry * self.sy)}" '
            f'fill="{fill}"{ink(outline)}{turn}{alpha}/>')

    def rim(self, d):
        """Only the outline of a shape, drawn over what is inside it."""
        self.items.append(f'<path d="{scale_path(d, self.sx, self.sy)}" fill="none"{ink()}/>')

    def line(self, d, color=BROWN, width=FEATURE):
        self.items.append(
            f'<path d="{scale_path(d, self.sx, self.sy)}" fill="none" stroke="{color}" '
            f'stroke-width="{num(width)}" stroke-linecap="round" stroke-linejoin="round"/>')

    def tube(self, d, fill, width, band=OUTLINE):
        """A round-ended stroke with an outline: a dark stroke under a fill stroke."""
        w = width * self.s
        sd = scale_path(d, self.sx, self.sy)
        common = 'fill="none" stroke-linecap="round" stroke-linejoin="round"'
        self.items.append(f'<path d="{sd}" {common} stroke="{BROWN}" stroke-width="{num(w + 2 * band)}"/>')
        self.items.append(f'<path d="{sd}" {common} stroke="{fill}" stroke-width="{num(w)}"/>')

    def traced(self, shapes, band=PROP_BAND):
        """Shapes from traced.json; an outlined one gets a Brown rim outside its fill."""
        for t in shapes:
            rim = (f' stroke="{BROWN}" stroke-width="{num(2 * band)}" stroke-linejoin="round" '
                   f'paint-order="stroke"') if t.get("outline") else ""
            self.items.append(f'<path d="{scale_path(t["d"], self.sx, self.sy)}" fill="{t["fill"]}"{rim}/>')

    def raw(self, s):
        self.items.append(s)

    def svg(self):
        return "\n".join(self.items)


# ---------------------------------------------------------------- the monkey

BODY = ("M 0,-86 C 24,-86 34,-70 36,-50 C 38,-30 44,-8 36,6 C 28,18 -28,18 -36,6 "
        "C -44,-8 -38,-30 -36,-50 C -34,-70 -24,-86 0,-86 Z")
BELLY = ("M 0,-50 C 16,-50 25,-34 25,-18 C 25,0 14,8 0,8 C -14,8 -25,0 -25,-18 "
         "C -25,-34 -16,-50 0,-50 Z")
ARM = "M -11,-4 C -11,-15 11,-15 11,-4 L 9,44 L -9,44 Z"
LEG = "M -12,-6 C -12,-17 12,-17 12,-6 L 11,30 L -11,30 Z"
TAIL = "M 0,0 C 30,10 60,8 70,-14 C 78,-34 70,-56 52,-58 C 38,-60 32,-46 40,-40 C 46,-36 52,-42 49,-48"
TAIL_LIMP = "M 0,0 C 20,12 46,16 74,14 C 94,12 104,4 100,-5 C 96,-13 86,-11 86,-4"


def head(c, back=False):
    """The logo head, traced: the parents wear their marks of age, the mother her
    lighter streak. The outline goes on last, so it is one even band all round."""
    shape = TRACED["Head"]
    pen = Pen(c["head"])
    pen.traced([dict(fill=c["fur"], d=shape["curl"], outline=True)], band=3.5)
    pen.shape(shape["silhouette"], c["fur"], outline=False)
    if not back:
        for d in shape["inner"]:
            pen.shape(d, c["ear"], outline=False)
        if c.get("hair"):
            pen.traced(TRACED[c["hair"]])
        pen.shape(shape["face"], c["face"], outline=False)
        if c.get("marks"):
            pen.traced(TRACED[c["marks"]])
    pen.rim(shape["silhouette"])
    return pen.svg()


def face(c, key):
    """A traced expression, on the head's own pivot."""
    pen = Pen(c["head"])
    pen.traced(TRACED[c["faces"][key]])
    return pen.svg()


def body(c, back=False):
    pen = Pen(c["body"] * c["girth"], c["body"])
    pen.shape(BODY, c["fur"])
    if not back:
        pen.shape(BELLY, c["face"], outline=False)
    return pen.svg()


def arm(c, side, fist=False):
    """side +1 is the character's left arm: in the front view it hangs on the viewer's right."""
    pen = Pen(c["body"])
    pen.shape(ARM, c["fur"])
    if fist:
        pen.ellipse(0, 53, 16, 14.5, c["hand"])
        pen.line(f"M {-12 * side},51 C {-6 * side},47 {2 * side},48 {5 * side},53", width=3.4)
        pen.line(f"M {4 * side},60 L {5 * side},65.5", width=3)
        pen.line(f"M {10 * side},56 L {11.5 * side},61", width=3)
    else:
        pen.ellipse(-10 * side, 46, 6, 8.5, c["hand"], rotate=-25 * side)
        pen.ellipse(0, 52, 13, 14, c["hand"])
    return pen.svg()


def leg(c, side):
    """side +1 is the character's left leg, its foot turned out to the viewer's right."""
    pen = Pen(c["body"])
    pen.shape(LEG, c["fur"])
    pen.ellipse(7 * side, 35, 18, 11, c["hand"])
    return pen.svg()


def tail(c, limp=False):
    pen = Pen(c["body"])
    pen.tube(TAIL_LIMP if limp else TAIL, c["fur"], 9)
    return pen.svg()


def bundle(c):
    """A cloth bundle tied on the back, its corners knotted over the shoulders."""
    pen = Pen(1.2 * c["body"])
    for side in (-1, 1):
        pen.shape(f"M {-6 * side},-2 C {-14 * side},-8 {-24 * side},-20 {-22 * side},-30 "
                  f"C {-13 * side},-32 {-2 * side},-18 {4 * side},-8 Z", GOLD)
    pen.shape("M -36,4 C -46,28 -38,62 0,64 C 38,62 46,28 36,4 C 26,-6 -26,-6 -36,4 Z", GOLD)
    pen.line("M -9,6 C -12,20 -16,32 -20,44", GOLD_DARK, 3.5)
    pen.line("M 10,6 C 13,18 16,28 18,38", GOLD_DARK, 3.5)
    for x, y in ((-24, 22), (0, 30), (24, 22), (-12, 48), (13, 50), (-30, 42), (30, 40)):
        pen.ellipse(x, y, 4.2, 4.2, CREAM, outline=False)
    pen.ellipse(0, -3, 11, 8, GOLD)
    return pen.svg()


def cane(c):
    """A walking stick held at its crook, reaching the ground in the stand."""
    pen = Pen(c["body"])
    pen.tube("M 0,58 L 0,-2 C 0,-10 -5,-15 -12,-14", "#B88A51", 6.5, band=PROP_BAND)
    return pen.svg()


def traced_prop(key, c, scale="head"):
    pen = Pen(c[scale])
    pen.traced(TRACED[key]["shapes"])
    return pen.svg()


HAND_Y = 52  # the hand's center on an arm, in body units
# a family member's props: where each hangs, and where it is drawn in a pose
PROPS = {
    "Hero": [dict(part="Bundle", at="back", nudge=(0, 0), z=("after", "HeadBack"), poses={"BackBundle"},
                  make=bundle)],
    "Father": [dict(part="BowTie", at="neck", nudge=(0, 17), z=("after", "Body"),
                    make=lambda c: traced_prop("Prop_BowTie", c)),
               dict(part="Cane", at="hand:ArmLeft", nudge=(0, 0), z=("before", "ArmLeft"), poses={"Stand"},
                    make=cane)],
    "Mother": [dict(part="Flower", at="on:Head", nudge=tuple(TRACED["Prop_Flower"]["anchor"]),
                    z=("after", "Head"), make=lambda c: traced_prop("Prop_Flower", c))],
    "Sister": [dict(part="Bow", at="on:Head", nudge=tuple(TRACED["Prop_Bow"]["anchor"]),
                    z=("after", "Head"), make=lambda c: traced_prop("Prop_Bow", c))],
}


def part_set(c):
    """Every rig part of one family member, name -> SVG snippet."""
    parts = {"Head": head(c)}
    parts.update({f"Face_{k}": face(c, k) for k in c["faces"]})
    parts.update({
        "Body": body(c),
        "ArmLeft": arm(c, 1),
        "ArmRight": arm(c, -1),
        "LegLeft": leg(c, 1),
        "LegRight": leg(c, -1),
        "Tail": tail(c),
        "Tail_Limp": tail(c, limp=True),
    })
    if c is HERO:
        parts.update({"HeadBack": head(c, back=True), "BodyBack": body(c, back=True),
                      "ArmLeft_Fist": arm(c, 1, fist=True), "ArmRight_Fist": arm(c, -1, fist=True)})
    for prop in PROPS[c["name"]]:
        parts[prop["part"]] = prop["make"](c)
    return parts


PART_SOCKET = {
    "Head": "neck", "HeadBack": "neck",
    "ArmLeft": "shoulderLeft", "ArmLeft_Fist": "shoulderLeft",
    "ArmRight": "shoulderRight", "ArmRight_Fist": "shoulderRight",
    "LegLeft": "hipLeft", "LegRight": "hipRight",
    "Tail": "tail", "Tail_Limp": "tail", "Bundle": "back", "BowTie": "neck",
    "Cane": "the left hand, upright", "Flower": "head, at its offset", "Bow": "head, at its offset",
}


# ---------------------------------------------------------------- the rig

def m_mul(a, b):
    """a after b, for 2D affine (a, b, c, d, e, f) as in SVG's matrix()."""
    a1, b1, c1, d1, e1, f1 = a
    a2, b2, c2, d2, e2, f2 = b
    return (a1 * a2 + c1 * b2, b1 * a2 + d1 * b2, a1 * c2 + c1 * d2, b1 * c2 + d1 * d2,
            a1 * e2 + c1 * f2 + e1, b1 * e2 + d1 * f2 + f1)


def m_move(x, y):
    return (1, 0, 0, 1, x, y)


def m_turn(deg):
    r = math.radians(deg)
    return (math.cos(r), math.sin(r), -math.sin(r), math.cos(r), 0, 0)


def m_scale(sx, sy):
    return (sx, 0, 0, sy, 0, 0)


def m_svg(m):
    return "matrix(" + " ".join(num(v) if i < 4 else num(v) for i, v in enumerate(
        (round(m[0], 4), round(m[1], 4), round(m[2], 4), round(m[3], 4), m[4], m[5]))) + ")"


def socket(c, name):
    x, y = SOCKETS[name]
    return x * c["body"] * c["girth"], y * c["body"]


# Each pose lists its parts in draw order, back to front:
# (part, where, rotation in degrees clockwise, (dx, dy) nudge in body units).
# where is "body" (the root), a socket, or "on:<part>" to ride on a part placed
# in the pose (the nudge then in that part's units); a character's PROPS are
# slotted in. mirror flips the rig (back view).
POSES = {
    "Stand": dict(mirror=False, items=[
        ("Tail", "tail", 0, (0, 0)),
        ("LegRight", "hipRight", 5, (0, 0)),
        ("LegLeft", "hipLeft", -5, (0, 0)),
        ("ArmRight", "shoulderRight", 24, (0, 0)),
        ("ArmLeft", "shoulderLeft", -24, (0, 0)),
        ("Body", "body", 0, (0, 0)),
        ("Head", "neck", 0, (0, 0)),
        ("Face_Normal", "on:Head", 0, (0, 0)),
    ]),
    "SitSlumped": dict(mirror=False, items=[
        ("Tail_Limp", "tail", 0, (0, 0)),
        ("LegRight", "hipRight", 50, (0, 0)),
        ("LegLeft", "hipLeft", -50, (0, 0)),
        ("Body", "body", 0, (0, 0)),
        ("ArmRight", "shoulderRight", -24, (0, 0)),
        ("ArmLeft", "shoulderLeft", 24, (0, 0)),
        ("Head", "neck", 9, (0, 11)),
        ("Face_Hungry", "on:Head", 0, (0, 6)),
    ]),
    "Determined": dict(mirror=False, items=[
        ("Tail", "tail", -18, (0, 0)),
        ("LegRight", "hipRight", 14, (0, 0)),
        ("LegLeft", "hipLeft", -14, (0, 0)),
        ("ArmRight_Fist", "shoulderRight", 130, (0, 0)),
        ("ArmLeft_Fist", "shoulderLeft", -22, (0, 0)),
        ("Body", "body", 0, (0, 0)),
        ("Head", "neck", -4, (0, -2)),
        ("Face_Determined", "on:Head", 0, (0, -3)),
    ]),
    "BackBundle": dict(mirror=True, items=[
        ("LegRight", "hipRight", 4, (0, 0)),
        ("LegLeft", "hipLeft", -12, (0, -5)),
        ("ArmRight", "shoulderRight", 30, (0, 0)),
        ("ArmLeft", "shoulderLeft", -22, (0, 0)),
        ("BodyBack", "body", 0, (0, 0)),
        ("Tail", "tailBack", 24, (0, 0)),
        ("HeadBack", "neck", -4, (0, 0)),
    ]),
}

# the poses each family member is drawn in
CAST = {"Hero": ["Stand", "SitSlumped", "Determined", "BackBundle"],
        "Father": ["Stand", "SitSlumped"], "Mother": ["Stand", "SitSlumped"],
        "Sister": ["Stand", "SitSlumped"]}


def pose_items(c, pose):
    items = [dict(part=p, at=w, rot=r, nudge=n) for p, w, r, n in POSES[pose]["items"]]
    for prop in PROPS[c["name"]]:
        if prop.get("poses") and pose not in prop["poses"]:
            continue
        where, ref = prop["z"]
        names = [i["part"] for i in items]
        if ref in names:
            items.insert(names.index(ref) + (where == "after"),
                         dict(part=prop["part"], at=prop["at"], rot=0, nudge=prop["nudge"]))
    return items


def compose(c, parts, items, mirror=False):
    """Items as one SVG group, its shadow under it; a part placed on another waits for it."""
    base = m_scale(-1, 1) if mirror else (1, 0, 0, 1, 0, 0)
    units = lambda part: c["head"] if part.startswith("Head") else c["body"]
    mats, pending = {}, list(items)
    while pending:
        before = len(pending)
        for it in list(pending):
            at, (dx, dy) = it["at"], it["nudge"]
            if at == "body":
                m = base
            elif ":" in at:
                kind, parent = at.split(":")
                if parent not in mats:
                    continue
                pm = mats[parent]
                if kind == "on":
                    k = units(parent)
                    m = m_mul(pm, m_mul(m_move(dx * k, dy * k), m_turn(it["rot"])))
                else:  # hand: upright at the hand's center
                    hy = HAND_Y * c["body"]
                    m = m_mul(m_move(pm[2] * hy + pm[4], pm[3] * hy + pm[5]), m_turn(it["rot"]))
            else:
                sx, sy = socket(c, at)
                m = m_mul(base, m_mul(m_move(sx + dx * c["body"], sy + dy * c["body"]), m_turn(it["rot"])))
            mats[it["part"]] = m
            pending.remove(it)
        if len(pending) == before:
            sys.exit(f"cannot place {[i['part'] for i in pending]} for {c['name']}")
    groups = [f'<g transform="{m_svg(mats[it["part"]])}">\n{parts[it["part"]]}\n</g>' for it in items]
    return with_shadow("\n".join(groups))


def pose_svg(c, parts, pose):
    return compose(c, parts, pose_items(c, pose), POSES[pose]["mirror"])


def portrait(c, parts, face_key):
    items = [dict(part="Head", at="body", rot=0, nudge=(0, 0)),
             dict(part=f"Face_{face_key}", at="on:Head", rot=0, nudge=(0, 0))]
    items += [dict(part=p["part"], at=p["at"], rot=0, nudge=p["nudge"])
              for p in PROPS[c["name"]] if p["at"] == "on:Head"]
    return compose(c, parts, items)


def silhouette(svg, color):
    svg = re.sub(r'(fill|stroke)="#[0-9A-Fa-f]{6}"', lambda m: f'{m.group(1)}="{color}"', svg)
    return svg


def with_shadow(svg):
    dx, dy = SHADOW_OFFSET
    return (f'<g transform="translate({num(dx)} {num(dy)})">\n{silhouette(svg, SHADOW)}\n</g>\n'
            f'<g>\n{svg}\n</g>')


# ---------------------------------------------------------------- the cobra

# after the team's Cobra_InBush: the cobra's whole head peeks from the leaves,
# its eyes glowing in rings; the leaves are two-tone, a light rib and dark veins,
# the front ones rimmed in the bush's darkest green
COBRA_GREEN, COBRA_INK = "#21A663", "#382216"
LEAF_EDGE, BUSH_DARK = "#08291F", "#08291F"
BACK_LEAVES = [("#1B6342", "#124B35"), ("#19583A", "#0D3829"), ("#225A3C", "#164D36"), ("#1E5B3B", "#124632")]
FRONT_LEAVES = [("#2C7C4A", "#19583A"), ("#257248", "#124C34")]


def cobra_head():
    """The head, its pivot at its middle; the face is the team's, traced."""
    pen = Pen(1.0)
    pen.raw(f'<ellipse cx="0" cy="0" rx="100.4" ry="96.4" fill="{COBRA_GREEN}" '
            f'stroke="{COBRA_INK}" stroke-width="10"/>')
    pen.traced(TRACED["Cobra_Face"])
    return pen.svg()


def leaf(x, y, angle, length, width, tones, rib, edge=False, veins=True):
    L, W = length, width
    light, dark = tones
    top = f"M 0,0 C {num(L * .3)},{num(-W)} {num(L * .75)},{num(-W * .8)} {num(L)},0 Z"
    whole = (f"M 0,0 C {num(L * .3)},{num(-W)} {num(L * .75)},{num(-W * .8)} {num(L)},0 "
             f"C {num(L * .75)},{num(W * .8)} {num(L * .3)},{num(W)} 0,0 Z")
    rim = (f' stroke="{LEAF_EDGE}" stroke-width="8" stroke-linejoin="round" paint-order="stroke"'
           if edge else "")
    out = [f'<g transform="translate({num(x)} {num(y)}) rotate({num(angle)})">',
           f'<path d="{whole}" fill="{light}"{rim}/>', f'<path d="{top}" fill="{dark}"/>']
    if veins:
        for t in (.28, .5, .72):
            out.append(f'<path d="M {num(L * t)},0 L {num(L * (t - .1))},{num(W * .5)}" fill="none" '
                       f'stroke="{dark}" stroke-width="3" stroke-linecap="round"/>')
    out.append(f'<path d="M {num(L * .04)},0 Q {num(L * .5)},{num(-W * .06)} {num(L * .93)},0" fill="none" '
               f'stroke="{rib}" stroke-width="3.5" stroke-linecap="round"/>')
    out.append("</g>")
    return "\n".join(out)


def vine(d):
    common = 'fill="none" stroke-linecap="round" stroke-linejoin="round"'
    return (f'<path d="{d}" {common} stroke="#0D3829" stroke-width="12"/>'
            f'<path d="{d}" {common} stroke="#19583A" stroke-width="6"/>')


def twig(x0, y0, x1, y1, bend, count):
    """A thin stem with small leaves along it, alternately left and right."""
    cx, cy = (x0 + x1) / 2 + bend, (y0 + y1) / 2
    out = [f'<path d="M {num(x0)},{num(y0)} Q {num(cx)},{num(cy)} {num(x1)},{num(y1)}" fill="none" '
           f'stroke="#225A3C" stroke-width="5" stroke-linecap="round"/>']
    for i in range(count):
        t = 0.25 + 0.75 * i / max(count - 1, 1)
        px = (1 - t) ** 2 * x0 + 2 * (1 - t) * t * cx + t * t * x1
        py = (1 - t) ** 2 * y0 + 2 * (1 - t) * t * cy + t * t * y1
        tx = 2 * (1 - t) * (cx - x0) + 2 * t * (x1 - cx)
        ty = 2 * (1 - t) * (cy - y0) + 2 * t * (y1 - cy)
        heading = math.degrees(math.atan2(ty, tx))
        side = 1 if i % 2 else -1
        out.append(leaf(px, py, heading + 42 * side, 34, 12, ("#245E3E", "#1B5235"), "#245E3E", veins=False))
    return "\n".join(out)


def cobra_in_bush(head_svg):
    """The cobra's head peeking over a bush, vines hanging behind it."""
    out = [vine("M -150,-250 C -185,-185 -105,-150 -132,-90 C -148,-55 -118,-30 -130,4"),
           vine("M 150,-250 C 120,-190 196,-150 168,-92 C 150,-56 178,-30 160,6"),
           twig(-118, 70, -52, -205, -28, 9), twig(172, 60, 132, -120, 22, 7)]
    for x, y, a, length, width, tones in (
            (-40, 40, -118, 250, 62, BACK_LEAVES[0]), (40, 40, -62, 250, 62, BACK_LEAVES[3]),
            (-10, 20, -96, 215, 52, BACK_LEAVES[2]), (20, 20, -80, 225, 54, BACK_LEAVES[1]),
            (-80, 60, -152, 205, 58, BACK_LEAVES[1]), (80, 60, -26, 210, 58, BACK_LEAVES[0]),
            (-110, 0, -138, 150, 44, BACK_LEAVES[3]), (110, 0, -42, 150, 44, BACK_LEAVES[2])):
        out.append(leaf(x, y, a, length, width, tones, "#2C7C4A"))
    out.append(head_svg)
    for cx, cy, rx, ry in ((0, 182, 240, 92), (-190, 190, 120, 80), (190, 190, 120, 80),
                           (-100, 152, 95, 54), (100, 152, 95, 54)):
        out.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="{BUSH_DARK}"/>')
    # where the team's drawing has them: the side leaves dip across the head's lower
    # corners, two more rise from the bush in a V under the face
    for x, y, a, length, width, tones, edge in (
            (-170, 236, -160, 170, 50, BACK_LEAVES[1], False), (170, 236, -20, 170, 50, BACK_LEAVES[1], False),
            (-60, 246, -105, 100, 30, BACK_LEAVES[3], False), (70, 246, -72, 100, 30, BACK_LEAVES[3], False),
            (-216, 19, 22, 190, 50, FRONT_LEAVES[1], True), (219, 14, 156, 192, 50, FRONT_LEAVES[1], True),
            (-6, 182, -137, 142, 44, FRONT_LEAVES[0], True), (-2, 182, -36, 143, 44, FRONT_LEAVES[0], True)):
        out.append(leaf(x, y, a, length, width, tones, "#438B50", edge=edge))
    return "\n".join(out)


# ---------------------------------------------------------------- rendering

def find_chrome():
    if os.environ.get("CHROME"):
        return os.environ["CHROME"]
    for p in ("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
              r"C:\Program Files\Google\Chrome\Application\chrome.exe",
              r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"):
        if os.path.exists(p):
            return p
    for name in ("google-chrome", "chromium", "chromium-browser", "chrome"):
        if shutil.which(name):
            return shutil.which(name)
    sys.exit("Google Chrome not found, set CHROME to its path")


CHROME = find_chrome()


def shoot(page, png, width, height, scale):
    with tempfile.TemporaryDirectory() as profile:
        subprocess.run(
            [CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
             "--no-default-browser-check", f"--user-data-dir={profile}",
             "--default-background-color=00000000", f"--force-device-scale-factor={scale}",
             f"--window-size={int(width)},{int(height)}", f"--screenshot={png}", Path(page).as_uri()],
            check=True, capture_output=True)


def svg_doc(body_svg, x0, y0, w, h, note=""):
    comment = f"<!-- {note} -->\n" if note else ""
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{num(w)}" height="{num(h)}" '
            f'viewBox="{num(x0)} {num(y0)} {num(w)} {num(h)}">\n{comment}{body_svg}\n</svg>\n')


# Chrome starts slowly, so every piece is drawn on one big sheet per pass and
# cut out of it: one sheet to measure the bounds, one to draw the 2x PNGs.
CELL, COLUMNS = 720, 8      # measuring cells; the pivot sits at (CELL / 2, PIVOT_Y)
PIVOT_Y = 430
PAD, GAP, SHEET_W = 3, 24, 3000


def measure(jobs, tmp):
    """Sets each job's bounds around its pivot: x0, y0, w, h in SVG units."""
    rows = math.ceil(len(jobs) / COLUMNS)
    cells = []
    for i, job in enumerate(jobs):
        cx, cy = (i % COLUMNS) * CELL + CELL // 2, (i // COLUMNS) * CELL + PIVOT_Y
        cells.append(f'<g transform="translate({cx} {cy})">\n{job["svg"]}\n</g>')
    page, png = tmp / "measure.svg", tmp / "measure.png"
    page.write_text(svg_doc("\n".join(cells), 0, 0, COLUMNS * CELL, rows * CELL))
    shoot(page, png, COLUMNS * CELL, rows * CELL, 1)
    alpha = Image.open(png).getchannel("A")
    for i, job in enumerate(jobs):
        left, top = (i % COLUMNS) * CELL, (i // COLUMNS) * CELL
        box = alpha.crop((left, top, left + CELL, top + CELL)).getbbox()
        if not box or min(box[:2]) == 0 or max(box[2:]) == CELL:
            sys.exit(f"{job['name']} does not fit its measuring cell: {box}")
        x0 = math.floor(box[0] - CELL // 2 - PAD)
        y0 = math.floor(box[1] - PIVOT_Y - PAD)
        job.update(x0=x0, y0=y0, w=math.ceil(box[2] - CELL // 2 + PAD) - x0,
                   h=math.ceil(box[3] - PIVOT_Y + PAD) - y0)
    by_name = {j["name"]: j for j in jobs}
    for job in jobs:
        if job.get("box_of"):  # a face shares its head's canvas, so the two line up as is
            job.update({k: by_name[job["box_of"]][k] for k in ("x0", "y0", "w", "h")})


def export(jobs, tmp):
    """Writes each job's SVG and draws all PNGs on one 2x sheet, then cuts them out."""
    x = y = row_h = 0
    for job in sorted(jobs, key=lambda j: -j["h"]):
        if x + job["w"] > SHEET_W:
            x, y, row_h = 0, y + row_h + GAP, 0
        job.update(sheet_x=x, sheet_y=y)
        x += job["w"] + GAP
        row_h = max(row_h, job["h"])
    sheet_h = y + row_h
    placed = []
    for job in jobs:
        folder = OUT_DIR / job["folder"]
        note = job.get("note", "")
        if job["folder"] == "parts":
            note = f"pivot at (0, 0) in viewBox units; {note}".strip("; ")
        (folder / f"{job['name']}.svg").write_text(
            svg_doc(job["svg"], job["x0"], job["y0"], job["w"], job["h"], note))
        placed.append(f'<svg x="{job["sheet_x"]}" y="{job["sheet_y"]}" width="{job["w"]}" height="{job["h"]}" '
                      f'viewBox="{job["x0"]} {job["y0"]} {job["w"]} {job["h"]}" overflow="hidden">\n'
                      f'{job["svg"]}\n</svg>')
    page, png = tmp / "export.svg", tmp / "export.png"
    page.write_text(svg_doc("\n".join(placed), 0, 0, SHEET_W, sheet_h))
    shoot(page, png, SHEET_W, sheet_h, PNG_SCALE)
    sheet = Image.open(png)
    for job in jobs:
        left, top = job["sheet_x"] * PNG_SCALE, job["sheet_y"] * PNG_SCALE
        sheet.crop((left, top, left + job["w"] * PNG_SCALE, top + job["h"] * PNG_SCALE)).save(
            OUT_DIR / job["folder"] / f"{job['name']}.png", optimize=True)


# ---------------------------------------------------------------- jobs

def build_jobs():
    jobs = []
    sets = {c["name"]: part_set(c) for c in CHARACTERS}
    for c in CHARACTERS:
        for part, svg in sets[c["name"]].items():
            jobs.append(dict(name=f"{c['name']}_{part}", folder="parts", svg=svg,
                             character=c["name"], part=part,
                             box_of=f"{c['name']}_Head" if part.startswith("Face_") else None))
    for c in CHARACTERS:
        for pose in CAST[c["name"]]:
            jobs.append(dict(name=f"{c['name']}_{pose}", folder="poses",
                             svg=pose_svg(c, sets[c["name"]], pose), character=c["name"], pose=pose))
        for key in c["faces"]:
            jobs.append(dict(name=f"{c['name']}_Portrait_{key}", folder="poses",
                             svg=portrait(c, sets[c["name"]], key), character=c["name"]))
    head = cobra_head()
    jobs.append(dict(name="Cobra_Head", folder="parts", svg=head, character="Cobra", part="Head",
                     attach="free: the head of the bush scene, its pivot at its middle"))
    jobs.append(dict(name="Cobra_InBush", folder="poses", svg=cobra_in_bush(head), character="Cobra",
                     note="after the team's reference/Cobra_InBush.png; no drop shadow"))
    return jobs


def write_rig(jobs):
    by_name = {j["name"]: j for j in jobs}
    rig = {
        "about": "Intro comic character rig. Units: 1 SVG unit = 1 px in the SVG, "
                 f"{PNG_SCALE} px in the PNG. pivotPx is in PNG pixels from the top-left; "
                 "unityPivot is normalized from the bottom-left, as a Sprite's custom pivot. "
                 "Sockets are in SVG units from the body's pivot, y down. A pose's rotation is "
                 "in degrees clockwise on screen, so a Unity z rotation is its negative; a nudge "
                 "is in body units off a socket, or in the parent's units on a part. "
                 "Left and right are the character's own; the back view mirrors the rig.",
        "pngScale": PNG_SCALE,
        "parts": {},
        "sockets": {},
        "props": {},
        "poses": {},
    }
    for j in jobs:
        if j["folder"] != "parts":
            continue
        px, py = -j["x0"] * PNG_SCALE, -j["y0"] * PNG_SCALE
        W, H = j["w"] * PNG_SCALE, j["h"] * PNG_SCALE
        part = j["part"]
        rig["parts"][j["name"]] = {
            "svg": f"parts/{j['name']}.svg", "png": f"parts/{j['name']}.png",
            "sizePx": [W, H], "pivotPx": [px, py],
            "unityPivot": [round(px / W, 4), round(1 - py / H, 4)],
            "attachTo": j.get("attach") or ("head pivot" if part.startswith("Face_") else
                                            "root" if part.startswith("Body") else
                                            PART_SOCKET.get(part, "")),
        }
    for c in CHARACTERS:
        rig["sockets"][c["name"]] = {k: [round(v, 2) for v in socket(c, k)] for k in SOCKETS}
        rig["props"][c["name"]] = [
            dict(part=f"{c['name']}_{p['part']}", at=p["at"], nudge=list(p["nudge"]),
                 drawn=f"{p['z'][0]} {p['z'][1]}", poses=sorted(p.get("poses") or CAST[c["name"]]))
            for p in PROPS[c["name"]]]
    for name, spec in POSES.items():
        rig["poses"][name] = {
            "mirror": spec["mirror"],
            "drawOrder": [dict(part=p, at=w, rotation=r, nudge=list(n)) for p, w, r, n in spec["items"]],
            "drawn": [f"{c}_{name}" for c in CAST if name in CAST[c]],
        }
    (OUT_DIR / "rig.json").write_text(json.dumps(rig, indent=2) + "\n")
    return by_name


# ---------------------------------------------------------------- overview

def font_face(name, file):
    data = base64.b64encode((FONT_DIR / file).read_bytes()).decode()
    return f"@font-face{{font-family:'{name}';src:url(data:font/ttf;base64,{data});}}"


def overview(by_name, tmp):
    k = 1.2

    def img(name, scale=k, extra=""):
        j = by_name[name]
        src = (OUT_DIR / j["folder"] / f"{name}.svg").as_uri()
        return f'<img src="{src}" style="width:{j["w"] * scale:.1f}px;height:{j["h"] * scale:.1f}px{extra}">'

    def card(inner, title, sub="", cls=""):
        return (f'<div class="card {cls}"><div class="art">{inner}</div>'
                f'<div class="label">{title}</div><div class="sub">{sub}</div></div>')

    def rig_cell(name, scale=1.2):
        j = by_name[name]
        px, py = -j["x0"] * scale, -j["y0"] * scale
        dots = ""
        if j["part"] in ("Body", "BodyBack") and j["character"] == "Hero":
            for key, (sx, sy) in SOCKETS.items():
                if key == "tailBack" and j["part"] == "Body" or key == "tail" and j["part"] == "BodyBack":
                    continue
                dots += f'<span class="sock" style="left:{px + sx * scale:.1f}px;top:{py + sy * scale:.1f}px"></span>'
        label = name.replace("Hero_", "").replace("_", " ")
        return (f'<div class="rig"><div class="stage" style="width:{j["w"] * scale:.1f}px;height:{j["h"] * scale:.1f}px">'
                f'{img(name, scale)}{dots}<span class="pivot" style="left:{px:.1f}px;top:{py:.1f}px"></span></div>'
                f'<div class="sub">{label}</div></div>')

    poses = "".join([
        card(img("Hero_Stand"), "Stand 站立", "normal face"),
        card(img("Hero_SitSlumped"), "Slumped 垂头丧气地坐着", "hungry face, head sunk, hugging an empty belly"),
        card(img("Hero_Determined"), "Determined 握拳站起", "determined face, fist raised"),
        card(img("Hero_BackBundle"), "Setting off 背影", "back view, bundle on the back"),
    ])
    faces = "".join([
        card(img("Hero_Portrait_Normal", 1.25), "Normal 普通", "hero, as the family sheet"),
        card(img("Hero_Portrait_Hungry", 1.25), "Hungry 饥饿", "hero, father, mother"),
        card(img("Hero_Portrait_Determined", 1.25), "Determined 坚定", "hero"),
        card(img("Sister_Portrait_Hungry", 1.25), "Hungry 饥饿 · sister 妹妹", "her own, big pleading eyes"),
    ])
    cast = ("Father", "Mother", "Hero", "Sister")
    family = (f'<div class="line">{"".join(img(f"{n}_Stand") for n in cast)}</div>'
              f'<div class="line">{"".join(img(f"{n}_SitSlumped") for n in cast)}</div>')
    family_card = card(family, "Family 一家四口",
                       "father 爸爸 · mother 妈妈 · hero, the older brother 哥哥 · sister 妹妹; "
                       "standing as usual, then hungry", "wide")
    cobra_card = card(img("Cobra_InBush", 0.85), "Cobra 眼镜蛇", "after the team's drawing", "mid")
    hero_parts = ["Head", "HeadBack", "Face_Normal", "Face_Hungry", "Face_Determined", "Body", "BodyBack",
                  "ArmRight", "ArmLeft", "ArmRight_Fist", "ArmLeft_Fist", "LegRight", "LegLeft", "Tail",
                  "Tail_Limp", "Bundle"]
    rig = "".join(rig_cell(f"Hero_{p}") for p in hero_parts)
    rig += "".join(rig_cell(n) for n in ("Father_BowTie", "Father_Cane", "Mother_Flower", "Sister_Bow",
                                         "Cobra_Head"))

    html = f"""<!doctype html><html><head><meta charset="utf-8"><style>
{font_face('B2', 'Baloo2-SemiBold.ttf')}
{font_face('B2X', 'Baloo2-ExtraBold.ttf')}
html,body{{margin:0;background:transparent}}
.sheet{{width:2000px;box-sizing:border-box;padding:44px 48px 52px;background:{BACKGROUND};
  color:{CREAM};font-family:'B2','PingFang SC','Microsoft YaHei',sans-serif}}
h1{{font-family:'B2X','PingFang SC',sans-serif;font-size:64px;line-height:1.1;margin:0;color:{GOLD};
  -webkit-text-stroke:10px {BROWN};paint-order:stroke fill;text-shadow:5px 6px 0 {SHADOW}}}
.intro{{font-size:22px;opacity:.85;margin:8px 0 6px}}
h2{{font-family:'B2X','PingFang SC',sans-serif;font-size:34px;color:{GOLD};margin:34px 0 14px}}
.row{{display:flex;gap:24px;align-items:stretch}}
.card{{flex:1;background:{PANEL};border-radius:22px;padding:22px 18px 18px;display:flex;
  flex-direction:column;align-items:center}}
.card.wide{{flex:2.2}}
.card.mid .art{{justify-content:center}}
.art{{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:flex-end;gap:26px;min-height:120px}}
.line{{display:flex;align-items:flex-end;gap:34px}}
.label{{font-family:'B2X','PingFang SC',sans-serif;font-size:28px;margin-top:14px}}
.sub{{font-size:18px;opacity:.72;text-align:center}}
.rigs{{display:flex;flex-wrap:wrap;gap:22px 30px;align-items:flex-end;background:{PANEL};border-radius:22px;padding:26px}}
.rig{{display:flex;flex-direction:column;align-items:center;gap:6px}}
.stage{{position:relative}}
.stage img{{position:absolute;left:0;top:0}}
.pivot{{position:absolute;width:14px;height:14px;margin:-9px 0 0 -9px;border:2px solid #FF4FA0;border-radius:50%;
  box-shadow:0 0 0 2px rgba(0,0,0,.5)}}
.pivot::before,.pivot::after{{content:'';position:absolute;background:#FF4FA0}}
.pivot::before{{left:6px;top:-6px;width:2px;height:26px}}
.pivot::after{{top:6px;left:-6px;height:2px;width:26px}}
.sock{{position:absolute;width:10px;height:10px;margin:-5px 0 0 -5px;border-radius:50%;background:#39D0FF;
  box-shadow:0 0 0 2px rgba(0,0,0,.6)}}
.key{{display:flex;gap:28px;font-size:18px;opacity:.85;margin-top:10px}}
.key i{{display:inline-block;width:12px;height:12px;border-radius:50%;margin-right:8px;vertical-align:-1px}}
</style></head><body><div class="sheet">
<h1>Intro Comic · Character Kit</h1>
<div class="intro">Round 2: the team's expressions, the family of four and the cobra, traced from the team's drawings onto the round 1 bodies. Parts are clean; a pose adds the Shadow drop shadow under its whole silhouette.</div>
<h2>Poses 姿势</h2><div class="row">{poses}</div>
<h2>Expressions 表情</h2><div class="row">{faces}</div>
<h2>Family &amp; Cobra 一家四口与眼镜蛇</h2><div class="row">{family_card}{cobra_card}</div>
<h2>Rig parts 部件与轴点</h2><div class="rigs">{rig}</div>
<div class="key"><span><i style="background:#FF4FA0"></i>pivot, the point a part turns about</span>
<span><i style="background:#39D0FF"></i>sockets on the body: neck (bow tie), shoulders, hips, tail, back (bundle)</span>
<span>Left / Right are the character's own: its left arm hangs on the viewer's right.</span></div>
</div></body></html>"""
    page = tmp / "overview.html"
    page.write_text(html, encoding="utf-8")
    png = OUT_DIR / "overview.png"
    shoot(page, png, 2000, 3400, 1)
    im = Image.open(png)
    im.crop(im.getchannel("A").getbbox()).save(png)


# ---------------------------------------------------------------- main

def main():
    for sub in ("parts", "poses"):
        folder = OUT_DIR / sub
        if folder.exists():
            shutil.rmtree(folder)
        folder.mkdir(parents=True)
    jobs = build_jobs()
    with tempfile.TemporaryDirectory() as tmp_name:
        tmp = Path(tmp_name)
        measure(jobs, tmp)
        export(jobs, tmp)
        by_name = write_rig(jobs)
        overview(by_name, tmp)
    print(f"wrote {len(jobs)} SVG + PNG pairs, rig.json and overview.png into {OUT_DIR}")


if __name__ == "__main__":
    main()
