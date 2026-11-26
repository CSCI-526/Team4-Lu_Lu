using UnityEditor;
using UnityEngine;

/// <summary>Editor menu items to flip the Hardcore unlock while
/// testing</summary>
public static class ProgressMenu {
    [MenuItem("Monkobra/Unlock Hardcore")]
    private static void UnlockHardcore() {
        ProgressStore.MarkBossBeaten();
        Debug.Log("ProgressMenu:\tHardcore unlocked: "
            + ProgressStore.IsHardcoreUnlocked);
    }

    [MenuItem("Monkobra/Reset Progress")]
    private static void ResetProgress() {
        ProgressStore.ResetProgress();
        Debug.Log("ProgressMenu:\tHardcore unlocked: "
            + ProgressStore.IsHardcoreUnlocked);
    }
}
