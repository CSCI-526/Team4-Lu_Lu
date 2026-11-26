using UnityEngine;

/// <summary>
/// Dev switch that skips every tutorial pop up and cut scene, so a play test
/// gets to the climb at once. It skips only in the Editor or a development
/// build (<see cref="Debug.isDebugBuild"/>) w/ its checkbox on; a release
/// build never skips, whatever the checkbox says.
/// <para>
/// Sits on the across-scene <c>GameController</c> prefab (<c>Resources/</c>),
/// so every scene has the checkbox; tick it on the prefab asset. Scripts ask
/// <see cref="IsSkipping"/>, nothing calls it.
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
        // a duplicate GameController destroys itself, never the first switch
        if (I != null && I != this) {
            return;
        }

        I = this;

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
