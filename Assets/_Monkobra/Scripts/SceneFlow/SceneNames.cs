using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// the scene names of the build and the one way to load them, so no script
/// types a scene name or leaves time frozen across a load
/// </summary>
public static class SceneNames {
    // Public Members  #########################################################
    public const string TITLE = "TitleScene";
    public const string LEVEL_SELECT = "LevelSelectScene";
    public const string FULL = "Lv1Scene";
    public const string ENDLESS = "EndlessScene";

    /// <summary>
    /// loads a scene by name, first undoing a pause or freeze, so the next
    /// scene never starts frozen or muted
    /// </summary>
    public static void Load(string sceneName) {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }
}
