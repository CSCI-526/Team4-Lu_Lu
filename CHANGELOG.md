# Monkobra CHANGELOG

<!--
bug spider web fail to load & log warning
-->


## [Unreleased]

### Added

##### Yuqing Lu

- tutorial goal line `Climb high. Eat bananas. Escape the cobra.` above the keys in `Lv1Scene`, in regular-weight LiberationSans at size 40, the largest text on the panel
- white `Energy` label on the `StaminaBar` prefab, above the left end of the bar
- `LowStaminaPrompt`: the first low-stamina feedback per scene load shows `Low energy — eat bananas before you slow down!` for `showDurationS` (4 s), on the same event as the bar shake & camera cool-down; hidden at once on a loss, & while a web holds the monkey, resuming after the escape

##### Belle Dai

- keyboard-style tutorial controls in `Lv1Scene`: WASD cluster & wide Space key drawn from uGUI Images, w/ captions `Move`, `Grab Banana`, `Hold to reach, release to grab`
- `MonkobraTutorial-SemiBold` UI font (Noto Sans TC subset, SIL OFL) so the tutorial renders in WebGL w/o system fonts

##### Erik

- `CameraCoolDownEfx` on the main camera: each move attempt while stamina is below 10% pulses a URP Vignette blackdown and widens the field of view (zoom-out), easing both back; in two steps, lighter while stamina is low and stronger once the bar is empty. `Lv1Scene` gets a global Volume with a Vignette override
- `CameraEfxConfig` asset: the camera efx tuning in one place, i.e. the vignette intensity & zoom-out of both cool-down steps, ease times, and the retrigger cooldown, was fields on `CameraCoolDownEfx`; the hit shake stays on its impulse sources
- `CameraHitShakeEfx` on the main camera: a branch hit gives a light camera shake, a falling cobra hit a heavier one, each from its own `CinemachineImpulseSource`
- `HitBranchDetection.I` singleton, `Hit` event carrying a `HitKind`, and a `ReportHit(Collider, HitKind)` overload
- `StaminaBarController.lowStaminaFraction` (default 0.1): the stamina bar shake and the camera cool-down play on a move attempt below this share of max stamina, not only on an empty bar; it drives the efx only, an empty bar alone still slows the monkey, and it is not part of `GameBalanceConfig`
- grab arm tracks its banana while interact is held: it re-aims every physics step, turning at `MonkeyConfig.reachAimSpeedDeg` (deg/s, 0 snaps), was aimed once on the press; a banana switched off or destroyed mid-hold is dropped, so the release grabs nothing
- Great Cobra holds its height in the starter zone: it stays active and orbits but does not climb until Playing begins
- `StarterZone` game state between Tutorial and Playing: the run is live (climb, grab, score, lose) until `GameController.EndStarterZone()` is called, then Playing begins
- `StarterZoneExitRelay`: on a trigger collider, the monkey entering ends the starter zone
- `RampedDifficultyService` `rampBase` field & `RampBaseYPosition`: the ramped difficulty starts at 0 at the y of the starter zone exit trigger, falling back to the player's start y while unassigned

### Changed

##### Yuqing Lu

- tutorial keys & start prompt moved down 55 to make room for the goal line: `KeyboardControls` y 35 → -20, `Press` y -180 → -235

##### Erik

- the ramp curve is read above the starter zone exit trigger, not the player's start y; score & height display still measure from the start y
- branch & banana curves in `GameBalanceConfig` retuned to start at difficulty 0
- Great Cobra `startHeight` -5 → -10
- `StaminaBarController.ExhaustedMoveAttempted` → `LowStaminaMoveAttempted`, raised through the new `IsLowStamina` check
- `HitBranchDetection` only detects and reports hits; the camera shake moved to `CameraHitShakeEfx`, and the impulse source left the Monkey prefab
- the commit rule in `AGENTS.md` lets agents commit w/ Unity running once the user confirms

##### Wenhai Dong

- web builds lock to 720p: both build profiles (and the project default) use a 1280×720 canvas, was 1920×1080, which overflowed smaller laptop screens
- `Lv1Scene` Canvas Scaler: Scale With Screen Size at a 1280×720 reference (match 0.5), was Constant Pixel Size, so the UI keeps its size on Mac Retina, 4K, and Windows alike
- `Lv1Scene` UI resized and repositioned for 720p (the 1080p layout scaled by 2/3): score & height top-left, stamina bar bottom-left, tutorial, lose screen, and web prompt centered; the climb progress bar is now anchored to the right-middle edge instead of 484 px up from the bottom-right corner, so it no longer runs off short screens
- web builds use a new `FitWindow` WebGL template instead of Unity's `Default`: the game keeps 16:9, scales to fit any browser window with black bars, and renders at the screen's real pixel density, so it shows in full on small laptops and stays sharp on Retina & 4K; both build profiles and the project default use it

### Deprecated

### Removed

### Fixed

### Security

[unreleased]: https://github.com/CSCI-526/Team4-Lu_Lu/compare/v1.0.0-alpha...dev

## [1.0.0-alpha] - 2026-10-19

### Added

##### Belle Dai

- keyboard-style tutorial controls: WASD cluster & Space key w/ captions
- tutorial UI font w/ WebGL support, no system fonts needed

##### Erik

- camera cool-down effect: vignette & zoom-out pulse on move attempts at low stamina, tuned in `CameraEfxConfig`
- camera hit shake: light for branch hits, heavier for falling cobra
- starter zone: live run before Playing begins, ended by the monkey entering an exit trigger; Great Cobra holds height meanwhile
- ramped difficulty starts at 0 at the starter zone exit
- grab arm tracks its banana while interact is held

### Changed

##### Erik

- difficulty ramp & branch/banana curves start above the starter zone exit
- Great Cobra starts lower (-5 → -10)
- low stamina, not only empty, now triggers move-attempt feedback
- `Lv1Scene` split into prefabs, easing scene merges

##### Wenhai Dong

- web builds lock to 720p w/ scaling UI layout, fit any screen & density
- web builds use new `FitWindow` template: 16:9, scales to browser window, sharp on Retina & 4K

### Removed

##### Erik

- timer-based stamina drain & its `GameBalanceConfig` fields

### Fixed

##### Erik

- stamina bar dropping by a lump while moving, stunned, or after releasing a key

[1.0.0-alpha]: https://github.com/CSCI-526/Team4-Lu_Lu/compare/v0.9.0...v1.0.0-alpha













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

[0.9.0]: https://github.com/CSCI-526/Team4-Lu_Lu/compare/v0.1.1...v0.9.0

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

[0.1.1]: https://github.com/CSCI-526/Team4-Lu_Lu/compare/v0.1.0...v0.1.1












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

[0.1.0]: https://github.com/CSCI-526/Team4-Lu_Lu/releases/tag/v0.1.0
