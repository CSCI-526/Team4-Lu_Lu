# Monkobra CHANGELOG

<!--
FIXME camera adjust
FIXME game play tunning
-->


## [Unreleased]

### Added

##### Belle Dai

- slippery bark: wet patches on the trunk (`SlipperyBarkZone`, spawned by `SlipperyBarkSpawner`, one tutorial band) make the monkey slip & lose grip; wet grip slows the arm stroke (`ArmController`)

##### Erik

- `Lv1Scene` split into nested prefabs: `World`, `Envs`, `Entities`, `Systems`, `UI` and a leaf prefab per unit, so teammates edit different files
- Great Cobra exit boost: extra climb speed for a while after the monkey leaves the starting zone, w/ center warning "Run! The Great Cobra is chasing you!"; `cobraExitBoostSpeedU` & `cobraExitBoostDurationS` in `GameBalanceConfig`
- `MovementConfig` asset: climb speed builds up along 2 curves (up, down) of how long the key is held, instead of a fixed acceleration
- `startingZoneBranchClearRadiusU` in `GameBalanceConfig`: branch clearance round the monkey before the run is playing
- fruit fly: a grabbed fruit's sprite flies from its screen spot to a target (`FruitFlyEffect`, `FruitFlyConfig` asset): banana to the stamina bar, coconut to the icon of the coconut HUD, pineapple to the monkey; per-fruit target & sprite via `FruitFlyOnGrab`
- `Coconut` placeholder fruit prefab, banana-shaped, no score, not spawned yet
- pineapple: a branch may bear one instead of a banana (`pineappleSpawnProbabilityCurve` in `GameBalanceConfig`); grabbing it raises both hands & pulls the monkey up a set height, w/ hazards off for the leap & a short landing grace (`PineappleLeapController`, `GameController.IsLeaping`, `PineappleLeapConfig` asset)
- F2 dev key: spawns a pineapple branch straight above the monkey (`HackPineappleSpawner`)
- fruit UI sprites (banana, coconut, pineapple) in `Sprites/UI/`, shown in flights at 3x size (`sizePx` 192 in `FruitFlyConfig`); sources credited in `CREDITS.md`
- coconut throw: a double-tap throws one, the `UseCoconut` action of the Input Actions asset (MultiTap on Space, E & gamepad North), listened to by `CoconutManager`; a coconut model (`CoconutProjectile`) falls from the monkey at `coconutThrowSpeedU`, & the great cobra freezes on touching it or after `coconutThrowDistanceU`, not at the tap; works in the starting zone too
- look-down camera: `CameraLookDown` goes live for `coconutLookDownS` after a throw to follow the fall, under look-left/look-right while Interact is held (`CameraRigManager`)
- F3 dev key: spawns a branch bearing a coconut above the monkey (`HackAction.SpawnCoconut`, `HackPineappleSpawner`)
- consistent pop up UI: every center message (4 coconut, 2 cobra warnings, low stamina & web hints) is a variant prefab of `PopUp/_PopUpBase`, a semi-transparent plate behind the text, edited in the Editor; `PopUpManager` shows one at a time, a higher priority interrupts & the interrupted one resumes, run-only & once-per-scene rules, all dropped on a loss; the widgets only raise events into it (see `docs/pop-up-doc.md`)
- tree bark texture on trunk segments (`TreeBark` material on `TreeSegment`); source credited in `CREDITS.md`
- analytics metric channel: every run event is an `AnalyticsMetric` stamped through `AnalyticsManager.RecordEvent`, and the report renders each from one loop, so a new metric is an enum entry, a title row & one subscription (`AnalyticsMetric`, `RunData.GetEvents`)
- analytics records pineapple leaps, rough bark contacts, cobra freezes, pauses, boss fight starts, armor saves & boss revives; `BossFightController` raises `FightStarted`, `ArmorAbsorbed` & `Revived`, `MonkeyController` raises `RoughBarkTouched`
- analytics report prints a Starting Zone Exit line (time & height)
- Download Analytics button at the top right of the lose screen (`LoseScreenDownloadWidget`), development builds & the Editor only: it hides itself otherwise

##### Wenhai Dong

- tree bark is less glossy but stays crisp: `TreeBark` smoothness 0.5 → 0.25, environment reflections off; `tree_bark.png` trilinear with 8× anisotropic filtering, so the trunk seen at a steep angle is not blurred
- Cobra King boss fight (`devWenhaiBossFightScene`, `Boss/BossFight` prefab): at `bossTriggerHeightU` the run enters a boss arena; the king, built from simple primitives, coils round the tree top and alternates rests with attack rounds (bite & tail sweep, fatal & warned; minion waves down vertical danger strips, sweeping across w/ one gap, fatal, called once a game w/ "Minions, help me!"; head butt, a knock-down) that grow with each lost third of its health; real banana branches in the arena, more and growing back faster later in the fight; every grabbed banana still feeds the monkey and is also thrown in a visible arc at the crown; spider webs in the arena, shaken off in fewer presses there (`SpiderWebStruggle.EscapeGainScale`); once the king is first hit, a golden banana (the banana model, gold w/ a thick outline) shows up now & then beside the monkey, refills stamina & shields one fatal hit; the monkey's climb is capped below the king, who butts down a monkey at the cap; the great cobra guards below without climbing, still circling, and now & then pokes its head straight up as a threat; attack warnings are filling danger zones with a "!" sign (BITE!, TAIL!, LOOK UP!); fruit grows on branch stubs; the shield is gold armor; the camera pulls back and tilts up to keep the king in view; first-throw keycap prompt & dotted line, speech, health bar; the last blow either way plays in slow motion (a K.O. banner & the monkey spun off the tree on a loss); the speech bubble stays on screen; at zero health the king shatters into cubes, its crown lands on the monkey and the run is won; Boss Fight group in `GameBalanceConfig`; `CobraMotor.StartGuard` (guard & peek); `MonkeyController.SetHeightCeiling` & `ClearHeightCeiling`; `ArmController` hands arena fruit to `BossFightController.TryThrow`; `CameraLookUp.ExtraAimOffsetU`; the fight starts in the gold armor under a short "You get a Golden Armor!" banner low on screen, kept until the first throw; every boss fight loss, the guarding great cobra's catch included, offers a revive for watching a mock USC CSCI 526 ad, which restarts the fight, and after a revive the first throw picks EASY (plain banana: less health, longer warnings & rests, one minion wave) or NORMAL (golden banana); a win no longer ends the run: with a swirl-in animation and a GOLDEN ARMOR badge the monkey keeps a Golden Armor that takes one great cobra catch (`CobraSensor` → `BossFightController.TryAbsorbCobraCatch`, `CobraMotor.KnockBack`), and the hazards and the chase resume (`CobraMotor.StopGuard`) after a head start with the cobra set further down; the boss fight starts at 500 u, well past every basic hazard and fruit; no pineapples or coconuts in the arena; normal mode rests a little shorter and in its last third minion waves stream on (3.5 s apart) alongside one bite or sweep per round, at most two attacks at once and overlapping only briefly (`finaleOverlapS`), until the fight ends; the arena opens with 3 webs instead of 4; 2 small rough bark patches slow the monkey in the arena, off its start spot and the banana branches; the king's lines dock under its health bar when its head is out of view; the mock ad has no countdown and stays until skipped, its Skip button showing up after 3 s; a pineapple leap stops at the arena's start height instead of flying past it (`PineappleLeapController` reads `BossFightController.LeapTopY`); from the K.O. until a revive lands the monkey is untouchable and let out of any web (`SpiderWebStruggle.ForceRelease`), so no web prompt or cobra save fires on the way; the armor's save throws the cobra 20 u down and freezes it 15 s, more than a coconut; a revive lands the monkey back at the fight's start spot (`MonkeyController.SyncOrbitAngle`).
- `GameState.Won` & `GameController.WinGame()`: a won run settles the score, freezes, and restarts on the lose screen's restart key; analytics records it as `RunStatus.Won`
- pause: Esc or P pauses or resumes a live run (starting zone or playing), and R restarts it from the pause; time freezes, audio pauses, and a `PauseScreen` panel ("Paused", "Press Esc or P to Resume", "Press R to Restart") covers the screen; `GameController.IsPaused`, `SetPaused`, and `PauseChanged`, with the panel toggled by `PauseScreenWidget`

##### Yuqing Lu

- coconut: a rare fruit from Height 150 (`coconutStartHeightU`, `coconutSpawnChancePerSegment` in `GameBalanceConfig`, at most one per segment) hung on a bare branch's stem tip, clear of other branches, fruit & webs (`CoconutSpawner`, `BranchController.TryHangFruit`); grabbed like a banana, it scores `scoreRewardPoints` x `coconutScoreMultiplier` & restores no stamina
- coconut use: a quick double-tap spends one to freeze the great cobra for `cobraFreezeDurationS` (`CobraMotor.TryFreeze`); refused while paused, trapped in a web or just out of one, & while already frozen; the cobra turns light blue while frozen (`CobraFreezeTint`)
- coconut HUD: count, freeze countdown & bar, a "Double-tap Space" hint until the first use of the session, & center messages
- coconut analytics: pickups, uses & coconut score per run in the session report
- `CoconutSpawner` test override for coconuts from Height 5 on every segment
- whole green coconut model `SM_Fruits_Coconut_1` from the PolyOne Free Fruits pack, & a matching UI sprite rendered from it (`coconut-green-256x256`, credited in `CREDITS.md`)
- trunk height markers: every 50 u of climbed height a number beside the trunk in world units labelled m (e.g. `150 m`), white bold on a dark rounded plate like the progress bar readout, kept on the trunk's right edge as seen from the camera so the trunk never hides it; a marker grows & its plate brightens the first time the monkey passes it (`HeightMarkerSystem` on `Envs/HeightMarkerSystem`, test override for a 5 u interval)

##### Houdong Pan

- lose screen leaderboard: simulated rivals, fixed per launch, ranked against the final score (`SessionLeaderboard`, `LeaderboardWidget`, `LeaderboardRowWidget`; `Leaderboard` & `LeaderboardRow` prefabs nested in `LoseScreen`)
- fruit grab prompt: keycap "space, hold, then release" teaches the first grab of each fruit kind per run (`FruitGrabPrompt`, `KeyPromptUI`); `ArmController.FruitGrabbed` event

### Changed

##### Belle Dai

- slippery bark → avoidable rough terrain after 3000 points: `SlipperyBark*` renamed `RoughBark*` (`RoughBarkZone`, `RoughBarkSpawner`, `RoughBarkSystem`), new bark textures & spawn settings in `GameBalanceConfig`

##### Erik

- starting zone: level opens at 30% stamina, exhaustion slows less (`StartingZoneStartStaminaFraction`, `StartingZoneExhaustedSpeedMultiplier`); exit trigger raised to y 60 & balance curve retuned
- pause (Esc, P), restart after lose (R, Space) & restart during pause (R) are `InputSystem_Actions` actions read by `GameController`; `InGameHUD` & `PopUps` prefabs split out of `_UI`
- idle stamina recovery runs at a quarter rate during the starting zone (`startingZoneIdleRecoveryMultiplier` in `PlayerBalanceConfig`)
- `exhaustedSpeedMultiplier` & `idleStaminaRecoveryPerS` moved from `GameBalanceConfig` to `MovementConfig`
- Starter Zone → Starting Zone: `GameState.StartingZone`, `EndStartingZone()`, `StartingZoneExitRelay`, `StartingZoneExitTrigger` prefab; Tutorial state & screen unchanged
- fruit grab aims at banana closest to monkey, not last to enter a detection zone; re-picked every physics step
- scripts renamed to house role suffixes (`Controller`, `Sensor`, `Motor`, `Widget`, `Relay`, `Feedback`, ~), e.g. `ArmRoot` to `ArmController`, `HitBranchDetection` to `HitBranchSensor`, `RampedDifficultyService` to `RampedDifficultyManager`; no behavior change
- code style pass over all scripts: same-line braces, MonoBehaviour section order with banners, Inspector guards, explicit types instead of `var`, plain logs gated by `isDebugBuild`
- `World` and `Systems` wrapper prefabs dropped: the light, volume and event system are root prefabs, and `ScreensManager`, `ScoreManager`, `RampedDifficultyManager` are components of the `GameController` prefab
- `AnalyticsManager` is now a component of the `GameController` prefab and no longer survives a scene reload, so each load starts a new session file
- slippery bark scripts follow the Unity MonoBehaviour conventions: tooltips, assignment guard, section banners, 80 columns
- Global Volume profile moved from `Scenes/Lv1Scene/` to `Settings/`
- stamina restore & bar punch land when the grabbed fruit's flight arrives, not at the grab; score still counts at the grab
- `InGameHUD` now nested in `_UI`, holding `ProgressBar`, `StaminaBar`, `ScoreHeight` & the pause hint; no visual change
- `CoconutManager` on the `GameController` prefab centralizes the coconut count, the throw & the HUD switching; the analytics report reads coconut pickups & uses from it
- `TopRightAnchor` and the coconut `Panel` removed: `CoconutHUD` sits straight under `InGameHUD`; the freeze countdown & bar split out of `CoconutWidget` into `FreezeTimerWidget`, the center prompts into `CoconutMessageWidget`
- banana model: the 3 stretched capsules of `BranchWithFruit` replaced by `SM_Fruits_Banana_Y1` from the PolyOne Free Fruits pack (child `BananaModel` of `Banana`); `BananaReachable` & `BananaGrabbable` outline widths thinned to fit the mesh
- config reorganization: `GameBalanceConfig` split by concern into `PlayerBalanceConfig`, `WorldBalanceConfig` & `CobraBalanceConfig`; `MonkeyConfig` into `MonkeyArmConfig` & `MonkeyInputConfig`; `MovementConfig` → `MonkeyClimbConfig`, now also holding orbit speed & deceleration; `PineappleLeapConfig` takes the hand rise time & landing grace; every tuned value carried over unchanged
- `Settings/` is `_Shared/` plus one folder per module (`Player/`, `Environment/`, `Mob/`, `Cameras/`, `UI/`, ~), assets directly inside

##### Yuqing Lu

- `Coconut` prefab: from banana-shaped placeholder to the coconut item: whole coconut model under a top pivot, `ScorePickup` of kind `Coconut`, `CoconutReachable` & `CoconutGrabbable` outlines, green flight sprite
- `ScoreManager` scores & counts each pickup by its `PickupKind`; `ScorePickup` gains a kind, banana by default
- `ArmController` gives a coconut's flight no stamina restore
- analytics report splits the reward score into banana & coconut score
- progress bar cobra gap in world units labelled m, matching the trunk height markers, no longer converted by `unitsPerMeter` (`ProgressBarWidget`); `unitsPerMeter` now only sets the distance score rate

##### Houdong Pan

- progress bar redrawn from generated antialiased face sprites w/ an upright readout (`ProgressBarWidget`, `ProgressBar` prefab)
- boss fight key prompt reuses `KeyPromptUI`, the keycap of the fruit tutorial (`BossFightHud`)

### Deprecated

### Removed

##### Belle Dai

- falling cobras removed on hit

##### Erik

- `devYuqingCoconutScene` personal dev scene, the coconut work lives in `Lv1Scene`
- `SlipTestScene` dev scene, after slippery bark landed in `Lv1Scene`
- `upSpeedU` & `downSpeedU` (`GameBalanceConfig`), replaced by the `MovementConfig` curves; `accelerationU` (`MonkeyConfig`)
- orphan `GoalText` object in `Lv1Scene` (never drawn, no parent Canvas) and its mention in the TDD

##### Yuqing Lu

- HUD Height label (`HeightDisplay` prefab, `HeightDisplayWidget`): the trunk height markers show the climbed height

### Fixed

##### Erik

- restart could spawn monkey inside a tree branch: no branch grows w/i `startingZoneBranchClearRadiusU` of monkey before the run is playing
- analytics dropped starting zone data: a run opened only when the zone ended, so zone bananas, webs, branch hits, height samples & the run time were lost; it now opens when the starting zone begins

##### Yuqing Lu

- coconuts never spawned in `Lv1Scene`: the `CoconutSystem` prefab (`CoconutSpawner`) was missing under `Envs`

### Security

[unreleased]: https://github.com/kami-lel/monkobra/compare/v1.0.0-alpha.2...dev

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
