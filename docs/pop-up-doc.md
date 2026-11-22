# Monkobra PopUps

Every short message shown over the screen during a run: the coconut prompts, the cobra warnings, the low-stamina hint, the web hint, and the grab hints for banana, pineapple, and coconut. They share one look (a semi-transparent plate behind a text) and one manager that decides which shows when. The wider UI map is in the [Technical Design Document](monkobra-tdd.md#ui), the script index in [CONTEXT.md](../CONTEXT.md).

## Parts

| Part | Role |
| --- | --- |
| `PopUp/PopUpId` | enum, one entry per popup, the key callers use |
| `PopUp/PopUpDefinition` | the data of one popup: kind, text, size, color, timing, priority, flags, screen height, fruit icon, hint icons |
| `Settings/PopUp/PopUpCatalog` | the asset holding one definition per `PopUpId` |
| `PopUp/MessagePopUp` | the popup entity: `Show` dresses it with a definition, then fades it in; `Hide` fades it out |
| `PopUp/PopUpManager` | singleton `PopUpManager.I`: shows one popup at a time on the entity of its kind and owns the rules below |
| `Prefabs/PopUp/_PopUpBase` | entity of a `Message` popup: `CanvasGroup`, `MessagePopUp`, a 9-sliced `Background` plate, a `Text` label, a `HintIcon` slot |
| `Prefabs/PopUp/PopUp_Fruit` | entity of a `Fruit` popup, a copy of the base with a fruit `Icon`, a static `Tail` label, and a `Line` driven by `PopUpWorldLink` |
| `PopUp/PopUpWorldLink` | on `PopUp_Fruit`: draws the `Line` from the popup bottom center to a world target set by its owner, hidden when there is none or it is behind the camera |
| `Prefabs/PopUp/PopUpManager` | root prefab, a direct child of `UI` drawn under the screens: `PopUpManager` with the 2 entities saved inactive beneath it |
| `Sprites/UI/Plate_Fill`, `Plate_Rim` | the UI style's plate and its rim, 9-sliced in the import settings |

The widgets stay as thin event listeners and hold no text: `CoconutMessageWidget`, `ProgressBarWidget` (the surge and chase warnings), `LowStaminaWidget`, and `SpiderWebWidget`, and `FruitGrabPrompt` (the `BananaFirst`, `PineappleFirst`, and `CoconutGrab` hints, each held while a fruit of its kind is in reach, or the nearest one within a wide radius that the player can see, on screen and on the trunk's near side, so the line never points off screen or into the trunk, until one is grabbed in the run, and shown again at the next such fruit) each call `PopUpManager.I.Show` or `Hide` with a `PopUpId`. `FruitGrabPrompt` also finds the entity with `PopUpManager.I.GetPopUp` (right after `Show`: the entity is shared by every `Fruit` popup) and hands the reach target to its `PopUpWorldLink` each frame, so the line follows the fruit.

## Where Config Lives

Each popup is one entry of `Settings/PopUp/PopUpCatalog`; open the asset and edit

- Text: `message`, with a `{0}` slot for a value the caller passes, and `touchMessage` for the touch wording (blank keeps `message`)
- Look: `fontSize` and `textColor`: Cream for plain text, a signal color (warning red, frozen blue) where it means something; the [UI Style](ui-style-doc.md#plate) plate and the Baloo 2 font sit on the entity prefab, shared by all (Panel at alpha 0.55, so the monkey's arm shows through, and a Gold `Rim` child)
- Position: `anchorY` (0 bottom, 1 top) and `offsetY`; the plate grows with the text
- Timing: show time (0 stays until hidden), fade-in, fade-out, and pulse rate
- Rules: priority, run-only, once-per-scene
- Icons: a `Fruit` popup's `icon` sprite, and the `keyboardHintPrefab` and `touchHintPrefab` of the animated hint, blank for none
- Entity Parts: on `PopUp_Fruit`, the `Icon` size, the `Tail` text, and the `Line` color and width (`lineWidth` of `PopUpWorldLink`); on both entities the `HintIcon` slot

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

1. Add a `PopUpId` entry
2. Add a definition with that id to `PopUpCatalog`, choose its `kind` (`Message` for a plain one, `Fruit` for a grab hint), and set its message, look, position, timing, rules, and icons; no prefab is made
3. Call `PopUpManager.I.Show(PopUpId.<Entry>, args)` from the listener of the event that should raise it

An id with no definition logs a warning and shows nothing.
