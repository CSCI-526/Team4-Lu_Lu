using System;
using UnityEngine;
using static CobraMotorRig;

/// <summary>
/// Cobra that coils round the trunk and climbs after the monkey without ever
/// stopping or backing down, so once it reaches the monkey, whatever part
/// touches first ends the run. Its base speed rises with the ramped
/// difficulty and by how far it trails the monkey, both as multipliers of
/// the monkey's top climb speed. Each mistake of the monkey (a branch or
/// falling cobra hit, or a web) makes it surge for a while, and mistakes
/// stack, faster and longer, possibly past the monkey's own speed; tuned in
/// <see cref="CobraBalanceConfig"/>. The one exception to never stopping: a
/// used coconut freezes the whole cobra for a while, see
/// <see cref="TryFreeze"/>. The body
/// wraps at least one full loop as a corkscrew with no gap round the trunk, so
/// a monkey dropping onto the cobra's height cannot slip past it by orbiting.
/// Segments are laid along the coil as a chain of rounded beads with small
/// gaps, slightly tapered, with the neck raised,
/// a slow travelling slither wave and eyes on the head. The body keeps its
/// material's own color.
/// Contact itself is handled by <see cref="CobraSensor"/>.
/// </summary>
public class CobraMotor: MonoBehaviour {
    /// <returns>if the cobra is surging after a mistake of the monkey
    /// </returns>
    public bool IsCatchingUp => rig.isCatchingUp;

    /// <summary>
    /// Raised on every mistake that adds to a surge, with the mistakes
    /// stacked in it so far, for a short warning.
    /// </summary>
    public event Action<int> CatchUpStarted;

    /// <summary>
    /// Raised once when the monkey leaves the starting zone and the cobra
    /// starts chasing, for a short warning.
    /// </summary>
    public event Action ChaseStarted;

    /// <returns>if the cobra guards the boss arena from below instead of
    /// chasing</returns>
    public bool IsGuarding => rig.isGuarding;

    /// <summary>
    /// The box round every body segment's position as posed now, head to
    /// tail, eg for a camera to fit the whole cobra
    /// </summary>
    /// <param name="bounds">the box, world space</param>
    /// <returns>if the body is set up and a box could be made</returns>
    public bool TryGetBodyBounds(out Bounds bounds) {
        bounds = new Bounds();
        if (rig.bodySegments == null || rig.bodySegments.Length == 0) {
            return false;
        }

        bool isFirst = true;
        foreach (Transform segment in rig.bodySegments) {
            if (segment == null) {
                continue;
            }
            if (isFirst) {
                bounds = new Bounds(segment.position, Vector3.zero);
                isFirst = false;
            } else {
                bounds.Encapsulate(segment.position);
            }
        }
        return !isFirst;
    }

    /// <summary>
    /// Stops the chase for the boss fight: the head is set at
    /// <paramref name="headWorldY"/> and held there for good, below the
    /// arena, still circling the trunk, while surges are ignored. Now & then
    /// its neck lifts in a smooth curve and the head springs up as a threat,
    /// fatal
    /// only to a monkey lingering that low; the coil never climbs.
    /// </summary>
    public void StartGuard(float headWorldY) {
        if (tuning.PathCenter == null) {
            return;
        }

        rig.isGuarding = true;
        rig.nextPeekTime = Time.time + UnityEngine.Random.Range(
            tuning.PeekIntervalRangeS.x, tuning.PeekIntervalRangeS.y
        );
        rig.surgeStacks = 0;
        rig.isCatchingUp = false;
        rig.guardHeadHeight = headWorldY - tuning.PathCenter.position.y;
        rig.headHeight = rig.guardHeadHeight;
        coil.UpdateBody();
    }

    /// <summary>
    /// Ends <see cref="StartGuard"/>: the chase picks up again from the
    /// guard height, easing up from a standstill.
    /// </summary>
    public void StopGuard() {
        if (!rig.isGuarding) {
            return;
        }
        rig.isGuarding = false;
        rig.guardPeek = 0f;
        rig.currentClimbSpeed = 0f;
        coil.UpdateBody();
    }

    /// <summary>
    /// Throws the head back <paramref name="distanceU"/> down the trunk, eg
    /// when the monkey's gold armor takes the bite; a guarding cobra ends
    /// its peek instead. Clears the running surge.
    /// </summary>
    public void KnockBack(float distanceU) {
        if (rig.isGuarding) {
            rig.guardPeek = 0f;
            rig.nextPeekTime = Time.time + UnityEngine.Random.Range(
                tuning.PeekIntervalRangeS.x, tuning.PeekIntervalRangeS.y
            );
        } else {
            rig.headHeight -= Mathf.Max(0f, distanceU);
            rig.currentClimbSpeed = 0f;
            rig.surgeStacks = 0;
            rig.isCatchingUp = false;
        }
        coil.UpdateBody();
    }

    /// <returns>if a coconut holds the cobra still right now</returns>
    public bool IsFrozen => rig.isFrozen;

    /// <returns>freeze time left, 0 when not frozen; s</returns>
    public float FreezeRemainingS =>
        rig.isFrozen ? Mathf.Max(0f, rig.freezeEndTime - Time.time) : 0f;

    /// <returns>full length of the running or last freeze; s</returns>
    public float FreezeDurationS => rig.freezeDurationS;

    /// <summary>
    /// Raised when a freeze starts, with its length; s.
    /// </summary>
    public event Action<float> Frozen;

    /// <summary>
    /// Raised when a freeze runs out and the cobra moves again.
    /// </summary>
    public event Action Thawed;

    /// <summary>
    /// Holds the whole cobra still for <paramref name="durationS"/>: no
    /// climbing, orbiting or slithering. Clears the running surge and the
    /// starting zone exit boost, and mistakes made while frozen add no surge.
    /// Afterwards the climb eases back up from a standstill. Contact still
    /// ends the run, see <see cref="CobraSensor"/>.
    /// </summary>
    /// <returns>if the freeze started; false while already frozen, so a
    /// freeze never stacks or resets</returns>
    public bool TryFreeze(float durationS) {
        if (!isActiveAndEnabled || rig.isFrozen || durationS <= 0f) {
            return false;
        }

        rig.isFrozen = true;
        rig.freezeDurationS = durationS;
        rig.freezeEndTime = Time.time + durationS;

        rig.surgeStacks = 0;
        rig.isCatchingUp = false;
        rig.exitBoostEndTime = 0f;
        rig.currentClimbSpeed = 0f;

        Frozen?.Invoke(durationS);
        return true;
    }

    public void PlayChaseAlertHiss() {
        if (rig.chaseAlertHissSource == null || !isActiveAndEnabled
            || rig.isGuarding
            || rig.isFrozen)return;
        hiss.StopProximityHiss();
        rig.chaseAlertHissSource.Play();
        rig.nextProximityHissTime =
            Time.time
                +Mathf.Max(tuning.HissIntervalS,
                    tuning.ProximityHiss.length
                        / Mathf.Max(.1f, tuning.HissPitch));
    }

    private void OnDisable() {
        hiss.StopProximityHiss();
        if (rig.chaseAlertHissSource != null) {
            rig.chaseAlertHissSource.Stop();
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private CobraMotorTuning tuning = new();

    // Private Members  ########################################################
    private CobraMotorRig rig;
    private CobraMotorChase chase;
    private CobraMotorSurge surge;
    private CobraMotorGuard guard;
    private CobraMotorHiss hiss;
    private CobraMotorCoil coil;

    private void Awake() {
        rig = new CobraMotorRig(this, tuning);
        rig.bodySegments = tuning.BodySegments;
        chase = new CobraMotorChase(rig, () => ChaseStarted?.Invoke());
        surge = new CobraMotorSurge(rig,
            (stacks) => CatchUpStarted?.Invoke(stacks));
        guard = new CobraMotorGuard(rig);
        hiss = new CobraMotorHiss(rig);
        coil = new CobraMotorCoil(rig);
        rig.Chase = chase;
        rig.Surge = surge;
        rig.Guard = guard;
        rig.Hiss = hiss;
        rig.Coil = coil;
        // Inspector Assignment Guard  -----------------------------------------
        if (tuning.PathCenter == null) {
            Debug.LogError(
                "CobraMotor:\tmust assign Inspector Field: pathCenter",
                this
            );
        }

        if (rig.bodySegments == null || rig.bodySegments.Length == 0) {
            Debug.LogError(
                "CobraMotor:\tmust assign Inspector Field: bodySegments",
                this
            );
        }

        if (tuning.PathCenter == null || rig.bodySegments == null
            || rig.bodySegments.Length == 0) {
            enabled = false;
            return;
        }

        if (tuning.CobraBalanceConfig == null) {
            Debug.LogWarning(
                "CobraMotor:\tno Inspector Field: cobraBalanceConfig, "
                    + "the cobra will not catch up",
                this
            );
        }

        coil.FillCoilSegments();
        rig.segmentPositions = new Vector3[rig.bodySegments.Length];
        coil.StyleBody();
        hiss.CreateProximityHiss();
    }

    private void Start() {
        if (!enabled) {
            return;
        }

        rig.headHeight = tuning.StartHeight;
        rig.currentClimbSpeed = tuning.ClimbSpeed;
        coil.UpdateBody();
    }

    private void Update() {
        if (rig.player == null) {
            GameObject playerObject = GameObject.FindGameObjectWithTag(
                "Player"
            );

            if (playerObject != null) {
                rig.player = playerObject.transform;
                surge.ListenForMistakes(playerObject);
            }
        }

        if (rig.isFrozen) {
            if (Time.time < rig.freezeEndTime) {
                // the whole body holds still, slither wave included
                rig.frozenTimeS += Time.deltaTime;
                return;
            }
            rig.isFrozen = false;
            Thawed?.Invoke();
        }

        rig.headAngle +=
            rig.Direction * tuning.OrbitSpeedDegrees * Time.deltaTime;

        if (rig.isGuarding) {
            guard.UpdateGuard();
            coil.UpdateBody();
            return;
        }

        chase.StartExitBoostOnZoneExit();

        float baseClimbSpeed = chase.BaseClimbSpeed();
        float targetClimbSpeed = baseClimbSpeed;

        // a surge ends when its stacked time runs out, and its stack with it
        if (rig.surgeStacks > 0 && Time.time >= rig.surgeEndTime) {
            rig.surgeStacks = 0;
        }
        rig.isCatchingUp = rig.surgeStacks > 0
            && tuning.CobraBalanceConfig != null;

        if (rig.isCatchingUp) {
            // a surge never slows near the monkey: it is the punishment
            targetClimbSpeed = Mathf.Max(
                baseClimbSpeed,
                Mathf.Min(
                    baseClimbSpeed
                        + tuning.CobraBalanceConfig.GetSurgeSpeedBonusU(
                            rig.surgeStacks),
                    tuning.CobraBalanceConfig.CobraMaxSurgeSpeedU
                )
            );
        } else if (Time.time < rig.exitBoostEndTime) {
            // leaving the starting zone releases the cobra with a burst
            targetClimbSpeed = baseClimbSpeed
                + tuning.CobraBalanceConfig.CobraExitBoostSpeedU;
        }

        // ease toward the target, so a speed-up or a slow-down never jumps
        float speedChangeRate = tuning.CobraBalanceConfig != null
            ? tuning.CobraBalanceConfig.CobraSpeedChangeRateU
            : float.PositiveInfinity;
        rig.currentClimbSpeed = Mathf.MoveTowards(
            rig.currentClimbSpeed,
            targetClimbSpeed,
            speedChangeRate * Time.deltaTime
        );

        // always up, never held at the player's height and never down: the
        // coil climbs on into the monkey, and a monkey moving or dropping
        // down runs into it instead of pushing it
        // except in the starting zone: the cobra stays active & orbits, but
        // holds its height until the zone ends
        if (!IsInStartingZone) {
            // the endless run's boss rush climbs it as fast as the monkey
            rig.headHeight += rig.currentClimbSpeed
                * EndlessBossRush.ClimbSpeedScale * Time.deltaTime;
        }

        coil.UpdateBody();
    }

    private void OnDestroy() {
        rig.cartoonVisual?.Dispose();
        surge.StopListening();
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        tuning.Validate();
    }

    private void LateUpdate() {
        coil.PoseCartoon();
    }
}
