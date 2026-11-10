# Monkobra PopUps

Every short message shown over the screen during a run: the coconut prompts, the cobra warnings, the low-stamina hint, the web hint, and the grab hints for banana, pineapple, and coconut. They share one look (a semi-transparent plate behind a text) and one manager that decides which shows when. The wider UI map is in the [Technical Design Document](monkobra-tdd.md#ui), the script index in [CONTEXT.md](../CONTEXT.md).

## Parts

| Part | Role |
| --- | --- |
| `PopUp/PopUpId` | enum, one entry per popup variant, the key callers use |
| `PopUp/MessagePopUp` | on every variant: its own text, look, timing, and flags; `Show` fades it in, `Hide` fades it out |
| `PopUp/PopUpManager` | singleton `PopUpManager.I` on the `_PopUps` root: shows one popup at a time and owns the rules below |
| `Prefabs/PopUp/_PopUpBase` | base prefab: `CanvasGroup`, `MessagePopUp`, a 9-sliced `Background` plate, a `Text` label |
| `Prefabs/PopUp/_PopUpFruitBase` | copy of the base for the fruit grab hints: a fruit `Icon`, the text, an inline SPACE BAR keycap, a static tail label, and a `Line` driven by `PopUpWorldLink` |
| `PopUp/PopUpWorldLink` | on `_PopUpFruitBase`: draws the `Line` from the popup bottom center to a world target set by its owner, hidden when there is none or it is behind the camera |
| `Prefabs/PopUp/<Subject>_<Variant>` | one variant of the base per popup, saved inactive; `Banana_First`, `Pineapple_First`, and `Coconut_Grab` vary `_PopUpFruitBase` and set their own icon sprite |
| `Prefabs/PopUp/_PopUps` | root prefab nesting every variant, carrying `PopUpManager`, a direct child of `_UI` drawn under `Screens` |
| `Sprites/PopUp/PopUpBackground` | the rounded plate sprite, its 9-slice border set in the import settings |

The widgets stay as thin event listeners and hold no text: `CoconutMessageWidget`, `ProgressBarWidget` (the surge and chase warnings), `LowStaminaWidget`, and `SpiderWebWidget`, and `FruitGrabPrompt` (the `BananaFirst`, `PineappleFirst`, and `CoconutGrab` hints, each held while a fruit of its kind is in reach, or within a wide radius even on the far side of the tree, until one is grabbed in the run, and shown again at the next such fruit; the coconut and pineapple outrank the banana) each call `PopUpManager.I.Show` or `Hide` with a `PopUpId`. `FruitGrabPrompt` also finds the variant with `PopUpManager.I.GetPopUp` and hands the reach target to its `PopUpWorldLink` each frame, so the line follows the fruit.

## Where Config Lives

Everything a designer tunes sits on the variant prefab, not in a script or an asset: open it in `Prefabs/PopUp/` and edit

- Text: the `message` field of `MessagePopUp`, with a `{0}` slot for a value the caller passes
- Look: the color and font size of the `Text` object, the color and alpha of the `Background` object
- Position: the anchors and anchored position of the root `RectTransform`; the plate grows with the text
- Timing: show time (0 stays until hidden), fade-in, fade-out, and pulse rate
- Rules: priority, run-only, once-per-scene
- Fruit Hint Parts: on `_PopUpFruitBase`, the `Icon` size, the `Key` label of the keycap, the `Tail` text, and the `Line` color and width (`lineWidth` of `PopUpWorldLink`); a variant sets only its icon sprite and its `message`, which stops before the keycap

## Rules

- One at a Time: a higher priority interrupts the showing popup, an equal or lower one queues, in priority order and first come first served within one priority
- Resume: an interrupted popup returns to the queue front with its show time left and plays the rest once the higher one is gone
- Same Popup Again: asking for the showing popup refreshes it in place, asking for a queued one replaces its entry
- Run Gating: a run-only popup shows only while `GameController.IsRunLive`, and is dropped from the queue if the run is not live when its turn comes
- Once Per Scene: a flagged popup is remembered for the scene load; the manager is rebuilt with the scene, so the next load shows it again
- Loss: on `GameState.Lost` the showing popup stops at once and the queue is cleared
- Pause: timing follows `Time.deltaTime`, so a pause freezes a popup and a resume continues it
- Holds: a popup with no show time, such as the web hint or a grab hint, stays until its owner calls `Hide`

## Add a Popup

1. Make a variant of `_PopUpBase` in `Prefabs/PopUp/`, named `<Subject>_<Variant>`, and set its message, look, position, timing, and rules; leave it inactive
2. Add a `PopUpId` entry and set it as the `id` of the variant
3. Nest the variant under `_PopUps`: the manager finds every variant under it, so no list needs filling
4. Call `PopUpManager.I.Show(PopUpId.<Entry>, args)` from the listener of the event that should raise it

An id with no variant under `_PopUps` logs a warning and shows nothing.
