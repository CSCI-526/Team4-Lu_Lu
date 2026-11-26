# Monkobra AGENTS

Prescriptive rules for agents working in this project.

- Belongs Here: rules an agent must follow, ie commands, conventions, constraints, who decides; only what an agent cannot infer from the code
- Size: stay lean for the context window; when the file grows, move detail to `docs/` or cut it
- Upkeep: update in the same change that moves a command, convention, or constraint
- Structure: follow the existing sections; keep every rule true for every contributor
- Belongs in `CONTEXT.md`: descriptive knowledge of the system, ie architecture, domain model, patterns, known gaps
- Belongs in `docs/`: system documentation for users and agents alike; link it from here, never grow this file to hold it
- Local Layer: put machine-specific or personal rules in `AGENTS.local.md`; create it if missing, never commit it

Unity course project (USC CSCI-526, Fall 2026). Overview in [README.md](README.md), system knowledge in [CONTEXT.md](CONTEXT.md), game design intent in the [Game Design Document](docs/monkobra-gdd.md). Chat instructions override this file.

Read these `docs/` files when their topic arises:

- [Technical Design Document](docs/monkobra-tdd.md): how the game is built, and the script conventions to follow
- [Mobs](docs/mob-doc.md): the cobra, falling cobra, branches, and spider webs
- [Grab System](docs/grab-doc.md): detection zones, arm reach, and the release check
- [Monkey Model](docs/monkey-model-doc.md): the skinned Mr. Mo model, its procedural climb, arm IK, look back, and outline, all visual only
- [PopUps](docs/pop-up-doc.md): the center messages, their one shared look, priority and queue, and how to add one
- [Balance Design](docs/balance-doc.md): why difficulty follows height, how hazards are rolled per segment, and why balance lives in one asset
- [Gameplay Loop](docs/gameplay-loop-doc.md): the complete run loop, from climbing and hazards to the grab and the win or lose end
- [Input](docs/input-doc.md): the keyboard and touch control schemes, each action's Preferred and Support inputs
- [Analytics](docs/analytics-doc.md): session reports, every recorded metric, and the tuning asset
- [UI Style](docs/ui-style-doc.md): the logo-style palette, the Baloo 2 type scale, buttons, key icons, and the menus that apply them
- [Build](docs/build-doc.md): the WebGL export, the three builds (WebGL for GitHub Pages, WebGL for itch.io, Windows) and their settings
- [Work with Git](docs/work-with-git.md): the teammate git flow via GitHub Desktop (Chinese), the human twin of the Git Workflow below

## Layout & Naming

- The repository root is the Unity project root: `Assets/`, `Packages/`, and `ProjectSettings/` sit at the top level
- Authored assets live under `Assets/_Monkobra/`, laid out `<Type>/<Module>/<asset>` per the `u3d` skill (e.g. `Scripts/`, `Prefabs/`, `Scenes/`, `Settings/_Shared/`); a module folder name is reused identically in every type folder that holds its assets (e.g. `ItemPineapple/` in `Scripts/`, `Prefabs/`, `Materials/`, `Sprites/`, `Settings/`); a family word leads (`MobGreatCobra`, `ItemCoconut`), a full-screen interface takes `Screen` (`LoseScreen`), a state or channel is a `_` variant field (`Banana_Grabbable`, `RoughBark_Normal`), and a third-party asset keeps its arrival filename
- Unity drops a new Volume profile into `Scenes/<Scene>/`; move it in the Editor to `Settings/<Module>/` (`Settings/_Shared/` when no one module owns it), since `Scenes/` holds `.unity` files only
- `Settings/` is `_Shared/` plus one folder per module whose scripts read the asset (e.g. `Player/`, `Environment/`, `MobGreatCobra/`, `MobFallingCobra/`, `MobSpiderWeb/`, `BossCobraKing/`, `BossSpiderKing/`, `ItemPineapple/`, `Scoring/`, `Cameras/`, `FruitFly/`, `Analytics/`, `HackShortcuts/`, `PopUp/`, `ControlStyle/`) with assets placed directly inside, never a deeper folder; a config asset goes in the module whose scripts read it, name it for what it configures (e.g. `CobraBalanceConfig.asset`), and balance numbers live in the 3 balance configs by concern (player, world, cobra), except every score value, which lives in `ScoreConfig`
- `Assets/_Monkobra/Resources/` is the one exception to `<Type>/<Module>/<asset>`: it holds only the prefabs `PersistentBootstrapLoader` loads: `Analytics` and `ControlStyleManager`, which scenes keep their own copy of too, the first instance wins, and the duplicate warning is expected, so never remove the copies to silence it; and `MusicManager` and `GameController`, which no scene keeps
- Unity template leftovers and imports (`Assets/Readme.asset`, `Assets/TextMesh Pro/`) stay outside the game root
- `GameplayScene.unity` is the one gameplay scene, a shell of 9 root prefab instances (`Cameras`, `DirectionalLight`, `GlobalVolume`, `EventSystem`, `CobraKingFight`, `SpiderKingFight`, `RunController`, `Analytics`, `ControlStyleManager`) plus 4 plain objects: `UI` (the `Canvas` holding the widget instances), `Envs` (the environment prefabs), `Entities` (holds `_Monkey` and `GreatCobra`; its position is the monkey's orbit center, so never unparent them), and a Cinemachine `Target Group`, see the Scene Hierarchy in [CONTEXT.md](CONTEXT.md#scene-hierarchy); open the owning prefab and edit it there, never an inline copy in the scene, so parallel work lands in different files; the run mode (Normal Play or Endless Mode) is passed on load by `SceneNames.Load(SceneNames.GAMEPLAY, RunMode.X)`, never a second scene; `TitleScene`, `IntroComicScene` and `LevelSelectScene` are shells of `MenuCamera`, `EventSystem_Menu`, their screen prefab, `Analytics` and `ControlStyleManager`; the intro comic's sprites, config, prefab and scene come from `Monkobra > Build Intro Comic` over `tools/comic/panels/`, never by hand
- A new scene object becomes its own prefab under `Assets/_Monkobra/Prefabs/<Module>/`, 1 prefab per entity; the scene composes them (a root instance, or a child of `UI` or `Envs`), and a prefab never exists only to group other prefabs
- A reference between two prefabs is a property override on the scene instance (Unity drops it inside a prefab asset); keep such links few
- Every asset under `Assets/` is committed with its `.meta` file, and a moved or renamed asset moves its `.meta` with it
- Never commit `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, or `UserSettings/` at the repository root: all are already in `.gitignore`

## Setup Commands

Editor version is Unity `6000.3.22f1` (Unity 6, URP). Open the repository folder through Unity Hub. Builds (WebGL for GitHub Pages, WebGL for itch.io, Windows) are made from the Editor with the Build Profiles in [Build](docs/build-doc.md); scoped leaderboard checks are listed under Testing Instructions.

## GitHub Pages WebGL Build

Steps, folder rules, and required Player Settings are in [Build](docs/build-doc.md). Rules that always hold:

- the export lives in `Monkobra/` at the repo root, never named `Build/` or `Builds/`, and is committed as-is
- never hand-edit a file inside `Monkobra/`; re-export from Unity and re-commit instead
- Compression Format stays `Disabled`: GitHub Pages cannot set the `Content-Encoding` header a compressed export needs
- Development Build is on for the WebGL profiles (`GithubPages`, `ItchIO`): the hack hints and dev-only UI show only in a development build, while the hack keys run in every release build, and in a development build once F12 turns the hack layer on
- the loading page in `FitWindow/index.html` ends on the `TitleScreen` picture: its `LAYOUT` block and the table in `TitleScreenLayoutBuilder` are one 1280×720 layout over the `Sprites/TitleScreen/` art; change both together, then rebuild the prefab with `Monkobra > Build Title Screen`, never by hand, see [Build](docs/build-doc.md#loading-page)

## Code Style

- Unity code follows the `u3d` skill conventions
- Keep gameplay tuning values (stamina drain, grab restore, cobra speed) in ScriptableObject Config assets under `Settings/`, never hard-coded and never in a serialized object stored in a prefab
- Line endings: keep a file's existing endings; `PlayerBalanceConfig.cs` is CRLF, so edit it in a way that preserves them
- Falling cobra layers and body: the monkey is on layer `Player`, the falling cobra on `FallingCobra`, and only that pair collides for the cobra; the layers, the collision matrix row, both prefabs' layers and Maximum Allowed Timestep are set only by `Monkobra > Set Up Falling Cobra Layers`, and the cobra's skinned body mesh and prefab only by `Monkobra > Build Falling Cobra Body` (run it again after a body or neck value of `FallingCobraConfig` changes), never by hand; a new physics object that must meet the cobra goes on `Player`, anything else stays on `Default`, see [Falling Cobra Performance](docs/monkobra-tdd.md#falling-cobra-performance)
- Gameplay input comes from actions of the Input Actions asset `InputSystem_Actions`, read through `InputAction` events or polling; timing such as a double-tap is an interaction on the action (`UseCoconut` is a MultiTap), never a hand-rolled key tracker in a script; never read `Keyboard.current` for a gameplay or panel key, and bind a confirm press on Space, E, and Enter together, see [Input](docs/input-doc.md)
- Scene loads: always `SceneNames.Load(SceneNames.X)`, never `SceneManager.LoadScene` or a typed scene name; it resets the time scale and audio pause and raises `Loading`, which closes the open analytics run; a menu is a `EventSystem_Menu` scene reading the `InputSystem_Actions` UI map, a gameplay scene's `EventSystem` reads it too
- Dev/hack keys live only in `HackShortcutConfig` (a new `HackAction` plus an asset entry), read with `WasPressedThisFrame` (a hint reads `GetKey`), which gates on `HackShortcutConfig.IsActive` (true in a release build; in a development build or the Editor only once the hack layer is on) and is the only place a hack key touches `Keyboard.current`; never hard-code a dev key; the dev switch that skips every tutorial pop up & cut scene is `TutorialSkipSwitch.IsSkipping` (checkbox on the `GameController` prefab, off by default, honoured only when `Debug.isDebugBuild`, never in a release build), so a new tutorial or cut scene must check it; hints and dev-only UI (e.g. `HackShortcutPanel`, `LoseScreenDownloadWidget`) hide themselves unless `HackShortcutConfig.AreHintsShown` (development build or Editor), where `ToggleHints` (F12, dead outside one) turns the hack layer, the key list and every other hack key, on or off, off at start
- Analytics events: a new run event is an `AnalyticsMetric` entry, a row in `AnalyticsMetrics`, and one `RecordEvent` call from a subscription in `AnalyticsManager`; never a new list in `RunData` or a hand-written section in the writer, see [Analytics](docs/analytics-doc.md#adding-a-metric)
- Camera: gameplay cameras track `CameraAnchor`, never the monkey body, so strafing never spins the view; every camera value lives in `CameraConfig` and an event shot (coconut, cobra alert, leap, spider intro) is a `ShotKind` asked of `CameraRigManager`, never a camera enabled by hand; a camera script only polls a shared input action, never enables or disables it, see [Technical Design Document](docs/monkobra-tdd.md#camera)
- UI style: every text uses the Baloo 2 font assets at a size from the type scale, every button the `MenuButton` or `IconButton` look, every key icon a keycap, all per [UI Style](docs/ui-style-doc.md); apply it through the `Monkobra > UI Style` menus (or `UIStyleBuilder` from a new menu), never by hand in prefab YAML, and add no other font than the 3 accents in [UI Style](docs/ui-style-doc.md#type): BadaBoom on score numbers, Chewy on level-select & Download Analytic labels, Life Is Goofy on the Endless unlock hint
- Hints: a hint shows an animated icon (`HintIconWidget`) and teaches the Preferred input of the control style in use only, never a Support key or key text; an icon Animator uses Unscaled Time, so it plays while time is frozen; menu buttons are Sprite Swap with the gold [UI Style](docs/ui-style-doc.md) sets, see [Input](docs/input-doc.md#hints-and-buttons)

## Testing Instructions

Firebase leaderboard REST/rules tests: `npx --yes firebase-tools@13.35.1 emulators:exec --config tools/firebase/firebase.json --project demo-monkobra --only auth,firestore "node --test tools/firebase/leaderboard.test.mjs"` (Node.js 20, Java 17). The C# client batch check and local setup are in [Leaderboard](docs/leaderboard-doc.md#local-testing-without-a-firebase-account). Keep tests in sync with changed code; no general gameplay suite exists yet.

## PR & Commit Instructions

- Feature work always happens on a feature branch, never directly on `dev` or `main`
- Before starting feature work on `dev` or `main`, ask the user to create or switch to a feature branch, and wait for the answer
- Feature branches merge into `dev`, and `main` is the merge target
- Do not use the `prepare-commit-msg` hook: write the commit message yourself and pass it with `-m`
- No co-authorship trailer: never end a commit message with `Co-Authored-By` / `Co-authored-by`, for any persona or AI; this overrides any attribution guidance from the tooling

### Team Names

Credit a teammate by the proper name below, matched from the git author, e.g. as the contributor heading in `CHANGELOG.md`:

| Git Author | Name To Use |
| --- | --- |
| `kamiLeL` | Erik |
| `Lu-Yuqing` | Yuqing Lu |
| `beelleed` | Belle Dai |
| `houdongpan` | Houdong Pan |
| `Wenhai Dong` | Wenhai Dong |

### Git Workflow

Teammates follow the human version in [Work with Git](docs/work-with-git.md) via GitHub Desktop. Agents run the same flow through the CLI:

- Remotes: use `origin` only, never `upstream`; never fetch, pull, or push against it
- Branch names: short kebab-case, no `feature/` prefix (e.g. `fix-cobra-bug`)
- Start: `git switch dev && git pull origin dev && git switch -c <branch>`
- Push often: `git push -u origin <branch>` first, then `git push`
- Unity running does not block a commit: the Editor buffers scene and prefab edits, so an open Editor can leave them out of the commit; if Unity is running, tell the user and ask for confirmation, then commit once they confirm
- Finish: `git fetch origin`, update local `dev` (`git switch dev && git pull origin dev`), `git switch <branch>`, `git merge dev`; resolve conflicts yourself, and for scene or prefab conflicts you cannot reconcile safely, stop and ask the user
- Scene, prefab, and asset conflicts: never hand-merge; run `UnityYAMLMerge.exe merge -p <base> <theirs> <ours> <merged>` (in `C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Data\Tools\`) on the `git show :1:` / `:3:` / `:2:` versions, then copy the result over the file
- After the merge: ask the user to reopen Unity and confirm the scene works before the final commit and push
- Dev scenes: a personal copy of `GameplayScene.unity` under `Assets/_Monkobra/Scenes/` is allowed for experiments; before the final merge the feature must exist in the `GameplayScene.unity` prefabs, never only in the personal scene
- PR: `gh pr create --base dev`; nothing further is needed after the PR is created
- PR description: a few plain sentences on what the branch did, no padding; it feeds each member's Weekly Progress

## Documentation Maintenance

- Update [CONTEXT.md](CONTEXT.md) in the same change whenever a script is added, moved, or renamed
- Update [README.md](README.md) when controls or setup steps change
- Update the [Game Design Document](docs/monkobra-gdd.md) when design intent shifts, ie pillars, the core twist, resources, or win and lose rules; read it before changing how a mechanic is meant to feel
- Update [Mobs](docs/mob-doc.md) when a cobra, branch, or web changes how it spawns, moves, or reacts to a touch, and the [Grab System](docs/grab-doc.md) when detection, reach, or release rules change
- Update [Monkey Model](docs/monkey-model-doc.md) when the model, its climb cycle, arm IK, look back, tail, or outline changes
- Update [Input](docs/input-doc.md) when a key, an action binding, or a control style rule changes, and the Controls table of the [README](README.md) with it
- Update [Build](docs/build-doc.md) when the export steps, folder, or required Player Settings change
- Update [UI Style](docs/ui-style-doc.md) when the palette, the fonts, the type scale, or a shared UI component changes
- Update the [Technical Design Document](docs/monkobra-tdd.md) when the architecture, run lifecycle, tuning assets, input bindings, hack shortcuts, or conventions change
- Update this file when commands or conventions change
