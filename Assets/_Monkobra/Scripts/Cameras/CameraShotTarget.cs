using UnityEngine;

/// <summary>
/// The point an event shot (coconut track, cobra alert) tracks: a blend of
/// the <see cref="CameraAnchor"/> and a moving subject, so the shot frames
/// the monkey and the subject together. Keeps the anchor's yaw, so the
/// camera never turns to face the subject, and eases both the follow of the
/// subject and its weight, so a fast subject (a thrown coconut) never whips
/// the view. Once the subject is gone, eg landed and destroyed, it holds its
/// last spot, so the shot can linger on it until the director ends it.
/// <para>
/// Runs in <c>LateUpdate</c> after the anchor (order -50) and before the
/// <c>CinemachineBrain</c> (order 100).
/// </para>
/// </summary>
[DefaultExecutionOrder(-40)]
public class CameraShotTarget: MonoBehaviour {
    // Public Methods  #########################################################
    /// <summary>
    /// Starts framing a subject, easing the view toward it from the anchor
    /// </summary>
    /// <param name="newSubject">what to frame with the monkey</param>
    public void SetSubject(Transform newSubject) {
        subject = newSubject;
        if (subject == null) {
            return;
        }
        subjectPos = subject.position;
        subjectVelocity = Vector3.zero;
        weight = 0f;
        hasSubject = true;
    }

    // Inspector Fields  #######################################################
    [Tooltip("the camera anchor, the monkey's side of the frame")]
    [SerializeField]
    private Transform anchor;

    [Tooltip("how far toward the subject the frame sits; 0 anchor, 1 subject")]
    [SerializeField]
    [Range(0f, 1f)]
    private float subjectWeight = 0.6f;

    [Tooltip("how long the frame takes to catch up with the subject; s")]
    [SerializeField]
    private float subjectSmoothTimeS = 0.25f;

    [Tooltip("how long the frame takes to reach its full weight; s")]
    [SerializeField]
    private float weightEaseS = 0.3f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (anchor == null) {
            Debug.LogWarning(
                "CameraShotTarget:\tmust assign Inspector Field: anchor",
                this
            );
            enabled = false;
        }
    }

    private void LateUpdate() {
        float dt = Time.deltaTime;
        if (subject != null) {
            subjectPos = Vector3.SmoothDamp(
                subjectPos,
                subject.position,
                ref subjectVelocity,
                subjectSmoothTimeS
            );
            weight = weightEaseS > 0f
                ? Mathf.MoveTowards(weight, subjectWeight, dt / weightEaseS)
                : subjectWeight;
        }

        Vector3 pos = hasSubject
            ? Vector3.Lerp(anchor.position, subjectPos, weight)
            : anchor.position;
        transform.SetPositionAndRotation(pos, anchor.rotation);
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        subjectSmoothTimeS = Mathf.Max(0f, subjectSmoothTimeS);
        weightEaseS = Mathf.Max(0f, weightEaseS);
    }

    // private members  ########################################################
    private Transform subject;
    private bool hasSubject; // a subject was set once, its last spot is held
    private Vector3 subjectPos; // eased subject position
    private Vector3 subjectVelocity; // SmoothDamp state
    private float weight; // eased share of the subject in the frame
}
