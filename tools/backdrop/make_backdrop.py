"""Paints the jungle backdrop textures of Monkobra from scratch.

Writes 3 small PNGs into ``Assets/_Monkobra/Textures/Environment/``:

- ``Backdrop_CanopyDetail.png``, 512 square, tiling, colour: round tree
  crowns seen from above, lit from the upper left, soft shade between them
- ``Backdrop_CanopyMacro.png``, 1024 square, linear data, the whole forest
  round the trunk out to ``MACRO_RADIUS_U``: RGB a soft tint (0.5 grey is
  neutral), A the Amazon river, a wide meander round the trunk
- ``Backdrop_Ridges.png``, 1024 x 1, linear data, wrapping round: the
  height profile of the 4 hill ridges, nearest in R to farthest in A
- ``Backdrop_ForestFloor.png``, 512 square, tiling, colour: the ground under
  the near trees seen from above, moss & bare earth, leaf litter, ferns and
  low shrubs, mottled with shade; nothing round & crown-like, so it never
  reads as a second canopy

Painted procedurally, no source images, so the art is original. Run from the
repository root::

    python tools/backdrop/make_backdrop.py
"""

import os

import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter, shift

OUT_DIR = os.path.join("Assets", "_Monkobra", "Textures", "Environment")
# the macro map's half width in world units, keep in step with the
# BackdropSky material's _MacroRadius
MACRO_RADIUS_U = 2700.0

rng = np.random.default_rng(526)


def save(name, rgba):
    os.makedirs(OUT_DIR, exist_ok=True)
    image = Image.fromarray(np.clip(rgba * 255 + 0.5, 0, 255).astype(np.uint8))
    path = os.path.join(OUT_DIR, name)
    image.save(path, optimize=True)
    print(f"{path}\t{image.size}\t{os.path.getsize(path) // 1024} KB")


def periodic_noise(shape, scale, octaves=3):
    """smooth noise that tiles in both directions, -1 to 1"""
    out = np.zeros(shape)
    amp = 1.0
    for o in range(octaves):
        cells = int(scale * 2**o)
        grid = rng.random((cells, cells))
        up = np.kron(grid, np.ones((shape[0] // cells + 1, shape[1] // cells + 1)))
        up = up[: shape[0], : shape[1]]
        up = gaussian_filter(up, sigma=shape[0] / cells * 0.45, mode="wrap")
        out += amp * (up - up.mean()) / (up.std() + 1e-9)
        amp *= 0.5
    return out / np.abs(out).max()


# Canopy Detail  ###############################################################
def canopy_detail(size=512, crowns=125):
    """round crowns as a height field, shaded by slope, casting soft shade"""
    yy, xx = np.mgrid[0:size, 0:size].astype(float)
    height = np.full((size, size), -1.0)
    owner = np.full((size, size), -1)
    # darts, so the crowns spread evenly with a few gaps
    centres, radii = [], []
    while len(centres) < crowns:
        c = rng.random(2) * size
        r = rng.uniform(26, 44)
        ok = True
        for (ox, oy), orad in zip(centres, radii):
            d = np.hypot(*(((c - (ox, oy)) + size / 2) % size - size / 2))
            if d < 0.86 * (r + orad):
                ok = False
                break
        if ok or rng.random() < 0.002:
            centres.append(c)
            radii.append(r)
    for i, ((cx, cy), r) in enumerate(zip(centres, radii)):
        dx = (xx - cx + size / 2) % size - size / 2
        dy = (yy - cy + size / 2) % size - size / 2
        angle = np.arctan2(dy, dx)
        # leafy scallops round the rim
        lump = 1 + 0.035 * np.sin(angle * rng.integers(6, 10) + rng.random() * 6.28)
        d2 = (dx * dx + dy * dy) / (r * lump) ** 2
        h = np.sqrt(np.clip(1 - d2, 0, None)) * r
        h = np.where(d2 < 1, h, -1)
        win = h > height
        height[win] = h[win]
        owner[win] = i
    covered = height > 0

    palette = np.array([
        [0.34, 0.52, 0.27], [0.38, 0.56, 0.28], [0.31, 0.49, 0.30],
        [0.43, 0.58, 0.29], [0.33, 0.50, 0.34], [0.46, 0.60, 0.31],
    ])
    crown_color = palette[rng.integers(0, len(palette), len(centres))]
    crown_color *= rng.uniform(0.92, 1.08, (len(centres), 1))
    base = crown_color[np.clip(owner, 0, None)]

    # lit from the upper left
    smooth = gaussian_filter(np.maximum(height, 0), 1.5, mode="wrap")
    gy, gx = np.gradient(smooth)
    normal = np.stack([-gx, -gy, np.full_like(gx, 1.6)], -1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    light = np.array([-0.5, -0.5, 0.71])
    lambert = np.clip(normal @ light, 0, 1)
    shade = 0.58 + 0.62 * lambert

    # soft cast shade: a crown up & left of a pixel standing taller shades it
    lifted = shift(np.maximum(height, 0), (7, 7), mode="wrap") - 6
    shadowed = gaussian_filter((lifted > height).astype(float), 2.0, mode="wrap")
    shade *= 1 - 0.28 * shadowed

    # leafy speckle
    speckle = gaussian_filter(rng.random((size, size)), 2.2, mode="wrap")
    speckle = (speckle - speckle.mean()) / speckle.std()
    shade *= 1 + 0.05 * speckle

    rgb = base * shade[..., None]
    # rims darken, so each crown reads round
    rim = np.clip(height / 9, 0, 1)
    rgb *= (0.72 + 0.28 * rim)[..., None]
    gap = np.array([0.20, 0.31, 0.22])
    rgb = np.where(covered[..., None], rgb, gap)
    rgb = gaussian_filter(rgb, (0.7, 0.7, 0), mode="wrap")
    alpha = np.ones((size, size, 1))
    return np.concatenate([np.clip(rgb, 0, 1), alpha], -1)


# Canopy Macro  ################################################################
def smoothstep(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def river_paths():
    """the river & its tributary in world units round the trunk at the origin"""
    paths = []
    # the main river sweeps round 3 quarters of the trunk, 900 to 1700 out,
    # coming in from & leaving to past the macro map's edge
    t = np.linspace(0, 1, 1400)
    angle = np.radians(-30 + 290 * t)
    radius = (1300 + 250 * np.sin(t * np.pi * 2.6 + 0.4) + 120 * np.sin(t * 19.0 + 1.0)
              + 50 * np.sin(t * 47.0))
    radius += 2000 * (smoothstep((0.1 - t) / 0.1) + smoothstep((t - 0.9) / 0.1))
    main = np.stack([np.cos(angle) * radius, np.sin(angle) * radius], -1)
    paths.append((main, 95, 135))
    # a tributary joining the main river where it comes in, from the one
    # quarter the main river leaves out
    t = np.linspace(0, 1, 500)[:, None]
    start = main[int(len(main) * 0.14)]
    end = np.array([np.cos(np.radians(285)), np.sin(np.radians(285))]) * 3300
    across = np.array([-(end - start)[1], (end - start)[0]]) / np.linalg.norm(end - start)
    wiggle = 160 * np.sin(t * 9.0) * np.sin(t * np.pi) + 40 * np.sin(t * 27.0) * np.sin(t * np.pi)
    paths.append((start + (end - start) * t + across * wiggle, 45, 60))
    return paths


def canopy_macro(size=1024):
    upp = 2 * MACRO_RADIUS_U / size  # world units per pixel
    yy, xx = np.mgrid[0:size, 0:size].astype(float)
    # Unity reads row 0 of the image as v = 1, so the top row is +z
    wx = (xx + 0.5) * upp - MACRO_RADIUS_U
    wz = MACRO_RADIUS_U - (yy + 0.5) * upp

    patches = periodic_noise((size, size), 3, 4)
    warm = np.array([0.56, 0.54, 0.44])
    cool = np.array([0.44, 0.50, 0.53])
    t = (patches[..., None] + 1) / 2
    tint = warm * t + cool * (1 - t)
    # darker & lighter stands of forest
    tint *= 1 + 0.16 * periodic_noise((size, size), 10, 3)[..., None]

    water = np.zeros((size, size))
    bank = np.zeros((size, size))
    for path, w0, w1 in river_paths():
        dist = np.full((size, size), 1e9)
        width = np.zeros((size, size))
        for i in range(0, len(path), 2):
            px, pz = path[i]
            if abs(px) > MACRO_RADIUS_U + 200 or abs(pz) > MACRO_RADIUS_U + 200:
                continue
            d = np.hypot(wx - px, wz - pz)
            closer = d < dist
            dist[closer] = d[closer]
            width[closer] = w0 + (w1 - w0) * i / len(path)
        water = np.maximum(water, np.clip((width - dist) / (1.5 * upp) + 0.5, 0, 1))
        bank = np.maximum(bank, np.clip(1 - (dist - width) / 70, 0, 1))
    for (lx, lz, r) in ((-1150, 900, 60), (1000, -1150, 50), (-1500, -650, 70)):
        d = np.hypot(wx - lx, (wz - lz) * 1.6)
        water = np.maximum(water, np.clip((r - d) / (1.5 * upp) + 0.5, 0, 1))
    bank *= 1 - water
    # a pale sandy bank, so the river reads from far off
    tint = tint * (1 - 0.6 * bank[..., None]) + np.array([0.78, 0.74, 0.56]) * 0.6 * bank[..., None]
    return np.concatenate([np.clip(tint, 0, 1), water[..., None]], -1)


# Forest Floor  ################################################################
def forest_floor(size=512):
    yy, xx = np.mgrid[0:size, 0:size].astype(float)
    # moss against bare earth, in broad irregular patches
    moss = (periodic_noise((size, size), 6, 4) + 1) / 2
    moss = np.clip((moss - 0.42) * 3.0, 0, 1)
    earth = np.array([0.30, 0.26, 0.17])
    green = np.array([0.27, 0.38, 0.19])
    rgb = earth * (1 - moss[..., None]) + green * moss[..., None]
    rgb *= (1 + 0.12 * periodic_noise((size, size), 32, 2))[..., None]

    # leaf litter: small elongated flecks, browns & olives
    litter = np.array([[0.42, 0.32, 0.18], [0.48, 0.40, 0.22], [0.34, 0.36, 0.18], [0.38, 0.26, 0.15]])
    for _ in range(2600):
        cx, cy = rng.random(2) * size
        length, width = rng.uniform(2.5, 6), rng.uniform(1.0, 2.2)
        a = rng.random() * np.pi
        x0, x1 = int(cx - 7), int(cx + 8)
        y0, y1 = int(cy - 7), int(cy + 8)
        ys = np.arange(y0, y1) % size
        xs = np.arange(x0, x1) % size
        dy = (np.arange(y0, y1) - cy)[:, None]
        dx = (np.arange(x0, x1) - cx)[None, :]
        u = dx * np.cos(a) + dy * np.sin(a)
        v = -dx * np.sin(a) + dy * np.cos(a)
        inside = np.clip(1.5 - np.sqrt((u / length) ** 2 + (v / width) ** 2) * 1.5, 0, 1)
        color = litter[rng.integers(0, len(litter))] * rng.uniform(0.85, 1.1)
        patch = rgb[np.ix_(ys, xs)]
        rgb[np.ix_(ys, xs)] = patch * (1 - inside[..., None]) + color * inside[..., None]

    # ferns & seedlings: fans of thin bright fronds
    fern = np.array([0.30, 0.48, 0.22])
    for _ in range(140):
        cx, cy = rng.random(2) * size
        fronds = rng.integers(5, 9)
        reach = rng.uniform(7, 16)
        start = rng.random() * 6.28
        for k in range(fronds):
            a = start + k * 6.28 / fronds + rng.uniform(-0.2, 0.2)
            for t in np.linspace(0.15, 1, 14):
                px = int(cx + np.cos(a) * reach * t) % size
                py = int(cy + np.sin(a) * reach * t) % size
                w = 1 if t > 0.6 else 2
                rgb[py - w + 1:py + w, px - w + 1:px + w] = fern * (0.8 + 0.4 * t)

    # low shrubs: ragged dark clumps, lit along one edge
    for _ in range(45):
        cx, cy = rng.random(2) * size
        r = rng.uniform(8, 18)
        dx = (xx - cx + size / 2) % size - size / 2
        dy = (yy - cy + size / 2) % size - size / 2
        angle = np.arctan2(dy, dx)
        ragged = r * (1 + 0.3 * np.sin(angle * rng.integers(5, 9) + rng.random() * 6.28)
                      + 0.15 * np.sin(angle * rng.integers(11, 17)))
        inside = np.clip(ragged - np.hypot(dx, dy), 0, 1)
        lit = np.clip(0.75 + 0.45 * (-dx - dy) / (r * 1.4), 0.55, 1.15)
        clump = np.array([0.22, 0.34, 0.17])[None, None] * lit[..., None]
        rgb = rgb * (1 - inside[..., None]) + clump * inside[..., None]

    # dappled shade from the canopy overhead
    shade = (periodic_noise((size, size), 10, 3) + 1) / 2
    rgb *= (0.62 + 0.5 * shade)[..., None]
    rgb = gaussian_filter(rgb, (0.5, 0.5, 0), mode="wrap")
    alpha = np.ones((size, size, 1))
    return np.concatenate([np.clip(rgb, 0, 1), alpha], -1)


# Ridges  ######################################################################
def ridges(width=1024):
    """4 periodic ridge profiles, 0 to 1, near (R) to far (A)"""
    u = np.arange(width) / width
    out = []
    # (sine frequencies & amplitudes): far ridges broad, near ones busier
    specs = [
        [(3, 1.0), (7, 0.5), (13, 0.25), (29, 0.1), (83, 0.03)],
        [(2, 1.0), (5, 0.55), (11, 0.25), (23, 0.08)],
        [(2, 1.0), (4, 0.6), (9, 0.25), (19, 0.07)],
        [(1, 0.8), (3, 1.0), (7, 0.35), (15, 0.1)],
    ]
    for spec in specs:
        line = np.zeros(width)
        for freq, amp in spec:
            line += amp * np.sin(2 * np.pi * (freq * u + rng.random()))
        # rounder tops, broader valleys
        line = (line - line.min()) / (line.max() - line.min())
        out.append(line ** 1.3)
    return np.stack(out, -1)[None]


if __name__ == "__main__":
    save("Backdrop_CanopyDetail.png", canopy_detail())
    save("Backdrop_CanopyMacro.png", canopy_macro())
    save("Backdrop_Ridges.png", ridges())
    save("Backdrop_ForestFloor.png", forest_floor())
