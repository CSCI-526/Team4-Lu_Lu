using System;
using UnityEngine;

/// <summary>
/// Whether each <see cref="TutorialItem"/> skips once it was seen this
/// session, so one item is switched back to always showing w/o a recompile.
/// <see cref="TutorialSkipManager"/> reads it; an item missing here skips.
/// </summary>
[CreateAssetMenu(
    fileName = "TutorialSkipConfig",
    menuName = "Scriptable Objects/TutorialSkipConfig"
)]
public class TutorialSkipConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <param name="item">the tutorial or cut scene asked about</param>
    /// <returns>if the item skips once seen this session</returns>
    public bool IsSkippedOnceSeen(TutorialItem item) {
        foreach (Entry entry in entries) {
            if (entry.item == item) {
                return entry.isSkippedOnceSeen;
            }
        }
        return true;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private Entry[] entries;

    [Serializable]
    private struct Entry {
        public TutorialItem item;

        [Tooltip("skip the item once it was seen this session; off shows it "
            + "every run")]
        public bool isSkippedOnceSeen;
    }
}
