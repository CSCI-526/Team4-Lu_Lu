using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dev-only button on the lose screen that downloads the session analytics
/// report: in the browser a file download, elsewhere the session file is saved.
/// <para>
/// Attach to the button object. Outside a development build the object turns
/// itself off, so a release build never shows it.
/// </para>
/// </summary>
public class LoseScreenDownloadWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("button that downloads the analytics report")]
    private Button button;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (button == null) {
            Debug.LogError(
                "LoseScreenDownloadWidget:\tmust assign Inspector Field: "
                    + "Button",
                this
            );
        }

        HideOutsideDevBuild();
    }

    private void OnEnable() {
        // covers anything that switches the object back on
        if (HideOutsideDevBuild() || button == null) {
            return;
        }

        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDisable() {
        if (button != null) {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    // Event Handlers  #########################################################
    private void OnButtonClicked() {
        if (!HackShortcutConfig.IsActive) {
            return;
        }
        if (AnalyticsManager.I == null) {
            Debug.LogWarning(
                "LoseScreenDownloadWidget:\tAnalyticsManager not found",
                this
            );
            return;
        }

        AnalyticsManager.I.DownloadReport();
    }

    // Private Helpers  ########################################################
    // whether the object was turned off, ie this is not a development build
    private bool HideOutsideDevBuild() {
        if (HackShortcutConfig.IsActive) {
            return false;
        }

        gameObject.SetActive(false);

        return true;
    }
}
