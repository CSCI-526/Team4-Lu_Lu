<plan-for-suite-preface>
Before working from this file, invoke `plan-for-suite-execution` skill through the `Skill` tool.
Ask whether to run under `cascade-all-movements` or `halt-each-movement`, and follow the chosen skill for how the Movements proceed.
</plan-for-suite-preface>

# Plan: Spider Webs and Falling Cobras Spawn Per Tree Segment Like Branches

Branch: `spider-web-endless` (feature branch). No commit is part of this Plan; AGENTS.md requires Unity closed before any commit. On approval this file is saved as `docs/tmp-segment-spawn-plan.md`, superseding `docs/tmp-falling-cobra-segment-plan.md`.

## Context

Branches and bananas already follow one model: each is rolled per tree segment, as a fixed attempt count against a probability curve sampled at the segment's ramped difficulty (0~1), all tuned in `GameBalanceConfig`, and parented to the segment so `Restart` recycles them. The user wants spider webs and falling cobras on that same model:

- each instance belongs to a tree segment
- probability is linked to the ramped difficulty system
- probability is configurable

## Current State (Verified in Code)

- Branches: `DynamicTreeSegmentPrefabRoot.GenerateBranches` rolls `BranchGenerationAttemptCount` attempts against `BranchSpawnProbabilityCurve.Evaluate(difficulty)`, then raises the static `BranchesGenerated` event. `ClearDecorations` destroys every non-prefab child on `Restart`.
- Spider web: `SpiderWebSpawner` (on `DTSPool`) already listens to `BranchesGenerated` and parents webs to the segment, but its odds ignore the ramped difficulty curve: `WebSlotsPerSegment`, `WebStartClimbU`, `WebFullChanceClimbU`, `WebMaxSpawnChance` and `WebSpawnChanceCurve` form their own climb-height ramp, and a hard start-climb gate.
- Falling cobra: `FallingCobraSpawner` is still the old global timer (60 u unlock, one at a time, branch scan). `FallingCobra` still warns on `Start` and falls straight down. Config for the new model already exists (old plan Movement 1, `e6c4c2a`): `FallingCobraAttemptCount`, `FallingCobraSpawnProbabilityCurve`, `FallingCobraFallSpeedU`, `FallingCobraHorizontalSpeedU`.
- Correction to the old plan: `DTSPool` does **not** call `SpiderWebSpawner.PopulateSegment`. It is event driven, and `DTSPool` "knows nothing of webs".

## Decisions

- Cobra spawner subscribes to `BranchesGenerated`, exactly like the web spawner, instead of being called by `DTSPool`. One mechanism for both, no `DTSPool` call sites. This replaces old Movement 4.
- Both spawners sample the curve at the segment's ramped difficulty: `RampedDifficultyService.I.GetRampedValueFromYPosition(segment.transform.position.y)`, the segment middle, same as branches.
- Web odds become attempts x probability curve, in a Spider Web block shaped like the Branch and Falling Cobra blocks. The start-climb gate, full-chance climb, max chance and share curve are removed: the curve is the gate (default 0 at difficulty 0). Existing asset values for those fields are dropped.
- Web test override stays, reduced to one forced chance; its start-climb field goes.
- Cobra placement, trigger distance and fall limit stay as in the old plan: ring of configurable radius, random y inside the segment, 3D-distance trigger, off after a fall limit.
- Chase (added by the user): the fall speed is constant and never changes; independently, every physics step the cobra re-reads the monkey's current angle about the tree axis and slides along the trunk surface toward it by the shortest arc at `FallingCobraHorizontalSpeedU`, so it keeps tracking a monkey that orbits while it falls. Ring radius stays fixed (it hugs the tree), the step is clamped to the remaining arc so it never overshoots, and once aligned it holds that angle while still falling.

## Progress

- Movement 1: done and committed (`e6c4c2a`), unchanged.
- Movements 2 to 7: ahead.

## Movements

Kinds: **Claude** = Claude edits and verifies. **User** = user works in the Unity Editor, Claude then verifies.

### Movement 1: Add Falling Cobra Config to GameBalanceConfig (Claude) — DONE

- Scope: `Assets/_Monkobra/Scripts/GameController/GameBalanceConfig.cs`
- Statement: Falling Cobra block: attempt count, spawn probability curve (x ramped difficulty, y chance), fall speed, horizontal chase speed, getters, `OnValidate` clamps.
- End State: Working = compiles; asset shows the block with defaults (attempts 2, curve 0 to 0.5, fall 6, horizontal 2).
- Order: none.

### Movement 2: Rework FallingCobra into a Self-Activating Root (Claude)

- Scope: `Assets/_Monkobra/Scripts/MobFallingCobra/FallingCobra.cs`
- Statement: states Dormant / Warning / Falling. `Initialize(GameBalanceConfig config, Vector3 treeAxisPosition)` called by the spawner; exposes `LengthU` so the spawner keeps it inside the segment. Dormant: when 3D distance to the monkey is below `triggerDistanceU` (prefab field), start the 1.25 s warning. Falling: constant downward speed from config; each `FixedUpdate` the angle to the monkey is re-read and the cobra steps around the ring (constant radius about the tree axis) toward it by the short arc, at most `FallingCobraHorizontalSpeedU * dt`, clamped to the remaining arc, so it homes on the monkey's live angle along the tree while falling at a constant rate. After `maxFallDistanceU` (prefab field) the hit capsule and object turn off. Hit path unchanged. Old fall-speed and despawn-below-player fields go. Default guard: require the cobra at or above the monkey's height to trigger.
- End State: Working = compiles; a hand-placed, `Initialize`d cobra stays dormant, triggers inside the distance, falls at a constant rate while sliding toward the monkey's live angle (and follows if the monkey orbits), turns off at the fall limit. A cobra never `Initialize`d stays dormant, so `FallingCobraDemo` stops auto-falling (accepted).
- Order: Movement 1.

### Movement 3: Rewrite FallingCobraSpawner as an Event-Driven Per-Segment Spawner (Claude)

- Scope: `Assets/_Monkobra/Scripts/MobFallingCobra/FallingCobraSpawner.cs` (rewritten in place, keeps `.meta` and GUID so the scene component survives), `Assets/_Monkobra/Scripts/Environment/DTSPool.cs` (comments and mock-tree warning only)
- Statement: spawner mirrors `SpiderWebSpawner`: first-wins static `listener`, subscribe to `DynamicTreeSegmentPrefabRoot.BranchesGenerated` in `OnEnable`, unsubscribe in `OnDisable`. `PopulateSegment` drops that segment's previous cobras, samples ramped difficulty at the segment middle, and for each of `FallingCobraAttemptCount` attempts rolls `FallingCobraSpawnProbabilityCurve`. Each hit: random angle on a `spawnRadiusU` ring, random local y in `[-h/2, h/2 - LengthU]`, at least `minCobraSeparationU` from cobras already there (retry cap, skip on failure); instantiate as a segment child, call `Initialize`. Fields: cobra prefab, `GameBalanceConfig`, spawn radius, separation, retries. Old timer, unlock and branch scan removed. `DTSPool`: extend the mock-tree warning and class summary to name the cobra spawner too.
- End State: Working = compiles; each recycled or first-stack segment gets its cobras rolled once, inside segment bounds, none outside; a null prefab or config disables the spawner with a warning.
- Order: Movements 1, 2.

### Movement 4: Move Spider Web Odds onto Attempts x Ramped Difficulty Curve (Claude) — REWRITTEN (was: Hook the Spawner into DTSPool)

- Scope: `Assets/_Monkobra/Scripts/GameController/GameBalanceConfig.cs`, `Assets/_Monkobra/Scripts/MobSpiderWeb/SpiderWebSpawner.cs`
- Statement: Spider Web block becomes `WebAttemptCount` (replaces slots) and `WebSpawnProbabilityCurve` (x ramped difficulty 0~1, y chance 0~1, default `Linear(0, 0, 1, 0.5)`), with getters and clamps; the start climb, full-chance climb, max chance and share-curve members go. `SpiderWebSpawner` rolls each attempt against the curve at the segment's ramped difficulty; `GetSpawnChance`, `StartClimbU`, `testStartClimbU` and the start-climb check in `IsSpotClear` go; the gizmo start ring goes; placement, separation, clearance and clear-on-reroll are untouched. `useTestOverride` keeps one `testSpawnChance` that replaces the curve. Class and tooltip text updated.
- End State: Working = compiles; no reference to the removed members remains (grep clean); webs appear by the curve alone, and the curve at difficulty 0 equal to 0 gives none early.
- Order: Movement 1. Independent of Movements 2 and 3.

### Movement 5: Wire the Scene in the Unity Editor (User, then Claude verifies)

- Scope: `Assets/_Monkobra/Scenes/Lv1Scene.unity` (`FallingCobraSpawner` and `SpiderWebSpawner` on `DTSPool`), the falling cobra prefab, `Settings/GameBalanceConfig.asset`
- Steps for the user:
  1. Open the project, let it recompile, confirm no compile errors.
  2. On `DTSPool` > `FallingCobraSpawner`: assign the falling cobra prefab and `GameBalanceConfig`.
  3. Press Play once, read `MonkeyPrefabRoot: ready, orbit radius X`, stop, set the cobra spawn radius near X.
  4. Check the falling cobra prefab shows `triggerDistanceU` and `maxFallDistanceU`.
  5. On `GameBalanceConfig.asset`: shape both curves (Spider Web, Falling Cobra) so neither is 0 where you want early spawns.
  6. Check `SpiderWebSpawner` still shows its prefab and config; save (Ctrl+S) and tell Claude.
- Verification by Claude: read the saved scene and prefab YAML, confirm references are non-zero and the radius matches; ask for a clean console.
- End State: Working = every reference assigned, clean console confirmed.
- Order: Movements 1 to 4.

### Movement 6: Playtest and Tune (User, then Claude verifies)

- Scope: no files, unless tuning changes config values
- Steps for the user:
  1. Climb: webs and dormant cobras appear on segments, more as difficulty ramps, none where the curve is 0.
  2. Edit a curve in the asset and replay: the density follows it.
  3. Approach a cobra: warning, then fall sliding toward your side; one hit, one drop; a miss vanishes at the fall limit.
  4. Pass recycled segments: no stale webs or cobras, no console errors.
  5. Report anything off with the numbers you want.
- Verification by Claude: adjust values only in config assets or fields, ask for a go.
- End State: Working = the user confirms behavior and balance.
- Order: Movement 5.

### Movement 7: Update Docs and Changelog (Claude)

- Scope: `docs/mob-doc.md`, `CONTEXT.md`, `docs/monkobra-tdd.md`, `docs/monkobra-gdd.md` (only where each describes web slots, start climb, max chance, the global cobra timer or the 60 u unlock), `CHANGELOG.md`
- Statement: rewrite web and falling-cobra Spawning, Behavior and settings tables to attempts x ramped-difficulty curve, per-segment, event driven; fix the `CONTEXT.md` script rows and the connection graph (add `Segment -->|BranchesGenerated| CobraSpawner`); log under `[Unreleased]`.
- End State: Working = no doc still describes web slots, the web start or full-chance climb, the global cobra timer, the 60 u unlock or the one-at-a-time rule.
- Order: Movement 6.

## Risks and Notes

- Removing web start-climb members drops the old values from `GameBalanceConfig.asset`; the new curve starts from defaults, so web density will feel different until tuned in Movement 6.
- Several cobras and webs can now be live at once, one roll per qualifying segment.
- 3D-distance trigger can fire beside or below the monkey; Movement 2 adds an at-or-above-height guard (say if unwanted).
- Frequency follows height and the curves, not a clock.

## Verification

- No tests exist: after each Claude Movement, check compile via the VS Code diagnostics and grep for removed members.
- End to end: Movements 5 and 6 in `Lv1Scene` as written.
