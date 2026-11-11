using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puts one text under two wordings, a keyboard one and a touch one, and
/// follows <see cref="ControlStyleManager"/> live, so a hint names the keys
/// the player really has. A blank wording shows nothing. Works on a
/// <see cref="TMP_Text"/> or a legacy <see cref="Text"/> next to it.
/// </summary>
public class ControlStyleLabel: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [TextArea]
    [Tooltip("wording while a keyboard is in use")]
    private string keyboardText = "";

    [SerializeField]
    [TextArea]
    [Tooltip("wording while touch is in use; blank hides the text")]
    private string touchText = "";

    [SerializeField]
    [Tooltip("TMP text to write; found on this object when empty")]
    private TMP_Text tmpLabel;

    [SerializeField]
    [Tooltip("legacy text to write; found on this object when empty")]
    private Text legacyLabel;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (tmpLabel == null) {
            tmpLabel = GetComponent<TMP_Text>();
        }
        if (legacyLabel == null) {
            legacyLabel = GetComponent<Text>();
        }

        if (tmpLabel == null && legacyLabel == null) {
            Debug.LogError(
                "ControlStyleLabel:\tneeds a TMP_Text or Text to write",
                this
            );
            enabled = false;
        }
    }

    private void OnEnable() {
        ControlStyleManager.StyleChanged += Apply;
        Apply(ControlStyleManager.Current);
    }

    private void OnDisable() {
        ControlStyleManager.StyleChanged -= Apply;
    }

    // Private Methods  ########################################################
    private void Apply(ControlStyle style) {
        string text = style == ControlStyle.Touch ? touchText : keyboardText;
        if (tmpLabel != null) {
            tmpLabel.text = text;
        }
        if (legacyLabel != null) {
            legacyLabel.text = text;
        }
    }
}
