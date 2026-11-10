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
- [PopUps](docs/pop-up-doc.md): the center messages, their one shared look, priority and queue, and how to add one
- [Balance Design](docs/balance-doc.md): why difficulty follows height, how hazards are rolled per segment, and why balance lives in one asset
- [Gameplay Loop](docs/gameplay-loop-doc.md): the complete run loop, from climbing and hazards to the grab and the win or lose end
- [Analytics](docs/analytics-doc.md): session reports, every recorded metric, and the tuning asset
- [Build](docs/build-doc.md): the WebGL export, the three builds (WebGL for GitHub Pages, WebGL for itch.io, Windows) and their settings
- [Work with Git](docs/work-with-git.md): the teammate git flow via GitHub Desktop (Chinese), the human twin of the Git Workflow below

## Layout & Naming

- The repository root is the Unity project root: `Assets/`, `Packages/`, and `ProjectSettings/` sit at the top level
- Authored assets live under `Assets/_Monkobra/`, laid out `<Type>/<Module>/<asset>` per the `coder-unity-engine` skill (e.g. `Scripts/`, `Prefabs/`, `Scenes/`, `Settings/_Shared/`); a module folder name is reused identically in every type folder that holds its assets (e.g. `ItemPineapple/` in `Scripts/`, `Prefabs/`, `Materials/`, `Sprites/`, `Settings/`); a family word leads (`MobGreatCobra`, `ItemCoconut`), a full-screen interface takes `Screen` (`LoseScreen`), a state or channel is a `_` variant field (`Banana_Grabbable`, `RoughBark_Normal`), and a third-party asset keeps its arrival filename
- Unity drops a new Volume profile into `Scenes/<Scene>/`; move it in the Editor to `Settings/<Module>/` (`Settings/_Shared/` when no one module owns it), since `Scenes/` holds `.unity` files only
- `Settings/` is `_Shared/` plus one folder per module whose scripts read the asset (e.g. `Player/`, `Environment/`, `MobGreatCobra/`, `ItemPineapple/`, `Cameras/`, `UI/`, `Analytics/`, `HackShortcuts/`) with assets placed directly inside, never a deeper folder; a config asset goes in the module whose scripts read it, name it for what it configures (e.g. `CobraBalanceConfig.asset`), and balance numbers live in the 3 balance configs by concern (player, world, cobra)
- Unity template leftovers and imports (`Assets/Readme.asset`, `Assets/TextMesh Pro/`) stay outside the game root
- `FullPlayScene.unity` and `EndlessScene.unity` are shells of 9 root prefab instances (`Cameras`, `DirectionalLight`, `GlobalVolume`, `EventSystem`, `Entities`, `BossFight`, `GameController`, `UI`, `Analytics`) plus a plain `Envs` object nesting the environment prefabs, see the Scene Hierarchy in [CONTEXT.md](CONTEXT.md#scene-hierarchy); open the owning prefab and edit it there, never an inline copy in the scene, so parallel work lands in different files and both gameplay scenes get it; `EndlessScene` differs from `FullPlayScene` only by the `GameController.runMode` override, so a change to `Envs` or a scene-level override is made in both scenes; `TitleScene` and `LevelSelectScene` are shells of `MenuCamera`, `MenuEventSystem`, their screen prefab and `Analytics`
- A new scene object becomes its own prefab under `Assets/_Monkobra/Prefabs/<Module>/`, nested in the matching root prefab; add a root only when no existing one fits
- A reference between two prefabs is a property override on the scene instance (Unity drops it inside a prefab asset); keep such links few
- Every asset under `Assets/` is committed with its `.meta` file, and a moved or renamed asset moves its `.meta` with it
- Never commit `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, `Logs/`, or `UserSettings/` at the repository root: all are already in `.gitignore`

## Setup Commands

Editor version is Unity `6000.3.22f1` (Unity 6, URP). Open the repository folder through Unity Hub. Builds (WebGL for GitHub Pages, WebGL for itch.io, Windows) are made from the Editor with the Build Profiles in [Build](docs/build-doc.md); no test command exists yet.

## GitHub Pages WebGL Build

Steps, folder rules, and required Player Settings are in [Build](docs/build-doc.md). Rules that always hold:

- the export lives in `Monkobra/` at the repo root, never named `Build/` or `Builds/`, and is committed as-is
- never hand-edit a file inside `Monkobra/`; re-export from Unity and re-commit instead
- Compression Format stays `Disabled`: GitHub Pages cannot set the `Content-Encoding` header a compressed export needs
- Development Build is on for the WebGL profiles (`GithubPages`, `ItchIO`): the hack keys only run in a development build

## Code Style

- Unity code follows the `coder-unity-engine` skill conventions
- Keep gameplay tuning values (stamina drain, grab restore, cobra speed) in serialized fields, never hard-coded
- Line endings: keep a file's existing endings; `PlayerBalanceConfig.cs` is CRLF, so edit it in a way that preserves them
- Gameplay input comes from actions of the Input Actions asset `InputSystem_Actions`, read through `InputAction` events or polling; timing such as a double-tap is an interaction on the action (`UseCoconut` is a MultiTap), never a hand-rolled key tracker in a script
- Scene loads: always `SceneNames.Load(SceneNames.X)`, never `SceneManager.LoadScene` or a typed scene name; it resets the time scale and audio pause and raises `Loading`, which closes the open analytics run; a menu is a `MenuEventSystem` scene reading the `InputSystem_Actions` UI map, a gameplay scene's `EventSystem` reads it too
- Dev/hack keys live only in `HackShortcutConfig` (a new `HackAction` plus an asset entry), read with `GetKey` and gated by `HackShortcutConfig.IsActive`; never hard-code a dev key or let one run in a release build; dev-only UI (e.g. `LoseScreenDownloadWidget`) hides itself unless `HackShortcutConfig.IsActive`
- Analytics events: a new run event is an `AnalyticsMetric` entry, a row in `AnalyticsMetrics`, and one `RecordEvent` call from a subscription in `AnalyticsManager`; never a new list in `RunData` or a hand-written section in the writer, see [Analytics](docs/analytics-doc.md#adding-a-metric)
- Camera: gameplay cameras track `CameraAnchor`, never the monkey body, so strafing never spins the view; every camera value lives in `CameraConfig` and an event shot (coconut, cobra alert, leap) is a `ShotKind` asked of `CameraRigManager`, never a camera enabled by hand; a camera script only polls a shared input action, never enables or disables it, see [Technical Design Document](docs/monkobra-tdd.md#camera)

## Testing Instructions

No test suite exists yet. When one is added, record the exact scoped command here, and keep tests in sync with changed code.

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
- Dev scenes: a personal copy of `FullPlayScene.unity` under `Assets/_Monkobra/Scenes/` is allowed for experiments; before the final merge the feature must exist in the `FullPlayScene.unity` prefabs, never only in the personal scene
- PR: `gh pr create --base dev`; nothing further is needed after the PR is created
- PR description: a few plain sentences on what the branch did, no padding; it feeds each member's Weekly Progress

## Documentation Maintenance

- Update [CONTEXT.md](CONTEXT.md) in the same change whenever a script is added, moved, or renamed
- Update [README.md](README.md) when controls or setup steps change
- Update the [Game Design Document](docs/monkobra-gdd.md) when design intent shifts, ie pillars, the core twist, resources, or win and lose rules; read it before changing how a mechanic is meant to feel
- Update [Mobs](docs/mob-doc.md) when a cobra, branch, or web changes how it spawns, moves, or reacts to a touch, and the [Grab System](docs/grab-doc.md) when detection, reach, or release rules change
- Update [Build](docs/build-doc.md) when the export steps, folder, or required Player Settings change
- Update the [Technical Design Document](docs/monkobra-tdd.md) when the architecture, run lifecycle, tuning assets, input bindings, hack shortcuts, or conventions change
- Update this file when commands or conventions change
