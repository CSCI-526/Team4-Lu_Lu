using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prefab root for one dynamic tree segment. Procedurally decorates itself
/// with branch prefabs, one per <see cref="BranchKind"/> (empty, banana,
/// coconut, pineapple), each spread by its own density curve, on creation,
/// and again each time
/// <see cref="Restart"/> moves it to a new place, raising
/// <see cref="BranchesGenerated"/> after each roll.
/// </summary>
public class TreeSegmentController: MonoBehaviour {
    // Public Members  #########################################################
    /// <summary>
    /// Raised with the segment each time it has rolled its branches, on its
    /// first Start and after every <see cref="Restart"/>, so decorators that
    /// must keep clear of branches, e.g. <see cref="SpiderWebSpawner"/>, add
    /// theirs then, whoever created or moved the segment.
    /// </summary>
    // System qualified: a using System would make Random ambiguous here
    public static event System.Action<TreeSegmentController>
        BranchesGenerated;

    // Public Methods  #########################################################
    /// <returns>the segment's world-space height, read from its Collider
    /// bounds</returns>
    public float GetHeight() {
        return segmentCollider.bounds.size.y;
    }

    /// <summary>
    /// Moves the segment to <paramref name="worldPosition"/> and starts it
    /// over: everything spawned onto it (branches, webs) is removed and the
    /// branches are scattered again for the new height. Decorators listening to
    /// <see cref="BranchesGenerated"/>, e.g. <see cref="SpiderWebSpawner"/>,
    /// redo theirs then.
    /// </summary>
    public void Restart(Vector3 worldPosition) {
        if (!isReady) {
            return;
        }

        ClearDecorations();
        transform.position = worldPosition;
        // the collider bounds read for placement must follow the move
        Physics.SyncTransforms();

        if (TreeManager.I == null) {
            Debug.LogWarning(
                "TreeSegmentController:\tno TreeManager in scene, "
                    + "skip branch generation",
                this
            );
        } else {
            GenerateBranches();
        }
        hasGenerated = true;
    }

    // Inspector Fields  #######################################################
    [SerializeField]
    [Tooltip("collider measuring segment height")]
    private Collider segmentCollider;

    [SerializeField]
    [Tooltip("branch prefab of the empty kind, bearing no fruit")]
    private GameObject branchEmptyPrefab;

    [SerializeField]
    [Tooltip("branch prefab of the banana kind")]
    private GameObject branchBananaPrefab;

    [SerializeField]
    [Tooltip("branch prefab of the coconut kind")]
    private GameObject branchCoconutPrefab;

    [SerializeField]
    [Tooltip("branch prefab of the pineapple kind")]
    private GameObject branchPineapplePrefab;

    [SerializeField]
    [Tooltip("shared branch placement tuning")]
    private BranchFruitPlacementConfig placementConfig;

    [SerializeField]
    [Tooltip("world balance tuning; branch density")]
    private WorldBalanceConfig worldBalanceConfig;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (segmentCollider == null) {
            Debug.LogWarning(
                "TreeSegmentController:\t"
                    + "must assign Inspector Field: segmentCollider",
                this
            );
        }
        foreach (BranchKind kind in ROLL_ORDER) {
            if (GetBranchPrefab(kind) == null) {
                Debug.LogWarning(
                    "TreeSegmentController:\tmust assign the branch prefab "
                        + "Inspector Field of kind " + kind,
                    this
                );
                hasAllBranchPrefabs = false;
            }
        }

        if (placementConfig == null) {
            Debug.LogWarning(
                "TreeSegmentController:\t"
                    + "must assign Inspector Field: placementConfig",
                this
            );
        }

        if (worldBalanceConfig == null) {
            Debug.LogWarning(
                "TreeSegmentController:\t"
                    + "must assign Inspector Field: worldBalanceConfig",
                this
            );
        }

        if (
            segmentCollider == null
            || !hasAllBranchPrefabs
            || placementConfig == null
            || worldBalanceConfig == null
        ) {
            enabled = false;
            return;
        }

        // whatever sits under the root now is part of the prefab, keep it
        // through every restart
        foreach (Transform child in transform) {
            prefabChildren.Add(child);
        }
        isReady = true;
    }

    // wait for Start, so TreeManager.I is set whatever the Awake order
    private void Start() {
        if (!enabled || hasGenerated) {
            return;
        }
        hasGenerated = true;
    private void OnEnable() {
        liveSegments.Add(this);
    }

    private void OnDisable() {
        liveSegments.Remove(this);
    }

        if (TreeManager.I == null) {
            Debug.LogWarning(
                "TreeSegmentController:\tno TreeManager in scene, "
                    + "skip branch generation",
                this
            );
            return;
        }
        GenerateBranches();
    }

    // constants  ##############################################################
    // rarest first, so the common empty branches never crowd a pineapple or
    // coconut out of its spot
    private static readonly BranchKind[] ROLL_ORDER = {
        BranchKind.Pineapple,
        BranchKind.Coconut,
        BranchKind.Banana,
        BranchKind.Empty
    };

    // private members  ########################################################
    private bool hasAllBranchPrefabs = true; // if every kind has its prefab
    private readonly HashSet<Transform> prefabChildren = new();
    private bool isReady; // if Awake passed the Inspector guard
    private bool hasGenerated; // if branches were rolled once already

    // private methods  ########################################################
    // inactive first, Destroy only lands at the end of the frame and the
    // dead branches must not block physics queries on the restart frame
    private void ClearDecorations() {
        for (int i = transform.childCount - 1; i >= 0; i--) {
    private readonly List<ScatterItem> scatterItems = new();

    // every enabled segment, so branches keep clear across joints
    private static readonly List<TreeSegmentController> liveSegments =
        new();
            Transform child = transform.GetChild(i);
            if (prefabChildren.Contains(child)) {
                continue;
            }
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    private void GenerateBranches() {
        float segmentHeight = GetHeight();
        float bottomY = segmentCollider.bounds.min.y;
        float topY = segmentCollider.bounds.max.y;

        // live branches of the other segments count for separation too, so
        // a joint between segments is no seam
        List<Vector3> placedPositions = GetNeighbourBranchPositions();

        // the fixed-height branches go first, so nothing crowds them out
        foreach (BranchKind kind in ROLL_ORDER) {
            float guaranteedY = GetGuaranteedWorldY(kind);
            if (
                !float.IsNaN(guaranteedY)
                && guaranteedY >= bottomY
                && guaranteedY < topY
            ) {
                SpawnGuaranteedBranch(
                    kind,
                    segmentHeight,
                    placedPositions,
                    transform.InverseTransformPoint(
                        new Vector3(0f, guaranteedY, 0f)
                    ).y,
                    GetMonkeyYaw()
                );
            }
        }

        // each kind is its own scatter stream: the spots whose world height
        // falls in this segment, from the density curve read at that height
        if (TrunkScatter.I == null) {
            Debug.LogWarning(
                "TreeSegmentController:\tno TrunkScatter in scene, "
                    + "skip scattered branches",
                this
            );
        } else {
            foreach (BranchKind kind in ROLL_ORDER) {
                BranchKind streamKind = kind;
                scatterItems.Clear();
                TrunkScatter.I.GetItems(
                    GetScatterChannel(streamKind),
                    bottomY,
                    topY,
                    y => GetBranchDensity(streamKind, y),
                    scatterItems
                );
                foreach (ScatterItem item in scatterItems) {
                    SpawnScatteredBranch(
                        streamKind,
                        item,
                        segmentHeight,
                        placedPositions
                    );
                }
            }
        }

        // branches are in and posed, decorators may now keep clear of them
        BranchesGenerated?.Invoke(this);
    }

    // expected branches of the kind per 10 u of trunk at a world height; in
    // the starting zone only the banana kind grows, at a flat density
    private float GetBranchDensity(BranchKind kind, float worldY) {
        if (IsInStartingZone(worldY)) {
            return kind == BranchKind.Banana
                ? worldBalanceConfig.StartingZoneBananaBranchDensity
                : 0f;
        }
        AnimationCurve curve = worldBalanceConfig.GetBranchDensityCurve(kind);
        return curve != null
            ? Mathf.Max(0f, curve.Evaluate(GetRampedDifficulty(worldY)))
            : 0f;
    }

    private GameObject GetBranchPrefab(BranchKind kind) {
        return kind switch {
            BranchKind.Empty => branchEmptyPrefab,
            BranchKind.Banana => branchBananaPrefab,
            BranchKind.Coconut => branchCoconutPrefab,
            BranchKind.Pineapple => branchPineapplePrefab,
    private static ScatterChannel GetScatterChannel(BranchKind kind) {
        return kind switch {
            BranchKind.Banana => ScatterChannel.BananaBranch,
            BranchKind.Coconut => ScatterChannel.CoconutBranch,
            BranchKind.Pineapple => ScatterChannel.PineappleBranch,
            _ => ScatterChannel.EmptyBranch
        };
    }

    // segment-local spots of the live branches on the other segments near
    // enough to crowd this one
    private List<Vector3> GetNeighbourBranchPositions() {
        List<Vector3> positions = new();
        float reachY = placementConfig.MinBranchSeparation;
        Bounds own = segmentCollider.bounds;
        foreach (TreeSegmentController other in liveSegments) {
            if (other == this || !other.isReady) {
                continue;
            }
            Bounds bounds = other.segmentCollider.bounds;
            if (
                bounds.min.y > own.max.y + reachY
                || bounds.max.y < own.min.y - reachY
            ) {
                continue;
            }
            for (int i = 0; i < other.transform.childCount; i++) {
                Transform child = other.transform.GetChild(i);
                if (
                    child.gameObject.activeSelf
                    && child.GetComponent<BranchController>() != null
                ) {
                    positions.Add(
                        transform.InverseTransformPoint(child.position)
                    );
                }
            }
        }
        return positions;
    }

            _ => null
        };
    }

    // the monkey's angle in deg around the trunk, in the segment's local
    // frame as a branch's yaw reads it; NaN w/o a monkey or when it stands on
    // the axis, so the branch yaw stays random
    private float GetMonkeyYaw() {
        if (
            RampedDifficultyManager.I == null
            || RampedDifficultyManager.I.PlayerTransform == null
        ) {
            return float.NaN;
        }

        Vector3 local = transform.InverseTransformPoint(
            RampedDifficultyManager.I.PlayerTransform.position
        );
        if (local.x * local.x + local.z * local.z <= Mathf.Epsilon) {
            return float.NaN;
        }
        return Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
    }

    // world y of the kind's guaranteed branch, NaN when it has none
    private float GetGuaranteedWorldY(BranchKind kind) {
        float climbedY = worldBalanceConfig.GetGuaranteedBranchHeightU(kind);
        if (climbedY < 0f || RampedDifficultyManager.I == null) {
            return float.NaN;
        }
        return RampedDifficultyManager.I.PlayerGameStartYPosition + climbedY;
    }

    // a branch at a fixed segment-local height, yawed near centerYaw within
    // the guaranteed branch spread either way, NaN for any yaw
    private void SpawnGuaranteedBranch(
        BranchKind kind,
        float segmentHeight,
        List<Vector3> placedPositions,
        float localHeight,
        float centerYaw
    ) {
        float spreadDeg = worldBalanceConfig.GuaranteedBranchYawSpreadDeg;
        TryPlaceBranch(
            kind,
            segmentHeight,
            placedPositions,
            localHeight,
            _ => float.IsNaN(centerYaw)
                ? Random.Range(0f, 360f)
                : centerYaw + Random.Range(-spreadDeg, spreadDeg),
            placementConfig.MaxPlacementAttempts,
            true
        );
    }

    // a scatter spot keeps its height; a blocked one retries yaw in golden
    // steps, then is skipped
    private void SpawnScatteredBranch(
        BranchKind kind,
        ScatterItem item,
        float segmentHeight,
        List<Vector3> placedPositions
    ) {
        TryPlaceBranch(
            kind,
            segmentHeight,
            placedPositions,
            transform.InverseTransformPoint(new Vector3(0f, item.Y, 0f)).y,
            item.GetRetryYawDeg,
            TrunkScatter.I.MaxRetries + 1,
            false
        );
    }

    // yawAtAttempt: angle in deg around the trunk to try on each attempt;
    // isCloseAccepted: whether a spot too near another is taken once the
    // attempts run out, else the branch is skipped
    private void TryPlaceBranch(
        BranchKind kind,
        float segmentHeight,
        List<Vector3> placedPositions,
        float localHeight,
        System.Func<int, float> yawAtAttempt,
        int maxAttempts,
        bool isCloseAccepted
    ) {
        Quaternion branchRotation = Quaternion.identity;
        Vector3 branchPosition = Vector3.zero;

        bool isClear = false;
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            branchRotation = Quaternion.Euler(0f, yawAtAttempt(attempt), 0f);
            branchPosition =
                branchRotation * Vector3.forward * placementConfig.BranchRadiusU
                + Vector3.up * localHeight;

            // a spot in the boss zone is never clear: only the fight grows
            // anything there
            isClear =
                IsClearOfMonkey(branchPosition)
                && !IsInBossZone(branchPosition);
            if (
                isClear
                && IsFarEnoughFromExisting(branchPosition, placedPositions)
            ) {
                break;
            }
        }

        // no spot clear of the monkey and the others: skip
        if (!isClear) {
            return;
        }
            isClear = isClear && isCloseAccepted;

        placedPositions.Add(branchPosition);

        GameObject branch = Instantiate(GetBranchPrefab(kind), transform);
        branch.transform.SetLocalPositionAndRotation(
            branchPosition,
            branchRotation
        );

        BranchController branchRoot =
            branch.GetComponent<BranchController>();
        if (branchRoot == null) {
            Debug.LogWarning(
                "TreeSegmentController:\t"
                    + kind + " branch prefab lacks BranchController",
                this
            );
        } else {
            branchRoot.Setup();
            KeepFruitAboveStart(branch, branchRoot, segmentHeight,
                placedPositions);
        }
    }

    // a fruit never hangs below the monkey's start, where it only confuses:
    // its branch is lifted till the fruit's lowest point is level w/ the
    // start, or dropped if that leaves the segment or crowds the monkey
    private void KeepFruitAboveStart(GameObject branch,
        BranchController branchRoot, float segmentHeight,
        List<Vector3> placedPositions) {
        float bottomY = branchRoot.FruitBottomY;
        if (float.IsNaN(bottomY) || RampedDifficultyManager.I == null) {
            return;
        }
        float startY = RampedDifficultyManager.I.PlayerGameStartYPosition
            + FRUIT_ABOVE_START_U;
        if (bottomY >= startY) {
            return;
        }
        Vector3 lifted = branch.transform.localPosition
            + transform.InverseTransformVector(Vector3.up * (startY - bottomY));
        bool isKept = transform.TransformPoint(lifted).y
                < segmentCollider.bounds.max.y
            && IsClearOfMonkey(lifted) && !IsInBossZone(lifted);
        placedPositions.RemoveAt(placedPositions.Count - 1);
        if (!isKept) {
            // off at once, so the decorators that run this frame never see it
            branch.SetActive(false);
            Destroy(branch);
            return;
        }
        branch.transform.localPosition = lifted;
        placedPositions.Add(lifted);
    }

    // how far above the monkey's start a fruit's lowest point must be; u
    private const float FRUIT_ABOVE_START_U = 0f;

    // iff the y lies below the starting zone exit; false w/o a service
    private bool IsInStartingZone(float worldY) {
        return RampedDifficultyManager.I != null
            && RampedDifficultyManager.I.IsInStartingZoneY(worldY);
    }

    // 0 when no service in scene
    private float GetRampedDifficulty(float worldY) {
        return RampedDifficultyManager.I != null
            ? RampedDifficultyManager.I.GetRampedValueFromYPosition(worldY)
            : 0f;
    }

    // iff the segment-local spot lies in the boss zone; false w/o a service
    private bool IsInBossZone(Vector3 localPosition) {
        return RampedDifficultyManager.I != null
            && RampedDifficultyManager.I.IsInBossZoneY(
                transform.TransformPoint(localPosition).y
            );
    }

    // iff the branch keeps the starting zone clearance from the monkey; always
    // true once the run is playing or w/o a monkey or controller
    private bool IsClearOfMonkey(Vector3 localPosition) {
        float clearRadius = worldBalanceConfig.StartingZoneBranchClearRadiusU;
        if (
            clearRadius <= 0f
            || RunController.I == null
            || !RunController.I.IsBeforePlaying
            || RampedDifficultyManager.I == null
            || RampedDifficultyManager.I.PlayerTransform == null
        ) {
            return true;
        }

        Vector3 worldPosition = transform.TransformPoint(localPosition);
        Vector3 monkeyPosition =
            RampedDifficultyManager.I.PlayerTransform.position;
        return Vector3.Distance(worldPosition, monkeyPosition) >= clearRadius;
    }

    private bool IsFarEnoughFromExisting(
        Vector3 candidate,
        List<Vector3> placedPositions
    ) {
        foreach (Vector3 placed in placedPositions) {
            if (
                Vector3.Distance(candidate, placed)
                < placementConfig.MinBranchSeparation
            ) {
                return false;
            }
        }
        return true;
    }
}
