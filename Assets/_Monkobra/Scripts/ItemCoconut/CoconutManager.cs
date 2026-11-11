using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Coconuts the monkey carries this run, and their use. Each coconut that
/// <see cref="ScoreManager"/> scores adds one; the UseCoconut input action
/// throws one: a <see cref="CoconutProjectile"/> falls from the monkey, and
/// when it hits
/// the cobra or has fallen its full distance the great cobra freezes through
/// <see cref="CobraMotor.TryFreeze"/>, for
/// <see cref="PlayerBalanceConfig.CobraFreezeDurationS"/>.
/// <para>
/// UseCoconut is an action of the Input Actions asset: a double-tap (Space, E,
/// Enter or gamepad North, the keys of the grab) set up there as a MultiTap
/// interaction, so its timing and keys are edited in the asset, not here. A
/// press held to reach never completes it. This enables the action while it
/// is enabled itself, and listens to its performed event only.
/// </para>
/// <para>
/// A coconut is only used while the run is live (<see cref="IsRunLive"/>: the
/// starting zone or playing), not paused
/// (<see cref="GameController.IsPaused"/>), and the monkey is neither caught
/// in a web nor just out of one.
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

    /// <returns>if the run is live, ie in the starting zone or playing, so the
    /// coconut HUD and prompts work; true w/o a GameController</returns>
    public static bool IsRunLive =>
        GameController.I == null || GameController.I.IsRunLive;

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
    /// Raised once a coconut is in the air, with the flying coconut, eg for
    /// a camera to follow it.
    /// </summary>
    public event Action<Transform> Thrown;

    /// <summary>
    /// Raised once the coconut hit the cobra or fell its full distance, just
    /// b4 the freeze is tried.
    /// </summary>
    public event Action Landed;

    /// <summary>
    /// Raised on a double-tap w/ coconuts in hand while the cobra is already
    /// frozen; nothing is used.
    /// </summary>
    public event Action FreezeRefused;

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("player balance tuning; freeze time & throw")]
    private PlayerBalanceConfig playerBalanceConfig;

    [SerializeField]
    [Tooltip("Input Actions asset that holds the coconut action")]
    private InputActionAsset inputActions;

    [SerializeField]
    [Tooltip("map/name of the coconut action in that asset")]
    private string useCoconutActionPath = "Player/UseCoconut";

    [SerializeField]
    [Tooltip("coconut model that falls from the monkey when one is used")]
    private CoconutProjectile projectilePrefab;

    [Header("Throw")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("the coconut starts this far below the monkey; u")]
    private float throwStartBelowU = 1.5f;

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
        if (playerBalanceConfig == null) {
            Debug.LogWarning(
                "CoconutManager:\tmust assign Inspector Field: "
                    + "playerBalanceConfig",
                this
            );
        }
        if (inputActions == null) {
            Debug.LogWarning(
                "CoconutManager:\tmust assign Inspector Field: "
                    + "inputActions",
                this
            );
        }
        if (projectilePrefab == null) {
            Debug.LogWarning(
                "CoconutManager:\tmust assign Inspector Field: "
                    + "projectilePrefab, a used coconut freezes at once",
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
            player = playerObject.transform;
            struggle = playerObject.GetComponent<SpiderWebStruggle>();
        }
        if (struggle != null) {
            struggle.Escaped += OnEscaped;
        }
    }

    private void OnEnable() {
        if (inputActions == null) {
            return;
        }
        useCoconutAction = inputActions.FindAction(useCoconutActionPath);
        if (useCoconutAction == null) {
            Debug.LogWarning(
                "CoconutManager:\tfail to find input action: "
                    + useCoconutActionPath,
                this
            );
            return;
        }
        useCoconutAction.performed += OnUseCoconutPerformed;
        useCoconutAction.Enable();
    }

    private void OnDisable() {
        if (useCoconutAction == null) {
            return;
        }
        useCoconutAction.performed -= OnUseCoconutPerformed;
        useCoconutAction.Disable();
        useCoconutAction = null;
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

    // a duplicate instance waits to be destroyed, it must not act
    private void OnUseCoconutPerformed(InputAction.CallbackContext _) {
        if (I == this) {
            TryUseCoconut();
        }
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
    private Transform player;
    private CoconutProjectile flyingCoconut; // thrown, not yet landed
    private SpiderWebStruggle struggle;
    private ScoreManager scoreManager;
    private float escapeGraceEndTime; // Time.time taps count again
    private InputAction useCoconutAction;

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

        bool show = IsRunLive && cobra != null && cobra.IsFrozen;
        if (freezeHud.gameObject.activeSelf != show) {
            freezeHud.gameObject.SetActive(show);
        }
    }

    // throws one coconut iff the run allows it and the cobra is free to
    // freeze; the freeze starts when the coconut lands, see OnCoconutLanded
    private void TryUseCoconut() {
        if (count <= 0 || cobra == null || !CanUseNow()) {
            return;
        }
        if (cobra.IsFrozen) {
            FreezeRefused?.Invoke();
            return;
        }
        // one in the air at a time, the next double-tap is not spent
        if (flyingCoconut != null) {
            return;
        }

        SetCount(count - 1);
        hasUsedThisSession = true;
        Used?.Invoke();

        if (projectilePrefab == null || player == null) {
            OnCoconutLanded(false);
            return;
        }
        flyingCoconut = Instantiate(
            projectilePrefab,
            player.position + Vector3.down * throwStartBelowU,
            Quaternion.identity
        );
        flyingCoconut.Launch(
            playerBalanceConfig.CoconutThrowSpeedU,
            playerBalanceConfig.CoconutThrowDistanceU,
            OnCoconutLanded
        );
        Thrown?.Invoke(flyingCoconut.transform);
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CoconutManager:\tcoconut thrown, " + count + " left",
                this
            );
        }
    }

    // the coconut hit the cobra or fell its full distance: the freeze starts
    private void OnCoconutLanded(bool isHit) {
        flyingCoconut = null;
        Landed?.Invoke();
        if (cobra == null) {
            return;
        }
        bool isFrozenNow = cobra.TryFreeze(playerBalanceConfig.CobraFreezeDurationS);
        if (Debug.isDebugBuild) {
            Debug.Log(
                "CoconutManager:\tcoconut landed, "
                    + (isHit ? "hit the cobra" : "missed")
                    + ", cobra frozen: " + isFrozenNow,
                this
            );
        }
    }

    // only mid-run (the starting zone counts), unpaused, and never while a
    // web holds the monkey or just let go of it
    private bool CanUseNow() {
        bool isPaused =
            Time.timeScale <= 0f
            || (GameController.I != null && GameController.I.IsPaused);
        if (!IsRunLive || isPaused || Time.time < escapeGraceEndTime) {
            return false;
        }
        return struggle == null
            || (!struggle.IsTrapped && !struggle.IsProtected);
    }
}
