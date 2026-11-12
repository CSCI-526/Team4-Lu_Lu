# MonKobra: Build Documentation

Unity `6000.3.22f1`. Every build uses its own Build Profile in `Assets/Settings/Build Profiles/` and goes to `builds/Monkobra v<version>+<Target>`, version from Player Settings. Each profile keeps its own copy of the Player Settings version, so `BuildVersionSync` (Editor script, also `Monkobra > Sync Build Profile Version`) copies the project one into every profile when the Editor loads; bump the version in Player Settings only. No build is committed; `builds/` stays gitignored. Build only from the profile; change a setting in the profile, never per build.

## GitHub Pages

Profile: `GithubPages` (WebGL)
Location: `builds/Monkobra v1.0.0-beta+GitHub`

Profile settings (Pages cannot set response headers):

- Compression Format: `Disabled`; a `Gzip` or `Brotli` build fails to load
- Decompression Fallback: off; tick it only if size forces compression back on
- WebGL Template: `FitWindow` (`Assets/WebGLTemplates/FitWindow/`): keeps the game at 16:9 and scales it to the largest box the browser window allows, black bars filling the rest, rendering at the screen's real pixel density; a Fullscreen button sits at the bottom right once the game is up. Set `MAX_RENDER_HEIGHT` in its `index.html` to cap the render resolution if frame rate drops. It also draws the [Loading Page](#loading-page)
- Splash Screen: "Show Splash Screen" off (Player Settings > Splash Image), so the loading page hands straight over to the title
- Development Build: on, since the hack hints and dev-only UI need it ([Dev Keys](#dev-keys)); the cost is a larger, slower export
- Default Canvas: 1280×720 (720p), only the starting size, since `FitWindow` resizes the canvas to the window; the `FullPlayScene` Canvas Scaler uses the same 1280×720 reference, so the UI lays out the same at any size
- Scenes: the global list, in order `TitleScene` (the startup scene), `LevelSelectScene`, `FullPlayScene`, `EndlessScene`

Steps:

1. `File > Build Profiles`, select `GithubPages`, click Build, choose the location
2. Test, since WebGL will not run from `file://`:

```bash
python3 -m http.server 8000 --directory "builds/Monkobra v1.0.0-beta+GitHub"
```

## itch.io

Profile: `ItchIO` (WebGL)
Location: `builds/Monkobra v1.0.0-beta+Itch`

Profile settings: same as GitHub Pages, Development Build on included (`Disabled` works; gzip is the fallback, avoid Brotli).

Steps:

1. `File > Build Profiles`, select `ItchIO`, click Build, choose the location
2. Zip the folder's contents, so `index.html` sits at the zip root
3. Page settings: Kind of project `HTML`, tick "played in the browser"
4. Embed options: "Embed in page" with a manual size of 1280×720; `FitWindow` fits the game into whatever frame size is set
5. Frame options: tick only "Fullscreen button"
6. Set visibility to Draft while testing, then Public
7. Re-upload: upload the new zip, delete the old one
8. Check: load in a private window, watch the devtools Network tab

Limits: 1,000 files, 240 characters per path, 500 MB total, 200 MB per file.

## Loading Page

Both WebGL builds open on a loading page drawn by `FitWindow/index.html` (HTML and JS, before Unity starts), over the game canvas:

- Picture: the TitleScreen picture (vines, Monkobra logo, branch, USC Games and Peel Five logos) on `#0A1F12`; the cobra eats a row of dots along the branch, each dot becoming one body segment, while the monkey hops ahead and lands at the end
- Progress: the cobra's head follows the real download progress of `createUnityInstance`, smoothed, and never faster than `LOADER_MIN_DURATION_S` (1.3 s) allows, so a cached, near-instant load still shows one short chase; the tuning constants sit at the top of the script
- Hand-over: its last frame is the `TitleScreen` prefab picture, with "Loading" where the title shows "Click to start"; `TitleScreen` calls `Plugins/WebGL/TitleScreenLoader.jslib` after its first frame and the page then fades out, or after 8 s if no call comes
- Art: `WebGLLoaderArt` (Editor script) copies `Sprites/TitleScreen/*.png` and the title font into the build's `TemplateData/TitleScreen/` after every WebGL build, so the page and the prefab share one set of files
- Layout: one 1280×720 table, written twice: the `LAYOUT` block of `index.html` and the table in `TitleScreenLayoutBuilder` (Editor script), which builds the `TitleScreen` prefab picture from `Monkobra > Build Title Screen`; change both together and rebuild, or the hand-over shows a jump

The Windows build has no page and opens on the title.

## Dev Keys

The hack keys (F1 no-clip, F2 max energy, F3 speed buff, F4 boss jump, F5~F8 spawns, F10 report download, F11 and F12 analytics, see [Hack Shortcuts](monkobra-tdd.md#hack-shortcuts)) run only when `Debug.isDebugBuild` is true, so the WebGL profiles (`GithubPages`, `ItchIO`) keep Development Build on and a player can press them. A build with it off, e.g. Windows by default, has none of them. The browser needs focus on the game canvas when F10 is pressed; F11 and F12 may also be taken by the browser (full screen, dev tools).

## Windows

Location: `builds/Monkobra v1.0.0-beta+Win`

1. `File > Build Profiles`, select Windows, click Build, choose the location
2. Run the `.exe`
3. Zip the whole folder
