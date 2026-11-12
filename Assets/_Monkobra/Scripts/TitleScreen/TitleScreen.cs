using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// the title screen: any key or click opens the level selection; the prompt
/// pulses on unscaled time. Its picture is the last frame of the WebGL
/// loading page (FitWindow template), which it tells to fade out once drawn,
/// so the browser hands over to the game without a visible cut.
/// </summary>
public class TitleScreen: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("Input Actions asset holding the UI/AnyKey action")]
    private InputActionAsset inputActions;

    [SerializeField]
    [Tooltip("path of the any-key action inside the asset")]
    private string anyKeyActionPath = "UI/AnyKey";

    [SerializeField]
    [Min(0f)]
    [Tooltip("unscaled seconds a press is ignored after load, so the key "
        + "that opened this scene (eg Back) does not skip it")]
    private float inputDelayS = 0.4f;

    [SerializeField]
    [Tooltip("the \"Press Any Key\" prompt, faded in and out")]
    private CanvasGroup promptGroup;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("seconds of one full prompt pulse")]
    private float pulsePeriodS = 1.6f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("lowest prompt alpha during a pulse")]
    private float pulseMinAlpha = 0.25f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (inputActions == null) {
            Debug.LogError(
                "TitleScreen:\tmust assign Inspector Field: Input Actions",
                this
            );
            enabled = false;
            return;
        }

        anyKeyAction = inputActions.FindAction(anyKeyActionPath);
        if (anyKeyAction == null) {
            Debug.LogError(
                "TitleScreen:\tfail to find action: " + anyKeyActionPath,
                this
            );
            enabled = false;
            return;
        }

        if (promptGroup == null) {
            Debug.LogError(
                "TitleScreen:\tmust assign Inspector Field: Prompt Group",
                this
            );
        }
    }

    private void OnEnable() {
        anyKeyAction?.Enable();
        enabledTime = Time.unscaledTime;
    }

    private void OnDisable() {
        anyKeyAction?.Disable();
    }

    private IEnumerator Start() {
        // two frame steps, so at least one finished frame is on screen
        yield return null;
        yield return null;
#if UNITY_WEBGL && !UNITY_EDITOR
        MonkobraTitleShown();
#endif
    }

    private void Update() {
        if (promptGroup != null) {
            float wave = 0.5f
                + 0.5f * Mathf.Cos(Time.unscaledTime * 2f * Mathf.PI
                    / pulsePeriodS);
            promptGroup.alpha = Mathf.Lerp(pulseMinAlpha, 1f, wave);
        }

        if (Time.unscaledTime - enabledTime < inputDelayS) {
            return;
        }

        if (anyKeyAction.WasPressedThisFrame()) {
            SceneNames.Load(SceneNames.LEVEL_SELECT);
        }
    }

    // Private Members  ########################################################
    private InputAction anyKeyAction;
    private float enabledTime;

#if UNITY_WEBGL && !UNITY_EDITOR
    // Plugins/WebGL/TitleScreenLoader.jslib; a no-op on a page without the
    // loader, and harmless when the title is shown again
    [DllImport("__Internal")]
    private static extern void MonkobraTitleShown();
#endif
}
