# Monkobra

A 3D vertical climbing game: scale a very tall tree while a cobra chases you from below. *Raiden* meets *Gold Miner*, in 3D.

Course project for USC CSCI-526, Fall 2026.

## Overview

You play a monkey racing up an endless tree. The cobra never stops climbing, and stamina drains with every meter. Dodge branches, then hold E or Space to reach for a banana and release to grab it: a clean grab is the only way to refill stamina. Every grab costs focus that would otherwise go to dodging, so each banana is a risk. Higher up, a rare coconut can be kept and spent with a quick double-tap to freeze the cobra for a while.

The pillars and mechanics are in the [Game Design Document](docs/monkobra-gdd.md), the hazards in [Mobs](docs/mob-doc.md), grabbing in the [Grab System](docs/grab-doc.md), the center messages in [PopUps](docs/pop-up-doc.md), difficulty in [Balance Design](docs/balance-doc.md), a full run in the [Gameplay Loop](docs/gameplay-loop-doc.md), the monkey's look and climb animation in the [Monkey Model](docs/monkey-model-doc.md), the shared look of every screen in [UI Style](docs/ui-style-doc.md), and playtest reports in [Analytics](docs/analytics-doc.md).

## Controls

| Input | Action |
| --- | --- |
| Up, Down or W, S | climb the tree |
| Left, Right or A, D | circle around the tree |
| Space, E or Enter (hold and release) | reach out and grab a banana |
| Space, E or Enter (quick double-tap) | spend a coconut to freeze the cobra |
| Space, E or Enter (press rapidly) | break free of a spider web |
| Esc or P; pause button, top right (click or tap) | pause or resume the run |
| R (while paused) | restart the run |
| R (lose screen) | restart the run |
| Nickname field (lose screen): type, then Enter or SAVE | save a display nickname without changing player identity |
| W, S or Up, Down; mouse hover (pause and lose screens) | move the highlight over Resume, Restart, Exit (back to the level selection) |
| Space, E, Enter or click (pause and lose screens) | confirm the highlighted option; on the lose screen with nothing highlighted, restart |
| Space, E, Enter, click or tap (title) | open the intro comic the first time, then the level selection |
| Space, E, Enter, click or tap (intro comic) | finish the panel, then turn the page |
| Esc or P; Skip button (intro comic) | skip to the level selection |
| W, S or Up, Down; mouse hover (menus) | move the highlight |
| Space, E, Enter or click (menus) | confirm |
| Esc or P (level selection) | back to the title |
| Space, E or Enter (boss revive offer, ad, reward panel) | watch the ad, skip the ad, continue |
| R (boss revive offer) | give up |

Every confirm key works everywhere a confirm is asked; Space is the one the hints teach. The course page button of the boss ad is clicked or tapped, never keyed. The full scheme is in [Input](docs/input-doc.md).

Gamepad: left stick or D-pad moves, the north button grabs (a double-tap spends a coconut), the south button breaks free of a web.

Touch: the first touch switches the game to touch mode, a key press switches it back, and every on-screen hint follows. A joystick appears bottom left and moves the monkey. The rest of the screen is the Space key: hold anywhere to reach and release to grab, double-tap anywhere to spend a coconut, tap rapidly to break free of a web, tap to start the run from the tutorial. Menus, the pause and lose screens and the boss buttons are tapped like any button.

Dev keys (their hints and the dev-only UI show only in development builds and the Editor; a release build keeps the keys live except F12): in a development build or the Editor F12 turns them on or off together with the on-screen list of these keys (off at start); F1 toggles the cobra no-clip cheat (the great cobra no longer kills), F2 toggles max energy (the stamina bar stays at max), F3 toggles a 3x speed buff, 1 jumps to the Cobra King's fight, 2 to the Spider King's, 3 spawns a pineapple branch above the monkey, 4 a coconut branch, 5 a falling cobra, 6 a spider web, F8 downloads the analytics session report, F9 starts a new analytics session, F10 saves a mock session; the lose screen also shows a Download Analytics button, top right. The list lives in `HackShortcutConfig`, see [Hack Shortcuts](docs/monkobra-tdd.md#hack-shortcuts). On a Mac laptop hold fn with the F key, since macOS takes F1 to F12 for system functions by default.

## Getting Started

Open the repository folder through Unity Hub with Editor `6000.3.22f1` (Unity 6, URP), then open `TitleScene` and press Play (or open `GameplayScene` to start a run directly). Endless Mode unlocks after the Cobra King is beaten once; for testing use the Editor menu `Monkobra/Unlock Endless`. The game also runs in the browser from the WebGL export served by GitHub Pages; the builds are described in [Build](docs/build-doc.md).

### Testing the Coconut

Coconuts appear only high on the tree and are rare, so a normal run meets few of them. For a quick check, press 4 during Play to spawn a coconut branch above the monkey, grab it, then double-tap Space to throw it and freeze the cobra.

The real values live in the Coconut group of `Settings/Player/PlayerBalanceConfig.asset`, see the [Grab System](docs/grab-doc.md#coconut).

### Optional Online Leaderboard

The lose screen supports Firebase's Spark free plan for persistent top scores.
The config asset and its prefab wiring already exist; follow
[Firebase Leaderboard](docs/leaderboard-doc.md) to enable Anonymous Auth, publish
the rules and enter the public client settings. Until configured it shows the
original simulated **PRACTICE** board. Local emulators allow testing without a
Firebase account; no paid services are required by this integration.

On the lose screen, click or tap the name field and enter 1–16 English letters,
numbers, spaces, hyphens or underscores (start with a letter or number), then
press SAVE or Enter. The nickname is remembered on this device; changing it keeps
the same player and highest score. Names may repeat without sharing scores.
While typing, run/menu shortcuts and hack keys are suppressed. Different browsers,
devices or cleared site data can create a new anonymous player; account login is
not part of the web playtest yet.

## Contributing

Agent rules are in [AGENTS.md](AGENTS.md), the system map in [CONTEXT.md](CONTEXT.md), the control scheme in [Input](docs/input-doc.md), how the game is built in the [Technical Design Document](docs/monkobra-tdd.md), the teammate git flow in [Work with Git](docs/work-with-git.md), and version history in [CHANGELOG.md](CHANGELOG.md).

## Acknowledgments

Team: Yuqing Lu, Yangyi Lu (Erik), Houdong Pan, Wenhai Dong, Belle Dai.
