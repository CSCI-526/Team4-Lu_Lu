using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// the options of the pause and lose screens: Resume (pause only), Restart,
/// and Exit back to the level selection. Put it on the screen panel, next to a
/// <see cref="MenuSelectionKeeper"/>, which gives the keys their highlight.
/// </summary>
public class RunScreenMenu: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("resumes the run, on the pause screen only, empty elsewhere")]
    private Button resumeButton;

    [SerializeField]
    [Tooltip("restarts the run in this scene")]
    private Button restartButton;

    [SerializeField]
    [Tooltip("leaves the run for the level selection")]
    private Button exitButton;

    [SerializeField]
    [Tooltip("confirms the highlighted option, the UI/Submit action (Space, "
        + "E, Enter)")]
    private InputActionReference submitAction;

    [SerializeField]
    [Tooltip("if a confirm press with nothing highlighted restarts the run, "
        + "the shortcut of the lose screen")]
    private bool isSubmitRestarts;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (restartButton == null || exitButton == null) {
            Debug.LogError(
                "RunScreenMenu:\tmust assign Inspector Fields: "
                    + "Restart Button, Exit Button",
                this
            );
            enabled = false;
            return;
        }

        if (isSubmitRestarts && submitAction == null) {
            Debug.LogError(
                "RunScreenMenu:\tmust assign Inspector Field: Submit Action",
                this
            );
        }
    }

    // the event system's input module owns the action, so it is only
    // switched on here, never off
    private void OnEnable() {
        submitAction?.action.Enable();
        if (resumeButton != null) {
            resumeButton.onClick.AddListener(OnResumeClicked);
        }
        restartButton.onClick.AddListener(OnRestartClicked);
        exitButton.onClick.AddListener(OnExitClicked);
    }

    private void OnDisable() {
        if (resumeButton != null) {
            resumeButton.onClick.RemoveListener(OnResumeClicked);
        }
        if (restartButton != null) {
            restartButton.onClick.RemoveListener(OnRestartClicked);
        }
        if (exitButton != null) {
            exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }

    private void Update() {
        if (!isSubmitRestarts || submitAction == null
            || !submitAction.action.WasPressedThisFrame()) {
            return;
        }

        // with a highlighted option the confirm press belongs to its button
        GameObject selected = EventSystem.current != null
            ? EventSystem.current.currentSelectedGameObject
            : null;
        if (selected != null && selected.activeInHierarchy) {
            return;
        }

        GameController game = GameController.I;
        if (game != null && game.CanRestartNow) {
            game.RestartRun();
        }
    }

    // Event Handlers  #########################################################
    private void OnResumeClicked() {
        if (GameController.I != null) {
            GameController.I.SetPaused(false);
        }
    }

    private void OnRestartClicked() {
        if (GameController.I != null) {
            GameController.I.RestartRun();
        }
    }

    private void OnExitClicked() {
        SceneNames.Load(SceneNames.LEVEL_SELECT);
    }
}
