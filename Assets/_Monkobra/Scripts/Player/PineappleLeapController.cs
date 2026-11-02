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
        && balanceConfig != null;

    // Public Methods  #########################################################
    /// <summary>Starts the leap, unless <see cref="CanBegin"/> is false.
    /// </summary>
    public void Begin() {
        if (!CanBegin) {
            return;
        }

        phase = Phase.Extend;
        phaseEndTime = Time.time + balanceConfig.PineappleArmExtendS;
        if (GameController.I != null) {
            GameController.I.SetLeaping(true);
        }
        monkey.ClearStun();
        monkey.IsHeld = true;
        foreach (ArmController arm in arms) {
            arm.BeginLeapReach(
                balanceConfig.PineappleLeapDistanceU,
                balanceConfig.PineappleArmExtendS
            );
        }
        if (Debug.isDebugBuild) {
            Debug.Log("PineappleLeapController:\thands up", this);
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("game balance tuning; leap distance, speed and timing")]
    private GameBalanceConfig balanceConfig;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (balanceConfig == null) {
            Debug.LogWarning(
                "PineappleLeapController:\tmust assign Inspector Field: "
                    + "balanceConfig",
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
                }
                break;
            case Phase.Grace:
                if (Time.time >= phaseEndTime) {
                    Finish();
                }
                break;
        }
    }

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

    // cached references  ------------------------------------------------------
    private MonkeyController monkey;
    private ArmController[] arms;

    // private methods  ########################################################
    private void StartPull() {
        phase = Phase.Pull;
        monkey.PullTo(
            monkey.transform.position
                + Vector3.up * balanceConfig.PineappleLeapDistanceU,
            balanceConfig.PineappleLeapSpeedU
        );
        if (Debug.isDebugBuild) {
            Debug.Log("PineappleLeapController:\tpull started", this);
        }
    }

    // control returns here; hazards stay off until the grace ends
    private void Land() {
        phase = Phase.Grace;
        phaseEndTime = Time.time + balanceConfig.PineappleLandGraceS;
        monkey.IsHeld = false;
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
