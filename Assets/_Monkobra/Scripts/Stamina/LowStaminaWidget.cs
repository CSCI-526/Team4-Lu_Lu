using UnityEngine;

/// <summary>
/// Listener that asks for the low-stamina hint on the first low-stamina
/// feedback since the scene loaded. It listens to the same
/// <see cref="StaminaBarController.LowStaminaMoveAttempted"/> that drives the
/// bar shake and the camera cool-down, so the hint opens with the first
/// cool-down pulse and shares its threshold, never a second one. Text, time,
/// and the once-per-scene rule live on the popup prefab; the
/// <see cref="PopUpManager"/> yields it to the web hint and drops it on a
/// loss.
/// <para>
/// Put it on an always-active object: it drops its stamina subscription after
/// the first event; nothing is static, so the hint returns on every scene
/// load.
/// </para>
/// </summary>
public class LowStaminaWidget: MonoBehaviour {
    // MonoBehaviour Lifecycle  ################################################
    // the stamina bar is a singleton set in Awake, so it is read once every
    // Awake ran
    private void Start() {
        staminaBar = StaminaBarController.I;
        if (staminaBar == null) {
            Debug.LogError(
                "LowStaminaWidget:\tfail to get singleton: "
                    + "StaminaBarController",
                this
            );
            enabled = false;
            return;
        }
        staminaBar.LowStaminaMoveAttempted += OnLowStaminaMoveAttempted;
    }

    private void OnDestroy() {
        UnsubscribeStamina();
    }

    // Event Handlers  #########################################################
    // the first cool-down pulse comes from this same event, so the hint opens
    // with it; later events no longer reach this script
    private void OnLowStaminaMoveAttempted() {
        UnsubscribeStamina();

        if (PopUpManager.I == null) {
            Debug.LogWarning(
                "LowStaminaWidget:\tfail to get singleton: PopUpManager",
                this
            );
            return;
        }
        PopUpManager.I.Show(PopUpId.LowStaminaHint);
    }

    // cached references  ------------------------------------------------------
    private StaminaBarController staminaBar;

    // private methods  ########################################################
    private void UnsubscribeStamina() {
        if (staminaBar != null) {
            staminaBar.LowStaminaMoveAttempted -= OnLowStaminaMoveAttempted;
            staminaBar = null;
        }
    }
}
