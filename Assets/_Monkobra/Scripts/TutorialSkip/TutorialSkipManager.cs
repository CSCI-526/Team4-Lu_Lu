using UnityEngine;

/// <summary>
/// Answers, for every tutorial pop up and cut scene, whether to skip it, so
/// a play test gets to the climb at once. The dev switch skips only in the
/// Editor or a development build (<see cref="Debug.isDebugBuild"/>) w/ its
/// checkbox on; a release build never skips, whatever the checkbox says.
/// <para>
/// Sits on the across-scene <c>GameController</c> prefab (<c>Resources/</c>),
/// so every scene has the checkbox; tick it on the prefab asset. Items ask
/// <see cref="ShouldSkip"/> by their <see cref="TutorialItem"/> key and tell
/// <see cref="MarkSeen"/> once shown or done.
/// </para>
/// </summary>
public class TutorialSkipManager: MonoBehaviour {
    // Public Members  #########################################################
    public static TutorialSkipManager I {
        get; private set;
    }

    /// <returns>if tutorials and cut scenes are skipped now, ie a development
    /// build or the Editor w/ the checkbox on; never in a release build
    /// </returns>
    /// <remarks>temporary, until every reader asks by its own key</remarks>
    public static bool IsSkipping =>
        Debug.isDebugBuild && I != null && I.isSkippingTutorialsAndCutscenes;

    /// <param name="item">the tutorial or cut scene asking</param>
    /// <returns>if the item is skipped now, ie the dev switch is on</returns>
    public bool ShouldSkip(TutorialItem item) {
        return Debug.isDebugBuild && isSkippingTutorialsAndCutscenes;
    }

    /// <summary>
    /// the item calls it once it has shown or been completed
    /// </summary>
    /// <param name="item">the tutorial or cut scene that was seen</param>
    public void MarkSeen(TutorialItem item) {
        // TODO: record the item in the session set
    }

    // Inspector Fields  #######################################################
    [Header("Tuning")]
    [SerializeField]
    [Tooltip("skip tutorial pop ups & cut scenes; only in the Editor or a "
        + "development build, a release build never skips")]
    private bool isSkippingTutorialsAndCutscenes;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // a duplicate GameController destroys itself, never the first manager
        if (I != null && I != this) {
            return;
        }

        I = this;

        if (IsSkipping) {
            Debug.Log(
                "TutorialSkipManager:\tskipping tutorials and cut scenes",
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
