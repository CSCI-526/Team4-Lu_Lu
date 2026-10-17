using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Camera shake for monkey hits, kept apart from hit detection. Listens to
/// <see cref="HitBranchDetection.Hit"/> and fires a light impulse for a
/// branch, a heavier one for a falling cobra. Each kind has its own
/// <see cref="CinemachineImpulseSource"/>, so amplitude and duration are
/// tuned per kind in the Inspector.
/// </summary>
public class CameraHitShakeEfx: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("sensor that reports the hits")]
    [SerializeField]
    private HitBranchDetection hitSensor;

    [Tooltip("impulse for a branch hit, the lighter shake")]
    [SerializeField]
    private CinemachineImpulseSource lightSource;

    [Tooltip("impulse for a falling cobra hit, the violent shake")]
    [SerializeField]
    private CinemachineImpulseSource heavySource;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (hitSensor == null) {
            Debug.LogWarning(
                "CameraHitShakeEfx:\tmust assign Inspector Field: hitSensor",
                this
            );
        }
        if (lightSource == null) {
            Debug.LogWarning(
                "CameraHitShakeEfx:\tmust assign Inspector Field: "
                    + "lightSource",
                this
            );
        }
        if (heavySource == null) {
            Debug.LogWarning(
                "CameraHitShakeEfx:\tmust assign Inspector Field: "
                    + "heavySource",
                this
            );
        }
    }

    private void OnEnable() {
        if (hitSensor != null) {
            hitSensor.Hit += OnHit;
        }
    }

    private void OnDisable() {
        if (hitSensor != null) {
            hitSensor.Hit -= OnHit;
        }
    }

    // Event Handlers  #########################################################
    // a missing heavy source falls back to the light one, so a hit never
    // goes without a shake
    private void OnHit(Collider _, HitBranchDetection.HitKind kind) {
        CinemachineImpulseSource source =
            kind == HitBranchDetection.HitKind.FallingCobra
                ? (heavySource != null ? heavySource : lightSource)
                : lightSource;
        if (source == null) {
            return;
        }

        source.GenerateImpulse();

        if (Debug.isDebugBuild) {
            Debug.Log($"CameraHitShakeEfx:\tshake {kind}", this);
        }
    }
}
