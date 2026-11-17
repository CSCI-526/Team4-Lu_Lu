using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Procedural climbing animation for the skinned monkey model: the climb
/// cycle all 4 limbs share, legs, torso, tail and an over the shoulder look
/// back down the trunk.
/// <list type="bullet">
/// <item>The climb cycle advances with how fast the monkey actually moves, so
/// the limbs keep up at any climb speed, run backward while it climbs down
/// and hold still while it stops. Limbs move in the diagonal gait of a real
/// monkey: each hand with the opposite foot. <see cref="MonkeyModelArms"/>
/// reads the hands' share through <see cref="LimbCycle"/>.</item>
/// <item>Hips sway onto the planted foot, the spine rolls against it and
/// the chest breathes.</item>
/// <item>On start a runtime copy of the mesh is reskinned: the arms' elbow
/// and wrist rings each move as one piece, so a stretched arm keeps its
/// thickness, and the tail, skinned to the hips in the asset, gets a chain
/// of bones that swings on a spring trailing the body.</item>
/// <item>Every few seconds the monkey turns its head over a shoulder and looks
/// down, showing its face; much more often while the great cobra is close
/// or surging. Never while reaching, leaping, webbed or in the boss arena.
/// </item>
/// </list>
/// Pure visuals: reads the monkey's state and moves model bones only, never a
/// collider, body, joint or gameplay value. Runs before
/// <see cref="MonkeyModelArms"/>, which then fits the arms to the posed torso.
/// </summary>
[DefaultExecutionOrder(-10)]
public class MonkeyModelAnimator: MonoBehaviour {
    // Public Members  #########################################################
    // the left hand leads the cycle, the right hand half a cycle behind
    public const float LEFT_LIMB_OFFSET = 0f;
    public const float RIGHT_LIMB_OFFSET = 0.5f;

    // Public Methods  #########################################################
    /// <summary>
    /// Where one limb is in the shared climb cycle: a long stance with the
    /// limb planted on the bark, sliding down as the body rises past it,
    /// then a quick swing back up, lifted off the bark.
    /// </summary>
    /// <param name="phaseOffset">the limb's offset in the cycle, 0 or 0.5
    /// </param>
    /// <param name="lift">0 at the bottom of the stroke, 1 at the top</param>
    /// <param name="clearance">0 on the bark, 1 at the swing's peak off it
    /// </param>
    public void LimbCycle(
        float phaseOffset,
        out float lift,
        out float clearance
    ) {
        float phase = Mathf.Repeat(climbPhase + phaseOffset, 1f);
        if (phase < stanceShare) {
            lift = 1f - phase / stanceShare;
            clearance = 0f;
            return;
        }
        float swing = (phase - stanceShare) / (1f - stanceShare);
        lift = Mathf.SmoothStep(0f, 1f, swing);
        clearance = Mathf.Sin(swing * Mathf.PI);
    }

    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("root of the skinned monkey model, eg MonkeyModel_MrMo_A")]
    private Transform modelRoot;

    [SerializeField]
    [Tooltip("left physics arm root, ie its hand, eg LArm")]
    private Transform leftHand;

    [SerializeField]
    [Tooltip("right physics arm root, ie its hand, eg RArm")]
    private Transform rightHand;

    [Header("Climb Cycle")]
    [SerializeField]
    [Min(0.1f)]
    [Tooltip(
        "distance moved per full climb cycle, each hand & foot stepping "
            + "once; lower steps faster; u"
    )]
    private float strideU = 1.6f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("fastest the cycle runs, so a dash never blurs the limbs; 1/s")]
    private float maxCycleHz = 4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("speed under which the monkey counts as still, the limbs hold; "
        + "u/s")]
    private float minMoveSpeedU = 0.2f;

    [SerializeField]
    [Range(0.3f, 0.9f)]
    [Tooltip("share of the cycle a limb spends planted on the bark")]
    private float stanceShare = 0.6f;

    [Header("Legs")]
    [SerializeField]
    [Tooltip("foot distance out from the middle, model units")]
    private float footSideU = 0.32f;

    [SerializeField]
    [Tooltip(
        "foot height at the bottom / top of its stroke, model units; the hip "
            + "is at 0.91, a high foot folds the thigh up into the body"
    )]
    private Vector2 footHeightU = new Vector2(0.15f, 0.45f);

    [SerializeField]
    [Tooltip(
        "foot distance toward the trunk at the bottom / top of its stroke, "
            + "model units"
    )]
    private Vector2 footDepthU = new Vector2(0.15f, 0.28f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("how far a swinging foot lifts off the bark, model units")]
    private float footClearanceU = 0.1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "how far the feet trail sideways while the monkey circles the trunk, "
            + "model units"
    )]
    private float footStrafeU = 0.08f;

    [SerializeField]
    [Range(0f, 90f)]
    [Tooltip("toe up pitch that sets the sole flat on the bark; deg")]
    private float footPitchDeg = 60f;

    [SerializeField]
    [Tooltip(
        "direction the knees point, model frame: x out to the leg's own side "
            + "(mirrored per leg), y up, z toward the trunk; the thigh twists "
            + "so the kneecap faces it, the shin folds behind"
    )]
    private Vector3 kneePole = new Vector3(0.8f, 0.2f, 0.6f);

    [Header("Body")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("hip shift onto the planted foot, model units")]
    private float hipSwayU = 0.04f;

    [SerializeField]
    [Range(0f, 20f)]
    [Tooltip("spine roll against the hip sway; deg")]
    private float spineRollDeg = 5f;

    [SerializeField]
    [Range(0f, 10f)]
    [Tooltip("chest rise and fall of each breath; deg")]
    private float breathDeg = 1.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("breaths per second")]
    private float breathHz = 0.35f;

    [Header("Arm Skin")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip(
        "share of the elbow ring that follows the forearm, the rest the "
            + "upper arm; the ring always moves as one, so the arm keeps its "
            + "thickness when stretched"
    )]
    private float elbowForearmShare = 0.6f;

    [Header("Leg Skin")]
    [SerializeField]
    [Range(0f, 0.6f)]
    [Tooltip(
        "share of the thigh's top ring that stays with the hips, cushioning "
            + "the butt so a raised thigh never folds it into the body"
    )]
    private float hipRingHipsShare = 0.25f;

    [Header("Tail")]
    [SerializeField]
    [Range(2, 8)]
    [Tooltip("bones made for the tail on start")]
    private int tailBoneCount = 4;

    [SerializeField]
    [Min(0f)]
    [Tooltip("tail swing per unit of body speed, trailing the motion; deg "
        + "per u/s")]
    private float tailLagDegPerU = 8f;

    [SerializeField]
    [Range(0f, 90f)]
    [Tooltip("largest tail swing from motion; deg")]
    private float tailMaxDeg = 35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("tail spring pull back to its goal; higher is snappier")]
    private float tailStiffness = 40f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("tail spring damping; lower wobbles longer after a stop")]
    private float tailDamping = 5f;

    [SerializeField]
    [Range(0f, 30f)]
    [Tooltip("idle tail curl wave per bone; deg")]
    private float tailIdleDeg = 6f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("idle tail curl waves per second")]
    private float tailIdleHz = 0.6f;

    [Header("Look Back")]
    [SerializeField]
    [Range(0f, 180f)]
    [Tooltip("how far the head turns over the shoulder, spine included; deg")]
    private float lookYawDeg = 110f;

    [SerializeField]
    [Range(0f, 80f)]
    [Tooltip("how far the head tips down to look down the trunk; deg")]
    private float lookPitchDeg = 30f;

    [SerializeField]
    [Min(0.05f)]
    [Tooltip("time to turn round, and again to turn back; s")]
    private float lookTurnS = 0.35f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("time spent looking down; s")]
    private float lookHoldS = 0.8f;

    [SerializeField]
    [Tooltip("random wait between looks while the cobra is far, min / max; s")]
    private Vector2 calmLookIntervalS = new Vector2(4f, 8f);

    [SerializeField]
    [Tooltip(
        "random wait between looks while the cobra is close or surging, "
            + "min / max; s"
    )]
    private Vector2 urgentLookIntervalS = new Vector2(1.5f, 3f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("gap below the monkey under which the cobra counts as close; u")]
    private float cobraNearU = 12f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (modelRoot == null || leftHand == null || rightHand == null) {
            Debug.LogWarning(
                "MonkeyModelAnimator:\tmust assign Inspector Fields: "
                    + "modelRoot, leftHand, rightHand",
                this
            );
            return;
        }

        leftArm = leftHand.GetComponent<ArmController>();
        rightArm = rightHand.GetComponent<ArmController>();
        web = GetComponent<SpiderWebStruggle>();

        hips = Bone("ORG-hips");
        spine = Bone("ORG-spine");
        chest = Bone("ORG-chest");
        neck = Bone("ORG-neck");
        head = Bone("ORG-head");
        leftLeg = BindLeg(".L");
        rightLeg = BindLeg(".R");
        if (hips == null || spine == null || chest == null || neck == null
            || head == null || leftLeg == null || rightLeg == null) {
            Debug.LogWarning(
                "MonkeyModelAnimator:\tmodel is missing a Rigify ORG bone, "
                    + "animation off",
                this
            );
            return;
        }
        Remember(hips);
        Remember(spine);
        Remember(chest);
        Remember(neck);
        Remember(head);

        BuildSkin();

        lastPosition = transform.position;
        ScheduleNextLook(false);
        isReady = true;
    }

    private void OnDestroy() {
        if (skinMesh != null) {
            Destroy(skinMesh);
        }
    }

    private void LateUpdate() {
        if (!isReady) {
            return;
        }
        float deltaS = Time.deltaTime;

        ResetToBind();
        UpdateMotion(deltaS);
        UpdateCycle(deltaS);
        UpdateLook(deltaS);

        // a hand and the opposite foot share the cycle: left foot with the
        // right hand, half a cycle from the left hand
        LimbCycle(RIGHT_LIMB_OFFSET, out float leftLift, out float leftClear);
        LimbCycle(LEFT_LIMB_OFFSET, out float rightLift, out float rightClear);

        PoseBody(leftLift - rightLift);
        PoseLook();
        PoseLeg(leftLeg, leftLift, leftClear);
        PoseLeg(rightLeg, rightLift, rightClear);
        PoseTail(deltaS);
    }

    // private members  ########################################################
    private const float MIN_LENGTH_U = 0.0001f;
    private const float MAX_DELTA_S = 0.05f;

    // share of the look's yaw each bone adds, spine to head, sums to 1
    private static readonly float[] LOOK_YAW_SHARES =
        { 0.1f, 0.2f, 0.3f, 0.4f };

    // a tail vertex is mostly the hips' and sits behind them
    private const float TAIL_MIN_HIPS_WEIGHT = 0.8f;
    private const float TAIL_MIN_DEPTH_U = 0.05f;

    // tail base share of the tail fading back into the hips
    private const float TAIL_BASE_BLEND = 0.12f;

    // half width of an arm ring's band, as a share of the forearm's length
    private const float ARM_RING_BAND = 0.25f;

    // leg rings: half heights of the hip & knee bands, the ankle band's
    // height above the ankle, and the foot's share of the ankle ring; model
    // units, the rest pose's rings sit 0.4u apart
    private const float HIP_RING_BAND_U = 0.13f;
    private const float KNEE_RING_BAND_U = 0.08f;
    private const float ANKLE_RING_BAND_U = 0.06f;
    private const float ANKLE_FOOT_SHARE = 0.65f;

    private bool isReady;

    private ArmController leftArm;
    private ArmController rightArm;
    private SpiderWebStruggle web;

    private Transform hips;
    private Transform spine;
    private Transform chest;
    private Transform neck;
    private Transform head;
    private Leg leftLeg;
    private Leg rightLeg;

    private readonly List<Transform> restBones = new List<Transform>();
    private readonly List<Vector3> restPositions = new List<Vector3>();
    private readonly List<Quaternion> restRotations = new List<Quaternion>();

    // body motion in the model's frame, smoothed; u/s
    private Vector3 lastPosition;
    private Vector3 modelVelocity;

    // the shared climb cycle, 0 to 1
    private float climbPhase;

    // look back: eased weight 0 to 1, the side it turns to, timers
    private bool isLooking;
    private float lookWeight;
    private float lookSide = 1f;
    private float lookEndTime;
    private float nextLookTime;

    // reskinned mesh copy; tail bones and the spring's angles & speeds; deg
    private Mesh skinMesh;
    private readonly List<Transform> tailBones = new List<Transform>();
    private Vector2 tailAngle;
    private Vector2 tailSpeed;

    // Setup  ------------------------------------------------------------------
    private Transform Bone(string boneName) {
        return ModelTwoBoneIK.FindBone(modelRoot, boneName);
    }

    private void Remember(Transform bone) {
        restBones.Add(bone);
        restPositions.Add(bone.localPosition);
        restRotations.Add(bone.localRotation);
    }

    private Leg BindLeg(string suffix) {
        Leg leg = new Leg {
            thigh = Bone("ORG-thigh" + suffix),
            shin = Bone("ORG-shin" + suffix),
            foot = Bone("ORG-foot" + suffix),
            heel = Bone("ORG-heel" + suffix),
        };
        if (leg.thigh == null || leg.shin == null || leg.foot == null) {
            return null;
        }
        leg.side = modelRoot.InverseTransformPoint(leg.thigh.position).x < 0f
            ? -1f
            : 1f;
        leg.footInModel =
            Quaternion.Inverse(modelRoot.rotation) * leg.foot.rotation;
        // standing at rest, the kneecap faces the model's front
        leg.kneeFrontInThigh =
            Quaternion.Inverse(leg.thigh.rotation) * modelRoot.forward;
        Remember(leg.thigh);
        Remember(leg.shin);
        Remember(leg.foot);
        if (leg.heel != null) {
            Remember(leg.heel);
        }
        return leg;
    }

    // reskins a copy of the mesh, leaving the shared asset untouched
    private void BuildSkin() {
        SkinnedMeshRenderer skin =
            modelRoot.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skin == null || skin.sharedMesh == null
            || !skin.sharedMesh.isReadable) {
            Debug.LogWarning(
                "MonkeyModelAnimator:\tno readable skinned mesh, arms thin "
                    + "out when stretched and the tail stays still",
                this
            );
            return;
        }

        Mesh source = skin.sharedMesh;
        SkinData data = new SkinData {
            bones = new List<Transform>(skin.bones),
            bindPoses = new List<Matrix4x4>(source.bindposes),
            weights = source.boneWeights,
            vertices = source.vertices,
        };
        if (data.weights.Length != data.vertices.Length
            || data.bones.Count != data.bindPoses.Count) {
            return;
        }

        // every vertex where the rest pose puts it, model frame
        data.restPoints = new Vector3[data.vertices.Length];
        for (int i = 0; i < data.vertices.Length; i++) {
            int bone = data.weights[i].boneIndex0;
            data.restPoints[i] = modelRoot.InverseTransformPoint(
                (data.bones[bone].localToWorldMatrix * data.bindPoses[bone])
                    .MultiplyPoint3x4(data.vertices[i])
            );
        }

        ReskinArm(data, ".L");
        ReskinArm(data, ".R");
        ReskinLeg(data, ".L");
        ReskinLeg(data, ".R");
        AddTailBones(data);

        skinMesh = Instantiate(source);
        skinMesh.name = source.name + "_Reskinned";
        skinMesh.boneWeights = data.weights;
        skinMesh.bindposes = data.bindPoses.ToArray();
        skin.bones = data.bones.ToArray();
        skin.sharedMesh = skinMesh;
    }

    // the asset skins the elbow & wrist rings to a mix of bones that pull
    // apart when the arm stretches, pinching it thin. Each ring is made one
    // piece: the elbow split between upper arm & forearm, the wrist and all
    // beyond it on the hand & fingers alone
    private void ReskinArm(SkinData data, string suffix) {
        Transform upper = Bone("ORG-upper_arm" + suffix);
        Transform fore = Bone("ORG-forearm" + suffix);
        Transform hand = Bone("ORG-hand" + suffix);
        if (upper == null || fore == null || hand == null) {
            return;
        }
        int upperIndex = data.bones.IndexOf(upper);
        int foreIndex = data.bones.IndexOf(fore);
        int handIndex = data.bones.IndexOf(hand);
        if (upperIndex < 0 || foreIndex < 0 || handIndex < 0) {
            return;
        }
        HashSet<int> handSide = new HashSet<int>();
        foreach (
            Transform part in hand.GetComponentsInChildren<Transform>(true)
        ) {
            int index = data.bones.IndexOf(part);
            if (index >= 0) {
                handSide.Add(index);
            }
        }

        float side =
            modelRoot.InverseTransformPoint(upper.position).x < 0f ? -1f : 1f;
        float elbowU = modelRoot.InverseTransformPoint(fore.position).x * side;
        float wristU = modelRoot.InverseTransformPoint(hand.position).x * side;
        float bandU = (wristU - elbowU) * ARM_RING_BAND;

        for (int i = 0; i < data.weights.Length; i++) {
            BoneWeight weight = data.weights[i];
            bool isArm =
                IsInfluence(weight, upperIndex)
                || IsInfluence(weight, foreIndex)
                || HasAny(weight, handSide);
            if (!isArm) {
                continue;
            }
            float alongU = data.restPoints[i].x * side;
            if (Mathf.Abs(alongU - elbowU) < bandU) {
                data.weights[i] = MakeWeight(
                    new List<int> { foreIndex, upperIndex },
                    new List<float> {
                        elbowForearmShare, 1f - elbowForearmShare
                    }
                );
            } else if (alongU > wristU - bandU) {
                data.weights[i] = MoveInfluence(
                    weight,
                    new[] { upperIndex, foreIndex },
                    handIndex
                );
            }
        }
    }

    // the asset skins each leg ring unevenly: the thigh's top ring rides the
    // thigh alone, so a raised thigh folds the butt into the body, and the
    // knee & ankle rings mix their bones vertex by vertex, pinching as they
    // bend. Each ring is made one even piece: the hip ring cushioned by the
    // hips, the knee split thigh & shin, the ankle mostly the foot's
    private void ReskinLeg(SkinData data, string suffix) {
        Transform thigh = Bone("ORG-thigh" + suffix);
        Transform shin = Bone("ORG-shin" + suffix);
        Transform foot = Bone("ORG-foot" + suffix);
        if (thigh == null || shin == null || foot == null) {
            return;
        }
        int hipsIndex = data.bones.IndexOf(hips);
        int thighIndex = data.bones.IndexOf(thigh);
        int shinIndex = data.bones.IndexOf(shin);
        int footIndex = data.bones.IndexOf(foot);
        if (hipsIndex < 0 || thighIndex < 0 || shinIndex < 0 || footIndex < 0) {
            return;
        }
        // the shin's other children, the heel bones, turn with the foot
        HashSet<int> lowerLeg = new HashSet<int>();
        foreach (
            Transform part in shin.GetComponentsInChildren<Transform>(true)
        ) {
            int index = data.bones.IndexOf(part);
            if (index >= 0) {
                lowerLeg.Add(index);
            }
        }
        HashSet<int> wholeLeg = new HashSet<int>(lowerLeg) { thighIndex };

        float side =
            modelRoot.InverseTransformPoint(thigh.position).x < 0f ? -1f : 1f;
        float hipY = modelRoot.InverseTransformPoint(thigh.position).y;
        float kneeY = modelRoot.InverseTransformPoint(shin.position).y;
        float ankleY = modelRoot.InverseTransformPoint(foot.position).y;

        for (int i = 0; i < data.weights.Length; i++) {
            Vector3 point = data.restPoints[i];
            if (point.x * side <= 0f) {
                continue;
            }
            BoneWeight weight = data.weights[i];
            float thighShare = ShareOf(weight, thighIndex);
            float legShare = ShareOf(weight, wholeLeg);
            float lowerShare = ShareOf(weight, lowerLeg);

            if (
                Mathf.Abs(point.y - hipY) < HIP_RING_BAND_U
                && thighShare >= 0.5f
            ) {
                data.weights[i] = MakeWeight(
                    new List<int> { thighIndex, hipsIndex },
                    new List<float> { 1f - hipRingHipsShare, hipRingHipsShare }
                );
            } else if (Mathf.Abs(point.y - kneeY) < KNEE_RING_BAND_U
                && legShare >= 0.5f) {
                data.weights[i] = MakeWeight(
                    new List<int> { thighIndex, shinIndex },
                    new List<float> { 0.5f, 0.5f }
                );
            } else if (point.y >= ankleY && point.y < ankleY + ANKLE_RING_BAND_U
                && lowerShare >= 0.5f) {
                data.weights[i] = MakeWeight(
                    new List<int> { footIndex, shinIndex },
                    new List<float> { ANKLE_FOOT_SHARE, 1f - ANKLE_FOOT_SHARE }
                );
            }
        }
    }

    private static float ShareOf(BoneWeight weight, int bone) {
        return ShareOf(weight, new HashSet<int> { bone });
    }

    private static float ShareOf(BoneWeight weight, HashSet<int> bones) {
        float share = 0f;
        if (bones.Contains(weight.boneIndex0)) share += weight.weight0;
        if (bones.Contains(weight.boneIndex1)) share += weight.weight1;
        if (bones.Contains(weight.boneIndex2)) share += weight.weight2;
        if (bones.Contains(weight.boneIndex3)) share += weight.weight3;
        return share;
    }

    // gives the hips-skinned tail its own bone chain
    private void AddTailBones(SkinData data) {
        int hipsIndex = data.bones.IndexOf(hips);
        if (hipsIndex < 0) {
            return;
        }

        // tail vertices & how far behind the hips each sits, model frame
        Matrix4x4 hipsSkin =
            hips.localToWorldMatrix * data.bindPoses[hipsIndex];
        Vector3 hipsInModel = modelRoot.InverseTransformPoint(hips.position);
        Vector3 back = BackInModel();
        List<int> tail = new List<int>();
        List<Vector3> tailPoints = new List<Vector3>();
        float minDepthU = float.MaxValue;
        float maxDepthU = float.MinValue;
        for (int i = 0; i < data.vertices.Length; i++) {
            BoneWeight weight = data.weights[i];
            if (weight.boneIndex0 != hipsIndex
                || weight.weight0 < TAIL_MIN_HIPS_WEIGHT) {
                continue;
            }
            Vector3 point = data.restPoints[i];
            float depthU = Vector3.Dot(point - hipsInModel, back);
            if (depthU < TAIL_MIN_DEPTH_U) {
                continue;
            }
            tail.Add(i);
            tailPoints.Add(point);
            minDepthU = Mathf.Min(minDepthU, depthU);
            maxDepthU = Mathf.Max(maxDepthU, depthU);
        }
        if (
            tail.Count < tailBoneCount
            || maxDepthU - minDepthU < MIN_LENGTH_U
        ) {
            Debug.LogWarning(
                "MonkeyModelAnimator:\tfound no tail on the model, tail stays "
                    + "still",
                this
            );
            return;
        }

        // each vertex's place along the tail, 0 at the base, 1 at the tip
        float[] along = new float[tail.Count];
        for (int i = 0; i < tail.Count; i++) {
            along[i] = Mathf.InverseLerp(
                minDepthU,
                maxDepthU,
                Vector3.Dot(tailPoints[i] - hipsInModel, back)
            );
        }

        // the first bone at the tail's base, the rest each at the middle of
        // their stretch of the tail
        Vector3[] centres = new Vector3[tailBoneCount];
        centres[0] = tailPoints[IndexOfMin(along)];
        for (int b = 1; b < tailBoneCount; b++) {
            float start = (float)b / tailBoneCount;
            float end = (float)(b + 1) / tailBoneCount;
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < tail.Count; i++) {
                bool isLast = b == tailBoneCount - 1;
                if (along[i] >= start && (along[i] < end || isLast)) {
                    sum += tailPoints[i];
                    count++;
                }
            }
            centres[b] = count > 0
                ? sum / count
                : hipsInModel + back * Mathf.Lerp(minDepthU, maxDepthU, start);
        }

        // chained, each placed before its child exists so none drags another;
        // each skins like the hips did, so the rest pose is unchanged
        int firstTailIndex = data.bones.Count;
        Transform parent = hips;
        for (int b = 0; b < tailBoneCount; b++) {
            Transform bone = new GameObject("TailBone_" + b).transform;
            bone.SetParent(parent, false);
            bone.position = modelRoot.TransformPoint(centres[b]);
            bone.rotation = modelRoot.rotation;
            bone.localScale = Vector3.one;
            tailBones.Add(bone);
            Remember(bone);
            data.bones.Add(bone);
            data.bindPoses.Add(bone.worldToLocalMatrix * hipsSkin);
            parent = bone;
        }

        for (int i = 0; i < tail.Count; i++) {
            float place = along[i] * (tailBoneCount - 1);
            int lower = Mathf.Min(Mathf.FloorToInt(place), tailBoneCount - 1);
            int upper = Mathf.Min(lower + 1, tailBoneCount - 1);
            float upperShare = upper == lower ? 0f : place - lower;
            float hipsShare = 1f - Mathf.Clamp01(along[i] / TAIL_BASE_BLEND);
            float tailShare = 1f - hipsShare;
            data.weights[tail[i]] = MakeWeight(
                new List<int> {
                    hipsIndex, firstTailIndex + lower, firstTailIndex + upper,
                },
                new List<float> {
                    hipsShare,
                    tailShare * (1f - upperShare),
                    tailShare * upperShare,
                }
            );
        }
    }

    // away from where the toes point, level, model frame
    private Vector3 BackInModel() {
        Transform toe = Bone("ORG-toe.L");
        if (toe == null) {
            return Vector3.back;
        }
        Vector3 forward = Vector3.ProjectOnPlane(
            modelRoot.InverseTransformDirection(
                toe.position - leftLeg.foot.position
            ),
            Vector3.up
        );
        return forward.sqrMagnitude > MIN_LENGTH_U
            ? -forward.normalized
            : Vector3.back;
    }

    private static int IndexOfMin(float[] values) {
        int best = 0;
        for (int i = 1; i < values.Length; i++) {
            if (values[i] < values[best]) {
                best = i;
            }
        }
        return best;
    }

    // Skin Weights  -----------------------------------------------------------
    private static bool IsInfluence(BoneWeight weight, int bone) {
        return (weight.boneIndex0 == bone && weight.weight0 > 0f)
            || (weight.boneIndex1 == bone && weight.weight1 > 0f)
            || (weight.boneIndex2 == bone && weight.weight2 > 0f)
            || (weight.boneIndex3 == bone && weight.weight3 > 0f);
    }

    private static bool HasAny(BoneWeight weight, HashSet<int> bones) {
        return (bones.Contains(weight.boneIndex0) && weight.weight0 > 0f)
            || (bones.Contains(weight.boneIndex1) && weight.weight1 > 0f)
            || (bones.Contains(weight.boneIndex2) && weight.weight2 > 0f)
            || (bones.Contains(weight.boneIndex3) && weight.weight3 > 0f);
    }

    // hands the share of each of the given bones over to another bone
    private static BoneWeight MoveInfluence(
        BoneWeight weight,
        int[] from,
        int to
    ) {
        List<int> bones = new List<int> {
            weight.boneIndex0, weight.boneIndex1,
            weight.boneIndex2, weight.boneIndex3,
        };
        List<float> shares = new List<float> {
            weight.weight0, weight.weight1, weight.weight2, weight.weight3,
        };
        for (int i = 0; i < bones.Count; i++) {
            if (System.Array.IndexOf(from, bones[i]) >= 0) {
                bones[i] = to;
            }
        }
        return MakeWeight(bones, shares);
    }

    // merges repeated bones, keeps the 4 heaviest, heaviest first, sums to 1
    private static BoneWeight MakeWeight(List<int> bones, List<float> shares) {
        Dictionary<int, float> merged = new Dictionary<int, float>();
        for (int i = 0; i < bones.Count; i++) {
            if (shares[i] <= 0f) {
                continue;
            }
            merged.TryGetValue(bones[i], out float total);
            merged[bones[i]] = total + shares[i];
        }
        List<KeyValuePair<int, float>> sorted =
            new List<KeyValuePair<int, float>>(merged);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value));

        int[] index = new int[4];
        float[] share = new float[4];
        float sum = 0f;
        for (int i = 0; i < 4 && i < sorted.Count; i++) {
            index[i] = sorted[i].Key;
            share[i] = sorted[i].Value;
            sum += share[i];
        }
        sum = Mathf.Max(sum, MIN_LENGTH_U);
        return new BoneWeight {
            boneIndex0 = index[0], weight0 = share[0] / sum,
            boneIndex1 = index[1], weight1 = share[1] / sum,
            boneIndex2 = index[2], weight2 = share[2] / sum,
            boneIndex3 = index[3], weight3 = share[3] / sum,
        };
    }

    // State  ------------------------------------------------------------------
    private void ResetToBind() {
        for (int i = 0; i < restBones.Count; i++) {
            restBones[i].localPosition = restPositions[i];
            restBones[i].localRotation = restRotations[i];
        }
    }

    private void UpdateMotion(float deltaS) {
        Vector3 position = transform.position;
        if (deltaS > 0f) {
            Vector3 velocity = modelRoot.InverseTransformDirection(
                (position - lastPosition) / deltaS
            );
            modelVelocity = Vector3.Lerp(
                modelVelocity,
                velocity,
                1f - Mathf.Exp(-8f * deltaS)
            );
        }
        lastPosition = position;
    }

    // one stride of travel, up, down or round the trunk, is one cycle
    private void UpdateCycle(float deltaS) {
        Vector2 travel = new Vector2(modelVelocity.x, modelVelocity.y);
        float speedU = travel.magnitude;
        if (speedU < minMoveSpeedU) {
            return;
        }
        float direction = modelVelocity.y < -minMoveSpeedU ? -1f : 1f;
        float rateHz = Mathf.Min(speedU / strideU, maxCycleHz);
        climbPhase = Mathf.Repeat(
            climbPhase + direction * rateHz * deltaS,
            1f
        );
    }

    private static bool IsReaching(ArmController arm) {
        return arm != null && arm.IsReaching;
    }

    private void UpdateLook(float deltaS) {
        bool canLook = CanLookBack();
        bool isUrgent = IsCobraClose();

        // the cobra closing in pulls the next look forward
        if (!isLooking && isUrgent
            && nextLookTime > Time.time + urgentLookIntervalS.y) {
            ScheduleNextLook(true);
        }

        if (!isLooking && canLook && Time.time >= nextLookTime) {
            isLooking = true;
            lookEndTime = Time.time + lookTurnS + lookHoldS;
            // a fresh look picks a shoulder, one cut short keeps its side
            if (lookWeight <= 0f) {
                lookSide = Random.value < 0.5f ? -1f : 1f;
            }
        }
        if (isLooking && (!canLook || Time.time >= lookEndTime)) {
            isLooking = false;
            ScheduleNextLook(isUrgent);
        }

        lookWeight = Mathf.MoveTowards(
            lookWeight,
            isLooking ? 1f : 0f,
            deltaS / lookTurnS
        );
    }

    private void ScheduleNextLook(bool isUrgent) {
        Vector2 range = isUrgent ? urgentLookIntervalS : calmLookIntervalS;
        nextLookTime = Time.time + Random.Range(range.x, range.y);
    }

    // only while the run is live and the player needn't watch the hands
    private bool CanLookBack() {
        GameController game = GameController.I;
        if (game == null || game.IsLeaping
            || (game.State != GameState.Playing
                && game.State != GameState.StartingZone)) {
            return false;
        }
        if (web != null && web.IsTrapped) {
            return false;
        }
        CobraKingFightController boss = CobraKingFightController.I;
        if (boss != null && boss.IsInArena) {
            return false;
        }
        return !IsReaching(leftArm) && !IsReaching(rightArm);
    }

    // the same gap the screen's cobra warning measures
    private bool IsCobraClose() {
        CoconutManager coconuts = CoconutManager.I;
        CobraMotor cobra = coconuts != null ? coconuts.Cobra : null;
        if (cobra == null || cobra.IsFrozen || cobra.IsGuarding) {
            return false;
        }
        if (cobra.IsCatchingUp) {
            return true;
        }
        return cobra.TryGetBodyBounds(out Bounds body)
            && transform.position.y - body.max.y < cobraNearU;
    }

    // Pose  -------------------------------------------------------------------
    // shift: left foot lift less right foot lift, weight goes onto the lower
    private void PoseBody(float shift) {
        hips.position += modelRoot.TransformVector(
            new Vector3(shift * hipSwayU, 0f, 0f)
        );
        spine.rotation =
            Quaternion.AngleAxis(-shift * spineRollDeg, modelRoot.forward)
            * spine.rotation;

        float breath =
            Mathf.Sin(Time.time * breathHz * Mathf.PI * 2f) * breathDeg;
        chest.rotation =
            Quaternion.AngleAxis(breath, modelRoot.right) * chest.rotation;
    }

    // over the shoulder: spine to head each add a share of the turn
    private void PoseLook() {
        if (lookWeight <= 0f) {
            return;
        }
        float weight = Mathf.SmoothStep(0f, 1f, lookWeight);
        float yawDeg = lookSide * lookYawDeg * weight;
        Vector3 up = modelRoot.up;
        Transform[] chain = { spine, chest, neck, head };
        for (int i = 0; i < chain.Length; i++) {
            chain[i].rotation =
                Quaternion.AngleAxis(yawDeg * LOOK_YAW_SHARES[i], up)
                * chain[i].rotation;
        }

        // tip the turned face down the trunk, neck and head sharing it
        Vector3 turnedRight =
            Quaternion.AngleAxis(yawDeg, up) * modelRoot.right;
        float pitchDeg = lookPitchDeg * weight;
        neck.rotation =
            Quaternion.AngleAxis(pitchDeg * 0.4f, turnedRight) * neck.rotation;
        head.rotation =
            Quaternion.AngleAxis(pitchDeg * 0.6f, turnedRight) * head.rotation;
    }

    private void PoseLeg(Leg leg, float lift, float clearance) {
        float strafe = Mathf.Clamp(
            -modelVelocity.x * footStrafeU,
            -footStrafeU,
            footStrafeU
        );
        Vector3 footInModel = new Vector3(
            leg.side * footSideU + strafe,
            Mathf.Lerp(footHeightU.x, footHeightU.y, lift),
            Mathf.Lerp(footDepthU.x, footDepthU.y, lift)
                - clearance * footClearanceU
        );
        Vector3 pole = modelRoot.TransformDirection(
            new Vector3(kneePole.x * leg.side, kneePole.y, kneePole.z)
        );
        ModelTwoBoneIK.Solve(
            leg.thigh,
            leg.shin,
            leg.foot,
            modelRoot.TransformPoint(footInModel),
            pole,
            leg.kneeFrontInThigh
        );

        // sole flat on the bark, toes up, eased while the foot swings; the
        // heel bone hangs off the shin, so it turns by the same amount
        Quaternion before = leg.foot.rotation;
        float pitchDeg = footPitchDeg * (1f - 0.4f * clearance);
        leg.foot.rotation =
            modelRoot.rotation
            * Quaternion.AngleAxis(-pitchDeg, Vector3.right)
            * leg.footInModel;
        if (leg.heel != null) {
            leg.heel.rotation =
                leg.foot.rotation * Quaternion.Inverse(before)
                * leg.heel.rotation;
        }
    }

    private void PoseTail(float deltaS) {
        if (tailBones.Count == 0) {
            return;
        }

        // the tail trails the body: rising drops it, strafing swings it back
        Vector2 goal = new Vector2(
            Mathf.Clamp(
                modelVelocity.x * tailLagDegPerU, -tailMaxDeg, tailMaxDeg),
            Mathf.Clamp(
                -modelVelocity.y * tailLagDegPerU, -tailMaxDeg, tailMaxDeg)
        );
        float stepS = Mathf.Min(deltaS, MAX_DELTA_S);
        tailSpeed += (
            (goal - tailAngle) * tailStiffness - tailSpeed * tailDamping
        ) * stepS;
        tailAngle += tailSpeed * stepS;

        Vector3 up = modelRoot.up;
        Vector3 right = modelRoot.right;
        float share = 1f / tailBones.Count;
        float time = Time.time * tailIdleHz * Mathf.PI * 2f;
        for (int i = 0; i < tailBones.Count; i++) {
            // the idle wave runs down the tail, each bone a little later
            float idleYaw = Mathf.Sin(time - i * 0.7f) * tailIdleDeg;
            float idlePitch =
                Mathf.Sin(time * 0.6f - i * 0.5f) * tailIdleDeg * 0.5f;
            Transform bone = tailBones[i];
            bone.rotation =
                Quaternion.AngleAxis(tailAngle.x * share + idleYaw, up)
                * Quaternion.AngleAxis(tailAngle.y * share + idlePitch, right)
                * bone.rotation;
        }
    }

    // Nested Types  ###########################################################
    private class Leg {
        public Transform thigh;
        public Transform shin;
        public Transform foot;
        public Transform heel;

        // -1 on the model's left, +1 on its right
        public float side;

        // the foot's rest rotation in the model's frame
        public Quaternion footInModel;

        // the way the kneecap faces, in the thigh's own frame
        public Vector3 kneeFrontInThigh;
    }

    // the mesh's skinning while it is rebuilt
    private class SkinData {
        public List<Transform> bones;
        public List<Matrix4x4> bindPoses;
        public BoneWeight[] weights;
        public Vector3[] vertices;
        public Vector3[] restPoints;
    }
}
