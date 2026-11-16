using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Fills the TMP text it sits on with one line per hack key, ie
/// <c>F8: download the analytics session report</c>, so a tester can see what
/// each dev key does. The list starts hidden, so it never covers the HUD in a
/// playtest, and the <see cref="HackAction.ToggleHints"/> key (F9) shows or
/// hides it. Outside a development build the hints are off, so it switches
/// its own object off; the keys still work.
/// <para>
/// Put it on the TMP text object itself. The text is written once, in
/// <c>Awake</c>.
/// </para>
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class HackShortcutPanel: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("dev/hack keys listed, in the order of its entries")]
    private HackShortcutConfig hackShortcuts;

    [SerializeField]
    [Tooltip("if the list shows when a scene starts; the ToggleHints key "
        + "shows or hides it either way")]
    private bool isShownAtStart;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (!HackShortcutConfig.AreHintsShown) {
            gameObject.SetActive(false);
            return;
        }

        // Inspector Assignment Guard  -----------------------------------------
        if (hackShortcuts == null) {
            Debug.LogWarning(
                "HackShortcutPanel:\tmust assign Inspector Field: Hack "
                    + "Shortcuts",
                this
            );
            enabled = false;
            return;
        }

        label = GetComponent<TMP_Text>();
        if (label == null) {
            Debug.LogError(
                "HackShortcutPanel:\tfail to get Component: TMP_Text",
                this
            );
            enabled = false;
            return;
        }

        label.text = BuildHelpText();
        label.enabled = isShownAtStart; // the object stays on, to hear F9
        if (Debug.isDebugBuild) {
            Debug.Log("HackShortcutPanel:\thelp text ready");
        }
    }

    // the toggle key shows or hides the list
    private void Update() {
        if (!HackShortcutConfig.IsActive) {
            return;
        }
        if (!hackShortcuts.WasPressedThisFrame(HackAction.ToggleHints)) {
            return;
        }
        label.enabled = !label.enabled;
    }

    // private members  ########################################################
    private TMP_Text label;

    // private methods  ########################################################
    // one "key: description" line per entry
    private string BuildHelpText() {
        StringBuilder sb = new StringBuilder();
        foreach (HackShortcutConfig.Entry entry in hackShortcuts.Entries) {
            if (sb.Length > 0) {
                sb.Append('\n');
            }
            sb.Append(entry.Key).Append(": ").Append(entry.Description);
        }
        return sb.ToString();
    }
}
