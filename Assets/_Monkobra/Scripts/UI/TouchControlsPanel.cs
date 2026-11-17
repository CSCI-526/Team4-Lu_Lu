using UnityEngine;

/// <summary>
/// Shows the virtual touch controls (<see cref="content"/>: the full-screen
/// action zone and the joystick) only while they can be used: touch is the
/// <see cref="ControlStyle"/> and a run can be played, ie the tutorial, the
/// starting zone or playing, not paused, not lost or won. Hidden, the
/// controls release their virtual buttons at once.
/// <para>
/// Sits on an always-active root and switches its <see cref="content"/> child,
/// so it never switches off the object whose events it listens to. Reads
/// <see cref="GameController"/> in <c>Start</c>, so Awake order never matters.
/// </para>
/// </summary>
public class TouchControlsPanel: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("child holding the action zone & the joystick; this script "
        + "switches it, never its own object")]
    private GameObject content;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (content == null) {
            Debug.LogError(
                "TouchControlsPanel:\tmust assign Inspector Field: Content",
                this
            );
            enabled = false;
        }
    }

    private void Start() {
        game = GameController.I;
        if (game == null) {
            Debug.LogError(
                "TouchControlsPanel:\tGameController instance not found",
                this
            );
            enabled = false;
            return;
        }

        game.StateChanged += OnStateChanged;
        game.PauseChanged += OnPauseChanged;
        isStarted = true;
        Refresh();
    }

    private void OnEnable() {
        ControlStyleManager.StyleChanged += OnStyleChanged;
        if (isStarted) {
            Refresh();
        }
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= OnStyleChanged;
    }

    private void OnDestroy() {
        if (game != null) {
            game.StateChanged -= OnStateChanged;
            game.PauseChanged -= OnPauseChanged;
        }
    }

    // Private Methods  ########################################################
    private void OnStyleChanged(ControlStyle _) {
        Refresh();
    }

    private void OnStateChanged(GameState _) {
        Refresh();
    }

    private void OnPauseChanged(bool _) {
        Refresh();
    }

    private void Refresh() {
        bool isShown = ControlStyleManager.IsTouch && IsPlayable();
        if (content.activeSelf != isShown) {
            content.SetActive(isShown);
        }
    }

    // if the run can be played: tutorial, starting zone or playing, unpaused
    private bool IsPlayable() {
        return !game.IsPaused && game.State != GameState.Lost
            && game.State != GameState.Won;
    }

    // Private Members  ########################################################
    private GameController game;
    private bool isStarted;
}
