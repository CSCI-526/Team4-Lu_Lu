using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class StaminaBarController: MonoBehaviour {
    // Public Members  #########################################################
    public static StaminaBarController I {
        get; private set;
    }

    public float CurrentStamina => currentStamina;
    public bool HasStamina => currentStamina > 0f;

    /// <returns>stamina ceiling from the balance config, 0 w/o a config
    /// </returns>
    public float MaxStamina =>
        balanceConfig != null ? balanceConfig.MaxStamina : 0f;

    /// <summary>raised each time a fruit restores stamina</summary>
    public event Action FruitConsumed;

    /// <summary>
    /// raised when the player tries to move while the bar is empty
    /// </summary>
    public event Action ExhaustedMoveAttempted;

    // Public Methods  #########################################################
    /// <summary>
    /// restores the per-fruit amount from <see cref="GameBalanceConfig"/>,
    /// called when an arm grabs a fruit
    /// </summary>
    public void AddStaminaByFruit() {
        AddStamina(balanceConfig.FruitStaminaRestore);
        FruitConsumed?.Invoke();
    }

    /// <summary>
    /// reports a move input, called every physics step the player holds one;
    /// raises <see cref="ExhaustedMoveAttempted"/> iff the bar is empty
    /// </summary>
    public void NotifyMoveAttempt() {
        if (HasStamina) {
            return;
        }

        ExhaustedMoveAttempted?.Invoke();
    }

    /// <summary>
    /// reports the monkey is resting, called every physics step no move key
    /// is held; the timer drain pauses while resting so recovery can show
    /// </summary>
    public void NotifyResting() {
        lastRestTime = Time.time;
    }

    public void AddStamina(float amount) {
        if (amount <= 0f) {
            return;
        }

        currentStamina = Mathf.Min(
            balanceConfig.MaxStamina,
            currentStamina + amount
        );

        staminaSlider.value = currentStamina;
    }

    /// <summary>
    /// drains stamina for one step of movement: climbing costs the most,
    /// orbiting the trunk costs less, descending is free. Rates come from
    /// <see cref="GameBalanceConfig"/>
    /// </summary>
    /// <param name="climbedU">upward distance covered this step; u</param>
    /// <param name="orbitedU">sideways arc length covered this step; u</param>
    public void DrainByMovement(float climbedU, float orbitedU) {
        if (!HasStamina) {
            return;
        }

        float amount =
            Mathf.Max(0f, climbedU) * balanceConfig.StaminaUpwardDrainPerU
            + Mathf.Max(0f, orbitedU) * balanceConfig.StaminaSidewayDrainPerU;
        if (amount <= 0f) {
            return;
        }

        currentStamina = Mathf.Max(0f, currentStamina - amount);
        staminaSlider.value = currentStamina;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("game balance tuning; stamina budget and drain rate")]
    private GameBalanceConfig balanceConfig;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (balanceConfig == null) {
            Debug.LogWarning(
                "StaminaBarController:\tmust assign Inspector Field: "
                    + "balanceConfig",
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

        currentStamina = balanceConfig.MaxStamina;
        staminaSlider.minValue = 0f;
        staminaSlider.maxValue = balanceConfig.MaxStamina;
        staminaSlider.value = currentStamina;

        if (Debug.isDebugBuild) {
            Debug.Log("StaminaBarController:\tready", this);
        }
    }

    private void OnDestroy() {
        if (I == this) {
            I = null;
        }
    }

    // constants  ##############################################################
    private const float REST_GRACE_S = 0.1f;

    // private members  ########################################################
    private float currentStamina;
    private float lastRestTime = float.NegativeInfinity; // Time.time

    // cached references  ------------------------------------------------------
    private Slider staminaSlider;
}
