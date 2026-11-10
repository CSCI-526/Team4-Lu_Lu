# Monkobra Technical Design Document

How Monkobra is built: the engine and packages, the run lifecycle, the systems that do not belong to one subsystem document, the conventions every script follows, and where to find the rest. Design intent is in the [Game Design Document](monkobra-gdd.md), controls and setup in the [README](../README.md), and the agent-facing system map in [CONTEXT.md](../CONTEXT.md).

## Document Map

| Document | Owns |
| --- | --- |
| [Game Design Document](monkobra-gdd.md) | what the player does and why: pillars, loop, mechanics, difficulty |
| This document | engine, architecture, run lifecycle, game balance, monkey, tree, stamina, score, UI, input, conventions |
| [Mobs](mob-doc.md) | cobra, falling cobra, branches, spider webs, and what each does when the monkey touches it |
| [Grab System](grab-doc.md) | detection zones, arm reach, release check, camera swap, outline feedback |
| [Build](build-doc.md) | the WebGL export, the three builds (WebGL for GitHub Pages, WebGL for itch.io, Windows) and their settings |
| [AGENTS.md](../AGENTS.md) | rules for agents |
| [CONTEXT.md](../CONTEXT.md) | descriptive system map and known gaps |
| [PopUps](pop-up-doc.md) | the center messages: one plate-and-text look, priority and queue, gating, how to add one |
| [Balance Design](balance-doc.md) | why difficulty follows height, the per-segment roll model, the single balance asset |
| [CHANGELOG.md](../CHANGELOG.md) | version history |

A system gets its own document when it spans several scripts and carries rules a reader cannot see from one file. Everything else stays here.

## Stack

| Aspect | Value |
| --- | --- |
| Engine | Unity 6 (`6000.3.22f1`) |
| Render Pipeline | URP 17.3.0, with a PC and a Mobile asset under `Settings/_Shared/` |
| Input | Input System 1.20.0 |
| Camera | Cinemachine 3.1.7 (view swaps, impulse shake) |
| UI | uGUI 2.0.0 with TextMesh Pro |
| Target | WebGL on GitHub Pages, built from the Editor |
| Tests | none yet, `com.unity.test-framework` is installed |

## Scenes

| Scene | Purpose |
| --- | --- |
| `Lv1Scene` | the game and the only scene; its root is 7 prefab instances (`Cameras`, `DirectionalLight`, `GlobalVolume`, `EventSystem`, `Entities`, `GameController`, `UI`) plus two plain objects, `Envs` and a Cinemachine `Target Group`; `DynamicTreeSegmentPool` also holds the spider web and falling cobra spawners |

Personal copies of `Lv1Scene` are allowed for experiments, see [AGENTS.md](../AGENTS.md#git-workflow).

The folder layout and a script-by-script index are in [CONTEXT.md](../CONTEXT.md#repository-layout).

## Architecture

```mermaid
graph LR
  Input[Input System] --> Monkey[Monkey: movement, arms]
  Monkey --> Grab[Grab System]
  Grab --> Stamina[StaminaBarController]
  Grab --> Score[ScoreManager]
  Score -->|coconut| Coconut[CoconutManager]
  Coconut -->|freeze| Mobs
  Mobs[Mobs] -->|touch| Monkey
  Mobs -->|cobra contact| Game[GameController]
  Stamina -->|empty| Monkey
  Mobs -->|falling cobra, web| Monkey
  Game -->|state| Analytics[AnalyticsManager]
  Game --> Screens[ScreensManager]
  Game -->|EndRun| Score
  Config[ScriptableObject configs] --> Monkey
  Config --> Stamina
  Config --> Score
```

Subsystems talk through scene singletons, C# events, and trigger callbacks, not through direct scene references. Details are in [Conventions](#conventions).

## Run Lifecycle

`GameController` is the scene singleton that owns the `GameState` machine, exposed as `State` with a `StateChanged` event. A run ends in a loss, or in a win by defeating the Cobra King.

```mermaid
stateDiagram-v2
  [*] --> Tutorial: scene load
  Tutorial --> StartingZone: any button
  StartingZone --> Playing: EndStartingZone
  Playing --> Lost: LoseGame
  Lost --> [*]: interact, reload scene
  Playing --> Won: WinGame, Cobra King defeated
  Won --> [*]: interact, reload scene
```

| State | Time Scale | Screen |
| --- | --- | --- |
| Tutorial | 0 | tutorial panel, any button starts the run |
| StartingZone | 1 | none, run is live, `LoseGame` works, until `EndStartingZone()`, called by `StartingZoneExitRelay` when the monkey enters its trigger |
| Playing | 1 | none, never paused |
| Lost | 0 | lose panel, `RestartAfterLose` (R or Space) reloads the scene after `restartInputDelay` |
| Won | 0 | a won run, time frozen, the same restart key reloads the scene |

`LoseGame()` runs once, from StartingZone or Playing: the cobra touches the monkey ([Mobs](mob-doc.md#contact)). It carries no cause: analytics infers one itself (`DeathCauseResolver`): a branch hit or web encounter w/i `AnalyticsConfig.DeathCauseWindowS` before the loss names the cause, otherwise it is the great cobra. `LoseGame` calls `ScoreManager.EndRun` so the score settles, then enters Lost. `ScreensManager` owns the panels: they start deactivated, and it shows the current state's panel in `Start` and on every `StateChanged`.

`WinGame()` currently has no caller: a Cobra King win now continues the run with an armor reward ([Mobs](mob-doc.md#win-reward)). When called it runs once, from StartingZone or Playing: like `LoseGame` it settles the score, then enters Won, which freezes time, and the lose screen's restart key reloads the scene. The boss fight's own win stops time on a reward panel instead, without entering Won. Analytics closes the run as `RunStatus.Won`, left out of the death summaries.

Pause is a flag, not a state: `GameController.SetPaused` (Esc or P) works only while the run is live, sets `IsPaused`, freezes time and audio, and raises `PauseChanged`, while `State` keeps its value, so resuming is no state change and analytics never sees a new run. R on the pause screen (`RestartDuringPause`) reloads the scene for a fresh run, like the lose screen's restart (`RestartAfterLose`, R or Space). Any state change ends a pause; the key that starts the run from the tutorial is ignored for the pause that frame. Gameplay already stops on `Time.timeScale <= 0`, so nothing else checks the flag.

A pineapple leap is a flag too: `GameController.SetLeaping` sets `IsLeaping` and raises `LeapChanged` while `State` keeps its value, for the same reason, and because the checks of `State == Playing` below must keep passing. It starts only in a live run and ends the starting zone. Hazards check the flag themselves ([Mobs](mob-doc.md#during-a-pineapple-leap)); the sequence is in the [Grab System](grab-doc.md#pineapple-leap).

Gameplay that must stop outside a run checks both `Time.timeScale <= 0` and the state. Grab, pickup, and scoring (`ArmController.TryGrabFruit`, `ScorePickup`, `ScoreManager.CanScore`) check `GameController.IsRunLive` (StartingZone or Playing); `SpiderWebStruggle` and the other HUD checkers still require `Playing`, so webs do not catch in the starting zone.

## Analytics

`AnalyticsManager` is a singleton component on the `GameController` prefab, not kept across reloads: a scene load starts a new session and file, and a reload in the middle of a run leaves that run unclosed at its last autosave. It observes the run (`GameController.StateChanged`, `ScoreManager.BananaCollected`, `HitBranchSensor.BranchHit`, `SpiderWebTrap.Encountered`, ~), opening a run when the starting zone begins so the zone's fruit, webs, and height count, and fills a `SessionData` of `RunData` entries. Every event is an `AnalyticsMetric` stamped through one `RecordEvent` path, so a new metric is an enum entry, a title row, and one subscription, see [Analytics](analytics-doc.md#adding-a-metric). `AnalyticsMarkdownWriter` renders every metric from one loop and `AnalyticsMarkdownWriter` renders it to one markdown file per scene load under `persistentDataPath/Analytics/`, rewritten on every save. Each run entry carries Mermaid graphs, and the file opens with a summary of lost runs. A run cut short by stopping Play mode, a scene reload, or the restart key is marked `Interrupted` and left out of the summary. Sample and autosave intervals and the death cause window are in `Settings/Analytics/AnalyticsConfig.asset`; the keys are in [Hack Shortcuts](#hack-shortcuts).

A WebGL player cannot reach `persistentDataPath` (it is a virtual IndexedDB filesystem), so the download key renders the session in memory and `WebDownload` hands it to the browser through `Plugins/WebGL/Download.jslib`. A run still open is rendered as it stands and keeps recording. Elsewhere the same key saves the session file and logs its path.

## Hack Shortcuts

Every dev key lives in one asset, `Settings/HackShortcuts/HackShortcutConfig.asset`, an instance of the `HackShortcutConfig` ScriptableObject: a list of entries, each a `HackAction`, a key, and a one-line description. Consumers look a key up with `GetKey(HackAction)` and skip it unless `HackShortcutConfig.IsActive`, ie a development build or the Editor, so none of them work in a release build.

| Key | Action | Read by |
| --- | --- | --- |
| F1 | toggle `GameController.CobraNoClip` | `GameController` |
| F2 | spawn a pineapple branch straight above the monkey | `HackPineappleSpawner` |
| F3 | spawn a coconut branch straight above the monkey | `HackPineappleSpawner` |
| F8 | download the analytics session report | `AnalyticsManager` |
| F9 | close the analytics session and open a new file | `AnalyticsManager` |
| F10 | save a mock analytics session file | `AnalyticsManager` |

`HackShortcutPanel` on a TMP text writes these lines from the asset in `Awake`, and hides its object outside a development build. A new hack key is a new `HackAction` plus an entry in the asset.

## Game Balance

Game balance numbers live in 3 assets, split by what they tune (how height becomes spawn chances is in [Balance Design](balance-doc.md)): `Settings/Player/PlayerBalanceConfig.asset` for the player's run, `Settings/Environment/WorldBalanceConfig.asset` for the generated world, and `Settings/MobGreatCobra/CobraBalanceConfig.asset` for both cobras, instances of the `PlayerBalanceConfig`, `WorldBalanceConfig`, and `CobraBalanceConfig` ScriptableObjects. To tweak the game's feel, edit these 3 files and nothing else: no balance value is held in a fourth asset or hard-coded in a script.

| Asset | Group | Fields | Read by |
| --- | --- | --- | --- |
| `PlayerBalanceConfig` | Stamina | `maxStamina`, `staminaUpwardDrainPerU`, `staminaSidewayDrainPerU`, `fruitStaminaRestore` | `StaminaBarController` |
| 〃 | Starting Zone | `startingZoneStartStaminaFraction`, `startingZoneExhaustedSpeedMultiplier`, `startingZoneIdleRecoveryMultiplier` | `StaminaBarController`, `MonkeyController` |
| 〃 | Branch Hit | `hitStunDurationS`, `hitDropDistanceU`, `hitDropSpeedU` | `MonkeyController`, `CobraMotor` |
| 〃 | Score | `pointsPerMeter`, `unitsPerMeter`, `scoreRewardPoints` | `ScoreManager` |
| 〃 | Coconut | `coconutStartHeightU`, `coconutSpawnChancePerSegment`, `coconutScoreMultiplier` (`CoconutRewardPoints`), `cobraFreezeDurationS`, `coconutThrowSpeedU`, `coconutThrowDistanceU` | `CoconutSpawner`, `ScoreManager`, `CoconutManager` |
| `WorldBalanceConfig` | Ramped Difficulty | `rampedDifficultyCurve` | `RampedDifficultyManager` |
| 〃 | Branch | `branchGenerationAttemptCount`, `startingZoneBranchClearRadiusU`, `startingZoneBranchChance`, `startingZoneBananaChance`, `branchSpawnProbabilityCurve`, `bananaSpawnProbabilityCurve`, `pineappleSpawnProbabilityCurve` | `TreeSegmentController` |
| 〃 | Rough Bark | `roughBarkStartScore`, `roughBarkSpeedMultiplier`, `roughBarkBlendS`, `roughBarkAttemptCount`, `roughBarkSpawnProbabilityCurve` | `RoughBarkSpawner`, `MonkeyController` |
| 〃 | Spider Web | `webAttemptCount`, `webSpawnProbabilityCurve` | `SpiderWebSpawner` |
| 〃 | Boss Fight | `bossTriggerHeightU`, `bossMaxHealth`, `bossBananaDamage` | `BossFightController` |
| `CobraBalanceConfig` | Falling Cobra | `fallingCobraAttemptCount`, `fallingCobraSpawnProbabilityCurve`, `fallingCobraFallSpeedU`, `fallingCobraHorizontalSpeedU`, `fallingCobraReachWakeDistanceU`, `fallingCobraBananaAmbushChance`, `fallingCobraRehitGraceS`, `fallingCobraMaxHits` | `FallingCobraSpawner`, `FallingCobraController` |
| 〃 | Great Cobra | `cobraBaseSpeedCurve`, `cobraSurgePerMistakeS`, `cobraMaxSurgeS`, `cobraSurgeSpeedPerMistakeU`, `cobraMaxSurgeSpeedU`, `cobraExitBoostSpeedU`, `cobraExitBoostDurationS`, `cobraSpeedChangeRateU` | `CobraMotor` |

- Wiring: each reader holds an inspector field for every slice it needs, never more: `playerBalanceConfig` on `StaminaBarController`, `ScoreManager`, `MonkeyController`, `CoconutSpawner`, `CoconutManager`, and `CobraMotor` (hit stun only); `worldBalanceConfig` on `MonkeyController` (rough bark), `TreeSegmentController`, `RampedDifficultyManager`, `SpiderWebSpawner`, `RoughBarkSpawner`, and `BossFightController` (boss fight); `cobraBalanceConfig` on `FallingCobraSpawner` and `CobraMotor`, each pointing at the one asset of its slice
- Not Balance: the arm stroke and reach are in `MonkeyArmConfig`; climb build-up, exhaustion, idle recovery, orbit speed, and deceleration are in `MonkeyClimbConfig`; input actions are in `MonkeyInputConfig`; the leap's distance, speed curve, hand rise, and landing grace are in `PineappleLeapConfig`; branch radius, spacing, and retries stay in `BranchFruitPlacementConfig`; web radius, spacing, clearance, retries, and the test override stay on `SpiderWebSpawner`; cobra spawn radius, spacing, retries, and ambush height stay on `FallingCobraSpawner`, and its trigger and fall distances on the falling cobra prefab; coconut clearance and the test override stay on `CoconutSpawner`, and the web escape grace and the throw start offset on `CoconutManager`; the height markers' interval, placement, look, and test override stay on `HeightMarkerSystem`
- Retired: `GameConfig`, `ScoreConfig`, and `ScoreReward` were folded into the balance configs and removed
- Suffixes: `U` is world units, `S` is seconds, `Deg` is degrees, `PerU` is per world unit travelled

The balance values in each section below are the current ones; change them in the asset, not in the section.

## Monkey Movement

`MonkeyController` drives the body. Its stun numbers come from [Game Balance](#game-balance), its climb build-up, exhaustion, idle recovery, orbit, and deceleration from `MonkeyClimbConfig`, and its input from `MonkeyInputConfig`.

- Body: kinematic, with gravity off. The script writes position and rotation outright through the Rigidbody, so no arm joint or collision can push it off its path
- Orbit: the horizontal input changes one angle, and the body sits exactly on a ring of fixed radius round its parent, the trunk
- Climb: the vertical input ramps a climb speed along world Y with separate acceleration and deceleration, and different maximums up and down
- Held: `IsHeld` freezes the body in place, set by the web trap ([Mobs](mob-doc.md#trap-and-escape)) and by a [pineapple leap](grab-doc.md#pineapple-leap), which pulls it up meanwhile
- Stun: after a branch hit the input is ignored and the body falls a set distance ([Mobs](mob-doc.md#hit-penalty))
- Stamina Cost: each step reports upward and sideways distance to `StaminaBarController.DrainByMovement`. The stun fall and descending cost nothing

Climb speed builds up along `ClimbUpSpeedCurve` and `ClimbDownSpeedCurve` in `Settings/Player/MonkeyClimbConfig.asset` (x: seconds the key has been held without a break, y: speed in u/s, whose end value is the top speed); the count resets on release, a flip of direction, a stun, or a web hold, and the exhausted multiplier scales the up curve. With no vertical key the climb eases to a stop at `DecelerationU`. Orbit speed and deceleration come from the same asset; the arm stroke and reach tuning are in `Settings/Player/MonkeyArmConfig.asset`, covered by the [Grab System](grab-doc.md#tuning).

## Tree

The tree is a stack of static and dynamic trunk segments under a `TreeTop`, which carries no win zone: a run only loses.

- Extent: `TreeManager` (singleton `I`) holds `MinY` and `MaxY`, 0 and 100 by default, from the finite tree; nothing reads them any more; `FallingCobraSpawner` takes each segment's pivot as the trunk axis
- Climb Height: heights that drive difficulty are measured above the monkey's start y by `RampedDifficultyManager.GetClimbedYDistance`: branch density, banana chance, web chance, falling cobra chance, the score, and the trunk height markers. Nothing shifts the world origin; if something ever does, it only has to move `PlayerGameStartYPosition` along
- Segments: each `TreeSegmentController` places branches on itself in `Start` and in every `Restart` ([Mobs](mob-doc.md#placement)), then raises the static `BranchesGenerated`, which `SpiderWebSpawner` answers with webs ([Mobs](mob-doc.md#placement-1)) and `FallingCobraSpawner` with falling cobras ([Mobs](mob-doc.md#spawning))
- Pool: `DynamicTreeSegmentPool` sits on an empty object, finds the `Player`-tagged object once, and instantiates `segmentCount` copies of `segmentPrefab` end to end, the first (lowest) one created at the world height `firstSegmentY`. It keeps them in a FIFO: only when the stack top is less than `lookAheadU` above the player, and the lowest segment lies wholly under the player, that lowest one is moved over the stack top by `TreeSegmentController.Restart`, which clears its branches and webs and rolls branches again; the webs follow through `BranchesGenerated`, the pool never calls the spawner. Its `mockTree` field names an editor-only stand-in, `Envs/Tree` in `Lv1Scene`, that is deactivated at runtime, so nothing that must run may sit under it

## Stamina

`StaminaBarController` is a singleton `Slider` that reads `PlayerBalanceConfig`. `StaminaBarAnimator` on the same GameObject punches the bar on `FruitConsumed` and shakes it on `LowStaminaMoveAttempted`, which `MonkeyController` triggers through `NotifyMoveAttempt` while an input is held and stamina is below the `lowStaminaFraction` field of `StaminaBarController` (10% by default, 20% on the `Lv1Scene` instance) of the maximum. That threshold is feedback only and sits outside `PlayerBalanceConfig`; only an empty bar changes movement.

- Movement Drain: `DrainByMovement` per physics step, upward distance costs more than sideways; it is the only drain, nothing lowers the bar on a timer or while the monkey stands still
- Restore: `AddStaminaByFruit` adds the per-banana amount, capped at the maximum, when the grabbed fruit's flight lands, so the bar rises and punches together ([Grab System](grab-doc.md#fruit-fly))
- Idle Recovery: `MonkeyController` adds `MonkeyClimbConfig.IdleStaminaRecoveryPerS` per second while no move key is pressed, scaled by `PlayerBalanceConfig.StartingZoneIdleRecoveryMultiplier` during the starting zone
- Depletion: reaching 0 does not end the run: `MonkeyController` scales upward climb speed only by `MonkeyClimbConfig.ExhaustedSpeedMultiplier` until stamina is above 0 again
- Label: the `StaminaBar` prefab carries a white TMP `Energy` label, `EnergyLabel`, above the left end of the bar; it is a child of the root, so it punches and shakes with the bar
- First Low Hint: `LowStaminaWidget` listens to the same `LowStaminaMoveAttempted`, so its once-per-scene hint opens with the first bar shake and cool-down pulse, at the same threshold ([UI](#ui))

All values live in `Settings/Player/PlayerBalanceConfig.asset`.

## Score

`ScoreManager` (singleton `I`, on the `GameController` object) keeps two scores and notifies the UI when the total changes.

- Distance Score: whole meters of the highest point climbed, read from `RampedDifficultyManager.CurrentPlayerClimbedYDistance`, refreshed every frame (player y minus the y at game start), times points per meter. Only a new high counts, so falling and re-climbing earns nothing
- Reward Score: points from each banana collected through its `ScorePickup`; `ScoreManager.BananaCollectedCount` counts those pickups. A coconut's `ScorePickup` (kind `Coconut`) adds `CoconutRewardPoints` instead, kept in `CoconutRewardScore`, counted in `CoconutCollectedCount`, and raises `CoconutCollected`
- Gate: `CanScore` is true only after start, before `EndRun`, while unpaused, and while the game is not over
- Display: `ScoreWidget` shows the total on the HUD and on the lose panel
- Units: `unitsPerMeter` only converts climbed world units to the meters the distance score pays for. Every height the player reads is plain world units: the trunk height markers and the progress bar's cobra gap label them `m`, and the lose panel's final height shows the bare number

All values live in `Settings/Player/PlayerBalanceConfig.asset`; `ScorePickup` carries no reward asset of its own.

The test scenes have no `ScoreManager`.

## Camera

Four Cinemachine cameras (behind, look-left, look-right, look-down) are swapped by priority. Every camera script sits on the main camera, next to the `CinemachineBrain`, and finds gameplay objects through their `I` singletons.

- Hit shake: `CameraHitShakeFeedback` reads `HitBranchSensor.I` and fires one of two `CinemachineImpulseSource` components on the camera by hit kind, light for a branch, heavy for a falling cobra. Amplitude and duration are tuned on the sources, and the strength a view feels also scales with its camera's impulse listener gain
- Cool-down: `CameraCoolDownFeedback` listens to `StaminaBarController.LowStaminaMoveAttempted` and pulses a URP Vignette on a global Volume plus a field-of-view widen on the live Cinemachine camera, easing both back to rest. It has two steps: a lighter one while stamina is below the low fraction, a stronger one once the bar is empty, and a low pulse in flight steps up when the bar empties. Both steps and the timing live in the `CameraEfxConfig` asset (the hit shake stays on its two impulse sources, since Cinemachine owns those values). A running pulse ignores repeats and a short cooldown follows

The swap rules are in the [Grab System](grab-doc.md#camera), the hit trigger in [Mobs](mob-doc.md#hit-penalty).

## Input

One asset, `Inputs/InputSystem_Actions`, holds all bindings, referenced through `InputActionReference` fields. No mouse button is bound (left, right, middle), so clicking does nothing in the game or on a uGUI button; keyboard and touch bindings stay. Pause (Esc or P), `RestartAfterLose` (R or Space), and `RestartDuringPause` (R) are `Player` actions assigned to `GameController`. The keys and buttons are listed under Controls in the [README](../README.md#controls).

- Move: shared by the body and both arms through `MonkeyInputConfig`
- Interact: the grab, read by both arms and the camera rig; `UseCoconut` is a separate `Player` action on the same keys with a MultiTap interaction (a double-tap, timing set in the asset), enabled and listened to by `CoconutManager`, found by the `inputActions` asset and `useCoconutActionPath` fields
- Jump: the web escape press, read by `SpiderWebStruggle`
- Collision: Space is bound to both Interact and Jump, so the struggle disables interact while the monkey is trapped

## UI

| Element | Script | Shows |
| --- | --- | --- |
| Stamina Bar | `UI/StaminaBarController` | current stamina, under an `Energy` label |
| Fruit Fly | `UI/FruitFlyEffect` | a grabbed fruit's sprite flying to the bar, the coconut counter, or the monkey ([Grab System](grab-doc.md#fruit-fly)) |
| Coconut | `UI/CoconutWidget`, `UI/FreezeTimerWidget`, `UI/CoconutMessageWidget` | count and double-tap hint; freeze countdown & bar; coconut messages through `PopUpManager`, all driven by `CoconutManager` ([Grab System](grab-doc.md#coconut)) |
| Climb Progress | `UI/ProgressBarWidget` | a window `windowSizeY` tall below the monkey, with a marker for how near the cobra's head is and the gap in world units labelled `m` |
| Score | `UI/ScoreWidget` | total score, on the HUD and the lose panel |
| Height Markers | `Environment/HeightMarkerSystem` | climbed height every `intervalU` beside the trunk, in world space rather than on the HUD canvas, see below |
| Screens | `UI/ScreensManager` | tutorial and lose panels |
| Lose Stats | `UI/LoseScreen` | final height (highest point climbed), final score, and bananas collected, filled each time the lose panel is enabled |
| Web Prompt | `UI/SpiderWebWidget` | the escape bar, and the first-trap hint through `PopUpManager` ([Mobs](mob-doc.md#trap-and-escape)) |
| Low Stamina Prompt | `UI/LowStaminaWidget` | asks `PopUpManager` for the first-low-stamina hint |
| PopUps | `PopUp/PopUpManager`, `PopUp/MessagePopUp` | every center message on one plate-and-text look, one at a time ([PopUps](pop-up-doc.md)) |
| Pause Screen | `UI/PauseScreenWidget` | dark panel with "Paused", "Press Esc or P to Resume", and "Press R to Restart" while `GameController.IsPaused`; on the always-active `PauseScreen` root under `_Screens`, it toggles its `Panel` child on `PauseChanged` |

The HUD canvas `_UI` nests `InGameHUD`, a full-stretch prefab holding `ProgressBar`, `StaminaBar`, `ScoreHeight`, the pause hint, `CoconutHUD`, `CoconutMessage` (the host of `CoconutMessageWidget`), and `FruitFlyLayer` (drawn last, above the bars); the `FreezeTimerHUD` prefab, the prompts and `Screens` stay direct children of `_UI`, drawn above it, with the `_PopUps` root just before `Screens`. `CoconutHUD` and `FreezeTimerHUD` start inactive and `CoconutManager` switches them on. `ScoreHeight` holds only the score label: the climbed height moved from the HUD to the trunk height markers.

The height markers replace a HUD height label. `HeightMarkerSystem` sits on `Envs/HeightMarkerSystem`, whose position marks the trunk axis, and works as follows:

- Pool: TextMeshPro labels are built at runtime in right and left pairs, marker k always on pair k mod pair count, so every marker shows on both sides of the trunk, and only markers from `showBelowU` under to `showAboveU` over the monkey show. Markers follow height, not tree segments, so a recycled segment can neither leave one behind nor show one twice
- Text: whole climbed world units (`GetClimbedYDistance`) plus `unitSuffix` (` m`), white bold, so a height tuned in `PlayerBalanceConfig`, e.g. `coconutStartHeightU`, reads straight off the trunk
- Plate: each label sits on a `SpriteRenderer` plate sliced from a circle texture generated at runtime, no imported art, colored like the progress bar's distance readout and sized to the glyphs; a sorting order keeps the text above it
- Placement: each frame after the `CinemachineBrain` (execution order 200 against its 100), the labels move beside the trunk silhouette, one on each side as seen from the camera, just far enough out that the trunk never hides them and a little behind the axis so branches and fruit draw over them, and face the screen. The monkey climbs on the camera-facing front, so they never cover it
- Boss Marker: the marker at `BossFightController.TriggerHeightU` gets a bigger, red, beating plate (Boss Marker fields) and a small Cobra King look-alike from `BossFightController.BuildMarkerFigure` on top of it, kept on whichever pooled label shows it; once `BossFightController.IsInArena` it is no longer shown
- Pulse: the first time the highest climbed height passes a marker, the label grows to `pulseScale` and its plate brightens for `pulseDurationS`; re-climbing after a drop does not repeat it
- Physics: neither label nor plate has a collider, so no spawner's overlap check or grab sees them
- Testing: Use Test Override swaps `intervalU` for `testIntervalU` (5); tick it in Play mode, so it never gets saved

`LowStaminaWidget` sits on an always-active Canvas child and holds no text. The first `StaminaBarController.LowStaminaMoveAttempted` since the scene loaded, the event behind the bar shake and the camera cool-down, asks `PopUpManager` for `LowStaminaHint`; the script then drops its subscription, and nothing is static, so a reload brings the hint back. The web hint outranks it, so a web trap interrupts a hint on screen and the manager resumes it afterward; a loss drops it. The hint sits below the screen center, clear of the web hint above it and of the vignette's dark edges; the Canvas has no render camera, so it draws as an overlay the vignette never darkens.

Every center message is a `PopUp` variant; the rules, the parts, and how to add one are in [PopUps](pop-up-doc.md).

The tutorial in `Lv1Scene` uses editable uGUI Images under `TutorialScreen/KeyboardControls` for the WASD cluster and a wide Space key. Thin white Image rectangles form each outline without a sprite dependency; the WASD keys also have inset keycap frames and diagonal bevel lines. Each key has an explicit, stretched Background child with a dark gray Image (alpha 0.82), ordered before its outlines and label to preserve contrast over the white overlay. Letters are white, and `space` sits inside the long rectangular key at the bottom left. Its uGUI Text captions show `Move`, `Grab Banana`, and `Hold to reach, release to grab`; the existing start prompt remains below them. The original translucent white overlay (alpha 0.392) washes out the game behind the tutorial to indicate that the run has not started. `ScreensManager` shows and hides the whole panel as before.

`Fonts/UI/MonkobraTutorial-SemiBold.ttf` is a renamed, static weight-600 subset of [Noto Sans TC](https://github.com/google/fonts/tree/main/ofl/notosanstc), distributed with its SIL Open Font License in `Fonts/UI/OFL.txt`. It embeds printable ASCII and a small set of Chinese glyphs, so the tutorial renders in WebGL without system fonts. Wording that uses other characters requires extending the font subset or supplying a font with those glyphs. The subset has no em dash `—`; the TMP `LiberationSans SDF` atlas has one, so the low-stamina hint uses that font.

## Conventions

- Singletons: scene-scoped and first-wins. `TreeManager`, `UpwardFruitSensor`, `StaminaBarController`, `FruitFlyEffect`, and `ScoreManager` destroy a duplicate component only. `GameController` and `ScreensManager` destroy the duplicate's whole GameObject
- Start Over Awake: a consumer that may start before its singleton reads it in `Start`, so Awake order never matters
- Events Over Calls: detection scripts forward trigger events (`Entered`, `Exited`, `BranchHit`) rather than calling their consumers, and a segment announces its branches with `BranchesGenerated` rather than calling its decorators
- Tags: `Branch`, `FruitCollider`, and `Player` drive trigger matching. Webs match by a Rigidbody component instead
- Tuning In Data: values stay in serialized fields, and shared tuning sits in a ScriptableObject: balance numbers in `PlayerBalanceConfig`, `WorldBalanceConfig`, and `CobraBalanceConfig` ([Game Balance](#game-balance)), climb and orbit in `MonkeyClimbConfig`, the leap in `PineappleLeapConfig`, monkey arm in `MonkeyArmConfig` and input in `MonkeyInputConfig`, branch geometry in `BranchFruitPlacementConfig`, the fruit flight in `FruitFlyConfig`. Gameplay values are never hard-coded
- Field Guards: required inspector fields are checked in `Awake` with a logged message naming the field
- Kinematic Body: nothing about the monkey is mass or gravity driven. Each arm is posed purely by its joint drives
- Style: `Player/`, `GameController/`, `Analytics/`, `MobGreatCobra/`, `MobFallingCobra/`, `UI/StaminaBarController`, `UI/ScreensManager`, and most of `Environment/` follow the house Unity style. `Scoring/`, `UI/ProgressBarWidget`, and `MobSpiderWeb/SpiderWebBuilder` are still in the template style, some with Chinese comments

## Build and Deploy

The only build is the WebGL export made from the Editor into `Monkobra/` and committed. Compression is disabled, because GitHub Pages cannot set response headers, and the `FitWindow` template keeps the game 16:9 at any window size. The steps and the required Player Settings are in [Build](build-doc.md).

## Known Gaps

Project-wide gaps are in [CONTEXT.md](../CONTEXT.md#known-gaps--constraints), subsystem ones in [Mobs](mob-doc.md#known-gaps) and the [Grab System](grab-doc.md#known-gaps).
