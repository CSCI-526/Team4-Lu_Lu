using TMPro;
using UnityEngine;

/// <summary>
/// One fruit row on the lose screen: the fruit's icon, then a count label
/// such as "x12". The icon sprite is set on the prefab instance, this script
/// only writes the count.
/// </summary>
public class LoseScreenCountRow: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>Shows the count as "x" plus the number.</summary>
    /// <param name="count">how many of the fruit the run collected</param>
    public void SetCount(int count) {
        if (countText == null) {
            return;
        }
        countText.text = "x" + count.ToString("N0");
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("label for the count, shown as x plus the number")]
    private TMP_Text countText;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (countText == null) {
            Debug.LogError(
                "LoseScreenCountRow:\tmust assign Inspector Field: "
                    + "Count Text",
                this
            );
        }
    }
}
