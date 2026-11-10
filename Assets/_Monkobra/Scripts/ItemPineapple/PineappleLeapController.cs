using UnityEngine;

/// <summary>
/// On the monkey root: runs one leap after a pineapple grab. Both hands rise
/// to a high point, the body is pulled up to it, then control returns and a
/// short landing grace runs out.
/// <para>
/// <see cref="GameController.IsLeaping"/> stays set from the grab to the end
/// of the grace. Runs in physics steps, so a pause freezes it.
/// </para>
/// </summary>
[RequireComponent(typeof(MonkeyController))]
public class PineappleLeapController: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>if a grab may start a leap now: no leap under way, and no web
    /// holds the monkey</returns>
    public bool CanBegin =>
        phase is Phase.Idle or Phase.Grace
        && monkey != null
        && !monkey.IsHeld
        && leapConfig != null;

    /// <summary>
    /// Raised once when the body reaches the leap's end and the landing
    /// grace begins, b4 <see cref="GameController.IsLeaping"/> ends.
    /// </summary>
    public event System.Action Landed;

    // Public Methods  #########################################################
    /// <summary>Starts the leap, unless <see cref="CanBegin"/> is false.
    /// </summary>
    public void Begin() {
        if (!CanBegin) {
            return;
        }

        phase = Phase.Extend;
        phaseEndTime = Time.time + leapConfig.ArmExtendS;
        if (GameController.I != null) {
            GameController.I.SetLeaping(true);
        }
        monkey.ClearStun();
        monkey.IsHeld = true;
        foreach (ArmController arm in arms) {
            arm.BeginLeapReach(
                leapConfig.DistanceU,
                leapConfig.ArmExtendS
            );
        }
        if (Debug.isDebugBuild) {
            Debug.Log("PineappleLeapController:\thands up", this);
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip(
        "leap distance, speed curve, hand rise time and landing grace time"
    )]
    private PineappleLeapConfig leapConfig;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (leapConfig == null) {
            Debug.LogWarning(
                "PineappleLeapController:\tmust assign Inspector Field: "
                    + "leapConfig",
                this
            );
        }

        monkey = GetComponent<MonkeyController>();
        if (monkey == null) {
            Debug.LogError(
                "PineappleLeapController:\t"
                    + "fail to get Component: MonkeyController",
                this
            );
        }
        arms = GetComponentsInChildren<ArmController>(true);
    }

    private void FixedUpdate() {
        switch (phase) {
            case Phase.Extend:
                if (Time.time >= phaseEndTime) {
                    StartPull();
                }
                break;
            case Phase.Pull:
                if (monkey.HasReachedPullTarget) {
                    Land();
                } else {
                    PullAtCurveSpeed();
                }
                break;
            case Phase.Grace:
                if (Time.time >= phaseEndTime) {
                    Finish();
                }
                break;
        }
    }

    // constants  ##############################################################
    // a curve reaching 0 must still finish the leap
    private const float MIN_PULL_SPEED_U = 0.5f;

    // private types  ##########################################################
    private enum Phase {
        Idle,
        Extend,
        Pull,
        Grace,
    }

    // private members  ########################################################
    private Phase phase = Phase.Idle;
    private float phaseEndTime; // Time.time ending a timed phase
    private float pullStartTime; // Time.time the pull started
    private Vector3 pullTarget; // world pos the body is pulled to

    // cached references  ------------------------------------------------------
    private MonkeyController monkey;
    private ArmController[] arms;

    // private methods  ########################################################
    private void StartPull() {
        phase = Phase.Pull;
        pullStartTime = Time.time;
        pullTarget =
            monkey.transform.position + Vector3.up * leapConfig.DistanceU;
        // never past the boss fight's arena: the leap stops at its height
        BossFightController bossFight = BossFightController.I;
        if (bossFight != null) {
            pullTarget.y = Mathf.Max(
                monkey.transform.position.y,
                Mathf.Min(pullTarget.y, bossFight.LeapTopY)
            );
        }
        PullAtCurveSpeed();
        if (Debug.isDebugBuild) {
            Debug.Log("PineappleLeapController:\tpull started", this);
        }
    }

    private void PullAtCurveSpeed() {
        float speedU = leapConfig.SpeedCurve.Evaluate(
            Time.time - pullStartTime
        );
        monkey.PullTo(pullTarget, Mathf.Max(speedU, MIN_PULL_SPEED_U));
    }

    // control returns here; hazards stay off until the grace ends
    private void Land() {
        phase = Phase.Grace;
        phaseEndTime = Time.time + leapConfig.LandGraceS;
        monkey.IsHeld = false;
        Landed?.Invoke();
        foreach (ArmController arm in arms) {
            arm.EndLeapReach();
        }
        if (Debug.isDebugBuild) {
            Debug.Log("PineappleLeapController:\tlanded", this);
        }
    }

    private void Finish() {
        phase = Phase.Idle;
        if (GameController.I != null) {
            GameController.I.SetLeaping(false);
        }
    }
}
