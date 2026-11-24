using System.Collections.Generic;
using UnityEngine;


public enum FallingCobraState {
    Dormant,
    Warning,
    Falling,
    Done,
}

    public struct SwimTrailPoint {
        public Vector3 position;
        public float distance;
        public SwimTrailPoint(Vector3 position, float distance) {
            this.position = position;
            this.distance = distance;
        }
    }

/// <summary>
/// The state every falling cobra role shares, plus the geometry
/// queries more than one role asks of it.
/// </summary>
public sealed class FallingCobraRig {
    // Constants  #############################################################
    public const float NeckOverlapU = .006f;
    public const float HeadCollarBlendU = .4f;
    // the body tube's rings along it & sides round it: the mesh is made
    // again on the CPU each frame a cobra moves, so kept low (was 257 x 32,
    // about 4.6x the vertices)
    public const int BodyRings = 96;
    public const int BodySides = 16;
    public const int BodyStride = BodySides + 1;
    public const string PLAYER_TAG = "Player";
    public const float MIN_RING_RADIUS_U = 0.001f;

    // Shared State  ##########################################################
    public readonly Transform host;
    public readonly FallingCobraTuning tuning;
    public FallingCobraHeadMesh HeadMesh;
    public FallingCobraBranchCoil BranchCoil;
    public FallingCobraTracking Tracking;
    public FallingCobraEyes Eyes;
    public FallingCobraSwimTrail Trail;
    public FallingCobraSpine Spine;
    public BoxCollider supportBranch;
    public bool perchBound;
    public Vector3 branchCenter;
    public Vector3 branchAxis;
    public Vector3 branchUp;
    public Vector3 branchSide;
    public float branchHalfLength;
    public float branchHalfHeight;
    public float branchHalfWidth;
    public float branchAnchorAlong;
    public Bounds originalHeadBounds;
    public float branchEscapeSide = 1f;
    public float branchHeadSideSign = 1f;
    public float perchedAimSide = 1f;
    public float perchedCoilPhaseDegrees;
    public Vector3[] roundedRingCenters;
    public Vector3[] originalRingCenters;
    public Vector2[] neckTextureUV;
    public Vector3[] neckSourceNormals;
    public float headNeckRadiusU;
    public Quaternion originalHeadFaceBasis;
    public Quaternion standingHeadRotation;
    public Vector3 originalHeadFaceCenter;
    public Vector3 releaseTrailOffset;
    public float releaseStandingBlend;
    public float[] spineContact;
    public AudioSource hissSource;
    public float nextHissTime;
    public float attackDistanceU;
    public float waitingHeadBlend;
    public float lastHeadPoseTime;
    public float attackStartedTime;
    public float strikeWindupBlend;
    public Vector3 strikeWindupOffset;
    public Vector3 launchWindupOffset;
    public Vector3 displayedTrailHead;
    public Vector3 displayedTrailVelocity;
    public float releaseHeadHeight;
    public float launchTrailDistance;
    public float launchNeckArc;
    public float committedTargetAngle;
    public Vector3 committedTargetPosition;
    public Quaternion shownHeadRotation;
    public bool headRotationReady;
    public float approachFallScale = 1f;
    public float headAimCommand;
    public float previousFeedbackAngle;
    public float headAngularSpeed;
    public bool headFeedbackReady;
    public bool descentStopped;
    public float stoppedTrailHeight;
    public bool pursuitPaused;
    public float[] launchBodyOffsets;
    public float launchBodyArc;
    public bool monkeyInHeadRange;
    public Transform cachedAimOwner;
    public Collider playerBodyCollider;
    public float headSteeringBias;
    public Vector3 supportDirection;
    public Vector3 previousHeadContact;
    public float supportMinimum;
    public bool supportCached;
    public bool headContactReady;
    public CapsuleCollider[] visibleBodyContacts;
    public readonly Collider[] sweptHeadHits = new Collider[32];
    public Transform[] eyePupils;
    public Material eyeWhiteMaterial;
    public Material eyePupilMaterial;
    public Mesh eyePupilMesh;
    public bool hasCommittedTarget;
    public Renderer referenceRenderer;
    public MaterialPropertyBlock surfaceBlock;
    public readonly List<SwimTrailPoint> swimTrail =
        new List<SwimTrailPoint>(256);
    public Vector3 previousTrailRoot;
    public Vector3 trailHeading;
    public float previousTrailTime;
    public float swimPhase;
    public float swimPhaseOffset;
    public float swimTravelDistance;
    public bool swimTrailReady;
    public bool spinePoseReady;
    public bool swimRotationInitialized;
    public Mesh animatedReferenceMesh;
    public Transform referenceVisual;
    public Vector3[] referenceWaveVertices;
    public Vector3[] referenceWaveNormals;
    public Vector3[] neckSurfaceBuffer;
    public Vector3[] neckSurfaceOriginal;
    public Vector3[] paintedBellyDirections;
    public Vector3[] originalHeadVertices;
    public Transform referenceHeadVisual;
    public Renderer referenceHeadRenderer;
    public int headVertexCount;
    public Vector3[] neckRim;
    public Vector3[] spineCenters;
    public Vector3[] spineTangents;
    public float[] spineRadii;
    public float originalHeadFront;
    public float referenceHeadBelowPivotU;
    public CartoonCobraVisual cartoonVisual;
    public Vector3[] fairSpine;
    public Vector3[] relaxedNeck;
    public FallingCobraState state = FallingCobraState.Dormant;
    public bool isInitialized;  // if the spawner armed this cobra
    public float fallSpeedU;  // from config; u/s
    public float horizontalSpeedU;  // from config; u/s along the ring
    public float reachWakeDistanceU;  // from config; wake range mid-grab; u
    public Vector2 axisXZ;  // tree axis, world xz
    public float ringRadiusU;  // horizontal distance to the axis; u
    public float warningStartTime;
    public float fallStartTime;
    public float fallenU;
    public Rigidbody body;
    public CapsuleCollider hitCollider;
    public Transform player;
    public ArmController[] playerArms;  // the monkey's arms, read for a reach
    public HitBranchSensor playerSensor;  // applies the drop

    public FallingCobraRig(Transform host, FallingCobraTuning tuning) {
        this.host = host;
        this.tuning = tuning;
    }

    // Public Methods  #########################################################
    public Vector3 GetTreeSurfacePoint(Vector3 position) {
        Vector3 outward =
            new Vector3(position.x - axisXZ.x, 0f, position.z - axisXZ.y);
        if (outward.sqrMagnitude<0.000001f) {
            outward = -host.forward;
        }
        outward.Normalize();
        float radius = tuning.TreeSurfaceRadiusU + tuning.TreeSurfaceClearanceU;
        return new Vector3(axisXZ.x + outward.x * radius, position.y,
            axisXZ.y + outward.z * radius);
    }

    public Vector3 OutsideTree(Vector3 point) {
        Vector3 outward =
            new Vector3(point.x - axisXZ.x, 0f, point.z - axisXZ.y);
        float radius = outward.magnitude,
            minimum = tuning.TreeSurfaceRadiusU + tuning.TreeSurfaceClearanceU;
        if (radius < minimum) {
            outward = radius > .00001f ? outward / radius : -host.forward;
            point.x = axisXZ.x + outward.x * minimum;
            point.z = axisXZ.y + outward.z * minimum;
        }
        return point;
    }

    public float GroundSpineOffset(int ring) {
        float behind = ring * tuning.SmoothBodyLengthU / (BodyRings - 1);
        return Mathf.Lerp(Mathf.Max(tuning.SmoothBodyRadiusU * .9f,
            originalHeadFront),
            spineRadii[ring] * .9f - tuning.BellyPressDepthU,
                Mathf.SmoothStep(0f, 1f,
                    behind / tuning.HeadContactBlendLengthU));
    }

    public float BodyTrailDistance(int ring) {
        float nominal = ring * tuning.SmoothBodyLengthU / (BodyRings - 1);
        if (launchBodyOffsets == null || !swimTrailReady) {
            return nominal;
        }
        float travel =
            swimTrail[swimTrail.Count - 1].distance - launchTrailDistance;
        float released =
            Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(launchBodyArc,
                    launchBodyArc + tuning.NeckFollowLengthU, travel));
        return Mathf.Lerp(launchBodyOffsets[ring], nominal, released);
    }

    public float RemainingCoilWeight(int ring) {
        if (state != FallingCobraState.Falling || supportBranch == null
            || !swimTrailReady) {
            return 0f;
        }
        float distance =
            swimTrail[swimTrail.Count - 1].distance - BodyTrailDistance(ring);
        return 1f
            - Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(launchTrailDistance - launchNeckArc
                    - tuning.UncoilSupportBlendU,
            launchTrailDistance - launchNeckArc + tuning.UncoilSupportBlendU,
                distance));
    }

    public float HeadDeparture(Vector3 position) {
        return Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(.2f, tuning.HeadTurnDropU,
                releaseHeadHeight - position.y));
    }

    public Vector3 SourceEyeForward()=>tuning.EyeAnchorsReady
        ?(tuning.LeftEyeNormalLocal + tuning.RightEyeNormalLocal).normalized
            :originalHeadFaceBasis * Vector3.forward;

    public Vector3 SourceEyeUp() {
        if (!tuning.EyeAnchorsReady) {
            return originalHeadFaceBasis * Vector3.up;
        }
        Vector3 up =
            Vector3.Cross(SourceEyeForward(),
                tuning.RightEyeSurfaceLocal
                    - tuning.LeftEyeSurfaceLocal).normalized;
        return Vector3.Dot(up, Vector3.up)<0f?-up:up;
    }

    public Quaternion SourceEyeBasis()=>
        Quaternion.LookRotation(SourceEyeForward(), SourceEyeUp());


    // Notice the monkey from above or below; a falling attack still never
    // climbs.
    public Vector3 ThreatPosition => spinePoseReady
        && referenceHeadVisual != null
        ? referenceHeadVisual.TransformPoint(tuning.EyeAnchorsReady
            ?(tuning.LeftEyeSurfaceLocal + tuning.RightEyeSurfaceLocal) * .5f
                - SourceEyeUp() * .06f:originalHeadFaceCenter) : host.position;

}
