using UnityEngine;

/// <summary>
/// Every score value of a run in one asset: the height value, ie how many
/// points a climbed meter pays, and how many points each
/// <see cref="RewardKind"/> adds. <see cref="ScoreManager"/> reads it; the
/// player balance asset holds none of these.
/// </summary>
[CreateAssetMenu(
    fileName = "ScoreConfig",
    menuName = "Scriptable Objects/ScoreConfig"
)]
public class ScoreConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>points earned per meter of height</returns>
    public int PointsPerMeter => pointsPerMeter;

    /// <returns>Unity units per meter of height</returns>
    public float UnitsPerMeter => unitsPerMeter;

    /// <returns>factor every score is multiplied by in Hardcore Mode
    /// </returns>
    public float HardcoreScoreScale => hardcoreScoreScale;

    /// <returns>points a banana grab adds</returns>
    public int BananaPoints => bananaPoints;

    /// <returns>points a coconut grab adds</returns>
    public int CoconutPoints => coconutPoints;

    /// <returns>points a pineapple grab adds</returns>
    public int PineapplePoints => pineapplePoints;

    /// <returns>points a coconut adds when it freezes the great cobra
    /// </returns>
    public int CobraFreezePoints => cobraFreezePoints;

    /// <returns>points a new height marker adds</returns>
    public int HeightMarkerPoints => heightMarkerPoints;

    /// <returns>points breaking free of a spider web adds</returns>
    public int WebEscapePoints => webEscapePoints;

    /// <returns>points beating the Cobra King adds</returns>
    public int CobraKingDefeatPoints => cobraKingDefeatPoints;

    /// <returns>points beating the Spider King adds</returns>
    public int SpiderKingDefeatPoints => spiderKingDefeatPoints;

    /// <returns>points a fruit landing on the Cobra King's crown adds
    /// </returns>
    public int CrownHitPoints => crownHitPoints;

    /// <returns>points hurting the Spider King adds</returns>
    public int SpiderHitPoints => spiderHitPoints;

    /// <returns>points parrying a Spider King tail strike adds</returns>
    public int TailParryPoints => tailParryPoints;

    /// <returns>points dodging a Spider King dash adds</returns>
    public int DashDodgePoints => dashDodgePoints;

    // Public Methods  #########################################################
    /// <returns>points one reward of the kind adds</returns>
    public int PointsFor(RewardKind kind) {
        switch (kind) {
            case RewardKind.Banana:
                return bananaPoints;
            case RewardKind.Coconut:
                return coconutPoints;
            case RewardKind.Pineapple:
                return pineapplePoints;
            case RewardKind.CobraFreeze:
                return cobraFreezePoints;
            case RewardKind.HeightMarker:
                return heightMarkerPoints;
            case RewardKind.WebEscape:
                return webEscapePoints;
            case RewardKind.CobraKingDefeat:
                return cobraKingDefeatPoints;
            case RewardKind.SpiderKingDefeat:
                return spiderKingDefeatPoints;
            case RewardKind.CrownHit:
                return crownHitPoints;
            case RewardKind.SpiderHit:
                return spiderHitPoints;
            case RewardKind.TailParry:
                return tailParryPoints;
            case RewardKind.DashDodge:
                return dashDodgePoints;
            default:
                return 0;
        }
    }

    // Inspector Fields  #######################################################
    [Header("Height")]
    [SerializeField]
    [Min(0)]
    [Tooltip("points earned per meter of height")]
    private int pointsPerMeter = 1;

    [SerializeField]
    [Min(0.001f)]
    [Tooltip("Unity units per meter of height")]
    private float unitsPerMeter = 0.1f;

    [Header("Hardcore")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("factor every score (height, fruit, rewards, bosses) is "
        + "multiplied by in Hardcore Mode")]
    private float hardcoreScoreScale = 1.5f;

    [Header("Rewards")]
    [SerializeField]
    [Min(0)]
    [Tooltip("points a banana grab adds")]
    private int bananaPoints = 100;

    [SerializeField]
    [Min(0)]
    [Tooltip("points a coconut grab adds")]
    private int coconutPoints = 200;

    [SerializeField]
    [Min(0)]
    [Tooltip("points a pineapple grab adds")]
    private int pineapplePoints = 50;

    [SerializeField]
    [Min(0)]
    [Tooltip("points a coconut adds when it freezes the great cobra")]
    private int cobraFreezePoints = 150;

    [SerializeField]
    [Min(0)]
    [Tooltip("points a new height marker adds")]
    private int heightMarkerPoints = 25;

    [SerializeField]
    [Min(0)]
    [Tooltip("points breaking free of a spider web adds")]
    private int webEscapePoints = 75;

    [Header("Bosses")]
    [SerializeField]
    [Min(0)]
    [Tooltip("points beating the Cobra King adds, once its reward is taken")]
    private int cobraKingDefeatPoints = 1000;

    [SerializeField]
    [Min(0)]
    [Tooltip("points beating the Spider King adds, once its reward is taken")]
    private int spiderKingDefeatPoints = 2000;

    [SerializeField]
    [Min(0)]
    [Tooltip("points a fruit landing on the Cobra King's crown adds")]
    private int crownHitPoints = 100;

    [SerializeField]
    [Min(0)]
    [Tooltip("points hurting the Spider King adds")]
    private int spiderHitPoints = 100;

    [SerializeField]
    [Min(0)]
    [Tooltip("points parrying a Spider King tail strike adds")]
    private int tailParryPoints = 150;

    [SerializeField]
    [Min(0)]
    [Tooltip("points dodging a Spider King dash adds")]
    private int dashDodgePoints = 75;

    // Editor Validation  ######################################################
    private void OnValidate() {
        pointsPerMeter = Mathf.Max(0, pointsPerMeter);
        unitsPerMeter = Mathf.Max(0.001f, unitsPerMeter);
        hardcoreScoreScale = Mathf.Max(0f, hardcoreScoreScale);
        bananaPoints = Mathf.Max(0, bananaPoints);
        coconutPoints = Mathf.Max(0, coconutPoints);
        pineapplePoints = Mathf.Max(0, pineapplePoints);
        cobraFreezePoints = Mathf.Max(0, cobraFreezePoints);
        heightMarkerPoints = Mathf.Max(0, heightMarkerPoints);
        webEscapePoints = Mathf.Max(0, webEscapePoints);
        cobraKingDefeatPoints = Mathf.Max(0, cobraKingDefeatPoints);
        spiderKingDefeatPoints = Mathf.Max(0, spiderKingDefeatPoints);
        crownHitPoints = Mathf.Max(0, crownHitPoints);
        spiderHitPoints = Mathf.Max(0, spiderHitPoints);
        tailParryPoints = Mathf.Max(0, tailParryPoints);
        dashDodgePoints = Mathf.Max(0, dashDodgePoints);
    }
}
