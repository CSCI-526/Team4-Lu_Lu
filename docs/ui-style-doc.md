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
- Title material: `Baloo2-ExtraBold SDF - Title` adds the logo's dark brown outline and brown shadow; the face color comes from the text color (cream for a title, gold for a score)
- Web: the loading page loads the very `Baloo2-SemiBold.ttf` (copied by `WebGLLoaderArt`), so "Loading" and the title's start prompt match at the hand-over
- Size: Baloo 2's capitals are 0.60 of the font size against LiberationSans' 0.69, and a line is 1.60 against 1.12, so at the same size it looks about 13% smaller and multi-line text spreads further apart; size from the scale below, never by copying an old LiberationSans size

| Level | Font asset | Material | Size | Color |
| --- | --- | --- | --- | --- |
| Screen title ("Select Mode", "Paused") | ExtraBold | Title | 64 | Cream |
| Score | ExtraBold | Title | by place | Gold |
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

## Key Icons

The input icons (`Sprites/InputIcons/`) stay, worn as keycaps: the icon's Image draws the matching `*_keycap` silhouette in Brown with a Shadow component (2 right, 3 down, Shadow color), and a child `Outline` Image draws the original outline in Cream on top (`UIStyleBuilder.MakeKeycap`). The fill has no holes, so the shadow never shows through the key.

An animated input icon (`Prefabs/InputIcons/`) is sized by its rect, never by its scale: its press clip writes the scale (1, 0.92, back to 1), so a scaled icon looks right in the Editor and shrinks back in Play. `UIStyleBuilder.SizeAnimatedIcons` moves any such scale into the rect.

## Layout

- Lay out against the 1280×720 Canvas Scaler (match 0.5) with anchors, layout groups, and a 24 unit margin from the screen edge, never a fixed position that only fits 16:9; check 16:9, 16:10, and 4:3
- Full-screen menus hang the loading page's vines (`TitleScreenLayoutBuilder.BuildVines`), so they read as the same place
- A `ControlStyleSwitch` picks its keyboard or touch group only in Play, so the Editor would show both rows on top of each other; the builders leave the keyboard group on and the touch group off (`UIStyleBuilder.ShowKeyboardGroups`), and Play switches them as usual

## Applying It

All of it is built by Editor menus, never by editing prefab YAML:

1. `Monkobra > UI Style > 1. Build Fonts`: the two TMP font assets, the Title material, and the TMP default font; existing font assets are kept, since a rebuild would change their GUIDs
2. `Monkobra > UI Style > 2. Build Button Prefabs`: `MenuButton` and `IconButton`
3. `Monkobra > UI Style > 3. Style Level Select`: the level selection and its key hint rows
4. `Monkobra > Build Title Screen`: the title picture, whose start prompt words take the loading page's font

`Build All` runs steps 1 to 3. To change the character set, cut the fonts again from the Google Fonts variable `Baloo2[wght].ttf` with fontTools (instance at weight 600 and 800, then subset), delete the two SDF assets, and run step 1.

Done so far: the level selection and the title's start prompt. The in-game HUD, pause, lose, and win screens follow.
