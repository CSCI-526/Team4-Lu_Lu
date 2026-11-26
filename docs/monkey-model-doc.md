# Monkobra Monkey Model

How the monkey the player sees is drawn and animated: the skinned Mr. Mo model, its procedural climb, its arms, and its outline. Everything here is visual only: the physics monkey, its colliders, and the grab judge every touch exactly as before, see the [Grab System](grab-doc.md). The wider system map is in the [Technical Design Document](monkobra-tdd.md), the script rows in [CONTEXT.md](../CONTEXT.md).

## The Model

- Asset: `Assets/Mr. Mo Base Character/` (`MrMo_A`), a third-party import kept outside the game root, credited in [CREDITS.md](../CREDITS.md)
- Material: `Materials/Player/MonkeyMrMo_A.mat`
- Prefab: it hangs under `MonkeyModel_MrMo_A` in `Prefabs/Player/_Monkey.prefab`, next to the physics body
- Hidden old look: the capsule body, tail, and arm cubes no longer render, but their colliders still play

## Who Owns What

| Script | Runs | Owns |
| --- | --- | --- |
| `MonkeyModelAnimator` | first (execution order -10) | the climb cycle, legs, hips, spine, breathing, tail, the look back, the arm reskin |
| `MonkeyModelArms` | after the animator (execution order 10) | the two arm IK chains, the blend onto the physics hands |
| `ModelTwoBoneIK` | static helper | world space two bone IK and bone lookup by name |
| `MonkeyOutline` | `Awake` | the cream outline material slot |

Each script only reads the monkey's state and moves model bones. None touches a collider, body, joint, or gameplay value.

## Climb Cycle

`MonkeyModelAnimator` advances one cycle phase with the monkey's real speed, not with a timer, so the limbs keep pace at any climb speed, run backward while it climbs down, and hold still when it stops.

- Gait: diagonal, each hand moves with the opposite foot; the left hand leads and the right is half a cycle behind
- Limb stroke: a long stance planted on the bark, sliding down as the body rises, then a quick swing back up, lifted off the bark
- Body: the hips sway onto the planted foot, the spine rolls against it, and the chest breathes
- Sharing: `LimbCycle` hands the arms their share of the phase, so hands and feet never drift apart
- Knocked down: while a hit stuns the monkey (`MonkeyController.IsStunned`) its slide down never drives the cycle; the phase settles the short way, at `knockedGripSettleHz`, to where both diagonal pairs overlap in stance, so all 4 limbs grip the bark and hold till control returns

## Arms

`MonkeyModelArms` poses each arm as a two bone IK chain, elbow out, down, and back, like hugging the trunk.

- Climbing: each hand follows the animator's cycle
- Reaching or leaping: the hand blends onto the physics hand of `ArmController`, so the visible grab lands where it is judged; a leap hands the arm back to the climb cycle as soon as the hands let go, not at the end of the landing grace, when the freed physics hand still swings back on its joint
- Past a bent arm's reach: the upper arm and forearm both lengthen alike
- Wrist: bends but never twists, so the forearm never wrings thin
- Fingers: curled on the bark, open to reach

## Skin and Tail

On start the animator reskins a runtime copy of the mesh, so the elbow and wrist rings each move as one piece and a stretched arm keeps its thickness. The tail, skinned to the hips in the asset, gets a runtime chain of bones that swings on a spring trailing the body, with a slow idle curl.

## Look Back

Every few seconds the monkey turns its head over a shoulder and looks down the trunk, showing its face; far more often while the great cobra is close or surging, see [Mobs](mob-doc.md). It never looks back while reaching, leaping, webbed, or in the boss arena.

## Outline

`MonkeyOutline` appends the cream `Outline_Monkey` material as an extra slot on every skinned or mesh renderer under the model root, the same world-unit inverted hull a fruit wears, see [Grab System](grab-doc.md#feedback). The width is 0.040 u, in world units, so the model scale never changes it; tune the color and width in `FruitOutlineStyle`, then run `Monkobra > Build > Outline Materials`.

## Tuning

Every value is a serialized field with a tooltip, never a hard-coded number, see [Technical Design Document](monkobra-tdd.md). The stride, the stance share, the leg, hip, breath, tail, and look back values are on `MonkeyModelAnimator` on the monkey prefab, the hand reach and elbow values on `MonkeyModelArms`, and the outline color and width in `Settings/FruitOutline/FruitOutlineStyle.asset`.

## Known Gaps

- The model is visual only: a collider still follows the old capsule, so a pose never changes what touches the monkey
