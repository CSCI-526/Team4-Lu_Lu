using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hangs a coconut on a bare branch of each <see cref="TreeSegmentController"/>
/// as soon as it has rolled its branches, on its first Start and after every
/// restart by <see cref="DTSPool"/>, by listening to
/// <see cref="TreeSegmentController.BranchesGenerated"/>. Put one on any
/// object active from scene load, never under the pool's mock tree.
/// <para>
/// Rolled from the Coconut group of <see cref="GameBalanceConfig"/>: at most
/// one coconut per segment, only on branches whose climbed height (the HUD
/// Height) has reached the start height, by one roll against the per segment
/// chance.
/// </para>
/// <para>
/// A coconut hangs where the branch's banana would, on a branch whose banana
/// was rolled off. A candidate is dropped if the coconut's grab volume
/// overlaps another branch, any fruit or a web; out of candidates, the
/// segment gets none.
/// </para>
/// <para>
/// Coconuts are children of their branch, so a restart's clear removes them
/// with it. The spawner also drops a segment's previous coconuts before
/// rolling it again, so none linger and none double up.
/// </para>
/// </summary>
public class CoconutSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip(
        "coconut prefab; its trigger SphereCollider is the volume kept clear "
            + "of other branches, fruit and webs"
    )]
    private ScorePickup coconutPrefab;

    [SerializeField]
    [Tooltip("game balance tuning; coconut start height & chance")]
    private GameBalanceConfig balanceConfig;

    [Header("Placement")]
    [SerializeField]
    [Tooltip(
        "padding around the coconut trigger that must also be free of other "
            + "branches, fruit and webs; u"
    )]
    private float clearanceU = 0.2f;

    [Header("Testing")]
    [SerializeField]
    [Tooltip(
        "replace the balance start height & chance w/ the test values below. "
            + "Tick it b4 Play, only in a dev scene, never in Lv1Scene"
    )]
    private bool useTestOverride = false;

    [SerializeField]
    [Min(0f)]
    [Tooltip("climbed height coconuts start from while testing; u")]
    private float testStartHeightU = 5f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("chance 0~1 that a segment gets a coconut while testing")]
    private float testSpawnChance = 1f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (coconutPrefab == null) {
            Debug.LogWarning(
                "CoconutSpawner:\tmust assign Inspector Field: coconutPrefab",
                this
            );
        }
        if (balanceConfig == null) {
            Debug.LogWarning(
                "CoconutSpawner:\tmust assign Inspector Field: balanceConfig",
                this
            );
        }
        if (coconutPrefab == null || balanceConfig == null) {
            enabled = false;
            return;
        }

        // the prefab's trigger, scaled by its root, as each coconut carries it
        SphereCollider trigger = coconutPrefab.GetComponent<SphereCollider>();
        if (trigger == null) {
            Debug.LogWarning(
                "CoconutSpawner:\tcoconutPrefab lacks a SphereCollider, "
                    + "clearance uses " + FALLBACK_TRIGGER_RADIUS_U + " u",
                this
            );
            triggerRadiusU = FALLBACK_TRIGGER_RADIUS_U;
        } else {
            Vector3 scale = coconutPrefab.transform.localScale;
            triggerRadiusU = trigger.radius
                * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }

        if (useTestOverride) {
            Debug.LogWarning(
                "CoconutSpawner:\ttest override on, coconuts from "
                    + testStartHeightU + " u at chance " + testSpawnChance,
                this
            );
        }
    }

    // scene OnEnables all run b4 any Start, so no segment rolls unheard.
    // First wins: a 2nd listener would roll every segment twice
    private void OnEnable() {
        if (listener != null && listener != this) {
            Debug.LogWarning(
                "CoconutSpawner:\tanother spawner already listens, this one "
                    + "stays idle",
                this
            );
            return;
        }
        listener = this;
        TreeSegmentController.BranchesGenerated += OnBranchesGenerated;
    }

    private void OnDisable() {
        if (listener != this) {
            return;
        }
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
        listener = null;
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        clearanceU = Mathf.Max(0f, clearanceU);
    }

    // Event Handlers  #########################################################
    private void OnBranchesGenerated(TreeSegmentController segment) {
        PopulateSegment(segment);
    }

    // constants  ##############################################################
    private const string BRANCH_TAG = "Branch";
    private const string FRUIT_TAG = "FruitCollider";
    private const float FALLBACK_TRIGGER_RADIUS_U = 0.8f;

    // private members  ########################################################
    private static CoconutSpawner listener; // the one spawner subscribed

    private float triggerRadiusU; // coconut grab volume, prefab scale applied
    private bool hasWarnedNoDifficulty; // missing service logged once
    private readonly List<ScorePickup> spawnedCoconuts = new();
    private readonly List<BranchController> candidates = new();

    private float StartHeightU => useTestOverride
        ? testStartHeightU
        : balanceConfig.CoconutStartHeightU;

    private float SpawnChance => useTestOverride
        ? testSpawnChance
        : balanceConfig.CoconutSpawnChancePerSegment;

    // private methods  ########################################################
    // rolls this segment's coconut, the segment already holds its branches
    private void PopulateSegment(TreeSegmentController segment) {
        if (segment == null || coconutPrefab == null) {
            return;
        }
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            if (!hasWarnedNoDifficulty) {
                Debug.LogWarning(
                    "CoconutSpawner:\tno RampedDifficultyManager in scene, "
                        + "skip coconuts",
                    this
                );
                hasWarnedNoDifficulty = true;
            }
            return;
        }

        ClearCoconuts(segment);

        CollectCandidates(segment, difficulty);
        if (candidates.Count == 0 || Random.value >= SpawnChance) {
            return;
        }

        // branches were posed through their transforms this frame, push the
        // poses into physics so the overlap test can see them
        Physics.SyncTransforms();

        // random order, the first clear branch gets the coconut
        for (int i = candidates.Count - 1; i >= 0; i--) {
            int pick = Random.Range(0, i + 1);
            BranchController branch = candidates[pick];
            candidates[pick] = candidates[i];
            if (TrySpawnCoconut(branch)) {
                break;
            }
        }
        candidates.Clear();
    }

    // a segment's previous coconuts go b4 it rolls again: its restart has
    // already cleared them with their branches, any still active are
    // destroyed here, so a segment never carries 2 rolls. Grabbed or
    // destroyed coconuts are forgotten on the way
    private void ClearCoconuts(TreeSegmentController segment) {
        for (int i = spawnedCoconuts.Count - 1; i >= 0; i--) {
            ScorePickup coconut = spawnedCoconuts[i];
            if (
                coconut != null
                && !coconut.transform.IsChildOf(segment.transform)
            ) {
                continue;
            }
            // inactive first, Destroy only lands at the end of the frame
            if (coconut != null && coconut.gameObject.activeSelf) {
                coconut.gameObject.SetActive(false);
                Destroy(coconut.gameObject);
            }
            spawnedCoconuts.RemoveAt(i);
        }
    }

    // live branches of the segment w/o a banana, high enough for a coconut
    private void CollectCandidates(
        TreeSegmentController segment,
        RampedDifficultyManager difficulty
    ) {
        candidates.Clear();
        float startHeightU = StartHeightU;
        foreach (
            BranchController branch
            in segment.GetComponentsInChildren<BranchController>()
        ) {
            if (HasBanana(branch)) {
                continue;
            }
            float climbedU = difficulty.GetClimbedYDistance(
                GetHangPosition(branch).y
            );
            if (climbedU >= startHeightU) {
                candidates.Add(branch);
            }
        }
    }

    private bool TrySpawnCoconut(BranchController branch) {
        Vector3 position = GetHangPosition(branch);
        if (!IsSpotClear(position, branch)) {
            return false;
        }

        ScorePickup coconut = Instantiate(
            coconutPrefab,
            position,
            branch.transform.rotation,
            branch.transform
        );
        spawnedCoconuts.Add(coconut);
        return true;
    }

    // the branch's banana pickup, switched off or not, else null
    private static ScorePickup GetBanana(BranchController branch) {
        foreach (
            ScorePickup pickup
            in branch.GetComponentsInChildren<ScorePickup>(true)
        ) {
            if (pickup.Kind == PickupKind.Banana) {
                return pickup;
            }
        }
        return null;
    }

    // a branch bears a banana iff its banana pickup is switched on
    private static bool HasBanana(BranchController branch) {
        ScorePickup banana = GetBanana(branch);
        return banana != null && banana.gameObject.activeSelf;
    }

    // where the branch's banana hangs in the prefab, so the coconut sits on
    // the same spot below the branch; the branch pivot if it has no banana
    private static Vector3 GetHangPosition(BranchController branch) {
        ScorePickup banana = GetBanana(branch);
        return banana != null
            ? banana.transform.position
            : branch.transform.position;
    }

    private bool IsSpotClear(Vector3 position, BranchController ownBranch) {
        // triggers too, fruit grab volumes and webs are ones
        Collider[] hits = Physics.OverlapSphere(
            position,
            triggerRadiusU + clearanceU,
            Physics.AllLayers,
            QueryTriggerInteraction.Collide
        );
        foreach (Collider hit in hits) {
            if (hit.transform.IsChildOf(ownBranch.transform)) {
                continue;
            }
            if (IsBlocker(hit)) {
                return false;
            }
        }
        return true;
    }

    // only other branches, their fruit and webs block a coconut; the trunk,
    // the monkey and anything else never count
    private static bool IsBlocker(Collider hit) {
        return hit.CompareTag(BRANCH_TAG)
            || hit.CompareTag(FRUIT_TAG)
            || hit.GetComponentInParent<BranchController>() != null
            || hit.GetComponentInParent<SpiderWebTrap>() != null;
    }
}
