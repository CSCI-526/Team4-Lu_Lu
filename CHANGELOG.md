# Monkobra CHANGELOG

<!--
Todo upd all docs base on codebase
Todo update dev log
todo upd gdd
-->


## [Unreleased]

### Added

### Changed

### Deprecated

### Removed

### Fixed

### Security

[unreleased]: https://github.com/kami-lel/monkobra/compare/v1.0.0-beta...dev

## [1.0.0-beta] - 2026-11-10

### Added

##### Belle Dai

- rough bark: avoidable raised patches on the trunk smoothly slow the monkey, replacing slippery bark (`RoughBarkZone`, `RoughBarkSpawner`)

##### Erik

- scene system: title, level select (Normal Play, Endless Mode, Back), `Lv1Scene` & `EndlessScene`; `SceneNames.Load` loads every scene
- Endless Mode: starts at the tree base in the post boss state, difficulty keeps ramping; unlocks once the Cobra King is beaten (Editor menu `Monkobra/Unlock Endless`, `Monkobra/Reset Progress`)
- pause & lose screens: Resume, Restart, Exit to Title by keys, mouse hover or click (`RunScreenMenu`)
- analytics: 1 session file per launch across scene loads, runs tagged Full or Endless; every run event is an `AnalyticsMetric`, so a new metric is 1 entry; records leaps, rough bark, freezes, pauses, boss fight events & the Starting Zone Exit
- dev builds only: Download Analytics button on the lose screen; hack keys F1~F12 for no-clip, max stamina, speed x3, boss jump, spawning a pineapple branch, coconut branch, falling cobra or spider web, report download, new session & mock session
- `Lv1Scene` split into nested prefabs, so teammates edit different files
- Great Cobra exit boost after the monkey leaves the starting zone, w/ "Run! The Great Cobra is chasing you!" warning
- climb speed builds up along curves of key hold time; branches cleared round the monkey before the run is playing
- fruit fly: a grabbed fruit's sprite flies to the stamina bar, coconut HUD or monkey
- pineapple leap: a grab pulls the monkey up, hazards off for the leap & a short landing grace
- coconut throw: a double-tap spends 1, it falls & freezes the cobra on touch or after a fixed distance; brown coconut model & new fruit sprites, credited in `CREDITS.md`
- consistent pop up UI: every center message is a variant of `PopUp/_PopUpBase`, shown 1 at a time by `PopUpManager` w/ priority & queue, see `docs/pop-up-doc.md`
- tree bark texture on trunk segments, credited in `CREDITS.md`
- camera redesign: every gameplay camera tracks `CameraAnchor`, so strafing no longer spins the view; climb framing, hold curves & noise on rough bark; all tuning in `CameraConfig`
- shot director (`CameraRigManager`): coconut, cobra alert & pineapple leap shots by priority, so events never chain into rapid cuts
- height marker pass trigger (`HeightMarkerTrigger`)
- difficulty: the post boss ramp keeps rising after the fight; a boss zone holds only the boss fight; 4 branch kinds (empty, banana, coconut, pineapple) w/ own chance curves; a guaranteed pineapple & coconut branch at fixed heights

##### Wenhai Dong

- Cobra King boss fight (`Boss/BossFight` prefab): at the trigger height the run enters an arena; the king coils round the tree top, alternates rests with attack rounds (bite, tail sweep, minion waves, head butt) that grow with each lost third of its health
- boss arena: banana branches whose fruit is also thrown at the crown, spider webs, a golden banana that refills stamina & shields 1 fatal hit; the monkey's climb is capped below the king, the great cobra guards below
- boss fight presentation: danger zone warnings, health bar & speech, first-throw key prompt, slow-motion K.O., camera pulls back & tilts up
- boss fight win: the king shatters, a reward panel shows, the monkey keeps a Golden Armor that absorbs 1 great cobra catch, the run goes on
- boss fight revive: every loss offers a revive for watching a mock ad; after a revive the first throw picks EASY or NORMAL
- boss fight height marker: a bigger, red, beating plate w/ a Cobra King look-alike, hidden once the fight begins
- `GameState.Won`: a won run settles the score & analytics records it as `RunStatus.Won`
- pause: Esc or P pauses or resumes a live run, R restarts it; time freezes, audio pauses, a `PauseScreen` panel shows
- tree bark less glossy but still crisp at steep angles

##### Yuqing Lu

- coconut: a rare fruit grabbed like a banana, scores w/ a multiplier & restores no stamina
- coconut use: a quick double-tap freezes the great cobra for a while; refused while paused, trapped in a web or already frozen; the cobra turns light blue while frozen
- coconut HUD: count, freeze countdown & bar, a double-tap hint until the first use, center messages; coconut analytics per run
- green coconut model & matching UI sprite from the PolyOne Free Fruits pack, credited in `CREDITS.md`
- trunk height markers: every 50 u a number beside the trunk in m on a dark rounded plate, 1 on each trunk edge as seen from the camera; the marker grows & brightens the first time the monkey passes it

##### Houdong Pan

- lose screen leaderboard: simulated rivals, fixed per launch, ranked against the final score
- fruit grab prompt: keycap "space, hold, then release" teaches the first grab of each fruit kind per run

### Changed

##### Erik

- starting zone: opens at lower stamina, exhaustion slows less, idle recovery slower, exit trigger raised; Starter Zone → Starting Zone
- pause & restart are `InputSystem_Actions` actions read by `GameController`
- fruit grab aims at the closest banana; stamina restore & bar punch land when the fruit's flight arrives
- scripts renamed to house role suffixes & code style pass over all scripts; no behavior change
- prefab restructure: `World` & `Systems` wrappers dropped, `Analytics`, `InGameHUD` & `PopUps` split out; `CoconutManager` centralizes the coconut count, throw & HUD
- banana model: `SM_Fruits_Banana_Y1` replaces the stretched capsules
- config reorganization: `GameBalanceConfig` split into `PlayerBalanceConfig`, `WorldBalanceConfig` & `CobraBalanceConfig`, `MonkeyConfig` into arm & input configs, `MovementConfig` → `MonkeyClimbConfig`, `Settings/` 1 folder per module; every value carried over
- camera: `CameraEfxConfig` → `CameraConfig`, `CameraLookUp` → `CameraClimbFraming`; low stamina pulse vignette only; hit shake scaled per kind; `CameraRigManager` no longer toggles the shared Interact action
- `DynamicTreeSegmentPool` holds more segments & looks further ahead
- height markers show up to 50 u ahead & pulse on touching their trigger
- ramped difficulty runs 0 at the starting zone exit to 1 at the boss fight; first appearances follow height
- first-grab fruit popup also detects an unlearned fruit nearby; falling cobra stops against tree branches; boss arena fruit shows the watermelon model

##### Yuqing Lu

- `Coconut` prefab: from banana-shaped placeholder to the coconut item w/ its own score kind & outlines
- `ScoreManager` scores & counts each pickup by `PickupKind`; a coconut's flight restores no stamina; analytics splits reward score into banana & coconut score
- progress bar cobra gap in m, matching the height markers; `unitsPerMeter` now only sets the distance score rate

##### Houdong Pan

- progress bar redrawn from generated antialiased sprites w/ an upright readout
- boss fight key prompt reuses the keycap of the fruit tutorial

### Removed

##### Belle Dai

- falling cobras removed on hit

##### Erik

- `devYuqingCoconutScene` & `SlipTestScene` personal dev scenes
- `upSpeedU`, `downSpeedU` & `accelerationU`, replaced by the climb speed curves
- orphan `GoalText` object in `Lv1Scene`
- look-left & look-right cameras: a grab shifts the climb view instead of cutting
- cobra alert on surges, falling cobra hits & proximity
- `CoconutSpawner` & the `CoconutSystem` prefab, replaced by the coconut branch kind; fixed coconut start height, rough bark score gate & forced first patch: the ramp decides

##### Yuqing Lu

- HUD Height label: the trunk height markers show the climbed height

### Fixed

##### Erik

- restart could spawn the monkey inside a branch
- analytics dropped starting zone data; a run now opens when the zone begins
- camera dizziness: world spin on strafe, background judder, side flip while grab held, nod on every up press
- hit shake reached only the behind view; stamina pulse could leave a widened FOV on the old camera

##### Yuqing Lu

- coconuts never spawned in `Lv1Scene` ⇐ `CoconutSystem` prefab missing under `Envs`

##### Wenhai Dong

- boss fight: an armored monkey could drop past the guarding great cobra & never climb back alive; the armor's save now flies it back to the fight's start, untouchable on the way
- boss fight: pineapples still grew in the arena ⇐ they score as bananas; now told apart by `LeapOnGrab`
- a branch hit in the starting zone showed "The cobra is speeding up!" ⇐ mistakes counted as surges there

[1.0.0-beta]: https://github.com/kami-lel/monkobra/compare/v1.0.0-alpha.2...v1.0.0-beta













## [1.0.0-alpha.2] - 2026-10-19

### Added

##### Erik

- `HackShortcutConfig` asset: all dev keys in one place, dev builds only
- F8 downloads the analytics session report in the browser (WebGL dev build)
- F1 toggles the cobra no-clip cheat
- `HackShortcutHelp`: TMP text listing the dev keys, placed on the `Lv1Scene` canvas

### Changed

##### Erik

- analytics F9 & F10 keys moved to `HackShortcutConfig`, dev builds only
- WebGL build profiles (`GithubPages`, `ItchIO`) now build as development builds, so the dev keys work in the browser

[1.0.0-alpha.2]: https://github.com/kami-lel/monkobra/compare/v1.0.0-alpha...v1.0.0-alpha.2

## [1.0.0-alpha] - 2026-10-19

### Added

##### Yuqing Lu

- tutorial goal line above the keys
- `Energy` label on the stamina bar
- first low-stamina hint: tells the player to eat bananas, once per scene load

##### Belle Dai

- keyboard-style tutorial controls: WASD cluster & Space key w/ captions
- tutorial UI font w/ WebGL support, no system fonts needed

##### Erik

- camera cool-down effect: vignette & zoom-out pulse on move attempts at low stamina, tuned in `CameraEfxConfig`
- camera hit shake: light for branch hits, heavier for falling cobra
- starting zone: live run before Playing begins, ended by the monkey entering an exit trigger; Great Cobra holds height meanwhile
- ramped difficulty starts at 0 at the starting zone exit
- grab arm tracks its banana while interact is held

### Changed

##### Yuqing Lu

- tutorial keys & start prompt moved down to make room for the goal line

##### Erik

- difficulty ramp & branch/banana curves start above the starting zone exit
- Great Cobra starts lower (-5 → -10)
- low stamina, not only empty, now triggers move-attempt feedback
- `Lv1Scene` split into prefabs, easing scene merges
- Mobile render profile now renders at full scale (1.0, was 0.8), matching the PC profile

##### Wenhai Dong

- web builds lock to 720p w/ scaling UI layout, fit any screen & density
- web builds use new `FitWindow` template: 16:9, scales to browser window, sharp on Retina & 4K

### Removed

##### Erik

- timer-based stamina drain & its `GameBalanceConfig` fields

### Fixed

##### Erik

- stamina bar dropping by a lump while moving, stunned, or after releasing a key

[1.0.0-alpha]: https://github.com/kami-lel/monkobra/compare/v0.9.0...v1.0.0-alpha













## [0.9.0] - 2026-10-12

### Added

##### Erik

- endless tree: `DTSPool` recycles the lowest segment over the top as the monkey climbs
- spider web & falling cobra spawn in `GameBalanceConfig`: an attempt count per segment and a probability curve of the ramped difficulty, the same model as branches
- analytics: a markdown session report per launch, saved during play, w/ Mermaid graphs per run (score split, height & stamina vs time) & a summary of lost runs (death causes, final score, height, duration)
- analytics records each run's death cause, inferred from the events just before the loss, & the death height
- analytics marks a run cut short by stopping Play mode, a scene reload, or F9 as `Interrupted`
- `AnalyticsConfig` asset: analytics keys, sample & autosave intervals, death cause window
- F10 saves a fully mocked session report, for checking it without playing

##### Yuqing Lu

- spider web trap: webs spawn on the upper trunk clear of branches & fruit, hold the monkey on contact, & break on rapid Space presses; first-trap hint, escape bar, short immunity after escaping
- `SpiderWebSpawner` test override: webs at a flat chance at any height
- `RampedDifficultyService.GetClimbedYDistance`: height of any y above the player's start

##### Belle Dai

- falling cobra: warns, then drops on the monkey; in `Lv1Scene` & the `FallingCobraDemo` test scene

##### Houdong Pan

- scoring: distance score from the highest climb plus a reward per banana, shown live on a HUD score label

##### Wenhai Dong

- great cobra base speed rises w/ the ramped difficulty
- great cobra mistake surge: each branch or falling cobra hit & every web trap speeds it up for a while, stacking; on-screen cue & red cobra marker while it lasts
- falling cobra wakes from farther away while the monkey reaches for a banana, & some ambush just above a banana
- falling cobra re-hit tuning: grace time & max hits per snake
- game balance: Great Cobra group in `GameBalanceConfig` & a playtest-driven pass over every hazard curve, for a 7~8 min run w/ the cobra pressing harder as the climb goes on

### Changed

##### Erik

- falling cobras roll per tree segment like branches, wake near the monkey, & fall at a constant speed while sliding toward it; the global timer, unlock height & one-at-a-time limit are gone
- spider webs spawn from an attempt count & a difficulty curve, replacing the start height, full-chance height, max chance & slot count
- analytics tracks every web encounter & records the highest height as a float
- `docs/work-with-git.md` asks for a short PR description, since it feeds each member's Weekly Progress

##### Yuqing Lu

- webs spawn per tree segment & measure height from the player's start, replacing a one-off pass at `Start`

##### Wenhai Dong

- great cobra: bead-chain body w/ taper, hooded head, slither wave, & a corkscrew coil of at least 1 loop round the trunk, so dropping onto it or orbiting into it is fatal
- balance retuned for a 7~8 min run: ramp reaches max at 1200 u (was 500), more branches, steadier bananas, idle stamina recovery 2 → 2.5 per s, banana restore 30 → 35
- `Lv1Scene` progress bar shows 60 u below the monkey (was 15)
- web & trap box 20% larger

##### Belle Dai

- falling cobra in `FallingCobraDemo` uses the branch-hit stun & drop instead of ending the run

### Fixed

##### Yuqing Lu

- webs missing from `Lv1Scene` since the endless tree

##### Wenhai Dong

- cobra contact script failed to load ⇐ class name mismatch
- cobra contact now counts the monkey's tail & hands, & overlap, not only body entry
- cobra no longer retreats w/ a descending monkey or parks at its height, so body contact kills at once
- monkey body collider matches its visible body, so contact registers from its bottom
- falling cobra drops the monkey on any-part contact, & no longer pushes it down several times in a row
- webs make the great cobra surge on every trap, not only the first

##### Belle Dai

- falling cobra warning starts red at once & pulses from that moment
- falling cobra spawner works on the endless tree

[0.9.0]: https://github.com/kami-lel/monkobra/compare/v0.1.1...v0.9.0

## [0.1.1] - 2026-09-29

### Added

- fruit grab: hold interact to stretch arm toward newest fruit, release to grab iff hand overlaps it
- fruit grab restores stamina
- movement-based stamina drain: climbing costs most, orbiting less, descent free
- `GameConfig` asset centralizing game-wide tuning

### Changed

- stamina tuning → `GameConfig`
- `StaminaBar` reachable as singleton `StaminaBar.I`
- tuning assets → `Settings/`
- arm reach: aim locks at press, replaces spring/overshoot reach
- arm joint reach limit ↑
- `Lv1Scene` camera views retuned

### Removed

- legacy arm drivers `MonkeyArm` & `HandHandler`, superseded by `ArmRoot`
- unused reach tuning from `MonkeyConfig`

[0.1.1]: https://github.com/kami-lel/monkobra/compare/v0.1.0...v0.1.1

## [0.1.0] - 2026-09-28

### Added

- Unity 6 URP project scaffold, `Lv1Scene`
- `README.md` game design & controls, `AGENTS.md`, `CONTEXT.md`
- monkey movement: orbit trunk, climb w/ accel/decel ramp, Input System controls
- hand-over-hand arm climb stroke on physics-jointed arms, tail
- Cinemachine camera rig: behind view, left/right side views driven by fruit-reach detection
- climbable tree from static & dynamic trunk segments under `TreeTop`
- procedural branch & banana placement, tuned by `BranchFruitPlacementConfig`
- branch-hit penalty: camera shake, stun, drop
- stamina bar UI w/ timed drain
- tree-progress bar tracking player height
- cobra chasing up trunk, game-over on contact
- win zone trigger, `WinScreen`, `ScreensManager`
- `MonkeyConfig` asset centralizing monkey tuning
- scene singletons `GameController`, `TreeManager`
- WebGL build served from `Monkobra/` via GitHub Pages

[0.1.0]: https://github.com/kami-lel/monkobra/releases/tag/v0.1.0
