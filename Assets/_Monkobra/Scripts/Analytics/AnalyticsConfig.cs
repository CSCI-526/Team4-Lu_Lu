using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shared tuning for <see cref="AnalyticsManager"/>: sampling, saving, and
/// how a death cause is inferred.
/// </summary>
[CreateAssetMenu(
    fileName = "AnalyticsConfig",
    menuName = "Scriptable Objects/AnalyticsConfig"
)]
public class AnalyticsConfig: ScriptableObject {
    // Public Members  #########################################################
    /// <returns>key that forces a new session file</returns>
    public Key RestartSessionKey => restartSessionKey;

    /// <returns>key that saves a mock session file, for layout checks</returns>
    public Key MockSessionKey => mockSessionKey;

    /// <returns>seconds b/t stamina samples while a run is playing</returns>
    public float StaminaSampleIntervalS => staminaSampleIntervalS;

    /// <returns>seconds b/t timed saves while a run is playing, 0 turns off
    /// </returns>
    public float AutosaveIntervalS => autosaveIntervalS;

    /// <returns>seconds before a loss in which a branch hit or web encounter
    /// counts as its cause</returns>
    public float DeathCauseWindowS => deathCauseWindowS;

    // Inspector Fields  #######################################################
    [Header("Session")]
    [SerializeField]
    [Tooltip("forces a new session file")]
    private Key restartSessionKey = Key.F9;

    [SerializeField]
    [Tooltip("saves a mock session file, the live session is untouched")]
    private Key mockSessionKey = Key.F10;

    [Header("Sampling")]
    [SerializeField]
    [Tooltip("seconds b/t stamina samples while a run is playing")]
    private float staminaSampleIntervalS = 2f;

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
        staminaSampleIntervalS = Mathf.Max(0.1f, staminaSampleIntervalS);
        autosaveIntervalS = Mathf.Max(0f, autosaveIntervalS);
        deathCauseWindowS = Mathf.Max(0f, deathCauseWindowS);
    }
}
