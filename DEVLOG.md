# Monkobra DEVLOG

## Week 8

- Endless Tree & Difficulty: lowest segment recycles over the top, so a run never ends; climb height maps to 0~1 difficulty driving all hazard chances. Initially fixed stack w/ linear branch count; changed to one shared curve in `GameBalanceConfig`
- Spider Web Trap: web holds the monkey, mash Space to break free. Initially placed once at start on fixed heights; changed to per-segment rolls to fit the recycled tree
- Falling Cobra: dormant until the monkey is near, then drops while sliding round the trunk toward it. Initially timer-unlocked & lethal; changed to per-segment rolls & a stun-and-drop hit
- Great Cobra & Stamina: cobra coils round the trunk, any contact kills. Initially empty stamina ended the run; changed to slow upward climb only, w/ progress bar tracking the cobra by height gap
- Fruit Outlines: 2 tiers, reachable & grabbable. Initially one outline, unclear whether a release would grab; changed to 2 materials matching the Grab System release check
- Run Analytics: per-session markdown report w/ death causes, per-run charts, height & stamina curves; F10 generates a mock session. Initially an event log appended at game over; changed to rewrite on every save plus 10 s autosave

#### Screenshots

<!-- TODO add screenshots, each with a one-line caption -->

- endless climb at low and high difficulty
- monkey in a spider web with escape bar
- falling cobra mid-drop
- HUD: stamina and progress bars
- reachable vs grabbable fruit outlines
- generated session report
