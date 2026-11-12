# Input

The target control scheme, in two control styles, Keyboard and Touch. Bindings and code are in the [Technical Design Document](monkobra-tdd.md#input).

- Preferred: the input the hints and tutorial teach
- Support: also works, never taught

## Intents

Four intents, the same in every screen. Each has one Preferred key and fixed Support keys.

| Intent | Preferred | Support | Touch |
| --- | --- | --- | --- |
| Move | W, A, S, D | Arrows | joystick; menus: tap |
| Confirm | Space | E, Enter | tap, or the action zone |
| Back, Pause | Esc | P | back or pause button |
| Restart | R | ➖ | restart button |

## Keyboard Style

### Confirm by Context

| Context | Confirm does |
| --- | --- |
| Menus | pick the highlighted option |
| Title | open level select |
| Tutorial | start the run |
| Climbing | hold, release: grab |
| Climbing | double-tap: throw a coconut |
| Spider web | press rapidly: break free |
| Boss revive | watch the ad |
| Boss ad | skip, once allowed |
| Boss reward | continue |

### Move by Context

| Context | Move does |
| --- | --- |
| Menus | W, S moves the highlight |
| Climbing | up, down climbs; left, right circles |

### Back, Pause, Restart by Context

| Context | Back, Pause | Restart |
| --- | --- | --- |
| Level select | back to title | ➖ |
| Climbing | pause | ➖ |
| Pause screen | resume | restart run |
| Lose screen | ➖ | restart run |
| Boss revive | ➖ | give up |

- Mouse: click presses any button; never moves the character, never switches the control style
- Lose screen: Confirm with nothing highlighted also restarts
- Course page: button only (click or tap), so Enter stays a plain Confirm

## Touch Style

| Intent | Touch |
| --- | --- |
| Move | joystick, bottom left |
| Confirm | action zone, anywhere else on screen |
| Back, Pause | button |
| Restart | button |

- Confirm by context: same table as Keyboard Style; hold, double-tap, rapid taps and a plain tap all land on the zone
- Menus and boss panels: tap the button
- Virtual gamepad: joystick writes `leftStick`, zone writes `buttonSouth`, so the zone acts as Space
- No virtual keyboard: its writes would read as key presses and flip the control style back
- Zone and joystick show only in Touch while the run is playable; UI draws above, so buttons take taps first

## Hints and Buttons

- Icons: a hint shows an animated icon, never a key name; one icon per control style, a Space press for Keyboard and a finger tap for Touch, with hold, double-tap and rapid-tap variants where the action needs one
- Preferred Only: a hint teaches the Preferred input of the style in use; a Support key (P, arrows, E, Enter) is never drawn or written
- Pause Screen: Keyboard shows the Esc and R key icons beside the Resume and Restart buttons; Touch shows the buttons alone
- Button Colours: blue is normal, green while hovered or keyboard-selected, red while pressed, grey when locked (the endless button before it unlocks)
- Button Shapes: text buttons use the wide set `Button_*`; the in-run pause button and the pause screen's Resume and Restart are square icon buttons (`HUDPause_*`, `HUDResume_*`, `HUDRestart_*`); all cut from `Sprites/InputIcons/FreeGUI-Button.png`

## Control Style Switching

`ControlStyleManager`, kept across scenes.

- Start: Touch on mobile or touch-only devices, else Keyboard
- To Touch: a touch press
- To Keyboard: any key press
- Mouse: never switches
- Hints: re-word on `StyleChanged`

## Other Devices

Gamepad works (left stick or D-pad moves, north grabs, south breaks web) but is not a designed control style.
