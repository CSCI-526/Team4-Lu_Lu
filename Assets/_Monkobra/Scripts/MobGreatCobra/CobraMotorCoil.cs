using UnityEngine;
using static CobraMotorRig;

/// <summary>Lays the body along the coil, styles it, and poses the cartoon
/// head.</summary>
public sealed class CobraMotorCoil {
    // private members  ########################################################
    private readonly CobraMotorRig rig;
    private readonly CobraMotorTuning tuning;
    private static readonly int BASE_COLOR_ID =
        Shader.PropertyToID("_BaseColor");
    private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
    private Bounds referenceHeadBounds;
    private bool hasHeadBounds;

    public CobraMotorCoil(CobraMotorRig rig) {
        this.rig = rig;
        tuning = rig.tuning;
    }

    // Public Methods  #########################################################
    /// <summary>
    /// Clone the last body segment until neighbouring segments sit no more
    /// than <see cref="maxSegmentGapU"/> apart along the whole coil, so the
    /// loop round the trunk has no hole the monkey fits through.
    /// </summary>
    public void FillCoilSegments() {
        int neededCount =
            Mathf.CeilToInt(rig.CoilLengthU / tuning.MaxSegmentGapU) + 1;

        if (neededCount <= rig.bodySegments.Length) {
            return;
        }

        Transform template = rig.bodySegments[rig.bodySegments.Length - 1];

        if (template == null) {
            Debug.LogError(
                "CobraMotor:\tlast body segment is empty, cannot fill coil",
                rig.owner
            );
            return;
        }

        int authoredCount = rig.bodySegments.Length;
        Transform[] filledSegments = new Transform[neededCount];
        rig.bodySegments.CopyTo(filledSegments, 0);

        for (int i = authoredCount; i < neededCount; i++) {
            Transform clone = Object.Instantiate(template, template.parent);
            clone.name = $"Segment_{i:00}";
            filledSegments[i] = clone;
        }

        rig.bodySegments = filledSegments;

        if (Debug.isDebugBuild) {
            Debug.Log(
                $"CobraMotor:\tcloned {neededCount - authoredCount} segments, "
                    + $"{neededCount} in total for {tuning.CoilTurns:F2} loops",
                rig.owner
            );
        }
    }

    /// <summary>
    /// Size every segment into a rounded bead, slightly tapered, flatten the
    /// head into a hood and give the head eyes. Each bead's capsule collider
    /// is stretched to reach its neighbours, so the visible gaps never become
    /// holes in the hit shape. Runs once, the shape along the body never
    /// changes. Colors are left to the segments' material.
    /// </summary>
    public void StyleBody() {
        float arcStep = rig.SegmentArcStepU;
        float segmentLength = arcStep * tuning.SegmentFill;
        MaterialPropertyBlock colorBlock = new();

        for (int i = 0; i < rig.bodySegments.Length; i++) {
            Transform segment = rig.bodySegments[i];

            if (segment == null) {
                continue;
            }

            float bodyFraction = (float)i / rig.LastIndex;
            float thickness =
                tuning.MaxThicknessU * Mathf.Max(
                    0.05f,
                    tuning.ThicknessProfile.Evaluate(bodyFraction)
                );

            // capsule mesh: local y runs along the body, 2 U long at scale 1,
            // local x is width, local z is depth off the trunk
            if (i == 0) {
                segment.localScale = new Vector3(
                    thickness * tuning.HeadWidthRatio,
                    tuning.HeadLengthU * 0.5f,
                    thickness * 0.75f
                );
            } else {
                segment.localScale = new Vector3(
                    thickness,
                    segmentLength * 0.5f,
                    thickness
                );
            }

            StretchHitShape(segment, arcStep);
        }

        if (tuning.CartoonAppearance.enabled && rig.bodySegments[0] != null
            && rig.bodySegments[0].TryGetComponent(out Renderer source)) {
            rig.cartoonVisual = new CartoonCobraVisual(rig.host,
                source.sharedMaterial, tuning.CartoonAppearance);
            rig.cartoonVisual.BuildHead(rig.bodySegments[0],
                tuning.MaxThicknessU);
            rig.cartoonRadii = new float[rig.bodySegments.Length];
            rig.cartoonOutward = new Vector3[rig.bodySegments.Length];
            for (int i = 0; i < rig.bodySegments.Length; i++) {
                if (rig.bodySegments[i]
                    .TryGetComponent(out Renderer renderer)) {
                    renderer.enabled = false;
                }
                float t = i / (float)rig.LastIndex;
                rig.cartoonRadii[i] = tuning.MaxThicknessU * 0.5f
                    * Mathf.Max(0.05f, tuning.ThicknessProfile.Evaluate(t))
                    * (1f - Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(0.8f, 1f, t)));
            }
        } else if (tuning.EyeSizeU > 0f && rig.bodySegments[0] != null) {
            AddEyes(rig.bodySegments[0], colorBlock);
        }
    }

    /// <summary>
    /// Make a segment's capsule collider run along the body and span a bit
    /// more than the spacing to the next segment, whatever its visible size.
    /// </summary>
    public static void StretchHitShape(Transform segment, float arcStep) {
        if (!segment.TryGetComponent(out CapsuleCollider hitShape)) {
            return;
        }

        float lengthScale = Mathf.Max(0.0001f, segment.localScale.y);
        hitShape.direction = 1;
        hitShape.center = Vector3.zero;
        hitShape.height = Mathf.Max(
            2f * hitShape.radius,
            arcStep * 1.1f / lengthScale
        );
    }

    public void AddEyes(Transform head, MaterialPropertyBlock colorBlock) {
        Vector3 headScale = head.localScale;
        Renderer headRenderer = head.GetComponent<Renderer>();

        for (int side = -1; side <= 1; side += 2) {
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = side < 0 ? "EyeLeft" : "EyeRight";

            // eyes are looks only, never part of the hit shape
            Object.Destroy(eye.GetComponent<Collider>());

            eye.transform.SetParent(head, false);

            // head-local: toward the front (+y), off to each side (x), on the
            // face turned away from the trunk (-z)
            eye.transform.localPosition = new Vector3(
                side * 0.32f,
                0.55f,
                -0.3f
            );
            eye.transform.localScale = new Vector3(
                tuning.EyeSizeU / headScale.x,
                tuning.EyeSizeU / headScale.y,
                tuning.EyeSizeU / headScale.z
            );

            Renderer eyeRenderer = eye.GetComponent<Renderer>();

            if (headRenderer != null) {
                eyeRenderer.sharedMaterial = headRenderer.sharedMaterial;
            }

            SetColor(eyeRenderer, Color.black, colorBlock);
        }
    }

    public static void SetColor(
        Renderer target,
        Color color,
        MaterialPropertyBlock colorBlock
    ) {
        if (target == null || target.sharedMaterial == null) {
            return;
        }

        Material material = target.sharedMaterial;
        target.GetPropertyBlock(colorBlock);

        if (material.HasProperty(BASE_COLOR_ID)) {
            colorBlock.SetColor(BASE_COLOR_ID, color);
        } else if (material.HasProperty(COLOR_ID)) {
            colorBlock.SetColor(COLOR_ID, color);
        } else {
            return;
        }

        target.SetPropertyBlock(colorBlock);
        colorBlock.Clear();
    }

    public void UpdateBody() {
        float angleStep = rig.SegmentAngleStepDeg;
        float heightStep = rig.SegmentHeightStepU;
        float arcStep = rig.SegmentArcStepU;
        // time spent frozen is skipped, so the wave resumes where it stopped
        float wavePhase =
            (Time.time - rig.frozenTimeS) * tuning.WaveSpeedHz * 2f * Mathf.PI;

        // the coil sits lower by the neck lift, so the raised head tip is
        // what reaches headHeight
        float coilTopHeight = rig.headHeight - tuning.NeckLiftU;

        for (int i = 0; i < rig.bodySegments.Length; i++) {
            float bodyDistance = i * arcStep;
            float neckFraction =
                Mathf.Clamp01(bodyDistance / tuning.NeckLengthU);

            float lift =
                tuning.NeckLiftU * rig.Chase.ChaseHeadRaiseBlend()
                    * (1f - neckFraction)
                    * (1f - neckFraction);
            float wave = tuning.WaveAmplitudeU * neckFraction * Mathf.Sin(
                wavePhase - bodyDistance / tuning.WaveLengthU * 2f * Mathf.PI
            );

            rig.segmentPositions[i] = rig.GetPathPosition(
                rig.headAngle - rig.Direction * angleStep * i,
                coilTopHeight - heightStep * i + lift + wave
            );
        }
        rig.Guard.ApplyGuardPeek(arcStep);

        for (int i = 0; i < rig.bodySegments.Length; i++) {
            Transform segment = rig.bodySegments[i];

            if (segment == null) {
                continue;
            }

            segment.position = rig.segmentPositions[i];

            if (rig.bodySegments.Length < 2) {
                continue;
            }

            // lay the capsule along the body, pointing toward the head
            Vector3 alongBody = i == 0
                ? rig.segmentPositions[0] - rig.segmentPositions[1]
                : rig.segmentPositions[i - 1] - rig.segmentPositions[i];
            Vector3 awayFromTrunk =
                rig.segmentPositions[i] - tuning.PathCenter.position;
            awayFromTrunk.y = 0f;

            if (alongBody.sqrMagnitude < 0.0001f
                || awayFromTrunk.sqrMagnitude < 0.0001f) {
                continue;
            }

            segment.rotation =
                Quaternion.LookRotation(alongBody, awayFromTrunk)
                * Quaternion.Euler(90f, 0f, 0f);
        }
    }

    public void PoseCartoon() {
        if (rig.cartoonVisual == null) {
            return;
        }
        for (int i = 0; i < rig.segmentPositions.Length; i++) {
            Vector3 radial =
                rig.segmentPositions[i] - tuning.PathCenter.position;
            radial.y = 0f;
            rig.cartoonOutward[i] = radial.normalized;
        }
        Vector3 travelDirection =
            rig.segmentPositions[0] - rig.segmentPositions[1];
        if (!rig.isGuarding) {
            travelDirection.y = 0f;
        }
        travelDirection = Vector3.ProjectOnPlane(travelDirection,
            rig.cartoonOutward[0]).normalized;
        if (travelDirection.sqrMagnitude < 0.001f) travelDirection =
            Vector3.Cross(rig.cartoonOutward[0], Vector3.up) * rig.Direction;
        Vector3 neckDirection = travelDirection;
        if (!rig.isGuarding && rig.player != null) {
            Vector3 toPlayer = rig.player.position - rig.segmentPositions[0];
            Vector3 towardPlayer = Vector3.ProjectOnPlane(toPlayer,
                rig.cartoonOutward[0]);
            if (towardPlayer.sqrMagnitude > 0.001f) {
                neckDirection =
                    Vector3.Slerp(travelDirection, towardPlayer.normalized,
                        rig.Chase.ChaseHeadRaiseBlend()).normalized;
            }
        }
        Quaternion desiredHeadRotation =
            Quaternion.LookRotation(rig.cartoonOutward[0], neckDirection);
        if (!rig.cartoonHeadRotationInitialized) {
            rig.cartoonHeadRotation = desiredHeadRotation;
            rig.cartoonHeadRotationInitialized = true;
        } else if (!rig.isFrozen) {
            Quaternion followedRotation =
                Quaternion.Slerp(rig.cartoonHeadRotation,
                desiredHeadRotation,
                1f - Mathf.Exp(-tuning.HeadFollowSharpness * Time.deltaTime));
            rig.cartoonHeadRotation =
                Quaternion.RotateTowards(rig.cartoonHeadRotation,
                followedRotation,
                tuning.HeadTurnSpeedDegrees * Time.deltaTime);
        }
        Quaternion headRotation = rig.cartoonHeadRotation;
        Vector3 headPosition = rig.segmentPositions[0];
        if (tuning.CartoonAppearance.referenceHead != null) {
            if (!hasHeadBounds) {
                // reference head never changes; read its mesh once
                referenceHeadBounds =
                    tuning.CartoonAppearance.referenceHead
                        .GetComponent<MeshFilter>().sharedMesh.bounds;
                hasHeadBounds = true;
            }
            Bounds headBounds = referenceHeadBounds;
            float headSize =
                tuning.MaxThicknessU * tuning.CartoonAppearance.headScale;
            float innerProjection = 0f;
            for (int corner = 0; corner < 8; corner++) {
                Vector3 point =
                    headBounds.center + Vector3.Scale(headBounds.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f,
                        (corner & 2) == 0 ? -1f : 1f,
                        (corner & 4) == 0 ? -1f : 1f));
                innerProjection = Mathf.Min(innerProjection,
                    Vector3.Dot(headRotation * (point * headSize),
                        rig.cartoonOutward[0]));
            }
            // Keep the inward-facing muzzle clear of the trunk while the neck
            // stays upright.
            headPosition += rig.cartoonOutward[0]
                * Mathf.Max(0f, -innerProjection - tuning.MaxThicknessU * 0.5f);
        } else {
            headPosition += rig.cartoonOutward[0] * tuning.MaxThicknessU * 0.6f
                + Vector3.up * tuning.MaxThicknessU * 0.2f;
        }
        rig.cartoonVisual.PoseHead(headPosition, headRotation);
        rig.Hiss.UpdateProximityHiss(headPosition);
        rig.cartoonVisual.UpdateBody(rig.segmentPositions, rig.cartoonRadii,
            rig.cartoonOutward,
            bellyInward: true);
    }

}
