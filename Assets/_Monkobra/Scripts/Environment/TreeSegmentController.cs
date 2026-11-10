using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prefab root for one dynamic tree segment. Procedurally decorates itself
/// with Branch With Fruit prefabs on creation, and again each time
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
    /// branches are rolled again for the new height. Decorators listening to
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
    [Tooltip("branch prefab spawned on segment")]
    private GameObject branchWithFruitPrefab;

    [SerializeField]
    [Tooltip("shared branch placement tuning")]
    private BranchFruitPlacementConfig placementConfig;

    [SerializeField]
    [Tooltip("world balance tuning; branch attempts & chance")]
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
        if (branchWithFruitPrefab == null) {
            Debug.LogWarning(
                "TreeSegmentController:\t"
                    + "must assign Inspector Field: branchWithFruitPrefab",
                this
            );
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
                "TreeSegmentController:	"
                    + "must assign Inspector Field: worldBalanceConfig",
                this
            );
        }

        if (
            segmentCollider == null
            || branchWithFruitPrefab == null
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

    // private members  ########################################################
    private readonly HashSet<Transform> prefabChildren = new();
    private bool isReady; // if Awake passed the Inspector guard
    private bool hasGenerated; // if branches were rolled once already

    // private methods  ########################################################
    // inactive first, Destroy only lands at the end of the frame and the
    // dead branches must not block physics queries on the restart frame
    private void ClearDecorations() {
        for (int i = transform.childCount - 1; i >= 0; i--) {
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
        List<Vector3> placedPositions = new();

        // fixed attempt count, each attempt spawns a branch by the chance the
        // curve gives at this segment's ramped difficulty, or by the flat
        // starting zone chance when the segment's middle lies in the zone
        float centerY = segmentCollider.bounds.center.y;
        float spawnProbability = IsInStartingZone(centerY)
            ? worldBalanceConfig.StartingZoneBranchChance
            : Mathf.Clamp01(
                worldBalanceConfig.BranchSpawnProbabilityCurve.Evaluate(
                    GetRampedDifficulty(centerY)
                )
            );

        int attemptCount = worldBalanceConfig.BranchGenerationAttemptCount;
        for (int i = 0; i < attemptCount; i++) {
            if (Random.value < spawnProbability) {
                SpawnBranch(segmentHeight, placedPositions);
            }
        }

        // branches are in and posed, decorators may now keep clear of them
        BranchesGenerated?.Invoke(this);
    }

    private void SpawnBranch(float segmentHeight, List<Vector3> placedPositions) {
        Quaternion branchRotation = Quaternion.identity;
        Vector3 branchPosition = Vector3.zero;

        int maxAttempts = placementConfig.MaxPlacementAttempts;
        bool isClear = false;
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            branchRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            float branchHeight = Random.Range(0f, segmentHeight);
            branchPosition =
                branchRotation * Vector3.forward * placementConfig.BranchRadius
                + Vector3.up * branchHeight;

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

        // no spot clear of the monkey: skip, a branch there is a bad spawn
        if (!isClear) {
            return;
        }

        placedPositions.Add(branchPosition);

        GameObject branch = Instantiate(branchWithFruitPrefab, transform);
        branch.transform.SetLocalPositionAndRotation(
            branchPosition,
            branchRotation
        );

        // fruit chances follow the difficulty at the branch's own height
        float rampedDifficulty =
            GetRampedDifficulty(branch.transform.position.y);
        float bananaProbability = IsInStartingZone(branch.transform.position.y)
            ? worldBalanceConfig.StartingZoneBananaChance
            : Mathf.Clamp01(
                worldBalanceConfig.BananaSpawnProbabilityCurve.Evaluate(
                    rampedDifficulty
                )
            );
        float pineappleProbability = Mathf.Clamp01(
            worldBalanceConfig.PineappleSpawnProbabilityCurve.Evaluate(
                rampedDifficulty
            )
        );
        BranchController branchRoot =
            branch.GetComponent<BranchController>();
        if (branchRoot == null) {
            Debug.LogWarning(
                "TreeSegmentController:\t"
                    + "branchWithFruitPrefab lacks BranchController",
                this
            );
        } else {
            branchRoot.Initialize(bananaProbability, pineappleProbability);
        }
    }

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
            || GameController.I == null
            || !GameController.I.IsBeforePlaying
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
