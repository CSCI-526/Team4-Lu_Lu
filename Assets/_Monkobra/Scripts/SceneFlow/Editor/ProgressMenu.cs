using UnityEditor;
using UnityEngine;

/// <summary>Editor menu items to flip the Endless unlock while testing</summary>
public static class ProgressMenu {
    [MenuItem("Monkobra/Unlock Endless")]
    private static void UnlockEndless() {
        ProgressStore.MarkBossBeaten();
        Debug.Log("ProgressMenu:\tEndless unlocked: "
            + ProgressStore.IsEndlessUnlocked);
    }

    [MenuItem("Monkobra/Reset Progress")]
    private static void ResetProgress() {
        ProgressStore.ResetProgress();
        Debug.Log("ProgressMenu:\tEndless unlocked: "
            + ProgressStore.IsEndlessUnlocked);
    }
}
