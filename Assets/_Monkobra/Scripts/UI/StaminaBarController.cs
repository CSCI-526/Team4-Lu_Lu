using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class StaminaBarController: MonoBehaviour {
    // Public Members  #########################################################
    public static StaminaBarController I {
        get; private set;
    }

    public float CurrentStamina => currentStamina;
    public bool HasStamina => currentStamina > 0f;

    /// <returns>if the dev god mode keeps the bar at max, toggled by the
    /// <see cref="HackAction.GodMode"/> key in a development build</returns>
    public bool IsGodMode {
        get; private set;
    }

    /// <summary>
    /// if stamina is below <c>lowStaminaFraction</c> of the max, empty
    /// included; feeds the feedback efx only, never gameplay
    /// </summary>
    public bool IsLowStamina =>
        playerBalanceConfig != null
        && currentStamina < lowStaminaFraction * MaxStamina;

    /// <returns>stamina ceiling from the balance config, 0 w/o a config
    /// </returns>
    public float MaxStamina =>
        playerBalanceConfig != null ? playerBalanceConfig.MaxStamina : 0f;

    /// <summary>raised each time a fruit restores stamina</summary>
    public event Action FruitConsumed;

    /// <summary>
    /// raised when the player tries to move while stamina is low, see
    /// <see cref="IsLowStamina"/>
    /// </summary>
    public event Action LowStaminaMoveAttempted;

    // Public Methods  #########################################################
    /// <summary>
    /// restores the per-fruit amount from <see cref="PlayerBalanceConfig"/>,
    /// called when a grabbed fruit's flight lands on its target, see
    /// <see cref="FruitFlyEffect"/>
    /// </summary>
    public void AddStaminaByFruit() {
        AddStamina(playerBalanceConfig.FruitStaminaRestore);
        FruitConsumed?.Invoke();
    }

    /// <summary>
    /// reports a move input, called every physics step the player holds one;
    /// raises <see cref="LowStaminaMoveAttempted"/> iff stamina is low
    /// </summary>
    public void NotifyMoveAttempt() {
        if (!IsLowStamina) {
            return;
        }

        LowStaminaMoveAttempted?.Invoke();
    }

    public void AddStamina(float amount) {
        if (amount <= 0f) {
            return;
        }

        currentStamina = Mathf.Min(
            playerBalanceConfig.MaxStamina,
            currentStamina + amount
        );

        staminaSlider.value = currentStamina;
    }

    /// <summary>
    /// drains stamina for one step of movement: climbing costs the most,
    /// orbiting the trunk costs less, descending is free. Rates come from
    /// <see cref="PlayerBalanceConfig"/>
    /// </summary>
    /// <param name="climbedU">upward distance covered this step; u</param>
    /// <param name="orbitedU">sideways arc length covered this step; u</param>
    public void DrainByMovement(float climbedU, float orbitedU) {
        if (IsGodMode || !HasStamina) {
            return;
        }

        float amount =
            Mathf.Max(0f, climbedU) * playerBalanceConfig.StaminaUpwardDrainPerU
            + Mathf.Max(0f, orbitedU) * playerBalanceConfig.StaminaSidewayDrainPerU;
        if (amount <= 0f) {
            return;
        }

        currentStamina = Mathf.Max(0f, currentStamina - amount);
        staminaSlider.value = currentStamina;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("player balance tuning; stamina budget and drain rate")]
    private PlayerBalanceConfig playerBalanceConfig;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "feedback efx (bar shake, camera cool-down) play on a move attempt "
            + "below this fraction of max stamina; 0.1 = below 10%. Efx "
            + "only, an empty bar alone slows the monkey"
    )]
    private float lowStaminaFraction = 0.1f;

    [SerializeField]
    [Tooltip("dev/hack keys, its god mode key works in a development build "
        + "only")]
    private HackShortcutConfig hackShortcuts;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (playerBalanceConfig == null) {
            Debug.LogWarning(
                "StaminaBarController:\tmust assign Inspector Field: "
                    + "playerBalanceConfig",
                this
            );
            enabled = false;
            return;
        }

        // drop this duplicate component only, the GameObject may hold more
        if (I != null && I != this) {
            Debug.LogWarning(
                "StaminaBarController:\tduplicate instance, removing", this);
            Destroy(this);
            return;
        }

        I = this;
        staminaSlider = GetComponent<Slider>();

        // the level opens on the starting zone, which starts below full
        currentStamina = playerBalanceConfig.MaxStamina
            * playerBalanceConfig.StartingZoneStartStaminaFraction;
        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = playerBalanceConfig.MaxStamina;
        staminaSlider.value = currentStamina;

        if (Debug.isDebugBuild) {
            Debug.Log("StaminaBarController:\tready", this);
        }
    }

    private void Update() {
        UpdateGodModeKey();
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    // private methods  ########################################################
    // dev/hack key toggling god mode; turning it on fills the bar at once
    private void UpdateGodModeKey() {
        if (hackShortcuts == null || !HackShortcutConfig.IsActive) {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Key godModeKey = hackShortcuts.GetKey(HackAction.GodMode);
        if (
            keyboard == null
            || godModeKey == Key.None
            || !keyboard[godModeKey].wasPressedThisFrame
        ) {
            return;
        }

        IsGodMode = !IsGodMode;
        if (IsGodMode) {
            AddStamina(MaxStamina);
        }
        if (Debug.isDebugBuild) {
            Debug.Log("StaminaBarController:\tgod mode " + IsGodMode, this);
        }
    }

    // private members  ########################################################
    private float currentStamina;

    // cached references  ------------------------------------------------------
    private Slider staminaSlider;
}
