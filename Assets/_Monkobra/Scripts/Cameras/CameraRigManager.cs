using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Owns the camera views under the monkey's <c>CameraRig</c> and swaps which
/// one is live. Defaults to the climb view. For a moment after a coconut is
/// thrown (<see cref="CoconutManager.Used"/>) the look-down view takes over,
/// to follow the fall. Swap is done by raising the live camera's
/// <see cref="CinemachineCamera.Priority"/> above the others. The
/// <c>CinemachineBrain</c> on the main camera handles the actual blend.
/// </summary>
public class CameraRigManager: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Tooltip("view tracking the monkey from behind, looking up")]
    [SerializeField]
    private CinemachineCamera behindCamera;

    [Tooltip("view looking down, live for a moment after a coconut is thrown")]
    [SerializeField]
    private CinemachineCamera lookDownCamera;

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
        if (lookDownCamera == null) {
            Debug.LogWarning(
                "CameraRigManager:\tmust assign Inspector Field: "
                    + "lookDownCamera",
                this
            );
        }

        activeView = CameraView.Behind;
        ApplyPriorities();
        if (Debug.isDebugBuild) {
            Debug.Log($"CameraRigManager:\tactiveView changed: {activeView}");
        }
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

    // a look-down view ends on its own
    private void Update() {
        if (activeView == CameraView.Down) {
            UpdateActiveView();
        }
    }

    // Event Handlers  #########################################################
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
        Down,
    }

    private CameraView activeView;
    private float lookDownEndTime; // Time.time the look-down view ends
    private CoconutManager coconuts;

    // private methods  ########################################################
    private void UpdateActiveView() {
        CameraView desiredView = Time.time < lookDownEndTime
            ? CameraView.Down
            : CameraView.Behind;
        if (desiredView == activeView) {
            return;
        }

        activeView = desiredView;
        ApplyPriorities();
    }

    private void ApplyPriorities() {
        SetPriority(behindCamera, CameraView.Behind);
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
