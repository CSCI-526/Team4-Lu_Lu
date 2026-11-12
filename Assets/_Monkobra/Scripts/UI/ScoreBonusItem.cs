using System;
using System.Globalization;
using TMPro;
using UnityEngine;

// one score bonus popup: a "+" text and a number text side by side
// both texts take the colour of the reward kind; fonts come from the prefab
public class ScoreBonusItem: MonoBehaviour {
    // Public Methods  #########################################################
    // writes the points after the plus sign and tints both texts by kind
    public void Show(RewardKind kind, long points) {
        if (valueText == null || plusText == null) {
            return;
        }
        valueText.text = points.ToString("N0", CultureInfo.InvariantCulture);
        Color color = ColorOf(kind);
        plusText.color = color;
        valueText.color = color;
    }

    // Inspector Fields  #######################################################
    [Serializable]
    private struct KindColor {
        public RewardKind kind;
        public Color color;
    }

    [SerializeField]
    [Tooltip("the plus sign text")]
    private TMP_Text plusText;

    [SerializeField]
    [Tooltip("the number text")]
    private TMP_Text valueText;

    [SerializeField]
    [Tooltip("colour per reward kind; a kind with no entry shows white")]
    private KindColor[] colors;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (plusText == null) {
            Debug.LogError(
                "ScoreBonusItem:\tmust assign Inspector Field: plusText",
                this);
        }
        if (valueText == null) {
            Debug.LogError(
                "ScoreBonusItem:\tmust assign Inspector Field: valueText",
                this);
        }
    }

    // private methods  ########################################################
    private Color ColorOf(RewardKind kind) {
        if (colors != null) {
            for (int i = 0; i < colors.Length; i++) {
                if (colors[i].kind == kind) {
                    return colors[i].color;
                }
            }
        }
        return Color.white;
    }
}
