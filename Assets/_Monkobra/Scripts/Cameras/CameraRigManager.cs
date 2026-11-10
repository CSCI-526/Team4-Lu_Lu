using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// The shot director. The climb camera is the default view; an event shot
/// (coconut track, later cobra alert and leap) takes over for a moment and
/// hands back. Each shot has a priority, a minimum hold, a hard duration cap
/// and a cooldown, all in <see cref="CameraConfig"/>, so events never chain
/// into rapid cuts: a shot of the same or a lower priority than the live one
/// is refused, a higher one preempts it, and a shot that just ended waits out
/// its cooldown. Swap is done by raising the live camera's
/// <see cref="CinemachineCamera.Priority"/>, the <c>CinemachineBrain</c> on
/// the main camera blends, with the per-pair times of its blender settings.
/// <para>
/// Reads no input: the grab framing lives in
/// <see cref="CameraClimbFraming"/>, so nothing here touches the shared
/// interact action.
/// </para>
/// </summary>
public class CameraRigManager: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>
    /// the event shots the director can cut to
    /// </summary>
    public enum ShotKind {
        Coconut,
        CobraAlert,
    }

    // Public Methods  #########################################################
    /// <summary>
    /// Asks for an event shot. Extends the shot if it is already live
    /// </summary>
    /// <param name="kind">which shot</param>
    /// <param name="holdS">how long to stay, capped by the shot's duration;
    /// infinite until <see cref="ReleaseShot"/>; s</param>
    /// <returns>if the shot is live now</returns>
    public bool RequestShot(ShotKind kind, float holdS = float.PositiveInfinity) {
        CameraConfig.ShotSettings settings = GetSettings(kind);
        if (config == null || settings == null || GetCamera(kind) == null) {
            return false;
        }

        float now = Time.time;
        ShotState state = states[(int)kind];
        if (state.isLive) {
            state.endTimeS = now + holdS;
            return true;
        }
        if (now < state.cooldownEndS) {
            return false;
        }
        if (liveKind.HasValue) {
            int livePriority = GetSettings(liveKind.Value).Priority;
            if (livePriority >= settings.Priority) {
                return false;
            }
            EndShot(liveKind.Value);
        }

        state.isLive = true;
        state.startTimeS = now;
        state.endTimeS = now + holdS;
        state.minHoldEndS = now + settings.MinHoldS;
        liveKind = kind;
        ApplyPriorities();
        if (Debug.isDebugBuild) {
            Debug.Log($"CameraRigManager:\tshot live: {kind}", this);
        }
        return true;
    }

    /// <param name="kind">which shot</param>
    /// <returns>if the shot is live now</returns>
    public bool IsShotLive(ShotKind kind) {
        return states[(int)kind].isLive;
    }

    /// <summary>
    /// Lets a live shot end soon, never before its minimum hold
    /// </summary>
    /// <param name="kind">which shot</param>
    /// <param name="extraHoldS">time to linger first; s</param>
    public void ReleaseShot(ShotKind kind, float extraHoldS = 0f) {
        ShotState state = states[(int)kind];
        if (!state.isLive) {
            return;
        }
        state.endTimeS = Mathf.Max(Time.time + extraHoldS, state.minHoldEndS);
    }

    // Inspector Fields  #######################################################
    [Tooltip("shot priorities, holds, cooldowns and durations")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("the default view, looking up the tree behind the monkey")]
    [SerializeField]
    private CinemachineCamera climbCamera;

    [Tooltip("shot that follows a thrown coconut down")]
    [SerializeField]
    private CinemachineCamera coconutCamera;

    [Tooltip("point the coconut shot tracks, set to the thrown coconut")]
    [SerializeField]
    private CameraShotTarget coconutTarget;

    [Tooltip("shot that looks down at the cobra after a cobra event")]
    [SerializeField]
    private CinemachineCamera cobraAlertCamera;

    [Tooltip("point the cobra alert tracks, set to the cobra's head")]
    [SerializeField]
    private CameraShotTarget cobraTarget;

    [Tooltip("the camera anchor, the monkey's height for the proximity alert")]
    [SerializeField]
    private Transform anchor;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: config",
                this
            );
        }
        if (climbCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: climbCamera",
                this
            );
        }
        if (coconutCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "coconutCamera",
                this
            );
        }
        if (coconutTarget == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "coconutTarget",
                this
            );
        }
        if (cobraAlertCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "cobraAlertCamera",
                this
            );
        }
        if (cobraTarget == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "cobraTarget",
                this
            );
        }

        for (int i = 0; i < states.Length; i++) {
            states[i] = new ShotState();
        }
        ApplyPriorities();
    }

    private void Start() {
        SubscribeCobra();
        coconuts = CoconutManager.I;
        if (coconuts == null) {
            Debug.LogWarning(
                "CameraRigManager:\tfail to get singleton: CoconutManager, "
                    + "no coconut shot",
                this
            );
            return;
        }
        coconuts.Thrown += OnCoconutThrown;
        coconuts.Landed += OnCoconutLanded;
    }

    // the cobra, found by type like CoconutManager does; all its events
    // count, none needs the cobra in the scene to be fatal
    private void OnEnable() {
        SubscribeCobra();
    }

    private void OnDisable() {
        UnsubscribeCobra();
    }

    private void OnDestroy() {
        UnsubscribeCobra();
        if (coconuts != null) {
            coconuts.Thrown -= OnCoconutThrown;
            coconuts.Landed -= OnCoconutLanded;
        }
    }

    private void Update() {
        if (config == null) {
            return;
        }
        CheckCobraProximity();
        if (!liveKind.HasValue) {
            return;
        }

        ShotKind kind = liveKind.Value;
        ShotState state = states[(int)kind];
        float now = Time.time;
        float capS = state.startTimeS + GetSettings(kind).DurationS;
        if (now >= state.endTimeS || now >= capS) {
            EndShot(kind);
        }
    }

    // Event Handlers  #########################################################
    private void OnCoconutThrown(Transform coconut) {
        if (coconutTarget != null) {
            coconutTarget.SetSubject(coconut);
        }
        RequestShot(ShotKind.Coconut);
    }

    // the shot lingers a moment on the landing spot
    private void OnCoconutLanded() {
        if (config != null) {
            ReleaseShot(ShotKind.Coconut, config.CoconutLandHoldS);
        }
    }

    // a cobra event worth a look, but not while it guards the boss arena
    private void OnCobraChaseStarted() {
        RequestCobraAlert();
    }

    private void OnCobraCatchUpStarted(int stacks) {
        RequestCobraAlert();
    }

    private void OnMonkeyHit(Collider _, HitBranchSensor.HitKind kind) {
        if (kind == HitBranchSensor.HitKind.FallingCobraController) {
            RequestCobraAlert();
        }
    }

    // constants  ##############################################################
    private const int CLIMB_PRIORITY = 10;
    private const int SHOT_LIVE_PRIORITY = 20;
    private const int SHOT_STANDBY_PRIORITY = 0;

    // private members  ########################################################
    private class ShotState {
        public bool isLive;
        public float startTimeS;
        public float endTimeS;
        public float minHoldEndS;
        public float cooldownEndS;
    }

    private readonly ShotState[] states =
        new ShotState[System.Enum.GetValues(typeof(ShotKind)).Length];
    private ShotKind? liveKind;
    private CoconutManager coconuts;
    private CobraMotor cobra;
    private HitBranchSensor hitSensor;

    // private methods  ########################################################
    private void SubscribeCobra() {
        // each source on its own, since a singleton may wake after this
        if (cobra == null) {
            cobra = FindFirstObjectByType<CobraMotor>();
            if (cobra != null) {
                cobra.ChaseStarted += OnCobraChaseStarted;
                cobra.CatchUpStarted += OnCobraCatchUpStarted;
            }
        }
        if (hitSensor == null) {
            hitSensor = HitBranchSensor.I;
            if (hitSensor != null) {
                hitSensor.Hit += OnMonkeyHit;
            }
        }
    }

    private void UnsubscribeCobra() {
        if (cobra != null) {
            cobra.ChaseStarted -= OnCobraChaseStarted;
            cobra.CatchUpStarted -= OnCobraCatchUpStarted;
            cobra = null;
        }
        if (hitSensor != null) {
            hitSensor.Hit -= OnMonkeyHit;
            hitSensor = null;
        }
    }

    // looks down at the cobra, framed with the monkey
    private void RequestCobraAlert() {
        if (config == null || cobra == null || cobraTarget == null
            || cobra.IsGuarding) {
            return;
        }
        Transform head = cobra.HeadTransform;
        if (head == null) {
            return;
        }

        if (!IsShotLive(ShotKind.CobraAlert)) {
            cobraTarget.SetSubject(head);
        }
        RequestShot(ShotKind.CobraAlert, config.CobraAlertShot.DurationS);
    }

    // optional: an alert whenever the cobra head comes within the configured
    // distance below the monkey, off while the distance is 0
    private void CheckCobraProximity() {
        float rangeU = config.CobraProximityU;
        if (rangeU <= 0f || cobra == null || anchor == null
            || cobra.IsGuarding || cobra.IsFrozen) {
            return;
        }
        Transform head = cobra.HeadTransform;
        if (head == null) {
            return;
        }

        float belowU = anchor.position.y - head.position.y;
        if (belowU > 0f && belowU < rangeU) {
            RequestCobraAlert();
        }
    }

    private CameraConfig.ShotSettings GetSettings(ShotKind kind) {
        if (config == null) {
            return null;
        }
        return kind switch {
            ShotKind.Coconut => config.CoconutShot,
            ShotKind.CobraAlert => config.CobraAlertShot,
            _ => null,
        };
    }

    private CinemachineCamera GetCamera(ShotKind kind) {
        return kind switch {
            ShotKind.Coconut => coconutCamera,
            ShotKind.CobraAlert => cobraAlertCamera,
            _ => null,
        };
    }

    private void EndShot(ShotKind kind) {
        ShotState state = states[(int)kind];
        state.isLive = false;
        state.cooldownEndS = Time.time + GetSettings(kind).CooldownS;
        liveKind = null;
        ApplyPriorities();
        if (Debug.isDebugBuild) {
            Debug.Log($"CameraRigManager:\tshot ended: {kind}", this);
        }
    }

    // the climb view always stands ready, the live shot rises above it
    private void ApplyPriorities() {
        SetPriority(climbCamera, CLIMB_PRIORITY);
        foreach (ShotKind kind in System.Enum.GetValues(typeof(ShotKind))) {
            bool isLive = liveKind.HasValue && liveKind.Value == kind;
            SetPriority(
                GetCamera(kind),
                isLive ? SHOT_LIVE_PRIORITY : SHOT_STANDBY_PRIORITY
            );
        }
    }

    private static void SetPriority(CinemachineCamera view, int value) {
        if (view == null) {
            return;
        }
        view.Priority = new PrioritySettings {
            Enabled = true,
            Value = value,
        };
    }
}
