using UnityEngine;

/// <summary>
/// Holds what any scene may need, for the whole launch: the run
/// <see cref="Mode"/>. The run's state machine lives on
/// <see cref="RunController"/>, which only <c>GameplayScene</c> has.
/// <para>
/// Lives across scene loads. Created by
/// <see cref="PersistentBootstrapLoader"/> from the
/// <c>Resources/GameController</c> prefab; nothing calls it to start.
/// </para>
/// </summary>
public class GameController: MonoBehaviour {
    // Public Members  #########################################################
    public static GameController I {
        get; private set;
    }

    /// <summary>which kind of run the next gameplay load plays, picked on
    /// load</summary>
    public RunMode Mode => RunModeSelection.Selected;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (I != null && I != this) {
            Debug.Log("GameController:\tduplicate instance destroyed", this);
            Destroy(gameObject);
            return;
        }

        I = this;
        // one controller spans the whole launch, across every scene load
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }
}
