using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardRowWidget: MonoBehaviour {
    [Header("References")]
    [SerializeField]
    private TMP_Text rankLabel;

    [SerializeField]
    private TMP_Text nameLabel;

    [SerializeField]
    private TMP_Text scoreLabel;

    [SerializeField]
    private Image background;

    [Header("Colors")]
    [SerializeField]
    private Color normalTextColor =
        new Color(0.88f, 0.92f, 0.9f, 1f);

    [SerializeField]
    private Color playerTextColor =
        new Color(1f, 0.81f, 0.35f, 1f);

    [SerializeField]
    private Color normalBackground =
        new Color(1f, 1f, 1f, 0.025f);

    [SerializeField]
    private Color alternateBackground =
        new Color(1f, 1f, 1f, 0.055f);

    [SerializeField]
    private Color playerBackground =
        new Color(1f, 0.72f, 0.2f, 0.18f);

    private void Awake() {
        if (rankLabel == null || nameLabel == null
            || scoreLabel == null || background == null) {
            Debug.LogError(
                "LeaderboardRowWidget: assign all labels and background.",
                this
            );
        }
    }

    public void ShowEntry(SessionLeaderboard.Entry entry, int rank) {
        if (rankLabel == null || nameLabel == null
            || scoreLabel == null || background == null) {
            return;
        }

        // 填入名次、名字和分数。
        rankLabel.text = rank.ToString("D2");
        nameLabel.text = entry.Name;
        scoreLabel.text = entry.Score.ToString("N0");

        // 玩家文字显示为金色加粗。
        Color textColor = entry.IsPlayer
            ? playerTextColor
            : normalTextColor;

        FontStyles style = entry.IsPlayer
            ? FontStyles.Bold
            : FontStyles.Normal;

        rankLabel.color = textColor;
        nameLabel.color = textColor;
        scoreLabel.color = textColor;

        rankLabel.fontStyle = style;
        nameLabel.fontStyle = style;
        scoreLabel.fontStyle = style;

        // 玩家高亮，其余行交替使用两种背景色。
        background.color = entry.IsPlayer
            ? playerBackground
            : rank % 2 == 0
                ? alternateBackground
                : normalBackground;
    }
}
