using TMPro;
using UnityEngine;

public class LeaderboardWidget: MonoBehaviour {
    [Header("References")]
    [SerializeField]
    private LeaderboardRowWidget[] rows;

    [SerializeField]
    private TMP_Text playerRankLabel;

    [Header("Simulated Rivals")]
    [SerializeField, Min(0)]
    private int minRivalScore = 100;

    [SerializeField, Min(0)]
    private int maxRivalScore = 2400;

    [SerializeField]
    private string[] rivalNames = {
        "BananaBandit",
        "TreeHopper",
        "CobraDodger",
        "JungleJumper",
        "SwingKing",
        "BarkClimber",
        "LeafRunner",
        "MangoMischief",
        "BranchBuddy",
        "VineRider",
        "CanopyKid",
        "GoldenBanana"
    };

    private void Awake() {
        if (rows == null || rows.Length == 0
            || playerRankLabel == null) {
            Debug.LogError(
                "LeaderboardWidget: assign rows and playerRankLabel.",
                this
            );

            enabled = false;
            return;
        }

        foreach (LeaderboardRowWidget row in rows) {
            if (row == null) {
                Debug.LogError(
                    "LeaderboardWidget: a row is not assigned.",
                    this
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
                "LeaderboardWidget: ScoreManager not found.",
                this
            );

            return;
        }

        ShowScore(score.TotalScore);
    }

    public void ShowScore(long score) {
        if (rows == null || rows.Length == 0
            || playerRankLabel == null) {
            return;
        }

        // 总行数减去玩家的一行，就是虚拟对手数量。
        SessionLeaderboard table =
            SessionLeaderboard.ForCurrentPlaytest(
                rows.Length - 1,
                minRivalScore,
                maxRivalScore,
                rivalNames
            );

        // 插入本局玩家成绩，取得排序后的结果。
        SessionLeaderboard.Entry[] standings =
            table.GetStandings(score);

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

        playerRankLabel.text =
            "YOUR RANK  #" + playerRank + " / " + standings.Length;
    }

    private void OnValidate() {
        minRivalScore = Mathf.Max(0, minRivalScore);
        maxRivalScore = Mathf.Max(minRivalScore, maxRivalScore);
    }
}