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
/// The cobra alert cuts away from the monkey, so while it is live the
/// monkey's input is locked (<see cref="MonkeyController.IsInputLocked"/>)
/// and <see cref="ShotChanged"/> tells the cut away bars to show.
/// </para>
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
        Leap,
        SpiderIntro,
    }

    /// <param name="kind">which shot</param>
    /// <returns>if the shot looks away from the monkey, so the input is
    /// locked & the cut away bars show</returns>
    public static bool IsCutAwayShot(ShotKind kind) {
        return kind == ShotKind.CobraAlert || kind == ShotKind.SpiderIntro;
    }

    /// <summary>
    /// Raised as an event shot goes live (true) or ends (false).
    /// </summary>
    public event System.Action<ShotKind, bool> ShotChanged;

    // Public Methods  #########################################################
    /// <summary>
    /// Asks for an event shot. Extends the shot if it is already live
    /// </summary>
    /// <param name="kind">which shot</param>
    /// <param name="holdS">how long to stay, capped by the shot's duration;
    /// infinite until <see cref="ReleaseShot"/>; s</param>
    /// <returns>if the shot is live now</returns>
    public bool RequestShot(
        ShotKind kind, float holdS = float.PositiveInfinity
    ) {
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
        AnnounceShot(kind, true);
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
    /// Lets a live shot end soon, never before its minimum hold unless asked
    /// </summary>
    /// <param name="kind">which shot</param>
    /// <param name="extraHoldS">time to linger first; s</param>
    /// <param name="isIgnoringMinHold">whether to end at once even inside the
    /// minimum hold</param>
    public void ReleaseShot(
        ShotKind kind,
        float extraHoldS = 0f,
        bool isIgnoringMinHold = false
    ) {
        ShotState state = states[(int)kind];
        if (!state.isLive) {
            return;
        }
        float endS = Time.time + extraHoldS;
        state.endTimeS = isIgnoringMinHold
            ? endS
            : Mathf.Max(endS, state.minHoldEndS);
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

    [Tooltip("shot that shows the whole cobra when it starts chasing")]
    [SerializeField]
    private CinemachineCamera cobraAlertCamera;

    [Tooltip("shot that looks up and pulls back during a pineapple leap")]
    [SerializeField]
    private CinemachineCamera leapCamera;

    [Tooltip("shot that looks up at the spider king's intro; the shot is "
        + "refused if unset")]
    [SerializeField]
    private CinemachineCamera spiderIntroCamera;

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
        if (leapCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: leapCamera",
                this
            );
        }
        if (spiderIntroCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "spiderIntroCamera, no spider intro shot",
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
        game = GameController.I;
        if (game == null) {
            Debug.LogWarning(
                "CameraRigManager:\tfail to get singleton: GameController, "
                    + "no leap shot",
                this
            );
        } else {
            game.LeapChanged += OnLeapChanged;
        }
        leapController = FindFirstObjectByType<PineappleLeapController>();
        if (leapController != null) {
            leapController.Landed += OnLeapLanded;
        }
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
        EndCobraBoost();
        ReleaseStartBoost();
        UnsubscribeCobra();
        // never leave the monkey locked behind a shot that can no longer end
        if (liveKind.HasValue && IsCutAwayShot(liveKind.Value)
            && MonkeyController.I != null) {
            MonkeyController.I.IsInputLocked = false;
        }
    }

    private void OnDestroy() {
        EndCobraBoost();
        ReleaseStartBoost();
        UnsubscribeCobra();
        if (game != null) {
            game.LeapChanged -= OnLeapChanged;
        }
        if (leapController != null) {
            leapController.Landed -= OnLeapLanded;
        }
        if (coconuts != null) {
            coconuts.Thrown -= OnCoconutThrown;
            coconuts.Landed -= OnCoconutLanded;
        }
    }

    private void Update() {
        if (config == null) {
            return;
        }
        // the leap shot follows the leap itself, polled as well as heard, so
        // a missed event can never leave the view stuck up
        if (liveKind == ShotKind.Leap && game != null && !game.IsLeaping) {
            ReleaseShot(ShotKind.Leap);
        }
        // the cobra's boosted climb ends as the camera arrives at it, or when
        // the shot is gone; its held exit boost starts as the shot ends
        if (isCobraBoosted
            && (Time.time >= cobraBoostEndTime
                || liveKind != ShotKind.CobraAlert)) {
            EndCobraBoost();
        }
        if (isStartBoostPending && liveKind != ShotKind.CobraAlert) {
            ReleaseStartBoost();
        }
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

    // the shot ends at the landing, or at its duration if that comes first
    private void OnCoconutLanded() {
        ReleaseShot(ShotKind.Coconut, 0f, true);
    }

    // the leap shot lasts as long as the leap, capped by its duration
    private void OnLeapChanged(bool isLeaping) {
        if (isLeaping) {
            RequestShot(ShotKind.Leap);
        } else {
            ReleaseShot(ShotKind.Leap);
        }
    }

    // the view hands back the moment the body lands, not after the grace
    private void OnLeapLanded() {
        ReleaseShot(ShotKind.Leap);
    }

    // the one cobra event that gets a look: it first starts moving, once the
    // monkey is out of the starting zone
    private void OnCobraChaseStarted() {
        RequestCobraAlert();
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
    private GameController game;
    private PineappleLeapController leapController;
    private CobraMotor cobra;
    private CameraCobraAlertFraming alertFraming;
    private bool isCobraBoosted; // this director boosts the cobra's climb
    private bool isStartBoostPending; // the held exit boost awaits the end
    private float cobraBoostEndTime; // Time.time the camera arrives

    // how long the head stays reared after the camera arrives, so it lowers
    // as the cobra settles to its chase speed; s
    private const float ALERT_REAR_AFTER_ARRIVAL_S = 1f;

    // private methods  ########################################################
    private void SubscribeCobra() {
        if (cobra == null) {
            cobra = FindFirstObjectByType<CobraMotor>();
            if (cobra != null) {
                cobra.ChaseStarted += OnCobraChaseStarted;
            }
        }
    }

    private void UnsubscribeCobra() {
        if (cobra != null) {
            cobra.ChaseStarted -= OnCobraChaseStarted;
            cobra = null;
        }
    }

    // shows the whole cobra, but not while it guards the boss arena
    private void RequestCobraAlert() {
        if (config == null || cobra == null || cobra.IsGuarding) {
            return;
        }
        // the shot is a journey: pull away, look, pull back the same way
        float travelS = config.CobraAlertTravelS;
        float totalS = 2f * travelS + config.CobraAlertSeeS;
        if (RequestShot(ShotKind.CobraAlert, totalS)) {
            if (alertFraming == null) {
                alertFraming =
                    FindFirstObjectByType<CameraCobraAlertFraming>();
            }
            if (alertFraming != null) {
                alertFraming.BeginJourney(
                    travelS, config.CobraAlertSeeS, travelS);
            }
            cobra.PlayChaseAlertHiss();
            // the cobra climbs at the boosted speed, at once, from the first
            // moment the camera pulls away, so the climb is plain to see;
            // it ends as the camera arrives
            float boostU = config.CobraAlertCreepSpeedU;
            if (boostU > 0f && !cobra.IsStoryHeld) {
                cobra.SetStartCreep(boostU, config.CobraAlertMinGapU);
                isCobraBoosted = true;
                isStartBoostPending = true;
                cobraBoostEndTime = Time.time + travelS;
            }
            cobra.PlayAlertRear(travelS + ALERT_REAR_AFTER_ARRIVAL_S);
        }
    }

    // the cobra is back at the required chase speed, its exit boost still
    // held; idempotent
    private void EndCobraBoost() {
        if (!isCobraBoosted) {
            return;
        }
        isCobraBoosted = false;
        if (cobra != null) {
            cobra.SetStartCreep(0f);
        }
    }

    // the held exit boost starts, as the monkey is free again; idempotent
    private void ReleaseStartBoost() {
        if (!isStartBoostPending) {
            return;
        }
        isStartBoostPending = false;
        if (cobra != null) {
            cobra.ReleaseStartBoost();
        }
    }

    private CameraConfig.ShotSettings GetSettings(ShotKind kind) {
        if (config == null) {
            return null;
        }
        return kind switch {
            ShotKind.Coconut => config.CoconutShot,
            ShotKind.CobraAlert => config.CobraAlertShot,
            ShotKind.Leap => config.LeapShot,
            ShotKind.SpiderIntro => config.SpiderIntroShot,
            _ => null,
        };
    }

    private CinemachineCamera GetCamera(ShotKind kind) {
        return kind switch {
            ShotKind.Coconut => coconutCamera,
            ShotKind.CobraAlert => cobraAlertCamera,
            ShotKind.Leap => leapCamera,
            ShotKind.SpiderIntro => spiderIntroCamera,
            _ => null,
        };
    }

    private void EndShot(ShotKind kind) {
        ShotState state = states[(int)kind];
        state.isLive = false;
        state.cooldownEndS = Time.time + GetSettings(kind).CooldownS;
        liveKind = null;
        ApplyPriorities();
        AnnounceShot(kind, false);
        if (Debug.isDebugBuild) {
            Debug.Log($"CameraRigManager:\tshot ended: {kind}", this);
        }
    }

    // cut away from the monkey, the player only watches: no input till it ends
    private void AnnounceShot(ShotKind kind, bool isLive) {
        if (IsCutAwayShot(kind) && MonkeyController.I != null) {
            MonkeyController.I.IsInputLocked = isLive;
        }
        ShotChanged?.Invoke(kind, isLive);
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
