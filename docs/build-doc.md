# MonKobra: Build Documentation

Unity `6000.3.22f1`. Every build uses its own Build Profile in `Assets/Settings/Build Profiles/` and goes to `builds/Monkobra v<version>+<Target>`, version from Player Settings. No build is committed; `builds/` stays gitignored. Build only from the profile; change a setting in the profile, never per build.

## GitHub Pages

Profile: `GithubPages` (WebGL)
Location: `builds/Monkobra v1.0.0-alpha.2+GitHub`

Profile settings (Pages cannot set response headers):

- Compression Format: `Disabled`; a `Gzip` or `Brotli` build fails to load
- Decompression Fallback: off; tick it only if size forces compression back on
- WebGL Template: `FitWindow` (`Assets/WebGLTemplates/FitWindow/`): keeps the game at 16:9 and scales it to the largest box the browser window allows, black bars filling the rest, rendering at the screen's real pixel density; a Fullscreen button sits at the bottom right. Set `MAX_RENDER_HEIGHT` in its `index.html` to cap the render resolution if frame rate drops
- Development Build: off
- Default Canvas: 1280×720 (720p), only the starting size, since `FitWindow` resizes the canvas to the window; the `Lv1Scene` Canvas Scaler uses the same 1280×720 reference, so the UI lays out the same at any size
- Scenes: the global list (`Lv1Scene` only)

Steps:

1. `File > Build Profiles`, select `GithubPages`, click Build, choose the location
2. Test, since WebGL will not run from `file://`:

```bash
python3 -m http.server 8000 --directory "builds/Monkobra v1.0.0-alpha.2+GitHub"
```

## itch.io

Profile: `ItchIO` (WebGL)
Location: `builds/Monkobra v1.0.0-alpha.2+Itch`

Profile settings: same as GitHub Pages (`Disabled` works; gzip is the fallback, avoid Brotli).

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

## Dev Keys

The hack keys (F1 no-clip, F8 report download, F9 and F10 analytics, see [Hack Shortcuts](monkobra-tdd.md#hack-shortcuts)) run only when `Debug.isDebugBuild` is true. The GitHub Pages export keeps Development Build off, so it has none of them. To download the analytics report from a browser, build WebGL with Development Build on, and serve it locally or upload it as a draft on itch.io. The browser needs focus on the game canvas when F8 is pressed.

## Windows

Location: `builds/Monkobra v1.0.0-alpha.2+Win`

1. `File > Build Profiles`, select Windows, click Build, choose the location
2. Run the `.exe`
3. Zip the whole folder
