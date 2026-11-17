# Monkobra CHANGELOG

<!--
Bug web spider trigger weird
Todo upd weekly devlog

bug endless mode need to start w/ full heath & no starting zone
todo pan branch & bark model/texture
todo erik lighting
todo erik skybox
todo erik sfx
todo add 3x fruit
fixme mpv tutorial for boss fight
fixme during boss fight, make energy bar gone, better ways to indicate 1 kill


todo todo vertical version
-->


## [Unreleased]

### Added

##### Erik

- Monkey outline: the skinned monkey wears a white, always-on outline (`MonkeyOutline`, `Monkey_Outline` material), the grabbable fruit look
- hack layer: F12 (development build only) turns the hack key list & every other hack key on or off, off at start; a release build keeps the keys live & F12 dead
- analytics records the Spider King: `SpiderFight`, `SpiderRevive` & `SpiderWin` metrics (new `SpiderKingFightController.Revived` event); the mock session plays a timed spider fight after a won Cobra King one
- Spider King parry lesson shows the animated double tap icon (keycap or finger by control style) under "Now!", replacing the "Tap SPACE 2 times" text (`BossFightHud.ShowDoubleTapHint`)
- accent fonts back after the Baloo 2 unification: BadaBoom on the score numbers (HUD, bonus popups, lose screen), Chewy on the level-select labels & Download Analytic, Life Is Goofy on the Endless unlock hint, which sits again on the button's right shoulder, in 2 lines; each in the screens it had before, Baloo 2 stays the TMP default

##### Yuqing Lu

- WebGL loading page: the cobra eats a row of dots along a branch on the real download progress (1.3 s at least, so a cached load still shows it) while the monkey hops ahead; it ends on the title picture & fades out once Unity draws, so the hand-over is unseen
- title screen redrawn: Monkobra logo, the grown cobra & the monkey on a branch, vines; the start prompt sits where the loading page writes "Loading"
- team logo screen before the WebGL loading page: USC Games & Peel Five side by side; they fade out to the background, then the loading page fades in; a click or key skips them, a reload in the same tab skips them too
- UI style from the logo (docs/ui-style-doc.md): Baloo 2 as the UI font (Latin set, now the TMP default), the palette, logo-style `MenuButton` & `IconButton` prefabs (selected grows inside a cream ring, pressed sinks, locked shows a lock), key icons as dark brown keycaps, all applied by the `Monkobra > UI Style` menus

### Changed

##### Erik

- project layout follows the u3d skills: `Models/` → `Meshes/`, SFX under `Audio/SFX/`, `Fonts/Baloo2/`, module-owned UI scripts & prefabs re-homed into their module folders, `Boss/` split into `BossCobraKing/` & `BossSpiderKing/` (classes renamed `CobraKing*`, `SpiderKing*`)
- 1 gameplay scene: `GameplayScene` takes the run mode from `RunModeSelection.Selected`, `EndlessScene` gone; Normal Play & Endless Mode load the same scene
- prefab rule, 1 prefab = 1 entity: group prefabs `Entities`, `_UI`, `_Screens`, `InGameHUD`, `LowStaminaPrompt` dissolved into the scene; `PauseButton`, `FruitFlyLayer`, `CoconutMessage` own prefabs
- 11 popup variants → 1 `PopUpCatalog` read by `PopUpManager`; 9 input icons → 2 (`InputIcon_Key`, `InputIcon_Touch`) + `HintIconLibrary`; branch prefabs `_BranchBase` & `Branch_X`; `EventSystem_Menu` is a variant of `EventSystem`
- cobra meshes thinned by the merged `simplify-snake-mesh` branch, same look
- script conventions applied project-wide: sections, Inspector Assignment Guard, 80 columns, typed locals, English comments; hack keys read through `HackShortcutConfig`, not `Keyboard.current`
- god classes split by role around a shared rig & `[Serializable]` tuning object, behavior unchanged, prefab values kept: falling cobra, great cobra motor, Cobra King fight, boss HUD, Spider King fight
- hot paths: cobra head bounds, freeze timer text & tutorial stick rect cached; dormant falling cobras skip the spine rebuild far from the monkey
- ramped difficulty in 3 stages, 1 per boss: 0~1 starting zone exit to the Cobra King, 1~2 b/t the 2 bosses (new `midBossRampCurve`), 2~ after the Spider King (`postBossRampCurve` starts at 2); Endless opens at 2; every hazard & cobra curve re-keyed to 0~3: old 1~2 -> 2~3, a new 1~2 dips a little then rises back to the old value at 1
- PC render pipeline: opaque texture off, nothing read it
- Spider King fight area moved down 50 u: zone 975~1050, trigger 1000 (was 1050), seen waiting from 945 u, so its height marker reads 1000 m
- hack keys regrouped: F1~F3 cheats, 1 & 2 jump to the Cobra King / Spider King fight (the spider jump was 9), 3~6 spawns, F8~F10 analytics, F12 the switch (was F9); F4~F7 & F11 stay free
- analytics titles tell the bosses apart: "Boss Fight Start" & "Boss Revive" → "Cobra Fight Start" & "Cobra Revive" (enum names unchanged)

##### Yuqing Lu

- pause & lose screens: Exit goes back to the level selection, not the title
- Made with Unity splash screen off
- level selection in the UI style: the loading page's vines, a logo-style title, gold buttons, the unlock hint under Endless Mode instead of over it, W and S side by side in the key hint
- title start prompt & the loading page's "Loading" in Baloo 2, so the hand-over keeps one font
- in-game HUD in the UI style: a smaller gold pause button, its Esc hint and the coconut counter laid out from the top-right corner so they never overlap; the progress bar wears the loading page's monkey and cobra heads, the cobra's neck its green segments; Baloo 2 energy, popups & tutorial; the score on a see-through plate with a "Score" caption, level with the pause button; popups on the same see-through plate, so the monkey's arm shows behind a grab hint; Space key icons as keycaps
- hack key list (F1–F12) hidden at start, F9 shows or hides it
- pause & lose screens in the UI style: square gold buttons with their words beside them and their keys on keycaps, logo-style titles; the lose screen's run card & leaderboard as matching plates with the key hint row under both; the boss fight's reward panel, revive card & badges in the UI palette

### Deprecated

### Removed

##### Erik

- dead assets: `_Banana.mat`, `SlipperyTutorialBand.asset`, `FreeGUI-Button.png`, `RoughBark_Albedo/_Normal.png`, empty `Assets/Resources/`

##### Yuqing Lu

- the tutorial's Noto Sans TC subset font and the old popup plate sprite: Baloo 2 is the UI font, with 3 accent fonts back (see Added)

### Fixed

##### Erik

- the first coconut & pineapple (the guaranteed ones) grow on the monkey's side of the trunk, within a configurable angle (`guaranteedBranchYawSpreadDeg` in `WorldBalanceConfig`), never on the far side

##### Yuqing Lu

- lose screen: the Download Analytic button is big enough for its words, and the key hint row fits at 4:3, now under both panels
- lose screen: the Restart and Exit buttons have their words back
- progress bar faces no longer squashed: their counter-stretch now matches the bar's
- progress bar: the cobra stays inside the bar as it reaches the monkey, and its body fills the bar from the bottom instead of hanging out below

### Security

[unreleased]: https://github.com/kami-lel/monkobra/compare/v1.0.0-beta.3...dev













## [1.0.0-beta.3] - 2026-11-11

### Added

##### Erik

- touch screen play: the first touch switches the game to touch mode, a real key press switches it back (`ControlStyleManager`, kept across scenes); in touch mode a joystick bottom left moves the monkey and the rest of the screen is the Space key (hold to reach, double-tap for a coconut, tap rapidly to escape a web, tap to start)
- phones & tablets, mobile browsers incl., start in touch mode, so the first tap counts; the WebGL page leaves touch gestures to the game
- virtual joystick sprites by rmeldev (CC0), credited in `CREDITS.md`
- pause button, top right, by click or tap, a square icon button; in Keyboard style the Esc key icon & "to Pause" sit beside it
- animated hint icons, one per control style (`HintIconSlot`), in the tutorial, popups, coconut hint & boss prompt; the touch tutorial shows a blue action area over the touch zone
- menu buttons from the FreeGUI sheet by pzUH (CC0), credited in `CREDITS.md`: blue, green on hover or key selection, red on press, grey when locked
- pause screen: square Resume & Restart buttons, with the Esc & R key icons in Keyboard style
- Exit to Title is the square `HUDExit_*` icon button on the pause & lose screens, and the lose screen's Restart is the square `HUDRestart_*` button with the R key icon in Keyboard style
- pause & lose screens: the "W / S to select, Space to confirm" text is the level select's `LevelSelectHint_Keyboard` / `_Touch` icon hints, switched by control style
- pause & lose screens: each option is a 3-column row, key icon, square button, text label (Resume, Restart, Exit); the key column hides in Touch style
- every key hint (tutorial, pause, lose & level select screens, coconut how-to, popups, keycaps, boss buttons) follows the control style live
- E & Enter join Space on every confirm: grab, coconut, web escape, tutorial start, menus, title & boss panels; P backs out of level select like Esc
- `PersistentBootstrap` creates `Analytics` & `ControlStyleManager` from `Resources/` before the first scene, so any scene plays with both; scene copies stay, the duplicate warning is expected
- [Input](docs/input-doc.md): the control scheme, Preferred vs Support keys per control style

### Changed

##### Erik

- title starts on Space, E, Enter, a click or a tap, no longer on any key
- boss revive, ad skip & reward panels read input actions, not raw keys; the course page is button only, Enter no longer opens it
- hints teach the Preferred input only: icons replace key names, and P, arrows, E & Enter are never drawn

### Deprecated

### Removed

##### Erik

- "Esc or P to Pause" label, the pause screen's key text & the keycap icon table (`Scripts/InputIcons/*`)

### Fixed

##### Erik

- HUD graphics & the tutorial overlay swallowed taps meant for the touch zone

### Security

[1.0.0-beta.3]: https://github.com/kami-lel/monkobra/compare/v1.0.0-beta.2...v1.0.0-beta.3













## [1.0.0-beta.2] - 2026-11-10

### Added

##### Erik

- green cobra proximity vignette: grows from bottom half to full ring as the Great Cobra closes in, beating faster per stage; hidden in the starting zone & while the cobra is frozen, guarding or the monkey leaps
- stamina & cobra vignettes alternate, never overlap

### Changed

##### Erik

- mock analytics session simulates real play: climb, hazards, fruit, cobra chase, boss fight & score follow the real rules, so death heights, durations & scores read like a playtest
- Great Cobra speed scales w/ gap to monkey: far-behind cobra runs faster, close one slower; `cobraBaseSpeedCurve` now a 0~2 multiplier of monkey top climb speed, new `cobraDistanceSpeedCurve`

##### Wenhai Dong

- second boss, the Spider King, at 1050 u (zone 1025~1100, bare like the cobra's): rip glowing silk knots by grabbing (x2 damage in its territory ring), it re-spins knots near itself; web shot leaves sticky webs, silk hook drag (tap Space / grab to escape), cocoon on a trapped monkey, leg swipe in its territory; grabbed bananas are tossed to the far side as a lure; the Cobra King armor carries over if unused; win gives Silk Gloves (webs break 2x faster); the Spider King is already seen waiting from 995 u; for testing, `isTestAtStart` (on for now) begins the fight at 30 u
- Spider King redone: starts w/ the gold armor swirled on (no words), a golden melon now & then gives it back; parries hurt it -1 & play a clash: the thing slows into the glowing tail, a white pop, shock rings & spark streaks, held grinding sparks, then it speeds back w/ a gold trail, all in a 1 s slow motion; the parry spot is closer to the monkey (gold at 2.8 u, batted at 1.6 u); the spent armor shatters off instead of hanging there; a dash is never called off once its red lane shows (its lane cut to the arena); the parry counts from the double tap; the dash lane shows exactly its run plus its catch reach (no end cap); a harder clash: a snap in, a dead stop w/ a camera punch in & shake, crossing white strokes, then a kick off; a parry readies the tail at once so all 3 slashes can be met, a whiff waits 0.55 s; a plainer charge (red rings drawn in round its head; no growing or red glow); the parry is met at the monkey's middle; the armor save is as in the Cobra King's arena (a knock down, the spider not thrown off); laid webs last 10 s; slash blades 4.5 u wide & faster (26 u/s), their hit swept so none skips past; never from closer than 6 u; the fight gets harder in 3 stages (at half & four fifths of its health lost) (web volleys of 2 then 3 bullets; slashes 1, then 2, then 3 in a row) & smoothly faster as its health goes; touching the spider plays a bite w/ red rings & "Don't touch it!"; a revive puts the armor back on w/ its swirl; plain K.O. words; the dash keeps exactly to its red lane; its arena is bare like a boss zone (also the test fight, lifted after the win); no webs at the start, the spider spins a trail of webs all the way it crawls (one per 1.5 u), each gone after 10 s; it starts right above the monkey & chases it (faster, 5.5 u/s), any touch of it is fatal; attacks: one homing silk bullet (24 u/s), charged only in view (it rushes into view first), only parried by double-tapping Space as each turns gold, the tail swings only w/ a bullet coming, so grabs never hitch (the monkey's real tail swings; the spider is stuck a fixed time however many come back), a dash from 7 u along a red lane that locks 0.45 s before it goes, and from afar a slash: after a seen charge, 3 silk blades flying fast along the bark at the monkey, each after a short red aim line, slow-fast or fast-slow, parryable as they turn gold & hurting the spider when batted back; the tail swing has a wind up & follow through instead of a cooldown; a parry plays in slow motion w/ a camera close up; the slash parry is taught right after the web one; killed by thrown melons (20 health), which are there from the start; only web bullets till the first parry (it stops time to teach it), then the fight; the first slash stops time once too; volley gaps 0.7/1.1 s (never under 0.6 s) so each bullet can be batted back; slashes from 4 u, likelier from afar; it keeps 8 u off & dashes only within 5 u, every 2.5 s (1.6 s late); stuck is stuck; nothing hurts in the first lesson; a touch the armor saves throws the monkey 7 u off, out of webs; the golden melon has the Cobra King's gold outline & waits 5 s after the armor goes, then 15 s; the armor takes one hit w/o a 2 s grace; a revive offers EASY / NORMAL as the Cobra King's; the loss is only the K.O.; the golden melon now really shows, as the Cobra King's; rope pull, giant web, knots & poison removed
- boss height markers: the Cobra King's look-alike now coils round its marker's plate (both ends & along the top, slithering, head swaying) & the Spider King's 1,050 m marker gets the red beating plate w/ a spider perched on it, legs gripping, flexing & tapping (`BossMarkerFigure`)
- Spider King UI in the UI style (`UIStyle`): its call out on a Panel plate, bold; its gold banners & pops in the UI Gold
- spider jump hack key moved from F9 to 9 (`HackAction.SpiderJump` = 12 after `ToggleHints`), as the UI style branch takes F9 for the hack key list
- Spider King: the gloves & sleeves plain silver, shining softly as the armor does; its screen flashes as the Cobra King's (white at the start & the winning blow, red on the K.O., gold on a spent armor); no flash on a revive or a reward snapping on, in either king's fight; the Spider King's last blow slow motion as the Cobra King's (0.2x for 1.4 s real)
- Spider King: the silk gloves are bigger & a silk sleeve grows up each whole arm, stretching w/ it (`ArmController.ArmVisual`)
- Spider King: no "Ouch!" line on a hit; on the win the Silk Gloves swirl onto the monkey's hands (spider style: dark, silk cuffs & web, red claws) as the Cobra King's armor does, & stay on, b4 the reward panel
- Spider King: the guard sits lower (20 u below the entry, was 14 u), the spider crawls down to 6 u below the entry (was 4 u); no GOLDEN ARMOR badge at the bottom right any more (either king)
- fix: a Spider King lesson stop w/ the monkey stuck in a web could never be passed (the double tap only struggled); the web now lets go as time stops
- Spider King: its slash charge looks dangerous & unlike the web shot's (blazing red eyes, a flickering red blade sign over its head, red rings & streaks bursting out); the web shot's charge rings are white now
- boss win panel plain again, as the Cobra King's (only a 0.5 s guard against a mashed Space)
- Spider King: touching it no longer kills, only its attacks hurt
- fix: the kept armor's save on the climb after showed nothing when the Cobra King was skipped (F9, endless start: its UI was never built); now "Saved!", the gold flash & badge always show, & the armor shatters off in gold bursts as the cobra is thrown back
- Spider King: golden melons come in pairs, one either side of the monkey; grabbing either gives the armor & both go
- Spider King: the monkey is 1.2x as fast in its arena (`MonkeyController.ArenaSpeedScale`), normal again after the win
- Spider King: calmer still early on (rest 3.2 s at full health, still about 0.9 s at the end; dash every 3.2 s early); the test fight at the start is off, the fight is back at 1050 u w/ the climb's own growth; F9 jumps to just below it (the Cobra King counted as beaten)
- Spider King: 15 health (was 20), its stages still at half & four fifths of it lost; an attack begun b4 a call out is played out, only new ones wait
- Spider King: a batted back web bullet no longer hurts the spider (only sticks it); the rapid / triple fire call out stays docked under the health bar for the stage; no attack while it calls out
- Spider King: a stage of volleys is called out once ("Rapid fire ready!" / "Triple fire ready!"), the line gliding from its head up under the health bar (`BossFightHud.CallOut`); each bullet of a volley charges again at its head (the orb swells, sparks & rings drawn in) through the gap b4 it
- Spider King: no red banner at a new stage (only its speech bubble); the golden melon shows at once when the armor goes, if missed again 5 s later, then every 10 s
- Spider King: a stage w/ volleys of 2 or more opens w/ at least 2 volleys b4 any slash; the golden melon comes every 10 s (was 15 s)
- Spider King: its chase speeds up less late in the fight (to 1.08x, attacks still to 1.3x); a ranged attack needs it well inside the view (18% in from the screen's edges, clearly on the near side); the stage banner has no second line
- Spider King: a new stage is called out w/ a red banner too ("You'll pay for that!" / "It attacks faster now", "ENOUGH!" / "It's furious!"); a dash the armor saves throws the monkey 8 u sideways off the dash lane, so it is not touched right after
- Spider King: a parry w/ more attacks coming plays only a short slow beat & a slight camera move, & a full parry shot is cut short once another attack comes, so a 2nd slash or bullet is always seen in time; it circles slower b/t attacks (0.4 of its stalk speed)
- Spider King: calmer early on (rest b/t attacks 2.6 s at full health, still about 0.9 s at the end; dash every 2.8 s early, 1.5 s late); b/t attacks it circles the monkey instead of standing still
- fix: Spider King parry clash fx face the camera as drawn in the parry shot (they faced the rest camera, so the white strokes & rings showed as big slanted white bars up close); the clash's white bits smaller & fewer (strokes, pop, rings), its second ring orange
- Spider King: the air wall no longer veils the view near it (far sheet only faintly tinted, a clearer patch round the monkey, hidden w/ the camera up at it); the parry shot a bit farther out (4.6 u)
- Spider King: the air wall fades in only near it & spreads out to the horizon; a revive clears the last try's golden melon; the bare area reaches down past the guard, so no climb fruit grows in the boss area; a parry gets its own camera shot (swoop in close, swing round & tilt, slam & shake at the clash, pan after the kicked back thing); a stuck spider still fires out its volley; it keeps 4.8 u off & dashes every 2.2 s (1.5 s late), so dashes come more often
- Spider King: the arena's cap is a plain see-through sheet; one "Nice!" per lesson parry (the lesson's own banner dropped); parries are timed to the monkey's edge facing the attack, not its middle, & met there
- Spider King: the arena's cap shown as a web over it that shows as the monkey nears & ripples on a bump; arena 18 u high w/ the guard 14 u below, a bit bigger than the Cobra King's; parries -1 & pop "Nice!"; it keeps 5.5 u off & dashes within 5 u
- fix: Spider King tuning never reached the game (the prefab kept the values from when it was last imported); every value is now written out in `SpiderFight.prefab`; checked against the asked tuning: rest b/t attacks back to 1.8 s & cocoon crawl to 3 u/s, both lost in a rewrite
- fix: fruit destroyed in reach (eg a segment recycled after a teleport) no longer breaks `UpwardFruitSensor` & every grab after it
- boss fight difficulty restored to pre-bare-zone tuning: arena grows own branches, rough bark & webs like the climb; `bossMaxHealth` back to 12
- boss fight: tail sweep joins at 1/5 king health lost, not 1/3; arena watermelons regrow slower (6 → 9 s) & are removed after the win; golden watermelon banner; webs never overlap
- boss fight: minion call never lost, repeated after a revive

### Removed

##### Erik

- Great Cobra close-range slow-down ⇐ distance curve replaces it

[1.0.0-beta.2]: https://github.com/kami-lel/monkobra/compare/v1.0.0-beta...v1.0.0-beta.2













## [1.0.0-beta] - 2026-11-10

### Added

##### Belle Dai

- rough bark: broad, raised, wavy-edged patches that hug the faceted trunk & smoothly slow climbing & circling alike while touched; overlaps never stack the slowdown (`RoughBarkZone`)
- rough bark placement: patches rolled per tree segment off the ramped difficulty, arcs kept apart so they never close into a full ring, so a path round always stays open (`RoughBarkSpawner`)
- rough bark art: painted albedo & normal map textures on the `RoughBark` material
- slippery bark prototype, reworked into rough bark before release: wet patches w/ a guaranteed tutorial band, a short eased slide & loosened grip that slowed the arm stroke, own test scene, an Editor installer & an automated play-mode smoke check driving the real monkey

##### Erik

- scene system: title, level select (Normal Play, Endless Mode, Back), `Lv1Scene` & `EndlessScene`; `SceneNames.Load` loads every scene
- Endless Mode: starts at the tree base in the post boss state, difficulty keeps ramping; unlocks once the Cobra King is beaten (Editor menu `Monkobra/Unlock Endless`, `Monkobra/Reset Progress`)
- pause & lose screens: Resume, Restart, Exit to Title by keys, mouse hover or click (`RunScreenMenu`)
- analytics: 1 session file per launch across scene loads, runs tagged Full or Endless; every run event is an `AnalyticsMetric`, so a new metric is 1 entry; records leaps, rough bark, freezes, pauses, boss fight events & the Starting Zone Exit
- dev builds only: Download Analytics button on the lose screen; hack keys F1~F12 for no-clip, max stamina, speed x3, boss jump, spawning a pineapple branch, coconut branch, falling cobra or spider web, report download, new session & mock session
- `Lv1Scene` split into nested prefabs, so teammates edit different files
- Great Cobra exit boost after the monkey leaves the starting zone, w/ "Run! The Great Cobra is chasing you!" warning
- climb speed builds up along curves of key hold time; branches cleared round the monkey before the run is playing
- fruit fly: a grabbed fruit's sprite flies to the stamina bar, coconut HUD or monkey
- pineapple leap: a grab pulls the monkey up, hazards off for the leap & a short landing grace
- coconut throw: a double-tap spends 1, it falls & freezes the cobra on touch or after a fixed distance; brown coconut model & new fruit sprites, credited in `CREDITS.md`
- consistent pop up UI: every center message is a variant of `PopUp/_PopUpBase`, shown 1 at a time by `PopUpManager` w/ priority & queue, see `docs/pop-up-doc.md`
- tree bark texture on trunk segments, credited in `CREDITS.md`
- camera redesign: every gameplay camera tracks `CameraAnchor`, so strafing no longer spins the view; climb framing, hold curves & noise on rough bark; all tuning in `CameraConfig`
- shot director (`CameraRigManager`): coconut, cobra alert & pineapple leap shots by priority, so events never chain into rapid cuts
- height marker pass trigger (`HeightMarkerTrigger`)
- difficulty: the post boss ramp keeps rising after the fight; a boss zone holds only the boss fight; 4 branch kinds (empty, banana, coconut, pineapple) w/ own chance curves; a guaranteed pineapple & coconut branch at fixed heights

##### Wenhai Dong

- Cobra King boss fight (`Boss/BossFight` prefab): at the trigger height the run enters an arena; the king coils round the tree top, alternates rests with attack rounds (bite, tail sweep, minion waves, head butt) that grow with each lost third of its health
- boss arena: banana branches whose fruit is also thrown at the crown, spider webs, a golden banana that refills stamina & shields 1 fatal hit; the monkey's climb is capped below the king, the great cobra guards below
- boss fight presentation: danger zone warnings, health bar & speech, first-throw key prompt, slow-motion K.O., camera pulls back & tilts up
- boss fight win: the king shatters, a reward panel shows, the monkey keeps a Golden Armor that absorbs 1 great cobra catch, the run goes on
- boss fight revive: every loss offers a revive for watching a mock ad; after a revive the first throw picks EASY or NORMAL
- boss fight height marker: a bigger, red, beating plate w/ a Cobra King look-alike, hidden once the fight begins
- `GameState.Won`: a won run settles the score & analytics records it as `RunStatus.Won`
- pause: Esc or P pauses or resumes a live run, R restarts it; time freezes, audio pauses, a `PauseScreen` panel shows
- tree bark less glossy but still crisp at steep angles

##### Yuqing Lu

- coconut: a rare fruit grabbed like a banana, scores w/ a multiplier & restores no stamina
- coconut use: a quick double-tap freezes the great cobra for a while; refused while paused, trapped in a web or already frozen; the cobra turns light blue while frozen
- coconut HUD: count, freeze countdown & bar, a double-tap hint until the first use, center messages; coconut analytics per run
- green coconut model & matching UI sprite from the PolyOne Free Fruits pack, credited in `CREDITS.md`
- trunk height markers: every 50 u a number beside the trunk in m on a dark rounded plate, 1 on each trunk edge as seen from the camera; the marker grows & brightens the first time the monkey passes it

##### Houdong Pan

- lose screen leaderboard: 9 rivals named from a joke pool (BananaKing, PeelMeDaddy, CSCI526, ~) w/ scores rolled in a range, ranked w/ this run's final score, ties going to the player, & a "YOUR RANK #n / 10" line (`LeaderboardWidget`, `Leaderboard` & `LeaderboardRow` prefabs)
- rivals stay fixed for the whole playtest, so every restart faces the same board; rolled on their own random generator, so hazard spawns are untouched (`SessionLeaderboard`)
- fruit grab prompt: a keycap "space, hold, then release" follows the fruit in reach & teaches the first grab of each fruit kind per run, hiding while a screen is open (`FruitGrabPrompt`)
- reusable keycap widget: the boss fight's keycap pulled out into 1 runtime widget, built in code w/o sprites or prefabs, now shared by the fruit tutorial & boss HUD (`KeyPromptWidget`)
- `ArmController.FruitGrabbed` event after every successful grab, for tutorials & HUD to listen to

### Changed

##### Belle Dai

- falling cobra strikes once: after dropping the monkey it switches itself off, so it can neither keep falling nor hit again; the monkey sensor & the snake share 1 hit guard, so a contact never counts twice (`FallingCobraController`)

##### Erik

- starting zone: opens at lower stamina, exhaustion slows less, idle recovery slower, exit trigger raised; Starter Zone → Starting Zone
- pause & restart are `InputSystem_Actions` actions read by `GameController`
- fruit grab aims at the closest banana; stamina restore & bar punch land when the fruit's flight arrives
- scripts renamed to house role suffixes & code style pass over all scripts; no behavior change
- prefab restructure: `World` & `Systems` wrappers dropped, `Analytics`, `InGameHUD` & `PopUps` split out; `CoconutManager` centralizes the coconut count, throw & HUD
- banana model: `SM_Fruits_Banana_Y1` replaces the stretched capsules
- config reorganization: `GameBalanceConfig` split into `PlayerBalanceConfig`, `WorldBalanceConfig` & `CobraBalanceConfig`, `MonkeyConfig` into arm & input configs, `MovementConfig` → `MonkeyClimbConfig`, `Settings/` 1 folder per module; every value carried over
- camera: `CameraEfxConfig` → `CameraConfig`, `CameraLookUp` → `CameraClimbFraming`; low stamina pulse vignette only; hit shake scaled per kind; `CameraRigManager` no longer toggles the shared Interact action
- `DynamicTreeSegmentPool` holds more segments & looks further ahead
- height markers show up to 50 u ahead & pulse on touching their trigger
- ramped difficulty runs 0 at the starting zone exit to 1 at the boss fight; first appearances follow height
- first-grab fruit popup also detects an unlearned fruit nearby; falling cobra stops against tree branches; boss arena fruit shows the watermelon model

##### Yuqing Lu

- `Coconut` prefab: from banana-shaped placeholder to the coconut item w/ its own score kind & outlines
- `ScoreManager` scores & counts each pickup by `PickupKind`; a coconut's flight restores no stamina; analytics splits reward score into banana & coconut score
- progress bar cobra gap in m, matching the height markers; `unitsPerMeter` now only sets the distance score rate

##### Houdong Pan

- progress bar redrawn: monkey & cobra faces drawn from 1 generated antialiased circle, no imported art; an upright distance readout & bracket span both markers; the cobra face slides aside when the 2 get close (`ProgressBarWidget`, `ProgressBar` prefab)
- tutorial & HUD wording: "Grab Banana" → "Grab Fruit", low stamina hint reads "Low Energy"

### Removed

##### Erik

- `devYuqingCoconutScene` & `SlipTestScene` personal dev scenes
- `upSpeedU`, `downSpeedU` & `accelerationU`, replaced by the climb speed curves
- orphan `GoalText` object in `Lv1Scene`
- look-left & look-right cameras: a grab shifts the climb view instead of cutting
- cobra alert on surges, falling cobra hits & proximity
- `CoconutSpawner` & the `CoconutSystem` prefab, replaced by the coconut branch kind; fixed coconut start height, rough bark score gate & forced first patch: the ramp decides

##### Yuqing Lu

- HUD Height label: the trunk height markers show the climbed height

### Fixed

##### Erik

- restart could spawn the monkey inside a branch
- analytics dropped starting zone data; a run now opens when the zone begins
- camera dizziness: world spin on strafe, background judder, side flip while grab held, nod on every up press
- hit shake reached only the behind view; stamina pulse could leave a widened FOV on the old camera

##### Yuqing Lu

- coconuts never spawned in `Lv1Scene` ⇐ `CoconutSystem` prefab missing under `Envs`

##### Wenhai Dong

- boss fight: an armored monkey could drop past the guarding great cobra & never climb back alive; the armor's save now flies it back to the fight's start, untouchable on the way
- boss fight: pineapples still grew in the arena ⇐ they score as bananas; now told apart by `LeapOnGrab`
- a branch hit in the starting zone showed "The cobra is speeding up!" ⇐ mistakes counted as surges there

[1.0.0-beta]: https://github.com/kami-lel/monkobra/compare/v1.0.0-alpha.2...v1.0.0-beta













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

- keyboard-style tutorial controls: a WASD cluster & wide Space key built from editable uGUI rectangles, no sprites, w/ inset keycap frames, bevel lines & a caption per key
- tutorial UI font: a static subset of Noto Sans TC holding ASCII & the Chinese glyphs used, so WebGL needs no system font; shipped w/ its SIL Open Font License

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

##### Houdong Pan

- more falling cobras: 3 spawn attempts per segment (was 2), & the chance curve climbs to full instead of topping out at 0.4

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

- falling cobra: a new hazard that perches on a real branch just above the monkey's path round the trunk, flashes a red warning, then drops on it; built w/ its own `FallingCobraDemo` test scene, then placed in `Lv1Scene`
- falling cobra spawns grow more frequent the higher the monkey climbs
- falling cobra hit: 1 trigger covers the whole snake, so a hit makes 1 drop & 1 camera shake, not 1 per body segment

##### Houdong Pan

- scoring system: points per meter of new best height only, so falling & re-climbing earns nothing, plus a reward per banana, shown live on a HUD score label; the score freezes when the run ends (`ScoreManager`, `ScoreDisplay`)
- each fruit pays its reward exactly once, even when both hands claim it, & never after the run ends (`ScorePickup`, `ScoreReward`)
- `ScoreConfig` asset: points per meter & units per meter; rewards open to future sources through `ScoreManager.TryAddReward`

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
