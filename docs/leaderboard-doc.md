# Firebase Leaderboard

The lose screen can show real, persistent top scores through Firebase Anonymous
Authentication and Cloud Firestore. It reuses the existing ten rows, fonts and
score source. Gameplay and score calculation are unchanged. The client uses
UnityWebRequest and REST, without a native Firebase SDK or Cloud Functions.

## Enable the online board

1. Create a Firebase project on the **Spark** plan. Do not link a Cloud Billing
   account or upgrade to Blaze. Analytics is optional for this integration.
2. Under Authentication, enable the **Anonymous** sign-in provider.
3. Create a **Cloud Firestore Standard edition** database named `(default)` in
   production mode. Choose its location before continuing.
4. Publish [firestore.rules](../tools/firebase/firestore.rules) in the database's
   Rules tab. These rules are for this game's dedicated database; review before
   replacing rules in a project that already contains other applications.
5. Under Project settings > General, register a Web app if needed to see its
   configuration. Copy `projectId` and `apiKey`. These are public client settings,
   **never** a service-account JSON file or private key.
6. The config `Settings/Scoring/FirebaseLeaderboardConfig.asset` already exists
   and is assigned to `Prefabs/LoseScreen/Leaderboard.prefab`; no menu step is
   needed.
7. Enter Project Id and Web Api Key in that asset, leave Use Emulators off, and
   enable Use Online Leaderboard. Save and play `GameplayScene`.
8. Finish a run, then finish a lower-scoring run: the saved highest score should
   remain. Use another browser profile/device to check shared rankings. Rebuild
   WebGL through the existing build profile for the hosted game to use it.

The committed config is an empty, disabled template. Configure the project ID
and client API key locally before enabling the online board; do not commit the
filled settings. A disabled, absent or incomplete config retains the original
simulated board, labelled **PRACTICE**. Never put private service credentials or
a player's refresh token in that asset.

## What is saved

- `/leaderboards/normal/scores/{uid}` and
  `/leaderboards/endless/scores/{uid}` hold separate rankings.
- Each document contains only `displayName` and integer `score`. Before a nickname
  is saved, it uses `Climber <first six UID characters>`. The lose screen has a
  name field and SAVE button; Enter also saves while editing. Names accept 1–16
  ASCII letters, digits, spaces, hyphens or underscores, starting with a letter
  or digit, to match the shipped font. Leading/trailing spaces are trimmed;
  markup, line breaks and empty names are rejected. Rendered names disable TMP
  rich text. A nickname is a display label, never a unique login or document ID.
  Names may repeat without merging scores. Renaming updates the current mode's
  existing UID document without reducing its score; the other mode adopts the
  new nickname on its next submission.
- A commit uses Firestore's `maximum` transform so a later lower submission or
  overlapping request cannot reduce a personal best. Equal scores retain
  Firestore's default document-name tie order, rather than favouring this device.
- The top ten are ordered by descending score. A highlighted player in those
  results gets a rank. Outside the top ten, the footer shows the player's best
  and **OUTSIDE TOP 10**, not a fabricated global rank.
- Auth refresh tokens, the chosen nickname and pending per-mode bests use PlayerPrefs,
  scoped to the Firebase project. Emulators use a separate preference scope.
  Clearing browser data, changing browser origins or using another device creates
  another anonymous identity; existing cloud scores remain. There is no
  cross-device account recovery in this prototype.
- Submission starts when the lose screen opens, after the final score settles.
  The pending best is saved before networking. Failed submissions retry at the
  next loss. Uploads can complete across scene changes; stale results never
  repaint another run. Closing the browser can interrupt the upload.
- While connecting, only the local personal best is shown, with `--` as rank.
  Failure shows **OFFLINE | SAVED LOCALLY** and leaves gameplay usable.
  The client does not continuously poll or install background timers.
- Name input is not auto-focused, so the existing restart controls still work.
  Click/tap the field to type; SAVE/Enter persists locally then resubmits the best.
  Offline renames remain pending until another submission. `TextInputFocus`
  suppresses gameplay/menu shortcuts and hack keys while editing and through the
  frame after focus ends, preventing R, P, E, Space or digits from acting on the run.
  The Latin-only field temporarily disables IME composition while focused,
  after TMP's activation step, and restores the prior IME mode on deselection
  or disable. This avoids Microsoft Zhuyin Shift-mode conflicts. Button hover
  does not steal selection while a text field is focused; explicit clicks work.
- The nickname input and SAVE button are part of `Leaderboard.prefab`; no menu
  builds them.

## Cost and trust limits

The intended deployment uses only Spark-compatible Anonymous Auth and Firestore.
Firestore currently includes 1 GiB storage, 50,000 document reads/day and 20,000
document writes/day. Reads are counted per document, not per leaderboard request.
Keep the project on Spark with no billing account: quota exhaustion limits
availability instead of starting usage-based billing. Pricing policies can change.

Rules require authentication, restrict writes to the signed-in user's document,
validate fields, prohibit score decreases/deletion and limit list queries to ten
documents. Other paths are denied. This is a course-project scoreboard:
client-submitted scores are **not proof of honest play**. A modified client can
submit a fabricated high score or create more anonymous identities; the existing
development/hack controls can also affect scores. Authoritative anti-cheat and
custom server-side rate limiting are outside this prototype.

Sources: [Spark plan](https://firebase.google.com/docs/projects/billing/firebase-pricing-plans),
[Firestore quotas](https://firebase.google.com/docs/firestore/quotas),
[Auth REST](https://firebase.google.com/docs/reference/rest/auth),
[Firestore REST authentication](https://firebase.google.com/docs/firestore/use-rest-api),
[maximum transform](https://firebase.google.com/docs/firestore/reference/rest/v1/Write).

## Local testing without a Firebase account

Prerequisites: Node.js 20 and Java 17. Firebase CLI 13.35.1 is pinned for this
local toolchain; it is a development tool, not a shipped game dependency.
From the repository root:

```powershell
npx --yes firebase-tools@13.35.1 emulators:exec --config tools/firebase/firebase.json --project demo-monkobra --only auth,firestore "node --test tools/firebase/leaderboard.test.mjs"
```

The test refuses to run without emulator environment variables. It exercises
anonymous Auth, REST commit/query, maximum-score preservation, separate modes,
renaming without a new UID or lost best, and rejection of unauthorized writes,
extra fields, invalid scores/names,
unbounded queries and deletion. It clears **only the local demo database**.

For an interactive Unity trial, keep the emulators running:

```powershell
npx --yes firebase-tools@13.35.1 emulators:start --config tools/firebase/firebase.json --project demo-monkobra --only auth,firestore
```

Enable Use Online Leaderboard and Use Emulators in the config, then Play in the
Editor and lose a run. The demo project and loopback endpoints are selected
automatically. **Use Emulators exists only in the Editor**; an exported build
needs real public Firebase settings. Emulator data is temporary by default.
After restarting empty emulators, run
`Monkobra > Leaderboard > Reset Local Emulator Identity` before playing again.
This clears only the demo refresh token and keeps pending scores.
Unity may ask to allow local HTTP requests for this Editor-only test; if so set
Player > Other Settings > Allow downloads over HTTP to Development Only locally.

For a batch check of the actual C# client, with those emulators running and no
other Unity instance using this project:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.3.22f1\Editor\Unity.exe' -batchmode -nographics -projectPath . -executeMethod FirebaseLeaderboardSetup.VerifyEmulator -logFile Temp/firebase-integration.log
```

Do not pass `-quit`: the async check exits Unity after completion. Success logs
`FIREBASE_INTEGRATION_PASS`; failure exits nonzero. This uses the demo preference
scope, resets its demo identity and increments its test personal best; production
preferences are untouched. It also checks that status messages fit the prefab.
