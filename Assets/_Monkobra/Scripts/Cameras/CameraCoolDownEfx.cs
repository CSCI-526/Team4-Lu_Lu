using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cool-down feedback on the whole view while the stamina bar is empty: a
/// circular blackdown from the URP Vignette plus a zoom-out from widening
/// the live <see cref="CinemachineCamera"/>'s field of view, both easing in
/// and back out. Sits on the main camera, next to the
/// <c>CinemachineBrain</c>. Pulses on every
/// <see cref="StaminaBarController.ExhaustedMoveAttempted"/>, and both
/// values return to rest when the pulse ends or this component is disabled.
/// </summary>
public class CameraCoolDownEfx: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Vignette")]
    [Tooltip("global volume whose profile holds a Vignette override")]
    [SerializeField]
    private Volume volume;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("vignette intensity at the pulse peak; 0 none, 1 full black edge")]
    private float peakIntensity = 0.6f;

    [Header("Zoom Out")]
    [SerializeField]
    [Tooltip("extra field of view at the pulse peak; deg")]
    private float fovDeltaDeg = 12f;

    [Header("Timing")]
    [SerializeField]
    [Tooltip("ease from rest to peak; s")]
    private float easeInS = 0.1f;

    [SerializeField]
    [Tooltip("ease from peak back to rest; s")]
    private float easeOutS = 0.4f;

    [SerializeField]
    [Tooltip("wait after a pulse ends before an empty-bar move may start "
        + "the next; s")]
    private float retriggerCooldownS = 0.2f;

    // Public Methods  #########################################################
    /// <summary>
    /// starts a pulse, or restarts a running one from rest weight
    /// </summary>
    public void Pulse() {
        if (vignette == null) {
            return;
        }

        if (!isPulsing) {
            baseIntensity = vignette.intensity.value;
            baseIsOverridden = vignette.intensity.overrideState;
            fovCamera = brain != null
                ? brain.ActiveVirtualCamera as CinemachineCamera
                : null;
            if (fovCamera != null) {
                baseFovDeg = fovCamera.Lens.FieldOfView;
            }
        }
        elapsedS = 0f;
        isPulsing = true;
        vignette.intensity.overrideState = true;

        if (Debug.isDebugBuild) {
            Debug.Log("CameraCoolDownEfx:\tpulse", this);
        }
    }

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        brain = GetComponent<CinemachineBrain>();
        if (brain == null) {
            Debug.LogWarning(
                "CameraCoolDownEfx:\tfail to get Component: CinemachineBrain",
                this
            );
        }

        if (volume == null) {
            Debug.LogWarning(
                "CameraCoolDownEfx:\tmust assign Inspector Field: volume",
                this
            );
            return;
        }

        // profile (not sharedProfile): edits stay off the shared asset
        if (!volume.profile.TryGet(out vignette)) {
            Debug.LogWarning(
                "CameraCoolDownEfx:\tvolume profile has no Vignette override",
                this
            );
        }
    }

    // the stamina bar lives in the HUD, so it is read after every Awake ran
    private void Start() {
        staminaBar = StaminaBarController.I;
        if (staminaBar == null) {
            Debug.LogError(
                "CameraCoolDownEfx:\tfail to get singleton: "
                    + "StaminaBarController",
                this
            );
            enabled = false;
            return;
        }

        staminaBar.ExhaustedMoveAttempted += OnExhaustedMoveAttempted;
    }

    private void OnDisable() {
        Unsubscribe();
        ResetPose();
    }

    private void OnDestroy() {
        Unsubscribe();
    }

    private void Update() {
        if (!isPulsing) {
            return;
        }

        elapsedS += Time.unscaledDeltaTime;
        float weight = CalcWeight(elapsedS);
        vignette.intensity.value = Mathf.Lerp(
            baseIntensity,
            peakIntensity,
            weight
        );
        if (fovCamera != null) {
            fovCamera.Lens.FieldOfView = baseFovDeg + fovDeltaDeg * weight;
        }

        if (elapsedS >= easeInS + easeOutS) {
            ResetPose();
            nextPulseTimeS = Time.unscaledTime + retriggerCooldownS;
        }
    }

    // Event Handlers  #########################################################
    // ignored while a pulse runs or cools down, a held key calls every step
    private void OnExhaustedMoveAttempted() {
        if (isPulsing || Time.unscaledTime < nextPulseTimeS) {
            return;
        }

        Pulse();
    }

    // private members  ########################################################
    private bool isPulsing;
    private float elapsedS;
    private float nextPulseTimeS;
    private float baseIntensity;
    private bool baseIsOverridden;
    private float baseFovDeg;

    // cached references  ------------------------------------------------------
    private Vignette vignette;
    private CinemachineBrain brain;
    private CinemachineCamera fovCamera;
    private StaminaBarController staminaBar;

    // private methods  ########################################################
    // 0 at rest, 1 at the peak; smoothstep eases both slopes
    private float CalcWeight(float timeS) {
        float t = timeS < easeInS
            ? (easeInS > 0f ? timeS / easeInS : 1f)
            : 1f - (easeOutS > 0f ? (timeS - easeInS) / easeOutS : 1f);
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
    }

    private void ResetPose() {
        if (isPulsing && vignette != null) {
            vignette.intensity.value = baseIntensity;
            vignette.intensity.overrideState = baseIsOverridden;
            if (fovCamera != null) {
                fovCamera.Lens.FieldOfView = baseFovDeg;
            }
        }
        isPulsing = false;
        fovCamera = null;
    }

    private void Unsubscribe() {
        if (staminaBar != null) {
            staminaBar.ExhaustedMoveAttempted -= OnExhaustedMoveAttempted;
            staminaBar = null;
        }
    }
}
