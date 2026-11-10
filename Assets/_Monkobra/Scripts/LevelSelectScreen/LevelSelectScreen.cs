using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// the mode menu: Normal Play, Endless Mode (locked until the Cobra King has
/// been beaten once), and Back to the title
/// </summary>
public class LevelSelectScreen: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("loads the normal run")]
    private Button normalButton;

    [SerializeField]
    [Tooltip("loads the endless run, greyed out while locked")]
    private Button endlessButton;

    [SerializeField]
    [Tooltip("returns to the title screen")]
    private Button backButton;

    [SerializeField]
    [Tooltip("unlock hint, shown only while Endless is locked")]
    private TMP_Text endlessLockedHint;

    [SerializeField]
    [Tooltip("goes back to the title, the UI/Cancel action (Esc)")]
    private InputActionReference cancelAction;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (normalButton == null || endlessButton == null
            || backButton == null) {
            Debug.LogError(
                "LevelSelectScreen:\tmust assign Inspector Fields: "
                    + "Normal Button, Endless Button, Back Button",
                this
            );
            enabled = false;
            return;
        }

        if (endlessLockedHint == null) {
            Debug.LogError(
                "LevelSelectScreen:\tmust assign Inspector Field: "
                    + "Endless Locked Hint",
                this
            );
        }

        if (cancelAction == null) {
            Debug.LogError(
                "LevelSelectScreen:\tmust assign Inspector Field: "
                    + "Cancel Action",
                this
            );
        }
    }

    private void OnEnable() {
        cancelAction?.action.Enable();
    }

    private void OnDisable() {
        cancelAction?.action.Disable();
    }

    private void Start() {
        bool isUnlocked = ProgressStore.IsEndlessUnlocked;
        endlessButton.interactable = isUnlocked;
        if (endlessLockedHint != null) {
            endlessLockedHint.gameObject.SetActive(!isUnlocked);
        }

        normalButton.onClick.AddListener(OnNormalClicked);
        endlessButton.onClick.AddListener(OnEndlessClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void Update() {
        if (cancelAction != null && cancelAction.action.WasPressedThisFrame()) {
            OnBackClicked();
        }
    }

    private void OnDestroy() {
        if (normalButton != null) {
            normalButton.onClick.RemoveListener(OnNormalClicked);
        }
        if (endlessButton != null) {
            endlessButton.onClick.RemoveListener(OnEndlessClicked);
        }
        if (backButton != null) {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    // Event Handlers  #########################################################
    private void OnNormalClicked() {
        SceneNames.Load(SceneNames.FULL);
    }

    private void OnEndlessClicked() {
        if (ProgressStore.IsEndlessUnlocked) {
            SceneNames.Load(SceneNames.ENDLESS);
        }
    }

    private void OnBackClicked() {
        SceneNames.Load(SceneNames.TITLE);
    }
}
