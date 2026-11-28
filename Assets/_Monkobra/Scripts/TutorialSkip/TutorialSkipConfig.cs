using System;
using UnityEngine;

/// <summary>
/// How many times each <see cref="TutorialItem"/> shows this session before
/// it skips, so one item is switched back to always showing or shown a few
/// times w/o a recompile. <see cref="TutorialSkipManager"/> reads it; an item
/// missing here skips after its first show.
/// </summary>
[CreateAssetMenu(
    fileName = "TutorialSkipConfig",
    menuName = "Scriptable Objects/TutorialSkipConfig"
)]
public class TutorialSkipConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <param name="item">the tutorial or cut scene asked about</param>
    /// <returns>times the item shows this session before it skips: its
    /// show limit when above 0, else 1 iff it skips once seen, else no limit
    /// </returns>
    public int GetShowLimit(TutorialItem item) {
        foreach (Entry entry in entries) {
            if (entry.item != item) {
                continue;
            }
            if (entry.showLimit > 0) {
                return entry.showLimit;
            }
            return entry.isSkippedOnceSeen ? 1 : int.MaxValue;
        }
        return 1;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private Entry[] entries;

    [Serializable]
    private struct Entry {
        public TutorialItem item;

        [Tooltip("skip the item once it was seen this session; off shows it "
            + "every run; a show limit above 0 wins over it")]
        public bool isSkippedOnceSeen;

        [Min(0)]
        [Tooltip("times the item shows this session before it skips; 0 "
            + "defers to the box above")]
        public int showLimit;
    }
}
