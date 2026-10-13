# MonKobra: Build Documentation

Unity `6000.3.22f1`. Every build goes to `builds/Monkobra v<version>+<Target>`, version from Player Settings. No build is committed; `builds/` stays gitignored. For every build: Development Build off, only intended scenes in `File > Build Profiles`.

## WebGL Player Settings (GitHub Pages & itch.io)

- Compression Format: `Disabled`
- WebGL Template: `Default`
- Canvas: stretch and fill

## GitHub Pages

Location: `builds/Monkobra v0.9.0+GitHub`

1. `File > Build Profiles > WebGL > Build` into the location
2. Test: `python3 -m http.server 8000 --directory "builds/Monkobra v0.9.0+GitHub"`

## itch.io

Location: `builds/Monkobra v0.9.0+Itch`

1. `File > Build Profiles > WebGL > Build` into the location
2. Zip the folder's contents, so `index.html` sits at the zip root
3. Page settings: Kind of project `HTML`, tick "played in the browser"
4. Embed in page at 960×600 with the fullscreen button; tick Mobile Friendly only if touch works
5. Set visibility to Draft while testing, then Public
6. Re-upload: upload the new zip, delete the old one
7. Check: load in a private window, watch the devtools Network tab

Limits: 1,000 files, 240 characters per path, 500 MB total, 200 MB per file.

## Windows

Location: `builds/Monkobra v0.9.0+Win`

1. `File > Build Profiles`, select Windows, build into the location
2. Run the `.exe`
3. Zip the whole folder

Not yet built once.
