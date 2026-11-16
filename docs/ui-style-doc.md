# MonKobra: UI Style

One look for every screen, taken from the Monkobra logo and the WebGL loading page: gold letters with a dark brown outline and a brown shadow down-right, on a dark jungle green. A new screen uses the fonts, colors, buttons, and key icons below and nothing else.

## Palette

The code twin is `UIStyle` (`Scripts/UI/UIStyle.cs`). Gameplay signals keep their own colors (the boss health bar and warnings stay red).

| Name | Hex | Use |
| --- | --- | --- |
| Background | `#0A1F12` | every full-screen background, the loading page included |
| Gold | `#EFB64D` | button face, score, highlights |
| Brown | `#352016` | outlines, button labels, keycaps |
| Shadow | `#89501F` | the shadow down-right of a title, button, or keycap |
| Green | `#24A965` | the cobra, success |
| Cream | `#FFF2D2` | text on the dark background, key icon outlines, the selection ring |
| GoldLight | `#FFD36E` | selected button face |
| GoldDark | `#D99A35` | pressed button face |
| LockedFill / LockedInk | `#9E8F78` / `#4A3A2C` | locked button face / its outline, label, and lock |
| Panel | `#102C1B` | cards and popups on the background |
| Divider | `#3B4C41` | thin rules (the background plus 20% white) |

## Type

One family, Baloo 2 (OFL, `Fonts/baloo2/`), in two weights; weight and size make the levels.

- Files: `Baloo2-SemiBold.ttf` and `Baloo2-ExtraBold.ttf`, static instances of the variable font cut to the Latin set (Basic Latin, Latin-1, common punctuation), 37 KB each, so a WebGL build stays small
- TMP: `Baloo2-SemiBold SDF` and `Baloo2-ExtraBold SDF`, static 1024 atlases of the same set; SemiBold is the TMP default, so text made in code without a font gets it, and a Bold style on SemiBold text draws the real ExtraBold; LiberationSans is the fallback for any glyph outside the set
- Title material: `Baloo2-ExtraBold SDF - Title` adds the logo's dark brown outline and brown shadow; the face color comes from the text color (cream for a title)
- HUD number material: `Baloo2-ExtraBold SDF - HudNumber`, a thin dark brown outline and a soft dark shadow, lighter than Title at HUD sizes, where Title's thick outline and hard shadow look heavy; made by step 4
- Web: the loading page loads the very `Baloo2-SemiBold.ttf` (copied by `WebGLLoaderArt`), so "Loading" and the title's start prompt match at the hand-over
- Size: Baloo 2's capitals are 0.60 of the font size against LiberationSans' 0.69, and a line is 1.60 against 1.12, so at the same size it looks about 13% smaller and multi-line text spreads further apart; size from the scale below, never by copying an old LiberationSans size

| Level | Font asset | Material | Size | Color |
| --- | --- | --- | --- | --- |
| Screen title ("Select Mode", "Paused") | ExtraBold | Title | 64 | Cream |
| HUD number (score, bonus) | ExtraBold | HudNumber | 24–40, auto | Gold |
| Panel title | ExtraBold | default | 40 | Cream |
| Button label | ExtraBold | default | 34 | Brown |
| Prompt next to key icons | SemiBold | default | 28–40 | Cream |
| HUD label | SemiBold | default | 22 | Cream |
| Small print | SemiBold | default | 18, never under 16 | Cream |

Sizes are 1280×720 canvas units.

## Buttons

Two prefabs in `Prefabs/UI/`: `MenuButton` (a long button with a label) and `IconButton` (a square button with a glyph). Restyle an existing button in place instead of replacing it (`UIStyleBuilder.StyleMenuButton`, `StyleIconButton`), so its references survive.

- Look: a Gold face with a 5 px Brown outline and a Shadow block 5 right and 6 down, as on the loading page's plates
- States, a Sprite Swap over `Sprites/UI/MenuButton_*` or `IconButton_*`: Normal; Selected, a lighter face inside a Cream ring (it doubles as hovered, since a hover selects); Pressed, the face sunk onto its shadow; Locked, LockedFill with a lock beside the label
- Feel: `MenuButtonPop` grows the selected button to 1.08, sinks the label or glyph with the Pressed face, and shows the lock while the button is not interactable; it runs on unscaled time, so a paused screen behaves the same
- Size: the sprites carry the ring and shadow inside the rect, 8 left and top, 13 right, 14 bottom, so a 420×70 face is a 441×92 rect; a column of buttons sets its spacing to the wanted gap minus 22
- Glyphs: `Sprites/UI/Icon_Pause`, `Icon_Resume`, `Icon_Restart`, `Icon_Exit`, `Icon_Lock`, white art tinted Brown by the Image

## Plate

A panel that shows what is behind it: `Sprites/UI/Plate_Fill`, tinted Panel at alpha 0.55, with a child `Rim` drawing `Sprites/UI/Plate_Rim` in Gold (both 9-sliced, one shape, radius 22). The rim is its own Image, never an `Outline` component: an Outline repeats the whole fill under it in its color, which shows through a see-through plate and tints it gold. `UIStyleBuilder.MakePlate` builds one on an existing Image. Used by the popups and the score.

## Key Icons

The input icons (`Sprites/InputIcons/`) stay, worn as keycaps: the icon's Image draws the matching `*_keycap` silhouette in Brown with a Shadow component (2 right, 3 down, Shadow color), and a child `Outline` Image draws the original outline in Cream on top (`UIStyleBuilder.MakeKeycap`). The fill has no holes, so the shadow never shows through the key.

A Space icon with a press clip keeps its keycap on the root Image and draws the key on the `Outline` child, so the clip's sprite curve points at `Outline`; the finger icons are no keys and only take the Cream. An animated input icon (`Prefabs/InputIcons/`) is sized by its rect, never by its scale: its press clip writes the scale (1, 0.92, back to 1), so a scaled icon looks right in the Editor and shrinks back in Play. `UIStyleBuilder.SizeAnimatedIcons` moves any such scale into the rect.

## Layout

- Lay out against the 1280×720 Canvas Scaler (match 0.5) with anchors, layout groups, and a 24 unit margin from the screen edge, never a fixed position that only fits 16:9; check 16:9, 16:10, and 4:3
- Full-screen menus hang the loading page's vines (`TitleScreenLayoutBuilder.BuildVines`), so they read as the same place
- A `ControlStyleSwitch` picks its keyboard or touch group only in Play, so the Editor would show both rows on top of each other; the builders leave the keyboard group on and the touch group off (`UIStyleBuilder.ShowKeyboardGroups`), and Play switches them as usual

## Applying It

All of it is built by Editor menus, never by editing prefab YAML:

1. `Monkobra > UI Style > 1. Build Fonts`: the two TMP font assets, the Title material, and the TMP default font; existing font assets are kept, since a rebuild would change their GUIDs
2. `Monkobra > UI Style > 2. Build Button Prefabs`: `MenuButton` and `IconButton`
3. `Monkobra > UI Style > 3. Style Level Select`: the level selection and its key hint rows
4. `Monkobra > UI Style > 4. Style HUD`: the input icons, the HUD prefabs, the popups, the tutorial, and the font overrides of both gameplay scenes
5. `Monkobra > UI Style > 5. Style Pause and Lose Screens`: the pause screen, the lose screen, and the leaderboard
6. `Monkobra > Build Title Screen`: the title picture, whose start prompt words take the loading page's font

`Build All` runs steps 1 to 3. To change the character set, cut the fonts again from the Google Fonts variable `Baloo2[wght].ttf` with fontTools (instance at weight 600 and 800, then subset), delete the two SDF assets, and run step 1.

## HUD

- Top-right cluster, from the corner inward: the pause button (an `IconButton`, a 64 face) inside the 24 safe margin, its "Esc to Pause" hint (keyboard only) 10 left of the face, the coconut counter 24 left of the hint; each offset follows from the sizes of the elements to its right, so the three never meet at any aspect ratio
- Climb progress: the bar is a Scrollbar turned 90 degrees and stretched 1.8 along its length; its heads and readout turn back and undo that stretch (scale y 1/1.8), so they stand upright at true size; the monkey and cobra wear the loading page's heads, the cobra's neck is a column of its green segments, stretched from under the head down to the bar's bottom end (`ProgressBarWidget.cobraBody`), so the cobra rises from the bottom and never hangs out of the bar; it stays in the bar when it nears the monkey, its head tucking under the monkey's (`isMarkerSideStepped` off); the head flashes red while the cobra surges (`ProgressBarWidget.markerFlash`); the distance reads cream on a Brown plate with a Gold rim
- Energy and web bars: a Panel track with a Brown rim, a Gold fill
- Score: a [plate](#plate) 220×64 in the top-left safe margin, level with the pause button, a "Score" caption at its left and the number right-aligned (it grows leftward, away from the bonus feed on its right), Gold with the HudNumber material; the bonus numbers take that material too, in their per-reward colors
- Popups: a [plate](#plate), as see-through as the old black one, so the monkey's reaching arm stays in sight behind a grab hint; plain text is Cream, while a variant's own signal color (a warning red, a frozen blue) stays
- Tutorial: a dark Background wash, Baloo 2 words, keycaps
- Hack key list (F1–F12): hidden when a scene starts, F9 shows or hides it

## Run Screens

- Pause: a Background wash over the frozen game, "Paused" as a screen title, and a column of square `IconButton`s (Resume, Restart, Exit, with the `Icon_Resume`, `Icon_Restart`, `Icon_Exit` glyphs), faces 20 apart; each button's words sit 14 right of its face in ExtraBold Cream, its key (Esc, R), keyboard only, on a keycap 10 left of it; the column is placed so its widest row is centered (`UIStyleBuilder.StyleIconColumn`)
- Lose: a near-opaque Background wash; the run card and the leaderboard are [plates](#plate) of one height (520), centered, and the key hint row sits at its true size under both, so nothing crowds the card at 4:3; on the card, "Game Over!" in the Title look, the final score in Gold with the HudNumber material, the rest in Cream, every line shrinking to fit its width; Restart and Exit are the same icon column as the pause screen; the dev-only Download Analytic button is a small `MenuButton` in the top-right safe margin
- Leaderboard: a plate whose gold rim replaces the old gold top line, a Cream title, Divider rules, Cream rows on faint Cream stripes, the player's row in Gold on a Gold stripe, drawn Bold, ie ExtraBold
- Boss fight panels, built in code by `BossFightHud`: the win's reward panel, the revive card, the badge, tags, the health bar plate and the speech bubble take `UIStyle` colors (Background wash, Panel plates, Gold titles drawn Bold, Cream words, a Gold button with a Brown label for the main choice); the mock course ad keeps its own course colors, and the signal colors (health red, warning red, the EASY green) stay

The old fonts and `Sprites/PopUp/PopUpBackground` are deleted last, in their own change, once every screen is checked.
