using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Answers, for every tutorial pop up and cut scene, whether to skip it, so
/// a play test gets to the climb at once. The dev switch skips only in the
/// Editor or a development build (<see cref="Debug.isDebugBuild"/>) w/ its
/// checkbox on; a release build never skips, whatever the checkbox says.
/// <para>
/// A component of the across-scene <c>GameController</c> prefab, which every
/// scene holds, so every scene has the checkbox; tick it on the prefab asset. Items ask
/// <see cref="ShouldSkip"/> by their <see cref="TutorialItem"/> key and tell
/// <see cref="MarkSeen"/> once shown or done.
/// </para>
/// </summary>
[DefaultExecutionOrder(-990)]
[RequireComponent(typeof(GameController))]
public class TutorialSkipManager: MonoBehaviour {
    // Public Members  #########################################################
    public static TutorialSkipManager I {
        get; private set;
    }

    /// <param name="item">the tutorial or cut scene asking</param>
    /// <returns>if the item is skipped now, ie the dev switch is on, or it was
    /// shown this session as many times as the config allows</returns>
    public bool ShouldSkip(TutorialItem item) {
        if (Debug.isDebugBuild && isSkippingTutorialsAndCutscenes) {
            return true;
        }
        if (!showCounts.TryGetValue(item, out int shown)) {
            return false;
        }
        int limit = config != null ? config.GetShowLimit(item) : 1;
        return shown >= limit;
    }

    /// <summary>
    /// the item calls it once per show or completion; each call counts one
    /// show toward its limit
    /// </summary>
    /// <param name="item">the tutorial or cut scene that was seen</param>
    public void MarkSeen(TutorialItem item) {
        showCounts.TryGetValue(item, out int shown);
        showCounts[item] = shown + 1;
    }

    // Inspector Fields  #######################################################
    [Header("Tuning")]
    [SerializeField]
    [Tooltip("skip tutorial pop ups & cut scenes; only in the Editor or a "
        + "development build, a release build never skips")]
    private bool isSkippingTutorialsAndCutscenes;

    [SerializeField]
    [Tooltip("how often each item shows before it skips; none skips each "
        + "item after its first show")]
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
    // times each item showed since launch; lives w/ this object, so only a
    // close clears it
    private readonly Dictionary<TutorialItem, int> showCounts = new();
}
