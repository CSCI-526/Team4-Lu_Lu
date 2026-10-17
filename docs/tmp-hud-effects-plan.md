# HUD Effects Plan: Cobra Indicator, Falling Snake Indicator, Banana Fly

<!--
todo great cobra indicator
todo falling snake indicator
todo grabbed banana -> stamina bar
-->

## Context

`CHANGELOG.md` carries three open TODOs: great cobra indicator, falling snake indicator, grabbed banana to stamina bar. Branch `add-camera-ui-juice` already holds camera and stamina juice, so the three HUD effects continue it.

Decisions:

- Great cobra: up triangle on the bottom screen edge at the cobra head's screen x (clamped inside the screen); hidden when far, larger when near, flashes red during a surge like the progress marker
- Falling snake: every awake falling cobra (Warning or Falling) above the monkey gets a green down triangle on the top screen edge at its screen x, flashing with the snake's own red pulse; "ray cast" is read as projecting each snake to the screen, not `Physics.Raycast`
- Banana: yellow circle spawns at the banana's projected screen point on grab, flies to the stamina bar, vanishes, the bar shakes

Scene wiring is done by hand in the Unity Editor, never by editing `Lv1Scene.unity` as text: `AGENTS.md` warns that an open Editor buffers scene edits and a text edit risks being lost.

## Starting Point

- `UI/ProgressBarRoot` already flashes `cobraMarker` by `CobraClimb.IsCatchingUp`; its pulse (`catchUpPulseHz`, `catchUpColor`) is the model for the cobra flash, and it builds its warning label at runtime on `canvas.rootCanvas`, the model for runtime-built HUD
- `MobFallingCobra/FallingCobra` has a private `State` (Dormant, Warning, Falling, Done) and `warningPulseHz`, with no public state or registry
- `Player/ArmRoot.TryGrabFruit` knows the banana (`fruit.bounds.center`), then calls `StaminaBarController.AddStaminaByFruit()`, which raises `FruitConsumed` with no position
- `UI/StaminaBarAnimator` shakes only on `LowStaminaMoveAttempted`, with a cooldown, and its shake is private
- `Lv1Scene` has an overlay Canvas (1280x720 reference scaler)

## Movement 1: Triangle Graphic

- Scope: `Assets/_Monkobra/Scripts/UI/TriangleGraphic.cs` (new)
- Statement: a small `MaskableGraphic` that draws one filled triangle, pointing up or down by a field, so no sprite asset is needed; shared by both indicators
- End State: the script compiles with no warnings; nothing in the scene uses it yet
- Order: none

## Movement 2: Great Cobra Indicator Script

- Scope: `Assets/_Monkobra/Scripts/UI/GreatCobraIndicator.cs` (new)
- Statement: builds one up triangle on the root canvas at runtime, places it on the bottom edge under the cobra head's projected x (clamped), scales it by the gap below the monkey (hidden past `hideDistanceU`, `minSize` at the edge of range up to `maxSize` close), and lerps its color to red on a pulse while `CobraClimb.IsCatchingUp`; hidden outside `GameState.Playing`; tuning in serialized fields
- End State: the script compiles; with its fields set it is ready to attach
- Order: Movement 1

## Movement 3: Wire and Check Cobra Indicator (user)

- Scope: `Lv1Scene` in the Editor only
- Statement: add `GreatCobraIndicator` to a HUD GameObject under the Canvas, assign `Segment_00` of the great cobra as the cobra head; press Play and watch the cobra approach
- End State: triangle shows at the bottom edge when the cobra is in range, grows as it nears, is absent when far, and flashes red after a branch hit; no console errors
- Order: Movement 2

## Movement 4: Falling Cobra Registry

- Scope: `Assets/_Monkobra/Scripts/MobFallingCobra/FallingCobra.cs`
- Statement: add a static registry filled in `OnEnable`/`OnDisable`, public `IsAlerting` (Warning or Falling) and `WarningPulse` (the same 0~1 pulse it paints on its body, so the HUD flashes in step); fall, hit, and spawn logic untouched
- End State: the snake behaves exactly as before; the new members are readable from other scripts
- Order: none

## Movement 5: Falling Snake Indicator Script

- Scope: `Assets/_Monkobra/Scripts/UI/FallingCobraIndicator.cs` (new)
- Statement: each frame walks the registry, keeps alerting cobras above the monkey, projects them with `Camera.WorldToScreenPoint`, and for those above the screen pins a pooled green down triangle on the top edge at the clamped screen x, flashing with `WarningPulse`; on-screen snakes get no marker
- End State: the script compiles; ready to attach
- Order: Movements 1 and 4

## Movement 6: Wire and Check Falling Snake Indicator (user)

- Scope: `Lv1Scene` in the Editor only
- Statement: add `FallingCobraIndicator` to the HUD GameObject; press Play and stand under a falling cobra, with another off-screen above if possible
- End State: a green down triangle flashes at the top edge while the snake is awake and above the view, then vanishes once it is on screen or switched off; several snakes show several triangles
- Order: Movement 5

## Movement 7: Banana Grab Event

- Scope: `Assets/_Monkobra/Scripts/Player/ArmRoot.cs`, `Assets/_Monkobra/Scripts/UI/StaminaBarController.cs`
- Statement: `ArmRoot.TryGrabFruit` captures the banana's world center before it switches off and passes it to `AddStaminaByFruit(Vector3)`, which raises a new `FruitGrabbed(Vector3)` beside the unchanged `FruitConsumed`
- End State: grabbing still restores stamina and punches the bar as before; the new event fires with the banana's position
- Order: none

## Movement 8: Banana Fly Efx Script

- Scope: `Assets/_Monkobra/Scripts/UI/BananaFlyEfx.cs` (new), `Assets/_Monkobra/Scripts/UI/StaminaBarAnimator.cs`
- Statement: `BananaFlyEfx` listens to `FruitGrabbed`, projects the point to the canvas, spawns a yellow circle, eases it to the stamina bar's rect over `flyDurationS`, destroys it, then calls a new public `StaminaBarAnimator.PlayShake()` that skips the low-stamina cooldown; the existing punch stays on `FruitConsumed`
- End State: the scripts compile; ready to attach
- Order: Movement 7

## Movement 9: Wire and Check Banana Fly (user)

- Scope: `Lv1Scene` in the Editor only
- Statement: add `BananaFlyEfx` to the HUD GameObject, assign the stamina bar; press Play and grab bananas, including two in quick succession
- End State: a yellow disc flies from where the banana was to the stamina bar, then the bar shakes; stamina still rises at once; no console errors
- Order: Movement 8

## Movement 10: Changelog and Docs

- Scope: `CHANGELOG.md`, `CONTEXT.md`, `docs/monkobra-tdd.md`, `docs/mob-doc.md`, `docs/grab-doc.md`
- Statement: `Added` entries under Erik; remove the three TODO lines now landed; script index rows for the new and changed scripts; TDD UI table rows; Mobs note on the registry and indicator; Grab note on `FruitGrabbed` and the fly efx
- End State: docs match the code
- Order: Movements 3, 6, and 9 checked in the Editor

## Verification

No tests exist (see `AGENTS.md`); verification is the manual Play-mode pass in Movements 3, 6, and 9, also at a second aspect ratio so edge clamping holds.
