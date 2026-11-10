using TMPro;
using UnityEngine;

/// <summary>
/// Fills the lose screen's final height and final score labels, and the
/// banana, pineapple, and coconut count rows, each time the screen is
/// enabled. Attach to the lose screen panel.
/// <para>
/// Height is the highest point reached, read from
/// <see cref="RampedDifficultyManager.HighestPlayerClimbedYDistance"/>, shown
/// as whole units. Score is <see cref="ScoreManager.TotalScore"/>. Each fruit
/// row shows its <see cref="ScoreManager"/> collected count.
/// </para>
/// </summary>
public class LoseScreen: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("label for the final height")]
    private TMP_Text finalHeightText;

    [SerializeField]
    [Tooltip("label for the final score")]
    private TMP_Text finalScoreText;

    [SerializeField]
    [Tooltip("icon and count row for the bananas collected")]
    private LoseScreenCountRow bananaRow;

    [SerializeField]
    [Tooltip("icon and count row for the pineapples collected")]
    private LoseScreenCountRow pineappleRow;

    [SerializeField]
    [Tooltip("icon and count row for the coconuts collected")]
    private LoseScreenCountRow coconutRow;

    [SerializeField]
    [Tooltip("text before the final height number")]
    private string heightPrefix = "Final Height: ";

    [SerializeField]
    [Tooltip("text before the final score number")]
    private string scorePrefix = "Final Score: ";

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (finalHeightText == null) {
            Debug.LogError(
                "LoseScreen:\tmust assign Inspector Field: "
                    + "Final Height Text",
                this
            );
        }
        if (finalScoreText == null) {
            Debug.LogError(
                "LoseScreen:\tmust assign Inspector Field: "
                    + "Final Score Text",
                this
            );
        }
        if (bananaRow == null) {
            Debug.LogError(
                "LoseScreen:\tmust assign Inspector Field: Banana Row",
                this
            );
        }
        if (pineappleRow == null) {
            Debug.LogError(
                "LoseScreen:\tmust assign Inspector Field: Pineapple Row",
                this
            );
        }
        if (coconutRow == null) {
            Debug.LogError(
                "LoseScreen:\tmust assign Inspector Field: Coconut Row",
                this
            );
        }
    }

    private void OnEnable() {
        RefreshFinalHeight();
        RefreshFinalScore();
        RefreshFruitCounts();
    }

    // private methods  ########################################################
    private void RefreshFinalHeight() {
        if (finalHeightText == null) {
            return;
        }
        if (RampedDifficultyManager.I == null) {
            Debug.LogWarning(
                "LoseScreen:\tRampedDifficultyService not found",
                this
            );
            return;
        }

        int heightU = Mathf.Max(
            0,
            Mathf.FloorToInt(
                RampedDifficultyManager.I.HighestPlayerClimbedYDistance
            )
        );
        finalHeightText.text = heightPrefix + heightU.ToString("N0");

        if (Debug.isDebugBuild) {
            Debug.Log($"LoseScreen:\tfinal height {heightU}");
        }
    }

    private void RefreshFinalScore() {
        if (finalScoreText == null) {
            return;
        }
        if (ScoreManager.I == null) {
            Debug.LogWarning(
                "LoseScreen:\tScoreManager not found",
                this
            );
            return;
        }

        long score = ScoreManager.I.TotalScore;
        finalScoreText.text = scorePrefix + score.ToString("N0");

        if (Debug.isDebugBuild) {
            Debug.Log($"LoseScreen:\tfinal score {score}");
        }
    }

    private void RefreshFruitCounts() {
        if (ScoreManager.I == null) {
            Debug.LogWarning(
                "LoseScreen:	ScoreManager not found",
                this
            );
            return;
        }

        ScoreManager score = ScoreManager.I;
        if (bananaRow != null) {
            bananaRow.SetCount(score.BananaCollectedCount);
        }
        if (pineappleRow != null) {
            pineappleRow.SetCount(score.PineappleCollectedCount);
        }
        if (coconutRow != null) {
            coconutRow.SetCount(score.CoconutCollectedCount);
        }

        if (Debug.isDebugBuild) {
            Debug.Log(
                $"LoseScreen:	banana {score.BananaCollectedCount}, "
                    + $"pineapple {score.PineappleCollectedCount}, "
                    + $"coconut {score.CoconutCollectedCount}"
            );
        }
    }
}
