# Monkobra Analytics

How the game records playtests: one markdown report per game launch, with a record of every run. The wider system map is in the [Technical Design Document](monkobra-tdd.md#analytics), the run states it observes are in the [Gameplay Loop](gameplay-loop-doc.md), and the hazards it counts are in [Mobs](mob-doc.md).

## Overview

`AnalyticsManager` is a singleton on its own `Analytics` prefab, placed in every scene; the first instance is kept across all scene loads. It listens to the game and writes the data to a markdown file, so a playtest can be read with any markdown viewer that renders Mermaid.

- Session: one game launch, one file; menus, scene loads and restarts keep the same session
- Run: one attempt from the start of play to a game over; every run adds one entry to the session file
- Report: the markdown file, rewritten whole on every save

```mermaid
graph LR
  Game[GameController] -->|StateChanged| AM[AnalyticsManager]
  Score[ScoreManager] -->|BananaCollected| AM
  Coconut[CoconutManager] -->|Collected, Used| AM
  Hit[HitBranchDetection] -->|BranchHit| AM
  Web[SpiderWebTrap] -->|Encountered| AM
  More[Leap, Pause, Boss Fight, ~] -->|events| AM
  AM --> Data[SessionData and RunData]
  Data --> Writer[AnalyticsMarkdownWriter]
  Writer --> SF[AnalyticsSessionFile]
  SF --> File[Session markdown file]
```

## Session Lifecycle

A session opens when the manager wakes, and a run follows the game state.

1. `GameState.StartingZone` opens a new run, takes its first sample, and saves; a run that starts straight at `GameState.Playing`, ie a dev scene, opens there instead
2. leaving the starting zone for `GameState.Playing` stamps the zone exit into the same run, it never opens a second one
3. every event of the [event list](#events) is stamped into the open run, each followed by a save
4. a timer samples height and stamina while the run is live, the starting zone included, and autosaves
5. `GameState.Lost` closes the run as `Lost`: the end, the score, and the death cause are filled in, then the file is saved
6. a run cut short, ie Play mode stopped, a scene reload mid run, an application pause, or the restart key, is closed as `Interrupted`

| Run Status | Meaning | In Session Summary |
| --- | --- | --- |
| `InProgress` | run still being played | ❌ |
| `Lost` | ended by a game over | ✔️ |
| `Interrupted` | cut short without a game over | ❌ |

## Output Files

Files land in `Application.persistentDataPath/Analytics/`.

- Session File: `0<yyyy-MM-dd_HH_mm_ss>.md`, named by the session start time
- Mock File: `0<stamp>-mock.md`, a fully faked session saved beside the real ones, the live session untouched
- Download: in a WebGL player the report is handed to the browser as a download instead (`WebDownload`); elsewhere it saves the session file

The file opens with an h1 title and the app version, then a `Session Summary` (only once a run was lost), then one `## Run N` entry per run.

## Metrics

Every figure the report carries, grouped by where it appears. Heights are in world units above the run start.

### Session Summary

Drawn from the `Lost` runs only; when lost runs of both run modes occur, one block per mode.

- Death Causes: pie of what ended each lost run, see [Death Causes](#death-causes)
- Final Score vs Run: bar chart of the total score of each lost run
- Highest Height vs Run: bar chart of the highest point each lost run reached
- Death Height vs Run: bar chart of the height at which each lost run ended
- Duration vs Run: bar chart of how long each lost run lasted, in seconds

### Run Header

- Mode: `Full` or `Endless`, the run mode of the scene that played the run
- Status: shown only when the run is not `Lost`
- Highest Height: highest point reached, which can exceed where the run ended
- Death Height: height at the moment of the loss, shown for a lost run
- Death Cause: what ended the run, shown for a lost run

### Run Timing

- Run Duration: seconds from start to end
- Time Start, Time Stop: wall clock times of the run's start and end
- Starting Zone Exit: time since the run start and height at which the monkey left the starting zone, shown only if it did
- Height vs Time: line of the sampled height over the run

### Score

- Final Score: total score of the run
- Score from Height: points earned by climbing
- Score from Banana: points earned by bananas, shown as `unit * count = total` when every banana paid the same
- Score from Coconut: points earned by coconuts, kept apart from the banana points in `RunData.CoconutRewardScore`, shown the same way
- Score Split: pie of height score against banana and coconut score

### Events

Each event list is a table of its time since the run start; banana, coconut, and branch rows also carry the height `y`.

- Banana Count: bananas collected, with the stamina curve (Stamina vs Time) under it
- Coconut Count: coconuts collected, from `CoconutManager.Collected`
- Coconut Used: coconuts thrown, from `CoconutManager.Used`, stamped at the throw, not at the landing; a double-tap refused while the cobra is already frozen is not counted
- Web Encounter: times the monkey touched a spider web
- Branch Hit: times the monkey's branch sensor met a branch; the falling cobra counts here too
- Pineapple Leap: pineapple leaps, stamped at the start of the leap
- Rough Bark Contact: times the monkey met a rough bark patch it was not already touching
- Cobra Freeze: times the great cobra froze, from a coconut or the gold armor
- Pause: times the run was paused, a resume is not counted
- Boss Fight Start: times the monkey entered the boss arena
- Armor Save: times the gold armor took a great cobra catch
- Boss Revive: times a revive restarted the boss fight

A metric that never happened still shows its heading with a count of 0.

#### Adding a Metric

Every event above is one `AnalyticsMetric`, so a new one never touches the writer:

1. add an entry to the `AnalyticsMetric` enum, which fixes its place in the report
2. add its row (heading and whether the table carries height `y`) to the table in `AnalyticsMetrics`
3. subscribe to its source in `AnalyticsManager` (`Observe`) and call `RecordEvent` from the handler; a source with no event gets a small one first

The stamp, the save, the report section, and the run reset follow from that.

### Death Causes

The player always dies to the great cobra, so the cause names what hit the monkey just before. `DeathCauseResolver` picks the latest branch hit or web encounter within `DeathCauseWindowS` before the loss.

| Cause | When |
| --- | --- |
| `Branch` | a branch hit inside the window, and later than any web |
| `Web` | a web encounter inside the window, and later than any branch |
| `GreatCobra` | neither inside the window |

## Tuning

Values are in `Settings/Analytics/AnalyticsConfig.asset`, never hard-coded.

- Sample Interval: seconds between height and stamina samples while a run plays, default 2, minimum 0.1
- Autosave Interval: seconds between timed saves while a run plays, default 10, 0 turns it off
- Death Cause Window: seconds before a loss in which a branch hit or web encounter counts as its cause, default 2

## Dev Keys

The keys work in every build, their hints show only in a development build or the Editor, and they live in `HackShortcutConfig` (see [Hack Shortcuts](monkobra-tdd.md#hack-shortcuts)).

| Action | Default Key | Effect |
| --- | --- | --- |
| `RestartSession` | F11 | closes the session and opens a new file, run numbering restarts |
| `DownloadReport` | F10 | downloads the report in the browser, saves the file elsewhere |
| `MockSession` | F12 | saves a mock session file |

The lose screen also carries a Download Analytics button at the top right (`LoseScreenDownloadWidget`) that does what `DownloadReport` does. It is for development builds and the Editor only: outside one the button turns itself off. The mock session does not generate the event metrics added after the first five.

## Scripts

All under `Assets/_Monkobra/Scripts/Analytics/`.

Collection, data, and visualization each have one file to open:

- Collection: `AnalyticsManager` observes the game, stamps events, samples, opens and closes runs, and asks for a save
- Data: `SessionData`, `RunData` are the plain data between the two
- Visualization: `AnalyticsMarkdownWriter` renders the data to markdown tables and Mermaid charts; it reads only the data it is given, so a chart never depends on the live scene

Around them:

- `AnalyticsSessionFile`: session file path, saving, browser download
- `AnalyticsHackKeys`: polls the dev keys
- `AnalyticsMetric`, `AnalyticsMetrics`: the event kinds and their report headings
- `LoseScreenDownloadWidget` (under `Scripts/LoseScreen/`): the dev-only download button
- `DeathCause`, `DeathCauseResolver`: infers and names the death cause
- `AnalyticsConfig`: tuning asset
- `MockAnalyticsGenerator`: builds the mock session
- `WebDownload`: browser download bridge
