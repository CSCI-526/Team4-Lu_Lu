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

    // Editor Validation  ######################################################
    private void OnValidate() {
        pointsPerMeter = Mathf.Max(0, pointsPerMeter);
        unitsPerMeter = Mathf.Max(0.001f, unitsPerMeter);
        bananaPoints = Mathf.Max(0, bananaPoints);
        coconutPoints = Mathf.Max(0, coconutPoints);
        pineapplePoints = Mathf.Max(0, pineapplePoints);
        cobraFreezePoints = Mathf.Max(0, cobraFreezePoints);
        heightMarkerPoints = Mathf.Max(0, heightMarkerPoints);
        webEscapePoints = Mathf.Max(0, webEscapePoints);
    }
}
