using UnityEngine;

/// <summary>
/// Holds what any scene may need, for the whole launch: the run
/// <see cref="Mode"/>. The run's state machine lives on
/// <see cref="RunController"/>, which only <c>GameplayScene</c> has.
/// <para>
/// The one owner of the launch's singletons: <see cref="AnalyticsManager"/>,
/// <see cref="ControlStyleManager"/>, <see cref="MusicManager"/> and
/// <see cref="TutorialSkipManager"/> are components of this same prefab,
/// which every scene holds one instance of. The first instance lives across
/// scene loads; a later one deactivates and destroys itself before its
/// sibling components wake, so only this class destroys a duplicate or calls
/// <c>DontDestroyOnLoad</c>. It runs before them (execution order -1000).
/// </para>
/// </summary>
[DefaultExecutionOrder(-1000)]
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
            // an inactive object never wakes its sibling managers
            gameObject.SetActive(false);
            Debug.Log("GameController:\tduplicate instance destroyed", this);
            Destroy(gameObject);
            return;
        }

        I = this;
        // one controller & its managers span the whole launch, across every scene load
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }
}
