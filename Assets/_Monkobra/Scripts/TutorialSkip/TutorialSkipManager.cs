using System.Collections.Generic;
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

    /// <param name="item">the tutorial or cut scene asking</param>
    /// <returns>if the item is skipped now, ie the dev switch is on, or it was
    /// seen this session and the config skips it once seen</returns>
    public bool ShouldSkip(TutorialItem item) {
        if (Debug.isDebugBuild && isSkippingTutorialsAndCutscenes) {
            return true;
        }
        return seen.Contains(item)
            && (config == null || config.IsSkippedOnceSeen(item));
    }

    /// <summary>
    /// the item calls it once it has shown or been completed
    /// </summary>
    /// <param name="item">the tutorial or cut scene that was seen</param>
    public void MarkSeen(TutorialItem item) {
        seen.Add(item);
    }

    // Inspector Fields  #######################################################
    [Header("Tuning")]
    [SerializeField]
    [Tooltip("skip tutorial pop ups & cut scenes; only in the Editor or a "
        + "development build, a release build never skips")]
    private bool isSkippingTutorialsAndCutscenes;

    [SerializeField]
    [Tooltip("whether each item skips once seen; none skips every seen item")]
    private TutorialSkipConfig config;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // a duplicate GameController destroys itself, never the first manager
        if (I != null && I != this) {
            return;
        }

        I = this;

        if (Debug.isDebugBuild && isSkippingTutorialsAndCutscenes) {
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

    // private members  ########################################################
    // items seen since launch; lives w/ this object, so only a close clears it
    private readonly HashSet<TutorialItem> seen = new();
}
