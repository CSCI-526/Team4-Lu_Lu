using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// the title screen: Space, E, Enter, a click or a tap opens the level
/// selection; the prompt pulses on unscaled time. Its picture is the last
/// frame of the WebGL loading page (FitWindow template), which it tells to
/// fade out once drawn, so the browser hands over to the game without a
/// visible cut.
/// </summary>
public class TitleScreen: MonoBehaviour {
    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("Input Actions asset holding the UI/StartPress action")]
    private InputActionAsset inputActions;

    [SerializeField]
    [Tooltip("path of the start-press action inside the asset: Space, E, "
        + "Enter, left click or a touch")]
    private string startActionPath = "UI/StartPress";

    [SerializeField]
    [Min(0f)]
    [Tooltip("unscaled seconds a press is ignored after load, so the key "
        + "that opened this scene (eg Back) does not skip it")]
    private float inputDelayS = 0.4f;

    [SerializeField]
    [Tooltip("the start prompt, faded in and out")]
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

        startAction = inputActions.FindAction(startActionPath);
        if (startAction == null) {
            Debug.LogError(
                "TitleScreen:\tfail to find action: " + startActionPath,
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
        startAction?.Enable();
        enabledTime = Time.unscaledTime;
    }

    private void OnDisable() {
        startAction?.Disable();
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

        if (startAction.WasPressedThisFrame()) {
            SceneNames.Load(SceneNames.LEVEL_SELECT);
        }
    }

    // Private Members  ########################################################
    private InputAction startAction;
    private float enabledTime;

#if UNITY_WEBGL && !UNITY_EDITOR
    // Plugins/WebGL/TitleScreenLoader.jslib; a no-op on a page without the
    // loader, and harmless when the title is shown again
    [DllImport("__Internal")]
    private static extern void MonkobraTitleShown();
#endif
}
