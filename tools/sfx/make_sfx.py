"""Synthesizes Monkobra's sound effects from scratch, in the music's palette.

The background music is ukulele and djembe in C major (see
``tools/music/make_music.py``); the effects share its instruments, its key
and its wooden, hand-played feel: mallet on marimba, a hollow shell, a
plucked ukulele, a cartoon squeak. Every sound is made from sines and
noise, no samples, so it is original and free of any license.

P0, the core feel, writes mono 16-bit WAVs:

- ``Grab_Banana``: a bright pop & 2 rising marimba notes
- ``Grab_Coconut``: a hollow knock on the shell & a softer one
- ``Grab_Miss``: a soft, deflated puff falling in pitch
- ``Hit_Branch``: a wooden bonk, leaves shaking & the monkey's squeak
- ``Lose_CobraBite``: a snapping bite, then the ukulele falling to a minor
  chord

P1, items, hazards & progress:

- ``Pineapple_Grab``: a big juicy bite, the skin crunching, then the juice
- ``Leap_Whoosh``: wind swelling & rising as the monkey is pulled up
- ``Coconut_Throw``: a short whoosh
- ``Coconut_Freeze``: the coconut's hollow bonk on the cobra
- ``Coconut_Thaw``: ice cracking apart
- ``Web_Stuck``: a gloopy, sticky splat stretching
- ``Web_Escape``: a rubbery snap & 2 rising ukulele notes
- ``Marker_Reached``: a glockenspiel chime, G5 & C6
- ``Stamina_Low``: 2 soft heartbeats

The Cobra King fight, into ``BossCobraKing/``:

- ``CobraKing_Entrance``: a deep gong & a long hiss
- ``CobraKing_AttackWarn``: 0.3 s, a hiss & a tone rushing up into a hard
  hit, the one cue before every warned attack
- ``CobraKing_ArmorBlock``: a bright metal clang
- ``CobraKing_FruitSplat``: a thrown fruit bursting on the crown, the rind
  cracking, a heavy thump & a big wet splat
- ``CobraKing_Shatter``: a crash & cubes tumbling apart
- ``CobraKing_Victory``: a short horn call blended with a strummed chord,
  a drum roll & a sparkle

Run from the repository root; ``--out`` picks the root folder, each sound
going into its module's folder under it::

    python tools/sfx/make_sfx.py --out Assets/_Monkobra/Audio/SFX
"""

import argparse
import os
import sys
import zlib

import numpy as np
import soundfile as sf
from scipy.signal import istft, stft

sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "music"))
import make_music  # noqa: E402
from make_music import (  # noqa: E402
    SR, djembe_bass, djembe_slap, djembe_tone, fade_out, filt, hz, peak1, shaker, sweep, times, ukulele,
)

rng = np.random.default_rng(1957)


# Helpers  #####################################################################
def noise(n):
    return rng.uniform(-1.0, 1.0, n)


def place(out, sound, at_s, gain=1.0):
    """mixes sound into out from at_s on"""
    # a short fade, so a part never stops with a click
    sound = fade_out(np.array(sound, dtype=float), 0.008)
    start = int(at_s * SR)
    end = min(len(out), start + len(sound))
    out[start:end] += sound[: end - start] * gain
    return out


def room(sig, wet=0.12, length_s=0.18):
    """a small wooden room, so the effects sit in the music's space"""
    sig = fade_out(np.array(sig, dtype=float), 0.008)
    t = times(length_s)
    ir = noise(len(t)) * np.exp(-t / (length_s / 5))
    ir = filt(ir, "low", 4500)
    tail = np.convolve(sig, ir)[: len(sig) + len(ir)]
    out = np.zeros(len(tail))
    out[: len(sig)] += sig
    return out + wet * peak1(tail) * np.max(np.abs(sig))


def finish(sig, gain):
    sig = fade_out(room(sig), 0.02)
    return peak1(sig) * gain


# Instruments  #################################################################
def marimba(f, dur_s=0.6):
    """bar struck by a soft mallet: the 4th & 10th partials of a tuned bar"""
    t = times(dur_s)
    tone = (
        np.sin(2 * np.pi * f * t) * np.exp(-t / 0.32)
        + 0.35 * np.sin(2 * np.pi * f * 3.93 * t) * np.exp(-t / 0.07)
        + 0.12 * np.sin(2 * np.pi * f * 9.8 * t) * np.exp(-t / 0.025)
    )
    mallet = filt(noise(len(t)), "low", 2500) * np.exp(-t / 0.003) * 0.4
    attack = np.minimum(t / 0.002, 1.0)
    return fade_out(peak1((tone + mallet) * attack), 0.05)


def squeak(f_start, f_peak, f_end, dur_s=0.22):
    """a little monkey cry: a buzzing voice through 2 'ee-a' formants,
    sliding up then down"""
    t = times(dur_s)
    rise = np.clip(t / (dur_s * 0.3), 0, 1)
    fall = np.clip((t - dur_s * 0.3) / (dur_s * 0.7), 0, 1)
    freq = np.where(t < dur_s * 0.3, f_start + (f_peak - f_start) * rise, f_peak + (f_end - f_peak) * fall)
    freq *= 1 + 0.03 * np.sin(2 * np.pi * 28 * t)
    phase = 2 * np.pi * np.cumsum(freq) / SR
    voice = sum(np.sin(k * phase) / k for k in range(1, 14))
    vowel = filt(voice, "band", [800, 1300]) + 0.7 * filt(voice, "band", [2300, 3200])
    env = np.minimum(t / 0.012, 1.0) * np.clip((dur_s - t) / 0.05, 0, 1)
    return peak1(vowel * env)


# P0 Sounds  ###################################################################
def grab_banana():
    """a bright pop & 2 rising marimba notes, G5 to C6"""
    out = np.zeros(int(0.75 * SR))
    t = times(0.08)
    pop = sweep(1500, 380, 0.008, t) * np.exp(-t / 0.025)
    pop += filt(noise(len(t)), "high", 3000) * np.exp(-t / 0.002) * 0.6
    place(out, pop, 0.0, 0.8)
    place(out, marimba(hz("G5")), 0.02, 0.7)
    place(out, marimba(hz("C6")), 0.09, 0.75)
    return finish(out, 0.8)


def grab_coconut():
    """a hollow knock on a coconut shell, & a softer one as it settles"""
    out = np.zeros(int(0.5 * SR))
    f = 205
    for at, gain, pitch in ((0.0, 1.0, 1.0), (0.085, 0.45, 1.06)):
        t = times(0.35)
        shell = (
            np.sin(2 * np.pi * f * pitch * t) * np.exp(-t / 0.09)
            + 0.6 * np.sin(2 * np.pi * f * pitch * 1.53 * t) * np.exp(-t / 0.06)
            + 0.35 * np.sin(2 * np.pi * f * pitch * 2.41 * t) * np.exp(-t / 0.035)
            + 0.2 * np.sin(2 * np.pi * f * pitch * 3.7 * t) * np.exp(-t / 0.02)
        )
        knock = filt(noise(len(t)), "band", [500, 2000]) * np.exp(-t / 0.008) * 0.8
        place(out, peak1(shell + knock), at, gain)
    return finish(out, 0.8)


def grab_miss():
    """a soft puff falling in pitch, deflated but never harsh"""
    t = times(0.32)
    freq = 210 + 300 * np.exp(-t / 0.09)
    tone = np.sin(2 * np.pi * np.cumsum(freq) / SR)
    puff = filt(noise(len(t)), "low", 1400) * 0.35
    env = np.minimum(t / 0.015, 1.0) * np.exp(-t / 0.11)
    return finish((tone + puff) * env, 0.45)


def hit_branch():
    """a wooden bonk on the head, leaves shaking, then the monkey's squeak"""
    out = np.zeros(int(0.75 * SR))
    t = times(0.4)
    bonk = sweep(190, 75, 0.03, t) * np.exp(-t / 0.11)
    bonk += filt(noise(len(t)), "low", 700) * np.exp(-t / 0.02) * 0.7
    crack = filt(noise(len(t)), "band", [900, 3200]) * np.exp(-t / 0.012) * 0.8
    place(out, peak1(bonk + crack), 0.0, 1.0)

    # leaves: little bursts, thick at first & thinning out
    for _ in range(70):
        at = 0.01 + rng.exponential(0.12)
        if at > 0.6:
            continue
        g = times(rng.uniform(0.008, 0.025))
        leaf = filt(noise(len(g)), "band", [2800, 8500]) * np.sin(np.pi * g / g[-1])
        place(out, leaf, at, rng.uniform(0.05, 0.16) * np.exp(-at / 0.25))

    place(out, squeak(700, 1250, 760, 0.2), 0.06, 0.55)
    return finish(out, 0.9)


def lose_cobra_bite():
    """a snapping bite, then the ukulele falling G4, E4, to a C minor chord"""
    out = np.zeros(int(2.0 * SR))
    t = times(0.2)
    teeth = filt(noise(len(t)), "high", 2500) * np.exp(-t / 0.004)
    crunch = filt(noise(len(t)), "band", [500, 2400]) * np.exp(-t / 0.03)
    crunch *= 0.5 + 0.5 * np.sign(np.sin(2 * np.pi * 45 * t))
    jaw = sweep(160, 70, 0.02, t) * np.exp(-t / 0.06)
    place(out, peak1(teeth + 0.6 * crunch + 0.8 * jaw), 0.0, 1.0)
    place(out, peak1(teeth), 0.035, 0.6)

    place(out, ukulele(hz("G4")), 0.32, 0.55)
    place(out, ukulele(hz("E4")), 0.52, 0.55)
    # the last chord strummed, slow & sad
    for k, note in enumerate(("C4", "Eb4", "G4", "C5")):
        place(out, ukulele(hz(note)), 0.78 + k * 0.035, 0.38)
    return finish(out, 0.85)


# P1 Sounds  ###################################################################
def bell(f, dur_s=1.2):
    """glockenspiel bar: inharmonic partials, the high ones dying first"""
    t = times(dur_s)
    tone = (
        np.sin(2 * np.pi * f * t) * np.exp(-t / 0.5)
        + 0.4 * np.sin(2 * np.pi * f * 2.76 * t) * np.exp(-t / 0.18)
        + 0.2 * np.sin(2 * np.pi * f * 5.40 * t) * np.exp(-t / 0.07)
        + 0.1 * np.sin(2 * np.pi * f * 8.93 * t) * np.exp(-t / 0.03)
    )
    strike = filt(noise(len(t)), "high", 4000) * np.exp(-t / 0.002) * 0.3
    return fade_out(peak1((tone + strike) * np.minimum(t / 0.001, 1.0)), 0.08)


def whoosh(dur_s, f_from, f_to, peak_at):
    """air rushing past: noise through a soft band gliding from f_from to
    f_to, shaped in the spectrum so the glide is smooth, swelling to
    peak_at of its length & dying away"""
    n = int(dur_s * SR)
    _, _, spec = stft(noise(n), SR, nperseg=1024, noverlap=768)
    freqs = np.linspace(0, SR / 2, spec.shape[0])[:, None]
    k = np.linspace(0, 1, spec.shape[1])[None, :]
    centre = f_from * (f_to / f_from) ** k
    # a bell-shaped band about an octave wide, on a log scale
    band = np.exp(-0.5 * (np.log2(np.maximum(freqs, 1.0) / centre) / 0.55) ** 2)
    _, out = istft(spec * band, SR, nperseg=1024, noverlap=768)
    out = out[:n]
    t = times(len(out) / SR)
    swell = np.clip(t / (dur_s * peak_at), 0, 1) ** 1.5
    fade = np.clip((dur_s - t) / (dur_s * (1 - peak_at)), 0, 1) ** 1.2
    return peak1(out * swell * fade)


def pineapple_grab():
    """a big juicy bite: the tough skin crunching, then the juice"""
    out = np.zeros(int(0.6 * SR))
    for k in range(7):
        g = times(rng.uniform(0.015, 0.035))
        bite = filt(noise(len(g)), "band", [600, 3800]) * np.exp(-g / 0.009)
        place(out, bite, k * 0.017 + rng.uniform(0, 0.004), rng.uniform(0.55, 1.0))
    t = times(0.12)
    jaw = sweep(200, 110, 0.02, t) * np.exp(-t / 0.04)
    place(out, jaw, 0.0, 0.5)
    t = times(0.3)
    juice = filt(noise(len(t)), "band", [1800, 6500])
    juice *= (0.6 + 0.4 * np.sin(2 * np.pi * 30 * t)) * np.exp(-t / 0.08) * 0.35
    place(out, juice, 0.05)
    return finish(out, 0.8)


def leap_whoosh():
    """the pull up: wind swelling & rising, with a faint rising whistle"""
    dur = 1.5
    t = times(dur)
    air = whoosh(dur, 350, 1800, 0.45)
    freq = 320 + 600 * (1 - np.exp(-t / 0.5))
    whistle = np.sin(2 * np.pi * np.cumsum(freq) / SR)
    whistle *= np.clip(t / 0.4, 0, 1) * np.clip((dur - t) / 0.7, 0, 1) * 0.12
    return finish(air + whistle, 0.6)


def coconut_throw():
    """a short whoosh, falling in pitch as it drops away"""
    return finish(whoosh(0.28, 2200, 700, 0.3), 0.55)


def coconut_freeze():
    """the coconut's hollow bonk on the cobra"""
    t = times(0.4)
    f = 190
    bonk = (
        np.sin(2 * np.pi * f * t) * np.exp(-t / 0.08)
        + 0.5 * np.sin(2 * np.pi * f * 1.53 * t) * np.exp(-t / 0.05)
    )
    bonk += filt(noise(len(t)), "band", [500, 2000]) * np.exp(-t / 0.008) * 0.8
    return finish(peak1(bonk), 0.8)


def coconut_thaw():
    """ice cracking apart: sharp snaps, then a shower of bits"""
    out = np.zeros(int(0.8 * SR))
    for k, at in enumerate((0.0, 0.06, 0.1)):
        g = times(0.12)
        snap = filt(noise(len(g)), "high", 1800) * np.exp(-g / 0.006)
        snap += sweep(900 - k * 150, 300, 0.01, g) * np.exp(-g / 0.02) * 0.6
        place(out, peak1(snap), at, 0.8 - 0.15 * k)
    for _ in range(30):
        at = 0.12 + rng.exponential(0.12)
        if at > 0.65:
            continue
        g = times(rng.uniform(0.01, 0.03))
        bit = filt(noise(len(g)), "band", [3000, 8000]) * np.exp(-g / 0.006)
        place(out, bit, at, rng.uniform(0.1, 0.25) * np.exp(-(at - 0.12) / 0.25))
    return finish(out, 0.7)


def web_stuck():
    """a gloopy, sticky splat, then the web stretching"""
    out = np.zeros(int(0.7 * SR))
    t = times(0.3)
    freq = 120 + 260 * np.exp(-t / 0.05)
    freq *= 1 + 0.25 * np.sin(2 * np.pi * 34 * t) * np.exp(-t / 0.1)
    glop = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-t / 0.1)
    squelch = filt(noise(len(t)), "band", [400, 1600])
    squelch *= (0.5 + 0.5 * np.sin(2 * np.pi * 26 * t)) * np.exp(-t / 0.07) * 0.6
    place(out, peak1(glop + squelch), 0.0, 1.0)
    t = times(0.35)
    freq = 260 - 90 * t / t[-1]
    phase = 2 * np.pi * np.cumsum(freq) / SR
    stretch = np.sin(phase + 1.2 * np.sin(phase)) * np.sin(np.pi * t / t[-1]) * 0.3
    place(out, stretch, 0.12)
    return finish(out, 0.7)


def web_escape():
    """a rubbery snap as the strands let go & 2 rising ukulele notes"""
    out = np.zeros(int(1.0 * SR))
    t = times(0.25)
    snap = filt(noise(len(t)), "high", 2000) * np.exp(-t / 0.004)
    freq = 380 + 220 * (1 - np.exp(-t / 0.04))
    twang = np.sin(2 * np.pi * np.cumsum(freq) / SR) * np.exp(-t / 0.07)
    place(out, peak1(snap + 0.8 * twang), 0.0, 0.85)
    place(out, ukulele(hz("G4")), 0.06, 0.45)
    place(out, ukulele(hz("C5")), 0.15, 0.5)
    return finish(out, 0.75)


def marker_reached():
    """a glockenspiel chime, G5 & C6 struck together, ringing on"""
    out = np.zeros(int(1.3 * SR))
    place(out, bell(hz("G5"), 1.2), 0.0, 0.6)
    place(out, bell(hz("C6"), 1.2), 0.012, 0.7)
    return finish(out, 0.6)


def stamina_low():
    """2 soft heartbeats, lub-dub, lub-dub"""
    out = np.zeros(int(1.2 * SR))
    for beat in (0.0, 0.55):
        for k, (at, f) in enumerate(((0.0, 72), (0.16, 60))):
            t = times(0.22)
            thump = sweep(f * 1.6, f, 0.02, t) * np.exp(-t / 0.07)
            thump += filt(noise(len(t)), "low", 300) * np.exp(-t / 0.015) * 0.4
            place(out, peak1(thump), beat + at, 1.0 if k == 0 else 0.7)
    return finish(out, 0.6)


# Cobra King  #################################################################
def gong(f, dur_s):
    """a hung gong: many beating, inharmonic partials, the high ones
    blooming a moment after the strike"""
    t = times(dur_s)
    tone = np.zeros(len(t))
    for ratio, amp, decay, bloom in (
        (1.00, 1.0, 1.6, 0.0), (1.47, 0.6, 1.2, 0.05), (2.03, 0.45, 1.0, 0.1),
        (2.71, 0.35, 0.8, 0.15), (3.42, 0.25, 0.6, 0.2), (4.17, 0.2, 0.45, 0.25),
        (5.51, 0.12, 0.3, 0.3),
    ):
        for detune in (0.997, 1.003):
            swell = 1 - np.exp(-t / (0.01 + bloom))
            tone += amp * swell * np.sin(2 * np.pi * f * ratio * detune * t) * np.exp(-t / decay)
    mallet = filt(noise(len(t)), "low", 600) * np.exp(-t / 0.02) * 1.5
    # the ring dies away over the last third, never cut off
    tail = np.clip((dur_s - t) / (dur_s * 0.35), 0, 1) ** 2
    return peak1((tone + mallet) * tail)


def hiss(dur_s, attack_s=0.15):
    """a snake's long hiss: bright noise, a little ragged"""
    t = times(dur_s)
    air = filt(noise(len(t)), "high", 3200)
    air = filt(air, "low", 9500)
    flutter = 1 + 0.25 * filt(noise(len(t)), "low", 18) / 0.05
    env = np.clip(t / attack_s, 0, 1) * np.clip((dur_s - t) / (dur_s * 0.45), 0, 1)
    return peak1(air * np.clip(flutter, 0.3, 1.6) * env)


def cobra_king_entrance():
    out = np.zeros(int(3.2 * SR))
    place(out, gong(98, 3.0), 0.0, 1.0)
    place(out, hiss(1.9), 0.35, 0.55)
    return finish(out, 0.9)


def cobra_king_attack_warn():
    """here it comes: a hiss & a tone rushing up for 0.18 s, then a hard
    hit landing on the moment; dry, so it stops dead"""
    length_s = 0.3
    rise_s = 0.18
    out = np.zeros(int(length_s * SR))

    t = times(rise_s)
    k = t / rise_s
    # 2 tones a fifth apart, sweeping up an octave & a half, fluttering
    freq = 330 * 2 ** (1.5 * k ** 1.6) * (1 + 0.03 * np.sin(2 * np.pi * 26 * t))
    phase = 2 * np.pi * np.cumsum(freq) / SR
    tone = sum(np.sin(n * phase) / n for n in range(1, 7))
    tone += 0.6 * sum(np.sin(n * 1.5 * phase) / n for n in range(1, 5))
    swell = k ** 1.8
    hiss = filt(filt(noise(len(t)), "high", 2500), "low", 9000) * swell
    place(out, peak1(tone) * swell * 0.55 + peak1(hiss) * 0.45, 0.0, 1.0)

    # the hit: a heavy low drum & a sharp crack
    t = times(length_s - rise_s)
    drum = sweep(170, 55, 0.015, t) * np.exp(-t / 0.05)
    crack = filt(noise(len(t)), "band", [1200, 6000]) * np.exp(-t / 0.012)
    place(out, peak1(np.tanh(2.0 * (drum + 0.9 * crack))), rise_s - 0.005, 1.0)
    out = np.tanh(1.3 * out) / np.tanh(1.3)
    return peak1(fade_out(out, 0.03)) * 0.95


def cobra_king_armor_block():
    """a bright clang on gold plate"""
    t = times(1.8)
    f = 520
    tone = np.zeros(len(t))
    for ratio, amp, decay in ((1.0, 1.0, 0.7), (2.32, 0.7, 0.45), (3.86, 0.5, 0.3),
                              (5.13, 0.35, 0.18), (6.92, 0.25, 0.1)):
        tone += amp * np.sin(2 * np.pi * f * ratio * t) * np.exp(-t / decay)
    strike = filt(noise(len(t)), "high", 2500) * np.exp(-t / 0.004) * 1.2
    thud = sweep(240, 150, 0.01, t) * np.exp(-t / 0.03) * 0.5
    tail = np.clip((1.8 - t) / 0.6, 0, 1) ** 2
    return finish(peak1((tone + strike + thud) * tail), 0.85)


def cobra_king_fruit_splat():
    """a watermelon bursting on the crown: the rind cracking open with a
    crunch, a heavy thump, a big wet splat & chunks raining down"""
    out = np.zeros(int(0.9 * SR))
    # the rind giving way: a few sharp, crisp cracks in quick succession
    for at, gain in ((0.0, 1.0), (0.011, 0.75), (0.024, 0.55), (0.04, 0.35)):
        g = times(0.05)
        crack = filt(noise(len(g)), "band", [1400, 5500]) * np.exp(-g / 0.006)
        crack += filt(noise(len(g)), "band", [500, 1400]) * np.exp(-g / 0.012) * 0.6
        place(out, peak1(crack), at, gain * 0.8)
    t = times(0.5)
    thump = sweep(150, 55, 0.025, t) * np.exp(-t / 0.11)
    place(out, peak1(np.tanh(2.0 * thump)), 0.0, 0.9)
    # the flesh splattering: a wet, slapping burst that squelches away
    splat = filt(noise(len(t)), "band", [350, 3200])
    splat *= (0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 21 * t))) * np.exp(-t / 0.09)
    place(out, peak1(splat), 0.008, 0.85)
    # chunks raining down, wet & low
    for _ in range(12):
        at = 0.07 + rng.exponential(0.11)
        if at > 0.6:
            continue
        g = times(0.1)
        chunk = sweep(rng.uniform(170, 300), 95, 0.015, g) * np.exp(-g / 0.025)
        chunk += filt(noise(len(g)), "band", [300, 1600]) * np.exp(-g / 0.015) * 0.7
        place(out, peak1(chunk), at, rng.uniform(0.15, 0.35) * np.exp(-(at - 0.07) / 0.3))
    out = np.tanh(2.6 * peak1(out)) / np.tanh(2.6)
    # weighty, but the rind's crack kept crisp
    out = filt(out, "low", 6000)
    return finish(out, 0.97)


def cobra_king_shatter():
    """a crash, then a heap of cubes tumbling & clattering apart"""
    out = np.zeros(int(1.8 * SR))
    t = times(0.6)
    crash = filt(noise(len(t)), "band", [600, 7000]) * np.exp(-t / 0.15)
    boom = sweep(110, 50, 0.05, t) * np.exp(-t / 0.25)
    place(out, peak1(crash + 1.2 * boom), 0.0, 1.0)
    for _ in range(90):
        at = 0.02 + rng.exponential(0.3)
        if at > 1.5:
            continue
        g = times(0.06)
        fc = rng.uniform(700, 2600)
        clack = np.sin(2 * np.pi * fc * g) * np.exp(-g / 0.012)
        clack += filt(noise(len(g)), "band", [fc * 0.8, fc * 2.5]) * np.exp(-g / 0.004) * 0.6
        place(out, clack, at, rng.uniform(0.1, 0.35) * np.exp(-at / 0.6))
    return finish(out, 0.9)


def horn(f, dur_s):
    """a brass call: bright harmonics opening up after the attack, a
    little vibrato coming in"""
    t = times(dur_s)
    vibrato = 1 + 0.006 * np.sin(2 * np.pi * 5.5 * t) * np.clip((t - 0.15) / 0.2, 0, 1)
    phase = 2 * np.pi * np.cumsum(f * vibrato) / SR
    bright = np.clip(t / 0.06, 0, 1)
    tone = np.zeros(len(t))
    for k in range(1, 13):
        tone += np.sin(k * phase) / k ** (1.6 - 0.7 * bright)
    env = np.clip(t / 0.03, 0, 1) * np.clip((dur_s - t) / 0.12, 0, 1)
    return peak1(filt(tone, "low", 4500) * env)


def cobra_king_victory():
    """a short horn call, G4 to C5, over a strummed C chord, a djembe roll
    landing on the chord & a sparkle on top"""
    out = np.zeros(int(2.8 * SR))
    place(out, horn(hz("G4"), 0.18), 0.0, 0.55)
    place(out, horn(hz("C5"), 0.95), 0.2, 0.6)
    place(out, horn(hz("E4"), 0.95), 0.2, 0.3)
    for k, at in enumerate(np.linspace(0.0, 0.18, 6)):
        place(out, djembe_tone(1.0 + 0.04 * k), at, 0.25 + 0.04 * k)
    place(out, djembe_bass(), 0.2, 0.55)
    place(out, djembe_slap(), 0.2, 0.4)
    for k, note in enumerate(("C4", "E4", "G4", "C5")):
        place(out, ukulele(hz(note)), 0.2 + k * 0.025, 0.35)
    for k, note in enumerate(("C6", "E6", "G6", "C7")):
        place(out, bell(hz(note), 0.9), 0.3 + k * 0.06, 0.18)
    for at in (0.2, 0.32, 0.44):
        place(out, shaker(), at, 0.2)
    return finish(out, 0.85)


# Main  ########################################################################
# name: (module folder, maker)
SOUNDS = {
    "Grab_Banana": ("Player", grab_banana),
    "Grab_Coconut": ("Player", grab_coconut),
    "Grab_Miss": ("Player", grab_miss),
    "Hit_Branch": ("Player", hit_branch),
    "Lose_CobraBite": ("Player", lose_cobra_bite),
    "Pineapple_Grab": ("Player", pineapple_grab),
    "Leap_Whoosh": ("Player", leap_whoosh),
    "Coconut_Throw": ("Player", coconut_throw),
    "Coconut_Freeze": ("Player", coconut_freeze),
    "Coconut_Thaw": ("Player", coconut_thaw),
    "Web_Stuck": ("Player", web_stuck),
    "Web_Escape": ("Player", web_escape),
    "Marker_Reached": ("Player", marker_reached),
    "Stamina_Low": ("Player", stamina_low),
    "CobraKing_Entrance": ("BossCobraKing", cobra_king_entrance),
    "CobraKing_AttackWarn": ("BossCobraKing", cobra_king_attack_warn),
    "CobraKing_ArmorBlock": ("BossCobraKing", cobra_king_armor_block),
    "CobraKing_FruitSplat": ("BossCobraKing", cobra_king_fruit_splat),
    "CobraKing_Shatter": ("BossCobraKing", cobra_king_shatter),
    "CobraKing_Victory": ("BossCobraKing", cobra_king_victory),
}


def make_all():
    global rng
    sounds = {}
    for name, (folder, make) in SOUNDS.items():
        # each its own noise, seeded by its name, so adding or dropping one
        # never changes another's take
        seed = zlib.crc32(name.encode())
        rng = np.random.default_rng(seed)
        # the borrowed ukulele draws on the music script's own noise
        make_music.rng = np.random.default_rng(seed + 1)
        sounds[name] = (folder, make().astype(np.float32))
    return sounds


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", default=os.path.join("Assets", "_Monkobra", "Audio", "SFX"))
    out_root = parser.parse_args().out
    for name, (folder, audio) in make_all().items():
        out_dir = os.path.join(out_root, folder)
        os.makedirs(out_dir, exist_ok=True)
        path = os.path.join(out_dir, name + ".wav")
        sf.write(path, audio, SR, subtype="PCM_16")
        print(f"{path}\t{len(audio) / SR:.2f} s")
