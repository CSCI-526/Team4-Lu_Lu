# MonKobra: Build Documentation

Unity `6000.3.22f1`. Every build uses its own Build Profile in `Assets/Settings/Build Profiles/` and goes to `builds/Monkobra v<version>+<Target>`, version from Player Settings. No build is committed; `builds/` stays gitignored. Build only from the profile; change a setting in the profile, never per build.

## GitHub Pages

Profile: `GithubPages` (WebGL)
Location: `builds/Monkobra v0.9.0+GitHub`

Profile settings (Pages cannot set response headers):

- Compression Format: `Disabled`; a `Gzip` or `Brotli` build fails to load
- Decompression Fallback: off; tick it only if size forces compression back on
- WebGL Template: `Default`
- Development Build: off
- Scenes: the global list (`Lv1Scene` only)

Steps:

1. `File > Build Profiles`, select `GithubPages`, click Build, choose the location
2. Test, since WebGL will not run from `file://`:

```bash
python3 -m http.server 8000 --directory "builds/Monkobra v0.9.0+GitHub"
```

## itch.io

Profile: `ItchIO` (WebGL)
Location: `builds/Monkobra v0.9.0+Itch`

Profile settings: same as GitHub Pages (`Disabled` works; gzip is the fallback, avoid Brotli).

Steps:

1. `File > Build Profiles`, select `ItchIO`, click Build, choose the location
2. Zip the folder's contents, so `index.html` sits at the zip root
3. Page settings: Kind of project `HTML`, tick "played in the browser"
4. Embed in page at 960×600 with the fullscreen button; tick Mobile Friendly only if touch works
5. Set visibility to Draft while testing, then Public
6. Re-upload: upload the new zip, delete the old one
7. Check: load in a private window, watch the devtools Network tab

Limits: 1,000 files, 240 characters per path, 500 MB total, 200 MB per file.

## Windows

Location: `builds/Monkobra v0.9.0+Win`

1. `File > Build Profiles`, select Windows, click Build, choose the location
2. Run the `.exe`
3. Zip the whole folder
