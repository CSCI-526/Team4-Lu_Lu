using UnityEngine;

/// <summary>
/// Dev switch that skips every tutorial pop up and cut scene, so a play test
/// gets to the climb at once. It skips only in the Editor or a development
/// build (<see cref="Debug.isDebugBuild"/>) w/ its checkbox on; a release
/// build never skips, whatever the checkbox says.
/// <para>
/// Lives across scene loads, so the title, the intro comic and the gameplay
/// scene all read the same checkbox. Created by
/// <see cref="PersistentBootstrapLoader"/> from the
/// <c>Resources/TutorialSkipSwitch</c> prefab; tick the checkbox on that
/// prefab. Scripts ask <see cref="IsSkipping"/>, nothing calls it.
/// </para>
/// </summary>
public class TutorialSkipSwitch: MonoBehaviour {
    // Public Members  #########################################################
    public static TutorialSkipSwitch I {
        get; private set;
    }

    /// <returns>if tutorials and cut scenes are skipped now, ie a development
    /// build or the Editor w/ the checkbox on; never in a release build
    /// </returns>
    public static bool IsSkipping =>
        Debug.isDebugBuild && I != null && I.isSkippingTutorialsAndCutscenes;

    // Inspector Fields  #######################################################
    [Header("Tuning")]
    [SerializeField]
    [Tooltip("skip tutorial pop ups & cut scenes; only in the Editor or a "
        + "development build, a release build never skips")]
    private bool isSkippingTutorialsAndCutscenes;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (I != null && I != this) {
            Destroy(gameObject);
            return;
        }

        I = this;
        // one switch spans the whole launch, across every scene load
        DontDestroyOnLoad(gameObject);

        if (IsSkipping) {
            Debug.Log(
                "TutorialSkipSwitch:\tskipping tutorials and cut scenes",
                this
            );
        }
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }
}
