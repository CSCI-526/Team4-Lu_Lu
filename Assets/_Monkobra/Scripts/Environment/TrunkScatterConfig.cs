using UnityEngine;

/// <summary>
/// System tuning of <see cref="TrunkScatter"/>: how loose the spacing is and
/// how finely the density is integrated. Density curves are balance and live
/// in the balance configs, never here.
/// </summary>
[CreateAssetMenu(
    fileName = "TrunkScatterConfig",
    menuName = "Scriptable Objects/TrunkScatterConfig"
)]
public class TrunkScatterConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>spacing jitter as a fraction of the even gap, 0~0.95; 0 is
    /// a rigid grid, near 1 lets neighbours almost touch</returns>
    public float JitterFraction => jitterFraction;

    /// <returns>yaw wobble either way around the golden angle spiral;
    /// deg</returns>
    public float YawJitterDeg => yawJitterDeg;

    /// <returns>step of the density integration; u</returns>
    public float IntegrationStepU => integrationStepU;

    /// <returns>yaw retries of a blocked item before it is skipped</returns>
    public int MaxRetries => maxRetries;

    // Inspector Fields  #######################################################
    [Header("Spacing")]
    [SerializeField]
    [Range(0f, 0.95f)]
    [Tooltip(
        "spacing jitter, fraction of the even gap; gaps never fall below "
            + "(1 - this) x the even gap"
    )]
    private float jitterFraction = 0.5f;

    [Header("Yaw")]
    [SerializeField]
    [Tooltip("yaw wobble either way around the golden angle spiral; deg")]
    private float yawJitterDeg = 25f;

    [SerializeField]
    [Tooltip("yaw retries of a blocked item b4 it is skipped")]
    private int maxRetries = 12;

    [Header("Integration")]
    [SerializeField]
    [Tooltip("step of the cumulative density table; u")]
    private float integrationStepU = 0.5f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        jitterFraction = Mathf.Clamp(jitterFraction, 0f, 0.95f);
        yawJitterDeg = Mathf.Max(0f, yawJitterDeg);
        maxRetries = Mathf.Max(0, maxRetries);
        integrationStepU = Mathf.Max(0.05f, integrationStepU);
    }
}
