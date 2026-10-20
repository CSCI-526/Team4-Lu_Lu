# Monkobra

A 3D vertical climbing game: scale a very tall tree while a cobra chases you from below. *Raiden* meets *Gold Miner*, in 3D.

Course project for USC CSCI-526, Fall 2026.

## Overview

You play a monkey racing up an endless tree. The cobra never stops climbing, and stamina drains with every meter. Dodge branches, then hold E or Space to reach for a banana and release to grab it: a clean grab is the only way to refill stamina. Every grab costs focus that would otherwise go to dodging, so each banana is a risk.

The pillars and mechanics are in the [Game Design Document](docs/monkobra-gdd.md), the hazards in [Mobs](docs/mob-doc.md), grabbing in the [Grab System](docs/grab-doc.md), difficulty in [Balance Design](docs/balance-doc.md), and a full run in the [Gameplay Loop](docs/gameplay-loop-doc.md).

## Controls

| Input | Action |
| --- | --- |
| Up, Down or W, S | climb the tree |
| Left, Right or A, D | circle around the tree |
| E or Space (hold and release) | reach out and grab a banana |
| Space (press rapidly) | break free of a spider web |

Gamepad: left stick or D-pad moves, the north button grabs, the south button breaks free of a web.

## Getting Started

Open the repository folder through Unity Hub with Editor `6000.3.22f1` (Unity 6, URP), then open `Lv1Scene` and press Play. The game also runs in the browser from the WebGL export served by GitHub Pages; the builds are described in [Build](docs/build-doc.md).

## Contributing

Agent rules are in [AGENTS.md](AGENTS.md), the system map in [CONTEXT.md](CONTEXT.md), how the game is built in the [Technical Design Document](docs/monkobra-tdd.md), the teammate git flow in [Work with Git](docs/work-with-git.md), and version history in [CHANGELOG.md](CHANGELOG.md).

## Acknowledgments

Team: Yuqing Lu, Yangyi Lu (Erik), Houdong Pan, Wenhai Dong, Belle Dai.
