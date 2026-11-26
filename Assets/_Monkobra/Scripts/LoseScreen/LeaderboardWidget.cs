using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Fills the nested leaderboard whenever the lose screen opens, after
/// RunController.LoseGame has settled ScoreManager's final score.
/// </summary>
public class LeaderboardWidget: MonoBehaviour {
    // Public Members  #########################################################
    public FirebaseLeaderboardConfig FirebaseConfig => firebaseConfig;

    // Public Methods  #########################################################
    public bool SavePlayerName(string name) {
        if (firebaseConfig == null || !firebaseConfig.IsConfigured
            || !new FirebaseLeaderboardClient(firebaseConfig).RememberName(
                name
            )) {
            return false;
        }
        ShowScore(lastShownScore);
        return true;
    }

    public void ShowScore(long score) {
        if (rows == null || rows.Length == 0 || playerRankLabel == null) {
            return;
        }
        lastShownScore = score;
        int version = ++requestVersion;
        if (firebaseConfig != null && firebaseConfig.IsConfigured) {
            ShowOnline(score, version);
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
        playerRankLabel.text = "PRACTICE  #" + playerRank
            + " / " + standings.Length;
    }

    // Inspector Fields  #######################################################
    [Header("References")]
    [SerializeField]
    [Tooltip("rows in visual order; nine rivals plus this run's player")]
    private LeaderboardRowWidget[] rows;
    [SerializeField]
    [Tooltip("text showing the player's own rank")]
    private TMP_Text playerRankLabel;

    [Header("Online Leaderboard (Optional)")]
    [SerializeField]
    [Tooltip("Blank or disabled keeps the original simulated practice board.")]
    private FirebaseLeaderboardConfig firebaseConfig;

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
        "BranchBuddy", "StupidApe", "CSCI526", "Nanaba"
    };

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
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

    private void OnDisable() {
        // Uploads may finish after a restart; an old result must not repaint
        // a later run, or touch a destroyed widget.
        requestVersion++;
    }

    // private members  ########################################################
    private int requestVersion;
    private long lastShownScore;

    // private methods  ########################################################
    private async void ShowOnline(long score, int version) {
        FirebaseLeaderboardClient client =
            new FirebaseLeaderboardClient(firebaseConfig);
        // HACK: Hardcore scores post to the old "endless" board key, rename
        // it w/ the Firebase rules & client in one later change
        string board = GameController.I != null
            && GameController.I.Mode == RunMode.Hardcore ? "endless" : "normal";
        long best = client.RememberScore(board, score);
        ShowPersonalBest(best);
        playerRankLabel.text = "CONNECTING...";
        try {
            FirebaseLeaderboardClient.Result result =
                await client.SubmitAndLoad(
                board, score, rows.Length
            );
            if (
                this == null || !isActiveAndEnabled || version != requestVersion
            ) {
                return;
            }
            int playerRank = 0;
            for (int i = 0; i < rows.Length; i++) {
                bool hasEntry = i < result.Entries.Length;
                rows[i].gameObject.SetActive(hasEntry);
                if (hasEntry) {
                    rows[i].ShowEntry(result.Entries[i], i + 1);
                    if (result.Entries[i].IsPlayer) {
                        playerRank = i + 1;
                    }
                }
            }
            playerRankLabel.text = playerRank > 0
                ? "YOUR RANK  #" + playerRank
                : "BEST " + result.PersonalBest.ToString("N0")
                    + "  |  OUTSIDE TOP " + Math.Min(10, rows.Length);
        }
        catch (Exception error) {
            if (
                this == null || !isActiveAndEnabled || version != requestVersion
            ) {
                return;
            }
            ShowPersonalBest(best);
            playerRankLabel.text = "OFFLINE  |  SAVED LOCALLY";
            Debug.LogWarning("LeaderboardWidget:\t" + error.Message, this);
        }
    }

    private void ShowPersonalBest(long score) {
        for (int i = 0; i < rows.Length; i++) {
            rows[i].gameObject.SetActive(i == 0);
        }
        rows[0].ShowEntry(new SessionLeaderboard.Entry(
            "YOU", score, true, 0
        ), 0);
    }
}
