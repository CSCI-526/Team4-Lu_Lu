using UnityEngine;
using static FallingCobraRig;
using static FallingCobraMath;

/// <summary>
/// A cobra hanging dormant on the tree that drops on the monkey. Spawned as a
/// child of a tree segment by <see cref="FallingCobraSpawner"/>, which hands
/// it the balance config and the tree axis through
/// <see cref="Initialize"/>.
/// <para>
/// Three states. Dormant: nothing moves until the monkey comes within
/// <c>triggerDistanceU</c> (3D), or within the farther
/// <see cref="CobraBalanceConfig.FallingCobraReachWakeDistanceU"/> while an
/// arm reaches for a banana. It can notice and watch a monkey passing above it.
/// Warning: it raises its head, watches the monkey and hisses in its normal
/// green color.
/// After a fixed head-up warning it attacks, even if the monkey stays distant
/// or retreats. Falling: a
/// short downward pursuit that follows the monkey within its tracking range.
/// The head rests against the bark and physical contacts follow the visible
/// snake. Any part of
/// the snake touching any part of the monkey (body, tail, hand, or its
/// branch sensor) applies one branch-hit drop and immediately switches the
/// snake off. It cannot keep falling or strike the monkey again. After
/// <c>maximumFallDurationS</c> or <c>maxFallDistanceU</c> it switches itself
/// off; the segment's restart
/// destroys it later.
/// </para>
/// </summary>
// TODO move the swim trail and the spine pipeline into role classes,
// Movement 11.2.2 of the structure Suite
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FallingCobraController: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>pivot to the far end of the snake, hit radius included; u
    /// </returns>
    public float LengthU => rig.animatedReferenceMesh != null
        ?tuning.SmoothBodyLengthU:rig.hitCollider.height;

    /// <returns>how far the snake's tail reaches below its pivot; u</returns>
    public float TailBelowPivotU => rig.animatedReferenceMesh != null
        ? rig.referenceHeadBelowPivotU : tuning.HitRadiusU;

    // Public Methods  #########################################################
    /// <summary>
    /// Arms the cobra. The ring it slides on is the horizontal distance from
    /// its current position to <paramref name="treeAxisPosition"/>, so call it
    /// once the cobra stands where it will hang.
    /// </summary>
    /// <param name="config">balance tuning; fall and slide speeds</param>
    /// <param name="treeAxisPosition">any world point on the trunk axis
    /// </param>
    public void Initialize(
        CobraBalanceConfig config,
        Vector3 treeAxisPosition
    ) {
        if (config == null) {
            Debug.LogWarning(
                "FallingCobraController:\tInitialize needs a "
                    + "CobraBalanceConfig, stay "
                    + "dormant",
                this
            );
            return;
        }

        rig.fallSpeedU = config.FallingCobraFallSpeedU;
        rig.horizontalSpeedU = config.FallingCobraHorizontalSpeedU;
        rig.reachWakeDistanceU = config.FallingCobraReachWakeDistanceU;
        rig.attackDistanceU = config.FallingCobraAttackDistanceU;
        rig.axisXZ = new Vector2(treeAxisPosition.x, treeAxisPosition.z);
        Vector3 position = transform.position;
        rig.ringRadiusU = Vector2.Distance(
            new Vector2(position.x, position.z),
            rig.axisXZ
        );
        rig.isInitialized = true;
        AlignReferenceToTree();
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    private FallingCobraTuning tuning = new();

    // Private Members  ########################################################
    private FallingCobraRig rig;
    private FallingCobraHeadMesh headMesh;
    private FallingCobraBranchCoil branchCoil;
    private FallingCobraTracking tracking;
    private FallingCobraEyes eyes;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        rig = new FallingCobraRig(transform, tuning);
        headMesh = new FallingCobraHeadMesh(rig);
        branchCoil = new FallingCobraBranchCoil(rig);
        tracking = new FallingCobraTracking(rig);
        eyes = new FallingCobraEyes(rig, TryHit);
        rig.HeadMesh = headMesh;
        rig.BranchCoil = branchCoil;
        rig.Tracking = tracking;
        rig.Eyes = eyes;
        rig.body = GetComponent<Rigidbody>();
        rig.hitCollider = GetComponent<CapsuleCollider>();
        if (rig.body == null || rig.hitCollider == null) {
            Debug.LogError(
                "FallingCobraController:\trequires Rigidbody and "
                    + "CapsuleCollider",
                this
            );
            enabled = false;
            return;
        }

        rig.body.isKinematic = true;
        rig.body.useGravity = false;

        float highestSegmentY = 0f;
        for (int i = 0; i < transform.childCount; i++) {
            Transform segment = transform.GetChild(i);
            if (segment.name.StartsWith("Segment_")) {
                segment.localPosition =
                    Vector3.up * (i * tuning.SegmentSpacingU);
                highestSegmentY = Mathf.Max(
                    highestSegmentY,
                    segment.localPosition.y
                );
            }
        }

        // One trigger covers the whole snake, so one collision creates one
        // branch-hit event and camera impulse instead of one per segment.
        foreach (
            Collider segmentCollider in GetComponentsInChildren<Collider>()
        ) {
            if (segmentCollider != rig.hitCollider) {
                segmentCollider.enabled = false;
            }
        }
        rig.hitCollider.direction = 1;
        rig.hitCollider.radius = tuning.HitRadiusU;
        rig.hitCollider.height = highestSegmentY + tuning.HitRadiusU * 2f;
        rig.hitCollider.center = Vector3.up * highestSegmentY * 0.5f;
        rig.hitCollider.isTrigger = true;
        rig.hitCollider.enabled = false;

        headMesh.BuildCartoonVisual();
        eyes.CreateVisibleContacts();
        eyes.CreateTrackingEyes();

        if (tuning.WarningHiss != null) {
            GameObject voice = new GameObject("SnakeHiss");
            voice.transform.SetParent(rig.referenceHeadVisual != null
                ? rig.referenceHeadVisual : transform, false);
            voice.transform.localPosition = rig.originalHeadFaceCenter;
            rig.hissSource = voice.AddComponent<AudioSource>();
            rig.hissSource.clip = tuning.WarningHiss;
            rig.hissSource.playOnAwake = false;
            rig.hissSource.loop = false;
            rig.hissSource.volume = tuning.WarningHissVolume;
            rig.hissSource.spatialBlend = 1f;
            rig.hissSource.dopplerLevel = 0f;
            rig.hissSource.minDistance = tuning.WarningHissMinDistanceU;
            rig.hissSource.maxDistance =
                Mathf.Max(tuning.TriggerDistanceU + tuning.AlertExitPaddingU,
                    tuning.WarningHissMinDistanceU);
        }
        rig.lastHeadPoseTime = Time.time;
    }

    private void OnDisable() {
        if (rig.hissSource != null) {
            rig.hissSource.Stop();
        }
    }

    private void OnDestroy() {
        rig.cartoonVisual?.Dispose();
        if (rig.eyeWhiteMaterial != null) {
            Destroy(rig.eyeWhiteMaterial);
        }
        if (rig.eyePupilMaterial != null) {
            Destroy(rig.eyePupilMaterial);
        }
        if (rig.eyePupilMesh != null) {
            Destroy(rig.eyePupilMesh);
        }
        if (rig.animatedReferenceMesh != null) {
            Destroy(rig.animatedReferenceMesh);
        }
    }

    private void AppendSwimTrail(Vector3 position) {
        if (rig.swimTrail.Count == 0) {
            rig.swimTrail.Add(new SwimTrailPoint(position, 0f));
            return;
        }
        SwimTrailPoint previous = rig.swimTrail[rig.swimTrail.Count - 1];
        float distance = Vector3.Distance(previous.position, position);
        if (distance>0.00001f) {
            rig.swimTrail.Add(new SwimTrailPoint(position,
                previous.distance + distance));
        }
    }

    private void BeginSwimTrail() {
        rig.swimTrail.Clear();
        rig.launchBodyOffsets = new float[BodyRings];
        for (int ring = BodyRings - 1; ring >= 0; ring--) {
            Vector3 position = rig.spinePoseReady ? rig.spineCenters[ring]
                : rig.body.position + transform.up
                    * (tuning.SmoothBodyLengthU * ring / (BodyRings - 1));
            Vector3 outward =
                new Vector3(position.x - rig.axisXZ.x, 0f,
                    position.z - rig.axisXZ.y).normalized;
            AppendSwimTrail(rig.OutsideTree(position - outward
                * rig.GroundSpineOffset(ring)));
            rig.launchBodyOffsets[ring] =
                rig.swimTrail[rig.swimTrail.Count - 1].distance;
        }
        rig.launchTrailDistance =
            rig.swimTrail[rig.swimTrail.Count - 1].distance;
        rig.launchBodyArc = rig.launchTrailDistance;
        for (int ring = 0; ring<BodyRings; ring++) {
            rig.launchBodyOffsets[ring] =
                rig.launchTrailDistance - rig.launchBodyOffsets[ring];
        }
        rig.launchNeckArc = 0f;
        int raisedEnd =
            Mathf.Clamp(Mathf.RoundToInt(tuning.StandingNeckLengthU
                / tuning.SmoothBodyLengthU * (BodyRings - 1)), 1,
                    BodyRings - 1);
        for (int ring = 1; ring <= raisedEnd; ring++) {
            rig.launchNeckArc +=
                Vector3.Distance(rig.spineCenters[ring - 1],
                    rig.spineCenters[ring]);
        }
        rig.previousTrailRoot = rig.swimTrail[rig.swimTrail.Count - 1].position;
        rig.displayedTrailHead = rig.previousTrailRoot;
        rig.displayedTrailVelocity = Vector3.zero;
        rig.releaseHeadHeight = rig.spineCenters[0].y;
        rig.releaseTrailOffset =
            rig.previousTrailRoot - rig.GetTreeSurfacePoint(rig.body.position);
        rig.releaseStandingBlend = rig.waitingHeadBlend;
        rig.previousTrailTime = Time.time;
        rig.trailHeading = tracking.GetAttackHeading(rig.body.position);
        rig.swimPhase = rig.swimPhaseOffset;
        rig.swimTravelDistance = 0f;
        rig.swimTrailReady = true;
    }

    private Vector3 GetAttackTrailRoot() {
        if (rig.pursuitPaused && rig.swimTrailReady) {
            return rig.previousTrailRoot;
        }
        float age = Mathf.Max(0f, Time.time - rig.attackStartedTime);
        float t = Mathf.Clamp01(age / tuning.ReleaseCurveDurationS);
        float ease = t * t * t * (t * (t * 6f - 15f) + 10f);
        Vector3 point =
            rig.GetTreeSurfacePoint(tracking.GetPredictedAttackPosition())
                + rig.releaseTrailOffset * (1f - ease);
        point -= rig.launchWindupOffset
            * Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01(age / tuning.StrikeLaunchRampS)) * (1f - ease);
        if (rig.descentStopped) {
            point.y = Mathf.Max(point.y, rig.stoppedTrailHeight);
        }
        return rig.OutsideTree(point);
    }

    private void AdvanceSwimTrail() {
        if (!rig.swimTrailReady) {
            BeginSwimTrail();
        }
        if (rig.pursuitPaused) {
            rig.previousTrailTime = Time.time;
            rig.displayedTrailVelocity = Vector3.zero;
            return;
        }
        float elapsed = Time.time - rig.previousTrailTime;
        if (elapsed <= 0.000001f) {
            return;
        }
        Vector3 currentRoot = GetAttackTrailRoot(),
            lastRoot = rig.previousTrailRoot;
        int samples = Mathf.Clamp(Mathf.CeilToInt(elapsed * 120f), 1, 64);
        float sampleTime = elapsed / samples;
        for (int sample = 1; sample <= samples; sample++) {
            float fraction = sample / (float)samples;
            Vector3 nominal =
                Vector3.Lerp(rig.previousTrailRoot, currentRoot, fraction);
            float advance =
                Vector3.Distance(rig.GetTreeSurfacePoint(lastRoot),
                    rig.GetTreeSurfacePoint(nominal));
            rig.swimTravelDistance += advance;
            rig.swimPhase +=
                advance / Mathf.Max(.5f, tuning.SwimWavelengthU) * Mathf.PI
                    * 2f;
            Vector3 outward =
                new Vector3(nominal.x - rig.axisXZ.x, 0f,
                    nominal.z - rig.axisXZ.y).normalized;
            Vector3 desired = tracking.GetAttackHeading(nominal);
            Vector3 previous =
                Vector3.ProjectOnPlane(rig.trailHeading, outward).normalized;
            if (previous.sqrMagnitude<0.000001f) {
                previous = desired;
            }
            float turn = Vector3.SignedAngle(previous, desired, outward);
            rig.trailHeading =
                Quaternion.AngleAxis(turn
                    * (1f - Mathf.Exp(-tuning.SwimTurnSharpness * sampleTime)),
                        outward) * previous;
            Vector3 lateral =
                Vector3.Cross(rig.trailHeading, outward).normalized;
            float enterDistance =
                Mathf.Max(tuning.SmoothBodyRadiusU * 4f,
                    tuning.HeadSwayEntryDistanceU);
            float enter =
                Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(rig.swimTravelDistance / enterDistance));
            Vector3 head =
                nominal + lateral
                    * (tuning.HeadSwayAmplitudeU * Mathf.Sin(rig.swimPhase)
                        * enter * tracking.SteadyHeadAim(nominal));
            head = Vector3.SmoothDamp(rig.displayedTrailHead, head,
                ref rig.displayedTrailVelocity,
                    Mathf.Lerp(tuning.HeadTrailSmoothTimeS
                        * tuning.StrikeHeadFollowScale,
                            tuning.HeadTrailSmoothTimeS,
                                Mathf.SmoothStep(0f, 1f,
                                    Mathf.Clamp01((Time.time
                                        - rig.attackStartedTime)
                                            / tuning.StrikeBurstDurationS))),
                                                Mathf.Infinity, sampleTime);
            // Store the head's branch-clearing route so the entire body follows
            // that route.
            for (int clearancePass = 0; clearancePass<4; clearancePass++) {
                Vector3 headOutward =
                    new Vector3(head.x - rig.axisXZ.x, 0f,
                        head.z - rig.axisXZ.y).normalized;
                Vector3 headCenter =
                    head + headOutward * rig.GroundSpineOffset(0);
                Quaternion headRotation =
                    Quaternion.Slerp(rig.standingHeadRotation,
                        tracking.DirectedAttackRotation(headCenter),
                            rig.HeadDeparture(headCenter));
                headCenter = tracking.FitHeadToBark(headCenter, headRotation);
                head = rig.OutsideTree(headCenter - headOutward
                    * rig.GroundSpineOffset(0));
                headCenter = head + headOutward * rig.GroundSpineOffset(0);
                head = rig.OutsideTree(head
                    + branchCoil.KeepHeadClearOfBranch(headCenter,
                        headRotation) - headCenter);
            }
            rig.displayedTrailHead = head;
            // Follow the displayed head poses; substep corner projections must
            // not add invisible detours.
            if (sample == samples) {
                AppendSwimTrail(head);
            }
            lastRoot = nominal;
        }
        rig.previousTrailRoot = currentRoot;
        rig.previousTrailTime = Time.time;
        float newest = rig.swimTrail[rig.swimTrail.Count - 1].distance;
        while (rig.swimTrail.Count>4
            && newest
                - rig.swimTrail[1].distance>Mathf.Max(tuning.SmoothBodyLengthU,
                    rig.launchBodyArc) + 1f) {
            rig.swimTrail.RemoveAt(0);
        }
    }

    private void SettleRemainingCoil() {
        if (rig.supportBranch == null) {
            return;
        }
        float gap = float.PositiveInfinity;
        for (int ring = 1; ring<BodyRings; ring++) {
            if (rig.RemainingCoilWeight(ring)<.99f) {
                continue;
            }
            gap = Mathf.Min(gap,
                Vector3.Dot(rig.spineCenters[ring] - rig.branchCenter,
                    rig.branchUp) - rig.branchHalfHeight - rig.spineRadii[ring]
                        - tuning.BranchSkinClearanceU);
        }
        if (float.IsInfinity(gap) || gap <= 0f) {
            return;
        }
        for (int ring = 1; ring<BodyRings; ring++) {
            rig.spineCenters[ring] -=
                rig.branchUp * (gap * rig.RemainingCoilWeight(ring));
        }
    }

    private Vector3 SampleSwimTrail(float behind, ref int cursor) {
        float distance =
            rig.swimTrail[rig.swimTrail.Count - 1].distance - behind;
        if (distance <= rig.swimTrail[0].distance) {
            Vector3 tail =
                (rig.swimTrail[0].position
                    - rig.swimTrail[1].position).normalized;
            return rig.OutsideTree(rig.swimTrail[0].position + tail
                * (rig.swimTrail[0].distance - distance));
        }
        while (cursor>1 && rig.swimTrail[cursor - 1].distance>distance) {
            cursor--;
        }
        int lower = cursor - 1, upper = cursor;
        SwimTrailPoint a = rig.swimTrail[lower], b = rig.swimTrail[upper];
        SwimTrailPoint before = rig.swimTrail[Mathf.Max(0, lower - 1)],
            after = rig.swimTrail[Mathf.Min(rig.swimTrail.Count - 1,
                upper + 1)];
        float span = Mathf.Max(.00001f, b.distance - a.distance);
        float t = Mathf.Clamp01((distance - a.distance) / span), t2 = t * t,
            t3 = t2 * t;
        Vector3 firstTangent =
            (b.position - before.position)
                * (span / Mathf.Max(.00001f, b.distance - before.distance));
        Vector3 lastTangent =
            (after.position - a.position)
                * (span / Mathf.Max(.00001f, after.distance - a.distance));
        Vector3 point =
            (2f * t3 - 3f * t2 + 1f) * a.position + (t3 - 2f * t2 + t)
                * firstTangent
            +(-2f * t3 + 3f * t2) * b.position + (t3 - t2) * lastTangent;
        return rig.OutsideTree(point);
    }

    private void AlignReferenceToTree() {
        AlignReferenceToTree(transform.position);
    }

    private void AlignReferenceToTree(Vector3 position) {
        if (!rig.isInitialized || rig.referenceVisual == null) {
            return;
        }
        Vector3 inward = new Vector3(rig.axisXZ.x - position.x, 0f,
            rig.axisXZ.y - position.z);
        if (inward.sqrMagnitude <= 0.001f) {
            return;
        }
        inward.Normalize();
        Vector3 tangent = Vector3.Cross(Vector3.up, -inward);
        Vector2 surfaceDelta = rig.state == FallingCobraState.Falling
            ? tracking.CommittedAttackVelocity(position)
                : tracking.SurfaceDeltaToPlayer(position);
        if (rig.state != FallingCobraState.Falling && surfaceDelta.y >= 0f) {
            surfaceDelta.y = -rig.fallSpeedU;
        }
        if (surfaceDelta.sqrMagnitude <= 0.001f) {
            return;
        }
        Vector3 heading =
            (tangent * surfaceDelta.x + Vector3.up * surfaceDelta.y).normalized;
        Vector3 tailDirection = -heading;
        if (rig.swimRotationInitialized) {
            Vector3 previous =
                Vector3.ProjectOnPlane(transform.up, inward).normalized;
            if (previous.sqrMagnitude > 0.001f) {
                float turn =
                    Vector3.SignedAngle(previous, tailDirection, inward);
                tailDirection =
                    Quaternion.AngleAxis(turn
                        * (1f - Mathf.Exp(-tuning.SwimTurnSharpness
                            * Time.deltaTime)),
                    inward) * previous;
            }
        }
        rig.referenceVisual.localRotation = Quaternion.identity;
        rig.body.rotation = Quaternion.LookRotation(inward, tailDirection);
        rig.swimRotationInitialized = true;
    }

    /// <summary>Bind this snake to a real, enabled branch before it can be
    /// shown or wake.</summary>
    public bool AttachToBranch(BoxCollider branch) {
        if (!rig.isInitialized || branch == null || !branch.enabled
            || !branch.gameObject.activeInHierarchy) {
            return false;
        }
        rig.supportBranch = branch;
        rig.headRotationReady = false;
        branchCoil.RefreshBranchFrame();
        if (rig.branchHalfLength < tuning.SittingCoilRadiusAlongU
            + tuning.SmoothBodyRadiusU + .1f) {
            rig.supportBranch = null;
            return false;
        }
        Vector3 outwards =
            new Vector3(rig.branchCenter.x - rig.axisXZ.x, 0f,
                rig.branchCenter.z - rig.axisXZ.y).normalized;
        Vector3 nearTrunk =
            new Vector3(rig.axisXZ.x, rig.branchCenter.y, rig.axisXZ.y)
                + outwards
                    * (tuning.TreeSurfaceRadiusU
                        + tuning.SittingCoilRadiusAlongU + .9f);
        rig.branchAnchorAlong =
            Mathf.Clamp(Vector3.Dot(nearTrunk - rig.branchCenter,
                rig.branchAxis),
            -rig.branchHalfLength + tuning.SittingCoilRadiusAlongU + .1f,
                rig.branchHalfLength - tuning.SittingCoilRadiusAlongU - .1f);
        Vector3 top = branchCoil.BranchCoilPoint(0f, tuning.SmoothBodyRadiusU);
        Vector3 root =
            new Vector3(rig.axisXZ.x, top.y - tuning.StandingBaseHeightU,
                rig.axisXZ.y) + outwards * rig.ringRadiusU;
        rig.body.position = root;
        transform.position = root;
        if (rig.player == null) {
            GameObject monkey = GameObject.FindGameObjectWithTag(PLAYER_TAG);
            if (monkey != null) {
                rig.player = monkey.transform;
            }
        }
        rig.branchHeadSideSign = rig.player != null
            && Vector3.Dot(rig.player.position - root, rig.branchSide) < -.01f
                ? -1f : 1f;
        rig.perchBound = true;
        if (rig.referenceRenderer != null) {
            rig.referenceRenderer.enabled = true;
        }
        if (rig.referenceHeadRenderer != null) {
            rig.referenceHeadRenderer.enabled = true;
        }
        Physics.SyncTransforms();
        LateUpdate();
        return true;
    }

    private void RoundNeckSurface(int neckEnd) {
        if (rig.neckSurfaceBuffer == null
            || rig.neckSurfaceBuffer.Length
                != rig.referenceWaveVertices.Length) {
            rig.neckSurfaceBuffer =
                new Vector3[rig.referenceWaveVertices.Length];
            rig.neckSurfaceOriginal =
                new Vector3[rig.referenceWaveVertices.Length];
        }
        System.Array.Copy(rig.referenceWaveVertices, rig.neckSurfaceOriginal,
            rig.referenceWaveVertices.Length);
        float step = tuning.SmoothBodyLengthU / (BodyRings - 1);
        int end =
            Mathf.Clamp(Mathf.CeilToInt(tuning.RoundedNeckLengthU / step), 3,
                Mathf.Min(BodyRings - 2, neckEnd + 4));
        float roundLength = end * step;
        for (int pass = 0; pass<tuning.RoundedNeckPasses; pass++) {
            System.Array.Copy(rig.referenceWaveVertices, rig.neckSurfaceBuffer,
                rig.referenceWaveVertices.Length);
            for (int ring = 1; ring<end; ring++) {
                float behind = ring * step;
                float weight =
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(behind / .16f))
                    *(1f-Mathf.SmoothStep(0f,1f,
                        Mathf.InverseLerp(roundLength*.65f,roundLength,
                            behind)));
                for (int side = 0; side <= BodySides; side++) {
                    int i = ring * BodyStride + side;
                    Vector3 average =
                        (rig.referenceWaveVertices[i - BodyStride]
                            + rig.referenceWaveVertices[i + BodyStride]) * .5f;
                    Vector3 rounded =
                        Vector3.Lerp(rig.referenceWaveVertices[i], average,
                            .5f * weight);
                    rig.neckSurfaceBuffer[i] =
                        rig.neckSurfaceOriginal[i]
                            + Vector3.ClampMagnitude(rounded
                                - rig.neckSurfaceOriginal[i],
                                    tuning.RoundedNeckMaxOffsetU);
                }
            }
            Vector3[] swap = rig.referenceWaveVertices;
            rig.referenceWaveVertices = rig.neckSurfaceBuffer;
            rig.neckSurfaceBuffer = swap;
        }
        if (rig.roundedRingCenters == null) {
            rig.roundedRingCenters = new Vector3[BodyRings];
            rig.originalRingCenters = new Vector3[BodyRings];
        }
        for (int ring = 0; ring <= end; ring++) {
            Vector3 center = Vector3.zero, original = Vector3.zero;
            for (int side = 0; side<BodySides; side++) {
                center += rig.referenceWaveVertices[ring * BodyStride + side];
                original += rig.neckSurfaceOriginal[ring * BodyStride + side];
            }
            rig.roundedRingCenters[ring] = center / BodySides;
            rig.originalRingCenters[ring] = original / BodySides;
        }
        Vector3 headAxis =
            rig.referenceVisual.InverseTransformDirection(
                rig.spineTangents[0]).normalized;
        Vector3 previousRight =
            rig.referenceVisual.InverseTransformDirection(
                rig.referenceHeadVisual.right).normalized;
        for (int ring = 1; ring<end; ring++) {
            float behind = ring * step;
            Vector3 axis =
                (rig.roundedRingCenters[ring + 1]
                    - rig.roundedRingCenters[ring - 1]).normalized;
            axis = Vector3.Slerp(headAxis, axis,
                Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(behind
                        / tuning.NeckCollarLengthU))).normalized;
            float thicknessFloor =
                tuning.SmoothBodyRadiusU * tuning.MinimumNeckThicknessShare
                    * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(behind / .35f));
            Vector3 transported =
                Vector3.ProjectOnPlane(previousRight, axis).normalized;
            if (transported.sqrMagnitude<.000001f) {
                transported =
                    Vector3.ProjectOnPlane(Vector3.right, axis).normalized;
            }
            Vector3 targetRight =
                Vector3.ProjectOnPlane(rig.referenceWaveVertices[ring
                    * BodyStride] - rig.roundedRingCenters[ring],
                        axis).normalized;
            if (targetRight.sqrMagnitude<.000001f) {
                targetRight = transported;
            }
            float roll = Vector3.SignedAngle(transported, targetRight, axis);
            Vector3 right =
                Quaternion.AngleAxis(Mathf.Clamp(roll,
                    -tuning.BodyTwistDegreesPerU * step,
                        tuning.BodyTwistDegreesPerU * step), axis)
                            * transported;
            Vector3 forward = Vector3.Cross(right, axis).normalized;
            previousRight = right;
            for (int side = 0; side <= BodySides; side++) {
                int i = ring * BodyStride + side;
                float angle = (side%BodySides) * Mathf.PI * 2f / BodySides;
                Vector3 radial =
                    right * Mathf.Cos(angle) + forward * Mathf.Sin(angle);
                float radius =
                    Mathf.Max(thicknessFloor,
                        Vector3.Distance(rig.neckSurfaceOriginal[i],
                            rig.originalRingCenters[ring]));
                rig.referenceWaveVertices[i] =
                    rig.roundedRingCenters[ring] + radial.normalized * radius;
            }
        }

    }

    private void SmoothTubeNormals() {
        if (rig.roundedRingCenters == null) {
            rig.roundedRingCenters = new Vector3[BodyRings];
        }
        for (int ring = 0; ring<BodyRings; ring++) {
            Vector3 center = Vector3.zero;
            for (int side = 0; side<BodySides; side++) {
                center += rig.referenceWaveVertices[ring * BodyStride + side];
            }
            rig.roundedRingCenters[ring] = center / BodySides;
        }
        float step = tuning.SmoothBodyLengthU / (BodyRings - 1);
        for (int ring = 1; ring<BodyRings; ring++)for (int side =
            0; side <= BodySides; side++) {
            int i = ring * BodyStride + side, s = side%BodySides;
            Vector3 along =
                rig.referenceWaveVertices[Mathf.Min(BodyRings - 1, ring + 1)
                    * BodyStride + side]
                -rig.referenceWaveVertices[(ring - 1) * BodyStride + side];
            Vector3 across =
                rig.referenceWaveVertices[ring * BodyStride + (s + 1)%BodySides]
                -rig.referenceWaveVertices[ring * BodyStride
                    + (s + BodySides - 1)%BodySides];
            Vector3 normal = Vector3.Cross(along, across);
            if (normal.sqrMagnitude<.00000001f) {
                continue;
            }
            normal.Normalize();
            if (Vector3.Dot(normal,
                rig.referenceWaveVertices[i]
                    - rig.roundedRingCenters[ring])<0f) {
                normal = -normal;
            }
            rig.referenceWaveNormals[i] =
                Vector3.Slerp(rig.referenceWaveNormals[i], normal,
                    SmoothVisual01(ring * step / .3f)).normalized;
        }
    }

    private void RelaxOriginalRaisedNeck() {
        if (rig.relaxedNeck == null || rig.relaxedNeck.Length != BodyRings) {
            rig.relaxedNeck = new Vector3[BodyRings];
        }
        System.Array.Copy(rig.spineCenters, rig.relaxedNeck, BodyRings);
        float step = tuning.SmoothBodyLengthU / (BodyRings - 1),
            newest = rig.swimTrail[rig.swimTrail.Count - 1].distance;
        int reach = Mathf.Max(1, Mathf.RoundToInt(.4f / step));
        float phase =
            Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((Time.time - rig.attackStartedTime) / .25f));
        for (int ring = 3; ring<BodyRings - 2; ring++) {
            float behind = ring * step, pathDistance = newest - behind;
            float start = rig.launchTrailDistance - rig.launchNeckArc;
            float window =
                Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(start - .25f, start + .25f, pathDistance))
                *(1f-Mathf.SmoothStep(0f,1f,
                    Mathf.InverseLerp(rig.launchTrailDistance+.25f,
                        rig.launchTrailDistance+.75f,pathDistance)))
                *Mathf.SmoothStep(0f,1f,Mathf.Clamp01(behind/.5f));
            if (window <= 0f || rig.RemainingCoilWeight(ring)>.01f) {
                continue;
            }
            Vector3 point =
                (rig.spineCenters[Mathf.Max(0, ring - 2 * reach)]
                    + rig.spineCenters[Mathf.Max(0, ring - reach)] * 4f
                        + rig.spineCenters[ring] * 6f
                +rig.spineCenters[Mathf.Min(BodyRings - 1, ring + reach)] * 4f
                    + rig.spineCenters[Mathf.Min(BodyRings - 1,
                        ring + 2 * reach)]) / 16f;
            rig.relaxedNeck[ring] =
                branchCoil.KeepClearOfBranch(
                    Vector3.Lerp(rig.spineCenters[ring], point, phase * window),
                    rig.spineRadii[ring] + .012f);
        }
        Vector3[] swap = rig.spineCenters;
        rig.spineCenters = rig.relaxedNeck;
        rig.relaxedNeck = swap;
    }

    private void FairSpineCorners(bool following, int neckEnd) {
        if (rig.fairSpine == null || rig.fairSpine.Length != BodyRings) {
            rig.fairSpine = new Vector3[BodyRings];
        }
        int end = following?BodyRings - 2:Mathf.Min(BodyRings - 2, neckEnd + 2);
        for (int pass = 0; pass<64; pass++) {
            System.Array.Copy(rig.spineCenters, rig.fairSpine, BodyRings);
            for (int ring = 3; ring<end; ring++) {
                float freeWeight = following?1f - rig.RemainingCoilWeight(ring)
                    :1f;
                if (freeWeight<.00001f) {
                    continue;
                }
                Vector3 before =
                    rig.spineCenters[ring] - rig.spineCenters[ring - 1],
                        after = rig.spineCenters[ring + 1]
                            - rig.spineCenters[ring];
                float bend = Vector3.Angle(before, after);
                float strength =
                    SmoothVisual01(Mathf.InverseLerp(2f, 8f, bend))
                        * freeWeight;
                if (strength<.00001f) {
                    continue;
                }
                Vector3 point =
                    (rig.spineCenters[ring - 1] + rig.spineCenters[ring] * 2f
                        + rig.spineCenters[ring + 1]) * .25f;
                rig.fairSpine[ring] =
                    branchCoil.KeepClearOfBranch(
                        Vector3.Lerp(rig.spineCenters[ring], point, strength),
                        rig.spineRadii[ring] + .012f);
            }
            Vector3[] swap = rig.spineCenters;
            rig.spineCenters = rig.fairSpine;
            rig.fairSpine = swap;
        }
    }

    private void LateUpdate() {
        if (rig.animatedReferenceMesh == null
            || rig.state == FallingCobraState.Done || !rig.perchBound) {
            return;
        }
        branchCoil.RefreshBranchFrame();
        AlignReferenceToTree();
        bool following = rig.state == FallingCobraState.Falling;
        float poseDelta = Mathf.Max(0f, Time.time - rig.lastHeadPoseTime);
        rig.lastHeadPoseTime = Time.time;
        float gain = 1f - Mathf.Exp(-tuning.HeadPoseSharpness * poseDelta);
        rig.waitingHeadBlend =
            Mathf.Lerp(rig.waitingHeadBlend,
                rig.state == FallingCobraState.Warning ? 1f : 0f, gain);
        float windup = rig.state == FallingCobraState.Warning
            && rig.monkeyInHeadRange
            ?Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(rig.fallStartTime
                    - tuning.StrikeWindupDurationS, rig.fallStartTime,
                        Time.time)):0f;
        rig.strikeWindupBlend =
            Mathf.Lerp(rig.strikeWindupBlend, windup,
                1f - Mathf.Exp(-tuning.StrikePoseSharpness * poseDelta));
        if (!following && rig.monkeyInHeadRange) {
            branchCoil.UpdatePerchedAim(poseDelta);
        }
        if (following) {
            AdvanceSwimTrail();
        }
        float phase =
            Time.time * tuning.BodyWaveSpeedHz * .3f * Mathf.PI * 2f
                + rig.swimPhaseOffset;
        Matrix4x4 toWorld = rig.referenceVisual.localToWorldMatrix,
            toLocal = rig.referenceVisual.worldToLocalMatrix;
        Vector3 rootOut =
            new Vector3(rig.body.position.x - rig.axisXZ.x, 0f,
                rig.body.position.z - rig.axisXZ.y).normalized;
        Vector3 rootSide = Vector3.Cross(Vector3.up, rootOut);

        int cursor = following ? rig.swimTrail.Count - 1 : 0;
        Vector3 idle = Vector3.zero;
        for (int ring = 0; ring < BodyRings; ring++) {
            float along = ring / (float)(BodyRings - 1);
            float taper =
                1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(.55f, 1f, along));
            float neckAlong = along * tuning.SmoothBodyLengthU;
            float neckWaist =
                Mathf.Lerp(tuning.NeckWaistScale, 1f,
                    Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(.35f, 1.25f, neckAlong)));
            rig.spineRadii[ring] =
                Mathf.Max(.001f, tuning.SmoothBodyRadiusU * taper * neckWaist);
            Vector3 contact;
            if (following) {
                contact =
                    SampleSwimTrail(rig.BodyTrailDistance(ring), ref cursor);
            }
            else {
                float angle =
                    Mathf.Sin(phase - along * tuning.BodyWaveCycles * Mathf.PI
                        * 2f) * tuning.BodyBendDegrees * .3f * Mathf.Deg2Rad
                    * Mathf.SmoothStep(0f, 1f, along / .3f);
                if (ring > 0) {
                    idle += new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0f)
                        * (tuning.SmoothBodyLengthU / (BodyRings - 1));
                }
                contact =
                    rig.GetTreeSurfacePoint(toWorld.MultiplyPoint3x4(idle));
            }
            Vector3 outward =
                new Vector3(contact.x - rig.axisXZ.x, 0f,
                    contact.z - rig.axisXZ.y).normalized;
            rig.spineCenters[ring] =
                contact + outward * rig.GroundSpineOffset(ring);
        }

        Vector3 crawlHead = rig.spineCenters[0];
        Vector3 headOut =
            new Vector3(crawlHead.x - rig.axisXZ.x, 0f,
                crawlHead.z - rig.axisXZ.y).normalized;
        Vector3 crawlTail = transform.up;
        if (following) {
            int sample = rig.swimTrail.Count - 1;
            Vector3 contact = SampleSwimTrail(0f, ref sample);
            sample = rig.swimTrail.Count - 1;
            Vector3 actual =
                Vector3.ProjectOnPlane(SampleSwimTrail(.06f, ref sample)
                    - contact, headOut).normalized;
            Vector3 nominal =
                Vector3.ProjectOnPlane(-rig.trailHeading, headOut).normalized;
            crawlTail =
                Vector3.Slerp(actual, nominal,
                    tuning.HeadGazeStabilization).normalized;
        }
        if (crawlTail.sqrMagnitude < .00001f) {
            crawlTail = transform.up;
        }
        Quaternion crawlRotation = Quaternion.LookRotation(-headOut, crawlTail);
        if (following) {
            crawlRotation = tracking.DirectedAttackRotation(crawlHead);
        }
        if (rig.state != FallingCobraState.Falling || rig.monkeyInHeadRange) {
            Vector3 watchPoint = following?rig.ThreatPosition
                :rig.supportBranch != null
                    ?branchCoil.BranchCoilPoint(0f, tuning.SmoothBodyRadiusU)
                        + Vector3.up
                            * (tuning.StandingFaceHeightU
                                - tuning.StandingBaseHeightU)
                +rootOut * tuning.BranchHeadForwardU + rig.branchSide
                    * (tuning.BranchHeadSideU
                        * rig.perchedAimSide):rig.ThreatPosition;
            if (!following) {
                watchPoint += rig.strikeWindupOffset;
            }
            Vector3 watchDelta = tracking.MonkeyAimPosition() - watchPoint;
            Vector3 direction = rig.monkeyInHeadRange
                && watchDelta.sqrMagnitude > .00001f
                ? watchDelta.normalized
                    : (rootOut + Vector3.down
                        * Mathf.Tan(tuning.WaitingLookDownDegrees
                            * Mathf.Deg2Rad)).normalized;
            Vector3 faceUp =
                Vector3.ProjectOnPlane(Vector3.up, direction).normalized;
            if (faceUp.sqrMagnitude < .00001f) {
                faceUp = rootOut;
            }
            Quaternion face = Quaternion.LookRotation(direction, faceUp);
            rig.standingHeadRotation =
                face * Quaternion.Inverse(rig.SourceEyeBasis());
        }
        Quaternion restRotation =
            Quaternion.LookRotation(rootOut * .5f + Vector3.down * .8660254f,
                Vector3.up) * Quaternion.Inverse(rig.SourceEyeBasis());
        float liftBlend =
            1f - (1f - rig.waitingHeadBlend) * (1f - rig.waitingHeadBlend);
        float turnBlend =
            Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(.15f, .85f, rig.waitingHeadBlend));
        float risingClearance =
            4f * rig.waitingHeadBlend * (1f - rig.waitingHeadBlend)
                * tuning.RisingFaceClearanceU;
        float departure = following?rig.HeadDeparture(crawlHead):0f;
        Quaternion headRotation = following
            ? Quaternion.Slerp(rig.standingHeadRotation, crawlRotation,
                departure)
            : Quaternion.Slerp(restRotation, rig.standingHeadRotation,
                turnBlend);
        Vector3 desiredFace =
            new Vector3(rig.axisXZ.x,
                rig.body.position.y + tuning.StandingFaceHeightU, rig.axisXZ.y)
            + rootOut
                * (tuning.TreeSurfaceRadiusU + tuning.TreeSurfaceClearanceU
                    + tuning.WaitingHeadLiftU);
        if (rig.supportBranch != null) desiredFace =
            branchCoil.BranchCoilPoint(0f, tuning.SmoothBodyRadiusU)
            + Vector3.up
                * (tuning.StandingFaceHeightU - tuning.StandingBaseHeightU)
                    + rootOut * tuning.BranchHeadForwardU + rig.branchSide
                        * (tuning.BranchHeadSideU * rig.perchedAimSide);
        if (!following) {
            Vector3 toward = tracking.MonkeyAimPosition() - desiredFace;
            if (toward.sqrMagnitude<.00001f) {
                toward = rootOut;
            }
            rig.strikeWindupOffset =
                (-toward.normalized * tuning.StrikePullbackU - Vector3.up
                    * tuning.StrikeCompressionU) * rig.strikeWindupBlend;
            desiredFace += rig.strikeWindupOffset;
        }
        Vector3 restFace = rig.supportBranch != null
            ? branchCoil.RestingBranchFace(restRotation)
                : crawlHead + restRotation * rig.originalHeadFaceCenter;
        Vector3 restHead = restFace - restRotation * rig.originalHeadFaceCenter;
        Vector3 standHead =
            desiredFace - rig.standingHeadRotation * rig.originalHeadFaceCenter;
        Vector3 raisedFace =
            Vector3.Lerp(restFace, desiredFace, liftBlend) + rootOut
                * risingClearance;
        Vector3 headCenter = following ? crawlHead
            : raisedFace - headRotation * rig.originalHeadFaceCenter;

        if (rig.headRotationReady) {
            headRotation =
                Quaternion.RotateTowards(rig.shownHeadRotation, headRotation,
                    tuning.HeadLookTurnSpeedDegrees * poseDelta);
        }
        rig.shownHeadRotation = headRotation;
        rig.headRotationReady = true;
        headCenter = tracking.FitHeadToBark(headCenter, headRotation);
        headCenter = branchCoil.KeepHeadClearOfBranch(headCenter, headRotation);

        float raisedLength =
            Mathf.Min(tuning.StandingNeckLengthU,
                tuning.SmoothBodyLengthU * .7f);
        float curveLength = following
            ? Mathf.Lerp(raisedLength, tuning.NeckFollowLengthU, departure)
                + .8f * Mathf.Sin(Mathf.PI * departure) : raisedLength;
        int endRing =
            Mathf.Clamp(Mathf.RoundToInt(curveLength / tuning.SmoothBodyLengthU
                * (BodyRings - 1)), 1, BodyRings - 2);
        bool continuousFreeNeck = false;
        float curveRingSpan = endRing, freeSpanDistance = 0f;
        Vector3 continuousAnchor = Vector3.zero,
            continuousAnchorDirection = Vector3.zero;
        if (following) {
            SettleRemainingCoil();
            int anchor = 1;
            while (anchor<BodyRings - 2
                && rig.RemainingCoilWeight(anchor)<.98f) {
                anchor++;
            }
            if (anchor<BodyRings - 2 && anchor >= 4) {
                int lower = anchor - 1;
                float transition = .5f - Mathf.Sin(Mathf.Asin(.96f) / 3f);
                float boundary =
                    rig.launchTrailDistance - rig.launchNeckArc
                        + Mathf.Lerp(-tuning.UncoilSupportBlendU,
                            tuning.UncoilSupportBlendU, transition);
                freeSpanDistance =
                    rig.swimTrail[rig.swimTrail.Count - 1].distance - boundary;
                float lowDistance = rig.BodyTrailDistance(lower),
                    highDistance = rig.BodyTrailDistance(anchor);
                float fraction =
                    Mathf.InverseLerp(lowDistance, highDistance,
                        freeSpanDistance);
                float span = Mathf.Max(.0001f, highDistance - lowDistance);
                curveRingSpan = lower + fraction;
                endRing = anchor;
                continuousFreeNeck = true;
                Vector3 first =
                    (rig.spineCenters[anchor] - rig.spineCenters[lower - 1])
                        * (span / Mathf.Max(.0001f,
                            highDistance - rig.BodyTrailDistance(lower - 1)));
                Vector3 last =
                    (rig.spineCenters[anchor + 1] - rig.spineCenters[lower])
                        * (span / Mathf.Max(.0001f,
                            rig.BodyTrailDistance(anchor + 1) - lowDistance));
                continuousAnchor =
                    SpineCurve(rig.spineCenters[lower],
                        rig.spineCenters[anchor], first, last, fraction);
                continuousAnchorDirection =
                    SpineCurveDirection(rig.spineCenters[lower],
                        rig.spineCenters[anchor], first, last, fraction);
            }
        }
        curveLength = continuousFreeNeck?freeSpanDistance
            :curveRingSpan * tuning.SmoothBodyLengthU / (BodyRings - 1);
        Vector3 curveEnd = continuousFreeNeck?continuousAnchor
            :rig.spineCenters[endRing];
        Vector3 curveEndDirection = continuousFreeNeck
            ?continuousAnchorDirection
            :(rig.spineCenters[endRing + 1]
                - rig.spineCenters[endRing - 1]).normalized;
        if (!following) {
            Vector3 standBaseContact =
                branchCoil.StandingTailPoint(0f, rootOut, rootSide);
            Vector3 standBase = rig.supportBranch != null
                ? branchCoil.BranchCoilPoint(0f, rig.spineRadii[endRing])
                    : standBaseContact + rootOut
                        * rig.GroundSpineOffset(endRing);
            curveEnd = standBase;
            curveEndDirection = rig.supportBranch != null
                ? (branchCoil.BranchCoilPoint(.02f, rig.spineRadii[endRing])
                    - standBase).normalized
                : (branchCoil.StandingTailPoint(.02f, rootOut, rootSide)
                    - standBaseContact).normalized;
            for (int ring = endRing; ring < BodyRings; ring++) {
                float behind =
                    (ring - endRing) * tuning.SmoothBodyLengthU
                        / (BodyRings - 1);
                Vector3 contact =
                    branchCoil.StandingTailPoint(behind, rootOut, rootSide);
                Vector3 outward =
                    new Vector3(contact.x - rig.axisXZ.x, 0f,
                        contact.z - rig.axisXZ.y).normalized;
                Vector3 stand = rig.supportBranch != null
                    ? branchCoil.BranchCoilPoint(behind, rig.spineRadii[ring])
                        : contact + outward * rig.GroundSpineOffset(ring);
                rig.spineCenters[ring] = stand;
            }
        }
        float handle = continuousFreeNeck
            ?Vector3.Distance(headCenter, curveEnd) * .9f
            :Mathf.Max(Vector3.Distance(headCenter, curveEnd) * .75f,
                curveLength * .65f);
        Vector3 startTangent = headRotation * Vector3.up * handle;
        Vector3 endTangent = curveEndDirection * handle;
        for (int ring = 0; ring < endRing; ring++) {
            float t = continuousFreeNeck
                ?Mathf.Clamp01(rig.BodyTrailDistance(ring)
                    / Mathf.Max(.001f, freeSpanDistance)):Mathf.Clamp01(ring
                        / curveRingSpan);
            rig.spineCenters[ring] =
                SpineCurve(headCenter, curveEnd, startTangent, endTangent, t);
            if (continuousFreeNeck && ring>0) {
                Vector3 point = rig.spineCenters[ring],
                    radial = new Vector3(point.x - rig.axisXZ.x, 0f,
                        point.z - rig.axisXZ.y);
                float rawRadius = radial.magnitude;
                Vector3 outwards = rawRadius>.0001f?radial / rawRadius:rootOut;
                float barkRadius =
                    tuning.TreeSurfaceRadiusU + tuning.TreeSurfaceClearanceU
                        + rig.spineRadii[ring] * .9f - tuning.BellyPressDepthU;
                float belowWood =
                    Vector3.Dot(rig.branchCenter + rig.branchUp
                        * rig.branchHalfHeight - headCenter, rig.branchUp);
                float contact =
                    SmoothVisual01(t / .2f)
                        * (1f - SmoothVisual01(Mathf.InverseLerp(.6f, 1f, t)))
                            * SmoothVisual01(belowWood
                                / tuning.BodyBarkSettleDropU);
                float radius = Mathf.Lerp(rawRadius, barkRadius, contact);
                float gap = radius - barkRadius;
                float softRadius =
                    barkRadius
                        + (gap + Mathf.Sqrt(gap * gap
                            + tuning.BodyBarkContactFeatherU
                                * tuning.BodyBarkContactFeatherU)) * .5f;
                radius = Mathf.Lerp(radius, softRadius,
                    SmoothVisual01(t / .05f));
                Vector3 side = Vector3.Cross(Vector3.up, outwards);
                point = new Vector3(rig.axisXZ.x, point.y, rig.axisXZ.y)
                    + outwards * radius;
                point += side
                    * (tuning.FreeNeckSwayU
                        * Mathf.Sin(phase + t * Mathf.PI * 2f)
                            * Mathf.Pow(Mathf.Sin(t * Mathf.PI), 4f));
                rig.spineCenters[ring] = point;
            }
            if (!following) {
                float arch = Mathf.Sin(Mathf.PI * t);
                rig.spineCenters[ring] +=
                    rootOut * (tuning.StandingChestLiftU * rig.waitingHeadBlend
                        * arch * arch);
                float nearHood =
                    Mathf.Sin(Mathf.PI
                        * Mathf.Clamp01(ring * tuning.SmoothBodyLengthU
                            / (BodyRings - 1)
                                / tuning.HoodNeckClearanceLengthU));
                rig.spineCenters[ring] -=
                    rootOut * (tuning.HoodNeckSetbackU * rig.waitingHeadBlend
                        * nearHood * nearHood);
                Vector3 faceForward =
                    headRotation * rig.originalHeadFaceBasis * Vector3.forward;
                rig.spineCenters[ring] -=
                    faceForward * (risingClearance * nearHood * nearHood);
            }
        }
        for (int ring = 1; ring < BodyRings; ring++) {
            float behind = ring * tuning.SmoothBodyLengthU / (BodyRings - 1);
            float radius =
                Mathf.Lerp(rig.headNeckRadiusU, rig.spineRadii[ring],
                    Mathf.SmoothStep(0f, 1f, behind / .4f));
            rig.spineCenters[ring] =
                branchCoil.KeepClearOfBranch(rig.spineCenters[ring], radius);
        }
        if (following) {
            SettleRemainingCoil();
            if (!continuousFreeNeck) {
                RelaxOriginalRaisedNeck();
            }
        }
        FairSpineCorners(following, endRing);
        rig.spineCenters[0] = headCenter;
        rig.referenceHeadVisual.SetPositionAndRotation(headCenter,
            headRotation);

        float collarLoftU = tuning.NeckCollarLengthU;
        Vector3 headOutward =
            new Vector3(headCenter.x - rig.axisXZ.x, 0f,
                headCenter.z - rig.axisXZ.y).normalized;
        Vector3 headBelly = branchCoil.SupportedPaintBelly(headOutward, 0f);
        Quaternion frame = headRotation;
        float surface =
            tuning.TreeSurfaceRadiusU + tuning.TreeSurfaceClearanceU
                - tuning.BellyPressDepthU;
        for (int ring = 0; ring < BodyRings; ring++) {
            float behind = ring * tuning.SmoothBodyLengthU / (BodyRings - 1);
            Vector3 tangent = ring == 0 ? headRotation * Vector3.up
                : (rig.spineCenters[Mathf.Min(BodyRings - 1, ring + 1)]
                    - rig.spineCenters[ring - 1]).normalized;
            if (tangent.sqrMagnitude < .00001f) {
                tangent = frame * Vector3.up;
            }
            rig.spineTangents[ring] = tangent;
            Vector3 outward =
                new Vector3(rig.spineCenters[ring].x - rig.axisXZ.x, 0f,
                    rig.spineCenters[ring].z - rig.axisXZ.y).normalized;
            float centreRadius =
                new Vector2(rig.spineCenters[ring].x - rig.axisXZ.x,
                    rig.spineCenters[ring].z - rig.axisXZ.y).magnitude;
            float lift = centreRadius - (surface + rig.spineRadii[ring] * .9f);
            rig.spineContact[ring] =
                1f - Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(.02f, .15f, lift));
            if (ring > 0) {
                Vector3 transported =
                    Vector3.ProjectOnPlane(frame * Vector3.forward,
                        tangent).normalized;
                if (transported.sqrMagnitude < .00001f) {
                    transported =
                        Vector3.ProjectOnPlane(-outward, tangent).normalized;
                }
                frame = Quaternion.LookRotation(transported, tangent);

            }
            float paintBranchWeight = following?rig.RemainingCoilWeight(ring)
                :Mathf.SmoothStep(0f, 1f,
                    Mathf.InverseLerp(raisedLength - .25f, raisedLength + .25f,
                        behind));
            Vector3 paintBelly = ring == 0?headBelly
                :branchCoil.SupportedPaintBelly(outward, paintBranchWeight);
            Vector3 right = frame * Vector3.right,
                belly = frame * Vector3.forward;
            float loft =
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(behind / collarLoftU));
            for (int side = 0; side <= BodySides; side++) {
                float angle = (side % BodySides) * Mathf.PI * 2f / BodySides,
                    cosine = Mathf.Cos(angle), sine = Mathf.Sin(angle);
                Vector3 localCircle =
                    new Vector3(cosine * rig.spineRadii[ring], 0f,
                        sine * rig.spineRadii[ring]);
                Vector3 section =
                    Vector3.Lerp(rig.neckRim[side], localCircle, loft);
                Vector3 point =
                    rig.spineCenters[ring] + frame * section - tangent
                        * (NeckOverlapU * (1f - loft));
                Vector3 normal = right * cosine + belly * sine;
                normal = Vector3.Slerp(frame * rig.neckSourceNormals[side],
                    normal, loft).normalized;
                Vector3 radial =
                    new Vector3(point.x - rig.axisXZ.x, 0f,
                        point.z - rig.axisXZ.y);
                if (ring > 0 && radial.magnitude < surface) {
                    radial.Normalize();
                    point.x = rig.axisXZ.x + radial.x * surface;
                    point.z = rig.axisXZ.y + radial.z * surface;
                    normal = -radial;
                }
                int index = ring * BodyStride + side;
                rig.referenceWaveVertices[index] =
                    toLocal.MultiplyPoint3x4(point);
                rig.referenceWaveNormals[index] =
                    toLocal.MultiplyVector(normal).normalized;
                rig.paintedBellyDirections[index] =
                    toLocal.MultiplyVector(paintBelly).normalized;
            }
        }
        RoundNeckSurface(endRing);
        int tip = BodyRings * BodyStride, neckCap = tip + 1;
        rig.referenceWaveVertices[tip] =
            toLocal.MultiplyPoint3x4(rig.spineCenters[BodyRings - 1]
                + rig.spineTangents[BodyRings - 1] * .005f);
        rig.referenceWaveNormals[tip] =
            toLocal.MultiplyVector(rig.spineTangents[BodyRings - 1]);
        rig.referenceWaveVertices[neckCap] =
            toLocal.MultiplyPoint3x4(headCenter - headRotation * Vector3.up
                * NeckOverlapU);
        rig.referenceWaveNormals[neckCap] =
            toLocal.MultiplyVector(-(headRotation * Vector3.up));
        rig.paintedBellyDirections[neckCap] =
            toLocal.MultiplyVector(headBelly).normalized;
        rig.paintedBellyDirections[tip] =
            rig.paintedBellyDirections[(BodyRings - 1) * BodyStride];
        SmoothTubeNormals();
        rig.animatedReferenceMesh.vertices = rig.referenceWaveVertices;
        rig.animatedReferenceMesh.normals = rig.referenceWaveNormals;
        rig.animatedReferenceMesh.SetUVs(2, rig.paintedBellyDirections);
        rig.animatedReferenceMesh.RecalculateBounds();
        Vector3 sourceBelly =
            -Vector3.ProjectOnPlane(rig.SourceEyeForward(),
                Vector3.up).normalized;
        float palettePhase =
            .25f - Mathf.Atan2(sourceBelly.z, sourceBelly.x) / (Mathf.PI * 2f);
        rig.referenceRenderer.GetPropertyBlock(rig.surfaceBlock);
        rig.surfaceBlock.SetVector("_TreeAxis",
            new Vector4(rig.axisXZ.x, 0f, rig.axisXZ.y, 0f));
        rig.surfaceBlock.SetFloat("_HeadBellyPhase", palettePhase);
        rig.surfaceBlock.SetVector("_HeadBellyDirection", headBelly);
        rig.surfaceBlock.SetFloat("_CollarLength", tuning.NeckCollarLengthU);
        rig.surfaceBlock.SetFloat("_HeadWatch", following ? 1f - departure
            : 1f);
        rig.surfaceBlock.SetFloat("_BodyLength", tuning.SmoothBodyLengthU);
        rig.referenceRenderer.SetPropertyBlock(rig.surfaceBlock);
        rig.referenceHeadRenderer.GetPropertyBlock(rig.surfaceBlock);
        rig.surfaceBlock.SetVector("_TreeAxis",
            new Vector4(rig.axisXZ.x, 0f, rig.axisXZ.y, 0f));
        rig.surfaceBlock.SetFloat("_HeadBellyPhase", palettePhase);
        rig.surfaceBlock.SetVector("_HeadBellyDirection", headBelly);
        rig.surfaceBlock.SetFloat("_CollarLength", tuning.NeckCollarLengthU);
        rig.surfaceBlock.SetFloat("_HeadWatch", following
            ? rig.waitingHeadBlend : 1f);
        rig.surfaceBlock.SetFloat("_HeadWatch", 1f);
        rig.referenceHeadRenderer.SetPropertyBlock(rig.surfaceBlock);
        rig.spinePoseReady = true;
        eyes.UpdateTrackingEyes();
        eyes.UpdateVisibleContacts();
    }

    private void Start() {
        GameObject playerObject = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObject != null) {
            rig.player = playerObject.transform;
            rig.playerArms =
                playerObject.GetComponentsInChildren<ArmController>(true);
            rig.playerSensor = 
                playerObject.GetComponentInChildren<HitBranchSensor>(true);
        }
    }

    private void Update() {
        switch (rig.state) {
            case FallingCobraState.Dormant:
                TryWake();
                break;
            case FallingCobraState.Warning:
                WatchMonkey();
                break;
        }
    }

    private void FixedUpdate() {
        tracking.UpdateTrackedMonkey();
        if (rig.state == FallingCobraState.Warning && CanStartFall()) {
            StartFalling();
        }
        if (rig.state == FallingCobraState.Falling) {
            StepFall();
            if (gameObject.activeInHierarchy) {
                eyes.SweepVisibleHead();
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        TryHit(other);
    }

    // Enter/stay and the monkey sensor all share the same one-hit guard.
    private void OnTriggerStay(Collider other) {
        TryHit(other);
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        tuning.Validate();
    }

    private void TryWake() {
        if (!rig.isInitialized || rig.player == null
            || rig.supportBranch == null) {
            return;
        }
        Vector3 position = rig.ThreatPosition;
        float wakeDistanceU = tracking.IsMonkeyReaching()
            ? Mathf.Max(tuning.TriggerDistanceU, rig.reachWakeDistanceU)
            : tuning.TriggerDistanceU;
        if (Vector3.Distance(position,
            tracking.MonkeyAimPosition()) >= wakeDistanceU) {
            return;
        }

        rig.state = FallingCobraState.Warning;
        rig.warningStartTime = Time.time;
        rig.fallStartTime = rig.warningStartTime + tuning.WarningDurationS;
        rig.nextHissTime = Time.time;
    }

    // Called from either side of the trigger pair. Claim the hit before
    // notifying listeners so callback order cannot create duplicate damage.
    public void TryHit(Collider other) {
        if (other == null || rig.state != FallingCobraState.Falling
            || !rig.hitCollider.enabled
            || GameController.I == null || !GameController.I.IsRunLive
            || GameController.I.IsLeaping || Time.timeScale <= 0f) {
            return;
        }
        HitBranchSensor sensor = other.GetComponent<HitBranchSensor>();
        if (sensor != null || !IsMonkeyPart(other)) {
            return;
        }
        rig.state = FallingCobraState.Done;
        rig.hitCollider.enabled = false;
        HitBranchSensor receiver = sensor != null ? sensor : rig.playerSensor;
        if (receiver != null) {
            receiver.ReportHit(
                rig.hitCollider,
                HitBranchSensor.HitKind.FallingCobraController
            );
        }
        // Keep the existing segment-owned cleanup lifecycle, but remove
        // visuals, physics and movement immediately on the first contact.
        gameObject.SetActive(false);
    }

    /// <returns>true for the monkey's physical parts: body, tail and hands;
    /// false for its other trigger volumes, which reach far past the body
    /// </returns>
    private static bool IsMonkeyPart(Collider other) {
        if (other.GetComponent<DetectionZoneRelay>() != null) {
            return false;
        }
        if (other.GetComponentInParent<ArmController>(true) != null) {
            return true;
        }
        if (other.isTrigger) {
            return false;
        }
        for (Transform t = other.transform; t != null; t = t.parent) {
            if (t.CompareTag(PLAYER_TAG)) {
                return true;
            }
        }
        return false;
    }

    private void WatchMonkey() {
        if (rig.supportBranch == null) {
            FinishFall();
            return;
        }
        if (rig.player == null || !rig.monkeyInHeadRange) {
            EndWatching();
            return;
        }
        if (rig.hissSource != null && Time.time >= rig.nextHissTime
            && !rig.hissSource.isPlaying) {
            rig.hissSource.Play();
            rig.nextHissTime =
                Time.time
                    + Mathf.Max(tuning.WarningHissIntervalS,
                        tuning.WarningHiss.length);
        }
    }

    private void EndWatching() {
        rig.state = FallingCobraState.Dormant;
        if (rig.hissSource != null) {
            rig.hissSource.Stop();
        }
    }

    private bool CanStartFall() {
        return rig.player != null && rig.monkeyInHeadRange
            && Time.time >= rig.fallStartTime
            && (GameController.I == null || !GameController.I.IsLeaping);
    }

    private void StartFalling() {
        if (!rig.monkeyInHeadRange) {
            return;
        }
        float targetSide = rig.player != null
            ?Vector3.Dot(tracking.MonkeyAimPosition() - rig.branchCenter,
                rig.branchSide):rig.perchedAimSide;
        if (Mathf.Abs(targetSide)>.05f) {
            rig.branchHeadSideSign = Mathf.Sign(targetSide);
        }
        rig.pursuitPaused = false;
        rig.state = FallingCobraState.Falling;
        rig.attackStartedTime = Time.time;
        rig.launchWindupOffset = rig.strikeWindupOffset;
        rig.approachFallScale = 1f;
        rig.headFeedbackReady = false;
        rig.headAimCommand = 0f;
        rig.descentStopped = false;
        rig.hasCommittedTarget = rig.player != null;
        if (rig.hasCommittedTarget) {
            rig.committedTargetPosition = tracking.MonkeyAimPosition();
            Vector2 target =
                new Vector2(rig.committedTargetPosition.x,
                    rig.committedTargetPosition.z) - rig.axisXZ;
            rig.committedTargetAngle =
                Mathf.Atan2(target.x, target.y) * Mathf.Rad2Deg;
        }
        if (rig.hissSource != null) {
            rig.hissSource.Stop();
        }
        rig.branchEscapeSide = rig.branchHeadSideSign;
        BeginSwimTrail();
        rig.hitCollider.enabled = true;
        rig.previousHeadContact = rig.ThreatPosition;
        rig.headContactReady = true;
        eyes.UpdateVisibleContacts();
    }

    private void FinishFall() {
        rig.state = FallingCobraState.Done;
        rig.hitCollider.enabled = false;
        gameObject.SetActive(false);
    }

    private void StepFall() {
        if (Time.time - rig.attackStartedTime >= tuning.MaximumFallDurationS) {
            FinishFall();
            return;
        }
        Vector3 position = rig.body.position;
        Vector2 velocity = tracking.CommittedAttackVelocity(position);
        float drop = -velocity.y * Time.fixedDeltaTime;
        position.y -= drop;
        if (rig.ringRadiusU > MIN_RING_RADIUS_U) {
            Vector2 radial = new Vector2(position.x, position.z) - rig.axisXZ;
            float angle =
                Mathf.Atan2(radial.x, radial.y) + velocity.x
                    * Time.fixedDeltaTime / rig.ringRadiusU;
            position.x = rig.axisXZ.x + Mathf.Sin(angle) * rig.ringRadiusU;
            position.z = rig.axisXZ.y + Mathf.Cos(angle) * rig.ringRadiusU;
        }
        rig.body.MovePosition(position);
        AlignReferenceToTree(position);
        rig.body.MoveRotation(rig.body.rotation);
        rig.fallenU += drop;
        if (rig.fallenU >= tuning.MaxFallDistanceU) {
            FinishFall();
        }
    }

}
