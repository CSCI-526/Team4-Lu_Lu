using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the 4 camera views under the monkey's <c>CameraRig</c> and swaps
/// which one is live. Defaults to behind. While the interact action is
/// held, switches to look-left/look-right based on
/// <see cref="UpwardFruitSensor.I"/> (right only if fruit is reachable
/// there and not on the left; left otherwise, including when neither side
/// has reachable fruit). Releasing the action returns to behind. For a
/// moment after a coconut is thrown (<see cref="CoconutManager.Used"/>) the
/// look-down view takes over from behind, to follow the fall, but yields to
/// look-left/look-right while interact is held. Swap is done by raising the
/// live camera's <see cref="CinemachineCamera.Priority"/> above the others. The
/// <c>CinemachineBrain</c> on the main camera handles the actual blend.
/// </summary>
public class CameraRigManager: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("view tracking the monkey from behind")]
    [SerializeField]
    private CinemachineCamera behindCamera;

    [Tooltip("view tracking the monkey while fruit is reachable on the left")]
    [SerializeField]
    private CinemachineCamera lookLeftCamera;

    [Tooltip("view tracking the monkey while fruit is reachable on the right")]
    [SerializeField]
    private CinemachineCamera lookRightCamera;

    [Tooltip("view looking down, live for a moment after a coconut is thrown")]
    [SerializeField]
    private CinemachineCamera lookDownCamera;

    [Tooltip("action that must be held for the camera to leave behind view")]
    [SerializeField]
    private InputActionReference interactAction;

    [Tooltip("the look-down view stays live this long after a throw; s")]
    [SerializeField]
    [Min(0f)]
    private float coconutLookDownS = 1.2f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (behindCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: behindCamera",
                this
            );
        }
        if (lookLeftCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "lookLeftCamera",
                this
            );
        }
        if (lookRightCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "lookRightCamera",
                this
            );
        }
        if (lookDownCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "lookDownCamera",
                this
            );
        }
        if (interactAction == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "interactAction",
                this
            );
        }

        activeView = CameraView.Behind;
        ApplyPriorities();
        if (Debug.isDebugBuild) {
            Debug.Log($"CameraRigManager:\tactiveView changed: {activeView}");
        }
    }

    private void OnEnable() {
        interactAction.action.Enable();
        interactAction.action.performed += OnInteractPerformed;
        interactAction.action.canceled += OnInteractCanceled;
    }

    private void OnDisable() {
        interactAction.action.performed -= OnInteractPerformed;
        interactAction.action.canceled -= OnInteractCanceled;
        interactAction.action.Disable();
    }

    private void Start() {
        coconuts = CoconutManager.I;
        if (coconuts == null) {
            Debug.LogWarning(
                "CameraRigManager:\tfail to get singleton: CoconutManager, "
                    + "no look-down view after a throw",
                this
            );
            return;
        }
        coconuts.Used += OnCoconutUsed;
    }

    private void OnDestroy() {
        if (coconuts != null) {
            coconuts.Used -= OnCoconutUsed;
        }
    }

    // held interact follows the fruit, and a look-down view ends on its own
    private void Update() {
        if (isInteractHeld || activeView == CameraView.Down) {
            UpdateActiveView();
        }
    }

    // Event Handlers  #########################################################
    private void OnInteractPerformed(InputAction.CallbackContext context) {
        isInteractHeld = true;
        UpdateActiveView();
    }

    private void OnInteractCanceled(InputAction.CallbackContext context) {
        isInteractHeld = false;
        UpdateActiveView();
    }

    private void OnCoconutUsed() {
        lookDownEndTime = Time.time + coconutLookDownS;
        UpdateActiveView();
    }

    // constants  ##############################################################
    private const int LIVE_PRIORITY = 20;
    private const int STANDBY_PRIORITY = 10;

    // private members  ########################################################
    private enum CameraView {
        Behind,
        Left,
        Right,
        Down,
    }

    private CameraView activeView;
    private bool isInteractHeld;
    private float lookDownEndTime; // Time.time the look-down view ends
    private CoconutManager coconuts;

    // private methods  ########################################################
    private void UpdateActiveView() {
        UpwardFruitSensor fruitDetection = UpwardFruitSensor.I;
        if (isInteractHeld && fruitDetection == null) {
            Debug.LogError(
                "CameraRigManager:\tfail to get singleton: "
                    + "UpwardFruitSensor",
                this
            );
            return;
        }

        // precedence: look left/right (held) over look down over behind
        CameraView desiredView =
            isInteractHeld
                ? fruitDetection.ReachForRight && !fruitDetection.ReachForLeft
                    ? CameraView.Right
                    : CameraView.Left
            : Time.time < lookDownEndTime ? CameraView.Down
            : CameraView.Behind;

        if (desiredView == activeView) {
            return;
        }

        activeView = desiredView;
        ApplyPriorities();
    }

    private void ApplyPriorities() {
        SetPriority(behindCamera, CameraView.Behind);
        SetPriority(lookLeftCamera, CameraView.Left);
        SetPriority(lookRightCamera, CameraView.Right);
        SetPriority(lookDownCamera, CameraView.Down);
    }

    // the camera of the active view goes live, every other one stands by
    private void SetPriority(CinemachineCamera view, CameraView viewKind) {
        if (view == null) {
            return;
        }
        view.Priority = new PrioritySettings {
            Enabled = true,
            Value = activeView == viewKind ? LIVE_PRIORITY : STANDBY_PRIORITY,
        };
    }
}
