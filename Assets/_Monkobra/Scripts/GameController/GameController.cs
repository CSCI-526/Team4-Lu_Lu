using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;

public class GameController: MonoBehaviour {
    // Public Members  #########################################################
    public static GameController I {
        get; private set;
    }

    public GameState State {
        get; private set;
    }

    public event Action<GameState> StateChanged;

    /// <summary>
    /// if the run is paused: time is frozen and the pause screen is up, while
    /// <see cref="State"/> keeps its live value, so resuming is not a new
    /// state change
    /// </summary>
    public bool IsPaused {
        get; private set;
    }

    /// <summary>raised with the new value each time the pause toggles</summary>
    public event Action<bool> PauseChanged;

    /// <summary>
    /// if the run is live, ie climbing and scoring work, whether or not the
    /// monkey is still protected
    /// </summary>
    public bool IsRunLive =>
        State == GameState.Playing || State == GameState.StartingZone;

    /// <summary>
    /// if the run has not reached <see cref="GameState.Playing"/> yet: the
    /// tutorial screen or the starting zone
    /// </summary>
    public bool IsBeforePlaying =>
        State == GameState.Tutorial || State == GameState.StartingZone;

    /// <summary>if the starting zone is under way</summary>
    public bool IsInStartingZone => State == GameState.StartingZone;

    /// <summary>
    /// whether cobra collisions are ignored entirely, a debug cheat; warns
    /// each time it is switched on
    /// </summary>
    public bool CobraNoClip {
        get => cobraNoClip;
        set {
            cobraNoClip = value;
            WarnIfCobraNoClip();
        }
    }

    // Public Methods  #########################################################
    public void EndStartingZone() {
        if (State == GameState.StartingZone) {
            SetState(GameState.Playing);
        }
    }

    /// <summary>
    /// pauses or resumes the run; a pause is ignored unless the run is live
    /// </summary>
    public void SetPaused(bool isPaused) {
        if (isPaused == IsPaused || (isPaused && !IsRunLive)) {
            return;
        }

        IsPaused = isPaused;
        ApplyTimeScale();
        AudioListener.pause = isPaused;
        PauseChanged?.Invoke(isPaused);

        if (Debug.isDebugBuild) {
            Debug.Log("GameController:\tpaused " + isPaused, this);
        }
    }

    public void LoseGame() {
        if (!IsRunLive) {
            return;
        }

        // settle score first
        if (ScoreManager.I != null) {
            ScoreManager.I.EndRun();
        }

        SetState(GameState.Lost);
        if (Debug.isDebugBuild) {
            Debug.Log("GameController:\tgame lost", this);
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("shared input tuning, its interact action restarts the scene on "
        + "the lose screen")]
    private MonkeyConfig monkeyConfig;

    [SerializeField]
    [Min(0f)]
    [Tooltip("unscaled seconds the lose screen ignores restart input")]
    private float restartInputDelay = 0.5f;

    [SerializeField]
    [Tooltip("toggles the pause while the run is live")]
    private InputAction pauseAction =
        CreateButton("Pause", "<Keyboard>/escape", "<Keyboard>/p");

    [SerializeField]
    [Tooltip("restarts the run from the pause screen")]
    private InputAction pauseRestartAction =
        CreateButton("Pause Restart", "<Keyboard>/r");

    [SerializeField]
    [Tooltip("debug cheat: cobra collisions never kill the monkey")]
    private bool cobraNoClip;

    [SerializeField]
    [Tooltip("dev/hack keys, its no-clip key works in a development build "
        + "only")]
    private HackShortcutConfig hackShortcuts;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (monkeyConfig == null) {
            Debug.LogError(
                "GameController:\tmust assign Inspector Field: Monkey Config",
                this
            );
        } else if (monkeyConfig.InteractAction == null) {
            Debug.LogError(
                "GameController:\tmust assign MonkeyConfig field: "
                    + "interactAction",
                monkeyConfig
            );
        }

        if (I != null && I != this) {
            Debug.LogWarning(
                "GameController:\tduplicate instance destroyed",
                this
            );

            Destroy(gameObject);
            return;
        }

        I = this;
        WarnIfCobraNoClip();
        SetState(GameState.Tutorial);
        anyPressSub = InputSystem.onAnyButtonPress.CallOnce(OnAnyPressed);
    }

    private void OnEnable() {
        pauseAction?.Enable();
        pauseRestartAction?.Enable();
    }

    private void OnDisable() {
        pauseAction?.Disable();
        pauseRestartAction?.Disable();
    }

    private void Update() {
        UpdateNoClipKey();
        UpdatePauseKey();

        if (State != GameState.Lost || InteractAction == null) {
            return;
        }

        if (Time.unscaledTime - lostTime < restartInputDelay) {
            return;
        }

        if (InteractAction.action.WasPressedThisFrame()) {
            ReloadScene();
        }
    }

    private void OnDestroy() {
        anyPressSub?.Dispose();

        if (I == this) {
            I = null;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    // Event Handlers  #########################################################
    private void OnAnyPressed(InputControl _) {
        anyPressSub = null;
        if (State == GameState.Tutorial) {
            SetState(GameState.StartingZone);
        }
    }

    // private methods  ########################################################
    // the pause key toggles the pause; outside a live run it does nothing.
    // The key that starts the run from the tutorial must not also pause it.
    // While paused, the restart key reloads the scene for a fresh run
    private void UpdatePauseKey() {
        if (
            IsPaused
            && pauseRestartAction != null
            && pauseRestartAction.WasPressedThisFrame()
        ) {
            if (Debug.isDebugBuild) {
                Debug.Log("GameController:\trestart from pause", this);
            }
            ReloadScene();
            return;
        }

        if (
            pauseAction == null
            || !pauseAction.WasPressedThisFrame()
            || Time.frameCount == stateChangedFrame
        ) {
            return;
        }

        SetPaused(!IsPaused);
    }

    // a fresh run; OnDestroy resets the time scale and audio for the new scene
    private static void ReloadScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // a button action bound to each given control path
    private static InputAction CreateButton(
        string name,
        params string[] bindings
    ) {
        var action = new InputAction(name, InputActionType.Button);
        foreach (string binding in bindings) {
            action.AddBinding(binding);
        }
        return action;
    }

    // frozen outside a live run and during a pause
    private void ApplyTimeScale() {
        Time.timeScale = IsRunLive && !IsPaused ? 1f : 0f;
    }

    // dev/hack key toggling the no-clip cheat
    private void UpdateNoClipKey() {
        if (hackShortcuts == null || !HackShortcutConfig.IsActive) {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Key noClipKey = hackShortcuts.GetKey(HackAction.NoClip);
        if (
            keyboard == null
            || noClipKey == Key.None
            || !keyboard[noClipKey].wasPressedThisFrame
        ) {
            return;
        }

        CobraNoClip = !CobraNoClip;
        if (Debug.isDebugBuild) {
            Debug.Log("GameController:\tcobra no clip " + CobraNoClip, this);
        }
    }

    private void WarnIfCobraNoClip() {
        if (cobraNoClip) {
            Debug.LogWarning(
                "GameController:\tCobra No Clip is set, cobra collisions "
                    + "cannot kill the monkey",
                this
            );
        }
    }

    private void SetState(GameState next) {
        State = next;
        stateChangedFrame = Time.frameCount;
        // a state change always ends a pause
        if (IsPaused) {
            IsPaused = false;
            AudioListener.pause = false;
            PauseChanged?.Invoke(false);
        }
        ApplyTimeScale();

        if (next == GameState.Lost) {
            lostTime = Time.unscaledTime;
            // a web trap may have paused interact; restart needs it live
            if (InteractAction != null) {
                InteractAction.action.Enable();
            }
        }

        StateChanged?.Invoke(next);
    }

    // private members  ########################################################
    private IDisposable anyPressSub;
    private float lostTime;
    private int stateChangedFrame = -1; // frame of the last SetState

    private InputActionReference InteractAction =>
        monkeyConfig != null ? monkeyConfig.InteractAction : null;
}
