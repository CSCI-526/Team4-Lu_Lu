# Monkobra DEVLOG

## Week 8: Pre-Alpha 0.9.0

- Endless Tree & Difficulty: lowest segment recycles over the top, so a run never ends; climb height maps to 0~1 difficulty driving all hazard chances. Initially fixed stack w/ linear branch count; changed to one shared curve in `GameBalanceConfig`
- Spider Web Trap: web holds the monkey, mash Space to break free. Initially placed once at start on fixed heights; changed to per-segment rolls to fit the recycled tree
- Falling Cobra: dormant until the monkey is near, then drops while sliding round the trunk toward it. Initially timer-unlocked & lethal; changed to per-segment rolls & a stun-and-drop hit
- Great Cobra & Stamina: cobra coils round the trunk, any contact kills. Initially empty stamina ended the run; changed to slow upward climb only, w/ progress bar tracking the cobra by height gap
- Fruit Outlines: 2 tiers, reachable & grabbable. Initially one outline, unclear whether a release would grab; changed to 2 materials matching the Grab System release check
- Run Analytics: per-session markdown report w/ death causes, per-run charts, height & stamina curves; F10 generates a mock session. Initially an event log appended at game over; changed to rewrite on every save plus 10 s autosave

![Endless climb with a web high above](devlog-assets/wk8%20Screenshot%202026-10-12%20165020.png)
*HUD: score and height (high point) top left, stamina bar bottom left, progress bar right showing the player's height relative to the great cobra. The run is infinite: the tree recycles its segments, so there is no summit*

![Monkey trapped in a spider web](devlog-assets/wk8%20Screenshot%202026-10-12%20165035.png)
*Spider web trap: the monkey is held on contact, with a prompt and escape bar to mash Space*

![Banana outlined on a branch](devlog-assets/wk8%20Screenshot%202026-10-12%20165048.png)
*Fruit outline: reachable fruit (inside the detection zone) gets a weaker outline; grabbable fruit (under the hand, so a release grabs it) gets a stronger one*

![Falling cobra above the monkey](devlog-assets/wk8%20Screenshot%202026-10-12%20165130.png)
*Falling cobra hanging dormant above the monkey, green while asleep*

![Falling cobra turned red](devlog-assets/wk8%20Screenshot%202026-10-12%20165246.png)
*Falling cobra awake: it turns red as a warning, then drops while sliding round the trunk toward the monkey*
