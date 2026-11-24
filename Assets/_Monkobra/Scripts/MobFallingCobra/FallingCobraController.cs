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
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FallingCobraController: MonoBehaviour {
    // Public Members  #########################################################
    /// <returns>pivot to the far end of the snake, hit radius included; u
    /// </returns>
    public float LengthU => rig.animatedReferenceMesh != null
        ?config.SmoothBodyLengthU:rig.hitCollider.height;

    /// <returns>how far the snake's tail reaches below its pivot; u</returns>
    public float TailBelowPivotU => rig.animatedReferenceMesh != null
        ? rig.referenceHeadBelowPivotU : config.HitRadiusU;

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
    [Tooltip("every tuned value of a falling cobra")]
    private FallingCobraConfig config;

    // private members  ########################################################
    private FallingCobraRig rig;
    private FallingCobraHeadMesh headMesh;
    private FallingCobraBranchCoil branchCoil;
    private FallingCobraTracking tracking;
    private FallingCobraEyes eyes;
    private FallingCobraSwimTrail trail;
    private FallingCobraSpine spine;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (config == null) {
            Debug.LogError(
                "FallingCobraController:\tmust assign Inspector Field: config",
                this
            );
            enabled = false;
            return;
        }

        rig = new FallingCobraRig(transform, config);
        headMesh = new FallingCobraHeadMesh(rig);
        branchCoil = new FallingCobraBranchCoil(rig);
        tracking = new FallingCobraTracking(rig);
        eyes = new FallingCobraEyes(rig, TryHit);
        rig.HeadMesh = headMesh;
        rig.BranchCoil = branchCoil;
        rig.Tracking = tracking;
        rig.Eyes = eyes;
        trail = new FallingCobraSwimTrail(rig);
        spine = new FallingCobraSpine(rig);
        rig.Trail = trail;
        rig.Spine = spine;
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
                    Vector3.up * (i * config.SegmentSpacingU);
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
        rig.hitCollider.radius = config.HitRadiusU;
        rig.hitCollider.height = highestSegmentY + config.HitRadiusU * 2f;
        rig.hitCollider.center = Vector3.up * highestSegmentY * 0.5f;
        rig.hitCollider.isTrigger = true;
        rig.hitCollider.enabled = false;

        headMesh.BuildCartoonVisual();
        eyes.CreateVisibleContacts();
        eyes.CreateTrackingEyes();

        if (config.WarningHiss != null) {
            GameObject voice = new GameObject("SnakeHiss");
            voice.transform.SetParent(rig.referenceHeadVisual != null
                ? rig.referenceHeadVisual : transform, false);
            voice.transform.localPosition = rig.originalHeadFaceCenter;
            rig.hissSource = voice.AddComponent<AudioSource>();
            rig.hissSource.clip = config.WarningHiss;
            rig.hissSource.playOnAwake = false;
            rig.hissSource.loop = false;
            rig.hissSource.volume = config.WarningHissVolume;
            rig.hissSource.spatialBlend = 1f;
            rig.hissSource.dopplerLevel = 0f;
            rig.hissSource.minDistance = config.WarningHissMinDistanceU;
            rig.hissSource.maxDistance =
                Mathf.Max(config.TriggerDistanceU + config.AlertExitPaddingU,
                    config.WarningHissMinDistanceU);
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
                        * (1f - Mathf.Exp(-config.SwimTurnSharpness
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
        if (rig.branchHalfLength < config.SittingCoilRadiusAlongU
            + config.SmoothBodyRadiusU + .1f) {
            rig.supportBranch = null;
            return false;
        }
        Vector3 outwards =
            new Vector3(rig.branchCenter.x - rig.axisXZ.x, 0f,
                rig.branchCenter.z - rig.axisXZ.y).normalized;
        Vector3 nearTrunk =
            new Vector3(rig.axisXZ.x, rig.branchCenter.y, rig.axisXZ.y)
                + outwards
                    * (config.TreeSurfaceRadiusU
                        + config.SittingCoilRadiusAlongU + .9f);
        rig.branchAnchorAlong =
            Mathf.Clamp(Vector3.Dot(nearTrunk - rig.branchCenter,
                rig.branchAxis),
            -rig.branchHalfLength + config.SittingCoilRadiusAlongU + .1f,
                rig.branchHalfLength - config.SittingCoilRadiusAlongU - .1f);
        Vector3 top = branchCoil.BranchCoilPoint(0f, config.SmoothBodyRadiusU);
        Vector3 root =
            new Vector3(rig.axisXZ.x, top.y - config.StandingBaseHeightU,
                rig.axisXZ.y) + outwards * rig.ringRadiusU;
        rig.body.position = root;
        transform.position = root;
        if (rig.player == null) {
            GameObject monkey = MonkeyController.GetRootOrNull();
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

    private void LateUpdate() {
        if (rig.animatedReferenceMesh == null
            || rig.state == FallingCobraState.Done || !rig.perchBound) {
            return;
        }
        if (IsDormantAndIdle()) {
            return;
        }
        branchCoil.RefreshBranchFrame();
        AlignReferenceToTree();
        spine.Update();
    }

    // a perched cobra holds its last pose, so skip the spine rebuild, the
    // costly part of a cobra (its whole body mesh made again on the CPU),
    // while it is off screen or beyond its wake & tracking range; the
    // margin lets any head turn finish first. Waking (Update) never waits
    // on it, and an awake cobra always rebuilds
    private bool IsDormantAndIdle() {
        if (rig.state != FallingCobraState.Dormant || !rig.spinePoseReady
            || rig.player == null) {
            return false;
        }
        if (rig.referenceRenderer != null && !rig.referenceRenderer.isVisible) {
            return true;
        }
        float rangeU = Mathf.Max(config.HeadTrackingRangeU,
            config.TriggerDistanceU, rig.reachWakeDistanceU);
        float gateU = rangeU + config.HeadTrackingExitPaddingU
            + IDLE_MARGIN_U;
        return (rig.player.position - transform.position).sqrMagnitude
            > gateU * gateU;
    }

    // past the wake & tracking range, how far a perched cobra still
    // rebuilds its pose, so a head turn ends smoothly; u
    private const float IDLE_MARGIN_U = 4f;

    private void Start() {
        GameObject playerObject = MonkeyController.GetRootOrNull();
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
                // a cobra never outlives its branch: one left in mid air by
                // a switched off branch goes off with it
                if (rig.perchBound && (rig.supportBranch == null
                    || !rig.supportBranch.gameObject.activeInHierarchy)) {
                    gameObject.SetActive(false);
                    break;
                }
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

    private void TryWake() {
        if (!rig.isInitialized || rig.player == null
            || rig.supportBranch == null) {
            return;
        }
        Vector3 position = rig.ThreatPosition;
        float wakeDistanceU = tracking.IsMonkeyReaching()
            ? Mathf.Max(config.TriggerDistanceU, rig.reachWakeDistanceU)
            : config.TriggerDistanceU;
        if (Vector3.Distance(position,
            tracking.MonkeyAimPosition()) >= wakeDistanceU) {
            return;
        }

        rig.state = FallingCobraState.Warning;
        rig.warningStartTime = Time.time;
        rig.fallStartTime = rig.warningStartTime + config.WarningDurationS;
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
                    + Mathf.Max(config.WarningHissIntervalS,
                        config.WarningHiss.length);
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
        trail.BeginSwimTrail();
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
        if (Time.time - rig.attackStartedTime >= config.MaximumFallDurationS) {
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
        if (rig.fallenU >= config.MaxFallDistanceU) {
            FinishFall();
        }
    }

}
