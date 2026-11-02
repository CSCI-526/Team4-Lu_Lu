using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Coconuts the monkey carries this run, and their use. Each coconut that
/// <see cref="ScoreManager"/> scores adds one; a quick double-tap on the
/// Interact action (Space, E or gamepad North, the grab key) uses one to
/// freeze the great cobra through <see cref="CobraMotor.TryFreeze"/>, for
/// <see cref="GameBalanceConfig.CobraFreezeDurationS"/>.
/// <para>
/// A tap is a press held no longer than
/// <see cref="GameBalanceConfig.CoconutTapMaxHoldS"/>, so holding to reach
/// never counts; a double-tap is 2 taps whose presses lie within
/// <see cref="GameBalanceConfig.CoconutDoubleTapWindowS"/>. It only polls the
/// action, never enables or disables it, so the grab works as before.
/// </para>
/// <para>
/// A coconut is only used while the run is <see cref="GameState.Playing"/>,
/// not paused (<see cref="GameController.IsPaused"/>), and the monkey is
/// neither caught in a web nor just out of one.
/// While the cobra is already frozen, nothing is used and
/// <see cref="FreezeRefused"/> is raised. The count lives in this scene only,
/// so a restart's reload empties it.
/// </para>
/// <para>
/// Put one on any object active from scene load; the cobra and the monkey
/// are found at runtime, so no cross-prefab link is needed. Widgets read
/// <see cref="I"/> in their Start.
/// </para>
/// <para>
/// It also owns the coconut HUD roots, which start inactive in their prefabs
/// (a script on an inactive object never runs, so no HUD can switch itself
/// on): <see cref="CoconutWidget"/>'s object turns on at the first coconut of
/// the scene and stays on, <see cref="FreezeTimerWidget"/>'s only while the
/// cobra is frozen in a run. Both are found at runtime, inactive ones
/// included.
/// </para>
/// </summary>
public class CoconutManager: MonoBehaviour {
    // Public Members  #########################################################
    public static CoconutManager I {
        get; private set;
    }

    /// <returns>coconuts carried now</returns>
    public int Count => count;

    /// <returns>the great cobra a coconut freezes, null if none in scene
    /// </returns>
    public CobraMotor Cobra => cobra;

    /// <returns>if a coconut was used since the app launched, so the how-to
    /// is no longer needed</returns>
    public static bool HasUsedThisSession => hasUsedThisSession;

    /// <summary>
    /// Raised w/ the new count whenever it changes: a pickup or a use.
    /// </summary>
    public event Action<int> CountChanged;

    /// <summary>
    /// Raised on each coconut scored, just b4 <see cref="CountChanged"/>.
    /// </summary>
    public event Action Collected;

    /// <summary>
    /// Relays <see cref="CobraMotor.Frozen"/> w/ the freeze time; s
    /// </summary>
    public event Action<float> Frozen;

    /// <summary>
    /// Relays <see cref="CobraMotor.Thawed"/>.
    /// </summary>
    public event Action Thawed;

    /// <summary>
    /// Relays <see cref="CobraMotor.CatchUpStarted"/> w/ its stack count.
    /// </summary>
    public event Action<int> CatchUpStarted;

    /// <summary>
    /// Raised once a coconut is used and the cobra froze.
    /// </summary>
    public event Action Used;

    /// <summary>
    /// Raised on a double-tap w/ coconuts in hand while the cobra is already
    /// frozen; nothing is used.
    /// </summary>
    public event Action FreezeRefused;

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("game balance tuning; freeze time & double-tap timing")]
    private GameBalanceConfig balanceConfig;

    [SerializeField]
    [Tooltip("monkey tuning; its Interact action is the double-tap key")]
    private MonkeyConfig monkeyConfig;

    [Header("Input Guards")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "taps are ignored this long after escaping a web, so mashing out of "
            + "it never spends a coconut; s"
    )]
    private float webEscapeGraceS = 1.5f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (balanceConfig == null) {
            Debug.LogWarning(
                "CoconutManager:\tmust assign Inspector Field: "
                    + "balanceConfig",
                this
            );
        }
        if (monkeyConfig == null || monkeyConfig.InteractAction == null) {
            Debug.LogWarning(
                "CoconutManager:\tmust assign Inspector Field: "
                    + "monkeyConfig, w/ an Interact action",
                this
            );
        }

        if (I != null && I != this) {
            Debug.LogWarning(
                "CoconutManager:\tduplicate instance, removing",
                this
            );
            Destroy(this);
            return;
        }
        I = this;

        // found in Awake, so widgets can read Cobra in their Start
        cobra = FindFirstObjectByType<CobraMotor>();
        if (cobra == null) {
            Debug.LogWarning(
                "CoconutManager:\tfail to find CobraMotor, coconuts "
                    + "freeze nothing",
                this
            );
        } else {
            cobra.Frozen += OnCobraFrozen;
            cobra.Thawed += OnCobraThawed;
            cobra.CatchUpStarted += OnCobraCatchUpStarted;
        }

        // HUD roots start inactive, so the lookup must include them
        coconutHud = FindFirstObjectByType<CoconutWidget>(
            FindObjectsInactive.Include
        );
        freezeHud = FindFirstObjectByType<FreezeTimerWidget>(
            FindObjectsInactive.Include
        );
        if (coconutHud == null) {
            Debug.LogWarning(
                "CoconutManager:\tfail to find CoconutWidget, no coconut HUD",
                this
            );
        }
        if (freezeHud == null) {
            Debug.LogWarning(
                "CoconutManager:\tfail to find FreezeTimerWidget, no freeze "
                    + "timer",
                this
            );
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObject != null) {
            struggle = playerObject.GetComponent<SpiderWebStruggle>();
        }
        if (struggle != null) {
            struggle.Escaped += OnEscaped;
        }
    }

    private void Start() {
        scoreManager = ScoreManager.I;
        if (scoreManager == null) {
            Debug.LogWarning(
                "CoconutManager:\tfail to get singleton: ScoreManager, "
                    + "pickups are not counted",
                this
            );
            return;
        }
        scoreManager.CoconutCollected += OnCoconutCollected;
    }

    private void OnDestroy() {
        if (scoreManager != null) {
            scoreManager.CoconutCollected -= OnCoconutCollected;
        }
        if (struggle != null) {
            struggle.Escaped -= OnEscaped;
        }
        if (cobra != null) {
            cobra.Frozen -= OnCobraFrozen;
            cobra.Thawed -= OnCobraThawed;
            cobra.CatchUpStarted -= OnCobraCatchUpStarted;
        }
        if (I == this) {
            I = null;
        }
    }

    private void Update() {
        UpdateDoubleTap();
        UpdateFreezeHud();
    }

    // a session is one app run: restarts reload the scene, statics stay;
    // reset on Play too, for an Editor w/o domain reload
    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration
    )]
    private static void ResetSession() {
        hasUsedThisSession = false;
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        webEscapeGraceS = Mathf.Max(0f, webEscapeGraceS);
    }

    // Event Handlers  #########################################################
    private void OnCoconutCollected() {
        // the HUD goes on b4 its widget hears of the pickup
        if (coconutHud != null && !coconutHud.gameObject.activeSelf) {
            coconutHud.gameObject.SetActive(true);
        }
        Collected?.Invoke();
        SetCount(count + 1);
    }

    private void OnCobraFrozen(float durationS) {
        Frozen?.Invoke(durationS);
    }

    private void OnCobraThawed() {
        Thawed?.Invoke();
    }

    private void OnCobraCatchUpStarted(int stacks) {
        CatchUpStarted?.Invoke(stacks);
    }

    private void OnEscaped() {
        escapeGraceEndTime = Time.time + webEscapeGraceS;
    }

    // constants  ##############################################################
    private const string PLAYER_TAG = "Player";

    // private members  ########################################################
    private static bool hasUsedThisSession; // a coconut was used since launch

    private int count;
    private CobraMotor cobra;
    private CoconutWidget coconutHud; // its object is the HUD root
    private FreezeTimerWidget freezeHud; // its object is the HUD root
    private SpiderWebStruggle struggle;
    private ScoreManager scoreManager;
    private float escapeGraceEndTime; // Time.time taps count again

    // double-tap detector  ----------------------------------------------------
    private float pressStartTime; // Time.time the held press began
    private float firstPressTime; // Time.time the 1st tap's press began
    private bool hasFirstTap; // 1st tap done, waiting for the 2nd press
    private bool isSecondPress; // the held press is the 2nd of a pair

    // private methods  ########################################################
    private void SetCount(int next) {
        if (next == count) {
            return;
        }
        count = next;
        CountChanged?.Invoke(count);
    }

    // freeze timer HUD is on only while the cobra is frozen in a run
    private void UpdateFreezeHud() {
        if (freezeHud == null) {
            return;
        }

        bool isPlaying =
            GameController.I == null
            || GameController.I.State == GameState.Playing;
        bool show = isPlaying && cobra != null && cobra.IsFrozen;
        if (freezeHud.gameObject.activeSelf != show) {
            freezeHud.gameObject.SetActive(show);
        }
    }

    // press & release edges of the grab key, read after the Input System has
    // run this frame; a long hold or a late 2nd press starts over
    private void UpdateDoubleTap() {
        if (balanceConfig == null || monkeyConfig == null) {
            return;
        }
        InputAction interact = monkeyConfig.InteractAction != null
            ? monkeyConfig.InteractAction.action
            : null;
        if (interact == null) {
            return;
        }

        float now = Time.time;
        if (interact.WasPressedThisFrame()) {
            pressStartTime = now;
            float sinceFirstS = now - firstPressTime;
            isSecondPress = hasFirstTap
                && sinceFirstS <= balanceConfig.CoconutDoubleTapWindowS;
            if (!isSecondPress) {
                firstPressTime = now;
                hasFirstTap = false;
            }
        }

        if (!interact.WasReleasedThisFrame()) {
            return;
        }

        bool isTap = now - pressStartTime <= balanceConfig.CoconutTapMaxHoldS;
        if (!isTap) {
            hasFirstTap = false;
            isSecondPress = false;
            return;
        }
        if (!isSecondPress) {
            hasFirstTap = true;
            return;
        }

        hasFirstTap = false;
        isSecondPress = false;
        TryUseCoconut();
    }

    // uses one coconut iff the run allows it and the cobra is free to freeze
    private void TryUseCoconut() {
        if (count <= 0 || cobra == null || !CanUseNow()) {
            return;
        }
        if (cobra.IsFrozen) {
            FreezeRefused?.Invoke();
            return;
        }
        if (!cobra.TryFreeze(balanceConfig.CobraFreezeDurationS)) {
            return;
        }

        SetCount(count - 1);
        hasUsedThisSession = true;
        Used?.Invoke();
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CoconutManager:\tcoconut used, cobra frozen for "
                    + balanceConfig.CobraFreezeDurationS + " s, " + count
                    + " left",
                this
            );
        }
    }

    // only mid-run, unpaused, and never while a web holds the monkey or just
    // let go of it
    private bool CanUseNow() {
        if (Time.timeScale <= 0f) {
            return false;
        }
        if (
            GameController.I != null
            && (GameController.I.State != GameState.Playing
                || GameController.I.IsPaused)
        ) {
            return false;
        }
        if (Time.time < escapeGraceEndTime) {
            return false;
        }
        return struggle == null
            || (!struggle.IsTrapped && !struggle.IsProtected);
    }
}
