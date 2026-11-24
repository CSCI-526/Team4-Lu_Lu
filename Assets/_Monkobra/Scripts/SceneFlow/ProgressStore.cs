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

    /// <summary>the Cobra King's lesson: throw a banana at his jewel</summary>
    public const string COBRA_KING_THROW_LESSON = "CobraKingThrow";
    /// <summary>the Spider King's lesson: parry a web bullet</summary>
    public const string SPIDER_KING_WEB_LESSON = "SpiderKingWeb";
    /// <summary>the Spider King's lesson: parry a crescent slash</summary>
    public const string SPIDER_KING_SLASH_LESSON = "SpiderKingSlash";

    /// <param name="lesson">one of the *_LESSON names</param>
    /// <returns>if the player has passed that boss lesson once, in any run
    /// of either mode, so it is not taught again</returns>
    public static bool HasLearned(string lesson) {
        return PlayerPrefs.GetInt(LESSON_KEY_PREFIX + lesson, 0) != 0;
    }

    /// <summary>records a passed boss lesson; saved at once</summary>
    /// <param name="lesson">one of the *_LESSON names</param>
    public static void MarkLearned(string lesson) {
        if (HasLearned(lesson)) {
            return;
        }

        PlayerPrefs.SetInt(LESSON_KEY_PREFIX + lesson, 1);
        PlayerPrefs.Save();
    }

    /// <summary>forgets all progress, so Endless locks again & the boss
    /// lessons are taught again</summary>
    public static void ResetProgress() {
        PlayerPrefs.DeleteKey(BOSS_BEATEN_KEY);
        PlayerPrefs.DeleteKey(LESSON_KEY_PREFIX + COBRA_KING_THROW_LESSON);
        PlayerPrefs.DeleteKey(LESSON_KEY_PREFIX + SPIDER_KING_WEB_LESSON);
        PlayerPrefs.DeleteKey(LESSON_KEY_PREFIX + SPIDER_KING_SLASH_LESSON);
        PlayerPrefs.Save();
    }

    // private members  ########################################################
    private const string BOSS_BEATEN_KEY = "Monkobra.BossBeatenOnce";
    private const string LESSON_KEY_PREFIX = "Monkobra.Learned.";
}
