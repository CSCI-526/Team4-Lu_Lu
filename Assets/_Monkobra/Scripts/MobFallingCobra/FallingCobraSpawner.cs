using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Perches dormant <see cref="FallingCobraController"/>s on the trunk of each
/// <see cref="TreeSegmentController"/> as soon as it has rolled its
/// branches, on its first Start and after every restart by
/// <see cref="DynamicTreeSegmentPool"/>, by listening to
/// <see cref="TreeSegmentController.BranchesGenerated"/>. Put one on
/// any object active from scene load, never under the pool's mock tree,
/// which is deactivated at runtime.
/// <para>
/// Spaced like branches by <see cref="TrunkScatter"/>, on its FallingCobra
/// channel: the spots whose height falls in the segment, from the falling
/// cobra density curve of <see cref="CobraBalanceConfig"/> read at each
/// height's ramped difficulty. Each cobra is a child of the segment its
/// branch grows on, so that segment's restart clear removes it.
/// </para>
/// <para>
/// A cobra wraps its resting body around the live branch nearest its spot,
/// within a vertical search window, on any segment. No branch near means
/// no cobra. Branches too close to another cobra are retried, nearest
/// first, then skipped.
/// </para>
/// <para>
/// Ambush: with <see cref="CobraBalanceConfig.FallingCobraBananaAmbushChance"/>
/// the cobra prefers a fruit branch, so reaching for that fruit is what
/// wakes it. The choice is hashed from the spot, never rolled. If no fruit
/// branch is near, or none fits, the cobra falls back to any branch.
/// </para>
/// </summary>
public class FallingCobraSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [FormerlySerializedAs("fallingCobraPrefab")]
    [Tooltip("falling cobra prefab, spawned dormant on a segment")]
    private FallingCobraController cobraPrefab;

    [SerializeField]
    [Tooltip("cobra balance tuning; cobra density & speeds")]
    private CobraBalanceConfig cobraBalanceConfig;

    [Header("Cobra Position")]
    [SerializeField]
    [Tooltip(
        "ring radius about the trunk axis the cobra hangs and slides on, "
            + "about the monkey's orbit radius; u"
    )]
    private float spawnRadiusU = 5.5f;

    [SerializeField]
    [Tooltip("min distance b/t cobra pivots on one segment; u")]
    private float minCobraSeparationU = 4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip(
        "a spot looks for a branch this far above and below its height; u"
    )]
    private float searchWindowU = 6f;

    [Header("Banana Ambush")]
    [SerializeField]
    [Tooltip("lowest height of an ambush cobra's pivot above its banana; u")]
    private float ambushMinAboveBananaU = 3f;

    [SerializeField]
    [Tooltip("highest height of an ambush cobra's pivot above its banana; u")]
    private float ambushMaxAboveBananaU = 6f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (cobraPrefab == null) {
            Debug.LogWarning(
                "FallingCobraSpawner:\tmust assign Inspector Field: "
                    + "cobraPrefab",
                this
            );
        }
        if (cobraBalanceConfig == null) {
            Debug.LogWarning(
                "FallingCobraSpawner:\tmust assign Inspector Field: "
                    + "cobraBalanceConfig",
                this
            );
        }
        if (cobraPrefab == null || cobraBalanceConfig == null) {
            enabled = false;
        }
    }

    // scene OnEnables all run b4 any Start, so no segment rolls unheard.
    // First wins: a 2nd listener would roll every segment twice
    private void OnEnable() {
        if (listener != null && listener != this) {
            Debug.LogWarning(
                "FallingCobraSpawner:\tanother spawner already listens, this "
                    + "one stays idle",
                this
            );
            return;
        }
        listener = this;
        // before any segment rolls, so the 1st cobra finds the head cached
        cobraPrefab.PrewarmHeadTemplate();
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
        spawnRadiusU = Mathf.Max(0f, spawnRadiusU);
        minCobraSeparationU = Mathf.Max(0f, minCobraSeparationU);
        searchWindowU = Mathf.Max(0f, searchWindowU);
        ambushMinAboveBananaU = Mathf.Max(0f, ambushMinAboveBananaU);
        ambushMaxAboveBananaU = Mathf.Max(
            ambushMinAboveBananaU,
            ambushMaxAboveBananaU
        );
    }

    // Event Handlers  #########################################################
    private void OnBranchesGenerated(TreeSegmentController segment) {
        PopulateSegment(segment);
    }

    // constants  ##############################################################
    private const string FRUIT_TAG = "FruitCollider";

    // private members  ########################################################
    private static FallingCobraSpawner listener; // the one spawner subscribed

    private bool hasWarnedNoDifficulty; // missing service logged once
    private readonly List<FallingCobraController> spawnedCobras = new();
    private readonly List<ScatterItem> scatterItems = new();
    private readonly List<BranchController> nearBranches = new();
    private readonly List<(BoxCollider box, float distance)> candidates =
        new();

    // private methods  ########################################################
    // rolls this spawner's cobra attempts for segment, which already holds its
    // branches
    private void PopulateSegment(TreeSegmentController segment) {
        if (segment == null) {
            return;
        }
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            if (!hasWarnedNoDifficulty) {
                Debug.LogWarning(
                    "FallingCobraSpawner:\tno RampedDifficultyManager in "
                        + "scene, skip cobras",
                    this
                );
                hasWarnedNoDifficulty = true;
            }
            return;
        }

        ClearCobras(segment);

        if (TrunkScatter.I == null) {
            Debug.LogWarning(
                "FallingCobraSpawner:\tno TrunkScatter in scene, skip "
                    + "cobras",
                this
            );
            return;
        }

        Bounds bounds = segment.GetBounds();
        scatterItems.Clear();
        TrunkScatter.I.GetItems(
            ScatterChannel.FallingCobra,
            bounds.min.y,
            bounds.max.y,
            GetDensity,
            scatterItems
        );
        foreach (ScatterItem item in scatterItems) {
            TrySpawnCobra(segment, item);
        }
    }

    // expected falling cobras per 10 u of trunk at a world height
    private float GetDensity(float worldY) {
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            return 0f;
        }
        return Mathf.Max(
            0f,
            cobraBalanceConfig.FallingCobraDensityCurve.Evaluate(
                difficulty.GetRampedValueFromYPosition(worldY)
            )
        );
    }

    // a segment's previous cobras go b4 it rolls again: its restart has
    // already cleared them, any still active are destroyed here, so a
    // segment never carries 2 rolls
    private void ClearCobras(TreeSegmentController segment) {
        for (int i = spawnedCobras.Count - 1; i >= 0; i--) {
            FallingCobraController cobra = spawnedCobras[i];
            if (cobra != null && cobra.transform.parent != segment.transform) {
                continue;
            }
            // inactive first, Destroy only lands at the end of the frame
            if (cobra != null && cobra.gameObject.activeSelf) {
                cobra.gameObject.SetActive(false);
                Destroy(cobra.gameObject);
            }
            spawnedCobras.RemoveAt(i);
        }
    }

    // perches a cobra on the live branch nearest the scatter spot, nearest
    // first, retrying farther ones when a spot is taken or in the boss zone
    private bool TrySpawnCobra(
        TreeSegmentController segment,
        ScatterItem item
    ) {
        nearBranches.Clear();
        TreeSegmentController.GetLiveBranches(
            item.Y - searchWindowU,
            item.Y + searchWindowU,
            nearBranches
        );

        // the trunk axis is the same for every segment
        Vector3 axis = segment.transform.position;
        Vector3 wanted = new Vector3(axis.x, item.Y, axis.z)
            + Quaternion.Euler(0f, item.YawDeg, 0f) * Vector3.forward
                * spawnRadiusU;

        // the ambush preference is hashed from the spot, so it never changes
        // with segment order
        bool isAmbush = ToUnit(item.RetrySeed)
            < cobraBalanceConfig.FallingCobraBananaAmbushChance;
        candidates.Clear();
        bool hasFruitCandidate = false;
        foreach (BranchController owner in nearBranches) {
            foreach (
                BoxCollider box
                in owner.GetComponentsInChildren<BoxCollider>()
            ) {
                if (!box.enabled || box.isTrigger
                    || !box.CompareTag("Branch")) continue;
                Vector3 center = box.transform.TransformPoint(box.center);
                candidates.Add((box, Vector3.Distance(center, wanted)));
                if (owner.Kind != BranchKind.Empty) hasFruitCandidate = true;
            }
        }
        // Every waiting snake needs real wood beneath its coil. No branch
        // means no spawn.
        if (candidates.Count == 0) return false;
        if (isAmbush && hasFruitCandidate) {
            candidates.RemoveAll(
                c => c.box.GetComponentInParent<BranchController>().Kind
                    == BranchKind.Empty
            );
        }
        candidates.Sort((x, y) => x.distance.CompareTo(y.distance));

        FallingCobraController cobra = Instantiate(cobraPrefab,
            segment.transform);
        int maxAttempts = Mathf.Min(
            candidates.Count,
            TrunkScatter.I.MaxRetries + 1
        );
        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            BoxCollider branch = candidates[attempt].box;
            // the cobra lives and dies with the segment its branch grows on
            TreeSegmentController owner =
                branch.GetComponentInParent<TreeSegmentController>();
            Transform ownerXfm = owner != null
                ? owner.transform : segment.transform;
            cobra.transform.SetParent(ownerXfm, true);
            Vector3 branchCenter =
                branch.transform.TransformPoint(branch.center);
            Vector3 outward = branchCenter - ownerXfm.position;
            outward.y = 0f; outward.Normalize();
            Vector3 position = new Vector3(ownerXfm.position.x,
                branchCenter.y,
                    ownerXfm.position.z) + outward * spawnRadiusU;
            cobra.transform.SetPositionAndRotation(position,
                ownerXfm.rotation);
            Physics.SyncTransforms();
            cobra.Initialize(cobraBalanceConfig, ownerXfm.position);
            if (!cobra.AttachToBranch(branch)
                || !IsSpotFree(cobra.transform.position)) continue;
            RampedDifficultyManager difficulty = RampedDifficultyManager.I;
            if (difficulty != null
                && (difficulty.IsInBossZoneY(cobra.transform.position.y
                    - cobra.TailBelowPivotU)
                || difficulty.IsInBossZoneY(cobra.transform.position.y)
                || difficulty.IsInBossZoneY(cobra.transform.position.y
                    + cobra.LengthU - cobra.TailBelowPivotU))) continue;
            spawnedCobras.Add(cobra);
            return true;
        }
        cobra.gameObject.SetActive(false);
        Destroy(cobra.gameObject);
        return false;
    }

    // 0~1 from the top 24 bits of a hash seed
    private static float ToUnit(uint seed) {
        return (seed >> 8) * (1f / 16777216f);
    }

    private bool IsSpotFree(Vector3 position) {
        foreach (FallingCobraController other in spawnedCobras) {
            if (
                other != null
                && other.gameObject.activeInHierarchy
                && Vector3.Distance(other.transform.position, position)
                    < minCobraSeparationU
            ) {
                return false;
            }
        }
        return true;
    }
}
