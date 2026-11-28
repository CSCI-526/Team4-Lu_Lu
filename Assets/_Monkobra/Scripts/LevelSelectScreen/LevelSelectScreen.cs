using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// the mode menu: Normal Play, Hardcore Mode (locked until the Cobra King has
/// been beaten once), and Back to the title
/// </summary>
public class LevelSelectScreen: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("loads the normal run")]
    private Button normalButton;

    [SerializeField]
    [Tooltip("loads the hardcore run, greyed out while locked")]
    [FormerlySerializedAs("endlessButton")]
    private Button hardcoreButton;

    [SerializeField]
    [Tooltip("returns to the title screen")]
    private Button backButton;

    [SerializeField]
    [Tooltip("unlock hint, shown only while Hardcore is locked")]
    [FormerlySerializedAs("endlessLockedHint")]
    private TMP_Text hardcoreLockedHint;

    [SerializeField]
    [Tooltip("goes back to the title, the UI/Cancel action (Esc, P)")]
    private InputActionReference cancelAction;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (normalButton == null || hardcoreButton == null
            || backButton == null) {
            Debug.LogError(
                "LevelSelectScreen:\tmust assign Inspector Fields: "
                    + "Normal Button, Hardcore Button, Back Button",
                this
            );
            enabled = false;
            return;
        }

        if (hardcoreLockedHint == null) {
            Debug.LogError(
                "LevelSelectScreen:\tmust assign Inspector Field: "
                    + "Hardcore Locked Hint",
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
        bool isUnlocked = ProgressStore.IsHardcoreUnlocked;
        hardcoreButton.interactable = isUnlocked;
        if (hardcoreLockedHint != null) {
            hardcoreLockedHint.gameObject.SetActive(!isUnlocked);
        }

        normalButton.onClick.AddListener(OnNormalClicked);
        hardcoreButton.onClick.AddListener(OnHardcoreClicked);
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
        if (hardcoreButton != null) {
            hardcoreButton.onClick.RemoveListener(OnHardcoreClicked);
        }
        if (backButton != null) {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    // Event Handlers  #########################################################
    private void OnNormalClicked() {
        SceneNames.Load(SceneNames.GAMEPLAY, RunMode.Full);
    }

    private void OnHardcoreClicked() {
        if (ProgressStore.IsHardcoreUnlocked) {
            SceneNames.Load(SceneNames.GAMEPLAY, RunMode.Hardcore);
        }
    }

    private void OnBackClicked() {
        SceneNames.Load(SceneNames.TITLE);
    }
}
