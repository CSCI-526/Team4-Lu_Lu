using UnityEngine;

/// <summary>
/// Shows the pause panel while <see cref="RunController.IsPaused"/> is set.
/// <para>
/// Put it on an always-active parent of the panel: it only toggles the panel,
/// so its subscription to <see cref="RunController.PauseChanged"/> lives on
/// while the panel is hidden.
/// </para>
/// </summary>
public class PauseScreenWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("pause panel on a child object, never this one, shown only "
        + "while the game is paused")]
    private GameObject panel;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (panel == null) {
            Debug.LogError(
                "PauseScreenWidget:\tmust assign Inspector Field: panel",
                this
            );
            enabled = false;
            return;
        }

        panel.SetActive(false);
    }

    // the game controller is a singleton set in Awake, so it is read once
    // every Awake ran
    private void Start() {
        gameController = RunController.I;
        if (gameController == null) {
            Debug.LogError(
                "PauseScreenWidget:\tfail to get singleton: RunController",
                this
            );
            return;
        }

        OnPauseChanged(gameController.IsPaused);
        gameController.PauseChanged += OnPauseChanged;
    }

    private void OnDestroy() {
        if (gameController != null) {
            gameController.PauseChanged -= OnPauseChanged;
            gameController = null;
        }
    }

    // Event Handlers  #########################################################
    private void OnPauseChanged(bool isPaused) {
        if (panel != null) {
            panel.SetActive(isPaused);
        }
    }

    // private members  ########################################################
    // cached references  ------------------------------------------------------
    private RunController gameController;
}
