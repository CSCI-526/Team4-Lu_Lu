using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Camera shake for monkey hits, kept apart from hit detection. Sits on the
/// main camera, next to the <c>CinemachineImpulseListener</c>. Listens to
/// <see cref="HitBranchSensor.Hit"/> and fires a light impulse for a
/// branch, a heavier one for a falling cobra. Each kind has its own
/// <see cref="CinemachineImpulseSource"/> on the same camera, so shape and
/// duration are tuned per kind in the Inspector, and the strength is scaled
/// per kind and overall by <see cref="CameraConfig"/>. Every camera carries
/// an impulse listener, so the shake reaches whichever view is live.
/// </summary>
public class CameraHitShakeFeedback: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("hit scales and the global shake scale; the comfort switch mutes")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("impulse for a branch hit, the lighter shake")]
    [SerializeField]
    private CinemachineImpulseSource lightSource;

    [Tooltip("impulse for a falling cobra hit, the violent shake")]
    [SerializeField]
    private CinemachineImpulseSource heavySource;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraHitShakeFeedback:\tmust assign Inspector Field: config",
                this
            );
        }
        if (lightSource == null) {
            Debug.LogWarning(
                "CameraHitShakeFeedback:\tmust assign Inspector Field: "
                    + "lightSource",
                this
            );
        }
        if (heavySource == null) {
            Debug.LogWarning(
                "CameraHitShakeFeedback:\tmust assign Inspector Field: "
                    + "heavySource",
                this
            );
        }
    }

    // the sensor lives in the monkey, so it is read after every Awake ran
    private void Start() {
        sensor = HitBranchSensor.I;
        if (sensor == null) {
            Debug.LogError(
                "CameraHitShakeFeedback:\tfail to get singleton: "
                    + "HitBranchSensor",
                this
            );
            enabled = false;
            return;
        }

        sensor.Hit += OnHit;
    }

    private void OnDisable() {
        Unsubscribe();
    }

    private void OnDestroy() {
        Unsubscribe();
    }

    // Event Handlers  #########################################################
    // a missing heavy source falls back to the light one, so a hit never
    // goes without a shake
    private void OnHit(Collider _, HitBranchSensor.HitKind kind) {
        bool isHeavy = kind == HitBranchSensor.HitKind.FallingCobraController;
        CinemachineImpulseSource source = isHeavy
            ? (heavySource != null ? heavySource : lightSource)
            : lightSource;
        if (source == null) {
            return;
        }

        float force = 1f;
        if (config != null) {
            if (config.IsReduceMotion) {
                return;
            }
            force = config.ShakeScale
                * (isHeavy ? config.HeavyHitScale : config.LightHitScale);
        }
        source.GenerateImpulseWithForce(force);

        if (Debug.isDebugBuild) {
            Debug.Log($"CameraHitShakeFeedback:\tshake {kind}", this);
        }
    }

    // private members  ########################################################
    private HitBranchSensor sensor;

    // private methods  ########################################################
    private void Unsubscribe() {
        if (sensor != null) {
            sensor.Hit -= OnHit;
            sensor = null;
        }
    }
}
