"""Composes and synthesizes Monkobra's background music from scratch.

Writes 2 seamless loops into ``Assets/_Monkobra/Audio/Music/``:

- ``SavannaClimb.ogg``: the main loop, C major, ukulele and djembe only: 2
  hand drums in front, a strummed ukulele under them and a short picked tune
- ``BossDrums.ogg``: the boss loop, A minor, faster and tenser: the same
  ukulele and djembes plus a low stick drum, a shaker and a pulsing bass

Every sound is made here from sines and noise, no samples, so the music is
original and free of any license. Each loop is rendered on a ring: a note
ringing past the end wraps onto the start, and the reverb is a circular
convolution, so the loop point is inaudible.

Needs ``numpy``, ``scipy`` and ``soundfile``. Run from the repository root::

    python tools/music/make_music.py
"""

import os

import numpy as np
import soundfile as sf
from scipy.signal import butter, sosfilt

SR = 44100
OUT_DIR = os.path.join("Assets", "_Monkobra", "Audio", "Music")

rng = np.random.default_rng(526)


# Helpers  #####################################################################
NOTE_STEPS = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}


def hz(name):
    """
    :param name: note name, eg ``F#4``
    :return: its frequency; Hz
    """
    step = NOTE_STEPS[name[0]]
    rest = name[1:]
    if rest[0] == "#":
        step += 1
        rest = rest[1:]
    elif rest[0] == "b":
        step -= 1
        rest = rest[1:]
    midi = 12 * (int(rest) + 1) + step
    return 440.0 * 2 ** ((midi - 69) / 12)


def times(dur_s):
    return np.arange(int(dur_s * SR)) / SR


def filt(sig, kind, cutoff):
    sos = butter(2, cutoff, btype=kind, fs=SR, output="sos")
    return sosfilt(sos, sig)


def noise(n):
    return rng.uniform(-1.0, 1.0, n)


def peak1(sig):
    return sig / max(np.max(np.abs(sig)), 1e-9)


def fade_out(sig, fade_s=0.01):
    n = min(int(fade_s * SR), len(sig))
    sig[-n:] *= np.linspace(1.0, 0.0, n)
    return sig


def sweep(f_start, f_end, tau_s, t):
    """sine whose pitch falls from f_start to f_end, as a struck skin does"""
    freq = f_end + (f_start - f_end) * np.exp(-t / tau_s)
    return np.sin(2 * np.pi * np.cumsum(freq) / SR)


# Instruments  #################################################################
def djembe_bass(pitch=1.0):
    t = times(0.55)
    tune = pitch * rng.uniform(0.97, 1.03)
    body = sweep(150 * tune, 70 * tune, 0.03, t) * np.exp(-t / 0.18)
    # a 2nd partial, so the bass still speaks on small speakers
    ring = 0.35 * np.sin(2 * np.pi * 140 * tune * t) * np.exp(-t / 0.09)
    thump = filt(noise(len(t)), "low", 900) * np.exp(-t / 0.012) * 0.5
    return fade_out(peak1(body + ring + thump))


def djembe_tone(pitch=1.0):
    t = times(0.35)
    f = 330 * pitch * rng.uniform(0.97, 1.03)
    body = (
        sweep(f * 1.15, f, 0.02, t) * np.exp(-t / 0.10)
        + 0.45 * np.sin(2 * np.pi * f * 1.59 * t) * np.exp(-t / 0.06)
        + 0.25 * np.sin(2 * np.pi * f * 2.14 * t) * np.exp(-t / 0.04)
    )
    hand = filt(noise(len(t)), "band", [900, 3200]) * np.exp(-t / 0.008) * 0.6
    return fade_out(peak1(body + hand))


def djembe_slap(pitch=1.0):
    t = times(0.25)
    f = 470 * pitch * rng.uniform(0.97, 1.03)
    body = (
        np.sin(2 * np.pi * f * t) * np.exp(-t / 0.05)
        + 0.8 * np.sin(2 * np.pi * f * 2.3 * t) * np.exp(-t / 0.04)
        + 0.5 * np.sin(2 * np.pi * f * 3.6 * t) * np.exp(-t / 0.03)
    )
    crack = filt(noise(len(t)), "high", 2200) * np.exp(-t / 0.018) * 1.1
    return fade_out(peak1(body + crack))


def low_drum():
    """dundun, a low stick drum"""
    t = times(0.5)
    tune = rng.uniform(0.98, 1.02)
    body = sweep(130 * tune, 82 * tune, 0.04, t) * np.exp(-t / 0.2)
    ring = 0.3 * np.sin(2 * np.pi * 164 * tune * t) * np.exp(-t / 0.08)
    stick = filt(noise(len(t)), "band", [700, 2500]) * np.exp(-t / 0.006) * 0.5
    return fade_out(peak1(body + ring + stick))


def shaker():
    t = times(0.12)
    env = np.minimum(t / 0.006, 1.0) * np.exp(-t / 0.03)
    return fade_out(peak1(filt(noise(len(t)), "high", 5500) * env), 0.005)


def ukulele(f, muted=False):
    """nylon string pluck: soft attack, high harmonics die first; muted is
    the damped chuck of a strumming hand"""
    t = times(0.12 if muted else 1.1)
    tone = np.zeros(len(t))
    for k in range(1, 11):
        if k * f > SR / 2.5:
            break
        # plucked near a quarter of the string
        weight = np.sin(k * np.pi * 0.27) / k**1.15
        damping = (45 + 12 * k) if muted else (2.4 + 1.3 * k)
        tone += weight * np.sin(2 * np.pi * k * f * t) * np.exp(-t * damping)
    nail = filt(noise(len(t)), "band", [1500, 5000]) * np.exp(-t / 0.004)
    tone = tone + nail * (0.5 if muted else 0.08)
    # the small body rings a little round 400 Hz
    return fade_out(peak1(tone + 0.25 * filt(tone, "band", [300, 520])), 0.03)


def bass(f, dur_s):
    t = times(dur_s + 0.08)
    tone = (
        np.sin(2 * np.pi * f * t)
        + 0.45 * np.sin(2 * np.pi * 2 * f * t) * np.exp(-t / 0.25)
        + 0.22 * np.sin(2 * np.pi * 3 * f * t) * np.exp(-t / 0.12)
    )
    env = np.minimum(t / 0.004, 1.0) * np.exp(-t / 0.4)
    return fade_out(peak1(np.tanh(1.5 * tone * env)), 0.06)


# Loop  ########################################################################
class Loop:
    """A stereo loop on a ring, addressed by bar and 16th step."""

    def __init__(self, bpm, bars, swing=0.0):
        self.bpm = bpm
        self.bars = bars
        self.step_s = 60.0 / bpm / 4
        self.swing = swing
        self.n = int(round(bars * 16 * self.step_s * SR))
        self.buses = {}

    def at(self, bar, step):
        """
        :return: time of a 16th step, odd steps pushed late by the swing; s
        """
        late = self.swing * self.step_s if int(step) % 2 == 1 else 0.0
        return (bar * 16 + step) * self.step_s + late

    def add(self, bus, sig, bar, step, gain=1.0, pan=0.0, jitter_s=0.0, late_s=0.0):
        buf = self.buses.setdefault(bus, np.zeros((self.n, 2)))
        when = self.at(bar, step) + late_s + rng.uniform(-jitter_s, jitter_s)
        start = int(round(when * SR)) % self.n
        left = np.cos((pan + 1) * np.pi / 4) * gain
        right = np.sin((pan + 1) * np.pi / 4) * gain
        # wrap past the end back onto the start
        offset = 0
        while offset < len(sig):
            room = min(self.n - start, len(sig) - offset)
            chunk = sig[offset : offset + room]
            buf[start : start + room, 0] += chunk * left
            buf[start : start + room, 1] += chunk * right
            offset += room
            start = 0

    def mix(self, levels, sends, target_rms=0.15):
        """
        :param levels: gain of each bus
        :param sends: share of each bus sent to the reverb
        :return: the mastered stereo loop
        """
        dry = np.zeros((self.n, 2))
        send = np.zeros((self.n, 2))
        for name, buf in self.buses.items():
            dry += buf * levels[name]
            send += buf * levels[name] * sends.get(name, 0.0)

        # a small bright room; convolved on the ring, its tail wraps round
        t = times(1.1)
        wet = np.zeros((self.n, 2))
        for ch in range(2):
            ir = filt(noise(len(t)), "band", [300, 6000]) * np.exp(-t / 0.22)
            ir[: int(0.012 * SR)] = 0.0
            ir /= np.sqrt(np.sum(ir**2))
            padded = np.zeros(self.n)
            padded[: len(ir)] = ir
            wet[:, ch] = np.fft.irfft(
                np.fft.rfft(send[:, ch]) * np.fft.rfft(padded), self.n
            )
        out = dry + wet
        # small speakers play nothing down here, it only eats headroom; the
        # filter runs twice round the ring so its start matches its end
        for ch in range(2):
            twice = filt(np.concatenate([out[:, ch], out[:, ch]]), "high", 45)
            out[:, ch] = twice[self.n :]

        out *= target_rms / np.sqrt(np.mean(out**2))
        # soft limiter: rounds off the drum peaks instead of clipping them
        return np.tanh(out * 1.6) * 0.94


# Patterns  ####################################################################
def play_djembe(loop, bus, bar, pattern, gain=1.0, pan=0.0, pitch=1.0):
    """
    :param pattern: 16 characters, 1 per step: ``B`` bass, ``t`` tone, ``s``
        slap, ``b`` a soft bass, ``,`` a ghost tone, ``.`` rest
    :param pitch: tuning of the drum, a smaller drum sits higher
    """
    for step, hit in enumerate(pattern):
        if hit == ".":
            continue
        accent = rng.uniform(0.88, 1.0)
        if hit == "B":
            sig, level = djembe_bass(pitch), 1.0 * accent
        elif hit == "b":
            sig, level = djembe_bass(pitch), 0.6
        elif hit == "t":
            sig, level = djembe_tone(pitch), 0.85 * accent
        elif hit == ",":
            sig, level = djembe_tone(pitch), 0.35
        else:
            sig, level = djembe_slap(pitch), 0.9 * accent
        loop.add(bus, sig, bar, step, gain * level, pan, 0.004)


def play_hits(loop, bus, make, bar, steps, gain=1.0, pan=0.0):
    """
    :param steps: ``{step: velocity}``
    """
    for step, velocity in steps.items():
        loop.add(bus, make(), bar, step, gain * velocity, pan, 0.002)


def play_shaker(loop, bar, gain=1.0):
    for step in range(16):
        # the off-beat 8ths lead, that is the bounce
        accent = 1.0 if step % 4 == 2 else 0.55 if step % 2 == 0 else 0.4
        loop.add("shaker", shaker(), bar, step, gain * accent, 0.55, 0.003)


def play_strums(loop, bar, chord, pattern, gain=1.0):
    """
    :param chord: 4 note names, the strings from the 4th (nearest the chin)
        to the 1st
    :param pattern: 16 characters, 1 per step: ``D`` down strum, ``U`` up
        strum, ``x`` muted chuck, ``.`` rest
    """
    for step, stroke in enumerate(pattern):
        if stroke == ".":
            continue
        # a down strum crosses all 4 strings, an up strum catches the top 3
        strings = chord if stroke != "U" else chord[:0:-1]
        level = {"D": 1.0, "U": 0.7, "x": 0.8}[stroke] * rng.uniform(0.9, 1.0)
        for i, name in enumerate(strings):
            loop.add("strum", ukulele(hz(name), muted=stroke == "x"), bar, step,
                     gain * level, -0.35, 0.002, late_s=i * 0.013)


def play_tune(loop, bar, notes, gain=1.0):
    """
    :param notes: ``[(step, note name, velocity)]``
    """
    for step, name, velocity in notes:
        loop.add("tune", ukulele(hz(name)), bar, step, gain * velocity, 0.3, 0.003)


# Main Loop  ###################################################################
def make_savanna_climb():
    """C major, 124 BPM, 16 bars, ukulele & 2 djembes only: groove, groove
    with the tune, a drum break, groove with the tune."""
    loop = Loop(bpm=124, bars=16, swing=0.14)

    # ukulele chord shapes, strings G C E A
    chords = {
        "C": ["G4", "C4", "E4", "C5"],
        "Am": ["A4", "C4", "E4", "A4"],
        "F": ["A4", "C4", "F4", "A4"],
        "G": ["G4", "D4", "G4", "B4"],
    }
    progression = (["C", "Am", "F", "G"] * 2
                   + ["F", "G", "C", "Am"] + ["F", "G", "C", "G"])

    # a short picked tune, C major pentatonic, 4 notes a bar
    tune = {
        4: [(0, "E5", .9), (3, "G5", .9), (6, "E5", .8), (8, "C5", 1)],
        5: [(0, "E5", .9), (3, "G5", .9), (6, "A5", 1), (8, "E5", .9)],
        6: [(0, "A5", 1), (3, "G5", .9), (6, "F5", .8), (8, "C5", .9)],
        7: [(0, "D5", .9), (3, "G5", .9), (6, "B5", 1), (8, "G5", .9), (12, "D5", .8)],
        12: [(0, "A5", .9), (3, "C6", 1), (6, "A5", .8), (8, "F5", .9)],
        13: [(0, "B5", .9), (3, "D6", 1), (6, "B5", .8), (8, "G5", .9)],
        14: [(0, "C6", 1), (3, "G5", .9), (6, "E5", .8), (8, "C5", .9)],
        15: [(0, "D5", .9), (3, "G5", .9), (6, "B5", .9), (8, "D6", 1)],
    }

    # lead djembe, and a smaller one answering it from the other side
    lead_1 = "B.tts..tt.B.s.tt"
    lead_2 = "B.tts.s.ttB.sstt"
    lead_break_1 = "BttsB.tsBttss.ts"
    lead_break_2 = "B.ssBttsttsBtsss"
    lead_fill = "B.tts.s.ttsstsss"
    second = "s..,t.s.s.,tt.s."
    second_break = "b.s.,ts.b.s.tts."

    island_strum = "D...D.U...U.D.U."
    break_chucks = "....x.......x.x."

    for bar in range(16):
        chord = chords[progression[bar]]
        is_break = 8 <= bar < 12

        if is_break:
            # the drums take the floor, the ukulele only ticks along
            play_djembe(loop, "lead", bar, lead_break_2 if bar % 2 == 1 else lead_break_1,
                        pan=-0.2)
            play_djembe(loop, "second", bar, second_break, pan=0.45, pitch=1.22)
            play_strums(loop, bar, chord, break_chucks)
        else:
            play_djembe(loop, "lead", bar, lead_fill if bar == 15
                        else lead_2 if bar % 4 == 3 else lead_1, pan=-0.2)
            play_djembe(loop, "second", bar, second, pan=0.45, pitch=1.22)
            play_strums(loop, bar, chord, island_strum)
        if bar in tune:
            play_tune(loop, bar, tune[bar])

    return loop.mix(
        levels={"lead": 1.0, "second": 0.5, "strum": 0.36, "tune": 0.6},
        sends={"lead": 0.2, "second": 0.25, "strum": 0.3, "tune": 0.4},
    )


# Boss Loop  ###################################################################
def make_boss_drums():
    """A minor, 132 BPM, 16 bars: the main loop's ukulele & djembes, tenser,
    plus a low stick drum, a shaker and a pulsing bass."""
    loop = Loop(bpm=132, bars=16, swing=0.06)

    chords = {
        "Am": {"uke": ["A4", "C4", "E4", "A4"], "root": "A2"},
        "F": {"uke": ["A4", "C4", "F4", "A4"], "root": "F2"},
        "E7": {"uke": ["G#4", "D4", "E4", "B4"], "root": "E2"},
        "Dm": {"uke": ["A4", "D4", "F4", "A4"], "root": "D3"},
    }
    progression = (["Am", "Am", "F", "E7"] * 2
                   + ["Dm", "Dm", "Am", "Am"] + ["F", "F", "E7", "E7"])

    # a sparse picked line leaning on the flat 6th (F) and the leading tone
    # (G#), the 2 notes that make A minor uneasy
    tune = {
        4: [(0, "E5", 1), (6, "F5", .9), (8, "E5", .9), (12, "C5", .8)],
        5: [(0, "E5", 1), (6, "F5", .9), (8, "E5", .9), (12, "A5", 1)],
        6: [(0, "A5", 1), (6, "G5", .9), (8, "F5", .9), (12, "C5", .8)],
        7: [(0, "B5", 1), (6, "G#5", .9), (8, "E5", .9), (12, "G#5", .9)],
        12: [(0, "A5", .9), (4, "C6", 1), (8, "A5", .9), (12, "F5", .8)],
        13: [(0, "A5", .9), (4, "C6", 1), (8, "D6", 1), (12, "C6", .9)],
        14: [(0, "B5", 1), (4, "G#5", .9), (8, "E5", .9), (12, "G#5", .9)],
        15: [(0, "B5", .9), (4, "D6", 1), (8, "E6", 1)],
    }

    lead_1 = "B.tts.ttB.tts.ts"
    lead_2 = "B.tts.ttBttsstss"
    lead_break = "BttsBttsB.tsBtss"
    lead_fill = "BttsttssBtsststs"
    second = "s.,ts.t,s.,tstt."

    tense_strum = "D..x..UU..U.x.U."
    break_chucks = "..x...x...x...x."

    for bar in range(16):
        chord = chords[progression[bar]]
        is_break = 8 <= bar < 12

        # Drums  ---------------------------------------------------------------
        if is_break:
            # the hand drums alone with the shaker, the low end holds back
            play_djembe(loop, "lead", bar, lead_break, pan=-0.2)
            play_hits(loop, "low", low_drum, bar, {0: 1.0, 8: 0.8}, pan=-0.05)
        else:
            play_djembe(loop, "lead", bar, lead_fill if bar == 15
                        else lead_2 if bar % 2 == 1 else lead_1, pan=-0.2)
            # the low drum gallops, that is the chase
            play_hits(loop, "low", low_drum, bar,
                      {0: 1.0, 3: 0.6, 6: 0.8, 8: 1.0, 11: 0.6, 14: 0.8}, pan=-0.05)
        play_djembe(loop, "second", bar, second, pan=0.45, pitch=1.22)
        play_shaker(loop, bar)
        if bar % 4 == 3:
            play_hits(loop, "low", low_drum, bar, {12: .7, 13: .8, 15: 1.0}, pan=-0.05)

        # Bass  ----------------------------------------------------------------
        # steady 8ths on the root, a heartbeat under the drums
        if not is_break:
            for step in range(0, 16, 2):
                loop.add("bass", bass(hz(chord["root"]), 2 * loop.step_s), bar, step,
                         1.0 if step % 4 == 0 else 0.7)

        # Ukulele  -------------------------------------------------------------
        play_strums(loop, bar, chord["uke"], break_chucks if is_break else tense_strum)
        if bar in tune:
            play_tune(loop, bar, tune[bar])

    return loop.mix(
        levels={"lead": 1.0, "second": 0.5, "low": 0.4, "shaker": 0.6,
                "bass": 0.15, "strum": 0.32, "tune": 0.55},
        sends={"lead": 0.2, "second": 0.25, "low": 0.15, "shaker": 0.1,
               "strum": 0.3, "tune": 0.4},
    )


# Main  ########################################################################
def save(name, audio):
    os.makedirs(OUT_DIR, exist_ok=True)
    path = os.path.join(OUT_DIR, name)
    with sf.SoundFile(path, "w", SR, 2, format="OGG", subtype="VORBIS") as out:
        # in pieces: libsndfile's Vorbis writer chokes on one huge block
        for start in range(0, len(audio), SR):
            out.write(audio[start : start + SR])
    print(f"{path}\t{len(audio) / SR:.2f} s\tpeak {np.max(np.abs(audio)):.2f}"
          f"\trms {np.sqrt(np.mean(audio**2)):.3f}")


if __name__ == "__main__":
    save("SavannaClimb.ogg", make_savanna_climb())
    save("BossDrums.ogg", make_boss_drums())
