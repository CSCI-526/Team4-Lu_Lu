using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ScoreWidget: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("score source")]
    private ScoreManager scoreManager;

    [SerializeField]
    [Tooltip("text before the number; empty for the bare HUD score, and the badaboom font holds digits, comma, plus only")]
    private string prefix = "";

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (scoreManager == null) {
            Debug.LogError(
                "ScoreWidget:\tmust assign Inspector Field: scoreManager",
                this);
        }
        label = GetComponent<TMP_Text>();
    }

    private void OnEnable() {
        if (scoreManager == null) {
            return;
        }
        scoreManager.ScoreChanged += Refresh;
        Refresh(scoreManager.TotalScore);
    }

    private void OnDisable() {
        if (scoreManager != null) {
            scoreManager.ScoreChanged -= Refresh;
        }
    }

    // private members  ########################################################
    private TMP_Text label;

    // private methods  ########################################################
    private void Refresh(long score) {
        label.text = prefix + score.ToString("N0", CultureInfo.InvariantCulture);
    }
}
