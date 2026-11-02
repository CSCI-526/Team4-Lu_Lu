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

    /// <summary>
    /// Ends a live run as a win, eg the Cobra King defeated: the score
    /// settles, time freezes and the restart key plays again.
    /// </summary>
    public void WinGame() {
        if (!IsRunLive) {
            return;
        }

        if (ScoreManager.I != null) {
            ScoreManager.I.EndRun();
        }

        SetState(GameState.Won);
        if (Debug.isDebugBuild) {
            Debug.Log("GameController:\tgame won", this);
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
    [Min(0f)]
    [Tooltip("unscaled seconds the lose screen ignores restart input")]
    private float restartInputDelay = 0.5f;

    [SerializeField]
    [Tooltip("toggles the pause while the run is live, the Player/Pause "
        + "action (Esc or P)")]
    private InputActionReference pauseAction;

    [SerializeField]
    [Tooltip("restarts the run from the lose screen, the "
        + "Player/RestartAfterLose action (R or Space)")]
    private InputActionReference restartAfterLoseAction;

    [SerializeField]
    [Tooltip("restarts the run from the pause screen, the "
        + "Player/RestartDuringPause action (R)")]
    private InputActionReference restartDuringPauseAction;

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
        if (pauseAction == null) {
            Debug.LogError(
                "GameController:\tmust assign Inspector Field: Pause Action",
                this
            );
        }

        if (restartAfterLoseAction == null) {
            Debug.LogError(
                "GameController:\tmust assign Inspector Field: "
                    + "Restart After Lose Action",
                this
            );
        }

        if (restartDuringPauseAction == null) {
            Debug.LogError(
                "GameController:\tmust assign Inspector Field: "
                    + "Restart During Pause Action",
                this
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
        pauseAction?.action.Enable();
        restartAfterLoseAction?.action.Enable();
        restartDuringPauseAction?.action.Enable();
    }

    private void OnDisable() {
        pauseAction?.action.Disable();
        restartAfterLoseAction?.action.Disable();
        restartDuringPauseAction?.action.Disable();
    }

    private void Update() {
        UpdateNoClipKey();
        UpdatePauseKey();

        bool isOver = State == GameState.Lost || State == GameState.Won;
        if (!isOver || restartAfterLoseAction == null) {
            return;
        }

        if (Time.unscaledTime - lostTime < restartInputDelay) {
            return;
        }

        if (restartAfterLoseAction.action.WasPressedThisFrame()) {
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
    // While paused, the pause restart key reloads the scene for a fresh run
    private void UpdatePauseKey() {
        if (
            IsPaused
            && restartDuringPauseAction != null
            && restartDuringPauseAction.action.WasPressedThisFrame()
        ) {
            if (Debug.isDebugBuild) {
                Debug.Log("GameController:\trestart from pause", this);
            }
            ReloadScene();
            return;
        }

        if (
            pauseAction == null
            || !pauseAction.action.WasPressedThisFrame()
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

        if (next == GameState.Lost || next == GameState.Won) {
            lostTime = Time.unscaledTime;
        }

        StateChanged?.Invoke(next);
    }

    // private members  ########################################################
    private IDisposable anyPressSub;
    private float lostTime;
    private int stateChangedFrame = -1; // frame of the last SetState
}
