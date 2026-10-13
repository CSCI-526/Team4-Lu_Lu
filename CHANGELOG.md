# Monkobra CHANGELOG

<!-- fixme: `Lv1Scene` ignores monitor aspect ratio: the camera is orthographic at a fixed size 10, so a wider monitor sees more world sideways & hazards sooner, & the Canvas Scaler is Constant Pixel Size, so UI does not follow resolution -->

## [Unreleased]

### Added

##### Erik

- `DTSPool`: recycles the lowest tree segment over the top as the monkey climbs, for an endless tree; deactivates an optional mock tree at runtime
- spider web spawn in `GameBalanceConfig`'s Spider Web group: an attempt count per segment and a probability curve of the ramped difficulty, the same model as branches
- falling cobra spawn in `GameBalanceConfig`'s Falling Cobra group: an attempt count per segment, a probability curve of the ramped difficulty, a constant fall speed, and a horizontal chase speed
- analytics run entries end with Mermaid graphs: a score split pie, a height vs time line, and a stamina vs time line, the last two drawn from the same timed samples
- analytics session files open with a Session Summary of lost runs: a death causes pie, then bar charts of final score, highest height, death height, and duration vs run; the file is rewritten on each save instead of appended to
- analytics run entries record the death cause and the death height, apart from the highest height reached
- analytics samples height & stamina together every `sampleIntervalS` seconds (default 2) while a run plays, plus at run start & end
- analytics saves the session file on session open, run start, banana, web, branch hit, and game over, not only on game over, so a run in progress is on disk
- analytics also saves every `autosaveIntervalS` seconds (default 10, 0 turns off) while a run plays; any save restarts the countdown
- analytics marks a run cut short by stopping Play mode, a scene reload, or F9 as `Interrupted`, with all its graphs, and leaves it out of the session summary
- analytics infers a lost run's death cause from its logged events: the latest branch hit or web encounter within `deathCauseWindowS` (default 2 s) before the loss, else the great cobra
- `AnalyticsConfig` asset (`Settings/Analytics/`): analytics keys, sample & autosave intervals, and the death cause window
- F10 saves a fully mocked session file (`MockAnalyticsGenerator`) beside the real ones, for checking the report without playing
- `StaminaBarController.MaxStamina`: the balance config's stamina ceiling
- `SpiderWebTrap.Encountered`: static event raised once per web, on the monkey's first body contact, trapped or not

##### Yuqing Lu

- Spider web trap in `Lv1Scene`, also in the `WebDemo` test scene: webs spawn on the upper trunk clear of branches and fruit, hold the monkey on contact, and break on rapid Space presses, with a first-trap hint and an escape bar that hide on escape or game over, and a short immunity after escaping
- `SpiderWebSpawner` test override: webs at a flat chance at any height, for quick checks
- `DynamicTreeSegmentPrefabRoot.BranchesGenerated`: static event raised after a segment rolls its branches, on first `Start` and every `Restart`
- `RampedDifficultyService.GetClimbedYDistance`: height of any y above the player's start y

##### Belle Dai

- falling cobra prototype: `FallingCobra` warns, then drops on the monkey, and `FallingCobraSpawner` places it; tried in the `FallingCobraDemo` test scene
- falling cobra spawner in `Lv1Scene`, on the active `DTSPool` object, using the pool's transform as the tree center

##### Houdong Pan

- scoring: `ScoreManager` adds a distance score from the highest climb to a reward score per banana, and stops once the run ends
- `ScorePickup` on a fruit hands its points over once, `ScoreReward` & `ScoreConfig` carry the point values, banana & coconut score assets
- `ScoreDisplay` HUD shows the live score in `Lv1Scene`

##### Wenhai Dong

- great cobra base speed rises with the ramped difficulty, from `cobraBaseSpeedCurve` in `GameBalanceConfig`'s new Great Cobra group (3.4 u/s at the start, 3.9 mid climb, 4.0 at the top)
- great cobra mistake surge: each branch or falling cobra hit (once per stun) and every web trap makes `CobraClimb` speed up for a while; repeated mistakes stack, faster and longer, up to past the monkey's up speed (`cobraSurgePerMistakeS`, `cobraMaxSurgeS`, `cobraSurgeSpeedPerMistakeU`, `cobraMaxSurgeSpeedU`, `cobraSpeedChangeRateU`)
- surge cue: on `CobraClimb.CatchUpStarted`, `ProgressBarRoot` shows "The cobra is speeding up!" (×N when stacked) at the screen center, sized to the screen height so it reads on 4K, for a moment, and flashes the cobra marker red while the surge lasts
- falling cobra reach wake: a dormant falling cobra wakes from farther away (`fallingCobraReachWakeDistanceU`) while the monkey reaches for a banana, combining the two hazards
- falling cobra banana ambush: `FallingCobraSpawner` hangs some cobras (`fallingCobraBananaAmbushChance`) just above a banana of their segment
- falling cobra re-hit: `fallingCobraRehitGraceS` & `fallingCobraMaxHits` tune how soon and how often one snake can drop the monkey again
- `HitBranchDetection.ReportHit`: applies a branch-hit drop from a hazard that touched the monkey outside the sensor
- game balancing: Great Cobra group in `GameBalanceConfig` & a playtest-driven tuning pass over every hazard curve, so one run lasts 7–8 min with the cobra pressing harder as the climb goes on (details under Changed)

### Changed

##### Erik

- analytics split `AnalyticsManager` into data containers `RunData` & `SessionData`, a static `AnalyticsMarkdownWriter`, and a lean manager that observes, fills, and saves
- `GameController.LoseGame()` takes no cause and `GameController` keeps no `LastDeathCause`; the `DeathCause` enum moved into `Analytics/`
- analytics run entries record the highest height reached as a float `HighestHeight`, not an integer final height
- analytics records every web encounter through `SpiderWebTrap.Encountered`, replacing a lookup of the one `SpiderWebStruggle` in the scene
- `DynamicTreeSegmentPrefabRoot.Restart` moves a segment and re-rolls its branches, clearing the old branches and webs
- `MonkeyPrefabRoot` exposes `IsHeld` to freeze the body in place
- falling cobras are rolled per tree segment like branches: `FallingCobraSpawner` listens to `BranchesGenerated`, hangs dormant cobras as segment children on a ring about the trunk, and no longer runs a global timer, a 60 u unlock, or a one-at-a-time limit
- `FallingCobra` wakes when the monkey is within a trigger distance, falls at a constant speed while sliding round the trunk toward the monkey's live angle, and switches itself off after a fall limit
- spider webs spawn from an attempt count and a ramped difficulty probability curve; the start climb, full-chance climb, max chance, and slot count are gone
- `docs/work-with-git.md` asks for a short PR description of what the branch did, since it feeds each member's Weekly Progress

##### Yuqing Lu

- `SpiderWebSpawner` places webs on each `BranchesGenerated` instead of a one-off pass at `Start`, measures height from the player's start y instead of `TreeManager` progress, and drops a segment's old webs before rolling it again; `startProgress`, `maxWebsPerSegment`, and `spawnChance` are gone
- `DTSPool` no longer references or calls `SpiderWebSpawner`, and warns when one sits under its mock tree

##### Wenhai Dong

- cobra coil fill: `CobraClimb` clones body segments so the coil has no gap
- cobra look: bead-chain body w/ small gaps & slight taper, flat hooded head w/ eyes, raised neck, slow slither wave
- cobra body coils ≥ 1 full loop round the trunk as a corkscrew, so dropping onto it or orbiting into it is always fatal
- cobra coil shape tuned by `coilTurns`, `coilPitchU`, `maxSegmentGapU`, replacing per-segment angle & height spacing
- `Cobra/` scripts → house Unity style
- cobra body thickness: `maxThicknessU` & a `thicknessProfile` curve along the body, tuned in `Lv1Scene`
- balance retuned in `GameBalanceConfig.asset` for a 7–8 min run: ramp reaches max at 1200 u (was 500); more branches (0.3 → 0.6 chance); steady bananas, about 0.42 per segment at the base, a slight dip in the web-only stretch, 0.72 mid climb, about 1 at the top; webs from difficulty 0.05 with 3 attempts, 0.4 by 0.25, 0.6 at the top; falling cobras from difficulty 0.15, 0.4 at the top, sliding at 4 u/s; idle stamina recovery 2 → 2.5 per s; banana restore 30 → 35
- balance pass on bananas: sparse early, a slight dip in the web-only stretch, then rising so the stamina budget holds for the whole climb, with the cobra, branch, web, and falling cobra curves tuned against it
- `Lv1Scene` climb progress bar shows 60 u below the monkey (was 15), so the cobra marker leaves the bottom
- `SpiderWeb` prefab: web and its trap box are 20% larger (root scale 1.5 → 1.8 on the web face)

##### Belle Dai

- falling cobra contact in `FallingCobraDemo` now uses the monkey's branch-hit stun and drop instead of ending the run

### Deprecated

### Removed

### Fixed

##### Yuqing Lu

- spider webs missing from `Lv1Scene` since the endless tree: `SpiderWebSpawner` sat on `Envs/Tree`, the mock tree `DTSPool` deactivates at runtime, so it never ran

##### Wenhai Dong

- cobra contact script class renamed `CobraCollisions` to match its file, so Unity can load it
- cobra contact counts the monkey's tail & hands, not only its body; detection triggers no longer matter
- cobra no longer retreats downward with a descending monkey, which let the monkey push it down and never touch it
- cobra contact also checked while overlapping, not only on entry
- monkey body collider height 1 → 2 to match its visible body, so contact registers from the monkey's bottom, not its middle (also reaches win zone & other triggers ~0.5 U sooner)
- cobra no longer parks at the player's height waiting for its head to come round; the coil climbs on into the player, so body contact kills at once
- falling cobra drops the monkey on contact with any part of it, not only the branch sensor; a body or tail contact used to switch the hit off without any drop
- falling cobra no longer pushes a monkey down several times in a row: after its first hit it stops homing in and its hit shape rests for the stun plus 1 s so the monkey can dodge sideways; a monkey still inside after that is dropped again, at most 2 drops per snake
- webs make the great cobra surge on every trap, not only the first trap of the scene

##### Belle Dai

- falling cobra warning starts red at once and pulses from that moment, instead of from the global clock
- falling cobra spawner reads an assigned `treeCenter` instead of `TreeManager`, so it works on the endless tree

### Security

[unreleased]: https://github.com/CSCI-526/Team4-Lu_Lu/compare/v0.1.1...dev












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
