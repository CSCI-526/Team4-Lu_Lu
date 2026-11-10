# Monkobra

A 3D vertical climbing game: scale a very tall tree while a cobra chases you from below. *Raiden* meets *Gold Miner*, in 3D.

Course project for USC CSCI-526, Fall 2026.

## Overview

You play a monkey racing up an endless tree. The cobra never stops climbing, and stamina drains with every meter. Dodge branches, then hold E or Space to reach for a banana and release to grab it: a clean grab is the only way to refill stamina. Every grab costs focus that would otherwise go to dodging, so each banana is a risk. Higher up, a rare coconut can be kept and spent with a quick double-tap to freeze the cobra for a while.

The pillars and mechanics are in the [Game Design Document](docs/monkobra-gdd.md), the hazards in [Mobs](docs/mob-doc.md), grabbing in the [Grab System](docs/grab-doc.md), difficulty in [Balance Design](docs/balance-doc.md), a full run in the [Gameplay Loop](docs/gameplay-loop-doc.md), and playtest reports in [Analytics](docs/analytics-doc.md).

## Controls

| Input | Action |
| --- | --- |
| Up, Down or W, S | climb the tree |
| Left, Right or A, D | circle around the tree |
| E or Space (hold and release) | reach out and grab a banana |
| E or Space (quick double-tap) | spend a coconut to freeze the cobra |
| Space (press rapidly) | break free of a spider web |
| Esc or P | pause or resume the run |
| R (while paused) | restart the run |
| R or Space (lose screen) | restart the run |

Gamepad: left stick or D-pad moves, the north button grabs (a double-tap spends a coconut), the south button breaks free of a web.

Dev keys (development builds and the Editor only): F1 toggles the cobra no-clip cheat, F2 spawns a pineapple branch above the monkey, F3 spawns a coconut branch above the monkey, F5 toggles god mode (the stamina bar stays at max), F6 toggles a 3x speed buff, F8 downloads the analytics session report, F9 starts a new analytics session, F10 saves a mock session; the lose screen also shows a Download Analytics button, top right. The list lives in `HackShortcutConfig`, see [Hack Shortcuts](docs/monkobra-tdd.md#hack-shortcuts). On a Mac laptop hold fn with the F key, since macOS takes F1 to F12 for system functions by default.

## Getting Started

Open the repository folder through Unity Hub with Editor `6000.3.22f1` (Unity 6, URP), then open `Lv1Scene` and press Play. The game also runs in the browser from the WebGL export served by GitHub Pages; the builds are described in [Build](docs/build-doc.md).

### Testing the Coconut

Coconuts appear only from Height 150, on about one segment in ten, so a normal run meets few of them. For a quick check either:

- press F3 (fn+F3 on a Mac laptop) during Play to spawn a coconut branch above the monkey, grab it, then double-tap Space to throw it and freeze the cobra
- select `Envs/CoconutSystem` in the scene, tick `CoconutSpawner` > Use Test Override before pressing Play, and leave the scene unsaved: coconuts then hang from Height 5 (Test Start Height U) on every segment (Test Spawn Chance 1)

The real values live in the Coconut group of `Settings/Player/PlayerBalanceConfig.asset`, see the [Grab System](docs/grab-doc.md#coconut).

## Contributing

Agent rules are in [AGENTS.md](AGENTS.md), the system map in [CONTEXT.md](CONTEXT.md), how the game is built in the [Technical Design Document](docs/monkobra-tdd.md), the teammate git flow in [Work with Git](docs/work-with-git.md), and version history in [CHANGELOG.md](CHANGELOG.md).

## Acknowledgments

Team: Yuqing Lu, Yangyi Lu (Erik), Houdong Pan, Wenhai Dong, Belle Dai.
