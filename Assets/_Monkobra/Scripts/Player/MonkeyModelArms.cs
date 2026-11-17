using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Poses the skinned monkey model's arms every frame.
/// <para>
/// While climbing, each hand follows the climb cycle of
/// <see cref="MonkeyModelAnimator"/>, which keeps pace with the monkey's real
/// speed: planted on the bark, sliding down as the body rises, then swung
/// back up. While reaching for fruit or leaping it blends over to its physics
/// <see cref="ArmController"/> hand, so the model's hand lands where the grab
/// is judged.
/// </para>
/// <para>
/// Each arm is a two bone IK chain whose elbow bends out, down and back, like
/// hugging the trunk. Past a bent arm's reach the upper arm and forearm both
/// lengthen alike; the animator reskins the arm so it keeps its thickness.
/// The wrist only bends, never twists, so the forearm never wrings thin.
/// </para>
/// <para>
/// Pure visuals: it reads the physics arms and moves model bones only, never a
/// collider, body or joint, so the physics hands judge every touch and grab
/// exactly as before. Runs after <see cref="MonkeyModelAnimator"/>.
/// </para>
/// </summary>
[DefaultExecutionOrder(10)]
public class MonkeyModelArms: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("root of the skinned monkey model, its axes give in, up & side")]
    private Transform modelRoot;

    [SerializeField]
    [Tooltip("the model's left arm bones and the physics arm they follow")]
    private ArmRig leftArm = new ArmRig();

    [SerializeField]
    [Tooltip("the model's right arm bones and the physics arm they follow")]
    private ArmRig rightArm = new ArmRig();

    [Header("Climb")]
    [SerializeField]
    [Tooltip("hand distance out from the middle while climbing, model units")]
    private float handSideU = 0.55f;

    [SerializeField]
    [Tooltip("hand height at the bottom / top of its stroke, model units")]
    private Vector2 handHeightU = new Vector2(1.45f, 2.05f);

    [SerializeField]
    [Tooltip("hand distance toward the trunk while gripping, model units")]
    private float handDepthU = 0.42f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("how far a swinging hand lifts off the bark, model units")]
    private float handClearanceU = 0.15f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "how fast a hand moves between its climb stroke and its physics hand "
            + "when a reach or leap starts or ends; 1/s"
    )]
    private float followSharpness = 14f;

    [Header("Reach")]
    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "how far short of the physics hand the model's wrist stops, so the "
            + "palm and fingers cover the hand instead of overshooting it; u"
    )]
    private float wristInsetU = 0.1f;

    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip(
        "shortest the arm may squash to, as a share of its modelled length, "
            + "so a hand pulled close never folds into the shoulder"
    )]
    private float minLengthScale = 0.5f;

    [SerializeField]
    [Range(0.5f, 0.99f)]
    [Tooltip(
        "how straight the arm may get before it stretches instead, as a "
            + "share of its full length: lower keeps a deeper elbow bend"
    )]
    private float maxStraightness = 0.88f;

    [Header("Elbow")]
    [SerializeField]
    [Tooltip(
        "direction the elbows bend toward, in the model's frame: x out to "
            + "the arm's own side (mirrored per arm), y up, z toward the trunk"
    )]
    private Vector3 elbowPole = new Vector3(0.7f, -1f, -0.6f);

    [Header("Hand")]
    [SerializeField]
    [Range(0f, 60f)]
    [Tooltip("how far the wrist bends the fingers in to the bark while "
        + "gripping; deg")]
    private float wristGripDeg = 25f;

    [SerializeField]
    [Range(0f, 45f)]
    [Tooltip("finger curl per joint while gripping the bark; deg")]
    private float fingerGripDeg = 28f;

    [SerializeField]
    [Range(0f, 45f)]
    [Tooltip("finger curl per joint with the hand open, reaching; deg")]
    private float fingerOpenDeg = 4f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("how far a hand opens at the top of its swing, between grips")]
    private float swingOpenShare = 0.6f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("share of the finger curl the thumb takes")]
    private float thumbCurlShare = 0.5f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        if (modelRoot == null) {
            Debug.LogWarning(
                "MonkeyModelArms:\tmust assign Inspector Field: modelRoot",
                this
            );
            return;
        }
        animator = GetComponent<MonkeyModelAnimator>();
        leftArm.Bind(this, modelRoot, "leftArm");
        rightArm.Bind(this, modelRoot, "rightArm");
        leftArm.phaseOffset = MonkeyModelAnimator.LEFT_LIMB_OFFSET;
        rightArm.phaseOffset = MonkeyModelAnimator.RIGHT_LIMB_OFFSET;
    }

    // after physics, the arm scripts and the torso pose this frame
    private void LateUpdate() {
        float deltaS = Time.deltaTime;
        bool isLeaping = GameController.I != null && GameController.I.IsLeaping;
        PoseArm(leftArm, isLeaping, deltaS);
        PoseArm(rightArm, isLeaping, deltaS);
    }

    // private members  ########################################################
    private const float MIN_LENGTH_U = 0.0001f;

    private MonkeyModelAnimator animator;

    private void PoseArm(ArmRig arm, bool isLeaping, float deltaS) {
        if (!arm.IsBound || !arm.target.gameObject.activeInHierarchy) {
            return;
        }

        // start from the modelled pose every frame, so nothing accumulates
        arm.ResetToBind();

        // Target  -------------------------------------------------------------
        // the physics hand rules while it reaches or leaps, else the cycle
        bool followsPhysics = arm.IsReaching || isLeaping || animator == null;
        arm.follow = Mathf.Lerp(
            arm.follow,
            followsPhysics ? 1f : 0f,
            1f - Mathf.Exp(-followSharpness * deltaS)
        );

        float lift = 0f;
        float clearance = 0f;
        if (animator != null) {
            animator.LimbCycle(arm.phaseOffset, out lift, out clearance);
        }
        Vector3 climbWrist = modelRoot.TransformPoint(new Vector3(
            arm.side * handSideU,
            Mathf.Lerp(handHeightU.x, handHeightU.y, lift),
            handDepthU - clearance * handClearanceU
        ));

        Vector3 shoulder = arm.upperArm.position;
        Vector3 toPhysics = arm.target.position - shoulder;
        float physicsU = toPhysics.magnitude;
        Vector3 physicsWrist = physicsU > MIN_LENGTH_U
            ? shoulder
                + toPhysics / physicsU * Mathf.Max(physicsU - wristInsetU, 0f)
            : climbWrist;

        Vector3 wrist = Vector3.Lerp(climbWrist, physicsWrist, arm.follow);

        // Reach  --------------------------------------------------------------
        float upperU = Vector3.Distance(shoulder, arm.forearm.position);
        float forearmU =
            Vector3.Distance(arm.forearm.position, arm.hand.position);
        float restU = upperU + forearmU;
        if (restU < MIN_LENGTH_U) {
            return;
        }
        Vector3 toWrist = wrist - shoulder;
        float wristU = toWrist.magnitude;
        float clampedU = Mathf.Max(wristU, restU * minLengthScale);
        if (wristU > MIN_LENGTH_U) {
            wrist = shoulder + toWrist / wristU * clampedU;
        }

        // past a comfortably bent arm both bones lengthen alike
        arm.Stretch(Mathf.Max(1f, clampedU / (restU * maxStraightness)));

        Vector3 pole = modelRoot.TransformDirection(
            new Vector3(elbowPole.x * arm.side, elbowPole.y, elbowPole.z)
        );
        ModelTwoBoneIK.Solve(arm.upperArm, arm.forearm, arm.hand, wrist, pole);

        // Hand  ---------------------------------------------------------------
        // 1 gripping the bark, 0 open: opens through each swing and to reach
        float grip = (1f - arm.follow) * (1f - swingOpenShare * clearance);

        // bend only, no twist round the forearm, so it never wrings thin
        Vector3 fingers = arm.hand.rotation * arm.fingersInHand;
        Vector3 flexed = Vector3.RotateTowards(
            fingers,
            modelRoot.forward,
            wristGripDeg * grip * Mathf.Deg2Rad,
            0f
        );
        arm.hand.rotation =
            Quaternion.FromToRotation(fingers, flexed) * arm.hand.rotation;

        Vector3 palm = arm.hand.rotation * arm.palmInHand;
        float curlDeg = Mathf.Lerp(fingerOpenDeg, fingerGripDeg, grip);
        arm.CurlFingers(
            Vector3.Cross(flexed, palm),
            curlDeg,
            curlDeg * thumbCurlShare
        );
    }

    // Nested Types  ###########################################################
    [Serializable]
    private class ArmRig {
        [Tooltip("physics arm root, which is its hand, eg LArm")]
        public Transform target;

        [Tooltip("model bone from shoulder to elbow, eg ORG-upper_arm.L")]
        public Transform upperArm;

        [Tooltip("model bone from elbow to wrist, eg ORG-forearm.L")]
        public Transform forearm;

        [Tooltip("model wrist bone, eg ORG-hand.L")]
        public Transform hand;

        public bool IsBound { get; private set; }

        public bool IsReaching =>
            controller != null && controller.IsReaching;

        // -1 on the model's left, +1 on its right, mirrors the arm's pose
        [NonSerialized]
        public float side = 1f;

        // this hand's offset in the shared climb cycle
        [NonSerialized]
        public float phaseOffset;

        // 0 on the climb stroke, 1 on the physics hand
        [NonSerialized]
        public float follow;

        // the hand's finger & palm directions in its own frame
        [NonSerialized]
        public Vector3 fingersInHand;

        [NonSerialized]
        public Vector3 palmInHand;

        private ArmController controller;

        private Quaternion bindUpperArmRotation;
        private Quaternion bindForearmRotation;
        private Quaternion bindHandRotation;
        private Vector3 bindForearmPosition;
        private Vector3 bindHandPosition;

        private readonly List<Transform> fingerBones = new List<Transform>();
        private readonly List<Quaternion> fingerRotations =
            new List<Quaternion>();
        private readonly List<bool> isThumb = new List<bool>();

        public void Bind(MonkeyModelArms owner, Transform model, string label) {
            if (target == null || upperArm == null || forearm == null
                || hand == null) {
                Debug.LogWarning(
                    "MonkeyModelArms:\tmust assign Inspector Field: "
                        + $"{label} (target, upperArm, forearm, hand)",
                    owner
                );
                return;
            }
            controller = target.GetComponent<ArmController>();

            bindUpperArmRotation = upperArm.localRotation;
            bindForearmRotation = forearm.localRotation;
            bindHandRotation = hand.localRotation;
            bindForearmPosition = forearm.localPosition;
            bindHandPosition = hand.localPosition;

            side = model.InverseTransformPoint(upperArm.position).x < 0f
                ? -1f
                : 1f;

            // the rest pose is a T-pose: fingers out along the arm, palms
            // facing the ground
            Vector3 fingersWorld =
                (hand.position - forearm.position).normalized;
            Vector3 palmWorld =
                Vector3.ProjectOnPlane(-model.up, fingersWorld).normalized;
            Quaternion toHand = Quaternion.Inverse(hand.rotation);
            fingersInHand = toHand * fingersWorld;
            palmInHand = toHand * palmWorld;

            CollectFingers(hand);
            IsBound = true;
        }

        public void ResetToBind() {
            upperArm.localRotation = bindUpperArmRotation;
            forearm.localRotation = bindForearmRotation;
            hand.localRotation = bindHandRotation;
            forearm.localPosition = bindForearmPosition;
            hand.localPosition = bindHandPosition;
            for (int i = 0; i < fingerBones.Count; i++) {
                fingerBones[i].localRotation = fingerRotations[i];
            }
        }

        // moves elbow & wrist out along the arm, both bones lengthen alike
        public void Stretch(float scale) {
            forearm.localPosition = bindForearmPosition * scale;
            hand.localPosition = bindHandPosition * scale;
        }

        // parents come before children, so each joint adds to the curl
        public void CurlFingers(Vector3 axis, float fingerDeg, float thumbDeg) {
            for (int i = 0; i < fingerBones.Count; i++) {
                fingerBones[i].rotation =
                    Quaternion.AngleAxis(
                        isThumb[i] ? thumbDeg : fingerDeg, axis)
                    * fingerBones[i].rotation;
            }
        }

        // the deforming finger bones, depth first so parents lead
        private void CollectFingers(Transform parent) {
            foreach (Transform child in parent) {
                bool isFinger = child.name.StartsWith("ORG-f_");
                bool thumb = child.name.StartsWith("ORG-thumb");
                if (isFinger || thumb) {
                    fingerBones.Add(child);
                    fingerRotations.Add(child.localRotation);
                    isThumb.Add(thumb);
                }
                CollectFingers(child);
            }
        }
    }
}
