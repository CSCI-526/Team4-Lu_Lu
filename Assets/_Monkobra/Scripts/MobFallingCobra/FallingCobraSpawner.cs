using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Hangs dormant <see cref="FallingCobra"/>s on the trunk of each
/// <see cref="DynamicTreeSegmentPrefabRoot"/> as soon as it has rolled its
/// branches, on its first Start and after every restart by
/// <see cref="DTSPool"/>, by listening to
/// <see cref="DynamicTreeSegmentPrefabRoot.BranchesGenerated"/>. Put one on
/// any object active from scene load, never under the pool's mock tree,
/// which is deactivated at runtime.
/// <para>
/// Rolled like branches: a fixed attempt count per segment, each attempt
/// against the falling cobra probability curve of
/// <see cref="GameBalanceConfig"/> sampled at the segment's ramped
/// difficulty. Each cobra is a child of its segment, so a restart's clear
/// removes it.
/// </para>
/// <para>
/// A cobra hangs at a random angle on a ring of <c>spawnRadiusU</c> about the
/// segment's pivot, which sits on the trunk axis, at a random height that
/// keeps the whole snake inside the segment. A spot too close to another
/// cobra of the segment is retried, and skipped when retries run out.
/// </para>
/// </summary>
public class FallingCobraSpawner: MonoBehaviour {
    // Inspector Fields  #######################################################
    [Header("Wiring")]
    [SerializeField]
    [FormerlySerializedAs("fallingCobraPrefab")]
    [Tooltip("falling cobra prefab, spawned dormant on a segment")]
    private FallingCobra cobraPrefab;

    [SerializeField]
    [Tooltip("game balance tuning; cobra attempts, chance & speeds")]
    private GameBalanceConfig balanceConfig;

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
    [Tooltip("position retries per cobra b4 skipping it")]
    private int maxPlacementAttempts = 20;

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
        if (balanceConfig == null) {
            Debug.LogWarning(
                "FallingCobraSpawner:\tmust assign Inspector Field: "
                    + "balanceConfig",
                this
            );
        }
        if (cobraPrefab == null || balanceConfig == null) {
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
        DynamicTreeSegmentPrefabRoot.BranchesGenerated += OnBranchesGenerated;
    }

    private void OnDisable() {
        if (listener != this) {
            return;
        }
        DynamicTreeSegmentPrefabRoot.BranchesGenerated -= OnBranchesGenerated;
        listener = null;
    }

    // Editor Validation  ######################################################
    private void OnValidate() {
        spawnRadiusU = Mathf.Max(0f, spawnRadiusU);
        minCobraSeparationU = Mathf.Max(0f, minCobraSeparationU);
        maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);
    }

    // Event Handlers  #########################################################
    private void OnBranchesGenerated(DynamicTreeSegmentPrefabRoot segment) {
        PopulateSegment(segment);
    }

    // private members  ########################################################
    private static FallingCobraSpawner listener; // the one spawner subscribed

    private bool hasWarnedNoDifficulty; // missing service logged once
    private readonly List<FallingCobra> spawnedCobras = new();

    // private methods  ########################################################
    // rolls this spawner's cobra attempts for segment, which already holds its
    // branches
    private void PopulateSegment(DynamicTreeSegmentPrefabRoot segment) {
        if (segment == null) {
            return;
        }
        RampedDifficultyService difficulty = RampedDifficultyService.I;
        if (difficulty == null) {
            if (!hasWarnedNoDifficulty) {
                Debug.LogWarning(
                    "FallingCobraSpawner:\tno RampedDifficultyService in "
                        + "scene, skip cobras",
                    this
                );
                hasWarnedNoDifficulty = true;
            }
            return;
        }

        ClearCobras(segment);

        // pivots sit mid-segment, so this is the segment's middle difficulty
        float spawnProbability = Mathf.Clamp01(
            balanceConfig.FallingCobraSpawnProbabilityCurve.Evaluate(
                difficulty.GetRampedValueFromYPosition(
                    segment.transform.position.y
                )
            )
        );

        int attemptCount = balanceConfig.FallingCobraAttemptCount;
        for (int i = 0; i < attemptCount; i++) {
            if (Random.value < spawnProbability) {
                TrySpawnCobra(segment);
            }
        }
    }

    // a segment's previous cobras go b4 it rolls again: its restart has
    // already cleared them, any still active are destroyed here, so a
    // segment never carries 2 rolls
    private void ClearCobras(DynamicTreeSegmentPrefabRoot segment) {
        for (int i = spawnedCobras.Count - 1; i >= 0; i--) {
            FallingCobra cobra = spawnedCobras[i];
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

    private bool TrySpawnCobra(DynamicTreeSegmentPrefabRoot segment) {
        Transform segmentXfm = segment.transform;

        // the prefab builds its hit capsule in Awake, so its length is only
        // known on an instance
        FallingCobra cobra = Instantiate(cobraPrefab, segmentXfm);

        // the trunk cylinder is centered on the segment pivot; keep the whole
        // snake, tail to head, inside the segment's own height
        float halfHeight = segment.GetHeight() * 0.5f;
        float tailU = cobra.TailBelowPivotU;
        float minLocalY = -halfHeight + tailU;
        float maxLocalY = halfHeight - (cobra.LengthU - tailU);
        if (maxLocalY < minLocalY) {
            Destroy(cobra.gameObject);
            return false;
        }

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++) {
            Quaternion yaw = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Vector3 localPosition =
                yaw * Vector3.forward * spawnRadiusU
                + Vector3.up * Random.Range(minLocalY, maxLocalY);
            Vector3 position = segmentXfm.TransformPoint(localPosition);
            if (!IsSpotFree(position)) {
                continue;
            }

            cobra.transform.SetPositionAndRotation(
                position,
                segmentXfm.rotation
            );
            Physics.SyncTransforms();
            cobra.Initialize(balanceConfig, segmentXfm.position);
            spawnedCobras.Add(cobra);
            return true;
        }

        Destroy(cobra.gameObject);
        return false;
    }

    private bool IsSpotFree(Vector3 position) {
        foreach (FallingCobra other in spawnedCobras) {
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
