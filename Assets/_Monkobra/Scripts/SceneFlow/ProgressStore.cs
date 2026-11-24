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

    /// <summary>the Cobra King's words on the gold armor the monkey gets
    /// </summary>
    public const string ARMOR_INTRO = "ArmorIntro";

    /// <param name="intro">one of the intro names above</param>
    /// <returns>if the player has seen that reward's words once, in any run
    /// of either mode, so they are not shown again; the boss lessons are
    /// taught every fight all the same</returns>
    public static bool HasSeen(string intro) {
        return PlayerPrefs.GetInt(SEEN_KEY_PREFIX + intro, 0) != 0;
    }

    /// <summary>records a seen reward intro; saved at once</summary>
    /// <param name="intro">one of the intro names above</param>
    public static void MarkSeen(string intro) {
        if (HasSeen(intro)) {
            return;
        }

        PlayerPrefs.SetInt(SEEN_KEY_PREFIX + intro, 1);
        PlayerPrefs.Save();
    }

    /// <summary>forgets all progress, so Endless locks again & the reward
    /// intros show again</summary>
    public static void ResetProgress() {
        PlayerPrefs.DeleteKey(BOSS_BEATEN_KEY);
        PlayerPrefs.DeleteKey(SEEN_KEY_PREFIX + ARMOR_INTRO);
        PlayerPrefs.Save();
    }

    // private members  ########################################################
    private const string BOSS_BEATEN_KEY = "Monkobra.BossBeatenOnce";
    private const string SEEN_KEY_PREFIX = "Monkobra.Seen.";
}
