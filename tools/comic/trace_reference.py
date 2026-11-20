"""Traces the team's reference art for the intro comic into vector paths.

The expressions, the family's marks and accessories, and the cobra's face were
drawn by the team as flat PNGs in ``tools/comic/reference/``. This turns every
colored shape in them into a path in the rig's own units, so
``make_characters.py`` draws them as is on the rig's heads.

A layer names a fill color, the color it lies on, and the region it may come
from; each pixel gets the share of the fill in its blend with that color, so
an edge is traced where the blend is half and half, and a shape keeps the
reference's exact outline. A monkey face maps by its cream face mask onto the
rig's head units at ``HEAD_SCALE``, its top at -118, and the head itself is
traced from the hungry sheet; the cobra maps by its head outline.

Writes ``tools/comic/reference/traced.json``. Run from the repository root
after a reference changes::

    python tools/comic/trace_reference.py

Needs numpy, scipy, scikit-image and Pillow.
"""

import json
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage
from skimage import measure

HERE = Path(__file__).resolve().parent
REF = HERE / "reference"
FACE_TOP = -118.0   # where the face mask's top sits in head units
HEAD_SCALE = 0.3618  # head units per pixel on the team's expression sheets
COBRA_CENTER, COBRA_SCALE = (999.5, 923.5), 0.25  # Cobra_InBush.png -> cobra units


def rgb(h):
    return np.array([int(h[i:i + 2], 16) for i in (1, 3, 5)], float)


def load(name):
    return np.asarray(Image.open(REF / name).convert("RGB"), float)


def share(px, fill, under, off_line=40.0):
    """How much of `fill` each pixel holds in a blend with `under`, 0 for other colors."""
    f, b = rgb(fill), rgb(under)
    d = f - b
    p = px - b
    t = (p @ d) / (d @ d)
    off = np.linalg.norm(p - t[..., None] * d, axis=-1)
    a = np.clip(t, 0.0, 1.0)
    a[off > off_line] = 0.0
    return a


def region_of(px, fill, under, box, off_line=40.0):
    """The largest shape of `fill` clear of the box's edge, holes filled, as a full-image mask."""
    x0, y0, x1, y1 = box
    m = share(px[y0:y1, x0:x1], fill, under, off_line) > 0.5
    lab, k = ndimage.label(m)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
    sizes = [0 if i in edge else ndimage.sum(m, lab, i) for i in range(1, k + 1)]
    big = lab == (int(np.argmax(sizes)) + 1)
    out = np.zeros(px.shape[:2], bool)
    out[y0:y1, x0:x1] = ndimage.binary_fill_holes(big)
    return out


def face_box(px, box):
    face = region_of(px, "#FFF2D2", "#845131", box)
    ys, xs = np.nonzero(face)
    return face, xs.min(), xs.max() + 1, ys.min(), ys.max() + 1


_SHEET = None


def face_mapping(px, box):
    """Maps a monkey head's pixels onto the rig's head units, by its cream face mask:
    an expression sheet at HEAD_SCALE, a family head scaled so its face mask
    matches the sheets' one."""
    global _SHEET
    if _SHEET is None:
        sheet = load("Face_Hungry.png")
        _, x0, x1, y0, y1 = face_box(sheet, (0, 0, sheet.shape[1], sheet.shape[0]))
        _SHEET = ((x1 - x0) * HEAD_SCALE, (y1 - y0) * HEAD_SCALE)
    face, x0, x1, y0, y1 = face_box(px, box)
    s = (_SHEET[0] / (x1 - x0) + _SHEET[1] / (y1 - y0)) / 2
    cx = (x0 + x1) / 2
    return face, (lambda x, y: ((x - cx) * s, (y - y0) * s + FACE_TOP))


def near(px, colors, tol):
    return np.min([np.linalg.norm(px - rgb(c), axis=-1) for c in colors], axis=0) <= tol


def trace(px, layer, to_rig, within):
    """One layer: [{fill, d, area}] per shape. layer = (colors, under, box, options).

    A shape is a patch of pure `colors` whose surroundings are mostly `under`
    (a color or a list), so a fill is only taken where it truly lies on that
    color; its edge is traced on the blend with the first `under` color."""
    colors, under, box, opt = layer
    colors = [colors] if isinstance(colors, str) else colors
    unders = [under] if isinstance(under, str) else under
    x0, y0, x1, y1 = box
    sub = px[y0:y1, x0:x1]
    a = share(sub, colors[0], unders[0], opt.get("off_line", 40.0))
    core = near(sub, colors, opt.get("core_tol", 8))
    if within is not None:
        keep = ndimage.binary_dilation(within, iterations=3)[y0:y1, x0:x1]
        a, core = a * keep, core & keep
    lab, k = ndimage.label(core)
    on_under = near(sub, unders, 30)
    shapes = []
    for i in range(1, k + 1):
        comp = lab == i
        if comp.sum() < opt.get("min_area", 10):
            continue
        rows, cols = np.nonzero(comp)
        if rows.min() == 0 or cols.min() == 0 or rows.max() == comp.shape[0] - 1 or cols.max() == comp.shape[1] - 1:
            continue
        filled = ndimage.binary_fill_holes(comp)
        rim = ndimage.binary_dilation(filled, iterations=4) & ~ndimage.binary_dilation(filled, iterations=2)
        if on_under[rim].mean() < opt.get("on_under", 0.5):
            continue
        grown = ndimage.binary_dilation(filled, iterations=2)
        alpha = np.maximum(np.where(grown, a, 0.0), filled.astype(float))
        alpha = ndimage.gaussian_filter(np.pad(alpha, 3), 0.6)
        median = np.median(sub[comp], axis=0)
        fill = min(colors, key=lambda c: np.linalg.norm(rgb(c) - median))
        parts = []
        for c in measure.find_contours(alpha, 0.5):
            c = measure.approximate_polygon(c, opt.get("tolerance", 0.35))
            pts = [to_rig(x + x0 - 3 + 0.5, y + y0 - 3 + 0.5) for y, x in c[:-1]]
            parts.append("M " + " L ".join(f"{x:.2f},{y:.2f}" for x, y in pts) + " Z")
        shapes.append({"fill": opt.get("as", fill), "d": " ".join(parts), "area": int(comp.sum())})
    return shapes


def trace_all(px, layers, to_rig, within):
    out = []
    for layer in layers:
        for s in trace(px, layer, to_rig, within):
            s.pop("area")
            if layer[3].get("outline"):
                s["outline"] = True
            out.append(s)
    return out


def recenter(shapes):
    """Moves an accessory's shapes to sit around (0, 0); returns them and where they were."""
    nums = [float(v) for s in shapes for v in s["d"].replace("M", " ").replace("L", " ").replace("Z", " ")
            .replace(",", " ").split()]
    xs, ys = nums[0::2], nums[1::2]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2

    def shift(d):
        out, toks = [], d.split()
        for t in toks:
            if "," in t:
                x, y = map(float, t.split(","))
                out.append(f"{x - cx:.2f},{y - cy:.2f}")
            else:
                out.append(t)
        return " ".join(out)

    return [dict(s, d=shift(s["d"])) for s in shapes], [round(cx, 2), round(cy, 2)]


# ---------------------------------------------------------------- monkeys

def head_shape():
    """The logo head the team's sheets are drawn on: its silhouette (dome and ears in
    one, inside the outline), the inner ears, the face mask and the curl."""
    px = load("Face_Hungry.png")
    box = (0, 0, px.shape[1], px.shape[0])
    face, to_rig = face_mapping(px, box)
    fur, line, bg, cream, ear = "#805033", "#4B2D20", "#F5F5F5", "#FFF2D2", "#E3AF66"
    inside = [fur, cream, ear, "#432A20", "#593822", "#F0C8AF"]
    silhouette = trace(px, (inside, [line, "#352016", "#593822"], box,
                            dict(min_area=20000, on_under=0.6)), to_rig, None)
    curl = [t for t in trace(px, (fur, bg, box, dict(min_area=200, on_under=0.3)), to_rig, None)
            if t["area"] < 4000]
    inner = trace(px, (ear, fur, box, dict(min_area=300)), to_rig, None)
    mask = trace(px, (cream, fur, box, dict(min_area=5000, on_under=0.3)), to_rig, None)
    assert len(silhouette) == len(curl) == len(mask) == 1 and len(inner) == 2, \
        (len(silhouette), len(curl), len(inner), len(mask))
    return {"Head": {"silhouette": silhouette[0]["d"], "inner": [t["d"] for t in inner],
                     "face": mask[0]["d"], "curl": curl[0]["d"]}}


def solo_faces():
    """The 3 expression sheets the team drew on the logo head."""
    out = {}
    ink, line, cream, high = "#432A20", "#593822", "#FFF2D2", "#FFF8E7"
    for key, file in (("Face_Determined", "Face_Determined.png"), ("Face_Hungry", "Face_Hungry.png"),
                      ("Face_HungrySister", "Face_HungrySister.png")):
        px = load(file)
        box = (0, 0, px.shape[1], px.shape[0])
        face, to_rig = face_mapping(px, box)
        out[key] = trace_all(px, [
            (["#E9B39D", "#F0C8AF", "#EBB9A2"], cream, box, dict(off_line=14, min_area=200, core_tol=5)),
            (ink, cream, box, dict(min_area=60)),
            (high, ink, box, dict(min_area=8)),
            (line, cream, box, dict(min_area=40)),
            (ink, line, box, dict(min_area=100)),
            (["#E9B39D", "#EBB9A2"], [ink, line], box, dict(min_area=40, off_line=30, core_tol=6)),
        ], to_rig, face)
    return out


FAMILY_HEADS = {"Father": (150, 380, 960, 1060), "Mother": (1100, 560, 1900, 1110),
                "Hero": (2180, 700, 2660, 1200), "Sister": (3000, 900, 3720, 1340)}


def family():
    px = load("Family.png")
    out = {}
    cream, fur, dark = "#FFF2D2", "#845131", "#4B2E20"
    for who in ("Father", "Mother"):
        box = FAMILY_HEADS[who]
        face, to_rig = face_mapping(px, box)
        out[f"{who}_Face_Normal"] = trace_all(px, [
            ("#FAE3C5", cream, box, dict(off_line=10, min_area=150, core_tol=4)),
            ("#422A1E", cream, box, dict(min_area=100)),
            ("#FFF8E6", "#422A1E", box, dict(min_area=6)),
            ("#8E6B4C", cream, box, dict(min_area=60, off_line=20)),
            ("#5C3C27", cream, box, dict(min_area=40)),
        ], to_rig, face)
        out[f"{who}_Marks"] = trace_all(px, [
            ("#AE8B66", [cream, fur], box, dict(min_area=200, off_line=20)),
            ("#EEDDBC", cream, box, dict(min_area=20, off_line=12, core_tol=5)),
            ("#DFCAA8", [cream, "#AE8B66", fur], box, dict(min_area=20, off_line=16, core_tol=5)),
        ], to_rig, None)
        if who == "Mother":
            out["Mother_Hair"] = trace_all(px, [("#9B7654", fur, (1300, 590, 1620, 730),
                                                  dict(min_area=200, off_line=16))], to_rig, None)
            flower, anchor = recenter(trace_all(px, [
                ("#CB8071", dark, (1640, 580, 1860, 780), dict(min_area=300, outline=True)),
                ("#E9B56C", dark, (1640, 580, 1860, 780), dict(min_area=60, outline=True)),
            ], to_rig, None))
            out["Prop_Flower"] = {"anchor": anchor, "shapes": flower}
        if who == "Father":
            tie, anchor = recenter(trace_all(px, [
                (["#58745B", "#6C8568"], dark, (420, 1010, 720, 1150), dict(min_area=100, outline=True)),
            ], to_rig, None))
            out["Prop_BowTie"] = {"anchor": anchor, "shapes": tie}
    for who in ("Hero", "Sister"):
        box = FAMILY_HEADS[who]
        face, to_rig = face_mapping(px, box)
        out[f"{who}_Face_Normal"] = trace_all(px, [
            ("#E9AD96", cream, box, dict(off_line=14, min_area=150, core_tol=5)),
            ("#422A1E", cream, box, dict(min_area=100)),
            ("#FFF8E6", "#422A1E", box, dict(min_area=6)),
            ("#5C3C27", cream, box, dict(min_area=30)),
        ], to_rig, face)
    bow, anchor = recenter(trace_all(px, [
        (["#CB8071", "#DB9280"], dark, (3270, 950, 3470, 1075), dict(min_area=40, outline=True)),
    ], to_rig, None))
    out["Prop_Bow"] = {"anchor": anchor, "shapes": bow}
    return out


# ---------------------------------------------------------------- cobra

def cobra():
    px = load("Cobra_InBush.png")
    (cx, cy), s = COBRA_CENTER, COBRA_SCALE
    to_rig = lambda x, y: ((x - cx) * s, (y - cy) * s)
    green, dark, tan = "#21A663", "#382216", "#F2D58B"
    head = region_of(px, green, dark, (560, 500, 1440, 1340))
    box = (560, 500, 1440, 1340)
    return {"Cobra_Face": trace_all(px, [
        ("#36B873", [green, dark], box, dict(min_area=400)),
        ("#62BF64", green, box, dict(min_area=200)),
        ("#A0D874", green, box, dict(min_area=100)),
        (dark, "#62BF64", box, dict(min_area=400)),
        ("#8EAC42", dark, box, dict(min_area=100)),
        ("#D6ED6D", "#8EAC42", box, dict(min_area=40)),
        ("#FFF6C4", "#D6ED6D", box, dict(min_area=20)),
        (tan, green, box, dict(min_area=2000)),
        (dark, tan, box, dict(min_area=300)),
        ("#FFF3CF", tan, box, dict(min_area=60)),
    ], to_rig, head)}


def main():
    traced = {}
    traced.update(head_shape())
    traced.update(solo_faces())
    traced.update(family())
    traced.update(cobra())
    path = REF / "traced.json"
    path.write_text(json.dumps(traced, indent=1) + "\n")
    for k, v in traced.items():
        if k == "Head":
            print(f"{k:22s} silhouette, 2 inner ears, face mask, curl")
            continue
        shapes = v["shapes"] if isinstance(v, dict) else v
        print(f"{k:22s} {len(shapes):3d} shapes  " + " ".join(sorted({s['fill'] for s in shapes})))
    print(f"wrote {path}")


if __name__ == "__main__":
    main()
