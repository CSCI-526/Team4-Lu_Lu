using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Cool-down feedback on the whole view: a circular blackdown from the URP
/// Vignette that eases in and back out. Sits on the main camera. A pulse
/// restarts safely when called again, and the vignette returns to its
/// resting value when the pulse ends or this component is disabled.
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

    [Header("Timing")]
    [SerializeField]
    [Tooltip("ease from rest to peak; s")]
    private float easeInS = 0.1f;

    [SerializeField]
    [Tooltip("ease from peak back to rest; s")]
    private float easeOutS = 0.4f;

    // Public Methods  #########################################################
    /// <summary>
    /// starts a pulse, or restarts a running one from rest weight so repeated
    /// calls each read as a hit
    /// </summary>
    public void Pulse() {
        if (vignette == null) {
            return;
        }

        if (!isPulsing) {
            baseIntensity = vignette.intensity.value;
            baseIsOverridden = vignette.intensity.overrideState;
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

    private void OnDisable() {
        ResetPose();
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

        if (elapsedS >= easeInS + easeOutS) {
            ResetPose();
        }
    }

    // private members  ########################################################
    private bool isPulsing;
    private float elapsedS;
    private float baseIntensity;
    private bool baseIsOverridden;

    // cached references  ------------------------------------------------------
    private Vignette vignette;

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
        }
        isPulsing = false;
    }
}
