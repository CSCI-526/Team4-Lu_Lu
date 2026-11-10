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

        for (int i = 0; i < states.Length; i++) {
            states[i] = new ShotState();
        }
        ApplyPriorities();
    }

    private void Start() {
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

    private void OnDestroy() {
        if (coconuts != null) {
            coconuts.Thrown -= OnCoconutThrown;
            coconuts.Landed -= OnCoconutLanded;
        }
    }

    private void Update() {
        if (!liveKind.HasValue || config == null) {
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

    // private methods  ########################################################
    private CameraConfig.ShotSettings GetSettings(ShotKind kind) {
        if (config == null) {
            return null;
        }
        return kind switch {
            ShotKind.Coconut => config.CoconutShot,
            _ => null,
        };
    }

    private CinemachineCamera GetCamera(ShotKind kind) {
        return kind switch {
            ShotKind.Coconut => coconutCamera,
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
