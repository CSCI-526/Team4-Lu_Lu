using UnityEngine;

/// <summary>
/// Listener of the coconut prompts: the double-tap teaching on the first
/// coconut of the scene (skipped once one was used this session), the
/// confirmation that the cobra froze, the refusal while it is already frozen,
/// and the warning that it moves again. It turns each
/// <see cref="CoconutManager"/> event into a
/// <see cref="PopUpManager.Show"/> call; text, look, and timing live on the
/// popup prefabs under <c>Prefabs/PopUp/</c>.
/// <para>
/// Keep this object active from scene load: it must hear the thaw, when the
/// freeze timer HUD is already off.
/// </para>
/// </summary>
public class CoconutMessageWidget: MonoBehaviour {
    // MonoBehaviour Lifecycle  ################################################
    private void Start() {
        manager = CoconutManager.I;
        if (manager == null) {
            Debug.LogWarning(
                "CoconutMessageWidget:\tfail to get singleton: "
                    + "CoconutManager, no coconut prompts",
                this
            );
            enabled = false;
            return;
        }
        manager.Collected += OnCollected;
        manager.Frozen += OnFrozen;
        manager.FreezeRefused += OnFreezeRefused;
        manager.Thawed += OnThawed;
    }

    private void OnDestroy() {
        if (manager != null) {
            manager.Collected -= OnCollected;
            manager.Frozen -= OnFrozen;
            manager.FreezeRefused -= OnFreezeRefused;
            manager.Thawed -= OnThawed;
        }
    }

    // Event Handlers  #########################################################
    // the popup itself is once-per-scene, a player who used a coconut already
    // this session knows how and never sees it
    private void OnCollected() {
        if (!CoconutManager.HasUsedThisSession) {
            Show(PopUpId.CoconutFirst);
        }
    }

    private void OnFrozen(float durationS) {
        Show(PopUpId.CoconutFrozen, Mathf.CeilToInt(durationS));
    }

    private void OnFreezeRefused() {
        Show(PopUpId.CoconutRefused);
    }

    private void OnThawed() {
        Show(PopUpId.CoconutThawed);
    }

    // private members  ########################################################
    private CoconutManager manager;

    // private methods  ########################################################
    private void Show(PopUpId id, params object[] args) {
        if (PopUpManager.I == null) {
            Debug.LogWarning(
                "CoconutMessageWidget:\tfail to get singleton: PopUpManager",
                this
            );
            return;
        }
        PopUpManager.I.Show(id, args);
    }
}
