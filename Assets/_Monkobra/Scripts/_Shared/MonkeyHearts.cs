using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The monkey's hearts in a boss fight: each fight starts it with the Loop's
/// hearts, a fatal fight hit the gold armor does not take costs 1, and 0
/// hearts lose the fight. Shared by both kings & the boss HUD; off on the
/// climb, where no hearts show.
/// </summary>
public static class MonkeyHearts {
    // Public Members  #########################################################
    /// <summary>raised when the hearts begin, change or end</summary>
    public static event Action Changed;

    /// <returns>hearts left now</returns>
    public static int Hearts { get; private set; }

    /// <returns>hearts the fight began with</returns>
    public static int MaxHearts { get; private set; }

    /// <returns>if a boss fight is on & its hearts count</returns>
    public static bool IsActive { get; private set; }

    // Public Methods  #########################################################
    /// <summary>Starts a fight's hearts, full.</summary>
    /// <param name="max">hearts this fight gives the monkey</param>
    public static void Begin(int max) {
        MaxHearts = Mathf.Max(1, max);
        Hearts = MaxHearts;
        IsActive = true;
        Changed?.Invoke();
    }

    /// <summary>Ends the hearts when the fight does.</summary>
    public static void End() {
        if (!IsActive) {
            return;
        }
        IsActive = false;
        Changed?.Invoke();
    }

    /// <summary>A fatal hit the armor did not take costs a heart.</summary>
    /// <returns>if the monkey still has a heart, so the fight goes on;
    /// false when none are left or no fight is on</returns>
    public static bool TryLose() {
        if (!IsActive) {
            return false;
        }
        Hearts = Mathf.Max(0, Hearts - 1);
        Changed?.Invoke();
        return Hearts > 0;
    }

    // private methods  ########################################################
    // a new scene or a domain reload in Play never keeps a past fight's hearts
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() {
        IsActive = false;
        Hearts = 0;
        MaxHearts = 0;
        Changed = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        End();
    }
}
