using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Shared tuning for <see cref="AnalyticsManager"/>: sampling, saving, and
/// how a death cause is inferred. Hack keys live in
/// <see cref="HackShortcutConfig"/>.
/// </summary>
[CreateAssetMenu(
    fileName = "AnalyticsConfig",
    menuName = "Scriptable Objects/AnalyticsConfig"
)]
public class AnalyticsConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>seconds b/t run samples of height & stamina while a run is
    /// playing</returns>
    public float SampleIntervalS => sampleIntervalS;

    /// <returns>seconds b/t timed saves while a run is playing, 0 turns off
    /// </returns>
    public float AutosaveIntervalS => autosaveIntervalS;

    /// <returns>seconds before a loss in which a branch hit or web encounter
    /// counts as its cause</returns>
    public float DeathCauseWindowS => deathCauseWindowS;

    // Inspector Fields  #######################################################
    [Header("Sampling")]
    [SerializeField]
    [FormerlySerializedAs("staminaSampleIntervalS")]
    [Tooltip("seconds b/t run samples of height & stamina while a run is "
        + "playing")]
    private float sampleIntervalS = 2f;

    [SerializeField]
    [Tooltip("seconds b/t timed saves while a run is playing, 0 turns off")]
    private float autosaveIntervalS = 10f;

    [Header("Death Cause")]
    [SerializeField]
    [Tooltip("a branch hit or web encounter w/i this many seconds b4 a loss "
        + "is its cause, else the great cobra")]
    private float deathCauseWindowS = 2f;

    // Editor Validation  ######################################################
    private void OnValidate() {
        sampleIntervalS = Mathf.Max(0.1f, sampleIntervalS);
        autosaveIntervalS = Mathf.Max(0f, autosaveIntervalS);
        deathCauseWindowS = Mathf.Max(0f, deathCauseWindowS);
    }
}
