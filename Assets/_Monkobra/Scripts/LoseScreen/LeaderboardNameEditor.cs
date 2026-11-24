using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Optional nickname for the anonymous player; never a
/// login.</summary>
[DefaultExecutionOrder(100)]
public class LeaderboardNameEditor: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    private LeaderboardWidget leaderboard;
    [SerializeField]
    private TMP_InputField nameInput;
    [SerializeField]
    private Button saveButton;
    [SerializeField]
    private TMP_Text hintLabel;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (leaderboard == null || nameInput == null || saveButton == null
            || hintLabel == null) {
            Debug.LogError("LeaderboardNameEditor:\tmust assign leaderboard, "
                + "nameInput, saveButton and hintLabel", this);
            enabled = false;
        }
    }

    private void OnEnable() {
        if (!enabled) {
            return;
        }
        FirebaseLeaderboardConfig config = leaderboard.FirebaseConfig;
        bool online = config != null && config.IsConfigured;
        nameInput.interactable = online;
        nameInput.SetTextWithoutNotify(online
            ? new FirebaseLeaderboardClient(config).PlayerName : "");
        nameInput.onValueChanged.AddListener(OnNameChanged);
        nameInput.onSubmit.AddListener(OnSubmitted);
        nameInput.onSelect.AddListener(OnTextSelected);
        nameInput.onDeselect.AddListener(OnTextDeselected);
        saveButton.onClick.AddListener(SaveName);
        OnNameChanged(nameInput.text);
        if (!online) {
            hintLabel.text = "Practice board: online names unavailable.";
        }
    }

    private void OnDisable() {
        if (nameInput != null) {
            if (nameInput.isFocused) {
                TextInputFocus.ProtectThisFrame();
            }
            nameInput.onValueChanged.RemoveListener(OnNameChanged);
            nameInput.onSubmit.RemoveListener(OnSubmitted);
            nameInput.onSelect.RemoveListener(OnTextSelected);
            nameInput.onDeselect.RemoveListener(OnTextDeselected);
        }
        if (saveButton != null) {
            saveButton.onClick.RemoveListener(SaveName);
        }
        RestoreIme();
    }

    // TMP activates its field in LateUpdate and enables IME there. Run after
    // that activation, so Microsoft's Zhuyin layout can enter plain Latin
    // characters without Shift fighting a new composition session.
    private void LateUpdate() {
        if (nameInput != null && nameInput.isFocused) {
            if (imeInput != null) {
                imeInput.imeCompositionMode = IMECompositionMode.Off;
            }
            imeKeyboard?.SetIMEEnabled(false);
        }
    }

    // private members  ########################################################
    private BaseInput imeInput;
    private Keyboard imeKeyboard;
    private IMECompositionMode previousImeMode;
    private bool wasImeSelected;
    private bool ownsIme;

    // Event Handlers  #########################################################
    private void OnNameChanged(string value) {
        bool valid = FirebaseLeaderboardClient.TryNormalizeName(value, out _);
        saveButton.interactable = nameInput.interactable && valid;
        hintLabel.text = string.IsNullOrWhiteSpace(value) || valid
            ? "Names may repeat. Each player keeps their own score."
            : "Use 1-16 letters/numbers, spaces, - or _.";
    }

    private void OnSubmitted(string value) {
        if (!nameInput.wasCanceled) {
            SaveName();
        }
    }

    private void OnTextSelected(string value) {
        if (!ownsIme) {
            imeInput = EventSystem.current != null
                ? EventSystem.current.currentInputModule?.input : null;
            imeKeyboard = Keyboard.current;
            previousImeMode = imeInput != null
                ? imeInput.imeCompositionMode : IMECompositionMode.Auto;
            wasImeSelected = imeKeyboard != null
                && imeKeyboard.imeSelected.isPressed;
            ownsIme = true;
        }
        TextInputFocus.ProtectThisFrame();
    }

    private void OnTextDeselected(string value) {
        RestoreIme();
        TextInputFocus.ProtectThisFrame();
    }

    private void RestoreIme() {
        if (!ownsIme) {
            return;
        }
        if (imeInput != null) {
            imeInput.imeCompositionMode = previousImeMode;
        }
        imeKeyboard?.SetIMEEnabled(wasImeSelected);
        imeInput = null;
        imeKeyboard = null;
        ownsIme = false;
    }

    private void SaveName() {
        TextInputFocus.ProtectThisFrame();
        if (!leaderboard.SavePlayerName(nameInput.text)) {
            hintLabel.text = "Use 1-16 letters/numbers, spaces, - or _.";
            return;
        }
        FirebaseLeaderboardClient.TryNormalizeName(nameInput.text,
            out string normalized);
        nameInput.SetTextWithoutNotify(normalized);
        hintLabel.text = "Name saved on this device.";
    }
}
