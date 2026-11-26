using UnityEditor;
using UnityEngine;

/// <summary>Editor menu items to flip the Endless unlock while
/// testing</summary>
public static class ProgressMenu {
    [MenuItem("Monkobra/Progress/Unlock Endless", false, 80)]
    private static void UnlockEndless() {
        ProgressStore.MarkBossBeaten();
        Debug.Log("ProgressMenu:\tEndless unlocked: "
            + ProgressStore.IsEndlessUnlocked);
    }

    [MenuItem("Monkobra/Progress/Reset Progress", false, 81)]
    private static void ResetProgress() {
        ProgressStore.ResetProgress();
        Debug.Log("ProgressMenu:\tEndless unlocked: "
            + ProgressStore.IsEndlessUnlocked);
    }
}
