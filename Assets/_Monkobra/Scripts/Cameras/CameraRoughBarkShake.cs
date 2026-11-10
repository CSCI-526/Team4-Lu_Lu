using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// A continuous rumble while the monkey moves across rough bark. Drives the
/// gains of the <see cref="CinemachineBasicMultiChannelPerlin"/> noise on
/// every camera below it, so the rumble holds through a shot change: the
/// noise sits at amplitude 0 until the monkey both touches the bark (its
/// <see cref="MonkeyController.RoughBarkSpeedScale"/> is below 1) and is
/// moving, then eases up, and eases back down when either stops. Standing
/// still on the bark, or being held by a web, stays calm. The strength is
/// scaled by <see cref="CameraConfig.ShakeScale"/> and silenced by the
/// comfort switch. Discrete hits are a separate impulse, see
/// <see cref="CameraHitShakeFeedback"/>.
/// <para>
/// Sits on the <c>CameraRig</c>, the parent of the cameras. Runs after the
/// controller step each frame, before the <c>CinemachineBrain</c>.
/// </para>
/// </summary>
public class CameraRoughBarkShake: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("bark noise gains, ease times and the global shake scale")]
    [SerializeField]
    private CameraConfig config;

    [Tooltip("shared input tuning, supplies the move action")]
    [SerializeField]
    private MonkeyInputConfig inputConfig;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogWarning(
                "CameraRoughBarkShake:\tmust assign Inspector Field: config",
                this
            );
        }
        if (inputConfig == null) {
            Debug.LogWarning(
                "CameraRoughBarkShake:\tmust assign Inspector Field: "
                    + "inputConfig",
                this
            );
        }
        if (config == null || inputConfig == null) {
            enabled = false;
            return;
        }

        noises = GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>(
            true
        );
    }

    // the monkey lives in another prefab, so it is read after every Awake ran
    private void Start() {
        monkey = FindFirstObjectByType<MonkeyController>();
        if (monkey == null) {
            Debug.LogWarning(
                "CameraRoughBarkShake:\tfail to find MonkeyController, "
                    + "no bark shake",
                this
            );
            enabled = false;
        }
    }

    private void OnDisable() {
        level = 0f;
        ApplyGains();
    }

    private void LateUpdate() {
        bool isOnBark = monkey.RoughBarkSpeedScale < 0.99f;
        bool isMoving = !monkey.IsHeld && IsMoveInputHeld();
        bool isShaking = isOnBark && isMoving && !config.IsReduceMotion;

        float easeS = isShaking ? config.RoughBarkEaseInS
            : config.RoughBarkEaseOutS;
        float step = easeS > 0f ? Time.deltaTime / easeS : 1f;
        level = Mathf.MoveTowards(level, isShaking ? 1f : 0f, step);
        ApplyGains();
    }

    // private members  ########################################################
    private float level; // 0 calm, 1 full bark shake

    // cached references  ------------------------------------------------------
    private CinemachineBasicMultiChannelPerlin[] noises;
    private MonkeyController monkey;

    // private methods  ########################################################
    private bool IsMoveInputHeld() {
        if (inputConfig.MoveAction == null) {
            return false;
        }
        return inputConfig.MoveAction.action.ReadValue<Vector2>().sqrMagnitude
            > 0.01f;
    }

    private void ApplyGains() {
        if (noises == null) {
            return;
        }
        float amplitude = config != null
            ? config.RoughBarkNoiseGain * config.ShakeScale * level
            : 0f;
        float frequency = config != null ? config.RoughBarkNoiseFrequency : 1f;
        for (int i = 0; i < noises.Length; i++) {
            if (noises[i] == null) {
                continue;
            }
            noises[i].AmplitudeGain = amplitude;
            noises[i].FrequencyGain = frequency;
        }
    }
}
