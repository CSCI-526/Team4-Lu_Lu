using TMPro;
using UnityEngine;

/// <summary>
/// Fills the nested leaderboard whenever the lose screen opens, after
/// GameController.LoseGame has settled ScoreManager's final score.
/// </summary>
public class LeaderboardWidget: MonoBehaviour {
    // Public Methods  #########################################################
    public void ShowScore(long score) {
        if (rows == null || rows.Length == 0 || playerRankLabel == null) {
            return;
        }
        SessionLeaderboard table = SessionLeaderboard.ForCurrentPlaytest(
            rows.Length - 1, minRivalScore, maxRivalScore, rivalNames
        );
        SessionLeaderboard.Entry[] standings = table.GetStandings(score);
        int playerRank = 0;
        for (int i = 0; i < rows.Length; i++) {
            if (rows[i] == null) {
                continue;
            }
            bool hasEntry = i < standings.Length;
            rows[i].gameObject.SetActive(hasEntry);
            if (!hasEntry) {
                continue;
            }
            rows[i].ShowEntry(standings[i], i + 1);
            if (standings[i].IsPlayer) {
                playerRank = i + 1;
            }
        }
        playerRankLabel.text = "YOUR RANK  #" + playerRank
            + " / " + standings.Length;
    }

    // Inspector Fields  #######################################################
    [Header("References")]
    [SerializeField]
    [Tooltip("rows in visual order; nine rivals plus this run's player")]
    private LeaderboardRowWidget[] rows;
    [SerializeField]
    private TMP_Text playerRankLabel;

    [Header("Simulated Rivals")]
    [SerializeField, Min(0)]
    [Tooltip("lowest possible rival score, inclusive; read once per playtest")]
    private int minRivalScore = 500;
    [SerializeField, Min(0)]
    [Tooltip("highest possible rival score, inclusive; read once per playtest")]
    private int maxRivalScore = 5000;
    [SerializeField]
    [Tooltip("name pool; selected once, without duplicates, per playtest")]
    private string[] rivalNames = {
        "FasterThanUrMom", "BananaKing", "BananaQueen", "PeelMeDaddy",
        "SwingKing", "1stMonkey", "Ceasar", "Minion",
        "BranchBuddy", "Stupid Ape", "CSCI526", "Nanaba"
    };

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (rows == null || rows.Length == 0 || playerRankLabel == null) {
            Debug.LogError(
                "LeaderboardWidget:\tmust assign rows and playerRankLabel",
                this
            );
            enabled = false;
            return;
        }
        foreach (LeaderboardRowWidget row in rows) {
            if (row == null) {
                Debug.LogError(
                    "LeaderboardWidget:\trows contains an unassigned row", this
                );
                enabled = false;
                return;
            }
        }
    }

    private void OnEnable() {
        if (!enabled) {
            return;
        }
        ScoreManager score = ScoreManager.I;
        if (score == null) {
            Debug.LogError(
                "LeaderboardWidget:\tScoreManager not found", this
            );
            return;
        }
        ShowScore(score.TotalScore);
    }

    private void OnValidate() {
        minRivalScore = Mathf.Max(0, minRivalScore);
        maxRivalScore = Mathf.Max(minRivalScore, maxRivalScore);
    }
}
