using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Public Firebase client settings; never a service account
/// key.</summary>
[CreateAssetMenu(
    fileName = "FirebaseLeaderboardConfig",
    menuName = "Scriptable Objects/FirebaseLeaderboardConfig"
)]
public class FirebaseLeaderboardConfig: ScriptableObject {
    // Public Members  #########################################################
    public bool IsConfigured => useOnlineLeaderboard && (UseEmulators
        || (Regex.IsMatch(projectId ?? "", "^[a-z][a-z0-9-]{4,28}[a-z0-9]$")
            && !string.IsNullOrWhiteSpace(webApiKey)));
    public string ProjectId => UseEmulators ? "demo-monkobra" : projectId;
    public string WebApiKey => UseEmulators ? "demo-api-key" : webApiKey.Trim();
    public int RequestTimeoutS => requestTimeoutS;
    public bool UseEmulators {
        get {
#if UNITY_EDITOR
            return useEmulators;
#else
            return false;
#endif
        }
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("Enable only after publishing the Firestore rules.")]
    private bool useOnlineLeaderboard;
    [SerializeField]
    private string projectId = "";
    [SerializeField]
    [Tooltip("Public Web API Key from Firebase project settings.")]
    private string webApiKey = "";
    [SerializeField, Range(5, 60)]
    private int requestTimeoutS = 15;

#if UNITY_EDITOR
    [Header("Local Testing (Editor Only)")]
    [SerializeField]
    [Tooltip("Requires Auth :9099 and Firestore :8080 emulators; no account.")]
    private bool useEmulators;
#endif
}
