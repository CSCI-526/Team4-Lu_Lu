using UnityEngine;

/// <summary>
/// Shared tuning for <see cref="FruitFlyEffect"/>: how long a grabbed fruit's
/// sprite takes to reach its target, how it looks on the way, and how its
/// motion eases. The flight duration also delays the stamina restore, which
/// lands on arrival.
/// </summary>
[CreateAssetMenu(
    fileName = "FruitFlyConfig",
    menuName = "Scriptable Objects/FruitFlyConfig"
)]
public class FruitFlyConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>grab to arrival; s</returns>
    public float DurationS => durationS;

    /// <returns>sprite opacity; 0 invisible, 1 opaque</returns>
    public float Alpha => alpha;

    /// <returns>sprite edge at launch, in canvas reference units; px</returns>
    public float SizePx => sizePx;

    /// <returns>size at arrival as a fraction of launch size</returns>
    public float EndScale => endScale;

    /// <returns>progress along the path vs normalized time, both 0..1
    /// </returns>
    public AnimationCurve Ease => ease;

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip(
        "grab to arrival; s. The stamina restore waits for arrival, so this "
            + "also delays it"
    )]
    private float durationS = 0.5f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("sprite opacity; 0 invisible, 1 opaque")]
    private float alpha = 0.6f;

    [SerializeField]
    [Tooltip("sprite edge at launch, in canvas reference units; px")]
    private float sizePx = 64f;

    [SerializeField]
    [Range(0f, 2f)]
    [Tooltip("size at arrival as a fraction of launch size; 1 = unchanged")]
    private float endScale = 0.6f;

    [SerializeField]
    [Tooltip(
        "progress along the path vs normalized time, both 0..1; a steep "
            + "start reads as a toss"
    )]
    private AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    // Editor Validation  ######################################################
    private void OnValidate() {
        durationS = Mathf.Max(0f, durationS);
        sizePx = Mathf.Max(0f, sizePx);
    }
}
