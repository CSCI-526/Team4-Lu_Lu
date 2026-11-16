using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The on-screen pause button, for touch or a mouse: a press pauses the run
/// like the pause key. Shown only while the run is live and not paused, so it
/// never covers the tutorial, the pause screen or the lose screen; resuming
/// is the pause screen's own button.
/// <para>
/// Hides by <see cref="CanvasGroup"/> instead of deactivating its object, so
/// it keeps listening to <see cref="GameController"/>, read in <c>Start</c>
/// so Awake order never matters.
/// </para>
/// </summary>
[RequireComponent(typeof(Button), typeof(CanvasGroup))]
public class PauseButton: MonoBehaviour {
    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        button = GetComponent<Button>();
        group = GetComponent<CanvasGroup>();
    }

    private void Start() {
        game = GameController.I;
        if (game == null) {
            Debug.LogError(
                "PauseButton:\tGameController instance not found",
                this
            );
            enabled = false;
            return;
        }

        button.onClick.AddListener(OnClick);
        game.StateChanged += OnStateChanged;
        game.PauseChanged += OnPauseChanged;
        Refresh();
    }

    private void OnDestroy() {
        if (button != null) {
            button.onClick.RemoveListener(OnClick);
        }
        if (game != null) {
            game.StateChanged -= OnStateChanged;
            game.PauseChanged -= OnPauseChanged;
        }
    }

    // Private Methods  ########################################################
    private void OnClick() {
        // a selected button would take the next Space (the grab) as a confirm
        if (EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(null);
        }
        game.SetPaused(true);
    }

    private void OnStateChanged(GameState _) {
        Refresh();
    }

    private void OnPauseChanged(bool _) {
        Refresh();
    }

    // shown and pressable only while a live run is not paused
    private void Refresh() {
        bool isShown = game.IsRunLive && !game.IsPaused;
        group.alpha = isShown ? 1f : 0f;
        group.interactable = isShown;
        group.blocksRaycasts = isShown;
    }

    // Private Members  ########################################################
    private Button button;
    private CanvasGroup group;
    private GameController game;
}
