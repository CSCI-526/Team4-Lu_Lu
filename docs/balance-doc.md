# Monkobra Balance Design

Why difficulty works the way it does: one height-driven ramp, a shared roll model for every hazard, and one home per number. The numbers are deliberately not here, they live in `Settings/Player/PlayerBalanceConfig.asset`, `Settings/Environment/WorldBalanceConfig.asset`, and `Settings/MobGreatCobra/CobraBalanceConfig.asset`, and change often. The field list and wiring are in the [Technical Design Document](monkobra-tdd.md#game-balance), what each hazard does on touch in [Mobs](mob-doc.md), and the intent behind the difficulty in the [Game Design Document](monkobra-gdd.md#difficulty).

## Design Goals

- One Knob For Pace: how fast the game gets harder is a single decision, not one per hazard
- Skill Sets The Pace: difficulty follows how high the monkey is, so a player who climbs well meets harder content sooner, and one who struggles stays in easier content
- Same Rules For Every Hazard: a designer who understands one spawn rule understands them all
- Data Not Code: no balance value is hard-coded, so a playtest finding is a one-asset edit

## Difficulty Follows Height

Difficulty is not a timer and not the score. It is a single value from 0 to 1, derived only from how far the monkey is above where it started.

```mermaid
graph LR
  Y[World y] --> Climb[Climbed height]
  Climb -->|ramp curve| Ramp[Ramped difficulty]
  Ramp -->|branch curve| Branch[Branch chance]
  Ramp -->|banana curve| Banana[Banana chance]
  Ramp -->|pineapple curve| Pineapple[Pineapple chance]
  Ramp -->|web curve| Web[Web chance]
  Ramp -->|falling cobra curve| Fall[Falling cobra chance]
```

- Why Height: height is what the player is trying to maximize, so tying difficulty to it makes the tension track progress; a timer would punish slow play instead of rewarding fast play
- Why A Shared Scale: difficulty is 0 at the starting zone exit and 1 at the boss fight, so every hazard curve shares one x axis and its first appearance is a point on that axis, found from its height; reshaping when difficulty arrives never touches the hazard curves. After the boss fight the ramp keeps rising above 1, and a curve past 1 shapes the post-boss run
- Two Baselines: every climb height (score, trunk height markers) is measured above the monkey's start y (`GetClimbedYDistance`), while the ramp itself starts at 0 at the y of the starting zone exit trigger (`RampBaseYPosition`), so the protected start is not counted as climbing difficulty. Both are owned by `RampedDifficultyManager`, and nothing else reads raw world y for balance, so shifting the world only means moving those baselines
- Floored: the service floors the ramp at 0 and leaves the top open, so past the curve's end difficulty stays at its last value; a hazard curve holds its last value past its own last key the same way
- Highest Not Current: score and the lose panel read the highest height reached, so falling and re-climbing is neither rewarded nor double counted

## Two Curves Per Hazard

Pace and density are separated on purpose:

- The ramp curve answers when: how much climbed height it takes to reach each level of difficulty
- A hazard's own curve answers how much: the chance of that hazard at each level of difficulty

Changing the ramp slides every hazard earlier or later together. Changing a hazard's curve reshapes only that hazard. This is why a hazard curve is not allowed to know about heights.

## One Roll Model

The tree is an endless stack of segments recycled upward ([Tree](monkobra-tdd.md#tree)), and every hazard is rolled per segment, when it is placed or restarted:

1. read the segment's ramped difficulty at its middle height
2. sample the hazard's probability curve there, clamped to 0~1
3. make a fixed number of attempts, each succeeding if a uniform roll falls below that chance

- Attempts Are A Ceiling: the attempt count is the most a segment can hold, and the chance says how often it gets there; the expected count is attempts times chance
- Why Attempts: a fixed count of small independent rolls gives natural variety segment to segment without any hazard clumping to a quota
- Why The Middle: a segment is one difficulty step, not a gradient, so every hazard in it shares the same value and a segment reads as one coherent chunk of the climb
- Fruit Differs: it is not rolled per segment; each generated branch rolls at its own height, pineapple first, then banana, so it bears 1 fruit or none. Branch density and fruit supply are therefore both difficulty-linked but tuned apart, and each pineapple rolled slightly lowers the banana supply. The coconut is the exception: `CoconutSpawner` rolls it once per segment at a flat chance from a start height, not from a curve, onto a branch left bare, see [Grab System](grab-doc.md#coconut)
- Order Matters: branches are placed first, then webs, falling cobras, and the coconut answer `BranchesGenerated`, so late hazards can keep clear of what already stands
- A Roll Can Place Nothing: webs and falling cobras are skipped when no clear spot is found, so the curve gives the chance of trying, not a guarantee

## Balance Lives In Three Assets

The balance numbers have 3 homes, split by what they tune: `PlayerBalanceConfig` for the player's run (stamina, starting zone, branch hit, score, coconut), `WorldBalanceConfig` for the generated world (ramped difficulty, branch and fruit, rough bark, spider web), and `CobraBalanceConfig` for both cobras (falling cobra, great cobra).

- Why Split By Concern: one asset had grown to a dozen unrelated groups, so a designer tuning the cobras scrolled past stamina and score; each question now has one obvious home, while every number still lives in exactly one asset, so there is no second, forgotten copy of it
- Cross-System Questions: stamina drain, banana supply, and climb speed together decide how long a run lasts, and they sit in different assets; read `PlayerBalanceConfig` and `WorldBalanceConfig` side by side when tuning a run's length
- Readers Hold The Reference: each consumer has a field for every slice it reads (`playerBalanceConfig`, `worldBalanceConfig`, `cobraBalanceConfig`) and no slice it does not, so there is no global to trip over, and a test scene can swap in another asset
- What Stays Out: tuning that is geometry or feel rather than balance stays with its owner: arm stroke and reach in `MonkeyArmConfig`, climb and orbit in `MonkeyClimbConfig`, branch geometry in `BranchFruitPlacementConfig`, the pineapple leap's distance, speed curve, and timings in `PineappleLeapConfig`, web and falling cobra spawn geometry on their spawners

## The Stamina Loop

Stamina is the pressure that makes the ramp matter, and it couples three balance groups:

- Drain: a cost per distance climbed or orbited, upward costing more than sideways, so wandering is worse than climbing; there is no timer drain, standing still never costs stamina
- Refill: bananas are the only real refill, apart from a slow recovery while the monkey rests
- Supply: banana chance by height decides how often a refill is on offer

Because bananas also cost focus to grab ([Grab System](grab-doc.md)), raising banana supply makes a run easier and also tempts the player into riskier grabs. Tune it knowing it moves both.

## Tuning Guide

- Pace: reshape the ramp curve; it is the only place height maps to difficulty
- Density Ceiling: change a hazard's attempt count
- Density Shape: change a hazard's probability curve
- Fairness Of Refill: change banana supply and the restore amount together, never one alone
- Reading A Change: only segments rolled after an edit reflect it, so play far enough for the pool to recycle, or restart the scene
- Webs Early: the `SpiderWebSpawner` test override skips the curve entirely ([Testing](mob-doc.md#testing))
- Coconut: `coconutSpawnProbabilityCurve` (`PlayerBalanceConfig`) rolls a coconut per segment from the ramped difficulty like every other hazard, its zero stretch setting where coconuts begin; `cobraFreezeDurationS` sets how much a coconut is worth. The `CoconutSpawner` test override swaps the curve for a flat chance and a start height for a quick check

## Known Gaps

- Every hazard shares one ramp, so a hazard cannot begin at its own height without reshaping its curve to stay flat until then
- A freshly created balance config's defaults differ from the tuned asset, so a recreated asset plays differently until retuned
- Difficulty ignores how close the chasing cobra is; only the cobra's own surge reacts to the player, speeding up after each hit or web and stacking with repeated mistakes, see [Mobs](mob-doc.md#motion)
