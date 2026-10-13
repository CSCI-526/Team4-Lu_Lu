# MonKobra: Build Documentation

Every version ships three builds, all made from the Editor (Unity `6000.3.22f1`). Design intent is in the [Game Design Document](monkobra-gdd.md), the system map in the [Technical Design Document](monkobra-tdd.md), and the agent rules in [AGENTS.md](../AGENTS.md).

## Build 1: WebGL for GitHub Pages

Pages serves the committed export as-is, with no GitHub-side build step. The method is adapted from the sibling repo `usc-csci526-asgn1-u3d-creative-core-learn`.

1. `File > Build Settings > WebGL > Build`, output directly into `Monkobra/`, a sibling of `Assets/`
2. Serve locally, since WebGL will not run from `file://`: `python3 -m http.server 8000 --directory Monkobra`
3. Commit and push `Monkobra/` (commit rules are in [AGENTS.md](../AGENTS.md))
4. `Settings > Pages > Deploy from branch` (root of `main`) serves it

Player Settings, required because Pages cannot set response headers:

- Compression Format: `Disabled`; a `Gzip` or `Brotli` export needs a `Content-Encoding` header and fails to load. If size forces compression back on, also tick Decompression Fallback
- WebGL Template: `Default`
- Canvas: stretch and fill, so the game fits whatever `index.html` embeds it in

## Build 2: WebGL for itch.io

Same platform and Player Settings as Build 1, output to `Builds/ItchIo/`, zipped and uploaded by hand.

Zip rules:

- `index.html` at the zip root, so zip the folder's contents, not the folder
- `.zip` only, relative case-sensitive paths
- Limits: 1,000 files, 240 characters per path, 500 MB total, 200 MB per file
- To update, upload the new zip and delete the old one

Compression:

- `Disabled` works; itch.io gzips `html`, `js`, `css`, `wasm` itself (`.data` is not on its list, so it may stay uncompressed, an inference)
- Gzip is the fallback if size grows; Brotli is reported to hang on itch.io, though those reports date from Unity 2020

Page settings:

- Kind Of Project: `HTML`, zip marked "played in the browser"
- Embed: "Embed in page" with a viewport (e.g. 960×600, the WebGL default canvas) plus the fullscreen button, or "Click to launch in fullscreen"
- Mobile Friendly: tick only if touch works
- Visibility: Draft or Restricted while testing, then Public
- Selling: HTML5 pages take donations only; set Kind Of Game to Downloadable to sell

After upload, load the page in a private window and check the devtools Network tab. A stall at the loading bar points first at compression. Source: [itch.io HTML5 docs](https://itch.io/docs/creators/html5).

## Build 3: Windows

No Windows build has been made yet, so this is the intended convention.

1. `File > Build Profiles`, select Windows, build into `Builds/Windows/`
2. Run the `.exe` from the output folder
3. Zip the whole folder (`.exe`, `_Data`, and DLLs stay together); the zip can go to itch.io as a download, which also allows selling

Player Settings already in the project: Product Name `Monkobra`, Company `Peel Five`, Version `0.9.0`, Fullscreen Window with switching allowed, not resizable, default 1024×768, Run In Background off.
