using System.Collections.Generic;
using UnityEngine;

/// <summary>Creates broad, avoidable rough-bark patches, rolled per segment
/// from the ramped difficulty. Segment ownership handles recycling and
/// cleanup.</summary>
public class RoughBarkSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [Tooltip("source of the spawn curve and the attempt count")]
    private WorldBalanceConfig worldBalanceConfig;

    [SerializeField]
    [Tooltip("material of every patch")]
    private Material barkMaterial;

    [Header("Patch")]
    [SerializeField]
    [Min(0.1f)]
    [Tooltip("trunk radius the patches sit on; u")]
    private float radiusU = 5.05f;

    [SerializeField]
    [Tooltip("patch height range, x min and y max; u")]
    private Vector2 patchHeightRangeU = new(5f, 8.5f);

    [SerializeField]
    [Tooltip("patch arc range round the trunk, x min and y max; deg")]
    private Vector2 patchArcRangeDeg = new(110f, 170f);

    [Header("Placement")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("min vertical gap b/t 2 patches; u")]
    private float patchSeparationU = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("no patch below this much ahead of the player; u")]
    private float spawnAheadU = 10f;

    [SerializeField]
    [Min(1)]
    [Tooltip("tries to place 1 patch before the roll is dropped")]
    private int placementRetries = 16;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("min gap b/t a patch and a branch, web or falling cobra; u")]
    private float obstacleClearanceU = 0.7f;

    // MonoBehaviour Lifecycle  ################################################
    private void Awake() {
        // Inspector Assignment Guard  -----------------------------------------
        if (worldBalanceConfig == null) {
            Debug.LogError(
                "RoughBarkSpawner:\tmust assign Inspector Field: "
                    + "worldBalanceConfig",
                this);
            enabled = false;
        }
        if (barkMaterial == null) {
            Debug.LogError(
                "RoughBarkSpawner:\tmust assign Inspector Field: barkMaterial",
                this);
            enabled = false;
        }
    }

    private void OnEnable() {
        TreeSegmentController.BranchesGenerated += OnBranchesGenerated;
    }

    private void Start() {
        GameObject target = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        player = target != null ? target.transform : null;
        if (player == null) {
            Debug.LogError("RoughBarkSpawner:\tmissing player", this);
            enabled = false;
            return;
        }

        foreach (TreeSegmentController segment in
            FindObjectsByType<TreeSegmentController>(FindObjectsSortMode.None)
        ) {
            segments.Add(segment);
            PopulateSegment(segment);
        }
    }

    private void OnDisable() {
        TreeSegmentController.BranchesGenerated -= OnBranchesGenerated;
    }

    private void OnValidate() {
        patchHeightRangeU.x = Mathf.Max(0.5f, patchHeightRangeU.x);
        patchHeightRangeU.y = Mathf.Max(
            patchHeightRangeU.x, patchHeightRangeU.y);
        patchArcRangeDeg.x = Mathf.Clamp(patchArcRangeDeg.x, 10f, 170f);
        patchArcRangeDeg.y = Mathf.Clamp(
            patchArcRangeDeg.y, patchArcRangeDeg.x, 170f);
    }

    // Event Handlers  #########################################################
    private void OnBranchesGenerated(TreeSegmentController segment) {
        segments.Add(segment);
        PopulateSegment(segment);
    }

    // constants  ##############################################################
    private const string PLAYER_TAG = "Player";

    // private members  ########################################################
    private readonly HashSet<TreeSegmentController> segments = new();
    private readonly List<RoughBarkZone> patches = new();

    // cached references  ------------------------------------------------------
    private Transform player;

    // private methods  ########################################################
    private void PopulateSegment(TreeSegmentController segment) {
        if (
            player == null
            || segment == null
            || !segment.gameObject.activeInHierarchy
        ) {
            return;
        }
        patches.RemoveAll(
            patch => patch == null || !patch.gameObject.activeInHierarchy);
        RampedDifficultyManager difficulty = RampedDifficultyManager.I;
        if (difficulty == null) {
            return;
        }

        float chance = Mathf.Clamp01(
            worldBalanceConfig.RoughBarkSpawnProbabilityCurve.Evaluate(
                difficulty.GetRampedValueFromYPosition(
                    segment.transform.position.y)));
        Physics.SyncTransforms();
        for (int i = 0; i < worldBalanceConfig.RoughBarkAttemptCount; i++) {
            if (Random.value >= chance) {
                continue;
            }
            float height = Random.Range(
                patchHeightRangeU.x, patchHeightRangeU.y);
            float arc = Random.Range(patchArcRangeDeg.x, patchArcRangeDeg.y);
            float extent = RoughBarkZone.GetVerticalExtent(height);
            float half = segment.GetHeight() * 0.5f;
            // The upper end belongs to this segment. A tall patch can
            // extend down across neighboring segments, while its owner
            // stays in the pool until the player has passed the whole area.
            float minY = Mathf.Max(
                segment.transform.position.y - half - extent,
                Mathf.Max(
                    difficulty.RampBaseYPosition,
                    player.position.y + spawnAheadU)
                    + extent);
            float maxY = segment.transform.position.y + half - extent;
            if (maxY <= minY) {
                continue;
            }
            TryPlacePatch(segment, difficulty, minY, maxY, height, arc, extent);
        }
    }

    // tries a few spots on the segment, keeps the first clear one
    private void TryPlacePatch(
        TreeSegmentController segment,
        RampedDifficultyManager difficulty,
        float minY,
        float maxY,
        float height,
        float arc,
        float extent
    ) {
        for (int retry = 0; retry < placementRetries; retry++) {
            Vector3 center = segment.transform.position;
            center.y = Random.Range(minY, maxY);
            float angle = Random.Range(0f, 360f);
            if (IsPatchInBossZone(difficulty, center.y, extent)) {
                continue;
            }
            if (!IsClear(center, angle, extent, arc)) {
                continue;
            }

            GameObject patch = new("RoughBarkPatch");
            patch.transform.SetParent(segment.transform, false);
            patch.transform.position = center;
            RoughBarkZone zone = patch.AddComponent<RoughBarkZone>();
            zone.Initialize(
                radiusU, height, angle, arc, barkMaterial, true,
                Random.Range(0, 100000));
            patches.Add(zone);
            return;
        }
    }

    // iff the patch, center y +- extent, overlaps the boss zone, where only
    // the boss fight grows anything
    private static bool IsPatchInBossZone(
        RampedDifficultyManager difficulty, float centerY, float extent) {
        return difficulty.IsInBossZoneY(centerY - extent)
            || difficulty.IsInBossZoneY(centerY)
            || difficulty.IsInBossZoneY(centerY + extent);
    }

    private bool IsClear(Vector3 center, float angle, float extent, float arc) {
        foreach (RoughBarkZone patch in patches) {
            // Disjoint vertical bounds prevent neighboring arcs joining
            // into a complete ring. Each arc leaves at least 190 dry
            // degrees.
            if (patch != null
                && Mathf.Abs(patch.transform.position.y - center.y)
                    < extent
                        + RoughBarkZone.GetVerticalExtent(patch.HeightU)
                        + patchSeparationU) {
                return false;
            }
        }

        int steps = Mathf.Max(
            1,
            Mathf.CeilToInt(
                radiusU * arc * Mathf.Deg2Rad / obstacleClearanceU));
        for (int i = 0; i <= steps; i++) {
            float sample =
                (angle + (i / (float)steps - 0.5f) * arc) * Mathf.Deg2Rad;
            Vector3 point = center
                + new Vector3(Mathf.Sin(sample), 0f, Mathf.Cos(sample))
                    * radiusU;
            foreach (Collider hit in Physics.OverlapCapsule(
                point - Vector3.up * extent,
                point + Vector3.up * extent,
                obstacleClearanceU,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide)
            ) {
                if (hit.GetComponentInParent<BranchController>() != null
                    || hit.GetComponentInParent<SpiderWebTrap>() != null
                    || hit.GetComponentInParent<FallingCobraController>()
                        != null) {
                    return false;
                }
            }
        }
        return true;
    }
}
