using UnityEngine;

/// <summary>
/// what the player has achieved across launches, kept in PlayerPrefs, so
/// Endless Mode unlocks once the Cobra King falls
/// </summary>
public static class ProgressStore {
    // Public Members  #########################################################
    /// <summary>if the Cobra King has been beaten at least once</summary>
    public static bool IsEndlessUnlocked =>
        PlayerPrefs.GetInt(BOSS_BEATEN_KEY, 0) != 0;

    /// <summary>records the first king win; saved at once, as WebGL only
    /// flushes PlayerPrefs on request</summary>
    public static void MarkBossBeaten() {
        if (IsEndlessUnlocked) {
            return;
        }

        PlayerPrefs.SetInt(BOSS_BEATEN_KEY, 1);
        PlayerPrefs.Save();
    }

    /// <summary>forgets all progress, so Endless locks again</summary>
    public static void ResetProgress() {
        PlayerPrefs.DeleteKey(BOSS_BEATEN_KEY);
        PlayerPrefs.Save();
    }

    // Private Members  ########################################################
    private const string BOSS_BEATEN_KEY = "Monkobra.BossBeatenOnce";
}
